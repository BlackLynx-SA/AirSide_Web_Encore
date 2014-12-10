#region Copyright
// BlackLynx (Pty) Ltd.
// Copyright (c) 2011 - 2014 All Right Reserved, http://www.blacklynx.co.za/
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

using ADB.AirSide.Encore.V1.App_Helpers;
using ADB.AirSide.Encore.V1.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ADB.AirSide.Encore.V1.Controllers
{
    [Authorize]
    public class ShiftsController : Controller
    {
        private Entities db = new Entities();

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        public ActionResult Calendar()
        {
            ViewBag.maintenanceTasks = new SelectList(db.as_maintenanceProfile.OrderBy(q => q.vc_description).Distinct(), "i_maintenanceId", "vc_description");
            return View();
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public ActionResult MaintenanceTasks()
        {
            return View();
        }

        #region Custom Shifts

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult insertCustomShift(as_shiftsCustom shiftData, int[] assetIds)
        {
            try
            {
                //set default values for creation
                shiftData.bt_completed = false;
                shiftData.dt_completionDate = new DateTime(1970, 1, 1);

                db.as_shiftsCustom.Add(shiftData);
                db.SaveChanges();

                //create the profile for this shift
                foreach (int item in assetIds)
                {
                    as_shiftsCustomProfile newProfile = new as_shiftsCustomProfile();
                    newProfile.i_assetId = item;
                    newProfile.i_shiftCustomId = shiftData.i_shiftCustomId;
                    db.as_shiftsCustomProfile.Add(newProfile);
                }

                //commit to db
                db.SaveChanges();

                return Json(new { message = "Successfully added custom shift", count = assetIds.Count() });
            } catch(Exception err)
            {
                Logging log = new Logging();
                log.log("Failed to insert a custom shift: " + err.InnerException.Message, "insertCustomShift", Logging.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(new { message = err.InnerException.Message });
            }
        }

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
                Logging log = new Logging();
                log.log("Failed to retrieve Technician Groups: " + err.Message, "getAllTechnicianGroups", Logging.logTypes.Error, Request.UserHostAddress);
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
                              where x.bt_completed == false
                              select new { 
                                description = z.vc_description + "(" + y.vc_description + ")",
                                dateTime = x.dt_scheduledDate,
                                groupName = a.vc_groupName,
                                validation = x.i_maintenanceValidationId
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
                return Json(shiftList);
            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.log("Failed to retrieve all shifts: " + err.Message, "getAllShifts", Logging.logTypes.Error, Request.UserHostAddress);
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
                Logging log = new Logging();
                log.log("Failed to retrieve all shifts: " + err.Message, "getAllShifts", Logging.logTypes.Error, Request.UserHostAddress);
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
                Logging log = new Logging();
                log.log("Failed to insert user defined shift: " + err.Message, "insertUserDefinedEvent", Logging.logTypes.Error, Request.UserHostAddress);
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
                Logging log = new Logging();
                log.log("Failed to retrieve user events: " + err.Message, "getUserActiveEvents", Logging.logTypes.Error, Request.UserHostAddress);
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
        
        //[HttpPost]
        //public JsonResult getValidationTasks(int areaSubId, int maintenanceId)
        //{
        //    try
        //    {
        //        //Get the relevant validation tasks for a shift
        //        //Create Date: 2014/01/01
        //        //Author: Bernard Willer

        //        var validation = (from x in db.as_maintenanceValidation
        //                              join y in asset)
        //    }
        //    catch (Exception err)
        //    {
        //        Logging log = new Logging();
        //        log.logError(err, User.Identity.Name + "(" + Request.UserHostAddress + ")");
        //        Response.StatusCode = 500;
        //        return Json(new { message = err.Message });
        //    }
        
        //}
        
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
                                subAreaId = z.i_areaSubId
                              }).ToList();

                List<ShiftData> shiftList = new List<ShiftData>();
                foreach (var item in shifts)
                {
                    ShiftData shift = new ShiftData();
                    shift.area = item.area;
                    shift.completed = item.end.ToString("dd-MM-yyyy h:mm tt");
                    shift.eventType = "Shift";
                    shift.start = item.start.ToString("dd-MM-yyyy h:mm tt");
                    shift.subArea = item.subArea;
                    shift.team = item.team;

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

                return Json(shiftList);
            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.log("Failed to retrieve shifts: " + err.Message, "getShifts", Logging.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
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
                Logging log = new Logging();
                log.log("Failed to insert user event: " + err.Message, "insertUserEvent", Logging.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult insertNewShift(string dateTime, string workPermit, int recuring, int groupId, int subAreaId)
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

                            db.as_shifts.Add(newShift);
                            db.SaveChanges();

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
                Logging log = new Logging();
                log.log("Failed to insert new shift: " + err.Message, "insertNewShift", Logging.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }
    
        #endregion
    }
}