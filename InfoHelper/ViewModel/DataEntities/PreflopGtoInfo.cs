using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GtoUtility;

namespace InfoHelper.ViewModel.DataEntities
{
    public class PreflopGtoInfo
    {
        public string Title { get; init; }

        public GtoStrategyContainer GtoStrategyContainer { get; init; }

        public GtoDiffs GtoDiffs { get; init; }

        public string Pocket { get; init; }
    }
}
