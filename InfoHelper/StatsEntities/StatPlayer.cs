using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoHelper.StatsEntities
{
    public class StatPlayer
    {
        public StatSet[] StatSets { get; init; }

        public DateTime LastAccessTime { get; set; } = DateTime.Now;
    }
}
