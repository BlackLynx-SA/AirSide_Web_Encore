using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ADB.AirSide.Encore.V1.Models.ViewModels
{
    public class MultiAssetViewModel
    {
        public int assetId { get; set; }
        public string rfidTag { get; set; }
        public int parentId { get; set; }
        public int worstCaseId { get; set; }
        public string serialNumber { get; set; }
        public string assetClass { get; set; }
    }
}