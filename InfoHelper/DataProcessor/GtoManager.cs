using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using GameInformationUtility;
using GtoUtility;

namespace InfoHelper.DataProcessor
{
    public class GtoManager
    {
        private readonly Dictionary<KeyGto, GtoStrategyContainer> _dictGtoPreflop;

        public GtoManager()
        {
            BinaryFormatter formatter = new BinaryFormatter();

            using FileStream gtosbvsbb = File.Open("gtoPreflop.bin", FileMode.Open);

            _dictGtoPreflop = (Dictionary<KeyGto, GtoStrategyContainer>)formatter.Deserialize(gtosbvsbb);
        }

        public void GetPreflopGtoStrategy(GameContext gc)
        {
            string hc1 = gc.HoleCards[gc.HeroPosition - 1][0];
            string hc2 = gc.HoleCards[gc.HeroPosition - 1][1];

            int playersCount = gc.Stacks.Count(s => s.HasValue);

            GameType gameType = playersCount == 2 ? GameType.Hu : GameType.SixMax;

            List<double> initStacksRemaining = new List<double>();

            for (int i = 0; i < gc.InitialStacks.Length; i++)
            {
                if(gc.IsPlayerIn[i] == null || !(bool)gc.IsPlayerIn[i])
                    continue;

                initStacksRemaining.Add((double)gc.InitialStacks[i]);
            }
        }
    }
}
