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
// CREATE DATE: 2014/11/01
// SUMMARY: This class contains all controller calls for the Users route
#endregion

using ADB.AirSide.Encore.V1.Models;
using AirSide.ServerModules.Helpers;
using AirSide.ServerModules.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace ADB.AirSide.Encore.V1.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private Entities db = new Entities();

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public UsersController()
            : this(new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext())))
        {
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public UsersController(UserManager<ApplicationUser> userManager)
        {
            UserManager = userManager;
            userManager.PasswordValidator = new MinimumLengthValidator(4);
            UserManager.UserValidator = new UserValidator<ApplicationUser>(UserManager)
            {
                AllowOnlyAlphanumericUserNames = false
            };
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public UserManager<ApplicationUser> UserManager { get; private set; }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public ActionResult TechnicianGroups()
        {
            return View();
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public ActionResult ViewAllUsers()
        {
            return View();
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult getAllUsers()
        {
            try
            {
                var users = (from x in db.UserProfiles
                             join y in db.as_airportProfile on x.i_airPortId equals y.i_airPortId
                             join z in db.as_accessProfile on x.i_accessLevelId equals z.i_accessLevelId
                             select new { 
                                userId = x.UserId,
                                firstName = x.FirstName,
                                lastName = x.LastName,
                                username = x.UserName,
                                access = z.vc_description,
                                airport = y.vc_airPortDescription
                             }).ToList();
                return Json(users);
            }
            catch(Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to retrieve all users: " + err.Message, "getAllUsers", LogHelper.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ChangePassword(int userId, string password)
        {
            try
            {
                var systemUser = db.UserProfiles.Find(userId);
                var userToChange = db.AspNetUsers.Find(systemUser.aspId);

                String hashedNewPassword = UserManager.PasswordHasher.HashPassword(password);
                userToChange.PasswordHash = hashedNewPassword;
                db.Entry(userToChange).State = EntityState.Modified;
                db.SaveChanges();

                return Json(systemUser);
            }
            catch(Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to change password: " + err.Message, "ChangePassword", LogHelper.logTypes.Error, Request.UserHostAddress);
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public ActionResult Register()
        {
            if (Request.IsAuthenticated)
            {
                var currentUser = from user in db.UserProfiles
                                  where user.UserName == User.Identity.Name
                                  select user;

                int accessLevel = currentUser.First().i_accessLevelId;
                int airportId = currentUser.First().i_airPortId;
                if (accessLevel == 1)
                {
                    ViewBag.i_airPortId = new SelectList(db.as_airportProfile, "i_airPortId", "vc_airPortDescription");
                    ViewBag.accessLevel = new SelectList(db.as_accessProfile, "i_accessLevelId", "vc_description");
                    return View();
                }
                else if (accessLevel == 2)
                {
                    var airPort = from airports in db.as_airportProfile
                                  where airports.i_airPortId == airportId
                                  select airports;
                    ViewBag.i_airPortId = new SelectList(airPort, "i_airPortId", "vc_airPortDescription");
                    ViewBag.accessLevel = new SelectList(db.as_accessProfile.Where(q => q.i_accessLevelId != 1), "i_accessLevelId", "vc_description");
                    return View();
                }
                else return HttpNotFound();
            }
            else return HttpNotFound();
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult insertGroupAssosiation(int techId, int groupId, int updateType)
        {
            try
            {
                //Find and update current entry
                var user = db.as_technicianGroupProfile.Find(techId);
                
                //Update either Current(0) or Default(1) Group
                if (updateType == 0)
                    user.i_currentGroup = groupId;
                else if (updateType == 1)
                    user.i_defaultGroup = groupId;

                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();

                CacheHelper cache = new CacheHelper();
                cache.rebuildTechnicianGroups();

                //update iOS Cache Hash
                cache = new CacheHelper();
                cache.updateiOSCache("getGroupsTechnicians");

                return Json(user);
            }
            catch (Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to insert tech group assosiation: " + err.Message, "insertGroupAssosiation", LogHelper.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult addNewGroup(string groupName)
        {
            try
            {
                as_technicianGroups group = new as_technicianGroups();
                group.vc_externalRef = groupName;
                group.vc_groupName = groupName;
                db.as_technicianGroups.Add(group);
                db.SaveChanges();

                return Json(group);
            }
            catch(Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to add new technician group: " + err.Message, "addNewGroup", LogHelper.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult getAllTechnicians()
        {
            try
            {
                var techs = (from x in db.as_technicianGroupProfile
                             join y in db.UserProfiles on x.UserId equals y.UserId
                             join z in db.as_technicianGroups on x.i_defaultGroup equals z.i_groupId
                             select new { 
                                techName = y.FirstName + " " + y.LastName,
                                defaultGroup = x.i_defaultGroup,
                                currentGroup = x.i_currentGroup,
                                techId = y.UserId,
                                defaultGroupName = z.vc_groupName
                             }).OrderBy(q=>q.techName).ToList();
                return Json(techs);
            }
            catch (Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to retrieve all technicians: " + err.Message, "getAllTechnicians", LogHelper.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult getAllTechnicianGroups()
        {
            try
            {
                var techgroups = db.as_technicianGroups.ToList();
                return Json(techgroups);
            }
            catch(Exception err)
            {
                LogHelper log = new LogHelper();
                log.log("Failed to retrieve technician groups: " + err.Message, "getAllTechnicianGroups", LogHelper.logTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                //Check if user is a technician
                if (model.accessLevel == 3)
                {
                    model.Password = "1234";
                    string fName = model.FirstName.ToLower().Replace("-", "").Replace(".","").Replace(" ","").Replace("'", "");
                    string lName = model.LastName.ToLower().Replace("-", "").Replace(".", "").Replace(" ", "").Replace("'", "").Substring(0, 3);
                    model.UserName = fName + lName;
                }
                else
                {
                    model.UserName = model.EmailAddress;
                }

                var user = new ApplicationUser() { UserName = model.UserName };
                var result = await UserManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    var createdUser = (from data in db.AspNetUsers
                                       where data.UserName == model.UserName
                                       select data).First();

                    //Create User Profile
                    UserProfile newUser = new UserProfile();
                    newUser.UserName = model.UserName;
                    newUser.FirstName = model.FirstName;
                    newUser.LastName = model.LastName;
                    newUser.EmailAddress = model.EmailAddress;
                    newUser.i_airPortId = model.i_airPortId;
                    newUser.i_accessLevelId = model.accessLevel;
                    newUser.dt_dateCreated = DateTime.Now;
                    newUser.aspId = createdUser.Id;
                    db.UserProfiles.Add(newUser);
                    db.SaveChanges();

                    //Update ASP Table
                    AspNetUser aspUser = db.AspNetUsers.Find(createdUser.Id);
                    aspUser.Email = model.EmailAddress;
                    db.Entry(aspUser).State = EntityState.Modified;
                    db.SaveChanges();

                    //Create User Default Technician Group
                    as_technicianGroupProfile newTech = new as_technicianGroupProfile();
                    newTech.UserId = newUser.UserId;
                    newTech.i_currentGroup = 10000;
                    newTech.i_defaultGroup = 10000;
                    db.as_technicianGroupProfile.Add(newTech);
                    db.SaveChanges();

                    ViewBag.message = model.UserName;
                    ViewBag.messageHead = "User Created";
                    return RedirectToAction("ViewAllUsers", "Users");
                }
                else
                {
                    var currentUser = from data in db.UserProfiles
                                      where user.UserName == User.Identity.Name
                                      select data;

                    int accessLevel = currentUser.First().i_accessLevelId;
                    int airportId = currentUser.First().i_airPortId;
                    if (accessLevel == 1)
                    {
                        ViewBag.i_airPortId = new SelectList(db.as_airportProfile, "i_airPortId", "vc_airPortDescription");
                        ViewBag.accessLevel = new SelectList(db.as_accessProfile, "i_accessLevelId", "vc_description");
                        return View();
                    }
                    else if (accessLevel == 2)
                    {
                        var airPort = from airports in db.as_airportProfile
                                      where airports.i_airPortId == airportId
                                      select airports;
                        ViewBag.i_airPortId = new SelectList(airPort, "i_airPortId", "vc_airPortDescription");
                        ViewBag.accessLevel = new SelectList(db.as_accessProfile.Where(q => q.i_accessLevelId != 1), "i_accessLevelId", "vc_description");
                        return View();
                    }
                    else return HttpNotFound();
                }
            }
            else
            {
                if (Request.IsAuthenticated)
                {
                    var currentUser = from user in db.UserProfiles
                                      where user.UserName == User.Identity.Name
                                      select user;

                    int accessLevel = currentUser.First().i_accessLevelId;
                    int airportId = currentUser.First().i_airPortId;
                    if (accessLevel == 1)
                    {
                        ViewBag.i_airPortId = new SelectList(db.as_airportProfile, "i_airPortId", "vc_airPortDescription");
                        ViewBag.accessLevel = new SelectList(db.as_accessProfile, "i_accessLevelId", "vc_description");
                        return View();
                    }
                    else if (accessLevel == 2)
                    {
                        var airPort = from airports in db.as_airportProfile
                                      where airports.i_airPortId == airportId
                                      select airports;
                        ViewBag.i_airPortId = new SelectList(airPort, "i_airPortId", "vc_airPortDescription");
                        ViewBag.accessLevel = new SelectList(db.as_accessProfile.Where(q => q.i_accessLevelId != 1), "i_accessLevelId", "vc_description");
                        return View();
                    }
                    else return HttpNotFound();
                }
                else return HttpNotFound();
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
    }
}