using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using InfoHelper.Utils;
using JetBrains.Annotations;
using Color = System.Windows.Media.Color;
using Image = System.Windows.Controls.Image;
using MessageBox = System.Windows.MessageBox;
using Path = System.Windows.Shapes.Path;
using Point = System.Windows.Point;

namespace InfoHelper.Windows
{
    /// <summary>
    /// Interaction logic for OptionsWindow.xaml
    /// </summary>
    public partial class OptionsWindow : Window
    {
        private readonly OptionsViewModel _viewModel;

        public OptionsWindow()
        {
            InitializeComponent();

            _viewModel = new OptionsViewModel();
        }

        private void LoadData()
        {
            string cursorsFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\Cursors");

            string[] cursorFiles = new string[] {};

            if (Directory.Exists(cursorsFolder))
                cursorFiles = Directory.GetFiles(cursorsFolder, "cursor*.png").Select(System.IO.Path.GetFileName).ToArray();

            _viewModel.ClientMouseCursorTemplates = cursorFiles;

            string optionsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\DeferredLoadedData\\Settings.xml");

            if (File.Exists(optionsPath))
            {
                XmlDocument xmlDoc = new XmlDocument();

                xmlDoc.Load(optionsPath);

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

                                    _viewModel.SaveFolder = node.Attributes["Value"].Value;

                                    break;

                                case "SnapshotsFolder":

                                    _viewModel.SnapshotsFolder = node.Attributes["Value"].Value;

                                    break;

                                case "TimerInterval":

                                    _viewModel.TimerInterval = node.Attributes["Value"].Value;

                                    break;

                                case "BitmapsBuffer":

                                    _viewModel.BitmapsBuffer = node.Attributes["Value"].Value;

                                    break;

                                case "GetLastNHands":

                                    _viewModel.GetLastNHands = node.Attributes["Value"].Value;

                                    break;

                                case "ScreenshotSaveInterval":

                                    _viewModel.ScreenshotSaveInterval = node.Attributes["Value"].Value;

                                    break;

                                case "WindowExpireTimeout":

                                    _viewModel.WindowExpireTimeout = node.Attributes["Value"].Value;

                                    break;

                                case "MouseCursor":

                                    string selectedCursor = node.Attributes["Value"].Value;

                                    _viewModel.SelectedClientMouseCursorTemplate = cursorFiles.FirstOrDefault(cf => cf == selectedCursor);

                                    break;

                                case "FormBackColor":

                                    string formBackColorString = node.Attributes["Value"].Value;

                                    try
                                    {
                                        Color convertedColor = (Color)System.Windows.Media.ColorConverter.ConvertFromString(formBackColorString);

                                        _viewModel.BackColor = new SolidColorBrush(convertedColor);
                                    }
                                    catch
                                    {
                                        // ignored
                                    }

                                    break;

                                #endregion

                                #region Server

                                case "ServerName":

                                    _viewModel.ServerName = node.Attributes["Value"].Value;

                                    break;

                                case "DBName":

                                    _viewModel.DbName = node.Attributes["Value"].Value;

                                    break;

                                #endregion

                                #region Solver

                                case "IsLocalServer":

                                    if(bool.TryParse(node.Attributes["Value"].Value, out bool isLocalServer))
                                        _viewModel.IsLocalServer = isLocalServer;

                                    _viewModel.IsRemoteServer = !_viewModel.IsLocalServer;

                                    break;

                                case "SolverFolder":

                                    _viewModel.SolverFolder = node.Attributes["Value"].Value;

                                    break;

                                case "SolverFile":

                                    _viewModel.SolverFile = node.Attributes["Value"].Value;

                                    break;

                                case "SolverPort":

                                    _viewModel.SolverPort = node.Attributes["Value"].Value;

                                    break;

                                case "SolverIp":

                                    _viewModel.SolverIp = node.Attributes["Value"].Value;

                                    break;

                                case "TurnSolverDuration":

                                    _viewModel.TurnSolverDuration = node.Attributes["Value"].Value;

                                    break;

                                case "RiverSolverDuration":

                                    _viewModel.RiverSolverDuration = node.Attributes["Value"].Value;

                                    break;

                                case "TurnSolverAccuracy":

                                    _viewModel.TurnSolverAccuracy = node.Attributes["Value"].Value;

                                    break;

                                case "RiverSolverAccuracy":

                                    _viewModel.RiverSolverAccuracy = node.Attributes["Value"].Value;

                                    break;

                                case "MinLineFrequency":

                                    _viewModel.MinLineFrequency = node.Attributes["Value"].Value;

                                    break;

                                #endregion

                                #region Rooms

                                #region GGPoker

                                case "ScanTables":

                                    if (bool.TryParse(node.Attributes["Value"].Value, out bool scanTables))
                                        _viewModel.ScanTables = scanTables;

                                    break;

                                case "CardBackIndex":

                                    _viewModel.CardBackIndex = node.Attributes["Value"].Value;

                                    break;

                                case "DeckIndex":

                                    _viewModel.DeckIndex = node.Attributes["Value"].Value;

                                    break;

                                case "PlayersImagesFolder":

                                    _viewModel.PlayersImagesFolder = node.Attributes["Value"].Value;

                                    break;

                                case "SavePicturesPerHand":

                                    if (bool.TryParse(node.Attributes["Value"].Value, out bool savePictures))
                                        _viewModel.SavePictures = savePictures;

                                    break;

                                case "PicturesSaveFolder":

                                    _viewModel.SavePicturesFolder = node.Attributes["Value"].Value;

                                    break;

                                #endregion

                                #endregion
                            }
                        }
                    }
                }
            }

        }

        private void SaveData()
        {
            FileStream fileStream = new FileStream(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\DeferredLoadedData\\Settings.xml"), FileMode.Create); ;

            XmlWriter xmlWriter = XmlWriter.Create(fileStream);

            xmlWriter.WriteStartElement("Settings");

            xmlWriter.WriteStartElement("ServerName");
            xmlWriter.WriteAttributeString("Value", _viewModel.ServerName);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("DBName");
            xmlWriter.WriteAttributeString("Value", _viewModel.DbName);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("SaveFolder");
            xmlWriter.WriteAttributeString("Value", _viewModel.SaveFolder);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("SnapshotsFolder");
            xmlWriter.WriteAttributeString("Value", _viewModel.SnapshotsFolder);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("TimerInterval");
            xmlWriter.WriteAttributeString("Value", _viewModel.TimerInterval);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("BitmapsBuffer");
            xmlWriter.WriteAttributeString("Value", _viewModel.BitmapsBuffer);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("GetLastNHands");
            xmlWriter.WriteAttributeString("Value", _viewModel.GetLastNHands);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("ScreenshotSaveInterval");
            xmlWriter.WriteAttributeString("Value", _viewModel.ScreenshotSaveInterval);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("IsLocalServer");
            xmlWriter.WriteAttributeString("Value", _viewModel.IsLocalServer.ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("SolverFolder");
            xmlWriter.WriteAttributeString("Value", _viewModel.SolverFolder);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("SolverFile");
            xmlWriter.WriteAttributeString("Value", _viewModel.SolverFile);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("SolverPort");
            xmlWriter.WriteAttributeString("Value", _viewModel.SolverPort);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("SolverIp");
            xmlWriter.WriteAttributeString("Value", _viewModel.SolverIp);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("WindowExpireTimeout");
            xmlWriter.WriteAttributeString("Value", _viewModel.WindowExpireTimeout);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("MouseCursor");
            xmlWriter.WriteAttributeString("Value", _viewModel.SelectedClientMouseCursorTemplate);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("TurnSolverDuration");
            xmlWriter.WriteAttributeString("Value", _viewModel.TurnSolverDuration);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("RiverSolverDuration");
            xmlWriter.WriteAttributeString("Value", _viewModel.RiverSolverDuration);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("TurnSolverAccuracy");
            xmlWriter.WriteAttributeString("Value", _viewModel.TurnSolverAccuracy);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("RiverSolverAccuracy");
            xmlWriter.WriteAttributeString("Value", _viewModel.RiverSolverAccuracy);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("MinLineFrequency");
            xmlWriter.WriteAttributeString("Value", _viewModel.MinLineFrequency);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("FormBackColor");
            xmlWriter.WriteAttributeString("Value", _viewModel.BackColor.ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("ScanTables");
            xmlWriter.WriteAttributeString("Value", _viewModel.ScanTables.ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("CardBackIndex");
            xmlWriter.WriteAttributeString("Value", _viewModel.CardBackIndex);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("DeckIndex");
            xmlWriter.WriteAttributeString("Value", _viewModel.DeckIndex);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("PlayersImagesFolder");
            xmlWriter.WriteAttributeString("Value", _viewModel.PlayersImagesFolder);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("SavePicturesPerHand");
            xmlWriter.WriteAttributeString("Value", _viewModel.SavePictures.ToString());
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("PicturesSaveFolder");
            xmlWriter.WriteAttributeString("Value", _viewModel.SavePicturesFolder);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteEndElement();

            xmlWriter.Flush();

            xmlWriter.Close();

            fileStream.Close();
        }

        #region Event handlers

        private void Button_Ok_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveData();

                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Achtung!", MessageBoxButton.OK);

                DialogResult = false;
            }
            finally
            {
                Close();
            }
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;

            Close();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderSave = new FolderBrowserDialog();

            DialogResult result = folderSave.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
                _viewModel.SaveFolder = folderSave.SelectedPath;
        }

        private void btnSnapshots_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderSave = new FolderBrowserDialog();

            DialogResult result = folderSave.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
                _viewModel.SnapshotsFolder = folderSave.SelectedPath;
        }

        private void btnDefaultColor_Click(object sender, EventArgs e)
        {
            _viewModel.BackColor = (SolidColorBrush)System.Windows.Application.Current.TryFindResource("DefaultFormBackColor");
        }

        private void btnSolverFolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderSolver = new FolderBrowserDialog();

            DialogResult result = folderSolver.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
                _viewModel.SolverFolder = folderSolver.SelectedPath;
        }

        private void btnSolverFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog solverFileDialog = new OpenFileDialog();

            DialogResult result = solverFileDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
                _viewModel.SolverFile = System.IO.Path.GetFileName(solverFileDialog.FileName);
        }

        private void btnPlayersImagesFolder_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderPlayersImages = new FolderBrowserDialog();

            DialogResult result = folderPlayersImages.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
                _viewModel.PlayersImagesFolder = folderPlayersImages.SelectedPath;
        }

        private void btnPicturesSaveFolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderSavePictures = new FolderBrowserDialog();

            DialogResult result = folderSavePictures.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
                _viewModel.SavePicturesFolder = folderSavePictures.SelectedPath;
        }
        
        private void UIElement_OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog();

            Color color = _viewModel.BackColor.Color;

            colorDialog.Color = color.ToDrawingColor();

            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                _viewModel.BackColor = new SolidColorBrush(colorDialog.Color.ToMediaColor());
        }

        private void OptionsWindow_SourceInitialized(object sender, EventArgs e)
        {
            try
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;

                WinAPI.SetWindowLong(hwnd, WinAPI.GWL_STYLE, WinAPI.GetWindowLong(hwnd, WinAPI.GWL_STYLE) & ~WinAPI.WS_SYSMENU);

                LoadData();

                DataContext = _viewModel;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Achtung!", MessageBoxButton.OK);

                DialogResult = false;

                Close();
            }
        }

        #endregion

        private class OptionsViewModel : INotifyPropertyChanged
        {
            private string _saveFolder;
            public string SaveFolder
            {
                get => _saveFolder;
                set
                {
                    _saveFolder = value;

                    OnPropertyChanged();
                }
            }

            private string _snapshotsFolder;
            public string SnapshotsFolder
            {
                get => _snapshotsFolder;
                set
                {
                    _snapshotsFolder = value;

                    OnPropertyChanged();
                }
            }

            public string BitmapsBuffer { get; set; }

            public string GetLastNHands { get; set; }

            public string TimerInterval { get; set; }

            public string ScreenshotSaveInterval { get; set; }

            public string WindowExpireTimeout { get; set; }

            public string[] ClientMouseCursorTemplates { get; set; }

            public string SelectedClientMouseCursorTemplate { get; set; }

            private SolidColorBrush _backColor = (SolidColorBrush)System.Windows.Application.Current.TryFindResource("DefaultFormBackColor");
            public SolidColorBrush BackColor
            {
                get => _backColor;
                set
                {
                    _backColor = value;

                    OnPropertyChanged();
                }
            }

            public string ServerName { get; set; }

            public string DbName { get; set; }

            public bool IsLocalServer { get; set; } = true;

            public bool IsRemoteServer { get; set; }

            private string _solverFolder;
            public string SolverFolder
            {
                get => _solverFolder;
                set
                {
                    _solverFolder = value;

                    OnPropertyChanged();
                }
            }

            private string _solverFile;
            public string SolverFile
            {
                get => _solverFile;
                set
                {
                    _solverFile = value;

                    OnPropertyChanged();
                }
            }

            public string SolverPort { get; set; }

            public string SolverIp { get; set; }

            public string TurnSolverDuration { get; set; }

            public string RiverSolverDuration { get; set; }

            public string TurnSolverAccuracy { get; set; }

            public string RiverSolverAccuracy { get; set; }

            public string MinLineFrequency { get; set; }

            public bool ScanTables { get; set; } = true;

            private string _cardBackIndex;
            public string CardBackIndex
            {
                get => _cardBackIndex;
                set
                {
                    if (value == null)
                        return;

                    _cardBackIndex = value;
                }
            }

            private string _deckIndex;
            public string DeckIndex
            {
                get => _deckIndex;
                set
                {
                    if (value == null)
                        return;

                    _deckIndex = value;
                }
            }

            public bool SavePictures { get; set; } = true;

            private string _savePicturesFolder;
            public string SavePicturesFolder
            {
                get => _savePicturesFolder;
                set
                {
                    _savePicturesFolder = value;

                    OnPropertyChanged();
                }
            }

            private string _playersImagesFolder;
            public string PlayersImagesFolder
            {
                get => _playersImagesFolder;
                set
                {
                    _playersImagesFolder = value;

                    OnPropertyChanged();
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            [NotifyPropertyChangedInvocator]
            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
