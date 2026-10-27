using System;
using System.IO;
using System.Linq;
using dotenv.net;

namespace IndustryDB.Services
{
    /// <summary>
    /// Loads settings from the webroot's env file: the one named by env_file: in
    /// automation/paths.yaml, found by looking upward from the app's folder
    /// (e.g. CloudRoot/automation/paths.yaml, with trade as a CloudRoot submodule).
    /// The file's EXIOBASE_* values (the Industry Database) fill in DATABASE_*
    /// and the DefaultConnection connection string, unless those are already set.
    /// Used by IndustryDB and DbMigrate.
    /// </summary>
    public static class WebrootEnv
    {
        /// <summary>
        /// Returns the env file's path, or null when there's no paths.yaml or
        /// env file above the app (then callers fall back to a local .env).
        /// </summary>
        public static string? Load()
        {
            var envFile = FindEnvFile();
            if (envFile == null)
            {
                return null;
            }

            DotEnv.Load(options: new DotEnvOptions(envFilePaths: new[] { envFile }, overwriteExistingVars: false));

            foreach (var key in new[] { "HOST", "PORT", "NAME", "USER", "PASSWORD" })
            {
                SetIfMissing($"DATABASE_{key}", Environment.GetEnvironmentVariable($"EXIOBASE_{key}"));
            }

            var host = Environment.GetEnvironmentVariable("EXIOBASE_HOST");
            if (!string.IsNullOrEmpty(host))
            {
                var port = Environment.GetEnvironmentVariable("EXIOBASE_PORT") ?? "5432";
                var sslMode = Environment.GetEnvironmentVariable("EXIOBASE_SSL_MODE") ?? "Require";
                SetIfMissing("ConnectionStrings__DefaultConnection",
                    $"Host={host};Database={Environment.GetEnvironmentVariable("EXIOBASE_NAME")};" +
                    $"Username={Environment.GetEnvironmentVariable("EXIOBASE_USER")};" +
                    $"Password={Environment.GetEnvironmentVariable("EXIOBASE_PASSWORD")};" +
                    $"Port={port};Ssl Mode={sslMode};Trust Server Certificate=true");
            }

            return envFile;
        }

        private static void SetIfMissing(string key, string? value)
        {
            if (!string.IsNullOrEmpty(value) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }

        // Same parsing as automation/env-file.mjs: the last env_file: line wins,
        // a trailing " # comment" and quotes are stripped, and a relative path is
        // relative to the automation folder.
        private static string? FindEnvFile()
        {
            foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            {
                for (var dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
                {
                    var pathsYaml = Path.Combine(dir.FullName, "automation", "paths.yaml");
                    if (!File.Exists(pathsYaml))
                    {
                        continue;
                    }

                    var line = File.ReadAllLines(pathsYaml).LastOrDefault(l => l.StartsWith("env_file:"));
                    if (line == null)
                    {
                        return null;
                    }

                    var value = line.Substring("env_file:".Length);
                    var comment = value.IndexOf(" #", StringComparison.Ordinal);
                    if (comment >= 0)
                    {
                        value = value.Substring(0, comment);
                    }
                    value = value.Trim().Trim('"', '\'');
                    if (value.Length == 0)
                    {
                        return null;
                    }

                    var envFile = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(pathsYaml)!, value));
                    return File.Exists(envFile) ? envFile : null;
                }
            }
            return null;
        }
    }
}
