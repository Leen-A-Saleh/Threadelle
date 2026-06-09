using System.Security.Claims;

namespace Threadelle.Helpers
{
    public static class ClaimsExtensions
    {
        /// <summary>Friendly display name: full name when available, otherwise the part of the email before '@'.</summary>
        public static string DisplayName(this ClaimsPrincipal user)
        {
            var full = user.FindFirstValue("FullName");
            if (!string.IsNullOrWhiteSpace(full)) return full;

            var name = user.Identity?.Name ?? "";
            var at = name.IndexOf('@');
            return at > 0 ? name[..at] : name;
        }

        public static string FirstName(this ClaimsPrincipal user)
        {
            var first = user.FindFirstValue("FirstName");
            if (!string.IsNullOrWhiteSpace(first)) return first;
            return user.DisplayName().Split(' ').FirstOrDefault() ?? "there";
        }

        public static string? ProfileImage(this ClaimsPrincipal user)
            => user.FindFirstValue("ProfileImage");
    }
}
