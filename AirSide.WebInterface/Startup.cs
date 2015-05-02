using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(ADB.AirSide.Encore.V1.Startup))]
namespace ADB.AirSide.Encore.V1
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}