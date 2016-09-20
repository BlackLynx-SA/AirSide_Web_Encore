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
using System.Configuration;
using System.Data.Entity;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace ADB.AirSide.Encore.V1.Controllers
{

	[Authorize]
	public class IosController : Controller
	{
		private readonly Entities _db = new Entities();
		private readonly CacheHelper _cache = new CacheHelper(ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString, ConfigurationManager.ConnectionStrings["MongoServer"].ConnectionString);
		private readonly DatabaseHelper _func = new DatabaseHelper();


		#region Authentication

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		public IosController()
			: this(new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext())))
		{
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		public IosController(UserManager<ApplicationUser> userManager)
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
			private set
			{
				if (value == null) throw new ArgumentNullException(nameof(value));
				_signInManager = value;
			}
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
						var loggedInUser = (from data in _db.UserProfiles
											where data.UserName == username
											select data).First();

						iOSLogin user = new iOSLogin();
						Guid newSession = Guid.NewGuid();

						var groupId = (from x in _db.as_technicianGroupProfile
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
				default:
					{
						iOSLogin user = new iOSLogin
						{
							FirstName = "None",
							LastName = "None",
							UserId = -1,
							i_accessLevel = -1,
							i_airPortId = -1,
							SessionKey = "None",
							i_groupId = -1
						};
						return Json(user);
					}
			}
		}

		#endregion

		#region Shifts

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		public JsonResult GetMaintenanceChecks(int maintenanceId)
		{
			try
			{
				//This method will return all check items assoisated with a maintenance profile
				//Create Date: 2015/03/04
				//Author: Bernard Willer

				var checks = _db.as_maintenanceCheckListDef.Where(q => q.i_maintenanceId == maintenanceId).ToList();

				return Json(checks);
			}
			catch (Exception err)
			{
				_cache.LogError(err, User.Identity.Name + "(" + Request.UserHostAddress + ")");
				Response.StatusCode = 500;
				return Json(new { message = err.Message });
			}
		
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------

		[HttpPost]
		public JsonResult GetAllMaintenanceChecks()
		{
			try
			{
				//This method will return all check items 
				//Create Date: 2015/03/18
				//Author: Bernard Willer

				var checks = _db.as_maintenanceCheckListDef.ToList();

				return Json(checks);
			}
			catch (Exception err)
			{
				_cache.LogError(err, User.Identity.Name + "(" + Request.UserHostAddress + ")");
				Response.StatusCode = 500;
				return Json(new { message = err.Message });
			}

		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		public JsonResult AddMaintenanceCheckEntity(as_maintenanceCheckListEntity entity)
		{
			try
			{
				//This code will insert a check entity to the db
				//Create Date: 2015/03/04
				//Author: Bernard Willer

				_db.as_maintenanceCheckListEntity.Add(entity);
				_db.SaveChanges();

				return Json(new { message = "Added Check" });
			}
			catch (Exception err)
			{
				_cache.LogError(err, User.Identity.Name + "(" + Request.UserHostAddress + ")");
				Response.StatusCode = 500;
				return Json(new { message = err.Message });
			}
		
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------

		[HttpPost]
		public JsonResult AddMaintenanceCheckEntities(List<TaskCheckUpload> entities)
		{
			try
			{
				//This code will insert a list of check entities to the db
				//Create Date: 2015/03/04
				//Author: Bernard Willer

				foreach (TaskCheckUpload entity in entities)
				{
					as_maintenanceCheckListEntity check = new as_maintenanceCheckListEntity
					{
						dt_captureDate = DateTime.Now,
						i_assetId = entity.i_assetId,
						i_maintenanceCheckId = entity.i_maintenanceCheckId,
						i_shiftId = entity.i_shiftId,
						UserId = entity.UserId,
						vc_capturedValue = entity.vc_capturedValue
					};

					_db.as_maintenanceCheckListEntity.Add(check);
					_db.SaveChanges();
				}

				return Json(new { message = "Added Check" });
			}
			catch (Exception err)
			{
				_cache.LogError(err, User.Identity.Name + "(" + Request.UserHostAddress + ")");
				Response.StatusCode = 500;
				return Json(new { message = err.Message });
			}

		}
		
		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		public JsonResult GetMaintenanceProfile()
		{
			var maintenanceProfiles = (from x in _db.as_maintenanceProfile
									   join y in _db.as_maintenanceValidation on x.i_maintenanceValidationId equals y.i_maintenanceValidationId
									   select new
									   {
										   x.i_maintenanceId, x.vc_description, x.i_maintenanceCategoryId, x.i_maintenanceValidationId, y.vc_validationName,
										   vc_validationDescr = y.vc_validationDescription
									   }).ToList();

			return Json(maintenanceProfiles);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------

		[HttpPost]
		public JsonResult GetAssetHistory(int assetId)
		{
			try
			{
				//This will send back a list of actions on asset
				//Create Date: 2015/02/06
				//Author: Bernard Willer

				List<assetHistory> allHistory = new List<assetHistory>();
				allHistory.AddRange(ValidationTasks(assetId));
				allHistory.AddRange(TorqueTasks(assetId));
				allHistory.AddRange(VisualSurveys(assetId));

				if (allHistory.Count == 1)
					if (allHistory[0].maintenance == null)
						allHistory.Clear();

				return Json(allHistory);
			}
			catch (Exception err)
			{
				_cache.LogError(err, User.Identity.Name + "(" + Request.UserHostAddress + ")");
				Response.StatusCode = 500;
				return Json(new { message = err.Message });
			}
		
		}

		[HttpPost]
		public JsonResult UploadImage(iOSImageUpload imageUpload)
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
				MemoryStream ms = new MemoryStream(bytes) {Position = 0};

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
				_cache.LogError(err, User.Identity.Name + "(" + Request.UserHostAddress + ")");
				Response.StatusCode = 500;
				return Json(new { message = err.Message });
			}
		
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		public JsonResult GetGroupsTechnicians(int groupId)
		{
			var technicians = from x in _db.UserProfiles
							  join y in _db.as_technicianGroupProfile on x.UserId equals y.UserId
							  join z in _db.as_technicianGroups on y.i_currentGroup equals z.i_groupId
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
		public JsonResult CheckVersion()
		{
			var iOsVersion = (from data in _db.as_settingsProfile
							  where data.vc_settingDescription == "iOSVersion"
							  select data.vc_settingValue).First();
			return Json(new { CurrentVersion = iOsVersion });
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		public async Task<JsonResult> InsertAssetValidation(List<ios_validationTaskProfile> validations)
		{
			
			DateTime now = DateTime.Now;

			foreach (var item in validations)
			{
				var validation = new as_validationTaskProfile
				{
					bt_validated = item.bt_validated,
					i_assetId = item.i_assetId,
					i_shiftId = item.i_shiftId,
					UserId = item.UserId,
					dt_dateTimeStamp =
						item.dt_dateTimeStamp != null
							? DateTime.ParseExact(item.dt_dateTimeStamp, "yyyyMMdd HHmmss", CultureInfo.InvariantCulture)
							: now
				};

				var log = "Start";

				try
				{
					log = "validationTaskProfile";
					_db.as_validationTaskProfile.Add(validation);
					_db.SaveChanges();

					var status = await _db.as_assetStatusProfile.Where(q=> q.i_assetProfileId == item.i_assetId).OrderByDescending(q=>q.dt_lastUpdated).FirstOrDefaultAsync();
					if(status != null)
					{
						log = "Assset Status";
						status.bt_assetStatus = false;
						_db.Entry(status).State = EntityState.Modified;
						_db.SaveChanges();

					}

					//rebuild cache for asset
					log = "Rebuild Cache";
					await _cache.RebuildAssetProfileForAsset(item.i_assetId);
				}

				catch (Exception err)
				{
					string tmp = log + "|" + validation.dt_dateTimeStamp + "|" + validation.i_assetId + "|" + validation.UserId + "|" +
								 validation.bt_validated
								 + "|" + validation.i_shiftId + "|" + validation.i_validationProfileId;
					  

					_cache.Log("Failed to insert validation values: " + err.Message + "|***" + tmp,
						"insertAssetValidation(iOS)", CacheHelper.LogTypes.Error, "iOS Device");
					Response.StatusCode = 500;
					return Json(err.Message);
				}

			}
			return Json(new { status = "success", count = validations.Count});
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		public JsonResult GetTechnicianShifts(int userId, int closeShifts)
		{
			try
			{
				bool closeShiftsFlag = closeShifts == 1;
				var shifts = (from x in _db.as_shifts
							  join y in _db.as_areaSubProfile on x.i_areaSubId equals y.i_areaSubId
							  join z in _db.as_areaProfile on y.i_areaId equals z.i_areaId
							  join a in _db.as_technicianGroups on x.i_technicianGroup equals a.i_groupId
							  join b in _db.as_maintenanceProfile on x.i_maintenanceId equals b.i_maintenanceId
							  where x.i_technicianGroup == userId && x.bt_completed == closeShiftsFlag && x.bt_custom == false
							  select new
							  {
								  x.i_shiftId,
								  sheduledDate = x.dt_scheduledDate, x.i_areaSubId,
								  sheduleTime = x.dt_scheduledDate,
								  permitNumber = x.vc_permitNumber,
								  techGroup = a.vc_groupName,
								  areaName = z.vc_description,
								  validation = b.i_maintenanceValidationId,
								  maintenanceId = x.i_maintenanceId,
							  }
							  ).ToList();
				List<technicianShift> shiftList = shifts.Select(item => new technicianShift
				{
					i_shiftId = item.i_shiftId, sheduledDate = item.sheduledDate.ToString("yyy/MM/dd"), i_areaSubId = item.i_areaSubId, sheduleTime = item.sheduleTime.ToString("hh:mm:ss"), permitNumber = item.permitNumber, techGroup = item.techGroup, areaName = item.areaName, validation = item.validation == 1 ? "YES" : "NO", shiftType = 0, assets = new int[0], maintenanceId = item.maintenanceId
				}).ToList();

				//Add custom shifts to array
				var customShifts = (from x in _db.as_shifts
								   join y in _db.as_technicianGroups on x.i_technicianGroup equals y.i_groupId
								   join z in _db.as_maintenanceProfile on x.i_maintenanceId equals z.i_maintenanceId
								   where y.i_groupId == userId && x.bt_completed == closeShiftsFlag && x.bt_custom
								   select new
								   {
									   x.i_shiftId,
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
					technicianShift shift = new technicianShift
					{
						i_shiftId = item.i_shiftId,
						sheduledDate = item.sheduledDate.ToString("yyy/MM/dd"),
						i_areaSubId = item.i_areaSubId,
						sheduleTime = item.sheduleTime.ToString("hh:mm:ss"),
						permitNumber = item.permitNumber,
						techGroup = item.techGroup,
						areaName = item.areaName,
						validation = item.validation == 1 ? "YES" : "NO",
						shiftType = 1,
						maintenanceId = item.maintenanceId
					};

					var assets = (from x in _db.as_shiftsCustomProfile
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
				_cache.Log("Failed to retrieve technician shifts: " + err.Message, "getTechnicianShifts(iOS)", CacheHelper.LogTypes.Error, "iOS Device");
				Response.StatusCode = 500;
				return Json(err.Message);
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		public async Task<JsonResult> InsertShiftData(List<shiftDataUpload> shiftData)
		{
			try
			{
				List<int> assets = new List<int>();
				foreach (shiftDataUpload shift in shiftData)
				{
					as_shiftData newData = new as_shiftData()
					{
						dt_captureDate =
							shift.vc_dateStamp != null
								? DateTime.ParseExact(shift.vc_dateStamp, "yyyyMMdd HHmmss", CultureInfo.InvariantCulture)
								: DateTime.Now,
						f_capturedValue = shift.f_capturedValue,
						i_assetCheckId = shift.i_assetCheckId,
						i_assetId = shift.i_assetId,
						i_maintenanceId = shift.i_maintenanceId,
						i_shiftId = shift.i_shiftId
					};

					//var data = _db.as_shiftData.FirstOrDefault(q => q.i_shiftId == shift.i_shiftId && q.i_assetId == shift.i_assetId);

					//if (data != null)
					//{
					//    _db.as_shiftData.Remove(data);
					//    _db.SaveChanges();
					//}

					_db.as_shiftData.Add(newData);
					_db.SaveChanges();

					try
					{
						await _cache.RebuildAssetProfileForAsset(shift.i_assetId);
					}
					catch (Exception err)
					{
						_cache.Log("Failed to update cache for asset " + shift.i_assetId.ToString() + " - " + err.InnerException.Message, "insertShiftData", CacheHelper.LogTypes.Error, "SYSTEM");
					}

					assets.Add(shift.i_assetId);
				}

				return Json(new { Status = 1, Assets =  assets.Select(o => o).Distinct().ToList() });
			}
			catch (Exception err)
			{
				Response.StatusCode = 500;
				return Json(new { Status = 0,  Error = err.Message});
			}
		}

		#endregion

		#region Assets

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		public JsonResult UpdateAssetStatusBulk(List<AssetStatusUpload> assetList)
		{
			try
			{
				foreach (var item in assetList)
				{
					var status = item.assetStatus != "False";

					_db.as_assetStatusProfile.Add(new as_assetStatusProfile
					{
					    dt_lastUpdated = DateTime.Now,
                        bt_assetStatus = status,
                        i_assetSeverity = item.assetSeverity,
                        i_assetProfileId = item.assetId
					});

					_db.SaveChanges();
					
				}

				return Json(new { assetCount = assetList.Count });
			}
			catch (Exception err)
			{
				Response.StatusCode = 500;
				_cache.Log("Failed to set asset status: " + err.Message, "updateAssetStatusBulk", CacheHelper.LogTypes.Error, "iOS");
				return Json("Failed");
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		public async Task<JsonResult> SetAssetStatus(int assetId, string assetStatus, int assetSeverity)
		{
			try
			{
				var status = assetStatus != "False";

			    _db.as_assetStatusProfile.Add(new as_assetStatusProfile
			    {
			        dt_lastUpdated = DateTime.Now,
                    i_assetProfileId = assetId,
                    i_assetSeverity = assetSeverity,
                    bt_assetStatus = status
			    });
				_db.SaveChanges();

				await _cache.RebuildAssetProfileForAsset(assetId);

				var returnType = new AssetStatus
				{
					i_assetProfileId = assetId,
					bt_assetStatus = status
				};

				return Json(returnType);

			}
			catch (Exception err)
			{

				_cache.Log("Failed to set asset status: " + err.Message, "setAssetStatus", CacheHelper.LogTypes.Error, "iOS");
				return Json("Failed");
			}

		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		public async Task<JsonResult> GetAllAssets()
		{
			try
			{
				List<mongoFullAsset> assetList = await _cache.GetAllAssetDownload();
				var jsonResult = Json(assetList);
				jsonResult.MaxJsonLength = int.MaxValue;
				return jsonResult;
			}
			catch (Exception err)
			{
				_cache.Log("Failed to retrieve all asset download: " + err.Message, "getAllAssets(iOS)", CacheHelper.LogTypes.Error, "iOS Device");
				Response.StatusCode = 500;
				return Json(err.Message);
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		public async Task<JsonResult> GetAllAssetClasses()
		{
			try
			{
				List<mongoAssetClassDownload> assetClassList = await _cache.GetAllAssetClasses();
				return Json(assetClassList);
			}
			catch (Exception err)
			{
				_cache.Log("Failed to retrieve all asset classes: " + err.Message, "getAllAssetClasses(iOS)", CacheHelper.LogTypes.Error, "iOS Device");
				Response.StatusCode = 500;
				return Json(err.Message);
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		public async Task<JsonResult> GetAssetPerTagId(string tagId)
		{
			try
			{
				List<AssetTagReply> assetList = new List<AssetTagReply>();

				var assetInfo = from x in _db.as_assetProfile
								join y in _db.as_locationProfile on x.i_locationId equals y.i_locationId
								join z in _db.as_areaSubProfile on y.i_areaSubId equals z.i_areaSubId
								join a in _db.as_assetClassProfile on x.i_assetClassId equals a.i_assetClassId
								where x.vc_rfidTag == tagId
								select new
								{
									x.i_assetId, x.vc_serialNumber, y.f_latitude, y.f_longitude, z.i_areaSubId, a.i_assetClassId
								};

				foreach (var item in assetInfo)
				{
					AssetTagReply asset = new AssetTagReply
					{
						assetId = item.i_assetId,
						serialNumber = item.vc_serialNumber,
						firstMaintainedDate = _func.GetFirstMaintanedDate(item.i_assetId).ToString("yyyMMdd"),
						lastMaintainedDate = await _cache.GetAssetPreviousDateForFirstTask(item.i_assetId),
						nextMaintenanceDate = await _cache.GetAssetNextDateForFirstTask(item.i_assetId),
						latitude = item.f_latitude,
						longitude = item.f_longitude,
						subAreaId = item.i_areaSubId,
						assetClassId = item.i_assetClassId
					};
					assetList.Add(asset);
				}
				return Json(assetList);
			}
			catch (Exception err)
			{
				_cache.Log("Failed to retrieve asset per tagId: " + err.Message, "getAssetPerTagId(iOS)", CacheHelper.LogTypes.Error, "iOS Device");
				Response.StatusCode = 500;
				return Json(err.Message);
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		public async Task<JsonResult> GetAssetAssosiations(int areaSubId)
		{
			try
			{
				List<mongoAssetDownload> assetListMongo = await _cache.GetAssetAssosiations(areaSubId);

				if (assetListMongo.Count == 0)
				{
					List<AssetDownload> assetList = new List<AssetDownload>();

					var assets = from x in _db.as_assetProfile
								 join y in _db.as_locationProfile on x.i_locationId equals y.i_locationId
								 where y.i_areaSubId == areaSubId
								 select new
								 {
									 x.i_assetId, x.i_assetClassId, x.vc_rfidTag, x.vc_serialNumber, x.i_locationId, y.i_areaSubId, y.f_longitude, y.f_latitude
								 };

					foreach (var item in assets)
					{
						AssetDownload asset = new AssetDownload
						{
							i_assetId = item.i_assetId,
							i_assetClassId = item.i_assetClassId,
							vc_tagId = item.vc_rfidTag,
							vc_serialNumber = item.vc_serialNumber,
							i_locationId = item.i_locationId,
							i_areaSubId = item.i_areaSubId,
							longitude = item.f_longitude,
							latitude = item.f_latitude,
							lastDate = _func.GetLastShiftDateForAsset(item.i_assetId),
							maintenance = "0",
							submitted = _func.GetSubmittedShiftData(item.i_assetId)
						};
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
				_cache.Log("Failed to retrieve asset assosiations: " + err.Message, "getAssetAssosiations(iOS)", CacheHelper.LogTypes.Error, "iOS Device");
				Response.StatusCode = 500;
				return Json(err.Message);
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		public JsonResult GetAssetWithTagId(string tagId)
		{
			try
			{
				List<AssetDownload> assetList = new List<AssetDownload>();

				var assets = from x in _db.as_assetProfile
							 join y in _db.as_locationProfile on x.i_locationId equals y.i_locationId
							 where x.vc_rfidTag == tagId
							 select new
							 {
								 x.i_assetId, x.i_assetClassId, x.vc_rfidTag, x.vc_serialNumber, x.i_locationId, y.i_areaSubId, y.f_longitude, y.f_latitude
							 };

				foreach (var item in assets)
				{
					AssetDownload asset = new AssetDownload
					{
						i_assetId = item.i_assetId,
						i_assetClassId = item.i_assetClassId,
						vc_tagId = item.vc_rfidTag,
						vc_serialNumber = item.vc_serialNumber,
						i_locationId = item.i_locationId,
						i_areaSubId = item.i_areaSubId,
						longitude = item.f_longitude,
						latitude = item.f_latitude,
						lastDate = _func.GetLastShiftDateForAsset(item.i_assetId),
						maintenance = "---"
					};
					assetList.Add(asset);
				}
				return Json(assetList);
			}
			catch (Exception err)
			{
				_cache.Log("Failed to retrieve asset with tagid: " + err.Message, "getAssetWithTagId(iOS)", CacheHelper.LogTypes.Error, "iOS Device");
				Response.StatusCode = 500;
				return Json(err.Message);
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		public async Task<JsonResult> UpdateAssetTagBulk(List<AssetAssosiationUpload> assetList)
		{
			try
			{
				int i = 0;
				foreach (AssetAssosiationUpload item in assetList)
				{
					as_assetProfile asset = _db.as_assetProfile.Find(item.assetId);

					if (asset != null)
					{
						string currentTagId = asset.vc_rfidTag;
						asset.vc_rfidTag = item.tagId;
						asset.vc_serialNumber = item.serialNumber;
						_db.Entry(asset).State = System.Data.Entity.EntityState.Modified;
						_db.SaveChanges();

						//Log the change
						_cache.Log("Tag id was updated from " + currentTagId + " to " + item.tagId + " (bulk update on asset id: " + item.assetId + ")", "updateAssetTag(iOS)", CacheHelper.LogTypes.Info, "iOS Device");
						i++;

						try
						{
							await _cache.RebuildAssetProfileForAsset(item.assetId);
							await _cache.CreateAllAssetDownloadForAsset(asset.i_assetId);
						}
						catch (Exception err)
						{
							_cache.Log("Failed to update cache for asset " + item.assetId.ToString() + " - " + err.InnerException.Message, "insertShiftData", CacheHelper.LogTypes.Error, "SYSTEM");
						}
					}
					else
					{
						_cache.Log("Reference tag for assetid " + item.assetId.ToString() + " wasn't found.", "updateAssetTag(iOS)", CacheHelper.LogTypes.Info, "iOS Device");
					}
				}

				return Json(new { assetCount = i.ToString() });
			}
			catch (Exception err)
			{
				_cache.Log("Failed to update asset tag: " + err.Message, "updateAssetTagBulk(iOS)", CacheHelper.LogTypes.Error, "iOS Device");
				Response.StatusCode = 500;
				return Json(err.Message);
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		public async Task<JsonResult> UpdateAssetTag(int assetId, int userId, string tagId, string serialNumber)
		{
			try
			{

				as_assetProfile asset = _db.as_assetProfile.Find(assetId);

				if (asset != null)
				{
					string currentTagId = asset.vc_rfidTag;
					asset.vc_rfidTag = tagId;
					asset.vc_serialNumber = serialNumber;
					_db.Entry(asset).State = System.Data.Entity.EntityState.Modified;
					_db.SaveChanges();

					//Log the change
					_cache.Log("Tag id was updated from " + currentTagId + " to " + tagId + " (single update on asset id: " + asset.i_assetId + ")", "updateAssetTag(iOS)", CacheHelper.LogTypes.Info, userId.ToString());

					try
					{
						await _cache.RebuildAssetProfileForAsset(asset.i_assetId);
						await _cache.CreateAllAssetDownloadForAsset(asset.i_assetId);
					}
					catch (Exception err)
					{
						_cache.Log("Failed to update cache for asset " + asset.i_assetId.ToString() + " - " + err.InnerException.Message, "insertShiftData", CacheHelper.LogTypes.Error, "SYSTEM");
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
				_cache.Log("Failed to update asset tag: " + err.Message, "updateAssetTag(iOS)", CacheHelper.LogTypes.Error, "iOS Device");
				Response.StatusCode = 500;
				return Json(err.Message);
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		public async Task<JsonResult> InsertAssetAssosiation(List<NewAssetAssosiation> assets)
		{
			try
			{
				foreach (NewAssetAssosiation asset in assets)
				{
					as_locationProfile location = new as_locationProfile
					{
						f_latitude = asset.latitude,
						f_longitude = asset.longitude,
						i_areaSubId = asset.i_areaSubId,
						vc_designation = "---"
					};

					_db.as_locationProfile.Add(location);
					_db.SaveChanges();

					as_assetProfile newAsset = new as_assetProfile
					{
						i_assetClassId = asset.i_assetClassId,
						i_locationId = location.i_locationId,
						vc_rfidTag = asset.vc_rfidTag,
						vc_serialNumber = asset.vc_serialNumber,
						dt_initDate = DateTime.Now
					};

					_db.as_assetProfile.Add(newAsset);
					_db.SaveChanges();

					try
					{
						await _cache.RebuildAssetProfileForAsset(newAsset.i_assetId);
					}
					catch (Exception err)
					{
						_cache.Log("Failed to update cache for asset " + newAsset.i_assetId.ToString() + " - " + err.InnerException.Message, "insertShiftData", CacheHelper.LogTypes.Error, "SYSTEM");
					}
				}

				//2014/10/28 - Changed "assets" to "success" after iPad failure
				return Json(new { success = assets.Count });
			}
			catch (Exception err)
			{
				_cache.Log("Failed to insert asset assosiation: " + err.Message + "|Inner: |" + err.InnerException.Message, "insertAssetAssosiation(iOS)", CacheHelper.LogTypes.Error, "iOS Device");
				Response.StatusCode = 500;
				return Json("[{error:" + err.Message + "}]");
			}
		}

		#endregion

		#region Areas

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		public JsonResult GetMapCenter()
		{
			string longitude = _db.as_settingsProfile.Where(q => q.vc_settingDescription == "Longitude").Select(q => q.vc_settingValue).FirstOrDefault();
			string latitude = _db.as_settingsProfile.Where(q => q.vc_settingDescription == "Latitude").Select(q => q.vc_settingValue).FirstOrDefault();

			mapCenter center = new mapCenter();
			if (latitude != null) center.latitude = double.Parse(latitude);
			if (longitude != null) center.longitude = double.Parse(longitude);

			return Json(center);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		public JsonResult GetMainAreas()
		{
			try
			{
				var areas = _db.as_areaProfile;
				return Json(areas.ToList());
			}
			catch (Exception err)
			{
				_cache.Log("Failed to get main areas: " + err.Message, "getMainAreas(iOS)", CacheHelper.LogTypes.Error, "iOS Device");
				Response.StatusCode = 500;
				return Json("error: " + err.Message);
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		public JsonResult GetSubAreas()
		{
			try
			{
				var subAreas = _db.as_areaSubProfile;
				return Json(subAreas.ToList());
			}
			catch (Exception err)
			{
				_cache.Log("Failed to get sub areas: " + err.Message, "getSubAreas(iOS)", CacheHelper.LogTypes.Error, "iOS Device");
				Response.StatusCode = 500;
				return Json("error: " + err.Message);
			}
		}

		#endregion

		#region Wrench Info

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		public JsonResult GetWrenches()
		{
			var wrenchList = (from data in _db.as_wrenchProfile select data).ToList();

			List<iOSwrench> iosWrenchList = wrenchList.Select(wrench => new iOSwrench
			{
				bt_active = wrench.bt_active, dt_lastCalibrated = wrench.dt_lastCalibrated.ToString("yyyyMMdd"), f_batteryLevel = wrench.f_batteryLevel, i_calibrationCycle = wrench.i_calibrationCycle, i_wrenchId = wrench.i_wrenchId, vc_model = wrench.vc_model, vc_serialNumber = wrench.vc_serialNumber
			}).ToList();
			return Json(iosWrenchList);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		public JsonResult UpdateBatteryLevel(List<WrenchBatteryUpdate> batteryUpdate)
		{
			try
			{
				foreach (WrenchBatteryUpdate battery in batteryUpdate)
				{
					as_wrenchProfile updateWrench = _db.as_wrenchProfile.Find(battery.wrenchId);
					updateWrench.f_batteryLevel = battery.batteryLevel;
					_db.Entry(updateWrench).State = System.Data.Entity.EntityState.Modified;
					_db.SaveChanges();
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
		public JsonResult InsertWrenchAssosiation(List<WrenchAssosiation> assosiations)
		{
			try
			{
				foreach (WrenchAssosiation assosiation in assosiations)
				{
					as_technicianWrenchProfile assosiationInsert = new as_technicianWrenchProfile
					{
						dt_dateTime = DateTime.Now,
						i_wrenchId = assosiation.wrenchId,
						UserId = assosiation.UserId
					};
					_db.as_technicianWrenchProfile.Add(assosiationInsert);
					_db.SaveChanges();
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
					if (fileName != null)
					{
						var path = Path.Combine(Server.MapPath("~/images/uploads"), fileName);
						file.SaveAs(path);

						if (IsImage(file))
						{
							try
							{
								SaveThumbNails(path);
							}
							catch (Exception err)
							{

								_cache.Log("Failed to upload File (Image Check): " + err.Message + " | " + err.InnerException.Message, "UploadFile", CacheHelper.LogTypes.Error, "iOS");
							}
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
					}


					as_fileUploadProfile dbFile = new as_fileUploadProfile
					{
						guid_file = guid,
						vc_filePath = "../../images/uploads/" + fileName,
						vc_fileDescription = fileName,
						i_fileType = fileType,
						dt_datetime = DateTime.Now
					};

					_db.as_fileUploadProfile.Add(dbFile);
					_db.SaveChanges();
				}

				return guid.ToString();
			}
			catch (Exception err)
			{
				_cache.Log("Failed to upload File: " + err.Message + " | " + err.InnerException.Message, "UploadFile", CacheHelper.LogTypes.Error, "iOS");
				Response.StatusCode = 500;
				return err.Message;
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		private void SaveThumbNails(string file)
		{
			// Get a bitmap.
			Bitmap bmp1 = new Bitmap(file);
			ImageCodecInfo jgpEncoder = GetEncoder(ImageFormat.Jpeg);

			var myEncoder =
				Encoder.Quality;

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

			return codecs.FirstOrDefault(codec => codec.FormatID == format.Guid);
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		[HttpPost]
		public string UpdateFileInfo(List<fileUpload> info)
		{
			try
			{
				string lastUpdated = "";
				foreach (fileUpload item in info)
				{
					as_fileUploadInfo file = new as_fileUploadInfo
					{
						guid_file = Guid.Parse(item.file_guid),
						vc_description = item.description,
						f_latitude = item.latitude,
						f_longitude = item.longitude,
						i_shiftId = item.shiftId,
						i_userId_logged = item.userId,
						i_severityId = item.severity,
						i_userId_resolved = 0,
						dt_dateTimeResolved = DateTime.Parse("2300/01/01")
					};

					_db.as_fileUploadInfo.Add(file);
					_db.SaveChanges();
					lastUpdated = item.file_guid;
				}

				return lastUpdated;
			}
			catch (Exception err)
			{
				_cache.Log("Failed to upload File Info: " + err.Message + " | " + err.InnerException.Message, "updateFileInfo", CacheHelper.LogTypes.Error, "iOS");
				Response.StatusCode = 500;
				return err.Message;
			}
		}

	    [HttpPost]
	    public async Task<JsonResult> UploadSingleFile(singleFileUpload fileInfo)
	    {
	        try
	        {
	            var fileGuid = Guid.NewGuid();
	            if (fileInfo.file.ContentLength > 0)
	            {
	                var fileName = Path.GetFileName(fileInfo.file.FileName);
	                if (fileName != null)
	                {
	                    var path = Path.Combine(Server.MapPath("~/images/uploads"), fileName);
                        fileInfo.file.SaveAs(path);

	                    if (IsImage(fileInfo.file))
	                    {
	                        try
	                        {
	                            SaveThumbNails(path);
	                        }
	                        catch (Exception err)
	                        {
	                            _cache.Log(
	                                "Failed to upload File (Image Check): " + err.Message + " | " +
	                                err.InnerException.Message, "UploadFile", CacheHelper.LogTypes.Error, "iOS");
	                        }
	                    }
	                }

	                var fileSplit = fileInfo.file.FileName.Split(char.Parse("."));
	                var extension = fileSplit[1];
	                var fileType = 0;

	                switch (extension)
	                {
	                    case "jpg":
	                        fileType = 1;
	                        break;
	                    case "m4a":
	                        fileType = 2;
	                        break;
	                    case "text":
	                        fileType = 3;
	                        break;
	                }

	                _db.as_fileUploadProfile.Add(
	                    new as_fileUploadProfile
	                    {
	                        guid_file = fileGuid,
	                        vc_filePath = "../../images/uploads/" + fileName,
	                        vc_fileDescription = fileName,
	                        i_fileType = fileType,
	                        dt_datetime = DateTime.Now
	                    }
	                    );

	                await _db.SaveChangesAsync();
	            }

	            _db.as_fileUploadInfo.Add(
	                new as_fileUploadInfo
	                {
	                    guid_file = fileGuid,
	                    vc_description = fileInfo.description,
	                    f_latitude = fileInfo.latitude,
	                    f_longitude = fileInfo.longitude,
	                    i_shiftId = fileInfo.shiftId,
	                    i_userId_logged = fileInfo.userId,
	                    i_severityId = fileInfo.severity,
	                    i_userId_resolved = 0,
	                    dt_dateTimeResolved = DateTime.Parse("2300/01/01")
	                }
	                );
	            await _db.SaveChangesAsync();

	            return Json(new {success = true, fileInfo.token});
	        }
	        catch (Exception err)
	        {
                _cache.Log("Failed to upload File: " + err.Message + " | " + err.InnerException.Message, "UploadSingleFile", CacheHelper.LogTypes.Error, "iOS");
                Response.StatusCode = 500;
	            return Json(new {success = false});
	        }
	    }

		#endregion

		#region Cache and Views

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		public ActionResult RebuildCache()
		{
			return View();
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------

		[HttpPost]
		public async Task<JsonResult> RebuildAssetFull()
		{
			try
			{
				await _cache.CreateAllAssetDownload();
				return Json(new { status = "success" });
			}
			catch (Exception err)
			{
				return Json(new {status = "failed", error = err.Message });
			}
		}


		//-----------------------------------------------------------------------------------------------------------------------------------------------------------

		[HttpPost]
		public async Task<JsonResult> RebuildAssetDownload()
		{
			try
			{
				Boolean flag = await _cache.CreateAssetDownloadCache();
				if(flag)
					return Json(new { status = "success" });
				else
				{
					return Json(new {status = "Failed at procedure"});
				}
			}
			catch (Exception)
			{
				return Json(new { status = "failed" });
			}
		}


		//-----------------------------------------------------------------------------------------------------------------------------------------------------------

		[HttpPost]
		public async Task<JsonResult> RebuildAssetClass()
		{
			try
			{
				await _cache.CreateAssetClassDownloadCache();
				return Json(new { status = "success" });
			}
			catch (Exception)
			{
				return Json(new { status = "failed" });
			}
		}


		//-----------------------------------------------------------------------------------------------------------------------------------------------------------

		public string UploadLogFile(HttpPostedFileBase file, int userId, string uid)
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
				as_iosLogProfile log = new as_iosLogProfile
				{
					dt_logCaptureDate = DateTime.Now,
					UserId = userId,
					vc_deviceUID = uid,
					vc_logContainer = container.Name,
					vc_logName = fileName + ".log"
				};

				_db.as_iosLogProfile.Add(log);
				_db.SaveChanges();

				return "Success";
			}
			catch (Exception err)
			{
				_cache.LogError(err, "iOS");
				Response.StatusCode = 500;
				return err.Message;
			}
		
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------
		
		public JsonResult GetCacheStatus()
		{
			
			try
			{
				//Sends the current hashes of the different download sets
				//Create Date: 2015/01/27   
				//Author: Bernard Willer

				var cache = _db.as_cacheProfile.Where(q => q.bt_active).ToList();
				return Json(cache);
			}
			catch (Exception err)
			{
				_cache.LogError(err, "iOS");
				Response.StatusCode = 500;
				return Json(new { message = err.Message });
			}
		
		}

		#endregion

		#region Web Views

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------

		public ActionResult IosImages()
		{
			//Send List of Images
			var allFiles = _db.as_iosImageProfile.Where(q => q.bt_active).OrderByDescending(q => q.dt_dateTimeStamp).ToList();
			ViewData["iosImages"] = allFiles;
			return View();
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------

		public ActionResult UploadiOsFile(HttpPostedFileBase file, string description, string version, string releaseNotes)
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

				as_iosImageProfile image = new as_iosImageProfile
				{
					dt_dateTimeStamp = DateTime.Now,
					bt_active = true,
					vc_description = description,
					vc_fileName = filename,
					vc_version = version,
					vc_releaseNotes = releaseNotes
				};

				_db.as_iosImageProfile.Add(image);
				_db.SaveChanges();

				Response.StatusCode = 200;
				return Json(new { message = "success" });
			}
			catch (Exception err)
			{
				_cache.LogError(err, User.Identity.Name + "(" + Request.UserHostAddress + ")");
				Response.StatusCode = 500;
				return Json(new { message = err.Message });
			}

		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------

		public ActionResult DownloadFile(int id)
		{
			try
			{
				//This function allows the user to download a file from Azure Storage
				//Create Date: 2015/01/27
				//Author: Bernard Willer

				as_iosImageProfile image = _db.as_iosImageProfile.Find(id);

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
				_cache.LogError(err, User.Identity.Name + "(" + Request.UserHostAddress + ")");
				Response.StatusCode = 500;
				return Json(new { message = err.Message });
			}
		
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------

		#endregion

		#region Helpers

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------

		private List<assetHistory> ValidationTasks(int assetId)
		{
			var validation = from x in _db.as_validationTaskProfile
							 join y in _db.UserProfiles on x.UserId equals y.UserId
							 join a in _db.as_shifts on x.i_shiftId equals a.i_shiftId
							 join z in _db.as_maintenanceProfile on a.i_maintenanceId equals z.i_maintenanceId
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
				assetHistory task = new assetHistory
				{
					type = 2,
					maintenance = item.maintenanceTask + "(" + item.user + ")",
					valueCaptured = item.user + " performed a " + item.maintenanceTask + " task",
					datetimeStamp = item.date.ToString("dd MMM, yyyy")
				};
				tasks.Add(task);
			}

			return tasks;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------

		private List<assetHistory> TorqueTasks(int assetId)
		{
			var torque = (from x in _db.as_shiftData
						  join y in _db.as_shifts on x.i_shiftId equals y.i_shiftId
						  join z in _db.as_technicianGroups on y.i_technicianGroup equals z.i_groupId
						  join a in _db.as_maintenanceProfile on y.i_maintenanceId equals a.i_maintenanceId
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

					task = new assetHistory
					{
						maintenance = "Fitting was torqued (" + item.name + ")",
						datetimeStamp = item.date.ToString("dd MMM, yyyy"),
						type = 1
					};
					shiftId = item.shiftId;
					pointer = 0;
				}

				if(pointer != 0)
					task.valueCaptured += "," + item.value.ToString(CultureInfo.InvariantCulture);
				else
					task.valueCaptured = item.value.ToString(CultureInfo.InvariantCulture);

				pointer++;
			}

			tasks.Add(task);

			return tasks;
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------

		private List<assetHistory> VisualSurveys(int assetId)
		{
			List<assetHistory> items = new List<assetHistory>();

			try
			{
				var location = (from x in _db.as_assetProfile
								join y in _db.as_locationProfile on x.i_locationId equals y.i_locationId
								where x.i_assetId == assetId
								select new
								{
									longitude = y.f_longitude,
									latitude = y.f_latitude
								}).FirstOrDefault();

				if (location != null)
				{
					as_get_closest_point_to_gps_coordinate1_Result closest = _db.as_get_closest_point_to_gps_coordinate(location.latitude, location.longitude).FirstOrDefault();

					var surveys = (from x in _db.as_fileUploadInfo
						join y in _db.as_fileUploadProfile on x.guid_file equals y.guid_file
						join b in _db.UserProfiles on x.i_userId_logged equals b.UserId
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
						assetHistory asset = new assetHistory
						{
							datetimeStamp = item.date.ToString("dd MMM, yyyy"),
							type = 3
						};
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
				}

				return items;
			} catch(Exception ex)
			{
				_cache.LogError(ex, User.Identity.Name);
				return items;
			}
		}

		//-----------------------------------------------------------------------------------------------------------------------------------------------------------

		#endregion
	}

}