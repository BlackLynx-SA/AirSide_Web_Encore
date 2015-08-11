using AirSide.ServerModules.Models;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirSide.ServerModules.Helpers
{
    public class DatabaseHelper : IDisposable
    {
        private readonly Entities db = new Entities();

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

        public int getAssetCountPerCustomShift(int shiftId)
        {
            int assetCount = (from x in db.as_shiftsCustomProfile where x.i_shiftId == shiftId select x).Count();
            return assetCount;
        }

        public int getCompletedAssetsForShift(int shiftId)
        {
            //TODO: This is a short term hack and needs to be fixed to identify the shifts properly
            int shiftCount = (from x in db.as_shiftData where x.i_shiftId == shiftId && x.i_shiftId > 8999 select x).GroupBy(q => q.i_assetId).Count();
            return shiftCount;
        }

        public int getCompletedAssetsForCustomShift(int shiftId)
        {
            //TODO: This is a short term hack and needs to be fixed to identify the shifts properly
            int shiftCount = (from x in db.as_shiftData where x.i_shiftId == shiftId && x.i_shiftId < 9000 select x).GroupBy(q => q.i_assetId).Count();
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
                                  where y.i_assetId == assetId
                                  select x.dt_captureDate).DefaultIfEmpty(newDate).First();
            return firstDate;
        }

        public maintenance[] getMaintenaceTasks(int assetId)
        {
            var asset = (from x in db.as_assetProfile
                         join y in db.as_assetClassProfile on x.i_assetClassId equals y.i_assetClassId
                         join z in db.as_assetClassMaintenanceProfile on y.i_assetClassId equals z.i_assetClassId
                         join a in db.as_frequencyProfile on z.i_frequencyId equals a.i_frequencyId
                         join b in db.as_maintenanceProfile on z.i_maintenanceId equals b.i_maintenanceId
                         where x.i_assetId == assetId
                         select new
                         {
                             frequency = a.f_frequency,
                             task = b.vc_description,
                             maintencanceId = b.i_maintenanceId,
                         });

            if (asset != null)
            {
                maintenance[] maintenanceArray = new maintenance[asset.Count()];
                List<maintenance> allTasks = new List<maintenance>();
                int i = 0;

                foreach (var item in asset)
                {
                    DateTime previousDate = getMaintenanceLastDate(item.maintencanceId, assetId);
                    DateTime nextDate = getMaintenanceNextDate(item.maintencanceId, assetId, item.frequency);
                    int color = getMaintenanceColour(previousDate, item.frequency);

                    maintenance maintenanceTask = new maintenance();
                    maintenanceTask.maintenanceTask = item.task;
                    maintenanceTask.previousDate = previousDate.ToString("yyyy/MM/dd");
                    maintenanceTask.nextDate = nextDate.ToString("yyyy/MM/dd");
                    maintenanceTask.maintenanceCycle = color;
                    maintenanceTask.maintenanceId = item.maintencanceId;

                    allTasks.Add(maintenanceTask);
                }

                foreach (var item in allTasks.OrderByDescending(q => q.maintenanceCycle))
                {
                    maintenance maintenanceTask = new maintenance();
                    maintenanceTask.maintenanceTask = item.maintenanceTask;
                    maintenanceTask.previousDate = item.previousDate;
                    maintenanceTask.nextDate = item.nextDate;
                    maintenanceTask.maintenanceCycle = item.maintenanceCycle;
                    maintenanceTask.maintenanceId = item.maintenanceId;
                    maintenanceArray[i] = maintenanceTask;
                    i++;
                }

                return maintenanceArray;
            }
            else
                return null;
        }

        public List<maintenance> getMaintenanceTasksDocDB(int assetId)
        {
            var asset = (from x in db.as_assetProfile
                         join y in db.as_assetClassProfile on x.i_assetClassId equals y.i_assetClassId
                         join z in db.as_assetClassMaintenanceProfile on y.i_assetClassId equals z.i_assetClassId
                         join a in db.as_frequencyProfile on z.i_frequencyId equals a.i_frequencyId
                         join b in db.as_maintenanceProfile on z.i_maintenanceId equals b.i_maintenanceId
                         where x.i_assetId == assetId
                         select new
                         {
                             frequency = a.f_frequency,
                             task = b.vc_description,
                             maintencanceId = b.i_maintenanceId,
                         });

            List<maintenance> allTasks = new List<maintenance>();

            foreach (var item in asset)
            {
                DateTime previousDate = getMaintenanceLastDate(item.maintencanceId, assetId);
                DateTime nextDate = getMaintenanceNextDate(item.maintencanceId, assetId, item.frequency);
                int color = getMaintenanceColour(previousDate, item.frequency);

                maintenance maintenanceTask = new maintenance();
                maintenanceTask.maintenanceTask = item.task;
                maintenanceTask.previousDate = previousDate.ToString("yyyy/MM/dd");
                maintenanceTask.nextDate = nextDate.ToString("yyyy/MM/dd");
                maintenanceTask.maintenanceCycle = color;
                maintenanceTask.maintenanceId = item.maintencanceId;

                allTasks.Add(maintenanceTask);
            }

            return allTasks;
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
                                join z in db.as_shifts on x.i_shiftId equals z.i_shiftId
                                where x.i_assetId == assetId && z.i_maintenanceId == maintenanceId
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
                //log.logError(err, "SYSTEM Cache");
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
                                join z in db.as_shifts on x.i_shiftId equals z.i_shiftId
                                where x.i_assetId == assetId && z.i_maintenanceId == maintenanceId
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
                //log.logError(err, "SYSTEM Cache");
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

                if (lastDate > new DateTime(1970, 1, 1))
                    daysDiff = (DateTime.Now - lastDate).Days;
                else
                    daysDiff = -1;

                int color = (int)MaintenanceColours.grey;

                //calculate the color
                if (daysDiff == -1) color = (int)MaintenanceColours.grey;
                else if (daysDiff <= (frequency / 3)) color = (int)MaintenanceColours.green;
                else if ((daysDiff > (frequency / 3)) && (daysDiff <= (frequency / 1.5))) color = (int)MaintenanceColours.yellow;
                else if ((daysDiff > (frequency / 1.5)) && (daysDiff <= frequency)) color = (int)MaintenanceColours.orange;
                else color = (int)MaintenanceColours.red;

                return color;
            }
            catch (Exception err)
            {
                //log.logError(err, "SYSTEM Cache");
                return (int)MaintenanceColours.grey;
            }

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

        public int getNumberOfFixingPoints(int assetClassId)
        {
            var fixingPoints = (from x in db.as_assetInfoProfile
                                where x.vc_description == "Fixing Points" && x.i_assetClassId == assetClassId
                               
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

        public List<ActivityChart> getAnomaliesForMonth()
        {
            try
            {
                //This procedure get all the anomalies reported in the last month
                //Create Date: 2015/03/18
                //Author: Bernard Willer

                List<ActivityChart> activities = new List<ActivityChart>();
                DateTime thisMonth = DateTime.Now.AddDays(-30);

                //Get Anomalies
                var anomalies = (from x in db.as_fileUploadProfile
                                 where x.dt_datetime >= thisMonth
                                 group x by new { y = x.dt_datetime.Year, m = x.dt_datetime.Month, d = x.dt_datetime.Day } into anomalyGroup
                                 select new
                                 {
                                     dateOfActivity = anomalyGroup.Key,
                                     numberOfActivities = anomalyGroup.Count()
                                 }).OrderBy(q => q.dateOfActivity).ToList();

                //Add Anomalies
                foreach (var anomaly in anomalies)
                {
                    ActivityChart activity = new ActivityChart();
                    activity.dateOfActivity = new DateTime(anomaly.dateOfActivity.y, anomaly.dateOfActivity.m, anomaly.dateOfActivity.d).ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
                    activity.numberOfActivities = anomaly.numberOfActivities;
                    activities.Add(activity);
                }

                return activities;
            }
            catch (Exception err)
            {
                List<ActivityChart> activities = new List<ActivityChart>();
                //log.logError(err, "SYSTEM");
                return activities;
            }
        }

        public List<ActivityChart> getActivitiesForMonth()
        {
            try
            {
                //This procedure send metrics for the activity chart
                //Create Date: 2015/01/20
                //Author: Bernard Willer

                List<ActivityChart> activities = new List<ActivityChart>();
                DateTime thisMonth = DateTime.Now.AddDays(-30);

                //collect all shifts
                var shifts = (from x in db.as_shiftData
                              where x.dt_captureDate >= thisMonth
                              group x by new { y = x.dt_captureDate.Year, m = x.dt_captureDate.Month, d = x.dt_captureDate.Day, asset = x.i_assetId } into shiftGroup
                              select new
                              {
                                  year = shiftGroup.Key.y,
                                  month = shiftGroup.Key.m,
                                  day = shiftGroup.Key.d,
                                  asset = shiftGroup.Key.asset
                              });

                var filtered = (from x in shifts
                                group x by new { y = x.year, m = x.month, d = x.day } into g
                                select new
                                {
                                    dateOfActivity = g.Key,
                                    numberOfActivities = g.Count()
                                }).OrderBy(q => q.dateOfActivity);


                foreach (var shift in filtered)
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
                //log.logError(err, "SYSTEM");
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
