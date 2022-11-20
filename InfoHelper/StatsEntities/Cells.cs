using System;
using System.Linq;

namespace InfoHelper.StatsEntities
{
    public abstract class DataCell : ICloneable
    {
        protected float Value = 0;

        public string Name { get; }

        public string Description { get; }

        public object CellData { get; set; }

        public DataCell[] ConnectedCells { get; set; }

        public BetRange[] BetRanges { get; set; }

        public float DefaultValue { get; set; } = float.NaN;

        public uint Sample { get; protected set; }

        public object ShallowCopy => MemberwiseClone();

        public CellSelectedState CellSelectedState { get; set; } = CellSelectedState.NotSelected;

        protected DataCell(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public void IncrementValue(float value = 1)
        {
            Value += value;
        }

        public void IncrementSample()
        {
            Sample++;
        }

        public object Clone()
        {
            DataCell dc = (DataCell)MemberwiseClone();

            dc.CellData = ((CellDataBase)CellData)?.Clone();
            dc.BetRanges = BetRanges?.Select(br => br with { }).ToArray();

            return dc;
        }

        public abstract double CalculatedValue { get; }
    }

    public class ValueCell : DataCell
    {
        public ValueCell(string name, string description) : base(name, description) { }

        public override double CalculatedValue => Sample;
    }

    public class StatCell : DataCell
    {
        public StatCell(string name, string description): base(name, description) { }

        public override double CalculatedValue => Value * 100 / Sample;
    }

    public class EvCell : DataCell
    {
        public EvCell(string name, string description) : base(name, description) { }

        public override double CalculatedValue => Value * 100 / Sample;
    }

    public record BetRange
    {
        public ushort LowBound { get; init; }

        public ushort UpperBound { get; init; }
    }

    public enum CellSelectedState : byte
    {
        Missed,
        Selected,
        NotSelected
    }
}
