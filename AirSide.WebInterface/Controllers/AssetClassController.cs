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

using System;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using AirSide.ServerModules.Helpers;
using AirSide.ServerModules.Models;

namespace ADB.AirSide.Encore.V1.Controllers
{
    [Authorize]
    public class AssetClassController : Controller
    {
        private readonly Entities _db = new Entities();
        private readonly CacheHelper _cache = new CacheHelper(ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString, ConfigurationManager.ConnectionStrings["MongoServer"].ConnectionString);

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public ActionResult AssetClasses()
        {
            return View();
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult GetAllMaintenanceValidators()
        {
            try
            {
                var validators = _db.as_maintenanceValidation.ToList();
                return Json(validators);
            }
            catch (Exception err)
            {
                _cache.Log("Failed to retrieve validation types: " + err.Message, "getAllMaintenanceValidators", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> RemoveTaskAssosiation(int assetMaintenanceId)
        {
            try
            {
                var assosiation = _db.as_assetClassMaintenanceProfile.Find(assetMaintenanceId);
                int assetClassId = assosiation.i_assetClassId;
                _db.as_assetClassMaintenanceProfile.Remove(assosiation);
                _db.SaveChanges();

                //Rebuild cache
                await _cache.RebuildAssetProfileForAssetClass(assetClassId);

                //update iOS Cache Hash
                _cache.UpdateiOsCache("getAllAssetClasses");

                return Json(new { message = "Success" });
            }
            catch (Exception err)
            {
                _cache.Log("Failed to remove task assosiation: " + err.Message, "removeTaskAssosiation", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult GetAssosiatedMaintenanceTasks(int assetClassId)
        {
            try
            {
                var assetMaintenance = (from x in _db.as_assetClassMaintenanceProfile
                                        join y in _db.as_frequencyProfile on x.i_frequencyId equals y.i_frequencyId
                                        join z in _db.as_maintenanceProfile on x.i_maintenanceId equals z.i_maintenanceId
                                        join a in _db.as_maintenanceValidation on z.i_maintenanceValidationId equals a.i_maintenanceValidationId
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
                _cache.Log("Failed to retrieve Assosiated Maintenance Tasks: " + err.Message, "getAllMaintenanceValidators", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> InsertMaintenanceTask(int assetClassId, int maintenanceId, int frequencyId)//, [System.Web.Http.FromUri] int[] validationIds)
        {
            try
            {
                //Get Frequency ID
                int freqId = _db.as_frequencyProfile.Where(q => q.f_frequency == frequencyId).Select(q => q.i_frequencyId).FirstOrDefault();

                //Add Assosiation
                as_assetClassMaintenanceProfile newAssosiation = new as_assetClassMaintenanceProfile();
                newAssosiation.i_assetClassId = assetClassId;
                newAssosiation.i_frequencyId = freqId;
                newAssosiation.i_maintenanceId = maintenanceId;

                _db.as_assetClassMaintenanceProfile.Add(newAssosiation);
                _db.SaveChanges();

                //Build cache for newly added task
                await _cache.RebuildAssetProfileForAssetClass(assetClassId);

                //update iOS Cache Hash
                _cache.UpdateiOsCache("getAllAssetClasses");

                return Json(new { message = "Success"});
            }
            catch(Exception err)
            {
                _cache.Log("Failed to insert new maintenance task: " + err.Message, "insertMaintenanceTask", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult GetAllMaintenanceTasks()
        {
            try
            {
                var tasks = (from x in _db.as_maintenanceProfile 
                                 join y in _db.as_maintenanceValidation on x.i_maintenanceValidationId equals y.i_maintenanceValidationId
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
                _cache.Log("Failed to retrieve all maintenance tasks: " + err.Message, "getAllMaintenanceTasks", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult InsertMaintenanceCategory(string name, string description, int type)
        {
            try
            {
                as_maintenanceCategory newCat = new as_maintenanceCategory();
                newCat.i_categoryType = type;
                newCat.vc_categoryDescription = description;
                newCat.vc_maintenanceCategory = name;

                _db.as_maintenanceCategory.Add(newCat);
                _db.SaveChanges();

                return Json(new {message = "Success" });
            }
            catch (Exception err)
            {
                _cache.Log("Failed to insert new maintenance category: " + err.Message, "insertMaintenanceCategory", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult EditMaintenanceCategory(int id, string categoryName)
        {
            try
            {
                var category = _db.as_maintenanceCategory.Find(id);
                category.vc_maintenanceCategory = categoryName;
                category.vc_categoryDescription = categoryName;
                _db.Entry(category).State = EntityState.Modified;
                _db.SaveChanges();

                return Json(new { message = "Category successfully edited and saved."});
            }
            catch(Exception err)
            {
                _cache.Log("Failed to edit maintenance category: " + err.Message, "editMaintenanceCategory", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(new { message = err.Message });
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult EditMaintenanceTask(int id, string taskName, string validationName)
        {
            try
            {
                var validation = _db.as_maintenanceValidation.Where(q => q.vc_validationName == validationName).FirstOrDefault();
                var task = _db.as_maintenanceProfile.Find(id);
                task.vc_description = taskName;
                task.i_maintenanceValidationId = validation.i_maintenanceValidationId;

                _db.Entry(task).State = EntityState.Modified;
                _db.SaveChanges();

                //update iOS Cache Hash
                _cache.UpdateiOsCache("getMaintenanceProfile");

                return Json(new { message = "Task successfully edited and saved." });
            }
            catch (Exception err)
            {
                _cache.Log("Failed to edit maintenance task: " + err.Message, "editMaintenanceTask", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(new { message = err.Message });
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult RemoveMaintenanceCategory(int id)
        {
            try
            {
                int maintenaceProfile = (from x in _db.as_assetClassMaintenanceProfile
                                         join y in _db.as_maintenanceProfile on x.i_maintenanceId equals y.i_maintenanceId
                                         join z in _db.as_maintenanceCategory on y.i_maintenanceCategoryId equals z.i_maintenanceCategoryId
                                         where z.i_maintenanceCategoryId == id
                                         select x).Count();

                if (maintenaceProfile == 0)
                {
                    var category = _db.as_maintenanceCategory.Find(id);
                    _db.as_maintenanceCategory.Remove(category);
                    _db.SaveChanges();
                    
                    //Remove All Tasks assosiated with the Category
                    var assosiatedTasks = _db.as_maintenanceProfile.Where(q => q.i_maintenanceCategoryId == id);
                    foreach(var item in assosiatedTasks)
                    {
                        _db.as_maintenanceProfile.Remove(item);
                        _db.SaveChanges();
                    }

                    //update iOS Cache Hash
                    _cache.UpdateiOsCache("getMaintenanceProfile");

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
                _cache.Log("Failed to remove maintenance category: " + err.Message, "removeMaintenanceCategory", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(new { message = err.Message });
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult RemoveMaintenanceTask(int id)
        {
            try
            {
                int maintenaceProfile = (from x in _db.as_assetClassMaintenanceProfile
                                         join y in _db.as_maintenanceProfile on x.i_maintenanceId equals y.i_maintenanceId
                                         where y.i_maintenanceId == id
                                         select x).Count();

                if (maintenaceProfile == 0)
                {
                    var task = _db.as_maintenanceProfile.Find(id);
                    _db.as_maintenanceProfile.Remove(task);
                    _db.SaveChanges();

                    //update iOS Cache Hash
                    _cache.UpdateiOsCache("getMaintenanceProfile");

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
                _cache.Log("Failed to remove maintenance category: " + err.Message, "removeMaintenanceCategory", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult InsertMaintenanceTaskType(string description, int catId, int validationId)
        {
            try
            {
                as_maintenanceProfile newTask = new as_maintenanceProfile();
                newTask.i_maintenanceCategoryId = catId;
                newTask.vc_description = description;
                newTask.i_maintenanceValidationId = validationId;

                _db.as_maintenanceProfile.Add(newTask);
                _db.SaveChanges();

                //update iOS Cache Hash
                _cache.UpdateiOsCache("getMaintenanceProfile");

                return Json(new { message = "Success" });
            }
            catch (Exception err)
            {
                _cache.Log("Failed to insert new maintenance task: " + err.Message, "insertMaintenanceTaskType", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult GetAllMaintenanceCategories()
        {
            try
            {
                var categories = _db.as_maintenanceCategory.ToList();
                return Json(categories);
            }
            catch(Exception err)
            {
                _cache.Log("Failed to retrieve all maintenance categories: " + err.Message, "getAllMaintenanceCategories", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult GetAllAssetClasses()
        {
            try
            {
                var assetClassList = (from x in _db.as_assetClassProfile
                                      join y in _db.as_pictureProfile on x.i_pictureId equals y.i_pictureId
                                      select new
                                      {
                                          assetClassId = x.i_assetClassId,
                                          assetDescription = x.vc_description,
                                          manufacturer = x.vc_manufacturer,
                                          pictureLocation = "../.." + y.vc_fileLocation,
                                          model = x.vc_model,
                                          manualURL = x.vc_webSiteLink
                                      }).ToList();
                return Json(assetClassList);
            }
            catch(Exception err)
            {
                _cache.Log("Failed to retrieve all asset classes: " + err.Message, "getAllAssetClasses", CacheHelper.LogTypes.Error, Request.UserHostAddress);
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
                var assetClass = _db.as_assetClassProfile.Find(id);
                var assetInfo = _db.as_assetInfoProfile.FirstOrDefault(q => q.i_assetClassId == assetClass.i_assetClassId && q.vc_description == "Fixing Points");
                ViewBag.description = assetClass.vc_description;
                ViewBag.manufacturer = assetClass.vc_manufacturer;
                ViewBag.model = assetClass.vc_model;
                ViewBag.manualURL = assetClass.vc_webSiteLink;
                ViewBag.picture = assetClass.i_pictureId;
                ViewBag.assetClassId = assetClass.i_assetClassId;
                ViewBag.fixingpoints = assetInfo != null ? assetInfo.vc_value : "0";
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
                ViewBag.fixingpoints = "-1";
                return View();
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult DeleteAssetClass(int id)
        {
            try
            {
                //Select the asset class to be deleted
                var assetClass = _db.as_assetClassProfile.Find(id);

                //Check if this asset class is being used
                int assets = _db.as_assetProfile.Where(q => q.i_assetClassId == assetClass.i_assetClassId).Count();

                //Remove Asset Class
                if (assets == 0)
                {
                    _db.as_assetClassProfile.Remove(assetClass);
                    _db.SaveChanges();

                    //update iOS Cache Hash
                    _cache.UpdateiOsCache("getAllAssetClasses");

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
                _cache.Log("Failed to delete asset class: " + err.Message, "deleteAssetClass", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> InsertUpdateAssetClass(string description, string manufacturer, string model, int pictureId, int assetClassId, string manualUrl, string fixingpoints)
        {
            try
            {
                //Create New Asset Class
                if(assetClassId == 0)
                {
                    //Validate Description
                    var existingAssetClass = _db.as_assetClassProfile.FirstOrDefault(q => q.vc_description == description);

                    if (existingAssetClass == null)
                    {
                        as_assetClassProfile newAssetClass = new as_assetClassProfile
                        {
                            i_pictureId = pictureId,
                            vc_description = description,
                            vc_manufacturer = manufacturer,
                            vc_model = model,
                            vc_webSiteLink = manualUrl
                        };

                       

                        _db.as_assetClassProfile.Add(newAssetClass);
                        _db.SaveChanges();

                        //Add Asset Info
                        as_assetInfoProfile newInfo = new as_assetInfoProfile
                        {
                            vc_description = "Fixing Points",
                            i_assetClassId = newAssetClass.i_assetClassId,
                            vc_value = fixingpoints,
                        };

                        _db.as_assetInfoProfile.Add(newInfo);
                        _db.SaveChanges();

                        //update iOS Cache Hash
                        _cache.UpdateiOsCache("getAllAssetClasses");
                        await _cache.RebuildAssetProfileForAssetClass(assetClassId);

                        return Json(new {description, assetClassId = newAssetClass.i_assetClassId });
                    }
                    else
                    {
                        Response.StatusCode = 500;
                        return Json("This description exists in database.");
                    }
                }
                else
                {
                    var assetClass = _db.as_assetClassProfile.Find(assetClassId);
                    assetClass.i_pictureId = pictureId;
                    assetClass.vc_description = description;
                    assetClass.vc_manufacturer = manufacturer;
                    assetClass.vc_model = model;
                    assetClass.vc_webSiteLink = manualUrl;

                    _db.Entry(assetClass).State = EntityState.Modified;
                    _db.SaveChanges();

                    var assetinfo =
                        _db.as_assetInfoProfile.FirstOrDefault(
                            q => q.i_assetClassId == assetClass.i_assetClassId && q.vc_description == "Fixing Points");

                    if (assetinfo != null)
                    {
                        assetinfo.vc_value = fixingpoints;

                        _db.Entry(assetinfo).State = EntityState.Modified;
                    }
                    _db.SaveChanges();

                    //update iOS Cache Hash
                    _cache.UpdateiOsCache("getAllAssetClasses");

                    return Json(description);
                }
            }
            catch(Exception err)
            {
                _cache.Log("Failed to insert/update asset class: " + err.Message, "insertUpdateAssetClass", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult GetFrequencies()
        {
            try
            {
                var frequencies = _db.as_frequencyProfile.Select(q => new { frequencyId = q.i_frequencyId, frequency = q.f_frequency, type = q.i_frequencyType }).Where(q=>q.type == 1).ToList();
                return Json(frequencies);
            }
            catch (Exception err)
            {
                _cache.Log("Faile to retrieve frequencies: " + err.Message, "getFrequencies", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult GetPictures()
        {
            try
            {
                var pictures = (from x in _db.as_pictureProfile
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
                _cache.Log("Failed to retrieve pictures: " + err.Message, "getPictures", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        public JsonResult GetTaskCheckList(int maintenanceId)
        {
            try
            {
                //Get All Check List items for maintenance task
                //Create Date: 2015/03/03
                //Author: Bernard Willer

                var items = _db.as_maintenanceCheckListDef.Where(q => q.i_maintenanceId == maintenanceId);

                return Json(items.ToList());
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
        [ValidateAntiForgeryToken]
        public JsonResult AddUpdateTaskList(TaskCheck checks)
        {
            
            try
            {
                //This method persists a check list for maintenance tasks
                //Create Date: 2015/03/03
                //Author: Bernard Willer

                //Remove Current List
                var currentList = _db.as_maintenanceCheckListDef.Where(x => x.i_maintenanceId == checks.maintenanceId);

                foreach (var item in currentList)
                {
                    _db.as_maintenanceCheckListDef.Remove(item);
                }

                _db.SaveChanges();

                //Persist New List
                foreach(string taskCheck in checks.taskChecks)
                {
                    as_maintenanceCheckListDef newDef = new as_maintenanceCheckListDef();
                    newDef.bt_active = true;
                    newDef.i_inputType = 0;
                    newDef.i_maintenanceId = checks.maintenanceId;
                    newDef.vc_description = taskCheck;

                    _db.as_maintenanceCheckListDef.Add(newDef);
                }

                _db.SaveChanges();

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
        
    }
}