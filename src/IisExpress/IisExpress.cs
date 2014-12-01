using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.Administration;

namespace Sandbox.IisExpress
{
    public class IisExpress : IDisposable
    {
        public const string DefaultIisExpressPath = @"C:\Program Files\IIS Express";
        private const string ReadyMsg = @"IIS Express is running.";

        public Site Site { get; protected set; }

        private readonly ProcessStartInfo _startInfo;

        private Process _process;

        public IisExpress(string siteName, string iisInstallationPath = DefaultIisExpressPath, string applicationHostConfigPath = null)
        {
            ApplicationHostConfigPath = GetApplicationHostConfigPath(applicationHostConfigPath);

            using (var manager = new ServerManager(ApplicationHostConfigPath))
            {
                var site = manager.Sites.FirstOrDefault(x => siteName == x.Name);
                if (site == null)
                    throw new ArgumentException("no site with that name exists in applicationHost.config", siteName);
                Site = site;
            }
            
            var path = string.IsNullOrWhiteSpace(iisInstallationPath) ? DefaultIisExpressPath : iisInstallationPath;
            var arguments = string.Format("/site:{0} /config:\"{1}\"", Site.Name, ApplicationHostConfigPath);
            _startInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(path, "iisexpress.exe"),
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
        }

        private static string GetApplicationHostConfigPath(string applicationHostConfigPath)
        {
            return applicationHostConfigPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"Documents\IISExpress\config\applicationhost.config");
        }

        public string ApplicationHostConfigPath { get; set; }

        public int ProcessId { get; private set; }

        public Task Start(CancellationToken cancellationToken = default(CancellationToken))
        {
            var tcs = new TaskCompletionSource<object>();

            if (cancellationToken.IsCancellationRequested)
            {
                tcs.SetCanceled();
                return tcs.Task;
            }

            try
            {
                var proc = new Process { EnableRaisingEvents = true, StartInfo = _startInfo };

                DataReceivedEventHandler onOutput = null;
                onOutput =
                    (sender, e) =>
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            tcs.TrySetCanceled();
                        }

                        try
                        {
                            Debug.WriteLine("  [StdOut]\t{0}", (object)e.Data);

                            if (string.Equals(ReadyMsg, e.Data, StringComparison.OrdinalIgnoreCase))
                            {
                                proc.OutputDataReceived -= onOutput;
                                _process = proc;
                                tcs.TrySetResult(null);
                            }
                        }
                        catch (Exception ex)
                        {
                            tcs.TrySetException(ex);
                            proc.Dispose();
                        }
                    };
                proc.OutputDataReceived += onOutput;
                proc.ErrorDataReceived += (sender, e) => Debug.WriteLine("  [StdOut]\t{0}", (object)e.Data);
                proc.Exited += (sender, e) => Debug.WriteLine("  IIS Express exited.");

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                ProcessId = proc.Id;
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }

        public Task Stop()
        {
            var tcs = new TaskCompletionSource<object>(null);
            try
            {
                _process.Exited += (sender, e) => tcs.TrySetResult(null);

                SendStopMessageToProcess(ProcessId);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }

        public void Quit()
        {
            Process proc;
            if ((proc = Interlocked.Exchange(ref _process, null)) != null)
            {
                proc.Kill();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Quit();
            }
        }

        public static IEnumerable<int> GetAvailablePorts(int count = 1)
        {
            var listeners = Enumerable.Range(1, count)
                .Select(x => new TcpListener(IPAddress.Loopback, 0))
                .ToList();

            listeners.ForEach(x => x.Start());
            var ports = listeners.Select(x => ((IPEndPoint) x.LocalEndpoint).Port).ToList();
            listeners.ForEach(x => x.Stop());

            return ports;
        }

        public static int GetAvailablePort()
        {
            return GetAvailablePorts().First();
        }

        public static Site AddSiteToApplicationHostConfig(string name, string path, IList<string> protocols, string applicationHostConfigPath = null)
        {
            if (protocols == null || !protocols.Any())
                throw new ArgumentException("at least one protocol is required", "protocols");

            using (var manager = new ServerManager(GetApplicationHostConfigPath(applicationHostConfigPath)))
            {
                var site = manager.Sites.Add(name, path, 0);
                site.Applications.Clear();
                var application=  site.Applications.Add("/", path);
                application.ApplicationPoolName = "Clr4IntegratedAppPool";
                site.Bindings.RemoveAt(0);
                var ports = GetAvailablePorts(protocols.Count).ToList();
                for (var i = 0; i < protocols.Count; i++)
                {
                    var protocol = protocols[i];
                    var port = protocol == "https" ? 44399 : ports[i];
                    site.Bindings.Add(string.Format("*:{0}:localhost", port), protocol);
                }

                manager.CommitChanges();

                return site;
            }
        }

        public static void RemoveSiteFromApplicationHostConfig(string name, string applicationHostConfigPath = null)
        {
            using (var manager = new ServerManager(GetApplicationHostConfigPath(applicationHostConfigPath)))
            {
                var site = manager.Sites.FirstOrDefault(x => name == x.Name);
                if (site != null)
                    manager.Sites.Remove(site);

                manager.CommitChanges();
            }
        }

        public IList<string> Endpoints
        {
            get
            {
                 return Site.Bindings
                    .Select(x => string.Format("{0}://{1}:{2}", x.Protocol, x.Host, x.EndPoint.Port))
                    .ToList();
            }
        }

        private static void SendStopMessageToProcess(int pid)
        {
            try
            {
                for (var ptr = NativeMethods.GetTopWindow(IntPtr.Zero); ptr != IntPtr.Zero; ptr = NativeMethods.GetWindow(ptr, 2))
                {
                    uint num;
                    NativeMethods.GetWindowThreadProcessId(ptr, out num);
                    if (pid == num)
                    {
                        var hWnd = new HandleRef(null, ptr);
                        NativeMethods.PostMessage(hWnd, 0x12, IntPtr.Zero, IntPtr.Zero);
                        return;
                    }
                }
            }
            catch (ArgumentException)
            {
            }
        }
    }

    internal static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetTopWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostMessage(HandleRef hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    }
}
