using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoHelper.StatsEntities
{
    public class StatPlayer : IDisposable
    {
        private bool _disposed;

        public StatSet[] StatSets { get; set; }

        public DateTime LastAccessTime { get; set; } = DateTime.Now;

        ~StatPlayer()
        {
            ReleaseResources();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            ReleaseResources();

            _disposed = true;

            GC.SuppressFinalize(this);
        }

        private void ReleaseResources()
        {
            StatSets = null;
        }
    }
}
