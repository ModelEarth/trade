using Microsoft.AspNetCore.Mvc;
using ModelEarth.Models;
using Microsoft.Data.SqlClient;   // only if you’re building SQL Server connections
using System.Data;
using Npgsql;
using Azure.Core;
using Azure;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Text.Json;

namespace ModelEarth.Controllers
{
    // ✅ Inherit from BaseController, not Controller
    public class DBController : BaseController
    {
        private readonly ILogger<DBController> _logger;
        private readonly IWebHostEnvironment _env;

        public DBController(ILogger<DBController> logger, IWebHostEnvironment env)
        {
            _env = env;
            _logger = logger;
        }

        // --- Keep your Index() that lists tables, etc. ---

        [HttpGet]
        public IActionResult GetConnections()
        {
            var list = LoadConnectionsFromCookie();   // ✅ from BaseController
            return View(list);                        // View expects List<DBConn>
        }

        [HttpGet]
        public IActionResult CreateConnection() => View(new DBConn());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateConnection(DBConn dbConn)
        {
            if (!ModelState.IsValid) return View(dbConn);

            var list = LoadConnectionsFromCookie();

            // Optional: replace by name to avoid dupes
            var existing = list.FirstOrDefault(c =>
                string.Equals(c.Name, dbConn.Name, StringComparison.OrdinalIgnoreCase));
            if (existing != null) list.Remove(existing);

            list.Add(dbConn);
            SaveConnectionsToCookie(list);

            return RedirectToAction(nameof(GetConnections));
        }

        // ---- Test helpers (optional) ----
        public IActionResult TestCookieWrite()
        {
            var list = LoadConnectionsFromCookie();
            list.Add(new DBConn
            {
                Name = $"TestDB #{list.Count + 1}",
                Server = "testserver.database.azure.com",
                Database = "exiobase"
            });
            SaveConnectionsToCookie(list);
            return RedirectToAction(nameof(TestCookieRead));
        }

        public IActionResult TestCookieRead()
        {
            var loadedList = LoadConnectionsFromCookie();
            if (loadedList.Count == 0)
                return Content("Cookie empty or not set (check HTTPS vs HTTP, and DevTools).");

            return Content($"Saved and loaded {loadedList.Count} connections. First: {loadedList[0].Name}");
        }

        // ---- Query page ----
        [HttpGet]
        public IActionResult Query() => View(new RunQueryVm());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Query(RunQueryVm vm)
        {
            if (string.IsNullOrWhiteSpace(vm.ConnName) || string.IsNullOrWhiteSpace(vm.Query))
            {
                ModelState.AddModelError("", "Connection and SQL are required.");
                return View(vm);
            }

            // 🧠 Trim and normalize SQL for checking
            var sql = vm.Query.Trim();

            // 🛑 Basic protection: only allow statements that start with SELECT
            if (!sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("", "Only SELECT statements are allowed.");
                return View(vm);
            }

            // Optional: forbid dangerous keywords anywhere in the query
            var forbidden = new[] { "INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "EXEC", "MERGE" };
            if (forbidden.Any(k => sql.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                ModelState.AddModelError("", "Only read-only SELECT queries are allowed.");
                return View(vm);
            }

            // ✅ Run the query only if it's safe
            var dbConn = LoadConnectionFromCookieByName(vm.ConnName);
            if (dbConn is null)
            {
                ModelState.AddModelError(nameof(vm.ConnName), $"Connection '{vm.ConnName}' not found.");
                return View(vm);
            }

            try
            {
                var cs = dbConn.IntegratedSecurity
                    ? $"Server=tcp:{dbConn.Server},1433;Database={dbConn.Database};Integrated Security=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
                    : $"Server=tcp:{dbConn.Server},1433;Database={dbConn.Database};User ID={dbConn.UserId};Password={dbConn.Password};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

                using var cn = new Microsoft.Data.SqlClient.SqlConnection(cs);
                using var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, cn) { CommandTimeout = 30 };
                cn.Open();
                using var rdr = cmd.ExecuteReader();

                var dt = new System.Data.DataTable();
                dt.Load(rdr);
                vm.Result = dt;
                return View(vm);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Query failed: {ex.GetBaseException().Message}");
                return View(vm);
            }
        }



    }
}


      

    
