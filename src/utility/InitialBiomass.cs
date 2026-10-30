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

        private static IDictionary<ulong, InitialBiomass> initialSites;
            //  Initial site biomass for each unique combination of initial
            //  community, ecoregion and spin-up last-disturbance code; Key =
            //  64-bit unsigned integer where the high 32 bits are the key of
            //  the community and ecoregion (ComputeKey) and the low 32 bits
            //  are the code (0 without SpinUpLastDisturbanceMap)

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
            initialSites = new Dictionary<ulong, InitialBiomass>();
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
            int lastDisturbanceCode;
            SpinUpReturnInterval returnInterval = ReturnIntervalFor(site, out lastDisturbanceCode);
            ulong key = ((ulong)ComputeKey(initialCommunity.MapCode, ecoregion.MapCode) << 32) | (uint)lastDisturbanceCode;
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

            SiteCohorts cohorts = MakeBiomassCohorts(sortedAgeCohorts, site, CohortBiomass.InitialBiomass, returnInterval);
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
        /// <param name="returnInterval">
        /// The site's return-interval spin-up, or null for the spin-up that cycles the present stand's age.
        /// </param>
        public static SiteCohorts MakeBiomassCohorts(List<ICohort> ageCohorts,
                                                     ActiveSite site,
                                                     ComputeMethod initialBiomassMethod,
                                                     SpinUpReturnInterval returnInterval = null)
        {
            SiteVars.Cohorts[site] = new SiteCohorts();
            
            if (ageCohorts.Count == 0)
                return SiteVars.Cohorts[site];

            double[,,] returnIntervalInputs = null;
            double[,] returnIntervalLive = null;
            if (returnInterval != null)
                GrowReturnIntervalStand(ageCohorts, site, initialBiomassMethod, returnInterval.ReturnInterval,
                                        out returnIntervalInputs, out returnIntervalLive);

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
            else if (returnInterval == null)
            {
                SiteVars.soilClass[site].SpinupSoils(site);
            }
            else
            {
                SiteVars.soilClass[site].SpinupSoils(site, returnIntervalInputs, returnIntervalLive, returnInterval);
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
            List<ISpecies> species = new List<ISpecies>();
            List<int> ages = new List<int>();
            foreach (ICohort cohort in ageCohorts)
            {
                species.Add(cohort.Species);
                ages.Add(cohort.Data.Age);
            }
            SpinUpBiomassCohorts(species, ages, site, initialBiomassMethod, true);
        }

        /// <summary>
        /// Grows cohorts of the given species and ages (oldest first) from the birth of the oldest to the present,
        /// recording their litter inputs by cohort age for the soil spin-up. killSnags: kill the cohorts that the
        /// initial snags say died just before the start.
        /// </summary>
        private static void SpinUpBiomassCohorts(List<ISpecies> species,
                                                 List<int> ages,
                                                 ActiveSite site,
                                                 ComputeMethod initialBiomassMethod,
                                                 bool killSnags)
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
            for (int time = -(ages[0]); time <= endTime; time += successionTimestep)
            {
                //EcoregionData.GenerateNewClimate(time, successionTimestep);           //LANDIS CLIMATE LIBRARY
                EcoregionData.GetAnnualTemperature(successionTimestep, 0);                        //ForCS CLIMATE
                SpeciesData.GenerateNewANPPandMaxBiomass(successionTimestep, time);
                //  Grow current biomass cohorts.

                if (time == endTime)
                {
                    SiteVars.soilClass[site].lastAge = ages[0];       //signal both that the spin-up is done, and the oldest age
                    SiteVars.soilClass[site].bKillNow = false;
                    //PlugIn.ModelCore.UI.WriteLine("InitialBiomass age={0}, site={1}", ageCohorts[0].Age, site);
                }

                PlugIn.GrowCohorts(site, successionTimestep, true);

                //  Add those cohorts that were born at the current year
                while (indexNextAgeCohort < ages.Count &&
                       ages[indexNextAgeCohort] == -time)
                {
                    ISpecies cohortSpecies = species[indexNextAgeCohort];
                    int initialBiomass = initialBiomassMethod(cohortSpecies, SiteVars.Cohorts[site], site);

                    SiteVars.Cohorts[site].AddNewCohort(cohortSpecies, 1,
                                                initialBiomass, new System.Dynamic.ExpandoObject());
                    SiteVars.soilClass[site].CollectBiomassMortality(cohortSpecies, 0, 0, 0, 0);      //dummy for getting it to recognize that the species is now present.

                    indexNextAgeCohort++;
                }
                if (time == endTime)
                {
                    SiteVars.soilClass[site].lastAge = ages[0];       //signal both that the spin-up is done, and the oldest age
                    SiteVars.soilClass[site].bKillNow = false;
                    // PlugIn.ModelCore.UI.WriteLine("InitialBiomass age={0}, site={1}", ageCohorts[0].Age, site);
                }
                else if (time == endTime - 1 && killSnags)   //just before the last timestep, we want to send a signal to the growth routine to kill the 
                {                               //cohorts that need to be snags. Hardwire this first for testing.
                    SiteVars.soilClass[site].bKillNow = true;
                }

            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// The site's return-interval spin-up (SpinUpReturnIntervals), with its LastDisturbance replaced by the
        /// site's code in SpinUpLastDisturbanceMap if the code is in that map's table; null if the soil spin-up is off
        /// or the site's ecoregion has no return interval. lastDisturbanceCode is the code used, or 0.
        /// </summary>
        private static SpinUpReturnInterval ReturnIntervalFor(ActiveSite site, out int lastDisturbanceCode)
        {
            lastDisturbanceCode = 0;
            SpinUpReturnInterval returnInterval;
            if (SoilVars.iParams.SoilSpinUpFlag == 0 ||
                !SoilVars.iParams.SpinUpReturnIntervals.TryGetValue(PlugIn.ModelCore.Ecoregion[site], out returnInterval))
                return null;

            SpinUpDisturbance lastDisturbance;
            if (SiteVars.SpinUpLastDisturbanceCode != null &&
                SoilVars.iParams.SpinUpLastDisturbanceCodes.TryGetValue(SiteVars.SpinUpLastDisturbanceCode[site], out lastDisturbance))
            {
                lastDisturbanceCode = SiteVars.SpinUpLastDisturbanceCode[site];
                return returnInterval.WithLastDisturbance(lastDisturbance);
            }
            return returnInterval;
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Grows the stand that a return-interval spin-up cycles: the site's community from bare ground for
        /// returnInterval years, each cohort established at its present offset from the oldest (a cohort that would not
        /// yet have established is left out). Returns its litter inputs by cohort age (0 to returnInterval) and its live
        /// biomass at the end, and leaves the site without cohorts and those records empty for the present stand.
        /// </summary>
        private static void GrowReturnIntervalStand(List<ICohort> ageCohorts,
                                                    ActiveSite site,
                                                    ComputeMethod initialBiomassMethod,
                                                    int returnInterval,
                                                    out double[,,] inputs,
                                                    out double[,] live)
        {
            int oldest = ageCohorts[0].Data.Age;
            List<ISpecies> species = new List<ISpecies>();
            List<int> ages = new List<int>();
            foreach (ICohort cohort in ageCohorts)
            {
                int age = returnInterval - (oldest - cohort.Data.Age);
                if (age >= 1)
                {
                    species.Add(cohort.Species);
                    ages.Add(age);
                }
            }
            SpinUpBiomassCohorts(species, ages, site, initialBiomassMethod, false);

            int nSpecies = PlugIn.ModelCore.Species.Count;
            inputs = new double[SoilClass.NUMBIOMASSCOMPONENTS, nSpecies, returnInterval + 1];
            live = new double[SoilClass.NUMBIOMASSCOMPONENTS, nSpecies];
            for (int i = 0; i < SoilClass.NUMBIOMASSCOMPONENTS; i++)
            {
                for (int idxSpecies = 0; idxSpecies < nSpecies; idxSpecies++)
                {
                    for (int age = 0; age <= returnInterval; age++)
                    {
                        inputs[i, idxSpecies, age] = SoilVars.BioInput[i, idxSpecies, age];
                        SoilVars.BioInput[i, idxSpecies, age] = 0.0;
                    }
                    live[i, idxSpecies] = SoilVars.BioLive[i, idxSpecies];
                    SoilVars.BioLive[i, idxSpecies] = 0.0;
                }
            }
            SiteVars.soilClass[site].lastAge = 0;
            SiteVars.Cohorts[site] = new SiteCohorts();
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
