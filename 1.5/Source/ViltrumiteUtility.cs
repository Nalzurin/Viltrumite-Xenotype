using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Viltrumites
{
    public static class ViltrumiteUtility
    {
        public static Hediff GetHediffDisrupted(Pawn pawn)
        {
            return pawn.health.hediffSet.GetFirstHediffOfDef(Definitions.V_InnerEarDisrupted);
        }
        public static Hediff GetHediffEquilibrium(Pawn pawn)
        {
            return pawn.health.hediffSet.GetFirstHediffOfDef(Definitions.V_InnerEarEquilibrium);
        }
    }
}
