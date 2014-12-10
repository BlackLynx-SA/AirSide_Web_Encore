#region Copyright
// BlackLynx (Pty) Ltd.
// Copyright (c) 2011 - 2014 All Right Reserved, http://www.blacklynx.co.za/
//
// THE CODE IN THIS SOURCE FILE HAS BEEN DEVELOPED BY BLACKLYNX (PTY) LTD. ("BL")
// THE USE OF ANY EXTRACT, MODULES OR UNITS ARE STICKLY FORBIDDEN.
// PLEASE OBTAIN APPROPRIATE APPROVAL FROM BL AT INFO@BLACKLYNX.CO.ZA
//
// AUTHOR: Bernard Willer
// EMAIL: bernard.willer@blacklynx.co.za
// CREATE DATE: 2014/11/01
// SUMMARY: This class contains all controller calls for the Home route
#endregion

#region Using

using ADB.AirSide.Encore.V1.Models;
using System.Web.Mvc;
using System.Linq;
using System;
using ADB.AirSide.Encore.V1.App_Helpers;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;

#endregion

namespace ADB.AirSide.Encore.V1.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private Entities db = new Entities();

        [AllowAnonymous]
        public ActionResult rebuildCache()
        {
            CacheHelper cache = new CacheHelper();
            cache.rebuildAssetProfile();
            return View();
        }

        //GET: home/startup
        public ActionResult StartUp()
        {
            return View();
        }

        // GET: home/index
        public ActionResult Index()
        {
            return View();
        }

        // GET: home/inbox
        public ActionResult Inbox()
        {
            return View();
        }

        // GET: home/calendar
        public ActionResult Calendar()
        {
            return View();
        }

        // GET: home/google-map
        public ActionResult GoogleMap()
        {
            return View();
        }

        // GET: home/widgets
        public ActionResult Widgets()
        {
            return View();
        }

        // GET: home/chat
        public ActionResult Chat()
        {
            return View();
        }

        //POST: home/getUserDetails
        [HttpPost]
        public JsonResult getUserDetails()
        {
            try
            {
                UserProfile userDetail = (from x in db.UserProfiles
                                  where x.EmailAddress == User.Identity.Name
                                  select x).FirstOrDefault();

                MD5 emailMD5 = MD5.Create();
                string emailHash = GetMd5Hash(emailMD5, userDetail.EmailAddress);

                return Json(new { client = userDetail.FirstName + " " + userDetail.LastName, email = emailHash });
            }
            catch(Exception err)
            {
                Logging log = new Logging();
                log.log("Failed to get User Details: " + err.Message, "getUserDetails", Logging.logTypes.Error, User.Identity.Name);
                return Json(new { client = "Unknown", email = "unknown@unknown.com" });
            }
        }

        #region AJAX Calls for Dashboard

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult setToDoStatus(int todoId)
        {
            try
            {
                //This procedure sets the To-Do item to inactive and sets the date
                //Create Date: 2014/12/09
                //Author: Bernard Willer

                var todo = db.as_todoProfile.Find(todoId);
                todo.bt_active = false;
                todo.dt_completedDate = DateTime.Now;

                db.Entry(todo).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();

                return Json(new { status = "Success" });
            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.logError(err, User.Identity.Name + "(" + Request.UserHostAddress + ")");
                Response.StatusCode = 500;
                return Json(new { message = err.Message });
            }
        
        }

        [HttpPost]
        public JsonResult getAllTodos()
        {
            try
            {
                var user = db.UserProfiles.Where(q => q.UserName == User.Identity.Name).First();
                var todos = db.as_todoProfile.Where(q => (q.UserId == user.UserId && q.bt_active == true) || (q.bt_private == false && q.bt_active == true)).ToList();
                var todosDone = db.as_todoProfile.Where(q => (q.UserId == user.UserId && q.bt_active == false)).OrderByDescending(q=>q.dt_completedDate).Take(5).ToList();
                List<ToDoList> todoItems = new List<ToDoList>();
                foreach(var item in todos)
                {
                    ToDoList list = new ToDoList();
                    list.date = item.dt_dateTime.ToString("yyyy/MM/dd");
                    list.vc_description = item.vc_description;
                    list.i_todoProfileId = item.i_todoProfileId;
                    list.i_todoCatId = item.i_todoCatId;
                    list.bt_active = item.bt_active;

                    todoItems.Add(list);
                }

                if(todosDone != null)
                {
                    foreach (var item in todosDone)
                    {
                        ToDoList list = new ToDoList();
                        list.date = item.dt_dateTime.ToString("yyyy/MM/dd");
                        list.vc_description = item.vc_description;
                        list.i_todoProfileId = item.i_todoProfileId;
                        list.i_todoCatId = item.i_todoCatId;
                        list.bt_active = item.bt_active;

                        todoItems.Add(list);
                    }
                }
                return Json(todoItems);
            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.logError(err, Request.UserHostAddress);
                return Json(new { error = err.Message });
            }
        }

        [HttpPost]
        public JsonResult getTodoCategories()
        {
            try
            {
                var user = db.UserProfiles.Where(q => q.UserName == User.Identity.Name).First();
                var categories = db.as_todoCategories.Where(q => q.UserId == user.UserId || q.bt_private == false).ToList();
                return Json(categories);
            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.logError(err, User.Identity.Name + "(" + Request.UserHostAddress + ")");
                return Json(new { error = err.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult insertNewTodo(string description, string category)
        {
            try
            {
                var categoryObject = db.as_todoCategories.Where(q => q.vc_description == category).First();
                var user = db.UserProfiles.Where(q => q.UserName == User.Identity.Name).First();
                
                as_todoProfile todo = new as_todoProfile();
                todo.UserId = user.UserId;
                todo.dt_dateTime = DateTime.Now;
                todo.bt_private = true;
                todo.bt_active = true;
                todo.vc_description = description;
                todo.i_todoCatId = categoryObject.i_todoCatId;
                todo.dt_completedDate = new DateTime(1970, 1, 1);

                db.as_todoProfile.Add(todo);
                db.SaveChanges();

                return Json(new { 
                    date = todo.dt_dateTime.ToString("yyyy/MM/dd"),
                    vc_description = todo.vc_description,
                    i_todoProfileId = todo.i_todoProfileId,
                    i_todoCatId = todo.i_todoCatId
                });
            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.log("Failed to insert user todos: " + err.Message + "|" + err.InnerException.Message, "insertNewTodo", Logging.logTypes.Error, User.Identity.Name);
                return Json(new { error = err.InnerException.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult updateTodo(int todoId, Boolean active)
        {
            try
            {
                var todo = db.as_todoProfile.Find(todoId);
                todo.bt_active = active;
                db.Entry(todo).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();

                return Json(new { status = "success", item = todo.vc_description });
            }
            catch (Exception err)
            {
                Logging log = new Logging();
                log.log("Failed to update user todos: " + err.Message + "|" + err.InnerException.Message, "updateTodo", Logging.logTypes.Error, User.Identity.Name);
                return Json(new { error = err.InnerException.Message });
            }
        }

        #endregion

        #region Helpers

        static string GetMd5Hash(MD5 md5Hash, string input)
        {

            // Convert the input string to a byte array and compute the hash. 
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes 
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data  
            // and format each one as a hexadecimal string. 
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string. 
            return sBuilder.ToString();
        }

        #endregion
    }
}