using Microsoft.Extensions.Logging;

namespace Maui.CodePush.Demo;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseCodePush(options =>
            {
                options.AddModule("Maui.CodePush.Demo.Feature");

                // Phase 2: configure server
                // options.ServerUrl = "https://codepush.example.com";
                // options.AppKey = "demo-app-key";
                // options.CheckOnStartup = true;
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
