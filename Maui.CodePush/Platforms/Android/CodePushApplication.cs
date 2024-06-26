using Android.Runtime;

namespace Maui.CodePush;

public abstract class CodePushApplication : MauiApplication
{
    protected CodePushApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
        AppDomain.CurrentDomain.AssemblyResolve += CodePush.AssemblyResolve;
    }
}
