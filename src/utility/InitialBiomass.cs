//  Copyright 2005-2010 Portland State University, University of Wisconsin
//  Authors:  Robert M. Scheller, James B. Domingo

using Landis.SpatialModeling;
using Landis.Library.UniversalCohorts;
using Landis.Core;
using System.Collections.Generic;
using Landis.Library.InitialCommunities.Universal;
using System;
using System.Security.Policy;
using Landis.Library.Succession.DensitySeeding;

namespace Landis.Extension.Succession.ForC
{
    /// <summary>
    /// The initial live and dead biomass at a site.
    /// </summary>
    public class InitialBiomass
    {
        public SoilClass soilClass;
        
        public double SoilOrganicMatterC;
        public double DeadWoodMass;
        public double LitterMass;
        public double DeadWoodDecayRate;
        public double LitterDecayRate;
        private SiteCohorts cohorts;


        //---------------------------------------------------------------------

        /// <summary>
        /// The site's initial cohorts.
        /// </summary>
        public SiteCohorts Cohorts
        {
            get
            {
                return cohorts;
            }
        }
        
        //---------------------------------------------------------------------

        private InitialBiomass(SiteCohorts cohorts,
        
                double soilOrganicMatterC,
                double deadWoodMass,
                double litterMass,
                double deadWoodDecayRate,
                double litterDecayRate,
                SoilClass soilClass
                )
        {
            this.cohorts = cohorts;
            this.SoilOrganicMatterC = soilOrganicMatterC;
            this.DeadWoodMass = deadWoodMass;
            this.LitterMass = litterMass;
            this.DeadWoodDecayRate = deadWoodDecayRate;
            this.LitterDecayRate = litterDecayRate;
            this.soilClass = soilClass;
        }

        //---------------------------------------------------------------------
        public static SiteCohorts Clone(SiteCohorts site_cohorts)
        {
            SiteCohorts clone = new SiteCohorts();
            foreach (ISpeciesCohorts speciesCohorts in site_cohorts)
                foreach (ICohort cohort in speciesCohorts)
                    clone.AddNewCohort(cohort.Species, cohort.Data.Age, cohort.Data.Biomass, new System.Dynamic.ExpandoObject());  
            return clone;
        }
        //---------------------------------------------------------------------

        private static IDictionary<uint, InitialBiomass> initialSites;
            //  Initial site biomass for each unique pair of initial
            //  community and ecoregion; Key = 32-bit unsigned integer where
            //  high 16-bits is the map code of the initial community and the
            //  low 16-bits is the ecoregion's map code

        private static IDictionary<uint, List<ICohort>> sortedCohorts;
        //  Age cohorts for an initial community sorted from oldest to
            //  youngest.  Key = initial community's map code

        private static ushort successionTimestep;

        //---------------------------------------------------------------------

        private static uint ComputeKey(uint initCommunityMapCode,
                                       ushort ecoregionMapCode)
        {
            return (uint)((initCommunityMapCode << 16) | ecoregionMapCode);
        }

        //---------------------------------------------------------------------

        static InitialBiomass()
        {
            initialSites = new Dictionary<uint, InitialBiomass>();
            sortedCohorts = new Dictionary<uint, List<ICohort>>();
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Initializes this class.
        /// </summary>
        /// <param name="timestep">
        /// The plug-in's timestep.  It is used for growing biomass cohorts.
        /// </param>
        public static void Initialize(int timestep)
        {
            successionTimestep = (ushort) timestep;
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Computes the initial biomass at a site.
        /// </summary>
        /// <param name="site">
        /// The selected site.
        /// </param>
        /// <param name="initialCommunity">
        /// The initial community of age cohorts at the site.
        /// </param>
        public static InitialBiomass Compute(ActiveSite site,
                                             ICommunity initialCommunity)
        {
            IEcoregion ecoregion = PlugIn.ModelCore.Ecoregion[site];
            uint key = ComputeKey(initialCommunity.MapCode, ecoregion.MapCode);
            InitialBiomass initialBiomass;
            if (initialSites.TryGetValue(key, out initialBiomass))
                return initialBiomass;

            //  If we don't have a sorted list of age cohorts for the initial
            //  community, make the list
            List<ICohort> sortedAgeCohorts;
            if (!sortedCohorts.TryGetValue(initialCommunity.MapCode, out sortedAgeCohorts))
            {
                sortedAgeCohorts = SortCohorts(initialCommunity.Cohorts);
                sortedCohorts[initialCommunity.MapCode] = sortedAgeCohorts;
            }

            SiteCohorts cohorts = MakeBiomassCohorts(sortedAgeCohorts, site);
            initialBiomass = new InitialBiomass(
                        cohorts,
                        SiteVars.SoilOrganicMatterC[site],
                        SiteVars.DeadWoodMass[site].Mass,
                        SiteVars.LitterMass[site].Mass,
                        SiteVars.DeadWoodDecayRate[site],
                        SiteVars.LitterDecayRate[site],
                        SiteVars.soilClass[site]
                        );

            initialSites[key] = initialBiomass;
            return initialBiomass;
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Makes a list of age cohorts in an initial community sorted from
        /// oldest to youngest.
        /// </summary>
        public static List<ICohort> SortCohorts(List<ISpeciesCohorts> sppCohorts)
        {
            List<ICohort> cohorts = new List<ICohort>();
            foreach (ISpeciesCohorts speciesCohorts in sppCohorts)
            {
                foreach (ICohort cohort in speciesCohorts)
                    cohorts.Add(cohort);
            }
            cohorts.Sort(Landis.Library.UniversalCohorts.Util.WhichIsOlderCohort);
            return cohorts;
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// A method that computes the initial biomass for a new cohort at a
        /// site based on the existing cohorts.
        /// </summary>
        public delegate int ComputeMethod(ISpecies species,
                                             SiteCohorts siteCohorts,
                                             ActiveSite  site);
        //---------------------------------------------------------------------

        /// <summary>
        /// Makes the set of biomass cohorts at a site based on the age cohorts
        /// at the site, using a specified method for computing a cohort's
        /// initial biomass.
        /// </summary>
        /// <param name="ageCohorts">
        /// A sorted list of age cohorts, from oldest to youngest.
        /// </param>
        /// <param name="site">
        /// Site where cohorts are located.
        /// </param>
        /// <param name="initialBiomassMethod">
        /// The method for computing the initial biomass for a new cohort.
        /// </param>
        public static SiteCohorts MakeBiomassCohorts(List<ICohort> ageCohorts,
                                                     ActiveSite site,
                                                     ComputeMethod initialBiomassMethod)
        {
            SiteVars.Cohorts[site] = new SiteCohorts();
            
            if (ageCohorts.Count == 0)
                return SiteVars.Cohorts[site];

            if (SoilVars.iParams.BiomassSpinUpFlag == 0)
            {
                foreach (var cohort in ageCohorts)
                {
                    SiteVars.Cohorts[site].AddNewCohort(cohort.Species, cohort.Data.Age,
                                                cohort.Data.Biomass, cohort.Data.AdditionalParameters);
                    SiteVars.TotalBiomass[site] = Library.UniversalCohorts.Cohorts.ComputeNonYoungBiomass(SiteVars.Cohorts[site]);
                    SiteVars.soilClass[site].CollectBiomassMortality(cohort.Species, 0, 0, 0, 0);      //dummy for getting it to recognize that the species is now present.
                }
            }
            else
            {
                SpinUpBiomassCohorts(ageCohorts, site, initialBiomassMethod);
            }

            if (SoilVars.iParams.SoilSpinUpFlag == 0)
            {
                ReadSoilValuesFromTable(site);
            }
            else
            {
                SiteVars.soilClass[site].SpinupSoils(site);
            }
            SiteVars.soilClass[site].LastInitialSoilPass(site);
            return SiteVars.Cohorts[site];
        }

        private static void ReadSoilValuesFromTable(ActiveSite site)
        {
            IEcoregion ecoregion = PlugIn.ModelCore.Ecoregion[site];
            foreach (var cohort in SiteVars.Cohorts[site])
            {
                SiteVars.soilClass[site].CalculateDecayRates(ecoregion, cohort.Species, site);
            }
        }

        private static void SpinUpBiomassCohorts(List<ICohort> ageCohorts,
                                                     ActiveSite site,
                                                     ComputeMethod initialBiomassMethod)
        {
            int indexNextAgeCohort = 0;
            //  The index in the list of sorted age cohorts of the next
            //  cohort to be considered

            //  Loop through time from -N to 0 where N is the oldest cohort.
            //  So we're going from the time when the oldest cohort was "born"
            //  to the present time (= 0).  Because the age of any age cohort
            //  is a multiple of the succession timestep, we go from -N to 0
            //  by that timestep.  NOTE: the case where timestep = 1 requires
            //  special treatment because if we start at time = -N with a
            //  cohort with age = 1, then at time = 0, its age will N+1 not N.
            //  Therefore, when timestep = 1, the ending time is -1.
            int endTime = (successionTimestep == 1) ? -1 : 0;
            for (int time = -(ageCohorts[0].Data.Age); time <= endTime; time += successionTimestep)
            {
                //EcoregionData.GenerateNewClimate(time, successionTimestep);           //LANDIS CLIMATE LIBRARY
                EcoregionData.GetAnnualTemperature(successionTimestep, 0);                        //ForCS CLIMATE
                SpeciesData.GenerateNewANPPandMaxBiomass(successionTimestep, time);
                //  Grow current biomass cohorts.

                if (time == endTime)
                {
                    SiteVars.soilClass[site].lastAge = ageCohorts[0].Data.Age;       //signal both that the spin-up is done, and the oldest age
                    SiteVars.soilClass[site].bKillNow = false;
                    //PlugIn.ModelCore.UI.WriteLine("InitialBiomass age={0}, site={1}", ageCohorts[0].Age, site);
                }

                PlugIn.GrowCohorts(site, successionTimestep, true);

                //  Add those cohorts that were born at the current year
                while (indexNextAgeCohort < ageCohorts.Count &&
                       ageCohorts[indexNextAgeCohort].Data.Age == -time)
                {
                    ISpecies species = ageCohorts[indexNextAgeCohort].Species;
                    int initialBiomass = initialBiomassMethod(species, SiteVars.Cohorts[site], site);

                    SiteVars.Cohorts[site].AddNewCohort(ageCohorts[indexNextAgeCohort].Species, 1,
                                                initialBiomass, new System.Dynamic.ExpandoObject());
                    SiteVars.soilClass[site].CollectBiomassMortality(species, 0, 0, 0, 0);      //dummy for getting it to recognize that the species is now present.

                    indexNextAgeCohort++;
                }
                if (time == endTime)
                {
                    SiteVars.soilClass[site].lastAge = ageCohorts[0].Data.Age;       //signal both that the spin-up is done, and the oldest age
                    SiteVars.soilClass[site].bKillNow = false;
                    // PlugIn.ModelCore.UI.WriteLine("InitialBiomass age={0}, site={1}", ageCohorts[0].Age, site);
                }
                else if (time == endTime - 1)   //just before the last timestep, we want to send a signal to the growth routine to kill the 
                {                               //cohorts that need to be snags. Hardwire this first for testing.
                    SiteVars.soilClass[site].bKillNow = true;
                }

            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Makes the set of biomass cohorts at a site based on the age cohorts
        /// at the site, using the default method for computing a cohort's
        /// initial biomass.
        /// </summary>
        /// <param name="ageCohorts">
        /// A sorted list of age cohorts, from oldest to youngest.
        /// </param>
        /// <param name="site">
        /// Site where cohorts are located.
        /// </param>
        public static SiteCohorts MakeBiomassCohorts(
                                                     List<ICohort> ageCohorts,
                                                     ActiveSite site)
        {
            return MakeBiomassCohorts(ageCohorts, site,
                                      CohortBiomass.InitialBiomass);
        }
    }
}
