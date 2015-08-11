
#define DEBUG

using AirSide.ServerModules.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AirSide.ServerModules.Helpers
{
    public class CacheHelper : IDisposable
    {
        //Mongo Globals
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        protected static IMongoClient _client;
        protected static IMongoDatabase _database;

        private readonly DatabaseHelper func;

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// This is the class initializer. It takes the database and connection string to the MongoDB as parameters.
        /// </summary>
        /// <param name="database">The MongoDB Database</param>
        /// <param name="connectionString">The MongoDB Connection String</param>
        public CacheHelper(string database, string connectionString)
        {
            _client = new MongoClient(connectionString);
            _database = _client.GetDatabase(database);
            func = new DatabaseHelper();
        }
       

        //SQL Entity Globals
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        private Entities db = new Entities();

        #region Cache Rebuild

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        public async Task<Boolean> createAssetClassDownloadCache()
        {
            try
            {
                List<assetClassDownload> assetList = new List<assetClassDownload>();

                var assets = (from x in db.as_assetClassProfile
                              select x);

                if (assets != null)
                {
                    mongoAssetClassDownload[] assetArray = new mongoAssetClassDownload[assets.Count()];
                    int i = 0;

                    foreach (var item in assets)
                    {
                        mongoAssetClassDownload asset = new mongoAssetClassDownload();
                        asset.i_assetClassId = item.i_assetClassId;
                        asset.vc_description = item.vc_description;
                        asset.i_assetCheckTypeId = 0;
                        asset.assetCheckCount = func.getNumberOfFixingPoints(item.i_assetClassId);
                        assetArray[i] = asset;
                        i++;
                    }

                    //Drop Existing
                    await _database.DropCollectionAsync("md_assetClassDownload");

                    //Recreate New
                    IMongoCollection<mongoAssetClassDownload> collection = _database.GetCollection<mongoAssetClassDownload>("md_assetClassDownload");
                    await collection.InsertManyAsync(assetArray);

                    return true;
                }
                else return false;
            }
            catch (Exception err)
            {
                log("Failed to create Asset Class Download Cache: " + err.Message, "createAssetClassDownloadCache(iOS)", logTypes.Error, "SYSTEM");
                return false;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        public async Task<Boolean> createAllAssetDownload()
        {
            try
            {
                var assets = (from x in db.as_assetProfile
                              join y in db.as_locationProfile on x.i_locationId equals y.i_locationId
                              select new
                              {
                                  assetId = x.i_assetId,
                                  serialNumber = x.vc_serialNumber,
                                  latitude = y.f_latitude,
                                  longitude = y.f_longitude,
                                  subAreaId = y.i_areaSubId,
                                  assetClassId = x.i_assetClassId,
                                  rfidTag = x.vc_rfidTag
                              });

                if (assets != null)
                {

                    mongoFullAsset[] assetArray = new mongoFullAsset[assets.Count()];
                    int i = 0;

                    foreach (var item in assets)
                    {
                        Boolean lightStatus = false;
                        var status = db.as_assetStatusProfile.Where(q => q.i_assetProfileId == item.assetId).FirstOrDefault();
                        if (status != null)
                            lightStatus = status.bt_assetStatus;

                        mongoFullAsset asset = new mongoFullAsset();
                        asset.assetId = item.assetId;
                        asset.serialNumber = item.serialNumber;
                        asset.firstMaintainedDate = func.getFirstMaintanedDate(item.assetId).ToString("yyyMMdd");
                        asset.lastMaintainedDate = await getAssetPreviousDateForFirstTask(item.assetId);
                        asset.nextMaintenanceDate = await getAssetNextDateForFirstTask(item.assetId);
                        asset.latitude = item.latitude;
                        asset.longitude = item.longitude;
                        asset.subAreaId = item.subAreaId;
                        asset.assetClassId = item.assetClassId;
                        asset.rfidTag = item.rfidTag;
                        asset.status = lightStatus;
                        assetArray[i] = asset;
                        i++;
                    }

                    //Drop Existing
                    await _database.DropCollectionAsync("md_assetFullDownload");

                    //Recreate New
                    IMongoCollection<mongoFullAsset> collection = _database.GetCollection<mongoFullAsset>("md_assetFullDownload");
                    await collection.InsertManyAsync(assetArray);

                    return true;
                }
                else return false;
            }
            catch (Exception err)
            {
                log("Failed to create Asset Full Download Cache: " + err.Message, "createAllAssetDownload(iOS)", logTypes.Error, "SYSTEM");
                return false;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        public async Task<Boolean> createAllAssetDownloadForAsset(int assetId)
        {
            try
            {
                var assets = (from x in db.as_assetProfile
                              join y in db.as_locationProfile on x.i_locationId equals y.i_locationId
                              where x.i_assetId == assetId
                              select new
                              {
                                  assetId = x.i_assetId,
                                  serialNumber = x.vc_serialNumber,
                                  latitude = y.f_latitude,
                                  longitude = y.f_longitude,
                                  subAreaId = y.i_areaSubId,
                                  assetClassId = x.i_assetClassId,
                                  rfidTag = x.vc_rfidTag
                              });

                if (assets != null)
                {

                    mongoFullAsset[] assetArray = new mongoFullAsset[assets.Count()];
                    int i = 0;

                    foreach (var item in assets)
                    {
                        Boolean lightStatus = false;
                        var status = db.as_assetStatusProfile.Where(q => q.i_assetProfileId == item.assetId).FirstOrDefault();
                        if (status != null)
                            lightStatus = status.bt_assetStatus;

                        mongoFullAsset asset = new mongoFullAsset();
                        asset.assetId = item.assetId;
                        asset.serialNumber = item.serialNumber;
                        asset.firstMaintainedDate = func.getFirstMaintanedDate(item.assetId).ToString("yyyMMdd");
                        asset.lastMaintainedDate = await getAssetPreviousDateForFirstTask(item.assetId);
                        asset.nextMaintenanceDate = await getAssetNextDateForFirstTask(item.assetId);
                        asset.latitude = item.latitude;
                        asset.longitude = item.longitude;
                        asset.subAreaId = item.subAreaId;
                        asset.assetClassId = item.assetClassId;
                        asset.rfidTag = item.rfidTag;
                        asset.status = lightStatus;
                        assetArray[i] = asset;
                        i++;
                    }

                    //Drop Current Profile
                    var filter = Builders<mongoFullAsset>.Filter.Eq(q => q.assetId, assetId);
                    IMongoCollection<mongoFullAsset> collection = _database.GetCollection<mongoFullAsset>("md_assetFullDownload");
                    await collection.DeleteOneAsync(filter);

                    //Insert Updated Asset
                    await collection.InsertManyAsync(assetArray);

                    return true;
                }
                else return false;
            }
            catch (Exception err)
            {
                log("Failed to create Asset Full Download Cache: " + err.Message, "createAllAssetDownload(iOS)", logTypes.Error, "SYSTEM");
                return false;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        public async Task<Boolean> createAssetDownloadCache()
        {
            try
            {
                

                var assets = from x in db.as_assetProfile
                             join y in db.as_locationProfile on x.i_locationId equals y.i_locationId
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

                if(assets != null)
                {
                    mongoAssetDownload[] assetList = new mongoAssetDownload[assets.Count()];
                    int i = 0;

                    foreach (var item in assets)
                    {
                        mongoAssetDownload asset = new mongoAssetDownload();
                        asset.i_assetId = item.i_assetId;
                        asset.i_assetClassId = item.i_assetClassId;
                        asset.vc_tagId = item.vc_rfidTag;
                        asset.vc_serialNumber = item.vc_serialNumber;
                        asset.i_locationId = item.i_locationId;
                        asset.longitude = item.f_longitude;
                        asset.latitude = item.f_latitude;
                        asset.lastDate = func.getLastShiftDateForAsset(item.i_assetId);
                        asset.maintenance = "0";
                        asset.submitted = func.getSubmittedShiftData(item.i_assetId);
                        assetList[i] = asset;
                        i++;
                    }

                    //Drop Existing
                    await _database.DropCollectionAsync("md_assetDownload");

                    //Recreate New
                    IMongoCollection<mongoAssetDownload> collection = _database.GetCollection<mongoAssetDownload>("md_assetDownload");
                    await collection.InsertManyAsync(assetList);

                    return true;
                } else 
                    return false;
            }
            catch (Exception err)
            {
                log("Failed to create Asset Download Cache: " + err.Message, "createAssetDownloadCache(iOS)", logTypes.Error, "SYSTEM");
                return false;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        //public async Task<bool> rebuildShiftAgregation()
        //{
        //    try
        //    {
               
        //        var shifts = (from x in db.as_shifts
        //                      join y in db.as_areaSubProfile on x.i_areaSubId equals y.i_areaSubId
        //                      join z in db.as_areaProfile on y.i_areaId equals z.i_areaId
        //                      join a in db.as_technicianGroups on x.i_technicianGroup equals a.i_groupId
        //                      where x.bt_completed == false
        //                      select new
        //                      {
        //                          i_shiftId = x.i_shiftId,
        //                          sheduledDate = x.dt_scheduledDate,
        //                          i_areaSubId = x.i_areaSubId,
        //                          sheduleTime = x.dt_scheduledDate,
        //                          permitNumber = x.vc_permitNumber,
        //                          techGroup = a.vc_groupName,
        //                          areaName = z.vc_description,
        //                          techGroupId = a.i_groupId
        //                      }
        //                      ).ToList();

        //        if (shifts != null)
        //        {
        //            BsonArray shiftArray = new BsonArray();
        //            DatabaseHelper dbHelper = new DatabaseHelper();

        //            foreach (var item in shifts)
        //            {
        //                BsonDocument shiftsCollection = new BsonDocument();
        //                shiftsCollection.Add("i_shiftId", item.i_shiftId);
        //                shiftsCollection.Add("sheduledDate", item.sheduledDate.ToString("yyy/MM/dd"));
        //                shiftsCollection.Add("i_areaSubId", item.i_areaSubId);
        //                shiftsCollection.Add("sheduleTime", item.sheduleTime.ToString("hh:mm:ss"));
        //                shiftsCollection.Add("permitNumber", item.permitNumber);
        //                shiftsCollection.Add("techGroup", item.techGroup);
        //                shiftsCollection.Add("areaName", item.areaName);
        //                shiftsCollection.Add("techGroupId", item.techGroupId);

        //                shiftArray.Add(shiftsCollection);
        //            }

        //            Drop Existing
        //            database.DropCollection("md_shiftdata");

        //            Recreate New
        //            MongoCollection collection = database.GetCollection<shiftInfo>("md_shiftdata");
        //            collection.InsertBatch(shiftArray);
        //            return true;
        //        }
        //        else return false;
        //    }
        //    catch (Exception err)
        //    {
        //        LogHelper log = new LogHelper();
        //        log("Failed to rebuild shift aggregation: " + err.Message, "rebuildShiftAgregation", logTypes.Error, "SYSTEM");
        //    }
        //}

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        //public void rebuildUserProfile()
        //{
        //    try
        //    {
        //        BsonArray usersArray = new BsonArray();
        //        var users = db.UserProfiles.ToList();
        //        foreach (var item in users)
        //        {
        //            var groupId = (from x in db.as_technicianGroupProfile
        //                           where x.UserId == item.UserId
        //                           select x.i_currentGroup).DefaultIfEmpty(0).First();
        //            Guid sessionkey = Guid.NewGuid();

        //            BsonDocument userCollection = new BsonDocument();
        //            userCollection.Add("Username", item.UserName);
        //            userCollection.Add("FirstName", item.FirstName);
        //            userCollection.Add("LastName", item.LastName);
        //            userCollection.Add("UserId", item.UserId);
        //            userCollection.Add("i_accessLevel", item.i_accessLevelId);
        //            userCollection.Add("i_airPortId", item.i_airPortId);
        //            userCollection.Add("SessionKey", sessionkey.ToString());
        //            userCollection.Add("i_groupId", groupId);

        //            usersArray.Add(userCollection);
        //        }

        //        Drop Existing
        //        database.DropCollection("md_usersprofile");

        //        Recreate New
        //        MongoCollection collection = database.GetCollection<mongoAssetDownload>("md_usersprofile");
        //        collection.InsertBatch(usersArray);
        //    }
        //    catch (Exception err)
        //    {
        //        LogHelper log = new LogHelper();
        //        log("Failed to rebuild users: " + err.Message, "rebuildUserProfile", logTypes.Error, "SYSTEM");
        //    }
        //}

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        //public void rebuildWrenchProfile()
        //{
        //    try
        //    {
        //        BsonArray wrenchArray = new BsonArray();
        //        DatabaseHelper dbHelper = new DatabaseHelper();

        //        List<as_wrenchProfile> wrenchList = new List<as_wrenchProfile>();

        //        wrenchList = (from data in db.as_wrenchProfile select data).ToList();

        //        foreach (as_wrenchProfile item in wrenchList)
        //        {
        //            BsonDocument wrenchCollection = new BsonDocument();
        //            wrenchCollection.Add("bt_active", item.bt_active);
        //            wrenchCollection.Add("dt_lastCalibrated", item.dt_lastCalibrated.ToString("yyyyMMdd"));
        //            wrenchCollection.Add("f_batteryLevel", item.f_batteryLevel);
        //            wrenchCollection.Add("i_calibrationCycle", item.i_calibrationCycle);
        //            wrenchCollection.Add("i_wrenchId", item.i_wrenchId);
        //            wrenchCollection.Add("vc_model", item.vc_model);
        //            wrenchCollection.Add("vc_serialNumber", item.vc_serialNumber);

        //            wrenchArray.Add(wrenchCollection);
        //        }

        //        Drop Existing
        //        database.DropCollection("md_wrenchprofile");

        //        Recreate New
        //        MongoCollection collection = database.GetCollection<mongoAssetDownload>("md_wrenchprofile");
        //        collection.InsertBatch(wrenchArray);
        //    }
        //    catch (Exception err)
        //    {
        //        LogHelper log = new LogHelper();
        //        log("Failed to rebuild wrench profile: " + err.Message, "rebuildWrenchProfile", logTypes.Error, "SYSTEM");
        //    }
        //}

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        //public void rebuildTechnicianGroups()
        //{
        //    try
        //    {
        //        BsonArray techArray = new BsonArray();
        //        DatabaseHelper dbHelper = new DatabaseHelper();

        //        var technicians = (from x in db.UserProfiles
        //                           join y in db.as_technicianGroupProfile on x.UserId equals y.UserId
        //                           where x.i_accessLevelId == 3
        //                           select new
        //                           {
        //                               x.UserId,
        //                               x.FirstName,
        //                               x.LastName,
        //                               x.UserName,
        //                               y.i_currentGroup,
        //                               y.i_defaultGroup,
        //                           }).ToList();

        //        foreach (var item in technicians)
        //        {
        //            BsonDocument techGroupCollection = new BsonDocument();
        //            techGroupCollection.Add("UserId", item.UserId);
        //            techGroupCollection.Add("FirstName", item.FirstName);
        //            techGroupCollection.Add("LastName", item.LastName);
        //            techGroupCollection.Add("UserName", item.UserName);
        //            techGroupCollection.Add("i_currentGroup", item.i_currentGroup);
        //            techGroupCollection.Add("i_defaultGroup", item.i_defaultGroup);

        //            techArray.Add(techGroupCollection);
        //        }

        //        //Drop Existing
        //        database.DropCollection("md_techgroups");

        //        //Recreate New
        //        MongoCollection collection = database.GetCollection<mongoAssetDownload>("md_techgroups");
        //        collection.InsertBatch(techArray);
        //    }
        //    catch (Exception err)
        //    {
        //        LogHelper log = new LogHelper();
        //        log("Failed to rebuild technician cache: " + err.Message, "rebuildTechnicianGroups", logTypes.Error, "SYSTEM");
        //    }
        //}

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        public async Task<bool> rebuildAssetProfileForAssetClass(int assetClassId)
        {
            try
            {
                var assets = from x in db.as_assetProfile
                             join y in db.as_assetClassProfile on x.i_assetClassId equals y.i_assetClassId
                             where y.i_assetClassId == assetClassId
                             select new {
                                 i_assetId = x.i_assetId,
                                 i_locationId = x.i_locationId,
                                 i_assetClassId = x.i_assetClassId,
                                 vc_rfidTag = x.vc_rfidTag,
                                 vc_serialNumber = x.vc_serialNumber,
                                 productUrl = y.vc_webSiteLink
                             };

                if (assets != null)
                {

                    mongoAssetProfile[] assetArray = new mongoAssetProfile[assets.Count()];
                    int i = 0;

                    foreach (var item in assets)
                    {
                        //Create different docuemnts 
                        mongoAssetProfile assetDoc = new mongoAssetProfile();
                        location locationDoc = new location();
                        assetClass assetClassDoc = new assetClass();
                        picture pictureDoc = new picture();

                        assetDoc.assetId = item.i_assetId;
                        assetDoc.locationId = item.i_locationId;
                        assetDoc.assetClassId = item.i_assetClassId;
                        assetDoc.rfidTag = item.vc_rfidTag;
                        assetDoc.serialNumber = item.vc_serialNumber;

                        //Get the Light Status
                        if (db.as_assetStatusProfile.Find(item.i_assetId) != null)
                            assetDoc.status = db.as_assetStatusProfile.Where(q => q.i_assetProfileId == item.i_assetId).Select(q => q.bt_assetStatus).FirstOrDefault();
                        else
                            assetDoc.status = false;

                        assetDoc.productUrl = item.productUrl;
                        assetDoc.maintenance = func.getMaintenaceTasks(item.i_assetId);

                        //get data for loaction
                        var location = db.as_locationProfile.Where(q => q.i_locationId == item.i_locationId).FirstOrDefault();
                        locationDoc.locationId = location.i_locationId;
                        locationDoc.longitude = location.f_longitude;
                        locationDoc.latitude = location.f_latitude;
                        locationDoc.designation = location.vc_designation;
                        locationDoc.areaSubId = location.i_areaSubId;

                        //Add Main Area
                        int mainArea = db.as_areaSubProfile.Where(q => q.i_areaSubId == location.i_areaSubId).Select(q => q.i_areaId).FirstOrDefault();
                        locationDoc.areaId = mainArea;

                        //Add doc to main doc
                        assetDoc.location = locationDoc;

                        //get asset class data
                        var assetclass = db.as_assetClassProfile.Where(q => q.i_assetClassId == item.i_assetClassId).FirstOrDefault();
                        assetClassDoc.assetClassId = assetclass.i_assetClassId;
                        assetClassDoc.description = assetclass.vc_description;
                        assetClassDoc.pictureId = assetclass.i_pictureId;
                        assetClassDoc.manufacturer = assetclass.vc_manufacturer;
                        assetClassDoc.model = assetclass.vc_model;

                        //add to main doc
                        assetDoc.assetClass = assetClassDoc;

                        //get picture data
                        var picture = db.as_pictureProfile.Where(q => q.i_pictureId == assetclass.i_pictureId).FirstOrDefault();
                        pictureDoc.pictureId = picture.i_pictureId;
                        pictureDoc.fileLocation = picture.vc_fileLocation;
                        pictureDoc.description = picture.vc_description;
                        
                        //add to main doc
                        assetDoc.picture = pictureDoc;

                        assetArray[i]  = assetDoc;
                        i++;
                    }

                    //Drop Current Profile
                    var filter = Builders<mongoAssetProfile>.Filter.Eq(q => q.assetClass.assetClassId, assetClassId);
                    IMongoCollection<mongoAssetProfile> collection = _database.GetCollection<mongoAssetProfile>("md_assetProfile");
                    await collection.DeleteOneAsync(filter);

                    //Insert Updated records
                    await collection.InsertManyAsync(assetArray);

                    //Check the maintenance Cycles
                    await recreateCurrentPreviousStatus(false, assetClassId);
                    return true;
                }
                else return false;
            }
            catch (Exception err)
            {
                log("Failed to rebuild asset profile for asset class: " + err.Message, "rebuildAssetProfileForAssetClass", logTypes.Error, "SYSTEM");
                return false;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        public async Task<bool> rebuildAssetProfileForAsset(int assetId)
        {
            try
            {
                var assets = (from x in db.as_assetProfile
                             join y in db.as_assetClassProfile on x.i_assetClassId equals y.i_assetClassId
                             where x.i_assetId == assetId
                             select new
                             {
                                 i_assetId = x.i_assetId,
                                 i_locationId = x.i_locationId,
                                 i_assetClassId = x.i_assetClassId,
                                 vc_rfidTag = x.vc_rfidTag,
                                 vc_serialNumber = x.vc_serialNumber,
                                 productUrl = y.vc_webSiteLink
                             });

                if (assets != null)
                {

                    mongoAssetProfile[] assetArray = new mongoAssetProfile[assets.Count()];
                    int i = 0;

                    foreach (var item in assets)
                    {
                        //Create different docuemnts 
                        mongoAssetProfile assetDoc = new mongoAssetProfile();
                        location locationDoc = new location();
                        assetClass assetClassDoc = new assetClass();
                        picture pictureDoc = new picture();

                        assetDoc.assetId = item.i_assetId;
                        assetDoc.locationId = item.i_locationId;
                        assetDoc.assetClassId = item.i_assetClassId;
                        assetDoc.rfidTag = item.vc_rfidTag;
                        assetDoc.serialNumber = item.vc_serialNumber;

                        //Get the Light Status
                        if (db.as_assetStatusProfile.Find(item.i_assetId) != null)
                            assetDoc.status = db.as_assetStatusProfile.Where(q => q.i_assetProfileId == item.i_assetId).Select(q => q.bt_assetStatus).FirstOrDefault();
                        else
                            assetDoc.status = false;

                        assetDoc.productUrl = item.productUrl;
                        assetDoc.maintenance = func.getMaintenaceTasks(item.i_assetId);

                        //get data for loaction
                        var location = db.as_locationProfile.Where(q => q.i_locationId == item.i_locationId).FirstOrDefault();
                        locationDoc.locationId = location.i_locationId;
                        locationDoc.longitude = location.f_longitude;
                        locationDoc.latitude = location.f_latitude;
                        locationDoc.designation = location.vc_designation;
                        locationDoc.areaSubId = location.i_areaSubId;

                        //Add Main Area
                        int mainArea = db.as_areaSubProfile.Where(q => q.i_areaSubId == location.i_areaSubId).Select(q => q.i_areaId).FirstOrDefault();
                        locationDoc.areaId = mainArea;

                        //Add doc to main doc
                        assetDoc.location = locationDoc;

                        //get asset class data
                        var assetclass = db.as_assetClassProfile.Where(q => q.i_assetClassId == item.i_assetClassId).FirstOrDefault();
                        assetClassDoc.assetClassId = assetclass.i_assetClassId;
                        assetClassDoc.description = assetclass.vc_description;
                        assetClassDoc.pictureId = assetclass.i_pictureId;
                        assetClassDoc.manufacturer = assetclass.vc_manufacturer;
                        assetClassDoc.model = assetclass.vc_model;

                        //add to main doc
                        assetDoc.assetClass = assetClassDoc;

                        //get picture data
                        var picture = db.as_pictureProfile.Where(q => q.i_pictureId == assetclass.i_pictureId).FirstOrDefault();
                        pictureDoc.pictureId = picture.i_pictureId;
                        pictureDoc.fileLocation = picture.vc_fileLocation;
                        pictureDoc.description = picture.vc_description;

                        //add to main doc
                        assetDoc.picture = pictureDoc;

                        assetArray[i] = assetDoc;
                        i++;
                    }

                    //Drop Current Profile
                    var filter = Builders<mongoAssetProfile>.Filter.Eq(q => q.assetId, assetId);
                    IMongoCollection<mongoAssetProfile> collection = _database.GetCollection<mongoAssetProfile>("md_assetProfile");
                    await collection.DeleteOneAsync(filter);

                    //Insert Updated records
                    await collection.InsertManyAsync(assetArray);

                    //Check the maintenance Cycles
                    await recreateCurrentPreviousStatus(true, assetId);
                    return true;
                }
                else return false;
            }
            catch (Exception err)
            {
                log("Failed to rebuild Asset Profile for asset: " + err.Message, "rebuildAssetProfileForAsset", logTypes.Error, "SYSTEM");
                return false;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        public async Task<bool> rebuildAssetProfile()
        {
            int assetID = 0;
            try
            {
                var assets = from x in db.as_assetProfile
                             join y in db.as_assetClassProfile on x.i_assetClassId equals y.i_assetClassId
                             select new
                             {
                                 i_assetId = x.i_assetId,
                                 i_locationId = x.i_locationId,
                                 i_assetClassId = x.i_assetClassId,
                                 vc_rfidTag = x.vc_rfidTag,
                                 vc_serialNumber = x.vc_serialNumber,
                                 productUrl = y.vc_webSiteLink
                             };

                if (assets != null)
                {

                    mongoAssetProfile[] assetArray = new mongoAssetProfile[assets.Count()];
                    int i = 0;

                    foreach (var item in assets)
                    {
                        //Create different docuemnts 
                        mongoAssetProfile assetDoc = new mongoAssetProfile();
                        location locationDoc = new location();
                        assetClass assetClassDoc = new assetClass();
                        picture pictureDoc = new picture();

                        assetDoc.assetId = item.i_assetId;
                        assetDoc.locationId = item.i_locationId;
                        assetDoc.assetClassId = item.i_assetClassId;
                        assetDoc.rfidTag = item.vc_rfidTag;
                        assetDoc.serialNumber = item.vc_serialNumber;

                        //Get the Light Status
                        if (db.as_assetStatusProfile.Find(item.i_assetId) != null)
                            assetDoc.status = db.as_assetStatusProfile.Where(q => q.i_assetProfileId == item.i_assetId).Select(q => q.bt_assetStatus).FirstOrDefault();
                        else
                            assetDoc.status = false;

                        assetDoc.productUrl = item.productUrl;
                        assetDoc.maintenance = func.getMaintenaceTasks(item.i_assetId);

                        //get data for loaction
                        var location = db.as_locationProfile.Where(q => q.i_locationId == item.i_locationId).FirstOrDefault();
                        locationDoc.locationId = location.i_locationId;
                        locationDoc.longitude = location.f_longitude;
                        locationDoc.latitude = location.f_latitude;
                        locationDoc.designation = location.vc_designation;
                        locationDoc.areaSubId = location.i_areaSubId;

                        //Add Main Area
                        int mainArea = db.as_areaSubProfile.Where(q => q.i_areaSubId == location.i_areaSubId).Select(q => q.i_areaId).FirstOrDefault();
                        locationDoc.areaId = mainArea;

                        //Add doc to main doc
                        assetDoc.location = locationDoc;

                        //get asset class data
                        var assetclass = db.as_assetClassProfile.Where(q => q.i_assetClassId == item.i_assetClassId).FirstOrDefault();
                        assetClassDoc.assetClassId = assetclass.i_assetClassId;
                        assetClassDoc.description = assetclass.vc_description;
                        assetClassDoc.pictureId = assetclass.i_pictureId;
                        assetClassDoc.manufacturer = assetclass.vc_manufacturer;
                        assetClassDoc.model = assetclass.vc_model;

                        //add to main doc
                        assetDoc.assetClass = assetClassDoc;

                        //get picture data
                        var picture = db.as_pictureProfile.Where(q => q.i_pictureId == assetclass.i_pictureId).FirstOrDefault();
                        pictureDoc.pictureId = picture.i_pictureId;
                        pictureDoc.fileLocation = picture.vc_fileLocation;
                        pictureDoc.description = picture.vc_description;

                        //add to main doc
                        assetDoc.picture = pictureDoc;

                        assetArray[i] = assetDoc;
                        i++;
                    }

                    //Drop Current Profile
                    await _database.DropCollectionAsync("md_assetProfile");

                    IMongoCollection<mongoAssetProfile> collection = _database.GetCollection<mongoAssetProfile>("md_assetProfile");
                    await collection.InsertManyAsync(assetArray);

                    //Check the maintenance Cycles
                    await recreateCurrentPreviousStatus(null, null);
                    return true;
                }
                else return false;
            }
            catch (Exception err)
            {
                log("Failed to rebuild asset profile:(" + assetID.ToString() + ") " + err.InnerException.Message, "rebuildAssetProfile", logTypes.Error, "SYSTEM");
                return false;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        private async Task<bool> recreateCurrentPreviousStatus(bool? assetFlag, int? Id)
        {
            DateTime lastValid = DateTime.Parse("2300/01/01");
            lastValid = DateTime.SpecifyKind(lastValid, DateTimeKind.Utc);
            BsonArray assetArray = new BsonArray();
            IMongoCollection<mongoAssetProfile> coll1 = _database.GetCollection<mongoAssetProfile>("md_assetProfile");
            IMongoCollection<MongoCurrentPreviousStatus> coll2 = _database.GetCollection<MongoCurrentPreviousStatus>("md_maintenanceTracking");
            List<mongoAssetProfile> assets = new List<mongoAssetProfile>();

            //Get the correct collection
            if (assetFlag != null)
            {
                if (assetFlag.Value)
                    assets = await coll1.Find(q => q.assetId == Id).ToListAsync();
                else
                    assets = await coll1.Find(q => q.assetClassId == Id).ToListAsync();
            }
            else
                assets = await coll1.Find(q => q.assetId != Id).ToListAsync();

            foreach(var item in assets)
            {
                foreach (var task in item.maintenance)
                {

                    MongoCurrentPreviousStatus maintenance = await coll2.Find(q => q.assetId == item.assetId && q.maintenanceId == task.maintenanceId && q.lastValid == lastValid).FirstOrDefaultAsync();

                    if (maintenance != null)
                    {
                        if (task.maintenanceCycle != maintenance.previousCycle)
                        {
                            var filter = new BsonDocument("Id", maintenance.Id);

                            var update = Builders<MongoCurrentPreviousStatus>.Update.Set(q => q.lastValid, DateTime.UtcNow);

                            await coll2.FindOneAndUpdateAsync(filter, update);

                            //Create new record
                            MongoCurrentPreviousStatus assetDoc = new MongoCurrentPreviousStatus();
                            assetDoc.assetId = item.assetId;
                            assetDoc.maintenanceId = task.maintenanceId;
                            assetDoc.previousCycle = task.maintenanceCycle;
                            assetDoc.firstValid = DateTime.UtcNow;
                            assetDoc.lastValid = lastValid;
                            
                            await coll2.InsertOneAsync(assetDoc);
                        }
                    }
                    else
                    {
                        MongoCurrentPreviousStatus assetDoc = new MongoCurrentPreviousStatus();
                        assetDoc.assetId = item.assetId;
                        assetDoc.maintenanceId = task.maintenanceId;
                        assetDoc.previousCycle = task.maintenanceCycle;
                        assetDoc.firstValid = DateTime.UtcNow;
                        assetDoc.lastValid = lastValid;

                        await coll2.InsertOneAsync(assetDoc);
                    }
                }
            }

            return true;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------


        #endregion

        #region Cache Queries

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        public async Task<List<MongoCurrentPreviousStatus>> getAssetStatusHistory()
        {
            IMongoCollection<MongoCurrentPreviousStatus> collection = _database.GetCollection<MongoCurrentPreviousStatus>("md_maintenanceTracking");
            var filter = new BsonDocument();
            List<MongoCurrentPreviousStatus> settings = new List<MongoCurrentPreviousStatus>();
            
            using (var cursor = await collection.FindAsync(filter))
            {
                while (await cursor.MoveNextAsync())
                {
                    var batch = cursor.Current;
                    foreach (var document in batch)
                    {
                        settings.Add(document);
                    }
                }
            }

            return settings;
        }
        
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        public async Task<mongoEmailSettings> getEmailSettings()
        {
            IMongoCollection<mongoEmailSettings> collection = _database.GetCollection<mongoEmailSettings>("md_emailSettings");
            var filter = new BsonDocument();
            mongoEmailSettings settings = new mongoEmailSettings();
            using (var cursor = await collection.FindAsync(filter))
            {
                while (await cursor.MoveNextAsync())
                {
                    var batch = cursor.Current;
                    foreach (var document in batch)
                    {
                        settings = document;
                    }
                }
            }

            if (settings == null)
            {
                //create default settings if it doesn't exist
                mongoEmailSettings mdSettings = new mongoEmailSettings();
                mdSettings.apiKey = "key-82e66599b538527a71b035abfcd0a0ae";
                mdSettings.domain = "adb-airside.com";
                mdSettings.fromAddress = "info@adb-airside.com";

                await collection.InsertOneAsync(mdSettings);

                //retry
                settings = mdSettings;
            }
            return settings;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        public async Task<string> getAssetNextDateForFirstTask(int assetId)
        {
            IMongoCollection<mongoAssetProfile> collection = _database.GetCollection<mongoAssetProfile>("md_assetProfile");

            List<mongoAssetProfile> assets = await collection.Find(q => q.assetId == assetId).ToListAsync();

            mongoAssetProfile asset = assets.FirstOrDefault();
            maintenance task = asset.maintenance.FirstOrDefault();

            return task.nextDate;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        public async Task<string> getAssetPreviousDateForFirstTask(int assetId)
        {
            try
            {
                IMongoCollection<mongoAssetProfile> collection = _database.GetCollection<mongoAssetProfile>("md_assetProfile");

                List<mongoAssetProfile> assets = await collection.Find(q => q.assetId == assetId).ToListAsync();

                mongoAssetProfile asset = assets.FirstOrDefault();
                maintenance task = asset.maintenance.FirstOrDefault();

                return task.previousDate;
            } catch
            {
                DateTime returnDate = new DateTime(1970, 1, 1);
                return returnDate.ToString("yyy/MM/dd");
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        public async Task<List<mongoAssetDownload>> getAssetAssosiations(int areaSubId)
        {
            IMongoCollection<mongoAssetDownload> collection = _database.GetCollection<mongoAssetDownload>("md_assetDownload");
            var filter = new BsonDocument();
            List<mongoAssetDownload> assets = new List<mongoAssetDownload>();
            using (var cursor = await collection.FindAsync(filter))
            {
                while (await cursor.MoveNextAsync())
                {
                    var batch = cursor.Current;
                    foreach (var document in batch)
                    {
                        assets.Add(document);
                    }
                }
            }
            return assets;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        public async Task<List<mongoAssetClassDownload>> getAllAssetClasses()
        {
            IMongoCollection<mongoAssetClassDownload> collection = _database.GetCollection<mongoAssetClassDownload>("md_assetClassDownload");
            var filter = new BsonDocument();
            List<mongoAssetClassDownload> assets = new List<mongoAssetClassDownload>();
            using (var cursor = await collection.FindAsync(filter))
            {
                while (await cursor.MoveNextAsync())
                {
                    var batch = cursor.Current;
                    foreach (var document in batch)
                    {
                        assets.Add(document);
                    }
                }
            }
            return assets;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        public async Task<List<mongoFullAsset>> getAllAssetDownload()
        {
            IMongoCollection<mongoFullAsset> collection = _database.GetCollection<mongoFullAsset>("md_assetFullDownload");
            var filter = new BsonDocument();
            List<mongoFullAsset> assets = new List<mongoFullAsset>();
            using (var cursor = await collection.FindAsync(filter))
            {
                while (await cursor.MoveNextAsync())
                {
                    var batch = cursor.Current;
                    foreach (var document in batch)
                    {
                        assets.Add(document);
                    }
                }
            }
            return assets;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        public async Task<List<mongoAssetProfile>> getAllAssets()
        {
            try
            {
                var collection = _database.GetCollection<mongoAssetProfile>("md_assetProfile");
                var filter = new BsonDocument();
                List<mongoAssetProfile> assets = new List<mongoAssetProfile>();
                using (var cursor = await collection.FindAsync(filter))
                {
                    while (await cursor.MoveNextAsync())
                    {
                        var batch = cursor.Current;
                        foreach (var document in batch)
                        {
                            assets.Add(document);
                        }
                    }
                }
                return assets;
            }
            catch (Exception err)
            {
                log("Failed to retrive all asset profiles from Mongo: " + err.Message, "getAllAssets", logTypes.Error, "SYSTEM");
                return null;
            }
        }

        #endregion

        #region LogHelper

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        public Boolean writeLog(mongoLogHelper log)
        {
            try
            {
                IMongoCollection<mongoLogHelper> collection = _database.GetCollection<mongoLogHelper>("md_logProfile");
                collection.InsertOneAsync(log);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        #region iOS Cache

        public void updateiOSCache(string module)
        {
            try
            {
                //This will update the cache hash for downloads
                //Create Date: 2015/01/27
                //Author: Bernard Willer

                as_cacheProfile cacheModule = db.as_cacheProfile.Where(q => q.vc_module == module).FirstOrDefault();
                Guid newHash = Guid.NewGuid();
                cacheModule.ui_currentHash = newHash;
                db.Entry(cacheModule).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
            }
            catch (Exception err)
            {
                logError(err, "SYSTEM");
            }

        }

        #endregion

        #region Logging
        
        public enum logTypes
        {
            Error = 101,
            Debug = 102,
            Info = 103
        }

        public Boolean logError(Exception err, string user, [CallerMemberName]string memberName = "")
        {
            try
            {
                mongoLogHelper log = new mongoLogHelper();
                if (err.InnerException != null)
                    log.logdescription = err.InnerException.Message;
                else
                    log.logdescription = err.Message;

                log.logTimeStamp = DateTime.Now;
                log.logTypeId = (int)logTypes.Error;
                log.logModule = memberName;
                log.aspUserId = user;

                //Commit to Mongo
                Boolean flag = writeLog(log);


                return true;
            }
            catch
            {
                return false;
            }
        }

        public void quickDebugLog(string logString)
        {
            try
            {
                mongoLogHelper log = new mongoLogHelper();
                log.logdescription = logString;
                log.logTimeStamp = DateTime.Now;
                log.logTypeId = (int)logTypes.Debug;
                log.logModule = "DEBUG";
                log.aspUserId = "DEV";

                //Commit to Mongo
                Boolean flag = writeLog(log);

            }
            catch (Exception err)
            {

            }
        }

        public void log(string logString, string module, logTypes logType, string aspUser)
        {
            mongoLogHelper log = new mongoLogHelper();
            log.logdescription = logString;
            log.logTimeStamp = DateTime.Now;
            log.logTypeId = (int)logType;
            log.logModule = module;
            log.aspUserId = aspUser;

            //Commit to Mongo
            Boolean flag = writeLog(log);
        }


        #endregion

        #region Helpers
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                db.Dispose();
            }
            // free native resources
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        #endregion
    }
}
