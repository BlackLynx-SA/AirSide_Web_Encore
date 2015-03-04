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
// SUMMARY: This class contains all controller calls for Asset Classes
#endregion

using ADB.AirSide.Encore.V1.Models;
using AirSide.ServerModules.Helpers;
using AirSide.ServerModules.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ADB.AirSide.Encore.V1.Controllers
{
    [Authorize]
    public class AssetClassController : Controller
    {
        private Entities db = new Entities();

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public ActionResult AssetClasses()
        {
            return View();
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult getAllMaintenanceValidators()
        {
            try
            {
                var validators = db.as_maintenanceValidation.ToList();
                return Json(validators);
            }
            catch (Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to retrieve validation types: " + err.Message, "getAllMaintenanceValidators", LogHelper.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult removeTaskAssosiation(int assetMaintenanceId)
        {
            try
            {
                var assosiation = db.as_assetClassMaintenanceProfile.Find(assetMaintenanceId);
                int assetClassId = assosiation.i_assetClassId;
                db.as_assetClassMaintenanceProfile.Remove(assosiation);
                db.SaveChanges();

                //Rebuild cache
                CacheHelper cache = new CacheHelper();
                cache.rebuildAssetProfileForAssetClass(assetClassId);

                //update iOS Cache Hash
                cache.updateiOSCache("getAllAssetClasses");

                return Json(new { message = "Success" });
            }
            catch (Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to remove task assosiation: " + err.Message, "removeTaskAssosiation", LogHelper.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult getAssosiatedMaintenanceTasks(int assetClassId)
        {
            try
            {
                var assetMaintenance = (from x in db.as_assetClassMaintenanceProfile
                                        join y in db.as_frequencyProfile on x.i_frequencyId equals y.i_frequencyId
                                        join z in db.as_maintenanceProfile on x.i_maintenanceId equals z.i_maintenanceId
                                        join a in db.as_maintenanceValidation on z.i_maintenanceValidationId equals a.i_maintenanceValidationId
                                        where x.i_assetClassId == assetClassId
                                        select new
                                        {
                                            maintenanceTask = z.vc_description,
                                            frequency = y.f_frequency,
                                            maintenanceId = x.i_maintenanceId,
                                            validation = a.vc_validationName,
                                            assetMaintenanceId = x.i_assetMaintenanceId
                                        }).ToList();

                return Json(assetMaintenance);
            }
            catch (Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to retrieve Assosiated Maintenance Tasks: " + err.Message, "getAllMaintenanceValidators", LogHelper.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult insertMaintenanceTask(int assetClassId, int maintenanceId, int frequencyId)//, [System.Web.Http.FromUri] int[] validationIds)
        {
            try
            {
                //Get Frequency ID
                int freqId = db.as_frequencyProfile.Where(q => q.f_frequency == frequencyId).Select(q => q.i_frequencyId).FirstOrDefault();

                //Add Assosiation
                as_assetClassMaintenanceProfile newAssosiation = new as_assetClassMaintenanceProfile();
                newAssosiation.i_assetClassId = assetClassId;
                newAssosiation.i_frequencyId = freqId;
                newAssosiation.i_maintenanceId = maintenanceId;

                db.as_assetClassMaintenanceProfile.Add(newAssosiation);
                db.SaveChanges();

                //Build cache for newly added task
                CacheHelper cache = new CacheHelper();
                cache.rebuildAssetProfileForAssetClass(assetClassId);

                //update iOS Cache Hash
                cache.updateiOSCache("getAllAssetClasses");

                return Json(new { message = "Success"});
            }
            catch(Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to insert new maintenance task: " + err.Message, "insertMaintenanceTask", LogHelper.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult getAllMaintenanceTasks()
        {
            try
            {
                var tasks = (from x in db.as_maintenanceProfile 
                                 join y in db.as_maintenanceValidation on x.i_maintenanceValidationId equals y.i_maintenanceValidationId
                                 select new {
                                     vc_description = x.vc_description,
                                     i_maintenanceId = x.i_maintenanceId,
                                     i_maintenanceCategoryId = x.i_maintenanceCategoryId,
                                     vc_validation = y.vc_validationName,
                                     i_validationId = y.i_maintenanceValidationId
                                 }).ToList();
                return Json(tasks);
            }
            catch (Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to retrieve all maintenance tasks: " + err.Message, "getAllMaintenanceTasks", LogHelper.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult insertMaintenanceCategory(string name, string description, int type)
        {
            try
            {
                as_maintenanceCategory newCat = new as_maintenanceCategory();
                newCat.i_categoryType = type;
                newCat.vc_categoryDescription = description;
                newCat.vc_maintenanceCategory = name;

                db.as_maintenanceCategory.Add(newCat);
                db.SaveChanges();

                return Json(new {message = "Success" });
            }
            catch (Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to insert new maintenance category: " + err.Message, "insertMaintenanceCategory", LogHelper.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult editMaintenanceCategory(int id, string categoryName)
        {
            try
            {
                var category = db.as_maintenanceCategory.Find(id);
                category.vc_maintenanceCategory = categoryName;
                db.Entry(category).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();

                return Json(new { message = "Category successfully edited and saved."});
            }
            catch(Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to edit maintenance category: " + err.Message, "editMaintenanceCategory", LogHelper.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(new { message = err.Message });
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult editMaintenanceTask(int id, string taskName, string validationName)
        {
            try
            {
                var validation = db.as_maintenanceValidation.Where(q => q.vc_validationName == validationName).FirstOrDefault();
                var task = db.as_maintenanceProfile.Find(id);
                task.vc_description = taskName;
                task.i_maintenanceValidationId = validation.i_maintenanceValidationId;

                db.Entry(task).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();

                //update iOS Cache Hash
                CacheHelper cache = new CacheHelper();
                cache.updateiOSCache("getMaintenanceProfile");

                return Json(new { message = "Task successfully edited and saved." });
            }
            catch (Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to edit maintenance task: " + err.Message, "editMaintenanceTask", LogHelper.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(new { message = err.Message });
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult removeMaintenanceCategory(int id)
        {
            try
            {
                int maintenaceProfile = (from x in db.as_assetClassMaintenanceProfile
                                         join y in db.as_maintenanceProfile on x.i_maintenanceId equals y.i_maintenanceId
                                         join z in db.as_maintenanceCategory on y.i_maintenanceCategoryId equals z.i_maintenanceCategoryId
                                         where z.i_maintenanceCategoryId == id
                                         select x).Count();

                if (maintenaceProfile == 0)
                {
                    var category = db.as_maintenanceCategory.Find(id);
                    db.as_maintenanceCategory.Remove(category);
                    db.SaveChanges();
                    
                    //Remove All Tasks assosiated with the Category
                    var assosiatedTasks = db.as_maintenanceProfile.Where(q => q.i_maintenanceCategoryId == id);
                    foreach(var item in assosiatedTasks)
                    {
                        db.as_maintenanceProfile.Remove(item);
                        db.SaveChanges();
                    }

                    //update iOS Cache Hash
                    CacheHelper cache = new CacheHelper();
                    cache.updateiOSCache("getMaintenanceProfile");

                    Response.StatusCode = 200;
                    return Json(new { message = category.vc_maintenanceCategory + " category successfully removed" });

                } else
                {
                    Response.StatusCode = 301; //Set status so object exists
                    return Json(new { message = "The category contains elements that have assosiations with other objects. Please delete the tasks assosiated and try again."});
                }
            }
            catch(Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to remove maintenance category: " + err.Message, "removeMaintenanceCategory", LogHelper.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(new { message = err.Message });
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult removeMaintenanceTask(int id)
        {
            try
            {
                int maintenaceProfile = (from x in db.as_assetClassMaintenanceProfile
                                         join y in db.as_maintenanceProfile on x.i_maintenanceId equals y.i_maintenanceId
                                         where y.i_maintenanceId == id
                                         select x).Count();

                if (maintenaceProfile == 0)
                {
                    var task = db.as_maintenanceProfile.Find(id);
                    db.as_maintenanceProfile.Remove(task);
                    db.SaveChanges();

                    //update iOS Cache Hash
                    CacheHelper cache = new CacheHelper();
                    cache.updateiOSCache("getMaintenanceProfile");

                    Response.StatusCode = 200;
                    return Json(new { message = task.vc_description + " task successfully removed" });

                }
                else
                {
                    Response.StatusCode = 301; //Set status so object exists
                    return Json(new { message = "This task has been assosiated with a asset class. Please remove the assosiation and try again." });
                }
            }
            catch (Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to remove maintenance category: " + err.Message, "removeMaintenanceCategory", LogHelper.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult insertMaintenanceTaskType(string description, int catId, int validationId)
        {
            try
            {
                as_maintenanceProfile newTask = new as_maintenanceProfile();
                newTask.i_maintenanceCategoryId = catId;
                newTask.vc_description = description;
                newTask.i_maintenanceValidationId = validationId;

                db.as_maintenanceProfile.Add(newTask);
                db.SaveChanges();

                //update iOS Cache Hash
                CacheHelper cache = new CacheHelper();
                cache.updateiOSCache("getMaintenanceProfile");

                return Json(new { message = "Success" });
            }
            catch (Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to insert new maintenance task: " + err.Message, "insertMaintenanceTaskType", LogHelper.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult getAllMaintenanceCategories()
        {
            try
            {
                var categories = db.as_maintenanceCategory.ToList();
                return Json(categories);
            }
            catch(Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to retrieve all maintenance categories: " + err.Message, "getAllMaintenanceCategories", LogHelper.logTypes.Error, Request.UserHostAddress);
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
                var AssetClassList = (from x in db.as_assetClassProfile
                                      join y in db.as_pictureProfile on x.i_pictureId equals y.i_pictureId
                                      select new
                                      {
                                          assetClassId = x.i_assetClassId,
                                          assetDescription = x.vc_description,
                                          manufacturer = x.vc_manufacturer,
                                          pictureLocation = "../.." + y.vc_fileLocation,
                                          model = x.vc_model,
                                          manualURL = x.vc_webSiteLink
                                      }).ToList();
                return Json(AssetClassList);
            }
            catch(Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to retrieve all asset classes: " + err.Message, "getAllAssetClasses", LogHelper.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public ActionResult CreateAssetClass()
        {
            return View();
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public ActionResult EditAssetClass(int id)
        {
            try
            {
                var assetClass = db.as_assetClassProfile.Find(id);

                ViewBag.description = assetClass.vc_description;
                ViewBag.manufacturer = assetClass.vc_manufacturer;
                ViewBag.model = assetClass.vc_model;
                ViewBag.manualURL = assetClass.vc_webSiteLink;
                ViewBag.picture = assetClass.i_pictureId;
                ViewBag.assetClassId = assetClass.i_assetClassId;
                ViewBag.error = "";

                return View();
            }
            catch(Exception err)
            {
                ViewBag.description = "";
                ViewBag.manufacturer = "";
                ViewBag.model = "";
                ViewBag.picture = "";
                ViewBag.assetClassId = -1;
                ViewBag.error = "Failed to retrieve record: " + err.Message;
                return View();
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult deleteAssetClass(int id)
        {
            try
            {
                //Select the asset class to be deleted
                var assetClass = db.as_assetClassProfile.Find(id);

                //Check if this asset class is being used
                int assets = db.as_assetProfile.Where(q => q.i_assetClassId == assetClass.i_assetClassId).Count();

                //Remove Asset Class
                if (assets == 0)
                {
                    db.as_assetClassProfile.Remove(assetClass);
                    db.SaveChanges();

                    //update iOS Cache Hash
                    CacheHelper cache = new CacheHelper();
                    cache.updateiOSCache("getAllAssetClasses");

                    return Json(assetClass.vc_description);
                } else
                {
                    //Return error if asset class is assosiated with an asset
                    Response.StatusCode = 500;
                    return Json("You can't delete this asset class. It's currently assosiated with loaded assets. Please remove the assosiation first.");
                }
            }
            catch(Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to delete asset class: " + err.Message, "deleteAssetClass", LogHelper.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult insertUpdateAssetClass(string description, string manufacturer, string model, int pictureId, int assetClassId, string manualUrl)
        {
            try
            {
                //Create New Asset Class
                if(assetClassId == 0)
                {
                    //Validate Description
                    var existingAssetClass = db.as_assetClassProfile.Where(q => q.vc_description == description).FirstOrDefault();

                    if (existingAssetClass == null)
                    {
                        as_assetClassProfile newAssetClass = new as_assetClassProfile();
                        newAssetClass.i_pictureId = pictureId;
                        newAssetClass.vc_description = description;
                        newAssetClass.vc_manufacturer = manufacturer;
                        newAssetClass.vc_model = model;
                        newAssetClass.vc_webSiteLink = manualUrl;

                        db.as_assetClassProfile.Add(newAssetClass);
                        db.SaveChanges();

                        //update iOS Cache Hash
                        CacheHelper cache = new CacheHelper();
                        cache.updateiOSCache("getAllAssetClasses");

                        return Json(new { description = description, assetClassId = newAssetClass.i_assetClassId });
                    }
                    else
                    {
                        Response.StatusCode = 500;
                        return Json("This description exists in database.");
                    }
                }
                else
                {
                    var assetClass = db.as_assetClassProfile.Find(assetClassId);
                    assetClass.i_pictureId = pictureId;
                    assetClass.vc_description = description;
                    assetClass.vc_manufacturer = manufacturer;
                    assetClass.vc_model = model;
                    assetClass.vc_webSiteLink = manualUrl;

                    db.Entry(assetClass).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();

                    //update iOS Cache Hash
                    CacheHelper cacheHelp = new CacheHelper();
                    cacheHelp.updateiOSCache("getAllAssetClasses");

                    return Json(description);
                }
            }
            catch(Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to insert/update asset class: " + err.Message, "insertUpdateAssetClass", LogHelper.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult getFrequencies()
        {
            try
            {
                var frequencies = db.as_frequencyProfile.Select(q => new { frequencyId = q.i_frequencyId, frequency = q.f_frequency, type = q.i_frequencyType }).Where(q=>q.type == 1).ToList();
                return Json(frequencies);
            }
            catch (Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Faile to retrieve frequencies: " + err.Message, "getFrequencies", LogHelper.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult getPictures()
        {
            try
            {
                var pictures = (from x in db.as_pictureProfile
                                where x.i_fileType == 2 || x.i_fileType == 3
                                select new
                                {
                                    text = x.vc_description,
                                    value = x.i_pictureId,
                                    description = "",
                                    imageSrc = "../.." + x.vc_fileLocation
                                }).ToList();
                return Json(pictures);
            }
            catch (Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to retrieve pictures: " + err.Message, "getPictures", LogHelper.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        public JsonResult getTaskCheckList(int maintenanceId)
        {
            try
            {
                //Get All Check List items for maintenance task
                //Create Date: 2015/03/03
                //Author: Bernard Willer

                var items = db.as_maintenanceCheckListDef.Where(q => q.i_maintenanceId == maintenanceId);

                return Json(items.ToList());
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
        [ValidateAntiForgeryToken]
        public JsonResult addUpdateTaskList(TaskCheck checks)
        {
            
            try
            {
                //This method persists a check list for maintenance tasks
                //Create Date: 2015/03/03
                //Author: Bernard Willer

                //Remove Current List
                var currentList = db.as_maintenanceCheckListDef.Where(x => x.i_maintenanceId == checks.maintenanceId);

                foreach (var item in currentList)
                {
                    db.as_maintenanceCheckListDef.Remove(item);
                }

                db.SaveChanges();

                //Persist New List
                foreach(string taskCheck in checks.taskChecks)
                {
                    as_maintenanceCheckListDef newDef = new as_maintenanceCheckListDef();
                    newDef.bt_active = true;
                    newDef.i_inputType = 0;
                    newDef.i_maintenanceId = checks.maintenanceId;
                    newDef.vc_description = taskCheck;

                    db.as_maintenanceCheckListDef.Add(newDef);
                }

                db.SaveChanges();

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
        
    }
}