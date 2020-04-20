//  Authors:  Caren Dymond, Sarah Beukema

using Landis.SpatialModeling;
using Landis.Core;
using Landis.Library.BiomassCohorts;
using System.Collections.Generic;
using Landis.Utilities;
using System;

namespace Landis.Extension.Succession.ForC
{
    /// <summary>
    /// Calculations for an individual cohort's biomass.
    /// </summary>
    /// <remarks>
    /// References:
    /// <list type="">
    ///     <item>
    ///     Crow, T. R., 1978.  Biomass and production in three contiguous
    ///     forests in northern Wisconsin. Ecology 59(2):265-273.
    ///     </item>
    ///     <item>
    ///     Niklas, K. J., Enquist, B. J., 2002.  Canonical rules for plant
    ///     organ biomass partitioning and annual allocation.  Amer. J. Botany
    ///     89(5): 812-819.
    ///     </item>
    /// </list>
    /// </remarks>
    public class CohortBiomass
        : Landis.Library.BiomassCohorts.ICalculator
    {

        /// <summary>
        /// The single instance of the biomass calculations that is used by
        /// the plug-in.
        /// </summary>
        public static CohortBiomass Calculator;

        //  Ecoregion where the cohort's site is located
        private IEcoregion ecoregion;

        //  Ratio of actual biomass to maximum biomass for the cohort.
        private double B_AP;

        //  Ratio of potential biomass to maximum biomass for the cohort.
        private double B_PM;

        //  Totaly mortality without annual leaf litter for the cohort
        private int M_noLeafLitter;

        private double growthReduction;
        private double defoliation;

        public static int SubYear;
        public static double SpinupMortalityFraction;

        //---------------------------------------------------------------------

        public int MortalityWithoutLeafLitter
        {
            get
            {
                return M_noLeafLitter;
            }
        }

        //---------------------------------------------------------------------

        public CohortBiomass()
        {
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Computes the change in a cohort's biomass due to Annual Net Primary
        /// Productivity (ANPP), age-related mortality (M_AGE), and development-
        /// related mortality (M_BIO).
        /// </summary>
        public int ComputeChange(ICohort cohort,
                                 ActiveSite site)
         {
            ecoregion = PlugIn.ModelCore.Ecoregion[site];
            int siteBiomass = SiteVars.TotalBiomass[site]; 

            //Save the pre-growth root biomass. This needs to be calculated BEFORE growth and mortality
            double TotalRoots = Roots.CalculateRootBiomass(site, cohort.Species, cohort.Biomass);
            SiteVars.soilClass[site].CollectRootBiomass(TotalRoots, 0);

            // First, calculate age-related mortality.
            // Age-related mortality will include woody and standing leaf biomass (=0 for deciduous trees).
            double mortalityAge = ComputeAgeMortality(cohort);

            double actualANPP = ComputeActualANPP(cohort, site, siteBiomass, SiteVars.PreviousYearMortality[site]);

            //  Age mortality is discounted from ANPP to prevent the over-
            //  estimation of mortality.  ANPP cannot be negative.
            actualANPP = Math.Max(1, actualANPP - mortalityAge);

            //  Growth-related mortality
            double mortalityGrowth = ComputeGrowthMortality(cohort, site, siteBiomass);

            //  Age-related mortality is discounted from growth-related
            //  mortality to prevent the under-estimation of mortality.  Cannot be negative.
            mortalityGrowth = Math.Max(0, mortalityGrowth - mortalityAge);

            //  Also ensure that growth mortality does not exceed actualANPP.
            mortalityGrowth = Math.Min(mortalityGrowth, actualANPP);

            //  Total mortality for the cohort
            double totalMortality = mortalityAge + mortalityGrowth;

            if (totalMortality > cohort.Biomass)
                throw new ApplicationException("Error: Mortality exceeds cohort biomass");

            // Defoliation ranges from 1.0 (total) to none (0.0).
            defoliation = CohortDefoliation.Compute(cohort, site, siteBiomass);
            double defoliationLoss = 0.0;
            if (defoliation > 0)
            {
                double standing_nonwood = ComputeFractionANPPleaf(cohort.Species) * actualANPP;
                defoliationLoss = standing_nonwood * defoliation;
                SiteVars.soilClass[site].DisturbanceImpactsDOM(site, "defol", 0);  //just soil impacts. Dist impacts are handled differently??
            }

            int deltaBiomass = (int)(actualANPP - totalMortality - defoliationLoss);
            double newBiomass = cohort.Biomass + (double)deltaBiomass;

            double totalLitter = UpdateDeadBiomass(cohort, actualANPP, totalMortality, site, newBiomass);

            //if (site.Location.Row == 279 && site.Location.Column == 64 && cohort.Species.Name == "Pl")
            //{
            //    PlugIn.ModelCore.UI.WriteLine("Yr={0},  Age={1}, mortGrow={2}, mortAge={3}, ANPP={4}, totLitter={5}", PlugIn.ModelCore.CurrentTime, cohort.Age, mortalityGrowth, mortalityAge, actualANPP, totalLitter);
            // }

            //if (cohort.Species.Name == "pinubank")
            //{
                //PlugIn.ModelCore.UI.WriteLine("Age={0}, ANPPact={1:0.0}, M={2:0.0}, litter={3:0.00}.", cohort.Age, actualANPP, totalMortality, totalLitter);
                //PlugIn.ModelCore.UI.WriteLine("Name={0}, Age={1}, B={2}, ANPPact={3:0.0}, delta={4:0.0}", cohort.Species.Name, cohort.Age, cohort.Biomass, actualANPP, deltaBiomass);
            //    PlugIn.ModelCore.UI.WriteLine("Name={0}, Age={1}, B={2}, Mage={3:0.0}, Mgrowth={4:0.0}, ANPPact={5:0.0}, delta={6:0.0}, newbiomass={7:0.0}", cohort.Species.Name, cohort.Age, cohort.Biomass, mortalityAge, mortalityGrowth, actualANPP, deltaBiomass, newBiomass);
            //}
            //The KillNow flag indicates that this is the year of growth in which to kill off some cohorts in order to make snags.
            if (SiteVars.soilClass[site].bKillNow && Snags.bSnagsPresent)
            {
                //if (SiteVars.soilClass[site].diedAt == cohort.Age && SiteVars.soilClass[site].spIndex == cohort.Species.Index)
                //there could be more than one species-age combination, so we have to loop through them.
                //However, the user has been asked to put the ages in order from smallest to largest, so we can stop looking
                //as soon as we reach an age that is older than the cohort's age.
                for (int idx = 0; idx < Snags.NUMSNAGS; idx++)
                {
                    if (cohort.Age == Snags.DiedAt[idx] && Snags.initSpecIdx[idx] == cohort.Species.Index)
                    {
                        deltaBiomass = -cohort.Biomass; //set biomass to 0 to make core remove this from the list

                        //when this cohort gets passed to the cohort died event, there is no longer any biomass present, so
                        //we have to capture the biomass information here, while we still can.

                        double foliar = (double)cohort.ComputeNonWoodyBiomass(site);
                        double wood = ((double)cohort.Biomass - foliar);
                        Snags.bSnagsUsed[idx] = true;

                        SiteVars.soilClass[site].CollectBiomassMortality(cohort.Species, cohort.Age, wood, foliar, 5 + idx);
                    }
                    if (Snags.DiedAt[idx] > cohort.Age || Snags.DiedAt[idx] == 0) break; 
                }
            }
            if (deltaBiomass > -cohort.Biomass)
            {   //if we didn't kill this cohort to make a snag, then update the post-growth root biomass.
                TotalRoots = Roots.CalculateRootBiomass(site, cohort.Species, newBiomass);
                SiteVars.soilClass[site].CollectRootBiomass(TotalRoots, 1);
            }


            return deltaBiomass;
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Computes M_AGE_ij: the mortality caused by the aging of the cohort.
        /// See equation 6 in Scheller and Mladenoff, 2004.
        /// </summary>
        private double ComputeAgeMortality(ICohort cohort)
        {
            double max_age = (double)cohort.Species.Longevity;
            double d = SpeciesData.MortCurveShape[cohort.Species];

            double M_AGE = cohort.Biomass * Math.Exp((double)cohort.Age / max_age * d) / Math.Exp(d);

            M_AGE = Math.Min(M_AGE, cohort.Biomass);
           // if (cohort.Species.Name == "pinubank")
           //{
           //     PlugIn.ModelCore.UI.WriteLine("Name={0}, Age={1}, B={2}, Mage={3:0.0}, max_age={4:0.0}, d={5:0.0}", cohort.Species.Name, cohort.Age, cohort.Biomass, M_AGE, max_age, d);
           // }

            if (PlugIn.ModelCore.CurrentTime <= 0 && SpinupMortalityFraction > 0.0)
            {
                M_AGE += cohort.Biomass * SpinupMortalityFraction;
                if (PlugIn.CalibrateMode)
                    PlugIn.ModelCore.UI.WriteLine("Yr={0}. SpinupMortalityFraction={1:0.0000}, AdditionalMortality={2:0.0}, Spp={3}, Age={4}.", (PlugIn.ModelCore.CurrentTime + SubYear), SpinupMortalityFraction, (cohort.Biomass * SpinupMortalityFraction), cohort.Species.Name, cohort.Age);
            }

            return M_AGE;
        }

        //---------------------------------------------------------------------

        private double ComputeActualANPP(ICohort cohort,
                                         ActiveSite site,
                                         int siteBiomass,
                                         int prevYearSiteMortality)
        {
            growthReduction = CohortGrowthReduction.Compute(cohort, site);

            double cohortBiomass = cohort.Biomass;
            double capacityReduction = 1.0;
            double maxANPP = SpeciesData.ANPP_MAX_Spp[cohort.Species][ecoregion];
            //double maxBiomass = SpeciesData.B_MAX_Spp[cohort.Species][ecoregion];  //CForCS
            double maxBiomass = SpeciesData.B_MAX_Spp[cohort.Species][ecoregion] * capacityReduction;
            double growthShape = SpeciesData.GrowthCurveShapeParm[cohort.Species];

            if (SiteVars.CapacityReduction != null && SiteVars.CapacityReduction[site] > 0)
            {
                capacityReduction = 1.0 - SiteVars.CapacityReduction[site];
                if (PlugIn.CalibrateMode)
                    PlugIn.ModelCore.UI.WriteLine("Yr={0}. Capacity Remaining={1:0.00}, Spp={2}, Age={3} B={4}.", (PlugIn.ModelCore.CurrentTime + SubYear), capacityReduction, cohort.Species.Name, cohort.Age, cohort.Biomass);
            }

            double indexC = CalculateCompetition(site, cohort);   //Biomass model

            //if ((indexC <= 0.0 && cohortBiomass > 0) || indexC > 1.0)   //Biomass model
            //    throw new ApplicationException("Application terminating.");

            //  Potential biomass, equation 3 in Scheller and Mladenoff, 2004   
            //double potentialBiomass = Math.Max(1, maxBiomass - siteBiomass);  //old CForCS
            double potentialBiomass = Math.Max(1, maxBiomass - siteBiomass + cohortBiomass);  //Biomass model

            //  Species can use new space immediately
            //  but not in the case of capacity reduction due to harvesting.
            if (capacityReduction >= 1.0)
                potentialBiomass = Math.Max(potentialBiomass, prevYearSiteMortality);

            //  Ratio of cohort's actual biomass to potential biomass
            B_AP = Math.Min(1.0, cohortBiomass / potentialBiomass);

            //  Ratio of cohort's potential biomass to maximum biomass.  The
            //  ratio cannot be exceed 1.
            //B_PM = Math.Min(1.0, potentialBiomass / maxBiomass);     //old CForCS
            B_PM = Math.Min(1.0, indexC);   //Biomass model

            //  Actual ANPP: equation (4) from Scheller & Mladenoff, 2004.
            //  Constants k1 and k2 control whether growth rate declines with
            //  age.  Set to default = 1.
            //double actualANPP = maxANPP * Math.E * B_AP * Math.Exp(-1 * B_AP) * B_PM;   //old CForCS
            double actualANPP = maxANPP * Math.E * Math.Pow(B_AP, growthShape) * Math.Exp(-1 * Math.Pow(B_AP, growthShape)) * B_PM;   //Biomass model

            // Calculated actual ANPP can not exceed the limit set by the
            //  maximum ANPP times the ratio of potential to maximum biomass.
            //  This down regulates actual ANPP by the available growing space.

            actualANPP = Math.Min(maxANPP * B_PM, actualANPP);

            if (growthReduction > 0)
                actualANPP *= (1.0 - growthReduction);

            //if (site.Location.Row == 279 && site.Location.Column == 64 && cohort.Species.Name == "Pl")
            //{
            //    PlugIn.ModelCore.UI.WriteLine("Yr={0},  Age={1}, PotBio={2}, CohortBio={3}, maxBio={4}, siteBio={5}, ANPP={6}.", PlugIn.ModelCore.CurrentTime, cohort.Age, potentialBiomass, cohort.Biomass, maxBiomass, siteBiomass, actualANPP);
            //}
            
            return actualANPP;
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// The mortality caused by development processes,
        /// including self-thinning and loss of branches, twigs, etc.
        /// See equation 5 in Scheller and Mladenoff, 2004.
        /// </summary>
        private double ComputeGrowthMortality(ICohort cohort, ActiveSite site, int siteBiomass)
        {
            //double percentDefoliation = CohortDefoliation.Compute(cohort, site, siteBiomass);

            //const double y0 = 0.01;  //ForCS
            //const double r = 0.08;   //ForCS
            double M_BIO = 1.0;
            double maxANPP = SpeciesData.ANPP_MAX_Spp[cohort.Species][ecoregion];

            //double M_BIO = maxANPP *
            //        (y0 / (y0 + (1 - y0) * Math.Exp(-r / y0 * B_AP))) *
            //        B_PM;   //ForCS

            //Michaelis-Menton function (added by Scheller et all 2010):  //Biomass
            if (B_AP > 1.0)
                M_BIO = maxANPP * B_PM;
            else
                M_BIO = maxANPP * (2.0 * B_AP) / (1.0 + B_AP) * B_PM;

            //  Mortality should not exceed the amount of living biomass
            M_BIO = Math.Min(cohort.Biomass, M_BIO);

            //  Calculated actual ANPP can not exceed the limit set by the
            //  maximum ANPP times the ratio of potential to maximum biomass.
            //  This down regulates actual ANPP by the available growing space.

            M_BIO = Math.Min(maxANPP * B_PM, M_BIO);

            if (growthReduction > 0)
                M_BIO *= (1.0 - growthReduction);

            return M_BIO;

        }

        //---------------------------------------------------------------------

        private double UpdateDeadBiomass(ICohort cohort, double actualANPP, double totalMortality, ActiveSite site, double newBiomass)
        {
            ISpecies species = cohort.Species;
            double leafLongevity = SpeciesData.LeafLongevity[species];
            double cohortBiomass = newBiomass; // Mortality is for the current year's biomass.
            double leafFraction = ComputeFractionANPPleaf(species);

            // First, deposit the a portion of the leaf mass directly onto the forest floor.
            // In this way, the actual amount of leaf biomass is added for the year.
            // In addition, add the equivalent portion of fine roots to the surface layer.

            // 0.8 was used to calibrate the model to steady-state Nitrogen.  Without this reduction, total N
            // increases by 0.038% each year.  
            // Remove jan 2020 to more closely match code with the current Biomass succession model

            //double annualLeafANPP = actualANPP * leafFraction * 0.8;
            double annualLeafANPP = actualANPP * leafFraction;

            // --------------------------------------------------------------------------------
            // The next section allocates mortality from standing (wood and leaf) biomass, i.e., 
            // biomass that has accrued from previous years' growth.

            // Subtract annual leaf growth as that was taken care of above.            
            totalMortality -= annualLeafANPP;

            // Assume that standing foliage is equal to this years annualLeafANPP * leaf longevity
            // minus this years leaf ANPP.  This assumes that actual ANPP has been relatively constant 
            // over the past 2 or 3 years (if coniferous).

            double standing_nonwood = (annualLeafANPP * leafLongevity) - annualLeafANPP;
            double standing_wood = Math.Max(0, cohortBiomass - standing_nonwood);

            double fractionStandingNonwood = standing_nonwood / cohortBiomass;

            //  Assume that the remaining mortality is divided proportionally
            //  between the woody mass and non-woody mass (Niklaus & Enquist,
            //  2002).   Do not include current years growth.
            double mortality_nonwood = Math.Max(0.0, totalMortality * fractionStandingNonwood);
            double mortality_wood = Math.Max(0.0, totalMortality - mortality_nonwood);

            if (mortality_wood < 0 || mortality_nonwood < 0)
                throw new ApplicationException("Error: Woody input is < 0");

            //  Total mortality not including annual leaf litter
            M_noLeafLitter = (int)mortality_wood;

            SiteVars.soilClass[site].CollectBiomassMortality(species, cohort.Age, mortality_wood, (mortality_nonwood + annualLeafANPP), 0);

            //add root biomass information - now calculated based on both woody and non-woody biomass
            Roots.CalculateRootTurnover(site, species, cohortBiomass);
            SiteVars.soilClass[site].CollectBiomassMortality(species, cohort.Age, Roots.CoarseRootTurnover, Roots.FineRootTurnover, 1);

            //if biomass is going down, then we need to capture a decrease in the roots as well.
            if (cohortBiomass < cohort.Biomass)
            {
                double preMortRoots = Roots.CalculateRootBiomass(site, species, cohort.Biomass);
                double preMortCoarse = Roots.CoarseRoot;
                double preMortFine = Roots.FineRoot;
                double TotRoots = Roots.CalculateRootBiomass(site, species, cohortBiomass);
                if (preMortRoots > TotRoots)    //if the root biomass went down, then we need to allocate that difference.
                {   //We will allocate the total root decline to the different pools based on the relative proportions
                    //prior to the decline. (Note that we are not calculating actual declines for each type because
                    //sometimes if we are changing calculation methods, we may change the allocation and may cause a large 
                    //decrease in one pool and an increase in the other.)
                    double diffFine = (preMortFine / preMortRoots) * (preMortRoots - TotRoots);
                    double diffCoarse = (preMortRoots - TotRoots) - diffFine;
                    SiteVars.soilClass[site].CollectBiomassMortality(species, cohort.Age, diffCoarse, diffFine, 1);

                    //write a note to the file if the allocation changes unexpectedly, but not during spin-up
                    if (((preMortCoarse - Roots.CoarseRoot) < 0 || (preMortFine - Roots.FineRoot) < 0) && PlugIn.ModelCore.CurrentTime > 0)
                    {   
                        string strCombo = "from: " + preMortCoarse;
                        strCombo += " to: " + Roots.CoarseRoot;
                        string strCombo2 = "from: " + preMortFine;
                        strCombo2 += " to: " + Roots.FineRoot;
                        PlugIn.ModelCore.UI.WriteLine("Root Dynamics: Overall root biomass declined but note change in coarse root allocation " + strCombo + " and fine root allocation" + strCombo2);
                    }
                }
                else if (PlugIn.ModelCore.CurrentTime > 0)
                {
                    //write a note to the file if the root biomass increases while abio decreases, but not during spin-up
                    string strCombo = "from: " + cohort.Biomass;
                    strCombo += " to: " + cohortBiomass;
                    string strCombo2 = "from: " + preMortRoots;
                    strCombo2 += " to: " + TotRoots;
                    PlugIn.ModelCore.UI.WriteLine("Root Dynamics: Note that aboveground biomass decreased " + strCombo + " but root biomass increased " + strCombo2);
                }
            }

            if (PlugIn.ModelCore.CurrentTime == 0)
            {
                SiteVars.soilClass[site].CollectBiomassMortality(species, cohort.Age, standing_wood, standing_nonwood, 3);
                Roots.CalculateRootTurnover(site, species, (standing_wood + standing_nonwood));
                SiteVars.soilClass[site].CollectBiomassMortality(species, cohort.Age, Roots.CoarseRootTurnover, Roots.FineRootTurnover, 4);
            }

            return (annualLeafANPP + mortality_nonwood + mortality_wood);
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Computes the cohort's biomass that is leaf litter
        /// or other non-woody components.  Assumption is that remainder is woody.
        /// </summary>
        public static double ComputeStandingLeafBiomass(double ANPPactual, ICohort cohort)
        {

            double annualLeafFraction = ComputeFractionANPPleaf(cohort.Species);

            double annualFoliar = ANPPactual * annualLeafFraction;

            double B_nonwoody = annualFoliar * SpeciesData.LeafLongevity[cohort.Species];

            //  Non-woody cannot be less than 2.5% or greater than leaf fraction of total
            //  biomass for a cohort.
            B_nonwoody = Math.Max(B_nonwoody, cohort.Biomass * 0.025);
            B_nonwoody = Math.Min(B_nonwoody, cohort.Biomass * annualLeafFraction);

            return B_nonwoody;
        }
        //---------------------------------------------------------------------

        public static double ComputeFractionANPPleaf(ISpecies species)
        {

            //  A portion of growth goes to creating leaves (Niklas and Enquist 2002).
            //  Approximate for angio and conifer:
            //  pg. 817, growth (G) ratios for leaf:stem (Table 4) = 0.54 or 35% leaf

            double leafFraction = 0.35;

            //  Approximately 3.5% of aboveground production goes to early leaf
            //  fall, bud scales, seed production, and herbivores (Crow 1978).
            //leafFraction += 0.035;

            return leafFraction;
        }
        //---------------------------------------------------------------------

        /// <summary>
        /// Computes the percentage of a cohort's standing biomass that is non-woody.
        /// April 2010: changed to be a constant percentage of foliage, so that the 
        /// calculations of turnover give reasonable numbers.
        /// </summary>

        public Percentage ComputeNonWoodyPercentage(ICohort cohort,
                                                    ActiveSite site)
        {
            double leaf = 0.1;
            Percentage temp = new Percentage(leaf);
            return temp;
        }
        //---------------------------------------------------------------------

        /// <summary>
        /// Computes the initial biomass for a cohort at a site.
        /// </summary>
        public static int InitialBiomass(ISpecies species,
                                            SiteCohorts siteCohorts,
                                            ActiveSite site)
        {
            IEcoregion ecoregion = PlugIn.ModelCore.Ecoregion[site];
            //double B_ACT = ActualSiteBiomass(siteCohorts, site, out ecoregion);

            double B_ACT = 0.0;
            //Biomass code
            // Copied from Library.BiomassCohorts.Cohorts.cs, but added restriction that age > 1 (required when running 1-year timestep)
            foreach (ISpeciesCohorts speciesCohorts in siteCohorts)
            {
                foreach (ICohort cohort in speciesCohorts)
                {
                    if ((cohort.Age > 1))
                        B_ACT += cohort.Biomass;
                }
            }
            //if (siteCohorts != null)
            //    B_ACT = (double)Cohorts.ComputeNonYoungBiomass(siteCohorts);  //CForCS

            double maxBiomass = SpeciesData.B_MAX_Spp[species][ecoregion];
            double maxANPP = SpeciesData.ANPP_MAX_Spp[species][ecoregion];

            //  Initial biomass exponentially declines in response to competition.
            //double initialBiomass = 0.02 * maxBiomass * Math.Exp(-1.6 * B_ACT / EcoregionData.B_MAX[ecoregion]);  //ForCS
            double initialBiomass = maxANPP * Math.Exp(-1.6 * B_ACT / EcoregionData.B_MAX[ecoregion]);     //Biomass

            // Initial biomass cannot be greater than maxANPP
            initialBiomass = Math.Min(maxANPP, initialBiomass);

            //  Initial biomass cannot be less than 2.  C. Dymond issue from August 2016
            initialBiomass = Math.Max(2.0, initialBiomass);

            return (int)initialBiomass;
        }

        //---------------------------------------------------------------------
        // New method for calculating competition limits.
        // Iterates through cohorts, assigning each a competitive efficiency

        private static double CalculateCompetition(ActiveSite site, ICohort cohort)
        {

            double competitionPower = 0.95;
            //double CMultiplier = Math.Max(Math.Sqrt(cohort.Biomass), 1.0);
            double CMultiplier = Math.Max(Math.Pow(cohort.Biomass, competitionPower), 1.0);
            double CMultTotal = CMultiplier;

            foreach (ISpeciesCohorts speciesCohorts in SiteVars.Cohorts[site])
                foreach (ICohort xcohort in speciesCohorts)
                {
                    if (xcohort.Age + 1 != cohort.Age || xcohort.Species.Index != cohort.Species.Index)
                    {
                        double tempCMultiplier = Math.Max(Math.Pow(xcohort.Biomass, competitionPower), 1.0);
                        CMultTotal += tempCMultiplier;
                    }
                }

            double Cfraction = CMultiplier / CMultTotal;

            return Cfraction;
        }

    }
}
