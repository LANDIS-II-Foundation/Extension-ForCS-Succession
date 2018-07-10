using System;
using System.Collections.Generic;
using System.Text;

//Note that this routine contains classes for both ANPP and MaxBiomass 
namespace Landis.Extension.Succession.ForC
{
    public interface IANPP : ITimeInput
    {
        double GramsPerMetre2Year { get; }

        double StdDev { get; }
    }

    // http://www.exforsys.com/tutorials/csharp/inheritance-in-csharp/1.html
    public class ANPP : TimeInput, IANPP
    {
        double m_dGramsPerMetre2Year = 0.0;
        double m_dStdDev = 0.0;

        /// <summary>
        /// Default ctor
        /// </summary>
        public ANPP()
        {
        }

        public ANPP(int nYear, double dGramsPerMetre2Year, double dStdDev)
        {
            this.Year = nYear;
            this.GramsPerMetre2Year = dGramsPerMetre2Year;
            this.StdDev = dStdDev;
        }

        public double GramsPerMetre2Year
        {
            get
            {
                return m_dGramsPerMetre2Year;
            }
            set
            {
                if (value < 0.0)
                    throw new Edu.Wisc.Forest.Flel.Util.InputValueException(value.ToString(), "Grams / m2-year must be >= 0.  The value provided is = {0}.", value);
                m_dGramsPerMetre2Year = value;
            }
        }

        public double StdDev
        {
            get
            {
                return m_dStdDev;
            }
            set
            {
                if (value < 0.0)
                    throw new Edu.Wisc.Forest.Flel.Util.InputValueException(value.ToString(), "Std deviation must be >= 0.  The value provided is = {0}.", value);
                m_dStdDev = value;
            }
        }

    }
    /****************************************************************************
     * Maximum Biomass Information
     *      -directly copied from ANPP stuff above
     * *************************************************************************/
    public interface IMaxBiomass : ITimeInput
    {
        double MaxBio { get; }

        //double StdDev { get; }
    }

    public class MaxBiomass : TimeInput, IMaxBiomass
    {
        double m_dMaxBiomass = 0.0;
        //double m_dStdDev = 0.0;

        /// <summary>
        /// Default ctor
        /// </summary>
        public MaxBiomass()
        {
        }

        public MaxBiomass(int nYear, double dMaxBiomass, double dStdDev)
        {
            this.Year = nYear;
            this.MaxBio = dMaxBiomass;
            //this.StdDev = dStdDev;
        }

        public double MaxBio
        {
            get
            {
                return m_dMaxBiomass;
            }
            set
            {
                if (value < 0.0)
                    throw new Edu.Wisc.Forest.Flel.Util.InputValueException(value.ToString(), "Maximum biomass must be >= 0.  The value provided is = {0}.", value);
                m_dMaxBiomass = value;
            }
        }

        /*public double StdDev
        {
            get
            {
                return m_dStdDev;
            }
            set
            {
                if (value < 0.0)
                    throw new Edu.Wisc.Forest.Flel.Util.InputValueException(value.ToString(), "Std deviation must be >= 0.  The value provided is = {0}.", value);
                m_dStdDev = value;
            }
        }*/
    }
    /****************************************************************************
     * Establishment Probability Information
     *      -directly copied from ANPP stuff above
     * *************************************************************************/
    public interface IEstabProb: ITimeInput
    {
        double Establishment { get; }
    }

    public class EstabProb: TimeInput, IEstabProb
    {
        double m_dEstabProb = 0.0;

        /// <summary>
        /// Default ctor
        /// </summary>
        public EstabProb()
        {
        }

        public EstabProb(int nYear, double dEstabProb)
        {
            this.Year = nYear;
            this.Establishment = dEstabProb;
        }

        public double Establishment
        {
            get
            {
                return m_dEstabProb;
            }
            set
            {
                if (value < 0.0)
                    throw new Edu.Wisc.Forest.Flel.Util.InputValueException(value.ToString(), "Establishment Probability must be >= 0.  The value provided is = {0}.", value);
                m_dEstabProb = value;
            }
        }

    }

    /****************************************************************************
     * Climate Annual Temperature
     *      -directly copied from ANPP stuff above
     * *************************************************************************/
    public interface IClimate : ITimeInput
    {
        double ClimateAnnualTemp { get; }
    }

    public class ClimateAnnual : TimeInput, IClimate
    {
        double m_dMeanAnnualTemp = 0.0;

        /// <summary>
        /// Default ctor
        /// </summary>
        public ClimateAnnual()
        {
        }

        public ClimateAnnual(int nYear, double dMeanAnnualTemp)
        {
            this.Year = nYear;
            this.ClimateAnnualTemp = dMeanAnnualTemp;
        }

        public double ClimateAnnualTemp
        {
            get
            {
                return m_dMeanAnnualTemp;
            }
            set
            {
                m_dMeanAnnualTemp = value;
            }
        }

    }

}
