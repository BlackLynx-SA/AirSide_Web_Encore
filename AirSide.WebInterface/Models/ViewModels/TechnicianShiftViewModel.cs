using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ADB.AirSide.Encore.V1.Models.ViewModels
{
    public class TechnicianShiftViewModel
    {
        public int TotalAssets { get; set; }
        public int CompletedAssets { get; set; }
        public int i_shiftId { get; set; }
        public string sheduledDate { get; set; }
        public int i_areaSubId { get; set; }
        public string sheduleTime { get; set; }
        public string permitNumber { get; set; }
        public string areaName { get; set; }
        public string techGroup { get; set; }
        public string validation { get; set; }
        public int shiftType { get; set; }
        public int[] assets { get; set; }
        public int maintenanceId { get; set; }
    }
}