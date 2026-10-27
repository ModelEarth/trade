using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using IndustryDB.Data;
// Load settings from the webroot's env file (automation/paths.yaml env_file:),
// or from a local .env when trade runs outside a webroot
if (IndustryDB.Services.WebrootEnv.Load() == null)
{
    dotenv.net.DotEnv.Load(options: new dotenv.net.DotEnvOptions(
        envFilePaths: new[] { "../.env", ".env" },
        ignoreExceptions: true
    ));
}

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("IndustryDBContextConnection") ?? throw new InvalidOperationException("Connection string 'IndustryDBContextConnection' not found.");

builder.Services.AddDbContext<IndustryDBContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false).AddEntityFrameworkStores<IndustryDBContext>();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register our services for dependency injection
builder.Services.AddScoped<IndustryDB.Services.ICsvImportService, IndustryDB.Services.CsvImportService>();
builder.Services.AddScoped<IndustryDB.Services.ITradeDataRepository, IndustryDB.Services.TradeDataRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
