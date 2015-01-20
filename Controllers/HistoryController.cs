﻿#region Copyright
// BlackLynx (Pty) Ltd.
// Copyright (c) 2011 - 2014 All Right Reserved, http://www.blacklynx.co.za/
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

using ADB.AirSide.Encore.V1.App_Helpers;
using ADB.AirSide.Encore.V1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ADB.AirSide.Encore.V1.Controllers
{
    [Authorize]
    public class HistoryController : Controller
    {
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        private Entities db = new Entities();

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        // GET: History
        public ActionResult AssetHistory()
        {
            return View();
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------------

        [HttpPost]
        public JsonResult GetAssetHistory(int assetId)
        {
            try
            {
                List<AssetHistory> allHistory = new List<AssetHistory>();
                allHistory.AddRange(validationTasks(assetId));
                allHistory.AddRange(torqueTasks(assetId));
                allHistory.AddRange(visualSurveys(assetId));

                return Json(allHistory.OrderByDescending(q=>q.dateStamp));
            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.log("Failed to retrieve asset history: " + err.Message, "GetAssetHistory", Logging.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        #region Helpers

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        private List<AssetHistory> validationTasks(int assetId)
        {
            var validation = from x in db.as_validationTaskProfile
                             join y in db.UserProfiles on x.UserId equals y.UserId
                             join a in db.as_shifts on x.i_shiftId equals a.i_shiftId
                             join z in db.as_maintenanceProfile on a.i_maintenanceId equals z.i_maintenanceId
                             where x.i_assetId == assetId
                             select new {
                                 user = y.FirstName + " " + y.LastName,
                                 date = x.dt_dateTimeStamp,
                                 maintenanceTask = z.vc_description
                             };

            List<AssetHistory> tasks = new List<AssetHistory>();

            foreach(var item in validation)
            {
                AssetHistory task = new AssetHistory();
                task.colour = "#ff7700";
                task.heading = "Validation Task Performed";
                task.icon = "fa-check";
                task.content = new String[2];
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
        
        private List<AssetHistory> torqueTasks(int assetId)
        {
            var torque = (from x in db.as_shiftData
                         join y in db.as_shifts on x.i_shiftId equals y.i_shiftId
                         join z in db.as_technicianGroups on y.UserId equals z.i_groupId
                         join a in db.as_maintenanceProfile on y.i_maintenanceId equals a.i_maintenanceId
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
                    
                    task = new AssetHistory();
                    task.colour = "#0074c8";
                    task.heading = "Fitting was torqued - " + item.name;
                    task.icon = "fa-wrench";
                    task.dateString = item.date.ToString("dd MMM, yyyy");
                    task.dateStamp = item.date;
                    task.content = new String[10];
                    task.type = 2;
                    shiftId = item.shiftId;
                    pointer = 0;
                }

                task.content[pointer] = item.value.ToString();
                pointer++;
            }

            tasks.Add(task);

            return tasks;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        private List<AssetHistory> visualSurveys(int assetId)
        {
            var surveys = (from x in db.as_fileUploadInfo
                          join y in db.as_fileUploadProfile on x.guid_file equals y.guid_file
                          join z in db.as_locationProfile on new { x.f_latitude, x.f_longitude } equals new { z.f_latitude, z.f_longitude }
                          join a in db.as_assetProfile on z.i_locationId equals a.i_locationId
                          join b in db.UserProfiles on x.i_userId equals b.UserId
                          where a.i_assetId == assetId
                          select new { 
                            user = b.FirstName + " " + b.LastName,
                            date = y.dt_datetime,
                            fileLocation = y.vc_filePath,
                            type = y.i_fileType
                          }).OrderByDescending(q=>q.date).Take(10);

            List<AssetHistory> items = new List<AssetHistory>();

            foreach(var item in surveys)
            {
                AssetHistory asset = new AssetHistory();
                asset.dateStamp = item.date;
                asset.dateString = item.date.ToString("dd MMM, yyyy");
                asset.type = 3;
                asset.content = new String[1];

                string[] filepath = item.fileLocation.Split(char.Parse("."));
                int place = filepath.Count() - 1;

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