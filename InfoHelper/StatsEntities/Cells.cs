using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoHelper.StatsEntities
{
    public abstract class DataCell
    {
        protected double Value = 0;

        public string Name { get; }

        public string Description { get; }

        public object Data { get; set; }

        public StatsCell ConnectedCell { get; set; }

        public double DefaultValue { get; set; } = double.MinValue;

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

        public int Sample { get; private set; }
    }

    public class ValueCell : DataCell
    {
        public ValueCell(string name, string description) : base(name, description) { }

        public override double CalculatedValue => Sample;
    }

    public class StatsCell : DataCell
    {
        public StatsCell(string name, string description): base(name, description) { }

        public override double CalculatedValue => Value * 100 / Sample;
    }

    public class EvCell : DataCell
    {
        public EvCell(string name, string description) : base(name, description) { }

        public override double CalculatedValue => Value / 100;
    }
}
