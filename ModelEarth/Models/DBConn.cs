namespace ModelEarth.Models
{
    public class DBConn
    {
        // Properties
        public string Name { get; set; }
        public string Server { get; set; }
        public string Database { get; set; }
        public string UserId { get; set; }
        public string Password { get; set; }
        public bool IntegratedSecurity { get; set; } = false;
        public int? Port { get; set; }

        // Function to return the connection string
        public string GetConnectionString()
        {
            if (IntegratedSecurity)
            {
                return $"Server={Server}{(Port.HasValue ? "," + Port.Value : "")};Database={Database};Integrated Security=True;";
            }
            else
            {
                return $"Server={Server}{(Port.HasValue ? "," + Port.Value : "")};Database={Database};User Id={UserId};Password={Password};";
            }
        }
    }
}
