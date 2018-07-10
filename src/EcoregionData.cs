//  Copyright 2005-2010 Portland State University, University of Wisconsin
//  Authors:  Robert M. Scheller, James B. Domingo

using Landis.SpatialModeling;
using Landis.Core;
using Edu.Wisc.Forest.Flel.Util;

namespace Landis.Extension.Succession.ForC
{
    public class EcoregionData
    {

        //user-defined by ecoregion
        public static Ecoregions.AuxParm<double> FieldCapacity;
        public static Ecoregions.AuxParm<double> Latitude;

        public static Ecoregions.AuxParm<double> AET;
        
        public static Ecoregions.AuxParm<double> ActiveSiteCount;
        public static Ecoregions.AuxParm<Percentage>[] ShadeBiomass;
        public static Ecoregions.AuxParm<int> B_MAX;  // used for shade calculations; derived below.

        /*  CODE RELATED TO THE USE OF ONE OF THE BIGGER LANDIS CLIMATE LIBRARIES
        // AnnualClimateArray contains climates for N years whereby N is the succession time step.
        // AnnualClimate is the active (current) year's climate, one of the elements in AnnualClimateArray.
        public static Ecoregions.AuxParm<AnnualClimate_Monthly[]> AnnualClimateArray; //Climate Library v2.0
        //public static Ecoregions.AuxParm<AnnualClimate[]> AnnualClimateArray;  //Climate Library on GitHub
        public static Ecoregions.AuxParm<AnnualClimate_Monthly> AnnualWeather; //Climate Library v2.0
        //public static Ecoregions.AuxParm<AnnualClimate> AnnualWeather;  //Climate Library on GitHub
        public static Ecoregions.AuxParm<bool[]> ClimateUpdates;
        */
        //New ForCs Climate (April 2018)
        private static IInputClimateParms m_iCParams;
        public static Ecoregions.AuxParm<double> AnnualTemperature;
        private static bool bWroteMsg1;

        //---------------------------------------------------------------------
        public static void Initialize(IInputParameters parameters, IInputClimateParms paramClimate)
        {
        
            m_iCParams          = paramClimate;
            ShadeBiomass        = parameters.MinRelativeBiomass;

            FieldCapacity = parameters.FieldCapacity;
            Latitude = parameters.Latitude;
    
            ActiveSiteCount = new Ecoregions.AuxParm<double>(PlugIn.ModelCore.Ecoregions);
            //ClimateUpdates  = new Ecoregions.AuxParm<bool[]>(PlugIn.ModelCore.Ecoregions);    //LANDIS CLIMATE LIBRARIES
            AET             = new Ecoregions.AuxParm<double>(PlugIn.ModelCore.Ecoregions);
            
            foreach (ActiveSite site in PlugIn.ModelCore.Landscape)
            {
                IEcoregion ecoregion = PlugIn.ModelCore.Ecoregion[site];
                
                ActiveSiteCount[ecoregion]++;
            }
            //NEW ForCS specific Climate
            AnnualTemperature = new Ecoregions.AuxParm<double>(PlugIn.ModelCore.Ecoregions);
            GetAnnualTemperature(parameters.Timestep, 0);

            /*  CODE RELATED TO THE USE OF ONE OF THE BIGGER LANDIS CLIMATE LIBRARIES

            GenerateNewClimate(0, parameters.Timestep);
            
            AnnualWeather = new Ecoregions.AuxParm<AnnualClimate_Monthly>(PlugIn.ModelCore.Ecoregions); //Climate Library v2.0
            //AnnualWeather = new Ecoregions.AuxParm<AnnualClimate>(PlugIn.ModelCore.Ecoregions);   //Climate Library on GitHub
            foreach(IEcoregion ecoregion in PlugIn.ModelCore.Ecoregions) 
            {
                if(ActiveSiteCount[ecoregion] > 0)
                {
                    SetAnnualClimate(ecoregion, 0);
                    ClimateUpdates[ecoregion] = new bool[PlugIn.ModelCore.EndTime + parameters.Timestep + 1];
                    ClimateUpdates[ecoregion][0] = true;
                }
            }
            foreach (IEcoregion ecoregion in PlugIn.ModelCore.Ecoregions)
            {
                if (ActiveSiteCount[ecoregion] > 0)
                {
                    SetAnnualClimate(ecoregion, 0);
                }
            }
            */
        }
        //---------------------------------------------------------------------
        public static void UpdateB_MAX()
        {
            B_MAX  = new Ecoregions.AuxParm<int>(PlugIn.ModelCore.Ecoregions);
            
            //  Fill in B_MAX array
            foreach (IEcoregion ecoregion in PlugIn.ModelCore.Ecoregions) 
            {
                if(ActiveSiteCount[ecoregion] > 0)
                {
                    int largest_B_MAX_Spp = 0;
                    foreach (ISpecies species in PlugIn.ModelCore.Species) 
                    {
                        largest_B_MAX_Spp = System.Math.Max(largest_B_MAX_Spp, SpeciesData.B_MAX_Spp[species][ecoregion]);
                    }
                    B_MAX[ecoregion] = largest_B_MAX_Spp;
                }
            }
         
        }

 /*  CODE RELATED TO THE USE OF ONE OF THE BIGGER LANDIS CLIMATE LIBRARIES

        //---------------------------------------------------------------------
        // Generates new climate parameters at an annual time step.
        // Note:  During the spin-up phase of growth, the same annual climates will
        // be used repeatedly in order.
        public static void SetAnnualClimate(IEcoregion ecoregion, int year)
        {
            int actualYear = PlugIn.ModelCore.CurrentTime + year;
            
            if(actualYear == 0 || actualYear != AnnualWeather[ecoregion].Year)
            {
                //PlugIn.ModelCore.Log.WriteLine("  SETTING ANNAUL CLIMATE:  Yr={0}, SimYr={1}, Eco={2}.", year, actualYear, ecoregion.Name);
                AnnualWeather[ecoregion] = AnnualClimateArray[ecoregion][year];
                AET[ecoregion] = AnnualClimate_Monthly.CalculateAnnualActualEvapotranspiration(AnnualWeather[ecoregion], FieldCapacity[ecoregion]); //Climate Library v2.0
                //AET[ecoregion] = AnnualClimate.CalculateAnnualActualEvapotranspiration(AnnualWeather[ecoregion], FieldCapacity[ecoregion]);  //Climate Library on GitHub
                //string weatherWrite = AnnualWeather[ecoregion].Write();
                //PlugIn.ModelCore.Log.WriteLine("{0}", weatherWrite);
            }
        }

        

        //---------------------------------------------------------------------
        // Generates new climate parameters at an annual time step.
        // 
        public static void GenerateNewClimate(int year, int years)
        {
        
            //PlugIn.ModelCore.Log.WriteLine("   Generating new climate for simulation year {0}.", year);

            AnnualClimateArray = new Ecoregions.AuxParm<AnnualClimate_Monthly[]>(PlugIn.ModelCore.Ecoregions); //Climate Library v2.0
            //AnnualClimateArray = new Ecoregions.AuxParm<AnnualClimate[]>(PlugIn.ModelCore.Ecoregions);   //Climate Library on GitHub
            
            // Issues with this approach:  Each ecoregion will have unique variability associated with 
            // temperature and precipitation.  In reality, we expect some regional synchronicity.  An 
            // easy-ish solution would be to use the same random number in combination with standard 
            // deviations for all ecoregions.  The converse problem is over synchronization of climate, but
            // that would certainly be preferrable over smaller regions.
            
            
            
            foreach(IEcoregion ecoregion in PlugIn.ModelCore.Ecoregions) 
            {
                if(ActiveSiteCount[ecoregion] > 0)
                {
                    AnnualClimate_Monthly[] tempClimate = new AnnualClimate_Monthly[years]; //Climate Library v2.0
                    //AnnualClimate[] tempClimate = new AnnualClimate[years]; //Climate Library on GitHub
            
                    for (int y = 0; y < years; y++)
                    {
                
                        int actualYear = year + y;

                        if (Climate.Future_AllData.ContainsKey(actualYear))  //Climate Library v2.0
                        //if (Climate.AllData.ContainsKey(actualYear)) //Climate Library on GitHub
                        {
                            //Climate.TimestepData = Climate.Future_AllData[actualYear];
                            //PlugIn.ModelCore.Log.WriteLine("  Changing TimestepData:  Yr={0}, Eco={1}.", actualYear, ecoregion.Name);
                            //PlugIn.ModelCore.Log.WriteLine("  Changing TimestepData:  AllData  Jan Ppt = {0:0.00}.", Climate.AllData[actualYear][ecoregion.Index,0].AvgPpt);
                            //PlugIn.ModelCore.Log.WriteLine("  Changing TimestepData:  Timestep Jan Ppt = {0:0.00}.", Climate.TimestepData[ecoregion.Index,0].AvgPpt);
                        }

                        tempClimate[y] = new AnnualClimate_Monthly(ecoregion, Latitude[ecoregion], Climate.Phase.Future_Climate, actualYear, actualYear);  //Climate Library v2.0
                        //tempClimate[y] = new AnnualClimate(ecoregion, actualYear, Latitude[ecoregion]);  //Climate Library on GitHub
                    }
                
                    AnnualClimateArray[ecoregion] = tempClimate;
                }
            }
        }
        */

        //---------------------------------------------------------------------
        // Generates new temperature value
        // 
        public static void GetAnnualTemperature(int years, int spinupyear)
        {
            int usetime = PlugIn.ModelCore.CurrentTime;
            if (spinupyear < 0) usetime = spinupyear;
            IClimate climatetemperature;

            for (int y = 0; y < years; ++y)
            {

                foreach (IEcoregion ecoregion in PlugIn.ModelCore.Ecoregions)
                {
                    if (ActiveSiteCount[ecoregion] > 0)
                    {
                        if (m_iCParams.ClimateAnnualCollection[ecoregion].TryGetValue(usetime + y, out climatetemperature))
                        {
                            AnnualTemperature[ecoregion] = climatetemperature.ClimateAnnualTemp;
                        }
                        else if (usetime < 0)  //then users didn't enter anything for all the spin-up time, and we must use the year 0 time.
                        {
                            if (m_iCParams.ClimateAnnualCollection[ecoregion].TryGetValue(0, out climatetemperature))
                            {
                                if (!bWroteMsg1)
                                {
                                    PlugIn.ModelCore.Log.WriteLine("ANPP values were not entered for the earliest spin-up years. Year 0 values will be used.");
                                    bWroteMsg1 = true;
                                }
                                AnnualTemperature[ecoregion] = climatetemperature.ClimateAnnualTemp;
                            }
                        }
                    }
                }
            }
        }

    }
}
