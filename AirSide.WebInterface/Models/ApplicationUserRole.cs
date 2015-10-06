using Microsoft.AspNet.Identity.EntityFramework;

namespace ADB.AirSide.Encore.V1.Models
{
    public class ApplicationUserRole : IdentityUserRole
    {
        public ApplicationUserRole()
            : base()
        { }

        public ApplicationRole Role { get; set; }
    }
}