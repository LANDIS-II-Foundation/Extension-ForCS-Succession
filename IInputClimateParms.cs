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
    /// The parameters for ForC Cliamte initialization.
    /// </summary>
    public interface IInputClimateParms
    {
        Ecoregions.AuxParm<ITimeCollection<IClimate>> ClimateAnnualCollection { get; }

    }

        /// </summary>
    public class InputClimateParms
        : IInputClimateParms
    {
        private Ecoregions.AuxParm<ITimeCollection<IClimate>> m_ClimateAnnualCollection; 
        private IEcoregionDataset m_dsEcoregion;

        public InputClimateParms()
        {
            this.m_dsEcoregion = PlugIn.ModelCore.Ecoregions;
            this.m_ClimateAnnualCollection = new Ecoregions.AuxParm<ITimeCollection<IClimate>>(m_dsEcoregion);
            foreach (IEcoregion ecoregion in m_dsEcoregion)
            {
                this.m_ClimateAnnualCollection[ecoregion] = new TimeCollection<IClimate>();
            }

         }
        public Ecoregions.AuxParm<ITimeCollection<IClimate>> ClimateAnnualCollection { get { return m_ClimateAnnualCollection; } }

        //---------------------------------------------------------------------

    }

}
