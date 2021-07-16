//  Authors:  Caren Dymond, Sarah Beukema

using Landis.SpatialModeling;
using Landis.Core;
using Landis.Library.BiomassCohorts;
using System.Collections.Generic;
using Landis.Utilities;

namespace Landis.Extension.Succession.ForC.AgeOnlyDisturbances
{
    /// <summary>
    /// The handlers for various type of events related to age-only
    /// disturbances.
    /// </summary>
    public static class Events
    {
        public static void CohortDied(object         sender,
                                      DeathEventArgs eventArgs)
        {
            ExtensionType disturbanceType = eventArgs.DisturbanceType;
            //PoolPercentages cohortReductions = Module.Parameters.CohortReductions[disturbanceType];

            ICohort cohort = eventArgs.Cohort;
            ActiveSite site = eventArgs.Site;

            if (disturbanceType.IsMemberOf("disturbance:harvest"))      //base harvest doesn't get here ever, but biomass harvest does.
                return;                                                 //It has already been accounted for, however in PlugIn.CohortDied, so don't repeat here.

            double foliar = (double) cohort.ComputeNonWoodyBiomass(site);
            double wood = ((double) cohort.Biomass - foliar);
            
            SiteVars.soilClass[site].DisturbanceImpactsBiomass(site, cohort.Species, cohort.Age, wood, foliar, disturbanceType.Name, 0);  
        }

        //---------------------------------------------------------------------

        private static double ReduceInput(double     poolInput,
                                          Percentage reductionPercentage)
        {
            double reduction = (poolInput * (double) reductionPercentage);
            return (poolInput - reduction);
        }

        //---------------------------------------------------------------------

        public static void SiteDisturbed(object               sender,
                                         DisturbanceEventArgs eventArgs)
        {
            //Note that SiteDisturbed is called before Cohort Death, for a disturbed site.
            //Also, SiteDisturbed is called even if cohort death has not occurred.

            ExtensionType disturbanceType = eventArgs.DisturbanceType;
            //PoolPercentages poolReductions = Module.Parameters.PoolReductions[disturbanceType];

            ActiveSite site = eventArgs.Site;
            //SiteVars.DeadWoodMass[site] *= poolReductions.Wood;
            
            //SiteVars.LitterMass[site] *= poolReductions.Foliar;

            SiteVars.soilClass[site].DisturbanceImpactsDOM(site, disturbanceType.Name, 0);
        }
    }
}
