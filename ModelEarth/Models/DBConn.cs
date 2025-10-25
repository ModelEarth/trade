namespace ModelEarth.Models
{
    public class DBConn
    {
        public string Name { get; set; }
        public string Server { get; set; }
        public string Database { get; set; }
        public string UserId { get; set; }
        public string Password { get; set; }
        public bool IntegratedSecurity { get; set; } = false;
        public int? Port { get; set; }
        public string Provider { get; set; } = "SqlServer";

        private int GetPort() =>
            Port ?? (Provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase) ? 5432 : 1433);

        public string GetConnectionString()
        {
            if (Provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                // PostgreSQL
                if (IntegratedSecurity)
                {
                    return $"Host={Server};Port={GetPort()};Database={Database};Integrated Security=true;Ssl Mode=Require;";
                }
                else
                {
                    return $"Host={Server};Port={GetPort()};Database={Database};Username={UserId};Password={Password};Ssl Mode=Require;";
                }
            }
            else
            {
                // SQL Server (default)
                if (IntegratedSecurity)
                {
                    return $"Server={Server},{GetPort()};Database={Database};Integrated Security=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
                }
                else
                {
                    return $"Server={Server},{GetPort()};Database={Database};User Id={UserId};Password={Password};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
                }
            }
        }
    }
}
