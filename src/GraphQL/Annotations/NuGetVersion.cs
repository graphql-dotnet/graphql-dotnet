namespace GraphQL;

[AttributeUsage(AttributeTargets.Assembly)]
internal class NuGetVersionAttribute : Attribute
{
    public NuGetVersionAttribute(string version)
    {
        Version = version;
    }

    public string Version { get; }
}
