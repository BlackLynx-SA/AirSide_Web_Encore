using ADB.AirSide.Encore.V1.Models;
using MongoDB.Bson;
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

        public BsonArray getMaintenaceTasks(int assetId)
        {
            var asset = (from x in db.as_assetProfile
                         join y in db.as_assetClassProfile on x.i_assetClassId equals y.i_assetClassId
                         join z in db.as_assetClassMaintenanceProfile on y.i_assetClassId equals z.i_assetClassId
                         join a in db.as_frequencyProfile on z.i_frequencyId equals a.i_frequencyId
                         join b in db.as_maintenanceProfile on z.i_maintenanceId equals b.i_maintenanceId
                         where x.i_assetId == assetId
                         select new { 
                            frequency = a.f_frequency,
                            task = b.vc_description,
                            maintencanceId = b.i_maintenanceId,
                         });

            BsonArray maintenanceArray = new BsonArray();
            foreach(var item in asset)
            {
                BsonDocument maintenanceTask = new BsonDocument();
                maintenanceTask.Add("maintenanceTask", item.task);
                DateTime previousDate = getMaintenanceLastDate(item.maintencanceId, assetId);
                maintenanceTask.Add("previousDate", previousDate.ToString("yyy/MM/dd"));
                maintenanceTask.Add("nextDate", getMaintenanceNextDate(item.maintencanceId, assetId, item.frequency).ToString("yyy/MM/dd"));
                maintenanceTask.Add("maintenanceCycle", getMaintenanceColour(previousDate, item.frequency));
                maintenanceTask.Add("maintenanceId", item.maintencanceId);
                maintenanceArray.Add(maintenanceTask);
            }

            return maintenanceArray;
        }

        public DateTime getMaintenanceLastDate(int maintenanceId, int assetId)
        {
            try
            {
                //Get the last maintained date for a asset
                //Create Date: 2014/12/09
                //Author: Bernard Willer

                Boolean shiftFlag = true;
                Boolean validationFlag = true;

                DateTime shiftDate = DateTime.Now;
                DateTime valDate = DateTime.Now;
                DateTime returnDate = new DateTime(1970, 1, 1);

                //Get the last shift date for asset
                var lastDate = (from x in db.as_shiftData
                                join y in db.as_assetProfile on x.i_assetId equals y.i_assetId
                                where x.i_assetId == assetId && x.i_maintenanceId == maintenanceId
                                select x).OrderByDescending(q => q.dt_captureDate).FirstOrDefault();

                if (lastDate == null)
                    shiftFlag = false;
                else
                    shiftDate = lastDate.dt_captureDate;


                //get last validation date for asset
                var lastValDate = (from x in db.as_validationTaskProfile
                                   where x.i_assetId == assetId
                                   select x).OrderByDescending(q => q.dt_dateTimeStamp).FirstOrDefault();

                if (lastValDate == null)
                    validationFlag = false;
                else
                    valDate = lastValDate.dt_dateTimeStamp;

                //get the latest date
                if (shiftFlag && validationFlag)
                {
                    if (shiftDate > valDate)
                        returnDate = shiftDate;
                    else
                        returnDate = valDate;
                }
                else if (shiftFlag)
                {
                    returnDate = shiftDate;
                }
                else if (validationFlag)
                {
                    returnDate = valDate;
                }

                return returnDate;

            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.logError(err, "SYSTEM Cache");
                return new DateTime(1970, 1, 1);
            }
        
        }

        public DateTime getMaintenanceNextDate(int maintenanceId, int assetId, double frequency)
        {
            //Gets the next maintenance date for a asset
            //Create Date: 2014/12/09
            //Author: Bernard Willer

            try
            {
                Boolean shiftFlag = true;
                Boolean validationFlag = true;

                DateTime shiftDate = DateTime.Now;
                DateTime valDate = DateTime.Now;
                DateTime returnDate = new DateTime(1970, 1, 1);

                //Get the last shift date for asset
                var lastDate = (from x in db.as_shiftData
                                join y in db.as_assetProfile on x.i_assetId equals y.i_assetId
                                where x.i_assetId == assetId && x.i_maintenanceId == maintenanceId
                                select x).OrderByDescending(q => q.dt_captureDate).FirstOrDefault();

                if (lastDate == null)
                    shiftFlag = false;
                else
                    shiftDate = lastDate.dt_captureDate;


                //get last validation date for asset
                var lastValDate = (from x in db.as_validationTaskProfile
                                   where x.i_assetId == assetId
                                   select x).OrderByDescending(q => q.dt_dateTimeStamp).FirstOrDefault();

                if (lastValDate == null)
                    validationFlag = false;
                else
                    valDate = lastValDate.dt_dateTimeStamp;

                //get the latest date
                if (shiftFlag && validationFlag)
                {
                    if (shiftDate > valDate)
                        returnDate = shiftDate.AddDays(frequency);
                    else
                        returnDate = valDate.AddDays(frequency);
                }
                else if (shiftFlag)
                {
                    returnDate = shiftDate.AddDays(frequency);
                }
                else if (validationFlag)
                {
                    returnDate = valDate.AddDays(frequency);
                }

                return returnDate;
            }    
            catch (Exception err)
            {
                Logging log = new Logging();
                log.logError(err, "SYSTEM Cache");
                return new DateTime(1970, 1, 1);
            }
        
        }

        public int getMaintenanceColour(DateTime lastDate, double frequency)
        {
            try
            {
                //Get the maintenace for given date
                //Create Date: 2014/12/09
                //Author: Bernard Willer

                //Calculate the day difference 
                int daysDiff = 0;

                if (lastDate > new DateTime(1970,1,1))
                    daysDiff = (DateTime.Now - lastDate).Days;
                else
                    daysDiff = -1;

                int color = (int)maintenanceColours.grey;

                //calculate the color
                if (daysDiff == -1) color = (int)maintenanceColours.grey;
                else if (daysDiff <= (frequency / 3)) color = (int)maintenanceColours.green;
                else if ((daysDiff > (frequency / 3)) && (daysDiff <= (frequency / 1.5))) color = (int)maintenanceColours.yellow;
                else if ((daysDiff > (frequency / 1.5)) && (daysDiff <= frequency)) color = (int)maintenanceColours.orange;
                else color = (int)maintenanceColours.red;

                return color;
            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.logError(err, "SYSTEM Cache");
                return (int)maintenanceColours.grey;
            }
        
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

        #region Procedures

        public List<ActivityChart> getActivitiesForMonth()
        {
            try
            {
                //This procedure send metrics for the activity chart
                //Create Date: 2015/01/20
                //Author: Bernard Willer

                List<ActivityChart> activities = new List<ActivityChart>();

                //collect all shifts
                var shifts = (from x in db.as_shiftData
                              group x by new { y = x.dt_captureDate.Year, m = x.dt_captureDate.Month, d = x.dt_captureDate.Day } into shiftGroup
                              select new {
                                  dateOfActivity = shiftGroup.Key,
                                  numberOfActivities = shiftGroup.Count()
                              }).ToList();

                foreach(var shift in shifts)
                {
                    ActivityChart activity = new ActivityChart();
                    activity.dateOfActivity = new DateTime(shift.dateOfActivity.y, shift.dateOfActivity.m, shift.dateOfActivity.d).ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
                    activity.numberOfActivities = shift.numberOfActivities;
                    activities.Add(activity);
                }

                return activities;
            }
            catch (Exception err)
            {
                List<ActivityChart> activities = new List<ActivityChart>();
                Logging log = new Logging();
                log.logError(err, "SYSTEM");
                return activities;
            }
        
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