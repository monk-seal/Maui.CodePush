using Android.App;
using Android.Runtime;
using System.Diagnostics;
using System.Net;
using System.Reflection;

namespace Maui.CodePush
{
    [Application]
    public class CodePushApplication : MauiApplication
    {
        private static readonly string _basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Modules");
        private static readonly string _tempPath = Path.Combine(Path.GetTempPath(), "Modules");

        protected CodePushApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }


        protected override MauiApp CreateMauiApp()
        {
            var officialAssembly = AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(a=> a.FullName == "Maui.CodePush.Demo");

            var assembly = AppDomain.CurrentDomain.Load("Maui.CodePush.Demo");
            var type = assembly.GetType("Maui.CodePush.Demo.MauiProgram");

            var method = type?.GetMethod("CreateMauiApp");
            var mauiApp = method?.Invoke(null, null);

            return mauiApp as MauiApp;
        }

        public override void OnCreate()
        {
            //CheckAssemblies();

            base.OnCreate();
        }


        private static Assembly CurrentDomain_AssemblyResolve(object? sender, ResolveEventArgs args)
        {
            var embeddedAssembly = new AssemblyName(args.Name);
            var moduleName = $"{embeddedAssembly.Name}.dll";
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

                //Caso corrompa a dll no download, pegamos uma versao estavel embarcada
                try
                {
                    var localDllFile = Path.Combine(_basePath, moduleName);
                    UnpackEmbeddedReference(moduleName, dllLocalPath, true);
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

        public static void UnpackEmbeddedReference(string moduleName, string localDllFile, bool deleteIfExists = false)
        {
            var aplication = Assembly.GetExecutingAssembly().GetName().Name;
            var resourceName = $"{aplication.Replace(" ", "_")}.Modules.{moduleName}";
            using (var embedded = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
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
        }

        private static string ResolveAssembly(string moduleName)
        {
            // Se existir versão mais nova baixada, usa ela
            var tempLocalDll = Path.Combine(_tempPath, moduleName);
            var localDllFile = Path.Combine(_basePath, moduleName);
            if (File.Exists(tempLocalDll))
                File.Copy(tempLocalDll, localDllFile, true);

            if (File.Exists(localDllFile))
                return localDllFile;

            // Caso contrário usa a versão embarcada
            UnpackEmbeddedReference(moduleName, localDllFile);

            return localDllFile;
        }

        public static void CheckAssemblies()
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), "AppModule.dll");
            using (var client = new WebClient())
            {
                //Download da dll no github
                client.DownloadFile("https://github.com/felipebaltazar/felipebaltazar/blob/output/AppModule.dll?raw=true", tempFilePath);
            }

            // Verifica se baixou corretamente
            if (!File.Exists(tempFilePath))
                return;

            // Busca o assembly carregado atualmente
            var assembly = AppDomain.CurrentDomain
                                    .GetAssemblies()
                                    .FirstOrDefault(a => a.GetName().Name == "AppModule");

            // Utiliza a localização do assembly carregado atualmente
            var filePath = assembly.Location;

            // Busca a versão das dlls (nova e atual)
            var newAssembly = FileVersionInfo.GetVersionInfo(tempFilePath)?.FileVersion ?? "1.0.0.0";
            var currentAssembly = FileVersionInfo.GetVersionInfo(filePath)?.FileVersion ?? "1.0.0.0";

            // Compara se a versão é a mesma
            if (!(new Version(currentAssembly) < new Version(newAssembly)))
                return;

            // Sobreescreve uma dll antiga
            File.Copy(tempFilePath, filePath, true);
        }
    }
}
