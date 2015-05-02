#region Using

using System.Web.Mvc;

#endregion

namespace ADB.AirSide.Encore.V1
{
    public static class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}