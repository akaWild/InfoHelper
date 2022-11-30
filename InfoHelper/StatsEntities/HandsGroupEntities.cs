using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoHelper.StatsEntities
{
    public abstract class HandsGroupBase : ICloneable
    {
        public int[] CounterActions { get; set; } = new int[3];

        public abstract object Clone();
    }

    public class PreflopHandsGroup : HandsGroupBase
    {
        public int[] PocketHands { get; private set; } = new int[169];

        public override object Clone()
        {
            PreflopHandsGroup preflopData = (PreflopHandsGroup)MemberwiseClone();

            preflopData.CounterActions = CounterActions.ToArray();

            preflopData.PocketHands = PocketHands.ToArray();

            return preflopData;
        }
    }

    public class PostflopHandsGroup : HandsGroupBase
    {
        public const int HandCategoriesCount = 100;

        public float MadeHandsDefaultValue = float.NaN;

        public float DrawHandsDefaultValue = float.NaN;

        public float ComboHandsDefaultValue = float.NaN;

        public ushort[] MadeHands { get; private set; } = new ushort[HandCategoriesCount];

        public ushort[] DrawHands { get; private set; } = new ushort[HandCategoriesCount];

        public ushort[] ComboHands { get; private set; } = new ushort[HandCategoriesCount];

        public override object Clone()
        {
            PostflopHandsGroup hg = (PostflopHandsGroup)MemberwiseClone();

            hg.CounterActions = CounterActions.ToArray();

            hg.MadeHands = MadeHands.ToArray();
            hg.DrawHands = DrawHands.ToArray();
            hg.ComboHands = ComboHands.ToArray();

            return hg;
        }
    }
}
