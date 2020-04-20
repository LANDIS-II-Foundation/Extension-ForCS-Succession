//  Authors:  Caren Dymond, Sarah Beukema

using Landis.Library.Succession;
using Landis.Core;
using Landis.SpatialModeling;
using System.Collections.Generic;
using Landis.Utilities;

namespace Landis.Extension.Succession.ForC
{
    /// <summary>
    /// A parser that reads ForCS specific climate parameters from text input.
    /// </summary>
    public class InputClimateParser
        : Landis.Utilities.TextParser<IInputClimateParms>
    {
        public static class Names
        {
            public const string ClimateTable = "ClimateTable";
        }

        private delegate void SetParmMethod<TParm>(ISpecies          species,
                                                   IEcoregion        ecoregion,
                                                   InputValue<TParm> newValue);

        //---------------------------------------------------------------------

        private IEcoregionDataset ecoregionDataset;

        static InputClimateParser()
        {
            Percentage dummy = new Percentage();

        }

        //---------------------------------------------------------------------

        public InputClimateParser()
        {
           this.ecoregionDataset = PlugIn.ModelCore.Ecoregions;
        }

        //---------------------------------------------------------------------

        protected override IInputClimateParms Parse()
        {
            InputVar<string> landisData = new InputVar<string>("LandisData");
            ReadVar(landisData);
            if (landisData.Value.Actual != PlugIn.ExtensionName)
                throw new InputValueException(landisData.Value.String, "The value is not \"{0}\"", PlugIn.ExtensionName);

            StringReader currentLine;
            Dictionary<string, int> lineNumbers = new Dictionary<string, int>();
           
            InputVar<int> nYear = new InputVar<int>("Time Step");
            InputVar<string> sEcoregion = new InputVar<string>("Eco");
            InputVar<double> dAvgT = new InputVar<double>("AvgT");
            InputClimateParms parameters = new InputClimateParms();  

            ReadName(Names.ClimateTable);

            int nread = 0;            
            while (!AtEndOfInput && (CurrentName != "No Section To Follow"))
            {
                currentLine = new StringReader(CurrentLine);

                ReadValue(nYear, currentLine);
                if (nYear.Value < 0)
                    throw new InputValueException(nYear.Name, "{0} is not a valid year.", nYear.ToString());

                ReadValue(sEcoregion, currentLine);
                IEcoregion ecoregion = GetEcoregion(sEcoregion.Value);

                ReadValue(dAvgT, currentLine);

                // Create a ClimateTable object.
                parameters.ClimateAnnualCollection[ecoregion].Add(new ClimateAnnual(nYear.Value, dAvgT.Value));

                nread += 1;
                GetNextLine();
            }
        
            return parameters; 
        }

        //---------------------------------------------------------------------
        private IEcoregion GetEcoregion(InputValue<string> ecoregionName)
        {
            IEcoregion ecoregion = ecoregionDataset[ecoregionName.Actual];
            if (ecoregion == null)
                throw new InputValueException(ecoregionName.String, "{0} is not an ecoregion name.", ecoregionName.String);

            return ecoregion;
        }

    }
}
