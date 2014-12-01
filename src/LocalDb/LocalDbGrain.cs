using System;
using Sandbox.Core;

namespace Sandbox.LocalDb
{
    public class LocalDbGrain : IGrain, ILocalDbConfiguration
    {
        public LocalDbGrain()
        {
            Version = LocalDb.Versions.V11;
            DatabasePrefix = null;
        }

        public string DatabasePrefix { get; set; }
        public LocalDb Instance { get; set; }

        public string Version { get; set; }

        public void Dispose()
        {
            if (Instance != null)
                Instance.Dispose();
        }

        public void Setup(Core.Sandbox sandbox)
        {
            var name = string.IsNullOrWhiteSpace(DatabasePrefix)
                ? null
                : string.Format("{0}_{1}", DateTime.Now.Ticks);

            Instance = new LocalDb(name, Version, sandbox.Location);
        }
    }
}