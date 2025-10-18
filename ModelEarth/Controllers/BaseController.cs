using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ModelEarth.Models;
using System.Text.Json;

namespace ModelEarth.Controllers
{
    public abstract class BaseController : Controller
    {
        protected const string ConnectionsCookieKey = "Connections";

        protected void SaveConnectionsToCookie(List<DBConn> connections)
        {
            var json = JsonSerializer.Serialize(
                connections,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });

            Response.Cookies.Append(ConnectionsCookieKey, json, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(7),
                HttpOnly = true,
                Secure = Request.IsHttps,   // works locally (http) and prod (https)
                SameSite = SameSiteMode.Lax
            });
        }

        protected List<DBConn> LoadConnectionsFromCookie()
        {
            if (Request.Cookies.TryGetValue(ConnectionsCookieKey, out var json) &&
                !string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    return JsonSerializer.Deserialize<List<DBConn>>(
                               json,
                               new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                           ) ?? new List<DBConn>();
                }
                catch
                {
                    // Corrupt/too large/changed format — start fresh
                }
            }
            return new List<DBConn>();
        }

        protected DBConn? LoadConnectionFromCookieByName(string connName)
        {
            var connections = LoadConnectionsFromCookie();
            return connections.FirstOrDefault(c =>
                string.Equals(c.Name, connName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
