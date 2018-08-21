//  Authors:  Caren Dymond, Sarah Beukema

using Landis.Library.Succession;
using Landis.Core;
using Landis.Utilities;

using System.Collections.Generic;
using System.Diagnostics;

namespace Landis.Extension.Succession.ForC
{
    /// <summary>
    /// The parameters for biomass succession.
    /// </summary>
    public class InputParameters
        : IInputParameters
    {
        private int timestep;
        private SeedingAlgorithms seedAlg;
        private string climateFile;
        private string climateFile2;
        private string initSnagFile;
        private bool calibrateMode;
        private double spinupMortalityFraction;

        private int m_nOutputBiomass;
        private int m_nOutputDOMPools;
        private int m_nOutputFlux;
        private int m_nOutputSummary;

        private int m_nSoilSpinUpFlag;
        private double m_dSpinUpTolerance;
        private int m_nSpinUpIterations;

        private string m_sForCSFunctionalGroupFilePath;

        private Landis.Library.Parameters.Ecoregions.AuxParm<Percentage>[] minRelativeBiomass;
        private List<ISufficientLight> sufficientLight;

        private Landis.Library.Parameters.Species.AuxParm<int> sppFunctionalType;
        private Landis.Library.Parameters.Species.AuxParm<double> leafLongevity;
        private Landis.Library.Parameters.Species.AuxParm<bool> epicormic;

        private Landis.Library.Parameters.Species.AuxParm<double> mortCurveShape;
        private Landis.Library.Parameters.Species.AuxParm<int> m_anMerchStemsMinAge;
        private Landis.Library.Parameters.Species.AuxParm<double> m_adMerchCurveParmA;
        private Landis.Library.Parameters.Species.AuxParm<double> m_adMerchCurveParmB;
        private Landis.Library.Parameters.Species.AuxParm<double> m_adPropNonMerch;
        private Landis.Library.Parameters.Species.AuxParm<double> growthCurveShape;

        private Landis.Library.Parameters.Ecoregions.AuxParm<double> fieldCapacity;
        private Landis.Library.Parameters.Ecoregions.AuxParm<double> latitude;

        private string ageOnlyDisturbanceParms;
        private string initCommunities;
        private string communitiesMap;

        private IEcoregionDataset m_dsEcoregion;
        private ISpeciesDataset m_dsSpecies;

        private IDictionary<int, IDOMPool> m_dictDOMPools;
        private Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<double[]>> m_aDOMDecayRates;
        private Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<double>> m_DOMInitialVFastAG;
        private Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<double[]>> m_aDOMPoolAmountT0;
        private Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<double[]>> m_aDOMPoolQ10;
        private double m_dPropBiomassFine;
        private double m_dPropBiomassCoarse;
        private double m_dPropDOMSlowAGToSlowBG;
        private double m_dPropDOMStemSnagToMedium;
        private double m_dPropDOMBranchSnagToFastAG;
        private IDisturbTransferFromPools[] m_aDisturbFireFromDOMPools;
        private IDictionary<string, IDisturbTransferFromPools> m_dictDisturbOtherFromDOMPools;
        private IDisturbTransferFromPools[] m_aDisturbFireFromBiomassPools;
        private IDictionary<string, IDisturbTransferFromPools> m_dictDisturbOtherFromBiomassPools;
        private Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<ITimeCollection<IANPP>>> m_ANPPTimeCollection;
        private Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<ITimeCollection<IMaxBiomass>>> m_MaxBiomassTimeCollection;
        private Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<ITimeCollection<IEstabProb>>> m_EstabProbTimeCollection;

        //root parameterss
        private Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<double[]>> m_MinWoodyBio;
        private Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<double[]>> m_Ratio;
        private Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<double[]>> m_PropFine;
        private Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<double[]>> m_FineTurnover;
        private Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<double[]>> m_CoarseTurnover;

        //private Ecoregions.AuxParm<Species.AuxParm<double>> m_dEstablishProbability ;
        private Landis.Library.Parameters.Species.AuxParm<Landis.Library.Parameters.Ecoregions.AuxParm<double>> m_dEstablishProbability;
        //private Species.AuxParm<Ecoregions.AuxParm<ITimeCollection<IEstabProb>>> m_EstabProbTimeCollection;

        //Initial Snag variables
        //private int[] m_SnagSpecies;
        //private int[] m_SnagAgeAtDeath; 
        //private int[] m_SnagTimeSinceDeath;
        //private string[] m_SnagDisturb; 


        //---------------------------------------------------------------------
        /// <summary>
        /// Timestep (years)
        /// </summary>

        public int Timestep
        {
            get
            {
                return timestep;
            }
            set
            {
                if (value < 0)
                    throw new InputValueException(value.ToString(), "Timestep must be > or = 0");
                timestep = value;
            }
        }

        //---------------------------------------------------------------------
        /// <summary>
        /// Seeding algorithm
        /// </summary>

        public SeedingAlgorithms SeedAlgorithm
        {
            get
            {
                return seedAlg;
            }
            set
            {
                seedAlg = value;
            }
        }
        //---------------------------------------------------------------------

        /// <summary>
        /// Path to the file with the initial communities' definitions.
        /// </summary>
        public string InitialCommunities
        {
            get
            {
                return initCommunities;
            }

            set
            {
                if (value != null)
                {
                    ValidatePath(value);
                }
                initCommunities = value;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Path to the raster file showing where the initial communities are.
        /// </summary>
        public string InitialCommunitiesMap
        {
            get
            {
                return communitiesMap;
            }

            set
            {
                if (value != null)
                {
                    ValidatePath(value);
                }
                communitiesMap = value;
            }
        }
        //---------------------------------------------------------------------
        public bool CalibrateMode
        {
            get
            {
                return calibrateMode;
            }
            set
            {
                calibrateMode = value;
            }
        }

        //---------------------------------------------------------------------

        public double SpinupMortalityFraction
        {
            get
            {
                return spinupMortalityFraction;
            }
            set
            {
                if (value < 0.0 || value > 0.5)
                    throw new InputValueException(value.ToString(), "SpinupMortalityFraction must be > 0.0 and < 0.5");
                spinupMortalityFraction = value;
            }
        }

        //---------------------------------------------------------------------
        /// <summary>
        /// Path to the required file with climatedata.
        /// </summary>
        public string ClimateFile
        {
            get
            {
                return climateFile;
            }
            set
            {
                string path = value;
                if (path.Trim(null).Length == 0)
                    throw new InputValueException(path, "\"{0}\" is not a valid path.", path);
                climateFile = value;
            }
        }

        public string ClimateFile2
        {
            get
            {
                return climateFile2;
            }
            set
            {
                string path = value;
                if (path.Trim(null).Length == 0)
                    throw new InputValueException(path, "\"{0}\" is not a valid path.", path);
                climateFile2 = value;
            }
        }

        public string InitSnagFile
        {
            get
            {
                return initSnagFile;
            }
            set
            {
                string path = value;
                if (path.Trim(null).Length == 0)
                    throw new InputValueException(path, "\"{0}\" is not a valid path.", path);
                initSnagFile = value;
            }
        }


        public int OutputBiomass { get { return m_nOutputBiomass; } }
        public int OutputDOMPools { get { return m_nOutputDOMPools; } }
        public int OutputFlux { get { return m_nOutputFlux; } }
        public int OutputSummary { get { return m_nOutputSummary; } }

        public int SoilSpinUpFlag { get { return m_nSoilSpinUpFlag; } }
        public double SpinUpTolerance { get { return m_dSpinUpTolerance; } }
        public int SpinUpIterations { get { return m_nSpinUpIterations; } }


        public string ForCSFunctionalGroupFilePath
        {
            get
            {
                return m_sForCSFunctionalGroupFilePath;
            }
            set
            {
                string path = value;
                if (path.Trim(null).Length == 0)
                    throw new InputValueException(path, "\"{0}\" is not a valid path.", path);
                m_sForCSFunctionalGroupFilePath = value;
            }
        }

        //---------------------------------------------------------------------
        /// <summary>
        /// Definitions of sufficient light probabilities.
        /// </summary>
        public List<ISufficientLight> LightClassProbabilities
        {
            get
            {
                return sufficientLight;
            }
            set
            {
                //Debug.Assert(sufficientLight.Count != 0);
                sufficientLight = value;
            }
        }
        //---------------------------------------------------------------------

        public Landis.Library.Parameters.Ecoregions.AuxParm<Percentage>[] MinRelativeBiomass
        {
            get
            {
                return minRelativeBiomass;
            }
        }

        //---------------------------------------------------------------------

        public Landis.Library.Parameters.Species.AuxParm<int> SppFunctionalType { get { return sppFunctionalType; } }
        public Landis.Library.Parameters.Species.AuxParm<double> LeafLongevity { get { return leafLongevity; } }
        public Landis.Library.Parameters.Species.AuxParm<bool> Epicormic { get { return epicormic; } }
        public Landis.Library.Parameters.Species.AuxParm<double> MortCurveShape { get { return mortCurveShape; } }
        public Landis.Library.Parameters.Species.AuxParm<int> MerchStemsMinAge { get { return m_anMerchStemsMinAge; } }
        public Landis.Library.Parameters.Species.AuxParm<double> MerchCurveParmA { get { return m_adMerchCurveParmA; } }
        public Landis.Library.Parameters.Species.AuxParm<double> MerchCurveParmB { get { return m_adMerchCurveParmB; } }
        public Landis.Library.Parameters.Species.AuxParm<double> PropNonMerch { get { return m_adPropNonMerch; } }
        public Landis.Library.Parameters.Species.AuxParm<double> GrowthCurveShapeParm { get { return growthCurveShape; } }

        public IDictionary<int, IDOMPool> DOMPools { get { return m_dictDOMPools; } }
        public Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<double[]>> DOMDecayRates { get { return m_aDOMDecayRates; } }
        public Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<double[]>> DOMPoolAmountT0 { get { return m_aDOMPoolAmountT0; } }
        public Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<double[]>> DOMPoolQ10 { get { return m_aDOMPoolQ10; } }
        public double PropBiomassFine { get { return m_dPropBiomassFine; } }
        public double PropBiomassCoarse { get { return m_dPropBiomassCoarse; } }
        public double PropDOMSlowAGToSlowBG { get { return m_dPropDOMSlowAGToSlowBG; } }
        public double PropDOMStemSnagToMedium { get { return m_dPropDOMStemSnagToMedium; } }
        public double PropDOMBranchSnagToFastAG { get { return m_dPropDOMBranchSnagToFastAG; } }

        public Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<double[]>> CoarseTurnover { get { return m_CoarseTurnover; } }
        public Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<double[]>> FineTurnover { get { return m_FineTurnover; } }
        public Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<double[]>> Ratio { get { return m_Ratio; } }
        public Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<double[]>> PropFine { get { return m_PropFine; } }
        public Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<double[]>> MinWoodyBio { get { return m_MinWoodyBio; } }


        /// <summary>
        /// Returns an array of IDisturbTransferFromPools objects, indexed by a 0-based severity level.
        /// </summary>
        public IDisturbTransferFromPools[] DisturbFireFromDOMPools { get { return m_aDisturbFireFromDOMPools; } }

        /// <summary>
        /// Returns a dictionary of IDisturbTransferFromPools objects, indexed by name of the disturbance (e.g. "Harvest").
        /// </summary>
        public IDictionary<string, IDisturbTransferFromPools> DisturbOtherFromDOMPools { get { return m_dictDisturbOtherFromDOMPools; } }

        /// <summary>
        /// Returns an array of IDisturbTransferFromPools objects, indexed by a 0-based severity level.
        /// </summary>
        public IDisturbTransferFromPools[] DisturbFireFromBiomassPools { get { return m_aDisturbFireFromBiomassPools; } }

        /// <summary>
        /// Returns a dictionary of IDisturbTransferFromPools objects, indexed by name of the disturbance (e.g. "Harvest").
        /// </summary>
        public IDictionary<string, IDisturbTransferFromPools> DisturbOtherFromBiomassPools { get { return m_dictDisturbOtherFromBiomassPools; } }

        public Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<ITimeCollection<IANPP>>> ANPPTimeCollection { get { return m_ANPPTimeCollection; } }
        public Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<ITimeCollection<IMaxBiomass>>> MaxBiomassTimeCollection { get { return m_MaxBiomassTimeCollection; } }
        public Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<ITimeCollection<IEstabProb>>> EstabProbTimeCollection { get { return m_EstabProbTimeCollection; } }
        
        //public Ecoregions.AuxParm<Species.AuxParm<double>> EstablishProbability { get { return m_dEstablishProbability; } }
        public Landis.Library.Parameters.Species.AuxParm<Landis.Library.Parameters.Ecoregions.AuxParm<double>> EstablishProbability { get { return m_dEstablishProbability; } }
        //public Species.AuxParm<Ecoregions.AuxParm<ITimeCollection<IEstabProb>>> EstabProbTimeCollection { get { return m_EstabProbTimeCollection; } }

        //---------------------------------------------------------------------
        public Landis.Library.Parameters.Ecoregions.AuxParm<double> FieldCapacity
        {
            get
            {
                return fieldCapacity;
            }
        }

        //---------------------------------------------------------------------
        public Landis.Library.Parameters.Ecoregions.AuxParm<double> Latitude
        {
            get
            {
                return latitude;
            }
        }

        //---------------------------------------------------------------------

        public string AgeOnlyDisturbanceParms
        {
            get
            {
                return ageOnlyDisturbanceParms;
            }
            set
            {
                string path = value;
                if (path.Trim(null).Length == 0)
                    throw new InputValueException(path, "\"{0}\" is not a valid path.", path);
                ageOnlyDisturbanceParms = value;
            }
        }


        public void SetOutputBiomass(InputValue<int> newValue)
        {
            System.Diagnostics.Debug.Assert(newValue != null);

            if (newValue.Actual < 1)
                throw new InputValueException(newValue.String, "{0} must be greater than 0.", newValue.String);

            m_nOutputBiomass = newValue.Actual;
        }

        public void SetOutputDOMPools(InputValue<int> newValue)
        {
            System.Diagnostics.Debug.Assert(newValue != null);

            if (newValue.Actual < 1)
                throw new InputValueException(newValue.String, "{0} must be greater than 0.", newValue.String);

            m_nOutputDOMPools = newValue.Actual;
        }

        public void SetOutputFlux(InputValue<int> newValue)
        {
            System.Diagnostics.Debug.Assert(newValue != null);

            if (newValue.Actual < 1)
                throw new InputValueException(newValue.String, "{0} must be greater than 0.", newValue.String);

            m_nOutputFlux = newValue.Actual;
        }
        public void SetOutputSummary(InputValue<int> newValue)
        {
            System.Diagnostics.Debug.Assert(newValue != null);

            if (newValue.Actual < 1)
                throw new InputValueException(newValue.String, "{0} must be greater than 0.", newValue.String);

            m_nOutputSummary = newValue.Actual;
        }
        //---------------------------------------------------------------------

        public void SetSoilSpinUpFlag(InputValue<int> newValue)
        {
            if (newValue.Actual <= 0)
                m_nSoilSpinUpFlag = 0;
            else
                m_nSoilSpinUpFlag = 1;
        }

        public void SetTolerance(InputValue<double> newValue)
        {
            m_dSpinUpTolerance = Util.CheckBiomassParm(newValue, 0, 100);
        }

        public void SetIterations(InputValue<int> newValue)
        {
            if (newValue.Actual < 1)
                throw new InputValueException(newValue.String, "{0} must be at least 1.", newValue.String);

            m_nSpinUpIterations = newValue.Actual;
        }

        //---------------------------------------------------------------------

        public void SetMinRelativeBiomass(byte shadeClass,
                                          IEcoregion ecoregion,
                                          InputValue<Percentage> newValue)
        {
            if (newValue != null)
            {
                if (newValue.Actual < 0.0 || newValue.Actual > 1.0)
                    throw new InputValueException(newValue.String,
                                                  "{0} is not between 0% and 100%", newValue.String);
            }
            minRelativeBiomass[shadeClass][ecoregion] = newValue;
        }
        //---------------------------------------------------------------------

        public void SetFunctionalType(ISpecies species,
                                     InputValue<int> newValue)
        {
            sppFunctionalType[species] = Util.CheckBiomassParm(newValue, 0, 100);
        }
        //---------------------------------------------------------------------

        public void SetLeafLongevity(ISpecies species,
                                     InputValue<double> newValue)
        {
            //Debug.Assert(species != null);
            leafLongevity[species] = Util.CheckBiomassParm(newValue, 0.9, 5.0);
        }

        //---------------------------------------------------------------------

        public void SetMortCurveShape(ISpecies species,
                                     InputValue<double> newValue)
        {
            //Debug.Assert(species != null);
            mortCurveShape[species] = Util.CheckBiomassParm(newValue, 1.0, 50.0);
        }
        //---------------------------------------------------------------------

        public void SetGrowthCurveShape(ISpecies species,
                                     InputValue<double> newValue)
        {
            //Debug.Assert(species != null);
            growthCurveShape[species] = Util.CheckBiomassParm(newValue, 0.0, 1.0);
        }
       //---------------------------------------------------------------------

        public void SetMerchStemsMinAge(ISpecies species, InputValue<int> newValue)
        {
            System.Diagnostics.Debug.Assert(newValue != null);
            if (newValue.Actual < 0)
                throw new InputValueException(newValue.String, "SpeciesParameters.MinAge: {0} is not greater than 0.", newValue.String);
            if (newValue.Actual > species.Longevity)
                PlugIn.ModelCore.UI.WriteLine("SpeciesParameters.MinAge: Species {0} has MinAge of {1} which is greater than the species longevity.", species.Name, newValue.String);
            m_anMerchStemsMinAge[species] = newValue;
        }
        //---------------------------------------------------------------------

        public void SetMerchCurveParmA(ISpecies species, InputValue<double> newValue)
        {
            m_adMerchCurveParmA[species] = Util.CheckBiomassParm(newValue, 0.0, 1.0);
        }
        //---------------------------------------------------------------------

        public void SetMerchCurveParmB(ISpecies species, InputValue<double> newValue)
        {
            m_adMerchCurveParmB[species] = Util.CheckBiomassParm(newValue, 0.0, 1.0);
        }
        //---------------------------------------------------------------------

        public void SetPropNonMerch(ISpecies species, InputValue<double> newValue)
        {
            m_adPropNonMerch[species] = Util.CheckBiomassParm(newValue, 0.0, 1.0);
        }
        //---------------------------------------------------------------------

        public void SetFieldCapacity(IEcoregion ecoregion, InputValue<double> newValue)
        {
            //Debug.Assert(ecoregion != null);
            fieldCapacity[ecoregion] = Util.CheckBiomassParm(newValue, 0.0, 1.0);
        }
        //---------------------------------------------------------------------

        public void SetFieldCapacity(IEcoregion ecoregion, double newValue)
        {
            fieldCapacity[ecoregion] = newValue;
        }
        //---------------------------------------------------------------------

        public void SetLatitude(IEcoregion ecoregion, InputValue<double> newValue)
        {
            //Debug.Assert(ecoregion != null);
            latitude[ecoregion] = Util.CheckBiomassParm(newValue, 0.0, 50.0);
        }
        //---------------------------------------------------------------------

        public void SetLatitude(IEcoregion ecoregion, double newValue)
        {
            latitude[ecoregion] = newValue;
        }

        //---------------------------------------------------------------------
        public void SetDOMPool(int nID, string sName)
        {
            //Debug.Assert(nID > 0);
            //Debug.Assert(string.IsNullOrEmpty(sName));
            DOMPool pool = new DOMPool(nID, sName);
            this.m_dictDOMPools.Add(pool.ID, pool);
        }
        //---------------------------------------------------------------------
        public void SetDOMPool(int nID, string sName, double dQ10, double dPropAtm)
        {
            //Debug.Assert(nID > 0);
            //Debug.Assert(string.IsNullOrEmpty(sName));
            DOMPool pool = new DOMPool(nID, sName, dQ10, dPropAtm);
            this.m_dictDOMPools.Add(pool.ID, pool);
        }
        //---------------------------------------------------------------------
        public void SetDOMDecayRate(IEcoregion ecoregion, ISpecies species, int idxDOMPool, InputValue<double> newValue)
        {
            //Debug.Assert(ecoregion != null);
            if (newValue.Actual == 0 && idxDOMPool != 9)
            {
                string strCombo = "Ecoregion: " + ecoregion.Name;
                strCombo += " species: " + species.Name;
                int actPool = idxDOMPool + 1;
                strCombo += " DOMPool: ";
                strCombo += actPool;
                PlugIn.ModelCore.UI.WriteLine("Warning: Decay rate for " + strCombo + " is 0. No decay will occur.");
            }
            this.m_aDOMDecayRates[ecoregion][species][idxDOMPool] = Util.CheckBiomassParm(newValue, 0.0, 1.0);
        }
        //---------------------------------------------------------------------
        public void SetDOMPoolAmountT0(IEcoregion ecoregion, ISpecies species, int idxDOMPool, InputValue<double> newValue)
        {
            //Debug.Assert(ecoregion != null);
            if (newValue.Actual < 0)
                throw new InputValueException(newValue.String, "{0} is not greater than 0.", newValue.String);
            if (newValue.Actual == 0 && idxDOMPool != 9)
            {
                string strCombo = "Ecoregion: " + ecoregion.Name;
                strCombo += " species: " + species.Name;
                int actPool = idxDOMPool + 1;
                strCombo += " DOMPool: ";
                strCombo += actPool;
                PlugIn.ModelCore.UI.WriteLine("Warning: Initial DOM value for " + strCombo + " is 0. This can cause modelling artifacts.");
            }
            this.m_aDOMPoolAmountT0[ecoregion][species][idxDOMPool] = newValue;
        }
        //---------------------------------------------------------------------
        public void SetDOMPoolQ10(IEcoregion ecoregion, ISpecies species, int idxDOMPool, InputValue<double> newValue)
        {
            //Debug.Assert(ecoregion != null);
            this.m_aDOMPoolQ10[ecoregion][species][idxDOMPool] = Util.CheckBiomassParm(newValue, 1.0, 5.0);
        }
        //---------------------------------------------------------------------

        public void SetDOMInitialVFastAG(IEcoregion ecoregion, ISpecies species, InputValue<double> newValue)
        {
            Debug.Assert(species != null);
            Debug.Assert(ecoregion != null);
            m_DOMInitialVFastAG[ecoregion][species] = CheckBiomassParm(newValue, 0.0, 1.0);
        }
        //---------------------------------------------------------------------

        public void SetPropBiomassFine(InputValue<double> dProp)
        {
            m_dPropBiomassFine = CheckBiomassParm(dProp, 0.0, 1.0);
        }
        public void SetPropBiomassCoarse(InputValue<double> dProp)
        {
            m_dPropBiomassCoarse = CheckBiomassParm(dProp, 0.0, 1.0);
        }
        //---------------------------------------------------------------------

        public void SetPropDOMSlowAGToSlowBG(InputValue<double> dProp)
        {
            m_dPropDOMSlowAGToSlowBG = CheckBiomassParm(dProp, 0.0, 1.0);
        }
        public void SetPropDOMStemSnagToMedium(InputValue<double> dProp)
        {
            m_dPropDOMStemSnagToMedium = CheckBiomassParm(dProp, 0.0, 1.0);
        }
        public void SetPropDOMBranchSnagToFastAG(InputValue<double> dProp)
        {
            m_dPropDOMBranchSnagToFastAG = CheckBiomassParm(dProp, 0.0, 1.0);
        }
        //---------------------------------------------------------------------

        public void SetDisturbFireFromDOMPools(IDisturbTransferFromPools[] aDisturbTransfer)
        {
            m_aDisturbFireFromDOMPools = aDisturbTransfer;
        }

        public void SetDisturbOtherFromDOMPools(IDictionary<string, IDisturbTransferFromPools> dictDisturbTransfer)
        {
            m_dictDisturbOtherFromDOMPools = dictDisturbTransfer;
        }
        //---------------------------------------------------------------------

        public void SetDisturbFireFromBiomassPools(IDisturbTransferFromPools[] aDisturbTransfer)
        {
            m_aDisturbFireFromBiomassPools = aDisturbTransfer;
        }

        public void SetDisturbOtherFromBiomassPools(IDictionary<string, IDisturbTransferFromPools> dictDisturbTransfer)
        {
            m_dictDisturbOtherFromBiomassPools = dictDisturbTransfer;
        }
        //---------------------------------------------------------------------
        //Roots
        public int SetMinWoodyBio(IEcoregion ecoregion, ISpecies species, InputValue<double> newValue)
        {
            if (newValue.Actual < 0)
                throw new InputValueException(newValue.String, "{0} is not greater than 0.", newValue.String);
            int i = 0;
            for (i = 0; i < 5; i++)
            {
                if (m_MinWoodyBio[ecoregion][species][i] == -999)
                {
                    m_MinWoodyBio[ecoregion][species][i] = newValue;
                    break;
                }
            }
            return (i);
        }
        public void SetRootRatio(IEcoregion ecoregion, ISpecies species, InputValue<double> newValue, int i)
        {
            m_Ratio[ecoregion][species][i] = CheckBiomassParm(newValue, 0.0, 1.0);
        }
        public void SetPropFine(IEcoregion ecoregion, ISpecies species, InputValue<double> newValue, int i)
        {
             m_PropFine[ecoregion][species][i] = CheckBiomassParm(newValue, 0.0, 1.0);
        }
        public void SetFineTurnover(IEcoregion ecoregion, ISpecies species, InputValue<double> newValue, int i)
        {
            m_FineTurnover[ecoregion][species][i] = CheckBiomassParm(newValue, 0.0, 1.0);
        }
        public void SetCoarseTurnover(IEcoregion ecoregion, ISpecies species, InputValue<double> newValue, int i)
        {
            m_CoarseTurnover[ecoregion][species][i] = CheckBiomassParm(newValue, 0.0, 1.0);
        }

        //---------------------------------------------------------------------
        //MARCH: initial snag information
        /*
        public void SetInitSnagInfo(ISpecies species, InputValue<int> dAgeAtDeath, InputValue<int> dTimeSinceDeath, InputValue<string> sDisturbType, int i)
        {
            m_SnagSpecies[i] = species.Index;
            m_SnagAgeAtDeath[i] = Util.CheckBiomassParm(dAgeAtDeath, 0, 999);
            m_SnagTimeSinceDeath[i] = Util.CheckBiomassParm(dTimeSinceDeath, 0, 999);
            m_SnagDisturb[i] = sDisturbType;
        }
        public int[] SnagSpecies { get { return m_SnagSpecies; } }
        public int[] SnagAgeAtDeath { get { return m_SnagAgeAtDeath; } }
        public int[] SnagTimeSinceDeath { get { return m_SnagTimeSinceDeath; } }
        public string[] SnagDisturb { get { return m_SnagDisturb; } }
        */
        //---------------------------------------------------------------------

        //---------------------------------------------------------------------

        public void SetANPPTimeCollection(IEcoregion ecoregion, ISpecies species, ITimeCollection<IANPP> oCollection)
        {
            //Debug.Assert(ecoregion != null);
            //Debug.Assert(species != null);
            //Debug.Assert(oTimeCollection != null);
            this.m_ANPPTimeCollection[ecoregion][species] = oCollection;
        }
        //---------------------------------------------------------------------

        public void SetMaxBiomassTimeCollection(IEcoregion ecoregion, ISpecies species, ITimeCollection<IMaxBiomass> oCollection)
        {
            //Debug.Assert(ecoregion != null);
            //Debug.Assert(species != null);
            //Debug.Assert(oTimeCollection != null);
            this.m_MaxBiomassTimeCollection[ecoregion][species] = oCollection;
        }
        //---------------------------------------------------------------------

        /*public void SetEstabProbTimeCollection(IEcoregion ecoregion, ISpecies species, ITimeCollection<IEstabProb> oCollection)
        {
            //Debug.Assert(ecoregion != null);
            //Debug.Assert(species != null);
            //Debug.Assert(oTimeCollection != null);
            this.m_EstabProbTimeCollection[species][ecoregion] = oCollection;
        }*/
        //---------------------------------------------------------------------

        public void SetEstablishProbability(IEcoregion ecoregion, ISpecies species, InputValue<double> dProp)
        {
            //Debug.Assert(ecoregion != null);
            //Debug.Assert(species != null);
            //this.m_dEstablishProbability[ecoregion][species] = CheckBiomassParm(dProp, 0.0, 1.0);
            this.m_dEstablishProbability[species][ecoregion] = CheckBiomassParm(dProp, 0.0, 1.0);
        }
        //---------------------------------------------------------------------

        public InputParameters()
        {

            minRelativeBiomass = new Landis.Library.Parameters.Ecoregions.AuxParm<Percentage>[6];
            for (byte shadeClass = 1; shadeClass <= 5; shadeClass++)
            {
                minRelativeBiomass[shadeClass] = new Landis.Library.Parameters.Ecoregions.AuxParm<Percentage>(PlugIn.ModelCore.Ecoregions);
            }

            sufficientLight = new List<ISufficientLight>();

            sppFunctionalType = new Landis.Library.Parameters.Species.AuxParm<int>(PlugIn.ModelCore.Species);
            leafLongevity = new Landis.Library.Parameters.Species.AuxParm<double>(PlugIn.ModelCore.Species);
            epicormic = new Landis.Library.Parameters.Species.AuxParm<bool>(PlugIn.ModelCore.Species);
            mortCurveShape = new Landis.Library.Parameters.Species.AuxParm<double>(PlugIn.ModelCore.Species);
            m_anMerchStemsMinAge = new Landis.Library.Parameters.Species.AuxParm<int>(PlugIn.ModelCore.Species);
            m_adMerchCurveParmA = new Landis.Library.Parameters.Species.AuxParm<double>(PlugIn.ModelCore.Species);
            m_adMerchCurveParmB = new Landis.Library.Parameters.Species.AuxParm<double>(PlugIn.ModelCore.Species);
            m_adPropNonMerch = new Landis.Library.Parameters.Species.AuxParm<double>(PlugIn.ModelCore.Species);
            growthCurveShape = new Landis.Library.Parameters.Species.AuxParm<double>(PlugIn.ModelCore.Species);

            fieldCapacity = new Landis.Library.Parameters.Ecoregions.AuxParm<double>(PlugIn.ModelCore.Ecoregions);
            latitude = new Landis.Library.Parameters.Ecoregions.AuxParm<double>(PlugIn.ModelCore.Ecoregions);

            this.m_dsSpecies = PlugIn.ModelCore.Species;
            this.m_dsEcoregion = PlugIn.ModelCore.Ecoregions;

            this.m_dictDOMPools = new Dictionary<int, IDOMPool>();
            this.m_aDOMDecayRates = CreateEcoregionSpeciesPoolParm<double>(SoilClass.NUMSOILPOOLS); // CreateSpeciesEcoregionPoolParm<double>();
            this.m_aDOMPoolAmountT0 = CreateEcoregionSpeciesPoolParm<double>(SoilClass.NUMSOILPOOLS); // CreateSpeciesEcoregionPoolParm<double>();
            this.m_aDOMPoolQ10 = CreateEcoregionSpeciesPoolParm<double>(SoilClass.NUMSOILPOOLS); // CreateSpeciesEcoregionPoolParm<double>();
            this.m_DOMInitialVFastAG = CreateEcoregionSpeciesParm<double>(); //  CreateSpeciesEcoregionParm<double>();
            this.m_dPropBiomassFine = 0.0;
            this.m_dPropBiomassCoarse = 0.0;
            this.m_dPropDOMSlowAGToSlowBG = 0.0;
            this.m_dPropDOMStemSnagToMedium = 0.0;
            this.m_dPropDOMBranchSnagToFastAG = 0.0;

            //Roots
            this.m_MinWoodyBio = CreateEcoregionSpeciesPoolParm<double>(5);
            this.m_Ratio = CreateEcoregionSpeciesPoolParm<double>(5);
            this.m_PropFine = CreateEcoregionSpeciesPoolParm<double>(5);
            this.m_FineTurnover = CreateEcoregionSpeciesPoolParm<double>(5);
            this.m_CoarseTurnover = CreateEcoregionSpeciesPoolParm<double>(5);
            //set the initial MinWoodyBiomass to -999 to indicate that it has not been initialized
            foreach (IEcoregion ecoregion in m_dsEcoregion)
            {
                foreach (ISpecies species in m_dsSpecies)
                {
                    for (int i=0; i<5; i++)
                        this.m_MinWoodyBio[ecoregion][species][i] = -999;
                }
            }

            //MARCH snag information
            /*
            this.m_SnagAgeAtDeath = new int[20];
            this.m_SnagTimeSinceDeath = new int[20];
            this.m_SnagSpecies = new int[20];
            this.m_SnagDisturb = new string[20];
            */
            // ANPP and Max Biomass Time Collection
            this.m_ANPPTimeCollection = new Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<ITimeCollection<IANPP>>>(m_dsEcoregion);
            this.m_MaxBiomassTimeCollection = new Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<ITimeCollection<IMaxBiomass>>>(m_dsEcoregion);
            this.m_EstabProbTimeCollection = new Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<ITimeCollection<IEstabProb>>>(m_dsEcoregion);
            foreach (IEcoregion ecoregion in m_dsEcoregion)
            {
                this.m_ANPPTimeCollection[ecoregion] = new Landis.Library.Parameters.Species.AuxParm<ITimeCollection<IANPP>>(m_dsSpecies);
                this.m_MaxBiomassTimeCollection[ecoregion] = new Landis.Library.Parameters.Species.AuxParm<ITimeCollection<IMaxBiomass>>(m_dsSpecies);
                this.m_EstabProbTimeCollection[ecoregion] = new Landis.Library.Parameters.Species.AuxParm<ITimeCollection<IEstabProb>>(m_dsSpecies);
                foreach (ISpecies species in m_dsSpecies)
                {
                    this.m_ANPPTimeCollection[ecoregion][species] = new TimeCollection<IANPP>();
                    this.m_MaxBiomassTimeCollection[ecoregion][species] = new TimeCollection<IMaxBiomass>();
                    this.m_EstabProbTimeCollection[ecoregion][species] = new TimeCollection<IEstabProb>();
                }
            }

            this.m_dEstablishProbability = CreateSpeciesEcoregionParm<double>();
       }

        private Landis.Library.Parameters.Species.AuxParm<Landis.Library.Parameters.Ecoregions.AuxParm<T>> CreateSpeciesEcoregionParm<T>()
        {
            Landis.Library.Parameters.Species.AuxParm<Landis.Library.Parameters.Ecoregions.AuxParm<T>> newParm;
            newParm = new Landis.Library.Parameters.Species.AuxParm<Landis.Library.Parameters.Ecoregions.AuxParm<T>>(m_dsSpecies);
            foreach (ISpecies species in m_dsSpecies)
            {
                newParm[species] = new Landis.Library.Parameters.Ecoregions.AuxParm<T>(m_dsEcoregion);
            }
            return newParm;
        }

        private Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<T>> CreateEcoregionSpeciesParm<T>()
        {
            Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<T>> newParm;
            newParm = new Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<T>>(m_dsEcoregion);
            foreach (IEcoregion ecoregion in m_dsEcoregion)
            {
                newParm[ecoregion] = new Landis.Library.Parameters.Species.AuxParm<T>(m_dsSpecies);
            }
            return newParm;
        }

        private Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<T[]>> CreateEcoregionSpeciesPoolParm<T>(int n)
        {
            Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<T[]>> newParm;
            newParm = new Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<T[]>>(m_dsEcoregion);
            foreach (IEcoregion ecoregion in m_dsEcoregion)
            {
                newParm[ecoregion] = new Landis.Library.Parameters.Species.AuxParm<T[]>(m_dsSpecies);
                foreach (ISpecies species in m_dsSpecies)
                {
                    newParm[ecoregion][species] = new T[n];
                }
            }
            return newParm;
        }

        private InputValue<double> CheckBiomassParm(InputValue<double> newValue, double minValue, double maxValue)
        {
            if (newValue != null)
            {
                if (newValue.Actual < minValue || newValue.Actual > maxValue)
                    throw new InputValueException(newValue.String, "{0} is not between [{1:0.0}, {2:0.0}]", newValue.String, minValue, maxValue);
            }
            return newValue;
        }
        //---------------------------------------------------------------------

        private void ValidatePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new InputValueException();
            if (path.Trim(null).Length == 0)
                throw new InputValueException(path,
                                              "\"{0}\" is not a valid path.",
                                              path);
        }

    }
}
