using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using BitmapHelper;
using InfoHelper.Utils;
using Color = System.Drawing.Color;

namespace InfoHelper.DataProcessor
{
    public class SettingsManager
    {
        public string Error { get; private set; }

        public bool RetrieveSettings()
        {
            Error = null;

            try
            {
                RetrieveSettingsPrivate();
            }
            catch (Exception e)
            {
                Error = e.Message;

                return false;
            }

            return true;
        }

        private void RetrieveSettingsPrivate()
        {
            string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\Settings\\Settings.xml");

            string saveFolder = string.Empty;

            string snapshotsFolder = string.Empty;

            string bitmapsBufferStr = string.Empty;

            string getLastNHandsStr = string.Empty;

            string minDeviationSampleStr = string.Empty;

            string timerIntervalStr = string.Empty;

            string screenshotSaveIntervalStr = string.Empty;

            string windowExpireTimeoutStr = string.Empty;

            string mouseCursorStr = string.Empty;

            string cursorSearchThreadsStr = string.Empty;

            string windowsBackColorStr = string.Empty;

            string serverName = string.Empty;

            string dbName = string.Empty;

            string gtoDbName = string.Empty;

            string isLocalSolverStr = string.Empty;

            string solverFolder = string.Empty;

            string solverFile = string.Empty;

            string solverPortStr = string.Empty;

            string solverIp = string.Empty;

            string turnSolverDurationStr = string.Empty;

            string riverSolverDurationStr = string.Empty;

            string turnSolverAccuracyStr = string.Empty;

            string riverSolverAccuracyStr = string.Empty;

            string minLineFrequencyStr = string.Empty;

            string allInThresholdStr = string.Empty;

            #region GG Poker

            string scanTablesStr = string.Empty;

            string cardBackStr = string.Empty;

            string deckStr = string.Empty;

            string playersImagesFolder = string.Empty;

            string savePicturesStr = string.Empty;

            string savePicturesFolder = string.Empty;

            #endregion

            XmlDocument xmlDoc = new XmlDocument();

            if (!File.Exists(settingsPath))
                throw new Exception("Settings file doesn't exist in current directory");

            xmlDoc.Load(settingsPath);

            XmlElement root = xmlDoc.DocumentElement;

            if (root != null)
            {
                foreach (XmlNode node in root)
                {
                    if (node.Attributes?["Value"] != null)
                    {
                        switch (node.Name)
                        {
                            #region General

                            case "SaveFolder":

                                saveFolder = node.Attributes["Value"].Value;

                                break;

                            case "SnapshotsFolder":

                                snapshotsFolder = node.Attributes["Value"].Value;

                                break;

                            case "BitmapsBuffer":

                                bitmapsBufferStr = node.Attributes["Value"].Value;

                                break;

                            case "GetLastNHands":

                                getLastNHandsStr = node.Attributes["Value"].Value;

                                break;

                            case "MinDeviationSample":

                                minDeviationSampleStr = node.Attributes["Value"].Value;

                                break;

                            case "TimerInterval":

                                timerIntervalStr = node.Attributes["Value"].Value;

                                break;

                            case "ScreenshotSaveInterval":

                                screenshotSaveIntervalStr = node.Attributes["Value"].Value;

                                break;

                            case "WindowExpireTimeout":

                                windowExpireTimeoutStr = node.Attributes["Value"].Value;

                                break;

                            case "MouseCursor":

                                mouseCursorStr = node.Attributes["Value"].Value;

                                break;

                            case "CursorSearchThreads":

                                cursorSearchThreadsStr = node.Attributes["Value"].Value;

                                break;

                            case "FormBackColor":

                                windowsBackColorStr = node.Attributes["Value"].Value;

                                break;

                            #endregion

                            #region Server

                            case "ServerName":

                                serverName = node.Attributes["Value"].Value;

                                break;

                            case "DBName":

                                dbName = node.Attributes["Value"].Value;

                                break;

                            case "GTODBName":

                                gtoDbName = node.Attributes["Value"].Value;

                                break;

                            #endregion

                            #region Solver

                            case "IsLocalServer":

                                isLocalSolverStr = node.Attributes["Value"].Value;

                                break;

                            case "SolverFolder":

                                solverFolder = node.Attributes["Value"].Value;

                                break;

                            case "SolverFile":

                                solverFile = node.Attributes["Value"].Value;

                                break;

                            case "SolverPort":

                                solverPortStr = node.Attributes["Value"].Value;

                                break;

                            case "SolverIp":

                                solverIp = node.Attributes["Value"].Value;

                                break;

                            case "TurnSolverDuration":

                                turnSolverDurationStr = node.Attributes["Value"].Value;

                                break;

                            case "RiverSolverDuration":

                                riverSolverDurationStr = node.Attributes["Value"].Value;

                                break;

                            case "TurnSolverAccuracy":

                                turnSolverAccuracyStr = node.Attributes["Value"].Value;

                                break;

                            case "RiverSolverAccuracy":

                                riverSolverAccuracyStr = node.Attributes["Value"].Value;

                                break;

                            case "MinLineFrequency":

                                minLineFrequencyStr = node.Attributes["Value"].Value;

                                break;

                            case "AllInThreshold":

                                allInThresholdStr = node.Attributes["Value"].Value;

                                break;

                            #endregion

                            #region Rooms

                            #region GGPoker

                            case "ScanTables":

                                scanTablesStr = node.Attributes["Value"].Value;

                                break;

                            case "CardBackIndex":

                                cardBackStr = node.Attributes["Value"].Value;

                                break;

                            case "DeckIndex":

                                deckStr = node.Attributes["Value"].Value;

                                break;

                            case "PlayersImagesFolder":

                                playersImagesFolder = node.Attributes["Value"].Value;

                                break;

                            case "SavePicturesPerHand":

                                savePicturesStr = node.Attributes["Value"].Value;

                                break;

                            case "PicturesSaveFolder":

                                savePicturesFolder = node.Attributes["Value"].Value;

                                break;

                                #endregion

                            #endregion
                        }
                    }
                }
            }

            #region Settings check up 

            #region General

            //Save folder
            if (string.IsNullOrEmpty(saveFolder))
                throw new Exception("Pictures save directory is not specified");

            if (!Directory.Exists(saveFolder))
                throw new Exception("Pictures save directory doesn't exist");

            //Snapshots folder
            if (string.IsNullOrEmpty(snapshotsFolder))
                throw new Exception("Snapshots save directory is not specified");

            if (!Directory.Exists(snapshotsFolder))
                throw new Exception("Snapshots save directory doesn't exist");

            //Bitmaps buffer
            if (string.IsNullOrEmpty(bitmapsBufferStr))
                throw new Exception("Bitmaps buffer is not specified");

            if (!int.TryParse(bitmapsBufferStr, out int bitmapsBuffer))
                throw new Exception("Bitmaps buffer has incorrect format");

            if (bitmapsBuffer < 1)
                throw new Exception("Bitmaps buffer should be more than 0");

            //Get last n hands
            if (string.IsNullOrEmpty(getLastNHandsStr))
                throw new Exception("Get last n hands value is not specified");

            if (!int.TryParse(getLastNHandsStr, out int getLastNHands))
                throw new Exception("Get last n hands value has incorrect format");

            if (getLastNHands is < 1 or > 1000000)
                throw new Exception("Get last n hands value should be between 1 and 1000000");

            //Min deviation sample
            if (string.IsNullOrEmpty(minDeviationSampleStr))
                throw new Exception("Minimum deviation sample value is not specified");

            if (!int.TryParse(minDeviationSampleStr, out int minDeviationSample))
                throw new Exception("Minimum deviation sample value has incorrect format");

            if (minDeviationSample is < 1 or > 100)
                throw new Exception("Minimum deviation sample value should be between 1 and 100");

            //Timer
            if (string.IsNullOrEmpty(timerIntervalStr))
                throw new Exception("Timer interval is not specified");

            if (!int.TryParse(timerIntervalStr, out int timerInterval))
                throw new Exception("Timer interval has incorrect format");

            if (timerInterval is < 1 or > 1000)
                throw new Exception("Timer interval should be between 1 and 1000");

            //Screenshot save interval
            if (string.IsNullOrEmpty(screenshotSaveIntervalStr))
                throw new Exception("Screenshots save interval is not specified");

            if (!int.TryParse(screenshotSaveIntervalStr, out int screenshotSaveInterval))
                throw new Exception("Screenshots save interval has incorrect format");

            if (screenshotSaveInterval < 1)
                throw new Exception("Screenshots save interval should be more than 0");

            //Window expire timeout
            if (string.IsNullOrEmpty(windowExpireTimeoutStr))
                throw new Exception("Window expire timeout is not specified");

            if (!int.TryParse(windowExpireTimeoutStr, out int windowExpireTimeout))
                throw new Exception("Window expire timeout has incorrect format");

            if (windowExpireTimeout is <= 0 or > 59)
                throw new Exception("Window expire timeout should be between 1 and 59");

            TimeSpan windowInfoExpireTimeout = new TimeSpan(0, 0, 0, windowExpireTimeout);

            //Mouse cursor
            if (string.IsNullOrEmpty(mouseCursorStr))
                throw new Exception("Mouse cursor is not specified");

            string cursorFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\Cursors", mouseCursorStr);

            if (!File.Exists(cursorFilePath))
                throw new Exception("Mouse cursor file doesn't exist");

            BitmapDecorator cursorImage = null;

            try
            {
                cursorImage = new BitmapDecorator((Bitmap)Image.FromFile(cursorFilePath, true));
            }
            catch
            {
                throw new Exception("Unable create cursor image from file");
            }

            //Cursor search threads
            if (string.IsNullOrEmpty(cursorSearchThreadsStr))
                throw new Exception("Cursor search threads value is not specified");

            if (!int.TryParse(cursorSearchThreadsStr, out int cursorSearchThreads))
                throw new Exception("Cursor search threads value has incorrect format");

            if (cursorSearchThreads != -1 && (cursorSearchThreads < 1 || cursorSearchThreads > Environment.ProcessorCount))
                throw new Exception($"Cursor search threads value should be between 1 and {Environment.ProcessorCount} or -1 as default");

            //Window back color brush
            SolidColorBrush windowBackground = null;

            if (string.IsNullOrEmpty(windowsBackColorStr))
                throw new Exception("Window back color is not specified");

            try
            {
                System.Windows.Media.Color convertedColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(windowsBackColorStr);

                windowBackground = new SolidColorBrush(convertedColor);
            }
            catch
            {
                throw new Exception("Window back color has incorrect format");
            }

            #endregion

            #region Server

            //Server name
            if (string.IsNullOrEmpty(serverName))
                throw new Exception("Server name is empty");

            //Db name
            if (string.IsNullOrEmpty(dbName))
                throw new Exception("Database name is empty");

            //Gto db name
            if (string.IsNullOrEmpty(gtoDbName))
                throw new Exception("Gto database name is empty");

            #endregion

            #region Solver 

            //Solver mode
            if (string.IsNullOrEmpty(isLocalSolverStr))
                throw new Exception("Solver mode selector is empty");

            if (!bool.TryParse(isLocalSolverStr, out bool isLocalSolver))
                throw new Exception("Solver mode selector has incorrect format");

            uint solverPort = 0;

            if (isLocalSolver)
            {
                //Solver folder
                if (string.IsNullOrEmpty(solverFolder))
                    throw new Exception("Solver directory is not specified");

                if (!Directory.Exists(solverFolder))
                    throw new Exception("Solver directory doesn't exist");

                //Solver file
                if (string.IsNullOrEmpty(solverFile))
                    throw new Exception("Solver file is not specified");

                if (!File.Exists(Path.Combine(solverFolder, solverFile)))
                    throw new Exception("Solver file doesn't exist");
            }
            else
            {
                //Solver port
                if (string.IsNullOrEmpty(solverPortStr))
                    throw new Exception("Remote solver port is not specified");

                if (!uint.TryParse(solverPortStr, out solverPort))
                    throw new Exception("Remote solver port has incorrect format (should be between 0 and 65535)");

                //Solver ip
                if (string.IsNullOrEmpty(solverIp))
                    throw new Exception("Remote solver ip is not specified");

                if (!Regex.IsMatch(solverIp, @"(localhost)|(\d{1,3}[.]\d{1,3}[.]\d{1,3}[.]\d{1,3})"))
                    throw new Exception("Remote solver ip has incorrect format");
            }

            //Turn solver duration
            if (string.IsNullOrEmpty(turnSolverDurationStr))
                throw new Exception("Turn solver duration is not specified");

            if (!int.TryParse(turnSolverDurationStr, out int turnSolverMaxDuration))
                throw new Exception("Turn solver duration has incorrect format");

            if (turnSolverMaxDuration <= 0 || turnSolverMaxDuration > 100)
                throw new Exception("Turn solver duration should be between 1 and 100");

            //River solver duration
            if (string.IsNullOrEmpty(riverSolverDurationStr))
                throw new Exception("River solver duration is not specified");

            if (!int.TryParse(riverSolverDurationStr, out int riverSolverMaxDuration))
                throw new Exception("River solver duration has incorrect format");

            if (riverSolverMaxDuration <= 0 || riverSolverMaxDuration > 100)
                throw new Exception("River solver duration should be between 1 and 100");

            //Turn solver accuracy
            if (string.IsNullOrEmpty(turnSolverAccuracyStr))
                throw new Exception("Turn solver accuracy is not specified");

            if (!double.TryParse(turnSolverAccuracyStr, NumberStyles.Any, new CultureInfo("en-US"), out double turnSolverAccuracy))
                throw new Exception("Turn solver accuracy has incorrect format");

            if (turnSolverAccuracy <= 0 || turnSolverAccuracy > 100)
                throw new Exception("Turn solver accuracy should be between 1 and 100");

            //River solver accuracy
            if (string.IsNullOrEmpty(riverSolverAccuracyStr))
                throw new Exception("River solver accuracy is not specified");

            if (!double.TryParse(riverSolverAccuracyStr, NumberStyles.Any, new CultureInfo("en-US"), out double riverSolverAccuracy))
                throw new Exception("River solver accuracy has incorrect format");

            if (riverSolverAccuracy <= 0 || riverSolverAccuracy > 100)
                throw new Exception("River solver accuracy should be between 1 and 100");

            //Min line frequency
            if (string.IsNullOrEmpty(minLineFrequencyStr))
                throw new Exception("Min line frequency is not specified");

            if (!int.TryParse(minLineFrequencyStr, out int minLineFrequency))
                throw new Exception("Min line frequency has incorrect format");

            if (minLineFrequency < 0 || minLineFrequency > 100)
                throw new Exception("Min line frequency should be between 0 and 100");

            //All in threshold
            if (string.IsNullOrEmpty(allInThresholdStr))
                throw new Exception("All in threshold is not specified");

            if (!int.TryParse(allInThresholdStr, out int allInThreshold))
                throw new Exception("All in threshold has incorrect format");

            if (allInThreshold < 0 || allInThreshold > 100)
                throw new Exception("All in threshold be between 0 and 100");

            #endregion

            #region Rooms

            #region GGPoker

            //Scan GGPoker tables
            if (string.IsNullOrEmpty(scanTablesStr))
                throw new Exception("Scan GGPoker tables option is not specified");

            if (!bool.TryParse(scanTablesStr, out bool scanTables))
                throw new Exception("Scan GGPoker tables option has incorrect format");

            int cardBack = 0;
            int deck = 0;
            bool savePictures = false;

            if (scanTables)
            {
                //Card back GGPoker
                if (string.IsNullOrEmpty(cardBackStr))
                    throw new Exception("GGPoker card back is not specified");

                if (!int.TryParse(cardBackStr, out cardBack))
                    throw new Exception("GGPoker card back has incorrect format");

                if (cardBack < 1)
                    throw new Exception("GGPoker card back value should be greater than 0");

                //Deck GGPoker
                if (string.IsNullOrEmpty(deckStr))
                    throw new Exception("GGPoker deck is not specified");

                if (!int.TryParse(deckStr, out deck))
                    throw new Exception("GGPoker deck has incorrect format");

                if (deck < 1)
                    throw new Exception("GGPoker deck value should be greater than 0");

                //Players images folder on GGPoker
                if (string.IsNullOrEmpty(playersImagesFolder))
                    throw new Exception("Players images folder on GGPoker is not specified");

                if (!Directory.Exists(playersImagesFolder))
                    throw new Exception("Players images folder on GGPoker doesn't exist");

                //Save pictures per hand on GGPoker
                if (string.IsNullOrEmpty(savePicturesStr))
                    throw new Exception("Save pictures per hand option on GGPoker is not specified");

                if (!bool.TryParse(savePicturesStr, out savePictures))
                    throw new Exception("Save pictures per hand option on GGPoker has incorrect format");

                if (savePictures)
                {
                    //Save pictures folder on GGPoker
                    if (string.IsNullOrEmpty(savePicturesFolder))
                        throw new Exception("Save pictures folder on GGPoker is not specified");

                    if (!Directory.Exists(savePicturesFolder))
                        throw new Exception("Save pictures folder on GGPoker doesn't exist");
                }
            }

            #endregion

            #endregion

            #endregion

            #region Adding options to resources

            Shared.SaveFolder = saveFolder;
            Shared.SnapshotsFolder = snapshotsFolder;
            Shared.BitmapsBuffer = bitmapsBuffer;
            Shared.GetLastNHands = getLastNHands;
            Shared.MinDeviationSample = minDeviationSample;
            Shared.TimerInterval = timerInterval;
            Shared.ScreenshotSaveInterval = screenshotSaveInterval;
            Shared.WindowExpireTimeout = windowInfoExpireTimeout;
            Shared.MouseCursor = cursorImage;
            Shared.CursorSearchThreads = cursorSearchThreads;
            Application.Current.Resources["CurrentFormBackColor"] = windowBackground;
            Shared.ServerName = serverName;
            Shared.DbName = dbName;
            Shared.GtoDbName = gtoDbName;
            Shared.IsLocalServer = isLocalSolver;
            Shared.SolverFolder = solverFolder;
            Shared.SolverFile = solverFile;
            Shared.SolverPort = solverPort;
            Shared.SolverIp = solverIp;
            Shared.TurnSolverDuration = turnSolverMaxDuration;
            Shared.RiverSolverDuration = riverSolverMaxDuration;
            Shared.TurnSolverAccuracy = turnSolverAccuracy;
            Shared.RiverSolverAccuracy = riverSolverAccuracy;
            Shared.MinLineFrequency = minLineFrequency;
            Shared.AllInThreshold = allInThreshold;
            Shared.ScanTables = scanTables;
            Shared.CardBackIndex = cardBack;
            Shared.DeckIndex = deck;
            Shared.PlayersImagesFolder = playersImagesFolder;
            Shared.SavePicturesPerHand = savePictures;
            Shared.PicturesSaveFolder = savePicturesFolder;

            #endregion

            //Solver sizings
            string solverSizingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\Settings\\SolverSizingSettings.xml");

            xmlDoc = new XmlDocument();

            if (!File.Exists(solverSizingsPath))
                throw new Exception("Solver sizing settings file doesn't exist");

            xmlDoc.Load(solverSizingsPath);

            #region Limp pot

            //Turn tree
            //Hero oop
            Shared.SolverSizingsInfo.LimpPot.TurnTree.HeroOop.OopDonkBetsTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/TurnTree/HeroOop/Turn/Bets/OopDonkBets/OopDonkBet"));
            Shared.SolverSizingsInfo.LimpPot.TurnTree.HeroOop.OopBetsTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/TurnTree/HeroOop/Turn/Bets/OopBets/OopBet"));
            Shared.SolverSizingsInfo.LimpPot.TurnTree.HeroOop.IpBetsTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/TurnTree/HeroOop/Turn/Bets/IpBets/IpBet"));
            Shared.SolverSizingsInfo.LimpPot.TurnTree.HeroOop.OopRaisesTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes($"/SolverSizingSettings/LimpPot/TurnTree/HeroOop/Turn/Raises/OopRaises/OopRaise"));
            Shared.SolverSizingsInfo.LimpPot.TurnTree.HeroOop.IpRaisesTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/TurnTree/HeroOop/Turn/Raises/IpRaises/IpRaise"));
            Shared.SolverSizingsInfo.LimpPot.TurnTree.HeroOop.OopReraisesTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/TurnTree/HeroOop/Turn/Reraises/OopReraises/OopReraise"));
            Shared.SolverSizingsInfo.LimpPot.TurnTree.HeroOop.IpReraisesTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/TurnTree/HeroOop/Turn/Reraises/IpReraises/IpReraise"));

            Shared.SolverSizingsInfo.LimpPot.TurnTree.HeroOop.OopDonkBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/TurnTree/HeroOop/River/Bets/OopDonkBets/OopDonkBet"));
            Shared.SolverSizingsInfo.LimpPot.TurnTree.HeroOop.OopBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/TurnTree/HeroOop/River/Bets/OopBets/OopBet"));
            Shared.SolverSizingsInfo.LimpPot.TurnTree.HeroOop.IpBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/TurnTree/HeroOop/River/Bets/IpBets/IpBet"));
            Shared.SolverSizingsInfo.LimpPot.TurnTree.HeroOop.OopRaisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/TurnTree/HeroOop/River/Raises/OopRaises/OopRaise"));
            Shared.SolverSizingsInfo.LimpPot.TurnTree.HeroOop.IpRaisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/TurnTree/HeroOop/River/Raises/IpRaises/IpRaise"));
            Shared.SolverSizingsInfo.LimpPot.TurnTree.HeroOop.OopReraisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/TurnTree/HeroOop/River/Reraises/OopReraises/OopReraise"));
            Shared.SolverSizingsInfo.LimpPot.TurnTree.HeroOop.IpReraisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/TurnTree/HeroOop/River/Reraises/IpReraises/IpReraise"));

            //Hero ip
            Shared.SolverSizingsInfo.LimpPot.TurnTree.HeroIp.OopDonkBetsTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/TurnTree/HeroIp/Turn/Bets/OopDonkBets/OopDonkBet"));
            Shared.SolverSizingsInfo.LimpPot.TurnTree.HeroIp.OopBetsTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/TurnTree/HeroIp/Turn/Bets/OopBets/OopBet"));
            Shared.SolverSizingsInfo.LimpPot.TurnTree.HeroIp.IpBetsTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/TurnTree/HeroIp/Turn/Bets/IpBets/IpBet"));
            Shared.SolverSizingsInfo.LimpPot.TurnTree.HeroIp.OopRaisesTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes($"/SolverSizingSettings/LimpPot/TurnTree/HeroIp/Turn/Raises/OopRaises/OopRaise"));
            Shared.SolverSizingsInfo.LimpPot.TurnTree.HeroIp.IpRaisesTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/TurnTree/HeroIp/Turn/Raises/IpRaises/IpRaise"));
            Shared.SolverSizingsInfo.LimpPot.TurnTree.HeroIp.OopReraisesTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/TurnTree/HeroIp/Turn/Reraises/OopReraises/OopReraise"));
            Shared.SolverSizingsInfo.LimpPot.TurnTree.HeroIp.IpReraisesTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/TurnTree/HeroIp/Turn/Reraises/IpReraises/IpReraise"));

            Shared.SolverSizingsInfo.LimpPot.TurnTree.HeroIp.OopDonkBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/TurnTree/HeroIp/River/Bets/OopDonkBets/OopDonkBet"));
            Shared.SolverSizingsInfo.LimpPot.TurnTree.HeroIp.OopBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/TurnTree/HeroIp/River/Bets/OopBets/OopBet"));
            Shared.SolverSizingsInfo.LimpPot.TurnTree.HeroIp.IpBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/TurnTree/HeroIp/River/Bets/IpBets/IpBet"));
            Shared.SolverSizingsInfo.LimpPot.TurnTree.HeroIp.OopRaisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/TurnTree/HeroIp/River/Raises/OopRaises/OopRaise"));
            Shared.SolverSizingsInfo.LimpPot.TurnTree.HeroIp.IpRaisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/TurnTree/HeroIp/River/Raises/IpRaises/IpRaise"));
            Shared.SolverSizingsInfo.LimpPot.TurnTree.HeroIp.OopReraisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/TurnTree/HeroIp/River/Reraises/OopReraises/OopReraise"));
            Shared.SolverSizingsInfo.LimpPot.TurnTree.HeroIp.IpReraisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/TurnTree/HeroIp/River/Reraises/IpReraises/IpReraise"));

            //River tree
            //Hero oop
            Shared.SolverSizingsInfo.LimpPot.RiverTree.HeroOop.OopDonkBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/RiverTree/HeroOop/River/Bets/OopDonkBets/OopDonkBet"));
            Shared.SolverSizingsInfo.LimpPot.RiverTree.HeroOop.OopBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/RiverTree/HeroOop/River/Bets/OopBets/OopBet"));
            Shared.SolverSizingsInfo.LimpPot.RiverTree.HeroOop.IpBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/RiverTree/HeroOop/River/Bets/IpBets/IpBet"));
            Shared.SolverSizingsInfo.LimpPot.RiverTree.HeroOop.OopRaisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/RiverTree/HeroOop/River/Raises/OopRaises/OopRaise"));
            Shared.SolverSizingsInfo.LimpPot.RiverTree.HeroOop.IpRaisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/RiverTree/HeroOop/River/Raises/IpRaises/IpRaise"));
            Shared.SolverSizingsInfo.LimpPot.RiverTree.HeroOop.OopReraisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/RiverTree/HeroOop/River/Reraises/OopReraises/OopReraise"));
            Shared.SolverSizingsInfo.LimpPot.RiverTree.HeroOop.IpReraisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/RiverTree/HeroOop/River/Reraises/IpReraises/IpReraise"));

            //Hero ip
            Shared.SolverSizingsInfo.LimpPot.RiverTree.HeroIp.OopDonkBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/RiverTree/HeroIp/River/Bets/OopDonkBets/OopDonkBet"));
            Shared.SolverSizingsInfo.LimpPot.RiverTree.HeroIp.OopBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/RiverTree/HeroIp/River/Bets/OopBets/OopBet"));
            Shared.SolverSizingsInfo.LimpPot.RiverTree.HeroIp.IpBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/RiverTree/HeroIp/River/Bets/IpBets/IpBet"));
            Shared.SolverSizingsInfo.LimpPot.RiverTree.HeroIp.OopRaisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/RiverTree/HeroIp/River/Raises/OopRaises/OopRaise"));
            Shared.SolverSizingsInfo.LimpPot.RiverTree.HeroIp.IpRaisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/RiverTree/HeroIp/River/Raises/IpRaises/IpRaise"));
            Shared.SolverSizingsInfo.LimpPot.RiverTree.HeroIp.OopReraisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/RiverTree/HeroIp/River/Reraises/OopReraises/OopReraise"));
            Shared.SolverSizingsInfo.LimpPot.RiverTree.HeroIp.IpReraisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/LimpPot/RiverTree/HeroIp/River/Reraises/IpReraises/IpReraise"));

            #endregion

            #region Raise pot

            //Turn tree
            //Hero oop
            Shared.SolverSizingsInfo.RaisePot.TurnTree.HeroOop.OopDonkBetsTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/TurnTree/HeroOop/Turn/Bets/OopDonkBets/OopDonkBet"));
            Shared.SolverSizingsInfo.RaisePot.TurnTree.HeroOop.OopBetsTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/TurnTree/HeroOop/Turn/Bets/OopBets/OopBet"));
            Shared.SolverSizingsInfo.RaisePot.TurnTree.HeroOop.IpBetsTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/TurnTree/HeroOop/Turn/Bets/IpBets/IpBet"));
            Shared.SolverSizingsInfo.RaisePot.TurnTree.HeroOop.OopRaisesTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes($"/SolverSizingSettings/RaisePot/TurnTree/HeroOop/Turn/Raises/OopRaises/OopRaise"));
            Shared.SolverSizingsInfo.RaisePot.TurnTree.HeroOop.IpRaisesTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/TurnTree/HeroOop/Turn/Raises/IpRaises/IpRaise"));
            Shared.SolverSizingsInfo.RaisePot.TurnTree.HeroOop.OopReraisesTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/TurnTree/HeroOop/Turn/Reraises/OopReraises/OopReraise"));
            Shared.SolverSizingsInfo.RaisePot.TurnTree.HeroOop.IpReraisesTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/TurnTree/HeroOop/Turn/Reraises/IpReraises/IpReraise"));

            Shared.SolverSizingsInfo.RaisePot.TurnTree.HeroOop.OopDonkBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/TurnTree/HeroOop/River/Bets/OopDonkBets/OopDonkBet"));
            Shared.SolverSizingsInfo.RaisePot.TurnTree.HeroOop.OopBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/TurnTree/HeroOop/River/Bets/OopBets/OopBet"));
            Shared.SolverSizingsInfo.RaisePot.TurnTree.HeroOop.IpBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/TurnTree/HeroOop/River/Bets/IpBets/IpBet"));
            Shared.SolverSizingsInfo.RaisePot.TurnTree.HeroOop.OopRaisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/TurnTree/HeroOop/River/Raises/OopRaises/OopRaise"));
            Shared.SolverSizingsInfo.RaisePot.TurnTree.HeroOop.IpRaisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/TurnTree/HeroOop/River/Raises/IpRaises/IpRaise"));
            Shared.SolverSizingsInfo.RaisePot.TurnTree.HeroOop.OopReraisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/TurnTree/HeroOop/River/Reraises/OopReraises/OopReraise"));
            Shared.SolverSizingsInfo.RaisePot.TurnTree.HeroOop.IpReraisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/TurnTree/HeroOop/River/Reraises/IpReraises/IpReraise"));

            //Hero ip
            Shared.SolverSizingsInfo.RaisePot.TurnTree.HeroIp.OopDonkBetsTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/TurnTree/HeroIp/Turn/Bets/OopDonkBets/OopDonkBet"));
            Shared.SolverSizingsInfo.RaisePot.TurnTree.HeroIp.OopBetsTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/TurnTree/HeroIp/Turn/Bets/OopBets/OopBet"));
            Shared.SolverSizingsInfo.RaisePot.TurnTree.HeroIp.IpBetsTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/TurnTree/HeroIp/Turn/Bets/IpBets/IpBet"));
            Shared.SolverSizingsInfo.RaisePot.TurnTree.HeroIp.OopRaisesTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes($"/SolverSizingSettings/RaisePot/TurnTree/HeroIp/Turn/Raises/OopRaises/OopRaise"));
            Shared.SolverSizingsInfo.RaisePot.TurnTree.HeroIp.IpRaisesTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/TurnTree/HeroIp/Turn/Raises/IpRaises/IpRaise"));
            Shared.SolverSizingsInfo.RaisePot.TurnTree.HeroIp.OopReraisesTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/TurnTree/HeroIp/Turn/Reraises/OopReraises/OopReraise"));
            Shared.SolverSizingsInfo.RaisePot.TurnTree.HeroIp.IpReraisesTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/TurnTree/HeroIp/Turn/Reraises/IpReraises/IpReraise"));

            Shared.SolverSizingsInfo.RaisePot.TurnTree.HeroIp.OopDonkBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/TurnTree/HeroIp/River/Bets/OopDonkBets/OopDonkBet"));
            Shared.SolverSizingsInfo.RaisePot.TurnTree.HeroIp.OopBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/TurnTree/HeroIp/River/Bets/OopBets/OopBet"));
            Shared.SolverSizingsInfo.RaisePot.TurnTree.HeroIp.IpBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/TurnTree/HeroIp/River/Bets/IpBets/IpBet"));
            Shared.SolverSizingsInfo.RaisePot.TurnTree.HeroIp.OopRaisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/TurnTree/HeroIp/River/Raises/OopRaises/OopRaise"));
            Shared.SolverSizingsInfo.RaisePot.TurnTree.HeroIp.IpRaisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/TurnTree/HeroIp/River/Raises/IpRaises/IpRaise"));
            Shared.SolverSizingsInfo.RaisePot.TurnTree.HeroIp.OopReraisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/TurnTree/HeroIp/River/Reraises/OopReraises/OopReraise"));
            Shared.SolverSizingsInfo.RaisePot.TurnTree.HeroIp.IpReraisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/TurnTree/HeroIp/River/Reraises/IpReraises/IpReraise"));

            //River tree
            //Hero oop
            Shared.SolverSizingsInfo.RaisePot.RiverTree.HeroOop.OopDonkBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/RiverTree/HeroOop/River/Bets/OopDonkBets/OopDonkBet"));
            Shared.SolverSizingsInfo.RaisePot.RiverTree.HeroOop.OopBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/RiverTree/HeroOop/River/Bets/OopBets/OopBet"));
            Shared.SolverSizingsInfo.RaisePot.RiverTree.HeroOop.IpBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/RiverTree/HeroOop/River/Bets/IpBets/IpBet"));
            Shared.SolverSizingsInfo.RaisePot.RiverTree.HeroOop.OopRaisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/RiverTree/HeroOop/River/Raises/OopRaises/OopRaise"));
            Shared.SolverSizingsInfo.RaisePot.RiverTree.HeroOop.IpRaisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/RiverTree/HeroOop/River/Raises/IpRaises/IpRaise"));
            Shared.SolverSizingsInfo.RaisePot.RiverTree.HeroOop.OopReraisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/RiverTree/HeroOop/River/Reraises/OopReraises/OopReraise"));
            Shared.SolverSizingsInfo.RaisePot.RiverTree.HeroOop.IpReraisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/RiverTree/HeroOop/River/Reraises/IpReraises/IpReraise"));

            //Hero ip
            Shared.SolverSizingsInfo.RaisePot.RiverTree.HeroIp.OopDonkBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/RiverTree/HeroIp/River/Bets/OopDonkBets/OopDonkBet"));
            Shared.SolverSizingsInfo.RaisePot.RiverTree.HeroIp.OopBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/RiverTree/HeroIp/River/Bets/OopBets/OopBet"));
            Shared.SolverSizingsInfo.RaisePot.RiverTree.HeroIp.IpBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/RiverTree/HeroIp/River/Bets/IpBets/IpBet"));
            Shared.SolverSizingsInfo.RaisePot.RiverTree.HeroIp.OopRaisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/RiverTree/HeroIp/River/Raises/OopRaises/OopRaise"));
            Shared.SolverSizingsInfo.RaisePot.RiverTree.HeroIp.IpRaisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/RiverTree/HeroIp/River/Raises/IpRaises/IpRaise"));
            Shared.SolverSizingsInfo.RaisePot.RiverTree.HeroIp.OopReraisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/RiverTree/HeroIp/River/Reraises/OopReraises/OopReraise"));
            Shared.SolverSizingsInfo.RaisePot.RiverTree.HeroIp.IpReraisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/RaisePot/RiverTree/HeroIp/River/Reraises/IpReraises/IpReraise"));

            #endregion

            #region 3bet pot

            //Turn tree
            //Hero oop
            Shared.SolverSizingsInfo.ThreeBetPot.TurnTree.HeroOop.OopDonkBetsTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/TurnTree/HeroOop/Turn/Bets/OopDonkBets/OopDonkBet"));
            Shared.SolverSizingsInfo.ThreeBetPot.TurnTree.HeroOop.OopBetsTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/TurnTree/HeroOop/Turn/Bets/OopBets/OopBet"));
            Shared.SolverSizingsInfo.ThreeBetPot.TurnTree.HeroOop.IpBetsTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/TurnTree/HeroOop/Turn/Bets/IpBets/IpBet"));
            Shared.SolverSizingsInfo.ThreeBetPot.TurnTree.HeroOop.OopRaisesTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes($"/SolverSizingSettings/ThreeBetPot/TurnTree/HeroOop/Turn/Raises/OopRaises/OopRaise"));
            Shared.SolverSizingsInfo.ThreeBetPot.TurnTree.HeroOop.IpRaisesTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/TurnTree/HeroOop/Turn/Raises/IpRaises/IpRaise"));
            Shared.SolverSizingsInfo.ThreeBetPot.TurnTree.HeroOop.OopReraisesTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/TurnTree/HeroOop/Turn/Reraises/OopReraises/OopReraise"));
            Shared.SolverSizingsInfo.ThreeBetPot.TurnTree.HeroOop.IpReraisesTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/TurnTree/HeroOop/Turn/Reraises/IpReraises/IpReraise"));

            Shared.SolverSizingsInfo.ThreeBetPot.TurnTree.HeroOop.OopDonkBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/TurnTree/HeroOop/River/Bets/OopDonkBets/OopDonkBet"));
            Shared.SolverSizingsInfo.ThreeBetPot.TurnTree.HeroOop.OopBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/TurnTree/HeroOop/River/Bets/OopBets/OopBet"));
            Shared.SolverSizingsInfo.ThreeBetPot.TurnTree.HeroOop.IpBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/TurnTree/HeroOop/River/Bets/IpBets/IpBet"));
            Shared.SolverSizingsInfo.ThreeBetPot.TurnTree.HeroOop.OopRaisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/TurnTree/HeroOop/River/Raises/OopRaises/OopRaise"));
            Shared.SolverSizingsInfo.ThreeBetPot.TurnTree.HeroOop.IpRaisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/TurnTree/HeroOop/River/Raises/IpRaises/IpRaise"));
            Shared.SolverSizingsInfo.ThreeBetPot.TurnTree.HeroOop.OopReraisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/TurnTree/HeroOop/River/Reraises/OopReraises/OopReraise"));
            Shared.SolverSizingsInfo.ThreeBetPot.TurnTree.HeroOop.IpReraisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/TurnTree/HeroOop/River/Reraises/IpReraises/IpReraise"));

            //Hero ip
            Shared.SolverSizingsInfo.ThreeBetPot.TurnTree.HeroIp.OopDonkBetsTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/TurnTree/HeroIp/Turn/Bets/OopDonkBets/OopDonkBet"));
            Shared.SolverSizingsInfo.ThreeBetPot.TurnTree.HeroIp.OopBetsTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/TurnTree/HeroIp/Turn/Bets/OopBets/OopBet"));
            Shared.SolverSizingsInfo.ThreeBetPot.TurnTree.HeroIp.IpBetsTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/TurnTree/HeroIp/Turn/Bets/IpBets/IpBet"));
            Shared.SolverSizingsInfo.ThreeBetPot.TurnTree.HeroIp.OopRaisesTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes($"/SolverSizingSettings/ThreeBetPot/TurnTree/HeroIp/Turn/Raises/OopRaises/OopRaise"));
            Shared.SolverSizingsInfo.ThreeBetPot.TurnTree.HeroIp.IpRaisesTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/TurnTree/HeroIp/Turn/Raises/IpRaises/IpRaise"));
            Shared.SolverSizingsInfo.ThreeBetPot.TurnTree.HeroIp.OopReraisesTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/TurnTree/HeroIp/Turn/Reraises/OopReraises/OopReraise"));
            Shared.SolverSizingsInfo.ThreeBetPot.TurnTree.HeroIp.IpReraisesTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/TurnTree/HeroIp/Turn/Reraises/IpReraises/IpReraise"));

            Shared.SolverSizingsInfo.ThreeBetPot.TurnTree.HeroIp.OopDonkBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/TurnTree/HeroIp/River/Bets/OopDonkBets/OopDonkBet"));
            Shared.SolverSizingsInfo.ThreeBetPot.TurnTree.HeroIp.OopBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/TurnTree/HeroIp/River/Bets/OopBets/OopBet"));
            Shared.SolverSizingsInfo.ThreeBetPot.TurnTree.HeroIp.IpBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/TurnTree/HeroIp/River/Bets/IpBets/IpBet"));
            Shared.SolverSizingsInfo.ThreeBetPot.TurnTree.HeroIp.OopRaisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/TurnTree/HeroIp/River/Raises/OopRaises/OopRaise"));
            Shared.SolverSizingsInfo.ThreeBetPot.TurnTree.HeroIp.IpRaisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/TurnTree/HeroIp/River/Raises/IpRaises/IpRaise"));
            Shared.SolverSizingsInfo.ThreeBetPot.TurnTree.HeroIp.OopReraisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/TurnTree/HeroIp/River/Reraises/OopReraises/OopReraise"));
            Shared.SolverSizingsInfo.ThreeBetPot.TurnTree.HeroIp.IpReraisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/TurnTree/HeroIp/River/Reraises/IpReraises/IpReraise"));

            //River tree
            //Hero oop
            Shared.SolverSizingsInfo.ThreeBetPot.RiverTree.HeroOop.OopDonkBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/RiverTree/HeroOop/River/Bets/OopDonkBets/OopDonkBet"));
            Shared.SolverSizingsInfo.ThreeBetPot.RiverTree.HeroOop.OopBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/RiverTree/HeroOop/River/Bets/OopBets/OopBet"));
            Shared.SolverSizingsInfo.ThreeBetPot.RiverTree.HeroOop.IpBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/RiverTree/HeroOop/River/Bets/IpBets/IpBet"));
            Shared.SolverSizingsInfo.ThreeBetPot.RiverTree.HeroOop.OopRaisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/RiverTree/HeroOop/River/Raises/OopRaises/OopRaise"));
            Shared.SolverSizingsInfo.ThreeBetPot.RiverTree.HeroOop.IpRaisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/RiverTree/HeroOop/River/Raises/IpRaises/IpRaise"));
            Shared.SolverSizingsInfo.ThreeBetPot.RiverTree.HeroOop.OopReraisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/RiverTree/HeroOop/River/Reraises/OopReraises/OopReraise"));
            Shared.SolverSizingsInfo.ThreeBetPot.RiverTree.HeroOop.IpReraisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/RiverTree/HeroOop/River/Reraises/IpReraises/IpReraise"));

            //Hero ip
            Shared.SolverSizingsInfo.ThreeBetPot.RiverTree.HeroIp.OopDonkBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/RiverTree/HeroIp/River/Bets/OopDonkBets/OopDonkBet"));
            Shared.SolverSizingsInfo.ThreeBetPot.RiverTree.HeroIp.OopBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/RiverTree/HeroIp/River/Bets/OopBets/OopBet"));
            Shared.SolverSizingsInfo.ThreeBetPot.RiverTree.HeroIp.IpBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/RiverTree/HeroIp/River/Bets/IpBets/IpBet"));
            Shared.SolverSizingsInfo.ThreeBetPot.RiverTree.HeroIp.OopRaisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/RiverTree/HeroIp/River/Raises/OopRaises/OopRaise"));
            Shared.SolverSizingsInfo.ThreeBetPot.RiverTree.HeroIp.IpRaisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/RiverTree/HeroIp/River/Raises/IpRaises/IpRaise"));
            Shared.SolverSizingsInfo.ThreeBetPot.RiverTree.HeroIp.OopReraisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/RiverTree/HeroIp/River/Reraises/OopReraises/OopReraise"));
            Shared.SolverSizingsInfo.ThreeBetPot.RiverTree.HeroIp.IpReraisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/ThreeBetPot/RiverTree/HeroIp/River/Reraises/IpReraises/IpReraise"));

            #endregion

            #region 4bet+ pot

            //Turn tree
            //Hero oop
            Shared.SolverSizingsInfo.FourBetPlusPot.TurnTree.HeroOop.OopDonkBetsTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/TurnTree/HeroOop/Turn/Bets/OopDonkBets/OopDonkBet"));
            Shared.SolverSizingsInfo.FourBetPlusPot.TurnTree.HeroOop.OopBetsTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/TurnTree/HeroOop/Turn/Bets/OopBets/OopBet"));
            Shared.SolverSizingsInfo.FourBetPlusPot.TurnTree.HeroOop.IpBetsTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/TurnTree/HeroOop/Turn/Bets/IpBets/IpBet"));
            Shared.SolverSizingsInfo.FourBetPlusPot.TurnTree.HeroOop.OopRaisesTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes($"/SolverSizingSettings/FourBetPlusPot/TurnTree/HeroOop/Turn/Raises/OopRaises/OopRaise"));
            Shared.SolverSizingsInfo.FourBetPlusPot.TurnTree.HeroOop.IpRaisesTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/TurnTree/HeroOop/Turn/Raises/IpRaises/IpRaise"));
            Shared.SolverSizingsInfo.FourBetPlusPot.TurnTree.HeroOop.OopReraisesTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/TurnTree/HeroOop/Turn/Reraises/OopReraises/OopReraise"));
            Shared.SolverSizingsInfo.FourBetPlusPot.TurnTree.HeroOop.IpReraisesTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/TurnTree/HeroOop/Turn/Reraises/IpReraises/IpReraise"));

            Shared.SolverSizingsInfo.FourBetPlusPot.TurnTree.HeroOop.OopDonkBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/TurnTree/HeroOop/River/Bets/OopDonkBets/OopDonkBet"));
            Shared.SolverSizingsInfo.FourBetPlusPot.TurnTree.HeroOop.OopBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/TurnTree/HeroOop/River/Bets/OopBets/OopBet"));
            Shared.SolverSizingsInfo.FourBetPlusPot.TurnTree.HeroOop.IpBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/TurnTree/HeroOop/River/Bets/IpBets/IpBet"));
            Shared.SolverSizingsInfo.FourBetPlusPot.TurnTree.HeroOop.OopRaisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/TurnTree/HeroOop/River/Raises/OopRaises/OopRaise"));
            Shared.SolverSizingsInfo.FourBetPlusPot.TurnTree.HeroOop.IpRaisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/TurnTree/HeroOop/River/Raises/IpRaises/IpRaise"));
            Shared.SolverSizingsInfo.FourBetPlusPot.TurnTree.HeroOop.OopReraisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/TurnTree/HeroOop/River/Reraises/OopReraises/OopReraise"));
            Shared.SolverSizingsInfo.FourBetPlusPot.TurnTree.HeroOop.IpReraisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/TurnTree/HeroOop/River/Reraises/IpReraises/IpReraise"));

            //Hero ip
            Shared.SolverSizingsInfo.FourBetPlusPot.TurnTree.HeroIp.OopDonkBetsTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/TurnTree/HeroIp/Turn/Bets/OopDonkBets/OopDonkBet"));
            Shared.SolverSizingsInfo.FourBetPlusPot.TurnTree.HeroIp.OopBetsTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/TurnTree/HeroIp/Turn/Bets/OopBets/OopBet"));
            Shared.SolverSizingsInfo.FourBetPlusPot.TurnTree.HeroIp.IpBetsTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/TurnTree/HeroIp/Turn/Bets/IpBets/IpBet"));
            Shared.SolverSizingsInfo.FourBetPlusPot.TurnTree.HeroIp.OopRaisesTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes($"/SolverSizingSettings/FourBetPlusPot/TurnTree/HeroIp/Turn/Raises/OopRaises/OopRaise"));
            Shared.SolverSizingsInfo.FourBetPlusPot.TurnTree.HeroIp.IpRaisesTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/TurnTree/HeroIp/Turn/Raises/IpRaises/IpRaise"));
            Shared.SolverSizingsInfo.FourBetPlusPot.TurnTree.HeroIp.OopReraisesTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/TurnTree/HeroIp/Turn/Reraises/OopReraises/OopReraise"));
            Shared.SolverSizingsInfo.FourBetPlusPot.TurnTree.HeroIp.IpReraisesTurn = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/TurnTree/HeroIp/Turn/Reraises/IpReraises/IpReraise"));

            Shared.SolverSizingsInfo.FourBetPlusPot.TurnTree.HeroIp.OopDonkBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/TurnTree/HeroIp/River/Bets/OopDonkBets/OopDonkBet"));
            Shared.SolverSizingsInfo.FourBetPlusPot.TurnTree.HeroIp.OopBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/TurnTree/HeroIp/River/Bets/OopBets/OopBet"));
            Shared.SolverSizingsInfo.FourBetPlusPot.TurnTree.HeroIp.IpBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/TurnTree/HeroIp/River/Bets/IpBets/IpBet"));
            Shared.SolverSizingsInfo.FourBetPlusPot.TurnTree.HeroIp.OopRaisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/TurnTree/HeroIp/River/Raises/OopRaises/OopRaise"));
            Shared.SolverSizingsInfo.FourBetPlusPot.TurnTree.HeroIp.IpRaisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/TurnTree/HeroIp/River/Raises/IpRaises/IpRaise"));
            Shared.SolverSizingsInfo.FourBetPlusPot.TurnTree.HeroIp.OopReraisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/TurnTree/HeroIp/River/Reraises/OopReraises/OopReraise"));
            Shared.SolverSizingsInfo.FourBetPlusPot.TurnTree.HeroIp.IpReraisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/TurnTree/HeroIp/River/Reraises/IpReraises/IpReraise"));

            //River tree
            //Hero oop
            Shared.SolverSizingsInfo.FourBetPlusPot.RiverTree.HeroOop.OopDonkBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/RiverTree/HeroOop/River/Bets/OopDonkBets/OopDonkBet"));
            Shared.SolverSizingsInfo.FourBetPlusPot.RiverTree.HeroOop.OopBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/RiverTree/HeroOop/River/Bets/OopBets/OopBet"));
            Shared.SolverSizingsInfo.FourBetPlusPot.RiverTree.HeroOop.IpBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/RiverTree/HeroOop/River/Bets/IpBets/IpBet"));
            Shared.SolverSizingsInfo.FourBetPlusPot.RiverTree.HeroOop.OopRaisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/RiverTree/HeroOop/River/Raises/OopRaises/OopRaise"));
            Shared.SolverSizingsInfo.FourBetPlusPot.RiverTree.HeroOop.IpRaisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/RiverTree/HeroOop/River/Raises/IpRaises/IpRaise"));
            Shared.SolverSizingsInfo.FourBetPlusPot.RiverTree.HeroOop.OopReraisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/RiverTree/HeroOop/River/Reraises/OopReraises/OopReraise"));
            Shared.SolverSizingsInfo.FourBetPlusPot.RiverTree.HeroOop.IpReraisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/RiverTree/HeroOop/River/Reraises/IpReraises/IpReraise"));

            //Hero ip
            Shared.SolverSizingsInfo.FourBetPlusPot.RiverTree.HeroIp.OopDonkBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/RiverTree/HeroIp/River/Bets/OopDonkBets/OopDonkBet"));
            Shared.SolverSizingsInfo.FourBetPlusPot.RiverTree.HeroIp.OopBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/RiverTree/HeroIp/River/Bets/OopBets/OopBet"));
            Shared.SolverSizingsInfo.FourBetPlusPot.RiverTree.HeroIp.IpBetsRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/RiverTree/HeroIp/River/Bets/IpBets/IpBet"));
            Shared.SolverSizingsInfo.FourBetPlusPot.RiverTree.HeroIp.OopRaisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/RiverTree/HeroIp/River/Raises/OopRaises/OopRaise"));
            Shared.SolverSizingsInfo.FourBetPlusPot.RiverTree.HeroIp.IpRaisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/RiverTree/HeroIp/River/Raises/IpRaises/IpRaise"));
            Shared.SolverSizingsInfo.FourBetPlusPot.RiverTree.HeroIp.OopReraisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/RiverTree/HeroIp/River/Reraises/OopReraises/OopReraise"));
            Shared.SolverSizingsInfo.FourBetPlusPot.RiverTree.HeroIp.IpReraisesRiver = ConvertNodesToSizingsArray(xmlDoc.SelectNodes("/SolverSizingSettings/FourBetPlusPot/RiverTree/HeroIp/River/Reraises/IpReraises/IpReraise"));

            #endregion

            float[] ConvertNodesToSizingsArray(XmlNodeList nodeList)
            {
                if (nodeList == null)
                    return new float[] { };

                List<float> values = new List<float>();

                foreach (object obj in nodeList)
                {
                    XmlNode node = obj as XmlNode;

                    if (!float.TryParse(node.InnerText, NumberStyles.Any, CultureInfo.InvariantCulture, out float value))
                        throw new FormatException($"Node {node.Name} has incorrect value");

                    values.Add(value);
                }

                return values.ToArray();
            }
        }
    }
}
