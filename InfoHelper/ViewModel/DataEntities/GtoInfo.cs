using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GtoUtility;

namespace InfoHelper.ViewModel.DataEntities
{
    public class GtoInfo
    {
        public string Title { get; init; }

        public int Round { get; init; }

        public GtoStrategyContainer GtoStrategyContainer { get; init; }

        public GtoStrategy[] PocketStrategies { get; init; }

        public GtoDiffs GtoDiffs { get; init; }

        public string Pocket { get; init; }

        public string PocketRender { get; init; }
    }
}
