using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using AirSide.ServerModules.Helpers;
using AirSide.ServerModules.Models;

namespace ADB.AirSide.Encore.V1.Controllers
{
    public class PhotometricController : BaseController
    {
        private readonly Entities _db = new Entities();
        private readonly CacheHelper _cache = new CacheHelper(ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString, ConfigurationManager.ConnectionStrings["MongoServer"].ConnectionString);

        //---------------------------------------------------------------------------------------------------------------------------

        public ActionResult Report()
        {
            return View();
        }

        //---------------------------------------------------------------------------------------------------------------------------

        public ActionResult Upload()
        {
            return View();
        }

        //---------------------------------------------------------------------------------------------------------------------------

        [HttpPost]
        public async Task<JsonResult> UploadData(HttpPostedFileBase file, int type)
        {
            try
            {
                // Verify that the user selected a file
                if (file != null && file.ContentLength > 0)
                {
                    var fileExtension = Path.GetExtension(file.FileName);

                    if (fileExtension.ToLower() == ".csv" && type == 1)
                    {
                        using (var readFile = new StreamReader(file.InputStream))
                        {
                            var line = "";
                            var counter = 0;
                            var dateStr = DateTime.Now;
                            while ((line = readFile.ReadLine()) != null)
                            {
                                
                                var fields = line.Split(char.Parse(";"));
                                if (counter == 0)
                                {
                                    var datePrep = fields[1] + " " + fields[0];
                                    dateStr = DateTime.ParseExact(datePrep, "dd/MM/yyyy HH:mm",
                                        CultureInfo.InvariantCulture);
                                }
                                else
                                {
                                    var measurement = new as_photometricProfile
                                    {
                                        id = Guid.NewGuid(),
                                        dateOfRun = dateStr,
                                        maxIntensity = int.Parse(fields[4]),
                                        averageIntensity = int.Parse(fields[5]),
                                        icaoPercentage = int.Parse(fields[6]),
                                        pictureUrl = fields[8]
                                    };

                                    //Get the asset Id
                                    var serialNumber = fields[1].Trim();
                                    var asset = _db.as_assetProfile.FirstOrDefault(c => c.vc_serialNumber == serialNumber);
                                    measurement.assetId = asset.i_assetId;

                                    var run =
                                        _db.as_photometricProfile.FirstOrDefault(
                                            c => c.assetId == asset.i_assetId && c.dateOfRun == dateStr);

                                    //Remove if it already exists
                                    if (run != null)
                                    {
                                        _db.as_photometricProfile.Remove(run);
                                        await _db.SaveChangesAsync();
                                    }

                                    //Write to DB
                                    _db.as_photometricProfile.Add(measurement);
                                    await _db.SaveChangesAsync();
                                }
                                counter++;
                            }
                        }

                    } else if (fileExtension.ToLower() == ".jpeg" && type == 2)
                    {
                        var fileNameGuid = Guid.NewGuid();
                        var guidFile = fileNameGuid.ToString().Replace("-", "").Substring(0, 10);
                        guidFile += fileExtension;

                        //Save File to local file system
                        var path = Path.Combine(Server.MapPath("~/Content/Img/photometric"), guidFile);
                        file.SaveAs(path);

                        //Update the database with the saved file url
                        var photo = _db.as_photometricProfile.FirstOrDefault(c => c.pictureUrl == file.FileName);
                        photo.pictureUrl = "../../Content/Img/photometric/" + guidFile;

                        _db.Entry(photo).State = EntityState.Modified;
                        await _db.SaveChangesAsync();
                    }
                    
                    return Json(new {message = true});
                }
                else
                {
                    Response.StatusCode = 500;
                    return Json("File selected is empty.");
                }
            }
            catch (Exception err)
            {
                _cache.Log("Failed to upload client image: " + err.Message, "uploadClientImage", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //---------------------------------------------------------------------------------------------------------------------------

        [HttpGet]
        public async Task<JsonResult> GetAvailableDates()
        {
            var data =
                _db.as_photometricProfile.OrderByDescending(q => q.dateOfRun)
                    .ToList()
                    .Select(q => q.dateOfRun.ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture))
                    .Distinct();

            return Json(data.ToList(), JsonRequestBehavior.AllowGet);
        }

        //---------------------------------------------------------------------------------------------------------------------------

        [HttpPost]
        public async Task<JsonResult> GetPhotometricData(string selectedDate)
        {
            try
            {
                var date = DateTime.ParseExact(selectedDate, "yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture);

                var data = await (from x in _db.as_photometricProfile
                    join y in _db.as_assetProfile on x.assetId equals y.i_assetId
                    where x.dateOfRun == date
                    select new
                    {
                        Id = x.id,
                        SerialNumber = y.vc_serialNumber,
                        MaxIntensity = x.maxIntensity,
                        AvgIntensity = x.averageIntensity,
                        ICAO = x.icaoPercentage,
                        PictureUrl = x.pictureUrl
                    }).ToListAsync();

                return Json(data);
            }
            catch (Exception err)
            {
                return Json(new {message = err.Message});
            }
        }

    }
}