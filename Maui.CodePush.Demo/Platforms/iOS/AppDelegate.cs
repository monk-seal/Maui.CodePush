using Foundation;

namespace Maui.CodePush.Demo;

[Register("AppDelegate")]
public class AppDelegate : CodePushAppDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
