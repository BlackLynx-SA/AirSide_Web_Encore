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
// SUMMARY: This class contains all controller calls for all Mapping related calls
#endregion

using ADB.AirSide.Encore.V1.Models;
using AirSide.ServerModules.Helpers;
using AirSide.ServerModules.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace ADB.AirSide.Encore.V1.Controllers
{
    [Authorize]
    public class MapController : Controller
    {
        private readonly Entities db = new Entities();
        private readonly CacheHelper cache = new CacheHelper(ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString, ConfigurationManager.ConnectionStrings["MongoServer"].ConnectionString);

        public ActionResult AirportMap()
        {
            ViewBag.assetClasses = new SelectList(db.as_assetClassProfile.OrderBy(q => q.vc_description).Distinct(), "i_assetClassId", "vc_description");
            ViewBag.mainAreas = new SelectList(db.as_areaProfile.OrderBy(q => q.vc_description).Distinct(), "i_areaId", "vc_description");
            ViewBag.surveyorDates = new SelectList(db.as_fileUploadProfile.OrderByDescending(q => q.dt_datetime).ToList().Select(q => q.dt_datetime.ToString("yyy/MM/dd")).Distinct(), "dt_datetime");
            ViewBag.photmetricDates = new SelectList(db.as_fbTechProfile.OrderByDescending(q => q.dt_dateTimeStamp).ToList().Select(q => q.dt_dateTimeStamp.ToString("yyy/MM/dd")).Distinct(), "dt_dateTimeStamp");
            ViewBag.techgroups = new SelectList(db.as_technicianGroups, "i_groupId", "vc_groupName");
            var maintenanceTasks = db.as_maintenanceProfile.ToList();
            
            //Add Worst Case Category
            as_maintenanceProfile worstCase = new as_maintenanceProfile();
            worstCase.i_maintenanceCategoryId = 0;
            worstCase.i_maintenanceId = 0;
            worstCase.i_maintenanceValidationId = 0;
            worstCase.vc_description = "Worst Case";
            maintenanceTasks.Add(worstCase);

            ViewData["maintenanceTasks"] = maintenanceTasks.OrderBy(q=>q.i_maintenanceId).ToList();
            ViewBag.firstTask = 0;
            ViewBag.taskDesc = "Worst Case";

            return View();
        }

        [HttpPost]
        public JsonResult getMainAreas()
        {
            var areas = db.as_areaProfile.ToList();
            return Json(areas);
        }

        [HttpPost]
        public async Task<JsonResult> getAllAssets()
        {
            try
            {
                var assets = await cache.GetAllAssets();
                var jsonResult = Json(assets);
                jsonResult.MaxJsonLength = int.MaxValue;
                return jsonResult;
            }
            catch (Exception err)
            {
                cache.Log("Failed to retrieve assets: " + err.Message, "getAllAssets", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        [HttpPost]
        public async Task<JsonResult> UpdateFaultyLight(int assetId, bool flag)
        {
            try
            {
                var asset = db.as_assetStatusProfile.Find(assetId);

                if (asset != null)
                {
                    asset.bt_assetStatus = flag;
                    db.Entry(asset).State = EntityState.Modified;
                    db.SaveChanges();
                }
                else
                {
                    as_assetStatusProfile status = new as_assetStatusProfile()
                    {
                        bt_assetStatus = flag,
                        dt_lastUpdated = DateTime.Now,
                        i_assetProfileId = assetId,
                        i_assetSeverity = 0
                    };

                    db.as_assetStatusProfile.Add(status);
                    db.SaveChanges();
                }

                await cache.RebuildAssetProfileForAsset(assetId);

                return Json(new {status = "success"});
            }
            catch (Exception err)
            {
                cache.Log("Failed to update asset status: " + err.Message, "updateFaultyLight", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        [HttpPost]
        public JsonResult getAllSubAreas()
        {
            var areas = db.as_areaSubProfile.OrderBy(q => q.vc_description).Distinct().ToList();
            return Json(areas);
        }

        [HttpPost]
        public JsonResult getSubAreas(int areaId)
        {
            var areas = db.as_areaSubProfile.Where(q => q.i_areaId == areaId).OrderBy(q => q.vc_description).ToList();
            return Json(areas);
        }

        [HttpPost]
        public JsonResult getMapCenter()
        {
            string longitude = db.as_settingsProfile.Where(q => q.vc_settingDescription == "Longitude").Select(q => q.vc_settingValue).FirstOrDefault();
            string latitude = db.as_settingsProfile.Where(q => q.vc_settingDescription == "Latitude").Select(q => q.vc_settingValue).FirstOrDefault();

            List<decimal> coordinates = new List<decimal>();
            coordinates.Add(decimal.Parse(latitude, CultureInfo.InvariantCulture));
            coordinates.Add(decimal.Parse(longitude, CultureInfo.InvariantCulture));

            return Json(coordinates);
        }

        [HttpPost]
        public JsonResult getSurveydData(string dateOfSurvey)
        {
            //Disseminate the date range
            string[] dates = dateOfSurvey.Split(char.Parse("-"));
            DateTime startDate = DateTime.ParseExact(dates[0], "yyyy/MM/dd", CultureInfo.InvariantCulture);
            DateTime endDate = DateTime.ParseExact(dates[1], "yyyy/MM/dd", CultureInfo.InvariantCulture);

            var surveyed = (from x in db.as_fileUploadProfile
                            join y in db.as_fileUploadInfo on x.guid_file equals y.guid_file
                            join z in db.UserProfiles on y.i_userId_logged equals z.UserId
                            where x.dt_datetime >= startDate && x.dt_datetime <= endDate
                            select new
                            {
                                guid = x.guid_file,
                                path = x.vc_filePath,
                                desr = x.vc_fileDescription,
                                user = z.FirstName + " " + z.LastName,
                                date = x.dt_datetime,
                                longitude = y.f_longitude,
                                latitude = y.f_latitude
                            }).ToList();

            List<surveyedData> normalise = new List<surveyedData>();

            foreach (var item in surveyed)
            {
                surveyedData newItem = new surveyedData();
                newItem.description = item.desr;
                newItem.guid = item.guid.ToString();
                newItem.url = item.path;
                newItem.technician = item.user;
                newItem.date = item.date.ToString("yyyy/MM/dd hh:mm");
                newItem.latitude = item.latitude;
                newItem.longitude = item.longitude;

                string[] filepath = item.path.Split(char.Parse("."));
                int place = filepath.Count() - 1;

                newItem.type = filepath[place];

                normalise.Add(newItem);
            }

            return Json(normalise);
        }

        [HttpPost]
        public JsonResult getFBTechData(string dateForData)
        {
            var data = (from x in db.as_fbTechProfile
                        join y in db.as_assetProfile on x.i_assetId equals y.i_assetId
                        join z in db.as_locationProfile on y.i_locationId equals z.i_locationId
                        join a in db.as_pictureProfile on x.i_pictureId equals a.i_pictureId
                        select new
                        {
                            tagid = y.vc_rfidTag,
                            longitude = z.f_longitude,
                            latitude = z.f_latitude,
                            picture = a.vc_fileLocation,
                            pass = x.bt_pass,
                            avgcd = x.f_averageCandela,
                            maxcd = x.f_maxCandela,
                            pericao = x.f_persentageICAO,
                            vdegree = x.f_verticalDegree,
                            hdegree = x.f_horizontalDegree,
                            xaxis = x.f_xAxis,
                            yaxis = x.f_yAxis,
                            dateOfPhoto = x.dt_dateTimeStamp
                        }).ToList().Where(q => q.dateOfPhoto.ToString("yyy/MM/dd") == dateForData);

            return Json(data);
        }

        //2014/12/09 Decomissioned 
        //[HttpPost]
        //public JsonResult getAllAssetProfiles(int? subAreaId, int? areaId)
        //{
        //    List<AssetDownload> assetList = new List<AssetDownload>();
        //    try
        //    {
        //        CacheHelper cache = new CacheHelper();
        //        List<mongoAssetProfile> cacheList = cache.getAllAssets();
        //        if (subAreaId == null && areaId == null)
        //        {
        //            foreach (mongoAssetProfile item in cacheList)
        //            {
        //                AssetDownload asset = new AssetDownload();
        //                asset.i_assetId = item.assetId;
        //                asset.i_assetClassId = item.assetClassId;
        //                asset.vc_tagId = item.rfidTag;
        //                asset.vc_serialNumber = item.rfidTag;
        //                asset.i_locationId = item.locationId;
        //                asset.i_areaSubId = item.location.areaSubId;
        //                asset.longitude = item.location.longitude;
        //                asset.latitude = item.location.latitude;
        //                asset.lastDate = item.previousDate;
        //                asset.maintenance = "0";
        //                asset.assetDesc = item.assetClass.description;
        //                asset.imagePath = item.picture.fileLocation;
        //                asset.imageDesc = item.picture.description;
        //                asset.frequencyId = item.maintenanceCycle;
        //                assetList.Add(asset);
        //            }
        //        }
        //        else if (areaId != null)
        //        {
        //            foreach (mongoAssetProfile item in cacheList)
        //            {
        //                if (item.location.areaId == areaId)
        //                {
        //                    AssetDownload asset = new AssetDownload();
        //                    asset.i_assetId = item.assetId;
        //                    asset.i_assetClassId = item.assetClassId;
        //                    asset.vc_tagId = item.rfidTag;
        //                    asset.vc_serialNumber = item.rfidTag;
        //                    asset.i_locationId = item.locationId;
        //                    asset.i_areaSubId = item.location.areaSubId;
        //                    asset.longitude = item.location.longitude;
        //                    asset.latitude = item.location.latitude;
        //                    asset.lastDate = item.previousDate;
        //                    asset.maintenance = "0";
        //                    asset.assetDesc = item.assetClass.description;
        //                    asset.imagePath = item.picture.fileLocation;
        //                    asset.imageDesc = item.picture.description;
        //                    asset.frequencyId = item.maintenanceCycle;
        //                    assetList.Add(asset);
        //                }
        //            }
        //        }
        //        else
        //        {
        //            foreach (mongoAssetProfile item in cacheList)
        //            {
        //                if (item.location.areaSubId == subAreaId)
        //                {
        //                    AssetDownload asset = new AssetDownload();
        //                    asset.i_assetId = item.assetId;
        //                    asset.i_assetClassId = item.assetClassId;
        //                    asset.vc_tagId = item.rfidTag;
        //                    asset.vc_serialNumber = item.rfidTag;
        //                    asset.i_locationId = item.locationId;
        //                    asset.i_areaSubId = item.location.areaSubId;
        //                    asset.longitude = item.location.longitude;
        //                    asset.latitude = item.location.latitude;
        //                    asset.lastDate = item.previousDate;
        //                    asset.maintenance = "0";
        //                    asset.assetDesc = item.assetClass.description;
        //                    asset.imagePath = item.picture.fileLocation;
        //                    asset.imageDesc = item.picture.description;
        //                    asset.frequencyId = item.maintenanceCycle;
        //                    assetList.Add(asset);
        //                }
        //            }
        //        }

        //        return Json(assetList);
        //    }
        //    catch (Exception err)
        //    {
        //        LogHelper log = new LogHelper();
        //        log.log("Failed to get Asset Profiles: " + err.Message, "getAllAssetProfiles", LogHelper.logTypes.Error, User.Identity.Name);
        //        return Json(assetList);
        //    }
        //}
    }
}