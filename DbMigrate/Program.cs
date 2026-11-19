using System;
using System.IO;
using Npgsql;

Console.WriteLine("🔧 Running database migrations...\n");

// Load settings from the webroot's env file (automation/paths.yaml env_file:),
// as the web app does, or else from .env in the trade repo's root
if (IndustryDB.Services.WebrootEnv.Load() != null)
{
    Console.WriteLine("✅ Loaded settings from the env file named in automation/paths.yaml\n");
}
else if (IndustryDB.Services.WebrootEnv.LoadRepoEnv())
{
    Console.WriteLine("✅ Loaded the trade repo's .env file\n");
}
else
{
    Console.WriteLine("⚠️  No env file found, using environment variables\n");
}

var host     = Environment.GetEnvironmentVariable("DATABASE_HOST")     ?? "localhost";
var database = Environment.GetEnvironmentVariable("DATABASE_NAME")     ?? "industrydb";
var user     = Environment.GetEnvironmentVariable("DATABASE_USER")     ?? "postgres";
var password = Environment.GetEnvironmentVariable("DATABASE_PASSWORD") ?? "";
var port     = Environment.GetEnvironmentVariable("DATABASE_PORT")     ?? "5432";

var connString = $"Host={host};Database={database};Username={user};Password={password};Port={port};SSL Mode=Require;Trust Server Certificate=true";

try
{
    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    Console.WriteLine($"✅ Connected to database: {database} on {host}\n");

    var scriptFiles = new[]
    {
        Path.Combine("..", "IndustryDB", "DB Scripts", "Postgres", "001_CreateTradeTable.sql"),
        Path.Combine("..", "IndustryDB", "DB Scripts", "Postgres", "002_CreateAdditionalTables.sql")
    };

    foreach (var scriptFile in scriptFiles)
    {
        var fullPath = Path.GetFullPath(scriptFile);
        if (!File.Exists(fullPath))
        {
            Console.WriteLine($"❌ Script not found: {fullPath}");
            continue;
        }

        Console.WriteLine($"📄 Executing: {Path.GetFileName(fullPath)}");

        var sql = await File.ReadAllTextAsync(fullPath);

        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();

        Console.WriteLine($"   ✅ Completed\n");
    }

    Console.WriteLine("📊 Verifying tables...");
    var verifySQL = @"
        SELECT tablename
        FROM pg_tables
        WHERE schemaname = 'public'
        ORDER BY tablename;
    ";

    await using var verifyCmd = new NpgsqlCommand(verifySQL, conn);
    await using var reader = await verifyCmd.ExecuteReaderAsync();

    Console.WriteLine("\nCreated tables:");
    while (await reader.ReadAsync())
    {
        Console.WriteLine($"  ✓ {reader.GetString(0)}");
    }

    Console.WriteLine("\n✅ All migrations completed successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    Environment.Exit(1);
}
