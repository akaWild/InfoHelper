using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoHelper.StatsEntities
{
    public class PostflopData : CellDataBase
    {
        public HandsGroup MainGroup { get; private set; } = new HandsGroup();

        public HandsGroup[] SubGroups { get; private set; } = new HandsGroup[4]
        {
            new HandsGroup(),
            new HandsGroup(),
            new HandsGroup(),
            new HandsGroup()
        };

        public override object Clone()
        {
            PostflopData postflopData = (PostflopData)MemberwiseClone();

            postflopData.MainGroup = (HandsGroup)MainGroup.Clone();
            postflopData.SubGroups = SubGroups.Select(sg => (HandsGroup)sg.Clone()).ToArray();

            return postflopData;
        }
    }

    public class HandsGroup : ICloneable
    {
        public const int HandCategoriesCount = 100;

        public float MadeHandsDefaultValue = float.NaN;

        public float DrawHandsDefaultValue = float.NaN;

        public float ComboHandsDefaultValue = float.NaN;

        public ushort[] MadeHands { get; private set; } = new ushort[HandCategoriesCount];

        public ushort[] DrawHands { get; private set; } = new ushort[HandCategoriesCount];

        public ushort[] ComboHands { get; private set; } = new ushort[HandCategoriesCount];

        public object Clone()
        {
            HandsGroup hg = (HandsGroup)MemberwiseClone();

            hg.MadeHands = MadeHands.ToArray();
            hg.DrawHands = DrawHands.ToArray();
            hg.ComboHands = ComboHands.ToArray();

            return hg;
        }
    }
}
