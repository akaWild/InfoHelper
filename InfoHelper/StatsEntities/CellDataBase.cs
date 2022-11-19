using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoHelper.StatsEntities
{
    public abstract class CellDataBase : ICloneable
    {
        public abstract object Clone();
    }
}
