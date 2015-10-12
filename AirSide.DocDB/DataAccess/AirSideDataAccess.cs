using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AirSide.ServerModules.Models;

namespace AirSide.DocDB.DataAccess
{
    class AirSideDataAccess
    {
        private readonly Entities _db = new Entities();

        ////Rebuilding DocumentDB Documents
        //public async void RebuildAssetProfile()
        //{
        //    var assets = from x in _db.as_assetProfile
        //                 join y in _db.as_assetClassProfile on x.i_assetClassId equals y.i_assetClassId
        //                 select new
        //                 {
        //                     x.i_assetId,
        //                     x.i_locationId,
        //                     x.i_assetClassId,
        //                     x.vc_rfidTag,
        //                     x.vc_serialNumber,
        //                     productUrl = y.vc_webSiteLink
        //                 };

	       // {
		      //  RebuildAssetProfile();
	       // }
        //}
    }
}
