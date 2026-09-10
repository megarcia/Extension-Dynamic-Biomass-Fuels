//  Authors:  Robert Scheller, Brian Miranda, Jimm Domingo

using Landis.Core;
using Landis.SpatialModeling;
//MG20260910 using Landis.Library.UniversalCohorts;
using Landis.Library.PnETCohorts;  //MG20260910

using System.Collections.Generic;

namespace Landis.Extension.DynamicFuels
{
    public class PlugIn
        : ExtensionMain
    {
        public static readonly ExtensionType extType = new ExtensionType("disturbance:fuels");
        public static readonly string ExtensionName = "Dynamic Fuel System";

        private string mapNameTemplate;
        private string pctConiferMapNameTemplate;
        private string pctDeadFirMapNameTemplate;
        private IEnumerable<IFuelType> fuelTypes;
        private IEnumerable<IDisturbanceType> disturbanceTypes;
        private double[] fuelCoefs;
        private int hardwoodMax;
        private int deadFirMaxAge;

        private static IInputParameters parameters;
        private static ICore modelCore;

        //---------------------------------------------------------------------

        public PlugIn()
            : base(ExtensionName, extType)
        {
        }

        //---------------------------------------------------------------------

        public static ICore ModelCore
        {
            get
            {
                return modelCore;
            }
        }
        
        //---------------------------------------------------------------------

        public override void LoadParameters(string dataFile,
                                            ICore mCore)
        {
            modelCore = mCore;
            InputParametersParser parser = new InputParametersParser();
            parameters = Landis.Data.Load<IInputParameters>(dataFile, parser);

        }

        //---------------------------------------------------------------------

        public override void Initialize()
        {

            Timestep                    = parameters.Timestep;
            mapNameTemplate             = parameters.MapFileNames;
            pctConiferMapNameTemplate   = parameters.PctConiferFileName;
            pctDeadFirMapNameTemplate   = parameters.PctDeadFirFileName;
            fuelTypes                   = parameters.FuelTypes;
            disturbanceTypes            = parameters.DisturbanceTypes;
            fuelCoefs                   = parameters.FuelCoefficients;
            hardwoodMax                 = parameters.HardwoodMax;
            deadFirMaxAge               = parameters.DeadFirMaxAge;


            MetadataHandler.InitializeMetadata(
                Timestep,
                mapNameTemplate,
                pctConiferMapNameTemplate,
                pctDeadFirMapNameTemplate);

            SiteVars.Initialize();
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Runs the component for a particular timestep.
        /// </summary>
        /// <param name="currentTime">
        /// The current model timestep.
        /// </param>
        public override void Run()
        {

            modelCore.UI.WriteLine("  Re-initializing all values to zero...");
            SiteVars.FuelType.ActiveSiteValues = 0;
            SiteVars.DecidFuelType.ActiveSiteValues = 0;

            modelCore.UI.WriteLine("  Calculating the Fuel Type Index for all active cells...");
            foreach (ActiveSite site in modelCore.Landscape)  //ActiveSites
            {
                CalcFuelType(site, fuelTypes, disturbanceTypes);
                SiteVars.PercentDeadFir[site] = CalcPercentDeadFir(site);
            }

            string path = MapNames.ReplaceTemplateVars(mapNameTemplate, modelCore.CurrentTime);
            modelCore.UI.WriteLine("   Writing Fuel map to {0}...", path);
            using (IOutputRaster<BytePixel> outputRaster = modelCore.CreateRaster<BytePixel>(path, modelCore.Landscape.Dimensions))
            {
                BytePixel pixel = outputRaster.BufferPixel;
                foreach (Site site in modelCore.Landscape.AllSites)
                {
                    if (site.IsActive)
                        pixel.MapCode.Value = (byte) ((int) SiteVars.FuelType[site] + 1);
                    else
                        pixel.MapCode.Value = 0;
                    
                    outputRaster.WriteBufferPixel();
                }
            }

            string conpath = MapNames.ReplaceTemplateVars(pctConiferMapNameTemplate, modelCore.CurrentTime);
            modelCore.UI.WriteLine("   Writing % Conifer map to {0} ...", conpath);
            using (IOutputRaster<BytePixel> outputRaster = modelCore.CreateRaster<BytePixel>(conpath, modelCore.Landscape.Dimensions))
            {
                BytePixel pixel = outputRaster.BufferPixel;
                foreach (Site site in modelCore.Landscape.AllSites)
                {
                    if (site.IsActive)
                        pixel.MapCode.Value = (byte)((int)SiteVars.PercentConifer[site]);
                    else
                        pixel.MapCode.Value = 0;
                    
                    outputRaster.WriteBufferPixel();
                }
            }

            string firpath = MapNames.ReplaceTemplateVars(pctDeadFirMapNameTemplate, modelCore.CurrentTime);
            modelCore.UI.WriteLine("   Writing % Dead Fir map to {0} ...", firpath);
            using (IOutputRaster<BytePixel> outputRaster = modelCore.CreateRaster<BytePixel>(firpath, modelCore.Landscape.Dimensions))
            {
                BytePixel pixel = outputRaster.BufferPixel;
                foreach (Site site in modelCore.Landscape.AllSites)
                {
                    if (site.IsActive)
                        pixel.MapCode.Value = (byte)((int)SiteVars.PercentDeadFir[site]);
                    else
                        pixel.MapCode.Value = 0;
                    
                    outputRaster.WriteBufferPixel();
                }
            }
        }

        //---------------------------------------------------------------------

        private int CalcFuelType(Site site,
                                 IEnumerable<IFuelType> FuelTypes,
                                 IEnumerable<IDisturbanceType> DisturbanceTypes)
        {

            double[] forTypValue = new double[100];  //Maximum of 100 fuel types
            double sumConifer = 0.0;
            double sumDecid = 0.0;
            IEcoregion ecoregion = modelCore.Ecoregion[(ActiveSite) site];
            
            foreach(ISpecies species in modelCore.Species)
            {

                //MG20260910 ISpeciesCohorts speciesCohorts = SiteVars.Cohorts[site][species];
                ISpeciesCohorts speciesCohorts = (ISpeciesCohorts)SiteVars.Cohorts[site][species];  //MG20260910

                if(speciesCohorts == null)
                {
                    modelCore.UI.WriteLine("found a null cohort");
                    continue;
                }

                foreach(IFuelType ftype in FuelTypes)
                {
                    if(ftype.Ecoregions[ecoregion.Index])
                    {

                        if(ftype[species.Index] != 0)
                        {
                            int sppValue = 0;

                            foreach(ICohort cohort in speciesCohorts)
                            {
                                //MG20260910 if(cohort.Data.Age >= ftype.MinAge && cohort.Data.Age <= ftype.MaxAge)
                                if(cohort.Data.UniversalData.Age >= ftype.MinAge && cohort.Data.UniversalData.Age <= ftype.MaxAge)  //MG20260910
                                {
                                    //MG20250910 sppValue += cohort.Data.UniversalData.Biomass;
                                    sppValue += cohort.Data.UniversalData.Biomass;  //MG20250910 
                                }
                                //MG20250910 modelCore.UI.WriteLine("sppName={0}, cohortAge={1}, cohortBiomass={2}, sppValue={3}", cohort.Species.Name, cohort.Data.Age, cohort.Data.Biomass, sppValue);
                                modelCore.UI.WriteLine("sppName={0}, cohortAge={1}, cohortBiomass={2}, sppValue={3}", cohort.Species.Name, cohort.Data.UniversalData.Age, cohort.Data.UniversalData.Biomass, sppValue);  //MG20250910 
                            }

                            if(ftype[species.Index] == -1)
                                forTypValue[ftype.Index] -= sppValue;

                            if(ftype[species.Index] == 1)
                                forTypValue[ftype.Index] += sppValue;
                        }
                    }
                }

            }



            int finalFuelType = 0;
            int decidFuelType = 0;
            int coniferFuelType = 0;
            int openFuelType = 0;
            int slashFuelType = 0;
            double maxValue = 0.0;
            double maxDecidValue = 0.0;
            double maxConiferValue = 0.0;
            double maxConPlantValue = 0.0;
            double maxOpenValue = 0.0;
            double maxSlashValue = 0.0;

            //Set the PERCENT CONIFER DOMINANCE:
            int coniferDominance = 0;
            int hardwoodDominance = 0;


            //First accumulate data for the various Base Fuel Types:
            foreach(IFuelType ftype in FuelTypes)
            {
                if(ftype != null)
                {

                    // Sum for the Conifer and Deciduous dominance
                    if ((ftype.BaseFuel == BaseFuelType.Conifer || ftype.BaseFuel == BaseFuelType.ConiferPlantation)
                        && forTypValue[ftype.Index] > 0)
                    {
                        sumConifer += forTypValue[ftype.Index];
                    }

                    //This is calculated for the mixed types:
                    if ((ftype.BaseFuel == BaseFuelType.Deciduous)
                        && forTypValue[ftype.Index] > 0)
                    {
                        sumDecid += forTypValue[ftype.Index];
                    }

                    // CONIFER
                    if(forTypValue[ftype.Index] > maxConiferValue && ftype.BaseFuel == BaseFuelType.Conifer)
                    {

                        maxConiferValue = forTypValue[ftype.Index];
                        if(maxConiferValue > maxConPlantValue)
                            coniferFuelType = ftype.Index;
                    }
                    // CONIFER PLANTATION
                    if (forTypValue[ftype.Index] > maxConPlantValue && ftype.BaseFuel == BaseFuelType.ConiferPlantation)
                    {

                        maxConPlantValue = forTypValue[ftype.Index];
                        if(maxConPlantValue > maxConiferValue)
                            coniferFuelType = ftype.Index;
                    }

                    // OPEN
                    if (forTypValue[ftype.Index] > maxOpenValue && ftype.BaseFuel == BaseFuelType.Open)
                    {

                        maxOpenValue = forTypValue[ftype.Index];
                        openFuelType = ftype.Index;
                    }

                    // SLASH
                    if (forTypValue[ftype.Index] > maxSlashValue && ftype.BaseFuel == BaseFuelType.Slash)
                    {

                        maxSlashValue = forTypValue[ftype.Index];
                        slashFuelType = ftype.Index;
                    }

                    // DECIDUOUS
                    if(forTypValue[ftype.Index] > maxDecidValue && ftype.BaseFuel == BaseFuelType.Deciduous)
                    {

                        maxDecidValue = forTypValue[ftype.Index];
                        decidFuelType = ftype.Index;
                    }

                }
            }

            // Rules indicating a CONIFER cell
            if (maxConiferValue >= maxConPlantValue
                && maxConiferValue >= maxDecidValue
                && maxConiferValue >= maxOpenValue
                && maxConiferValue >= maxSlashValue)
            {
                maxValue = maxConiferValue;
            }

            // Rules indicating a DECIDUOUS cell
            else if (maxDecidValue >= maxConiferValue
                    && maxDecidValue >= maxConPlantValue
                    && maxDecidValue >= maxOpenValue
                    && maxDecidValue >= maxSlashValue)
            {
                maxValue = maxDecidValue;
            }

            // Rules indicating a CONIFER PLANTATION cell
            else if (maxConPlantValue >= maxConiferValue
                    && maxConPlantValue >= maxDecidValue
                    && maxConPlantValue >= maxOpenValue
                    && maxConPlantValue >= maxSlashValue)
            {
                maxValue = maxConPlantValue;
                finalFuelType = coniferFuelType;
                decidFuelType = 0;
                sumConifer = 100;
                sumDecid = 0;
            }

            // Rules indicating a SLASH cell
            else if (maxSlashValue >= maxConiferValue
                    && maxSlashValue >= maxConPlantValue
                    && maxSlashValue >= maxDecidValue
                    && maxSlashValue >= maxOpenValue)
            {
                maxValue = maxSlashValue;
                finalFuelType = slashFuelType;
                decidFuelType = 0;
                sumConifer = 0;
                sumDecid = 0;
            }

            // Rules indicating an OPEN (typically grass) cell
            else if (maxOpenValue >= maxConiferValue
                    && maxOpenValue >= maxConPlantValue
                    && maxOpenValue >= maxDecidValue
                    && maxOpenValue >= maxSlashValue)
            {
                maxValue = maxOpenValue;
                finalFuelType = openFuelType;
                decidFuelType = 0;
                sumConifer = 0;
                sumDecid = 0;
            }


            //Set the PERCENT DOMINANCE values:
            if (sumConifer > 0 || sumDecid > 0)
            {
                coniferDominance = (int)((sumConifer / (sumConifer + sumDecid) * 100) + 0.5);
                hardwoodDominance = (int)((sumDecid / (sumConifer + sumDecid) * 100) + 0.5);
                if (hardwoodDominance < hardwoodMax)
                {
                    coniferDominance = 100;
                    hardwoodDominance = 0;
                    finalFuelType = coniferFuelType;
                    decidFuelType = 0;
                }
                if (coniferDominance < hardwoodMax)
                {
                    coniferDominance = 0;
                    hardwoodDominance = 100;
                    finalFuelType = decidFuelType;
                    decidFuelType = 0;
                }
                if (hardwoodDominance > hardwoodMax && coniferDominance > hardwoodMax)
                    finalFuelType = coniferFuelType;
            }

            //---------------------------------------------------------------------
            // Next check the disturbance types.  This will override any other existing fuel type.
            foreach(DisturbanceType slash in DisturbanceTypes)
            {
                //if (SiteVars.HarvestCohortsKilled != null && SiteVars.HarvestCohortsKilled[site] > 0)
                //{
                    if (SiteVars.TimeOfLastHarvest != null &&
                        (modelCore.CurrentTime - SiteVars.TimeOfLastHarvest[site] <= slash.MaxAge))
                    {
                        foreach (string pName in slash.PrescriptionNames)
                        {
                            if (SiteVars.HarvestPrescriptionName != null && SiteVars.HarvestPrescriptionName[site].Trim() == pName.Trim())
                            {
                                finalFuelType = slash.FuelIndex; //Name;
                                decidFuelType = 0;
                                coniferDominance = 0;
                                hardwoodDominance = 0;
                            }
                        }
                    }
                //}
                //Check for fire severity effects of fuel type
                if (SiteVars.FireSeverity != null && SiteVars.FireSeverity[site] > 0)
                {
                    if (SiteVars.TimeOfLastFire != null &&
                        (modelCore.CurrentTime - SiteVars.TimeOfLastFire[site] <= slash.MaxAge))
                    {
                        foreach (string pName in slash.PrescriptionNames)
                        {
                            if (pName.StartsWith("FireSeverity"))
                            {
                                if((pName.Substring((pName.Length - 1), 1)).ToString() == SiteVars.FireSeverity[site].ToString())
                                {
                                    finalFuelType = slash.FuelIndex; //Name;
                                    decidFuelType = 0;
                                    coniferDominance = 0;
                                    hardwoodDominance = 0;
                                }
                            }
                        }
                    }
                }
                //Check for wind severity effects of fuel type
                if (SiteVars.WindSeverity != null && SiteVars.WindSeverity[site] > 0)
                {
                    if (SiteVars.TimeOfLastWind != null &&
                        (modelCore.CurrentTime - SiteVars.TimeOfLastWind[site] <= slash.MaxAge))
                    {
                        foreach (string pName in slash.PrescriptionNames)
                        {
                            if (pName.StartsWith("WindSeverity"))
                            {
                                if ((pName.Substring((pName.Length - 1), 1)).ToString() == SiteVars.WindSeverity[site].ToString())
                                {
                                    finalFuelType = slash.FuelIndex; //Name;
                                    decidFuelType = 0;
                                    coniferDominance = 0;
                                    hardwoodDominance = 0;
                                }
                            }
                        }
                    }
                }
            }

            SiteVars.PercentConifer[site]   = coniferDominance;
            SiteVars.PercentHardwood[site]  = hardwoodDominance;

            SiteVars.FuelType[site]         = finalFuelType;
            SiteVars.DecidFuelType[site]    = decidFuelType;

            return finalFuelType;

        }

        private int CalcPercentDeadFir(ActiveSite site)
        {

            int numDeadFir = 0;

            if(SiteVars.NumberDeadFirCohorts == null)
                return 0;

            int minimumStartTime = System.Math.Max(0, SiteVars.TimeOfLastFire[site]);
            for(int i = minimumStartTime; i <= modelCore.CurrentTime; i++)
            {
                if(modelCore.CurrentTime - i <= deadFirMaxAge)
                    if(SiteVars.NumberDeadFirCohorts[site].ContainsKey(i))
                        numDeadFir += SiteVars.NumberDeadFirCohorts[site][i];
            }

            int numSiteCohorts = 0;
            int percentDeadFir = 0;

            foreach (ISpecies species in modelCore.Species)
            {

                //MG20260910 ISpeciesCohorts speciesCohorts = SiteVars.Cohorts[site][species];
                ISpeciesCohorts speciesCohorts = (ISpeciesCohorts)SiteVars.Cohorts[site][species];  //MG20260910 
                if (speciesCohorts == null)
                    continue;
                foreach (ICohort cohort in speciesCohorts)
                    numSiteCohorts++;
            }


            percentDeadFir = (int) ( ((double) numDeadFir / (double) (numSiteCohorts + numDeadFir)) * 100.0 + 0.5);


            return System.Math.Min(percentDeadFir, 100);
        }


        // Not Implemented; Use to add unique parameters to cohort.data
        public override void AddCohortData()
        {
            return;
        }
    }
}
