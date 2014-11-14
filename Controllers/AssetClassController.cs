using ADB.AirSide.Encore.V1.App_Helpers;
using ADB.AirSide.Encore.V1.Models;
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
        public ActionResult AssetClasses()
        {
            return View();
        }

        [HttpPost]
        public JsonResult getAllAssetClasses()
        {
            try
            {
                var AssetClassList = (from x in db.as_assetClassProfile
                                      join y in db.as_pictureProfile on x.i_pictureId equals y.i_pictureId
                                      join z in db.as_frequencyProfile on x.i_frequencyId equals z.i_frequencyId
                                      select new
                                      {
                                          assetClassId = x.i_assetClassId,
                                          assetDescription = x.vc_description,
                                          manufacturer = x.vc_manufacturer,
                                          pictureLocation = "../.." + y.vc_fileLocation,
                                          frequency = z.f_frequency,
                                          model = x.vc_model
                                      }).ToList();
                return Json(AssetClassList);
            }
            catch(Exception err)
            {
                Logging log = new Logging();
                log.log("Failed to retrieve all asset classes: " + err.Message, "getAllAssetClasses", Logging.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        public ActionResult CreateAssetClass()
        {
            return View();
        }

        public ActionResult EditAssetClass(int id)
        {
            try
            {
                var assetClass = db.as_assetClassProfile.Find(id);
                double frequency = db.as_frequencyProfile.Where(q => q.i_frequencyId == assetClass.i_frequencyId).Select(q => q.f_frequency).First();

                ViewBag.description = assetClass.vc_description;
                ViewBag.manufacturer = assetClass.vc_manufacturer;
                ViewBag.model = assetClass.vc_model;
                ViewBag.picture = assetClass.i_pictureId;
                ViewBag.frequency = Math.Round(frequency);
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
                ViewBag.frequency = "";
                ViewBag.assetClassId = -1;
                ViewBag.error = "Failed to retrieve record: " + err.Message;
                return View();
            }
        }

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
                Logging log = new Logging();
                log.log("Failed to delete asset class: " + err.Message, "deleteAssetClass", Logging.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult insertUpdateAssetClass(string description, string manufacturer, string model, int pictureId, double frequency, int assetClassId)
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

                        //Get the Frequency Id
                        int frequencyId = db.as_frequencyProfile.Where(q => q.f_frequency == frequency).Select(q => q.i_frequencyId).First();

                        as_assetClassProfile newAssetClass = new as_assetClassProfile();
                        newAssetClass.i_frequencyId = frequencyId;
                        newAssetClass.i_pictureId = pictureId;
                        newAssetClass.vc_description = description;
                        newAssetClass.vc_manufacturer = manufacturer;
                        newAssetClass.vc_model = model;

                        db.as_assetClassProfile.Add(newAssetClass);
                        db.SaveChanges();

                        return Json(description);
                    }
                    else
                    {
                        Response.StatusCode = 500;
                        return Json("This description exists in database.");
                    }
                }
                else
                {
                    //Get the Frequency Id
                    int frequencyId = db.as_frequencyProfile.Where(q => q.f_frequency == frequency).Select(q => q.i_frequencyId).First();

                    var assetClass = db.as_assetClassProfile.Find(assetClassId);
                    int currentFrequency = assetClass.i_frequencyId;
                    assetClass.i_frequencyId = frequencyId;
                    assetClass.i_pictureId = pictureId;
                    assetClass.vc_description = description;
                    assetClass.vc_manufacturer = manufacturer;
                    assetClass.vc_model = model;

                    db.Entry(assetClass).State = System.Data.Entity.EntityState.Modified;
                    db.SaveChanges();

                    //Update Cache
                    if(currentFrequency != assetClass.i_frequencyId)
                    {
                        CacheHelper cache = new CacheHelper();
                        cache.rebuildAssetProfileForAssetClass(assetClass.i_assetClassId);
                    }
                    return Json(description);
                }
            }
            catch(Exception err)
            {
                Logging log = new Logging();
                log.log("Failed to insert/update asset class: " + err.Message, "insertUpdateAssetClass", Logging.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        [HttpPost]
        public JsonResult getFrequencies()
        {
            try
            {
                var frequencies = db.as_frequencyProfile.Select(q => new { frequencyId = q.i_frequencyId, frequency = q.f_frequency }).ToList();
                return Json(frequencies);
            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.log("Faile to retrieve frequencies: " + err.Message, "getFrequencies", Logging.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

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
                Logging log = new Logging();
                log.log("Failed to retrieve pictures: " + err.Message, "getPictures", Logging.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }
    }
}