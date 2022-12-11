using HandUtility;
using InfoHelper.DataProcessor;

namespace InfoHelper.StatsEntities
{
    public class HeroHandInfo
    {
        public double Equity { get; init; }

        public HandType HandType { get; init; } = HandType.None;
    }
}
