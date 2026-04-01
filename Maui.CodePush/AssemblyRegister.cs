namespace Maui.CodePush;

internal class AssemblyRegister : IAssemblyCollection
{
    private const string EXTENSION = ".dll";

    private readonly HashSet<string> _assemblies = new();

    public ICollection<string> Assemblies => _assemblies;

    public void AddAssembly(string assemblyName)
    {
        var name = assemblyName;

        if (!Path.GetExtension(name).Equals(EXTENSION, StringComparison.OrdinalIgnoreCase))
        {
            name += EXTENSION;
        }

        if (!_assemblies.Add(name))
            throw new Exception($"{assemblyName} registred twice in codepush");
    }
}
