using System.Web.Mvc;

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