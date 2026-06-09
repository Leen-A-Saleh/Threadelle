using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Threadelle.Helpers
{
    public static class ProductActivityLogger
    {
        private static readonly string LogFilePath = Path.Combine(AppContext.BaseDirectory, "product_activity.json");
        private static readonly object LogLock = new object();

        public class ProductActivityEntry
        {
            public string ProductName { get; set; } = string.Empty;
            public string Action { get; set; } = string.Empty; // "Created", "Edited", "Deleted", "Duplicated", "Published", "Unpublished"
            public string AdminUser { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
        }

        public static async Task LogActivity(string productName, string action, string adminUser)
        {
            try
            {
                List<ProductActivityEntry> logs;
                lock (LogLock)
                {
                    logs = GetActivitiesSync();
                }

                logs.Insert(0, new ProductActivityEntry
                {
                    ProductName = productName,
                    Action = action,
                    AdminUser = adminUser,
                    Timestamp = DateTime.UtcNow
                });

                var json = JsonSerializer.Serialize(logs.Take(100), new JsonSerializerOptions { WriteIndented = true });
                
                // Write asynchronously
                await File.WriteAllTextAsync(LogFilePath, json);
            }
            catch
            {
                // Fail silently to avoid breaking request
            }
        }

        public static async Task<List<ProductActivityEntry>> GetActivities()
        {
            try
            {
                if (!File.Exists(LogFilePath)) return new List<ProductActivityEntry>();
                var json = await File.ReadAllTextAsync(LogFilePath);
                return JsonSerializer.Deserialize<List<ProductActivityEntry>>(json) ?? new List<ProductActivityEntry>();
            }
            catch
            {
                return new List<ProductActivityEntry>();
            }
        }

        private static List<ProductActivityEntry> GetActivitiesSync()
        {
            try
            {
                if (!File.Exists(LogFilePath)) return new List<ProductActivityEntry>();
                var json = File.ReadAllText(LogFilePath);
                return JsonSerializer.Deserialize<List<ProductActivityEntry>>(json) ?? new List<ProductActivityEntry>();
            }
            catch
            {
                return new List<ProductActivityEntry>();
            }
        }
    }
}
