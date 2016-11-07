
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
        public async Task<bool> CreateAssetClassDownloadCache()
        {
            try
            {
                var assets = (from x in _db.as_assetClassProfile
                              select x);

                {
                    var assetArray = new mongoAssetClassDownload[assets.Count()];
                    var i = 0;

                    foreach (var item in assets)
                    {
                        var asset = new mongoAssetClassDownload
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
                    var collection = Database.GetCollection<mongoAssetClassDownload>("md_assetClassDownload");
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
        public async Task<bool> CreateAllAssetDownload()
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
                    var assetArray = new mongoFullAsset[assets.Count()];
                    var i = 0;

                    foreach (var item in assets)
                    {
                        var lightStatus = false;
                        var status = await _db.as_assetStatusProfile.OrderByDescending(q=>q.dt_lastUpdated).FirstOrDefaultAsync(q => q.i_assetProfileId == item.assetId);
                        if (status != null)
                            lightStatus = status.bt_assetStatus;
                    try
                    {
                        var asset = new mongoFullAsset
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
                    var collection = Database.GetCollection<mongoFullAsset>("md_assetFullDownload");
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

        public async Task<bool> CreateAllAssetDownload(int id)
        {
            try
            {
                var assets = (from x in _db.as_assetProfile
                              join y in _db.as_locationProfile on x.i_locationId equals y.i_locationId
                              where x.i_assetId == id
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
                    var assetArray = new mongoFullAsset[assets.Count()];
                    var i = 0;

                    foreach (var item in assets)
                    {
                        var lightStatus = false;
                        var status = await _db.as_assetStatusProfile.OrderByDescending(q => q.dt_lastUpdated).FirstOrDefaultAsync(q => q.i_assetProfileId == item.assetId);
                        if (status != null)
                            lightStatus = status.bt_assetStatus;
                        try
                        {
                            var asset = new mongoFullAsset
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

                    //Drop Current Profile
                    var filter = Builders<mongoFullAsset>.Filter.Eq(q => q.assetId, id);
                    var collection = Database.GetCollection<mongoFullAsset>("md_assetFullDownload");
                    await collection.DeleteOneAsync(filter);

                    //Insert Updated records
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
        public async Task<bool> CreateAllAssetDownloadForAsset(int assetId)
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

                    var assetArray = new mongoFullAsset[assets.Count()];
                    var i = 0;

                    foreach (var item in assets)
                    {
                        var lightStatus = false;
                        var status = await _db.as_assetStatusProfile.OrderByDescending(q=>q.dt_lastUpdated).FirstOrDefaultAsync(q => q.i_assetProfileId == item.assetId);
                        if (status != null)
                            lightStatus = status.bt_assetStatus;

                        var asset = new mongoFullAsset
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
                    var collection = Database.GetCollection<mongoFullAsset>("md_assetFullDownload");
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
        public async Task<bool> CreateAssetDownloadCache()
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
                    var assetList = new mongoAssetDownload[assets.Count()];
                    var i = 0;

                    foreach (var item in assets)
                    {
                        var asset = new mongoAssetDownload
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
                    var collection = Database.GetCollection<mongoAssetDownload>("md_assetDownload");
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

                    var assetArray = new mongoAssetProfile[assets.Count()];
                    var i = 0;

                    foreach (var item in assets)
                    {
                        //Create different docuemnts 
                        var assetDoc = new mongoAssetProfile();
                        var locationDoc = new location();
                        var assetClassDoc = new assetClass();
                        var pictureDoc = new picture();

                        assetDoc.assetId = item.i_assetId;
                        assetDoc.locationId = item.i_locationId;
                        assetDoc.assetClassId = item.i_assetClassId;
                        assetDoc.rfidTag = item.vc_rfidTag;
                        assetDoc.serialNumber = item.vc_serialNumber;

                        //Get the Light Status
                        var flag = await _db.as_assetStatusProfile.Where(q => q.i_assetProfileId == item.i_assetId)
                        .OrderByDescending(q => q.dt_lastUpdated).CountAsync();

                        if (flag > 0)
                        {

                            assetDoc.status = await
                                _db.as_assetStatusProfile.Where(q => q.i_assetProfileId == item.i_assetId)
                                    .OrderByDescending(q => q.dt_lastUpdated)
                                    .Select(q => q.bt_assetStatus)
                                    .FirstOrDefaultAsync();
                        }
                        else assetDoc.status = false;

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
                            var mainArea = _db.as_areaSubProfile.Where(q => q.i_areaSubId == location.i_areaSubId).Select(q => q.i_areaId).FirstOrDefault();
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
                    var collection = Database.GetCollection<mongoAssetProfile>("md_assetProfile");
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

                    var assetArray = new mongoAssetProfile[assets.Count()];
                    var i = 0;

                    foreach (var item in assets)
                    {
                        //Create different docuemnts 
                        var assetDoc = new mongoAssetProfile();
                        var locationDoc = new location();
                        var assetClassDoc = new assetClass();
                        var pictureDoc = new picture();

                        assetDoc.assetId = item.i_assetId;
                        assetDoc.locationId = item.i_locationId;
                        assetDoc.assetClassId = item.i_assetClassId;
                        assetDoc.rfidTag = item.vc_rfidTag;
                        assetDoc.serialNumber = item.vc_serialNumber;

                        var flag = await _db.as_assetStatusProfile.Where(q => q.i_assetProfileId == item.i_assetId)
                        .OrderByDescending(q => q.dt_lastUpdated).CountAsync();

                        if (flag > 0)
                        {

                            assetDoc.status = await
                                _db.as_assetStatusProfile.Where(q => q.i_assetProfileId == item.i_assetId)
                                    .OrderByDescending(q => q.dt_lastUpdated)
                                    .Select(q => q.bt_assetStatus)
                                    .FirstOrDefaultAsync();
                        }
                        else assetDoc.status = false;

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
                            var mainArea = _db.as_areaSubProfile.Where(q => q.i_areaSubId == location.i_areaSubId).Select(q => q.i_areaId).FirstOrDefault();
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
                    var collection = Database.GetCollection<mongoAssetProfile>("md_assetProfile");
                    await collection.DeleteManyAsync(filter);

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
            try
            {
                var assets = from x in _db.as_assetProfile
                             join y in _db.as_assetClassProfile on x.i_assetClassId equals y.i_assetClassId
                             select new
                             {
                                 x.i_assetId, x.i_locationId, x.i_assetClassId, x.vc_rfidTag, x.vc_serialNumber,
                                 productUrl = y.vc_webSiteLink
                             };

                var assetArray = new mongoAssetProfile[assets.Count()];
                var i = 0;

                foreach (var item in assets)
                {
                    //Create different docuemnts 
                    var assetDoc = new mongoAssetProfile();
                    var locationDoc = new location();
                    var assetClassDoc = new assetClass();
                    var pictureDoc = new picture();

                    assetDoc.assetId = item.i_assetId;
                    assetDoc.locationId = item.i_locationId;
                    assetDoc.assetClassId = item.i_assetClassId;
                    assetDoc.rfidTag = item.vc_rfidTag;
                    assetDoc.serialNumber = item.vc_serialNumber;

                    //Get the Light Status

                    var flag = await _db.as_assetStatusProfile.Where(q => q.i_assetProfileId == item.i_assetId)
                        .OrderByDescending(q => q.dt_lastUpdated).CountAsync();

                    if (flag > 0)
                    {

                        assetDoc.status = await
                            _db.as_assetStatusProfile.Where(q => q.i_assetProfileId == item.i_assetId)
                                .OrderByDescending(q => q.dt_lastUpdated)
                                .Select(q => q.bt_assetStatus)
                                .FirstOrDefaultAsync();
                    }
                    else assetDoc.status = false;

                    assetDoc.productUrl = item.productUrl;
                    assetDoc.maintenance = _func.GetMaintenaceTasks(item.i_assetId);

                    //get data for loaction
                    var location = await _db.as_locationProfile.FirstOrDefaultAsync(q => q.i_locationId == item.i_locationId);
                    if (location != null)
                    {
                        locationDoc.locationId = location.i_locationId;
                        locationDoc.longitude = location.f_longitude;
                        locationDoc.latitude = location.f_latitude;
                        locationDoc.designation = location.vc_designation;
                        locationDoc.areaSubId = location.i_areaSubId;

                        //Add Main Area
                        var mainArea = await _db.as_areaSubProfile.Where(q => q.i_areaSubId == location.i_areaSubId).Select(q => q.i_areaId).FirstOrDefaultAsync();
                        locationDoc.areaId = mainArea;
                    }

                    //Add doc to main doc
                    assetDoc.location = locationDoc;

                    //get asset class data
                    var assetclass = await _db.as_assetClassProfile.FirstOrDefaultAsync(q => q.i_assetClassId == item.i_assetClassId);
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
                        var picture = await _db.as_pictureProfile.FirstOrDefaultAsync(q => q.i_pictureId == assetclass.i_pictureId);
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

                await Database.GetCollection<mongoAssetProfile>("md_assetProfile").InsertManyAsync(assetArray);

                //Check the maintenance Cycles
                await RecreateCurrentPreviousStatus(null, null);
                return true;
                
            }
            catch (Exception err)
            {
                if (err.InnerException != null)
                    Log("Failed to rebuild asset profile" + err.InnerException.Message, "rebuildAssetProfile", LogTypes.Error, "SYSTEM");
                else
                    Log("Failed to rebuild asset profile" + err.Message, "rebuildAssetProfile", LogTypes.Error, "SYSTEM");

                return false;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        private async Task<bool> RecreateCurrentPreviousStatus(bool? assetFlag, int? id)
        {
            var lastValid = DateTime.Parse("2300/01/01");
            lastValid = DateTime.SpecifyKind(lastValid, DateTimeKind.Utc);
            var coll1 = Database.GetCollection<mongoAssetProfile>("md_assetProfile");
            var coll2 = Database.GetCollection<MongoCurrentPreviousStatus>("md_maintenanceTracking");
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
                assets = await coll1.Find(q => q.assetId != 0).ToListAsync();

            foreach(var item in assets)
            {
                foreach (var task in item.maintenance)
                {

                    var maintenance = await coll2.Find(q => q.assetId == item.assetId && q.maintenanceId == task.maintenanceId && q.lastValid == lastValid).FirstOrDefaultAsync();

                    if (maintenance != null)
                    {
                        if (task.maintenanceCycle != maintenance.previousCycle)
                        {
                            var filter = new BsonDocument("Id", maintenance.Id);

                            var update = Builders<MongoCurrentPreviousStatus>.Update.Set(q => q.lastValid, DateTime.UtcNow);

                            await coll2.FindOneAndUpdateAsync(filter, update);

                            //Create new record
                            var assetDoc = new MongoCurrentPreviousStatus
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
                        var assetDoc = new MongoCurrentPreviousStatus
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
            var settings = new List<MongoCurrentPreviousStatus>();
            try
            {
                var collection = Database.GetCollection<MongoCurrentPreviousStatus>("md_maintenanceTracking");
                var filter = new BsonDocument();
                

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
            catch (Exception)
            {
                return settings;
            }
        }
        
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //Converted to new Mongo API 2015/08/11
        public async Task<mongoEmailSettings> GetEmailSettings()
        {
            var collection = Database.GetCollection<mongoEmailSettings>("md_emailSettings");
            var filter = new BsonDocument();
            var settings = new mongoEmailSettings();
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
                var mdSettings = new mongoEmailSettings
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
                var collection =
                    Database.GetCollection<mongoAssetProfile>("md_assetProfile");

                var assets = await collection.Find(q => q.assetId == assetId).ToListAsync();

                var asset = assets.FirstOrDefault();
                if (asset != null)
                {
                    var task = asset.maintenance.FirstOrDefault();

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
                var collection = Database.GetCollection<mongoAssetProfile>("md_assetProfile");

                var assets = await collection.Find(q => q.assetId == assetId).ToListAsync();

                var asset = assets.FirstOrDefault();
                if (asset != null)
                {
                    var task = asset.maintenance.FirstOrDefault();

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
            var collection = Database.GetCollection<mongoAssetDownload>("md_assetDownload");
            var filter = new BsonDocument();
            var assets = new List<mongoAssetDownload>();
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
            var assets = new List<mongoAssetClassDownload>();
            try
            {
                var collection =
                    Database.GetCollection<mongoAssetClassDownload>("md_assetClassDownload");
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
        public async Task<List<mongoFullAsset>> GetAllAssetDownload()
        {
            var assets = new List<mongoFullAsset>();
            try
            {
                var collection = Database.GetCollection<mongoFullAsset>("md_assetFullDownload");
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
                var assets = new List<mongoAssetProfile>();
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

        public async Task<List<mongoAssetProfile>> GetAssetInfo(int id)
        {
            try
            {
                var collection = Database.GetCollection<mongoAssetProfile>("md_assetProfile");
                var filter = Builders<mongoAssetProfile>.Filter.Eq("assetId", id); 
                return await collection.Find(filter).ToListAsync();
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
                var collection = Database.GetCollection<mongoLogHelper>("md_logProfile");
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
                var log = new mongoLogHelper
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
                var log = new mongoLogHelper
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
            var log = new mongoLogHelper
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
