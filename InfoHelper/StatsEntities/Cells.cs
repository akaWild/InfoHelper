using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoHelper.StatsEntities
{
    public class DataCell
    {
        public string Name { get; }

        public DataCell(string name)
        {
            Name = name;
        }
    }

    public class ValueCell : DataCell
    {
        public int Attempts { get; set; }

        public object Data { get; set; }

        public ValueCell(string name) : base(name) { }
    }

    public class StatsCell : ValueCell
    {
        public int Mades { get; set; }

        public StatsCell ConnectedCell { get; set; }

        public StatsCell(string name) : base(name) { }
    }
}
