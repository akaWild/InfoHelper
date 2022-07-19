using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoHelper.StatsEntities
{
    public class StatsPlayer
    {
        public GeneralStatsSet GeneralStats { get; } = new GeneralStatsSet();

        public PreflopStatsSet[] PreflopStats { get; }

        public PostflopStatsSet[] PostflopStats { get; }

        public StatsPlayer()
        {
            PreflopStats = new PreflopStatsSet[8]
            {
                new PreflopStatsSet(Gametype.SixMax, Position.Sb),
                new PreflopStatsSet(Gametype.SixMax, Position.Bb),
                new PreflopStatsSet(Gametype.SixMax, Position.Ep),
                new PreflopStatsSet(Gametype.SixMax, Position.Mp),
                new PreflopStatsSet(Gametype.SixMax, Position.Co),
                new PreflopStatsSet(Gametype.SixMax, Position.Btn),
                new PreflopStatsSet(Gametype.Hu, Position.Sb | Position.Btn),
                new PreflopStatsSet(Gametype.Hu, Position.Bb),
            };
        }
    }
}
