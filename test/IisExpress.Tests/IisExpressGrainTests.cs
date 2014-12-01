using System.Collections.Generic;
using System.IO;
using System.Net;
using Xunit;

namespace Sandbox.IisExpress.Tests
{
    public class IisExpressGrainTests
    {
        public const string Html = "<html><body><h1>Hello World!</h1></body></html>";

        [Fact]
        public void Can_register_iis_express_grain()
        {
            var src = CreateTempTestDirectory();

            using (var sandbox = new Core.Sandbox().UseIisExpress(src).Play() as dynamic)
            {
                var url = sandbox.IisExpress.Instance.Endpoints[0] as string;
                Assert.Equal(Html, Get(url));
            }
        }

        [Fact]
        public void Can_register_iis_express_grain_with_https()
        {
            var src = CreateTempTestDirectory();

            using (var sandbox = new Core.Sandbox().UseIisExpress((box, cfg) =>
            {
                cfg.Source = src;
                cfg.UseHttps();
            }).Play() as dynamic)
            {
                var endpoints = sandbox.IisExpress.Instance.Endpoints;
                var url = endpoints[1] as string;
                Assert.Equal(Html, Get(url));
            }
        }

        private string CreateTempTestDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            File.WriteAllText(Path.Combine(path, "index.html"), Html);
            File.WriteAllText(Path.Combine(path, "web.config"), "<?xml version=\"1.0\"?><configuration><system.webServer><defaultDocument enabled=\"true\"><files><clear /><add value=\"index.html\" /></files></defaultDocument></system.webServer></configuration>");

            return path;
        }

        private static string Get(string url)
        {
             var request = WebRequest.CreateHttp(url) as HttpWebRequest;
            using (var response = request.GetResponse())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
