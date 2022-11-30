using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoHelper.StatsEntities
{
    public abstract class CellDataBase : ICloneable
    {
        public abstract HandsGroupBase MainGroup { get; protected set; }

        public abstract HandsGroupBase[] SubGroups { get; protected set; }

        public object Clone()
        {
            CellDataBase cellData = (CellDataBase)MemberwiseClone();

            cellData.MainGroup = (HandsGroupBase)MainGroup.Clone();
            cellData.SubGroups = SubGroups.Select(sg => (HandsGroupBase)sg.Clone()).ToArray();

            return cellData;
        }
    }

    public class PreflopData : CellDataBase
    {
        //public void AddHand(string hc1, string hc2)
        //{
        //    (char firstCardFaceValue, char secondCardFaceValue) = (hc1[0], hc2[0]);

        //    if (Array.IndexOf(Common.FaceValues, firstCardFaceValue) < Array.IndexOf(Common.FaceValues, secondCardFaceValue))
        //        (firstCardFaceValue, secondCardFaceValue) = (hc2[0], hc1[0]);

        //    string hand = $"{firstCardFaceValue}{secondCardFaceValue}";

        //    if (firstCardFaceValue != secondCardFaceValue)
        //        hand += hc1[1] == hc2[1] ? "s" : "o";

        //    PocketHands[Array.IndexOf(Common.HoleCards, hand)]++;
        //}

        public override HandsGroupBase MainGroup { get; protected set; } = new PreflopHandsGroup();

        public override HandsGroupBase[] SubGroups { get; protected set; } = new PreflopHandsGroup[4]
        {
            new PreflopHandsGroup(),
            new PreflopHandsGroup(),
            new PreflopHandsGroup(),
            new PreflopHandsGroup()
        };
    }

    public class PostflopData : CellDataBase
    {
        public override HandsGroupBase MainGroup { get; protected set; } = new PostflopHandsGroup();

        public override HandsGroupBase[] SubGroups { get; protected set; } = new PostflopHandsGroup[4]
        {
            new PostflopHandsGroup(),
            new PostflopHandsGroup(),
            new PostflopHandsGroup(),
            new PostflopHandsGroup()
        };
    }
}
