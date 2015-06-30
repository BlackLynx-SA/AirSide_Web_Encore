using Microsoft.Reporting.WebForms;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirSide.ServerModules.Models
{
    #region Enums
    public enum ReportFileTypes
    {
        image = 1,
        pdf = 2,
        excel = 3,
        word = 4
    }

    public enum MaintenanceColours
    {
        grey = 0,
        green = 1,
        yellow = 2,
        orange = 3,
        red = 4
    }

    public enum PictureTypes
    {
        open = 1,
        close = 2,
        uploaded = 3
    }

    public enum SeverityLevels
    {
        Critical = 1,
        Major = 2,
        Minor = 3,
        None = 4
    }

    public enum DashboardMetrics
    {
        ShiftsOpen = 100,
        ShiftsCompleted = 101,
        ActiveShiftCompletion = 102,
        AnomaliesReported = 103,
        AnomaliesResolved = 104,
        FaultyLights = 105,
        FaultyLightsResolved = 106
    }

    public enum AnomalyType
    {
        Picture = 1,
        Voice = 2,
        Text = 3
    }

    #endregion

    #region General Models

    public class ios_validationTaskProfile
    {
        public int i_validationProfileId { get; set; }
        public int UserId { get; set; }
        public int i_assetId { get; set; }
        public string dt_dateTimeStamp { get; set; }
        public bool bt_validated { get; set; }
        public int i_shiftId { get; set; }
    }

    public class AnomalyAlert
    {
        public string guid { get; set; }
        public AnomalyType AlertType { get; set; }
        public string ReportedUser { get; set; }
        public string DateReported { get; set; }
        public string TimeCalculation { get; set; }
        public string ItemUrl { get; set; }
    }

    public class TaskCheck
    {
        public int maintenanceId { get; set; }
        public string[] taskChecks { get; set; }
    }

    public class ReportSettings
    {
        public string blobContainer { get; set; }
        public string blobReference { get; set; }
        public ReportFileTypes fileType { get; set; }
        public ReportDataSource[] dataSources { get; set; }
        public string logoReference { get; set; }
        public string reportName { get; set; }
    }

    public class TaskCheckUpload
    {
        public int i_maintenanceCheckId { get; set; }
        public string vc_capturedValue { get; set; }
        public int UserId { get; set; }
        public int i_assetId { get; set; }
        public int i_shiftId { get; set; }
    }

    public class ReportHeader
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class ReportBytes
    {
        public byte[] renderedBytes { get; set; }
        public string mimeType { get; set; }
        public ReportHeader header { get; set; }
    }

    public class AssetHistory
    {
        public DateTime dateStamp { get; set; }
        public string dateString { get; set; }
        public string heading { get; set; }
        public string[] content { get; set; }
        public string icon { get; set; }
        public string colour { get; set; }
        public int type { get; set; }
    }

    public class CustomShiftBounds
    {
        public double NELat { get; set; }
        public double NELong { get; set; }
        public double SWLat { get; set; }
        public double SWLong { get; set; }
    }

    public class CustomShiftClass
    {
        public string scheduledDate { get; set; }
        public int techGroupId { get; set; }
        public int maintenanceId { get; set; }
        public string permitNumber { get; set; }
        public string externalRef { get; set; }
        public int filterType { get; set; }
        public int filterValue { get; set; }
        public int maintenanceFilter { get; set; }
        public string dateRange { get; set; }
        public bool voiceChk { get; set; }
        public bool textChk { get; set; }
        public bool imageChk { get; set; }
    }

    public class AssetStatus
    {
        public int i_assetProfileId { get; set; }
        public Boolean bt_assetStatus { get; set; }
    }

    public class HomeStats
    {
        public int color { get; set; }
    }

    public class ToDoList
    {
        public string date { get; set; }
        public string vc_description { get; set; }
        public int i_todoProfileId { get; set; }
        public int i_todoCatId { get; set; }
        public Boolean bt_active { get; set; }
    }

    public class activeShifts
    {
        public int i_shiftId { get; set; }
        public string areaSub { get; set; }
        public string techName { get; set; }
        public string yearStr { get; set; }
        public string monthStr { get; set; }
        public string dayStr { get; set; }
        public string timeStr { get; set; }
        public string thStRd { get; set; }
        public int i_areaSubId { get; set; }
        public string fullDateTime { get; set; }
        public double persentage { get; set; }
    }



    public class RegisterModel
    {
        [Required]
        [Display(Name = "User name")]
        public string UserName { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Required]
        [Display(Name = "Firstname")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Lastname")]
        public string LastName { get; set; }

        [Required]
        [Display(Name = "Email Address")]
        [DataType(DataType.EmailAddress)]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string EmailAddress { get; set; }

        [Required]
        [Display(Name = "Airport")]
        public int i_airPortId { get; set; }

        [Required]
        [Display(Name = "Access Level")]
        public int accessLevel { get; set; }

        public virtual as_airportProfile as_airportProfile { get; set; }
    }

    public class UserSetupModel
    {
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
        public int vc_airPortDescription { get; set; }
        public int vc_description { get; set; }
        public string oper { get; set; }
        public int id { get; set; }
    }

    public class techGroup
    {
        public int? i_groupId { get; set; }
        public string vc_groupName { get; set; }
        public string vc_externalRef { get; set; }
    }

    public class techDefaultGroup
    {
        public int UserId { get; set; }
        public string Technician { get; set; }
        public string DefaultGroup { get; set; }
    }

    public class fileUpload
    {
        public string file_guid { get; set; }
        public string description { get; set; }
        public int shiftId { get; set; }
        public double longitude { get; set; }
        public double latitude { get; set; }
        public int userId { get; set; }
        public int severity { get; set; }
    }

    public class NewAssetAssosiation
    {
        public int i_assetClassId { get; set; }
        public string vc_rfidTag { get; set; }
        public string vc_serialNumber { get; set; }
        public float latitude { get; set; }
        public float longitude { get; set; }
        public int i_areaSubId { get; set; }
    }

    public class AssetDownload
    {
        public int i_assetId { get; set; }
        public string vc_tagId { get; set; }
        public int i_assetClassId { get; set; }
        public string vc_serialNumber { get; set; }
        public int i_locationId { get; set; }
        public int i_areaSubId { get; set; }
        public double longitude { get; set; }
        public double latitude { get; set; }
        public string lastDate { get; set; }
        public string maintenance { get; set; }
        public string assetDesc { get; set; }
        public string imagePath { get; set; }
        public string imageDesc { get; set; }
        public int frequencyId { get; set; }
        public Boolean submitted { get; set; }
    }

    public class AssetTagReply
    {
        public int assetId { get; set; }
        public string serialNumber { get; set; }
        public string firstMaintainedDate { get; set; }
        public string lastMaintainedDate { get; set; }
        public string nextMaintenanceDate { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public int subAreaId { get; set; }
        public int assetClassId { get; set; }
    }

    public class assetClassDownload
    {
        public int i_assetClassId { get; set; }
        public string vc_description { get; set; }
        public int i_assetCheckTypeId { get; set; }
        public int assetCheckCount { get; set; }
    }

    public class iOSwrench
    {
        public int i_wrenchId { get; set; }
        public string vc_model { get; set; }
        public string dt_lastCalibrated { get; set; }
        public int i_calibrationCycle { get; set; }
        public double f_batteryLevel { get; set; }
        public string vc_serialNumber { get; set; }
        public Boolean bt_active { get; set; }
    }

    public class WrenchBatteryUpdate
    {
        public int wrenchId { get; set; }
        public double batteryLevel { get; set; }
    }

    public class WrenchAssosiation
    {
        public int wrenchId { get; set; }
        public int UserId { get; set; }
    }

    public class generalInfo
    {
        public int userCount { get; set; }
        public string userContent { get; set; }
        public int assetCount { get; set; }
        public string assetContent { get; set; }
        public int shiftCount { get; set; }
        public string shiftContent { get; set; }
        public int overdueCount { get; set; }
        public string overdueContent { get; set; }
        public int midCycleCount { get; set; }
        public string midCycleContent { get; set; }
        public int doneCount { get; set; }
        public string doneContent { get; set; }
    }

    public class iOSLogin
    {
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string SessionKey { get; set; }
        public int i_airPortId { get; set; }
        public int i_accessLevel { get; set; }
        public int i_groupId { get; set; }
    }

    public class technicianShift
    {
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

    public class iOsValidation
    {
        public int i_validationProfileId { get; set; }
        public int UserId { get; set; }
        public int i_assetId { get; set; }
        public System.DateTime dt_dateTimeStamp { get; set; }
        public bool bt_validated { get; set; }
        public int shiftId { get; set; }
    }

    public class BigExcelDump
    {
        public DateTime dt_captureDate { get; set; }
        public string f_capturedValue { get; set; }
        public int i_assetCheckId { get; set; }
        public Boolean bt_completed { get; set; }
        public DateTime dt_scheduledDate { get; set; }
        public DateTime dt_completionDate { get; set; }
        public string vc_permitNumber { get; set; }
        public string vc_rfidTag { get; set; }
        public string vc_serialNumber { get; set; }
        public string assetClass { get; set; }
        public string vc_manufacturer { get; set; }
        public string vc_model { get; set; }
        public double f_latitude { get; set; }
        public double f_longitude { get; set; }
        public string vc_designation { get; set; }
        public string subArea { get; set; }
        public string mainArea { get; set; }
    }

    public class ActiveShiftsReport
    {
        public DateTime dt_scheduledDate { get; set; }
        public string vc_groupName { get; set; }
        public string vc_externalRef { get; set; }
        public string vc_permitNumber { get; set; }
        public Boolean bt_completed { get; set; }
        public string area { get; set; }
        public string subArea { get; set; }
    }

    public class assetHistory
    {
        public string datetimeStamp { get; set; }
        public string maintenance { get; set; }
        public string valueCaptured { get; set; }
        public int type { get; set; }
    }

    public class shiftInfo
    {
        public string description { get; set; }
        public string groupName { get; set; }
        public string dateTime { get; set; }
        public int validationId { get; set; }
    }

    public class EventInfo
    {
        public string title { get; set; }
        public string description { get; set; }
        public string icon { get; set; }
        public string color { get; set; }
        public string start { get; set; }
    }

    public class ShiftData
    {
        public string eventType { get; set; }
        public string start { get; set; }
        public string completed { get; set; }
        public string team { get; set; }
        public double progress { get; set; }
        public string area { get; set; }
        public string subArea { get; set; }
        public int shiftData { get; set; }
        public int assets { get; set; }
        public int shiftId { get; set; }
        public int shiftType { get; set; }
        public string validation { get; set; }
        public string task { get; set; }
        public string permit { get; set; }
    }

    public class ActivityChart
    {
        public double dateOfActivity { get; set; }
        public int numberOfActivities { get; set; }
    }

    #endregion

    #region MongoDB Models

    public class MongoCurrentPreviousStatus
    {
        public ObjectId Id { get; set; }
        public int assetId { get; set; }
        public int maintenanceId { get; set; }
        public int previousCycle { get; set; }
        public DateTime firstValid { get; set; }
        public DateTime lastValid { get; set; }
    }

    public class mongoEmailSettings
    {
        public ObjectId Id { get; set; }
        public string apiKey { get; set; }
        public string domain { get; set; }
        public string fromAddress { get; set; }
    }

    public class mongoFullAsset
    {
        public ObjectId Id { get; set; }
        public int assetId { get; set; }
        public string serialNumber { get; set; }
        public string firstMaintainedDate { get; set; }
        public string lastMaintainedDate { get; set; }
        public string nextMaintenanceDate { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public int subAreaId { get; set; }
        public int assetClassId { get; set; }
        public string rfidTag { get; set; }
        public Boolean status { get; set; }
    }

    public class mongoAssetClassDownload
    {
        public ObjectId Id { get; set; }
        public int i_assetClassId { get; set; }
        public string vc_description { get; set; }
        public int i_assetCheckTypeId { get; set; }
        public int assetCheckCount { get; set; }
    }

    public class mongoAssetDownload
    {
        public ObjectId Id { get; set; }
        public int i_assetId { get; set; }
        public string vc_tagId { get; set; }
        public int i_assetClassId { get; set; }
        public string vc_serialNumber { get; set; }
        public int i_locationId { get; set; }
        public int i_areaSubId { get; set; }
        public double longitude { get; set; }
        public double latitude { get; set; }
        public string lastDate { get; set; }
        public string maintenance { get; set; }
        public string assetDesc { get; set; }
        public string imagePath { get; set; }
        public string imageDesc { get; set; }
        public int frequencyId { get; set; }
        public Boolean submitted { get; set; }
    }

    public class surveyedData
    {
        public string guid { get; set; }
        public string url { get; set; }
        public string description { get; set; }
        public string type { get; set; }
        public string technician { get; set; }
        public string date { get; set; }
        public double longitude { get; set; }
        public double latitude { get; set; }
    }

    public class mapCenter
    {
        public double longitude { get; set; }
        public double latitude { get; set; }
    }

    public class AssetStatusUpload
    {
        public int assetId { get; set; }
        public string assetStatus { get; set; }
        public int assetSeverity { get; set; }
    }

    public class AssetAssosiationUpload
    {
        public int assetId { get; set; }
        public int UserId { get; set; }
        public string tagId { get; set; }
        public string serialNumber { get; set; }
    }

    public class mongoLogHelper
    {
        public ObjectId Id { get; set; }
        public int logTypeId { get; set; }
        public string logdescription { get; set; }
        public System.DateTime logTimeStamp { get; set; }
        public string logModule { get; set; }
        public string aspUserId { get; set; }
    }

    public class mongoAssetProfile
    {
        public ObjectId Id { get; set; }
        public int assetId { get; set; }
        public int locationId { get; set; }
        public int assetClassId { get; set; }
        public string rfidTag { get; set; }
        public string serialNumber { get; set; }
        public Boolean status { get; set; }
        public string productUrl { get; set; }

        public location location { get; set; }
        public assetClass assetClass { get; set; }
        public picture picture { get; set; }
        public List<maintenance> maintenance { get; set; }
    }

    public class maintenance
    {
        public string maintenanceTask { get; set; }
        public string previousDate { get; set; }
        public string nextDate { get; set; }
        public int maintenanceCycle { get; set; }
        public int maintenanceId { get; set; }
    }

    public class location
    {
        public int locationId { get; set; }
        public double longitude { get; set; }
        public double latitude { get; set; }
        public string designation { get; set; }
        public int areaSubId { get; set; }
        public int areaId { get; set; }
    }

    public class assetClass
    {
        public int assetClassId { get; set; }
        public string description { get; set; }
        public int pictureId { get; set; }
        public string manufacturer { get; set; }
        public string model { get; set; }
        public int frequencyId { get; set; }
    }

    public class frequency
    {
        public int frequencyId { get; set; }
        public string description { get; set; }
        public double frequencyValue { get; set; }
    }

    public class picture
    {
        public int pictureId { get; set; }
        public string fileLocation { get; set; }
        public string description { get; set; }
    }
    #endregion

    #region Reporting

    public class Analytic_Cycles
    {
        public double noData { get; set; }
        public double completed { get; set; }
        public double midCycle { get; set; }
        public double almostDue { get; set; }
        public double due { get; set; }
        public double totalAssets { get; set; }
        public double totalTasks { get; set; }
        public double assosiatedTasks { get; set; }
        public double totalShifts { get; set; }
    }

    public class EventReportInfo
    {
        public string TechGroup { get; set; }
        public int NumberOfAssets { get; set; }
        public string MaintenanceTask { get; set; }
        public decimal PercentageComplete { get; set; }
        public decimal PercentageNotComplete { get; set; }
        public string EventDate { get; set; }
    }

    public class EventAssetInfo
    {
        public string AssetName { get; set; }
        public string PriorState { get; set; }
        public string CurrentState { get; set; }
        public string TypeOfAsset { get; set; }
        public string DateOfEvent { get; set; }
        public string ValidationType { get; set; }
    }

    public class EventCheckList
    {
        public string ListItem { get; set; }
    }

    public class EventScheduleData
    {
        public string DateOfEvent { get; set; }
        public string TimeOfEvent { get; set; }
        public string Description { get; set; }
    }

    #endregion

    #region iOSModels

    public class iOSImageUpload
    {
        public string description { get; set; }
        public string image { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
    }

    #endregion

    #region Dashboard Models

    public class DashboardActivityMetrics
    {
        public DashboardMetrics indicatorEnum { get; set; }
        public double value { get; set; }
    }

    #endregion
}
