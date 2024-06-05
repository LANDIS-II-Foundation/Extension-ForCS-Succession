//  Copyright 2005-2010 Portland State University, University of Wisconsin
//  Authors:  Robert M. Scheller, James B. Domingo

using Landis.SpatialModeling;
using Landis.Core;
using Landis.Library.UniversalCohorts;
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
        private static ISiteVar<SiteCohorts> universalCohorts;
        private static ISiteVar<ISiteCohorts> universalCohortSiteVar;


        // Time of last succession simulation:
        private static ISiteVar<int> timeOfLast;
        private static ISiteVar<int> previousYearMortality;
        private static ISiteVar<int> currentYearMortality;
        private static ISiteVar<int> totalBiomass;


        // Soil and litter variables:
        public static ISiteVar<Pool> DeadWoodMass;
        public static ISiteVar<Pool> LitterMass;
        public static ISiteVar<double> SoilOrganicMatterC;
        //public static ISiteVar<double> DeadWoodMass;
        public static ISiteVar<double> DeadWoodDecayRate;
        //public static ISiteVar<double> LitterMass;
        public static ISiteVar<double> LitterDecayRate;
        public static ISiteVar<double> AbovegroundNPPcarbon;

        public static ISiteVar<byte> fireSeverity;
        public static ISiteVar<string> HarvestPrescriptionName;

        public static ISiteVar<SoilClass> soilClass;    
        public static ISiteVar<double> capacityReduction;

        //Site-level values for printing maps
        public static ISiteVar<double> NPP;
        public static ISiteVar<double> RH;
        public static ISiteVar<double> NEP;
        public static ISiteVar<double> NBP;
        public static ISiteVar<double> TotBiomassC;
        public static ISiteVar<double> ToFPSC;


        //---------------------------------------------------------------------

        /// <summary>
        /// Initializes the module.
        /// </summary>
        public static void Initialize(IInputParameters iParams, IInputDMParameters iDMParams)
        {
            System.Diagnostics.Debug.Assert(iParams != null);

            universalCohorts = PlugIn.ModelCore.Landscape.NewSiteVar<SiteCohorts>();
            //ISiteVar<Landis.Library.UniversalCohorts.ISiteCohorts> biomassCohortSiteVar = Landis.Library.Succession.CohortSiteVar<Landis.Library.UniversalCohorts.ISiteCohorts>.Wrap(biomassCohorts);
            universalCohortSiteVar = Landis.Library.Succession.CohortSiteVar<Landis.Library.UniversalCohorts.ISiteCohorts>.Wrap(universalCohorts);
            
        
            timeOfLast          = PlugIn.ModelCore.Landscape.NewSiteVar<int>();
            previousYearMortality = PlugIn.ModelCore.Landscape.NewSiteVar<int>();
            currentYearMortality = PlugIn.ModelCore.Landscape.NewSiteVar<int>();
            totalBiomass = PlugIn.ModelCore.Landscape.NewSiteVar<int>();

            DeadWoodMass = PlugIn.ModelCore.Landscape.NewSiteVar<Pool>();
            LitterMass = PlugIn.ModelCore.Landscape.NewSiteVar<Pool>();
            SoilOrganicMatterC = PlugIn.ModelCore.Landscape.NewSiteVar<double>();
            //DeadWoodMass        = PlugIn.ModelCore.Landscape.NewSiteVar<double>();
            //LitterMass          = PlugIn.ModelCore.Landscape.NewSiteVar<double>();
            DeadWoodDecayRate   = PlugIn.ModelCore.Landscape.NewSiteVar<double>();
            LitterDecayRate     = PlugIn.ModelCore.Landscape.NewSiteVar<double>();
            AbovegroundNPPcarbon           = PlugIn.ModelCore.Landscape.NewSiteVar<double>();

            NPP             = PlugIn.ModelCore.Landscape.NewSiteVar<double>();
            RH              = PlugIn.ModelCore.Landscape.NewSiteVar<double>();
            NEP             = PlugIn.ModelCore.Landscape.NewSiteVar<double>();
            NBP             = PlugIn.ModelCore.Landscape.NewSiteVar<double>();
            TotBiomassC     = PlugIn.ModelCore.Landscape.NewSiteVar<double>();
            ToFPSC          = PlugIn.ModelCore.Landscape.NewSiteVar<double>();

            fireSeverity        = PlugIn.ModelCore.GetSiteVar<byte>("Fire.Severity");
            HarvestPrescriptionName = PlugIn.ModelCore.GetSiteVar<string>("Harvest.PrescriptionName");

            soilClass = PlugIn.ModelCore.Landscape.NewSiteVar<SoilClass>();

            foreach (ActiveSite site in PlugIn.ModelCore.Landscape)
            {
                //  site cohorts are initialized by the PlugIn.InitializeSite method
                DeadWoodMass[site] = new Pool();
                LitterMass[site] = new Pool();
            }

            //PlugIn.ModelCore.UI.WriteLine("Before Register " + DateTime.Now.ToString("HH:mm:ss.fff", System.Globalization.DateTimeFormatInfo.InvariantInfo));

            PlugIn.ModelCore.RegisterSiteVar(universalCohortSiteVar, "Succession.UniversalCohorts");
            PlugIn.ModelCore.RegisterSiteVar(LitterMass, "Succession.Litter");
            PlugIn.ModelCore.RegisterSiteVar(DeadWoodMass, "Succession.WoodyDebris");

            //PlugIn.ModelCore.UI.WriteLine("After Register " + DateTime.Now.ToString("HH:mm:ss.fff", System.Globalization.DateTimeFormatInfo.InvariantInfo));


            foreach (ActiveSite site in PlugIn.ModelCore.Landscape)
            {
            //    cohorts[site]   = new SiteCohorts();
                // Note: we need this both here and in Plug-in
                Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();  //temporary - for memory testing SEPT
                double totalMBOfPhysicalMemory = currentProcess.WorkingSet64 / 100000.0;
                double totalMBOfVirtualMemory = currentProcess.VirtualMemorySize64 / 100000.0;
                //PlugIn.ModelCore.UI.WriteLine("Before a Site Physical, Virtual Memory Use: " + totalMBOfPhysicalMemory.ToString() + ", " + totalMBOfVirtualMemory.ToString());


                soilClass[site] = new SoilClass(iParams, site, iDMParams);
                //PlugIn.ModelCore.UI.WriteLine("Each site " + DateTime.Now.ToString("HH:mm:ss.fff", System.Globalization.DateTimeFormatInfo.InvariantInfo));
                totalMBOfPhysicalMemory = currentProcess.WorkingSet64 / 100000.0;
                totalMBOfVirtualMemory = currentProcess.VirtualMemorySize64 / 100000.0;
                //PlugIn.ModelCore.UI.WriteLine("After a Site Physical, Virtual Memory Use: " + totalMBOfPhysicalMemory.ToString() + ", " + totalMBOfVirtualMemory.ToString());

            }

            //PlugIn.ModelCore.UI.WriteLine("After Sites " + DateTime.Now.ToString("HH:mm:ss.fff", System.Globalization.DateTimeFormatInfo.InvariantInfo));

        }

        //---------------------------------------------------------------------
        public static void ResetAnnualValues(Site site)
        {
            
            SiteVars.AbovegroundNPPcarbon[site]     = 0.0;
            SiteVars.TotalBiomass[site] = 0;
            SiteVars.TotalBiomass[site] = Library.UniversalCohorts.Cohorts.ComputeNonYoungBiomass(SiteVars.Cohorts[site]);

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
                return universalCohorts;
            }
            set
            {
                universalCohorts = value;
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
