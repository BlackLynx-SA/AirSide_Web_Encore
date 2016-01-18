using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AirSide.ServerModules.Models;

namespace ADB.AirSide.Encore.V1.App_Helpers
{
    public class AdminAccess : AuthorizeAttribute
    {
        private readonly Entities _db = new Entities();
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var user = _db.UserProfiles.FirstOrDefault(q => q.UserName == httpContext.User.Identity.Name);
            if (user != null && (user.i_accessLevelId == 1 || user.i_accessLevelId == 2)) return true;
            else return false;
        }
    }
}