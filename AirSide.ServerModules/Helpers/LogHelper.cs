using AirSide.ServerModules.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AirSide.ServerModules.Helpers
{
    public class LogHelper : IDisposable
    {
        public enum logTypes
        {
            Error = 101,
            Debug = 102,
            Info = 103
        }

        private Entities db = new Entities();

        public Boolean logError(Exception err, string user, [CallerMemberName]string memberName = "")
        {
            try
            {
                mongoLogHelper log = new mongoLogHelper();
                if (err.InnerException != null)
                    log.logdescription = err.InnerException.Message;
                else
                    log.logdescription = err.Message;

                log.logTimeStamp = DateTime.Now;
                log.logTypeId = (int)logTypes.Error;
                log.logModule = memberName;
                log.aspUserId = user;

                //Commit to Mongo
                CacheHelper cache = new CacheHelper();
                Boolean flag = cache.writeLog(log);


                return true;
            }
            catch
            {
                return false;
            }
        }

        public void quickDebugLog(string logString)
        {
            try
            {
                mongoLogHelper log = new mongoLogHelper();
                log.logdescription = logString;
                log.logTimeStamp = DateTime.Now;
                log.logTypeId = (int)logTypes.Debug;
                log.logModule = "DEBUG";
                log.aspUserId = "DEV";

                //Commit to Mongo
                CacheHelper cache = new CacheHelper();
                Boolean flag = cache.writeLog(log);

            }
            catch (Exception err)
            {

            }
        }

        public void log(string logString, string module, logTypes logType, string aspUser)
        {
            try
            {
                mongoLogHelper log = new mongoLogHelper();
                log.logdescription = logString;
                log.logTimeStamp = DateTime.Now;
                log.logTypeId = (int)logType;
                log.logModule = module;
                log.aspUserId = aspUser;

                //Commit to Mongo
                CacheHelper cache = new CacheHelper();
                Boolean flag = cache.writeLog(log);

            }
            catch (Exception err)
            {

            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                db.Dispose();
            }
            // free native resources
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
