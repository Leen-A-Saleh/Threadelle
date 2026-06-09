using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Threadelle.Models;

namespace Threadelle.Services
{
    /// <summary>
    /// Adds the user's full name (and a short first name) to their claims so the UI
    /// can greet them by name instead of showing their email address.
    /// </summary>
    public class AppClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
    {
        public AppClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<IdentityOptions> options)
            : base(userManager, roleManager, options) { }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            var identity = await base.GenerateClaimsAsync(user);

            if (!string.IsNullOrWhiteSpace(user.FullName))
            {
                identity.AddClaim(new Claim("FullName", user.FullName));
                var first = user.FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                if (!string.IsNullOrEmpty(first))
                    identity.AddClaim(new Claim("FirstName", first));
            }

            if (!string.IsNullOrWhiteSpace(user.ProfileImageUrl))
                identity.AddClaim(new Claim("ProfileImage", user.ProfileImageUrl));

            return identity;
        }
    }
}
