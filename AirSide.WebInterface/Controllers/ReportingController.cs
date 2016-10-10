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
// CREATE DATE: 2015/03/04
// SUMMARY: This class contains all controller calls for the Reporting Module
#endregion

using AirSide.ServerModules.Helpers;
using AirSide.ServerModules.Models;
using Microsoft.Reporting.WebForms;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ADB.AirSide.Encore.V1.Controllers
{
    [Authorize]
    public class ReportingController : BaseController
    {
        private readonly Entities db = new Entities();
        private readonly CacheHelper cache = new CacheHelper(ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString, ConfigurationManager.ConnectionStrings["MongoServer"].ConnectionString);

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        #region Shifts Report

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        public ActionResult ShiftReport()
        {
            return View();
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        public ActionResult Setup()
        {
            return View();
        }
        
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public ActionResult Surveyor()
        {
            return View();
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        public FileContentResult getShiftsPerDateRangeReport(string dateRange, int type)
        {
            try
            {
                //This method will generate a shift report per date range
                //Create Date: 2015/03/04
                //Author: Bernard Willer

                //Disseminate the date range
                string[] dates = dateRange.Split(char.Parse("-"));
                DateTime startDate = DateTime.ParseExact(dates[0], "yyyy/MM/dd", CultureInfo.InvariantCulture);
                DateTime endDate = DateTime.ParseExact(dates[1], "yyyy/MM/dd", CultureInfo.InvariantCulture);

                //Create Report Settings
                ReportSettings settings = new ReportSettings();

                settings.blobContainer = "reportcontent";
                settings.blobReference = "ShiftReport.rdlc";

                settings.fileType = (ReportFileTypes)type;
                settings.dataSources = new ReportDataSource[1];

                //Set the host reference for the logo
                string[] host = Request.Headers["Host"].Split('.');
                settings.logoReference = host[0];

                settings.reportName = "ShiftReport";

                //Prepare Data Sources for Report
                ReportDataSource reportDataSource = new ReportDataSource("ShiftDS", getShiftReportDataForRange(startDate, endDate));

                settings.dataSources[0] = reportDataSource;

                //Create Report Object 
                ReportingHelper report = new ReportingHelper();

                //Render the report
                ReportBytes renderedReport = report.GenerateReport(settings);

                Response.AddHeader(renderedReport.header.name, renderedReport.header.value);
                cache.Log("User " + User.Identity.Name + " requested Shift Date Range Report -> Mime: " + renderedReport.mimeType, "ShiftReport", CacheHelper.LogTypes.Info, User.Identity.Name);
                return File(renderedReport.renderedBytes, "application/pdf ");
            }
            catch (Exception err)
            {
                cache.LogError(err.InnerException, User.Identity.Name + "(" + Request.UserHostAddress + ")");
                Response.StatusCode = 500;
                return null;
            }

        }

        //Data Helpers
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        private List<ActiveShiftsReport> getShiftReportDataForRange(DateTime startDate, DateTime endDate)
        {
            //Planned Shifts
            var data = from x in db.as_shifts
                        join z in db.as_technicianGroups on x.i_technicianGroup equals z.i_groupId
                        join a in db.as_areaSubProfile on x.i_areaSubId equals a.i_areaSubId
                        join b in db.as_areaProfile on a.i_areaId equals b.i_areaId
                        where x.dt_scheduledDate >= startDate && x.dt_scheduledDate <= endDate && x.bt_custom == false
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

            //Custom Shifts
            data = from x in db.as_shifts
                       join z in db.as_technicianGroups on x.i_technicianGroup equals z.i_groupId
                       where x.dt_scheduledDate >= startDate && x.dt_scheduledDate <= endDate && x.bt_custom == true
                       select new
                       {
                           dt_scheduledDate = x.dt_scheduledDate,
                           vc_groupName = z.vc_groupName,
                           vc_externalRef = z.vc_externalRef,
                           vc_permitNumber = x.vc_permitNumber,
                           bt_completed = x.bt_completed,
                           area = "Custom Selected",
                           subArea = "---"
                       };

            return returnList;

        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        private byte[] ConvertTiffToJpeg(byte[] file)
        {
            Stream stream = new MemoryStream(file);
            using (Image imageFile = Image.FromStream(stream))
            {
                FrameDimension frameDimensions = new FrameDimension(
                    imageFile.FrameDimensionsList[0]);

                // Gets the number of pages from the tiff image (if multipage) 
                int frameNum = imageFile.GetFrameCount(frameDimensions);
                string[] jpegPaths = new string[frameNum];

                // Selects one frame at a time and save as jpeg. 
                imageFile.SelectActiveFrame(frameDimensions, 0);
                using (Bitmap bmp = new Bitmap(imageFile))
                {
                    ImageConverter converter = new ImageConverter();
                    return (byte[])converter.ConvertTo(bmp, typeof(byte[]));
                }
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        #endregion 
    }
}