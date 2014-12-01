namespace IisExpress
{
    public interface IIisExpressConfiguration
    {
        string Source { get; set; }
        string IisExpressInstallationPath { get; set; }
        IIisExpressConfiguration UseHttp(bool use = true);
        IIisExpressConfiguration UseHttps(bool use = true);
    }
}