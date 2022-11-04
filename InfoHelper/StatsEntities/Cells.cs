using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoHelper.StatsEntities
{
    public abstract class DataCell : ICloneable
    {
        protected double Value = 0;

        public string Name { get; }

        public string Description { get; }

        public object CellData { get; set; }

        public DataCell[] ConnectedCells { get; set; }

        public double DefaultValue { get; set; } = double.NaN;

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

        public abstract double CalculatedValue { get; }

        public int Sample { get; protected set; }

        public abstract object Clone();
    }

    public class ValueCell : DataCell
    {
        public ValueCell(string name, string description) : base(name, description) { }

        public override double CalculatedValue => Sample;

        public override object Clone()
        {
            ValueCell vc = new ValueCell(Name, Description)
            {
                Value = Value,
                CellData = CellData,
                DefaultValue = DefaultValue,
                Sample = Sample
            };

            return vc;
        }
    }

    public class StatCell : DataCell
    {
        public StatCell(string name, string description): base(name, description) { }

        public override double CalculatedValue => Value * 100 / Sample;

        public override object Clone()
        {
            StatCell sc = new StatCell(Name, Description)
            {
                Value = Value,
                CellData = CellData,
                DefaultValue = DefaultValue,
                Sample = Sample
            };

            return sc;
        }
    }

    public class EvCell : DataCell
    {
        public EvCell(string name, string description) : base(name, description) { }

        public override double CalculatedValue => Value * 100 / Sample;

        public override object Clone()
        {
            EvCell ec = new EvCell(Name, Description)
            {
                Value = Value,
                CellData = CellData,
                DefaultValue = DefaultValue,
                Sample = Sample
            };

            return ec;
        }
    }
}
