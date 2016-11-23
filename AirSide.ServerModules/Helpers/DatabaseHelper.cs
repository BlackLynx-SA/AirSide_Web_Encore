using System;
using System.Collections.Generic;
using System.Linq;
using AirSide.ServerModules.Models;

namespace AirSide.ServerModules.Helpers
{
    public class DatabaseHelper : IDisposable
    {
        private readonly Entities _db = new Entities();

        #region Functions

        public int GetAssetCountPerSubArea(int subArea)
        {
            var assetCount = (from x in _db.as_assetProfile
                              join y in _db.as_locationProfile on x.i_locationId equals y.i_locationId
                              join z in _db.as_areaSubProfile on y.i_areaSubId equals z.i_areaSubId
                              where z.i_areaSubId == subArea
                              select x).Count();
            return assetCount;
        }

        public int GetAssetCountPerCustomShift(int shiftId)
        {
            var assetCount = (from x in _db.as_shiftsCustomProfile where x.i_shiftId == shiftId select x).Count();
            return assetCount;
        }

        public int GetCompletedAssetsForShift(int shiftId)
        {
            var shiftCount = (from x in _db.as_shiftData join y in _db.as_shifts on x.i_shiftId equals y.i_shiftId where x.i_shiftId == shiftId && y.i_areaSubId != 0 select x).GroupBy(q => q.i_assetId).Count();
            return shiftCount;
        }

        public int GetCompletedAssetsForCustomShift(int shiftId)
        {
            var shiftCount = (from x in _db.as_shiftData join y in _db.as_shifts on x.i_shiftId equals y.i_shiftId where x.i_shiftId == shiftId && y.i_areaSubId == 0 select x).GroupBy(q => q.i_assetId).Count();
            return shiftCount;
        }

        public int GetAssetCountPerArea(int areaId)
        {
            var assetCount = (from x in _db.as_assetProfile
                              join y in _db.as_locationProfile on x.i_locationId equals y.i_locationId
                              join z in _db.as_areaSubProfile on y.i_areaSubId equals z.i_areaSubId
                              where z.i_areaId == areaId
                              select x).Count();
            return assetCount;
        }

        public DateTime GetFirstMaintanedDate(int assetId)
        {
            var newDate = new DateTime(1970, 1, 1);
            try
            {
                var firstDate = (from x in _db.as_shiftData
                    join y in _db.as_assetProfile on x.i_assetId equals y.i_assetId
                    where y.i_assetId == assetId
                    select x.dt_captureDate).DefaultIfEmpty(newDate).First();
                return firstDate;
            }
            catch (Exception)
            {
                return newDate;
            }
        }

        public maintenance[] GetMaintenaceTasks(int assetId)
        {
            var asset = (from x in _db.as_assetProfile
                         join y in _db.as_assetClassProfile on x.i_assetClassId equals y.i_assetClassId
                         join z in _db.as_assetClassMaintenanceProfile on y.i_assetClassId equals z.i_assetClassId
                         join a in _db.as_frequencyProfile on z.i_frequencyId equals a.i_frequencyId
                         join b in _db.as_maintenanceProfile on z.i_maintenanceId equals b.i_maintenanceId
                         where x.i_assetId == assetId
                         select new
                         {
                             frequency = a.f_frequency,
                             task = b.vc_description,
                             maintencanceId = b.i_maintenanceId,
                         });

            {
                var maintenanceArray = new maintenance[asset.Count()];
                var allTasks = new List<maintenance>();
                var i = 0;

                foreach (var item in asset)
                {
                    var previousDate = GetMaintenanceLastDate(item.maintencanceId, assetId);
                    var nextDate = GetMaintenanceNextDate(item.maintencanceId, assetId, item.frequency);
                    var color = GetMaintenanceColour(previousDate, item.frequency);

                    var maintenanceTask = new maintenance
                    {
                        maintenanceTask = item.task,
                        previousDate = previousDate.ToString("yyyy/MM/dd"),
                        nextDate = nextDate.ToString("yyyy/MM/dd"),
                        maintenanceCycle = color,
                        maintenanceId = item.maintencanceId
                    };

                    allTasks.Add(maintenanceTask);
                }

                foreach (var item in allTasks.OrderByDescending(q => q.maintenanceCycle))
                {
                    var maintenanceTask = new maintenance
                    {
                        maintenanceTask = item.maintenanceTask,
                        previousDate = item.previousDate,
                        nextDate = item.nextDate,
                        maintenanceCycle = item.maintenanceCycle,
                        maintenanceId = item.maintenanceId
                    };
                    maintenanceArray[i] = maintenanceTask;
                    i++;
                }

                return maintenanceArray;
            }
        }

        public List<maintenance> GetMaintenanceTasksDocDb(int assetId)
        {
            var asset = (from x in _db.as_assetProfile
                         join y in _db.as_assetClassProfile on x.i_assetClassId equals y.i_assetClassId
                         join z in _db.as_assetClassMaintenanceProfile on y.i_assetClassId equals z.i_assetClassId
                         join a in _db.as_frequencyProfile on z.i_frequencyId equals a.i_frequencyId
                         join b in _db.as_maintenanceProfile on z.i_maintenanceId equals b.i_maintenanceId
                         where x.i_assetId == assetId
                         select new
                         {
                             frequency = a.f_frequency,
                             task = b.vc_description,
                             maintencanceId = b.i_maintenanceId,
                         });

            var allTasks = new List<maintenance>();

            foreach (var item in asset)
            {
                var previousDate = GetMaintenanceLastDate(item.maintencanceId, assetId);
                var nextDate = GetMaintenanceNextDate(item.maintencanceId, assetId, item.frequency);
                var color = GetMaintenanceColour(previousDate, item.frequency);

                var maintenanceTask = new maintenance
                {
                    maintenanceTask = item.task,
                    previousDate = previousDate.ToString("yyyy/MM/dd"),
                    nextDate = nextDate.ToString("yyyy/MM/dd"),
                    maintenanceCycle = color,
                    maintenanceId = item.maintencanceId
                };

                allTasks.Add(maintenanceTask);
            }

            return allTasks;
        }

        public DateTime GetMaintenanceLastDate(int maintenanceId, int assetId)
        {
            try
            {
                //Get the last maintained date for a asset and the maintenance id
                //Create Date: 2016/03/31
                //Author: Bernard Willer

                var maintenance = _db.as_maintenanceProfile.Find(maintenanceId);
                
                var shiftFlag = true;
                var validationFlag = true;

                var shiftDate = DateTime.Now;
                var valDate = DateTime.Now;
                var returnDate = new DateTime(1970, 1, 1);

                if (maintenance.i_maintenanceValidationId == 2)
                {
                    //Get the last shift date for asset
                    var lastDate = (from x in _db.as_shiftData
                        join y in _db.as_assetProfile on x.i_assetId equals y.i_assetId
                        join z in _db.as_shifts on x.i_shiftId equals z.i_shiftId
                        where x.i_assetId == assetId && z.i_maintenanceId == maintenanceId
                        select x).OrderByDescending(q => q.dt_captureDate).FirstOrDefault();

                    if (lastDate == null)
                        shiftFlag = false;
                    else
                        shiftDate = lastDate.dt_captureDate;

                    if (shiftFlag) returnDate = shiftDate;

                } else if (maintenance.i_maintenanceValidationId == 1)
                {
                    //get last validation date for asset
                    var lastValDate = (from x in _db.as_validationTaskProfile
                        where x.i_assetId == assetId && x.i_maintenanceId == maintenanceId
                        select x).OrderByDescending(q => q.dt_dateTimeStamp).FirstOrDefault();

                    if (lastValDate == null)
                        validationFlag = false;
                    else
                        valDate = lastValDate.dt_dateTimeStamp;

                    if (validationFlag) returnDate = valDate;
                }
                
                return returnDate;

            }
            catch (Exception)
            {
                //log.logError(err, "SYSTEM Cache");
                return new DateTime(1970, 1, 1);
            }

        }

        public DateTime GetMaintenanceNextDate(int maintenanceId, int assetId, double frequency)
        {
            //Gets the next maintenance date for a asset and maintenance id
            //Create Date: 2016/03/31
            //Author: Bernard Willer

            try
            {
                var maintenance = _db.as_maintenanceProfile.Find(maintenanceId);

                var shiftFlag = true;
                var validationFlag = true;

                var shiftDate = DateTime.Now;
                var valDate = DateTime.Now;
                var returnDate = new DateTime(1970, 1, 1);

                if (maintenance.i_maintenanceValidationId == 2)
                {
                    //Get the last shift date for asset
                    var lastDate = (from x in _db.as_shiftData
                        join y in _db.as_assetProfile on x.i_assetId equals y.i_assetId
                        join z in _db.as_shifts on x.i_shiftId equals z.i_shiftId
                        where x.i_assetId == assetId && z.i_maintenanceId == maintenanceId
                        select x).OrderByDescending(q => q.dt_captureDate).FirstOrDefault();

                    if (lastDate == null)
                        shiftFlag = false;
                    else
                        shiftDate = lastDate.dt_captureDate;

                    if (shiftFlag) returnDate = shiftDate.AddDays(frequency);
                } else if (maintenance.i_maintenanceValidationId == 1)
                {
                    //get last validation date for asset
                    var lastValDate = (from x in _db.as_validationTaskProfile
                        where x.i_assetId == assetId && x.i_maintenanceId == maintenanceId
                        select x).OrderByDescending(q => q.dt_dateTimeStamp).FirstOrDefault();

                    if (lastValDate == null)
                        validationFlag = false;
                    else
                        valDate = lastValDate.dt_dateTimeStamp;

                    if (validationFlag) returnDate = valDate.AddDays(frequency);
                }

                return returnDate;
            }
            catch (Exception)
            {
                //log.logError(err, "SYSTEM Cache");
                return new DateTime(1970, 1, 1);
            }

        }

        public int GetMaintenanceColour(DateTime lastDate, double frequency)
        {
            try
            {
                //Get the maintenace for given date
                //Create Date: 2014/12/09
                //Author: Bernard Willer

                //Calculate the day difference 
                int daysDiff;

                if (lastDate > new DateTime(1970, 1, 1))
                    daysDiff = (DateTime.Now - lastDate).Days;
                else
                    daysDiff = -1;

                int color;

                //calculate the color
                if (daysDiff == -1) color = (int)MaintenanceColours.grey;
                else if (daysDiff <= (frequency / 3)) color = (int)MaintenanceColours.green;
                else if ((daysDiff > (frequency / 3)) && (daysDiff <= (frequency / 1.5))) color = (int)MaintenanceColours.yellow;
                else if ((daysDiff > (frequency / 1.5)) && (daysDiff <= frequency)) color = (int)MaintenanceColours.orange;
                else color = (int)MaintenanceColours.red;

                return color;
            }
            catch (Exception)
            {
                //log.logError(err, "SYSTEM Cache");
                return (int)MaintenanceColours.grey;
            }

        }

        public string GetLastAssetMaintenanceType(int assetId)
        {
            var maintenanceType = (from x in _db.as_shiftData
                                   join y in _db.as_maintenanceProfile on x.i_maintenanceId equals y.i_maintenanceId
                                   where x.i_assetId == assetId
                                   select new
                                   {
                                       dateStamp = x.dt_captureDate,
                                       description = y.vc_description
                                   }).OrderByDescending(q => q.dateStamp).FirstOrDefault();
            if (maintenanceType != null) return maintenanceType.description;
            else return "---";
        }

        public DateTime GetLastDateForSubArea(int areaSubId)
        {
            var firstDate = (from x in _db.as_areaProfile
                             join y in _db.as_areaSubProfile on x.i_areaId equals y.i_areaId
                             join z in _db.as_locationProfile on y.i_areaSubId equals z.i_areaSubId
                             join a in _db.as_assetProfile on z.i_locationId equals a.i_locationId
                             join b in _db.as_shiftData on a.i_assetId equals b.i_assetId
                             where x.i_areaId == areaSubId
                             select b).OrderBy(q => q.dt_captureDate).FirstOrDefault();

            var returnDate = new DateTime(1970, 1, 1);
            if (firstDate != null) returnDate = firstDate.dt_captureDate;
            return returnDate;
        }

        public DateTime GetLastMaintanedDate(int assetId)
        {
            var lastDate = (from x in _db.as_shiftData
                            join y in _db.as_assetProfile on x.i_assetId equals y.i_assetId
                            where x.i_assetId == assetId
                            select x).OrderByDescending(q => q.dt_captureDate).FirstOrDefault();

            var returnDate = new DateTime(1970, 1, 1);
            if (lastDate != null)
                returnDate = lastDate.dt_captureDate;
            return returnDate;
        }

        public string GetLastShiftDateForAsset(int assetId)
        {
            var lastDate = (from x in _db.as_shiftData
                            where x.i_assetId == assetId
                            select x).OrderByDescending(q => q.dt_captureDate).FirstOrDefault();

            var returnDate = ("---");
            if (lastDate != null)
                returnDate = lastDate.dt_captureDate.ToString("yyyMMdd");

            return returnDate;
        }

        public int GetNumberOfFixingPoints(int assetClassId)
        {
            var fixingPoints = (from x in _db.as_assetInfoProfile
                                where x.vc_description == "Fixing Points" && x.i_assetClassId == assetClassId
                               
                                select x).FirstOrDefault();
            return fixingPoints != null ? int.Parse(fixingPoints.vc_value) : 0;
        }

        public bool GetSubmittedShiftData(int assetId)
        {
            var completed = (from x in _db.as_shiftData
                             join y in _db.as_shifts on x.i_shiftId equals y.i_shiftId
                             where y.bt_completed && x.i_assetId == assetId
                             select x).FirstOrDefault();
            return completed != null;
        }


        #endregion

        #region Procedures

        public List<ActivityChart> GetAnomaliesForMonth()
        {
            try
            {
                //This procedure get all the anomalies reported in the last month
                //Create Date: 2015/03/18
                //Author: Bernard Willer

                var thisMonth = DateTime.Now.AddDays(-30);

                //Get Anomalies
                var anomalies = (from x in _db.as_fileUploadProfile
                                 where x.dt_datetime >= thisMonth
                                 group x by new { y = x.dt_datetime.Year, m = x.dt_datetime.Month, d = x.dt_datetime.Day } into anomalyGroup
                                 select new
                                 {
                                     dateOfActivity = anomalyGroup.Key,
                                     numberOfActivities = anomalyGroup.Count()
                                 }).OrderBy(q => q.dateOfActivity).ToList();

                //Add Anomalies
                var activities = new List<ActivityChart>();

                foreach (var item in anomalies)
                {
                    var anomaly = new ActivityChart
                    {
                        dateOfActivity = CalcDateForChart(item.dateOfActivity.y, item.dateOfActivity.m, item.dateOfActivity.d),
                        numberOfActivities = item.numberOfActivities
                    };

                    activities.Add(anomaly);
                }

                return activities;
            }
            catch (Exception)
            {
                var activities = new List<ActivityChart>();
                //log.logError(err, "SYSTEM");
                return activities;
            }
        }

        public List<ActivityChart> GetActivitiesForMonth()
        {
            try
            {
                //This procedure send metrics for the activity chart
                //Create Date: 2015/01/20
                //Author: Bernard Willer

                var thisMonth = DateTime.Now.AddDays(-30);

                //collect all shifts
                var shifts = (from x in _db.as_shiftData
                              where x.dt_captureDate >= thisMonth
                              group x by new { y = x.dt_captureDate.Year, m = x.dt_captureDate.Month, d = x.dt_captureDate.Day, asset = x.i_assetId } into shiftGroup
                              select new
                              {
                                  year = shiftGroup.Key.y,
                                  month = shiftGroup.Key.m,
                                  day = shiftGroup.Key.d, shiftGroup.Key.asset
                              });

                var filtered = (from x in shifts
                                group x by new { y = x.year, m = x.month, d = x.day } into g
                                select new
                                {
                                    dateOfActivity = g.Key,
                                    numberOfActivities = g.Count()
                                }).OrderBy(q => q.dateOfActivity);


                var activities = new List<ActivityChart>();

                foreach (var item in filtered)
                {
                    var activity = new ActivityChart
                    {
                        dateOfActivity = CalcDateForChart(item.dateOfActivity.y, item.dateOfActivity.m, item.dateOfActivity.d),
                        numberOfActivities = item.numberOfActivities
                    };

                    activities.Add(activity);
                }

                return activities;
            }
            catch (Exception)
            {
                var activities = new List<ActivityChart>();
                return activities;
            }

        }

        private double CalcDateForChart(int year, int month, int day)
        {
            var seventy = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var returnDate = new DateTime(year, month, day);
            returnDate = returnDate.ToUniversalTime();
            var gap = returnDate.Subtract(seventy);
            return gap.TotalMilliseconds;
        }

        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                _db.Dispose();
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
