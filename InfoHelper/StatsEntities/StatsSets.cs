using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoHelper.StatsEntities
{
    public enum PanelType
    {
        General,
        Preflop,
        Postflop
    }

    public enum Gametype
    {
        Hu,
        SixMax
    }

    [Flags]
    public enum Position
    {
        Sb = 0x01,
        Bb = 0x02,
        Ep = 0x04,
        Mp = 0x08,
        Co = 0x10,
        Btn = 0x20,
        Any = 0x3F
    }

    [Flags]
    public enum VsPosition
    {
        Sb = 0x01,
        Bb = 0x02,
        Ep = 0x04,
        Mp = 0x08,
        Co = 0x10,
        Btn = 0x20,
        Any = 0x3F
    }

    [Flags]
    public enum PreflopPotType
    {
        Unopened = 0x01,
        Limp = 0x02,
        Raise = 0x04,
        Isolate = 0x08,
        ThreeBet = 0x10,
        Squeeze = 0x20,
        RaiseIsolate = 0x40,
        FourBet = 0x80,
        FiveBet = 0x100,
        Any = 0x1FF
    }

    [Flags]
    public enum PreflopActingType
    {
        Caller = 0x01,
        ColdCaller = 0x02,
        LimpCaller = 0x04,
        CallerCaller = 0x08,
        RaiseCaller = 0x10,
        Raiser = 0x20,
        LimpRaiser = 0x40,
        CallerRaiser = 0x80,
        Any = 0xFF
    }

    [Flags]
    public enum PrevRoundActingType
    {
        Aggressor = 0x01,
        Caller = 0x02,
        NoRaise = 0x04,
        Any = 0x07
    }

    [Flags]
    public enum RelativePosition
    {
        Ip = 0x01,
        Oop = 0x02,
        Any = 0x03
    }

    [Flags]
    public enum PostflopRound
    {
        Flop = 0x01,
        Turn = 0x02,
        River = 0x04,
        Any = 0x07
    }

    public class StatsSetBase
    {
        public List<DataCell> Cells { get; } = new List<DataCell>();
    }

    public class GeneralStatsSet : StatsSetBase
    {
        public GeneralStatsSet()
        {
            Cells.Add(new ValueCell("Hands", "Total hands played"));
            Cells.Add(new StatsCell("Vpip", "Voluntarily put in the pot"));
            Cells.Add(new StatsCell("Pfr", "Preflop raise"));
            Cells.Add(new StatsCell("AggFq_F", "Aggression frequency flop"));
            Cells.Add(new StatsCell("AggFq_T", "Aggression frequency turn"));
            Cells.Add(new StatsCell("AggFq_R", "Aggression frequency river"));
            Cells.Add(new StatsCell("WentToSd", "Went to showdown"));
            Cells.Add(new StatsCell("WonOnSd", "Won at showdown"));
            Cells.Add(new ValueCell("EvBb", "EvBb/100 hands"));
        }
    }

    public class PreflopStatsSet : StatsSetBase
    {
        public Gametype GameType { get; }

        public Position Position { get; }

        public PreflopStatsSet(Gametype gameType, Position position)
        {
            GameType = gameType;
            Position = position;

            Cells.AddRange(CellsManager.GetPreflopCells(gameType, position));
        }
    }

    public class PostflopStatsSet : StatsSetBase
    {
        public Gametype GameType { get; }

        public Position Position { get; }

        public VsPosition VsPosition { get; }

        public PreflopPotType PreflopPotType { get; }

        public PreflopActingType PreflopActingType { get; }

        public PrevRoundActingType PrevRoundActingType { get; }

        public RelativePosition RelativePosition { get; }

        public PostflopRound PostflopRound { get; }

        public PostflopStatsSet(Gametype gameType, Position position, VsPosition vsPosition, PreflopPotType preflopPotType, PreflopActingType preflopActingType, PrevRoundActingType prevRoundActingType,
            RelativePosition relativePosition, PostflopRound postflopRound)
        {
            GameType = gameType;
            Position = position;
            VsPosition = vsPosition;
            PreflopPotType = preflopPotType;
            PreflopActingType = preflopActingType;
            PrevRoundActingType = prevRoundActingType;
            RelativePosition = relativePosition;
            PostflopRound = postflopRound;
        }
    }
}
