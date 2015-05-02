using System;
using Microsoft.Azure.WebJobs;
using AirSide.ServerModules.Helpers;

namespace AirSide.AssetCache.WebJob
{
    public class Program
    {
        static void Main()
        {
            LogHelper log = new LogHelper();
            try
            {
                //A simple webjob to recreate cache in MongoDB
                //Create Date: 2015/01/22
                //Author: Bernard Willer
                var host = new JobHost();
                log.log("WebJob Starting", "Main", LogHelper.logTypes.Info, "WEBJOB");
                host.Call(typeof(Program).GetMethod("reCreateWebCache"));
                log.log("WebJob Completed Web Cache Rebuild", "Main", LogHelper.logTypes.Info, "WEBJOB");
                host.Call(typeof(Program).GetMethod("ReCreateiOSCache"));
                log.log("WebJob Completed iOS Cache Rebuild", "Main", LogHelper.logTypes.Info, "WEBJOB");
                log.log("WebJob Finished", "Main", LogHelper.logTypes.Info, "WEBJOB");
            }
            catch (Exception err)
            {
                log.logError(err, "WEBJOB");
            }
        }

        [NoAutomaticTriggerAttribute]
        public static void ReCreateiOSCache()
        {
            try
            {
                CacheHelper cache = new CacheHelper();
                cache.createAllAssetDownload();
                cache.createAssetDownloadCache();
                cache.createAssetClassDownloadCache();
            }
            catch (Exception err)
            {
                LogHelper log = new LogHelper();
                log.logError(err, "WEBJOB");
            }
        }

        [NoAutomaticTriggerAttribute]
        public static void reCreateWebCache()
        {
            CacheHelper cache = new CacheHelper();
            cache.rebuildAssetProfile();
        }
    }
}
