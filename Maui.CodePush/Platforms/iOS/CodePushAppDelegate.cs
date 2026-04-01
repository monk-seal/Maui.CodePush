namespace Maui.CodePush;

public abstract class CodePushAppDelegate : MauiUIApplicationDelegate
{
    internal static string AssemblyName { get; private set; } = string.Empty;

    protected CodePushAppDelegate() : base()
    {
        AssemblyName = GetType()?.Assembly?.GetName()?.Name ?? string.Empty;
    }
}
