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
        private static readonly Dictionary<string, List<string>> HandPlayers = new Dictionary<string, List<string>>();

        public static void ProcessData(PokerWindow pokerWindow, ScreenParserData screenData, BitmapDecorator bmp)
        {
            if (pokerWindow.PokerRoom == PokerRoom.GGPoker)
            {
                if (Shared.SavePicturesPerHand)
                {
                    string hc1 = screenData.HoleCards[3][0], hc2 = screenData.HoleCards[3][1];

                    if (screenData.DealerPosition != null && !string.IsNullOrEmpty(hc1) && !string.IsNullOrEmpty(hc2) && screenData.Nicks[3] != null)
                    {
                        string tableId = $"{pokerWindow.PokerWindowInfo.TableId}";

                        string handId = $"{hc1}{hc2}";

                        string dealerId = $"{screenData.DealerPosition}";

                        string key = $"{tableId}_{handId}_{dealerId}";

                        bool needSave = false;

                        if (!HandPlayers.ContainsKey(key))
                        {
                            HandPlayers.Add(key, new List<string>(screenData.Nicks.Where(n => n != null)));

                            needSave = true;
                        }
                        else
                        {
                            foreach (string nick in screenData.Nicks)
                            {
                                if(nick == null || HandPlayers[key].Contains(nick))
                                    continue;

                                HandPlayers[key].Add(nick);

                                needSave = true;
                            }
                        }

                        if(needSave)
                            bmp.Crop(pokerWindow.Position).Save(Path.Combine(Shared.PicturesSaveFolder, $"{key}_{DateTime.Now.Ticks}.bmp"));
                    }
                }
            }
        }
    }
}
