//  Copyright 2005-2010 Portland State University, University of Wisconsin
//  Authors:  Robert M. Scheller, James B. Domingo

using Landis.SpatialModeling;
using Landis.Core;
using Landis.Library.BiomassCohorts;
using System.Collections.Generic;
using System;
using System.Diagnostics;       //temporary - for memory testing SEPT

namespace Landis.Extension.Succession.ForC
{
    /// <summary>
    /// The pools of dead biomass for the landscape's sites.
    /// </summary>
    public static class SiteVars
    {
        private static ISiteVar<Landis.Library.BiomassCohorts.SiteCohorts> biomassCohorts;

        private static ISiteVar<Landis.Library.AgeOnlyCohorts.ISiteCohorts> baseCohortsSiteVar;


        // Time of last succession simulation:
        private static ISiteVar<int> timeOfLast;
        private static ISiteVar<int> previousYearMortality;
        private static ISiteVar<int> currentYearMortality;
        private static ISiteVar<int> totalBiomass;

        
        // Soil and litter variables:
        public static ISiteVar<double> SoilOrganicMatterC;
        public static ISiteVar<double> DeadWoodMass;
        public static ISiteVar<double> DeadWoodDecayRate;
        public static ISiteVar<double> LitterMass;
        public static ISiteVar<double> LitterDecayRate;
        public static ISiteVar<double> AbovegroundNPPcarbon;

        public static ISiteVar<byte> fireSeverity;

        public static ISiteVar<SoilClass> soilClass;    
        public static ISiteVar<double> capacityReduction;

        //---------------------------------------------------------------------

        /// <summary>
        /// Initializes the module.
        /// </summary>
        public static void Initialize(IInputParameters iParams)
        {
            System.Diagnostics.Debug.Assert(iParams != null);

            biomassCohorts = PlugIn.ModelCore.Landscape.NewSiteVar<Library.BiomassCohorts.SiteCohorts>();
            ISiteVar<Landis.Library.BiomassCohorts.ISiteCohorts> biomassCohortSiteVar = Landis.Library.Succession.CohortSiteVar<Landis.Library.BiomassCohorts.ISiteCohorts>.Wrap(biomassCohorts);

            baseCohortsSiteVar = Landis.Library.Succession.CohortSiteVar<Landis.Library.AgeOnlyCohorts.ISiteCohorts>.Wrap(biomassCohorts);
            
        
            timeOfLast          = PlugIn.ModelCore.Landscape.NewSiteVar<int>();
            previousYearMortality = PlugIn.ModelCore.Landscape.NewSiteVar<int>();
            currentYearMortality = PlugIn.ModelCore.Landscape.NewSiteVar<int>();
            totalBiomass = PlugIn.ModelCore.Landscape.NewSiteVar<int>();
            
            SoilOrganicMatterC  = PlugIn.ModelCore.Landscape.NewSiteVar<double>();
            DeadWoodMass        = PlugIn.ModelCore.Landscape.NewSiteVar<double>();
            LitterMass          = PlugIn.ModelCore.Landscape.NewSiteVar<double>();
            DeadWoodDecayRate   = PlugIn.ModelCore.Landscape.NewSiteVar<double>();
            LitterDecayRate     = PlugIn.ModelCore.Landscape.NewSiteVar<double>();
            AbovegroundNPPcarbon           = PlugIn.ModelCore.Landscape.NewSiteVar<double>();
            
            fireSeverity        = PlugIn.ModelCore.GetSiteVar<byte>("Fire.Severity");

            soilClass = PlugIn.ModelCore.Landscape.NewSiteVar<SoilClass>();

            PlugIn.ModelCore.RegisterSiteVar(biomassCohortSiteVar, "Succession.BiomassCohorts");
            PlugIn.ModelCore.RegisterSiteVar(baseCohortsSiteVar, "Succession.AgeCohorts");
            
            foreach (ActiveSite site in PlugIn.ModelCore.Landscape)
            {
            //    cohorts[site]   = new SiteCohorts();
                // Note: we need this both here and in Plug-in
                Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();  //temporary - for memory testing SEPT
                double totalMBOfPhysicalMemory = currentProcess.WorkingSet64 / 100000.0;
                double totalMBOfVirtualMemory = currentProcess.VirtualMemorySize64 / 100000.0;
                //PlugIn.ModelCore.UI.WriteLine("Before a Site Physical, Virtual Memory Use: " + totalMBOfPhysicalMemory.ToString() + ", " + totalMBOfVirtualMemory.ToString());

                soilClass[site] = new SoilClass(iParams, site);
                totalMBOfPhysicalMemory = currentProcess.WorkingSet64 / 100000.0;
                totalMBOfVirtualMemory = currentProcess.VirtualMemorySize64 / 100000.0;
                //PlugIn.ModelCore.UI.WriteLine("After a Site Physical, Virtual Memory Use: " + totalMBOfPhysicalMemory.ToString() + ", " + totalMBOfVirtualMemory.ToString());

            }
            
        }
       
        //---------------------------------------------------------------------
        public static void ResetAnnualValues(Site site)
        {
            
            SiteVars.AbovegroundNPPcarbon[site]     = 0.0;
            SiteVars.TotalBiomass[site] = 0;
            SiteVars.TotalBiomass[site] = Library.BiomassCohorts.Cohorts.ComputeNonYoungBiomass(SiteVars.Cohorts[site]);

            SiteVars.PreviousYearMortality[site] = SiteVars.CurrentYearMortality[site];
            SiteVars.CurrentYearMortality[site] = 0;
            
        }
        //---------------------------------------------------------------------

        /// <summary>
        /// Biomass cohorts at each site.
        /// </summary>
        public static ISiteVar<SiteCohorts> Cohorts
        {
            get
            {
                return biomassCohorts;
            }
            set
            {
                biomassCohorts = value;
            }
        }

        //---------------------------------------------------------------------

        public static ISiteVar<int> TimeOfLast
        {
            get {
                return timeOfLast;
            }
        }
        //---------------------------------------------------------------------

        public static ISiteVar<byte> FireSeverity
        {
            get
            {
                return fireSeverity;
            }
            set {
                fireSeverity = value;
            }

        }

        //---------------------------------------------------------------------
        /// <summary>
        /// </summary>
        public static ISiteVar<double> CapacityReduction
        {
            get
            {
                return capacityReduction;
            }
            set
            {
                capacityReduction = value;
            }
        }
        //---------------------------------------------------------------------

        /// <summary>
        /// Previous Year Site Mortality.
        /// </summary>
        public static ISiteVar<int> TotalBiomass
        {
            get
            {
                return totalBiomass;
            }
        }
        //---------------------------------------------------------------------

        /// <summary>
        /// Previous Year Site Mortality.
        /// </summary>
        public static ISiteVar<int> PreviousYearMortality
        {
            get
            {
                return previousYearMortality;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Previous Year Site Mortality.
        /// </summary>
        public static ISiteVar<int> CurrentYearMortality
        {
            get
            {
                return currentYearMortality;
            }
        }
        
    }
}
