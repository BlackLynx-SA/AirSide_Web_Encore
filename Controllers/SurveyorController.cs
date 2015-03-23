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
                                Url = y.vc_filePath
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

                Alerts.Add(alert);
            }

            ViewData["Alerts"] = Alerts;

            return View();
        }

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
    }
}