//  Authors:  Caren Dymond, Sarah Beukema (based on code from Robert Scheller)


using Landis.Core;
using Landis.SpatialModeling;
using Landis.Utilities;
using System.IO;
using System;

using Landis.Library.Metadata;
using System.Data;
using System.Collections.Generic;
using System.Collections;



namespace Landis.Extension.Succession.ForC
{
    public class Outputs
    {
       
        public static void WriteMaps(string outpath, int outint)
        {
            string[] pathname = new string[7];
            int printval = 0;
            //pathname[0] = "ForCS/SoilC-{timestep}.img";
            //pathname[1] = "ForCS/BiomassC-{timestep}.img";
            //pathname[2] = "ForCS/NPP-{timestep}.img";
            //pathname[3] = "ForCS/NEP-{timestep}.img";
            //pathname[4] = "ForCS/NBP-{timestep}.img";
            //pathname[5] = "ForCS/RH-{timestep}.img";
            //pathname[6] = "ForCS/ToFPS-{timestep}.img";
            pathname[0] = outpath+"/SoilC-{timestep}.img";
            pathname[1] = outpath + "/BiomassC-{timestep}.img";
            pathname[2] = outpath + "/NPP-{timestep}.img";
            pathname[3] = outpath + "/NEP-{timestep}.img";
            pathname[4] = outpath + "/NBP-{timestep}.img";
            pathname[5] = outpath + "/RH-{timestep}.img";
            pathname[6] = outpath + "/ToFPS-{timestep}.img";

            for (int j = 0; j < 7; j++)
            {
                // Skip map outputs if ForCSMapControl table indicates so
                if (j == 0 && SoilVars.iParams.OutputSDOMC == 0)
                {
                    continue;
                }
                if (j == 1 && SoilVars.iParams.OutputBiomassC == 0)
                {
                    continue;
                }
                if (j == 2 && SoilVars.iParams.OutputNPP == 0)
                {
                    continue;
                }
                if (j == 3 && SoilVars.iParams.OutputNEP == 0)
                {
                    continue;
                }
                if (j == 4 && SoilVars.iParams.OutputNBP == 0)
                {
                    continue;
                }
                if (j == 5 && SoilVars.iParams.OutputRH == 0)
                {
                    continue;
                }
                if (j == 6 && SoilVars.iParams.OutputToFPS == 0)
                {
                    continue;
                }
                //string path1 = MapNames.ReplaceTemplateVars(@"ForCS\SoilC-{timestep}.img", PlugIn.ModelCore.CurrentTime);
                string path1 = MapNames.ReplaceTemplateVars(@pathname[j], PlugIn.ModelCore.CurrentTime);
                using (IOutputRaster<IntPixel> outputRaster = PlugIn.ModelCore.CreateRaster<IntPixel>(path1, PlugIn.ModelCore.Landscape.Dimensions))
                {
                    IntPixel pixel = outputRaster.BufferPixel;
                    foreach (Site site in PlugIn.ModelCore.Landscape.AllSites)
                    {
                        if (site.IsActive)
                        {
                            switch (j)      //put the right value into the variable that will be assigned to the map
                            {
                                case 0:
                                    printval = (int)((SiteVars.SoilOrganicMatterC[site]));
                                    break;
                                case 1:
                                    printval = (int)((SiteVars.TotBiomassC[site]));
                                    break;
                                case 2:
                                    printval = (int)((SiteVars.NPP[site]));
                                    break;
                                case 3:
                                    printval = (int)((SiteVars.NEP[site]));
                                    break;
                                case 4:
                                    printval = (int)((SiteVars.NBP[site]));
                                    break;
                                case 5:
                                    printval = (int)((SiteVars.RH[site]));
                                    break;
                                case 6:
                                    printval = (int)((SiteVars.ToFPSC[site]));
                                    break;
                                default:
                                    printval = 0;
                                    break;
                            }


                            //now print the value to the map
                            pixel.MapCode.Value = printval;
                        }
                        else
                        {
                            //  Inactive site
                            pixel.MapCode.Value = 0;
                        }
                        outputRaster.WriteBufferPixel();
                    }
                }
            }
        }
        
    }
}
