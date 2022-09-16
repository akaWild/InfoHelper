using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InfoHelper.Utils;

namespace InfoHelper.StatsEntities
{
    public class PreflopData
    {
        private readonly int[] _data = new int[169];

        public void AddHand(string hc1, string hc2)
        {
            (char firstCardFaceValue, char secondCardFaceValue) = (hc1[0], hc2[0]);

            if (Array.IndexOf(Common.FaceValues, firstCardFaceValue) < Array.IndexOf(Common.FaceValues, secondCardFaceValue))
                (firstCardFaceValue, secondCardFaceValue) = (hc2[0], hc1[0]);

            string hand = $"{firstCardFaceValue}{secondCardFaceValue}";

            if (firstCardFaceValue != secondCardFaceValue)
                hand += hc1[1] == hc2[1] ? "s" : "o";

            _data[Array.IndexOf(Common.HoleCards, hand)]++;
        }

        public int this[int index] => _data[index];
    }
}
