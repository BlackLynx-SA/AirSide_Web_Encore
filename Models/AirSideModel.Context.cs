﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ADB.AirSide.Encore.V1.Models
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class Entities : DbContext
    {
        public Entities()
            : base("name=Entities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<as_airportProfile> as_airportProfile { get; set; }
        public virtual DbSet<as_areaProfile> as_areaProfile { get; set; }
        public virtual DbSet<as_areaSubProfile> as_areaSubProfile { get; set; }
        public virtual DbSet<as_assetClassMaintenanceProfile> as_assetClassMaintenanceProfile { get; set; }
        public virtual DbSet<as_assetClassProfile> as_assetClassProfile { get; set; }
        public virtual DbSet<as_assetInfoProfile> as_assetInfoProfile { get; set; }
        public virtual DbSet<as_assetProfile> as_assetProfile { get; set; }
        public virtual DbSet<as_assetStatusProfile> as_assetStatusProfile { get; set; }
        public virtual DbSet<as_electricalNodeProfile> as_electricalNodeProfile { get; set; }
        public virtual DbSet<as_eventPofile> as_eventPofile { get; set; }
        public virtual DbSet<as_eventTypes> as_eventTypes { get; set; }
        public virtual DbSet<as_fbTechProfile> as_fbTechProfile { get; set; }
        public virtual DbSet<as_frequencyProfile> as_frequencyProfile { get; set; }
        public virtual DbSet<as_locationProfile> as_locationProfile { get; set; }
        public virtual DbSet<as_maintenanceCategory> as_maintenanceCategory { get; set; }
        public virtual DbSet<as_maintenanceProfile> as_maintenanceProfile { get; set; }
        public virtual DbSet<as_maintenanceValidation> as_maintenanceValidation { get; set; }
        public virtual DbSet<as_maintenanceValidationProfile> as_maintenanceValidationProfile { get; set; }
        public virtual DbSet<as_nodeRegionProfile> as_nodeRegionProfile { get; set; }
        public virtual DbSet<as_pictureProfile> as_pictureProfile { get; set; }
        public virtual DbSet<as_reportParameters> as_reportParameters { get; set; }
        public virtual DbSet<as_reportProfile> as_reportProfile { get; set; }
        public virtual DbSet<as_settingsProfile> as_settingsProfile { get; set; }
        public virtual DbSet<as_shiftData> as_shiftData { get; set; }
        public virtual DbSet<as_shifts> as_shifts { get; set; }
        public virtual DbSet<as_technicianGroupProfile> as_technicianGroupProfile { get; set; }
        public virtual DbSet<as_technicianGroups> as_technicianGroups { get; set; }
        public virtual DbSet<as_technicianWrenchProfile> as_technicianWrenchProfile { get; set; }
        public virtual DbSet<as_userExternalSession> as_userExternalSession { get; set; }
        public virtual DbSet<as_wrenchProfile> as_wrenchProfile { get; set; }
        public virtual DbSet<UserProfile> UserProfiles { get; set; }
        public virtual DbSet<as_accessProfile> as_accessProfile { get; set; }
        public virtual DbSet<AspNetRole> AspNetRoles { get; set; }
        public virtual DbSet<AspNetUserClaim> AspNetUserClaims { get; set; }
        public virtual DbSet<AspNetUserLogin> AspNetUserLogins { get; set; }
        public virtual DbSet<AspNetUser> AspNetUsers { get; set; }
        public virtual DbSet<as_fileUploadInfo> as_fileUploadInfo { get; set; }
        public virtual DbSet<as_fileUploadProfile> as_fileUploadProfile { get; set; }
        public virtual DbSet<as_todoCategories> as_todoCategories { get; set; }
        public virtual DbSet<as_todoProfile> as_todoProfile { get; set; }
    }
}
