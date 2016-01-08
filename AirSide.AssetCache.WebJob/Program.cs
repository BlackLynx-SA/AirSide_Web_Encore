#define DEBUG

using System;
using Microsoft.Azure.WebJobs;
using AirSide.ServerModules.Helpers;

namespace AirSide.AssetCache.WebJob
{
    public class Program
    {
        #if !DEBUG
            private static readonly CacheHelper Cache = new CacheHelper("AirSideEncore","mongodb://127.0.0.1");
#else
            //private static readonly CacheHelper Cache = new CacheHelper("AirSideHamad","mongodb://mongodb://172.16.0.5");
            private static readonly CacheHelper Cache = new CacheHelper("AirSideBirmingham","mongodb://mongodb://172.16.0.5");
            //private static readonly CacheHelper Cache = new CacheHelper("AirSideEncore","mongodb://mongodb://172.16.0.5");
            //private static readonly CacheHelper Cache = new CacheHelper("AirSideBaneasa","mongodb://mongodb://172.16.0.5");
            //private static readonly CacheHelper Cache = new CacheHelper("AirSideHeathrow","mongodb://mongodb://172.16.0.5");
#endif

        static void Main()
        {
            try
            {
                //A simple webjob to recreate cache in MongoDB
                //Create Date: 2015/01/22
                //Author: Bernard Willer
                var host = new JobHost();
                Cache.Log("WebJob Starting", "Main", CacheHelper.LogTypes.Info, "WEBJOB");
                host.Call(typeof(Program).GetMethod("reCreateWebCache"));
                Cache.Log("WebJob Completed Web Cache Rebuild", "Main", CacheHelper.LogTypes.Info, "WEBJOB");
                host.Call(typeof(Program).GetMethod("ReCreateiOSCache"));
                Cache.Log("WebJob Completed iOS Cache Rebuild", "Main", CacheHelper.LogTypes.Info, "WEBJOB");
                Cache.Log("WebJob Finished", "Main", CacheHelper.LogTypes.Info, "WEBJOB");
            }
            catch (Exception err)
            {
                Cache.LogError(err, "WEBJOB");
            }
        }

        [NoAutomaticTriggerAttribute]
        public async static void ReCreateiOsCache()
        {
            try
            {
                await Cache.CreateAllAssetDownload();
                await Cache.CreateAssetDownloadCache();
                await Cache.CreateAssetClassDownloadCache();
            }
            catch (Exception err)
            {
                Cache.LogError(err, "WEBJOB");
            }
        }

        [NoAutomaticTriggerAttribute]
        public async static void ReCreateWebCache()
        {
            await Cache.RebuildAssetProfile();
        }
    }
}
