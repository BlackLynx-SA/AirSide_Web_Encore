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
// SUMMARY: This class contains all controller calls for all Asset Calls
#endregion

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using ADB.AirSide.Encore.V1.Models.ViewModels;
using AirSide.ServerModules.Helpers;
using AirSide.ServerModules.Models;

namespace ADB.AirSide.Encore.V1.Controllers
{
    [Authorize]
    public class AssetController : Controller
    {
        private readonly Entities _db = new Entities();
        // GET: Asset
        public ActionResult Assets()
        {
            return View();
        }

        public ActionResult MultiAssets()
        {
            ViewBag.allAssets = new SelectList(_db.as_assetProfile.OrderBy(q => q.vc_rfidTag).Distinct(), "i_assetId", "vc_rfidTag");
            return View();
        }

        [HttpGet]
        public async Task<JsonResult> GetAllMultiAssets()
        {
            var multiAssets = _db.as_multiAssetProfile;

            var allAssets = new List<MultiAssetViewModel>();

            foreach (var item in multiAssets.Select(q => q.i_assetId).Distinct())
            {
                var data = await GetAssetInfo(item);

                if (data.Count <= 0) continue;
                var asset = new MultiAssetViewModel
                {
                    assetId = item,
                    parentId = -1,
                    serialNumber = data[0].serialNumber,
                    rfidTag = data[0].rfidTag,
                    assetClass = GetAssetClass(data[0].assetClassId),
                    worstCaseId = data[0].maintenance[0].maintenanceCycle
                };

                allAssets.Add(asset);
            }

            foreach (var item in multiAssets)
            {
                var data = await GetAssetInfo(item.i_childId);
                if (data.Count <= 0) continue;
                var asset = new MultiAssetViewModel
                {
                    assetId = item.i_childId,
                    parentId = item.i_assetId,
                    serialNumber = data[0].serialNumber,
                    rfidTag = data[0].rfidTag,
                    assetClass = GetAssetClass(data[0].assetClassId),
                    worstCaseId = data[0].maintenance[0].maintenanceCycle
                };

                allAssets.Add(asset);
            }

            return Json(allAssets, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<JsonResult> InsertAssetToParrent(int assetId, int parentId)
        {
            var data = _db.as_multiAssetProfile.Where(q => q.i_assetId == parentId && q.i_childId == assetId).ToList();

            if (data.Count != 0) return Json(new {message = "Success"});
            var asset = new as_multiAssetProfile
            {
                i_multiId = 0,
                i_assetId = parentId,
                i_childId = assetId
            };

            _db.as_multiAssetProfile.Add(asset);
            await _db.SaveChangesAsync();

            return Json(new {message = "Success"});
        }

        [HttpPost]
        public async Task<JsonResult> DeleteAssetFromParent(int assetId, int parentId)
        {
            var data = _db.as_multiAssetProfile.FirstOrDefault(q => q.i_assetId == parentId && q.i_childId == assetId);

            _db.as_multiAssetProfile.Remove(data);

            await _db.SaveChangesAsync();

            return Json(new { message = "Success" });

        }

        #region Helpers

        private string GetAssetClass(int id)
        {
            var data = _db.as_assetClassProfile.Where(q => q.i_assetClassId == id).ToList();
            return data[0].vc_description;
        }

        private async Task<List<mongoAssetProfile>> GetAssetInfo(int id)
        {
            using (
                var core = new CacheHelper(ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString,
                    ConfigurationManager.ConnectionStrings["MongoServer"].ConnectionString))
            {
                return await core.GetAssetInfo(id);
            }
        }

        #endregion
    }
}