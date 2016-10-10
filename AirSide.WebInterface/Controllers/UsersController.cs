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
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace ADB.AirSide.Encore.V1.Controllers
{
    [Authorize]
    public class UsersController : BaseController
    {
        private readonly Entities _db = new Entities();
        private readonly CacheHelper _cache = new CacheHelper(ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString, ConfigurationManager.ConnectionStrings["MongoServer"].ConnectionString);

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
        public JsonResult GetAllUsers()
        {
            try
            {
                var users = (from x in _db.UserProfiles
                             join y in _db.as_airportProfile on x.i_airPortId equals y.i_airPortId
                             join z in _db.as_accessProfile on x.i_accessLevelId equals z.i_accessLevelId
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
                _cache.Log("Failed to retrieve all users: " + err.Message, "getAllUsers", CacheHelper.LogTypes.Error, Request.UserHostAddress);
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
                var systemUser = _db.UserProfiles.Find(userId);
                var userToChange = _db.AspNetUsers.Find(systemUser.aspId);

                var hashedNewPassword = UserManager.PasswordHasher.HashPassword(password);
                userToChange.PasswordHash = hashedNewPassword;
                _db.Entry(userToChange).State = EntityState.Modified;
                _db.SaveChanges();

                return Json(systemUser);
            }
            catch(Exception err)
            {
                _cache.Log("Failed to change password: " + err.Message, "ChangePassword", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        public ActionResult Register()
        {
            if (Request.IsAuthenticated)
            {
                var currentUser = from user in _db.UserProfiles
                                  where user.UserName == User.Identity.Name
                                  select user;

                var accessLevel = currentUser.First().i_accessLevelId;
                var airportId = currentUser.First().i_airPortId;
                if (accessLevel == 1)
                {
                    ViewBag.i_airPortId = new SelectList(_db.as_airportProfile, "i_airPortId", "vc_airPortDescription");
                    ViewBag.accessLevel = new SelectList(_db.as_accessProfile, "i_accessLevelId", "vc_description");
                    return View();
                }
                if (accessLevel == 2)
                {
                    var airPort = from airports in _db.as_airportProfile
                        where airports.i_airPortId == airportId
                        select airports;
                    ViewBag.i_airPortId = new SelectList(airPort, "i_airPortId", "vc_airPortDescription");
                    ViewBag.accessLevel = new SelectList(_db.as_accessProfile.Where(q => q.i_accessLevelId != 1), "i_accessLevelId", "vc_description");
                    return View();
                }
                return HttpNotFound();
            }
            return HttpNotFound();
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult InsertGroupAssosiation(int techId, int groupId, int updateType)
        {
            try
            {
                //Find and update current entry
                var user = _db.as_technicianGroupProfile.Find(techId);
                
                //Update either Current(0) or Default(1) Group
                if (updateType == 0)
                    user.i_currentGroup = groupId;
                else if (updateType == 1)
                    user.i_defaultGroup = groupId;

                _db.Entry(user).State = EntityState.Modified;
                _db.SaveChanges();

                //update iOS Cache Hash
                _cache.UpdateiOsCache("getGroupsTechnicians");

                return Json(user);
            }
            catch (Exception err)
            {
                _cache.Log("Failed to insert tech group assosiation: " + err.Message, "insertGroupAssosiation", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult AddNewGroup(string groupName)
        {
            try
            {
                var group = new as_technicianGroups();
                group.vc_externalRef = groupName;
                group.vc_groupName = groupName;
                _db.as_technicianGroups.Add(group);
                _db.SaveChanges();

                return Json(group);
            }
            catch(Exception err)
            {
                _cache.Log("Failed to add new technician group: " + err.Message, "addNewGroup", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult GetAllTechnicians()
        {
            try
            {
                var techs = (from x in _db.as_technicianGroupProfile
                             join y in _db.UserProfiles on x.UserId equals y.UserId
                             join z in _db.as_technicianGroups on x.i_defaultGroup equals z.i_groupId
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
                _cache.Log("Failed to retrieve all technicians: " + err.Message, "getAllTechnicians", CacheHelper.LogTypes.Error, Request.UserHostAddress);
                Response.StatusCode = 500;
                return Json(err.Message);
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
        [HttpPost]
        public JsonResult GetAllTechnicianGroups()
        {
            try
            {
                var techgroups = _db.as_technicianGroups.ToList();
                return Json(techgroups);
            }
            catch(Exception err)
            {
                _cache.Log("Failed to retrieve technician groups: " + err.Message, "getAllTechnicianGroups", CacheHelper.LogTypes.Error, Request.UserHostAddress);
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
                    var fName = model.FirstName.ToLower().Replace("-", "").Replace(".","").Replace(" ","").Replace("'", "");
                    var lName = model.LastName.ToLower().Replace("-", "").Replace(".", "").Replace(" ", "").Replace("'", "").Substring(0, 3);
                    model.UserName = fName + lName;
                }
                else
                {
                    model.UserName = model.EmailAddress;
                }

                var user = new ApplicationUser() { UserName = model.UserName, Email = model.EmailAddress };
                var result = await UserManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    //Create User Profile
                    var newUser = new UserProfile
                    {
                        UserName = model.UserName,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        EmailAddress = model.EmailAddress,
                        i_airPortId = model.i_airPortId,
                        i_accessLevelId = model.accessLevel,
                        dt_dateCreated = DateTime.Now,
                        aspId = user.Id
                    };
                    _db.UserProfiles.Add(newUser);
                    _db.SaveChanges();

                    //Create User Default Technician Group
                    var newTech = new as_technicianGroupProfile
                    {
                        UserId = newUser.UserId,
                        i_currentGroup = 10000,
                        i_defaultGroup = 10000
                    };
                    _db.as_technicianGroupProfile.Add(newTech);
                    _db.SaveChanges();

                    ViewBag.message = model.UserName;
                    ViewBag.messageHead = "User Created";
                    return RedirectToAction("ViewAllUsers", "Users");
                }
                var currentUser = from data in _db.UserProfiles
                    where user.UserName == User.Identity.Name
                    select data;

                var accessLevel = currentUser.First().i_accessLevelId;
                var airportId = currentUser.First().i_airPortId;
                if (accessLevel == 1)
                {
                    ViewBag.i_airPortId = new SelectList(_db.as_airportProfile, "i_airPortId", "vc_airPortDescription");
                    ViewBag.accessLevel = new SelectList(_db.as_accessProfile, "i_accessLevelId", "vc_description");
                    return View();
                }
                if (accessLevel == 2)
                {
                    var airPort = from airports in _db.as_airportProfile
                        where airports.i_airPortId == airportId
                        select airports;
                    ViewBag.i_airPortId = new SelectList(airPort, "i_airPortId", "vc_airPortDescription");
                    ViewBag.accessLevel = new SelectList(_db.as_accessProfile.Where(q => q.i_accessLevelId != 1), "i_accessLevelId", "vc_description");
                    return View();
                }
                return HttpNotFound();
            }
            if (Request.IsAuthenticated)
            {
                var currentUser = from user in _db.UserProfiles
                    where user.UserName == User.Identity.Name
                    select user;

                var accessLevel = currentUser.First().i_accessLevelId;
                var airportId = currentUser.First().i_airPortId;
                if (accessLevel == 1)
                {
                    ViewBag.i_airPortId = new SelectList(_db.as_airportProfile, "i_airPortId", "vc_airPortDescription");
                    ViewBag.accessLevel = new SelectList(_db.as_accessProfile, "i_accessLevelId", "vc_description");
                    return View();
                }
                if (accessLevel == 2)
                {
                    var airPort = from airports in _db.as_airportProfile
                        where airports.i_airPortId == airportId
                        select airports;
                    ViewBag.i_airPortId = new SelectList(airPort, "i_airPortId", "vc_airPortDescription");
                    ViewBag.accessLevel = new SelectList(_db.as_accessProfile.Where(q => q.i_accessLevelId != 1), "i_accessLevelId", "vc_description");
                    return View();
                }
                return HttpNotFound();
            }
            return HttpNotFound();
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        
    }
}