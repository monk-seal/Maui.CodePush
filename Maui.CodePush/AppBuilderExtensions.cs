namespace Maui.CodePush;

public static class AppBuilderExtensions
{
    public static MauiAppBuilder UseCodePush(this MauiAppBuilder appBuilder, Action<CodePushOptions> configure)
    {
        var options = new CodePushOptions();
        configure(options);

        CodePush.Initialize(options);

        return appBuilder;
    }
}
