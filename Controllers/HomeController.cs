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

using ADB.AirSide.Encore.V1.Models;
using System.Web.Mvc;
using System.Linq;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using Microsoft.Reporting.WebForms;
using System.Globalization;
using System.Web;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using System.Configuration;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using AirSide.ServerModules.Models;
using AirSide.ServerModules.Helpers;

#endregion

namespace ADB.AirSide.Encore.V1.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private Entities db = new Entities();

        [AllowAnonymous]
        public ActionResult rebuildCache()
        {
            CacheHelper cache = new CacheHelper();
            cache.rebuildAssetProfile();
            return View();
        }

        //GET: home/startup
        public ActionResult StartUp()
        {
            return View();
        }

        public ActionResult uploadBlob()
        {
            return View();
        }

        public ActionResult uploadBlobFile(HttpPostedFileBase file)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=airsidereporting;AccountKey=mCK8CqoLGGIu1c3BQ8BQEI4OtIKllkiwJQv4lMB4A6811TxLXsYzTITL8W7Z2gMztfrkbLUFuqDSe6+ZzPTGpg==");

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            CloudBlobContainer container = blobClient.GetContainerReference("reportcontent");

            // Retrieve reference to a blob named "myblob".
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(file.FileName);

            blockBlob.UploadFromStream(file.InputStream);

            Response.StatusCode = 200;
            return Json(new { message= "success"});
        }

        // GET: home/index
        public ActionResult Index()
        {

            //Section for all Map Events for maintenance tasks
            var maintenanceTasks = db.as_maintenanceProfile.ToList();
           
            //Add Worst Case Category
            as_maintenanceProfile worstCase = new as_maintenanceProfile();
            worstCase.i_maintenanceCategoryId = 0;
            worstCase.i_maintenanceId = 0;
            worstCase.i_maintenanceValidationId = 0;
            worstCase.vc_description = "Worst Case";
            maintenanceTasks.Add(worstCase);

            ViewData["maintenanceTasks"] = maintenanceTasks.OrderBy(q => q.i_maintenanceId).ToList();
            ViewBag.NumberAssets = db.as_assetProfile.Count();
            ViewBag.AssetInit = getAssetsInit();
            ViewBag.NumberUsers = db.UserProfiles.Count();
            ViewBag.NumberActiveShifts = db.as_shifts.Where(q => q.bt_completed == false).Count();
            ViewBag.CompletedShifts = getCompletedShifts();

            //Maintenance
            ViewBag.completedMaint = getCompletedAssets();
            ViewBag.midMaint = getMidAssets();
            ViewBag.almostMaint = getAlmostAssets();
            ViewBag.dueAssets = getDueAssets();
            ViewBag.totalTasks = getTotalTasks();

            return View();
        }

        #region Dashboard Helpers

        private string getCompletedAssets()
        {
            CacheHelper cache = new CacheHelper();
            List<mongoAssetProfile> assets = cache.getAllAssets();
            double assetCount = 0;
            double total = 0;

            if (assets != null)
            {
                foreach (mongoAssetProfile asset in assets)
                {
                    foreach (maintenance maint in asset.maintenance)
                    {
                        if (maint.maintenanceCycle == 1) assetCount++;
                        total++;
                    }
                }

                double persentage = ((assetCount / total) * 100);
                return Math.Round(persentage, 2).ToString();
            }
            else
            {

                return "0";
            }
        }

        private string getMidAssets()
        {
            try
            {
                CacheHelper cache = new CacheHelper();
                List<mongoAssetProfile> assets = cache.getAllAssets();
                double assetCount = 0;
                double total = 0;
                if (assets != null)
                {
                    foreach (mongoAssetProfile asset in assets)
                    {
                        foreach (maintenance maint in asset.maintenance)
                        {
                            if (maint.maintenanceCycle == 2) assetCount++;
                            total++;
                        }
                    }

                    double persentage = ((assetCount / total) * 100);

                    return Math.Round(persentage, 2).ToString();
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

        private string getAlmostAssets()
        {
            try
            {
                CacheHelper cache = new CacheHelper();
                List<mongoAssetProfile> assets = cache.getAllAssets();
                int assetCount = 0;
                int total = 0;
                if (assets != null)
                {
                    foreach (mongoAssetProfile asset in assets)
                    {
                        foreach (maintenance maint in asset.maintenance)
                        {
                            if (maint.maintenanceCycle == 3) assetCount++;
                            total++;
                        }
                    }

                    double persentage = ((assetCount / total) * 100);

                    return Math.Round(persentage, 2).ToString();
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

        private string getDueAssets()
        {
            CacheHelper cache = new CacheHelper();
            List<mongoAssetProfile> assets = cache.getAllAssets();
            double assetCount = 0;
            double total = 0;
            if (assets != null)
            {
                foreach (mongoAssetProfile asset in assets)
                {
                    foreach (maintenance maint in asset.maintenance)
                    {
                        if (maint.maintenanceCycle == 4) assetCount++;
                        total++;
                    }
                }

                double persentage = ((assetCount / total) * 100);

                return Math.Round(persentage, 2).ToString();
            }
            else {
                return "0";
            }
        }

        private string getNoDataAssets()
        {
            CacheHelper cache = new CacheHelper();
            List<mongoAssetProfile> assets = cache.getAllAssets();
            double assetCount = 0;
            double total = 0;
            foreach (mongoAssetProfile asset in assets)
            {
                foreach (maintenance maint in asset.maintenance)
                {
                    if (maint.maintenanceCycle == 0) assetCount++;
                    total++;
                }
            }

            double persentage = ((assetCount / total) * 100);

            return Math.Round(persentage, 2).ToString();
        }

        private string getTotalTasks()
        {
            CacheHelper cache = new CacheHelper();
            List<mongoAssetProfile> assets = cache.getAllAssets();
            double total = 0;
            foreach (mongoAssetProfile asset in assets)
            {
                foreach (maintenance maint in asset.maintenance)
                {
                    total++;
                }
            }

            return total.ToString();
        }

        private string getCompletedShifts()
        {
            var shifts = (from x in db.as_shifts
                          join y in db.as_shiftData on x.i_shiftId equals y.i_shiftId
                          group x by new { y = y.dt_captureDate.Year, m = y.dt_captureDate.Month, d = y.dt_captureDate.Day } into shiftGroup
                          select shiftGroup.Count());
            string valueString = "";
            foreach (var shift in shifts)
            {
                valueString += shift.ToString() + ",";
            }

            return valueString;

        }

        private string getAssetsInit()
        {
            var assets = (from x in db.as_assetProfile
                          group x by new { y = x.dt_initDate.Year, m = x.dt_initDate.Month, d = x.dt_initDate.Day } into assetGroup
                          select assetGroup.Count()).ToList();
            string valueString = "";
            foreach(var asset in assets)
            {
                valueString += asset.ToString() + ",";
            }

            return valueString;
        }
        
        [HttpPost]
        public JsonResult getActivities()
        {
            DatabaseHelper database = new DatabaseHelper();

            List<ActivityChart> activities = database.getActivitiesForMonth();

            return Json(activities);
        }

        [HttpPost]
        public JsonResult getAnomalies()
        {
            DatabaseHelper database = new DatabaseHelper();

            List<ActivityChart> activities = database.getAnomaliesForMonth();

            return Json(activities);
        }

        #endregion

        // GET: home/inbox
        public ActionResult Inbox()
        {
            return View();
        }

        // GET: home/calendar
        public ActionResult Calendar()
        {
            return View();
        }

        // GET: home/google-map
        public ActionResult GoogleMap()
        {
            return View();
        }

        // GET: home/widgets
        public ActionResult Widgets()
        {
            return View();
        }

        // GET: home/chat
        public ActionResult Chat()
        {
            return View();
        }

        //POST: home/getUserDetails
        [HttpPost]
        public JsonResult getUserDetails()
        {
            try
            {
                UserProfile userDetail = (from x in db.UserProfiles
                                  where x.EmailAddress == User.Identity.Name
                                  select x).FirstOrDefault();

                MD5 emailMD5 = MD5.Create();
                string emailHash = GetMd5Hash(emailMD5, userDetail.EmailAddress);

                return Json(new { client = userDetail.FirstName + " " + userDetail.LastName, email = emailHash });
            }
            catch(Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to get User Details: " + err.Message, "getUserDetails", LogHelper.logTypes.Error, User.Identity.Name);
                return Json(new { client = "Unknown", email = "unknown@unknown.com" });
            }
        }

        #region AJAX Calls for Dashboard

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult setToDoStatus(int todoId)
        {
            try
            {
                //This procedure sets the To-Do item to inactive and sets the date
                //Create Date: 2014/12/09
                //Author: Bernard Willer

                var todo = db.as_todoProfile.Find(todoId);
                todo.bt_active = false;
                todo.dt_completedDate = DateTime.Now;

                db.Entry(todo).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();

                return Json(new { status = "Success" });
            }
            catch (Exception err)
            {
                LogHelper log = new LogHelper();
                log.logError(err, User.Identity.Name + "(" + Request.UserHostAddress + ")");
                Response.StatusCode = 500;
                return Json(new { message = err.Message });
            }
        
        }

        [HttpPost]
        public JsonResult getMetricsforActivity()
        {
            try
            {
                //Get All Metrics for Activity Dashboard
                //Create Date: 2015/03/19
                //Author: Bernard Willer
                DateTime monthDate = DateTime.Now.AddDays(-30);

                //create conversion set
                var shifts = (from x in db.as_shifts
                              where x.dt_scheduledDate >= monthDate
                              select new { 
                                completed = x.bt_completed,
                              });

                List<DashboardActivityMetrics> metrics = new List<DashboardActivityMetrics>();
                int totalComplete = shifts.Where(q => q.completed == true).Count();
                int totalOpen = shifts.Where(q => q.completed == false).Count();

                DashboardActivityMetrics conversion = new DashboardActivityMetrics();
                conversion.indicatorEnum = DashboardMetrics.ShiftsCompleted;
                conversion.value = totalComplete;
                metrics.Add(conversion);

                conversion = new DashboardActivityMetrics();
                conversion.indicatorEnum = DashboardMetrics.ShiftsOpen;
                conversion.value = totalOpen;
                metrics.Add(conversion);

                //Get Reported Faulty Lights
                var faultyLights = from x in db.as_assetStatusProfile
                                   where x.dt_lastUpdated >= monthDate
                                   select new { 
                                        completed = x.bt_assetStatus
                                   };

                totalComplete = faultyLights.Where(q => q.completed == false).Count();
                totalOpen = faultyLights.Where(q => q.completed == true).Count();

                conversion = new DashboardActivityMetrics();
                conversion.indicatorEnum = DashboardMetrics.FaultyLights;
                conversion.value = totalOpen;
                metrics.Add(conversion);

                conversion = new DashboardActivityMetrics();
                conversion.indicatorEnum = DashboardMetrics.FaultyLightsResolved;
                conversion.value = totalComplete;
                metrics.Add(conversion);
              
                return Json(metrics.ToList());
            }
            catch (Exception err)
            {
                LogHelper log = new LogHelper();
                log.logError(err, User.Identity.Name + "(" + Request.UserHostAddress + ")");
                Response.StatusCode = 500;
                return Json(new { message = err.Message });
            }
        
        }

        [HttpPost]
        public JsonResult getAllTodos()
        {
            try
            {
                var user = db.UserProfiles.Where(q => q.UserName == User.Identity.Name).First();
                var todos = db.as_todoProfile.Where(q => (q.UserId == user.UserId && q.bt_active == true) || (q.bt_private == false && q.bt_active == true)).ToList();
                var todosDone = db.as_todoProfile.Where(q => (q.UserId == user.UserId && q.bt_active == false)).OrderByDescending(q=>q.dt_completedDate).Take(5).ToList();
                List<ToDoList> todoItems = new List<ToDoList>();
                foreach(var item in todos)
                {
                    ToDoList list = new ToDoList();
                    list.date = item.dt_dateTime.ToString("yyyy/MM/dd");
                    list.vc_description = item.vc_description;
                    list.i_todoProfileId = item.i_todoProfileId;
                    list.i_todoCatId = item.i_todoCatId;
                    list.bt_active = item.bt_active;

                    todoItems.Add(list);
                }

                if(todosDone != null)
                {
                    foreach (var item in todosDone)
                    {
                        ToDoList list = new ToDoList();
                        list.date = item.dt_dateTime.ToString("yyyy/MM/dd");
                        list.vc_description = item.vc_description;
                        list.i_todoProfileId = item.i_todoProfileId;
                        list.i_todoCatId = item.i_todoCatId;
                        list.bt_active = item.bt_active;

                        todoItems.Add(list);
                    }
                }
                return Json(todoItems);
            }
            catch (Exception err)
            {
                LogHelper log = new LogHelper();
                log.logError(err, Request.UserHostAddress);
                return Json(new { error = err.Message });
            }
        }

        [HttpPost]
        public JsonResult getTodoCategories()
        {
            try
            {
                var user = db.UserProfiles.Where(q => q.UserName == User.Identity.Name).First();
                var categories = db.as_todoCategories.Where(q => q.UserId == user.UserId || q.bt_private == false).ToList();
                return Json(categories);
            }
            catch (Exception err)
            {
                LogHelper log = new LogHelper();
                log.logError(err, User.Identity.Name + "(" + Request.UserHostAddress + ")");
                return Json(new { error = err.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult insertNewTodo(string description, string category)
        {
            try
            {
                var categoryObject = db.as_todoCategories.Where(q => q.vc_description == category).First();
                var user = db.UserProfiles.Where(q => q.UserName == User.Identity.Name).First();
                
                as_todoProfile todo = new as_todoProfile();
                todo.UserId = user.UserId;
                todo.dt_dateTime = DateTime.Now;
                todo.bt_private = true;
                todo.bt_active = true;
                todo.vc_description = description;
                todo.i_todoCatId = categoryObject.i_todoCatId;
                todo.dt_completedDate = new DateTime(1970, 1, 1);

                db.as_todoProfile.Add(todo);
                db.SaveChanges();

                return Json(new { 
                    date = todo.dt_dateTime.ToString("yyyy/MM/dd"),
                    vc_description = todo.vc_description,
                    i_todoProfileId = todo.i_todoProfileId,
                    i_todoCatId = todo.i_todoCatId,
                    bt_active = todo.bt_active
                });
            }
            catch (Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to insert user todos: " + err.Message + "|" + err.InnerException.Message, "insertNewTodo", LogHelper.logTypes.Error, User.Identity.Name);
                return Json(new { error = err.InnerException.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult updateTodo(int todoId, Boolean active)
        {
            try
            {
                var todo = db.as_todoProfile.Find(todoId);
                todo.bt_active = active;
                db.Entry(todo).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();

                return Json(new { status = "success", item = todo.vc_description });
            }
            catch (Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to update user todos: " + err.Message + "|" + err.InnerException.Message, "updateTodo", LogHelper.logTypes.Error, User.Identity.Name);
                return Json(new { error = err.InnerException.Message });
            }
        }

        #endregion

        #region Helpers

        static string GetMd5Hash(MD5 md5Hash, string input)
        {

            // Convert the input string to a byte array and compute the hash. 
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes 
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data  
            // and format each one as a hexadecimal string. 
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string. 
            return sBuilder.ToString();
        }

        #endregion

        #region Reporting

        public ActionResult AnalyticsReport(string fileType)
        {
            LogHelper log = new LogHelper();
            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=airsidereporting;AccountKey=mCK8CqoLGGIu1c3BQ8BQEI4OtIKllkiwJQv4lMB4A6811TxLXsYzTITL8W7Z2gMztfrkbLUFuqDSe6+ZzPTGpg==");
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference("reportcontent");
                CloudBlockBlob blockBlob = container.GetBlockBlobReference("AnalyticsReport.rdlc");

                LocalReport localReport = new LocalReport();
                localReport.EnableExternalImages = true;

                using (var memoryStream = new MemoryStream())
                {
                    blockBlob.DownloadToStream(memoryStream);
                    memoryStream.Position = 0;
                    localReport.LoadReportDefinition(memoryStream);
                }

                ReportDataSource reportDataSource = new ReportDataSource("MaintenanceDS", getAnalyticReportCycles());
                localReport.DataSources.Add(reportDataSource);

                reportDataSource = new ReportDataSource("ShiftsDS", getShifts().OrderBy(q=>q.start));
                localReport.DataSources.Add(reportDataSource);

                //Set the host reference for the logo
                string[] host = Request.Headers["Host"].Split('.');
                ReportParameter paramLogo = new ReportParameter();
                paramLogo.Name = "AirportLogo";
                paramLogo.Values.Add(@"http://airsidecdn.azurewebsites.net/images/" + host[0].ToLower() + ".png");
                localReport.SetParameters(paramLogo);

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
                Response.AddHeader("content-disposition", "attachment; filename=AirSideAnalyticReport." + fileNameExtension);
                log.log("User " + User.Identity.Name + " requested AirSideAnalyticReport Report -> Mime: " + mimeType + ", File Extension: " + fileNameExtension, "AirSideAnalyticReport", LogHelper.logTypes.Info, User.Identity.Name);
                return File(renderedBytes, mimeType);
            }
            catch (Exception err)
            {
                log.log("Faile to generate report: " + err.Message + "|" + err.InnerException.Message, "AirSideAnalyticReport", LogHelper.logTypes.Error, User.Identity.Name);
                Response.StatusCode = 500;
                return Json(new { error = err.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        private MemoryStream getBlobStream(string reportName)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=airsidereporting;AccountKey=mCK8CqoLGGIu1c3BQ8BQEI4OtIKllkiwJQv4lMB4A6811TxLXsYzTITL8W7Z2gMztfrkbLUFuqDSe6+ZzPTGpg==");

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            CloudBlobContainer container = blobClient.GetContainerReference("reportcontent");

            // Retrieve reference to a blob named "myblob.txt"
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(reportName);

            MemoryStream memoryStream = new MemoryStream();
            blockBlob.DownloadToStream(memoryStream);
            
            return memoryStream;
        }

        private List<Analytic_Cycles> getAnalyticReportCycles()
        {
            Analytic_Cycles cycles = new Analytic_Cycles();
            cycles.completed = double.Parse(getCompletedAssets());
            cycles.almostDue = double.Parse(getAlmostAssets());
            cycles.due = double.Parse(getDueAssets());
            cycles.midCycle = double.Parse(getMidAssets());
            cycles.noData = double.Parse(getNoDataAssets());
            cycles.totalAssets = db.as_assetProfile.Count();
            cycles.totalTasks = double.Parse(getTotalTasks());
            cycles.totalShifts = db.as_shifts.Where(q => q.bt_completed == false).Count();

            List<Analytic_Cycles> allCycles = new List<Analytic_Cycles>();
            allCycles.Add(cycles);

            return allCycles;
        }

        private List<ShiftData> getShifts()
        {
            try
            {
                DatabaseHelper dbHelper = new DatabaseHelper();

                var shifts = (from x in db.as_shifts
                              join y in db.as_technicianGroups on x.i_technicianGroup equals y.i_groupId
                              join z in db.as_areaSubProfile on x.i_areaSubId equals z.i_areaSubId
                              join a in db.as_areaProfile on z.i_areaId equals a.i_areaId
                              join b in db.as_maintenanceProfile on x.i_maintenanceId equals b.i_maintenanceId
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

                List<ShiftData> shiftList = new List<ShiftData>();
                foreach (var item in shifts)
                {
                    ShiftData shift = new ShiftData();
                    shift.area = item.area;
                    shift.completed = item.end.ToString("dd-MM-yyyy h:mm tt");
                    shift.eventType = item.eventType;
                    shift.start = item.start.ToString("dd-MM-yyyy h:mm tt");
                    shift.subArea = item.subArea;
                    shift.team = item.team;
                    shift.shiftId = item.shiftId;
                    shift.shiftType = 1;
                   
                    shift.shiftData = dbHelper.getCompletedAssetsForShift(item.shiftId);
                    shift.assets = dbHelper.getAssetCountPerSubArea(item.subAreaId);
                    if (shift.assets == 0) shift.progress = 0;
                    else
                        shift.progress = Math.Round(((double)shift.shiftData / (double)shift.assets) * 100, 0);

                    shiftList.Add(shift);
                }

                shifts = (from x in db.as_shifts
                              join y in db.as_technicianGroups on x.i_technicianGroup equals y.i_groupId
                              join b in db.as_maintenanceProfile on x.i_maintenanceId equals b.i_maintenanceId
                              where x.bt_completed == false && x.bt_custom == true
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
                    ShiftData shift = new ShiftData();
                    shift.area = item.area;
                    shift.completed = item.end.ToString("dd-MM-yyyy h:mm tt");
                    shift.eventType = item.eventType;
                    shift.start = item.start.ToString("dd-MM-yyyy h:mm tt");
                    shift.subArea = item.subArea;
                    shift.team = item.team;
                    shift.shiftId = item.shiftId;
                    shift.shiftType = 2;

                    shift.shiftData = dbHelper.getCompletedAssetsForShift(item.shiftId);
                    shift.assets = db.as_shiftsCustomProfile.Where(q => q.i_shiftId == item.shiftId).Count();
                    if (shift.assets == 0) shift.progress = 0;
                    else
                        shift.progress = Math.Round(((double)shift.shiftData / (double)shift.assets) * 100, 0);

                    shiftList.Add(shift);
                }

                return shiftList;
            }
            catch (Exception err)
            {
                List<ShiftData> shiftList = new List<ShiftData>();
                LogHelper log = new LogHelper();
                log.log("Failed to retrieve shifts: " + err.Message, "getShifts", LogHelper.logTypes.Error, Request.UserHostAddress);
                return shiftList;
            }
        }

        #endregion
    }
}