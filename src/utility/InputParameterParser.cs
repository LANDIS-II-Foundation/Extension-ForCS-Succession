//  Authors:  Caren Dymond, Sarah Beukema

using Landis.Library.Succession;
using Landis.Core;
using Landis.SpatialModeling;
using System.Collections.Generic;
using Landis.Utilities;

namespace Landis.Extension.Succession.ForC
{
    /// <summary>
    /// A parser that reads biomass succession parameters from text input.
    /// </summary>
    public class InputParametersParser
        : Landis.Utilities.TextParser<IInputParameters>
    {
        public static class Names
        {
            public const string Timestep = "Timestep";
            public const string SeedingAlgorithm = "SeedingAlgorithm";
            public const string CalibrateMode = "CalibrateMode";
            public const string ClimateFile = "ClimateFile";
            public const string ClimateFile2 = "ForCSClimateFile";
            public const string PnETGroupFile = "PnETfunctionalGroupFile";
            public const string ForCSOutput = "ForCSOutput";
            public const string SpinUp = "SpinUp";
            public const string AvailableLightBiomass = "AvailableLightBiomass";
            public const string LightEstablishmentTable = "LightEstablishmentTable";
            public const string SpeciesParameters = "SpeciesParameters";
            public const string FireReductionParameters = "FireReductionParameters";
            public const string AgeOnlyDisturbanceParms = "AgeOnlyDisturbances:BiomassParameters";
            public const string DOMPools = "DOMPools";
            public const string EcoSppDOMParms = "EcoSppDOMParameters";
            public const string ForCSProportions = "ForCSProportions";
            public const string DisturbFireTransferDOM = "DisturbFireTransferDOM";
            public const string DisturbOtherTransferDOM = "DisturbOtherTransferDOM";
            public const string DisturbFireTransferBiomass = "DisturbFireTransferBiomass";
            public const string DisturbOtherTransferBiomass = "DisturbOtherTransferBiomass";
            public const string ANPPTimeSeries = "ANPPTimeSeries";
            public const string MaxBiomassTimeSeries = "MaxBiomassTimeSeries";
            public const string EstablishProbabilities = "EstablishProbabilities";
            public const string RootDynamics = "RootDynamics";
            public const string initSnagFile = "SnagFile";
            public const string SnagData = "SnagData";
            public const string DMFile = "DisturbanceMatrixFile";

        }

        private delegate void SetParmMethod<TParm>(ISpecies          species,
                                                   IEcoregion        ecoregion,
                                                   InputValue<TParm> newValue);

        //---------------------------------------------------------------------

        private IEcoregionDataset ecoregionDataset;
        private ISpeciesDataset speciesDataset;
        private Dictionary<string, int> speciesLineNums;
        private InputVar<string> speciesName;
        //private InputVar<string> ecoregionName;

        ////---------------------------------------------------------------------

        //public override string LandisDataValue
        //{
        //    get {
        //        return "ForC Succession";
        //    }
        //}

        //---------------------------------------------------------------------

        static InputParametersParser()
        {
            SeedingAlgorithmsUtil.RegisterForInputValues();
            
            //    FIXME: Need to add RegisterForInputValues method to
            //  Percentage class, but for now, we'll trigger it by creating
            //  a local variable of that type.
            Percentage dummy = new Percentage();
            //RegisterForInputValues();

        }

        //---------------------------------------------------------------------

        public InputParametersParser()
        {
            this.ecoregionDataset = PlugIn.ModelCore.Ecoregions;
            this.speciesDataset = PlugIn.ModelCore.Species;
            this.speciesLineNums = new Dictionary<string, int>();
            this.speciesName = new InputVar<string>("Species");
        }

        //---------------------------------------------------------------------

        protected override IInputParameters Parse()
        {
            InputVar<string> landisData = new InputVar<string>("LandisData");
            ReadVar(landisData);
            if (landisData.Value.Actual != PlugIn.ExtensionName)
                throw new InputValueException(landisData.Value.String, "The value is not \"{0}\"", PlugIn.ExtensionName);

            StringReader currentLine;
            Dictionary<string, int> lineNumbers = new Dictionary<string, int>();

            // InputVars common to different read routines.
            InputVar<int> nDOMPoolID = new InputVar<int>("DOMPoolID");
            InputVar<double> dPropAir = new InputVar<double>("Prop to Air");
            InputVar<double> dPropFloor = new InputVar<double>("Prop to Floor");
            InputVar<double> dPropFPS = new InputVar<double>("Prop to FPS");
            InputVar<double> dPropDOM = new InputVar<double>("Prop to DOM");
            InputVar<string> sDisturbType = new InputVar<string>("Disturbance Type");
            InputVar<int> nIntensity = new InputVar<int>("Intensity");
            InputVar<int> nBiomassPoolID = new InputVar<int>("Biomass Pool ID");
            int nread = 0;
            int neco = 0;

            //ReadLandisDataVar();
            
            InputParameters parameters = new InputParameters();  

            //Get number of active ecoregions
            foreach (IEcoregion ecoregion in PlugIn.ModelCore.Ecoregions)
            {
                if (ecoregion.Active) neco++;
            }
            
            InputVar<int> timestep = new InputVar<int>(Names.Timestep);
            ReadVar(timestep);
            parameters.Timestep = timestep.Value;

            InputVar<SeedingAlgorithms> seedAlg = new InputVar<SeedingAlgorithms>(Names.SeedingAlgorithm);
            ReadVar(seedAlg);
            parameters.SeedAlgorithm = seedAlg.Value;

            /*LANDIS CLIMATE LIBRARY
            InputVar<string> climateFile = new InputVar<string>(Names.ClimateFile);
            ReadVar(climateFile);
            parameters.ClimateFile = climateFile.Value;
             */

            InputVar<string> climateFile2 = new InputVar<string>(Names.ClimateFile2);
            ReadVar(climateFile2);
            parameters.ClimateFile2 = climateFile2.Value;

            //input from the biomass version of the model
            //---------------------------------------------------------------------------------

            InputVar<string> initCommunities = new InputVar<string>("InitialCommunities");
            ReadVar(initCommunities);
            parameters.InitialCommunities = initCommunities.Value;

            InputVar<string> communitiesMap = new InputVar<string>("InitialCommunitiesMap");
            ReadVar(communitiesMap);
            parameters.InitialCommunitiesMap = communitiesMap.Value;

            //---------------------------------------------------------------------------------

            InputVar<string> dmFile = new InputVar<string>(Names.DMFile);
            ReadVar(dmFile);
            parameters.DMFile = dmFile.Value;

            //MARCH: new snag initial conditions file (optional)
            InputVar<string> initSnagFile = new InputVar<string>(Names.initSnagFile);
            if (ReadOptionalVar(initSnagFile))
                parameters.InitSnagFile = initSnagFile.Value;


            //input from the biomass version of the model (optional variables)
            InputVar<bool> calimode = new InputVar<bool>(Names.CalibrateMode);
            if (ReadOptionalVar(calimode))
                parameters.CalibrateMode = calimode.Value;
            else
                parameters.CalibrateMode = false;

            InputVar<double> spinMort = new InputVar<double>("SpinupMortalityFraction");
            if (ReadOptionalVar(spinMort))
                parameters.SpinupMortalityFraction = spinMort.Value;
            else
                parameters.SpinupMortalityFraction = 0.0;

            // ForCSOutput
            ReadName(Names.ForCSOutput);
            currentLine = new StringReader(CurrentLine);
            InputVar<int> nOutputBiomass = new InputVar<int>("Output Biomass Interval");
            ReadValue(nOutputBiomass, currentLine);
            parameters.SetOutputBiomass(nOutputBiomass.Value);
            InputVar<int> nOutputDOMPools = new InputVar<int>("Output DOM Pools Interval");
            ReadValue(nOutputDOMPools, currentLine);
            parameters.SetOutputDOMPools(nOutputDOMPools.Value);
            InputVar<int> nOutputFlux = new InputVar<int>("Output Flux Interval");
            ReadValue(nOutputFlux, currentLine);
            parameters.SetOutputFlux(nOutputFlux.Value);
            InputVar<int> nOutputSummary = new InputVar<int>("Output Summary Interval");
            ReadValue(nOutputSummary, currentLine);
            parameters.SetOutputSummary(nOutputSummary.Value);

            //PlugIn.ModelCore.UI.WriteLine("Intervals so far {0}, {1}, {2}, {3}", parameters.OutputBiomass, parameters.OutputDOMPools, parameters.OutputFlux, parameters.OutputSummary );
            GetNextLine();

            InputVar<string> sMapDir = new InputVar<string>("MapOutputDir");
            if (ReadOptionalVar(sMapDir))
            {
                parameters.OutputMapPath = sMapDir.Value;
                //throw NewParseException("Got Map Dir {0}", sMapDir.Value);

            }
            else
            {
                parameters.OutputMapPath = "ForCS";
                //throw NewParseException("Didn't get Map Dir {0}", sMapDir.Value);
            }

            InputVar<int> nMapOutput = new InputVar<int>("MapOutputInterval");
            if (ReadOptionalVar(nMapOutput))
            {
                parameters.SetOutputMap(nMapOutput.Value);  //note: defaults to 0 if not present
            }
            PlugIn.ModelCore.UI.WriteLine("Intervals so far {0}, {1}", parameters.OutputMapPath, parameters.OutputMap);

            //GetNextLine();

            // SoilSpinUp
            ReadName(Names.SpinUp);
            currentLine = new StringReader(CurrentLine);
            InputVar<int> nSoilSpinFlag = new InputVar<int>("On/Off Flag");
            ReadValue(nSoilSpinFlag, currentLine);
            parameters.SetSoilSpinUpFlag(nSoilSpinFlag.Value);
            InputVar<int> nBiomassSpinFlag = new InputVar<int>("Biomass Spin-Up Flag");
            ReadValue(nBiomassSpinFlag, currentLine);
            parameters.SetBiomassSpinUpFlag(nBiomassSpinFlag.Value);
            InputVar<double> dTolerance = new InputVar<double>("Tolerance");
            ReadValue(dTolerance, currentLine);
            parameters.SetTolerance(dTolerance.Value);
            InputVar<int> nIterations = new InputVar<int>("Max Iterations");
            ReadValue(nIterations, currentLine);
            parameters.SetIterations(nIterations.Value);
            GetNextLine();

            //--------------------------
            //  MinRelativeBiomass table

            ReadName(Names.AvailableLightBiomass);

            List<IEcoregion> ecoregions = ReadEcoregions();
            string lastEcoregion = ecoregions[ecoregions.Count-1].Name;

            InputVar<byte> shadeClassVar = new InputVar<byte>("Shade Class");
            for (byte shadeClass = 1; shadeClass <= 5; shadeClass++) {
                if (AtEndOfInput)
                    throw NewParseException("Expected a line with available light class {0}", shadeClass);

                currentLine = new StringReader(CurrentLine);
                ReadValue(shadeClassVar, currentLine);
                if (shadeClassVar.Value.Actual != shadeClass)
                    throw new InputValueException(shadeClassVar.Value.String,
                                                  "Expected the available light class {0}", shadeClass);

                foreach (IEcoregion ecoregion in ecoregions) 
                {
                    InputVar<Percentage> MinRelativeBiomass = new InputVar<Percentage>("Ecoregion " + ecoregion.Name);
                    ReadValue(MinRelativeBiomass, currentLine);
                    parameters.SetMinRelativeBiomass(shadeClass, ecoregion, MinRelativeBiomass.Value);
                }

                CheckNoDataAfter("the Ecoregion " + lastEcoregion + " column",
                                 currentLine);
                GetNextLine();
            }

            //----------------------------------------------------------
            //  Read table of sufficient light probabilities.
            //  Available light classes are in increasing order.
            ReadName(Names.LightEstablishmentTable);

            InputVar<byte> sc = new InputVar<byte>("Available Light Class");
            InputVar<double> pl0 = new InputVar<double>("Probability of Germination - Light Level 0");
            InputVar<double> pl1 = new InputVar<double>("Probability of Germination - Light Level 1");
            InputVar<double> pl2 = new InputVar<double>("Probability of Germination - Light Level 2");
            InputVar<double> pl3 = new InputVar<double>("Probability of Germination - Light Level 3");
            InputVar<double> pl4 = new InputVar<double>("Probability of Germination - Light Level 4");
            InputVar<double> pl5 = new InputVar<double>("Probability of Germination - Light Level 5");
            
            int previousNumber = 0;

            while (! AtEndOfInput && CurrentName != Names.SpeciesParameters
                                  && previousNumber != 6) {
                currentLine = new StringReader(CurrentLine);

                ISufficientLight suffLight = new SufficientLight();
                
                parameters.LightClassProbabilities.Add(suffLight);

                ReadValue(sc, currentLine);
                suffLight.ShadeClass = sc.Value;

                //  Check that the current shade class is 1 more than
                //  the previous number (numbers are must be in increasing order).
                if (sc.Value.Actual != (byte) previousNumber + 1)
                    throw new InputValueException(sc.Value.String,
                                                  "LightEstablishmentTable: Expected the severity number {0}",
                                                  previousNumber + 1);
                previousNumber = (int) sc.Value.Actual;

                ReadValue(pl0, currentLine);
                suffLight.ProbabilityLight0 = pl0.Value;

                ReadValue(pl1, currentLine);
                suffLight.ProbabilityLight1 = pl1.Value;

                ReadValue(pl2, currentLine);
                suffLight.ProbabilityLight2 = pl2.Value;

                ReadValue(pl3, currentLine);
                suffLight.ProbabilityLight3 = pl3.Value;

                ReadValue(pl4, currentLine);
                suffLight.ProbabilityLight4 = pl4.Value;

                ReadValue(pl5, currentLine);
                suffLight.ProbabilityLight5 = pl5.Value;


                CheckNoDataAfter("the " + pl5.Name + " column",
                                 currentLine);
                GetNextLine();
            }
            if (parameters.LightClassProbabilities.Count == 0)
                throw NewParseException("LightEstablishmentTable: Insufficient light probabilities defined.");
            if (previousNumber != 5)
                throw NewParseException("LightEstablishmentTable: Expected shade class {0}", previousNumber + 1);

            //-------------------------
            //  Species Parameters table

            ReadName(Names.SpeciesParameters);

            speciesLineNums.Clear();  //  If parser re-used (i.e., for testing purposes)

            InputVar<int> ft = new InputVar<int>("Functional Type");
            InputVar<double> leafLongevity = new InputVar<double>("Leaf Longevity");
            InputVar<byte> shadeTolerance = new InputVar<byte>("Shade Tolerance");
            InputVar<byte> fireTolerance = new InputVar<byte>("Fire Tolerance");
            InputVar<double> mortCurveShapeParm = new InputVar<double>("Mortality Curve Shape Parameter");
            InputVar<double> mbm = new InputVar<double>("Max Biomass Multiplier");
            InputVar<int> nMerchStemsMinAge = new InputVar<int>("Merch. Stems Min Age");
            InputVar<double> dMerchCurveParmA = new InputVar<double>("Merch. Curve Parm A");
            InputVar<double> dMerchCurveParmB = new InputVar<double>("Merch. Curve Parm B");
            InputVar<double> dPropNonMerch = new InputVar<double>("Proportion Non-Merchantible");
            InputVar<double> growthCurveShapeParm = new InputVar<double>("Growth Curve Shape Parameter");
            
            string lastColumn = "the " + mbm.Name + " column";

            nread = 0;
            //while (!AtEndOfInput && (CurrentName != Names.AgeOnlyDisturbanceParms))
                while (!AtEndOfInput && (CurrentName != Names.DOMPools))
                {
                currentLine = new StringReader(CurrentLine);
                ISpecies species = ReadSpecies(currentLine);

                ReadValue(leafLongevity, currentLine);
                parameters.SetLeafLongevity(species, leafLongevity.Value);

                ReadValue(mortCurveShapeParm, currentLine);
                parameters.SetMortCurveShape(species, mortCurveShapeParm.Value);

                ReadValue(nMerchStemsMinAge, currentLine);
                parameters.SetMerchStemsMinAge(species, nMerchStemsMinAge.Value);

                ReadValue(dMerchCurveParmA, currentLine);
                parameters.SetMerchCurveParmA(species, dMerchCurveParmA.Value);
                ReadValue(dMerchCurveParmB, currentLine);
                parameters.SetMerchCurveParmB(species, dMerchCurveParmB.Value);

                ReadValue(dPropNonMerch, currentLine);
                parameters.SetPropNonMerch(species, dPropNonMerch.Value);

                ReadValue(growthCurveShapeParm, currentLine);
                parameters.SetGrowthCurveShape(species, growthCurveShapeParm.Value);

                ReadValue(shadeTolerance, currentLine);
                parameters.SetShadeTolerance(species, shadeTolerance.Value);

                ReadValue(fireTolerance, currentLine);
                parameters.SetFireTolerance(species, fireTolerance.Value);

                nread += 1;

                CheckNoDataAfter(lastColumn, currentLine);
                GetNextLine();
            }
            if (nread < speciesDataset.Count)
                throw NewParseException("SpeciesParameters: Data were only entered for {0} species!", nread);
                                              
            
            //--------- Read In Ecoregion Table ---------------------------
            //No longer reading in this table - instead just hard-wiring two variables: Latitude and FieldCapacity
            foreach (IEcoregion ecoregion in PlugIn.ModelCore.Ecoregions)
            {
                parameters.SetFieldCapacity(ecoregion, 49.0);
                parameters.SetLatitude(ecoregion, 0.4);
            }


            //---------------------------------------------------------------------
            /*
            InputVar<string> ageOnlyDisturbanceParms = new InputVar<string>(Names.AgeOnlyDisturbanceParms);
                
            ReadVar(ageOnlyDisturbanceParms);
            parameters.AgeOnlyDisturbanceParms = ageOnlyDisturbanceParms.Value;
            */

            //-------------------------
            //  DOM Decay Parameters
            ReadName(Names.DOMPools);
            InputVar<string> sDOMPool = new InputVar<string>("DOMPoolName");
            nread = 0;
            previousNumber = 0;
            while (!AtEndOfInput && (CurrentName != Names.EcoSppDOMParms))
            {
                currentLine = new StringReader(CurrentLine);

                ReadValue(nDOMPoolID, currentLine);
                ReadValue(sDOMPool, currentLine);
                ReadValue(dPropAir, currentLine);

                if (nDOMPoolID.Value.Actual != (byte)previousNumber + 1)
                    throw new InputValueException(nDOMPoolID.Value.String,
                                                  "DOMPools: Expected the pool ID {0}",
                                                  previousNumber + 1);

                parameters.SetDOMPool(nDOMPoolID.Value, sDOMPool.Value, 0, dPropAir.Value);

                previousNumber = nDOMPoolID.Value;
                nread += 1;
                CheckNoDataAfter(lastColumn, currentLine);
                GetNextLine();
            }
            if (nread < SoilClass.NUMSOILPOOLS)
                throw NewParseException("DOMPools: Parameters were not entered for all DOM pools!");

            //-------------------------
            //  Eco-Spp-DOM Pool Parameters
            ReadName(Names.EcoSppDOMParms);
            speciesLineNums.Clear();  //  If parser re-used (i.e., for testing purposes)

            InputVar<string> sEcoregion = new InputVar<string>("Ecoregion");
            InputVar<string> sSpecies = new InputVar<string>("Species");
            InputVar<double> dDecayRate = new InputVar<double>("Base Decay Rate");
            InputVar<double> dAmountT0 = new InputVar<double>("Initial Amount");
            InputVar<double> dQ10 = new InputVar<double>("Q10");

            lastColumn = "the " + dAmountT0.Name + " column";

            nread = 0;
            while (!AtEndOfInput && (CurrentName != Names.ForCSProportions))
            {
                currentLine = new StringReader(CurrentLine);

                ReadValue(sEcoregion, currentLine);
                IEcoregion ecoregion = GetEcoregion(sEcoregion.Value);

                ReadValue(sSpecies, currentLine);
                ISpecies species = GetSpecies(sSpecies.Value);

                ReadValue(nDOMPoolID, currentLine);
                if ((nDOMPoolID.Value <= 0) || (nDOMPoolID.Value > SoilClass.NUMSOILPOOLS))
                    throw new InputValueException("nDOMPoolID", "EcoSppDOMParamters: {0} is not a valid DOM pool ID.", nDOMPoolID.Value.Actual);
                
                ReadValue(dDecayRate, currentLine);
                ReadValue(dAmountT0, currentLine);
                ReadValue(dQ10, currentLine);

                // Convert from a 1-base to 0-base. (Don't need to do that because user enters the actual DOM pool)
                parameters.SetDOMDecayRate(ecoregion, species, nDOMPoolID.Value - 1, dDecayRate.Value);
                parameters.SetDOMPoolAmountT0(ecoregion, species, nDOMPoolID.Value - 1, dAmountT0.Value);
                parameters.SetDOMPoolQ10(ecoregion, species, nDOMPoolID.Value - 1, dQ10.Value);

                nread += 1;
                
                CheckNoDataAfter(lastColumn, currentLine);
                GetNextLine();
            }
            if (nread < neco * speciesDataset.Count * SoilClass.NUMSOILPOOLS)
            {
                int missrow = (neco * speciesDataset.Count * SoilClass.NUMSOILPOOLS - nread);
                throw new InputValueException(nDOMPoolID.Name, "{0} rows were missing from the EcoSppDOMParamters table.", missrow);
            }

            //-------------------------
            //  ForCSProportions
            ReadName(Names.ForCSProportions);
            currentLine = new StringReader(CurrentLine);
            
            InputVar<double> dPropBiomassFine = new InputVar<double>("Biomass Fine Roots");
            ReadValue(dPropBiomassFine, currentLine);
            parameters.SetPropBiomassFine(dPropBiomassFine.Value);
            
            InputVar<double> dPropBiomassCoarse = new InputVar<double>("Biomass Coarse Roots");
            ReadValue(dPropBiomassCoarse, currentLine);
            parameters.SetPropBiomassCoarse(dPropBiomassCoarse.Value);
            
            InputVar<double> dPropDOMSlowAGToSlowBG = new InputVar<double>("DOM SlowAG to SlowBG");
            ReadValue(dPropDOMSlowAGToSlowBG, currentLine);
            parameters.SetPropDOMSlowAGToSlowBG(dPropDOMSlowAGToSlowBG.Value);
            
            InputVar<double> dPropDOMStemSnagToMedium = new InputVar<double>("DOM Stem Snag to Medium");
            ReadValue(dPropDOMStemSnagToMedium, currentLine);
            parameters.SetPropDOMStemSnagToMedium(dPropDOMStemSnagToMedium.Value);

            InputVar<double> dPropDOMBranchSnagToFastAG = new InputVar<double>("DOM Branch Snag to FastAG");
            ReadValue(dPropDOMBranchSnagToFastAG, currentLine);
            parameters.SetPropDOMBranchSnagToFastAG(dPropDOMBranchSnagToFastAG.Value);

            GetNextLine();

            //-------------------------
            //  ANPPTimeSeries
            ReadName(Names.ANPPTimeSeries);
            InputVar<int> nYear = new InputVar<int>("Year");
            InputVar<double> dANPP = new InputVar<double>("ANPP grams per m2-year");
            InputVar<double> dStdDev = new InputVar<double>("ANPP Std Dev.");
            int firstYear = -999;
            nread = 0;
            while (!AtEndOfInput && (CurrentName != Names.MaxBiomassTimeSeries))
            {
                currentLine = new StringReader(CurrentLine);

                ReadValue(nYear, currentLine);
                //if (nYear.Value < 0)
                //    throw new InputValueException(nYear.Name, "ANPP: {0} is not a valid year.", nYear.ToString());

                if (firstYear == -999)
                {
                    if (nYear.Value > 0)
                        throw new InputValueException(nYear.Name, "The first year for ANPP must be <=0.");
                    firstYear = 1;
                }

                ReadValue(sEcoregion, currentLine);
                IEcoregion ecoregion = GetEcoregion(sEcoregion.Value);

                ReadValue(sSpecies, currentLine);
                ISpecies species = GetSpecies(sSpecies.Value);

                ReadValue(dANPP, currentLine);
                ReadValue(dStdDev, currentLine);

                // Comment out the 2 lines below if we don't care if multiple rows with the
                // same (ecoregion, species, year) tuple exist.
                if (parameters.ANPPTimeCollection[ecoregion][species].Contains(new ANPP(nYear.Value, 0.0, 0.0)))
                    throw new InputValueException(nYear.Name, "ANPP: Year {0} is already entered for the given ecoregion '{1}' and species '{2}'.", nYear.ToString(), ecoregion.Name, species.Name);

                // Create a ANPP object and add it to the time series.
                parameters.ANPPTimeCollection[ecoregion][species].Add(new ANPP(nYear.Value, dANPP.Value, dStdDev.Value));

                if (nYear.Value == 0)
                    nread += 1;

                CheckNoDataAfter(lastColumn, currentLine);
                GetNextLine();
            }

            if (firstYear == -999)
                throw new InputValueException(nYear.Name, "No data were entered for ANPP. You must enter at least year 0.");

            if (nread < neco * speciesDataset.Count)
                throw new InputValueException("ANPP", "ANPP values were not entered for year 0 for all species and ecoregions! Please check.");
                //PlugIn.ModelCore.UI.WriteLine("ANPP: ANPP values wre not entered for year 0 for all species and ecoregions! Please check.");

            //-------------------------
            //  Maximum Biomass TimeSeries
            ReadName(Names.MaxBiomassTimeSeries);
            InputVar<double> dMaxBiomass = new InputVar<double>("MaxBiomass grams per m2");
            firstYear = -999;
            nread = 0;
            while (!AtEndOfInput && (CurrentName != Names.EstablishProbabilities))
            {
                currentLine = new StringReader(CurrentLine);

                ReadValue(nYear, currentLine);

                if (firstYear == -999)
                {
                    if (nYear.Value > 0)
                        throw new InputValueException(nYear.Name, "The first year for MaxBiomass must be <=0.");
                    firstYear = 1;
                }

                ReadValue(sEcoregion, currentLine);
                IEcoregion ecoregion = GetEcoregion(sEcoregion.Value);

                ReadValue(sSpecies, currentLine);
                ISpecies species = GetSpecies(sSpecies.Value);

                ReadValue(dMaxBiomass, currentLine);

                // Create a MaxBiomass object and add it to the time series.

                parameters.MaxBiomassTimeCollection[ecoregion][species].Add(new MaxBiomass(nYear.Value, dMaxBiomass.Value, 0.0));

                if (nYear.Value == 0)
                    nread += 1;

                CheckNoDataAfter(lastColumn, currentLine);
                GetNextLine();
            }

            if (firstYear == -999)
                throw new InputValueException(nYear.Name, "No data were entered for MaxBiomass. You must enter at least year 0.");

            if (nread < neco * speciesDataset.Count)
                throw new InputValueException("MaxBiomass", "MaxBiomass values wre not entered for year 0 for all species and ecoregions! Please check.");

            //-------------------------
            //  EstablishProbabilities
            ReadName(Names.EstablishProbabilities);
            InputVar<double> dEstProb = new InputVar<double>("Establish Probability");
            nread = 0;
            while (!AtEndOfInput && (CurrentName != Names.RootDynamics))
            {
                currentLine = new StringReader(CurrentLine);

                ReadValue(nYear, currentLine);
                if (nYear.Value < 0)
                    throw new InputValueException(nYear.Name, "{0} is not a valid year.", nYear.ToString());

                ReadValue(sEcoregion, currentLine);
                IEcoregion ecoregion = GetEcoregion(sEcoregion.Value);

                ReadValue(sSpecies, currentLine);
                ISpecies species = GetSpecies(sSpecies.Value);

                ReadValue(dEstProb, currentLine);

                //if (parameters.EstabProbTimeCollection[ecoregion][species].Contains(new EstabProb(nYear.Value, 0.0)))
                //    throw new InputValueException(nYear.Name, "Year {0} is already entered for the given ecoregion '{1}' and species '{2}'.", nYear.ToString(), ecoregion.Name, species.Name);

                // Create an EstablishmentProbability object.
                //parameters.SetEstablishProbability(ecoregion, species, dEstProb.Value);
                parameters.EstabProbTimeCollection[ecoregion][species].Add(new EstabProb(nYear.Value, dEstProb.Value));

                nread += 1;
                CheckNoDataAfter(lastColumn, currentLine);
                GetNextLine();
            }
            if (nread < neco * speciesDataset.Count)
                throw new InputValueException("Establishment Probabilities", "EstablishmentProbabilities: Establishment probabilities were not entered for all species and ecoregions! Please check.");
            //PlugIn.ModelCore.UI.WriteLine("EstablishmentProbabilities: Establishment probabilities were not entered for all species and ecoregions! Please check.");


            //-------------------------
            //  Root Dynamics
            ReadName(Names.RootDynamics);
            InputVar<double> dMinWoody = new InputVar<double>("Min Woody Biomass");
            InputVar<double> dRatio = new InputVar<double>("Wood:Root Ratio");
            InputVar<double> dPropFine = new InputVar<double>("Prop Fine");
            InputVar<double> dFineTurnover = new InputVar<double>("Fine Turnover");
            InputVar<double> dCoarseTurnover = new InputVar<double>("Coarse Turnover");
            nread = 0;
            //MARCH TEMPORARY commentedt out while (!AtEndOfInput && (CurrentName != "No Section To Follow"))
            while (!AtEndOfInput && (CurrentName != "No Section To Follow" && CurrentName != Names.SnagData))
            {
                currentLine = new StringReader(CurrentLine);

                ReadValue(sEcoregion, currentLine);
                IEcoregion ecoregion = GetEcoregion(sEcoregion.Value);

                ReadValue(sSpecies, currentLine);
                ISpecies species = GetSpecies(sSpecies.Value);

                ReadValue(dMinWoody, currentLine);
                int idxval = parameters.SetMinWoodyBio(ecoregion, species, dMinWoody.Value);

                if (idxval > 0 && dMinWoody.Value == 0)
                    PlugIn.ModelCore.UI.WriteLine("Root Parameters: The MinBiomass=0 for an eco-spp combo was not entered first. Roots may not be calculated correctly");
                if (idxval == 0 && dMinWoody.Value > 0)
                    PlugIn.ModelCore.UI.WriteLine("Root Parameters: The first MinBiomass value entered for an eco-spp combo was not 0.  Roots may not be calculated correctly");

                ReadValue(dRatio, currentLine);
                parameters.SetRootRatio(ecoregion, species, dRatio.Value, idxval);
                
                ReadValue(dPropFine, currentLine);
                parameters.SetPropFine(ecoregion, species, dPropFine.Value, idxval);
                
                ReadValue(dFineTurnover, currentLine);
                parameters.SetFineTurnover(ecoregion, species, dFineTurnover.Value, idxval);
                
                ReadValue(dCoarseTurnover, currentLine);
                parameters.SetCoarseTurnover(ecoregion, species, dCoarseTurnover.Value, idxval);

                if (dMinWoody.Value == 0) //we only want to count those that have a min biomass=0;
                    nread += 1;
                CheckNoDataAfter(lastColumn, currentLine);
                GetNextLine();
            }
            if (nread < neco * speciesDataset.Count)
                throw new InputValueException("Root Dynamics", "Root Dynamics: Root parameters for a MinimumBiomass = 0 were not entered for all species and ecoregions! Please check.");


            
            /*
            //MARCH Read snag file
            //first try - assume the information is at the bottom of the main ForC file.

            InputVar<int> dAgeAtDeath = new InputVar<int>("Age At Death");
            InputVar<int> dTimeSinceDeath = new InputVar<int>("Time Since Death");

            ReadName(Names.SnagData);

            nread = 0;            
            while (!AtEndOfInput && (CurrentName != "No Section To Follow"))
            {
                currentLine = new StringReader(CurrentLine);
                ISpecies species = ReadSpecies(currentLine);

                ReadValue(dAgeAtDeath, currentLine);
                //parameters.SetAgeAtDeath(species, dAgeAtDeath.Value);

                ReadValue(dTimeSinceDeath, currentLine);
                //parameters.SetTimeSinceDeath(species, dTimeSinceDeath.Value);

                ReadValue(sDisturbType, currentLine);
                bool bOk = CheckDisturbanceType(sDisturbType);
                if (!bOk)
                    throw new InputValueException(sDisturbType.Name, "Initial Snag Data {0} is not a valid disturbance type. Check that name is all lowercase.", sDisturbType.Value.Actual);

                if (nread > 19)
                    throw new InputValueException(Names.SnagData, "Too many intial snag types were entered. The remaining ones will not be read");
                else
                    parameters.SetInitSnagInfo(species, dAgeAtDeath.Value, dTimeSinceDeath.Value, sDisturbType.Value, nread);

                nread++;
                GetNextLine();
            }
             */
          
            
            
            return parameters; 
        }

         /*protected void ReadDynamicTable(List<Dynamic.ParametersUpdate> parameterUpdates)
        {
            int? prevYear = null;
            int prevYearLineNum = 0;
            InputVar<int> year = new InputVar<int>("Year", Dynamic.InputValidation.ReadYear);
            InputVar<string> file = new InputVar<string>("Parameter File");
            while (! AtEndOfInput) {
                StringReader currentLine = new StringReader(CurrentLine);

                ReadValue(year, currentLine);
                if (prevYear.HasValue) {
                    if (year.Value.Actual < prevYear.Value)
                        throw new InputValueException(year.Value.String,
                                                      "Year {0} is before year {1} which was on line {2}",
                                                      year.Value.Actual, prevYear.Value, prevYearLineNum);
                    if (year.Value.Actual == prevYear.Value)
                        throw new InputValueException(year.Value.String,
                                                      "Year {0} was already used on line {1}",
                                                      year.Value.Actual, prevYearLineNum);
                }
                prevYear = year.Value.Actual;
                prevYearLineNum = LineNumber;

                ReadValue(file, currentLine);
                Dynamic.InputValidation.CheckPath(file.Value);

                CheckNoDataAfter("the " + file + " column", currentLine);
                parameterUpdates.Add(new Dynamic.ParametersUpdate(year.Value.Actual,
                                                                        file.Value.Actual));
                GetNextLine();
            }
        }*/
        //---------------------------------------------------------------------

        /// <summary>
        /// Reads a species name from the current line, and verifies the name.
        /// </summary>
        private ISpecies ReadSpecies(StringReader currentLine)
        {
            ReadValue(speciesName, currentLine);
            ISpecies species = speciesDataset[speciesName.Value.Actual];
            if (species == null)
                throw new InputValueException(speciesName.Value.String,
                                              "{0} is not a species name.",
                                              speciesName.Value.String);
            int lineNumber;
            if (speciesLineNums.TryGetValue(species.Name, out lineNumber))
                throw new InputValueException(speciesName.Value.String,
                                              "The species {0} was previously used on line {1}",
                                              speciesName.Value.String, lineNumber);
            else
                speciesLineNums[species.Name] = LineNumber;
            return species;
        }
        //---------------------------------------------------------------------

        private ISpecies GetSpecies(InputValue<string> sName)
        {
            ISpecies species = speciesDataset[sName.Actual];
            if (species == null)
                throw new InputValueException(sName.String, "{0} is not an species name.", sName.String);

            return species;
        }
        //---------------------------------------------------------------------


        /// <summary>
        /// Reads an ecoregion name from the current line, and verifies the name.
        /// </summary>
        private IEcoregion ReadEcoregion(StringReader currentLine)
        {
            InputVar<string> ecoregionName = new InputVar<string>("Ecoregion"); ;
            ReadValue(ecoregionName, currentLine);
            IEcoregion ecoregion = ecoregionDataset[ecoregionName.Value.Actual];
            ISpecies species = speciesDataset[speciesName.Value.Actual];
            if (ecoregion == null)
                throw new InputValueException(ecoregionName.Value.String, "{0} is not an ecoregion name.", ecoregionName.Value.String);

            int lineNumber;
            if (speciesLineNums.TryGetValue(ecoregion.Name, out lineNumber))
                throw new InputValueException(ecoregionName.Value.String, "The ecoregion {0} was previously used on line {1}", ecoregionName.Value.String, lineNumber);
            else
                speciesLineNums[ecoregion.Name] = LineNumber;
            return ecoregion;
        }
        //---------------------------------------------------------------------

        /// <summary>
        /// Reads ecoregion names as column headings
        /// </summary>
        private List<IEcoregion> ReadEcoregions()
        {
            if (AtEndOfInput)
                throw NewParseException("Expected a line with the names of 1 or more active ecoregions.");

            InputVar<string> ecoregionName = new InputVar<string>("Ecoregion");
            List<IEcoregion> ecoregions = new List<IEcoregion>();
            StringReader currentLine = new StringReader(CurrentLine);
            TextReader.SkipWhitespace(currentLine);
            while (currentLine.Peek() != -1) {
                ReadValue(ecoregionName, currentLine);
                IEcoregion ecoregion = ecoregionDataset[ecoregionName.Value.Actual];
                if (ecoregion == null)
                    throw new InputValueException(ecoregionName.Value.String,
                                                  "{0} is not an ecoregion name.",
                                                  ecoregionName.Value.String);
                if (! ecoregion.Active)
                    throw new InputValueException(ecoregionName.Value.String,
                                                  "{0} is not an active ecoregion",
                                                  ecoregionName.Value.String);
                if (ecoregions.Contains(ecoregion))
                    throw new InputValueException(ecoregionName.Value.String,
                                                  "The ecoregion {0} appears more than once.",
                                                  ecoregionName.Value.String);
                ecoregions.Add(ecoregion);
                TextReader.SkipWhitespace(currentLine);
            }
            GetNextLine();

            return ecoregions;
        }

        //---------------------------------------------------------------------

        private IEcoregion GetEcoregion(InputValue<string>      ecoregionName,
                                        Dictionary<string, int> lineNumbers)
        {
            IEcoregion ecoregion = ecoregionDataset[ecoregionName.Actual];
            if (ecoregion == null)
                throw new InputValueException(ecoregionName.String,
                                              "{0} is not an ecoregion name.",
                                              ecoregionName.String);
            int lineNumber;
            if (lineNumbers.TryGetValue(ecoregion.Name, out lineNumber))
                throw new InputValueException(ecoregionName.String,
                                              "The ecoregion {0} was previously used on line {1}",
                                              ecoregionName.String, lineNumber);
            else
                lineNumbers[ecoregion.Name] = LineNumber;

            return ecoregion;
        }
        //---------------------------------------------------------------------

        private IEcoregion GetEcoregion(InputValue<string> ecoregionName)
        {
            IEcoregion ecoregion = ecoregionDataset[ecoregionName.Actual];
            if (ecoregion == null)
                throw new InputValueException(ecoregionName.String, "{0} is not an ecoregion name.", ecoregionName.String);

            return ecoregion;
        }
        //---------------------------------------------------------------------

        /// <summary>
        /// Reads a table for a species parameter that varies by ecoregion.
        /// </summary>
        private void ReadSpeciesEcoregionParm<TParm>(string               tableName,
                                                     SetParmMethod<TParm> setParmMethod,
                                                     params string[]      namesThatFollow)
        {
            ReadName(tableName);
            List<IEcoregion> ecoregions = ReadEcoregions();
            string lastEcoregion = ecoregions[ecoregions.Count-1].Name;

            List<string> namesAfterTable;
            if (namesThatFollow == null)
                namesAfterTable = new List<string>();
            else
                namesAfterTable = new List<string>(namesThatFollow);

            speciesLineNums.Clear();
            while (! AtEndOfInput && ! namesAfterTable.Contains(CurrentName)) {
                StringReader currentLine = new StringReader(CurrentLine);
                ISpecies species = ReadSpecies(currentLine);

                foreach (IEcoregion ecoregion in ecoregions) {
                    InputVar<TParm> parameter = new InputVar<TParm>("Ecoregion " + ecoregion.Name);
                    ReadValue(parameter, currentLine);
                    setParmMethod(species, ecoregion, parameter.Value);
                }

                CheckNoDataAfter("the Ecoregion " + lastEcoregion + " column",
                                 currentLine);
                GetNextLine();
            }
        }
        //---------------------------------------------------------------------
 
        private bool CheckDisturbanceType(InputVar<string> sName)
        {
            string[] DistType = new string[] { "fire", "harvest", "wind", "bda", "drought", "other", "land use" };
            int i = 0;
            for (i = DistType.GetUpperBound(0); i > 0; i--)
            {
                if (sName.Value == DistType[i])
                    break;
            }
            if (i == 0)
            {
                return (false);
            }
            else
                return (true);

        }

    }
}
