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

using ADB.AirSide.Encore.V1.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ADB.AirSide.Encore.V1.App_Helpers
{
    public class CacheHelper : IDisposable
    {
        //Mongo Globals
        private static string connectionString = Settings.MongoDBServer;
        
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        private static MongoClient client = new MongoClient(connectionString);
        
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        private static MongoServer server = client.GetServer();

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        private static MongoDatabase database = server.GetDatabase(Settings.MongoDBDatabase);

        //SQL Entity Globals
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        private Entities db = new Entities();

        #region Cache Rebuild



        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public Boolean createAssetClassDownloadCache()
        {
            try
            {
                List<assetClassDownload> assetList = new List<assetClassDownload>();
                DatabaseHelper func = new DatabaseHelper();

                var assets = (from x in db.as_assetClassProfile
                              select x);

                BsonArray assetArray = new BsonArray();

                foreach (var item in assets)
                {
                    BsonDocument asset = new BsonDocument();
                    asset.Add("i_assetClassId", item.i_assetClassId);
                    asset.Add("vc_description", item.vc_description);
                    asset.Add("i_assetCheckTypeId", 0);
                    asset.Add("assetCheckCount", func.getNumberOfFixingPoints(item.i_assetClassId));
                    assetArray.Add(asset);
                }

                //Drop Existing
                database.DropCollection("md_assetClassDownload");

                //Recreate New
                MongoCollection collection = database.GetCollection<mongoAssetClassDownload>("md_assetClassDownload");
                collection.InsertBatch(assetArray);

                return true;
            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.log("Failed to create Asset Class Download Cache: " + err.Message, "createAssetClassDownloadCache(iOS)", Logging.logTypes.Error, "SYSTEM");
                return false;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public Boolean createAllAssetDownload()
        {
            try
            {
                DatabaseHelper func = new DatabaseHelper();

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



                BsonArray assetArray = new BsonArray();

                foreach (var item in assets)
                {
                    Boolean lightStatus = false;
                    var status = db.as_assetStatusProfile.Where(q => q.i_assetProfileId == item.assetId).FirstOrDefault();
                    if (status != null)
                        lightStatus = status.bt_assetStatus;

                    BsonDocument asset = new BsonDocument();
                    asset.Add("assetId", item.assetId);
                    asset.Add("serialNumber", item.serialNumber);
                    asset.Add("firstMaintainedDate", func.getFirstMaintanedDate(item.assetId).ToString("yyyMMdd"));
                    asset.Add("lastMaintainedDate", func.getLastMaintanedDate(item.assetId).ToString("yyyMMdd"));
                    asset.Add("nextMaintenanceDate", func.getNextMaintenanceDate(item.assetId).ToString("yyyMMdd"));
                    asset.Add("latitude", item.latitude);
                    asset.Add("longitude", item.longitude);
                    asset.Add("subAreaId", item.subAreaId);
                    asset.Add("assetClassId", item.assetClassId);
                    asset.Add("rfidTag", item.rfidTag);
                    asset.Add("status", lightStatus);

                    assetArray.Add(asset);
                }

                //Drop Existing
                database.DropCollection("md_assetFullDownload");

                //Recreate New
                MongoCollection collection = database.GetCollection<mongoFullAsset>("md_assetFullDownload");
                collection.InsertBatch(assetArray);

                return true;
            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.log("Failed to create Asset Full Download Cache: " + err.Message, "createAllAssetDownload(iOS)", Logging.logTypes.Error, "SYSTEM");
                return false;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public Boolean createAssetDownloadCache()
        {
            try
            {
                BsonArray assetList = new BsonArray();
                DatabaseHelper func = new DatabaseHelper();

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

                foreach (var item in assets)
                {
                    BsonDocument asset = new BsonDocument();
                    asset.Add("i_assetId", item.i_assetId);
                    asset.Add("i_assetClassId", item.i_assetClassId);
                    asset.Add("vc_tagId", item.vc_rfidTag);
                    asset.Add("vc_serialNumber", item.vc_serialNumber);
                    asset.Add("i_locationId", item.i_locationId);
                    asset.Add("i_areaSubId", item.i_areaSubId);
                    asset.Add("longitude", item.f_longitude);
                    asset.Add("latitude", item.f_latitude);
                    asset.Add("lastDate", func.getLastShiftDateForAsset(item.i_assetId));
                    asset.Add("maintenance", "0");
                    asset.Add("submitted", func.getSubmittedShiftData(item.i_assetId));
                    assetList.Add(asset);
                }

                //Drop Existing
                database.DropCollection("md_assetDownload");

                //Recreate New
                MongoCollection collection = database.GetCollection<mongoAssetDownload>("md_assetDownload");
                collection.InsertBatch(assetList);

                return true;
            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.log("Failed to create Asset Download Cache: " + err.Message, "createAssetDownloadCache(iOS)", Logging.logTypes.Error, "SYSTEM");
                return false;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public void rebuildShiftAgregation()
        {
            try
            {
                BsonArray shiftArray = new BsonArray();
                DatabaseHelper dbHelper = new DatabaseHelper();
                var shifts = (from x in db.as_shifts
                              join y in db.as_areaSubProfile on x.i_areaSubId equals y.i_areaSubId
                              join z in db.as_areaProfile on y.i_areaId equals z.i_areaId
                              join a in db.as_technicianGroups on x.UserId equals a.i_groupId
                              where x.bt_completed == false
                              select new
                              {
                                  i_shiftId = x.i_shiftId,
                                  sheduledDate = x.dt_scheduledDate,
                                  i_areaSubId = x.i_areaSubId,
                                  sheduleTime = x.dt_scheduledDate,
                                  permitNumber = x.vc_permitNumber,
                                  techGroup = a.vc_groupName,
                                  areaName = z.vc_description,
                                  techGroupId = a.i_groupId
                              }
                              ).ToList();
                foreach (var item in shifts)
                {
                    BsonDocument shiftsCollection = new BsonDocument();
                    shiftsCollection.Add("i_shiftId", item.i_shiftId);
                    shiftsCollection.Add("sheduledDate", item.sheduledDate.ToString("yyy/MM/dd"));
                    shiftsCollection.Add("i_areaSubId", item.i_areaSubId);
                    shiftsCollection.Add("sheduleTime", item.sheduleTime.ToString("hh:mm:ss"));
                    shiftsCollection.Add("permitNumber", item.permitNumber);
                    shiftsCollection.Add("techGroup", item.techGroup);
                    shiftsCollection.Add("areaName", item.areaName);
                    shiftsCollection.Add("techGroupId", item.techGroupId);

                    shiftArray.Add(shiftsCollection);
                }

                //Drop Existing
                database.DropCollection("md_shiftdata");

                //Recreate New
                MongoCollection collection = database.GetCollection<mongoAssetDownload>("md_shiftdata");
                collection.InsertBatch(shiftArray);
            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.log("Failed to rebuild shift aggregation: " + err.Message, "rebuildShiftAgregation", Logging.logTypes.Error, "SYSTEM");
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public void rebuildUserProfile()
        {
            try
            {
                BsonArray usersArray = new BsonArray();
                var users = db.UserProfiles.ToList();
                foreach (var item in users)
                {
                    var groupId = (from x in db.as_technicianGroupProfile
                                   where x.UserId == item.UserId
                                   select x.i_currentGroup).DefaultIfEmpty(0).First();
                    Guid sessionkey = Guid.NewGuid();

                    BsonDocument userCollection = new BsonDocument();
                    userCollection.Add("Username", item.UserName);
                    userCollection.Add("FirstName", item.FirstName);
                    userCollection.Add("LastName", item.LastName);
                    userCollection.Add("UserId", item.UserId);
                    userCollection.Add("i_accessLevel", item.i_accessLevelId);
                    userCollection.Add("i_airPortId", item.i_airPortId);
                    userCollection.Add("SessionKey", sessionkey.ToString());
                    userCollection.Add("i_groupId", groupId);

                    usersArray.Add(userCollection);
                }

                //Drop Existing
                database.DropCollection("md_usersprofile");

                //Recreate New
                MongoCollection collection = database.GetCollection<mongoAssetDownload>("md_usersprofile");
                collection.InsertBatch(usersArray);
            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.log("Failed to rebuild users: " + err.Message, "rebuildUserProfile", Logging.logTypes.Error, "SYSTEM");
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public void rebuildWrenchProfile()
        {
            try
            {
                BsonArray wrenchArray = new BsonArray();
                DatabaseHelper dbHelper = new DatabaseHelper();

                List<as_wrenchProfile> wrenchList = new List<as_wrenchProfile>();

                wrenchList = (from data in db.as_wrenchProfile select data).ToList();

                foreach (as_wrenchProfile item in wrenchList)
                {
                    BsonDocument wrenchCollection = new BsonDocument();
                    wrenchCollection.Add("bt_active", item.bt_active);
                    wrenchCollection.Add("dt_lastCalibrated", item.dt_lastCalibrated.ToString("yyyyMMdd"));
                    wrenchCollection.Add("f_batteryLevel", item.f_batteryLevel);
                    wrenchCollection.Add("i_calibrationCycle", item.i_calibrationCycle);
                    wrenchCollection.Add("i_wrenchId", item.i_wrenchId);
                    wrenchCollection.Add("vc_model", item.vc_model);
                    wrenchCollection.Add("vc_serialNumber", item.vc_serialNumber);

                    wrenchArray.Add(wrenchCollection);
                }

                //Drop Existing
                database.DropCollection("md_wrenchprofile");

                //Recreate New
                MongoCollection collection = database.GetCollection<mongoAssetDownload>("md_wrenchprofile");
                collection.InsertBatch(wrenchArray);
            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.log("Failed to rebuild wrench profile: " + err.Message, "rebuildWrenchProfile", Logging.logTypes.Error, "SYSTEM");
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
                
        public void rebuildTechnicianGroups()
        {
            try
            {
                BsonArray techArray = new BsonArray();
                DatabaseHelper dbHelper = new DatabaseHelper();

                var technicians = (from x in db.UserProfiles
                                   join y in db.as_technicianGroupProfile on x.UserId equals y.UserId
                                   where x.i_accessLevelId == 3
                                   select new
                                   {
                                       x.UserId,
                                       x.FirstName,
                                       x.LastName,
                                       x.UserName,
                                       y.i_currentGroup,
                                       y.i_defaultGroup,
                                   }).ToList();

                foreach (var item in technicians)
                {
                    BsonDocument techGroupCollection = new BsonDocument();
                    techGroupCollection.Add("UserId", item.UserId);
                    techGroupCollection.Add("FirstName", item.FirstName);
                    techGroupCollection.Add("LastName", item.LastName);
                    techGroupCollection.Add("UserName", item.UserName);
                    techGroupCollection.Add("i_currentGroup", item.i_currentGroup);
                    techGroupCollection.Add("i_defaultGroup", item.i_defaultGroup);

                    techArray.Add(techGroupCollection);
                }

                //Drop Existing
                database.DropCollection("md_techgroups");

                //Recreate New
                MongoCollection collection = database.GetCollection<mongoAssetDownload>("md_techgroups");
                collection.InsertBatch(techArray);
            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.log("Failed to rebuild technician cache: " + err.Message, "rebuildTechnicianGroups", Logging.logTypes.Error, "SYSTEM");
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public void rebuildAssetProfileForAssetClass(int assetClassId)
        {
            try
            {
                var assets = db.as_assetProfile.Where(q => q.i_assetClassId == assetClassId);
                BsonArray assetArray = new BsonArray();
                DatabaseHelper dbHelper = new DatabaseHelper();

                foreach (as_assetProfile item in assets)
                {
                    //Create different docuemnts 
                    BsonDocument assetDoc = new BsonDocument();
                    BsonDocument locationDoc = new BsonDocument();
                    BsonDocument assetClassDoc = new BsonDocument();
                    BsonDocument frequencyDoc = new BsonDocument();
                    BsonDocument pictureDoc = new BsonDocument();

                    assetDoc.Add("assetId", item.i_assetId);
                    assetDoc.Add("locationId", item.i_locationId);
                    assetDoc.Add("assetClassId", item.i_assetClassId);
                    assetDoc.Add("rfidTag", item.vc_rfidTag);
                    assetDoc.Add("serialNumber", item.vc_serialNumber);
                    assetDoc.Add("maintenance", dbHelper.getMaintenaceTasks(item.i_assetId));

                    //Get the Light Status
                    if (db.as_assetStatusProfile.Find(item.i_assetId) != null)
                        assetDoc.Add("status", db.as_assetStatusProfile.Where(q => q.i_assetProfileId == item.i_assetId).Select(q => q.bt_assetStatus).FirstOrDefault());
                    else
                        assetDoc.Add("status", false);

                    //get data for loaction
                    var location = db.as_locationProfile.Where(q => q.i_locationId == item.i_locationId).FirstOrDefault();
                    locationDoc.Add("locationId", location.i_locationId);
                    locationDoc.Add("longitude", location.f_longitude);
                    locationDoc.Add("latitude", location.f_latitude);
                    locationDoc.Add("designation", location.vc_designation);
                    locationDoc.Add("areaSubId", location.i_areaSubId);

                    //Add Main Area
                    int mainArea = db.as_areaSubProfile.Where(q => q.i_areaSubId == location.i_areaSubId).Select(q => q.i_areaId).FirstOrDefault();
                    locationDoc.Add("areaId", mainArea);

                    //Add doc to main doc
                    assetDoc.Add("location", locationDoc);

                    //get asset class data
                    var assetclass = db.as_assetClassProfile.Where(q => q.i_assetClassId == item.i_assetClassId).FirstOrDefault();
                    assetClassDoc.Add("assetClassId", assetclass.i_assetClassId);
                    assetClassDoc.Add("description", assetclass.vc_description);
                    assetClassDoc.Add("pictureId", assetclass.i_pictureId);
                    assetClassDoc.Add("manufacturer", assetclass.vc_manufacturer);
                    assetClassDoc.Add("model", assetclass.vc_model);
                    assetClassDoc.Add("frequencyId", assetclass.i_frequencyId);

                    //add to main doc
                    assetDoc.Add("assetClass", assetClassDoc);

                    //get frequency data
                    var frequency = db.as_frequencyProfile.Where(q => q.i_frequencyId == assetclass.i_frequencyId).FirstOrDefault();
                    frequencyDoc.Add("frequencyId", frequency.i_frequencyId);
                    frequencyDoc.Add("description", frequency.vc_description);
                    frequencyDoc.Add("frequencyValue", frequency.f_frequency);

                    //add to main doc
                    assetDoc.Add("frequency", frequencyDoc);

                    //get picture data
                    var picture = db.as_pictureProfile.Where(q => q.i_pictureId == assetclass.i_pictureId).FirstOrDefault();
                    pictureDoc.Add("pictureId", picture.i_pictureId);
                    pictureDoc.Add("fileLocation", picture.vc_fileLocation);
                    pictureDoc.Add("description", picture.vc_description);

                    //add to main doc
                    assetDoc.Add("picture", pictureDoc);

                    assetArray.Add(assetDoc);
                }

                //Drop Current Profile
                var query = Query<mongoAssetProfile>.EQ(q => q.assetClass.assetClassId, assetClassId);
                MongoCollection collection = database.GetCollection<mongoAssetProfile>("md_assetProfile");
                collection.Remove(query);

                //Insert Updated records
                collection.InsertBatch(assetArray);
            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.log("Failed to rebuild asset profile for asset class: " + err.Message, "rebuildAssetProfileForAssetClass", Logging.logTypes.Error, "SYSTEM");
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public void rebuildAssetProfileForAsset(int assetId)
        {
            try
            {
                var asset = db.as_assetProfile.Find(assetId);
                BsonArray assetArray = new BsonArray();
                DatabaseHelper dbHelper = new DatabaseHelper();

                //Create different docuemnts 
                BsonDocument assetDoc = new BsonDocument();
                BsonDocument locationDoc = new BsonDocument();
                BsonDocument assetClassDoc = new BsonDocument();
                BsonDocument frequencyDoc = new BsonDocument();
                BsonDocument pictureDoc = new BsonDocument();

                assetDoc.Add("assetId", asset.i_assetId);
                assetDoc.Add("locationId", asset.i_locationId);
                assetDoc.Add("assetClassId", asset.i_assetClassId);
                assetDoc.Add("rfidTag", asset.vc_rfidTag);
                assetDoc.Add("serialNumber", asset.vc_serialNumber);
                assetDoc.Add("maintenance", dbHelper.getMaintenaceTasks(assetId));

                //Get the Light Status
                if (db.as_assetStatusProfile.Find(asset.i_assetId) != null)
                    assetDoc.Add("status", db.as_assetStatusProfile.Where(q => q.i_assetProfileId == asset.i_assetId).Select(q => q.bt_assetStatus).FirstOrDefault());
                else
                    assetDoc.Add("status", false);

                //get data for loaction
                var location = db.as_locationProfile.Where(q => q.i_locationId == asset.i_locationId).FirstOrDefault();
                locationDoc.Add("locationId", location.i_locationId);
                locationDoc.Add("longitude", location.f_longitude);
                locationDoc.Add("latitude", location.f_latitude);
                locationDoc.Add("designation", location.vc_designation);
                locationDoc.Add("areaSubId", location.i_areaSubId);

                //Add Main Area
                int mainArea = db.as_areaSubProfile.Where(q => q.i_areaSubId == location.i_areaSubId).Select(q => q.i_areaId).FirstOrDefault();
                locationDoc.Add("areaId", mainArea);

                //Add doc to main doc
                assetDoc.Add("location", locationDoc);

                //get asset class data
                var assetclass = db.as_assetClassProfile.Where(q => q.i_assetClassId == asset.i_assetClassId).FirstOrDefault();
                assetClassDoc.Add("assetClassId", assetclass.i_assetClassId);
                assetClassDoc.Add("description", assetclass.vc_description);
                assetClassDoc.Add("pictureId", assetclass.i_pictureId);
                assetClassDoc.Add("manufacturer", assetclass.vc_manufacturer);
                assetClassDoc.Add("model", assetclass.vc_model);
                assetClassDoc.Add("frequencyId", assetclass.i_frequencyId);

                //add to main doc
                assetDoc.Add("assetClass", assetClassDoc);

                //get frequency data
                var frequency = db.as_frequencyProfile.Where(q => q.i_frequencyId == assetclass.i_frequencyId).FirstOrDefault();
                frequencyDoc.Add("frequencyId", frequency.i_frequencyId);
                frequencyDoc.Add("description", frequency.vc_description);
                frequencyDoc.Add("frequencyValue", frequency.f_frequency);

                //add to main doc
                assetDoc.Add("frequency", frequencyDoc);

                //get picture data
                var picture = db.as_pictureProfile.Where(q => q.i_pictureId == assetclass.i_pictureId).FirstOrDefault();
                pictureDoc.Add("pictureId", picture.i_pictureId);
                pictureDoc.Add("fileLocation", picture.vc_fileLocation);
                pictureDoc.Add("description", picture.vc_description);

                //add to main doc
                assetDoc.Add("picture", pictureDoc);

                assetArray.Add(assetDoc);

                //Drop Current Profile
                var query = Query<mongoAssetProfile>.EQ(q => q.assetId, assetId);
                MongoCollection collection = database.GetCollection<mongoAssetProfile>("md_assetProfile");
                collection.Remove(query);

                //Insert Updated Asset
                collection.InsertBatch(assetArray);
            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.log("Failed to rebuild Asset Profile for asset: " + err.Message, "rebuildAssetProfileForAsset", Logging.logTypes.Error, "SYSTEM");
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public void rebuildAssetProfile()
        {
            int assetID = 0;
            try
            {
                var assets = db.as_assetProfile;
                BsonArray assetArray = new BsonArray();
                DatabaseHelper dbHelper = new DatabaseHelper();

                foreach (as_assetProfile item in assets)
                {
                    //Create different docuemnts 
                    BsonDocument assetDoc = new BsonDocument();
                    BsonDocument locationDoc = new BsonDocument();
                    BsonDocument assetClassDoc = new BsonDocument();
                    BsonDocument frequencyDoc = new BsonDocument();
                    BsonDocument pictureDoc = new BsonDocument();

                    assetID = item.i_assetId;

                    assetDoc.Add("assetId", item.i_assetId);
                    assetDoc.Add("locationId", item.i_locationId);
                    assetDoc.Add("assetClassId", item.i_assetClassId);
                    assetDoc.Add("rfidTag", item.vc_rfidTag);
                    assetDoc.Add("serialNumber", item.vc_serialNumber);

                    //Get the Light Status
                    if (db.as_assetStatusProfile.Find(item.i_assetId) != null)
                        assetDoc.Add("status", db.as_assetStatusProfile.Where(q => q.i_assetProfileId == item.i_assetId).Select(q => q.bt_assetStatus).FirstOrDefault());
                    else
                        assetDoc.Add("status", false);

                    assetDoc.Add("maintenance", dbHelper.getMaintenaceTasks(item.i_assetId));
                    
                    //get data for loaction
                    var location = db.as_locationProfile.Where(q => q.i_locationId == item.i_locationId).FirstOrDefault();
                    locationDoc.Add("locationId", location.i_locationId);
                    locationDoc.Add("longitude", location.f_longitude);
                    locationDoc.Add("latitude", location.f_latitude);
                    locationDoc.Add("designation", location.vc_designation);
                    locationDoc.Add("areaSubId", location.i_areaSubId);

                    //Add Main Area
                    int mainArea = db.as_areaSubProfile.Where(q => q.i_areaSubId == location.i_areaSubId).Select(q => q.i_areaId).FirstOrDefault();
                    locationDoc.Add("areaId", mainArea);

                    //Add doc to main doc
                    assetDoc.Add("location", locationDoc);

                    //get asset class data
                    var assetclass = db.as_assetClassProfile.Where(q => q.i_assetClassId == item.i_assetClassId).FirstOrDefault();
                    assetClassDoc.Add("assetClassId", assetclass.i_assetClassId);
                    assetClassDoc.Add("description", assetclass.vc_description);
                    assetClassDoc.Add("pictureId", assetclass.i_pictureId);
                    assetClassDoc.Add("manufacturer", assetclass.vc_manufacturer);
                    assetClassDoc.Add("model", assetclass.vc_model);
                    assetClassDoc.Add("frequencyId", assetclass.i_frequencyId);

                    //add to main doc
                    assetDoc.Add("assetClass", assetClassDoc);

                    //get frequency data
                    var frequency = db.as_frequencyProfile.Where(q => q.i_frequencyId == assetclass.i_frequencyId).FirstOrDefault();
                    frequencyDoc.Add("frequencyId", frequency.i_frequencyId);
                    frequencyDoc.Add("description", frequency.vc_description);
                    frequencyDoc.Add("frequencyValue", frequency.f_frequency);

                    //add to main doc
                    assetDoc.Add("frequency", frequencyDoc);

                    //get picture data
                    var picture = db.as_pictureProfile.Where(q => q.i_pictureId == assetclass.i_pictureId).FirstOrDefault();
                    pictureDoc.Add("pictureId", picture.i_pictureId);
                    pictureDoc.Add("fileLocation", picture.vc_fileLocation);
                    pictureDoc.Add("description", picture.vc_description);

                    //add to main doc
                    assetDoc.Add("picture", pictureDoc);

                    assetArray.Add(assetDoc);
                }

                //Drop Current Profile
                database.DropCollection("md_assetProfile");
                MongoCollection collection = database.GetCollection<mongoAssetProfile>("md_assetProfile");
                collection.InsertBatch(assetArray);
            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.log("Failed to rebuild asset profile:(" + assetID.ToString() + ") " + err.InnerException.Message, "rebuildAssetProfile", Logging.logTypes.Error, "SYSTEM");
            }
        }

        #endregion

        #region Cache Queries

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public mongoEmailSettings getEmailSettings()
        {
            MongoCollection collection = database.GetCollection<mongoEmailSettings>("md_emailSettings");
            mongoEmailSettings settings = (from x in collection.AsQueryable<mongoEmailSettings>()
                                           select x).FirstOrDefault();
            if(settings == null)
            {
                //create default settings if it doesn't exist
                BsonDocument mdSettings = new BsonDocument();
                mdSettings.Add("apiKey", "key-82e66599b538527a71b035abfcd0a0ae");
                mdSettings.Add("domain", "adb-airside.com");
                mdSettings.Add("fromAddress", "info@adb-airside.com");
                collection.Insert(mdSettings);
                
                //retry
                settings = (from x in collection.AsQueryable<mongoEmailSettings>()
                            select x).FirstOrDefault();
            }
            return settings;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public List<mongoAssetDownload> getAssetAssosiations(int areaSubId)
        {
            MongoCollection collection = database.GetCollection<mongoAssetDownload>("md_assetDownload");
            List<mongoAssetDownload> assets = (from x in collection.AsQueryable<mongoAssetDownload>()
                                               where x.i_areaSubId == areaSubId
                                               select x).ToList();
            return assets;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public List<mongoAssetClassDownload> getAllAssetClasses()
        {
            MongoCollection collection = database.GetCollection<mongoAssetClassDownload>("md_assetClassDownload");
            List<mongoAssetClassDownload> assets = (from x in collection.AsQueryable<mongoAssetClassDownload>()
                                                    select x).ToList();
            return assets;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public List<mongoFullAsset> getAllAssetDownload()
        {
            MongoCollection collection = database.GetCollection<mongoFullAsset>("md_assetFullDownload");
            List<mongoFullAsset> assets = (from x in collection.AsQueryable<mongoFullAsset>()
                                           select x).ToList();
            return assets;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public List<mongoAssetProfile> getAllAssets()
        {
            try
            {
                MongoCollection collection = database.GetCollection<mongoAssetProfile>("md_assetProfile");
                List<mongoAssetProfile> assets = (from x in collection.AsQueryable<mongoAssetProfile>() select x).ToList();
                return assets;
            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.log("Failed to retrive all asset profiles from Mongo: " + err.Message, "getAllAssets", Logging.logTypes.Error, "SYSTEM");
                return null;
            }
        }

        #endregion

        #region Logging

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public Boolean writeLog(mongoLogging log)
        {
            try
            {
                MongoCollection collection = database.GetCollection<mongoLogging>("md_logProfile");
                collection.Insert(log);
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
                Logging log = new Logging();
                log.logError(err, "SYSTEM");
            }
        
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