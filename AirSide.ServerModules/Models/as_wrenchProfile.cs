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
    
    public partial class as_wrenchProfile
    {
        public int i_wrenchId { get; set; }
        public string vc_model { get; set; }
        public System.DateTime dt_lastCalibrated { get; set; }
        public int i_calibrationCycle { get; set; }
        public double f_batteryLevel { get; set; }
        public string vc_serialNumber { get; set; }
        public bool bt_active { get; set; }
    }
}
