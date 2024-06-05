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
    public class InputDMParser
        : Landis.Utilities.TextParser<IInputDMParameters>
    {
        public static class Names
        {
            public const string DisturbFireTransferDOM = "DisturbFireTransferDOM";
            public const string DisturbOtherTransferDOM = "DisturbOtherTransferDOM";
            public const string DisturbFireTransferBiomass = "DisturbFireTransferBiomass";
            public const string DisturbOtherTransferBiomass = "DisturbOtherTransferBiomass";
        }

        private delegate void SetParmMethod<TParm>(ISpecies          species,
                                                   IEcoregion        ecoregion,
                                                   InputValue<TParm> newValue);

        //---------------------------------------------------------------------
        static InputDMParser()
        {
        }

        //---------------------------------------------------------------------

        public InputDMParser()
        {
        }

        //---------------------------------------------------------------------

        protected override IInputDMParameters Parse()
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
            DisturbTransferFromPools[] aDisturbTransferPools;
            Dictionary<string, IDisturbTransferFromPools> dictDisturbTransfer;
            InputVar<string> sDisturbType = new InputVar<string>("Disturbance Type");
            InputVar<int> nIntensity = new InputVar<int>("Intensity");
            InputVar<int> nBiomassPoolID = new InputVar<int>("Biomass Pool ID");
            int nread = 0;
           
            InputDMParameters parameters = new InputDMParameters();

            PlugIn.ModelCore.UI.WriteLine("Reading new DM input file.");

            //First set-up the DOM pools (This used to be done already, but we will do it manually now
            for (int n=1; n<11; n++)
            {
                parameters.SetDOMPool(n, "not specified");
            }
            //-------------------------
            //  DisturbFireTransferDOM Parameters
            ReadName(Names.DisturbFireTransferDOM);
            aDisturbTransferPools = new DisturbTransferFromPools[FireEffects.mk_nIntensityCount];
            for (int n = 0; n < FireEffects.mk_nIntensityCount; n++)
            {
                aDisturbTransferPools[n] = new DisturbTransferFromPools(DisturbTransferFromPool.mk_sDisturbTypeFire);
                aDisturbTransferPools[n].InitializeDOMPools(parameters.DOMPools);
            }

            string lastColumn = "the " + dPropFPS.Name + " column";
            nread = 0;
            while (!AtEndOfInput && (CurrentName != Names.DisturbOtherTransferDOM))
            {
                currentLine = new StringReader(CurrentLine);

                ReadValue(nIntensity, currentLine);
                // Validate input, as Intensity value is 1-based in range [1, FireEffects.mk_nIntensityCount]/
                if ((nIntensity.Value < 1) || (nIntensity.Value > FireEffects.mk_nIntensityCount))
                    throw new InputValueException(nIntensity.Name, "DisturbFireTransferDOM: {0} is not a valid Intensity value.", nIntensity.Value.Actual);

                ReadValue(nDOMPoolID, currentLine);
                if ((nDOMPoolID.Value < 1) || (nDOMPoolID.Value > SoilClass.NUMSOILPOOLS))
                    throw new InputValueException(nDOMPoolID.Name, "DisturbFireTransferDOM: {0} is not a valid DOM pool ID.", nDOMPoolID.Value.Actual);

                // Convert Intensity from 1-based in input file to 0-based simple array.
                DisturbTransferFromPool oDisturbTransfer = (DisturbTransferFromPool)((aDisturbTransferPools[nIntensity.Value - 1]).GetDisturbTransfer(nDOMPoolID.Value));

                ReadValue(dPropAir, currentLine);
                oDisturbTransfer.PropToAir = dPropAir.Value;

                ReadValue(dPropDOM, currentLine);
                oDisturbTransfer.PropToDOM = dPropDOM.Value;

                ReadValue(dPropFPS, currentLine);
                oDisturbTransfer.PropToFPS = dPropFPS.Value;

                if ((nDOMPoolID.Value == 2 || nDOMPoolID.Value == 4 || nDOMPoolID.Value == 7) && dPropFPS.Value > 0)
                    PlugIn.ModelCore.UI.WriteLine("DisturbFireTransferDOM: You have asked for belowground DOM pools to be transfered to the FPS after a fire. Please check.");

                if ((dPropAir.Value + dPropDOM.Value + dPropFPS.Value) > 1.0)
                    PlugIn.ModelCore.UI.WriteLine("DisturbFireTransferDOM: Proportions must not be greater than 1.");

                nread += 1;
                CheckNoDataAfter(lastColumn, currentLine);
                GetNextLine();
            }
            parameters.SetDisturbFireFromDOMPools(aDisturbTransferPools);

            if (nread < FireEffects.mk_nIntensityCount * (SoilClass.NUMSOILPOOLS - 1))
                PlugIn.ModelCore.UI.WriteLine("DisturbFireTransferDOM: Some rows are missing. C in these DOM pools will not be affected by the fire.");

            //-------------------------
            //  DisturbOtherTransferDOM Parameters
            ReadName(Names.DisturbOtherTransferDOM);
            dictDisturbTransfer = new Dictionary<string, IDisturbTransferFromPools>();

            lastColumn = "the " + dPropFPS.Name + " column";
            nread = 0;
            while (!AtEndOfInput && (CurrentName != Names.DisturbFireTransferBiomass))
            {
                currentLine = new StringReader(CurrentLine);

                ReadValue(sDisturbType, currentLine);
                //bool bOk = CheckDisturbanceType(sDisturbType);
                //if (!bOk)
                //    throw new InputValueException(sDisturbType.Name, "DisturbOtherTransferDOM: {0} is not a valid disturbance type. Check that the name is all lowercase", sDisturbType.Value.Actual);

                DisturbTransferFromPools oDisturbTransferPools;
                if (dictDisturbTransfer.ContainsKey(sDisturbType.Value))
                {
                    oDisturbTransferPools = (DisturbTransferFromPools)dictDisturbTransfer[sDisturbType.Value];
                    //PlugIn.ModelCore.UI.WriteLine("Input new key Dist={0}.", sDisturbType.Value);
                }
                else
                {
                    oDisturbTransferPools = new DisturbTransferFromPools(sDisturbType.Value);
                    oDisturbTransferPools.InitializeDOMPools(parameters.DOMPools);
                    dictDisturbTransfer.Add(sDisturbType.Value, oDisturbTransferPools);
                }

                ReadValue(nDOMPoolID, currentLine);
                if ((nDOMPoolID.Value < 1) || (nDOMPoolID.Value > SoilClass.NUMSOILPOOLS))
                    throw new InputValueException(nDOMPoolID.Name, "DisturbOtherTransferDOM: {0} is not a valid DOM pool ID.", nDOMPoolID.Value.Actual);

                DisturbTransferFromPool oDisturbTransfer = (DisturbTransferFromPool)oDisturbTransferPools.GetDisturbTransfer(nDOMPoolID.Value);

                ReadValue(dPropAir, currentLine);
                oDisturbTransfer.PropToAir = dPropAir.Value;

                ReadValue(dPropDOM, currentLine);
                oDisturbTransfer.PropToDOM = dPropDOM.Value;

                ReadValue(dPropFPS, currentLine);
                oDisturbTransfer.PropToFPS = dPropFPS.Value;

                if ((nDOMPoolID.Value == 2 || nDOMPoolID.Value == 4 || nDOMPoolID.Value == 7) && dPropFPS.Value > 0)
                    PlugIn.ModelCore.UI.WriteLine("DisturbOtherTransferDOM: You have asked for belowground DOM pools to be transfered to the FPS after a disturbance. Please check.");

                if ((dPropAir.Value + dPropDOM.Value + dPropFPS.Value) > 1.0)
                    PlugIn.ModelCore.UI.WriteLine("DisturbOtherTransferDOM: Proportions must not be greater than 1.");

                nread += 1;
                CheckNoDataAfter(lastColumn, currentLine);
                GetNextLine();
            }
            parameters.SetDisturbOtherFromDOMPools(dictDisturbTransfer);

            if (nread < 3 * (SoilClass.NUMSOILPOOLS - 1))
                PlugIn.ModelCore.UI.WriteLine("DisturbOtherFromDOMPools: Some rows are missing. C in these DOM pools will not be affected by the disturbance.");

            //-------------------------
            //  DisturbFireTransferBiomass Parameters
            ReadName(Names.DisturbFireTransferBiomass);
            aDisturbTransferPools = new DisturbTransferFromPools[FireEffects.mk_nIntensityCount];
            for (int n = 0; n < FireEffects.mk_nIntensityCount; n++)
            {
                aDisturbTransferPools[n] = new DisturbTransferFromPools(DisturbTransferFromPool.mk_sDisturbTypeFire);
                aDisturbTransferPools[n].InitializeBiomassPools();
            }

            lastColumn = "the " + dPropFPS.Name + " column";
            nread = 0;
            while (!AtEndOfInput && (CurrentName != Names.DisturbOtherTransferBiomass))
            {
                currentLine = new StringReader(CurrentLine);

                ReadValue(nIntensity, currentLine);
                // Validate input, as Intensity value is 1-based in range [1, FireEffects.mk_nIntensityCount]/
                if ((nIntensity.Value < 1) || (nIntensity.Value > FireEffects.mk_nIntensityCount))
                    throw new InputValueException(nIntensity.Name, "DisturbFireTransferBiomass: {0} is not a valid Intensity value.", nIntensity.Value.Actual);

                ReadValue(nBiomassPoolID, currentLine);
                if ((nBiomassPoolID.Value < 1) || (nBiomassPoolID.Value > SoilClass.NUMBIOMASSCOMPONENTS))
                    throw new InputValueException(nBiomassPoolID.Name, "DisturbFireTransferBiomass: {0} is not a valid biomass pool ID.", nBiomassPoolID.Value.Actual);

                // Convert Intensity from 1-based in input file to 0-based simple array.
                DisturbTransferFromPool oDisturbTransfer = (DisturbTransferFromPool)((aDisturbTransferPools[nIntensity.Value - 1]).GetDisturbTransfer(nBiomassPoolID.Value));

                ReadValue(dPropAir, currentLine);
                oDisturbTransfer.PropToAir = dPropAir.Value;

                ReadValue(dPropFPS, currentLine);
                oDisturbTransfer.PropToFPS = dPropFPS.Value;

                if (dPropFPS.Value > 0)
                    PlugIn.ModelCore.UI.WriteLine("DisturbFireTransferBiomass: Warning: you have asked for C to go to the FPS after a fire. Is this correct?");

                ReadValue(dPropDOM, currentLine);
                oDisturbTransfer.PropToDOM = dPropDOM.Value;

                if ((dPropAir.Value + dPropDOM.Value + dPropFPS.Value) != 1.0)
                    PlugIn.ModelCore.UI.WriteLine("DisturbFireTransferBiomass: Proportions must add to 1.");

                nread += 1;

                CheckNoDataAfter(lastColumn, currentLine);
                GetNextLine();
            }
            parameters.SetDisturbFireFromBiomassPools(aDisturbTransferPools);

            if (nread < FireEffects.mk_nIntensityCount * (SoilClass.NUMBIOMASSCOMPONENTS - 1))
                PlugIn.ModelCore.UI.WriteLine("DisturbFireTransferBiomass: Some combinations of Fire Intensity and biomass type are missing. When these biomass components are killed, C loss will not be captured.");

            //-------------------------
            //  DisturbOtherTransferBiomass Parameters
            ReadName(Names.DisturbOtherTransferBiomass);
            dictDisturbTransfer = new Dictionary<string, IDisturbTransferFromPools>();

            lastColumn = "the " + dPropFPS.Name + " column";
            nread = 0;
            while (!AtEndOfInput && (CurrentName != "No Section To Follow"))
            {
                currentLine = new StringReader(CurrentLine);

                ReadValue(sDisturbType, currentLine);

                //bool bOk = CheckDisturbanceType(sDisturbType);
                //if (!bOk)
                //    throw new InputValueException(sDisturbType.Name, "DisturbOtherTransferBiomass: {0} is not a valid disturbance type. Check that name is all lowercase.", sDisturbType.Value.Actual);

                DisturbTransferFromPools oDisturbTransferPools;
                if (dictDisturbTransfer.ContainsKey(sDisturbType.Value))
                    oDisturbTransferPools = (DisturbTransferFromPools)dictDisturbTransfer[sDisturbType.Value];
                else
                {
                    oDisturbTransferPools = new DisturbTransferFromPools(sDisturbType.Value);
                    oDisturbTransferPools.InitializeBiomassPools();
                    dictDisturbTransfer.Add(sDisturbType.Value, oDisturbTransferPools);
                }

                ReadValue(nBiomassPoolID, currentLine);
                if ((nBiomassPoolID.Value < 1) || (nBiomassPoolID.Value > SoilClass.NUMSOILPOOLS))
                    throw new InputValueException(nBiomassPoolID.Name, "DisturbOtherTransferBiomass: {0} is not a valid biomass pool ID.", nBiomassPoolID.ToString());

                DisturbTransferFromPool oDisturbTransfer = (DisturbTransferFromPool)oDisturbTransferPools.GetDisturbTransfer(nBiomassPoolID.Value);

                ReadValue(dPropAir, currentLine);
                oDisturbTransfer.PropToAir = dPropAir.Value;

                ReadValue(dPropFPS, currentLine);
                oDisturbTransfer.PropToFPS = dPropFPS.Value;

                if (nBiomassPoolID.Value >= 5 && dPropFPS.Value > 0)
                    PlugIn.ModelCore.UI.WriteLine("DisturbOtherTransferBiomass: Warning: you have asked for root C to go to the FPS. Is this correct?");

                ReadValue(dPropDOM, currentLine);
                oDisturbTransfer.PropToDOM = dPropDOM.Value;

                if ((dPropAir.Value + dPropDOM.Value + dPropFPS.Value) != 1.0)
                    PlugIn.ModelCore.UI.WriteLine("DisturbOtherTransferBiomass: Proportions must add to 1.");

                nread += 1;
                CheckNoDataAfter(lastColumn, currentLine);
                GetNextLine();
            }
            parameters.SetDisturbOtherFromBiomassPools(dictDisturbTransfer);

            if (nread < 3 * (SoilClass.NUMBIOMASSCOMPONENTS - 1))
                PlugIn.ModelCore.UI.WriteLine("DisturbOtherTransferBiomass: Some biomass components are missing. When these biomass components are killed, C loss will not be captured.");

            return parameters; 
        }

 
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
