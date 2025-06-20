using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;
using UnityEngine;
using Verse;

namespace Viltrumites
{

    [HarmonyPatch]
    public static class CaravanPathViltrumite_Patch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Caravan_PathFollower), "IsPassable");
        }
        private static bool Postfix(bool result, Caravan ___caravan)
        {
            
            if (___caravan.PawnsListForReading.Count == 1 && ___caravan.PawnsListForReading[0].genes.HasActiveGene(Definitions.Flight))
            {

                return true;
            }
            return result;
        }
    }


    [HarmonyPatch]
    public static class CaravanPathCostViltrumite_Patch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Caravan_PathFollower), "CostToMove", new Type[] {typeof(int), typeof(int)});
        }
        private static int Postfix(int result, Caravan ___caravan)
        {

            if (___caravan.PawnsListForReading.Count == 1 && ___caravan.PawnsListForReading[0].genes.HasActiveGene(Definitions.Flight))
            {

                return 1;
            }
            return result;
        }
    }
    [HarmonyPatch]
    public static class ViltrumiteEMPDamage_Patch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(DamageWorker), "Apply");
        }
        private static bool Prefix(DamageInfo dinfo, Thing victim)
        {
            if(victim is Pawn pawn)
            {
                Log.Message("Checking if pawn is viltrumite");
                if (dinfo.Def == DamageDefOf.EMP && pawn.genes.HasActiveGene(Definitions.SensitiveInnerEars))
                {
                    pawn.health.AddHediff(Definitions.V_InnerEarDisrupted);
                }
            }
          

            return true;
        }
    }
}
