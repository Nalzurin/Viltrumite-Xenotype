using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Viltrumites
{
    public class Gene_SuperhumanResilience : Gene
    {
        public override void PostRemove()
        {
            Hediff hediff = GetHediff();
            if (hediff != null)
            {
                pawn.health.RemoveHediff(hediff);
            }
            base.PostRemove();
        }

        public override void PostAdd()
        {
            base.PostAdd();
            ApplyHediff();
        }

        public override void Tick()
        {
            base.Tick();
            if ((pawn.Spawned && ViltrumiteUtility.GetHediffDisrupted(pawn) != null && GetHediff() != null) || ViltrumiteUtility.GetHediffEquilibrium(pawn) == null)
            {
                RemoveHediff();
            }
            else if (pawn.Spawned && pawn.IsHashIntervalTick(2000) && !Rand.Chance(0.75f) && GetHediff() == null && Rand.Chance(0.5f))
            {
                ApplyHediff();
            }
        }

        public Hediff GetHediff()
        {
            return pawn.health.hediffSet.GetFirstHediffOfDef(Definitions.V_SuperhumanResilience);
        }

        public void ApplyHediff()
        {
            if (GetHediff() == null)
            {
                Hediff hediff = HediffMaker.MakeHediff(Definitions.V_SuperhumanResilience, pawn);
                pawn.health.AddHediff(hediff);
            }
        }
        public void RemoveHediff()
        {
            Hediff hediff = GetHediff();
            if (hediff != null)
            {
                pawn.health.RemoveHediff(hediff);
            }
        }

    }

}
