//  Authors:  Caren Dymond, Sarah Beukema

using Landis.Core;
using Landis.SpatialModeling;
using Landis.Library.Succession;
using Landis.Library.InitialCommunities.Universal;
using Landis.Library.UniversalCohorts;
using System.Collections.Generic;
using Landis.Utilities;
using System;                   //temporary - for debugging using VS2017

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
        private IInputDMParameters paramDM;
        public static bool CalibrateMode;
        public static double CurrentYearSiteMortality;
        //private static int time;
        public static int MaxLife;
        private ICommunity initialCommunity;

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
     
            InputParametersParser parser = new InputParametersParser();
            parameters = Landis.Data.Load<IInputParameters>(dataFile, parser);
            InputClimateParser parser3 = new InputClimateParser();
            paramClimate = Landis.Data.Load<IInputClimateParms>(parameters.ClimateFile2, parser3);
            InputDMParser parser4 = new InputDMParser();
            paramDM = Landis.Data.Load<IInputDMParameters>(parameters.DMFile, parser4);

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

            SiteVars.Initialize(parameters, paramDM);
        }

        //---------------------------------------------------------------------

        public static ICore ModelCore
        {
            get
            {
                return modelCore;
            }
        }
       
        public override void Initialize()
        {
            Timestep              = parameters.Timestep;
            sufficientLight       = parameters.LightClassProbabilities;

            //SiteVars.Initialize();
            //Console.ReadLine();     //temporary line for debugging in VS2017

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
            Landis.Library.UniversalCohorts.Cohorts.Initialize(Timestep, new CohortBiomass());
            
            //landscapeCohorts = new LandscapeCohorts(SiteVars.Cohorts); 
            //Cohorts = landscapeCohorts;
            
            Reproduction.SufficientResources = SufficientLight;
            Reproduction.Establish = Establish;
            Reproduction.AddNewCohort = AddNewCohort;
            Reproduction.MaturePresent = MaturePresent;
            base.Initialize(modelCore, parameters.SeedAlgorithm); 

            InitialBiomass.Initialize(Timestep);

            Cohort.MortalityEvent += CohortMortality;
            //Landis.Library.UniversalCohorts.Cohort.DeathEvent += CohortTotalMortality;

            AgeOnlyDisturbances.Module.Initialize();

            InitializeSites(parameters.InitialCommunities, parameters.InitialCommunitiesMap, modelCore);
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

            //write the maps, if the timestep is right
            if(parameters.OutputMap > 0)   //0 = don't print
            {  
                if(PlugIn.ModelCore.CurrentTime % parameters.OutputMap == 0)
                {
                    Outputs.WriteMaps(parameters.OutputMapPath, parameters.OutputMap);
                }
            }

        }

        //---------------------------------------------------------------------

        protected override void InitializeSite(ActiveSite site) //, ICommunity initialCommunity)
        {
            InitialBiomass initialBiomass = InitialBiomass.Compute(site, initialCommunity);
            SiteVars.Cohorts[site] = InitialBiomass.Clone(initialBiomass.Cohorts);

            // Note: we need this both here and in SiteVars.Initialize()?
            SiteVars.soilClass[site] = new SoilClass(initialBiomass.soilClass);

            SiteVars.SoilOrganicMatterC[site]   = initialBiomass.SoilOrganicMatterC;            
            SiteVars.DeadWoodMass[site].Mass         = initialBiomass.DeadWoodMass;
            SiteVars.LitterMass[site].Mass           = initialBiomass.LitterMass;
            SiteVars.DeadWoodDecayRate[site]         = initialBiomass.DeadWoodDecayRate;
            SiteVars.LitterDecayRate[site]           = initialBiomass.LitterDecayRate;

            SiteVars.soilClass[site].BiomassOutput(site, 1);

        }
        //---------------------------------------------------------------------
        public void CohortMortality(object sender, MortalityEventArgs eventArgs)
        {
            //SiteVars.HarvestPrescriptionName = PlugIn.ModelCore.GetSiteVar<string>("Harvest.PrescriptionName");
            //PlugIn.ModelCore.UI.WriteLine("Cohort Partial Mortality: site {0}  DistType {1}", eventArgs.Site, eventArgs.DisturbanceType);

            ExtensionType disturbanceType = eventArgs.DisturbanceType;
            ActiveSite site = eventArgs.Site;

            ICohort cohort = eventArgs.Cohort;
            ISpecies species = cohort.Species;

            double foliar = (double)cohort.ComputeNonWoodyBiomass(site);
            double wood = ((double)cohort.Data.Biomass - foliar);

            if (eventArgs.Reduction >= 1)
            {
                if (disturbanceType == null)
                {

                    double totalRoot = Roots.CalculateRootBiomass(site, species, cohort.Data.Biomass);

                    SiteVars.soilClass[site].CollectBiomassMortality(species, cohort.Data.Age, wood, foliar, 0);
                    SiteVars.soilClass[site].CollectBiomassMortality(species, cohort.Data.Age, Roots.CoarseRoot, Roots.FineRoot, 1);
                    if (site.DataIndex == 1)
                        PlugIn.ModelCore.UI.WriteLine("{0} Roots from dying cohort {1}", PlugIn.ModelCore.CurrentTime, Roots.FineRoot);
                }


                if (disturbanceType != null)
                {

                    Disturbed[site] = true;
                    if (disturbanceType.IsMemberOf("disturbance:fire"))
                        Reproduction.CheckForPostFireRegen(eventArgs.Cohort, site);
                    else
                        Reproduction.CheckForResprouting(eventArgs.Cohort, site);

                    if (disturbanceType.IsMemberOf("disturbance:harvest"))  //only do this for harvest. Fire and wind are done in the Events.CohortDied routine.
                        SiteVars.soilClass[site].DisturbanceImpactsBiomass(site, cohort.Species, cohort.Data.Age, wood, foliar, disturbanceType.Name, 0);

                }
            }
            else
            {
                float mortality = eventArgs.Reduction;
                float fractionPartialMortality = mortality / (float)cohort.Data.Biomass;
                double foliarInput = foliar * fractionPartialMortality;
                double woodInput = wood * fractionPartialMortality;
                //PlugIn.ModelCore.UI.WriteLine("Inputs:  {0}, {1}, {2}, {3}", fractionPartialMortality, foliar, woodInput, foliarInput);

                SiteVars.soilClass[site].DisturbanceImpactsBiomass(site, cohort.Species, cohort.Data.Age, woodInput, foliarInput, disturbanceType.Name, 0);

                //if (disturbanceType.IsMemberOf("disturbance:harvest"))
                //{
                //    if (!Disturbed[site]) // this is the first cohort killed/damaged
                //    {
                //        HarvestEffects.ReduceLayers(SiteVars.HarvestPrescriptionName[site], site);
                //    }
                //    SiteVars.soilClass[site].DisturbanceImpactsBiomass(site, cohort.Species, cohort.Age, wood, foliar, disturbanceType.Name, 0);
                //    woodInput -= woodInput * (float)HarvestEffects.GetCohortWoodRemoval(site);
                //    foliarInput -= foliarInput * (float)HarvestEffects.GetCohortLeafRemoval(site);
                //}
                //if (disturbanceType.IsMemberOf("disturbance:fire"))
                //{

                //    SiteVars.FireSeverity = PlugIn.ModelCore.GetSiteVar<byte>("Fire.Severity");

                //    if (!Disturbed[site]) // this is the first cohort killed/damaged
                //    {
                //        SiteVars.SmolderConsumption[site] = 0.0;
                //        SiteVars.FlamingConsumption[site] = 0.0;
                //        if (SiteVars.FireSeverity != null && SiteVars.FireSeverity[site] > 0)
                //            FireEffects.ReduceLayers(SiteVars.FireSeverity[site], site);

                //    }

                //    double live_woodFireConsumption = woodInput * (float)FireEffects.ReductionsTable[(int)SiteVars.FireSeverity[site]].CohortWoodReduction;
                //    double live_foliarFireConsumption = foliarInput * (float)FireEffects.ReductionsTable[(int)SiteVars.FireSeverity[site]].CohortLeafReduction;

                //    SiteVars.SmolderConsumption[site] += live_woodFireConsumption;
                //    SiteVars.FlamingConsumption[site] += live_foliarFireConsumption;
                //    woodInput -= (float)live_woodFireConsumption;
                //    foliarInput -= (float)live_foliarFireConsumption;
                //}

                //ForestFloor.AddWoodLitter(woodInput, cohort.Species, site);
                //ForestFloor.AddFoliageLitter(foliarInput, cohort.Species, site);

                //Roots.AddCoarseRootLitter(Roots.CalculateCoarseRoot(cohort, cohort.WoodBiomass * fractionPartialMortality), cohort, cohort.Species, site);
                //Roots.AddFineRootLitter(Roots.CalculateFineRoot(cohort, cohort.LeafBiomass * fractionPartialMortality), cohort, cohort.Species, site);

                //PlugIn.ModelCore.UI.WriteLine("EVENT: Cohort Partial Mortality: species={0}, age={1}, disturbance={2}.", cohort.Species.Name, cohort.Age, disturbanceType);
                //PlugIn.ModelCore.UI.WriteLine("       Cohort Reductions:  Foliar={0:0.00}.  Wood={1:0.00}.", HarvestEffects.GetCohortLeafRemoval(site), HarvestEffects.GetCohortLeafRemoval(site));
                //PlugIn.ModelCore.UI.WriteLine("       InputB/TotalB:  Foliar={0:0.00}/{1:0.00}, Wood={2:0.0}/{3:0.0}.", foliarInput, cohort.LeafBiomass, woodInput, cohort.WoodBiomass);
                Disturbed[site] = true;
            }

            return;
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Add a new cohort to a site.
        /// This is a Delegate method to base succession.
        /// </summary>

        public void AddNewCohort(ISpecies species, ActiveSite site, string reproductionType, double propBiomass = 1.0)
        {
            SiteVars.Cohorts[site].AddNewCohort(species, 1, CohortBiomass.InitialBiomass(species, SiteVars.Cohorts[site], site), new System.Dynamic.ExpandoObject());

            //PlugIn.ModelCore.UI.WriteLine("Added new cohort: Yr={0},  Site={1}, Species={2}, Type={3}", PlugIn.ModelCore.CurrentTime, site, species.Index, reproductionType);
 
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
                SiteVars.Cohorts[site].Grow(site, (y == years && isSuccessionTimestep), true);
                //Debug.Assert(cohorts.TotalBiomass >= 0,"ERROR: total biomass is less than 0");

                SiteVars.soilClass[site].BiomassOutput(site, 0); 
                //update total biomass, before sending this to the soil routines.
                SiteVars.TotalBiomass[site] = Library.UniversalCohorts.Cohorts.ComputeNonYoungBiomass(SiteVars.Cohorts[site]);
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
                if (lights.ShadeClass == SpeciesData.ShadeTolerance[species])
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
                        if (cohort.Data.Age > 5)
                            B_ACT += cohort.Data.Biomass;
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

        public override void InitializeSites(string initialCommunitiesText, string initialCommunitiesMap, ICore modelCore)
        {
            ModelCore.UI.WriteLine("   Loading initial communities from file \"{0}\" ...", initialCommunitiesText);
            DatasetParser parser = new DatasetParser(Timestep, ModelCore.Species, additionalCohortParameters, initialCommunitiesText);
            IDataset communities = Landis.Data.Load<IDataset>(initialCommunitiesText, parser);

            ModelCore.UI.WriteLine("   Reading initial communities map \"{0}\" ...", initialCommunitiesMap);
            IInputRaster<UIntPixel> map;
            map = ModelCore.OpenRaster<UIntPixel>(initialCommunitiesMap);
            using (map)
            {
                UIntPixel pixel = map.BufferPixel;
                foreach (Site site in ModelCore.Landscape.AllSites)
                {
                    map.ReadBufferPixel();
                    uint mapCode = pixel.MapCode.Value;
                    if (!site.IsActive)
                        continue;

                    ActiveSite activeSite = (ActiveSite)site;
                    initialCommunity = communities.Find(mapCode);
                    if (initialCommunity == null)
                    {
                        throw new ApplicationException(string.Format("Unknown map code for initial community: {0}", mapCode));
                    }

                    InitializeSite(activeSite);
                }
            }
        }

        // NEW DYNAMIC COHORT PARAMETERS GO HERE
        public override void AddCohortData()
        {
            return;
        }
    }

}
