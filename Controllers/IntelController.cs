#region Using

using System.Web.Mvc;

#endregion

namespace ADB.AirSide.Encore.V1.Controllers
{
    public class IntelController : Controller
    {
        // GET: /intel/settings
        public ActionResult Settings()
        {
            return View();
        }

        // GET: /intel/versions
        public ActionResult Versions()
        {
            return View();
        }
    }
}