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

            string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.xml");

            string saveFolder = string.Empty;

            string snapshotsFolder = string.Empty;

            string bitmapsBufferStr = string.Empty;

            string getLastNHandsStr = string.Empty;

            string timerIntervalStr = string.Empty;

            string screenshotSaveIntervalStr = string.Empty;

            string windowExpireTimeoutStr = string.Empty;

            string mouseCursorStr = string.Empty;

            string windowsBackColorStr = string.Empty;

            string serverName = string.Empty;

            string dbName = string.Empty;

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

            #region GG Poker

            string scanTablesGgStr = string.Empty;

            string cardBackGgStr = string.Empty;

            string deckGgStr = string.Empty;

            string savePicturesGgStr = string.Empty;

            string savePicturesFolderGg = string.Empty;

            #endregion

            XmlDocument xmlDoc = new XmlDocument();

            if (!File.Exists(settingsPath))
            {
                Error = "Settings file doesn't exist in current directory";

                return false;
            }

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

                            #endregion

                            #region Rooms

                            #region GGPoker

                            case "ScanTablesGg":

                                scanTablesGgStr = node.Attributes["Value"].Value;

                                break;

                            case "CardBackIndexGg":

                                cardBackGgStr = node.Attributes["Value"].Value;

                                break;

                            case "DeckIndexGg":

                                deckGgStr = node.Attributes["Value"].Value;

                                break;

                            case "SavePicturesPerHandGg":

                                savePicturesGgStr = node.Attributes["Value"].Value;

                                break;

                            case "PicturesSaveFolderGg":

                                savePicturesFolderGg = node.Attributes["Value"].Value;

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
            {
                Error = "Pictures save directory is not specified";

                return false;
            }

            if (!Directory.Exists(saveFolder))
            {
                Error = "Pictures save directory doesn't exist";

                return false;
            }

            //Snapshots folder
            if (string.IsNullOrEmpty(snapshotsFolder))
            {
                Error = "Snapshots save directory is not specified";

                return false;
            }

            if (!Directory.Exists(snapshotsFolder))
            {
                Error = "Snapshots save directory doesn't exist";

                return false;
            }

            //Bitmaps buffer
            if (string.IsNullOrEmpty(bitmapsBufferStr))
            {
                Error = "Bitmaps buffer is not specified";

                return false;
            }

            if (!int.TryParse(bitmapsBufferStr, out int bitmapsBuffer))
            {
                Error = "Bitmaps buffer has incorrect format";

                return false;
            }

            if (bitmapsBuffer < 1)
            {
                Error = "Bitmaps buffer should be more than 0";

                return false;
            }

            //Get last n hands
            if (string.IsNullOrEmpty(getLastNHandsStr))
            {
                Error = "Get last n hands value is not specified";

                return false;
            }

            if (!int.TryParse(getLastNHandsStr, out int getLastNHands))
            {
                Error = "Get last n hands value has incorrect format";

                return false;
            }

            if (getLastNHands < 1)
            {
                Error = "Get last n hands value should be more than 0";

                return false;
            }

            //Timer
            if (string.IsNullOrEmpty(timerIntervalStr))
            {
                Error = "Timer interval is not specified";

                return false;
            }

            if (!int.TryParse(timerIntervalStr, out int timerInterval))
            {
                Error = "Timer interval has incorrect format";

                return false;
            }

            if (timerInterval < 1 || timerInterval > 1000)
            {
                Error = "Timer interval should be between 1 and 1000";

                return false;
            }

            //Screenshot save interval
            if (string.IsNullOrEmpty(screenshotSaveIntervalStr))
            {
                Error = "Screenshots save interval is not specified";

                return false;
            }

            if (!int.TryParse(screenshotSaveIntervalStr, out int screenshotSaveInterval))
            {
                Error = "Screenshots save interval has incorrect format";

                return false;
            }

            if (screenshotSaveInterval < 1)
            {
                Error = "Screenshots save interval should be more than 0";

                return false;
            }

            //Window expire timeout
            if (string.IsNullOrEmpty(windowExpireTimeoutStr))
            {
                Error = "Window expire timeout is not specified";

                return false;
            }

            if (!int.TryParse(windowExpireTimeoutStr, out int windowExpireTimeout))
            {
                Error = "Window expire timeout has incorrect format";

                return false;
            }

            if (windowExpireTimeout <= 0 || windowExpireTimeout > 59)
            {
                Error = "Window expire timeout should be between 1 and 59";

                return false;
            }

            TimeSpan windowInfoExpireTimeout = new TimeSpan(0, 0, 0, windowExpireTimeout);

            //Mouse cursor
            if (string.IsNullOrEmpty(mouseCursorStr))
            {
                Error = "Mouse cursor is not specified";

                return false;
            }

            string cursorFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\Cursors", mouseCursorStr);

            if (!File.Exists(cursorFilePath))
            {
                Error = "Mouse cursor file doesn't exist";

                return false;
            }

            BitmapDecorator cursorImage = null;

            try
            {
                cursorImage = new BitmapDecorator((Bitmap)Image.FromFile(cursorFilePath, true));
            }
            catch
            {
                Error = "Unable create cursor image from file";

                return false;
            }

            //Window back color brush
            SolidColorBrush windowBackground = null;

            if (string.IsNullOrEmpty(windowsBackColorStr))
            {
                Error = "Window back color is not specified";

                return false;
            }

            try
            {
                System.Windows.Media.Color convertedColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(windowsBackColorStr);

                windowBackground = new SolidColorBrush(convertedColor);
            }
            catch
            {
                Error = "Window back color has incorrect format";

                return false;
            }

            #endregion

            #region Server

            //Server name
            if (string.IsNullOrEmpty(serverName))
            {
                Error = "Server name is empty";

                return false;
            }

            //Db name
            if (string.IsNullOrEmpty(dbName))
            {
                Error = "Database name is empty";

                return false;
            }

            #endregion

            #region Solver 

            //Solver mode
            if (string.IsNullOrEmpty(isLocalSolverStr))
            {
                Error = "Solver mode selector is empty";

                return false;
            }

            if (!bool.TryParse(isLocalSolverStr, out bool isLocalSolver))
            {
                Error = "Solver mode selector has incorrect format";

                return false;
            }

            uint solverPort = 0;

            if (isLocalSolver)
            {
                //Solver folder
                if (string.IsNullOrEmpty(solverFolder))
                {
                    Error = "Solver directory is not specified";

                    return false;
                }

                if (!Directory.Exists(solverFolder))
                {
                    Error = "Solver directory doesn't exist";

                    return false;
                }

                //Solver file
                if (string.IsNullOrEmpty(solverFile))
                {
                    Error = "Solver file is not specified";

                    return false;
                }

                if (!File.Exists(Path.Combine(solverFolder, solverFile)))
                {
                    Error = "Solver file doesn't exist";

                    return false;
                }
            }
            else
            {
                //Solver port
                if (string.IsNullOrEmpty(solverPortStr))
                {
                    Error = "Remote solver port is not specified";

                    return false;
                }

                if (!uint.TryParse(solverPortStr, out solverPort))
                {
                    Error = "Remote solver port has incorrect format (should be between 0 and 65535)";

                    return false;
                }

                //Solver ip
                if (string.IsNullOrEmpty(solverIp))
                {
                    Error = "Remote solver ip is not specified";

                    return false;
                }

                if (!Regex.IsMatch(solverIp, @"(localhost)|(\d{1,3}[.]\d{1,3}[.]\d{1,3}[.]\d{1,3})"))
                {
                    Error = "Remote solver ip has incorrect format";

                    return false;
                }
            }

            //Turn solver duration
            if (string.IsNullOrEmpty(turnSolverDurationStr))
            {
                Error = "Turn solver duration is not specified";

                return false;
            }

            if (!int.TryParse(turnSolverDurationStr, out int turnSolverMaxDuration))
            {
                Error = "Turn solver duration has incorrect format";

                return false;
            }

            if (turnSolverMaxDuration <= 0 || turnSolverMaxDuration > 100)
            {
                Error = "Turn solver duration should be between 1 and 100";

                return false;
            }

            //River solver duration
            if (string.IsNullOrEmpty(riverSolverDurationStr))
            {
                Error = "River solver duration is not specified";

                return false;
            }

            if (!int.TryParse(riverSolverDurationStr, out int riverSolverMaxDuration))
            {
                Error = "River solver duration has incorrect format";

                return false;
            }

            if (riverSolverMaxDuration <= 0 || riverSolverMaxDuration > 100)
            {
                Error = "River solver duration should be between 1 and 100";

                return false;
            }

            //Turn solver accuracy
            if (string.IsNullOrEmpty(turnSolverAccuracyStr))
            {
                Error = "Turn solver accuracy is not specified";

                return false;
            }

            if (!double.TryParse(turnSolverAccuracyStr, NumberStyles.Any, new CultureInfo("en-US"), out double turnSolverAccuracy))
            {
                Error = "Turn solver accuracy has incorrect format";

                return false;
            }

            if (turnSolverAccuracy <= 0 || turnSolverAccuracy > 100)
            {
                Error = "Turn solver accuracy should be between 1 and 100";

                return false;
            }

            //River solver accuracy
            if (string.IsNullOrEmpty(riverSolverAccuracyStr))
            {
                Error = "River solver accuracy is not specified";

                return false;
            }

            if (!double.TryParse(riverSolverAccuracyStr, NumberStyles.Any, new CultureInfo("en-US"), out double riverSolverAccuracy))
            {
                Error = "River solver accuracy has incorrect format";

                return false;
            }

            if (riverSolverAccuracy <= 0 || riverSolverAccuracy > 100)
            {
                Error = "River solver accuracy should be between 1 and 100";

                return false;
            }

            //Min line frequency
            if (string.IsNullOrEmpty(minLineFrequencyStr))
            {
                Error = "Min line frequency is not specified";

                return false;
            }

            if (!int.TryParse(minLineFrequencyStr, out int minLineFrequency))
            {
                Error = "Min line frequency has incorrect format";

                return false;
            }

            if (minLineFrequency < 0 || minLineFrequency > 100)
            {
                Error = "Min line frequency should be between 0 and 100";

                return false;
            }

            #endregion

            #region Rooms

            #region GGPoker

            //Scan GGPoker tables
            if (string.IsNullOrEmpty(scanTablesGgStr))
            {
                Error = "Scan GGPoker tables option is not specified";

                return false;
            }

            if (!bool.TryParse(scanTablesGgStr, out bool scanTablesGg))
            {
                Error = "Scan GGPoker tables option has incorrect format";

                return false;
            }

            int cardBackGg = 0;
            int deckGg = 0;
            bool savePicturesGg = false;

            if (scanTablesGg)
            {
                //Card back GGPoker
                if (string.IsNullOrEmpty(cardBackGgStr))
                {
                    Error = "GGPoker card back is not specified";

                    return false;
                }

                if (!int.TryParse(cardBackGgStr, out cardBackGg))
                {
                    Error = "GGPoker card back has incorrect format";

                    return false;
                }

                if (cardBackGg < 1)
                {
                    Error = "GGPoker card back value should be greater than 0";

                    return false;
                }

                //Deck GGPoker
                if (string.IsNullOrEmpty(deckGgStr))
                {
                    Error = "GGPoker deck is not specified";

                    return false;
                }

                if (!int.TryParse(deckGgStr, out deckGg))
                {
                    Error = "GGPoker deck has incorrect format";

                    return false;
                }

                if (deckGg < 1)
                {
                    Error = "GGPoker deck value should be greater than 0";

                    return false;
                }

                //Save pictures per hand on GGPoker
                if (string.IsNullOrEmpty(savePicturesGgStr))
                {
                    Error = "Save pictures per hand option on GGPoker is not specified";

                    return false;
                }

                if (!bool.TryParse(savePicturesGgStr, out savePicturesGg))
                {
                    Error = "Save pictures per hand option on GGPoker has incorrect format";

                    return false;
                }

                if (savePicturesGg)
                {
                    //Save pictures folder on GGPoker
                    if (string.IsNullOrEmpty(savePicturesFolderGg))
                    {
                        Error = "Save pictures folder on GGPoker is not specified";

                        return false;
                    }

                    if (!Directory.Exists(savePicturesFolderGg))
                    {
                        Error = "Save pictures folder on GGPoker doesn't exist";

                        return false;

                    }
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
            Shared.TimerInterval = timerInterval;
            Shared.ScreenshotSaveInterval = screenshotSaveInterval;
            Shared.WindowExpireTimeout = windowInfoExpireTimeout;
            Shared.MouseCursor = cursorImage;
            Application.Current.Resources["CurrentFormBackColor"] = windowBackground;
            Shared.ServerName = serverName;
            Shared.DbName = dbName;
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
            Shared.ScanTablesGg = scanTablesGg;
            Shared.CardBackIndexGg = cardBackGg;
            Shared.DeckIndexGg = deckGg;
            Shared.SavePicturesPerHandGg = savePicturesGg;
            Shared.PicturesSaveFolderGg = savePicturesFolderGg;

            #endregion

            return true;
        }
    }
}
