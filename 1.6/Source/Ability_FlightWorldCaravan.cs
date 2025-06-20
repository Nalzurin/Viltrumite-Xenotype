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
using CombatExtended.Compatibility;
using UnityEngine.Experimental.GlobalIllumination;
namespace Viltrumites
{
    // Unused code, not deleting just yet
    public class CompProperties_ViltrumiteLauncher : HediffCompProperties
    {
        public CompProperties_ViltrumiteLauncher()
        {
            compClass = typeof(HediffCompProperties);
        }
    }
    public class CompViltrumiteLauncher : HediffComp
    {
        public CompProperties_ViltrumiteLauncher Props => props as CompProperties_ViltrumiteLauncher;
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

        public void StartChoosingDestination() {
            Caravan car = Pawn.GetCaravan();
            CameraJumper.TryJump(CameraJumper.GetWorldTarget(car));
            Find.WorldSelector.ClearSelection();
            int tile = car.Tile;
            TargeterMouseAttachment = TargeterMouseAttachmentFunc();
            Find.WorldTargeter.BeginTargeting(ChoseWorldTarget, canTargetTiles: true, TargeterMouseAttachment, closeWorldTabWhenFinished: true, delegate
            {
                GenDraw.DrawWorldRadiusRing(tile, MaxLaunchDistance);   
            }, (GlobalTargetInfo target) => TargetingLabelGetter(target, tile, MaxLaunchDistance, new List<IThingHolder> { Transporter }, TryLaunch));

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
       
        public static string TargetingLabelGetter(GlobalTargetInfo target, int tile, int maxLaunchDistance, IEnumerable<IThingHolder> pods, Action<PlanetTile, TransportersArrivalAction> launchAction)
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
                RenderTexture source = PortraitsCache.Get(Pawn, new Vector2(50f, 50f), Rot4.South);
                CurPawnTexture = (MakeReadableTextureInstance(source), Find.TickManager.TicksGame);
            }
            return CurPawnTexture.Item1;
        }

        private bool ChoseWorldTarget(GlobalTargetInfo target)
        {
            return ChoseWorldTarget(target, Pawn.GetCaravan().Tile, new List<IThingHolder> { Transporter }, MaxLaunchDistance, TryLaunch);
        }

        public static bool ChoseWorldTarget(GlobalTargetInfo target, int tile, IEnumerable<IThingHolder> pods, int maxLaunchDistance, Action<PlanetTile, TransportersArrivalAction> launchAction)
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

        public static IEnumerable<FloatMenuOption> GetOptionsForTile(int tile, IEnumerable<IThingHolder> pods, Action<PlanetTile, TransportersArrivalAction> launchAction)
        {
            if (TransportersArrivalAction_FormCaravan.CanFormCaravanAt(pods, tile))
            {
                yield return new FloatMenuOption("V_FlyTo".Translate(), delegate
                {
                    launchAction(tile, new TransportersArrivalAction_FormCaravan("MessageShuttleArrived"));
                });
            }
           
        }

        public void TryLaunch(PlanetTile destinationTile, TransportersArrivalAction arrivalAction)
        {
            Caravan car = Pawn.GetCaravan();
            car.Tile = destinationTile;
        }
    }
}
