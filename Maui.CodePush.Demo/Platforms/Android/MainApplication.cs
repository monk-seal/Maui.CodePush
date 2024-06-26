using Android.App;
using Android.Runtime;

namespace Maui.CodePush.Demo;

[Application]
public class MainApplication : CodePushApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
