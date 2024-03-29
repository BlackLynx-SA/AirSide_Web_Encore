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
// CREATE DATE: 2015/02/06
// SUMMARY: This class contains all controller calls for the Surveyor Module
#endregion

using AirSide.ServerModules.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;

namespace ADB.AirSide.Encore.V1.Controllers
{
    [Authorize]
    public class SurveyorController : BaseController
    {
        private readonly Entities _db = new Entities();

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public ActionResult AlertBox(int severityId)
        {
            var surveyor = (from x in _db.as_fileUploadInfo
                join y in _db.as_fileUploadProfile on x.guid_file equals y.guid_file
                join z in _db.UserProfiles on x.i_userId_logged equals z.UserId
                where x.bt_resolved == false && x.i_severityId == severityId
                select new
                {
                    Person = z.FirstName + " " + z.LastName,
                    Date = y.dt_datetime,
                    TypeAnomaly = y.i_fileType,
                    Url = y.vc_filePath,
                    Guid = y.guid_file
                }).OrderByDescending(q => q.Date);

            var alerts = (from item in surveyor
                let difference = DbFunctions.DiffHours(DateTime.Now, item.Date)*-1 //Math.Round((DateTime.Now - item.Date).TotalHours)
                            select new 
                {
                    AlertType = (AnomalyType) item.TypeAnomaly,
                    DateReported = item.Date,
                    ItemUrl = item.Url,
                    ReportedUser = item.Person,
                    TimeCalculation = difference.Value,
                    guid = item.Guid
                }).ToList();

            var newAlerts = alerts.Select(item => new AnomalyAlert
            {
                AlertType = item.AlertType, DateReported = item.DateReported.ToString("yyyy/MM/dd HH:mm:ss"), guid = item.guid.ToString(), ItemUrl = item.ItemUrl, ReportedUser = item.ReportedUser, TimeCalculation = item.TimeCalculation.ToString(CultureInfo.InvariantCulture)
            }).ToList();

            ViewData["Alerts"] = newAlerts;
         
            return View();
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult GetAlertSummary()
        {
            var alerts = from x in _db.as_fileUploadInfo
                         where x.bt_resolved == false
                         group x by new { severity = x.i_severityId } into alert
                         select new {
                             AlertType = alert.Key,
                             AlertCount = alert.Count()
                         };


            return Json(alerts.ToList());
                         
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult CloseAnomaly(string guid)
        {
            Guid receivedGuid = new Guid(guid);
            var anomaly = _db.as_fileUploadInfo.Find(receivedGuid);
            var user = _db.UserProfiles.First(q => q.UserName == User.Identity.Name);
            
            anomaly.bt_resolved = true;
            anomaly.i_userId_resolved = user.UserId;

            _db.Entry(anomaly).State = System.Data.Entity.EntityState.Modified;
            _db.SaveChanges();

            return Json(new { message = "success" });
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult GetSurveyorData(int anomalyType, string dateRange, Boolean all)
        {
            var dates = dateRange.Split(char.Parse("-"));
            var startDate = DateTime.ParseExact(dates[0], "yyyy/MM/dd", CultureInfo.InvariantCulture);
            var endDate = DateTime.ParseExact(dates[1], "yyyy/MM/dd", CultureInfo.InvariantCulture);

            var surveyItems = new List<surveyedData>();

            if (all)
            {
                var survey = (from x in _db.as_fileUploadProfile
                             join y in _db.as_fileUploadInfo on x.guid_file equals y.guid_file
                             join z in _db.UserProfiles on y.i_userId_logged equals z.UserId
                             where x.dt_datetime >= startDate && x.dt_datetime <= endDate && x.i_fileType == anomalyType
                             select new
                             {
                                 Uuid = x.guid_file,
                                 Technician = z.FirstName + " " + z.LastName,
                                 Date = x.dt_datetime,
                                 Url = x.vc_filePath,
                                 FileType = x.i_fileType,
                                 Longitude = y.f_longitude,
                                 Latitude = y.f_latitude,
                                 Resolved = y.bt_resolved,
                                 Severity = y.i_severityId,
                                 Desc = y.vc_description
                             }).OrderByDescending(q=>q.Date);

                foreach (var item in survey)
                {
                    var dataPoint = new surveyedData
                    {
                        date = item.Date.ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture),
                        technician = item.Technician,
                        latitude = item.Latitude,
                        longitude = item.Longitude,
                        guid = item.Uuid.ToString(),
                        type = item.FileType.ToString(CultureInfo.InvariantCulture),
                        url = item.Url,
                        description = item.Desc
                    };

                    surveyItems.Add(dataPoint);
                }
            }
            else
            {
                var survey = (from x in _db.as_fileUploadProfile
                             join y in _db.as_fileUploadInfo on x.guid_file equals y.guid_file
                             join z in _db.UserProfiles on y.i_userId_logged equals z.UserId
                             where x.dt_datetime >= startDate && x.dt_datetime <= endDate && y.bt_resolved == false && x.i_fileType == anomalyType
                             select new
                             {
                                 Uuid = x.guid_file,
                                 Technician = z.FirstName + " " + z.LastName,
                                 Date = x.dt_datetime,
                                 Url = x.vc_filePath,
                                 FileType = x.i_fileType,
                                 Longitude = y.f_longitude,
                                 Latitude = y.f_latitude,
                                 Severity = y.i_severityId,
                                 Resolved = y.bt_resolved,
                                 Desc = y.vc_description
                             }).OrderByDescending(q=>q.Date);

                foreach (var item in survey)
                {
                    var dataPoint = new surveyedData
                    {
                        date = item.Date.ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture),
                        technician = item.Technician,
                        latitude = item.Latitude,
                        longitude = item.Longitude,
                        guid = item.Uuid.ToString(),
                        type = item.FileType.ToString(CultureInfo.InvariantCulture),
                        url = item.Url,
                        description = item.Desc
                    };

                    surveyItems.Add(dataPoint);
                }
            }

            return Json(surveyItems);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        [HttpPost]
        public JsonResult GetSingleViewData(string guid)
        {
            var surveyGuid = new Guid(guid);

            var survey = (from x in _db.as_fileUploadProfile
                          join y in _db.as_fileUploadInfo on x.guid_file equals y.guid_file
                          join z in _db.UserProfiles on y.i_userId_logged equals z.UserId
                          where x.guid_file == surveyGuid
                          select new
                          {
                              Uuid = x.guid_file,
                              Technician = z.FirstName + " " + z.LastName,
                              Date = x.dt_datetime,
                              Url = x.vc_filePath,
                              FileType = x.i_fileType,
                              Longitude = y.f_longitude,
                              Latitude = y.f_latitude,
                              Severity = y.i_severityId,
                              Resolved = y.bt_resolved,
                              Desc = y.vc_description
                          }).OrderByDescending(q => q.Date);

            var surveyList = new List<surveyedData>();

            foreach (var item in survey)
            {
                var dataPoint = new surveyedData
                {
                    date = item.Date.ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture),
                    technician = item.Technician,
                    latitude = item.Latitude,
                    longitude = item.Longitude,
                    guid = item.Uuid.ToString(),
                    type = item.FileType.ToString(CultureInfo.InvariantCulture),
                    url = item.Url,
                    description = item.Desc
                };

                surveyList.Add(dataPoint);
            }
            
            return Json(surveyList);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        public ActionResult AnomalySingleView()
        {
            return View();
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
    }
}