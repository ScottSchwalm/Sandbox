namespace Sandbox.LocalDb
{
    public interface ILocalDbConfiguration
    {
        string DatabasePrefix { get; set; }
        string Version { get; set; }
    }
}