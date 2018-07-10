using System;
using System.Collections.Generic;
using System.Text;

namespace Landis.Extension.Succession.ForC
{
    /// <summary>
    /// eDOMPoolIDs - IDs used to index directly into the calculations.
    /// Note that unlike Soils.SoilPoolType, these are 1-based.
    /// </summary>
    public enum eDOMPoolIDs
    {
        VeryFastAG = 1,
        VeryFastBG,
        FastAG,
        FastBG,
        Medium,
        SlowAG,
        SlowBG,
        SoftStemSnag,
        SoftBranchSnag,
        BlackCarbon
    };

    public interface IDOMPool
    {
        int ID { get; }

        string Name { get; }

        double Q10 { get; }

        double PropAir { get; }
    }

    public class DOMPool : IDOMPool
    {
        private int m_nID;
        private string m_sName;
        private double m_dQ10 = 0.0;
        private double m_dPropAir = 0.0;

        public DOMPool(int nID, string sName)
        {
            // Set the member data through the property, so error/range checking code doesn't have to be duplicated.
            this.ID = nID;
            this.Name = sName;
        }

        public DOMPool(int nID, string sName, double dQ10, double dPropAir)
        {
            // Set the member data through the property, so error/range checking code doesn't have to be duplicated.
            this.ID = nID;
            this.Name = sName;
            this.Q10 = dQ10;
            this.PropAir = dPropAir;
        }

        public int ID
        {
            get
            {
                return m_nID;
            }
            set
            {
                if (value <= 0)
                    throw new Edu.Wisc.Forest.Flel.Util.InputValueException(value.ToString(), "DOM pool must have an ID larger than 0.  The value provided is = {0}.", value);
                m_nID = value;
            }
        }

        public string Name
        {
            get
            {
                return m_sName;
            }
            set
            {
                string sName = value.Trim();
                if (String.IsNullOrEmpty(sName))
                    throw new Edu.Wisc.Forest.Flel.Util.InputValueException(sName, "A DOM pool must have a valid name.");
                m_sName = value;
            }
        }

        public double Q10
        {
            get
            {
                return m_dQ10;
            }
            set
            {
                if (value < 0.0)
                    throw new Edu.Wisc.Forest.Flel.Util.InputValueException(value.ToString(), "Q10 must be greater than or equal to 0.");
                m_dQ10 = value;
            }
        }

        public double PropAir
        {
            get
            {
                return m_dPropAir;
            }
            set
            {
                if ((value < 0.0) || (value > 1.0))
                    throw new Edu.Wisc.Forest.Flel.Util.InputValueException(value.ToString(), "Proportion to Air must be in the range [0.0, 1.0].");
                m_dPropAir = value;
            }
        }
    }
}
