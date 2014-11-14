using ADB.AirSide.Encore.V1.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace ADB.AirSide.Encore.V1.App_Helpers
{
    public class Logging : IDisposable
    {
        public enum logTypes
        {
            Error = 101,
            Debug = 102,
            Info = 103
        }

        private Entities db = new Entities();

        public void log(string logString, string module, logTypes logType, string aspUser)
        {
            try
            {
                mongoLogging log = new mongoLogging();
                log.logdescription = logString;
                log.logTimeStamp = DateTime.Now;
                log.logTypeId = (int)logType;
                log.logModule = module;
                log.aspUserId = aspUser;

                //Commit to Mongo
                CacheHelper cache = new CacheHelper();
                Boolean flag = cache.writeLog(log);

                if (!flag)
                {
                    if (!File.Exists(getLogFile()))
                    {
                        StreamWriter f = new StreamWriter(getLogFile());
                        f.WriteLine(DateTime.Now.ToString("yyy/MM/dd hh:mm:ss") + "|Mongo DB Write Failed|Log detail: " + module + "|" + logType.ToString() + "|" + logString + "|" + aspUser);
                        f.Close();
                    }
                    else
                    {
                        StreamWriter f = File.AppendText(getLogFile());
                        f.WriteLine(DateTime.Now.ToString("yyy/MM/dd hh:mm:ss") + "|Mongo DB Write Failed|Log detail: " + module + "|" + logType.ToString() + "|" + logString + "|" + aspUser);
                        f.Close();
                    }
                }
            }
            catch (Exception err)
            {
                //Fail back to FS when DB can't write
                if (!File.Exists(getLogFile()))
                {
                    StreamWriter f = new StreamWriter(getLogFile());
                    f.WriteLine(DateTime.Now.ToString("yyy/MM/dd hh:mm:ss") + "|DB Write Failed with: " + err.Message + "|Log detail: " + module + "|" + logType.ToString() + "|" + logString + "|" + aspUser);
                    f.Close();
                }
                else
                {
                    StreamWriter f = File.AppendText(getLogFile());
                    f.WriteLine(DateTime.Now.ToString("yyy/MM/dd hh:mm:ss") + "|DB Write Failed with: " + err.Message + "|Log detail: " + module + "|" + logType.ToString() + "|" + logString + "|" + aspUser);
                    f.Close();
                }
            }
        }

        private string getLogFile()
        {
            string path = HttpContext.Current.Server.MapPath("/") + "\\Logs\\";
            path += DateTime.Now.ToString("yyyMMdd");
            path += ".log";
            return path;
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