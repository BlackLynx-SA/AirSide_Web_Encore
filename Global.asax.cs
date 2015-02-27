#region Using

using ADB.AirSide.Encore.V1.App_Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

#endregion

namespace ADB.AirSide.Encore.V1
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            //Add own Json Value Provider
            //ValueProviderFactories.Factories.Remove(ValueProviderFactories.Factories.OfType<System.Web.Mvc.JsonValueProviderFactory>().FirstOrDefault());
            //ValueProviderFactories.Factories.Add(new AirSideJsonValueProviderFactory());

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}