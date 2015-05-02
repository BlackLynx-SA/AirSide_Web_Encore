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
// CREATE DATE: 2015/02/06
// SUMMARY: This class contains all controller calls for the Surveyor Module
#endregion

using ADB.AirSide.Encore.V1.Models;
using AirSide.ServerModules.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ADB.AirSide.Encore.V1.Controllers
{
    [Authorize]
    public class SurveyorController : Controller
    {
        private Entities db = new Entities();

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public ActionResult AlertBox(int severityId)
        {

            var surveyor = (from x in db.as_fileUploadInfo
                            join y in db.as_fileUploadProfile on x.guid_file equals y.guid_file
                            join z in db.UserProfiles on x.i_userId_logged equals z.UserId
                            where x.bt_resolved == false && x.i_severityId == severityId
                            select new { 
                                Person = z.FirstName + " " + z.LastName,
                                Date = y.dt_datetime,
                                TypeAnomaly = y.i_fileType,
                                Url = y.vc_filePath,
                                Guid = y.guid_file
                            }).OrderByDescending(q=>q.Date);

            List<AnomalyAlert> Alerts = new List<AnomalyAlert>();
            foreach(var item in surveyor)
            {
                double difference = Math.Round((DateTime.Now - item.Date).TotalHours);

                AnomalyAlert alert = new AnomalyAlert();
                alert.AlertType = (AnomalyType)item.TypeAnomaly;
                alert.DateReported = item.Date.ToString("yyyy/MM/dd HH:mm:ss");
                alert.ItemUrl = item.Url;
                alert.ReportedUser = item.Person;
                alert.TimeCalculation = difference.ToString();
                alert.guid = item.Guid.ToString();

                Alerts.Add(alert);
            }

            ViewData["Alerts"] = Alerts;

            return View();
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult getAlertSummary()
        {
            var alerts = from x in db.as_fileUploadInfo
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
        public JsonResult closeAnomaly(string guid)
        {
            Guid receivedGuid = new Guid(guid);
            var anomaly = db.as_fileUploadInfo.Find(receivedGuid);
            var user = db.UserProfiles.Where(q=>q.UserName == User.Identity.Name).First();
            
            anomaly.bt_resolved = true;
            anomaly.i_userId_resolved = user.UserId;

            db.Entry(anomaly).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();

            return Json(new { message = "success" });
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult getSurveyorData(int anomalyType, string dateRange, Boolean all)
        {
            string[] dates = dateRange.Split(char.Parse("-"));
            DateTime startDate = DateTime.ParseExact(dates[0], "yyyy/MM/dd", CultureInfo.InvariantCulture);
            DateTime endDate = DateTime.ParseExact(dates[1], "yyyy/MM/dd", CultureInfo.InvariantCulture);

            List<surveyedData> SurveyItems = new List<surveyedData>();

            if (all)
            {
                var survey = (from x in db.as_fileUploadProfile
                             join y in db.as_fileUploadInfo on x.guid_file equals y.guid_file
                             join z in db.UserProfiles on y.i_userId_logged equals z.UserId
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

                foreach(var item in survey)
                {
                    surveyedData newSurvey = new surveyedData();
                    newSurvey.date = item.Date.ToString("yyyy/MM/dd HH:mm");
                    newSurvey.technician = item.Technician;
                    newSurvey.latitude = item.Latitude;
                    newSurvey.longitude = item.Longitude;
                    newSurvey.guid = item.Uuid.ToString();
                    newSurvey.type = item.Severity.ToString();
                    newSurvey.url = item.Url;
                    newSurvey.description = item.Desc;

                    SurveyItems.Add(newSurvey);
                }
            } else
            {
                var survey = (from x in db.as_fileUploadProfile
                             join y in db.as_fileUploadInfo on x.guid_file equals y.guid_file
                             join z in db.UserProfiles on y.i_userId_logged equals z.UserId
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
                    surveyedData newSurvey = new surveyedData();
                    newSurvey.date = item.Date.ToString("yyyy/MM/dd HH:mm");
                    newSurvey.technician = item.Technician;
                    newSurvey.latitude = item.Latitude;
                    newSurvey.longitude = item.Longitude;
                    newSurvey.guid = item.Uuid.ToString();
                    newSurvey.type = item.Severity.ToString();
                    newSurvey.url = item.Url;
                    newSurvey.description = item.Desc;

                    SurveyItems.Add(newSurvey);
                }
            }

            return Json(SurveyItems);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        [HttpPost]
        public JsonResult getSingleViewData(string guid)
        {
            List<surveyedData> SurveyItems = new List<surveyedData>();

            Guid surveyGuid = new Guid(guid);

            var survey = (from x in db.as_fileUploadProfile
                          join y in db.as_fileUploadInfo on x.guid_file equals y.guid_file
                          join z in db.UserProfiles on y.i_userId_logged equals z.UserId
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

            foreach (var item in survey)
            {
                surveyedData newSurvey = new surveyedData();
                newSurvey.date = item.Date.ToString("yyyy/MM/dd HH:mm");
                newSurvey.technician = item.Technician;
                newSurvey.latitude = item.Latitude;
                newSurvey.longitude = item.Longitude;
                newSurvey.guid = item.Uuid.ToString();
                newSurvey.type = item.FileType.ToString();
                newSurvey.url = item.Url;
                newSurvey.description = item.Desc;

                SurveyItems.Add(newSurvey);
            }

            return Json(SurveyItems);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        public ActionResult AnomalySingleView()
        {
            return View();
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
    }
}