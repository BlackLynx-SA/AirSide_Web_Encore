using ADB.AirSide.Encore.V1.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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

        public Boolean logError(Exception err, string user, [CallerMemberName]string memberName = "")
        {
            try
            {
                mongoLogging log = new mongoLogging();
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

                if (!flag)
                {
                    flag = writeLogToFile(err, memberName, user);
                }
                return true;
            }
            catch (Exception error)
            {
                writeLogToFile(error, memberName, user);
                return false;
            }
        }

        private Boolean writeLogToFile(Exception err, string memberName, string user)
        {
            if (!File.Exists(getLogFile()))
            {
                StreamWriter f = new StreamWriter(getLogFile());
                if (err.InnerException != null)
                    f.WriteLine(DateTime.Now.ToString("yyy/MM/dd hh:mm:ss") + "|Mongo DB Write Failed|Log detail: " + memberName + "|" + logTypes.Error.ToString() + "|" + err.InnerException.Message + "|" + user);
                else
                    f.WriteLine(DateTime.Now.ToString("yyy/MM/dd hh:mm:ss") + "|Mongo DB Write Failed|Log detail: " + memberName + "|" + logTypes.Error.ToString() + "|" + err.Message + "|" + user);

                f.Close();
            }
            else
            {
                StreamWriter f = File.AppendText(getLogFile());
                if (err.InnerException != null)
                    f.WriteLine(DateTime.Now.ToString("yyy/MM/dd hh:mm:ss") + "|Mongo DB Write Failed|Log detail: " + memberName + "|" + logTypes.Error.ToString() + "|" + err.InnerException.Message + "|" + "");
                else
                    f.WriteLine(DateTime.Now.ToString("yyy/MM/dd hh:mm:ss") + "|Mongo DB Write Failed|Log detail: " + memberName + "|" + logTypes.Error.ToString() + "|" + err.Message + "|" + "");

                f.Close();
            }

            return true;
        }

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