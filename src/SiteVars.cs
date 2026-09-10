//  Authors:  Robert Scheller, Brian Miranda, Jimm Domingo

//MG20260910 using Landis.Library.UniversalCohorts;
using Landis.Library.PnETCohorts;  //MG20260910
using Landis.SpatialModeling;

using System.Collections.Generic;

namespace Landis.Extension.DynamicFuels
{

    ///<summary>
    /// Site Variables for a fuels plug-in.
    /// </summary>
    public static class SiteVars
    {

        //---------------------------------------------------------------------

        public static void Initialize()
        {
            //MG20260910 Cohorts = PlugIn.ModelCore.GetSiteVar<SiteCohorts>("Succession.UniversalCohorts");
            Cohorts = PlugIn.ModelCore.GetSiteVar<Landis.Library.PnETCohorts.SiteCohorts>("Succession.CohortsPnET");  //MG20260910 
            if (Cohorts == null)
            {
                string mesg = string.Format("Cohorts are empty. Please double-check that this extension is compatible with your chosen succession extension.");
                throw new System.ApplicationException(mesg);
            }

            FuelType        = PlugIn.ModelCore.Landscape.NewSiteVar<int>();
            DecidFuelType   = PlugIn.ModelCore.Landscape.NewSiteVar<int>();
            PercentConifer  = PlugIn.ModelCore.Landscape.NewSiteVar<int>();
            PercentHardwood = PlugIn.ModelCore.Landscape.NewSiteVar<int>();
            PercentDeadFir  = PlugIn.ModelCore.Landscape.NewSiteVar<int>();

            TimeOfLastHarvest       = PlugIn.ModelCore.GetSiteVar<int>("Harvest.TimeOfLastEvent");
            HarvestPrescriptionName = PlugIn.ModelCore.GetSiteVar<string>("Harvest.PrescriptionName");
            HarvestCohortsKilled    = PlugIn.ModelCore.GetSiteVar<int>("Harvest.CohortsDamaged");

            TimeOfLastFire          = PlugIn.ModelCore.GetSiteVar<int>("Fire.TimeOfLastEvent");
            FireSeverity            = PlugIn.ModelCore.GetSiteVar<byte>("Fire.Severity");

            TimeOfLastWind          = PlugIn.ModelCore.GetSiteVar<int>("Wind.TimeOfLastEvent");
            WindSeverity            = PlugIn.ModelCore.GetSiteVar<byte>("Wind.Severity");

            NumberDeadFirCohorts    = PlugIn.ModelCore.GetSiteVar<Dictionary<int,int>>("BDA.NumCFSConifers");

            PlugIn.ModelCore.RegisterSiteVar(SiteVars.FuelType, "Fuels.CFSFuelType");
            PlugIn.ModelCore.RegisterSiteVar(SiteVars.DecidFuelType, "Fuels.DecidFuelType");
            PlugIn.ModelCore.RegisterSiteVar(SiteVars.PercentConifer, "Fuels.PercentConifer");
            PlugIn.ModelCore.RegisterSiteVar(SiteVars.PercentHardwood, "Fuels.PercentHardwood");
            PlugIn.ModelCore.RegisterSiteVar(SiteVars.PercentDeadFir, "Fuels.PercentDeadFir");

        }
        //---------------------------------------------------------------------

        public static ISiteVar<int> FuelType { get; set; }
        //---------------------------------------------------------------------

        public static ISiteVar<int> DecidFuelType { get; set; }
        //---------------------------------------------------------------------

        public static ISiteVar<int> PercentConifer { get; private set; }

        //---------------------------------------------------------------------

        public static ISiteVar<int> PercentHardwood { get; private set; }
        //---------------------------------------------------------------------

        public static ISiteVar<int> PercentDeadFir { get; private set; }
        //---------------------------------------------------------------------

        public static ISiteVar<string> HarvestPrescriptionName { get; private set; }
        //---------------------------------------------------------------------

        public static ISiteVar<int> TimeOfLastHarvest { get; private set; }
        //---------------------------------------------------------------------

        public static ISiteVar<int> HarvestCohortsKilled { get; private set; }
        //---------------------------------------------------------------------
        public static ISiteVar<int> TimeOfLastFire { get; private set; }
        //---------------------------------------------------------------------

        public static ISiteVar<byte> FireSeverity { get; private set; }
        //---------------------------------------------------------------------
        public static ISiteVar<int> TimeOfLastWind { get; private set; }
        //---------------------------------------------------------------------

        public static ISiteVar<byte> WindSeverity { get; private set; }
        //---------------------------------------------------------------------

        public static ISiteVar<Dictionary<int, int>> NumberDeadFirCohorts { get; private set; }

        //---------------------------------------------------------------------

        //MG20260910 public static ISiteVar<SiteCohorts> Cohorts
        public static ISiteVar<Landis.Library.PnETCohorts.SiteCohorts> Cohorts  //MG20260910
        { get; private set; }
    }
}
