using Dapper;
using IndustryDB.Models.Data;
using Npgsql;

namespace IndustryDB.Services
{
    /// <summary>
    /// Repository for all database operations related to trade data.
    /// Handles bulk inserts, deletes, and statistics using Dapper and PostgreSQL.
    /// </summary>
    public class TradeDataRepository : ITradeDataRepository
    {
        private readonly IConfiguration _config;
        private readonly ILogger<TradeDataRepository> _logger;

        public TradeDataRepository(IConfiguration config, ILogger<TradeDataRepository> logger)
        {
            _config = config;
            _logger = logger;
        }

        private string GetConnectionString()
        {
            return _config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection not found in configuration");
        }

        /// <summary>
        /// Bulk insert trade records into the specified table with batching for performance.
        /// </summary>
        public async Task<int> BulkInsertAsync(
            IEnumerable<Trade> records,
            string tableName,
            int batchSize = 1000)
        {
            var connString = GetConnectionString();
            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();

            var recordsList = records.ToList();
            int totalInserted = 0;

            // Determine SQL based on table name
            var sql = GetInsertSql(tableName);

            // Insert in batches
            for (int i = 0; i < recordsList.Count; i += batchSize)
            {
                var batch = recordsList.Skip(i).Take(batchSize);

                try
                {
                    var rowsInserted = await conn.ExecuteAsync(sql, batch);
                    totalInserted += rowsInserted;

                    _logger.LogInformation(
                        "Inserted batch {BatchNumber}: {Count} records into {Table}",
                        (i / batchSize) + 1,
                        rowsInserted,
                        tableName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error inserting batch into {Table}", tableName);
                    throw;
                }
            }

            _logger.LogInformation(
                "Total inserted into {Table}: {Count} records",
                tableName,
                totalInserted);

            return totalInserted;
        }

        /// <summary>
        /// Gets the appropriate INSERT SQL statement based on table name.
        /// </summary>
        private string GetInsertSql(string tableName)
        {
            return tableName.ToLower() switch
            {
                "public.trade" => @"
                    INSERT INTO public.trade
                    (trade_id, year, region1, region2, industry1, industry2, amount, flow_type, country)
                    VALUES
                    (@TradeId, @Year, @Region1, @Region2, @Industry1, @Industry2, @Amount, @FlowType, @Country)",

                "public.trade_employment" => @"
                    INSERT INTO public.trade_employment
                    (trade_id, year, region1, region2, industry1, industry2, employment_value, flow_type, country)
                    VALUES
                    (@TradeId, @Year, @Region1, @Region2, @Industry1, @Industry2, @Amount, @FlowType, @Country)",

                "public.trade_factor" => @"
                    INSERT INTO public.trade_factor
                    (trade_id, year, country, flow_type, factor_id, coefficient, level)
                    VALUES
                    (@TradeId, @Year, @Country, @FlowType, @FactorId, @Coefficient, @Level)",

                "public.trade_impact" => @"
                    INSERT INTO public.trade_impact
                    (trade_id, year, region1, region2, industry1, industry2, level, flow_type, country)
                    VALUES
                    (@TradeId, @Year, @Region1, @Region2, @Industry1, @Industry2, @Amount, @FlowType, @Country)",

                "public.trade_material" => @"
                    INSERT INTO public.trade_material
                    (trade_id, year, region1, region2, industry1, industry2, material_value, flow_type, country)
                    VALUES
                    (@TradeId, @Year, @Region1, @Region2, @Industry1, @Industry2, @Amount, @FlowType, @Country)",

                "public.trade_resource" => @"
                    INSERT INTO public.trade_resource
                    (trade_id, year, region1, region2, industry1, industry2, resource_value, flow_type, country)
                    VALUES
                    (@TradeId, @Year, @Region1, @Region2, @Industry1, @Industry2, @Amount, @FlowType, @Country)",

                "public.bea_table1" => @"
                    INSERT INTO public.bea_table1
                    (trade_id, year, region1, region2, industry1, industry2, bea_value, flow_type, country)
                    VALUES
                    (@TradeId, @Year, @Region1, @Region2, @Industry1, @Industry2, @Amount, @FlowType, @Country)",

                "public.bea_table2" => @"
                    INSERT INTO public.bea_table2
                    (trade_id, year, region1, region2, industry1, industry2, bea_value, flow_type, country)
                    VALUES
                    (@TradeId, @Year, @Region1, @Region2, @Industry1, @Industry2, @Amount, @FlowType, @Country)",

                "public.bea_table3" => @"
                    INSERT INTO public.bea_table3
                    (trade_id, year, region1, region2, industry1, industry2, bea_value, flow_type, country)
                    VALUES
                    (@TradeId, @Year, @Region1, @Region2, @Industry1, @Industry2, @Amount, @FlowType, @Country)",

                _ => throw new ArgumentException($"Unknown table name: {tableName}")
            };
        }

        /// <summary>
        /// Clears all data for a specific year by calling the clear_year_data stored procedure.
        /// </summary>
        public async Task<Dictionary<string, int>> ClearYearDataAsync(short year)
        {
            var connString = GetConnectionString();
            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();

            var sql = "SELECT * FROM clear_year_data(@Year)";
            var results = await conn.QueryAsync<ClearYearResult>(sql, new { Year = year });

            var resultDict = results.ToDictionary(r => r.table_name, r => r.rows_deleted);

            _logger.LogInformation(
                "Cleared year {Year} data. Total rows deleted: {Total}",
                year,
                resultDict.Values.Sum());

            return resultDict;
        }

        /// <summary>
        /// Gets import statistics for a specific year.
        /// </summary>
        public async Task<List<ImportStatistics>> GetImportStatisticsAsync(short year)
        {
            var connString = GetConnectionString();
            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();

            var sql = "SELECT * FROM get_import_statistics(@Year)";
            var results = await conn.QueryAsync<ImportStatistics>(sql, new { Year = year });

            return results.ToList();
        }

        /// <summary>
        /// Gets row counts for all tables, optionally filtered by year.
        /// </summary>
        public async Task<List<TableCount>> GetTableCountsAsync(short? year = null)
        {
            var connString = GetConnectionString();
            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();

            var sql = "SELECT * FROM get_table_counts(@Year)";
            var results = await conn.QueryAsync<TableCount>(sql, new { Year = year });

            return results.ToList();
        }

        /// <summary>
        /// Gets distinct countries that have data for a specific year.
        /// </summary>
        public async Task<List<CountryInfo>> GetDistinctCountriesAsync(short year)
        {
            var connString = GetConnectionString();
            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();

            var sql = "SELECT * FROM get_distinct_countries(@Year)";
            var results = await conn.QueryAsync<CountryInfo>(sql, new { Year = year });

            return results.ToList();
        }

        /// <summary>
        /// Tests database connectivity.
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var connString = GetConnectionString();
                await using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();

                var result = await conn.ExecuteScalarAsync<int>("SELECT 1");
                return result == 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection test failed");
                return false;
            }
        }
    }

    // DTOs for stored procedure results
    public class ClearYearResult
    {
        public string table_name { get; set; } = "";
        public int rows_deleted { get; set; }
    }

    public class ImportStatistics
    {
        public string region1 { get; set; } = "";
        public string flow_type { get; set; } = "";
        public long trade_count { get; set; }
        public long employment_count { get; set; }
        public long factor_count { get; set; }
        public long impact_count { get; set; }
        public long material_count { get; set; }
        public long resource_count { get; set; }
        public decimal total_amount { get; set; }
    }

    public class TableCount
    {
        public string table_name { get; set; } = "";
        public long row_count { get; set; }
        public short? year_filter { get; set; }
    }

    public class CountryInfo
    {
        public string country_code { get; set; } = "";
        public int tradeflow_count { get; set; }
        public long total_trade_records { get; set; }
    }
}
