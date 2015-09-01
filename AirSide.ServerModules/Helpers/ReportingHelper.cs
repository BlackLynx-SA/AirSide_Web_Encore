#region Copyright
// BlackLynx (Pty) Ltd.
// Copyright (c) 2011 - 2015 All Right Reserved, http://www.blacklynx.co.za/
//
// THE CODE IN THIS SOURCE FILE HAS BEEN DEVELOPED BY BLACKLYNX (PTY) LTD. ("BL")
// THE USE OF ANY EXTRACT, MODULES OR UNITS ARE STICKLY FORBIDDEN.
// PLEASE OBTAIN APPROPRIATE APPROVAL FROM BL AT INFO@BLACKLYNX.CO.ZA
//
// AUTHOR: Bernard Willer
// EMAIL: bernard.willer@blacklynx.co.za
// CREATE DATE: 2015/03/04
// SUMMARY: This class contains all Server Module Classes for the reporting framework
#endregion

using System;
using System.IO;
using AirSide.ServerModules.Models;
using Microsoft.Reporting.WebForms;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AirSide.ServerModules.Helpers
{
    public class ReportingHelper
    {
        //-----------------------------------------------------------------------------------------------------------------------------------------------------------


        public ReportBytes generateReport(ReportSettings settings)
        {
            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=airsidereporting;AccountKey=mCK8CqoLGGIu1c3BQ8BQEI4OtIKllkiwJQv4lMB4A6811TxLXsYzTITL8W7Z2gMztfrkbLUFuqDSe6+ZzPTGpg==");
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(settings.blobContainer);
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(settings.blobReference);

                LocalReport localReport = new LocalReport();
                localReport.EnableExternalImages = true;

                using (var memoryStream = new MemoryStream())
                {
                    blockBlob.DownloadToStream(memoryStream);
                    memoryStream.Position = 0;
                    localReport.LoadReportDefinition(memoryStream);
                }

                foreach (ReportDataSource item in settings.dataSources)
                {
                    localReport.DataSources.Add(item);
                }

                if (settings.logoReference != null)
                {
                    ReportParameter paramLogo = new ReportParameter();
                    paramLogo.Name = "AirportLogo";
                    paramLogo.Values.Add(@"http://airsidecdn.azurewebsites.net/images/" + settings.logoReference.ToLower() + ".png");
                    localReport.SetParameters(paramLogo);
                }

                string fileType = "";
                switch (settings.fileType)
                {
                    case ReportFileTypes.image: fileType = "Image";
                        break;
                    case ReportFileTypes.pdf: fileType = "PDF";
                        break;
                    case ReportFileTypes.excel: fileType = "Excel";
                        break;
                    case ReportFileTypes.word: fileType = "Word";
                        break;
                    default: fileType = "PDF";
                        break;
                }

                string reportType = fileType;
                string mimeType;
                string encoding;
                string fileNameExtension;

                string deviceInfo =
                "<DeviceInfo>" +
                "  <OutputFormat>" + fileType + "</OutputFormat>" +
                "  <PageWidth>210mm</PageWidth>" +
                "  <PageHeight>297mm</PageHeight>" +
                "  <MarginTop>1cm</MarginTop>" +
                "  <MarginLeft>1cm</MarginLeft>" +
                "  <MarginRight>1cm</MarginRight>" +
                "  <MarginBottom>1cm</MarginBottom>" +
                "</DeviceInfo>";

                Warning[] warnings;
                string[] streams;
                byte[] renderedBytes;

                //Render the report
                ReportBytes newReport = new ReportBytes();
                newReport.renderedBytes = localReport.Render(
                    reportType,
                    deviceInfo,
                    out mimeType,
                    out encoding,
                    out fileNameExtension,
                    out streams,
                    out warnings);

                //log.log("Report Rendered", "Report", LogHelper.logTypes.Debug, "SYSTEM");

                newReport.header = new ReportHeader();
                newReport.header.name = "content-disposition";
                newReport.header.value = "attachment; filename="+ settings.reportName +"." + fileNameExtension;
                newReport.mimeType = mimeType;

                return newReport;
            }
            catch (Exception err)
            {
                //log.log("Failed to generate report: " + err.Message + "|" + err.InnerException.Message, settings.reportName, LogHelper.logTypes.Error, "REPORTING");
                ReportBytes newReport = new ReportBytes();
                return newReport;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
    }
}
