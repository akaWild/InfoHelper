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
        public static int MinDeviationSample { get; set; }
        public static int TimerInterval { get; set; }
        public static int ScreenshotSaveInterval { get; set; }
        public static TimeSpan WindowExpireTimeout { get; set; }
        public static BitmapDecorator MouseCursor { get; set; }
        public static int CursorSearchThreads { get; set; }
        public static string ServerName { get; set; }
        public static string DbName { get; set; }
        public static string GtoDbName { get; set; }
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
        public static int AllInThreshold { get; set; }
        public static bool ScanTables { get; set; }
        public static int CardBackIndex { get; set; }
        public static int DeckIndex { get; set; }
        public static string PlayersImagesFolder { get; set; }
        public static bool SavePicturesPerHand { get; set; }
        public static string PicturesSaveFolder { get; set; }

        public static class SolverSizingsInfo
        {
            public static class LimpPot
            {
                public static class TurnTree
                {
                    public static class HeroOop
                    {
                        //Turn
                        public static float[] OopDonkBetsTurn { get; set; }
                        public static float[] OopBetsTurn { get; set; }
                        public static float[] IpBetsTurn { get; set; }
                        public static float[] OopRaisesTurn { get; set; }
                        public static float[] IpRaisesTurn { get; set; }
                        public static float[] OopReraisesTurn { get; set; }
                        public static float[] IpReraisesTurn { get; set; }

                        //River
                        public static float[] OopDonkBetsRiver { get; set; }
                        public static float[] OopBetsRiver { get; set; }
                        public static float[] IpBetsRiver { get; set; }
                        public static float[] OopRaisesRiver { get; set; }
                        public static float[] IpRaisesRiver { get; set; }
                        public static float[] OopReraisesRiver { get; set; }
                        public static float[] IpReraisesRiver { get; set; }
                    }

                    public static class HeroIp
                    {
                        //Turn
                        public static float[] OopDonkBetsTurn { get; set; }
                        public static float[] OopBetsTurn { get; set; }
                        public static float[] IpBetsTurn { get; set; }
                        public static float[] OopRaisesTurn { get; set; }
                        public static float[] IpRaisesTurn { get; set; }
                        public static float[] OopReraisesTurn { get; set; }
                        public static float[] IpReraisesTurn { get; set; }

                        //River
                        public static float[] OopDonkBetsRiver { get; set; }
                        public static float[] OopBetsRiver { get; set; }
                        public static float[] IpBetsRiver { get; set; }
                        public static float[] OopRaisesRiver { get; set; }
                        public static float[] IpRaisesRiver { get; set; }
                        public static float[] OopReraisesRiver { get; set; }
                        public static float[] IpReraisesRiver { get; set; }
                    }
                }

                public static class RiverTree
                {
                    public static class HeroOop
                    {
                        //River
                        public static float[] OopDonkBetsRiver { get; set; }
                        public static float[] OopBetsRiver { get; set; }
                        public static float[] IpBetsRiver { get; set; }
                        public static float[] OopRaisesRiver { get; set; }
                        public static float[] IpRaisesRiver { get; set; }
                        public static float[] OopReraisesRiver { get; set; }
                        public static float[] IpReraisesRiver { get; set; }
                    }

                    public static class HeroIp
                    {
                        //River
                        public static float[] OopDonkBetsRiver { get; set; }
                        public static float[] OopBetsRiver { get; set; }
                        public static float[] IpBetsRiver { get; set; }
                        public static float[] OopRaisesRiver { get; set; }
                        public static float[] IpRaisesRiver { get; set; }
                        public static float[] OopReraisesRiver { get; set; }
                        public static float[] IpReraisesRiver { get; set; }
                    }
                }
            }

            public static class RaisePot
            {
                public static class TurnTree
                {
                    public static class HeroOop
                    {
                        //Turn
                        public static float[] OopDonkBetsTurn { get; set; }
                        public static float[] OopBetsTurn { get; set; }
                        public static float[] IpBetsTurn { get; set; }
                        public static float[] OopRaisesTurn { get; set; }
                        public static float[] IpRaisesTurn { get; set; }
                        public static float[] OopReraisesTurn { get; set; }
                        public static float[] IpReraisesTurn { get; set; }

                        //River
                        public static float[] OopDonkBetsRiver { get; set; }
                        public static float[] OopBetsRiver { get; set; }
                        public static float[] IpBetsRiver { get; set; }
                        public static float[] OopRaisesRiver { get; set; }
                        public static float[] IpRaisesRiver { get; set; }
                        public static float[] OopReraisesRiver { get; set; }
                        public static float[] IpReraisesRiver { get; set; }
                    }

                    public static class HeroIp
                    {
                        //Turn
                        public static float[] OopDonkBetsTurn { get; set; }
                        public static float[] OopBetsTurn { get; set; }
                        public static float[] IpBetsTurn { get; set; }
                        public static float[] OopRaisesTurn { get; set; }
                        public static float[] IpRaisesTurn { get; set; }
                        public static float[] OopReraisesTurn { get; set; }
                        public static float[] IpReraisesTurn { get; set; }

                        //River
                        public static float[] OopDonkBetsRiver { get; set; }
                        public static float[] OopBetsRiver { get; set; }
                        public static float[] IpBetsRiver { get; set; }
                        public static float[] OopRaisesRiver { get; set; }
                        public static float[] IpRaisesRiver { get; set; }
                        public static float[] OopReraisesRiver { get; set; }
                        public static float[] IpReraisesRiver { get; set; }
                    }
                }

                public static class RiverTree
                {
                    public static class HeroOop
                    {
                        //River
                        public static float[] OopDonkBetsRiver { get; set; }
                        public static float[] OopBetsRiver { get; set; }
                        public static float[] IpBetsRiver { get; set; }
                        public static float[] OopRaisesRiver { get; set; }
                        public static float[] IpRaisesRiver { get; set; }
                        public static float[] OopReraisesRiver { get; set; }
                        public static float[] IpReraisesRiver { get; set; }
                    }

                    public static class HeroIp
                    {
                        //River
                        public static float[] OopDonkBetsRiver { get; set; }
                        public static float[] OopBetsRiver { get; set; }
                        public static float[] IpBetsRiver { get; set; }
                        public static float[] OopRaisesRiver { get; set; }
                        public static float[] IpRaisesRiver { get; set; }
                        public static float[] OopReraisesRiver { get; set; }
                        public static float[] IpReraisesRiver { get; set; }
                    }
                }
            }

            public static class ThreeBetPot
            {
                public static class TurnTree
                {
                    public static class HeroOop
                    {
                        //Turn
                        public static float[] OopDonkBetsTurn { get; set; }
                        public static float[] OopBetsTurn { get; set; }
                        public static float[] IpBetsTurn { get; set; }
                        public static float[] OopRaisesTurn { get; set; }
                        public static float[] IpRaisesTurn { get; set; }
                        public static float[] OopReraisesTurn { get; set; }
                        public static float[] IpReraisesTurn { get; set; }

                        //River
                        public static float[] OopDonkBetsRiver { get; set; }
                        public static float[] OopBetsRiver { get; set; }
                        public static float[] IpBetsRiver { get; set; }
                        public static float[] OopRaisesRiver { get; set; }
                        public static float[] IpRaisesRiver { get; set; }
                        public static float[] OopReraisesRiver { get; set; }
                        public static float[] IpReraisesRiver { get; set; }
                    }

                    public static class HeroIp
                    {
                        //Turn
                        public static float[] OopDonkBetsTurn { get; set; }
                        public static float[] OopBetsTurn { get; set; }
                        public static float[] IpBetsTurn { get; set; }
                        public static float[] OopRaisesTurn { get; set; }
                        public static float[] IpRaisesTurn { get; set; }
                        public static float[] OopReraisesTurn { get; set; }
                        public static float[] IpReraisesTurn { get; set; }

                        //River
                        public static float[] OopDonkBetsRiver { get; set; }
                        public static float[] OopBetsRiver { get; set; }
                        public static float[] IpBetsRiver { get; set; }
                        public static float[] OopRaisesRiver { get; set; }
                        public static float[] IpRaisesRiver { get; set; }
                        public static float[] OopReraisesRiver { get; set; }
                        public static float[] IpReraisesRiver { get; set; }
                    }
                }

                public static class RiverTree
                {
                    public static class HeroOop
                    {
                        //River
                        public static float[] OopDonkBetsRiver { get; set; }
                        public static float[] OopBetsRiver { get; set; }
                        public static float[] IpBetsRiver { get; set; }
                        public static float[] OopRaisesRiver { get; set; }
                        public static float[] IpRaisesRiver { get; set; }
                        public static float[] OopReraisesRiver { get; set; }
                        public static float[] IpReraisesRiver { get; set; }
                    }

                    public static class HeroIp
                    {
                        //River
                        public static float[] OopDonkBetsRiver { get; set; }
                        public static float[] OopBetsRiver { get; set; }
                        public static float[] IpBetsRiver { get; set; }
                        public static float[] OopRaisesRiver { get; set; }
                        public static float[] IpRaisesRiver { get; set; }
                        public static float[] OopReraisesRiver { get; set; }
                        public static float[] IpReraisesRiver { get; set; }
                    }
                }
            }

            public static class FourBetPlusPot
            {
                public static class TurnTree
                {
                    public static class HeroOop
                    {
                        //Turn
                        public static float[] OopDonkBetsTurn { get; set; }
                        public static float[] OopBetsTurn { get; set; }
                        public static float[] IpBetsTurn { get; set; }
                        public static float[] OopRaisesTurn { get; set; }
                        public static float[] IpRaisesTurn { get; set; }
                        public static float[] OopReraisesTurn { get; set; }
                        public static float[] IpReraisesTurn { get; set; }

                        //River
                        public static float[] OopDonkBetsRiver { get; set; }
                        public static float[] OopBetsRiver { get; set; }
                        public static float[] IpBetsRiver { get; set; }
                        public static float[] OopRaisesRiver { get; set; }
                        public static float[] IpRaisesRiver { get; set; }
                        public static float[] OopReraisesRiver { get; set; }
                        public static float[] IpReraisesRiver { get; set; }
                    }

                    public static class HeroIp
                    {
                        //Turn
                        public static float[] OopDonkBetsTurn { get; set; }
                        public static float[] OopBetsTurn { get; set; }
                        public static float[] IpBetsTurn { get; set; }
                        public static float[] OopRaisesTurn { get; set; }
                        public static float[] IpRaisesTurn { get; set; }
                        public static float[] OopReraisesTurn { get; set; }
                        public static float[] IpReraisesTurn { get; set; }

                        //River
                        public static float[] OopDonkBetsRiver { get; set; }
                        public static float[] OopBetsRiver { get; set; }
                        public static float[] IpBetsRiver { get; set; }
                        public static float[] OopRaisesRiver { get; set; }
                        public static float[] IpRaisesRiver { get; set; }
                        public static float[] OopReraisesRiver { get; set; }
                        public static float[] IpReraisesRiver { get; set; }
                    }
                }

                public static class RiverTree
                {
                    public static class HeroOop
                    {
                        //River
                        public static float[] OopDonkBetsRiver { get; set; }
                        public static float[] OopBetsRiver { get; set; }
                        public static float[] IpBetsRiver { get; set; }
                        public static float[] OopRaisesRiver { get; set; }
                        public static float[] IpRaisesRiver { get; set; }
                        public static float[] OopReraisesRiver { get; set; }
                        public static float[] IpReraisesRiver { get; set; }
                    }

                    public static class HeroIp
                    {
                        //River
                        public static float[] OopDonkBetsRiver { get; set; }
                        public static float[] OopBetsRiver { get; set; }
                        public static float[] IpBetsRiver { get; set; }
                        public static float[] OopRaisesRiver { get; set; }
                        public static float[] IpRaisesRiver { get; set; }
                        public static float[] OopReraisesRiver { get; set; }
                        public static float[] IpReraisesRiver { get; set; }
                    }
                }
            }
        }
    }
}
