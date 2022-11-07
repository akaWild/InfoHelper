using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoHelper.StatsEntities
{
    public class PostflopData
    {
        public HandsGroup MainGroup { get; } = new HandsGroup();

        public HandsGroup[] SubGroups { get; } = new HandsGroup[4]
        {
            new HandsGroup(),
            new HandsGroup(),
            new HandsGroup(),
            new HandsGroup()
        };
    }

    public class HandsGroup
    {
        public int[] MadeHands { get; } = new int[50];

        public int[] DrawHands { get; } = new int[50];
    }
}
