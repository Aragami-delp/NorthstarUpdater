// TODO: More error handling
// Idea: Steam auto find path (maybe origin too)

using System;
using System.IO.Compression;
using System.Net;
using System.Windows;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Path = System.IO.Path;

namespace NorthstarUpdater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string m_folderPath;
        private string m_newestVersion;

        /// <summary>
        /// Folder of Titanfall2.exe
        /// </summary>
        private string FolderPath
        {
            get => m_folderPath;
            set
            {
                m_folderPath = value;
                tBox_gameFolderPath.Text = value;
                //UpdaterSettings.Default.GameExePath = value;
            }
        }

        /// <summary>
        /// Newest Version of Northstar as string
        /// </summary>
        private string NewestVersion
        {
            get => m_newestVersion;
            set => lbl_newVer.Content = value;
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void bt_Update_Click(object sender, RoutedEventArgs e)
        {
            string linkToDownload = GetZipDownloadLink();
            string downloadedFile = DownloadLatestNorthstarRelease(linkToDownload);
            ExtractZipToFolder(FolderPath, downloadedFile);
        }

        private string GetNewestVersionString()
        {
            string json;
            using (WebClient wc = new WebClient())
            {
                wc.Headers.Add("User-Agent: Other");
                try
                {
                    json = wc.DownloadString("http://api.github.com/repos/R2Northstar/Northstar/releases/latest");
                }
                catch (System.Net.WebException _netExc)
                {
                    ShowErrorDialog("Error connecting to git");
                    json = "";
                }
            }

            if (String.IsNullOrWhiteSpace(json))
            {
                ShowErrorDialog("Json link not valid");
                return json;
            }

            JObject data = (JObject)JsonConvert.DeserializeObject(json);
            string retVal = data.GetValue("name").Value<string>();

            return retVal;
        }

        /// <summary>
        /// Find the download link for the newest version of northstar and returns its download link
        /// </summary>
        /// <returns>Download link (*.zip most likely)</returns>
        private string GetZipDownloadLink()
        {
            string json;
            using (WebClient wc = new WebClient())
            {
                wc.Headers.Add("User-Agent: Other");
                try
                {
                    json = wc.DownloadString("http://api.github.com/repos/R2Northstar/Northstar/releases/latest");
                }
                catch (System.Net.WebException _netExc)
                {
                    ShowErrorDialog("Error connecting to git");
                    json = "";
                }
            }

            if (String.IsNullOrWhiteSpace(json))
            {
                ShowErrorDialog("Json link not valid");
                return json;
            }

            JObject data = (JObject)JsonConvert.DeserializeObject(json);
            string retVal = data.GetValue("assets").First.Value<JObject>().GetValue("browser_download_url").Value<string>();

            return retVal;
        }

        /// <summary>
        /// Downloads a file from the given url
        /// </summary>
        /// <param name="_linkToDownload">Url to download</param>
        /// <returns>File path and name of the temp file</returns>
        private string DownloadLatestNorthstarRelease(string _linkToDownload)
        {
            string pathToFile = Path.GetTempFileName();
            using (WebClient wc = new WebClient())
            {
                wc.Headers.Add("User-Agent: Other");
                try
                {
                    wc.DownloadFile(_linkToDownload, pathToFile);
                }
                catch (System.Net.WebException _netExc)
                {
                    ShowErrorDialog("Error downloading file");
                }
            }

            if (String.IsNullOrWhiteSpace(pathToFile))
            {
                ShowErrorDialog("Download failed");
                return null;
            }

            return pathToFile;
        }

        /// <summary>
        /// Extracts the given zip file and extracts it into the given folder, overriding everything there
        /// </summary>
        /// <param name="_gameFolder">Folder of the game (same as Titanfall2.exe)</param>
        /// <param name="_filePath">File with path of the zip</param>
        private void ExtractZipToFolder(string _gameFolder, string _filePath)
        {
            ZipFile.ExtractToDirectory(_filePath, _gameFolder, true);
        }

        private void SaveUserSettings()
        {
            UpdaterSettings.Default.Save();
        }

        private void MainWindow_OnClosed(object? _sender, EventArgs _e)
        {
            SaveUserSettings();
        }

        private string ShowPathDialog()
        {
            OpenFileDialog dia = new OpenFileDialog();
            dia.Filter = "Executables (*.exe)|*.exe";
            dia.Title = "Select Game.exe";
            if (dia.ShowDialog() == true && Path.GetFileName(dia.FileName) == "Titanfall2.exe")
            {
                return Path.GetDirectoryName(dia.FileName);
            }

            return UpdaterSettings.Default.GameExePath != null ? UpdaterSettings.Default.GameExePath : "";
        }

        private void bt_selectFolder_Click(object sender, RoutedEventArgs e)
        {
            FolderPath = ShowPathDialog();
        }

        private void MainWindow_OnLoaded(object _sender, RoutedEventArgs _e)
        {
            FolderPath = UpdaterSettings.Default.GameExePath;
            NewestVersion = GetNewestVersionString();
        }

        public static void ShowErrorDialog(string _errorText)
        {
            MessageBox.Show(_errorText, "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
        }
    }
}
