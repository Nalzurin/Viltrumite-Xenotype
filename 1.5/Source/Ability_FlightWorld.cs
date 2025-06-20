using RimWorld.Planet;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using HarmonyLib;
using Verse.AI.Group;
using LudeonTK;
using Verse.Noise;
namespace Viltrumites
{
    public class Ability_FlightWorld : Ability
    {
        Texture2D TargeterMouseAttachment;
        public List<CompTransporter> TransportersInGroup; 
        private CompTransporter cachedCompTransporter;

        private ThingWithComps transporter;
        Map Map;
        public (Texture2D, int) CurPawnTexture = default((Texture2D, int));
        public int MaxLaunchDistance = 5000;
        public CompTransporter Transporter;
        bool doLaunch = false;
        Pawn tempPawn;
        public Ability_FlightWorld()
        {
        }

        public Ability_FlightWorld(Pawn pawn)
            : base(pawn)
        {
        }

        public Ability_FlightWorld(Pawn pawn, Precept sourcePrecept)
            : base(pawn, sourcePrecept)
        {
        }

        public Ability_FlightWorld(Pawn pawn, AbilityDef def)
            : base(pawn, def)
        {
        }

        public Ability_FlightWorld(Pawn pawn, Precept sourcePrecept, AbilityDef def)
            : base(pawn, sourcePrecept, def)
        {
        }

        public static Texture2D MakeReadableTextureInstance(RenderTexture source)
        {
            RenderTexture temporary = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            temporary.name = "MakeReadableTexture_Temp";
            Graphics.Blit(source, temporary);
            RenderTexture active = RenderTexture.active;
            RenderTexture.active = temporary;
            Texture2D texture2D = new Texture2D(source.width, source.height);
            texture2D.ReadPixels(new Rect(0f, 0f, temporary.width, temporary.height), 0, 0);
            texture2D.Apply();
            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(temporary);
            return texture2D;
        }


        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Map = pawn.Map;
            IntVec3 pos = pawn.Position;
            base.Activate(target, dest);
            transporter = ThingMaker.MakeThing(ThingDefOf.TransportPod) as ThingWithComps;
            if (cachedCompTransporter != null)
            {
                Transporter = cachedCompTransporter;
            }
            else
            {
                Transporter = transporter.GetComp<CompTransporter>();
            }
            CameraJumper.TryJump(CameraJumper.GetWorldTarget(pawn));
            Find.WorldSelector.ClearSelection();
            int tile = pawn.Map.Tile;
            TargeterMouseAttachment = TargeterMouseAttachmentFunc();
            PawnGenerationRequest request = new PawnGenerationRequest(tile: tile, mustBeCapableOfViolence: false, colonistRelationChanceFactor: 1f, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: true, certainlyBeenInCryptosleep: false, forceRedressWorldPawnIfFormerColonist: false, worldPawnFactionDoesntMatter: false, biocodeWeaponChance: 0.1f, kind: PawnKindDefOf.Colonist, faction:Faction.OfPlayer, forceGenerateNewPawn: true, allowDead: false, allowDowned: true, canGeneratePawnRelations: true, biocodeApparelChance: 1f, validatorPreGear: null, validatorPostGear: null, minChanceToRedressWorldPawn: null, fixedBiologicalAge: null, fixedChronologicalAge: null, fixedLastName: null, fixedBirthName: null, fixedTitle: null, fixedIdeo: null, forceNoIdeo: false, forceNoBackstory: false, forbidAnyTitle: false, forceDead: false, forcedXenogenes: null, forcedEndogenes: null, forcedXenotype: null, forcedCustomXenotype: null, allowedXenotypes: null, forceBaselinerChance: 0f);
            tempPawn = PawnGenerator.GeneratePawn(request);
            Transporter.innerContainer.TryAdd(tempPawn);
            TransporterUtility.InitiateLoading(Gen.YieldSingle(Transporter));
            Find.WorldTargeter.BeginTargeting(ChoseWorldTarget, canTargetTiles: true, TargeterMouseAttachment, closeWorldTabWhenFinished: true, delegate
            {
                GenDraw.DrawWorldRadiusRing(tile, MaxLaunchDistance);
            }, (GlobalTargetInfo target) => TargetingLabelGetter(target, tile, MaxLaunchDistance, new List<IThingHolder> {Transporter }, TryLaunch));

            return true;
        }
        public static string TargetingLabelGetter(GlobalTargetInfo target, int tile, int maxLaunchDistance, IEnumerable<IThingHolder> pods, Action<int, TransportPodsArrivalAction> launchAction)
        {
            if (!target.IsValid)
            {
                return null;
            }
            int num = Find.WorldGrid.TraversalDistanceBetween(tile, target.Tile);
            if (maxLaunchDistance > 0 && num > maxLaunchDistance)
            {
                GUI.color = ColorLibrary.RedReadable;
                return "TransportPodDestinationBeyondMaximumRange".Translate();
            }
            IEnumerable<FloatMenuOption> source = GetOptionsForTile(target.Tile, pods, launchAction);
            if (!source.Any())
            {
                return string.Empty;
            }
            if (source.Count() == 1)
            {
                if (source.First().Disabled)
                {
                    GUI.color = ColorLibrary.RedReadable;
                }
                return source.First().Label;
            }
            if (target.WorldObject is MapParent mapParent)
            {
                return "ClickToSeeAvailableOrders_WorldObject".Translate(mapParent.LabelCap);
            }
            return "ClickToSeeAvailableOrders_Empty".Translate();
        }
        Texture2D TargeterMouseAttachmentFunc()
        {
            if (Find.TickManager.TicksGame - CurPawnTexture.Item2 > 60)
            {
                RenderTexture source = PortraitsCache.Get(pawn, new Vector2(50f, 50f), Rot4.South);
                CurPawnTexture = (MakeReadableTextureInstance(source), Find.TickManager.TicksGame);
            }
            return CurPawnTexture.Item1;
        }

        private bool ChoseWorldTarget(GlobalTargetInfo target)
        {
            return ChoseWorldTarget(target, pawn.Map.Tile, new List<IThingHolder> { Transporter }, MaxLaunchDistance, TryLaunch);
        }

        public static bool ChoseWorldTarget(GlobalTargetInfo target, int tile, IEnumerable<IThingHolder> pods, int maxLaunchDistance, Action<int, TransportPodsArrivalAction> launchAction)
        {

            if (!target.IsValid)
            {
                Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }
            int num = Find.WorldGrid.TraversalDistanceBetween(tile, target.Tile);
            if (maxLaunchDistance > 0 && num > maxLaunchDistance)
            {
                Messages.Message("TransportPodDestinationBeyondMaximumRange".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }
            IEnumerable<FloatMenuOption> source =  GetOptionsForTile(target.Tile, pods, launchAction);
            if (!source.Any())
            {
                if (Find.World.Impassable(target.Tile))
                {
                    Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                    return false;
                }
                launchAction(target.Tile, null);
                return true;
            }
            if (source.Count() == 1)
            {
                if (!source.First().Disabled)
                {
                    source.First().action();
                    return true;
                }
                return false;
            }
            return true;
        }

        public static IEnumerable<FloatMenuOption> GetOptionsForTile(int tile, IEnumerable<IThingHolder> pods, Action<int, TransportPodsArrivalAction> launchAction)
        {
            if (TransportPodsArrivalAction_FormCaravan.CanFormCaravanAt(pods, tile))
            {
                yield return new FloatMenuOption("V_FlyTo".Translate(), delegate
                {
                    launchAction(tile, new TransportPodsArrivalAction_FormCaravan("MessageShuttleArrived"));
                });
            }
           
        }

        public void TryLaunch(int destinationTile, TransportPodsArrivalAction arrivalAction)
        {
            GenSpawn.Spawn(transporter, pawn.Position, Map);
            Transporter.innerContainer.Remove(tempPawn);
            pawn.DeSpawn();
            Transporter.innerContainer.TryAdd(pawn);

            Transporter.TryRemoveLord(Map);
            int groupID = Transporter.groupID;
            CompTransporter compTransporter = Transporter;
            ThingOwner directlyHeldThings = compTransporter.GetDirectlyHeldThings();
            ActiveDropViltrumite activeDropPawn = (ActiveDropViltrumite)ThingMaker.MakeThing(Definitions.V_ActiveDropViltrumite);
            activeDropPawn.pawn = pawn;
            activeDropPawn.Contents = new ActiveDropPodInfo();
            activeDropPawn.Contents.despawnPodBeforeSpawningThing = true;
            activeDropPawn.Contents.openDelay = 0;
            activeDropPawn.Contents.innerContainer.TryAddRangeOrTransfer(directlyHeldThings, canMergeWithExistingStacks: true, destroyLeftover: true);
            ViltrumiteLeaving pawnLeaving = (ViltrumiteLeaving)SkyfallerMaker.MakeSkyfaller(Definitions.V_ViltrumiteLeaving, activeDropPawn);
            pawnLeaving.pawn = pawn;
            pawnLeaving.groupID = groupID;
            pawnLeaving.destinationTile = destinationTile;
            pawnLeaving.worldObjectDef = Definitions.V_TravelingViltrumite;
            pawnLeaving.arrivalAction = arrivalAction;
            compTransporter.CleanUpLoadingVars(Map);
            compTransporter.parent.Destroy();
            GenSpawn.Spawn(pawnLeaving, compTransporter.parent.Position, Map);
            CameraJumper.TryHideWorld();
            transporter = null;
            cachedCompTransporter = null;
        }
    }
    public class ActiveDropViltrumite : ActiveDropPod
    {
        public Pawn pawn;

        public override Graphic Graphic => pawn.Graphic;

        public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
        {
            pawn.Drawer.renderer.RenderPawnAt(DrawPos, Rot4.South);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref pawn, "pawn");
        }
    }

    public class ViltrumiteLeaving : FlyShipLeaving
    {
        private static readonly List<Thing> tmpActiveDropPods = new List<Thing>();

        public Pawn pawn;

        public override Graphic Graphic => pawn.Graphic;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            ticksToDiscard = 500;
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float num = 0f;
            if (def.skyfaller.rotateGraphicTowardsDirection)
            {
                num = angle;
            }
            if (def.skyfaller.angleCurve != null)
            {
                angle = def.skyfaller.angleCurve.Evaluate(base.TimeInAnimation);
            }
            if (def.skyfaller.rotationCurve != null)
            {
                num += def.skyfaller.rotationCurve.Evaluate(base.TimeInAnimation);
            }
            if (def.skyfaller.xPositionCurve != null)
            {
                drawLoc.x += def.skyfaller.xPositionCurve.Evaluate(base.TimeInAnimation);
            }
            if (def.skyfaller.zPositionCurve != null)
            {
                drawLoc.z += def.skyfaller.zPositionCurve.Evaluate(base.TimeInAnimation);
            }
            pawn.Drawer.renderer.RenderPawnAt(drawLoc, pawn.Rotation);
            DrawDropSpotShadow();
        }

        protected override void LeaveMap()
        {
            Traverse<bool> traverse = Traverse.Create(this).Field<bool>("alreadyLeft");
            if (traverse.Value || !createWorldObject)
            {
                if (base.Contents != null)
                {
                    foreach (Thing item in (IEnumerable<Thing>)base.Contents.innerContainer)
                    {
                        if (item is Pawn pawn)
                        {
                            pawn.ExitMap(allowedToJoinOrCreateCaravan: false, Rot4.Invalid);
                        }
                    }
                    base.Contents.innerContainer.ClearAndDestroyContentsOrPassToWorld(DestroyMode.QuestLogic);
                }
                base.LeaveMap();
                return;
            }
            if (groupID < 0)
            {
                Log.Error("Drop pod left the map, but its group ID is " + groupID);
                Destroy();
                return;
            }
            if (destinationTile < 0)
            {
                Log.Error("Drop pod left the map, but its destination tile is " + destinationTile);
                Destroy();
                return;
            }
            Lord lord = TransporterUtility.FindLord(groupID, base.Map);
            if (lord != null)
            {
                base.Map.lordManager.RemoveLord(lord);
            }
            TravelingViltrumite travelingPawn = (TravelingViltrumite)WorldObjectMaker.MakeWorldObject(worldObjectDef);
            travelingPawn.pawn = this.pawn;
            travelingPawn.Tile = base.Map.Tile;
            travelingPawn.SetFaction(Faction.OfPlayer);
            travelingPawn.destinationTile = destinationTile;
            travelingPawn.arrivalAction = arrivalAction;
            Find.WorldObjects.Add(travelingPawn);
            tmpActiveDropPods.Clear();
            tmpActiveDropPods.AddRange(base.Map.listerThings.ThingsInGroup(ThingRequestGroup.ActiveDropPod));
            for (int i = 0; i < tmpActiveDropPods.Count; i++)
            {
                if (tmpActiveDropPods[i] is FlyShipLeaving flyShipLeaving && flyShipLeaving.groupID == groupID)
                {
                    Traverse.Create(this).Field("alreadyLeft").SetValue(true);
                    travelingPawn.AddPod(flyShipLeaving.Contents, justLeftTheMap: true);
                    flyShipLeaving.Contents = null;
                    flyShipLeaving.Destroy();
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref pawn, "pawn");
        }
    }
    public class TravelingViltrumite : TravelingTransportPods
    {
        [HarmonyPatch(typeof(TravelingTransportPods), "Arrived")]
        public static class TravelingTransportPods_Arrived
        {
            public static bool Prefix(TravelingTransportPods __instance)
            {
                if (__instance is TravelingViltrumite travelingViltrumite)
                {
                    travelingViltrumite.ArrivedOverride();
                    return false;
                }
                return true;
            }
        }

        [TweakValue("00", 0f, 1f)]
        public static float drawTest = 0.083f;

        public Pawn pawn;

        public override Material Material => MaterialPool.MatFrom(new MaterialRequest(PortraitsCache.Get(pawn, new Vector2(50f, 50f), Rot4.South)));

        public override string Label => pawn.Label;

        public override string LabelShort => pawn.LabelShort;

        public override string LabelShortCap => pawn.LabelShortCap;

        public override void Draw()
        {
            float averageTileSize = Find.WorldGrid.averageTileSize;
            Vector3 drawPos = DrawPos;
            float num = 0.7f * averageTileSize;
            float num2 = drawTest;
            Material material = Material;
            Vector3 normalized = drawPos.normalized;
            Vector3 upwards = normalized;
            Quaternion q = Quaternion.LookRotation(Vector3.up, upwards);
            Vector3 s = new Vector3(num, 1f, num);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(drawPos + normalized * num2, q, s);
            int worldLayer = WorldCameraManager.WorldLayer;
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, worldLayer);
        }

        public void ArrivedOverride()
        {
            Traverse traverse = Traverse.Create(this).Field("arrived");
            Traverse traverse2 = Traverse.Create(this).Field("pods");
            if (traverse.GetValue<bool>())
            {
                return;
            }
            traverse.SetValue(true);
            if (arrivalAction == null || !arrivalAction.StillValid(traverse2.GetValue<List<ActiveDropPodInfo>>(), destinationTile))
            {
                arrivalAction = null;
                List<Map> maps = Find.Maps;
                for (int i = 0; i < maps.Count; i++)
                {
                    if (maps[i].Tile == destinationTile)
                    {
                        arrivalAction = new ViltrumiteArrivalAction_LandInSpecificCell(maps[i].Parent, pawn);
                        break;
                    }
                }
                if (arrivalAction == null)
                {
                    Map orGenerateMap = GetOrGenerateMapUtility.GetOrGenerateMap(destinationTile, null);
                    arrivalAction = new ViltrumiteArrivalAction_LandInSpecificCell(orGenerateMap.Parent, pawn);
                }
            }
            if (arrivalAction != null && arrivalAction.ShouldUseLongEvent(traverse2.GetValue<List<ActiveDropPodInfo>>(), destinationTile))
            {
                LongEventHandler.QueueLongEvent(delegate
                {
                    AccessTools.Method(typeof(TravelingTransportPods), "DoArrivalAction").Invoke(this, null);
                }, "GeneratingMapForNewEncounter", doAsynchronously: false, null);
            }
            else
            {
                AccessTools.Method(typeof(TravelingTransportPods), "DoArrivalAction").Invoke(this, null);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref pawn, "pawn");
        }
    }

    public class ViltrumiteArrivalAction_LandInSpecificCell : TransportPodsArrivalAction_LandInSpecificCell
    {
        private MapParent mapParent;

        private Pawn pawn;

        public ViltrumiteArrivalAction_LandInSpecificCell()
        {
        }

        public ViltrumiteArrivalAction_LandInSpecificCell(MapParent mapParent, Pawn pawn)
        {
            this.mapParent = mapParent;
            this.pawn = pawn;
        }

        public override void Arrived(List<ActiveDropPodInfo> pods, int tile)
        {
            Thing lookTarget = TransportPodsArrivalActionUtility.GetLookTarget(pods);
            DropTravelingPawns(pods, mapParent.Map.Center, mapParent.Map);
            Messages.Message("V_ViltrumiteArrived".Translate(lookTarget.Named("PAWN")), lookTarget, MessageTypeDefOf.TaskCompletion);
        }

        public void DropTravelingPawns(List<ActiveDropPodInfo> dropPods, IntVec3 near, Map map)
        {
            TransportPodsArrivalActionUtility.RemovePawnsFromWorldPawns(dropPods);
            for (int i = 0; i < dropPods.Count; i++)
            {
                DropCellFinder.TryFindDropSpotNear(near, map, out var result, allowFogged: false, canRoofPunch: true);
                MakePawnDropAt(result, map, dropPods[i]);
            }
        }

        public void MakePawnDropAt(IntVec3 c, Map map, ActiveDropPodInfo info)
        {
            ActiveDropViltrumite activeDropPawn = (ActiveDropViltrumite)ThingMaker.MakeThing(Definitions.V_ActiveDropViltrumite);
            activeDropPawn.pawn = this.pawn;
            activeDropPawn.Contents = info;
            ViltrumiteIncoming pawnIncoming = SkyfallerMaker.SpawnSkyfaller(Definitions.V_ViltrumiteIncoming, activeDropPawn, c, map) as ViltrumiteIncoming;
            pawnIncoming.pawn = activeDropPawn.Contents.innerContainer.First() as Pawn;
            foreach (Thing item in (IEnumerable<Thing>)activeDropPawn.Contents.innerContainer)
            {
                if (item is Pawn pawn && pawn.IsWorldPawn())
                {
                    Find.WorldPawns.RemovePawn(pawn);
                    pawn.psychicEntropy?.SetInitialPsyfocusLevel();
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_References.Look(ref mapParent, "mapParent");
        }
    }

    public class ViltrumiteIncoming : DropPodIncoming
    {
        public Pawn pawn;

        public override Graphic Graphic => pawn.Graphic;

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            float num = 0f;
            if (def.skyfaller.rotateGraphicTowardsDirection)
            {
                num = angle;
            }
            if (def.skyfaller.angleCurve != null)
            {
                angle = def.skyfaller.angleCurve.Evaluate(base.TimeInAnimation);
            }
            if (def.skyfaller.rotationCurve != null)
            {
                num += def.skyfaller.rotationCurve.Evaluate(base.TimeInAnimation);
            }
            if (def.skyfaller.xPositionCurve != null)
            {
                drawLoc.x += def.skyfaller.xPositionCurve.Evaluate(base.TimeInAnimation);
            }
            if (def.skyfaller.zPositionCurve != null)
            {
                drawLoc.z += def.skyfaller.zPositionCurve.Evaluate(base.TimeInAnimation);
            }
            pawn.Drawer.renderer.RenderPawnAt(drawLoc, pawn.Rotation);
            DrawDropSpotShadow();
        }
        protected override void Impact()
        {
            base.Impact();
            IntVec3 positionHeld = pawn.PositionHeld;
            Map mapHeld = pawn.MapHeld;
            DamageDef bomb = DamageDefOf.Bomb;
            Pawn instigator = pawn;
            List<Thing> ignoredThings = Gen.YieldSingle((Thing)pawn).ToList();
            GenExplosion.DoExplosion(positionHeld, mapHeld, 15f, bomb, instigator, 15, -1f, null, null, null, null, null, 0f, 1, null, applyDamageToExplosionCellsNeighbors: false, null, 0f, 1, 0f, damageFalloff: false, null, ignoredThings);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref pawn, "pawn");
        }
    }
}
