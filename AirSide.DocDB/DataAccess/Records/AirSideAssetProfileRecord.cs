using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirSide.DocDB.DataAccess.Records
{
    public class AirSideAssetProfileRecord
    {
        public int AssetId { get; set; }
        public int LocationId { get; set; }
        public int AssetClassId { get; set; }
        public string RfidTag { get; set; }
        public string SerialNumber { get; set; }
        public bool Status { get; set; }
        public string ProductUrl { get; set; }

        public Location Location { get; set; }
        public AssetClass AssetClass { get; set; }
        public Picture Picture { get; set; }
        public Maintenance[] Maintenance { get; set; }
    }


    public class Maintenance
    {
        public string MaintenanceTask { get; set; }
        public string PreviousDate { get; set; }
        public string NextDate { get; set; }
        public int MaintenanceCycle { get; set; }
        public int MaintenanceId { get; set; }
    }

    public class Location
    {
        public int LocationId { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public string Designation { get; set; }
        public int AreaSubId { get; set; }
        public int AreaId { get; set; }
    }

    public class AssetClass
    {
        public int AssetClassId { get; set; }
        public string Description { get; set; }
        public int PictureId { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public int FrequencyId { get; set; }
    }

    public class Picture
    {
        public int PictureId { get; set; }
        public string FileLocation { get; set; }
        public string Description { get; set; }
    }
}
