using ADB.AirSide.Encore.V1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ADB.AirSide.Encore.V1.App_Helpers
{
    public class DatabaseHelper : IDisposable
    {
        private Entities db = new Entities();

        #region Functions

        public int getAssetCountPerSubArea(int subArea)
        {
            int assetCount = (from x in db.as_assetProfile
                              join y in db.as_locationProfile on x.i_locationId equals y.i_locationId
                              join z in db.as_areaSubProfile on y.i_areaSubId equals z.i_areaSubId
                              where z.i_areaSubId == subArea
                              select x).Count();
            return assetCount;
        }

        public int getCompletedAssetsForShift(int shiftId)
        {
            int shiftCount = (from x in db.as_shiftData where x.i_shiftId == shiftId select x).GroupBy(q => q.i_assetId).Count();
            return shiftCount;
        }

        public int getAssetCountPerArea(int areaId)
        {
            int assetCount = (from x in db.as_assetProfile
                              join y in db.as_locationProfile on x.i_locationId equals y.i_locationId
                              join z in db.as_areaSubProfile on y.i_areaSubId equals z.i_areaSubId
                              where z.i_areaId == areaId
                              select x).Count();
            return assetCount;
        }

        public DateTime getFirstMaintanedDate(int assetId)
        {
            DateTime newDate = new DateTime(1970, 1, 1);
            DateTime firstDate = (from x in db.as_shiftData
                                  join y in db.as_assetProfile on x.i_assetId equals y.i_assetId
                                  select x.dt_captureDate).DefaultIfEmpty(newDate).First();
            return firstDate;
        }

        public int getFrequencyColourForAsset(int assetId)
        {
            //Calculate the day difference 
            var lastDate = (from x in db.as_shiftData
                            join y in db.as_assetProfile on x.i_assetId equals y.i_assetId
                            where x.i_assetId == assetId
                            select x).OrderByDescending(q => q.dt_captureDate).FirstOrDefault();
            int daysDiff = 0;

            if (lastDate != null)
                daysDiff = (DateTime.Now - lastDate.dt_captureDate).Days;
            else
                daysDiff = -1;

            //Get the maintenance frequency
            double frequency = 0;
            if (daysDiff > -1)
            {
                frequency = (from x in db.as_assetProfile
                             join y in db.as_assetClassProfile on x.i_assetClassId equals y.i_assetClassId
                             join z in db.as_frequencyProfile on y.i_frequencyId equals z.i_frequencyId
                             where x.i_assetId == assetId
                             select z.f_frequency).FirstOrDefault();
            }

            int color = (int)maintenanceColours.grey;

            //calculate the color
            if (daysDiff == -1) color = (int)maintenanceColours.grey;
            else if (daysDiff <= (frequency / 3)) color = (int)maintenanceColours.green;
            else if ((daysDiff > (frequency / 3)) && (daysDiff <= (frequency / 1.5))) color = (int)maintenanceColours.yellow;
            else if ((daysDiff > (frequency / 1.5)) && (daysDiff <= frequency)) color = (int)maintenanceColours.orange;
            else color = (int)maintenanceColours.red;

            return color;
        }

        public string getLastAssetMaintenanceType(int assetId)
        {
            var maintenanceType = (from x in db.as_shiftData
                                   join y in db.as_maintenanceProfile on x.i_maintenanceId equals y.i_maintenanceId
                                   where x.i_assetId == assetId
                                   select new
                                   {
                                       dateStamp = x.dt_captureDate,
                                       description = y.vc_description
                                   }).OrderByDescending(q => q.dateStamp).FirstOrDefault();
            return maintenanceType.description;
        }

        public DateTime getLastDateForSubArea(int areaSubId)
        {
            var firstDate = (from x in db.as_areaProfile
                             join y in db.as_areaSubProfile on x.i_areaId equals y.i_areaId
                             join z in db.as_locationProfile on y.i_areaSubId equals z.i_areaSubId
                             join a in db.as_assetProfile on z.i_locationId equals a.i_locationId
                             join b in db.as_shiftData on a.i_assetId equals b.i_assetId
                             where x.i_areaId == areaSubId
                             select b).OrderBy(q => q.dt_captureDate).FirstOrDefault();

            DateTime returnDate = new DateTime(1970, 1, 1);
            if (firstDate != null) returnDate = firstDate.dt_captureDate;
            return returnDate;
        }

        public DateTime getLastMaintanedDate(int assetId)
        {
            var lastDate = (from x in db.as_shiftData
                            join y in db.as_assetProfile on x.i_assetId equals y.i_assetId
                            where x.i_assetId == assetId
                            select x).OrderByDescending(q => q.dt_captureDate).FirstOrDefault();

            DateTime returnDate = new DateTime(1970, 1, 1);
            if (lastDate != null)
                returnDate = lastDate.dt_captureDate;
            return returnDate;
        }

        public string getLastShiftDateForAsset(int assetId)
        {
            var lastDate = (from x in db.as_shiftData
                            where x.i_assetId == assetId
                            select x).OrderByDescending(q => q.dt_captureDate).FirstOrDefault();

            string returnDate = ("---");
            if (lastDate != null)
                returnDate = lastDate.dt_captureDate.ToString("yyyMMdd");

            return returnDate;
        }

        public DateTime getNextMaintenanceDate(int assetId)
        {
            var nextDate = (from x in db.as_shiftData
                            join y in db.as_assetProfile on x.i_assetId equals y.i_assetId
                            join z in db.as_assetClassProfile on y.i_assetClassId equals z.i_assetClassId
                            join a in db.as_frequencyProfile on z.i_frequencyId equals a.i_frequencyId
                            where x.i_assetId == assetId
                            select new
                            {
                                captureDate = x.dt_captureDate,
                                frequency = a.f_frequency
                            }).OrderByDescending(q => q.captureDate).FirstOrDefault();

            if (nextDate != null)
                return (nextDate.captureDate.AddDays(nextDate.frequency));
            else
            {
                DateTime newDate = new DateTime(1970, 1, 1);
                return newDate;
            }
        }

        public int getNumberOfFixingPoints(int assetClassId)
        {
            var fixingPoints = (from x in db.as_assetInfoProfile
                                where x.vc_description == "Fixing Points"
                                select x).FirstOrDefault();
            if (fixingPoints != null)
                return int.Parse(fixingPoints.vc_value);
            else
                return 0;
        }

        public Boolean getSubmittedShiftData(int assetId)
        {
            var completed = (from x in db.as_shiftData
                             join y in db.as_shifts on x.i_shiftId equals y.i_shiftId
                             where y.bt_completed == true && x.i_assetId == assetId
                             select x).FirstOrDefault();
            if (completed != null)
                return true;
            else
                return false;
        }


        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                db.Dispose();
            }
            // free native resources
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}