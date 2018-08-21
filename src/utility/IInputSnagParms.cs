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
    /// The parameters for ForC snag initialization.
    /// </summary>
    public interface IInputSnagParms
    {
        int[] SnagSpecies { get ; } 
        int[] SnagAgeAtDeath { get; }
        int[] SnagTimeSinceDeath { get; }
        string[] SnagDisturb { get; }       
    }

        /// </summary>
    public class InputSnagParms
        : IInputSnagParms
    {
        //Initial Snag variables
        private int[] m_SnagSpecies;
        private int[] m_SnagAgeAtDeath;
        private int[] m_SnagTimeSinceDeath;
        private string[] m_SnagDisturb;

        public InputSnagParms()
        {
            this.m_SnagAgeAtDeath = new int[Snags.NUMSNAGS];
            this.m_SnagTimeSinceDeath = new int[Snags.NUMSNAGS];
            this.m_SnagSpecies = new int[Snags.NUMSNAGS];
            this.m_SnagDisturb = new string[Snags.NUMSNAGS];            
        }

        //---------------------------------------------------------------------
        //initial snag information
        public void SetInitSnagInfo(ISpecies species, InputValue<int> dAgeAtDeath, InputValue<int> dTimeSinceDeath, InputValue<string> sDisturbType, int i)
        {
            m_SnagSpecies[i] = species.Index;
            m_SnagAgeAtDeath[i] = Util.CheckBiomassParm(dAgeAtDeath, 0, 999);
            m_SnagTimeSinceDeath[i] = Util.CheckBiomassParm(dTimeSinceDeath, 0, 999);
            m_SnagDisturb[i] = sDisturbType;
        }
        public int[] SnagSpecies { get { return m_SnagSpecies; } }
        public int[] SnagAgeAtDeath { get { return m_SnagAgeAtDeath; } }
        public int[] SnagTimeSinceDeath { get { return m_SnagTimeSinceDeath; } }
        public string[] SnagDisturb { get { return m_SnagDisturb; } }

        //---------------------------------------------------------------------

    }

}
