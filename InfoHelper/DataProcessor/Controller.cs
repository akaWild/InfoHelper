using System;
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
        private BitmapsContainer _bitmapContainer;

        private DateTime _lastScreenshotSaveTime;

        private bool _shouldStop = true;

        private readonly ViewModelMain _mainWindowState;

        private readonly DispatcherTimer _timer;

        private readonly SettingsManager _settingsManager;

        private readonly CaptureCardManager _captureCardManager;

        private readonly MethodInfo _findWindowsGg;

        private readonly Type _screenParserTypeGg;
        
        public Controller(ViewModelMain window)
        {
            _mainWindowState = window;

            _mainWindowState.ControlsState.ExitRequested += ControlsState_ExitRequested;

            _mainWindowState.ControlsState.ShowOptionsRequested += ControlsState_ShowOptionsRequested;

            _mainWindowState.ControlsState.RunningStateChanged += ControlsState_RunningStateChanged;

            _mainWindowState.ControlsState.FlushPicturesRequested += ControlsState_FlushPicturesRequested;

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

                Assembly assemblyWindowsManagerGg = Assembly.LoadFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\GGPoker\\PokerWindowsManager.dll"));

                Type windowsManagerTypeGg = assemblyWindowsManagerGg.GetType("PokerWindowsManager.PokerWindowsManager");

                _findWindowsGg = windowsManagerTypeGg.GetMethod("FindWindows", new Type[] { typeof(BitmapDecorator) });

                Assembly assemblyScreenParserGg = Assembly.LoadFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\GGPoker\\ScreenParser.dll"));

                _screenParserTypeGg = assemblyScreenParserGg.GetType("Parser.ScreenParser");
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
                    windows = (PokerWindow[])_findWindowsGg.Invoke(null, new object[] { bmpDecor });

                WindowInfo[] winInfos = new WindowInfo[windows.Length];

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
                        bool hasError = false;

                        Type screenParserType = null;

                        if (window.PokerRoom == PokerRoom.GGPoker)
                            screenParserType = _screenParserTypeGg;

                        if (screenParserType != null)
                        {
                            IScreenParser screenParser = (IScreenParser)screenParserType.GetConstructor(new[]
                            {
                                typeof(BitmapDecorator), typeof(Rectangle), typeof(string), typeof(int), typeof(int)
                            }).Invoke(new object[] { bmpDecor, window.Position, window.TableSize.ToString(), Shared.CardBackIndexGg, Shared.DeckIndexGg });

                            Rectangle mouseRect = default;

                            if (cursor != null)
                                mouseRect = new Rectangle(cursor.Value.X - window.Position.X, cursor.Value.Y - window.Position.Y, Shared.MouseCursor.Width, Shared.MouseCursor.Height);

                            hasError = mouseRect != default && screenParser.RestrictedRegions.ContainsRect(mouseRect);

                            if (!hasError)
                            {
                                ScreenParserData screenData = screenParser.ParseWindow();

                                PokerRoomManager.ProcessData(window, screenData, bmpDecor);

                                isHeroActing = screenData.IsHeroActing;
                            }
                        }

                        if(hasError)
                            winState = window.IsFocused ? WindowState.ErrorFront : WindowState.ErrorBack;
                        else
                            winState = window.IsFocused ? WindowState.OkFront : WindowState.OkBack;
                    }

                    winInfos[i] = new WindowInfo(window.Position.ToWindowsRect(), winState, isHeroActing);
                }

                _mainWindowState.WindowsInfoState.WinInfos = winInfos;

                _mainWindowState.WindowsInfoState.UpdateBindings();
            }
            finally
            {
                bmpDecor?.Dispose();

                clientScreenBitmap?.Dispose();
            }
        }

        private void Start()
        {
            _shouldStop = false;

            _mainWindowState.ControlsState.ResetError();

            if (_bitmapContainer == null)
                _bitmapContainer = new BitmapsContainer(Shared.BitmapsBuffer);
            else
                _bitmapContainer.Capacity = Shared.BitmapsBuffer;

            _captureCardManager.Initialize();

            _timer.Interval = TimeSpan.FromMilliseconds(Shared.TimerInterval);

            _timer.Start();

            _mainWindowState.ControlsState.Start();
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
                ResetControls();

                _mainWindowState.ControlsState.Stop();

                _shouldStop = true;
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

        private void ResetControls()
        {
            _mainWindowState.WindowsInfoState.WinInfos = null;

            _mainWindowState.WindowsInfoState.UpdateBindings();
        }
    }
}
