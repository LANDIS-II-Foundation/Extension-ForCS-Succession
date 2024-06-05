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
    public class InputSnagParser
        : Landis.Utilities.TextParser<IInputSnagParms>
    {
        public static class Names
        {
            public const string SnagData = "SnagData";
        }

        private delegate void SetParmMethod<TParm>(ISpecies          species,
                                                   IEcoregion        ecoregion,
                                                   InputValue<TParm> newValue);

        //---------------------------------------------------------------------

        //private IEcoregionDataset ecoregionDataset;
        private ISpeciesDataset speciesDataset;
        private Dictionary<string, int> speciesLineNums;
        private InputVar<string> speciesName;

        static InputSnagParser()
        {
            Percentage dummy = new Percentage();

        }

        //---------------------------------------------------------------------

        public InputSnagParser()
        {
            this.speciesDataset = PlugIn.ModelCore.Species;
            this.speciesLineNums = new Dictionary<string, int>();
            this.speciesName = new InputVar<string>("Species");
        }

        //---------------------------------------------------------------------

        protected override IInputSnagParms Parse()
        {
            InputVar<string> landisData = new InputVar<string>("LandisData");
            ReadVar(landisData);
            if (landisData.Value.Actual != PlugIn.ExtensionName)
                throw new InputValueException(landisData.Value.String, "The value is not \"{0}\"", PlugIn.ExtensionName);

            StringReader currentLine;
            Dictionary<string, int> lineNumbers = new Dictionary<string, int>();
           
            InputVar<int> dAgeAtDeath = new InputVar<int>("Age At Death");
            InputVar<int> dTimeSinceDeath = new InputVar<int>("Time Since Death");
            InputVar<string> sDisturbType = new InputVar<string>("Disturbance Type");
            InputSnagParms parameters = new InputSnagParms();  

            ReadName(Names.SnagData);

            int nread = 0;            
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
            
            return parameters; 
        }



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
            //int lineNumber;
            //if (speciesLineNums.TryGetValue(species.Name, out lineNumber))
            //    throw new InputValueException(speciesName.Value.String,
            //                                  "The species {0} was previously used on line {1}",
            //                                  speciesName.Value.String, lineNumber);
            //else
            //    speciesLineNums[species.Name] = LineNumber;
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
