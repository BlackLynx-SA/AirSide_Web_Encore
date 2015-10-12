using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using AirSide.DocDB.DataAccess;
using AirSide.DocDB.DataAccess.Records;

namespace ADB.AirSide.Encore.V1.Controllers
{
    [Authorize]
    public class DocDbController : Controller
    {
        // GET: DocDb
        public ActionResult MethodCreation()
        {
            return View();
        }

        #region AJAX Calls
        
        #endregion
    }
}