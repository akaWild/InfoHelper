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
    public enum PlayerPosition
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
    public enum PrevRoundActingType
    {
        Aggressor = 0x01,
        Caller = 0x02,
        NoRaise = 0x04,
        Any = 0x07
    }

    [Flags]
    public enum PlayersOnFlop
    {
        Hu = 0x01,
        Multiway = 0x02,
        Any = 0x03
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

    public class StatsSet
    {
        public List<DataCell> Cells { get; } = new List<DataCell>();

        public PanelType PanelType { get; }

        public Gametype GameType { get; }

        public PlayerPosition Position { get; }

        public PlayerPosition VsPosition { get; }

        public PreflopPotType PreflopPotType { get; }

        public PreflopActingType PreflopActingType { get; }

        public PrevRoundActingType PrevRoundActingType { get; }

        public PlayersOnFlop PlayersOnFlop { get; }

        public RelativePosition RelativePosition { get; }

        public PostflopRound PostflopRound { get; }
    }
}
