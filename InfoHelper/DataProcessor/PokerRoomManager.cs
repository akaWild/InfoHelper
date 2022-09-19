using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BitmapHelper;
using InfoHelper.Utils;
using Microsoft.Win32;
using PokerWindowsUtility;
using ScreenParserUtility;

namespace InfoHelper.DataProcessor
{
    public class PokerRoomManager
    {
        private static readonly Dictionary<string, DateTime> SavedFiles = new Dictionary<string, DateTime>();

        public static void ProcessData(PokerWindow pokerWindow, ScreenParserData screenData, BitmapDecorator bmp)
        {
            if (pokerWindow.PokerRoom == PokerRoom.GGPoker)
            {
                if (Shared.SavePicturesPerHand)
                {
                    string hc1 = screenData.HoleCards[3][0], hc2 = screenData.HoleCards[3][1];

                    if (screenData.DealerPosition != null && !string.IsNullOrEmpty(hc1) && !string.IsNullOrEmpty(hc2))
                    {
                        string tableId = $"{pokerWindow.PokerWindowInfo.TableId}";

                        string handId = $"{hc1}{hc2}";

                        string dealerId = $"{screenData.DealerPosition}";

                        string fileName = $"{tableId}_{handId}_{dealerId}";

                        if (!SavedFiles.ContainsKey(fileName) || DateTime.Now.Subtract(SavedFiles[fileName]).TotalSeconds > 20)
                        {
                            bmp.Crop(pokerWindow.Position).Save(Path.Combine(Shared.PicturesSaveFolder, $"{fileName}_{DateTime.Now.Ticks}.bmp"));

                            SavedFiles[fileName] = DateTime.Now;
                        }
                    }
                }
            }
        }
    }
}
