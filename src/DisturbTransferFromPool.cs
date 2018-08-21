using System;
using System.Collections.Generic;
using System.Text;
using Landis.Utilities;

namespace Landis.Extension.Succession.ForC
{
    public interface IDisturbTransferFromPool
    {
        int ID { get; }

        string Name { get; }

        double PropToAir { get; }

        double PropToFloor { get; }

        double PropToFPS { get; }

        double PropToDOM { get; }
    }

    public interface IDisturbTransferFromPools
    {
        /// <param name="nPoolID">Pool ID, 1-based</param>
        IDisturbTransferFromPool GetDisturbTransfer(int nPoolID);
    }

    class DisturbTransferFromPool : IDisturbTransferFromPool
    {
        public const string mk_sDisturbTypeFire = "Fire";
        public const string mk_sDisturbTypeHarvest = "Harvest";

        private int m_nID;
        private string m_sName;
        private double m_dPropToAir = 0.0;
        private double m_dPropToFloor = 0.0;
        private double m_dPropToFPS = 0.0;
        private double m_dPropToDOM = 0.0;

        public DisturbTransferFromPool(int nID)
        {
            // Set the member data through the property, so error/range checking code doesn't have to be duplicated.
            this.ID = nID;
        }

        public DisturbTransferFromPool(int nID, string sName)
        {
            // Set the member data through the property, so error/range checking code doesn't have to be duplicated.
            this.ID = nID;
            this.Name = sName;
        }

        public DisturbTransferFromPool(int nID, double dPropToAir, double dPropToFloor, double dPropToFPS, double dPropToDOM)
        {
            // Set the member data through the property, so error/range checking code doesn't have to be duplicated.
            this.ID = nID;
            if ((dPropToAir + dPropToFloor + dPropToFPS + dPropToDOM) > 1.0)
                throw new Landis.Utilities.InputValueException("Proportions", "Sum of all proportions must be no greater than 1.0.  The total of the proportions is = {0}.", dPropToAir + dPropToFloor + dPropToFPS + dPropToDOM);
            this.PropToAir = dPropToAir;
            this.PropToFloor = dPropToFloor;
            this.PropToFPS = dPropToFPS;
            this.PropToDOM = dPropToDOM;
        }

        public DisturbTransferFromPool(int nID, string sName, double dPropToAir, double dPropToFloor, double dPropToFPS, double dPropToDOM)
        {
            // Set the member data through the property, so error/range checking code doesn't have to be duplicated.
            this.ID = nID;
            this.Name = sName;
            if ((dPropToAir + dPropToFloor + dPropToFPS + dPropToDOM) > 1.0)
                throw new Landis.Utilities.InputValueException("Proportions", "Sum of all proportions must be no greater than 1.0.  The total of the proportions is = {0}.", dPropToAir + dPropToFloor + dPropToFPS + dPropToDOM);
            this.PropToAir = dPropToAir;
            this.PropToFloor = dPropToFloor;
            this.PropToFPS = dPropToFPS;
            this.PropToDOM = dPropToDOM;
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
                    throw new Landis.Utilities.InputValueException(value.ToString(), "ID must be greater than 0.  The value provided is = {0}.", value);
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
                if (string.IsNullOrEmpty(value))
                    throw new Landis.Utilities.InputValueException(value.ToString(), "A Name must be provided.");
                m_sName = value;
            }
        }

        public double PropToAir
        {
            get
            {
                return m_dPropToAir;
            }
            set
            {
                if ((value < 0.0) || (value > 1.0))
                    throw new Landis.Utilities.InputValueException(value.ToString(), "Proportion to Air must be in the range [0.0, 1.0].");
                m_dPropToAir = value;
            }
        }

        public double PropToFloor
        {
            get
            {
                return m_dPropToFloor;
            }
            set
            {
                if ((value < 0.0) || (value > 1.0))
                    throw new InputValueException(value.ToString(), "Proportion to Floor must be in the range [0.0, 1.0].");
                m_dPropToFloor = value;
            }
        }

        public double PropToFPS
        {
            get
            {
                return m_dPropToFPS;
            }
            set
            {
                if ((value < 0.0) || (value > 1.0))
                    throw new InputValueException(value.ToString(), "Proportion to FPS must be in the range [0.0, 1.0].");
                m_dPropToFPS = value;
            }
        }

        public double PropToDOM
        {
            get
            {
                return m_dPropToDOM;
            }
            set
            {
                if ((value < 0.0) || (value > 1.0))
                    throw new Landis.Utilities.InputValueException(value.ToString(), "Proportion to DOM must be in the range [0.0, 1.0].");
                m_dPropToDOM = value;
            }
        }

    }
    class DisturbTransferFromPools : IDisturbTransferFromPools
    {
        private string m_sName;
        /// <summary>
        /// Implemented as a dictionary, however it could also be a simple array.
        /// </summary>
        private System.Collections.Generic.Dictionary<int, DisturbTransferFromPool> m_dict;

        public DisturbTransferFromPools(string sName)
        {
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(sName));
            m_sName = sName;
            m_dict = new Dictionary<int,DisturbTransferFromPool>();
        }

        public void InitializeDOMPools(IDictionary<int, IDOMPool> dictDOMPools)
        {
            m_dict.Clear();
            foreach (KeyValuePair<int, IDOMPool> kvp in dictDOMPools)
            {
                m_dict.Add(kvp.Value.ID, new DisturbTransferFromPool(kvp.Value.ID, kvp.Value.Name));
            }
        }

        public void InitializeBiomassPools()
        {
            m_dict.Clear();
            m_dict.Add((int)SoilClass.eBiomassPoolIDs.Merchantable, new DisturbTransferFromPool((int)SoilClass.eBiomassPoolIDs.Merchantable, "Merchantable"));
            m_dict.Add((int)SoilClass.eBiomassPoolIDs.Foliage, new DisturbTransferFromPool((int)SoilClass.eBiomassPoolIDs.Foliage, "Foliage"));
            m_dict.Add((int)SoilClass.eBiomassPoolIDs.Other, new DisturbTransferFromPool((int)SoilClass.eBiomassPoolIDs.Other, "Other"));
            m_dict.Add((int)SoilClass.eBiomassPoolIDs.SubMerchantable, new DisturbTransferFromPool((int)SoilClass.eBiomassPoolIDs.SubMerchantable, "Sub-Merchantable"));
            m_dict.Add((int)SoilClass.eBiomassPoolIDs.CoarseRoot, new DisturbTransferFromPool((int)SoilClass.eBiomassPoolIDs.CoarseRoot, "Coarse Root"));
            m_dict.Add((int)SoilClass.eBiomassPoolIDs.FineRoot, new DisturbTransferFromPool((int)SoilClass.eBiomassPoolIDs.FineRoot, "Fine Root"));
        }

        /// <param name="nPoolID">Pool ID, 1-based</param>
        public IDisturbTransferFromPool GetDisturbTransfer(int nPoolID)
        {
            //if ((nPoolID < (int)eDOMPoolIDs.VeryFastAG) || (nPoolID > (int)eDOMPoolIDs.BlackCarbon))
            if (!m_dict.ContainsKey(nPoolID))
                throw new Landis.Utilities.InputValueException(nPoolID.ToString(), "Pool ID cannot be found.  Has Initialize*() been called?");
            return m_dict[nPoolID];
        }
    }
}
