//  Authors:  Caren Dymond, Sarah Beukema

using Landis.SpatialModeling;
using Landis.Library.BiomassCohorts;
using Landis.Core;
using System.Collections.Generic;
using Landis.Utilities;
using Landis.Library.Succession;
using Landis.Library.Parameters;
using System.IO;
using System;


namespace Landis.Extension.Succession.ForC
{
    public class SpeciesData
    {
        public static Landis.Library.Parameters.Species.AuxParm<int> FuncType;
        public static Landis.Library.Parameters.Species.AuxParm<double> LeafLongevity;
        public static Landis.Library.Parameters.Species.AuxParm<bool> Epicormic;
        
        public static Landis.Library.Parameters.Species.AuxParm<double> MortCurveShape;

        public static Landis.Library.Parameters.Species.AuxParm<int> MerchStemsMinAge;
        public static Landis.Library.Parameters.Species.AuxParm<double> MerchCurveParmA;
        public static Landis.Library.Parameters.Species.AuxParm<double> MerchCurveParmB;
        public static Landis.Library.Parameters.Species.AuxParm<double> PropNonMerch;

        public static Landis.Library.Parameters.Species.AuxParm<Landis.Library.Parameters.Ecoregions.AuxParm<double>> EstablishProbability;
        public static Landis.Library.Parameters.Species.AuxParm<Landis.Library.Parameters.Ecoregions.AuxParm<double>> ANPP_MAX_Spp;
        public static Landis.Library.Parameters.Species.AuxParm<Landis.Library.Parameters.Ecoregions.AuxParm<int>> B_MAX_Spp;
        //  Establishment probability modifier for each species in each ecoregion (from biomass succession)
        public static Landis.Library.Parameters.Species.AuxParm<Landis.Library.Parameters.Ecoregions.AuxParm<double>> EstablishModifier;

        //root parameterss
        public static Landis.Library.Parameters.Species.AuxParm<Landis.Library.Parameters.Ecoregions.AuxParm<double[]>> MinWoodyBio;
        public static Landis.Library.Parameters.Species.AuxParm<Landis.Library.Parameters.Ecoregions.AuxParm<double[]>> Ratio;
        public static Landis.Library.Parameters.Species.AuxParm<Landis.Library.Parameters.Ecoregions.AuxParm<double[]>> PropFine;
        public static Landis.Library.Parameters.Species.AuxParm<Landis.Library.Parameters.Ecoregions.AuxParm<double[]>> FineTurnover;
        public static Landis.Library.Parameters.Species.AuxParm<Landis.Library.Parameters.Ecoregions.AuxParm<double[]>> CoarseTurnover;

        private static IInputParameters m_iParams;
        private static StreamWriter log;
        //private static Random rnd1 ;
        //private static Random rnd2 ;
        private static bool bWroteMsg1;
        private static bool bWroteMsg2;
        //private static bool bWroteMsg3;


        //---------------------------------------------------------------------
        public static void Initialize(IInputParameters parameters)
        {
            string logFileName   = "ForCS-ANPP-Establishment-log.csv"; 
            PlugIn.ModelCore.UI.WriteLine("   Opening a Growth and Establishment log file \"{0}\" ...", logFileName);
            try {
                log = Landis.Data.CreateTextFile(logFileName);
            }
            catch (Exception err) {
                string mesg = string.Format("{0}", err.Message);
                throw new System.ApplicationException(mesg);
            }
            
            log.AutoFlush = true;
            log.WriteLine("Time, Ecoregion, Species, MaxANPP, MaxBiomass, MAT, EstabProb");


            FuncType            = parameters.SppFunctionalType;
            LeafLongevity       = parameters.LeafLongevity;
            Epicormic           = parameters.Epicormic;
            
            MortCurveShape       = parameters.MortCurveShape;

            MerchStemsMinAge = parameters.MerchStemsMinAge;
            MerchCurveParmA = parameters.MerchCurveParmA;
            MerchCurveParmB = parameters.MerchCurveParmB;
            PropNonMerch = parameters.PropNonMerch;

            //initialize the roots
            Ratio = Util.CreateSpeciesEcoregionArrayParm<double>(PlugIn.ModelCore.Species, PlugIn.ModelCore.Ecoregions, 5);
            MinWoodyBio = Util.CreateSpeciesEcoregionArrayParm<double>(PlugIn.ModelCore.Species, PlugIn.ModelCore.Ecoregions, 5);
            PropFine = Util.CreateSpeciesEcoregionArrayParm<double>(PlugIn.ModelCore.Species, PlugIn.ModelCore.Ecoregions, 5);
            FineTurnover = Util.CreateSpeciesEcoregionArrayParm<double>(PlugIn.ModelCore.Species, PlugIn.ModelCore.Ecoregions, 5);
            CoarseTurnover = Util.CreateSpeciesEcoregionArrayParm<double>(PlugIn.ModelCore.Species, PlugIn.ModelCore.Ecoregions, 5);

            foreach (IEcoregion ecoregion in PlugIn.ModelCore.Ecoregions)
            {
                if (EcoregionData.ActiveSiteCount[ecoregion] == 0)
                    continue;
                foreach (ISpecies species in PlugIn.ModelCore.Species)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        Ratio[species][ecoregion][i] = parameters.Ratio[ecoregion][species][i];
                        MinWoodyBio[species][ecoregion][i] = parameters.MinWoodyBio[ecoregion][species][i];
                        PropFine[species][ecoregion][i] = parameters.PropFine[ecoregion][species][i];
                        FineTurnover[species][ecoregion][i] = parameters.FineTurnover[ecoregion][species][i];
                        CoarseTurnover[species][ecoregion][i] = parameters.CoarseTurnover[ecoregion][species][i];
                    }
                }
            }


            m_iParams = parameters;
            //rnd1 = new Random(123); //initialize with a seed so that runs are repeatable
            //rnd1 = new Random(1444);    //TODO: read the seed in from the input file - or use the LANDIS one
            bWroteMsg1 = false;
            bWroteMsg2 = false;
            //bWroteMsg3 = false;

            // The initial ANPP and max biomass:
            GenerateNewANPPandMaxBiomass(parameters.Timestep, 0);

            // The initial set of establishment probabilities:
            //EstablishProbability = Establishment.GenerateNewEstablishProbabilities(parameters.Timestep);  
            //Jun11  EstablishProbability = m_iParams.EstablishProbability;
            //Reproduction.ChangeEstablishProbabilities(Util.ToArray<double>(SpeciesData.EstablishProbability));
        }

        //---------------------------------------------------------------------------
        public static void GenerateNewANPPandMaxBiomass(int years, int spinupyear)
        {
            ANPP_MAX_Spp = Util.CreateSpeciesEcoregionParm<double>(PlugIn.ModelCore.Species, PlugIn.ModelCore.Ecoregions);
            B_MAX_Spp = Util.CreateSpeciesEcoregionParm<int>(PlugIn.ModelCore.Species, PlugIn.ModelCore.Ecoregions);
            EstablishProbability = Util.CreateSpeciesEcoregionParm<double>(PlugIn.ModelCore.Species, PlugIn.ModelCore.Ecoregions);
            EstablishModifier = Util.CreateSpeciesEcoregionParm<double>(PlugIn.ModelCore.Species, PlugIn.ModelCore.Ecoregions);
            double MeanAnnualTemperature = 0.0;

            int usetime = PlugIn.ModelCore.CurrentTime;
            if (spinupyear < 0) usetime = spinupyear;

            for (int y = 0; y < years; ++y) 
            {
                foreach(IEcoregion ecoregion in PlugIn.ModelCore.Ecoregions) 
                {
                    if(EcoregionData.ActiveSiteCount[ecoregion] == 0)
                        continue;

                     /* CODE RELATED TO THE USE OF ONE OF THE BIGGER LANDIS CLIMATE LIBRARIES
                    
                    AnnualClimate_Monthly ecoClimate = EcoregionData.AnnualWeather[ecoregion]; //Climate Library v2
                    //AnnualClimate ecoClimate = EcoregionData.AnnualClimateArray[ecoregion][y];//Climate Library on GitHub
                
                    if(ecoClimate == null)
                        throw new System.ApplicationException("Error: CLIMATE NULL.");

                    if (usetime >= 0)
                        MeanAnnualTemperature = (double)ecoClimate.CalculateMeanAnnualTemp(usetime); //Climate Library v2
                        //MeanAnnualTemperature = (double)ecoClimate.MeanAnnualTemp(usetime);  //Climate Library on GitHub
                    else
                        MeanAnnualTemperature = (double)ecoClimate.CalculateMeanAnnualTemp(0); //Climate Library v2
                        //MeanAnnualTemperature = (double)ecoClimate.MeanAnnualTemp(0);  //Climate Library on GitHub
                    */
                    foreach(ISpecies species in PlugIn.ModelCore.Species)
                    {
                        IANPP anpp;
                        EstablishModifier[species][ecoregion] = 1.0;
                        if (m_iParams.ANPPTimeCollection[ecoregion][species].TryGetValue(usetime + y, out anpp))
                        {
                            PlugIn.ModelCore.NormalDistribution.Mu = anpp.GramsPerMetre2Year;
                            PlugIn.ModelCore.NormalDistribution.Sigma = anpp.StdDev;
                            double sppANPP = PlugIn.ModelCore.NormalDistribution.NextDouble();
                            sppANPP = PlugIn.ModelCore.NormalDistribution.NextDouble();
                            //double sppANPP = NORMDIST(anpp.GramsPerMetre2Year, anpp.StdDev);
                            ANPP_MAX_Spp[species][ecoregion] += sppANPP;
                        }
                        else if (usetime < 0)  //then users didn't enter anything for all the spin-up time, and we must use the year 0 time.
                        {
                            if (m_iParams.ANPPTimeCollection[ecoregion][species].TryGetValue(0, out anpp))
                            {
                                if (!bWroteMsg1)
                                {
                                    PlugIn.ModelCore.UI.WriteLine("ANPP values were not entered for the earliest spin-up years. Year 0 values will be used.");
                                    bWroteMsg1 = true;
                                }
                                PlugIn.ModelCore.NormalDistribution.Mu = anpp.GramsPerMetre2Year;
                                PlugIn.ModelCore.NormalDistribution.Sigma = anpp.StdDev;
                                double sppANPP = PlugIn.ModelCore.NormalDistribution.NextDouble();
                                sppANPP = PlugIn.ModelCore.NormalDistribution.NextDouble();
                                //double sppANPP = NORMDIST(anpp.GramsPerMetre2Year, anpp.StdDev);
                                ANPP_MAX_Spp[species][ecoregion] += sppANPP;
                            }
                        }
                        IMaxBiomass maxbio;
                        if (m_iParams.MaxBiomassTimeCollection[ecoregion][species].TryGetValue(usetime + y, out maxbio))
                            B_MAX_Spp[species][ecoregion] = (int)maxbio.MaxBio;
                        else if (usetime < 0)   //then users didn't enter anything for all the spin-up time, and we must use the year 0 time.
                        {
                            if (!bWroteMsg2)
                            {
                                PlugIn.ModelCore.UI.WriteLine("MaxBiomass values were not entered for the earliest spin-up years. Year 0 values will be used.");
                                bWroteMsg2 = true;
                            }
                            if (m_iParams.MaxBiomassTimeCollection[ecoregion][species].TryGetValue(0, out maxbio))
                                B_MAX_Spp[species][ecoregion] = (int)maxbio.MaxBio;
                        }
                        //Establishmenet is only used in projection, not spin-up. So, don't check for values
                        //when usetime < 0
                        IEstabProb estprob;
                        if (usetime >= 0)
                        {
                            if (m_iParams.EstabProbTimeCollection[ecoregion][species].TryGetValue(usetime + y, out estprob))
                                EstablishProbability[species][ecoregion] = (double)estprob.Establishment;
                        }

                        //write output file only for acutal model run
                        if (usetime + y > 0)
                            log.WriteLine("{0}, {1}, {2}, {3:0.0}, {4:0.0}, {5:0.000}, {6:0.000} ", usetime + y, ecoregion.Name, species.Name, ANPP_MAX_Spp[species][ecoregion], B_MAX_Spp[species][ecoregion], MeanAnnualTemperature, EstablishProbability[species][ecoregion]);
                    }
                }
            }            
            EcoregionData.UpdateB_MAX();
            
            return;           
        }

        /*********************************************************************************************************************
        *  NORMDIST
        *  Description: return random value from a normal distrubution with given mean and standard deviation
        **********************************************************************************************************************/
        //static double NORMDIST(double mean, double std)
        //{
        //    double u1, u2, v1, v2, s, z1, z2;

        //    do {
        //        u1 = rnd1.NextDouble();
        //        u2 = rnd1.NextDouble();
        //        v1 = 2*u1 - 1;
        //        v2 = 2*u2 - 1;
        //        s = v1*v1 + v2*v2;
        //    } while (s > 1 || s==0); 

        //    z1 = Math.Sqrt(-2 * Math.Log(s) / s) * v1;
        //    z2 = Math.Sqrt(-2 * Math.Log(s) / s) * v2;
        //    double rand1 = (z1*std + mean);
        //    //rand2 = (z2*sigma2 + mean);
        //    return (rand1);
        //}

    }

}
