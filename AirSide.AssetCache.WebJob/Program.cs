#define DEBUG

using System;
using Microsoft.Azure.WebJobs;
using AirSide.ServerModules.Helpers;

namespace AirSide.AssetCache.WebJob
{
    public class Program
    {
        #if DEBUG
        private static readonly CacheHelper cache = new CacheHelper("AirSideEncore","mongodb://127.0.0.1");
        #endif

        static void Main()
        {
            try
            {
                //A simple webjob to recreate cache in MongoDB
                //Create Date: 2015/01/22
                //Author: Bernard Willer
                var host = new JobHost();
                cache.log("WebJob Starting", "Main", CacheHelper.logTypes.Info, "WEBJOB");
                host.Call(typeof(Program).GetMethod("reCreateWebCache"));
                cache.log("WebJob Completed Web Cache Rebuild", "Main", CacheHelper.logTypes.Info, "WEBJOB");
                host.Call(typeof(Program).GetMethod("ReCreateiOSCache"));
                cache.log("WebJob Completed iOS Cache Rebuild", "Main", CacheHelper.logTypes.Info, "WEBJOB");
                cache.log("WebJob Finished", "Main", CacheHelper.logTypes.Info, "WEBJOB");
            }
            catch (Exception err)
            {
                cache.logError(err, "WEBJOB");
            }
        }

        [NoAutomaticTriggerAttribute]
        public async static void ReCreateiOSCache()
        {
            try
            {
                await cache.createAllAssetDownload();
                await cache.createAssetDownloadCache();
                await cache.createAssetClassDownloadCache();
            }
            catch (Exception err)
            {
                cache.logError(err, "WEBJOB");
            }
        }

        [NoAutomaticTriggerAttribute]
        public async static void reCreateWebCache()
        {
            await cache.rebuildAssetProfile();
        }
    }
}
