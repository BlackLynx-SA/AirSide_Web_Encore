using AirSide.ServerModules.Models;
using Microsoft.Azure.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirSide.ServerModules.Helpers
{
    public class AzureDocumentDBHelper
    {
        private Entities db = new Entities();

        public async Task<bool> CreateAssetList()
        {
            try
            {
                var assets = from x in db.as_assetProfile
                             join y in db.as_assetClassProfile on x.i_assetClassId equals y.i_assetClassId
                             select new
                             {
                                 i_assetId = x.i_assetId,
                                 i_locationId = x.i_locationId,
                                 i_assetClassId = x.i_assetClassId,
                                 vc_rfidTag = x.vc_rfidTag,
                                 vc_serialNumber = x.vc_serialNumber,
                                 productUrl = y.vc_webSiteLink
                             };
                DatabaseHelper dbHelper = new DatabaseHelper();


                foreach (var item in assets)
                {
                    mongoAssetProfile asset = new mongoAssetProfile();

                    asset.assetId = item.i_assetId;
                    asset.locationId = item.i_locationId;
                    asset.assetClassId = item.i_assetClassId;
                    asset.rfidTag = item.vc_rfidTag;
                    asset.serialNumber = item.vc_serialNumber;

                    //Get the Light Status
                    if (db.as_assetStatusProfile.Find(item.i_assetId) != null)
                        asset.status = db.as_assetStatusProfile.Where(q => q.i_assetProfileId == item.i_assetId).Select(q => q.bt_assetStatus).FirstOrDefault();
                    else
                        asset.status = false;

                    asset.productUrl = item.productUrl;
                    asset.maintenance = dbHelper.getMaintenanceTasksDocDB(item.i_assetId);

                    //get data for loaction
                    location locationInfo = new location();
                    var location = db.as_locationProfile.Where(q => q.i_locationId == item.i_locationId).FirstOrDefault();
                    locationInfo.locationId = location.i_locationId;
                    locationInfo.longitude = location.f_longitude;
                    locationInfo.latitude = location.f_latitude;
                    locationInfo.designation = location.vc_designation;
                    locationInfo.areaSubId = location.i_areaSubId;
                    locationInfo.areaId = db.as_areaSubProfile.Where(q => q.i_areaSubId == location.i_areaSubId).Select(q => q.i_areaId).FirstOrDefault();

                    asset.location = locationInfo;

                    //get asset class data
                    assetClass assetClassInfo = new assetClass();
                    var assetclass = db.as_assetClassProfile.Where(q => q.i_assetClassId == item.i_assetClassId).FirstOrDefault();
                    assetClassInfo.assetClassId = assetclass.i_assetClassId;
                    assetClassInfo.description = assetclass.vc_description;
                    assetClassInfo.pictureId = assetclass.i_pictureId;
                    assetClassInfo.manufacturer = assetclass.vc_manufacturer;
                    assetClassInfo.model = assetclass.vc_model;

                    asset.assetClass = assetClassInfo;

                    //get picture data
                    picture pictureInfo = new picture();
                    var picture = db.as_pictureProfile.Where(q => q.i_pictureId == assetclass.i_pictureId).FirstOrDefault();
                    pictureInfo.pictureId = picture.i_pictureId;
                    pictureInfo.fileLocation = picture.vc_fileLocation;
                    pictureInfo.description = picture.vc_description;

                    asset.picture = pictureInfo;

                    //Add to DocumentDB
                    Document doc = await DocumentDBRepository<mongoAssetProfile>.CreateItemAsync(asset);
                }
                return true;
            }
            catch (Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to rebuild asset profile for asset class: " + err.Message, "rebuildAssetProfileForAssetClass", LogHelper.logTypes.Error, "SYSTEM");
                return false;
            }
        }

        public async Task<List<mongoAssetProfile>> getAllAssets()
        {
            IEnumerable<mongoAssetProfile> returnList = DocumentDBRepository<mongoAssetProfile>.GetItems(q => q.status == false);
            return returnList.ToList();
        }
    }
}
