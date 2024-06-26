namespace Maui.CodePush;

public static class AppBuilderExtensions
{
    public static MauiAppBuilder UseCodePush(this MauiAppBuilder appBuilder, Action<IAssemblyCollection> setup)
    {
        setup?.Invoke(CodePush.Register);

#if ANDROID
        var applicationDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".__override__");
        foreach (var assembly in CodePush.Register.Assemblies)
        {
            Path.Combine(applicationDataFolder, assembly);
            if (File.Exists(assembly))
            {
                File.Delete(assembly);
            }
        }
#endif

        return appBuilder;
    }
}
