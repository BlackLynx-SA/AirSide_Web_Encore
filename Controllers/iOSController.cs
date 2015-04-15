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
// SUMMARY: This class contains all controller calls for iOS calls to use with the iPad application
#endregion

using ADB.AirSide.Encore.V1.Models;
using AirSide.ServerModules.Helpers;
using AirSide.ServerModules.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
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

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		public iOSController()
			: this(new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext())))
		{
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		public iOSController(UserManager<ApplicationUser> userManager)
		{
			UserManager = userManager;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		public UserManager<ApplicationUser> UserManager { get; private set; }

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		private ApplicationSignInManager _signInManager;

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		public ApplicationSignInManager SignInManager
		{
			get
			{
				return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
			}
			private set { _signInManager = value; }
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
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

		#endregion

		#region Shifts

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		public JsonResult getMaintenanceChecks(int maintenanceId)
		{
			try
			{
				//This method will return all check items assoisated with a maintenance profile
				//Create Date: 2015/03/04
				//Author: Bernard Willer

				var checks = db.as_maintenanceCheckListDef.Where(q => q.i_maintenanceId == maintenanceId).ToList();

				return Json(checks);
			}
			catch (Exception err)
			{
				LogHelper log = new LogHelper();
				log.logError(err, User.Identity.Name + "(" + Request.UserHostAddress + ")");
				Response.StatusCode = 500;
				return Json(new { message = err.Message });
			}
		
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------

		[HttpPost]
		public JsonResult getAllMaintenanceChecks()
		{
			try
			{
				//This method will return all check items 
				//Create Date: 2015/03/18
				//Author: Bernard Willer

				var checks = db.as_maintenanceCheckListDef.ToList();

				return Json(checks);
			}
			catch (Exception err)
			{
				LogHelper log = new LogHelper();
				log.logError(err, User.Identity.Name + "(" + Request.UserHostAddress + ")");
				Response.StatusCode = 500;
				return Json(new { message = err.Message });
			}

		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		public JsonResult addMaintenanceCheckEntity(as_maintenanceCheckListEntity entity)
		{
			try
			{
				//This code will insert a check entity to the db
				//Create Date: 2015/03/04
				//Author: Bernard Willer

				db.as_maintenanceCheckListEntity.Add(entity);
				db.SaveChanges();

				return Json(new { message = "Added Check" });
			}
			catch (Exception err)
			{
				LogHelper log = new LogHelper();
				log.logError(err, User.Identity.Name + "(" + Request.UserHostAddress + ")");
				Response.StatusCode = 500;
				return Json(new { message = err.Message });
			}
		
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------

		[HttpPost]
		public JsonResult addMaintenanceCheckEntities(List<TaskCheckUpload> entities)
		{
			try
			{
				//This code will insert a list of check entities to the db
				//Create Date: 2015/03/04
				//Author: Bernard Willer

				foreach (TaskCheckUpload entity in entities)
				{
					as_maintenanceCheckListEntity check = new as_maintenanceCheckListEntity();
					check.dt_captureDate = DateTime.Now;
					check.i_assetId = entity.i_assetId;
					check.i_maintenanceCheckId = entity.i_maintenanceCheckId;
					check.i_shiftId = entity.i_shiftId;
					check.UserId = entity.UserId;
					check.vc_capturedValue = entity.vc_capturedValue;

					db.as_maintenanceCheckListEntity.Add(check);
					db.SaveChanges();
				}

				return Json(new { message = "Added Check" });
			}
			catch (Exception err)
			{
				LogHelper log = new LogHelper();
				log.logError(err, User.Identity.Name + "(" + Request.UserHostAddress + ")");
				Response.StatusCode = 500;
				return Json(new { message = err.Message });
			}

		}
		
		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		public JsonResult getMaintenanceProfile()
		{
			var maintenanceProfiles = (from x in db.as_maintenanceProfile
									   join y in db.as_maintenanceValidation on x.i_maintenanceValidationId equals y.i_maintenanceValidationId
									   select new
									   {
										   i_maintenanceId = x.i_maintenanceId,
										   vc_description = x.vc_description,
										   i_maintenanceCategoryId = x.i_maintenanceCategoryId,
										   i_maintenanceValidationId = x.i_maintenanceValidationId,
										   vc_validationName = y.vc_validationName,
										   vc_validationDescr = y.vc_validationDescription
									   }).ToList();

			return Json(maintenanceProfiles);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------

		[HttpPost]
		public JsonResult getAssetHistory(int assetId)
		{
			try
			{
				//This will send back a list of actions on asset
				//Create Date: 2015/02/06
				//Author: Bernard Willer

				List<assetHistory> allHistory = new List<assetHistory>();
				allHistory.AddRange(validationTasks(assetId));
				allHistory.AddRange(torqueTasks(assetId));
				allHistory.AddRange(visualSurveys(assetId));

				if (allHistory.Count == 1)
					if (allHistory[0].maintenance == null)
						allHistory.Clear();

				return Json(allHistory);
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
		public JsonResult uploadImage(iOSImageUpload imageUpload)
		{
			try
			{
				//Uploading of coverted image to storage
				//Create Date: 2015/02/06   
				//Author: Bernard Willer

				Guid guid = Guid.NewGuid();
				string filename = guid.ToString();
				filename = filename.Replace("-", "");
				filename = filename.Substring(0, 5) + ".png";

				//Convert received image
				byte[] bytes = Convert.FromBase64String(imageUpload.image);
				MemoryStream ms = new MemoryStream(bytes);

				ms.Position = 0;
				CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=airsideios;AccountKey=mv73114kNAR2ZtJhZWwpU8W/tzVjH3R7rgtNc5LGQNeCUqR/UGpS3bBwwdX/L6ieG/Hi99JHJSwdxPWYRydYHA==");
				CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
				CloudBlobContainer container = blobClient.GetContainerReference("surveyorimages");
				CloudBlockBlob blockBlob = container.GetBlockBlobReference(filename);
				blockBlob.UploadFromStream(ms);

				//Free Memory
				ms.Dispose();
				Response.StatusCode = 200;
				return Json(new { message = "success" });
			}
			catch (Exception err)
			{
				LogHelper log = new LogHelper();
				log.logError(err, User.Identity.Name + "(" + Request.UserHostAddress + ")");
				Response.StatusCode = 500;
				return Json(new { message = err.Message });
			}
		
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
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

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		[AllowAnonymous]
		public JsonResult checkVersion()
		{
			var iOSVersion = (from data in db.as_settingsProfile
							  where data.vc_settingDescription == "iOSVersion"
							  select data.vc_settingValue).First();
			return Json(new { CurrentVersion = iOSVersion });
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		//TODO: Change date time to come from iPad not server
		public JsonResult insertAssetValidation(List<as_validationTaskProfile> validations)
		{
			try
			{
				DateTime now = DateTime.Now;
				foreach (as_validationTaskProfile item in validations)
				{
					item.dt_dateTimeStamp = now;
					db.as_validationTaskProfile.Add(item);
					db.SaveChanges();

					//rebuild cache for asset
					CacheHelper cache = new CacheHelper();
					cache.rebuildAssetProfileForAsset(item.i_assetId);
				}

				return Json(new { status = "success", count = validations.Count});
			}
			catch (Exception err)
			{
				LogHelper log = new LogHelper();
				log.log("Failed to insert validation values: " + err.Message, "insertAssetValidation(iOS)", LogHelper.logTypes.Error, "iOS Device");
				Response.StatusCode = 500;
				return Json(err.Message);
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
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
							  join a in db.as_technicianGroups on x.i_technicianGroup equals a.i_groupId
							  join b in db.as_maintenanceProfile on x.i_maintenanceId equals b.i_maintenanceId
							  where x.i_technicianGroup == userId && x.bt_completed == closeShiftsFlag && x.bt_custom == false
							  select new
							  {
								  i_shiftId = x.i_shiftId,
								  sheduledDate = x.dt_scheduledDate,
								  i_areaSubId = x.i_areaSubId,
								  sheduleTime = x.dt_scheduledDate,
								  permitNumber = x.vc_permitNumber,
								  techGroup = a.vc_groupName,
								  areaName = z.vc_description,
								  validation = b.i_maintenanceValidationId,
								  maintenanceId = x.i_maintenanceId,
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
					if (item.validation == 1)
						shift.validation = "YES";
					else 
						shift.validation = "NO";
					
					//2015/01/19
					//Add functionality for custom shifts
					shift.shiftType = 0;
					shift.assets = new int[0];
					shift.maintenanceId = item.maintenanceId;

					shiftList.Add(shift);
				}

				//Add custom shifts to array
				var customShifts = (from x in db.as_shifts
								   join y in db.as_technicianGroups on x.i_technicianGroup equals y.i_groupId
								   join z in db.as_maintenanceProfile on x.i_maintenanceId equals z.i_maintenanceId
								   where y.i_groupId == userId && x.bt_completed == closeShiftsFlag && x.bt_custom == true
								   select new
								   {
									   i_shiftId = x.i_shiftId,
									   sheduledDate = x.dt_scheduledDate,
									   i_areaSubId = 0,
									   sheduleTime = x.dt_scheduledDate,
									   permitNumber = x.vc_permitNumber,
									   techGroup = y.vc_groupName,
									   areaName = "Custom",
									   validation = z.i_maintenanceValidationId,
									   maintenanceId = x.i_maintenanceId
								   }).ToList();

				foreach (var item in customShifts)
				{
					technicianShift shift = new technicianShift();
					shift.i_shiftId = item.i_shiftId;
					shift.sheduledDate = item.sheduledDate.ToString("yyy/MM/dd");
					shift.i_areaSubId = item.i_areaSubId;
					shift.sheduleTime = item.sheduleTime.ToString("hh:mm:ss");
					shift.permitNumber = item.permitNumber;
					shift.techGroup = item.techGroup;
					shift.areaName = item.areaName;
					if (item.validation == 1)
						shift.validation = "YES";
					else 
						shift.validation = "NO";

					shift.shiftType = 1;
					shift.maintenanceId = item.maintenanceId;
					var assets = (from x in db.as_shiftsCustomProfile
								  where x.i_shiftId == item.i_shiftId
								  select x.i_assetId).ToList();

					shift.assets = new int[assets.Count];
					int i = 0;
					foreach(int asset in assets)
					{
						shift.assets[i] = asset;
						i++;
					}

					shiftList.Add(shift);
				}

				return Json(shiftList);
			}
			catch (Exception err)
			{
				LogHelper log = new LogHelper();
				log.log("Failed to retrieve technician shifts: " + err.Message, "getTechnicianShifts(iOS)", LogHelper.logTypes.Error, "iOS Device");
				Response.StatusCode = 500;
				return Json(err.Message);
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
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
						LogHelper log = new LogHelper();
						log.log("Failed to update cache for asset " + shift.i_assetId.ToString() + " - " + err.InnerException.Message, "insertShiftData", LogHelper.logTypes.Error, "SYSTEM");
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

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		public JsonResult updateAssetStatusBulk(List<AssetStatusUpload> assetList)
		{
			LogHelper log = new LogHelper();
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
						asset.dt_lastUpdated = DateTime.Now;

						db.Entry(asset).State = System.Data.Entity.EntityState.Modified;
						db.SaveChanges();
					}
					else
					{
						as_assetStatusProfile newStatus = new as_assetStatusProfile();
						newStatus.bt_assetStatus = status;
						newStatus.i_assetProfileId = item.assetId;
						newStatus.i_assetSeverity = item.assetSeverity;
						asset.dt_lastUpdated = DateTime.Now;

						db.as_assetStatusProfile.Add(newStatus);
						db.SaveChanges();
					}
				}

				return Json(new { assetCount = assetList.Count });
			}
			catch (Exception err)
			{
				Response.StatusCode = 500;
				log.log("Failed to set asset status: " + err.Message, "updateAssetStatusBulk", LogHelper.logTypes.Error, "iOS");
				return Json("Failed");
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		public JsonResult setAssetStatus(int assetId, string assetStatus, int assetSeverity)
		{
			LogHelper log = new LogHelper();
			try
			{
				Boolean status = true;
				if (assetStatus == "False") status = false;
				var asset = db.as_assetStatusProfile.Find(assetId);

				if (asset != null)
				{
					asset.bt_assetStatus = status;
					asset.dt_lastUpdated = DateTime.Now;

					db.Entry(asset).State = System.Data.Entity.EntityState.Modified;
					db.SaveChanges();
				}
				else
				{
					as_assetStatusProfile newStatus = new as_assetStatusProfile();
					newStatus.bt_assetStatus = status;
					newStatus.i_assetProfileId = assetId;
					newStatus.i_assetSeverity = assetSeverity;
					newStatus.dt_lastUpdated = DateTime.Now;

					db.as_assetStatusProfile.Add(newStatus);
					db.SaveChanges();
				}

				CacheHelper cache = new CacheHelper();
				cache.rebuildAssetProfileForAsset(assetId);

				AssetStatus returnType = new AssetStatus();
				returnType.i_assetProfileId = assetId;
				returnType.bt_assetStatus = status;

				return Json(returnType);

			}
			catch (Exception err)
			{

				log.log("Failed to set asset status: " + err.Message, "setAssetStatus", LogHelper.logTypes.Error, "iOS");
				return Json("Failed");
			}

		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
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
				LogHelper log = new LogHelper();
				log.log("Failed to retrieve all asset download: " + err.Message, "getAllAssets(iOS)", LogHelper.logTypes.Error, "iOS Device");
				Response.StatusCode = 500;
				return Json(err.Message);
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		public JsonResult getAllAssetClasses()
		{
			try
			{
				CacheHelper cache = new CacheHelper();
				List<mongoAssetClassDownload> assetClassList = cache.getAllAssetClasses();

				return Json(assetClassList);

				//if (assetClassList.Count == 0)
				//{
				//    List<assetClassDownload> assetList = new List<assetClassDownload>();
				//    DatabaseHelper func = new DatabaseHelper();

				//    var assets = (from x in db.as_assetClassProfile
				//                  select x);

				//    foreach (var item in assets)
				//    {
				//        assetClassDownload asset = new assetClassDownload();
				//        asset.i_assetClassId = item.i_assetClassId;
				//        asset.vc_description = item.vc_description;
				//        asset.i_assetCheckTypeId = 0;
				//        asset.assetCheckCount = func.getNumberOfFixingPoints(item.i_assetClassId);
				//        assetList.Add(asset);
				//    }
				//    return Json(assetList);
				//}
				//else
				//{
				//    return Json(assetClassList);
				//}

			}
			catch (Exception err)
			{
				LogHelper log = new LogHelper();
				log.log("Failed to retrieve all asset classes: " + err.Message, "getAllAssetClasses(iOS)", LogHelper.logTypes.Error, "iOS Device");
				Response.StatusCode = 500;
				return Json(err.Message);
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		public JsonResult getAssetPerTagId(string tagId)
		{
			try
			{
				List<AssetTagReply> assetList = new List<AssetTagReply>();
				DatabaseHelper func = new DatabaseHelper();
				CacheHelper cache = new CacheHelper();

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
					asset.lastMaintainedDate = cache.getAssetPreviousDateForFirstTask(item.i_assetId);
					asset.nextMaintenanceDate =  cache.getAssetNextDateForFirstTask(item.i_assetId);
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
				LogHelper log = new LogHelper();
				log.log("Failed to retrieve asset per tagId: " + err.Message, "getAssetPerTagId(iOS)", LogHelper.logTypes.Error, "iOS Device");
				Response.StatusCode = 500;
				return Json(err.Message);
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
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
				LogHelper log = new LogHelper();
				log.log("Failed to retrieve asset assosiations: " + err.Message, "getAssetAssosiations(iOS)", LogHelper.logTypes.Error, "iOS Device");
				Response.StatusCode = 500;
				return Json(err.Message);
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
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
				LogHelper log = new LogHelper();
				log.log("Failed to retrieve asset with tagid: " + err.Message, "getAssetWithTagId(iOS)", LogHelper.logTypes.Error, "iOS Device");
				Response.StatusCode = 500;
				return Json(err.Message);
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		public JsonResult updateAssetTagBulk(List<AssetAssosiationUpload> assetList)
		{
			try
			{
				LogHelper log = new LogHelper();
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
						log.log("Tag id was update from " + currentTagId + " to " + item.tagId, "updateAssetTag(iOS)", LogHelper.logTypes.Info, "iOS Device");
						i++;

						try
						{
							CacheHelper cache = new CacheHelper();
							cache.rebuildAssetProfileForAsset(item.assetId);
							cache.createAllAssetDownloadForAsset(asset.i_assetId);
						}
						catch (Exception err)
						{
							log.log("Failed to update cache for asset " + item.assetId.ToString() + " - " + err.InnerException.Message, "insertShiftData", LogHelper.logTypes.Error, "SYSTEM");
						}
					}
					else
					{
						log.log("Reference tag for assetid " + item.assetId.ToString() + " wasn't found.", "updateAssetTag(iOS)", LogHelper.logTypes.Info, "iOS Device");
					}
				}

				return Json(new { assetCount = i.ToString() });
			}
			catch (Exception err)
			{
				LogHelper log = new LogHelper();
				log.log("Failed to update asset tag: " + err.Message, "updateAssetTagBulk(iOS)", LogHelper.logTypes.Error, "iOS Device");
				Response.StatusCode = 500;
				return Json(err.Message);
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		public JsonResult updateAssetTag(int assetId, int UserId, string tagId, string serialNumber)
		{
			try
			{
				LogHelper log = new LogHelper();

				as_assetProfile asset = db.as_assetProfile.Find(assetId);

				if (asset != null)
				{
					string currentTagId = asset.vc_rfidTag;
					asset.vc_rfidTag = tagId;
					asset.vc_serialNumber = serialNumber;
					db.Entry(asset).State = System.Data.Entity.EntityState.Modified;
					db.SaveChanges();

					//Log the change
					log.log("Tag id was update from " + currentTagId + " to " + tagId, "updateAssetTag(iOS)", LogHelper.logTypes.Info, UserId.ToString());

					try
					{
						CacheHelper cache = new CacheHelper();
						cache.rebuildAssetProfileForAsset(asset.i_assetId);
						cache.createAllAssetDownloadForAsset(asset.i_assetId);
					}
					catch (Exception err)
					{
						log.log("Failed to update cache for asset " + asset.i_assetId.ToString() + " - " + err.InnerException.Message, "insertShiftData", LogHelper.logTypes.Error, "SYSTEM");
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
				LogHelper log = new LogHelper();
				log.log("Failed to update asset tag: " + err.Message, "updateAssetTag(iOS)", LogHelper.logTypes.Error, "iOS Device");
				Response.StatusCode = 500;
				return Json(err.Message);
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
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
						LogHelper log = new LogHelper();
						log.log("Failed to update cache for asset " + newAsset.i_assetId.ToString() + " - " + err.InnerException.Message, "insertShiftData", LogHelper.logTypes.Error, "SYSTEM");
					}
				}

				//2014/10/28 - Changed "assets" to "success" after iPad failure
				return Json(new { success = assets.Count });
			}
			catch (Exception err)
			{
				LogHelper log = new LogHelper();
				log.log("Failed to insert asset assosiation: " + err.Message, "insertAssetAssosiation(iOS)", LogHelper.logTypes.Error, "iOS Device");
				Response.StatusCode = 500;
				return Json("[{error:" + err.Message + "}]");
			}
		}

		#endregion

		#region Areas

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
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

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
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
				LogHelper log = new LogHelper();
				log.log("Failed to get main areas: " + err.Message, "getMainAreas(iOS)", LogHelper.logTypes.Error, "iOS Device");
				Response.StatusCode = 500;
				return Json("error: " + err.Message);
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
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
				LogHelper log = new LogHelper();
				log.log("Failed to get sub areas: " + err.Message, "getSubAreas(iOS)", LogHelper.logTypes.Error, "iOS Device");
				Response.StatusCode = 500;
				return Json("error: " + err.Message);
			}
		}

		#endregion

		#region Wrench Info

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
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

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
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

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
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

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
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

							LogHelper log = new LogHelper();
							log.log("Failed to upload File (Image Check): " + err.Message + " | " + err.InnerException.Message, "UploadFile", LogHelper.logTypes.Error, "iOS");
						}
					}

					string[] fileSplit = file.FileName.Split(char.Parse("."));
					string extension = fileSplit[1];
					int fileType = 0;

					switch(extension)
					{
						case "jpg": fileType = 1;
							break;
						case "m4a": fileType = 2;
							break;
						case "text": fileType = 3;
							break;
						default:
							break;
					}


					as_fileUploadProfile dbFile = new as_fileUploadProfile();
					dbFile.guid_file = guid;
					dbFile.vc_filePath = "../../images/uploads/" + fileName;
					dbFile.vc_fileDescription = fileName;
					dbFile.i_fileType = fileType;
					dbFile.dt_datetime = DateTime.Now;

					db.as_fileUploadProfile.Add(dbFile);
					db.SaveChanges();
				}

				return guid.ToString();
			}
			catch (Exception err)
			{
				LogHelper log = new LogHelper();
				log.log("Failed to upload File: " + err.Message + " | " + err.InnerException.Message, "UploadFile", LogHelper.logTypes.Error, "iOS");
				Response.StatusCode = 500;
				return err.Message;
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		private void saveThumbNails(string file)
		{
			// Get a bitmap.
			Bitmap bmp1 = new Bitmap(file);
			ImageCodecInfo jgpEncoder = GetEncoder(ImageFormat.Jpeg);

			System.Drawing.Imaging.Encoder myEncoder =
				System.Drawing.Imaging.Encoder.Quality;

			EncoderParameters myEncoderParameters = new EncoderParameters(1);

			EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, 50L);
			myEncoderParameters.Param[0] = myEncoderParameter;
			bmp1.Save(file.Replace(".jpg", "_50.jpg"), jgpEncoder, myEncoderParameters);

			myEncoderParameter = new EncoderParameter(myEncoder, 100L);
			myEncoderParameters.Param[0] = myEncoderParameter;
			bmp1.Save(file.Replace(".jpg", "_med.jpg"), jgpEncoder, myEncoderParameters);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
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

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
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

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
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
					file.i_userId_logged = item.userId;
					file.i_severityId = item.severity;
					file.i_userId_resolved = 0;
					file.dt_dateTimeResolved = DateTime.Parse("2300/01/01");

					db.as_fileUploadInfo.Add(file);
					db.SaveChanges();
					lastUpdated = item.file_guid;
				}

				return lastUpdated;
			}
			catch (Exception err)
			{
				LogHelper log = new LogHelper();
				log.log("Failed to upload File Info: " + err.Message + " | " + err.InnerException.Message, "updateFileInfo", LogHelper.logTypes.Error, "iOS");
				Response.StatusCode = 500;
				return err.Message;
			}
		}

		#endregion

		#region Cache and Views

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		public ActionResult rebuildCache()
		{
			CacheHelper cache = new CacheHelper();
			@ViewBag.AssetFullDownload = cache.createAllAssetDownload();
			@ViewBag.AssetRebuild = cache.createAssetDownloadCache();
			@ViewBag.AssetClassRebuild = cache.createAssetClassDownloadCache();
			return View();
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		public ActionResult UnitTests()
		{
			return View();
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		public string UploadLogFile(HttpPostedFileBase file, int UserId, string uid)
		{
			try
			{
				//This module persists the log files to Azure Stroage Container and ref to SQL
				//Create Date: 2015/01/27
				//Author: Bernard Willer

				Guid uploadGuid = Guid.NewGuid();
				string fileName = uploadGuid.ToString();
				fileName = fileName.Replace("-", "");

				//Upload to Storage Container
				CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=airsideios;AccountKey=mv73114kNAR2ZtJhZWwpU8W/tzVjH3R7rgtNc5LGQNeCUqR/UGpS3bBwwdX/L6ieG/Hi99JHJSwdxPWYRydYHA==");
				CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
				CloudBlobContainer container = blobClient.GetContainerReference("logs");
				CloudBlockBlob blockBlob = container.GetBlockBlobReference(fileName + ".log");
				blockBlob.UploadFromStream(file.InputStream);

				//Persist to Database
				as_iosLogProfile log = new as_iosLogProfile();
				log.dt_logCaptureDate = DateTime.Now;
				log.UserId = UserId;
				log.vc_deviceUID = uid;
				log.vc_logContainer = container.Name;
				log.vc_logName = fileName + ".log";

				db.as_iosLogProfile.Add(log);
				db.SaveChanges();

				return "Success";
			}
			catch (Exception err)
			{
				LogHelper log = new LogHelper();
				log.logError(err, "iOS");
				Response.StatusCode = 500;
				return err.Message;
			}
		
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		public JsonResult getCacheStatus()
		{
			
			try
			{
				//Sends the current hashes of the different download sets
				//Create Date: 2015/01/27   
				//Author: Bernard Willer

				var cache = db.as_cacheProfile.Where(q => q.bt_active == true).ToList();
				return Json(cache);
			}
			catch (Exception err)
			{
				LogHelper log = new LogHelper();
				log.logError(err, "iOS");
				Response.StatusCode = 500;
				return Json(new { message = err.Message });
			}
		
		}

		#endregion

		#region Web Views

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------

		public ActionResult iOSImages()
		{
			//Send List of Images
			var allFiles = db.as_iosImageProfile.Where(q => q.bt_active == true).OrderByDescending(q => q.dt_dateTimeStamp).ToList();
			ViewData["iosImages"] = allFiles;
			return View();
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------

		public ActionResult uploadiOSFile(HttpPostedFileBase file, string description, string version, string releaseNotes)
		{

			try
			{
				//Upload iOS files for testing during development
				//Create Date: 2015/01/27
				//Author: Bernard Willer

				//Upload to Storage Container
				Guid guid = Guid.NewGuid();
				string filename = guid.ToString();
				filename = filename.Replace("-", "");
				filename = filename.Substring(0, 5) + "_" + file.FileName;

				CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=airsideios;AccountKey=mv73114kNAR2ZtJhZWwpU8W/tzVjH3R7rgtNc5LGQNeCUqR/UGpS3bBwwdX/L6ieG/Hi99JHJSwdxPWYRydYHA==");
				CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
				CloudBlobContainer container = blobClient.GetContainerReference("iosimages");
				CloudBlockBlob blockBlob = container.GetBlockBlobReference(filename);
				blockBlob.UploadFromStream(file.InputStream);

				as_iosImageProfile image = new as_iosImageProfile();
				image.dt_dateTimeStamp = DateTime.Now;
				image.bt_active = true;
				image.vc_description = description;
				image.vc_fileName = filename;
				image.vc_version = version;
				image.vc_releaseNotes = releaseNotes;

				db.as_iosImageProfile.Add(image);
				db.SaveChanges();

				Response.StatusCode = 200;
				return Json(new { message = "success" });
			}
			catch (Exception err)
			{
				LogHelper log = new LogHelper();
				log.logError(err, User.Identity.Name + "(" + Request.UserHostAddress + ")");
				Response.StatusCode = 500;
				return Json(new { message = err.Message });
			}

		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------

		public ActionResult downloadFile(int id)
		{
			try
			{
				//This function allows the user to download a file from Azure Storage
				//Create Date: 2015/01/27
				//Author: Bernard Willer

				as_iosImageProfile image = db.as_iosImageProfile.Find(id);

				//Downlaod File
				CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=airsideios;AccountKey=mv73114kNAR2ZtJhZWwpU8W/tzVjH3R7rgtNc5LGQNeCUqR/UGpS3bBwwdX/L6ieG/Hi99JHJSwdxPWYRydYHA==");
				CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
				CloudBlobContainer container = blobClient.GetContainerReference("iosimages");
				CloudBlockBlob blockBlob = container.GetBlockBlobReference(image.vc_fileName);

				MemoryStream memoryStream = new MemoryStream();
				blockBlob.DownloadToStream(memoryStream);
				memoryStream.Position = 0;
				Response.AddHeader("content-disposition", "attachment; filename=" + image.vc_fileName);
				return File(memoryStream, "application/");
			}
			catch (Exception err)
			{
				LogHelper log = new LogHelper();
				log.logError(err, User.Identity.Name + "(" + Request.UserHostAddress + ")");
				Response.StatusCode = 500;
				return Json(new { message = err.Message });
			}
		
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------

		#endregion

		#region Helpers

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------

		private List<assetHistory> validationTasks(int assetId)
		{
			var validation = from x in db.as_validationTaskProfile
							 join y in db.UserProfiles on x.UserId equals y.UserId
							 join a in db.as_shifts on x.i_shiftId equals a.i_shiftId
							 join z in db.as_maintenanceProfile on a.i_maintenanceId equals z.i_maintenanceId
							 where x.i_assetId == assetId
							 select new
							 {
								 user = y.FirstName + " " + y.LastName,
								 date = x.dt_dateTimeStamp,
								 maintenanceTask = z.vc_description
							 };

			List<assetHistory> tasks = new List<assetHistory>();

			foreach (var item in validation.OrderByDescending(q=>q.date).Take(3))
			{
				assetHistory task = new assetHistory();
				task.type = 2;
				task.maintenance = item.maintenanceTask + "(" + item.user + ")";
				task.valueCaptured = item.user + " performed a " + item.maintenanceTask + " task";
				task.datetimeStamp = item.date.ToString("dd MMM, yyyy");
				tasks.Add(task);
			}

			return tasks;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------

		private List<assetHistory> torqueTasks(int assetId)
		{
			var torque = (from x in db.as_shiftData
						  join y in db.as_shifts on x.i_shiftId equals y.i_shiftId
						  join z in db.as_technicianGroups on y.i_technicianGroup equals z.i_groupId
						  join a in db.as_maintenanceProfile on y.i_maintenanceId equals a.i_maintenanceId
						  where x.i_assetId == assetId
						  select new
						  {
							  name = z.vc_groupName,
							  date = x.dt_captureDate,
							  maintenanceTask = a.vc_description,
							  value = x.f_capturedValue,
							  shiftId = x.i_shiftId
						  }).OrderByDescending(q => q.date).Take(10);

			List<assetHistory> tasks = new List<assetHistory>();

			int shiftId = 0;
			int pointer = 0;

			assetHistory task = new assetHistory();

			foreach (var item in torque)
			{
				if (item.shiftId != shiftId || shiftId == 0)
				{
					if (shiftId != 0)
						tasks.Add(task);

					task = new assetHistory();
					task.maintenance = "Fitting was torqued (" + item.name + ")";
					task.datetimeStamp = item.date.ToString("dd MMM, yyyy");
					task.type = 1;
					shiftId = item.shiftId;
					pointer = 0;
				}

				if(pointer != 0)
					task.valueCaptured += "," + item.value.ToString();
				else
					task.valueCaptured = item.value.ToString();

				pointer++;
			}

			tasks.Add(task);

			return tasks;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------

		private List<assetHistory> visualSurveys(int assetId)
		{
			List<assetHistory> items = new List<assetHistory>();

			try
			{
				var location = (from x in db.as_assetProfile
								join y in db.as_locationProfile on x.i_locationId equals y.i_locationId
								where x.i_assetId == assetId
								select new
								{
									longitude = y.f_longitude,
									latitude = y.f_latitude
								}).FirstOrDefault();

				as_get_closest_point_to_gps_coordinate1_Result closest = db.as_get_closest_point_to_gps_coordinate(location.latitude, location.longitude).FirstOrDefault();

				var surveys = (from x in db.as_fileUploadInfo
							   join y in db.as_fileUploadProfile on x.guid_file equals y.guid_file
							   join b in db.UserProfiles on x.i_userId_logged equals b.UserId
							   where x.f_longitude == closest.longitude && x.f_latitude == closest.latitude
							   select new
							   {
								   user = b.FirstName + " " + b.LastName,
								   date = y.dt_datetime,
								   fileLocation = y.vc_filePath,
								   type = y.i_fileType,
								   resolved = x.bt_resolved
							   }).OrderByDescending(q => q.date).Take(10);

				
				foreach (var item in surveys.OrderByDescending(q => q.date).Take(5))
				{
					assetHistory asset = new assetHistory();
					asset.datetimeStamp = item.date.ToString("dd MMM, yyyy");
					asset.type = 3;
					string resolved = "Open";
					if (item.resolved) resolved = "Resolved";
					asset.valueCaptured = item.user + "(" + resolved + ")";

					string[] filepath = item.fileLocation.Split(char.Parse("."));
					int place = filepath.Count() - 1;

					if (filepath[place] == "jpg")
					{
						asset.maintenance = "Image taken by " + item.user + "(" + resolved + ")";
					}
					else if (filepath[place] == "m4a")
					{
						asset.maintenance = "Voice memo taken by" + item.user + "(" + resolved + ")";
					}
					else if (filepath[place] == "text")
					{
						asset.maintenance = "Text captured by " + item.user + "(" + resolved + ")";
					}

					items.Add(asset);
				}

				return items;
			} catch(Exception ex)
			{
				LogHelper log = new LogHelper();
				log.logError(ex, User.Identity.Name);
				return items;
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------

		#endregion
	}

}