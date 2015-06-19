#region Using

using ADB.AirSide.Encore.V1.App_Helpers;
using System.Web.Mvc;

#endregion

namespace ADB.AirSide.Encore.V1
{
    public static class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new HandleAntiforgeryTokenErrorAttribute() { ExceptionType = typeof(HttpAntiForgeryException) }
            );
        }
    }
}