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
                cache.Log("WebJob Starting", "Main", CacheHelper.LogTypes.Info, "WEBJOB");
                host.Call(typeof(Program).GetMethod("reCreateWebCache"));
                cache.Log("WebJob Completed Web Cache Rebuild", "Main", CacheHelper.LogTypes.Info, "WEBJOB");
                host.Call(typeof(Program).GetMethod("ReCreateiOSCache"));
                cache.Log("WebJob Completed iOS Cache Rebuild", "Main", CacheHelper.LogTypes.Info, "WEBJOB");
                cache.Log("WebJob Finished", "Main", CacheHelper.LogTypes.Info, "WEBJOB");
            }
            catch (Exception err)
            {
                cache.LogError(err, "WEBJOB");
            }
        }

        [NoAutomaticTriggerAttribute]
        public async static void ReCreateiOSCache()
        {
            try
            {
                await cache.CreateAllAssetDownload();
                await cache.CreateAssetDownloadCache();
                await cache.CreateAssetClassDownloadCache();
            }
            catch (Exception err)
            {
                cache.LogError(err, "WEBJOB");
            }
        }

        [NoAutomaticTriggerAttribute]
        public async static void reCreateWebCache()
        {
            await cache.RebuildAssetProfile();
        }
    }
}
