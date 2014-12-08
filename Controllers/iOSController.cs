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
// SUMMARY: This class contains all controller calls for iOS calls to use with the iPad application
#endregion

using ADB.AirSide.Encore.V1.App_Helpers;
using ADB.AirSide.Encore.V1.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace ADB.AirSide.Encore.V1.Controllers
{
    [Authorize]
    public class iOSController : Controller
    {
        private Entities db = new Entities();

        #region Authentication

        public iOSController()
            : this(new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext())))
        {
        }

        public iOSController(UserManager<ApplicationUser> userManager)
        {
            UserManager = userManager;
        }

        public UserManager<ApplicationUser> UserManager { get; private set; }

        private ApplicationSignInManager _signInManager;

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set { _signInManager = value; }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> Login(string username, string password)
        {

            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, change to shouldLockout: true
            var result = await SignInManager.PasswordSignInAsync(username, password, false, shouldLockout: false);
            switch (result)
            {
                case SignInStatus.Success:
                    {
                        var loggedInUser = (from data in db.UserProfiles
                                            where data.UserName == username
                                            select data).First();

                        iOSLogin user = new iOSLogin();
                        Guid newSession = Guid.NewGuid();

                        var groupId = (from x in db.as_technicianGroupProfile
                                       where x.UserId == loggedInUser.UserId
                                       select x.i_currentGroup).First();

                        user.FirstName = loggedInUser.FirstName;
                        user.LastName = loggedInUser.LastName;
                        user.UserId = loggedInUser.UserId;
                        user.i_accessLevel = loggedInUser.i_accessLevelId;
                        user.i_airPortId = loggedInUser.i_airPortId;
                        user.SessionKey = newSession.ToString();
                        user.i_groupId = groupId;
                        return Json(user);
                    }
                case SignInStatus.Failure:
                default:
                    {
                        iOSLogin user = new iOSLogin();
                        user.FirstName = "None";
                        user.LastName = "None";
                        user.UserId = -1;
                        user.i_accessLevel = -1;
                        user.i_airPortId = -1;
                        user.SessionKey = "None";
                        user.i_groupId = -1;
                        return Json(user);
                    }
            }
        }

        //2014/11/11 - The following method has been depreciated to make the calls more secure
        //[HttpPost]
        //[AllowAnonymous]
        //public async Task<ActionResult> loginRemote(string username, string password)
        //{
        //    var userValidate = await UserManager.FindAsync(username, password);
        //    if (userValidate != null)
        //    {
        //        var loggedInUser = (from data in db.UserProfiles
        //                            where data.UserName == username
        //                            select data).First();

        //        iOSLogin user = new iOSLogin();
        //        Guid newSession = Guid.NewGuid();

        //        var groupId = (from x in db.as_technicianGroupProfile
        //                       where x.UserId == loggedInUser.UserId
        //                       select x.i_currentGroup).First();

        //        user.FirstName = loggedInUser.FirstName;
        //        user.LastName = loggedInUser.LastName;
        //        user.UserId = loggedInUser.UserId;
        //        user.i_accessLevel = loggedInUser.i_accessLevelId;
        //        user.i_airPortId = loggedInUser.i_airPortId;
        //        user.SessionKey = newSession.ToString();
        //        user.i_groupId = groupId;

        //        //Write session to DB
        //        as_userExternalSession session = new as_userExternalSession();
        //        session.UserId = user.UserId;
        //        session.vc_sessionKey = newSession.ToString();
        //        session.dt_dateTime = DateTime.Now;
        //        db.as_userExternalSession.Add(session);
        //        db.SaveChanges();

        //        return Json(user);
        //    }
        //    else
        //    {
        //        iOSLogin user = new iOSLogin();
        //        user.FirstName = "None";
        //        user.LastName = "None";
        //        user.UserId = -1;
        //        user.i_accessLevel = -1;
        //        user.i_airPortId = -1;
        //        user.SessionKey = "None";
        //        user.i_groupId = -1;
        //        return Json(user);
        //    }
        //}

        #endregion

        #region Shifts

        [HttpPost]
        public JsonResult getMaintenanceProfile()
        {
            List<as_maintenanceProfile> maintenance = db.as_maintenanceProfile.ToList();
            return Json(maintenance);
        }

        [HttpPost]
        public JsonResult getGroupsTechnicians(int groupId)
        {
            var technicians = from x in db.UserProfiles
                              join y in db.as_technicianGroupProfile on x.UserId equals y.UserId
                              join z in db.as_technicianGroups on y.i_currentGroup equals z.i_groupId
                              where z.i_groupId == groupId && x.i_accessLevelId == 3
                              select new
                              {
                                  x.UserId,
                                  x.FirstName,
                                  x.LastName,
                                  x.UserName,
                                  y.i_currentGroup,
                                  y.i_defaultGroup
                              };
            return Json(technicians.ToList());
        }

        [HttpPost]
        [AllowAnonymous]
        public JsonResult checkVersion()
        {
            var iOSVersion = (from data in db.as_settingsProfile
                              where data.vc_settingDescription == "iOSVersion"
                              select data.vc_settingValue).First();
            return Json(new { CurrentVersion = iOSVersion });
        }

        [HttpGet]
        public string testService()
        {
            var assets = (from data in db.as_assetProfile
                          select data).First();

            return assets.vc_rfidTag;
        }

        [HttpPost]
        public JsonResult getTechnicianShifts(int userId, int closeShifts)
        {
            try
            {
                Boolean closeShiftsFlag = false;
                if (closeShifts == 1) closeShiftsFlag = true;
                var shifts = (from x in db.as_shifts
                              join y in db.as_areaSubProfile on x.i_areaSubId equals y.i_areaSubId
                              join z in db.as_areaProfile on y.i_areaId equals z.i_areaId
                              join a in db.as_technicianGroups on x.UserId equals a.i_groupId
                              where x.UserId == userId && x.bt_completed == closeShiftsFlag
                              select new
                              {
                                  i_shiftId = x.i_shiftId,
                                  sheduledDate = x.dt_scheduledDate,
                                  i_areaSubId = x.i_areaSubId,
                                  sheduleTime = x.dt_scheduledDate,
                                  permitNumber = x.vc_permitNumber,
                                  techGroup = a.vc_groupName,
                                  areaName = z.vc_description
                              }
                              ).ToList();
                List<technicianShift> shiftList = new List<technicianShift>();
                foreach (var item in shifts)
                {
                    technicianShift shift = new technicianShift();
                    shift.i_shiftId = item.i_shiftId;
                    shift.sheduledDate = item.sheduledDate.ToString("yyy/MM/dd");
                    shift.i_areaSubId = item.i_areaSubId;
                    shift.sheduleTime = item.sheduleTime.ToString("hh:mm:ss");
                    shift.permitNumber = item.permitNumber;
                    shift.techGroup = item.techGroup;
                    shift.areaName = item.areaName;
                    shiftList.Add(shift);
                }
                return Json(shiftList);
            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.log("Failed to retrieve technician shifts: " + err.Message, "getTechnicianShifts(iOS)", Logging.logTypes.Error, "iOS Device");
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        [HttpPost]
        public JsonResult insertShiftData(List<as_shiftData> shiftData)
        {
            try
            {
                foreach (as_shiftData shift in shiftData)
                {
                    shift.dt_captureDate = DateTime.Now;
                    db.as_shiftData.Add(shift);
                    db.SaveChanges();

                    try
                    {
                        CacheHelper cache = new CacheHelper();
                        cache.rebuildAssetProfileForAsset(shift.i_assetId);
                    }
                    catch (Exception err)
                    {
                        Logging log = new Logging();
                        log.log("Failed to update cache for asset " + shift.i_assetId.ToString() + " - " + err.InnerException.Message, "insertShiftData", Logging.logTypes.Error, "SYSTEM");
                    }
                }
                return Json("[{success:1}]");
            }
            catch (Exception err)
            {
                return Json("[{error:" + err.Message + "}]");
            }
        }

        #endregion

        #region Assets

        [HttpPost]
        public JsonResult updateAssetStatusBulk(List<AssetStatusUpload> assetList)
        {
            Logging log = new Logging();
            try
            {
                foreach (AssetStatusUpload item in assetList)
                {
                    Boolean status = true;
                    if (item.assetStatus == "False") status = false;
                    var asset = db.as_assetStatusProfile.Find(item.assetId);

                    if (asset != null)
                    {
                        asset.bt_assetStatus = status;

                        db.Entry(asset).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();
                    }
                    else
                    {
                        as_assetStatusProfile newStatus = new as_assetStatusProfile();
                        newStatus.bt_assetStatus = status;
                        newStatus.i_assetProfileId = item.assetId;
                        newStatus.i_assetSeverity = item.assetSeverity;

                        db.as_assetStatusProfile.Add(newStatus);
                        db.SaveChanges();
                    }
                }

                return Json(new { assetCount = assetList.Count });
            }
            catch (Exception err)
            {
                Response.StatusCode = 500;
                log.log("Failed to set asset status: " + err.Message, "updateAssetStatusBulk", Logging.logTypes.Error, "iOS");
                return Json("Failed");
            }
        }

        [HttpPost]
        public JsonResult setAssetStatus(int assetId, string assetStatus, int assetSeverity)
        {
            Logging log = new Logging();
            try
            {
                Boolean status = true;
                if (assetStatus == "False") status = false;
                var asset = db.as_assetStatusProfile.Find(assetId);

                if (asset != null)
                {
                    asset.bt_assetStatus = status;

                    db.Entry(asset).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                }
                else
                {
                    as_assetStatusProfile newStatus = new as_assetStatusProfile();
                    newStatus.bt_assetStatus = status;
                    newStatus.i_assetProfileId = assetId;
                    newStatus.i_assetSeverity = assetSeverity;

                    db.as_assetStatusProfile.Add(newStatus);
                    db.SaveChanges();
                }

                AssetStatus returnType = new AssetStatus();
                returnType.i_assetProfileId = assetId;
                returnType.bt_assetStatus = status;

                return Json(returnType);

            }
            catch (Exception err)
            {

                log.log("Failed to set asset status: " + err.Message, "setAssetStatus", Logging.logTypes.Error, "iOS");
                return Json("Failed");
            }

        }

        [HttpPost]
        public JsonResult getAllAssets()
        {
            try
            {
                CacheHelper cache = new CacheHelper();
                List<mongoFullAsset> assetClassList = cache.getAllAssetDownload();
                return Json(assetClassList);
            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.log("Failed to retrieve all asset download: " + err.Message, "getAllAssets(iOS)", Logging.logTypes.Error, "iOS Device");
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        [HttpPost]
        public JsonResult getAllAssetClasses()
        {
            try
            {
                CacheHelper cache = new CacheHelper();
                List<mongoAssetClassDownload> assetClassList = cache.getAllAssetClasses();

                if (assetClassList.Count == 0)
                {
                    List<assetClassDownload> assetList = new List<assetClassDownload>();
                    DatabaseHelper func = new DatabaseHelper();

                    var assets = (from x in db.as_assetClassProfile
                                  select x);

                    foreach (var item in assets)
                    {
                        assetClassDownload asset = new assetClassDownload();
                        asset.i_assetClassId = item.i_assetClassId;
                        asset.vc_description = item.vc_description;
                        asset.i_assetCheckTypeId = 0;
                        asset.assetCheckCount = func.getNumberOfFixingPoints(item.i_assetClassId);
                        assetList.Add(asset);
                    }
                    return Json(assetList);
                }
                else
                {
                    return Json(assetClassList);
                }

            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.log("Failed to retrieve all asset classes: " + err.Message, "getAllAssetClasses(iOS)", Logging.logTypes.Error, "iOS Device");
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        [HttpPost]
        public JsonResult getAssetPerTagId(string tagId)
        {
            try
            {
                List<AssetTagReply> assetList = new List<AssetTagReply>();
                DatabaseHelper func = new DatabaseHelper();

                var assetInfo = from x in db.as_assetProfile
                                join y in db.as_locationProfile on x.i_locationId equals y.i_locationId
                                join z in db.as_areaSubProfile on y.i_areaSubId equals z.i_areaSubId
                                join a in db.as_assetClassProfile on x.i_assetClassId equals a.i_assetClassId
                                where x.vc_rfidTag == tagId
                                select new
                                {
                                    i_assetId = x.i_assetId,
                                    vc_serialNumber = x.vc_serialNumber,
                                    f_latitude = y.f_latitude,
                                    f_longitude = y.f_longitude,
                                    i_areaSubId = z.i_areaSubId,
                                    i_assetClassId = a.i_assetClassId
                                };

                foreach (var item in assetInfo)
                {
                    AssetTagReply asset = new AssetTagReply();
                    asset.assetId = item.i_assetId;
                    asset.serialNumber = item.vc_serialNumber;
                    asset.firstMaintainedDate = func.getFirstMaintanedDate(item.i_assetId).ToString("yyyMMdd");
                    asset.lastMaintainedDate = func.getLastMaintanedDate(item.i_assetId).ToString("yyyMMdd");
                    asset.nextMaintenanceDate = func.getNextMaintenanceDate(item.i_assetId).ToString("yyyMMdd");
                    asset.latitude = item.f_latitude;
                    asset.longitude = item.f_longitude;
                    asset.subAreaId = item.i_areaSubId;
                    asset.assetClassId = item.i_assetClassId;
                    assetList.Add(asset);
                }
                return Json(assetList);
            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.log("Failed to retrieve asset per tagId: " + err.Message, "getAssetPerTagId(iOS)", Logging.logTypes.Error, "iOS Device");
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        [HttpPost]
        public JsonResult getAssetAssosiations(int areaSubId)
        {
            try
            {
                CacheHelper cache = new CacheHelper();
                List<mongoAssetDownload> assetListMongo = cache.getAssetAssosiations(areaSubId);

                if (assetListMongo.Count == 0)
                {
                    List<AssetDownload> assetList = new List<AssetDownload>();
                    DatabaseHelper func = new DatabaseHelper();

                    var assets = from x in db.as_assetProfile
                                 join y in db.as_locationProfile on x.i_locationId equals y.i_locationId
                                 where y.i_areaSubId == areaSubId
                                 select new
                                 {
                                     i_assetId = x.i_assetId,
                                     i_assetClassId = x.i_assetClassId,
                                     vc_rfidTag = x.vc_rfidTag,
                                     vc_serialNumber = x.vc_serialNumber,
                                     i_locationId = x.i_locationId,
                                     i_areaSubId = y.i_areaSubId,
                                     f_longitude = y.f_longitude,
                                     f_latitude = y.f_latitude
                                 };

                    foreach (var item in assets)
                    {
                        AssetDownload asset = new AssetDownload();
                        asset.i_assetId = item.i_assetId;
                        asset.i_assetClassId = item.i_assetClassId;
                        asset.vc_tagId = item.vc_rfidTag;
                        asset.vc_serialNumber = item.vc_serialNumber;
                        asset.i_locationId = item.i_locationId;
                        asset.i_areaSubId = item.i_areaSubId;
                        asset.longitude = item.f_longitude;
                        asset.latitude = item.f_latitude;
                        asset.lastDate = func.getLastShiftDateForAsset(item.i_assetId);
                        asset.maintenance = "0";
                        asset.submitted = func.getSubmittedShiftData(item.i_assetId);
                        assetList.Add(asset);
                    }
                    return Json(assetList);
                }
                else
                {
                    return Json(assetListMongo);
                }

            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.log("Failed to retrieve asset assosiations: " + err.Message, "getAssetAssosiations(iOS)", Logging.logTypes.Error, "iOS Device");
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        [HttpPost]
        public JsonResult getAssetWithTagId(string tagId)
        {
            try
            {
                List<AssetDownload> assetList = new List<AssetDownload>();
                DatabaseHelper func = new DatabaseHelper();

                var assets = from x in db.as_assetProfile
                             join y in db.as_locationProfile on x.i_locationId equals y.i_locationId
                             where x.vc_rfidTag == tagId
                             select new
                             {
                                 i_assetId = x.i_assetId,
                                 i_assetClassId = x.i_assetClassId,
                                 vc_rfidTag = x.vc_rfidTag,
                                 vc_serialNumber = x.vc_serialNumber,
                                 i_locationId = x.i_locationId,
                                 i_areaSubId = y.i_areaSubId,
                                 f_longitude = y.f_longitude,
                                 f_latitude = y.f_latitude
                             };

                foreach (var item in assets)
                {
                    AssetDownload asset = new AssetDownload();
                    asset.i_assetId = item.i_assetId;
                    asset.i_assetClassId = item.i_assetClassId;
                    asset.vc_tagId = item.vc_rfidTag;
                    asset.vc_serialNumber = item.vc_serialNumber;
                    asset.i_locationId = item.i_locationId;
                    asset.i_areaSubId = item.i_areaSubId;
                    asset.longitude = item.f_longitude;
                    asset.latitude = item.f_latitude;
                    asset.lastDate = func.getLastShiftDateForAsset(item.i_assetId);
                    asset.maintenance = "---";
                    assetList.Add(asset);
                }
                return Json(assetList);
            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.log("Failed to retrieve asset with tagid: " + err.Message, "getAssetWithTagId(iOS)", Logging.logTypes.Error, "iOS Device");
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        [HttpPost]
        public JsonResult updateAssetTagBulk(List<AssetAssosiationUpload> assetList)
        {
            try
            {
                Logging log = new Logging();
                int i = 0;
                foreach (AssetAssosiationUpload item in assetList)
                {
                    as_assetProfile asset = db.as_assetProfile.Find(item.assetId);

                    if (asset != null)
                    {
                        string currentTagId = asset.vc_rfidTag;
                        asset.vc_rfidTag = item.tagId;
                        asset.vc_serialNumber = item.serialNumber;
                        db.Entry(asset).State = System.Data.Entity.EntityState.Modified;
                        db.SaveChanges();

                        //Log the change
                        log.log("Tag id was update from " + currentTagId + " to " + item.tagId, "updateAssetTag(iOS)", Logging.logTypes.Info, "iOS Device");
                        i++;

                        try
                        {
                            CacheHelper cache = new CacheHelper();
                            cache.rebuildAssetProfileForAsset(item.assetId);
                        }
                        catch (Exception err)
                        {
                            log.log("Failed to update cache for asset " + item.assetId.ToString() + " - " + err.InnerException.Message, "insertShiftData", Logging.logTypes.Error, "SYSTEM");
                        }
                    }
                    else
                    {
                        log.log("Reference tag for assetid " + item.assetId.ToString() + " wasn't found.", "updateAssetTag(iOS)", Logging.logTypes.Info, "iOS Device");
                    }
                }

                return Json(new { assetCount = i.ToString() });
            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.log("Failed to update asset tag: " + err.Message, "updateAssetTagBulk(iOS)", Logging.logTypes.Error, "iOS Device");
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        [HttpPost]
        public JsonResult updateAssetTag(int assetId, int UserId, string tagId, string serialNumber)
        {
            try
            {
                Logging log = new Logging();

                as_assetProfile asset = db.as_assetProfile.Find(assetId);

                if (asset != null)
                {
                    string currentTagId = asset.vc_rfidTag;
                    asset.vc_rfidTag = tagId;
                    asset.vc_serialNumber = serialNumber;
                    db.Entry(asset).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();

                    //Log the change
                    log.log("Tag id was update from " + currentTagId + " to " + tagId, "updateAssetTag(iOS)", Logging.logTypes.Info, UserId.ToString());

                    try
                    {
                        CacheHelper cache = new CacheHelper();
                        cache.rebuildAssetProfileForAsset(asset.i_assetId);
                    }
                    catch (Exception err)
                    {
                        log.log("Failed to update cache for asset " + asset.i_assetId.ToString() + " - " + err.InnerException.Message, "insertShiftData", Logging.logTypes.Error, "SYSTEM");
                    }

                    return Json("[{success:1}]");
                }
                else
                {
                    Response.StatusCode = 500;
                    return Json("Asset Tag doesn't exist");
                }

            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.log("Failed to update asset tag: " + err.Message, "updateAssetTag(iOS)", Logging.logTypes.Error, "iOS Device");
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        [HttpPost]
        public JsonResult insertAssetAssosiation(List<NewAssetAssosiation> assets)
        {
            try
            {
                foreach (NewAssetAssosiation asset in assets)
                {
                    as_locationProfile location = new as_locationProfile();
                    location.f_latitude = asset.latitude;
                    location.f_longitude = asset.longitude;
                    location.i_areaSubId = asset.i_areaSubId;
                    location.vc_designation = "---";

                    db.as_locationProfile.Add(location);
                    db.SaveChanges();

                    as_assetProfile newAsset = new as_assetProfile();
                    newAsset.i_assetClassId = asset.i_assetClassId;
                    newAsset.i_locationId = location.i_locationId;
                    newAsset.vc_rfidTag = asset.vc_rfidTag;
                    newAsset.vc_serialNumber = asset.vc_serialNumber;

                    db.as_assetProfile.Add(newAsset);
                    db.SaveChanges();

                    try
                    {
                        CacheHelper cache = new CacheHelper();
                        cache.rebuildAssetProfileForAsset(newAsset.i_assetId);
                    }
                    catch (Exception err)
                    {
                        Logging log = new Logging();
                        log.log("Failed to update cache for asset " + newAsset.i_assetId.ToString() + " - " + err.InnerException.Message, "insertShiftData", Logging.logTypes.Error, "SYSTEM");
                    }
                }

                //2014/10/28 - Changed "assets" to "success" after iPad failure
                return Json(new { success = assets.Count });
            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.log("Failed to insert asset assosiation: " + err.Message, "insertAssetAssosiation(iOS)", Logging.logTypes.Error, "iOS Device");
                Response.StatusCode = 500;
                return Json("[{error:" + err.Message + "}]");
            }
        }

        #endregion

        #region Areas

        [HttpPost]
        public JsonResult getMapCenter()
        {
            string longitude = db.as_settingsProfile.Where(q => q.vc_settingDescription == "Longitude").Select(q => q.vc_settingValue).FirstOrDefault();
            string latitude = db.as_settingsProfile.Where(q => q.vc_settingDescription == "Latitude").Select(q => q.vc_settingValue).FirstOrDefault();

            mapCenter center = new mapCenter();
            center.latitude = double.Parse(latitude);
            center.longitude = double.Parse(longitude);

            return Json(center);
        }

        [HttpPost]
        public JsonResult getMainAreas()
        {
            try
            {
                var areas = db.as_areaProfile;
                return Json(areas.ToList());
            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.log("Failed to get main areas: " + err.Message, "getMainAreas(iOS)", Logging.logTypes.Error, "iOS Device");
                Response.StatusCode = 500;
                return Json("error: " + err.Message);
            }
        }

        [HttpPost]
        public JsonResult getSubAreas()
        {
            try
            {
                var subAreas = db.as_areaSubProfile;
                return Json(subAreas.ToList());
            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.log("Failed to get sub areas: " + err.Message, "getSubAreas(iOS)", Logging.logTypes.Error, "iOS Device");
                Response.StatusCode = 500;
                return Json("error: " + err.Message);
            }
        }

        #endregion

        #region Wrench Info

        [HttpPost]
        public JsonResult getWrenches()
        {
            List<iOSwrench> iosWrenchList = new List<iOSwrench>();
            List<as_wrenchProfile> wrenchList = new List<as_wrenchProfile>();

            wrenchList = (from data in db.as_wrenchProfile select data).ToList();

            foreach (as_wrenchProfile wrench in wrenchList)
            {
                iOSwrench iosWrench = new iOSwrench();
                iosWrench.bt_active = wrench.bt_active;
                iosWrench.dt_lastCalibrated = wrench.dt_lastCalibrated.ToString("yyyyMMdd");
                iosWrench.f_batteryLevel = wrench.f_batteryLevel;
                iosWrench.i_calibrationCycle = wrench.i_calibrationCycle;
                iosWrench.i_wrenchId = wrench.i_wrenchId;
                iosWrench.vc_model = wrench.vc_model;
                iosWrench.vc_serialNumber = wrench.vc_serialNumber;

                iosWrenchList.Add(iosWrench);
            }
            return Json(iosWrenchList);
        }

        [HttpPost]
        public JsonResult updateBatteryLevel(List<WrenchBatteryUpdate> batteryUpdate)
        {
            try
            {
                foreach (WrenchBatteryUpdate battery in batteryUpdate)
                {
                    as_wrenchProfile updateWrench = db.as_wrenchProfile.Find(battery.wrenchId);
                    updateWrench.f_batteryLevel = battery.batteryLevel;
                    db.Entry(updateWrench).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();
                }
                return Json("Success");
            }
            catch
            {
                return Json("Failed");
            }
        }

        [HttpPost]
        public JsonResult insertWrenchAssosiation(List<WrenchAssosiation> assosiations)
        {
            try
            {
                foreach (WrenchAssosiation assosiation in assosiations)
                {
                    as_technicianWrenchProfile assosiationInsert = new as_technicianWrenchProfile();
                    assosiationInsert.dt_dateTime = DateTime.Now;
                    assosiationInsert.i_wrenchId = assosiation.wrenchId;
                    assosiationInsert.UserId = assosiation.UserId;
                    db.as_technicianWrenchProfile.Add(assosiationInsert);
                    db.SaveChanges();
                }

                return Json("Success");
            }
            catch
            {
                return Json("Failed");
            }
        }

        #endregion

        #region FileUpload

        public string UploadFile(HttpPostedFileBase file)
        {
            try
            {
                Guid guid = Guid.NewGuid();
                if (file.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(file.FileName);
                    var path = Path.Combine(Server.MapPath("~/images/uploads"), fileName);
                    file.SaveAs(path);

                    if (IsImage(file))
                    {
                        try
                        {
                            saveThumbNails(path);
                        }
                        catch (Exception err)
                        {
                            Console.WriteLine(err.Message);
                        }
                    }

                    as_fileUploadProfile dbFile = new as_fileUploadProfile();
                    dbFile.guid_file = guid;
                    dbFile.vc_filePath = "../../images/uploads/" + fileName;
                    dbFile.vc_fileDescription = fileName;
                    dbFile.i_fileType = 1;
                    dbFile.dt_datetime = DateTime.Now;

                    db.as_fileUploadProfile.Add(dbFile);
                    db.SaveChanges();
                }

                return guid.ToString();
            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.log("Failed to upload File: " + err.Message + " | " + err.InnerException.Message, "UploadFile", Logging.logTypes.Error, "iOS");
                Response.StatusCode = 500;
                return err.Message;
            }
        }

        private void saveThumbNails(string file)
        {
            // Get a bitmap.
            Bitmap bmp1 = new Bitmap(file);
            ImageCodecInfo jgpEncoder = GetEncoder(ImageFormat.Jpeg);

            // Create an Encoder object based on the GUID 
            // for the Quality parameter category.
            System.Drawing.Imaging.Encoder myEncoder =
                System.Drawing.Imaging.Encoder.Quality;

            // Create an EncoderParameters object. 
            // An EncoderParameters object has an array of EncoderParameter 
            // objects. In this case, there is only one 
            // EncoderParameter object in the array.
            EncoderParameters myEncoderParameters = new EncoderParameters(1);

            EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, 50L);
            myEncoderParameters.Param[0] = myEncoderParameter;
            bmp1.Save(file.Replace(".jpg", "_50.jpg"), jgpEncoder, myEncoderParameters);

            myEncoderParameter = new EncoderParameter(myEncoder, 100L);
            myEncoderParameters.Param[0] = myEncoderParameter;
            bmp1.Save(file.Replace(".jpg", "_med.jpg"), jgpEncoder, myEncoderParameters);
        }

        private bool IsImage(HttpPostedFileBase file)
        {
            if (file.ContentType.Contains("image"))
            {
                return true;
            }

            string[] formats = new string[] { ".jpg", ".png", ".gif", ".jpeg" };

            // linq
            return formats.Any(item => file.FileName.EndsWith(item, StringComparison.OrdinalIgnoreCase));
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {

            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        [HttpPost]
        public string updateFileInfo(List<fileUpload> info)
        {
            try
            {
                string lastUpdated = "";
                foreach (fileUpload item in info)
                {
                    as_fileUploadInfo file = new as_fileUploadInfo();
                    file.guid_file = Guid.Parse(item.file_guid);
                    file.vc_description = item.description;
                    file.f_latitude = item.latitude;
                    file.f_longitude = item.longitude;
                    file.i_shiftId = item.shiftId;
                    file.i_userId = item.userId;

                    db.as_fileUploadInfo.Add(file);
                    db.SaveChanges();
                    lastUpdated = item.file_guid;
                }

                return lastUpdated;
            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.log("Failed to upload File Info: " + err.Message + " | " + err.InnerException.Message, "updateFileInfo", Logging.logTypes.Error, "iOS");
                Response.StatusCode = 500;
                return err.Message;
            }
        }

        #endregion

        #region Cache and Views

        public ActionResult rebuildCache()
        {
            CacheHelper cache = new CacheHelper();
            @ViewBag.AssetFullDownload = cache.createAllAssetDownload();
            @ViewBag.AssetRebuild = cache.createAssetDownloadCache();
            @ViewBag.AssetClassRebuild = cache.createAssetClassDownloadCache();
            return View();
        }

        public ActionResult UnitTests()
        {
            return View();
        }

        #endregion


    }

}