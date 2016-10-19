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
// SUMMARY: This class contains all controller calls for the Home route
#endregion

#region Using

using System.Web.Mvc;
using System.Linq;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using Microsoft.Reporting.WebForms;
using System.Web;
using Microsoft.WindowsAzure.Storage;
using System.Configuration;
using System.Globalization;
using System.IO;
using AirSide.ServerModules.Models;
using AirSide.ServerModules.Helpers;
using System.Threading.Tasks;
using ADB.AirSide.Encore.V1.Helpers;

#endregion

namespace ADB.AirSide.Encore.V1.Controllers
{
    [Authorize]
    public class HomeController : BaseController
    {
        private readonly Entities _db = new Entities();
        private readonly CacheHelper _cache = new CacheHelper(ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString, ConfigurationManager.ConnectionStrings["MongoServer"].ConnectionString);
        private readonly DatabaseHelper _database = new DatabaseHelper();

        //GET: home/startup
        public ActionResult StartUp()
        {
            return View();
        }

        public ActionResult UploadBlob()
        {
            return View();
        }

        public ActionResult UploadBlobFile(HttpPostedFileBase file)
        {
            var storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=airsidereporting;AccountKey=mCK8CqoLGGIu1c3BQ8BQEI4OtIKllkiwJQv4lMB4A6811TxLXsYzTITL8W7Z2gMztfrkbLUFuqDSe6+ZzPTGpg==");

            var blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            var container = blobClient.GetContainerReference("reportcontent");

            // Retrieve reference to a blob named "myblob".
            var blockBlob = container.GetBlockBlobReference(file.FileName);

            blockBlob.UploadFromStream(file.InputStream);

            Response.StatusCode = 200;
            return Json(new { message= "success"});
        }

        // GET: home/index
        public async Task<ActionResult> Index()
        {
            //Section for all Map Events for maintenance tasks
            var maintenanceTasks = _db.as_maintenanceProfile.ToList();
           
            //Add Worst Case Category
            var worstCase = new as_maintenanceProfile
            {
                i_maintenanceCategoryId = 0,
                i_maintenanceId = 0,
                i_maintenanceValidationId = 0,
                vc_description = "Worst Case"
            };
            maintenanceTasks.Add(worstCase);

            ViewData["maintenanceTasks"] = maintenanceTasks.OrderBy(q => q.i_maintenanceId).ToList();
            ViewBag.NumberAssets = _db.as_assetProfile.Count();
            ViewBag.AssetInit = GetAssetsInit();
            ViewBag.NumberUsers = _db.UserProfiles.Count();
            ViewBag.NumberActiveShifts = _db.as_shifts.Count(q => q.bt_completed == false);
            ViewBag.CompletedShifts = GetCompletedShifts();

            //Maintenance
            ViewBag.completedMaint = await GetCompletedAssets();
            ViewBag.midMaint = await GetMidAssets();
            ViewBag.almostMaint = await GetAlmostAssets();
            ViewBag.dueAssets = await GetDueAssets();
            ViewBag.totalTasks = await GetTotalTasks();

            return View();
        }

        #region Dashboard Helpers

        private async Task<string> GetCompletedAssets()
        {
            var assets = await _cache.GetAllAssets();
            double assetCount = 0;
            double total = 0;

            if (assets != null)
            {
                foreach (var asset in assets)
                {
                    foreach (var maint in asset.maintenance)
                    {
                        if (maint.maintenanceCycle == 1) assetCount++;
                        total++;
                    }
                }

                var persentage = ((assetCount / total) * 100);
                return Math.Round(persentage, 2).ToString(CultureInfo.InvariantCulture);
            }
            else
            {

                return "0";
            }
        }

        private async Task<string> GetMidAssets()
        {
            try
            {
                var assets = await _cache.GetAllAssets();
                double assetCount = 0;
                double total = 0;
                if (assets != null)
                {
                    foreach (var asset in assets)
                    {
                        foreach (var maint in asset.maintenance)
                        {
                            if (maint.maintenanceCycle == 2) assetCount++;
                            total++;
                        }
                    }

                    var persentage = ((assetCount / total) * 100);

                    return Math.Round(persentage, 2).ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    return "0";
                }
            }
            catch
            {
                return "0";
            }
        }

        private async Task<string> GetAlmostAssets()
        {
            try
            {
                var assets = await _cache.GetAllAssets();
                var assetCount = 0;
                var total = 0;
                if (assets != null)
                {
                    foreach (var asset in assets)
                    {
                        foreach (var maint in asset.maintenance)
                        {
                            if (maint.maintenanceCycle == 3) assetCount++;
                            total++;
                        }
                    }

                    double persentage = ((assetCount / total) * 100);

                    return Math.Round(persentage, 2).ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    return "0";
                }
            } catch
            {
                return "0";
            }
        }

        private async Task<string> GetDueAssets()
        {
            var assets = await _cache.GetAllAssets();
            double assetCount = 0;
            double total = 0;
            if (assets != null)
            {
                foreach (var asset in assets)
                {
                    foreach (var maint in asset.maintenance)
                    {
                        if (maint.maintenanceCycle == 4) assetCount++;
                        total++;
                    }
                }

                var persentage = ((assetCount / total) * 100);

                return Math.Round(persentage, 2).ToString(CultureInfo.InvariantCulture);
            }
            else {
                return "0";
            }
        }

        private async Task<string> GetNoDataAssets()
        {
            var assets = await _cache.GetAllAssets();
            double assetCount = 0;
            double total = 0;
            foreach (var asset in assets)
            {
                foreach (var maint in asset.maintenance)
                {
                    if (maint.maintenanceCycle == 0) assetCount++;
                    total++;
                }
            }

            var persentage = ((assetCount / total) * 100);

            return Math.Round(persentage, 2).ToString(CultureInfo.InvariantCulture);
        }

        private async Task<string> GetTotalTasks()
        {
            var assets = await _cache.GetAllAssets();

            var total = assets.Aggregate<mongoAssetProfile, double>(0, (current, asset) => current + asset.maintenance.Length);

            return total.ToString(CultureInfo.InvariantCulture);
        }

        private string GetCompletedShifts()
        {
            var shifts = (from x in _db.as_shifts
                          join y in _db.as_shiftData on x.i_shiftId equals y.i_shiftId
                          group x by new { y = y.dt_captureDate.Year, m = y.dt_captureDate.Month, d = y.dt_captureDate.Day } into shiftGroup
                          select shiftGroup.Count()).Take(10);
            var valueString = "";
            foreach (var shift in shifts)
            {
                valueString += shift + ",";
            }

            return valueString;

        }

        private string GetAssetsInit()
        {
            var assets = (from x in _db.as_assetProfile
                          group x by new { y = x.dt_initDate.Year, m = x.dt_initDate.Month, d = x.dt_initDate.Day } into assetGroup
                          select assetGroup.Count()).ToList();

            return assets.Aggregate("", (current, asset) => current + (asset.ToString() + ","));
        }
        
        [HttpPost]
        public JsonResult GetActivities()
        {
            var activities = _database.GetActivitiesForMonth();

            return Json(activities);
        }

        [HttpPost]
        public JsonResult GetAnomalies()
        {
            var activities = _database.GetAnomaliesForMonth();

            return Json(activities);
        }

        #endregion

        //POST: home/getUserDetails
        [HttpPost]
        public JsonResult GetUserDetails()
        {
            try
            {
                var userDetail = (from x in _db.UserProfiles
                                  where x.EmailAddress == User.Identity.Name
                                  select x).FirstOrDefault();

                var emailMd5 = MD5.Create();
                if (userDetail != null)
                {
                    var emailHash = GetMd5Hash(emailMd5, userDetail.EmailAddress);

                    return Json(new { client = userDetail.FirstName + " " + userDetail.LastName, email = emailHash });
                } else
                    return Json(new { client = "---", email = "---" });
            }
            catch(Exception err)
            {
                _cache.Log("Failed to get User Details: " + err.Message, "getUserDetails", CacheHelper.LogTypes.Error, User.Identity.Name);
                return Json(new { client = "Unknown", email = "unknown@unknown.com" });
            }
        }

        #region AJAX Calls for Dashboard

        [HttpPost]
        public JsonResult GetMetricsforActivity()
        {
            try
            {
                //Get All Metrics for Activity Dashboard
                //Create Date: 2015/03/19
                //Author: Bernard Willer
                var monthDate = DateTime.Now.AddDays(-30);

                //create conversion set
                var shifts = (from x in _db.as_shifts
                              where x.dt_scheduledDate >= monthDate
                              select new { 
                                completed = x.bt_completed,
                              });

                var metrics = new List<DashboardActivityMetrics>();
                var totalComplete = shifts.Count(q => q.completed);
                var totalOpen = shifts.Count(q => q.completed == false);

                var conversion = new DashboardActivityMetrics
                {
                    indicatorEnum = DashboardMetrics.ShiftsCompleted,
                    value = totalComplete
                };
                metrics.Add(conversion);

                conversion = new DashboardActivityMetrics
                {
                    indicatorEnum = DashboardMetrics.ShiftsOpen,
                    value = totalOpen
                };
                metrics.Add(conversion);

                //Get Reported Faulty Lights
                var faultyLights = from x in _db.as_assetStatusProfile
                                   where x.dt_lastUpdated >= monthDate
                                   select new { 
                                        completed = x.bt_assetStatus
                                   };

                totalComplete = faultyLights.Count(q => q.completed == false);
                totalOpen = faultyLights.Count(q => q.completed);

                conversion = new DashboardActivityMetrics
                {
                    indicatorEnum = DashboardMetrics.FaultyLights,
                    value = totalOpen
                };
                metrics.Add(conversion);

                conversion = new DashboardActivityMetrics
                {
                    indicatorEnum = DashboardMetrics.FaultyLightsResolved,
                    value = totalComplete
                };
                metrics.Add(conversion);
              
                return Json(metrics.ToList());
            }
            catch (Exception err)
            {
                _cache.LogError(err, User.Identity.Name + "(" + Request.UserHostAddress + ")");
                Response.StatusCode = 500;
                return Json(new { message = err.Message });
            }
        
        }

        #endregion

        #region Helpers

        static string GetMd5Hash(MD5 md5Hash, string input)
        {

            // Convert the input string to a byte array and compute the hash. 
            var data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes 
            // and create a string.
            var sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data  
            // and format each one as a hexadecimal string. 
            foreach (var t in data)
            {
                sBuilder.Append(t.ToString("x2"));
            }

            // Return the hexadecimal string. 
            return sBuilder.ToString();
        }

        #endregion

        #region Reporting

        public async Task<ActionResult> AnalyticsReport(string fileType)
        {
            try
            {
                var storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=airsidereporting;AccountKey=mCK8CqoLGGIu1c3BQ8BQEI4OtIKllkiwJQv4lMB4A6811TxLXsYzTITL8W7Z2gMztfrkbLUFuqDSe6+ZzPTGpg==");
                var blobClient = storageAccount.CreateCloudBlobClient();
                var container = blobClient.GetContainerReference("reportcontent");
                var blockBlob = container.GetBlockBlobReference("AnalyticsReport.rdlc");

                var localReport = new LocalReport {EnableExternalImages = true};

                using (var memoryStream = new MemoryStream())
                {
                    blockBlob.DownloadToStream(memoryStream);
                    memoryStream.Position = 0;
                    localReport.LoadReportDefinition(memoryStream);
                }

                var reportDataSource = new ReportDataSource("MaintenanceDS", await GetAnalyticReportCycles());
                localReport.DataSources.Add(reportDataSource);

                reportDataSource = new ReportDataSource("ShiftsDS", GetShifts().OrderBy(q=>q.start));
                localReport.DataSources.Add(reportDataSource);

                //Set the host reference for the logo
                var host = Request.Headers["Host"].Split('.');
                var paramLogo = new ReportParameter {Name = "AirportLogo"};
                paramLogo.Values.Add(@"http://airsidecdn.azurewebsites.net/images/" + host[0].ToLower() + ".png");
                localReport.SetParameters(paramLogo);

                var reportType = fileType;
                string mimeType;
                string encoding;
                string fileNameExtension;

                var deviceInfo =
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
                Response.AddHeader("content-disposition", "attachment; filename=AirSideAnalyticReport." + fileNameExtension);
                _cache.Log("User " + User.Identity.Name + " requested AirSideAnalyticReport Report -> Mime: " + mimeType + ", File Extension: " + fileNameExtension, "AirSideAnalyticReport", CacheHelper.LogTypes.Info, User.Identity.Name);
                return File(renderedBytes, mimeType);
            }
            catch (Exception err)
            {
                if (err.InnerException != null)
                    _cache.Log("Faile to generate report: " + err.Message + "|" + err.InnerException.Message, "AirSideAnalyticReport", CacheHelper.LogTypes.Error, User.Identity.Name);
                Response.StatusCode = 500;
                return Json(new { error = err.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        private MemoryStream GetBlobStream(string reportName)
        {
            var storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=airsidereporting;AccountKey=mCK8CqoLGGIu1c3BQ8BQEI4OtIKllkiwJQv4lMB4A6811TxLXsYzTITL8W7Z2gMztfrkbLUFuqDSe6+ZzPTGpg==");

            var blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            var container = blobClient.GetContainerReference("reportcontent");

            // Retrieve reference to a blob named "myblob.txt"
            var blockBlob = container.GetBlockBlobReference(reportName);

            var memoryStream = new MemoryStream();
            blockBlob.DownloadToStream(memoryStream);
            
            return memoryStream;
        }

        private async Task<List<Analytic_Cycles>> GetAnalyticReportCycles()
        {
            var cycles = new Analytic_Cycles
            {
                completed = double.Parse(await GetCompletedAssets()),
                almostDue = double.Parse(await GetAlmostAssets()),
                due = double.Parse(await GetDueAssets()),
                midCycle = double.Parse(await GetMidAssets()),
                noData = double.Parse(await GetNoDataAssets()),
                totalAssets = _db.as_assetProfile.Count(),
                totalTasks = double.Parse(await GetTotalTasks()),
                totalShifts = _db.as_shifts.Count(q => q.bt_completed == false)
            };

            var allCycles = new List<Analytic_Cycles> {cycles};

            return allCycles;
        }

        private List<ShiftData> GetShifts()
        {
            try
            {
                var shifts = (from x in _db.as_shifts
                              join y in _db.as_technicianGroups on x.i_technicianGroup equals y.i_groupId
                              join z in _db.as_areaSubProfile on x.i_areaSubId equals z.i_areaSubId
                              join a in _db.as_areaProfile on z.i_areaId equals a.i_areaId
                              join b in _db.as_maintenanceProfile on x.i_maintenanceId equals b.i_maintenanceId
                              where x.bt_completed == false && x.bt_custom == false
                              select new
                              {
                                  eventType = b.vc_description,
                                  start = x.dt_scheduledDate,
                                  end = x.dt_completionDate,
                                  area = a.vc_description,
                                  subArea = z.vc_description,
                                  progress = 0,
                                  team = y.vc_groupName,
                                  shiftId = x.i_shiftId,
                                  subAreaId = z.i_areaSubId
                              }).ToList();

                var shiftList = new List<ShiftData>();
                foreach (var item in shifts)
                {
                    var shift = new ShiftData
                    {
                        area = item.area,
                        completed = item.end.ToString("dd-MM-yyyy h:mm tt"),
                        eventType = item.eventType,
                        start = item.start.ToString("dd-MM-yyyy h:mm tt"),
                        subArea = item.subArea,
                        team = item.team,
                        shiftId = item.shiftId,
                        shiftType = 1,
                        shiftData = _database.GetCompletedAssetsForShift(item.shiftId),
                        assets = _database.GetAssetCountPerSubArea(item.subAreaId)
                    };

                    shift.progress = shift.assets == 0 ? 0 : Math.Round((shift.shiftData / (double)shift.assets) * 100, 0);

                    shiftList.Add(shift);
                }

                shifts = (from x in _db.as_shifts
                              join y in _db.as_technicianGroups on x.i_technicianGroup equals y.i_groupId
                              join b in _db.as_maintenanceProfile on x.i_maintenanceId equals b.i_maintenanceId
                              where x.bt_completed == false && x.bt_custom
                              select new
                              {
                                  eventType = b.vc_description,
                                  start = x.dt_scheduledDate,
                                  end = x.dt_completionDate,
                                  area = "Selected Assets",
                                  subArea = "",
                                  progress = 0,
                                  team = y.vc_groupName,
                                  shiftId = x.i_shiftId,
                                  subAreaId = x.i_areaSubId
                              }).ToList();

                foreach (var item in shifts)
                {
                    var shift = new ShiftData
                    {
                        area = item.area,
                        completed = item.end.ToString("dd-MM-yyyy h:mm tt"),
                        eventType = item.eventType,
                        start = item.start.ToString("dd-MM-yyyy h:mm tt"),
                        subArea = item.subArea,
                        team = item.team,
                        shiftId = item.shiftId,
                        shiftType = 2,
                        shiftData = _database.GetCompletedAssetsForShift(item.shiftId),
                        assets = _db.as_shiftsCustomProfile.Count(q => q.i_shiftId == item.shiftId)
                    };

                    shift.progress = shift.assets == 0 ? 0 : Math.Round((shift.shiftData / (double)shift.assets) * 100, 0);

                    shiftList.Add(shift);
                }
                
                return shiftList;
            }
            catch (Exception err)
            {
                var shiftList = new List<ShiftData>();
                _cache.Log("Failed to retrieve shifts: " + err.Message, "getShifts", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                return shiftList;
            }
        }

        #endregion

        public ActionResult SetCulture(string culture)
        {
            // Validate input
            culture = CultureHelper.GetImplementedCulture(culture);
            // Save culture in a cookie
            var cookie = Request.Cookies["_culture"];
            if (cookie != null)
                cookie.Value = culture;   // update cookie value
            else
            {
                cookie = new HttpCookie("_culture")
                {
                    Value = culture,
                    Expires = DateTime.Now.AddYears(1)
                };
            }
            Response.Cookies.Add(cookie);
            return RedirectToAction("Index");
        }
    }
}