using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Threadelle.Helpers
{
    /// <summary>Turns display names into URL-safe slugs. Uniqueness is enforced by the services.</summary>
    public static class SlugHelper
    {
        public static string Slugify(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "item";

            var text = input.Trim().ToLowerInvariant().Replace("&", " and ");
            text = RemoveDiacritics(text);
            text = Regex.Replace(text, @"[^a-z0-9]+", "-").Trim('-');

            return string.IsNullOrEmpty(text) ? "item" : text;
        }

        private static string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
