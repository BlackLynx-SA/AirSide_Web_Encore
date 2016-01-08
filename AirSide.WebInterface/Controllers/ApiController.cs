using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using AirSide.ServerModules.Helpers;
using AirSide.ServerModules.Models;

namespace ADB.AirSide.Encore.V1.Controllers
{
    [Authorize]
    public class ApiController : Controller
    {
        private readonly Entities _db = new Entities();
        private readonly CacheHelper _cache = new CacheHelper(ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString, ConfigurationManager.ConnectionStrings["MongoServer"].ConnectionString);
        private readonly DatabaseHelper _func = new DatabaseHelper();

        private const string SharedKey = "AA3CCC5D1AE24BCC964811D724B73326B15C7238D1F14246B91AC1A681DAF24C";

        public ActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<bool> RebuildWebCache(string key)
        {
            try
            {
                if (key == SharedKey)
                {
                    await _cache.RebuildAssetProfile();
                }
                Response.StatusCode = 200;
                return true;
            }
            catch (Exception)
            {
                Response.StatusCode = 500;
                return false;
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<bool> RebuildiOs1Cache(string key)
        {
            try
            {
                if (key == SharedKey)
                {
                    await _cache.CreateAssetClassDownloadCache();
                }
                Response.StatusCode = 200;
                return true;
            }
            catch (Exception)
            {
                Response.StatusCode = 500;
                return false;
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<bool> RebuildiOs2Cache(string key)
        {
            try
            {
                if (key == SharedKey)
                {
                    await _cache.CreateAssetDownloadCache();
                }
                Response.StatusCode = 200;
                return true;
            }
            catch (Exception)
            {
                Response.StatusCode = 500;
                return false;
            }
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<bool> RebuildiOs3Cache(string key)
        {
            try
            {
                if (key == SharedKey)
                {
                    await _cache.CreateAllAssetDownload();
                }
                Response.StatusCode = 200;
                return true;
            }
            catch (Exception)
            {
                Response.StatusCode = 500;
                return false;
            }
        }
        [AllowAnonymous]
        [HttpPost]
        public bool LogError(string key, string jobName)
        {
            try
            {
                if (key == SharedKey)
                {
                    _cache.Log("Scheduled Job failed for: " + jobName, "Scheduled Web Job", CacheHelper.LogTypes.Error,
                        "SYSTEM");
                }
                Response.StatusCode = 200;
                return true;
            }
            catch (Exception)
            {
                Response.StatusCode = 500;
                return false;
            }
        }
    }
}