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
    /// The parameters for Disturbance Matrices.
    /// </summary>
    public interface IInputDMParameters
    //    : Dynamic.IParameters
    {
        IDictionary<int, IDOMPool> DOMPools { get;}
        /// <summary>
        /// Returns an array of IDisturbTransferFromPools objects, indexed by a 0-based severity level.
        /// </summary>
        IDisturbTransferFromPools[] DisturbFireFromDOMPools { get; }

        /// <summary>
        /// Returns a dictionary of IDisturbTransferFromPools objects, indexed by name of the disturbance (e.g. "Harvest").
        /// </summary>
        IDictionary<string, IDisturbTransferFromPools> DisturbOtherFromDOMPools { get; }

        /// <summary>
        /// Returns an array of IDisturbTransferFromPools objects, indexed by a 0-based severity level.
        /// </summary>
        IDisturbTransferFromPools[] DisturbFireFromBiomassPools { get; }

        /// <summary>
        /// Returns a dictionary of IDisturbTransferFromPools objects, indexed by name of the disturbance (e.g. "Harvest").
        /// </summary>
        IDictionary<string, IDisturbTransferFromPools> DisturbOtherFromBiomassPools { get; }
    }

    /// </summary>
    public class InputDMParameters
        : IInputDMParameters
    {
        private IDictionary<int, IDOMPool> m_dictDOMPools;
        private IDisturbTransferFromPools[] m_aDisturbFireFromDOMPools;
        private IDictionary<string, IDisturbTransferFromPools> m_dictDisturbOtherFromDOMPools;
        private IDisturbTransferFromPools[] m_aDisturbFireFromBiomassPools;
        private IDictionary<string, IDisturbTransferFromPools> m_dictDisturbOtherFromBiomassPools;
        //---------------------------------------------------------------------
        public IDictionary<int, IDOMPool> DOMPools { get { return m_dictDOMPools; } }

        /// <summary>
        /// Returns an array of IDisturbTransferFromPools objects, indexed by a 0-based severity level.
        /// </summary>
        public IDisturbTransferFromPools[] DisturbFireFromDOMPools { get { return m_aDisturbFireFromDOMPools; } }

        /// <summary>
        /// Returns a dictionary of IDisturbTransferFromPools objects, indexed by name of the disturbance (e.g. "Harvest").
        /// </summary>
        public IDictionary<string, IDisturbTransferFromPools> DisturbOtherFromDOMPools { get { return m_dictDisturbOtherFromDOMPools; } }

        /// <summary>
        /// Returns an array of IDisturbTransferFromPools objects, indexed by a 0-based severity level.
        /// </summary>
        public IDisturbTransferFromPools[] DisturbFireFromBiomassPools { get { return m_aDisturbFireFromBiomassPools; } }

        /// <summary>
        /// Returns a dictionary of IDisturbTransferFromPools objects, indexed by name of the disturbance (e.g. "Harvest").
        /// </summary>
        public IDictionary<string, IDisturbTransferFromPools> DisturbOtherFromBiomassPools { get { return m_dictDisturbOtherFromBiomassPools; } }

        //---------------------------------------------------------------------

        public void SetDisturbFireFromDOMPools(IDisturbTransferFromPools[] aDisturbTransfer)
        {
            m_aDisturbFireFromDOMPools = aDisturbTransfer;
        }

        public void SetDisturbOtherFromDOMPools(IDictionary<string, IDisturbTransferFromPools> dictDisturbTransfer)
        {
            m_dictDisturbOtherFromDOMPools = dictDisturbTransfer;
        }
        //---------------------------------------------------------------------

        public void SetDisturbFireFromBiomassPools(IDisturbTransferFromPools[] aDisturbTransfer)
        {
            m_aDisturbFireFromBiomassPools = aDisturbTransfer;
        }

        public void SetDisturbOtherFromBiomassPools(IDictionary<string, IDisturbTransferFromPools> dictDisturbTransfer)
        {
            m_dictDisturbOtherFromBiomassPools = dictDisturbTransfer;
        }
        //---------------------------------------------------------------------
        public void SetDOMPool(int nID, string sName)
        {
            //Debug.Assert(nID > 0);
            //Debug.Assert(string.IsNullOrEmpty(sName));
            DOMPool pool = new DOMPool(nID, sName);
            this.m_dictDOMPools.Add(pool.ID, pool);
        }

        //---------------------------------------------------------------------
        public InputDMParameters()
        {
            this.m_dictDOMPools = new Dictionary<int, IDOMPool>();
        }

    }
}
