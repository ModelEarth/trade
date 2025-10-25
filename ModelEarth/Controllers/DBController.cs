using Microsoft.AspNetCore.Mvc;
using ModelEarth.Models;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;   // SqlServer provider
using Npgsql;                     // Postgres provider
using System.Text.RegularExpressions;

namespace ModelEarth.Controllers
{
    // Inherit from your BaseController (which contains cookie helpers)
    public class DBController : BaseController
    {
        private readonly ILogger<DBController> _logger;
        private readonly IWebHostEnvironment _env;

        public DBController(ILogger<DBController> logger, IWebHostEnvironment env)
        {
            _env = env;
            _logger = logger;
        }

        // =========================
        // Connections (list/create)
        // =========================

        [HttpGet]
        public IActionResult GetConnections()
        {
            var list = LoadConnectionsFromCookie();   // from BaseController
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

            // Optional: replace by name to avoid duplicates
            var existing = list.FirstOrDefault(c =>
                string.Equals(c.Name, dbConn.Name, StringComparison.OrdinalIgnoreCase));
            if (existing != null) list.Remove(existing);

            list.Add(dbConn);
            SaveConnectionsToCookie(list);

            return RedirectToAction(nameof(GetConnections));
        }

        // =========================
        // (Optional) Cookie tests
        // =========================

        public IActionResult TestCookieWrite()
        {
            var list = LoadConnectionsFromCookie();
            list.Add(new DBConn
            {
                Name = $"TestDB #{list.Count + 1}",
                Provider = "SqlServer",
                Server = "localhost",
                Database = "master",
                Port = 1433
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

        // =========================
        // Query (read-only console)
        // =========================

        [HttpGet]
        public IActionResult Query() => View(new RunQueryVm());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Query(RunQueryVm vm)
        {
            // ---- Basic input validation ----
            if (string.IsNullOrWhiteSpace(vm.ConnName) || string.IsNullOrWhiteSpace(vm.Query))
            {
                ModelState.AddModelError("", "Connection and SQL are required.");
                return View(vm);
            }

            // ---- Enforce read-only: allow SELECT/CTE; block write ops, stacked statements ----
            var sql = (vm.Query ?? "").Trim();

            // Strip leading comments/whitespace (/* ... */ or -- ...)
            sql = Regex.Replace(sql, @"^\s*(--.*?$|/\*[\s\S]*?\*/)\s*", "", RegexOptions.Multiline);

            // Allow SELECT or WITH (CTE) as first keyword
            var firstWordMatch = Regex.Match(sql, @"^\s*([A-Za-z]+)");
            var firstWord = firstWordMatch.Success ? firstWordMatch.Groups[1].Value : "";
            if (!firstWord.Equals("SELECT", System.StringComparison.OrdinalIgnoreCase) &&
                !firstWord.Equals("WITH", System.StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("", "Only read-only SELECT queries are allowed.");
                return View(vm);
            }

            // Optional: disallow multiple statements (very simple guard)
            if (sql.IndexOf(';') >= 0 && !sql.TrimEnd().EndsWith(";"))
            {
                ModelState.AddModelError("", "Multiple statements are not allowed.");
                return View(vm);
            }

            // Block obviously dangerous keywords anywhere
            string[] forbidden =
            {
                "INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "TRUNCATE",
                "EXEC", "MERGE", "CREATE", "GRANT", "REVOKE"
            };
            if (forbidden.Any(k => sql.IndexOf(k, System.StringComparison.OrdinalIgnoreCase) >= 0))
            {
                ModelState.AddModelError("", "Only read-only SELECT queries are allowed.");
                return View(vm);
            }

            // ---- Resolve connection ----
            var dbConn = LoadConnectionFromCookieByName(vm.ConnName);
            if (dbConn is null)
            {
                ModelState.AddModelError(nameof(vm.ConnName), $"Connection '{vm.ConnName}' not found.");
                return View(vm);
            }

            try
            {
                // Build connection string from the model (supports SqlServer/Postgres)
                var cs = dbConn.GetConnectionString();
                var dt = new DataTable();

                if (string.Equals(dbConn.Provider, "Postgres", System.StringComparison.OrdinalIgnoreCase))
                {
                    // ------- PostgreSQL (Npgsql) -------
                    using var cn = new NpgsqlConnection(cs);
                    using var cmd = new NpgsqlCommand(sql, cn) { CommandTimeout = 30 };
                    cn.Open();
                    using var rdr = cmd.ExecuteReader();
                    dt.Load(rdr);
                }
                else
                {
                    // ------- SQL Server (SqlClient) -------
                    using var cn = new SqlConnection(cs);
                    using var cmd = new SqlCommand(sql, cn) { CommandTimeout = 30 };
                    cn.Open();
                    using var rdr = cmd.ExecuteReader();
                    dt.Load(rdr);
                }

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
