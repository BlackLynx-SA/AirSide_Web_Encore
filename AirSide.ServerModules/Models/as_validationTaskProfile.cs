//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace AirSide.ServerModules.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class as_validationTaskProfile
    {
        public int i_validationProfileId { get; set; }
        public int UserId { get; set; }
        public int i_assetId { get; set; }
        public System.DateTime dt_dateTimeStamp { get; set; }
        public bool bt_validated { get; set; }
        public int i_shiftId { get; set; }
        public int i_maintenanceId { get; set; }
    }
}
