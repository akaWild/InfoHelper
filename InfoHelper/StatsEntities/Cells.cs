using System;
using System.Linq;

namespace InfoHelper.StatsEntities
{
    public abstract class DataCell : ICloneable
    {
        protected double Value = 0;

        public string Name { get; }

        public string Description { get; }

        public object CellData { get; set; }

        public DataCell[] ConnectedCells { get; set; }

        public BetRange[] BetRanges { get; set; }

        public double DefaultValue { get; set; } = double.NaN;

        public int Sample { get; protected set; }

        public object ShallowCopy => MemberwiseClone();

        public CellSelectedState CellSelectedState { get; set; } = CellSelectedState.NotSelected;

        protected DataCell(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public void IncrementValue(double value = 1)
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

            dc.Value = Value;
            dc.CellData = CellData;
            dc.BetRanges = BetRanges?.Select(br => br with { }).ToArray();
            dc.DefaultValue = DefaultValue;
            dc.CellSelectedState = CellSelectedState;
            dc.Sample = Sample;

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
        public int LowBound { get; init; }

        public int UpperBound { get; init; }
    }

    public enum CellSelectedState
    {
        Missed,
        Selected,
        NotSelected
    }
}
