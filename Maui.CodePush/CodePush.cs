using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using static Maui.CodePush.AppBuilderExtensions;

namespace Maui.CodePush;

public static class CodePush
{
    private static readonly string _basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Modules");
    private static readonly string _tempPath = Path.Combine(Path.GetTempPath(), "Modules");

    internal static CodePushAssemblyRegister Register = new();

    [ModuleInitializer]
    public static void Setup()
    {
        if (!Directory.Exists(_basePath))
            Directory.CreateDirectory(_basePath);

        if (!Directory.Exists(_tempPath))
            Directory.CreateDirectory(_tempPath);
    }

    public static Assembly? AssemblyResolve(object? sender, ResolveEventArgs args)
    {
        var embeddedAssembly = new AssemblyName(args.Name);
        var moduleName = $"{embeddedAssembly.Name}.dll";

        if (!Register.Assemblies.Any(n => n == moduleName))
            return null;

        var dllLocalPath = ResolveAssembly(moduleName);

        if (string.IsNullOrWhiteSpace(dllLocalPath))
            return null;

        if (!File.Exists(dllLocalPath))
            return null;

        try
        {
            return Assembly.LoadFrom(dllLocalPath);
        }
        catch (BadImageFormatException e)
        {
            Console.WriteLine("Unable to load {0}.", dllLocalPath);
            Console.WriteLine(e.Message.Substring(0,
                              e.Message.IndexOf(".") + 1));

            try
            {
                var localDllFile = Path.Combine(_basePath, moduleName);
                TryUnpackEmbeededAssemmbly(moduleName, dllLocalPath);
                return Assembly.LoadFrom(dllLocalPath);
            }
            catch (BadImageFormatException ex)
            {
                Console.WriteLine("Unable to load {0}.", dllLocalPath);
                Console.WriteLine(ex.Message.Substring(0,
                                  ex.Message.IndexOf(".") + 1));
            }
        }

        return null;
    }

    private static Assembly TryUnpackEmbeededAssemmbly(string moduleName, string moduleFilePath)
    {
        try
        {
            UnpackEmbeddedReference(moduleName, moduleFilePath);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }

        if (!File.Exists(moduleFilePath))
            return null;

        return Assembly.LoadFrom(moduleFilePath);
    }

    private static void UnpackEmbeddedReference(string moduleName, string localDllFile, bool deleteIfExists = false)
    {
#if ANDROID
        var test = Platform.AppContext.Assets.List("");
        using (var embedded = Platform.AppContext.Assets.Open(moduleName))
        {
            if (embedded is null)
                return;

            if (deleteIfExists && File.Exists(localDllFile))
                File.Delete(localDllFile);

            using (var fileStream = File.Create(localDllFile))
            {
                embedded.CopyTo(fileStream);
            }
        }
#endif
    }

    private static string ResolveAssembly(string moduleName)
    {
        // Uses the newest version of the module
        var tempLocalDll = Path.Combine(_tempPath, moduleName);
        var localDllFile = Path.Combine(_basePath, moduleName);

        if (File.Exists(tempLocalDll))
            File.Copy(tempLocalDll, localDllFile, true);

        if (File.Exists(localDllFile))
            return localDllFile;

        // Resolve from assets
        UnpackEmbeddedReference(moduleName, localDllFile);

        return localDllFile;
    }

    public static void CheckUpdates()
    {
        using (var client = new WebClient())
        {
            //Download from server
            foreach (var assemblyName in Register.Assemblies)
            {
                var tempFilePath = Path.Combine(Path.GetTempPath(), assemblyName);
                client.DownloadFile($"https://raw.githubusercontent.com/felipebaltazar/felipebaltazar/inapp-update-tests/{assemblyName}", tempFilePath);

            }
        }

        foreach (var assemblyName in Register.Assemblies)
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), assemblyName);

            // Verifica se baixou corretamente
            if (!File.Exists(tempFilePath))
                return;

            // Search the assembly
            var fileName = Path.GetFileNameWithoutExtension(assemblyName);
            var assembly = AppDomain.CurrentDomain
                                    .GetAssemblies()
                                    .FirstOrDefault(a => a.GetName().Name == fileName);

            var filePath = assembly?.Location ?? tempFilePath;

            // Search about the version of the new assembly and the current assembly
            var newAssembly = FileVersionInfo.GetVersionInfo(tempFilePath)?.FileVersion ?? "1.0.0.0";
            var currentAssembly = FileVersionInfo.GetVersionInfo(filePath)?.FileVersion ?? "1.0.0.0";

            // Compare the new version with the current version
            // if (!(new Version(currentAssembly) < new Version(newAssembly)))
            //     return;

            // Override the old dll
            File.Copy(tempFilePath, filePath, true);
        }
    }

    public static void Rollback()
    {
        Directory.Delete(_basePath, true);
        Directory.Delete(_tempPath, true);

        Directory.CreateDirectory(_basePath);
        Directory.CreateDirectory(_tempPath);
    }
}
