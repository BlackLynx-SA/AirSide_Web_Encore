#region Copyright
// BlackLynx (Pty) Ltd.
// Copyright (c) 2011 - 2015 All Right Reserved, http://www.blacklynx.co.za/
//
// THE CODE IN THIS SOURCE FILE HAS BEEN DEVELOPED BY BLACKLYNX (PTY) LTD. ("BL")
// THE USE OF ANY EXTRACT, MODULES OR UNITS ARE STICKLY FORBIDDEN.
// PLEASE OBTAIN APPROPRIATE APPROVAL FROM BL AT INFO@BLACKLYNX.CO.ZA
//
// AUTHOR: Bernard Willer
// EMAIL: bernard.willer@blacklynx.co.za
// CREATE DATE: 2014/11/01
// SUMMARY: This class contains all controller calls for the Shifts route
#endregion

using AirSide.ServerModules.Helpers;
using AirSide.ServerModules.Models;
using Microsoft.Reporting.WebForms;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace ADB.AirSide.Encore.V1.Controllers
{
    [Authorize]
    public class ShiftsController : BaseController
    {
        private readonly Entities _db = new Entities();
        private readonly CacheHelper _cache;
        private readonly DatabaseHelper _dbHelper = new DatabaseHelper();

        public ShiftsController()
        {
            _cache = new CacheHelper(ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString, ConfigurationManager.ConnectionStrings["MongoServer"].ConnectionString);
        }

        public ActionResult Calendar()
        {
            ViewBag.maintenanceTasks = new SelectList(_db.as_maintenanceProfile.OrderBy(q => q.vc_description).Distinct(), "i_maintenanceId", "vc_description");
            return View();
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public ActionResult MaintenanceTasks()
        {
            ViewBag.maintenanceValidation = new SelectList(_db.as_maintenanceValidation.OrderBy(q => q.vc_validationName).Distinct(), "i_maintenanceValidationId", "vc_validationName");
            return View();
        }

        #region Custom Shifts

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> AddCustomShift(CustomShiftClass shift, CustomShiftBounds bounds)
        {
            try
            {
                CultureInfo provider = CultureInfo.InvariantCulture;

                //2015/01/19 - Custom Shift Creation
                //  Types:
                //  1. Asset Filter -> All Assets, Asset Type, Selected task's cycle
                //  2. Area Filter -> Main and Sub Area
                //  Enums:
                //  101 - All Assets
                //  102 - Asset Type
                //  103 - Maintenance Cycle
                //  104 - Main Area
                //  105 - Sub Area
                //  106 - Faulty Lights

                //Get User 
                var aspUser = _db.AspNetUsers.FirstOrDefault(q => q.UserName == User.Identity.Name);
                var user = _db.UserProfiles.FirstOrDefault(q => q.aspId == aspUser.Id);

                as_shifts newShift = new as_shifts
                {
                    bt_completed = false,
                    dt_completionDate = new DateTime(1970, 1, 1),
                    dt_scheduledDate = DateTime.ParseExact(shift.scheduledDate, "dd/MM/yyyy h:mm tt", provider),
                    i_maintenanceId = shift.maintenanceId,
                    i_technicianGroup = shift.techGroupId,
                    vc_externalRef = shift.externalRef ?? "---",
                    vc_permitNumber = shift.permitNumber ?? "---",
                    bt_custom = true,
                    dt_dateCreated = DateTime.Now
                };

                if (user != null) newShift.i_createdBy = user.UserId;
                newShift.i_closedBy = 0;

                _db.as_shifts.Add(newShift);
                _db.SaveChanges();

                var assets = await FindAssets(shift, bounds);
                foreach (as_shiftsCustomProfile shiftProfile in assets.Select(asset => new as_shiftsCustomProfile
                {
                    i_assetId = asset,
                    i_shiftId = newShift.i_shiftId
                }))
                {
                    _db.as_shiftsCustomProfile.Add(shiftProfile);
                }

                _db.SaveChanges();

                //update iOS Cache Hash
                _cache.UpdateiOsCache("getTechnicianShifts");

                return Json(new { message = "Success", count = assets.Count });
            }
            catch (Exception err)
            {
                _cache.Log("Failed to insert custom shift: " + err.Message, "addCustomShift", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //2015/01/19 Custom Shift Helpers----------------------------------------------------------------------------------------------------------------------------
        private async Task<List<int>> FindAssets(CustomShiftClass shift, CustomShiftBounds bounds)
        {
            try
            {
                //This helper function takes the parameters send by the frontend and calculates which assets it applies to
                //Create Date: 2015/01/19
                //Author: Bernard Willer

                List<int> assets = new List<int>(); 

                switch (shift.filterType)
                {   case 101:   //Process All
                        assets = await ProcessAllAssets(shift, bounds);
                        break;
                    case 102: //Process Asset Type
                        assets = await ProcessAssetType(shift, bounds);
                        break;
                    case 103: //Process Maintneace Cycle
                        assets = await ProcessAssetCycle(shift, bounds);
                        break;
                    case 104: //Main Area
                        assets = await ProcessMainArea(shift, bounds);
                        break;
                    case 105: //Sub Area
                        assets = await ProcessSubArea(shift, bounds);
                        break;
                    case 106: //Faulty Lights
                        assets = await ProcessFaultyLights(shift, bounds);
                        break;
                    case 107: //Visual Surveyor => TODO: Need to build it out for reported other than just assets
                        assets = ProcessVisualSurveyor(shift, bounds);
                        break;
                }
                return assets;
            }
            catch (Exception err)
            {
                List<int> assets = new List<int>();
                _cache.LogError(err, User.Identity.Name + "(" + Request.UserHostAddress + ")");
                return assets;
            }
        
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        private class AssetObject
        {
            public int assetId { get; set; }
            public double latitude { get; set; }
            public double longitude { get; set; }
        }

        private List<int> ProcessVisualSurveyor(CustomShiftClass shift, CustomShiftBounds bounds)
        {
            //Disseminate the date range
            string[] dates = shift.dateRange.Split(char.Parse("-"));
            DateTime startDate = DateTime.ParseExact(dates[0], "yyyy/MM/dd", CultureInfo.InvariantCulture);
            DateTime endDate = DateTime.ParseExact(dates[1], "yyyy/MM/dd", CultureInfo.InvariantCulture);

            List<int> assetList = new List<int>();
            List<AssetObject> assets = new List<AssetObject>();

            //Get Images Elements
            if(shift.imageChk)
            {
                var assetSelect = from x in _db.as_fileUploadProfile
                                  join y in _db.as_fileUploadInfo on x.guid_file equals y.guid_file
                                  join z in _db.as_locationProfile on new { latitude = Math.Round(y.f_latitude, 5), longitude = Math.Round(y.f_longitude, 5) } equals new { latitude = Math.Round(z.f_latitude, 5), longitude = Math.Round(z.f_longitude, 5) }
                                  join a in _db.as_assetProfile on z.i_locationId equals a.i_locationId
                                  where x.i_fileType == 1 && y.bt_resolved == false && x.dt_datetime >= startDate && x.dt_datetime <= endDate
                                  select new
                                  {
                                    assetId = a.i_assetId,
                                    latitude = z.f_latitude,
                                    longitude = z.f_longitude
                                  };

                foreach(var item in assetSelect)
                {
                    AssetObject obj = new AssetObject
                    {
                        assetId = item.assetId,
                        latitude = item.latitude,
                        longitude = item.longitude
                    };

                    assets.Add(obj);
                }
            }

            //Get Voice Elements
            if (shift.voiceChk)
            {
                var assetSelect = from x in _db.as_fileUploadProfile
                                  join y in _db.as_fileUploadInfo on x.guid_file equals y.guid_file
                                  join z in _db.as_locationProfile on new { latitude = Math.Round(y.f_latitude, 5), longitude = Math.Round(y.f_longitude, 5) } equals new { latitude = Math.Round(z.f_latitude, 5), longitude = Math.Round(z.f_longitude, 5) }
                                  join a in _db.as_assetProfile on z.i_locationId equals a.i_locationId
                                  where x.i_fileType == 2 && y.bt_resolved == false && x.dt_datetime >= startDate && x.dt_datetime <= endDate
                                  select new
                                  {
                                      assetId = a.i_assetId,
                                      latitude = z.f_latitude,
                                      longitude = z.f_longitude
                                  };

                foreach (var item in assetSelect)
                {
                    var obj = new AssetObject
                    {
                        assetId = item.assetId,
                        latitude = item.latitude,
                        longitude = item.longitude
                    };

                    assets.Add(obj);
                }
            }

            //Get Images Elements
            if (shift.textChk)
            {
                var assetSelect = from x in _db.as_fileUploadProfile
                                  join y in _db.as_fileUploadInfo on x.guid_file equals y.guid_file
                                  join z in _db.as_locationProfile on new { latitude = Math.Round(y.f_latitude, 5), longitude = Math.Round(y.f_longitude, 5) } equals new { latitude = Math.Round(z.f_latitude, 5), longitude = Math.Round(z.f_longitude, 5) }
                                  join a in _db.as_assetProfile on z.i_locationId equals a.i_locationId
                                  where x.i_fileType == 3 && y.bt_resolved == false && x.dt_datetime >= startDate && x.dt_datetime <= endDate
                                  select new
                                  {
                                      assetId = a.i_assetId,
                                      latitude = z.f_latitude,
                                      longitude = z.f_longitude
                                  };

                foreach (var item in assetSelect)
                {
                    AssetObject obj = new AssetObject
                    {
                        assetId = item.assetId,
                        latitude = item.latitude,
                        longitude = item.longitude
                    };

                    assets.Add(obj);
                }
            }


            _cache.QuickDebugLog(assets.Count.ToString());

            foreach (var asset in assets)
            {
                bool latFlag = false;
                bool longFlag = false;
                
                if (asset.latitude >= bounds.SWLat && asset.latitude <= bounds.NELat) latFlag = true;
                if (asset.longitude >= bounds.SWLong && asset.longitude <= bounds.NELong) longFlag = true;

                if (latFlag && longFlag)
                {
                    assetList.Add(asset.assetId);
                }
            }

            return assetList;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        private async Task<List<int>> ProcessFaultyLights(CustomShiftClass shift, CustomShiftBounds bounds)
        {
            if (shift == null) throw new ArgumentNullException(nameof(shift));
            var assets = await _cache.GetAllAssets();
            var assetList = new List<int>();
            foreach (var asset in assets)
            {
                bool latFlag = false;
                bool longFlag = false;

                if (asset.location.latitude >= bounds.SWLat && asset.location.latitude <= bounds.NELat) latFlag = true;
                if (asset.location.longitude >= bounds.SWLong && asset.location.longitude <= bounds.NELong) longFlag = true;

                if (latFlag && longFlag)
                {
                    if(asset.status)
                        assetList.Add(asset.assetId);
                }
            }

            return assetList;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        private async Task<List<int>> ProcessAssetCycle(CustomShiftClass shift, CustomShiftBounds bounds)
        {
            var assets = await _cache.GetAllAssets();
            var assetList = new List<int>();
            foreach (var asset in assets)
            {
                bool latFlag = false;
                bool longFlag = false;

                if (asset.location.latitude >= bounds.SWLat && asset.location.latitude <= bounds.NELat) latFlag = true;
                if (asset.location.longitude  >= bounds.SWLong && asset.location.longitude <= bounds.NELong) longFlag = true;

                if (latFlag && longFlag)
                {
                    assetList.AddRange(from item in asset.maintenance where item.maintenanceCycle == shift.filterValue && item.maintenanceId == shift.maintenanceFilter select asset.assetId);
                }
            }

            return assetList;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        private async Task<List<int>> ProcessSubArea(CustomShiftClass shift, CustomShiftBounds bounds)
        {
            var assets = await _cache.GetAllAssetDownload();
            var assetList = new List<int>();
            foreach (var asset in assets)
            {
                bool latFlag = false;
                bool longFlag = false;

                if (asset.latitude >= bounds.SWLat && asset.latitude <= bounds.NELat) latFlag = true;
                if (asset.longitude >= bounds.SWLong && asset.longitude <= bounds.NELong) longFlag = true;

                if (!latFlag || !longFlag || asset.subAreaId != shift.filterValue) continue;
                int task = (from x in _db.as_assetClassProfile
                    join y in _db.as_assetProfile on x.i_assetClassId equals y.i_assetClassId
                    join z in _db.as_assetClassMaintenanceProfile on x.i_assetClassId equals z.i_assetClassId
                    where y.i_assetId == asset.assetId && z.i_maintenanceId == shift.maintenanceFilter
                    select x.i_assetClassId).DefaultIfEmpty(0).FirstOrDefault();
                if (task > 0)
                    assetList.Add(asset.assetId);
            }

            return assetList;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        private async Task<List<int>> ProcessMainArea(CustomShiftClass shift, CustomShiftBounds bounds)
        {
            var assets = await _cache.GetAllAssets();
            var assetList = new List<int>();
            foreach (var asset in assets)
            {
                bool latFlag = false;
                bool longFlag = false;

                if (asset.location.latitude >= bounds.SWLat && asset.location.latitude <= bounds.NELat) latFlag = true;
                if (asset.location.longitude >= bounds.SWLong && asset.location.longitude <= bounds.NELong) longFlag = true;

                if (latFlag && longFlag && asset.location.areaId == shift.filterValue)
                {
                    int task = (from x in _db.as_assetClassProfile
                                join y in _db.as_assetProfile on x.i_assetClassId equals y.i_assetClassId
                                join z in _db.as_assetClassMaintenanceProfile on x.i_assetClassId equals z.i_assetClassId
                                where y.i_assetId == asset.assetId && z.i_maintenanceId == shift.maintenanceFilter
                                select x.i_assetClassId).DefaultIfEmpty(0).FirstOrDefault();
                    if (task > 0)
                        assetList.Add(asset.assetId);
                }
            }

            return assetList;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        private async Task<List<int>> ProcessAssetType(CustomShiftClass shift, CustomShiftBounds bounds)
        {
            List<mongoFullAsset> assets = await _cache.GetAllAssetDownload();
            List<int> assetList = new List<int>();
            foreach (var asset in assets)
            {
                Boolean latFlag = false;
                Boolean longFlag = false;

                if (asset.latitude >= bounds.SWLat && asset.latitude <= bounds.NELat) latFlag = true;
                if (asset.longitude >= bounds.SWLong && asset.longitude <= bounds.NELong) longFlag = true;

                if (latFlag && longFlag && asset.assetClassId == shift.filterValue)
                {
                    int task = (from x in _db.as_assetClassProfile
                                join y in _db.as_assetProfile on x.i_assetClassId equals y.i_assetClassId
                                join z in _db.as_assetClassMaintenanceProfile on x.i_assetClassId equals z.i_assetClassId
                                where y.i_assetId == asset.assetId && z.i_maintenanceId == shift.maintenanceFilter
                                select x.i_assetClassId).DefaultIfEmpty(0).FirstOrDefault();
                    if (task > 0)
                        assetList.Add(asset.assetId);
                }
            }

            return assetList;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        private async Task<List<int>> ProcessAllAssets(CustomShiftClass shift, CustomShiftBounds bounds)
        {
            var assets = await _cache.GetAllAssetDownload();
            var assetList = new List<int>();
            foreach(var asset in assets)
            {
                bool latFlag = false;
                bool longFlag = false;

                if (asset.latitude >= bounds.SWLat && asset.latitude <= bounds.NELat) latFlag = true;
                if (asset.longitude >= bounds.SWLong && asset.longitude <= bounds.NELong) longFlag = true;

                if (!latFlag || !longFlag) continue;
                int task = (from x in _db.as_assetClassProfile
                    join y in _db.as_assetProfile on x.i_assetClassId equals y.i_assetClassId
                    join z in _db.as_assetClassMaintenanceProfile on x.i_assetClassId equals z.i_assetClassId
                    where y.i_assetId == asset.assetId && z.i_maintenanceId == shift.maintenanceFilter
                    select x.i_assetClassId).DefaultIfEmpty(0).FirstOrDefault();
                if (task > 0)
                    assetList.Add(asset.assetId);
            }

            return assetList;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        #endregion

        #region Area Based Shifts

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult GetAllTechnicianGroups()
        {
            try
            {
                var techGroups = _db.as_technicianGroups.OrderBy(q => q.vc_groupName).ToList();
                return Json(techGroups);
            }
            catch(Exception err)
            {
                _cache.Log("Failed to retrieve Technician Groups: " + err.Message, "getAllTechnicianGroups", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult GetAllShifts()
        {
            try
            {
                //Area Based Shifts
                var shifts = (from x in _db.as_shifts
                              join y in _db.as_areaSubProfile on x.i_areaSubId equals y.i_areaSubId
                              join z in _db.as_areaProfile on y.i_areaId equals z.i_areaId
                              join a in _db.as_technicianGroups on x.i_technicianGroup equals a.i_groupId
                              join b in _db.as_maintenanceProfile on x.i_maintenanceId equals b.i_maintenanceId
                              join c in _db.as_maintenanceValidation on b.i_maintenanceValidationId equals c.i_maintenanceValidationId
                              where x.bt_completed == false && x.bt_custom == false
                              select new { 
                                description = z.vc_description + "(" + y.vc_description + ")",
                                dateTime = x.dt_scheduledDate,
                                groupName = a.vc_groupName,
                                validation = c.i_maintenanceValidationId
                              }).ToList();

                List<shiftInfo> shiftList = shifts.Select(item => new shiftInfo
                {
                    description = item.description, dateTime = item.dateTime.ToString("yyyy/MM/dd h:mm tt"), groupName = item.groupName, validationId = item.validation
                }).ToList();

                //Custom Shifts
                shifts = (from x in _db.as_shifts
                          join a in _db.as_technicianGroups on x.i_technicianGroup equals a.i_groupId
                          join b in _db.as_maintenanceProfile on x.i_maintenanceId equals b.i_maintenanceId
                          join c in _db.as_maintenanceValidation on b.i_maintenanceValidationId equals c.i_maintenanceValidationId
                          where x.bt_completed == false && x.bt_custom
                          select new
                          {
                              description = "Selected Assets",
                              dateTime = x.dt_scheduledDate,
                              groupName = a.vc_groupName,
                              validation = c.i_maintenanceValidationId
                          }).ToList();

                shiftList.AddRange(shifts.Select(item => new shiftInfo
                {
                    description = item.description, dateTime = item.dateTime.ToString("yyyy/MM/dd h:mm tt"), groupName = item.groupName, validationId = item.validation
                }));

                return Json(shiftList);
            }
            catch (Exception err)
            {
                _cache.Log("Failed to retrieve all shifts: " + err.Message, "getAllShifts", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult GetUserEvents()
        {
            try
            {
                var allEvents = _db.as_eventTypes.Where(q => q.vc_userId == "All" || q.vc_userId == User.Identity.Name).ToList();
                return Json(allEvents);
            }
            catch (Exception err)
            {
                _cache.Log("Failed to retrieve all shifts: " + err.Message, "getAllShifts", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult InsertUserDefinedEvent(string description, string title, string color, string icon, Boolean userSpesific)
        {
            try
            {
                as_eventTypes newEvent = new as_eventTypes
                {
                    vc_color = color,
                    vc_description = description,
                    vc_icon = icon,
                    vc_title = title,
                    vc_userId = userSpesific ? User.Identity.Name : "All"
                };

                //write to DB
                _db.as_eventTypes.Add(newEvent);
                _db.SaveChanges();
                return Json(newEvent);
            }
            catch (Exception err)
            {
                _cache.Log("Failed to insert user defined shift: " + err.Message, "insertUserDefinedEvent", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult GetUserActiveEvents()
        {
            try
            {
                var events = (from x in _db.as_eventPofile
                              join y in _db.as_eventTypes on x.i_eventId equals y.i_eventId
                              where y.vc_userId == "All" || y.vc_userId == User.Identity.Name && x.bt_active
                              select new { 
                                    title = y.vc_title,
                                    description = y.vc_description,
                                    color = y.vc_color,
                                    icon = y.vc_icon,
                                    start = x.dt_dateTimeStart
                              }).ToList();

                List<EventInfo> eventList = events.Select(item => new EventInfo
                {
                    title = item.title, description = item.description, icon = item.icon, color = item.color, start = item.start.ToString("yyyy/MM/dd h:mm tt")
                }).ToList();

                return Json(eventList);
            }
            catch (Exception err)
            {
                _cache.Log("Failed to retrieve user events: " + err.Message, "getUserActiveEvents", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public ActionResult AllShifts()
        {
            return View();
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public ActionResult GetAirSideCalendar()
        {
            MemoryStream mStream = new MemoryStream();
            StreamWriter writer = new StreamWriter(mStream) {AutoFlush = true};


            string airportName = _db.as_settingsProfile.Where(q=>q.vc_settingDescription == "Airport Name").Select(q=>q.vc_settingValue).FirstOrDefault();

            var shifts = (from x in _db.as_shifts
                          join y in _db.as_maintenanceProfile on x.i_maintenanceId equals y.i_maintenanceId
                          join z in _db.as_areaSubProfile on x.i_areaSubId equals z.i_areaSubId
                          join a in _db.as_areaProfile on z.i_areaId equals a.i_areaId
                          where x.bt_completed == false
                          select new { 
                                beginDate = x.dt_scheduledDate,
                                location = airportName + " - " + a.vc_description + " (" + z.vc_description + ")",
                                description = y.vc_description
                          }).ToList();
            //header
            writer.WriteLine("BEGIN:VCALENDAR");
            writer.WriteLine("PRODID:-//ADB AirSide//AirSide//EN");
            writer.WriteLine("VERSION:2.0");

            foreach (var shift in shifts)
            {
                //Event Start
                writer.WriteLine("BEGIN:VEVENT");
                writer.WriteLine("X-WR-CALNAME:ADB AirSide");
                writer.WriteLine("X-WR-CALDESC:All events for the ADB AirSide Solution");

                //BODY
                writer.WriteLine("DTSTART:" + shift.beginDate.ToString("yyyyMMddTHHmmssZ"));
                writer.WriteLine("DTEND:" + shift.beginDate.AddHours(2).ToString("yyyyMMddTHHmmssZ"));
                writer.WriteLine("ORGANIZER;CN=ADB AirSide:MAILTO:support@adb-airside.com");
                writer.WriteLine("LOCATION:" + shift.location);
                writer.WriteLine("CATEGORIES:AirSide Event");
                writer.WriteLine("DESCRIPTION;ENCODING=QUOTED-PRINTABLE: Maintenance task to be performed: " + shift.description + "=0D=0A");
                writer.WriteLine("SUMMARY:AirSide Planned Event");

                //FOOTER
                writer.WriteLine("PRIORITY:3");
                writer.WriteLine("END:VEVENT");
            }

            //End Calendar
            writer.WriteLine("END:VCALENDAR");

            //MAKE IT DOWNLOADABLE
            Response.Clear(); //clears the current output content from the buffer
            Response.AppendHeader("Content-Disposition", "attachment; filename=AirSideEvents.ics");
            Response.AppendHeader("Content-Length", mStream.Length.ToString());
            Response.ContentType = "text/calendar";
            Response.BinaryWrite(mStream.ToArray());
            Response.End();
            return File(mStream.ToArray(), "text/calendar");
        }
        
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult GetShifts(Boolean active)
        {
            try
            {

                //Planned Area Shifts
                var shifts = (from x in _db.as_shifts
                              join y in _db.as_technicianGroups on x.i_technicianGroup equals y.i_groupId
                              join z in _db.as_areaSubProfile on x.i_areaSubId equals z.i_areaSubId
                              join a in _db.as_areaProfile on z.i_areaId equals a.i_areaId
                              join b in _db.as_maintenanceProfile on x.i_maintenanceId equals b.i_maintenanceId
                              join c in _db.as_maintenanceValidation on b.i_maintenanceValidationId equals c.i_maintenanceValidationId
                              where x.bt_completed != active && x.bt_custom == false
                              select new { 
                                eventType = "Shift",
                                start = x.dt_scheduledDate,
                                end = x.dt_completionDate,
                                area = a.vc_description,
                                subArea = z.vc_description,
                                progress = 0,
                                team = y.vc_groupName,
                                shiftId = x.i_shiftId,
                                subAreaId = z.i_areaSubId,
                                validation = c.vc_validationName,
                                validationId = c.i_maintenanceValidationId,
                                task = b.vc_description,
                                permit = x.vc_permitNumber
                              }).ToList();

                List<ShiftData> shiftList = new List<ShiftData>();
                foreach (var item in shifts)
                {
                    ShiftData shift = new ShiftData
                    {
                        area = item.area,
                        completed = item.end.ToString("yyyy/MM/dd HH:mm"),
                        eventType = "Shift",
                        start = item.start.ToString("yyyy/MM/dd HH:mm"),
                        subArea = item.subArea,
                        team = item.team,
                        shiftId = item.shiftId,
                        shiftType = 1,
                        validation = item.validation,
                        task = item.task,
                        permit = item.permit
                    };

                    //Calculate the shift progress
                    if (item.validationId == 2)
                    {
                        if (active)
                        {
                            shift.shiftData = _dbHelper.GetCompletedAssetsForShift(item.shiftId);
                            shift.assets = _dbHelper.GetAssetCountPerSubArea(item.subAreaId);
                            shift.progress = shift.assets == 0 ? 0 : Math.Round(value: (shift.shiftData / (double)shift.assets) * 100, digits: 0);
                        }
                        else
                        {
                            shift.progress = 0;
                        }
                    } else if(item.validationId == 1)
                    {
                        if (active)
                        {
                            shift.shiftData = _db.as_validationTaskProfile.Count(q => q.i_shiftId == item.shiftId && q.bt_validated);
                            shift.assets = _dbHelper.GetAssetCountPerSubArea(item.subAreaId);
                            shift.progress = shift.assets == 0 ? 0 : Math.Round((shift.shiftData / (double)shift.assets) * 100, 0);
                        }
                        else
                        {
                            shift.progress = 0;
                        }
                    }

                    shiftList.Add(shift);
                }

                //Custom Shifts
                shifts = (from x in _db.as_shifts
                          join y in _db.as_technicianGroups on x.i_technicianGroup equals y.i_groupId
                          join b in _db.as_maintenanceProfile on x.i_maintenanceId equals b.i_maintenanceId
                          join c in _db.as_maintenanceValidation on b.i_maintenanceValidationId equals c.i_maintenanceValidationId
                          where x.bt_completed != active && x.bt_custom
                          select new
                          {
                              eventType = "Shift",
                              start = x.dt_scheduledDate,
                              end = x.dt_completionDate,
                              area = "Selected Assets",
                              subArea = "---",
                              progress = 0,
                              team = y.vc_groupName,
                              shiftId = x.i_shiftId,
                              subAreaId = 0,
                              validation = c.vc_validationName,
                              validationId = c.i_maintenanceValidationId,
                              task = b.vc_description,
                              permit = x.vc_permitNumber
                          }).ToList();

                foreach (var item in shifts)
                {
                    ShiftData shift = new ShiftData
                    {
                        area = item.area,
                        completed = item.end.ToString("yyyy/MM/dd HH:mm"),
                        eventType = "Custom Shift",
                        start = item.start.ToString("yyyy/MM/dd HH:mm"),
                        subArea = item.subArea,
                        team = item.team,
                        shiftId = item.shiftId,
                        shiftType = 2,
                        validation = item.validation,
                        task = item.task,
                        permit = item.permit
                    };

                    //Calculate the shift progress
                    if (item.validationId == 2)
                    {
                        if (active)
                        {
                            shift.shiftData = _dbHelper.GetCompletedAssetsForShift(item.shiftId);
                            shift.assets = _db.as_shiftsCustomProfile.Count(q => q.i_shiftId == item.shiftId);
                            shift.progress = shift.assets == 0 ? 0 : Math.Round((shift.shiftData / (double)shift.assets) * 100, 0);
                        }
                        else
                        {
                            shift.progress = 0;
                        }
                    } else if(item.validationId == 1)
                    {
                        if (active)
                        {
                            shift.shiftData = _db.as_validationTaskProfile.Count(q => q.i_shiftId == item.shiftId && q.bt_validated);
                            shift.assets = _db.as_shiftsCustomProfile.Count(q => q.i_shiftId == item.shiftId);
                            shift.progress = shift.assets == 0 ? 0 : Math.Round(value: (shift.shiftData / (double)shift.assets) * 100, digits: 0);
                        }
                        else
                        {
                            shift.progress = 0;
                        }
                    }

                    shiftList.Add(shift);
                }

                
                return Json(shiftList.OrderByDescending(q=>q.completed));
            }
            catch (Exception err)
            {
                _cache.Log("Failed to retrieve shifts: " + err.Message, "getShifts", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UpdateShiftStatus(int shiftId, int shiftType)
        {
            try
            {
                //This function will update a active shift's status to complete
                //Create Date: 2015/01/19
                //Author: Bernard Willer

                //Get User
                var aspUser = _db.AspNetUsers.FirstOrDefault(q => q.UserName == User.Identity.Name);
                var user = _db.UserProfiles.FirstOrDefault(q => q.aspId == aspUser.Id);
                               
                var shift = _db.as_shifts.Find(shiftId);
                shift.bt_completed = true;
                shift.dt_completionDate = DateTime.Now;
                if (user != null) shift.i_closedBy = user.UserId;
                _db.Entry(shift).State = System.Data.Entity.EntityState.Modified;
                _db.SaveChanges();

                //update iOS Cache Hash
                _cache.UpdateiOsCache("getTechnicianShifts");

                return Json(new { message = "success" });
            }
            catch (Exception err)
            {
                _cache.LogError(err, User.Identity.Name + "(" + Request.UserHostAddress + ")");
                Response.StatusCode = 500;
                Response.Status = err.Message;
                return Json(new { message = err.Message });
            }
        
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult InsertUserEvent(string dateTime, int recuring, int groupId)
        {
            try
            {
                CultureInfo provider = CultureInfo.InvariantCulture;

                switch (recuring)
                {
                    case 0:
                        {
                            as_eventPofile newEvent = new as_eventPofile
                            {
                                bt_active = true,
                                bt_viewed = false
                            };
                            DateTime eventTime = DateTime.ParseExact(dateTime, "dd/MM/yyyy h:mm tt", provider);
                            newEvent.dt_dateTimeStart = eventTime;
                            newEvent.i_eventId = groupId;

                            _db.as_eventPofile.Add(newEvent);
                            _db.SaveChanges();

                        }
                        break;
                }

                return Json(new {message = "Event Created" });
            }
            catch (Exception err)
            {
                _cache.Log("Failed to insert user event: " + err.Message, "insertUserEvent", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult InsertNewShift(string dateTime, string workPermit, int recuring, int groupId, int subAreaId, int maintenanceId)
        {
            try
            {
                CultureInfo provider = CultureInfo.InvariantCulture;

                switch (recuring)
                {
                    case 0:
                        {
                            //Get User
                            var aspUser = _db.AspNetUsers.FirstOrDefault(q => q.UserName == User.Identity.Name);
                            var user = _db.UserProfiles.FirstOrDefault(q => q.aspId == aspUser.Id);

                            var newShift = new as_shifts();
                            DateTime shiftTime = DateTime.ParseExact(dateTime, "dd/MM/yyyy h:mm tt", provider);
                            newShift.bt_completed = false;
                            newShift.dt_completionDate = new DateTime(1970, 1, 1);
                            newShift.dt_scheduledDate = shiftTime;
                            newShift.i_technicianGroup = groupId;
                            newShift.i_areaSubId = subAreaId;
                            newShift.vc_permitNumber = workPermit ?? "---";
                            newShift.i_maintenanceId = maintenanceId;
                            newShift.vc_externalRef = "---";
                            newShift.bt_custom = false;
                            newShift.dt_dateCreated = DateTime.Now;
                            if (user != null) newShift.i_createdBy = user.UserId;
                            newShift.i_closedBy = 0;

                            _db.as_shifts.Add(newShift);
                            _db.SaveChanges();

                            //update iOS Cache Hash
                            _cache.UpdateiOsCache("getTechnicianShifts");
                        }
                        break;
                    case 1:
                        {

                        }
                        break;
                    case 2:
                        {

                        }
                        break;
                }

                return Json(new {message = "Shift Created" });
            }
            catch (Exception err)
            {
                _cache.Log("Failed to insert new shift: " + err.Message, "insertNewShift", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }
    
        #endregion

        #region Reporting

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public ActionResult ShiftDataDump(int shiftId, int type, string fileType)
        {
            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=airsidereporting;AccountKey=mCK8CqoLGGIu1c3BQ8BQEI4OtIKllkiwJQv4lMB4A6811TxLXsYzTITL8W7Z2gMztfrkbLUFuqDSe6+ZzPTGpg==");
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference("reportcontent");
                CloudBlockBlob blockBlob = container.GetBlockBlobReference("ExcelDataDump.rdlc");

                LocalReport localReport = new LocalReport();

                using (var memoryStream = new MemoryStream())
                {
                    blockBlob.DownloadToStream(memoryStream);
                    memoryStream.Position = 0;
                    localReport.LoadReportDefinition(memoryStream);
                }

                ReportDataSource reportDataSource = new ReportDataSource("AirSide_Reporting", GetReportData(shiftId, type));
                localReport.DataSources.Add(reportDataSource);

                string reportType = fileType;
                string mimeType;
                string encoding;
                string fileNameExtension;

                string deviceInfo =
                "<DeviceInfo>" +
                "  <OutputFormat>" + fileType + "</OutputFormat>" +
                "  <PageWidth>210mm</PageWidth>" +
                "  <PageHeight>297mm</PageHeight>" +
                "  <MarginTop>1cm</MarginTop>" +
                "  <MarginLeft>1cm</MarginLeft>" +
                "  <MarginRight>1cm</MarginRight>" +
                "  <MarginBottom>1cm</MarginBottom>" +
                "</DeviceInfo>";

                Warning[] warnings;
                string[] streams;

                //Render the report
                var renderedBytes = localReport.Render(
                    reportType,
                    deviceInfo,
                    out mimeType,
                    out encoding,
                    out fileNameExtension,
                    out streams,
                    out warnings);
                Response.AddHeader("content-disposition", "attachment; filename=ShiftDataDump." + fileNameExtension);
                _cache.Log("User " + User.Identity.Name + " requested ExcelDataDump Report -> Mime: " + mimeType + ", File Extension: " + fileNameExtension, "ExcelDataDump", CacheHelper.LogTypes.Info, User.Identity.Name);
                return File(renderedBytes, mimeType);
            }
            catch (Exception err)
            {
                _cache.Log("Faile to generate report: " + err.Message + "|" + err.InnerException.Message, "ExcelDataDump", CacheHelper.LogTypes.Error, User.Identity.Name);
                Response.StatusCode = 500;
                return Json(new { error = err.Message });
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        private List<BigExcelDump> GetReportData(int shiftId, int type)
        {
            bool customShift = type == 2;
            
            //Captured Torque Data
                var data = (from x in _db.as_shiftData
                            join y in _db.as_shifts on x.i_shiftId equals y.i_shiftId
                            join z in _db.as_assetProfile on x.i_assetId equals z.i_assetId
                            join a in _db.as_locationProfile on z.i_locationId equals a.i_locationId
                            join b in _db.as_areaSubProfile on a.i_areaSubId equals b.i_areaSubId
                            join c in _db.as_areaProfile on b.i_areaId equals c.i_areaId
                            join d in _db.as_assetClassProfile on z.i_assetClassId equals d.i_assetClassId
                            where x.i_shiftId == shiftId && y.bt_custom == customShift
                            select new
                            {
                                x.dt_captureDate, x.f_capturedValue, x.i_assetCheckId, y.bt_completed, y.dt_scheduledDate, y.dt_completionDate, y.vc_permitNumber, z.vc_rfidTag, z.vc_serialNumber,
                                assetClass = d.vc_description, d.vc_manufacturer, d.vc_model, a.f_latitude, a.f_longitude, a.vc_designation,
                                subArea = b.vc_description,
                                mainArea = c.vc_description
                            }).ToList();

                List<BigExcelDump> returnList = data.Select(item => new BigExcelDump
                {
                    assetClass = item.assetClass, bt_completed = item.bt_completed, dt_captureDate = item.dt_captureDate, dt_completionDate = item.dt_completionDate, dt_scheduledDate = item.dt_scheduledDate, f_capturedValue = item.f_capturedValue.ToString(CultureInfo.InvariantCulture), f_latitude = item.f_latitude, f_longitude = item.f_longitude, i_assetCheckId = item.i_assetCheckId, mainArea = item.mainArea, subArea = item.subArea, vc_designation = item.vc_designation, vc_manufacturer = item.vc_manufacturer, vc_model = item.vc_model, vc_permitNumber = item.vc_permitNumber, vc_rfidTag = item.vc_rfidTag, vc_serialNumber = item.vc_serialNumber
                }).ToList();

            var validation = (from x in _db.as_validationTaskProfile
                        join y in _db.as_shifts on x.i_shiftId equals y.i_shiftId
                        join z in _db.as_assetProfile on x.i_assetId equals z.i_assetId
                        join a in _db.as_locationProfile on z.i_locationId equals a.i_locationId
                        join b in _db.as_areaSubProfile on a.i_areaSubId equals b.i_areaSubId
                        join c in _db.as_areaProfile on b.i_areaId equals c.i_areaId
                        join d in _db.as_assetClassProfile on z.i_assetClassId equals d.i_assetClassId
                        where x.i_shiftId == shiftId && y.bt_custom == customShift
                        select new
                        {
                            dt_captureDate = x.dt_dateTimeStamp,
                            capturedValue = x.bt_validated,
                            i_assetCheckId = 0, y.bt_completed, y.dt_scheduledDate, y.dt_completionDate, y.vc_permitNumber, z.vc_rfidTag, z.vc_serialNumber,
                            assetClass = d.vc_description, d.vc_manufacturer, d.vc_model, a.f_latitude, a.f_longitude, a.vc_designation,
                            subArea = b.vc_description,
                            mainArea = c.vc_description
                        }).ToList();

            returnList.AddRange(validation.Select(item => new BigExcelDump
            {
                assetClass = item.assetClass, bt_completed = item.bt_completed, dt_captureDate = item.dt_captureDate, dt_completionDate = item.dt_completionDate, dt_scheduledDate = item.dt_scheduledDate, f_capturedValue = item.capturedValue ? "Validated" : "Not Validated", f_latitude = item.f_latitude, f_longitude = item.f_longitude, i_assetCheckId = item.i_assetCheckId, mainArea = item.mainArea, subArea = item.subArea, vc_designation = item.vc_designation, vc_manufacturer = item.vc_manufacturer, vc_model = item.vc_model, vc_permitNumber = item.vc_permitNumber, vc_rfidTag = item.vc_rfidTag, vc_serialNumber = item.vc_serialNumber
            }));

            return returnList;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public ActionResult ShiftReport(int shiftId, int type)
        {
            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=airsidereporting;AccountKey=mCK8CqoLGGIu1c3BQ8BQEI4OtIKllkiwJQv4lMB4A6811TxLXsYzTITL8W7Z2gMztfrkbLUFuqDSe6+ZzPTGpg==");
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference("reportcontent");
                CloudBlockBlob blockBlob = container.GetBlockBlobReference("ShiftReport.rdlc");

                LocalReport localReport = new LocalReport();

                using (var memoryStream = new MemoryStream())
                {
                    blockBlob.DownloadToStream(memoryStream);
                    memoryStream.Position = 0;
                    localReport.LoadReportDefinition(memoryStream);
                }

                ReportDataSource reportDataSource = new ReportDataSource("ShiftDS", GetShiftReportData(shiftId, type));
                localReport.DataSources.Add(reportDataSource);

                string reportType = "PDF";
                string mimeType;
                string encoding;
                string fileNameExtension;

                string deviceInfo =
                "<DeviceInfo>" +
                "  <OutputFormat>PDF</OutputFormat>" +
                "  <PageWidth>210mm</PageWidth>" +
                "  <PageHeight>297mm</PageHeight>" +
                "  <MarginTop>1cm</MarginTop>" +
                "  <MarginLeft>1cm</MarginLeft>" +
                "  <MarginRight>1cm</MarginRight>" +
                "  <MarginBottom>1cm</MarginBottom>" +
                "</DeviceInfo>";

                Warning[] warnings;
                string[] streams;

                //Render the report
                var renderedBytes = localReport.Render(
                    reportType,
                    deviceInfo,
                    out mimeType,
                    out encoding,
                    out fileNameExtension,
                    out streams,
                    out warnings);
                Response.AddHeader("content-disposition", "attachment; filename=ShiftReport." + fileNameExtension);
                _cache.Log("User " + User.Identity.Name + " requested PDF ShiftReport Report -> Mime: " + mimeType + ", File Extension: " + fileNameExtension, "ShiftReport", CacheHelper.LogTypes.Info, User.Identity.Name);
                return File(renderedBytes, mimeType);
            }
            catch (Exception err)
            {
                _cache.Log("ShiftReport Error: " + err.Message + " Inner: " + err.InnerException, "ShiftReport", CacheHelper.LogTypes.Error, User.Identity.Name);
                Response.StatusCode = 500;
                return Json(new { error = err.Message });
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        private List<ActiveShiftsReport> GetShiftReportData(int shiftId, int type)
        {
            bool customShift = type == 2;

            var data = from x in _db.as_shifts
                        join y in _db.as_shiftData on x.i_shiftId equals y.i_shiftId
                        join z in _db.as_technicianGroups on x.i_technicianGroup equals z.i_groupId
                        join a in _db.as_areaSubProfile on x.i_areaSubId equals a.i_areaSubId
                        join b in _db.as_areaProfile on a.i_areaId equals b.i_areaId
                        where x.i_shiftId == shiftId && x.bt_custom == customShift
                        select new
                        {
                            x.dt_scheduledDate, z.vc_groupName, z.vc_externalRef, x.vc_permitNumber, x.bt_completed,
                            area = b.vc_description,
                            subArea = a.vc_description
                        };

            var returnList = new List<ActiveShiftsReport>();

            foreach (var item in data)
            {
                ActiveShiftsReport newShift = new ActiveShiftsReport
                {
                    area = item.area,
                    bt_completed = item.bt_completed,
                    dt_scheduledDate = item.dt_scheduledDate,
                    subArea = item.subArea,
                    vc_externalRef = item.vc_externalRef,
                    vc_groupName = item.vc_groupName,
                    vc_permitNumber = item.vc_permitNumber
                };

                returnList.Add(newShift);
            }
            return returnList;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        
        #endregion

        #region Event Report

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        public ActionResult EventReport(int shiftId, int type)
        {
            try
            {
                //Create Report Settings
                ReportSettings settings = new ReportSettings
                {
                    blobContainer = "reportcontent",
                    blobReference = "EventReport.rdlc",
                    fileType = ReportFileTypes.pdf,
                    dataSources = new ReportDataSource[4]
                };

                //Prepare Data Sources for Report
                ReportDataSource reportDataSource = new ReportDataSource("SheduleTasks", GetEventSheduleData(shiftId, type));
                settings.dataSources[0] = reportDataSource;

                reportDataSource = new ReportDataSource("AssetInfo", GetEventAssetInfo(shiftId, type));
                settings.dataSources[1] = reportDataSource;

                reportDataSource = new ReportDataSource("ReportInfo", GetEventReportInfo(shiftId, type));
                settings.dataSources[2] = reportDataSource;

                reportDataSource = new ReportDataSource("CheckList", GetEventCheckList(shiftId, type));
                settings.dataSources[3] = reportDataSource;

                settings.reportName = "EventReport";

                //Set the host reference for the logo
                string[] host = Request.Headers["Host"].Split('.');
                settings.logoReference = host[0];

                //Create Report Object 
                ReportingHelper report = new ReportingHelper();

                //Render the report
                ReportBytes renderedReport = report.GenerateReport(settings);

                Response.AddHeader(renderedReport.header.name, renderedReport.header.value);
                _cache.Log("User " + User.Identity.Name + " requested Event Report -> Mime: " + renderedReport.mimeType, "EventReport", CacheHelper.LogTypes.Info, User.Identity.Name);
                return File(renderedReport.renderedBytes, "application/pdf ");
            }
            catch (Exception ex)
            {
                _cache.LogError(ex, User.Identity.Name);
                Response.StatusCode = 500;
                return null;
            }

        }


        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        private List<EventScheduleData> GetEventSheduleData(int shiftId, int type)
        {
            if (type <= 0) throw new ArgumentOutOfRangeException(nameof(type));
            List<EventScheduleData> returnList = new List<EventScheduleData>();

            var shiftObject = _db.as_shifts.Find(shiftId);

            var userOpen = _db.UserProfiles.Find(shiftObject.i_createdBy);
            var userClose = _db.UserProfiles.Find(shiftObject.i_closedBy);
            var techGroup = _db.as_technicianGroups.Find(shiftObject.i_technicianGroup);

            var maintenance = _db.as_maintenanceProfile.Find(shiftObject.i_maintenanceId);

            string shiftType = "";

            if (maintenance.i_maintenanceValidationId == 1) shiftType = "Validation";
            else if (maintenance.i_maintenanceValidationId == 2) shiftType = "Torque";

            //Event Created
            EventScheduleData item = new EventScheduleData
            {
                DateOfEvent = shiftObject.dt_dateCreated.ToString("yyyy/MM/dd"),
                TimeOfEvent = shiftObject.dt_dateCreated.ToString("HH:mm:ss"),
                Description =
                    shiftType + " shift created by " + userOpen.FirstName + " " + userOpen.LastName +
                    ". The shift was created to " + maintenance.vc_description + " on " +
                    shiftObject.dt_scheduledDate.ToString("yyy/MM/dd HH:mm")
            };
            returnList.Add(item);
            

            DateTime firstDate = new DateTime(1970,1,1);
            DateTime lastDate = new DateTime(1970,1,1);

            if(maintenance.i_maintenanceValidationId == 1)
            {
                int count = _db.as_shiftData.Count(q => q.i_shiftId == shiftId);
                if (count > 0)
                {
                    var shiftData = _db.as_validationTaskProfile.Where(q => q.i_shiftId == shiftId);
                    firstDate = shiftData.OrderBy(q => q.dt_dateTimeStamp).First().dt_dateTimeStamp;
                    lastDate = shiftData.OrderByDescending(q => q.dt_dateTimeStamp).First().dt_dateTimeStamp;
                }
            }
            else if (maintenance.i_maintenanceValidationId == 2)
            {
                int count = _db.as_shiftData.Count(q => q.i_shiftId == shiftId);
                if (count > 0)
                {
                    var shiftData = _db.as_shiftData.Where(q => q.i_shiftId == shiftId).OrderBy(q => q.dt_captureDate);
                    firstDate = shiftData.OrderBy(q => q.dt_captureDate).First().dt_captureDate;
                    lastDate = shiftData.OrderByDescending(q => q.dt_captureDate).First().dt_captureDate;
                }
            }

            //First Event
            item = new EventScheduleData
            {
                DateOfEvent = firstDate.ToString("yyyy/MM/dd"),
                TimeOfEvent = firstDate.ToString("HH:mm:ss"),
                Description = "The first asset was maintained by " + techGroup.vc_groupName
            };
            returnList.Add(item);

            //Last Event
            item = new EventScheduleData
            {
                DateOfEvent = lastDate.ToString("yyyy/MM/dd"),
                TimeOfEvent = lastDate.ToString("HH:mm:ss"),
                Description = "The last asset was maintained by " + techGroup.vc_groupName
            };
            returnList.Add(item);

            //Event Closed By
            item = new EventScheduleData
            {
                DateOfEvent = shiftObject.dt_completionDate.ToString("yyyy/MM/dd"),
                TimeOfEvent = shiftObject.dt_completionDate.ToString("HH:mm:ss"),
                Description = "The shift was closed by " + userClose.FirstName + " " + userClose.LastName + "."
            };
            returnList.Add(item);

            return returnList;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        private async Task<List<EventAssetInfo>> GetEventAssetInfo(int shiftId, int type)
        {
            if (type <= 0) throw new ArgumentOutOfRangeException(nameof(type));
            List<EventAssetInfo> returnList = new List<EventAssetInfo>();

            var shift = _db.as_shifts.Find(shiftId);
            var maintenance = _db.as_maintenanceProfile.Find(shift.i_maintenanceId);

            var assetStatus = await _cache.GetAssetStatusHistory();

            if(maintenance.i_maintenanceValidationId == 2)
            {
                var shiftData = _db.as_shiftData.Where(q => q.i_shiftId == shiftId);

                var dataSet = from x in shiftData
                              group x by x.i_assetId into asset
                              select new { 
                                asset = asset.Key
                              };

                foreach(var item in dataSet)
                {
                    EventAssetInfo info = new EventAssetInfo();
                    var asset = _db.as_assetProfile.Find(item.asset);
                    var assetClass = _db.as_assetClassProfile.Find(asset.i_assetClassId);
                    var dateOfCapture = _db.as_shiftData.FirstOrDefault(q => q.i_shiftId == shiftId && q.i_assetId == item.asset);

                    var status = assetStatus.FirstOrDefault(q => dateOfCapture != null && (q.lastValid.Date <= dateOfCapture.dt_captureDate.Date && q.assetId == item.asset));

                    info.AssetName = asset.vc_serialNumber;
                    info.CurrentState = "---";
                    if (status != null)
                    {
                        switch (status.previousCycle)
                        {
                            case 0: info.PriorState = "No Data (Blue)";
                                break;
                            case 1: info.PriorState = "Recently Updated (Green)";
                                break;
                            case 2: info.PriorState = "Mid Cycle (Yellow)";
                                break;
                            case 3: info.PriorState = "Almost Due (Orange)";
                                break;
                            case 4: info.PriorState = "Over Due (Red)";
                                break;
                        }
                    } else
                    {
                        info.PriorState = "No Data (Blue)";
                    }
                    if (dateOfCapture != null)
                        info.DateOfEvent = dateOfCapture.dt_captureDate.ToString("yyy/MM/dd HH:mm");
                    info.TypeOfAsset = assetClass.vc_description;
                    info.ValidationType = "Torquing";
                    returnList.Add(info);
                }
            } else if(maintenance.i_maintenanceValidationId == 1)
            {
                var shiftData = _db.as_validationTaskProfile.Where(q => q.i_shiftId == shiftId);
                var dataSet = from x in shiftData
                              select new
                              {
                                  assetId = x.i_assetId,
                                  maintenanceDate = x.dt_dateTimeStamp,
                              };

                foreach (var item in dataSet)
                {
                    EventAssetInfo info = new EventAssetInfo();
                    var asset = _db.as_assetProfile.Find(item.assetId);
                    var assetClass = _db.as_assetClassProfile.Find(asset.i_assetClassId);

                    var status = assetStatus.FirstOrDefault(q => q.lastValid.Date == item.maintenanceDate.Date && q.assetId == item.assetId);

                    info.AssetName = asset.vc_serialNumber;
                    info.CurrentState = "---";
                    if (status != null)
                    {
                        switch (status.previousCycle)
                        {
                            case 0: info.PriorState = "No Data (Blue)";
                                break;
                            case 1: info.PriorState = "Recently Updated (Green)";
                                break;
                            case 2: info.PriorState = "Mid Cycle (Yellow)";
                                break;
                            case 3: info.PriorState = "Almost Due (Orange)";
                                break;
                            case 4: info.PriorState = "Over Due (Red)";
                                break;
                        }
                    } else
                        info.PriorState = "No Data (Blue)";

                    info.DateOfEvent = item.maintenanceDate.ToString("yyy/MM/dd HH:mm");
                    info.TypeOfAsset = assetClass.vc_description;
                    info.ValidationType = "Scan Asset";
                    returnList.Add(info);
                }
            }
           

            return returnList;
        }
        
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        private List<EventCheckList> GetEventCheckList(int shiftId, int type)
        {
            List<EventCheckList> returnList = new List<EventCheckList>();
            bool customShift = type == 2;
           
            int maintenanceId = (from x in _db.as_shifts
                                    where x.i_shiftId == shiftId && x.bt_custom == customShift
                                    select x.i_maintenanceId).FirstOrDefault();

            var checkList = from x in _db.as_maintenanceCheckListDef
                            where x.i_maintenanceId == maintenanceId
                            select x.vc_description;

            foreach(var item in checkList)
            {
                EventCheckList listItem = new EventCheckList {ListItem = item};

                returnList.Add(listItem);
            }

            return returnList;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        private List<EventReportInfo> GetEventReportInfo(int shiftId, int type)
        {
            List<EventReportInfo> returnList = new List<EventReportInfo>();
            bool customShift = type == 2;

            var shift = _db.as_shifts.Find(shiftId);
            var maintenance = _db.as_maintenanceProfile.Find(shift.i_maintenanceId);

            if (maintenance.i_maintenanceValidationId == 2)
            {
                int count = (from x in _db.as_shiftData
                             join y in _db.as_shifts on x.i_shiftId equals y.i_shiftId
                             where y.i_shiftId == shiftId && y.bt_custom == customShift
                             select x).Count();

                if (count > 0)
                {
                    var shiftData = from x in _db.as_shiftData
                                    join y in _db.as_shifts on x.i_shiftId equals y.i_shiftId
                                    where y.i_shiftId == shiftId && y.bt_custom == customShift
                                    select new
                                    {
                                        MaintenanceId = y.i_maintenanceId,
                                        SheduledDate = y.dt_scheduledDate,
                                        AssetId = x.i_assetId,
                                        AreaId = y.i_areaSubId,
                                        TechGroupId = y.i_technicianGroup
                                    };

                    var firstOrDefault = shiftData.FirstOrDefault();
                    if (firstOrDefault != null)
                    {
                        int areaId = firstOrDefault.AreaId;
                        int totalAssets;

                        if (areaId != 0)
                            totalAssets = (from x in _db.as_assetProfile
                                join y in _db.as_locationProfile on x.i_locationId equals y.i_locationId
                                where y.i_areaSubId == areaId
                                select x).Count();
                        else
                            totalAssets = _db.as_shiftsCustomProfile.Count(q => q.i_shiftId == shiftId);

                        int completedAssets = (from x in shiftData
                            group x by x.AssetId into assetGroup
                            select new
                            {
                                numberAssets = assetGroup.Count()
                            }).Count();

                        int techGroupId = firstOrDefault.TechGroupId;

                        string technicianGroup = (from x in _db.as_technicianGroups
                            where x.i_groupId == techGroupId
                            select x.vc_groupName).FirstOrDefault();

                        decimal percentage = Math.Round(completedAssets / (decimal)totalAssets * 100, 0);

                        string eventDate = firstOrDefault.SheduledDate.ToString("yyyy/MM/dd");

                        int maintenanceId = firstOrDefault.MaintenanceId;

                        string maintenanceTask = (from x in _db.as_maintenanceProfile
                            where x.i_maintenanceId == maintenanceId
                            select x.vc_description).FirstOrDefault();

                        EventReportInfo info = new EventReportInfo
                        {
                            EventDate = eventDate,
                            MaintenanceTask = maintenanceTask,
                            NumberOfAssets = totalAssets,
                            PercentageComplete = percentage,
                            PercentageNotComplete = 100 - percentage,
                            TechGroup = technicianGroup
                        };

                        returnList.Add(info);
                    }
                }
            } else if(maintenance.i_maintenanceValidationId == 1)
            {
                int count = (from x in _db.as_validationTaskProfile
                             join y in _db.as_shifts on x.i_shiftId equals y.i_shiftId
                             where y.i_shiftId == shiftId && y.bt_custom == customShift
                             select x).Count();

                if (count > 0)
                {
                    var shiftData = from x in _db.as_validationTaskProfile
                                    join y in _db.as_shifts on x.i_shiftId equals y.i_shiftId
                                    where y.i_shiftId == shiftId && y.bt_custom == customShift
                                    select new
                                    {
                                        MaintenanceId = y.i_maintenanceId,
                                        SheduledDate = y.dt_scheduledDate,
                                        AssetId = x.i_assetId,
                                        AreaId = y.i_areaSubId,
                                        TechGroupId = y.i_technicianGroup
                                    };

                    var firstOrDefault = shiftData.FirstOrDefault();
                    if (firstOrDefault != null)
                    {
                        int areaId = firstOrDefault.AreaId;
                        int totalAssets;

                        if (areaId != 0)
                            totalAssets = (from x in _db.as_assetProfile
                                join y in _db.as_locationProfile on x.i_locationId equals y.i_locationId
                                where y.i_areaSubId == areaId
                                select x).Count();
                        else
                            totalAssets = _db.as_shiftsCustomProfile.Count(q => q.i_shiftId == shiftId);

                        int completedAssets = (from x in shiftData
                            group x by x.AssetId into assetGroup
                            select new
                            {
                                numberAssets = assetGroup.Count()
                            }).Count();

                        int techGroupId = firstOrDefault.TechGroupId;

                        string technicianGroup = (from x in _db.as_technicianGroups
                            where x.i_groupId == techGroupId
                            select x.vc_groupName).FirstOrDefault();

                        decimal percentage = Math.Round(completedAssets / (decimal)totalAssets * 100, 0);

                        string eventDate = firstOrDefault.SheduledDate.ToString("yyyy/MM/dd");

                        int maintenanceId = firstOrDefault.MaintenanceId;

                        string maintenanceTask = (from x in _db.as_maintenanceProfile
                            where x.i_maintenanceId == maintenanceId
                            select x.vc_description).FirstOrDefault();

                        EventReportInfo info = new EventReportInfo
                        {
                            EventDate = eventDate,
                            MaintenanceTask = maintenanceTask,
                            NumberOfAssets = totalAssets,
                            PercentageComplete = percentage,
                            PercentageNotComplete = 100 - percentage,
                            TechGroup = technicianGroup
                        };

                        returnList.Add(info);
                    }
                }
            }
          
            return returnList;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        #endregion
    }
}