using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BitmapHelper;

namespace InfoHelper.Utils
{
    public static class Shared
    {
        public static int ClientWorkSpaceWidth { get; set; } = 1920;
        public static int ClientWorkSpaceHeight { get; set; } = 1040;
        public static int ClientTaskBarHeight { get; set; } = 40;

        public static double CellHeight { get; set; }

        public static string SaveFolder { get; set; }
        public static string SnapshotsFolder { get; set; }
        public static int BitmapsBuffer { get; set; }
        public static int GetLastNHands { get; set; }
        public static int TimerInterval { get; set; }
        public static int ScreenshotSaveInterval { get; set; }
        public static TimeSpan WindowExpireTimeout { get; set; }
        public static BitmapDecorator MouseCursor { get; set; }
        public static string ServerName { get; set; }
        public static string DBName { get; set; }
        public static bool IsLocalServer { get; set; }
        public static string SolverFolder { get; set; }
        public static string SolverFile { get; set; }
        public static uint SolverPort { get; set; }
        public static string SolverIp { get; set; }
        public static int TurnSolverDuration { get; set; }
        public static int RiverSolverDuration { get; set; }
        public static double TurnSolverAccuracy { get; set; }
        public static double RiverSolverAccuracy { get; set; }
        public static int MinLineFrequency { get; set; }
        public static bool ScanTablesGg { get; set; }
        public static int CardBackIndexGg { get; set; }
        public static int DeckIndexGg { get; set; }
        public static bool SavePicturesPerHandGg { get; set; }
        public static string PicturesSaveFolderGg { get; set; }
    }
}
