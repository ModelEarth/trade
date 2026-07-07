using Microsoft.AspNetCore.Mvc;
using IndustryDB.Models.Data;
using IndustryDB.Services;

namespace IndustryDB.Controllers
{
    /// <summary>
    /// API Controller for trade data import operations.
    /// Handles CSV file imports from the trade-data repository into PostgreSQL.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TradeImportController : ControllerBase
    {
        private readonly ICsvImportService _csvService;
        private readonly ITradeDataRepository _repository;
        private readonly ILogger<TradeImportController> _logger;

        // Track background import status
        private static readonly Dictionary<string, ImportProgress> _importJobs = new();

        public TradeImportController(
            ICsvImportService csvService,
            ITradeDataRepository repository,
            ILogger<TradeImportController> logger)
        {
            _csvService = csvService;
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// POST /api/tradeimport/create-database
        /// Initiates a background import of CSV files into the database.
        /// </summary>
        [HttpPost("create-database")]
        public async Task<IActionResult> CreateDatabase([FromBody] DatabaseCreationRequest request)
        {
            if (request.Year < 2019 || request.Year > 2030)
            {
                return BadRequest(new { error = "Invalid year. Must be between 2019 and 2030." });
            }

            var jobId = Guid.NewGuid().ToString();

            // Start import in background
            _ = Task.Run(async () =>
            {
                try
                {
                    await ImportAllDataAsync(jobId, request.Year, request.Countries, request.ClearExistingData);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Import job {JobId} failed", jobId);
                    UpdateJobStatus(jobId, "Failed", error: ex.Message);
                }
            });

            return Ok(new
            {
                message = "Import started",
                jobId = jobId,
                year = request.Year,
                clearExistingData = request.ClearExistingData
            });
        }

        /// <summary>
        /// GET /api/tradeimport/status/{jobId}
        /// Gets the status of an import job.
        /// </summary>
        [HttpGet("status/{jobId}")]
        public IActionResult GetImportStatus(string jobId)
        {
            if (_importJobs.TryGetValue(jobId, out var progress))
            {
                return Ok(progress);
            }

            return NotFound(new { error = "Import job not found" });
        }

        /// <summary>
        /// GET /api/tradeimport/statistics/{year}
        /// Gets import statistics for a specific year.
        /// </summary>
        [HttpGet("statistics/{year}")]
        public async Task<IActionResult> GetStatistics(short year)
        {
            try
            {
                var stats = await _repository.GetImportStatisticsAsync(year);
                var tableCounts = await _repository.GetTableCountsAsync(year);
                var countries = await _repository.GetDistinctCountriesAsync(year);

                return Ok(new
                {
                    year = year,
                    statistics = stats,
                    tableCounts = tableCounts,
                    countries = countries
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statistics for year {Year}", year);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/tradeimport/test-connection
        /// Tests database connectivity.
        /// </summary>
        [HttpGet("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            var isConnected = await _repository.TestConnectionAsync();
            return Ok(new { connected = isConnected });
        }

        /// <summary>
        /// Main import logic - runs in background.
        /// </summary>
        private async Task ImportAllDataAsync(
            string jobId,
            short year,
            string[]? countries,
            bool clearExistingData)
        {
            var progress = new ImportProgress
            {
                JobId = jobId,
                Year = year,
                Status = "Running",
                StartedAt = DateTime.UtcNow
            };
            _importJobs[jobId] = progress;

            try
            {
                // Clear existing data if requested
                if (clearExistingData)
                {
                    UpdateJobStatus(jobId, "Running", currentStep: "Clearing existing data...");
                    var deleted = await _repository.ClearYearDataAsync(year);
                    _logger.LogInformation("Cleared {Count} rows for year {Year}", deleted.Values.Sum(), year);
                }

                // Get list of countries to import
                countries ??= _csvService.GetAvailableCountries(year).ToArray();

                if (countries.Length == 0)
                {
                    UpdateJobStatus(jobId, "Failed", error: $"No countries found for year {year}");
                    return;
                }

                UpdateJobStatus(jobId, "Running", currentStep: $"Found {countries.Length} countries to import");

                int totalRecordsImported = 0;

                // Import each country
                foreach (var country in countries)
                {
                    UpdateJobStatus(jobId, "Running", currentStep: $"Processing country: {country}");

                    // Import all 3 tradeflow types
                    foreach (var tradeflowType in new[] { "imports", "exports", "domestic" })
                    {
                        UpdateJobStatus(jobId, "Running", currentStep: $"Processing {country}/{tradeflowType}");

                        var recordsImported = await ImportCountryDataAsync(year, country, tradeflowType);
                        totalRecordsImported += recordsImported;

                        progress.RecordsImported = totalRecordsImported;
                        progress.CountriesProcessed++;
                    }
                }

                // Mark as completed
                progress.Status = "Completed";
                progress.CompletedAt = DateTime.UtcNow;
                progress.CurrentStep = $"Import completed. Total records: {totalRecordsImported}";

                _logger.LogInformation(
                    "Import job {JobId} completed. Year: {Year}, Records: {Records}",
                    jobId, year, totalRecordsImported);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Import job {JobId} failed", jobId);
                UpdateJobStatus(jobId, "Failed", error: ex.Message);
            }
        }

        /// <summary>
        /// Imports all CSV files for a specific country-year-tradeflow combination.
        /// </summary>
        private async Task<int> ImportCountryDataAsync(short year, string country, string tradeflowType)
        {
            int totalRecords = 0;

            // Get all CSV files for this country-tradeflow
            var csvFiles = _csvService.GetCsvFilesForImport(year, country, tradeflowType);

            if (csvFiles.Count == 0)
            {
                _logger.LogWarning(
                    "No CSV files found for {Year}/{Country}/{TradeflowType}",
                    year, country, tradeflowType);
                return 0;
            }

            // Process each CSV file
            foreach (var csvFile in csvFiles)
            {
                try
                {
                    // Determine target table from filename
                    var tableName = _csvService.GetTableNameFromFileName(Path.GetFileName(csvFile));

                    // Read CSV records
                    var importRecords = await _csvService.ReadCsvFileAsync(csvFile);

                    if (importRecords.Count == 0)
                    {
                        _logger.LogWarning("CSV file is empty: {File}", csvFile);
                        continue;
                    }

                    // Convert to Trade objects with metadata
                    var trades = importRecords.Select(r => new Trade
                    {
                        Year = year,
                        Region1 = r.Region1,
                        Region2 = r.Region2,
                        Industry1 = r.Industry1,
                        Industry2 = r.Industry2,
                        Amount = r.Amount,
                        FlowType = tradeflowType,
                        Country = country
                    }).ToList();

                    // Bulk insert into database
                    var inserted = await _repository.BulkInsertAsync(trades, tableName);
                    totalRecords += inserted;

                    _logger.LogInformation(
                        "Imported {Count} records from {File} into {Table}",
                        inserted, Path.GetFileName(csvFile), tableName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error importing CSV file: {File}", csvFile);
                    // Continue with next file instead of failing entire import
                }
            }

            return totalRecords;
        }

        /// <summary>
        /// Helper method to update job status.
        /// </summary>
        private void UpdateJobStatus(string jobId, string status, string? currentStep = null, string? error = null)
        {
            if (_importJobs.TryGetValue(jobId, out var progress))
            {
                progress.Status = status;
                if (currentStep != null) progress.CurrentStep = currentStep;
                if (error != null) progress.ErrorMessage = error;
                if (status == "Completed") progress.CompletedAt = DateTime.UtcNow;
            }
        }
    }

    /// <summary>
    /// Tracks the progress of a background import job.
    /// </summary>
    public class ImportProgress
    {
        public string JobId { get; set; } = "";
        public short Year { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Running, Completed, Failed
        public string CurrentStep { get; set; } = "";
        public int RecordsImported { get; set; }
        public int CountriesProcessed { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
