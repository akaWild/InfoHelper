using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoHelper.StatsEntities
{
    [Flags]
    public enum GameType
    {
        Hu = 0x01,
        SixMax = 0x02,
        Any = 0x03
    }

    [Flags]
    public enum Round
    {
        Preflop = 0x01,
        Postflop = 0x02,
        Any = 0x03
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
    public enum RelativePosition
    {
        Oop = 0x01,
        Ip = 0x02,
        Any = 0x03
    }

    [Flags]
    public enum PlayersOnFlop
    {
        Hu = 0x01,
        Multiway = 0x02,
        Any = 0x03
    }

    [Flags]
    public enum PreflopPotType
    {
        Unknown = 0x00,
        LimpPot = 0x01,
        RaisePot = 0x02,
        IsolatePot = 0x04,
        ThreeBetPot = 0x08,
        SqueezePot = 0x10,
        RaiseIsolatePot = 0x20,
        FourBetPot = 0x40,
        FiveBetPot = 0x80,
        Any = 0xFF
    }

    [Flags]
    public enum PreflopActions
    {
        Check = 0x01,
        Call = 0x02,
        CallCall = 0x04,
        CallRaise = 0x08,
        Raise = 0x10,
        RaiseCall = 0x20,
        RaiseRaise = 0x40,
        AnyCall = 0x80,
        AnyRaise = 0x100,
        Any = 0x1FF
    }

    [Flags]
    public enum SetType
    {
        General = 0x01,
        PreflopBtn = 0x02,
        PreflopSb = 0x04,
        PreflopBb = 0x08,
        PreflopEp = 0x10,
        PreflopMp = 0x20,
        PreflopCo = 0x40,
        PreflopSbvsBb = 0x80,
        PreflopBbvsSb = 0x100,
        PostflopHuIp = 0x200,
        PostflopHuOop = 0x400,
        PostflopGeneral = 0x800,
        Any = 0xFFF
    }

    public class StatSet : ICloneable
    {
        public DataCell[] Cells { get; set; }

        public GameType GameType { get; init; }

        public Round Round { get; init; }

        public Position Position { get; init; }

        public Position OppPosition { get; init; }

        public RelativePosition RelativePosition { get; init; }

        public PlayersOnFlop PlayersOnFlop { get; init; }

        public PreflopPotType PreflopPotType { get; init; }

        public PreflopActions PreflopActions { get; init; }

        public SetType SetType { get; init; }

        public object Clone()
        {
            StatSet ss = new StatSet()
            {
                GameType = GameType,
                Round = Round,
                Position = Position,
                RelativePosition = RelativePosition,
                OppPosition = OppPosition,
                PlayersOnFlop = PlayersOnFlop,
                PreflopPotType = PreflopPotType,
                PreflopActions = PreflopActions,
                SetType = SetType,
            };

            return ss;
        }

        public override string ToString()
        {
            return $"Game type: {GameType}\r\nRound: {Round}\r\nPosition: {Position}\r\nOpp position: {OppPosition}\r\nRelative position: {RelativePosition}\r\n" +
                   $"Players on flop: {PlayersOnFlop}\r\nPreflop pot type: {PreflopPotType}\r\nPreflop actions: {PreflopActions}\r\nSet type: {SetType}";
        }
    }
}
