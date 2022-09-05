using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BitmapHelper;
using InfoHelper.Utils;
using PokerWindowsUtility;
using ScreenParserUtility;

namespace InfoHelper.DataProcessor
{
    public class PokerRoomManager
    {
        private static readonly Dictionary<string, DateTime> SavedFilesGg = new Dictionary<string, DateTime>();

        public static void ProcessData(PokerWindow pokerWindow, ScreenParserData screenData, BitmapDecorator bmp)
        {
            if (pokerWindow.PokerRoom == PokerRoom.GGPoker)
            {
                if (Shared.SavePicturesPerHandGg)
                {
                    if (screenData.IsHeroActing)
                    {
                        string hc1 = screenData.HoleCards[3][0], hc2 = screenData.HoleCards[3][1];

                        bool CheckParsedInfo()
                        {
                            for (int i = 0; i < screenData.Nicks.Length; i++)
                            {
                                if (screenData.Nicks[i] == null && screenData.Stacks[i] != null)
                                    return false;
                            }

                            return true;
                        }

                        if (screenData.DealerPosition != null && !string.IsNullOrEmpty(hc1) && !string.IsNullOrEmpty(hc2) && CheckParsedInfo())
                        {
                            string tableId = $"{pokerWindow.PokerWindowInfo.TableId}";

                            string handId = $"{hc1}{hc2}";

                            string dealerId = $"{screenData.DealerPosition}";

                            string fileName = $"{tableId}_{handId}_{dealerId}";

                            if (!SavedFilesGg.ContainsKey(fileName) || DateTime.Now.Subtract(SavedFilesGg[fileName]).TotalSeconds > 30)
                            {
                                string saveDir = Path.Combine(Shared.PicturesSaveFolderGg, DateTime.Now.Ticks.ToString());

                                if (!Directory.Exists(saveDir))
                                    Directory.CreateDirectory(saveDir);

                                bmp.Crop(pokerWindow.Position).Save(Path.Combine(saveDir, $"{fileName}.bmp"));

                                SavedFilesGg[fileName] = DateTime.Now;
                            }
                        }
                    }
                }
            }
        }
    }
}
