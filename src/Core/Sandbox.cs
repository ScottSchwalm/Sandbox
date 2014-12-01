using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Sandbox.Core
{
    public class Sandbox : Expando, IDisposable
    {
        public Sandbox()
        {
            Id = DateTime.UtcNow.Ticks;
            Description = string.Format("Sandbox <{0}>", Id);

            Location = Path.Combine(Path.GetTempPath(), string.Format("sandbox_{0}", Path.GetRandomFileName()));
        }

        public long Id { get; set; }
        public Sandbox SetDescription(string description)
        {
            Description = description;
            return this;
        }

        public string Description { get; protected set; }
        public string Location { get; protected set; }

        public Sandbox Play()
        {
            CreateTemporaryDirectory();
            Grains.ToList().ForEach(x => x.Setup(this));

            return this;
        }

        private void CreateTemporaryDirectory()
        {
            if (!Directory.Exists(Location))
                Directory.CreateDirectory(Location);
        }

        public void Dispose()
        {
            Grains.ToList().ForEach(x => x.Dispose());
            TryDeleteFilesAndFoldersRecursively(Location);
        }

        public static void TryDeleteFilesAndFoldersRecursively(string directory)
        {
            for (var count = 0; count < 3; count++)
            {
                try
                {
                    Directory.Delete(directory, true);
                    return;
                }
                catch (IOException)
                {
                    count++;
                    Thread.Sleep(1);
                }
                catch (UnauthorizedAccessException)
                {
                    // shrug ?
                }
            }
        }

        public IList<IGrain> Grains
        {
            get
            {
                 return Properties
                    .Select(x => x.Value)
                    .Where(x => x is IGrain)
                    .Cast<IGrain>()
                    .ToList();
            }
        }
    }

    public interface IGrain : IDisposable
    {
        void Setup(Sandbox sandbox);
    }
}
