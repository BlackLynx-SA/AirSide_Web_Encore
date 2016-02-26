
#define DEBUG

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AirSide.ServerModules.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AirSide.ServerModules.Helpers
{
    public class CacheHelper : IDisposable
    {
        //Mongo Globals
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        protected static IMongoClient Client;
        protected static IMongoDatabase Database;

        private readonly DatabaseHelper _func;

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// This is the class initializer. It takes the database and connection string to the MongoDB as parameters.
        /// </summary>
        /// <param name="database">The MongoDB Database</param>
        /// <param name="connectionString">The MongoDB Connection String</param>
        public CacheHelper(string database, string connectionString)
        {
            Client = new MongoClient(connectionString);
            Database = Client.GetDatabase(database);
            _func = new DatabaseHelper();
        }
       

        //SQL Entity Globals
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        private readonly Entities _db = new Entities();

        #region Cache Rebuild

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        public async Task<Boolean> CreateAssetClassDownloadCache()
        {
            try
            {
                //List<assetClassDownload> assetList = new List<assetClassDownload>();

                var assets = (from x in _db.as_assetClassProfile
                              select x);

                {
                    mongoAssetClassDownload[] assetArray = new mongoAssetClassDownload[assets.Count()];
                    int i = 0;

                    foreach (var item in assets)
                    {
                        mongoAssetClassDownload asset = new mongoAssetClassDownload
                        {
                            i_assetClassId = item.i_assetClassId,
                            vc_description = item.vc_description,
                            i_assetCheckTypeId = 0,
                            assetCheckCount = _func.GetNumberOfFixingPoints(item.i_assetClassId)
                        };
                        assetArray[i] = asset;
                        i++;
                    }

                    //Drop Existing
                    await Database.DropCollectionAsync("md_assetClassDownload");

                    //Recreate New
                    IMongoCollection<mongoAssetClassDownload> collection = Database.GetCollection<mongoAssetClassDownload>("md_assetClassDownload");
                    await collection.InsertManyAsync(assetArray);

                    return true;
                }
            }
            catch (Exception err)
            {
                Log("Failed to create Asset Class Download Cache: " + err.Message, "createAssetClassDownloadCache(iOS)", LogTypes.Error, "SYSTEM");
                return false;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        public async Task<Boolean> CreateAllAssetDownload()
        {
            try
            {
                var assets = (from x in _db.as_assetProfile
                              join y in _db.as_locationProfile on x.i_locationId equals y.i_locationId
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

                {
                    Log("Assets Selected", "CreateAllAssetDownload(iOS)", LogTypes.Debug, "SYSTEM");
                    mongoFullAsset[] assetArray = new mongoFullAsset[assets.Count()];
                    int i = 0;

                    foreach (var item in assets)
                    {
                        Boolean lightStatus = false;
                        var status = _db.as_assetStatusProfile.FirstOrDefault(q => q.i_assetProfileId == item.assetId);
                        if (status != null)
                            lightStatus = status.bt_assetStatus;
                    try
                    {
                        mongoFullAsset asset = new mongoFullAsset
                        {
                            assetId = item.assetId,
                            serialNumber = item.serialNumber,
                            firstMaintainedDate = _func.GetFirstMaintanedDate(item.assetId).ToString("yyyMMdd"),
                            lastMaintainedDate = await GetAssetPreviousDateForFirstTask(item.assetId),
                            nextMaintenanceDate = await GetAssetNextDateForFirstTask(item.assetId),
                            latitude = item.latitude,
                            longitude = item.longitude,
                            subAreaId = item.subAreaId,
                            assetClassId = item.assetClassId,
                            rfidTag = item.rfidTag,
                            status = lightStatus
                        };
                        assetArray[i] = asset;
                        i++;
                    }
                    catch (Exception e)
                    {
                        Log("Object Error: " + e.Message, "CreateAllAssetDownload(iOS)", LogTypes.Debug, "SYSTEM");
                    }

                }
                    Log("Object created", "CreateAllAssetDownload(iOS)", LogTypes.Debug, "SYSTEM");

                    //Drop Existing
                    await Database.DropCollectionAsync("md_assetFullDownload");
                    Log("Dropped DB", "CreateAllAssetDownload(iOS)", LogTypes.Debug, "SYSTEM");

                    //Recreate New
                    IMongoCollection<mongoFullAsset> collection = Database.GetCollection<mongoFullAsset>("md_assetFullDownload");
                    await collection.InsertManyAsync(assetArray);
                    Log("Inserted new", "CreateAllAssetDownload(iOS)", LogTypes.Debug, "SYSTEM");

                    return true;
                }
            }
            catch (Exception err)
            {
                Log("Failed to create Asset Full Download Cache: " + err.Message, "createAllAssetDownload(iOS)", LogTypes.Error, "SYSTEM");
                return false;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        public async Task<Boolean> CreateAllAssetDownloadForAsset(int assetId)
        {
            try
            {
                var assets = (from x in _db.as_assetProfile
                              join y in _db.as_locationProfile on x.i_locationId equals y.i_locationId
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

                {

                    mongoFullAsset[] assetArray = new mongoFullAsset[assets.Count()];
                    int i = 0;

                    foreach (var item in assets)
                    {
                        Boolean lightStatus = false;
                        var status = _db.as_assetStatusProfile.FirstOrDefault(q => q.i_assetProfileId == item.assetId);
                        if (status != null)
                            lightStatus = status.bt_assetStatus;

                        mongoFullAsset asset = new mongoFullAsset
                        {
                            assetId = item.assetId,
                            serialNumber = item.serialNumber,
                            firstMaintainedDate = _func.GetFirstMaintanedDate(item.assetId).ToString("yyyMMdd"),
                            lastMaintainedDate = await GetAssetPreviousDateForFirstTask(item.assetId),
                            nextMaintenanceDate = await GetAssetNextDateForFirstTask(item.assetId),
                            latitude = item.latitude,
                            longitude = item.longitude,
                            subAreaId = item.subAreaId,
                            assetClassId = item.assetClassId,
                            rfidTag = item.rfidTag,
                            status = lightStatus
                        };
                        assetArray[i] = asset;
                        i++;
                    }

                    //Drop Current Profile
                    var filter = Builders<mongoFullAsset>.Filter.Eq(q => q.assetId, assetId);
                    IMongoCollection<mongoFullAsset> collection = Database.GetCollection<mongoFullAsset>("md_assetFullDownload");
                    await collection.DeleteOneAsync(filter);

                    //Insert Updated Asset
                    await collection.InsertManyAsync(assetArray);

                    return true;
                }
            }
            catch (Exception err)
            {
                Log("Failed to create Asset Full Download Cache: " + err.Message, "createAllAssetDownload(iOS)", LogTypes.Error, "SYSTEM");
                return false;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        public async Task<Boolean> CreateAssetDownloadCache()
        {
            try
            {
                var assets = from x in _db.as_assetProfile
                             join y in _db.as_locationProfile on x.i_locationId equals y.i_locationId
                             select new
                             {
                                 x.i_assetId, x.i_assetClassId, x.vc_rfidTag, x.vc_serialNumber, x.i_locationId, y.i_areaSubId, y.f_longitude, y.f_latitude
                             };

                {
                    mongoAssetDownload[] assetList = new mongoAssetDownload[assets.Count()];
                    int i = 0;

                    foreach (var item in assets)
                    {
                        mongoAssetDownload asset = new mongoAssetDownload
                        {
                            i_assetId = item.i_assetId,
                            i_assetClassId = item.i_assetClassId,
                            vc_tagId = item.vc_rfidTag,
                            vc_serialNumber = item.vc_serialNumber,
                            i_locationId = item.i_locationId,
                            longitude = item.f_longitude,
                            latitude = item.f_latitude,
                            lastDate = _func.GetLastShiftDateForAsset(item.i_assetId),
                            maintenance = "0",
                            submitted = _func.GetSubmittedShiftData(item.i_assetId)
                        };
                        assetList[i] = asset;
                        i++;
                    }

                    //Drop Existing
                    await Database.DropCollectionAsync("md_assetDownload");

                    //Recreate New
                    IMongoCollection<mongoAssetDownload> collection = Database.GetCollection<mongoAssetDownload>("md_assetDownload");
                    await collection.InsertManyAsync(assetList);

                    return true;
                }
            }
            catch (Exception err)
            {
                Log("Failed to create Asset Download Cache: " + err.Message, "createAssetDownloadCache(iOS)", LogTypes.Error, "SYSTEM");
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
        public async Task<bool> RebuildAssetProfileForAssetClass(int assetClassId)
        {
            try
            {
                var assets = from x in _db.as_assetProfile
                             join y in _db.as_assetClassProfile on x.i_assetClassId equals y.i_assetClassId
                             where y.i_assetClassId == assetClassId
                             select new {
                                 x.i_assetId, x.i_locationId, x.i_assetClassId, x.vc_rfidTag, x.vc_serialNumber,
                                 productUrl = y.vc_webSiteLink
                             };

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
                        assetDoc.status = _db.as_assetStatusProfile.Find(item.i_assetId) != null && _db.as_assetStatusProfile.Where(q => q.i_assetProfileId == item.i_assetId).Select(q => q.bt_assetStatus).FirstOrDefault();

                        assetDoc.productUrl = item.productUrl;
                        assetDoc.maintenance = _func.GetMaintenaceTasks(item.i_assetId);

                        //get data for loaction
                        var location = _db.as_locationProfile.FirstOrDefault(q => q.i_locationId == item.i_locationId);
                        if (location != null)
                        {
                            locationDoc.locationId = location.i_locationId;
                            locationDoc.longitude = location.f_longitude;
                            locationDoc.latitude = location.f_latitude;
                            locationDoc.designation = location.vc_designation;
                            locationDoc.areaSubId = location.i_areaSubId;

                            //Add Main Area
                            int mainArea = _db.as_areaSubProfile.Where(q => q.i_areaSubId == location.i_areaSubId).Select(q => q.i_areaId).FirstOrDefault();
                            locationDoc.areaId = mainArea;
                        }

                        //Add doc to main doc
                        assetDoc.location = locationDoc;

                        //get asset class data
                        var assetclass = _db.as_assetClassProfile.FirstOrDefault(q => q.i_assetClassId == item.i_assetClassId);
                        if (assetclass != null)
                        {
                            assetClassDoc.assetClassId = assetclass.i_assetClassId;
                            assetClassDoc.description = assetclass.vc_description;
                            assetClassDoc.pictureId = assetclass.i_pictureId;
                            assetClassDoc.manufacturer = assetclass.vc_manufacturer;
                            assetClassDoc.model = assetclass.vc_model;

                            //add to main doc
                            assetDoc.assetClass = assetClassDoc;

                            //get picture data
                            var picture = _db.as_pictureProfile.FirstOrDefault(q => q.i_pictureId == assetclass.i_pictureId);
                            if (picture != null)
                            {
                                pictureDoc.pictureId = picture.i_pictureId;
                                pictureDoc.fileLocation = picture.vc_fileLocation;
                                pictureDoc.description = picture.vc_description;
                            }
                        }

                        //add to main doc
                        assetDoc.picture = pictureDoc;

                        assetArray[i]  = assetDoc;
                        i++;
                    }

                    //Drop Current Profile
                    var filter = Builders<mongoAssetProfile>.Filter.Eq(q => q.assetClass.assetClassId, assetClassId);
                    IMongoCollection<mongoAssetProfile> collection = Database.GetCollection<mongoAssetProfile>("md_assetProfile");
                    await collection.DeleteOneAsync(filter);

                    //Insert Updated records
                    await collection.InsertManyAsync(assetArray);

                    //Check the maintenance Cycles
                    await RecreateCurrentPreviousStatus(false, assetClassId);
                    return true;
                }
            }
            catch (Exception err)
            {
                Log("Failed to rebuild asset profile for asset class: " + err.Message, "rebuildAssetProfileForAssetClass", LogTypes.Error, "SYSTEM");
                return false;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        public async Task<bool> RebuildAssetProfileForAsset(int assetId)
        {
            try
            {
                var assets = (from x in _db.as_assetProfile
                             join y in _db.as_assetClassProfile on x.i_assetClassId equals y.i_assetClassId
                             where x.i_assetId == assetId
                             select new
                             {
                                 x.i_assetId, x.i_locationId, x.i_assetClassId, x.vc_rfidTag, x.vc_serialNumber,
                                 productUrl = y.vc_webSiteLink
                             });

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
                        assetDoc.status = _db.as_assetStatusProfile.Find(item.i_assetId) != null && _db.as_assetStatusProfile.Where(q => q.i_assetProfileId == item.i_assetId).Select(q => q.bt_assetStatus).FirstOrDefault();

                        assetDoc.productUrl = item.productUrl;
                        assetDoc.maintenance = _func.GetMaintenaceTasks(item.i_assetId);

                        //get data for loaction
                        var location = _db.as_locationProfile.FirstOrDefault(q => q.i_locationId == item.i_locationId);
                        if (location != null)
                        {
                            locationDoc.locationId = location.i_locationId;
                            locationDoc.longitude = location.f_longitude;
                            locationDoc.latitude = location.f_latitude;
                            locationDoc.designation = location.vc_designation;
                            locationDoc.areaSubId = location.i_areaSubId;

                            //Add Main Area
                            int mainArea = _db.as_areaSubProfile.Where(q => q.i_areaSubId == location.i_areaSubId).Select(q => q.i_areaId).FirstOrDefault();
                            locationDoc.areaId = mainArea;
                        }

                        //Add doc to main doc
                        assetDoc.location = locationDoc;

                        //get asset class data
                        var assetclass = _db.as_assetClassProfile.FirstOrDefault(q => q.i_assetClassId == item.i_assetClassId);
                        if (assetclass != null)
                        {
                            assetClassDoc.assetClassId = assetclass.i_assetClassId;
                            assetClassDoc.description = assetclass.vc_description;
                            assetClassDoc.pictureId = assetclass.i_pictureId;
                            assetClassDoc.manufacturer = assetclass.vc_manufacturer;
                            assetClassDoc.model = assetclass.vc_model;

                            //add to main doc
                            assetDoc.assetClass = assetClassDoc;

                            //get picture data
                            var picture = _db.as_pictureProfile.FirstOrDefault(q => q.i_pictureId == assetclass.i_pictureId);
                            if (picture != null)
                            {
                                pictureDoc.pictureId = picture.i_pictureId;
                                pictureDoc.fileLocation = picture.vc_fileLocation;
                                pictureDoc.description = picture.vc_description;
                            }
                        }

                        //add to main doc
                        assetDoc.picture = pictureDoc;

                        assetArray[i] = assetDoc;
                        i++;
                    }

                    //Drop Current Profile
                    var filter = Builders<mongoAssetProfile>.Filter.Eq(q => q.assetId, assetId);
                    IMongoCollection<mongoAssetProfile> collection = Database.GetCollection<mongoAssetProfile>("md_assetProfile");
                    await collection.DeleteOneAsync(filter);

                    //Insert Updated records
                    await collection.InsertManyAsync(assetArray);

                    //Check the maintenance Cycles
                    await RecreateCurrentPreviousStatus(true, assetId);
                    return true;
                }
            }
            catch (Exception err)
            {
                Log("Failed to rebuild Asset Profile for asset: " + err.Message, "rebuildAssetProfileForAsset", LogTypes.Error, "SYSTEM");
                return false;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        public async Task<bool> RebuildAssetProfile()
        {
            int assetId = 0;
            try
            {
                var assets = from x in _db.as_assetProfile
                             join y in _db.as_assetClassProfile on x.i_assetClassId equals y.i_assetClassId
                             select new
                             {
                                 x.i_assetId, x.i_locationId, x.i_assetClassId, x.vc_rfidTag, x.vc_serialNumber,
                                 productUrl = y.vc_webSiteLink
                             };

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
                        assetDoc.status = _db.as_assetStatusProfile.Find(item.i_assetId) != null && _db.as_assetStatusProfile.Where(q => q.i_assetProfileId == item.i_assetId).Select(q => q.bt_assetStatus).FirstOrDefault();

                        assetDoc.productUrl = item.productUrl;
                        assetDoc.maintenance = _func.GetMaintenaceTasks(item.i_assetId);

                        //get data for loaction
                        var location = _db.as_locationProfile.FirstOrDefault(q => q.i_locationId == item.i_locationId);
                        if (location != null)
                        {
                            locationDoc.locationId = location.i_locationId;
                            locationDoc.longitude = location.f_longitude;
                            locationDoc.latitude = location.f_latitude;
                            locationDoc.designation = location.vc_designation;
                            locationDoc.areaSubId = location.i_areaSubId;

                            //Add Main Area
                            int mainArea = _db.as_areaSubProfile.Where(q => q.i_areaSubId == location.i_areaSubId).Select(q => q.i_areaId).FirstOrDefault();
                            locationDoc.areaId = mainArea;
                        }

                        //Add doc to main doc
                        assetDoc.location = locationDoc;

                        //get asset class data
                        var assetclass = _db.as_assetClassProfile.FirstOrDefault(q => q.i_assetClassId == item.i_assetClassId);
                        if (assetclass != null)
                        {
                            assetClassDoc.assetClassId = assetclass.i_assetClassId;
                            assetClassDoc.description = assetclass.vc_description;
                            assetClassDoc.pictureId = assetclass.i_pictureId;
                            assetClassDoc.manufacturer = assetclass.vc_manufacturer;
                            assetClassDoc.model = assetclass.vc_model;

                            //add to main doc
                            assetDoc.assetClass = assetClassDoc;

                            //get picture data
                            var picture = _db.as_pictureProfile.FirstOrDefault(q => q.i_pictureId == assetclass.i_pictureId);
                            if (picture != null)
                            {
                                pictureDoc.pictureId = picture.i_pictureId;
                                pictureDoc.fileLocation = picture.vc_fileLocation;
                                pictureDoc.description = picture.vc_description;
                            }
                        }

                        //add to main doc
                        assetDoc.picture = pictureDoc;

                        assetArray[i] = assetDoc;
                        i++;
                    }

                    //Drop Current Profile
                    await Database.DropCollectionAsync("md_assetProfile");

                    IMongoCollection<mongoAssetProfile> collection = Database.GetCollection<mongoAssetProfile>("md_assetProfile");
                    await collection.InsertManyAsync(assetArray);

                    //Check the maintenance Cycles
                    await RecreateCurrentPreviousStatus(null, null);
                    return true;
                }
            }
            catch (Exception err)
            {
                Log("Failed to rebuild asset profile:(" + assetId + ") " + err.InnerException.Message, "rebuildAssetProfile", LogTypes.Error, "SYSTEM");
                return false;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        private async Task<bool> RecreateCurrentPreviousStatus(bool? assetFlag, int? id)
        {
            DateTime lastValid = DateTime.Parse("2300/01/01");
            lastValid = DateTime.SpecifyKind(lastValid, DateTimeKind.Utc);
            IMongoCollection<mongoAssetProfile> coll1 = Database.GetCollection<mongoAssetProfile>("md_assetProfile");
            IMongoCollection<MongoCurrentPreviousStatus> coll2 = Database.GetCollection<MongoCurrentPreviousStatus>("md_maintenanceTracking");
            List<mongoAssetProfile> assets;

            //Get the correct collection
            if (assetFlag != null)
            {
                if (assetFlag.Value)
                    assets = await coll1.Find(q => q.assetId == id).ToListAsync();
                else
                    assets = await coll1.Find(q => q.assetClassId == id).ToListAsync();
            }
            else
                assets = await coll1.Find(q => q.assetId != id).ToListAsync();

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
                            MongoCurrentPreviousStatus assetDoc = new MongoCurrentPreviousStatus
                            {
                                assetId = item.assetId,
                                maintenanceId = task.maintenanceId,
                                previousCycle = task.maintenanceCycle,
                                firstValid = DateTime.UtcNow,
                                lastValid = lastValid
                            };

                            await coll2.InsertOneAsync(assetDoc);
                        }
                    }
                    else
                    {
                        MongoCurrentPreviousStatus assetDoc = new MongoCurrentPreviousStatus
                        {
                            assetId = item.assetId,
                            maintenanceId = task.maintenanceId,
                            previousCycle = task.maintenanceCycle,
                            firstValid = DateTime.UtcNow,
                            lastValid = lastValid
                        };

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
        public async Task<List<MongoCurrentPreviousStatus>> GetAssetStatusHistory()
        {
            IMongoCollection<MongoCurrentPreviousStatus> collection = Database.GetCollection<MongoCurrentPreviousStatus>("md_maintenanceTracking");
            var filter = new BsonDocument();
            List<MongoCurrentPreviousStatus> settings = new List<MongoCurrentPreviousStatus>();
            
            using (var cursor = await collection.FindAsync(filter))
            {
                while (await cursor.MoveNextAsync())
                {
                    var batch = cursor.Current;
                    settings.AddRange(batch);
                }
            }

            return settings;
        }
        
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        public async Task<mongoEmailSettings> GetEmailSettings()
        {
            IMongoCollection<mongoEmailSettings> collection = Database.GetCollection<mongoEmailSettings>("md_emailSettings");
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
                mongoEmailSettings mdSettings = new mongoEmailSettings
                {
                    apiKey = "key-82e66599b538527a71b035abfcd0a0ae",
                    domain = "adb-airside.com",
                    fromAddress = "info@adb-airside.com"
                };

                await collection.InsertOneAsync(mdSettings);

                //retry
                settings = mdSettings;
            }
            return settings;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        public async Task<string> GetAssetNextDateForFirstTask(int assetId)
        {
            try
            {
                IMongoCollection<mongoAssetProfile> collection =
                    Database.GetCollection<mongoAssetProfile>("md_assetProfile");

                List<mongoAssetProfile> assets = await collection.Find(q => q.assetId == assetId).ToListAsync();

                mongoAssetProfile asset = assets.FirstOrDefault();
                if (asset != null)
                {
                    maintenance task = asset.maintenance.FirstOrDefault();

                    if (task != null) return task.nextDate;
                    else return "1970/01/01";
                }
                else return "1970/01/01";
            }
            catch (Exception)
            {
                return "1970/01/01";
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        public async Task<string> GetAssetPreviousDateForFirstTask(int assetId)
        {
            try
            {
                IMongoCollection<mongoAssetProfile> collection = Database.GetCollection<mongoAssetProfile>("md_assetProfile");

                List<mongoAssetProfile> assets = await collection.Find(q => q.assetId == assetId).ToListAsync();

                mongoAssetProfile asset = assets.FirstOrDefault();
                if (asset != null)
                {
                    maintenance task = asset.maintenance.FirstOrDefault();

                    if (task != null) return task.previousDate;
                    else
                        return "1970/01/01";
                }
                else
                    return "1970/01/01";
            } catch
            {
                return "1970/01/01";
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        public async Task<List<mongoAssetDownload>> GetAssetAssosiations(int areaSubId)
        {
            IMongoCollection<mongoAssetDownload> collection = Database.GetCollection<mongoAssetDownload>("md_assetDownload");
            var filter = new BsonDocument();
            List<mongoAssetDownload> assets = new List<mongoAssetDownload>();
            using (var cursor = await collection.FindAsync(filter))
            {
                while (await cursor.MoveNextAsync())
                {
                    var batch = cursor.Current;
                    assets.AddRange(batch);
                }
            }
            return assets;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        public async Task<List<mongoAssetClassDownload>> GetAllAssetClasses()
        {
            IMongoCollection<mongoAssetClassDownload> collection = Database.GetCollection<mongoAssetClassDownload>("md_assetClassDownload");
            var filter = new BsonDocument();
            List<mongoAssetClassDownload> assets = new List<mongoAssetClassDownload>();
            using (var cursor = await collection.FindAsync(filter))
            {
                while (await cursor.MoveNextAsync())
                {
                    var batch = cursor.Current;
                    assets.AddRange(batch);
                }
            }
            return assets;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        public async Task<List<mongoFullAsset>> GetAllAssetDownload()
        {
            List<mongoFullAsset> assets = new List<mongoFullAsset>();
            try
            {
                IMongoCollection<mongoFullAsset> collection = Database.GetCollection<mongoFullAsset>("md_assetFullDownload");
                var filter = new BsonDocument();
                using (var cursor = await collection.FindAsync(filter))
                {
                    while (await cursor.MoveNextAsync())
                    {
                        var batch = cursor.Current;
                        assets.AddRange(batch);
                    }
                }
                return assets;
            }
            catch (Exception)
            {
                return assets;
            }
           
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        public async Task<List<mongoAssetProfile>> GetAllAssets()
        {
            try
            {
                var collection = Database.GetCollection<mongoAssetProfile>("md_assetProfile");
                var filter = new BsonDocument();
                List<mongoAssetProfile> assets = new List<mongoAssetProfile>();
                using (var cursor = await collection.FindAsync(filter))
                {
                    while (await cursor.MoveNextAsync())
                    {
                        var batch = cursor.Current;
                        assets.AddRange(batch);
                    }
                }
                return assets;
            }
            catch (Exception err)
            {
                Log("Failed to retrive all asset profiles from Mongo: " + err.Message, "getAllAssets", LogTypes.Error, "SYSTEM");
                return null;
            }
        }

        #endregion

        #region LogHelper

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        public Boolean WriteLog(mongoLogHelper log)
        {
            try
            {
                IMongoCollection<mongoLogHelper> collection = Database.GetCollection<mongoLogHelper>("md_logProfile");
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

        public void UpdateiOsCache(string module)
        {
            try
            {
                //This will update the cache hash for downloads
                //Create Date: 2015/01/27
                //Author: Bernard Willer

                as_cacheProfile cacheModule = _db.as_cacheProfile.FirstOrDefault(q => q.vc_module == module);
                Guid newHash = Guid.NewGuid();
                if (cacheModule != null)
                {
                    cacheModule.ui_currentHash = newHash;
                    _db.Entry(cacheModule).State = EntityState.Modified;
                }
                _db.SaveChanges();
            }
            catch (Exception err)
            {
                LogError(err, "SYSTEM");
            }

        }

        #endregion

        #region Logging
        
        public enum LogTypes
        {
            Error = 101,
            Debug = 102,
            Info = 103
        }

        public Boolean LogError(Exception err, string user, [CallerMemberName]string memberName = "")
        {
            try
            {
                mongoLogHelper log = new mongoLogHelper
                {
                    logdescription = err.InnerException?.Message ?? err.Message,
                    logTimeStamp = DateTime.Now,
                    logTypeId = (int) LogTypes.Error,
                    logModule = memberName,
                    aspUserId = user
                };


                //Commit to Mongo
                WriteLog(log);


                return true;
            }
            catch
            {
                return false;
            }
        }

        public void QuickDebugLog(string logString)
        {
            try
            {
                mongoLogHelper log = new mongoLogHelper
                {
                    logdescription = logString,
                    logTimeStamp = DateTime.Now,
                    logTypeId = (int) LogTypes.Debug,
                    logModule = "DEBUG",
                    aspUserId = "DEV"
                };

                //Commit to Mongo
                WriteLog(log);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public void Log(string logString, string module, LogTypes logType, string aspUser)
        {
            mongoLogHelper log = new mongoLogHelper
            {
                logdescription = logString,
                logTimeStamp = DateTime.Now,
                logTypeId = (int) logType,
                logModule = module,
                aspUserId = aspUser
            };

            //Commit to Mongo
            WriteLog(log);
        }

        #endregion

        #region Helpers
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                _db.Dispose();
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
