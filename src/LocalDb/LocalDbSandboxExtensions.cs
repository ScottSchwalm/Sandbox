using System;

namespace Sandbox.LocalDb
{
    public static class LocalDbSandboxExtensions
    {
        public static Core.Sandbox UseLocalDb(this Core.Sandbox sandbox, Action<dynamic, ILocalDbConfiguration> config = null)
        {
            var dynamicSb = sandbox as dynamic;
            var grain = new LocalDbGrain();

            dynamicSb.LocalDb = grain;

            if (config != null)
                config(sandbox, grain);

            return sandbox;
        }
    }
}