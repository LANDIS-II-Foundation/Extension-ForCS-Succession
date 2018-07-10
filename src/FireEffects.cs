//  Copyright 2007-2010 Portland State University, University of Wisconsin-Madison
//  Author: Robert Scheller, Ben Sulman

using Landis.Core;
using Landis.SpatialModeling;
using Edu.Wisc.Forest.Flel.Util;
using Landis.Library.BiomassCohorts;

using System;
using System.Collections.Generic;

namespace Landis.Extension.Succession.ForC
{
    public class FireEffects
    {
        public const int mk_nIntensityCount = 5;

        //---------------------------------------------------------------------

        // Crown scorching is when a cohort to loses its foliage but is not killed.
        public static double CrownScorching(ICohort cohort, byte siteSeverity)
        {

            int difference = (int)siteSeverity - cohort.Species.FireTolerance;
            double ageFraction = 1.0 - ((double)cohort.Age / (double)cohort.Species.Longevity);

            if (SpeciesData.Epicormic[cohort.Species])
            {
                if (difference < 0)
                    return 0.5 * ageFraction;
                if (difference == 0)
                    return 0.75 * ageFraction;
                if (difference > 0)
                    return 1.0 * ageFraction;
            }

            //Reset fire severity ???
            //SiteVars.FireSeverity[site] = 0;

            return 0.0;
        }

    }


    public class ForestFloor
    {
        //this routine is called from disturbances such as drought
        //public void DisturbanceImpactsBiomass(ActiveSite site, ISpecies species, int age, double wood, double nonwood, string DistTypeName, int tmpFireSeverity)
        public static void DisturbanceImpactsBiomass(ActiveSite site, ISpecies species, int age, double wood, double nonwood, string DistTypeName, int tmpFireSeverity)
        {
            SiteVars.soilClass[site].DisturbanceImpactsBiomass(site, species, age, wood, nonwood, DistTypeName, 0);  
            int iage = age;
        }
    }
}
