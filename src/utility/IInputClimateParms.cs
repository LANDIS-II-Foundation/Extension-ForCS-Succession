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
    /// The parameters for ForC Cliamte initialization.
    /// </summary>
    public interface IInputClimateParms
    {
        Landis.Library.Parameters.Ecoregions.AuxParm<ITimeCollection<IClimate>> ClimateAnnualCollection { get; }

    }

        /// </summary>
    public class InputClimateParms
        : IInputClimateParms
    {
        private Landis.Library.Parameters.Ecoregions.AuxParm<ITimeCollection<IClimate>> m_ClimateAnnualCollection; 
        private IEcoregionDataset m_dsEcoregion;

        public InputClimateParms()
        {
            this.m_dsEcoregion = PlugIn.ModelCore.Ecoregions;
            this.m_ClimateAnnualCollection = new Landis.Library.Parameters.Ecoregions.AuxParm<ITimeCollection<IClimate>>(m_dsEcoregion);
            foreach (IEcoregion ecoregion in m_dsEcoregion)
            {
                this.m_ClimateAnnualCollection[ecoregion] = new TimeCollection<IClimate>();
            }

         }
        public Landis.Library.Parameters.Ecoregions.AuxParm<ITimeCollection<IClimate>> ClimateAnnualCollection { get { return m_ClimateAnnualCollection; } }

        //---------------------------------------------------------------------

    }

}
