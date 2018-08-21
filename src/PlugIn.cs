//  Authors:  Caren Dymond, Sarah Beukema

using Landis.Core;
using Landis.SpatialModeling;
using Landis.Library.Succession;
using Landis.Library.InitialCommunities;
using Landis.Library.BiomassCohorts;
using System.Collections.Generic;
using Landis.Utilities;
using System.Diagnostics;       //temporary - for memory testing SEPT

namespace Landis.Extension.Succession.ForC
{
    public class PlugIn
        : Landis.Library.Succession.ExtensionBase
    {
        public static readonly string ExtensionName = "ForC Succession";
        private static ICore modelCore;
        private IInputParameters parameters;
        private IInputSnagParms paramSnag;
        private IInputClimateParms paramClimate;
        public static bool CalibrateMode;
        public static double CurrentYearSiteMortality;
        //private static int time;
        public static int MaxLife;

        private List<ISufficientLight> sufficientLight;

        //---------------------------------------------------------------------

        public PlugIn()
            : base(ExtensionName)
        {
        }

        //---------------------------------------------------------------------

        public override void LoadParameters(string dataFile, ICore mCore)
        {

            modelCore = mCore;

            Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();  //temporary - for memory testing SEPT
            double totalMBOfPhysicalMemory = currentProcess.WorkingSet64 / 100000.0;
            double totalMBOfVirtualMemory = currentProcess.VirtualMemorySize64 / 100000.0;
            //modelCore.Log.WriteLine("Before Initial Physical, Virtual Memory Use: " + totalMBOfPhysicalMemory.ToString() + ", " + totalMBOfVirtualMemory.ToString());
         
            InputParametersParser parser = new InputParametersParser();
            parameters = Landis.Data.Load<IInputParameters>(dataFile, parser);
            InputClimateParser parser3 = new InputClimateParser();
            paramClimate = Landis.Data.Load<IInputClimateParms>(parameters.ClimateFile2, parser3);

            if (parameters.InitSnagFile != null)
            {
                InputSnagParser parser2 = new InputSnagParser();
                paramSnag = Landis.Data.Load<IInputSnagParms>(parameters.InitSnagFile, parser2);
            }
            MaxLife = 0;
            foreach (ISpecies species in modelCore.Species)
            {
                if (MaxLife < species.Longevity)
                    MaxLife = species.Longevity;
            }

            SiteVars.Initialize(parameters);
            totalMBOfPhysicalMemory = currentProcess.WorkingSet64 / 100000.0;
            totalMBOfVirtualMemory = currentProcess.VirtualMemorySize64 / 100000.0;
            //modelCore.Log.WriteLine("After Initial Physical, Virtual Memory Use: " + totalMBOfPhysicalMemory.ToString() + ", " + totalMBOfVirtualMemory.ToString());
        }

        //---------------------------------------------------------------------

        public static ICore ModelCore
        {
            get
            {
                return modelCore;
            }
        }
        //---------------------------------------------------------------------

        //public static int SuccessionTimeStep
        //{
        //    get
        //    {
        //        return time;
        //    }
        //}

        //---------------------------------------------------------------------

        public override void Initialize()
        {
            Timestep              = parameters.Timestep;
            sufficientLight       = parameters.LightClassProbabilities;
            
            //SiteVars.Initialize();
            
            //  Initialize climate.  A list of ecoregion indices is passed so that
            //  the climate library can operate independently of the LANDIS-II core.
            List<int> ecoregionIndices = new List<int>();
            foreach(IEcoregion ecoregion in PlugIn.ModelCore.Ecoregions)
                ecoregionIndices.Add(ecoregion.Index);
            //Climate.Initialize(parameters.ClimateFile, false, modelCore);     //LANDIS CLIMATE LIBRARY

            EcoregionData.Initialize(parameters, paramClimate);
            SpeciesData.Initialize(parameters);
            CalibrateMode = parameters.CalibrateMode;
            CohortBiomass.SpinupMortalityFraction = parameters.SpinupMortalityFraction;
            Snags.Initialize(paramSnag);

            //  Cohorts must be created before the base class is initialized
            //  because the base class' reproduction module uses the core's
            //  SuccessionCohorts property in its Initialization method.
            Landis.Library.BiomassCohorts.Cohorts.Initialize(Timestep, new CohortBiomass());
            
            //landscapeCohorts = new LandscapeCohorts(SiteVars.Cohorts); 
            //Cohorts = landscapeCohorts;
            
            Reproduction.SufficientResources = SufficientLight;
            Reproduction.Establish = Establish;
            Reproduction.AddNewCohort = AddNewCohort;
            Reproduction.MaturePresent = MaturePresent;
            base.Initialize(modelCore, parameters.SeedAlgorithm); 

            InitialBiomass.Initialize(Timestep);

            Cohort.DeathEvent += CohortDied;
            AgeOnlyDisturbances.Module.Initialize();

            Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();  //temporary - for memory testing SEPT
            double totalMBOfPhysicalMemory = currentProcess.WorkingSet64 / 100000.0;
            double totalMBOfVirtualMemory = currentProcess.VirtualMemorySize64 / 100000.0;
            //modelCore.Log.WriteLine("Before Initial Sites Physical, Virtual Memory Use: " + totalMBOfPhysicalMemory.ToString() + ", " + totalMBOfVirtualMemory.ToString());

            InitializeSites(parameters.InitialCommunities, parameters.InitialCommunitiesMap, modelCore);

            totalMBOfPhysicalMemory = currentProcess.WorkingSet64 / 100000.0;
            totalMBOfVirtualMemory = currentProcess.VirtualMemorySize64 / 100000.0;
            //modelCore.Log.WriteLine("After Initial Sites Physical, Virtual Memory Use: " + totalMBOfPhysicalMemory.ToString() + ", " + totalMBOfVirtualMemory.ToString());
        }

        //---------------------------------------------------------------------

        public override void Run()
        {
            if (PlugIn.ModelCore.CurrentTime > 0 && SiteVars.CapacityReduction == null)
                SiteVars.CapacityReduction = PlugIn.ModelCore.GetSiteVar<double>("Harvest.CapacityReduction");

            SiteVars.FireSeverity = PlugIn.ModelCore.GetSiteVar<byte>("Fire.Severity");

            //EcoregionData.GenerateNewClimate(PlugIn.ModelCore.CurrentTime, Timestep); //LANDIS CLIMATE LIBRARY
            EcoregionData.GetAnnualTemperature(Timestep, 0);                            //ForCS CLIMATE
                      
            SpeciesData.GenerateNewANPPandMaxBiomass(Timestep, 0);
            //Reproduction.ChangeEstablishProbabilities(Util.ToArray<double>(SpeciesData.EstablishProbability));

            base.RunReproductionFirst();            
        }

        //---------------------------------------------------------------------

        protected override void InitializeSite(ActiveSite site,
                                               ICommunity initialCommunity)
        {
            InitialBiomass initialBiomass = InitialBiomass.Compute(site, initialCommunity);
            SiteVars.Cohorts[site] = InitialBiomass.Clone(initialBiomass.Cohorts); //.Clone();

            // Note: we need this both here and in SiteVars.Initialize()?
            SiteVars.soilClass[site] = new SoilClass(initialBiomass.soilClass);

            SiteVars.SoilOrganicMatterC[site]   = initialBiomass.SoilOrganicMatterC;            
            SiteVars.DeadWoodMass[site]         = initialBiomass.DeadWoodMass;
            SiteVars.LitterMass[site]           = initialBiomass.LitterMass;
            SiteVars.DeadWoodDecayRate[site]         = initialBiomass.DeadWoodDecayRate;
            SiteVars.LitterDecayRate[site]           = initialBiomass.LitterDecayRate;

            SiteVars.soilClass[site].BiomassOutput(site, 1); 

        }

        //---------------------------------------------------------------------
    
        public void CohortDied(object         sender,
                               DeathEventArgs eventArgs)
        {

            //PlugIn.ModelCore.UI.WriteLine("Cohort Died! :-(");

            ExtensionType disturbanceType = eventArgs.DisturbanceType;
            ActiveSite site = eventArgs.Site;
            
            ICohort cohort = eventArgs.Cohort;
            ISpecies species = cohort.Species;
            
            double foliar = (double) cohort.ComputeNonWoodyBiomass(site);
            double wood = ((double) cohort.Biomass - foliar);

            //PlugIn.ModelCore.UI.WriteLine("Cohort Died: species={0}, age={1}, biomass={2}, foliage={3}.", cohort.Species.Name, cohort.Age, cohort.Biomass, foliar);
            
            if (disturbanceType == null) {
                //PlugIn.ModelCore.UI.WriteLine("NO EVENT: Cohort Died: species={0}, age={1}, disturbance={2}.", cohort.Species.Name, cohort.Age, eventArgs.DisturbanceType);

                double totalRoot = Roots.CalculateRootBiomass(site, species, cohort.Biomass);
                
                SiteVars.soilClass[site].CollectBiomassMortality(species, cohort.Age, wood, foliar, 0);
                SiteVars.soilClass[site].CollectBiomassMortality(species, cohort.Age, Roots.CoarseRoot, Roots.FineRoot, 1); 
            }
            
            if (disturbanceType != null) {
                //PlugIn.ModelCore.UI.WriteLine("DISTURBANCE EVENT: Cohort Died: species={0}, age={1}, disturbance={2}.", cohort.Species.Name, cohort.Age, eventArgs.DisturbanceType);
            
                Disturbed[site] = true;
                if (disturbanceType.IsMemberOf("disturbance:fire"))
                    Reproduction.CheckForPostFireRegen(eventArgs.Cohort, site);
                else
                    Reproduction.CheckForResprouting(eventArgs.Cohort, site);

                if (disturbanceType.IsMemberOf("disturbance:harvest"))  //only do this for harvest. Fire and wind are done in the Events.CohortDied routine.
                    SiteVars.soilClass[site].DisturbanceImpactsBiomass(site, cohort.Species, cohort.Age, wood, foliar, disturbanceType.Name, 0);

            }
        }
        
        //---------------------------------------------------------------------

        /// <summary>
        /// Add a new cohort to a site.
        /// This is a Delegate method to base succession.
        /// </summary>

        public void AddNewCohort(ISpecies species, ActiveSite site)
        {
            SiteVars.Cohorts[site].AddNewCohort(species, 1, CohortBiomass.InitialBiomass(species, SiteVars.Cohorts[site], site));
        }
 
        //---------------------------------------------------------------------

        protected override void AgeCohorts(ActiveSite site,
                                           ushort     years,
                                           int?       successionTimestep)
        {
            GrowCohorts(site, years, successionTimestep.HasValue);
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// Grows all cohorts at a site for a specified number of years.  The
        /// dead pools at the site also decompose for the given time period.
        /// </summary>
        public static void GrowCohorts(//SiteCohorts cohorts,
                                       ActiveSite  site,
                                       int         years,
                                       bool        isSuccessionTimestep)
        {
            IEcoregion ecoregion = PlugIn.ModelCore.Ecoregion[site];
            double preGrowthBiomass = 0;
            SiteVars.ResetAnnualValues(site);
 
            for (int y = 0; y < years; y++)
            {
                /*  CODE RELATED TO THE USE OF ONE OF THE BIGGER LANDIS CLIMATE LIBRARIES

                // Do not reset annual climate if it has already happend for this year.
                if(!EcoregionData.ClimateUpdates[ecoregion][y + PlugIn.ModelCore.CurrentTime])
                {
                    EcoregionData.SetAnnualClimate(PlugIn.ModelCore.Ecoregion[site], y);
                    EcoregionData.ClimateUpdates[ecoregion][y + PlugIn.ModelCore.CurrentTime] = true;
                }
                
                // if spin-up phase, allow each initial community to have a unique climate
                if(PlugIn.ModelCore.CurrentTime == 0)
                {
                    EcoregionData.SetAnnualClimate(PlugIn.ModelCore.Ecoregion[site], y);
                }
                */

                preGrowthBiomass = SiteVars.TotalBiomass[site];
                SiteVars.Cohorts[site].Grow(site, (y == years && isSuccessionTimestep));
                //Debug.Assert(cohorts.TotalBiomass >= 0,"ERROR: total biomass is less than 0");

                SiteVars.soilClass[site].BiomassOutput(site, 0); 
                //update total biomass, before sending this to the soil routines.
                SiteVars.TotalBiomass[site] = Library.BiomassCohorts.Cohorts.ComputeNonYoungBiomass(SiteVars.Cohorts[site]);
                SiteVars.soilClass[site].ProcessSoils(site, SiteVars.TotalBiomass[site], preGrowthBiomass, 0);
            }
            
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// Determines if there is sufficient light at a site for a species to
        /// germinate/resprout.  
        /// Also accounts for SITE level N limitations.  N limits could not
        /// be accommodated in the Establishment Probability as that is an ecoregion x spp property.
        /// Therefore, would better be described as "SiteLevelDeterminantReproduction".
        /// </summary>
        public bool SufficientLight(ISpecies   species,
                                           ActiveSite site)
        {
            
            //PlugIn.ModelCore.UI.WriteLine("  Calculating Sufficient Light from Succession.");
            byte siteShade = PlugIn.ModelCore.GetSiteVar<byte>("Shade")[site];
            
            double lightProbability = 0.0;
            bool found = false;
            
            foreach(ISufficientLight lights in sufficientLight)
            {
            
                //PlugIn.ModelCore.UI.WriteLine("Sufficient Light:  ShadeClass={0}, Prob0={1}.", lights.ShadeClass, lights.ProbabilityLight0);
                if (lights.ShadeClass == species.ShadeTolerance)
                {
                    if (siteShade == 0)  lightProbability = lights.ProbabilityLight0;
                    if (siteShade == 1)  lightProbability = lights.ProbabilityLight1;
                    if (siteShade == 2)  lightProbability = lights.ProbabilityLight2;
                    if (siteShade == 3)  lightProbability = lights.ProbabilityLight3;
                    if (siteShade == 4)  lightProbability = lights.ProbabilityLight4;
                    if (siteShade == 5)  lightProbability = lights.ProbabilityLight5;
                    found = true;
                }
            }
            
            if(!found)
                PlugIn.ModelCore.UI.WriteLine("A Sufficient Light value was not found for {0}.", species.Name);

            return PlugIn.ModelCore.GenerateUniform() < lightProbability;
            
        }
        //---------------------------------------------------------------------

        public override byte ComputeShade(ActiveSite site)
        {
            //return LivingBiomass.ComputeShade(site);
            IEcoregion ecoregion = PlugIn.ModelCore.Ecoregion[site];
            double B_MAX = (double) EcoregionData.B_MAX[ecoregion];
            double B_ACT = 0.0;

            if (SiteVars.Cohorts[site] != null)
            {
                foreach (ISpeciesCohorts sppCohorts in SiteVars.Cohorts[site])
                    foreach (ICohort cohort in sppCohorts)
                        if (cohort.Age > 5)
                            B_ACT += cohort.Biomass;
            }

            int lastMortality = SiteVars.PreviousYearMortality[site];
            B_ACT = System.Math.Min(EcoregionData.B_MAX[ecoregion] - lastMortality, B_ACT);
            
            //  Relative living biomass (ratio of actual to maximum site
            //  biomass).
            double B_AM = B_ACT / B_MAX;

            for (byte shade = 5; shade >= 1; shade--) 
            {
                if(EcoregionData.ShadeBiomass[shade][ecoregion] <= 0)
                {
                    string mesg = string.Format("Minimum relative biomass has not been defined for ecoregion {0}", ecoregion.Name);
                    throw new System.ApplicationException(mesg);
                }
                //PlugIn.ModelCore.UI.WriteLine("Shade Calculation:  lastMort={0:0.0}, B_MAX={1}, oldB={2}, B_ACT={3}, shade={4}.", lastMortality, B_MAX,oldBiomass,B_ACT,shade);
                if (B_AM >= EcoregionData.ShadeBiomass[shade][ecoregion])
                {
                    return shade;
                }
            }
            
            //PlugIn.ModelCore.UI.WriteLine("Shade Calculation:  lastMort={0:0.0}, B_MAX={1}, oldB={2}, B_ACT={3}, shade=0.", lastMortality, B_MAX,oldBiomass,B_ACT);
            
            return 0;
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Determines if a species can establish on a site.
        /// This is a Delegate method to base succession.
        /// </summary>
        public bool Establish(ISpecies species, ActiveSite site)
        {
            IEcoregion ecoregion = modelCore.Ecoregion[site];
            double establishProbability = SpeciesData.EstablishProbability[species][ecoregion];

            return modelCore.GenerateUniform() < establishProbability; // modEstabProb;
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Determines if there is a mature cohort at a site.  
        /// This is a Delegate method to base succession.
        /// </summary>
        public bool MaturePresent(ISpecies species, ActiveSite site)
        {
            return SiteVars.Cohorts[site].IsMaturePresent(species);
        }

    }
    
}
