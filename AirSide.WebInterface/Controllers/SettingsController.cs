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
// SUMMARY: This class contains all controller calls for the Settings route
#endregion

using ADB.AirSide.Encore.V1.Models;
using AirSide.ServerModules.Helpers;
using AirSide.ServerModules.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace ADB.AirSide.Encore.V1.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly Entities db = new Entities();
        private readonly CacheHelper cache = new CacheHelper(Settings.MongoDBDatabase, Settings.MongoDBServer);

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        public ActionResult ImageLibrary()
        {
            return View();
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        public async Task<ActionResult> EmailSettings()
        {
            mongoEmailSettings mailSettings = await cache.getEmailSettings();
            ViewBag.apiKey = mailSettings.apiKey;
            ViewBag.domain = mailSettings.domain;
            ViewBag.fromAddress = mailSettings.fromAddress;
            return View();
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------

        [HttpPost]
        public JsonResult GetAllAirSideImages()
        {
            try
            {
                List<as_pictureProfile> pictures = db.as_pictureProfile.Where(q => q.i_fileType == 2 || q.i_fileType == 3).ToList();
                return Json(pictures);
            }
            catch(Exception err)
            {
                cache.log("Failed to retrieve all AirSide images: " + err.Message, "getAllAirSideImages", CacheHelper.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult EditImage(int imageId, string description)
        {
            try
            {
                var image = db.as_pictureProfile.Find(imageId);
                image.vc_description = description;

                //Save to Database
                db.Entry(image).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();

                return Json("Success");
            }
            catch (Exception err)
            {
                cache.log("Failed to edit image: " + err.Message, "EditImage", CacheHelper.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult DeleteImage(int imageId)
        {
            try
            {
                var image = db.as_pictureProfile.Find(imageId);

                var assetClass = db.as_assetClassProfile.Where(q => q.i_pictureId == imageId).FirstOrDefault();

                if (assetClass == null)
                {
                    //Remove from DB
                    db.as_pictureProfile.Remove(image);
                    db.SaveChanges();

                    //Remove from File System
                    string fileName = image.vc_fileLocation;
                    fileName = fileName.Replace("/Images/Uploads/", "");
                    var path = Path.Combine(Server.MapPath("~/Images/Uploads"), fileName);
                    System.IO.File.Delete(path);

                    return Json("Success");
                } else
                {
                    Response.StatusCode = 500;
                    return Json("File is used in a Asset Class, change the picture in the asset class first. (" + assetClass.vc_description + ")");
                }
            }
            catch(Exception err)
            {
                cache.log("Failed to delete image: " + err.Message, "DeleteImage", CacheHelper.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult UploadClientImage(HttpPostedFileBase file, string description)
        {
            try
            {
                Guid fileNameGuid = Guid.NewGuid();
                string guidFile = fileNameGuid.ToString();

                // Verify that the user selected a file
                if (file != null && file.ContentLength > 0)
                {
                    var fileExtension = Path.GetExtension(file.FileName);

                    guidFile = guidFile.Replace("-", "").Substring(0, 10);
                    guidFile += fileExtension;
                    var path = Path.Combine(Server.MapPath("~/Images/Uploads"), guidFile);
                    file.SaveAs(path);

                    as_pictureProfile newPic = new as_pictureProfile();
                    newPic.i_fileType = 3;
                    newPic.vc_description = description;
                    newPic.vc_fileLocation = "/Images/Uploads/" + guidFile;

                    db.as_pictureProfile.Add(newPic);
                    db.SaveChanges();
                    Response.StatusCode = 200;
                    return Json(guidFile);
                } else
                {
                    Response.StatusCode = 500;
                    return Json("File selected is empty.");
                }
            }
            catch(Exception err)
            {
                cache.log("Failed to upload client image: " + err.Message, "uploadClientImage", CacheHelper.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
    }
}