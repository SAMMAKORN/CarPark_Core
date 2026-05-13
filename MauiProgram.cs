using CarPark.Data;
using CarPark.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CarPark
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

            using var stream = FileSystem.OpenAppPackageFileAsync("appsettings.json")
                                .GetAwaiter()
                                .GetResult();

            var config = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build();

            builder.Configuration.AddConfiguration(config);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddDbContextFactory<AppDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });

            builder.Services.AddSingleton<CurrentUserContext>();
            builder.Services.AddScoped<UserAuthService>();
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<ParkingLotService>();
            builder.Services.AddScoped<ParkingRateRuleService>();
            builder.Services.AddScoped<ParkingGateService>();
            builder.Services.AddScoped<ParkingLotScheduleService>();
            builder.Services.AddScoped<ParkingTransactionService>();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}