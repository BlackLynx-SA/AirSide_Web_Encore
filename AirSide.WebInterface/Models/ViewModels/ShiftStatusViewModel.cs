using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ADB.AirSide.Encore.V1.Models.ViewModels
{
    public class ShiftStatusViewModel
    {
        public int TotalAssets { get; set; }
        public int CompletedAssets { get; set; }
        public List<ShiftAssetStatus> Assets { get; set; }
        
    }

    public class ShiftAssetStatus
    {
        public int AssetId { get; set; }
        public bool Completed { get; set; }
        public int Values { get; set; }
    }
}