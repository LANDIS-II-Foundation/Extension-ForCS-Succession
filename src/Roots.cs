//  Authors:  Caren Dymond, Sarah Beukema

using Landis.SpatialModeling;
using Landis.Core;
using System.Collections.Generic;
using Landis.Utilities;

namespace Landis.Extension.Succession.ForC
{
    /// <summary>
    /// Fine and coarse roots.
    /// </summary>
    public class Roots
    {
        public static double CoarseRoot = 0.0;
        public static double FineRoot = 0.0;
        public static double CoarseRootTurnover = 0.0;
        public static double FineRootTurnover = 0.0;


        //---------------------------------------------------------------------

        /// <summary>
        /// Kills coarse roots and add the biomass directly to the SOC pool.
        /// </summary>
        public static void AddCoarseRootLitter(double abovegroundWoodBiomass,
                                    ISpecies   species,
                                    ActiveSite site)
        {

            double coarseRootBiomass = CalculateCoarseRoot(abovegroundWoodBiomass); // Ratio above to below

            if(coarseRootBiomass > 0)
            {
                SiteVars.SoilOrganicMatterC[site] += coarseRootBiomass * 0.47;  // = convert to g C / m2
            }
        }

        //---------------------------------------------------------------------
        /// <summary>
        /// Kills fine roots and add the biomass directly to the SOC pool.
        /// </summary>
        public static void AddFineRootLitter(double abovegroundFoliarBiomass, 
                                      ISpecies   species,
                                      ActiveSite site)
        {
            double fineRootBiomass = CalculateFineRoot(abovegroundFoliarBiomass); 
            
            if(fineRootBiomass > 0)
            {
                SiteVars.SoilOrganicMatterC[site] += fineRootBiomass * 0.47;  // = convert to g C / m2
            }    
            
        }
        
        /// <summary>
        /// Calculate coarse and fine roots based on total aboveground biomass.
        /// Niklas & Enquist 2002: 25% of total stocks
        /// </summary>
        public static double CalculateCoarseRoot(double abio)
        {
            return (abio * 0.24);
        }
        public static double CalculateFineRoot(double abio)
        {
            return (abio * 0.06);
        }

        /// <summary>
        /// New ways of doing the roots (Nov 2011):
        /// Calculate coarse and fine roots based on woody biomass.
        /// These are no longer straight percentages.
        /// </summary>

        public static double CalculateRootBiomass(ActiveSite site, ISpecies species, double abio)
        {
            IEcoregion ecoregion = PlugIn.ModelCore.Ecoregion[site];

            double totalroot = 0.0;
            int i = 0;
            for (i = 0; i < 4; i++)
            {
                if (SpeciesData.MinWoodyBio[species][ecoregion][i + 1] > -999)
                {
                    if (abio >= SpeciesData.MinWoodyBio[species][ecoregion][i] &&
                        abio < SpeciesData.MinWoodyBio[species][ecoregion][i + 1])
                        break;
                }
                else
                    break;
            }

            totalroot = abio * SpeciesData.Ratio[species][ecoregion][i];
            FineRoot = totalroot * SpeciesData.PropFine[species][ecoregion][i];
            CoarseRoot = totalroot - FineRoot;
            return (totalroot);
        }

        public static void CalculateRootTurnover(ActiveSite site, ISpecies species, double abio)
        {
            IEcoregion ecoregion = PlugIn.ModelCore.Ecoregion[site];
            
            double totalroot = 0.0;
            int i = 0;
            totalroot = CalculateRootBiomass(site, species, abio);

            for (i = 0; i < 4; i++)
            {
                if (SpeciesData.MinWoodyBio[species][ecoregion][i + 1] > -999)
                {
                    if (abio >= SpeciesData.MinWoodyBio[species][ecoregion][i] &&
                        abio < SpeciesData.MinWoodyBio[species][ecoregion][i + 1])
                        break;
                }
                else
                    break;
            }

            CoarseRootTurnover = CoarseRoot * SpeciesData.CoarseTurnover[species][ecoregion][i];
            FineRootTurnover = FineRoot * SpeciesData.FineTurnover[species][ecoregion][i];
        }   
    }


    //--------------------------------------------------------
    // A place to hold the initial snag information
    //--------------------------------------------------------
    public class Snags
    {

        public static bool bSnagsPresent = false;
        private double[,] BioSnag = new double[2, PlugIn.ModelCore.Species.Count];
        public const int NUMSNAGS = 1000;

        public static int[] DiedAt = new int[NUMSNAGS];
        public static int[] initSpecIdx = new int[NUMSNAGS];
        public static int[] initSnagAge = new int[NUMSNAGS];
        public static string[] initSnagDist = new string[NUMSNAGS];
        public static bool[] bSnagsUsed = new bool[NUMSNAGS];     //flag for if this site contained this snag type.

        public static void Initialize(IInputSnagParms parameters)
        {
            if (parameters != null)
            {
                bSnagsPresent = true;
                for (int i = 0; i < NUMSNAGS; i++)
                {
                    DiedAt[i] = parameters.SnagAgeAtDeath[i];
                    initSnagAge[i] = parameters.SnagTimeSinceDeath[i];
                    initSpecIdx[i] = parameters.SnagSpecies[i];
                    initSnagDist[i] = parameters.SnagDisturb[i];
                    bSnagsUsed[i] = false;
                }
            }
        }

    }

}
