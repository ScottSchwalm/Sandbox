using System.Data.SqlClient;
using System.IO;
using Xunit;

namespace Sandbox.LocalDb.Tests
{
    public class LocalDbGrainTests
    {
        [Fact]
        public void Can_add_localdb_grain_to_sandbox()
        {
            var sandbox = new Core.Sandbox().UseLocalDb() as dynamic;

            Assert.NotNull(sandbox.LocalDb);
            Assert.IsType<LocalDbGrain>(sandbox.LocalDb);
        }

        [Fact]
        public void Can_setup_and_tear_down_localdb()
        {
            var path = string.Empty;
            using (var sandbox = new Core.Sandbox().UseLocalDb() as dynamic)
            {
                sandbox.Play();
                using (var connection = sandbox.LocalDb.Instance.OpenConnection() as SqlConnection)
                {
                    path = sandbox.LocalDb.Instance.Location;
                    var dataTable = connection.GetSchema();
                    Assert.NotNull(dataTable);
                }
            }

            Assert.False(Directory.Exists(path));
        }

        [Fact]
        public void Can_configure_localdb_using_action()
        {
            var sandbox =
                new Core.Sandbox()
                    .UseLocalDb((sb, cfg) =>
                    {
                        cfg.DatabasePrefix = "test";
                        cfg.Version = LocalDb.Versions.V12;
                    }) as dynamic;

            Assert.Equal("test", sandbox.LocalDb.DatabasePrefix);
            Assert.Equal(LocalDb.Versions.V12, sandbox.LocalDb.Version);
        }

        [Fact]
        public void Null_when_not_setup()
        {
            using (var sandbox = new Core.Sandbox().UseLocalDb() as dynamic)
            {
                Assert.Null(sandbox.LocalDb.Instance);
            }
        }
    }
}
