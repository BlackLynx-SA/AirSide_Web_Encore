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

using ADB.AirSide.Encore.V1.Models;
using AirSide.ServerModules.Helpers;
using AirSide.ServerModules.Models;
using Microsoft.Reporting.WebForms;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ADB.AirSide.Encore.V1.Controllers
{
    [Authorize]
    public class ShiftsController : Controller
    {
        private readonly LogHelper logger = new LogHelper();
        private Entities db = new Entities();

        public ActionResult Calendar()
        {
            ViewBag.maintenanceTasks = new SelectList(db.as_maintenanceProfile.OrderBy(q => q.vc_description).Distinct(), "i_maintenanceId", "vc_description");
            return View();
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public ActionResult MaintenanceTasks()
        {
            ViewBag.maintenanceValidation = new SelectList(db.as_maintenanceValidation.OrderBy(q => q.vc_validationName).Distinct(), "i_maintenanceValidationId", "vc_validationName");
            return View();
        }

        #region Custom Shifts

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult addCustomShift(CustomShiftClass shift)
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

                as_shiftsCustom newShift = new as_shiftsCustom();
                newShift.bt_completed = false;
                newShift.dt_completionDate = new DateTime(1970, 1, 1);
                newShift.dt_scheduledDate = DateTime.ParseExact(shift.scheduledDate, "dd/MM/yyyy h:mm tt", provider);
                newShift.i_maintenanceId = shift.maintenanceId;
                newShift.i_techGroupId = shift.techGroupId;
                newShift.vc_externalReference = shift.externalRef;
                newShift.vc_permitNumber = shift.permitNumber;

                db.as_shiftsCustom.Add(newShift);
                db.SaveChanges();

                List<int> assets = findAssets(shift);
                foreach(int asset in assets)
                {
                    as_shiftsCustomProfile shiftProfile = new as_shiftsCustomProfile();
                    shiftProfile.i_assetId = asset;
                    shiftProfile.i_shiftCustomId = newShift.i_shiftCustomId;
                    db.as_shiftsCustomProfile.Add(shiftProfile);
                }

                db.SaveChanges();

                //update iOS Cache Hash
                CacheHelper cacheHelp = new CacheHelper();
                cacheHelp.updateiOSCache("getTechnicianShifts");

                return Json(new { message = "Success", count = assets.Count() });
            }
            catch (Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to insert custom shift: " + err.Message, "addCustomShift", LogHelper.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //2015/01/19 Custom Shift Helpers----------------------------------------------------------------------------------------------------------------------------
        private List<int> findAssets(CustomShiftClass shift)
        {
            try
            {
                //This helper function takes the parameters send by the frontend and calculates which assets it applies to
                //Create Date: 2015/01/19
                //Author: Bernard Willer

                List<int> assets = new List<int>(); ;

                switch (shift.filterType)
                {   case 101:   //Process All
                        assets = processAllAssets(shift);
                        break;
                    case 102: //Process Asset Type
                        assets = processAssetType(shift);
                        break;
                    case 103: //Process Maintneace Cycle
                        assets = processAssetCycle(shift);
                        break;
                    case 104: //Main Area
                        assets = processMainArea(shift);
                        break;
                    case 105: //Sub Area
                        assets = processSubArea(shift);
                        break;
                    case 106: //Faulty Lights
                        assets = processFaultyLights(shift);
                        break;
                    case 107: //Visual Surveyor => TODO: Need to build it out for reported other than just assets
                        assets = processVisualSurveyor(shift);
                        break;
                    default:
                        break;
                }
                return assets;
            }
            catch (Exception err)
            {
                List<int> assets = new List<int>();
                LogHelper log = new LogHelper();
                log.logError(err, User.Identity.Name + "(" + Request.UserHostAddress + ")");
                return assets;
            }
        
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        private class assetObject
        {
            public int assetId { get; set; }
            public double latitude { get; set; }
            public double longitude { get; set; }
        }

        private List<int> processVisualSurveyor(CustomShiftClass shift)
        {
            //Disseminate the date range
            string[] dates = shift.dateRange.Split(char.Parse("-"));
            DateTime startDate = DateTime.ParseExact(dates[0], "yyyy/MM/dd", CultureInfo.InvariantCulture);
            DateTime endDate = DateTime.ParseExact(dates[1], "yyyy/MM/dd", CultureInfo.InvariantCulture);

            List<int> assetList = new List<int>();
            List<assetObject> assets = new List<assetObject>();

            //Get Images Elements
            if(shift.imageChk)
            {
                var assetSelect = from x in db.as_fileUploadProfile
                                  join y in db.as_fileUploadInfo on x.guid_file equals y.guid_file
                                  join z in db.as_locationProfile on new { latitude = Math.Round(y.f_latitude, 5), longitude = Math.Round(y.f_longitude, 5) } equals new { latitude = Math.Round(z.f_latitude, 5), longitude = Math.Round(z.f_longitude, 5) }
                                  join a in db.as_assetProfile on z.i_locationId equals a.i_locationId
                                  where x.i_fileType == 1 && y.bt_resolved == false && x.dt_datetime >= startDate && x.dt_datetime <= endDate
                                  select new
                                  {
                                    assetId = a.i_assetId,
                                    latitude = z.f_latitude,
                                    longitude = z.f_longitude
                                  };

                foreach(var item in assetSelect)
                {
                    assetObject obj = new assetObject();
                    obj.assetId = item.assetId;
                    obj.latitude = item.latitude;
                    obj.longitude = item.longitude;

                    assets.Add(obj);
                }
            }

            //Get Voice Elements
            if (shift.voiceChk)
            {
                var assetSelect = from x in db.as_fileUploadProfile
                                  join y in db.as_fileUploadInfo on x.guid_file equals y.guid_file
                                  join z in db.as_locationProfile on new { latitude = Math.Round(y.f_latitude, 5), longitude = Math.Round(y.f_longitude, 5) } equals new { latitude = Math.Round(z.f_latitude, 5), longitude = Math.Round(z.f_longitude, 5) }
                                  join a in db.as_assetProfile on z.i_locationId equals a.i_locationId
                                  where x.i_fileType == 2 && y.bt_resolved == false && x.dt_datetime >= startDate && x.dt_datetime <= endDate
                                  select new
                                  {
                                      assetId = a.i_assetId,
                                      latitude = z.f_latitude,
                                      longitude = z.f_longitude
                                  };

                foreach (var item in assetSelect)
                {
                    assetObject obj = new assetObject();
                    obj.assetId = item.assetId;
                    obj.latitude = item.latitude;
                    obj.longitude = item.longitude;

                    assets.Add(obj);
                }
            }

            //Get Images Elements
            if (shift.textChk)
            {
                var assetSelect = from x in db.as_fileUploadProfile
                                  join y in db.as_fileUploadInfo on x.guid_file equals y.guid_file
                                  join z in db.as_locationProfile on new { latitude = Math.Round(y.f_latitude, 5), longitude = Math.Round(y.f_longitude, 5) } equals new { latitude = Math.Round(z.f_latitude, 5), longitude = Math.Round(z.f_longitude, 5) }
                                  join a in db.as_assetProfile on z.i_locationId equals a.i_locationId
                                  where x.i_fileType == 3 && y.bt_resolved == false && x.dt_datetime >= startDate && x.dt_datetime <= endDate
                                  select new
                                  {
                                      assetId = a.i_assetId,
                                      latitude = z.f_latitude,
                                      longitude = z.f_longitude
                                  };

                foreach (var item in assetSelect)
                {
                    assetObject obj = new assetObject();
                    obj.assetId = item.assetId;
                    obj.latitude = item.latitude;
                    obj.longitude = item.longitude;

                    assets.Add(obj);
                }
            }


            logger.quickDebugLog(assets.Count.ToString());

            foreach (var asset in assets)
            {
                Boolean latFlag = false;
                Boolean longFlag = false;
                
                if (asset.latitude >= shift.SWLat && asset.latitude <= shift.NELat) latFlag = true;
                if (asset.longitude >= shift.SWLong && asset.longitude <= shift.NELong) longFlag = true;

                if (latFlag && longFlag)
                {
                    assetList.Add(asset.assetId);
                }
            }

            return assetList;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        private List<int> processFaultyLights(CustomShiftClass shift)
        {
            CacheHelper cache = new CacheHelper();
            List<mongoAssetProfile> assets = cache.getAllAssets();
            List<int> assetList = new List<int>();
            foreach (var asset in assets)
            {
                Boolean latFlag = false;
                Boolean longFlag = false;

                if (asset.location.latitude >= shift.SWLat && asset.location.latitude <= shift.NELat) latFlag = true;
                if (asset.location.longitude >= shift.SWLong && asset.location.longitude <= shift.NELong) longFlag = true;

                if (latFlag && longFlag)
                {
                    if(asset.status)
                        assetList.Add(asset.assetId);
                }
            }

            return assetList;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        private List<int> processAssetCycle(CustomShiftClass shift)
        {
            CacheHelper cache = new CacheHelper();
            List<mongoAssetProfile> assets = cache.getAllAssets();
            List<int> assetList = new List<int>();
            foreach (var asset in assets)
            {
                Boolean latFlag = false;
                Boolean longFlag = false;

                if (asset.location.latitude >= shift.SWLat && asset.location.latitude <= shift.NELat) latFlag = true;
                if (asset.location.longitude  >= shift.SWLong && asset.location.longitude <= shift.NELong) longFlag = true;

                if (latFlag && longFlag)
                {
                    foreach (var item in asset.maintenance)
                    {
                        if(item.maintenanceCycle == shift.filterValue && item.maintenanceId == shift.maintenanceFilter)
                            assetList.Add(asset.assetId);
                    }
                }
            }

            return assetList;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        private List<int> processSubArea(CustomShiftClass shift)
        {
            CacheHelper cache = new CacheHelper();
            List<mongoFullAsset> assets = cache.getAllAssetDownload();
            List<int> assetList = new List<int>();
            foreach (var asset in assets)
            {
                Boolean latFlag = false;
                Boolean longFlag = false;

                if (asset.latitude >= shift.SWLat && asset.latitude <= shift.NELat) latFlag = true;
                if (asset.longitude >= shift.SWLong && asset.longitude <= shift.NELong) longFlag = true;

                if (latFlag && longFlag && asset.subAreaId == shift.filterValue)
                {
                    int task = (from x in db.as_assetClassProfile
                                join y in db.as_assetProfile on x.i_assetClassId equals y.i_assetClassId
                                join z in db.as_assetClassMaintenanceProfile on x.i_assetClassId equals z.i_assetClassId
                                where y.i_assetId == asset.assetId && z.i_maintenanceId == shift.maintenanceFilter
                                select x.i_assetClassId).DefaultIfEmpty(0).FirstOrDefault();
                    if (task > 0)
                        assetList.Add(asset.assetId);
                }
            }

            return assetList;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        private List<int> processMainArea(CustomShiftClass shift)
        {
            CacheHelper cache = new CacheHelper();
            List<mongoAssetProfile> assets = cache.getAllAssets();
            List<int> assetList = new List<int>();
            foreach (var asset in assets)
            {
                Boolean latFlag = false;
                Boolean longFlag = false;

                if (asset.location.latitude >= shift.SWLat && asset.location.latitude <= shift.NELat) latFlag = true;
                if (asset.location.longitude >= shift.SWLong && asset.location.longitude <= shift.NELong) longFlag = true;

                if (latFlag && longFlag && asset.location.areaId == shift.filterValue)
                {
                    int task = (from x in db.as_assetClassProfile
                                join y in db.as_assetProfile on x.i_assetClassId equals y.i_assetClassId
                                join z in db.as_assetClassMaintenanceProfile on x.i_assetClassId equals z.i_assetClassId
                                where y.i_assetId == asset.assetId && z.i_maintenanceId == shift.maintenanceFilter
                                select x.i_assetClassId).DefaultIfEmpty(0).FirstOrDefault();
                    if (task > 0)
                        assetList.Add(asset.assetId);
                }
            }

            return assetList;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        private List<int> processAssetType(CustomShiftClass shift)
        {
            CacheHelper cache = new CacheHelper();
            List<mongoFullAsset> assets = cache.getAllAssetDownload();
            List<int> assetList = new List<int>();
            foreach (var asset in assets)
            {
                Boolean latFlag = false;
                Boolean longFlag = false;

                if (asset.latitude >= shift.SWLat && asset.latitude <= shift.NELat) latFlag = true;
                if (asset.longitude >= shift.SWLong && asset.longitude <= shift.NELong) longFlag = true;

                if (latFlag && longFlag && asset.assetClassId == shift.filterValue)
                {
                    int task = (from x in db.as_assetClassProfile
                                join y in db.as_assetProfile on x.i_assetClassId equals y.i_assetClassId
                                join z in db.as_assetClassMaintenanceProfile on x.i_assetClassId equals z.i_assetClassId
                                where y.i_assetId == asset.assetId && z.i_maintenanceId == shift.maintenanceFilter
                                select x.i_assetClassId).DefaultIfEmpty(0).FirstOrDefault();
                    if (task > 0)
                        assetList.Add(asset.assetId);
                }
            }

            return assetList;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        private List<int> processAllAssets(CustomShiftClass shift)
        {
            CacheHelper cache = new CacheHelper();
            List<mongoFullAsset> assets = cache.getAllAssetDownload();
            List<int> assetList = new List<int>();
            foreach(var asset in assets)
            {
                Boolean latFlag = false;
                Boolean longFlag = false;

                if (asset.latitude >= shift.SWLat && asset.latitude <= shift.NELat) latFlag = true;
                if (asset.longitude >= shift.SWLong && asset.longitude <= shift.NELong) longFlag = true;

                if(latFlag && longFlag)
                {
                    int task = (from x in db.as_assetClassProfile
                                join y in db.as_assetProfile on x.i_assetClassId equals y.i_assetClassId
                                join z in db.as_assetClassMaintenanceProfile on x.i_assetClassId equals z.i_assetClassId
                                where y.i_assetId == asset.assetId && z.i_maintenanceId == shift.maintenanceFilter
                                select x.i_assetClassId).DefaultIfEmpty(0).FirstOrDefault();
                    if (task > 0)
                        assetList.Add(asset.assetId);
                }
            }

            return assetList;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        #endregion

        #region Area Based Shifts

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult getAllTechnicianGroups()
        {
            try
            {
                var techGroups = db.as_technicianGroups.OrderBy(q => q.vc_groupName).ToList();
                return Json(techGroups);
            }
            catch(Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to retrieve Technician Groups: " + err.Message, "getAllTechnicianGroups", LogHelper.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult getAllShifts()
        {
            try
            {
                var shifts = (from x in db.as_shifts
                              join y in db.as_areaSubProfile on x.i_areaSubId equals y.i_areaSubId
                              join z in db.as_areaProfile on y.i_areaId equals z.i_areaId
                              join a in db.as_technicianGroups on x.UserId equals a.i_groupId
                              join b in db.as_maintenanceProfile on x.i_maintenanceId equals b.i_maintenanceId
                              join c in db.as_maintenanceValidation on b.i_maintenanceValidationId equals c.i_maintenanceValidationId
                              where x.bt_completed == false
                              select new { 
                                description = z.vc_description + "(" + y.vc_description + ")",
                                dateTime = x.dt_scheduledDate,
                                groupName = a.vc_groupName,
                                validation = c.i_maintenanceValidationId
                              }).ToList();

                List<shiftInfo> shiftList = new List<shiftInfo>();
                foreach(var item in shifts)
                {
                    shiftInfo newItem = new shiftInfo();
                    newItem.description = item.description;
                    newItem.dateTime = item.dateTime.ToString("dd-MM-yyyy h:mm tt");
                    newItem.groupName = item.groupName;
                    newItem.validationId = item.validation;

                    //Push to list
                    shiftList.Add(newItem);
                }

                //Custom Shifts
                var customShifts = (from x in db.as_shiftsCustom
                                    join y in db.as_technicianGroups on x.i_techGroupId equals y.i_groupId
                                    join b in db.as_maintenanceProfile on x.i_maintenanceId equals b.i_maintenanceId
                                    join c in db.as_maintenanceValidation on b.i_maintenanceValidationId equals c.i_maintenanceValidationId
                                    where x.bt_completed == false
                                    select new {
                                        description = "Custom Defined",
                                        dateTime = x.dt_scheduledDate,
                                        groupName = y.vc_groupName,
                                        validation = c.i_maintenanceValidationId
                                    }).ToList();

                foreach (var item in customShifts)
                {
                    shiftInfo newItem = new shiftInfo();
                    newItem.description = item.description;
                    newItem.dateTime = item.dateTime.ToString("dd-MM-yyyy h:mm tt");
                    newItem.groupName = item.groupName;
                    newItem.validationId = item.validation;

                    //Push to list
                    shiftList.Add(newItem);
                }

                return Json(shiftList);
            }
            catch (Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to retrieve all shifts: " + err.Message, "getAllShifts", LogHelper.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult getUserEvents()
        {
            try
            {
                var allEvents = db.as_eventTypes.Where(q => q.vc_userId == "All" || q.vc_userId == User.Identity.Name).ToList();
                return Json(allEvents);
            }
            catch (Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to retrieve all shifts: " + err.Message, "getAllShifts", LogHelper.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult insertUserDefinedEvent(string description, string title, string color, string icon, Boolean userSpesific)
        {
            try
            {
                as_eventTypes newEvent = new as_eventTypes();
                newEvent.vc_color = color;
                newEvent.vc_description = description;
                newEvent.vc_icon = icon;
                newEvent.vc_title = title;
                if (userSpesific)
                    newEvent.vc_userId = User.Identity.Name;
                else
                    newEvent.vc_userId = "All";

                //write to DB
                db.as_eventTypes.Add(newEvent);
                db.SaveChanges();
                return Json(newEvent);
            }
            catch (Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to insert user defined shift: " + err.Message, "insertUserDefinedEvent", LogHelper.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult getUserActiveEvents()
        {
            try
            {
                var events = (from x in db.as_eventPofile
                              join y in db.as_eventTypes on x.i_eventId equals y.i_eventId
                              where y.vc_userId == "All" || y.vc_userId == User.Identity.Name && x.bt_active == true
                              select new { 
                                    title = y.vc_title,
                                    description = y.vc_description,
                                    color = y.vc_color,
                                    icon = y.vc_icon,
                                    start = x.dt_dateTimeStart
                              }).ToList();

                List<EventInfo> eventList = new List<EventInfo>();
                foreach(var item  in events)
                {
                    EventInfo eventItem = new EventInfo();
                    eventItem.title = item.title;
                    eventItem.description = item.description;
                    eventItem.icon = item.icon;
                    eventItem.color = item.color;
                    eventItem.start = item.start.ToString("dd-MM-yyyy h:mm tt");

                    eventList.Add(eventItem);
                }

                return Json(eventList);
            }
            catch (Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to retrieve user events: " + err.Message, "getUserActiveEvents", LogHelper.logTypes.Error, Request.UserHostAddress);
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
        
        public ActionResult getAirSideCalendar()
        {
            MemoryStream mStream = new MemoryStream();
            StreamWriter writer = new StreamWriter(mStream);

            writer.AutoFlush = true;

            string airportName = db.as_settingsProfile.Where(q=>q.vc_settingDescription == "Airport Name").Select(q=>q.vc_settingValue).FirstOrDefault();

            var shifts = (from x in db.as_shifts
                          join y in db.as_maintenanceProfile on x.i_maintenanceId equals y.i_maintenanceId
                          join z in db.as_areaSubProfile on x.i_areaSubId equals z.i_areaSubId
                          join a in db.as_areaProfile on z.i_areaId equals a.i_areaId
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

            var customShifts = (from x in db.as_shiftsCustom
                                join y in db.as_maintenanceProfile on x.i_maintenanceId equals y.i_maintenanceId
                                where x.bt_completed == false
                                select new {
                                    beginDate = x.dt_scheduledDate,
                                    location = airportName + " - Custom Shift",
                                    description = y.vc_description
                                }).ToList();

            foreach (var shift in customShifts)
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
        public JsonResult getShifts(Boolean active)
        {
            try
            {
                DatabaseHelper dbHelper = new DatabaseHelper();

                var shifts = (from x in db.as_shifts
                              join y in db.as_technicianGroups on x.UserId equals y.i_groupId
                              join z in db.as_areaSubProfile on x.i_areaSubId equals z.i_areaSubId
                              join a in db.as_areaProfile on z.i_areaId equals a.i_areaId
                              join b in db.as_maintenanceProfile on x.i_maintenanceId equals b.i_maintenanceId
                              join c in db.as_maintenanceValidation on b.i_maintenanceValidationId equals c.i_maintenanceValidationId
                              where x.bt_completed != active
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
                                task = b.vc_description,
                                permit = x.vc_permitNumber
                              }).ToList();

                List<ShiftData> shiftList = new List<ShiftData>();
                foreach (var item in shifts)
                {
                    ShiftData shift = new ShiftData();
                    shift.area = item.area;
                    shift.completed = item.end.ToString("dd-MM-yyyy HH:mm");
                    shift.eventType = "Shift";
                    shift.start = item.start.ToString("dd-MM-yyyy HH:mm");
                    shift.subArea = item.subArea;
                    shift.team = item.team;
                    shift.shiftId = item.shiftId;
                    shift.shiftType = 1;
                    shift.validation = item.validation;
                    shift.task = item.task;
                    shift.permit = item.permit;

                    //Calculate the shift progress
                    if (active)
                    {
                        shift.shiftData = dbHelper.getCompletedAssetsForShift(item.shiftId);
                        shift.assets = dbHelper.getAssetCountPerSubArea(item.subAreaId);
                        if (shift.assets == 0) shift.progress = 0; 
                        else
                            shift.progress = Math.Round(((double)shift.shiftData / (double)shift.assets) * 100, 0);
                    } else
                    {
                        shift.progress = 0;
                    }

                    shiftList.Add(shift);
                }

                //Custom Shifts 
                var customShifts = (from x in db.as_shiftsCustom
                                      join y in db.as_technicianGroups on x.i_techGroupId equals y.i_groupId
                                      join b in db.as_maintenanceProfile on x.i_maintenanceId equals b.i_maintenanceId
                                      join c in db.as_maintenanceValidation on b.i_maintenanceValidationId equals c.i_maintenanceValidationId
                                      where x.bt_completed != active
                                      select new
                                      {
                                          eventType = "Custom Shift",
                                          start = x.dt_scheduledDate,
                                          end = x.dt_completionDate,
                                          area = "Selected Assets",
                                          subArea = "",
                                          progress = 0,
                                          team = y.vc_groupName,
                                          shiftId = x.i_shiftCustomId,
                                          subAreaId = 0,
                                          validation = c.vc_validationName,
                                          task = b.vc_description,
                                          permit = x.vc_permitNumber
                                      }).ToList();

                foreach (var item in customShifts)
                {
                    ShiftData shift = new ShiftData();
                    shift.area = item.area;
                    shift.completed = item.end.ToString("dd-MM-yyyy HH:mm");
                    shift.eventType = "Custom Shift";
                    shift.start = item.start.ToString("dd-MM-yyyy HH:mm");
                    shift.subArea = item.subArea;
                    shift.team = item.team;
                    shift.shiftId = item.shiftId;
                    shift.shiftType = 2;
                    shift.validation = item.validation;
                    shift.task = item.task;
                    shift.permit = item.permit;

                    //Calculate the shift progress
                    if (active)
                    {
                        shift.shiftData = dbHelper.getCompletedAssetsForCustomShift(item.shiftId);
                        shift.assets = dbHelper.getAssetCountPerCustomShift(item.shiftId);
                        if (shift.assets == 0) shift.progress = 0;
                        else
                            shift.progress = Math.Round(((double)shift.shiftData / (double)shift.assets) * 100, 0);
                    }
                    else
                    {
                        shift.progress = 0;
                    }

                    shiftList.Add(shift);
                }

                return Json(shiftList);
            }
            catch (Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to retrieve shifts: " + err.Message, "getShifts", LogHelper.logTypes.Error, Request.UserHostAddress);
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

                if(shiftType == 1)
                {
                    var shift = db.as_shifts.Find(shiftId);
                    shift.bt_completed = true;
                    shift.dt_completionDate = DateTime.Now;
                    db.Entry(shift).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();

                    //update iOS Cache Hash
                    CacheHelper cacheHelp = new CacheHelper();
                    cacheHelp.updateiOSCache("getTechnicianShifts");

                    return Json(new { message = "success" });
                } else if (shiftType == 2)
                {
                    var shift = db.as_shiftsCustom.Find(shiftId);
                    shift.bt_completed = true;
                    shift.dt_completionDate = DateTime.Now;
                    db.Entry(shift).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();

                    //update iOS Cache Hash
                    CacheHelper cacheHelp = new CacheHelper();
                    cacheHelp.updateiOSCache("getTechnicianShifts");

                    return Json(new { message = "success" });
                } else
                {
                    Response.StatusCode = 500;
                    return Json(new { message = "No valid shift type was specified."});
                }
            }
            catch (Exception err)
            {
                LogHelper log = new LogHelper();
                log.logError(err, User.Identity.Name + "(" + Request.UserHostAddress + ")");
                Response.StatusCode = 500;
                Response.Status = err.Message;
                return Json(new { message = err.Message });
            }
        
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult insertUserEvent(string dateTime, int recuring, int groupId)
        {
            try
            {
                CultureInfo provider = CultureInfo.InvariantCulture;

                switch (recuring)
                {
                    case 0:
                        {
                            as_eventPofile newEvent = new as_eventPofile();
                            newEvent.bt_active = true;
                            newEvent.bt_viewed = false;
                            DateTime eventTime = DateTime.ParseExact(dateTime, "dd/MM/yyyy h:mm tt", provider);
                            newEvent.dt_dateTimeStart = eventTime;
                            newEvent.i_eventId = groupId;

                            db.as_eventPofile.Add(newEvent);
                            db.SaveChanges();

                        }
                        break;
                    default:
                        break;
                }

                return Json(new {message = "Event Created" });
            }
            catch (Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to insert user event: " + err.Message, "insertUserEvent", LogHelper.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult insertNewShift(string dateTime, string workPermit, int recuring, int groupId, int subAreaId, int maintenanceId)
        {
            try
            {
                CacheHelper cache = new CacheHelper();
                CultureInfo provider = CultureInfo.InvariantCulture;

                switch (recuring)
                {
                    case 0:
                        {
                            as_shifts newShift = new as_shifts();
                            DateTime shiftTime = DateTime.ParseExact(dateTime, "dd/MM/yyyy h:mm tt", provider);
                            newShift.bt_completed = false;
                            newShift.dt_completionDate = new DateTime(1970, 1, 1);
                            newShift.dt_scheduledDate = shiftTime;
                            newShift.UserId = groupId;
                            newShift.i_areaSubId = subAreaId;
                            newShift.vc_permitNumber = workPermit;
                            newShift.i_maintenanceId = maintenanceId;

                            db.as_shifts.Add(newShift);
                            db.SaveChanges();

                            //update iOS Cache Hash
                            cache.updateiOSCache("getTechnicianShifts");

                            //Update MongoDB
                            cache.rebuildShiftAgregation();
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
                    default:
                        break;
                }

                return Json(new {message = "Shift Created" });
            }
            catch (Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to insert new shift: " + err.Message, "insertNewShift", LogHelper.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }
    
        #endregion

        #region Reporting

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public ActionResult ShiftDataDump(int shiftId, int type, string fileType)
        {
            LogHelper log = new LogHelper(); 
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

                ReportDataSource reportDataSource = new ReportDataSource("AirSide_Reporting", getReportData(shiftId, type));
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
                byte[] renderedBytes;

                //Render the report
                renderedBytes = localReport.Render(
                    reportType,
                    deviceInfo,
                    out mimeType,
                    out encoding,
                    out fileNameExtension,
                    out streams,
                    out warnings);
                Response.AddHeader("content-disposition", "attachment; filename=ShiftDataDump." + fileNameExtension);
                log.log("User " + User.Identity.Name + " requested ExcelDataDump Report -> Mime: " + mimeType + ", File Extension: " + fileNameExtension, "ExcelDataDump", LogHelper.logTypes.Info, User.Identity.Name);
                return File(renderedBytes, mimeType);
            }
            catch (Exception err)
            {
                log.log("Faile to generate report: " + err.Message + "|" + err.InnerException.Message, "ExcelDataDump", LogHelper.logTypes.Error, User.Identity.Name);
                Response.StatusCode = 500;
                return Json(new { error = err.Message });
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        private List<BigExcelDump> getReportData(int shiftId, int type)
        {
            if (type == 1)
            {
                var data = (from x in db.as_shiftData
                            join y in db.as_shifts on x.i_shiftId equals y.i_shiftId
                            join z in db.as_assetProfile on x.i_assetId equals z.i_assetId
                            join a in db.as_locationProfile on z.i_locationId equals a.i_locationId
                            join b in db.as_areaSubProfile on a.i_areaSubId equals b.i_areaSubId
                            join c in db.as_areaProfile on b.i_areaId equals c.i_areaId
                            join d in db.as_assetClassProfile on z.i_assetClassId equals d.i_assetClassId
                            where x.i_shiftId == shiftId
                            select new
                            {
                                dt_captureDate = x.dt_captureDate,
                                f_capturedValue = x.f_capturedValue,
                                i_assetCheckId = x.i_assetCheckId,
                                bt_completed = y.bt_completed,
                                dt_scheduledDate = y.dt_scheduledDate,
                                dt_completionDate = y.dt_completionDate,
                                vc_permitNumber = y.vc_permitNumber,
                                vc_rfidTag = z.vc_rfidTag,
                                vc_serialNumber = z.vc_serialNumber,
                                assetClass = d.vc_description,
                                vc_manufacturer = d.vc_manufacturer,
                                vc_model = d.vc_model,
                                f_latitude = a.f_latitude,
                                f_longitude = a.f_longitude,
                                vc_designation = a.vc_designation,
                                subArea = b.vc_description,
                                mainArea = c.vc_description
                            }).ToList();

                List<BigExcelDump> returnList = new List<BigExcelDump>();

                foreach (var item in data)
                {
                    BigExcelDump newItem = new BigExcelDump();
                    newItem.assetClass = item.assetClass;
                    newItem.bt_completed = item.bt_completed;
                    newItem.dt_captureDate = item.dt_captureDate;
                    newItem.dt_completionDate = item.dt_completionDate;
                    newItem.dt_scheduledDate = item.dt_scheduledDate;
                    newItem.f_capturedValue = item.f_capturedValue;
                    newItem.f_latitude = item.f_latitude;
                    newItem.f_longitude = item.f_longitude;
                    newItem.i_assetCheckId = item.i_assetCheckId;
                    newItem.mainArea = item.mainArea;
                    newItem.subArea = item.subArea;
                    newItem.vc_designation = item.vc_designation;
                    newItem.vc_manufacturer = item.vc_manufacturer;
                    newItem.vc_model = item.vc_model;
                    newItem.vc_permitNumber = item.vc_permitNumber;
                    newItem.vc_rfidTag = item.vc_rfidTag;
                    newItem.vc_serialNumber = item.vc_serialNumber;

                    returnList.Add(newItem);

                }

                return returnList;
            } else if(type == 2)
            {
                var data = (from x in db.as_shiftData
                            join y in db.as_shiftsCustom on x.i_shiftId equals y.i_shiftCustomId
                            join z in db.as_assetProfile on x.i_assetId equals z.i_assetId
                            join a in db.as_locationProfile on z.i_locationId equals a.i_locationId
                            join d in db.as_assetClassProfile on z.i_assetClassId equals d.i_assetClassId
                            where x.i_shiftId == shiftId
                            select new
                            {
                                dt_captureDate = x.dt_captureDate,
                                f_capturedValue = x.f_capturedValue,
                                i_assetCheckId = x.i_assetCheckId,
                                bt_completed = y.bt_completed,
                                dt_scheduledDate = y.dt_scheduledDate,
                                dt_completionDate = y.dt_completionDate,
                                vc_permitNumber = y.vc_permitNumber,
                                vc_rfidTag = z.vc_rfidTag,
                                vc_serialNumber = z.vc_serialNumber,
                                assetClass = d.vc_description,
                                vc_manufacturer = d.vc_manufacturer,
                                vc_model = d.vc_model,
                                f_latitude = a.f_latitude,
                                f_longitude = a.f_longitude,
                                vc_designation = a.vc_designation,
                                subArea = "Selected Assets",
                                mainArea = "Custom Shift"
                            }).ToList();

                List<BigExcelDump> returnList = new List<BigExcelDump>();

                foreach (var item in data)
                {
                    BigExcelDump newItem = new BigExcelDump();
                    newItem.assetClass = item.assetClass;
                    newItem.bt_completed = item.bt_completed;
                    newItem.dt_captureDate = item.dt_captureDate;
                    newItem.dt_completionDate = item.dt_completionDate;
                    newItem.dt_scheduledDate = item.dt_scheduledDate;
                    newItem.f_capturedValue = item.f_capturedValue;
                    newItem.f_latitude = item.f_latitude;
                    newItem.f_longitude = item.f_longitude;
                    newItem.i_assetCheckId = item.i_assetCheckId;
                    newItem.mainArea = item.mainArea;
                    newItem.subArea = item.subArea;
                    newItem.vc_designation = item.vc_designation;
                    newItem.vc_manufacturer = item.vc_manufacturer;
                    newItem.vc_model = item.vc_model;
                    newItem.vc_permitNumber = item.vc_permitNumber;
                    newItem.vc_rfidTag = item.vc_rfidTag;
                    newItem.vc_serialNumber = item.vc_serialNumber;

                    returnList.Add(newItem);

                }

                return returnList;
            } else
            {
                List<BigExcelDump> returnList = new List<BigExcelDump>();
                return returnList;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public ActionResult ShiftReport(int shiftId, int type)
        {
            LogHelper log = new LogHelper(); 
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

                ReportDataSource reportDataSource = new ReportDataSource("ShiftDS", getShiftReportData(shiftId, type));
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
                byte[] renderedBytes;

                //Render the report
                renderedBytes = localReport.Render(
                    reportType,
                    deviceInfo,
                    out mimeType,
                    out encoding,
                    out fileNameExtension,
                    out streams,
                    out warnings);
                Response.AddHeader("content-disposition", "attachment; filename=ShiftReport." + fileNameExtension);
                log.log("User " + User.Identity.Name + " requested PDF ShiftReport Report -> Mime: " + mimeType + ", File Extension: " + fileNameExtension, "ShiftReport", LogHelper.logTypes.Info, User.Identity.Name);
                return File(renderedBytes, mimeType);
            }
            catch (Exception err)
            {
                log.log("ShiftReport Error: " + err.Message + " Inner: " + err.InnerException.ToString(), "ShiftReport", LogHelper.logTypes.Error, User.Identity.Name);
                Response.StatusCode = 500;
                return Json(new { error = err.Message });
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        private List<ActiveShiftsReport> getShiftReportData(int shiftId, int type)
        {
            if (type == 1)
            {
                var data = from x in db.as_shifts
                           join y in db.as_shiftData on x.i_shiftId equals y.i_shiftId
                           join z in db.as_technicianGroups on x.UserId equals z.i_groupId
                           join a in db.as_areaSubProfile on x.i_areaSubId equals a.i_areaSubId
                           join b in db.as_areaProfile on a.i_areaId equals b.i_areaId
                           where x.i_shiftId == shiftId
                           select new
                           {
                               dt_scheduledDate = x.dt_scheduledDate,
                               vc_groupName = z.vc_groupName,
                               vc_externalRef = z.vc_externalRef,
                               vc_permitNumber = x.vc_permitNumber,
                               bt_completed = x.bt_completed,
                               area = b.vc_description,
                               subArea = a.vc_description
                           };

                List<ActiveShiftsReport> returnList = new List<ActiveShiftsReport>();

                foreach (var item in data)
                {
                    ActiveShiftsReport newShift = new ActiveShiftsReport();
                    newShift.area = item.area;
                    newShift.bt_completed = item.bt_completed;
                    newShift.dt_scheduledDate = item.dt_scheduledDate;
                    newShift.subArea = item.subArea;
                    newShift.vc_externalRef = item.vc_externalRef;
                    newShift.vc_groupName = item.vc_groupName;
                    newShift.vc_permitNumber = item.vc_permitNumber;

                    returnList.Add(newShift);
                }
                return returnList;
            }
            else if (type == 2)
            {
                var data = from x in db.as_shiftsCustom
                           join y in db.as_shiftData on x.i_shiftCustomId equals y.i_shiftId
                           join z in db.as_technicianGroups on x.i_techGroupId equals z.i_groupId
                           where x.i_shiftCustomId == shiftId
                           select new
                           {
                               dt_scheduledDate = x.dt_scheduledDate,
                               vc_groupName = z.vc_groupName,
                               vc_externalRef = z.vc_externalRef,
                               vc_permitNumber = x.vc_permitNumber,
                               bt_completed = x.bt_completed,
                               area = "Custom Shift",
                               subArea = "Selected Assets"
                           };

                List<ActiveShiftsReport> returnList = new List<ActiveShiftsReport>();

                foreach (var item in data)
                {
                    ActiveShiftsReport newShift = new ActiveShiftsReport();
                    newShift.area = item.area;
                    newShift.bt_completed = item.bt_completed;
                    newShift.dt_scheduledDate = item.dt_scheduledDate;
                    newShift.subArea = item.subArea;
                    newShift.vc_externalRef = item.vc_externalRef;
                    newShift.vc_groupName = item.vc_groupName;
                    newShift.vc_permitNumber = item.vc_permitNumber;

                    returnList.Add(newShift);
                }
                return returnList;
            } else
            {
                List<ActiveShiftsReport> returnList = new List<ActiveShiftsReport>();
                return returnList;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        
        #endregion

        #region Event Report

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        public ActionResult EventReport(int shiftId, int type)
        {
            LogHelper log = new LogHelper();
            try
            {
                //Create Report Settings
                ReportSettings settings = new ReportSettings();

                settings.blobContainer = "reportcontent";
                settings.blobReference = "EventReport.rdlc";

                settings.fileType = ReportFileTypes.pdf;

                //Set the data sources
                settings.dataSources = new ReportDataSource[4];
                
                //Prepare Data Sources for Report
                ReportDataSource reportDataSource = new ReportDataSource("SheduleTasks", getEventSheduleData(shiftId, type));
                settings.dataSources[0] = reportDataSource;

                reportDataSource = new ReportDataSource("AssetInfo", getEventAssetInfo(shiftId, type));
                settings.dataSources[1] = reportDataSource;

                reportDataSource = new ReportDataSource("ReportInfo", getEventReportInfo(shiftId, type));
                settings.dataSources[2] = reportDataSource;

                reportDataSource = new ReportDataSource("CheckList", getEventCheckList(shiftId, type));
                settings.dataSources[3] = reportDataSource;


                //Set the host reference for the logo
                string[] host = Request.Headers["Host"].Split('.');
                settings.logoReference = host[0];

                //Create Report Object 
                ReportingHelper report = new ReportingHelper();

                //Render the report
                ReportBytes renderedReport = report.generateReport(settings);

                Response.AddHeader(renderedReport.header.name, renderedReport.header.value);
                log.log("User " + User.Identity.Name + " requested Event Report -> Mime: " + renderedReport.mimeType, "EventReport", LogHelper.logTypes.Info, User.Identity.Name);
                return File(renderedReport.renderedBytes, "application/pdf ");
            }
            catch (Exception ex)
            {
                log.logError(ex, User.Identity.Name);
                Response.StatusCode = 500;
                return null;
            }

        }


        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        private List<EventScheduleData> getEventSheduleData(int shiftId, int type)
        {
            List<EventScheduleData> returnList = new List<EventScheduleData>();

            return returnList;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        private List<EventAssetInfo> getEventAssetInfo(int shiftId, int type)
        {
            List<EventAssetInfo> returnList = new List<EventAssetInfo>();

            return returnList;
        }
        
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        private List<EventCheckList> getEventCheckList(int shiftId, int type)
        {
            List<EventCheckList> returnList = new List<EventCheckList>();

            if(type == 1)
            {
                int maintenanceId = (from x in db.as_shifts
                                     where x.i_shiftId == shiftId
                                     select x.i_maintenanceId).FirstOrDefault();

                var checkList = from x in db.as_maintenanceCheckListDef
                                where x.i_maintenanceId == maintenanceId
                                select x.vc_description;

                foreach(var item in checkList)
                {
                    EventCheckList listItem = new EventCheckList();
                    listItem.ListItem = item;

                    returnList.Add(listItem);
                }

            } else if (type == 2)
            {
                int maintenanceId = (from x in db.as_shiftsCustom
                                     where x.i_shiftCustomId == shiftId
                                     select x.i_maintenanceId).FirstOrDefault();

                var checkList = from x in db.as_maintenanceCheckListDef
                                where x.i_maintenanceId == maintenanceId
                                select x.vc_description;

                foreach (var item in checkList)
                {
                    EventCheckList listItem = new EventCheckList();
                    listItem.ListItem = item;

                    returnList.Add(listItem);
                }
            }
            return returnList;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        private List<EventReportInfo> getEventReportInfo(int shiftId, int type)
        {
            List<EventReportInfo> returnList = new List<EventReportInfo>();
            if (type == 1)
            {
                var shiftData = from x in db.as_shiftData
                                join y in db.as_shifts on x.i_shiftId equals y.i_shiftId
                                where y.i_shiftId == shiftId
                                select new
                                {
                                    MaintenanceId = y.i_maintenanceId,
                                    SheduledDate = y.dt_scheduledDate,
                                    AssetId = x.i_assetId,
                                    AreaId = y.i_areaSubId,
                                    TechGroupId = y.UserId
                                };

                int areaId = shiftData.FirstOrDefault().AreaId;

                int totalAssets = (from x in db.as_assetProfile
                                   join y in db.as_locationProfile on x.i_locationId equals y.i_locationId
                                   where y.i_areaSubId == areaId
                                   select x).Count();

                int completedAssets = (from x in shiftData
                                       group x by x.AssetId into assetGroup
                                       select new
                                       {
                                           numberAssets = assetGroup.Count()
                                       }).Count();

                int techGroupId = shiftData.FirstOrDefault().TechGroupId;

                string technicianGroup = (from x in db.as_technicianGroups
                                          where x.i_groupId == techGroupId
                                          select x.vc_groupName).FirstOrDefault();

                decimal percentage = Math.Round((decimal)completedAssets / (decimal)totalAssets * 100,0);

                string EventDate = shiftData.FirstOrDefault().SheduledDate.ToString("yyyy/MM/dd");

                int maintenanceId = shiftData.FirstOrDefault().MaintenanceId;

                string maintenanceTask = (from x in db.as_maintenanceProfile
                                         where x.i_maintenanceId == maintenanceId
                                         select x.vc_description).FirstOrDefault();

                EventReportInfo info = new EventReportInfo();
                info.EventDate = EventDate;
                info.MaintenanceTask = maintenanceTask;
                info.NumberOfAssets = totalAssets;
                info.PercentageComplete = percentage;
                info.PercentageNotComplete = 100 - percentage;
                info.TechGroup = technicianGroup;

                returnList.Add(info);
            } else if(type == 2)
            {
                var shiftData = from x in db.as_shiftData
                                join y in db.as_shiftsCustom on x.i_shiftId equals y.i_shiftCustomId
                                where y.i_shiftCustomId == shiftId
                                select new
                                {
                                    MaintenanceId = y.i_maintenanceId,
                                    SheduledDate = y.dt_scheduledDate,
                                    AssetId = x.i_assetId,
                                    TechGroupId = y.i_techGroupId
                                };

                int totalAssets = (from x in db.as_shiftsCustomProfile
                                       where x.i_shiftCustomId == shiftId
                                       select x).Count();

                var completedAssets = (from x in shiftData
                                       group x by x.AssetId into assetGroup
                                       select new
                                       {
                                           numberAssets = assetGroup.Count()
                                       }).Count();

                int techGroupId = shiftData.FirstOrDefault().TechGroupId;

                string technicianGroup = (from x in db.as_technicianGroups
                                          where x.i_groupId == techGroupId
                                          select x.vc_groupName).FirstOrDefault();

                decimal percentage = completedAssets / totalAssets * 100;

                string EventDate = shiftData.FirstOrDefault().SheduledDate.ToString("yyyy/MM/dd");

                int maintenanceId = shiftData.FirstOrDefault().MaintenanceId;

                string maintenanceTask = (from x in db.as_maintenanceProfile
                                          where x.i_maintenanceId == maintenanceId
                                          select x.vc_description).FirstOrDefault();

                EventReportInfo info = new EventReportInfo();
                info.EventDate = EventDate;
                info.MaintenanceTask = maintenanceTask;
                info.NumberOfAssets = totalAssets;
                info.PercentageComplete = percentage;
                info.PercentageNotComplete = 100 - percentage;
                info.TechGroup = technicianGroup;

                returnList.Add(info);
            }

            return returnList;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        #endregion
    }
}