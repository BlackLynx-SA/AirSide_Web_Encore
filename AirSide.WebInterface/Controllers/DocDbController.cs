using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using AirSide.DocDB.DataAccess;
using AirSide.DocDB.DataAccess.Records;

namespace ADB.AirSide.Encore.V1.Controllers
{
    [Authorize]
    public class DocDbController : Controller
    {
        // GET: DocDb
        public ActionResult MethodCreation()
        {
            return View();
        }

        #region AJAX Calls

        [HttpPost]
        public async Task<JsonResult> CreateAssetClassCollection()
        {
            //AirSideDocDbRepo<AirSideAssetProfileRecord>.AirSideDb = "AirSideEncore";
            //AirSideDocDbRepo<AirSideAssetProfileRecord>.AirSideCollection = "Asset";
            //AirSideDocDbRepo<AirSideAssetProfileRecord>.AirSideEndPoint = "https://encore.documents.azure.com:443/";
            //AirSideDocDbRepo<AirSideAssetProfileRecord>.AirSideKey =
            //    "GkIJ4xxNJ4hgaGlN2T2XBkZkDrARTl8yGf4eiK3sJfFndbgxMk8WfSvS+F9dq5nx6RKH10sX1AP8ZCjVJMS34w==";

            //AirSideAssetProfileRecord test = new AirSideAssetProfileRecord()
            //{
            //    AssetId = 1,
            //    Latitude = 0,
            //    Longitude = 0,
            //    Rfid = "000000000000",
            //    Serial = "---"
            //};

            //await AirSideDocDbRepo<AirSideAssetProfileRecord>.CreateItemAsync(test);

            return Json(true);
        }

        #endregion
    }
}