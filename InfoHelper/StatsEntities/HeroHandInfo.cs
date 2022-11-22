namespace InfoHelper.StatsEntities
{
    public class HeroHandInfo
    {
        public double Equity { get; init; }

        public HandType HandType { get; init; } = HandType.None;
    }

    public enum HandType
    {
        None,
        MadeHand,
        DrawHand,
        ComboHand
    }
}
