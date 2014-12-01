using System;
using System.Diagnostics;
using Xunit;

namespace Sandbox.Core.Tests
{
    public class SandboxTests : IDisposable
    {
        public Sandbox Sandbox { get; set; }

        public SandboxTests()
        {
            Sandbox = new Sandbox();
        }

        [Fact]
        public void Can_create_a_sandbox()
        {
            Assert.NotNull(Sandbox);
        }

        [Fact]
        public void Can_play_in_a_sandbox()
        {
            Assert.NotNull(Sandbox.Play());
        }

        [Fact]
        public void Can_setup_a_grain()
        {
            using (var sb = new Sandbox())
            {
                var sandbox = sb as dynamic;
                sandbox.Test = new TestGrain();
                sandbox.Play();

                Assert.True(sandbox.Test.IsSetup);
            }
        }

        [Fact]
        public void Can_dispose_of_grains()
        {
            var sandbox = new Sandbox() as dynamic;
            sandbox.Test = new TestGrain();
            sandbox.Dispose();
            Assert.True(sandbox.Test.IsDisposed);
        }

        public void Dispose()
        {
            Sandbox.Dispose();
        }
    }

    public class TestGrain : IGrain
    {
        public bool IsDisposed { get; protected set; }
        public bool IsSetup { get; protected set; }

        public void Dispose()
        {
            IsDisposed = true;
        }

        public void Setup(Sandbox sandbox)
        {
            Debug.WriteLine("Setting up Test Grain.");
            IsSetup = true;
        }
    }
}
