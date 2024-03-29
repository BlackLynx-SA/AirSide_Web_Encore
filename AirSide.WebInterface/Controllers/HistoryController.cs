﻿#region Copyright
// BlackLynx (Pty) Ltd.
// Copyright (c) 2011 - 2015 All Right Reserved, http://www.blacklynx.co.za/
//
// THE CODE IN THIS SOURCE FILE HAS BEEN DEVELOPED BY BLACKLYNX (PTY) LTD. ("BL")
// THE USE OF ANY EXTRACT, MODULES OR UNITS ARE STICKLY FORBIDDEN.
// PLEASE OBTAIN APPROPRIATE APPROVAL FROM BL AT INFO@BLACKLYNX.CO.ZA
//
// AUTHOR: Bernard Willer
// EMAIL: bernard.willer@blacklynx.co.za
// CREATE DATE: 2015/01/13
// SUMMARY: This class contains all controller calls for any history related queries
#endregion

using AirSide.ServerModules.Helpers;
using AirSide.ServerModules.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using Microsoft.Ajax.Utilities;

namespace ADB.AirSide.Encore.V1.Controllers
{
    [Authorize]
    public class HistoryController : BaseController
    {
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        private readonly Entities _db = new Entities();
        private readonly CacheHelper _log = new CacheHelper(ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString, ConfigurationManager.ConnectionStrings["MongoServer"].ConnectionString);


        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        // GET: History
        public ActionResult AssetHistory(int? id)
        {
            ViewBag.assetId = id ?? 0;
            return View();
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------------

        [HttpPost]
        public JsonResult GetAssetHistory(int assetId)
        {
            try
            {
                List<AssetHistory> allHistory = new List<AssetHistory>();
                allHistory.AddRange(ValidationTasks(assetId));
                allHistory.AddRange(TorqueTasks(assetId));
                allHistory.AddRange(VisualSurveys(assetId));
                allHistory.AddRange(ValidationCheckListTasks(assetId));
                allHistory.AddRange(FaultyLightsTasks(assetId));
                allHistory.AddRange(AdhocTask(assetId));
                allHistory.AddRange(AdhocValidationTasks(assetId));

                if (allHistory.Count != 1) return Json(allHistory.OrderByDescending(q => q.dateStamp));
                if (allHistory[0].heading == null)
                    allHistory.Clear();

                return Json(allHistory.OrderByDescending(q=>q.dateStamp));
            }
            catch (Exception err)
            {
                _log.Log("Failed to retrieve asset history: " + err.Message, "GetAssetHistory", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        #region Helpers

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        private List<AssetHistory> ValidationCheckListTasks(int assetId)
        {
            var validation = (from x in _db.as_maintenanceCheckListEntity
                              join y in _db.UserProfiles on x.UserId equals y.UserId
                              join b in _db.as_maintenanceCheckListDef on x.i_maintenanceCheckId equals b.i_maintenanceCheckId
                              where x.i_assetId == assetId
                              select new
                              {
                                  user = y.FirstName + " " + y.LastName,
                                  date = x.dt_captureDate,
                                  checkItem = b.vc_description,
                                  checkValue = x.vc_capturedValue

                              }).ToList();

            List<AssetHistory> tasks = new List<AssetHistory>();

            foreach (var item in validation)
            {
                bool flag = true;
                foreach (var taskItem in tasks.Where(taskItem => taskItem.dateString == item.date.ToString("dd MMM, yyyy") && taskItem.content[0] == item.user))
                {
                    flag = false;
                    taskItem.content[1] += "- " + item.checkItem + " : <i>" + item.checkValue + "</i><br/>";
                }

                if (flag)
                {
                    var task = new AssetHistory
                    {
                        colour = "#c928f7",
                        heading = "Validation Check List Performed",
                        icon = "fa-check-square-o",
                        content = new String[2]
                    };
                    task.content[0] = item.user;
                    task.content[1] = "- "  + item.checkItem + " : <i>" + item.checkValue + "</i><br/>";
                    task.dateString = item.date.ToString("dd MMM, yyyy");
                    task.dateStamp = item.date;
                    task.type = 5;
                    tasks.Add(task);
                }
            }

            return tasks;
        }
                
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        private List<AssetHistory> ValidationTasks(int assetId)
        {
            var validation = (from x in _db.as_validationTaskProfile
                             join y in _db.UserProfiles on x.UserId equals y.UserId
                             join a in _db.as_shifts on x.i_shiftId equals a.i_shiftId
                             join z in _db.as_maintenanceProfile on a.i_maintenanceId equals z.i_maintenanceId
                             where x.i_assetId == assetId
                             select new {
                                 user = y.FirstName + " " + y.LastName,
                                 date = x.dt_dateTimeStamp,
                                 maintenanceTask = z.vc_description
                             }).ToList();

            List<AssetHistory> tasks = new List<AssetHistory>();

            foreach(var item in validation.DistinctBy(x => x.maintenanceTask))
            {
                var task = new AssetHistory
                {
                    colour = "#ff7700",
                    heading = "Validation Task Performed",
                    icon = "fa-check",
                    content = new string[2]
                };
                task.content[0] = item.user;
                task.content[1] = item.maintenanceTask;
                task.dateString = item.date.ToString("dd MMM, yyyy");
                task.dateStamp = item.date;
                task.type = 1;
                tasks.Add(task);
            }

            return tasks;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        private List<AssetHistory> FaultyLightsTasks(int assetId)
        {
            var validation = (from x in _db.as_assetStatusProfile
                              where x.i_assetProfileId == assetId
                              select x
                              ).Distinct();

            List<AssetHistory> tasks = new List<AssetHistory>();

            foreach (var item in validation)
            {
                var task = new AssetHistory
                {
                    icon = "fa-exclamation-triangle",
                    content = new String[2]
                };

                if (item.bt_assetStatus)
                {
                    task.heading = "Faulty Light Reported";
                    task.content[0] = "This light is still faulty.";
                    task.colour = "#fc2e2e";
                }
                else
                {
                    task.heading = "Faulty Light Reported (Resolved)";
                    task.content[0] = "This faulty light was resolved on this date";
                    task.colour = "#28f736";
                }

                task.content[1] = "";
                task.dateString = item.dt_lastUpdated.ToString("dd MMM, yyyy");
                task.dateStamp = item.dt_lastUpdated;
                task.type = 4;
                tasks.Add(task);
            }

            return tasks;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        private List<AssetHistory> AdhocTask(int assetId)
        {
            var torque = (from x in _db.as_shiftData
                          where x.i_assetId == assetId && x.i_shiftId == 0
                          select new
                          {
                              name = "ADHOC",
                              date = x.dt_captureDate,
                              maintenanceTask = "ADHOC Torque",
                              value = x.f_capturedValue,
                              shiftId = x.i_shiftId
                          }).OrderByDescending(q => q.date).Take(10);

            List<AssetHistory> tasks = new List<AssetHistory>();

            int shiftId = 0;
            int pointer = 0;

            AssetHistory task = new AssetHistory();

            foreach (var item in torque)
            {
                if (item.shiftId != shiftId || shiftId == 0)
                {
                    if (shiftId != 0)
                        tasks.Add(task);

                    task = new AssetHistory
                    {
                        colour = "#8800c8",
                        heading = "Fitting was torqued(ADHOC)",
                        icon = "fa-wrench",
                        dateString = item.date.ToString("dd MMM, yyyy"),
                        dateStamp = item.date,
                        content = new String[10],
                        type = 2
                    };
                    shiftId = item.shiftId;
                    pointer = 0;
                }

                task.content[pointer] = item.value.ToString(CultureInfo.InvariantCulture);
                pointer++;
            }

            tasks.Add(task);

            return tasks;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        private List<AssetHistory> AdhocValidationTasks(int assetId)
        {
            var validation = (from x in _db.as_validationTaskProfile
                              join y in _db.UserProfiles on x.UserId equals y.UserId
                              join z in _db.as_maintenanceProfile on x.i_maintenanceId equals z.i_maintenanceId
                              where x.i_assetId == assetId
                              select new
                              {
                                  user = y.FirstName + " " + y.LastName,
                                  date = x.dt_dateTimeStamp,
                                  maintenanceTask = z.vc_description
                              }).ToList();

            List<AssetHistory> tasks = new List<AssetHistory>();

            foreach (var item in validation.DistinctBy(x => x.maintenanceTask))
            {
                var task = new AssetHistory
                {
                    colour = "#ff7700",
                    heading = "Validation Task Performed (ADHOC)",
                    icon = "fa-check",
                    content = new string[2]
                };
                task.content[0] = item.user;
                task.content[1] = item.maintenanceTask;
                task.dateString = item.date.ToString("dd MMM, yyyy");
                task.dateStamp = item.date;
                task.type = 1;
                tasks.Add(task);
            }

            return tasks;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        private List<AssetHistory> TorqueTasks(int assetId)
        {
            var torque = (from x in _db.as_shiftData
                         join y in _db.as_shifts on x.i_shiftId equals y.i_shiftId
                         join z in _db.as_technicianGroups on y.i_technicianGroup equals z.i_groupId
                         join a in _db.as_maintenanceProfile on y.i_maintenanceId equals a.i_maintenanceId
                         where x.i_assetId == assetId
                          select new { 
                            name = z.vc_groupName,
                            date = x.dt_captureDate,
                            maintenanceTask = a.vc_description,
                            value = x.f_capturedValue,
                            shiftId = x.i_shiftId
                         }).OrderByDescending(q=>q.date).Take(10);

            List<AssetHistory> tasks = new List<AssetHistory>();

            int shiftId = 0;
            int pointer = 0;

            AssetHistory task = new AssetHistory();
                  
            foreach (var item in torque)
            {
                if (item.shiftId != shiftId || shiftId == 0)
                {
                    if(shiftId != 0)
                        tasks.Add(task);

                    task = new AssetHistory
                    {
                        colour = "#0074c8",
                        heading = "Fitting was torqued - " + item.name,
                        icon = "fa-wrench",
                        dateString = item.date.ToString("dd MMM, yyyy"),
                        dateStamp = item.date,
                        content = new String[10],
                        type = 2
                    };
                    shiftId = item.shiftId;
                    pointer = 0;
                }

                task.content[pointer] = item.value.ToString(CultureInfo.InvariantCulture);
                pointer++;
            }

            tasks.Add(task);

            return tasks;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        private List<AssetHistory> VisualSurveys(int assetId)
        {
            var surveys = (from x in _db.as_fileUploadInfo
                          join y in _db.as_fileUploadProfile on x.guid_file equals y.guid_file
                          join z in _db.as_locationProfile on new { latitude = x.f_latitude, longitude = x.f_longitude } equals new { latitude = Math.Round(z.f_latitude,6), longitude = Math.Round(z.f_longitude,6) }
                          join a in _db.as_assetProfile on z.i_locationId equals a.i_locationId
                          join b in _db.UserProfiles on x.i_userId_logged equals b.UserId
                          where a.i_assetId == assetId
                          select new { 
                            user = b.FirstName + " " + b.LastName,
                            date = y.dt_datetime,
                            fileLocation = y.vc_filePath,
                            type = y.i_fileType
                          }).OrderByDescending(q=>q.date);

            List<AssetHistory> items = new List<AssetHistory>();

            foreach(var item in surveys)
            {
                AssetHistory asset = new AssetHistory
                {
                    dateStamp = item.date,
                    dateString = item.date.ToString("dd MMM, yyyy"),
                    type = 3,
                    content = new String[1]
                };

                string[] filepath = item.fileLocation.Split(char.Parse("."));
                int place = filepath.Length - 1;

                if(filepath[place] == "jpg")
                {
                    asset.colour = "#24f62e";
                    asset.icon = "fa-file-image-o";
                    asset.content[0] = item.fileLocation;
                    asset.heading = "Surveyor Image Taken";
                }
                else if (filepath[place] == "m4a")
                {
                    asset.colour = "#00aae9";
                    asset.icon = "fa-microphone";
                    asset.content[0] = item.fileLocation;
                    asset.heading = "Surveyor Voice Memo";
                }
                else if (filepath[place] == "text")
                {
                    asset.colour = "#c91e16";
                    asset.icon = "fa-file-text-o";
                    asset.content[0] = item.fileLocation;
                    asset.heading = "Surveyor Text";
                }

                items.Add(asset);
            }

            return items;
        }
        
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        #endregion
    }
}