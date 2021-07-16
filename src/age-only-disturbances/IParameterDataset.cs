//  Copyright 2009 Conservation Biology Institute
//  Authors:  Robert M. Scheller
//  License:  Available at  
//  http://www.landis-ii.org/developers/LANDIS-IISourceCodeLicenseAgreement.pdf

using Landis.Core;

namespace Landis.Extension.Succession.ForC.AgeOnlyDisturbances
{
    /// <summary>
    /// A collection of biomass parameters for age-only disturbances.
    /// </summary>
    public interface IParameterDataset
    {
        /// <summary>
        /// A table of percentages that a disturbance reduce a cohort's
        /// inputs to the dead pools.
        /// </summary>
        IPercentageTable CohortReductions
        {
            get;set;
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// A table of percentages that a disturbances reduces a site's dead
        /// pools.
        /// </summary>
        IPercentageTable PoolReductions
        {
            get;set;
        }
    }
}
