using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using IisExpress;
using Microsoft.Web.Administration;
using Sandbox.Core;

namespace Sandbox.IisExpress
{
    public class IisExpressGrain : IGrain, IIisExpressConfiguration
    {
        public IisExpressGrain()
        {
            IisExpressInstallationPath = IisExpress.DefaultIisExpressPath;
            Protocols = new List<string> { "http" };
        }

        public void Dispose()
        {
            Instance.Dispose();
            IisExpress.RemoveSiteFromApplicationHostConfig(Instance.Site.Name);
        }

        public void Setup(Core.Sandbox sandbox)
        {
            if (string.IsNullOrWhiteSpace(Source) || !Directory.Exists(Source))
                throw new ArgumentNullException("Source", "a source is required for iis express");

            // copy the source to the sandbox location
            Destination = Path.Combine(sandbox.Location, "Web");
            var destination = new DirectoryInfo(Destination);
            CopyAll(new DirectoryInfo(Source), destination);
            SetDestinationPermissions(new DirectoryInfo(sandbox.Location));

            // create an applicationHost.config
            var site = IisExpress.AddSiteToApplicationHostConfig(string.Format("sandbox_{0}", sandbox.Id), Destination, Protocols);

            // create new IisExpress instance
            Instance = new IisExpress(site.Name);

            Instance.Start();
        }

        private void SetDestinationPermissions(DirectoryInfo directory)
        {
            var security = directory.GetAccessControl();
            security.AddAccessRule(
            new FileSystemAccessRule("everyone", 
                FileSystemRights.FullControl | FileSystemRights.Read | FileSystemRights.ReadAndExecute | FileSystemRights.Modify,
                InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.InheritOnly, 
                AccessControlType.Allow));
            directory.SetAccessControl(security);
        }

        public string Destination { get; set; }

        public string Source { get; set; }
        public string IisExpressInstallationPath { get; set; }
        public IIisExpressConfiguration UseHttp(bool use = true)
        {
            if (use)
                Protocols.Add("http");
            else
                Protocols.Remove("http");

            Protocols = Protocols.Distinct().ToList();

            return this;
        }
        public IIisExpressConfiguration UseHttps(bool use = true)
        {
            if (use)
                Protocols.Add("https");
            else
                Protocols.Remove("https");

            Protocols = Protocols.Distinct().ToList();
            return this;
        }

        public IisExpress Instance { get; protected set; }
        public IList<string> Protocols { get; set; }

        protected static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            // Check if the target directory exists; if not, create it.
            if (!Directory.Exists(target.FullName)) {
                Directory.CreateDirectory(target.FullName);
            }

            // Copy each file into the new directory.
            foreach (var fi in source.GetFiles()) {
                Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (var subDirectory in source.GetDirectories()) {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(subDirectory.Name);
                CopyAll(subDirectory, nextTargetSubDir);
            }
        }
    }

    public static class IisExpressGrainExtensions
    {
        public static Core.Sandbox UseIisExpress(this Core.Sandbox sandbox, string source)
        {
            return UseIisExpress(sandbox, (box, cfg) => cfg.Source = source);
        }

        public static Core.Sandbox UseIisExpress(this Core.Sandbox sandbox, Action<dynamic, IIisExpressConfiguration> config = null)
        {
            var dynamicSb = sandbox as dynamic;
            var grain = new IisExpressGrain();

            dynamicSb.IisExpress = grain;

            if (config != null)
                config(sandbox, grain);

            return sandbox;
        }
    }
}