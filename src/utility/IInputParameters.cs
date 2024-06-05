//  Authors:  Caren Dymond, Sarah Beukema

using Landis.Library.Succession;
using Landis.Core;
using Landis.Utilities;
using Landis.Library.Parameters;

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
        string DMFile { get; set; }

        int OutputBiomass { get; }
        int OutputDOMPools { get; }
        int OutputFlux { get; }
        int OutputSummary { get; }
        int OutputMap { get; }
        string OutputMapPath { get;  }

        int SoilSpinUpFlag { get; }
        double SpinUpTolerance { get ; }
        int SpinUpIterations { get ; }

        /// The maximum relative biomass for each shade class.
        Landis.Library.Parameters.Ecoregions.AuxParm<Percentage>[] MinRelativeBiomass { get; }
        /// Definitions of sufficient light probabilities.
        List<ISufficientLight> LightClassProbabilities { get; }

        Landis.Library.Parameters.Species.AuxParm<int> SppFunctionalType { get;}
        Landis.Library.Parameters.Species.AuxParm<double> LeafLongevity { get;}
        Landis.Library.Parameters.Species.AuxParm<bool> Epicormic { get;}
        Landis.Library.Parameters.Species.AuxParm<byte> FireTolerance { get; }
        Landis.Library.Parameters.Species.AuxParm<byte> ShadeTolerance { get; }
        Landis.Library.Parameters.Species.AuxParm<double> MortCurveShape { get;}
        Landis.Library.Parameters.Species.AuxParm<int> MerchStemsMinAge { get;}
        Landis.Library.Parameters.Species.AuxParm<double> MerchCurveParmA { get;}
        Landis.Library.Parameters.Species.AuxParm<double> MerchCurveParmB { get;}
        Landis.Library.Parameters.Species.AuxParm<double> PropNonMerch { get;}
        Landis.Library.Parameters.Species.AuxParm<double> GrowthCurveShapeParm { get;}

        Landis.Library.Parameters.Ecoregions.AuxParm<double> FieldCapacity { get;}
        Landis.Library.Parameters.Ecoregions.AuxParm<double> Latitude { get;}

        IDictionary<int, IDOMPool> DOMPools { get;}
        Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<double[]>> DOMDecayRates { get;}
        Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<double[]>> DOMPoolAmountT0 { get;}
        Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<double[]>> DOMPoolQ10 { get;}
        double PropBiomassFine { get;}
        double PropBiomassCoarse { get;}
        double PropDOMSlowAGToSlowBG { get;}
        double PropDOMStemSnagToMedium { get;}
        double PropDOMBranchSnagToFastAG { get;}

        //root parameterss
        Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<double[]>> MinWoodyBio { get;}
        Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<double[]>> Ratio { get;}
        Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<double[]>> PropFine { get;}
        Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<double[]>> FineTurnover { get;}
        Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<double[]>> CoarseTurnover { get;}

        /*
        //MARCH: initial snag parameters
        int[] SnagSpecies { get ; } 
        int[] SnagAgeAtDeath { get; }
        int[] SnagTimeSinceDeath { get; }
        string[] SnagDisturb { get; }
        */

        Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<ITimeCollection<IANPP>>> ANPPTimeCollection { get;}
        Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<ITimeCollection<IMaxBiomass>>> MaxBiomassTimeCollection { get;}
        Landis.Library.Parameters.Ecoregions.AuxParm<Landis.Library.Parameters.Species.AuxParm<ITimeCollection<IEstabProb>>> EstabProbTimeCollection { get;}
        //Species.AuxParm<Ecoregions.AuxParm<ITimeCollection<IEstabProb>>> EstabProbTimeCollection { get;}

        //Ecoregions.AuxParm<Species.AuxParm<double>> EstablishProbability { get;}
        Landis.Library.Parameters.Species.AuxParm<Landis.Library.Parameters.Ecoregions.AuxParm<double>> EstablishProbability { get;}

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
