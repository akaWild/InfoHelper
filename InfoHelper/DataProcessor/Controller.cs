﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;
using BitmapHelper;
using GameInformationUtility;
using InfoHelper.StatsEntities;
using InfoHelper.Utils;
using InfoHelper.ViewModel.DataEntities;
using InfoHelper.ViewModel.States;
using InfoHelper.Windows;
using PokerCommonUtility;
using PokerWindowsUtility;
using ScreenParserUtility;
using Application = System.Windows.Application;

namespace InfoHelper.DataProcessor
{
    public class Controller
    {
        private List<WindowContextInfo> _winContextInfos = new List<WindowContextInfo>();

        private BitmapsContainer _bitmapContainer;

        private DateTime _lastScreenshotSaveTime;

        private PlayersManager _playersManager;

        private readonly ViewModelMain _mainWindowState;

        private readonly ViewModelPlayers _playersWindowState;

        private readonly DispatcherTimer _timer;

        private readonly SettingsManager _settingsManager;

        private readonly CaptureCardManager _captureCardManager;

        private readonly HudsManager _hudsManager;

        private readonly MethodInfo _findWindows;

        private readonly MethodInfo _analyzeParserData;

        private readonly Type _screenParserType;

        private readonly PlayersWindow _playersWindow;

        private readonly object _winContextLock = new object();

        public Controller(ViewModelMain vmMain, ViewModelPlayers vmPlayers)
        {
            _mainWindowState = vmMain;

            _playersWindowState = vmPlayers;

            _mainWindowState.ControlsState.ExitRequested += ControlsState_ExitRequested;

            _mainWindowState.ControlsState.ShowOptionsRequested += ControlsState_ShowOptionsRequested;

            _mainWindowState.ControlsState.ShowPlayersRequested += ControlsState_ShowPlayersRequested;

            _mainWindowState.ControlsState.RunningStateChanged += ControlsState_RunningStateChanged;

            _mainWindowState.ControlsState.FlushPicturesRequested += ControlsState_FlushPicturesRequested;

            _playersWindow = new PlayersWindow { DataContext = _playersWindowState };

            _playersWindow.IsVisibleChanged += (sender, args) => _mainWindowState.ControlsState.PlayersWindowVisible = _playersWindow.IsVisible;

            _timer = new DispatcherTimer();

            _timer.Tick += _timer_Tick;

            _settingsManager = new SettingsManager();

            if (!_settingsManager.RetrieveSettings())
                _mainWindowState.ControlsState.SetError(_settingsManager.Error, ErrorType.Settings);

            try
            {
                AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
                {
                    string currentAssemblyFolder = Path.GetDirectoryName(args.RequestingAssembly.Location);

                    FileInfo[] neighborAssemblies = new DirectoryInfo(currentAssemblyFolder).GetFiles("*.dll");

                    FileInfo requiredAssembly = neighborAssemblies.FirstOrDefault(a => args.Name.Contains(Path.GetFileNameWithoutExtension(a.Name)));

                    if (requiredAssembly == null)
                        return null;

                    return Assembly.LoadFrom(requiredAssembly.FullName);
                };

                StatsManager.LoadCells();

                _captureCardManager = new CaptureCardManager();

                _hudsManager = new HudsManager(_mainWindowState);

                Assembly assemblyWindowsManager = Assembly.LoadFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\GGPoker\\PokerWindowsManager.dll"));

                Type windowsManagerType = assemblyWindowsManager.GetType("PokerWindowsManager.PokerWindowsManager");

                _findWindows = windowsManagerType.GetMethod("FindWindows", new Type[] { typeof(BitmapDecorator) });

                Assembly assemblyScreenParser = Assembly.LoadFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\GGPoker\\ScreenParser.dll"));

                _screenParserType = assemblyScreenParser.GetType("Parser.ScreenParser");

                Assembly assemblyParserDataAnalyzer = Assembly.LoadFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\GGPoker\\ParserDataAnalyzer.dll"));

                Type parserDataAnalyzer = assemblyParserDataAnalyzer.GetType("ParserDataAnalyzer.ParserDataAnalyzer");

                _analyzeParserData = parserDataAnalyzer.GetMethod("AnalyzeParserData", BindingFlags.Static | BindingFlags.Public | BindingFlags.CreateInstance);
            }
            catch (Exception ex)
            {
                HandleException(ex, ErrorType.Critical);
            }
        }

        private void _timer_Tick(object sender, EventArgs e)
        {
            try
            {
                Process();
            }
            catch (Exception ex)
            {
                HandleException(ex, ErrorType.Ordinary);

                Stop();

                Task.Factory.StartNew(SavePictures);
            }
        }

        private void ControlsState_FlushPicturesRequested(object sender, EventArgs e)
        {
            Task.Factory.StartNew(SavePictures);
        }

        private void ControlsState_RunningStateChanged(object sender, EventArgs e)
        {
            if (!_mainWindowState.ControlsState.IsRunning)
                Start();
            else
                Stop();
        }

        private void ControlsState_ShowOptionsRequested(object sender, EventArgs e)
        {
            OptionsWindow optionsWindow = new OptionsWindow();

            if ((bool)optionsWindow.ShowDialog())
            {
                bool retrieveSettingsResult = _settingsManager.RetrieveSettings();

                if(retrieveSettingsResult)
                    _mainWindowState.ControlsState.ResetError();
                else 
                    _mainWindowState.ControlsState.SetError(_settingsManager.Error, ErrorType.Settings);
            }
        }

        private void ControlsState_ShowPlayersRequested(object sender, EventArgs e)
        {
            _playersWindow.Show();
        }

        private void ControlsState_ExitRequested(object sender, EventArgs e)
        {
            Application.Current.Shutdown(0);
        }

        private void Process()
        {
            Bitmap clientScreenBitmap = null;

            BitmapDecorator bmpDecor = null;

            try
            {
                clientScreenBitmap = _captureCardManager.CaptureFrame();

                if (_mainWindowState.ControlsState.SavePictures)
                {
                    if (DateTime.Now.Subtract(_lastScreenshotSaveTime) >= new TimeSpan(0, 0, 0, 0, Shared.ScreenshotSaveInterval))
                    {
                        _lastScreenshotSaveTime = DateTime.Now;

                        clientScreenBitmap.Save(Path.Combine(Shared.SnapshotsFolder, $"{DateTime.Now.Ticks}.bmp"));
                    }
                }

                bmpDecor = new BitmapDecorator(clientScreenBitmap);

                _bitmapContainer.Add(bmpDecor.Copy());

                Point? cursor = CursorManager.FindCursor(bmpDecor);

                PokerWindow[] windows = new PokerWindow[] { };

                if (Shared.ScanTablesGg)
                    windows = (PokerWindow[])_findWindows.Invoke(null, new object[] { bmpDecor });

                List<WindowContextInfo> winContextInfosCopy = null;

                lock (_winContextLock)
                    winContextInfosCopy = _winContextInfos.ToList();

                WindowInfo[] winInfos = new WindowInfo[windows.Length];

                GameContext foregroundGameContext = null;

                for (int i = 0; i < windows.Length; i++)
                {
                    WindowState winState;

                    windows[i].IsFocused = cursor != null && windows[i].Position.Contains(cursor.Value);

                    PokerWindow window = windows[i];

                    bool isHeroActing = false;

                    if(!window.CaptionMatched)
                        winState = window.IsFocused ? WindowState.WrongCaptionFront : WindowState.WrongCaptionBack;
                    else
                    {
                        WindowContextInfo winContextInfo = winContextInfosCopy.FirstOrDefault(wci => wci.GameContext.TableId == window.PokerWindowInfo.TableId);

                        if (winContextInfo == null)
                        {
                            winContextInfo = new WindowContextInfo(new GameContext(window.PokerWindowInfo.TableId));

                            winContextInfosCopy.Add(winContextInfo);
                        }
                        else
                            winContextInfo.LastAccessTime = DateTime.Now;

                        bool regionsOverlapped = false;

                        IScreenParser screenParser = (IScreenParser)_screenParserType.GetConstructor(new[]
                        {
                            typeof(BitmapDecorator), typeof(Rectangle), typeof(string), typeof(int), typeof(int)
                        }).Invoke(new object[] { bmpDecor, window.Position, window.TableSize.ToString(), Shared.CardBackIndexGg, Shared.DeckIndexGg });

                        Rectangle mouseRect = default;

                        if (cursor != null)
                            mouseRect = new Rectangle(cursor.Value.X - window.Position.X, cursor.Value.Y - window.Position.Y, Shared.MouseCursor.Width, Shared.MouseCursor.Height);

                        regionsOverlapped = mouseRect != default && screenParser.RestrictedRegions.ContainsRect(mouseRect);

                        if (!regionsOverlapped)
                        {
                            ScreenParserData screenData = screenParser.ParseWindow();

                            PokerRoomManager.ProcessData(window, screenData, bmpDecor);

                            bool analyzeResult = (bool)_analyzeParserData.Invoke(null, new object[] { screenData, winContextInfo.GameContext });

                            if (analyzeResult)
                            {
                                for (int j = 0; j < screenData.Nicks.Length; j++)
                                {
                                    if (!screenData.IsNickOverlapped[j])
                                    {
                                        (string name, bool isConfirmed) = _playersManager.GetPlayer(screenData.NickImages[j].ToBitmapSource(), screenData.Nicks[j]);

                                        winContextInfo.GameContext.Players[j] = name;
                                        winContextInfo.GameContext.IsPlayerConfirmed[j] = isConfirmed;
                                    }
                                }
                            }

                            isHeroActing = winContextInfo.GameContext.Error == string.Empty;

                            if (window.IsFocused)
                                foregroundGameContext = winContextInfo.GameContext;
                        }

                        if(regionsOverlapped)
                            winState = window.IsFocused ? WindowState.ErrorFront : WindowState.ErrorBack;
                        else
                            winState = window.IsFocused ? WindowState.OkFront : WindowState.OkBack;
                    }

                    winInfos[i] = new WindowInfo(window.Position.ToWindowsRect(), winState, isHeroActing);
                }

                List<WindowContextInfo> winContextInfosUpdated = winContextInfosCopy.Where(wci => DateTime.Now.Subtract(wci.LastAccessTime) < Shared.WindowExpireTimeout).ToList();

                lock (_winContextLock)
                    _winContextInfos = winContextInfosUpdated;

                _hudsManager.UpdateWindows(winInfos);

                _hudsManager.UpdateHuds(foregroundGameContext);
            }
            finally
            {
                bmpDecor?.Dispose();

                clientScreenBitmap?.Dispose();
            }
        }

        private void Start()
        {
            try
            {
                _mainWindowState.ControlsState.ResetError();

                if (_bitmapContainer == null)
                    _bitmapContainer = new BitmapsContainer(Shared.BitmapsBuffer);
                else
                    _bitmapContainer.Capacity = Shared.BitmapsBuffer;

                _playersManager = new PlayersManager(_playersWindowState);

                _captureCardManager.Initialize();

                _timer.Interval = TimeSpan.FromMilliseconds(Shared.TimerInterval);

                _timer.Start();

                _mainWindowState.ControlsState.Start();
            }
            catch (Exception ex)
            {
                HandleException(ex, ErrorType.Ordinary);
            }
        }

        private void Stop()
        {
            _timer.Stop();

            try
            {
                _captureCardManager.DisposeResources();
            }
            catch (Exception ex)
            {
                HandleException(ex, ErrorType.Ordinary);
            }
            finally
            {
                _hudsManager.ResetControls();

                _mainWindowState.ControlsState.Stop();
            }
        }

        private void SavePictures()
        {
            try
            {
                Bitmap[] bitmaps = _bitmapContainer?.GetImages();

                if (bitmaps == null || bitmaps.Length == 0)
                    return;

                DateTime time = DateTime.Now;

                string saveFolderId = $"{time.Day}_{time.Month}_{time.Year} {time.Hour}_{time.Minute}_{time.Second}";

                DirectoryInfo snapshotsDirectory = null;

                string path = $"{Shared.SaveFolder}\\{saveFolderId}\\";

                snapshotsDirectory = !Directory.Exists(path) ? Directory.CreateDirectory(path) : new DirectoryInfo(path);

                _mainWindowState.ControlsState.BeginFlushingPictures();

                ViewModelProgressBarState progressBar = _mainWindowState.ControlsState.ProgressBarState;

                progressBar.MinValue = 0;

                progressBar.MaxValue = 100;

                progressBar.Visible = true;

                for (int i = 0; i < bitmaps.Length; i++)
                {
                    Bitmap bmp = bitmaps[i];

                    string fileName = $"{DateTime.Now.Ticks}.bmp";

                    string bitmapFileName = Path.Combine(snapshotsDirectory.FullName, fileName);

                    bmp.Save(bitmapFileName);

                    bmp.Dispose();

                    progressBar.Value = ((double)i + 1) * 100 / bitmaps.Length;
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, ErrorType.Ordinary);
            }
            finally
            {
                _mainWindowState.ControlsState.ProgressBarState.Visible = false;

                _mainWindowState.ControlsState.EndFlushingPictures();
            }
        }

        private void HandleException(Exception ex, ErrorType errorType)
        {
            try
            {
                try
                {
                    Logger.AddRecord(AppDomain.CurrentDomain.BaseDirectory, $"{ex.Message}. {ex.InnerException?.Message ?? string.Empty}. {ex.InnerException?.StackTrace ?? string.Empty}{ex.StackTrace}");

                    StackTrace st = new StackTrace(ex, true);

                    StackFrame[] frames = st.GetFrames();

                    string message = $"{ex.Message}. {frames[0]}".Replace(Environment.NewLine, " ");

                    _mainWindowState.ControlsState.SetError(message, errorType);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private class WindowContextInfo
        {
            public GameContext GameContext { get; }

            public DateTime LastAccessTime { get; set; }

            public WindowContextInfo(GameContext gc)
            {
                GameContext = gc;

                LastAccessTime = DateTime.Now;
            }
        }
    }
}
