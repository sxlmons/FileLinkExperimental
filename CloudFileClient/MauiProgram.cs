using CloudFileClient.Pages;
using CloudFileClient.Services;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace CloudFileClient
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
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    
                    // Add Inter font family
                    fonts.AddFont("Inter_18-Regular.ttf", "InterRegular");
                    fonts.AddFont("Inter_18-Medium.ttf", "InterMedium");
                    fonts.AddFont("Inter_18-SemiBold.ttf", "InterSemiBold");
                    fonts.AddFont("Inter_18-Bold.ttf", "InterBold");
                })
                .UseSkiaSharp();
            
            // Register services
            builder.Services.AddSingleton<NetworkService>();
            builder.Services.AddSingleton<AuthenticationService>();
            builder.Services.AddSingleton<FileService>();
            builder.Services.AddSingleton<DirectoryService>();

            // Register pages
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<MainPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}