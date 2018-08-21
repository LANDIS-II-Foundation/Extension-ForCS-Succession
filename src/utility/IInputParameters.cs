//  Copyright 2005-2010 Portland State University, University of Wisconsin
//  Authors:  Robert M. Scheller, James B. Domingo

using Landis.Library.Succession;
using Landis.Core;

using Edu.Wisc.Forest.Flel.Util;

using System.Collections.Generic;
using System.Diagnostics;

namespace Landis.Extension.Succession.ForC
{
    /// <summary>
    /// The parameters for biomass succession.
    /// </summary>
    public interface IInputParameters
    //    : Dynamic.IParameters
    {
        int Timestep { get; set;}
        SeedingAlgorithms SeedAlgorithm { get;set;}
        string InitialCommunities { get; set; }
        string InitialCommunitiesMap { get; set; }
        bool CalibrateMode { get; set; }
        double SpinupMortalityFraction { get; set;}
        string ClimateFile { get;set;}
        string ClimateFile2 { get; set; }
        string InitSnagFile { get; set; }

        int OutputBiomass { get; }
        int OutputDOMPools { get; }
        int OutputFlux { get; }
        int OutputSummary { get; }

        int SoilSpinUpFlag { get; }
        double SpinUpTolerance { get ; }
        int SpinUpIterations { get ; }

        /// The maximum relative biomass for each shade class.
        Ecoregions.AuxParm<Percentage>[] MinRelativeBiomass { get; }
        /// Definitions of sufficient light probabilities.
        List<ISufficientLight> LightClassProbabilities { get; }

        Species.AuxParm<int> SppFunctionalType { get;}
        Species.AuxParm<double> LeafLongevity { get;}
        Species.AuxParm<bool> Epicormic { get;}
        Species.AuxParm<double> MortCurveShape { get;}
        Species.AuxParm<int> MerchStemsMinAge { get;}
        Species.AuxParm<double> MerchCurveParmA { get;}
        Species.AuxParm<double> MerchCurveParmB { get;}
        Species.AuxParm<double> PropNonMerch { get;}
        Species.AuxParm<double> GrowthCurveShapeParm { get;}

        Ecoregions.AuxParm<double> FieldCapacity { get;}
        Ecoregions.AuxParm<double> Latitude { get;}

        IDictionary<int, IDOMPool> DOMPools { get;}
        Ecoregions.AuxParm<Species.AuxParm<double[]>> DOMDecayRates { get;}
        Ecoregions.AuxParm<Species.AuxParm<double[]>> DOMPoolAmountT0 { get;}
        Ecoregions.AuxParm<Species.AuxParm<double[]>> DOMPoolQ10 { get;}
        double PropBiomassFine { get;}
        double PropBiomassCoarse { get;}
        double PropDOMSlowAGToSlowBG { get;}
        double PropDOMStemSnagToMedium { get;}
        double PropDOMBranchSnagToFastAG { get;}

        //root parameterss
        Ecoregions.AuxParm<Species.AuxParm<double[]>> MinWoodyBio { get;}
        Ecoregions.AuxParm<Species.AuxParm<double[]>> Ratio { get;}
        Ecoregions.AuxParm<Species.AuxParm<double[]>> PropFine { get;}
        Ecoregions.AuxParm<Species.AuxParm<double[]>> FineTurnover { get;}
        Ecoregions.AuxParm<Species.AuxParm<double[]>> CoarseTurnover { get;}

        /*
        //MARCH: initial snag parameters
        int[] SnagSpecies { get ; } 
        int[] SnagAgeAtDeath { get; }
        int[] SnagTimeSinceDeath { get; }
        string[] SnagDisturb { get; }
        */

        /// <summary>
        /// Returns an array of IDisturbTransferFromPools objects, indexed by a 0-based severity level.
        /// </summary>
        IDisturbTransferFromPools[] DisturbFireFromDOMPools { get;}

        /// <summary>
        /// Returns a dictionary of IDisturbTransferFromPools objects, indexed by name of the disturbance (e.g. "Harvest").
        /// </summary>
        IDictionary<string, IDisturbTransferFromPools> DisturbOtherFromDOMPools { get;}

        /// <summary>
        /// Returns an array of IDisturbTransferFromPools objects, indexed by a 0-based severity level.
        /// </summary>
        IDisturbTransferFromPools[] DisturbFireFromBiomassPools { get;}

        /// <summary>
        /// Returns a dictionary of IDisturbTransferFromPools objects, indexed by name of the disturbance (e.g. "Harvest").
        /// </summary>
        IDictionary<string, IDisturbTransferFromPools> DisturbOtherFromBiomassPools { get;}

        Ecoregions.AuxParm<Species.AuxParm<ITimeCollection<IANPP>>> ANPPTimeCollection { get;}
        Ecoregions.AuxParm<Species.AuxParm<ITimeCollection<IMaxBiomass>>> MaxBiomassTimeCollection { get;}
        Ecoregions.AuxParm<Species.AuxParm<ITimeCollection<IEstabProb>>> EstabProbTimeCollection { get;}
        //Species.AuxParm<Ecoregions.AuxParm<ITimeCollection<IEstabProb>>> EstabProbTimeCollection { get;}

        //Ecoregions.AuxParm<Species.AuxParm<double>> EstablishProbability { get;}
        Species.AuxParm<Ecoregions.AuxParm<double>> EstablishProbability { get;}

        //---------------------------------------------------------------------

        /// <summary>
        /// Path to the optional file with the biomass parameters for age-only
        /// disturbances.
        /// </summary>
        string AgeOnlyDisturbanceParms
        {
            get;
            set;
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// A list of zero or more updates to the biomass parameters because of
        /// climate change.
        /// </summary>
        //List<Dynamic.ParametersUpdate> DynamicUpdates
        //{
        //    get;
        //}
    }
}
