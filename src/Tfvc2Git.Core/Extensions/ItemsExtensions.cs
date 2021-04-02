using System.Text.RegularExpressions;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace Tfvc2Git.Core.Extensions
{
    public static class ItemsExtensions
    {
        public static string GetRelativePath(this Item item, string basePath)
        {
            var result = Regex.Replace(
                item.ServerItem,
                Regex.Escape(basePath),
                string.Empty,
                RegexOptions.IgnoreCase
            );
            return result.TrimStart('/');
        }
    }
}