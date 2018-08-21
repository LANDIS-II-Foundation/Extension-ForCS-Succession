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
        public Ecoregions.AuxParm<ITimeCollection<IClimate>> ClimateAnnualCollection { get; }

    }

        /// </summary>
    public class InputClimateParms
        : IInputClimateParms
    {
 
        public Ecoregions.AuxParm<ITimeCollection<IClimate>> ClimateAnnualCollection { get { return m_ClimateAnnualCollection; } }

        //---------------------------------------------------------------------

    }

}
