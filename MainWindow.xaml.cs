using Microsoft.VisualBasic.Devices;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NeteasyDecoder
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        private static string InfoAPI = @"https://api.imjad.cn/cloudmusic/?type=detail&id=";
        private static WebClient webClient = new WebClient();
        private static Computer MyComputer = null;

        public event PropertyChangedEventHandler PropertyChanged;

        private string _ProcessingInfos;
        public string ProcessingInfos { get { return _ProcessingInfos; } set { _ProcessingInfos = value; PropertyChanged(this, new PropertyChangedEventArgs("ProcessingInfos")); } }
        private string _SelectedInputPath;
        public string SelectedInputPath { get { return _SelectedInputPath; } set { _SelectedInputPath = value; PropertyChanged(this, new PropertyChangedEventArgs("SelectedInputPath")); } }
        private string _SelectedOutputPath;
        public string SelectedOutputPath { get { return _SelectedOutputPath; } set { _SelectedOutputPath = value; PropertyChanged(this, new PropertyChangedEventArgs("SelectedOutputPath")); } }
        private bool _AutoRename;
        public bool AutoRename { get { return _AutoRename; } set { _AutoRename = value; PropertyChanged(this, new PropertyChangedEventArgs("AutoRename")); } }

        public MainWindow()
        {
            InitializeComponent();
            webClient.Credentials = CredentialCache.DefaultCredentials;
            webClient.Encoding = Encoding.UTF8;

            this.DataContext = this;
        }
        public void ProcessRun()
        {
            if (Directory.Exists(SelectedInputPath))
            {
                if (!Directory.Exists(SelectedOutputPath))
                    Directory.CreateDirectory(SelectedOutputPath);

                foreach (var item in Directory.GetFiles(SelectedInputPath))
                {
                    DecodeFile(item, SelectedOutputPath);
                }
            }
        }
        public bool Rename(string filename)
        {
            var ID = System.IO.Path.GetFileNameWithoutExtension(filename).Split('-')[0];
            var JsonData = "";
            try
            {
                JsonData = webClient.DownloadString(InfoAPI + ID);
            }
            catch (Exception weber)
            {
                ProcessingInfos +=$"下载错误 :{weber.Message}\n";
            }
            if (string.IsNullOrWhiteSpace(JsonData)) return false;

            try
            {
                var Infos = JObject.Parse(JsonData);

                string SongName = Infos["songs"][0]["name"].ToString();
                string ArtName = "";
                foreach (var nitem in Infos["songs"][0]["ar"])
                {
                    if (!string.IsNullOrWhiteSpace(ArtName)) ArtName += "/";
                    ArtName += nitem["name"].ToString();
                }
                //string AlbumName = Infos["songs"][0]["al"]["name"].ToString();
                //string alia = "";
                //foreach (var aitem in Infos["songs"][0]["alia"])
                //{
                //    if (!string.IsNullOrWhiteSpace(alia)) alia += "/";
                //    alia += aitem.ToString();
                //}
                //var NewFileName = $"{SongName}-{ArtName}-{AlbumName}-{alia}".Replace('/', '&').Replace('\\', '&').Replace(':', '：').Replace('?', '？').Replace('<', '《').Replace('>', '》').Replace('*', '@').Replace('\"', '\'').Replace("|", "%7c");
                var NewFileName = $"{SongName}-{ArtName}".Replace('/', '&').Replace('\\', '&').Replace(':', '：').Replace('?', '？').Replace('<', '《').Replace('>', '》').Replace('*', '@').Replace('\"', '\'').Replace("|", "%7c");

                if (MyComputer == null) MyComputer = new Computer();
                var OriginalExtention = System.IO.Path.GetExtension(filename);
                
                if (File.Exists( NewFileName + OriginalExtention))
                    NewFileName += DateTime.Now.Ticks.ToString();
                try
                {
                    MyComputer.FileSystem.RenameFile(filename, NewFileName + OriginalExtention);
                }catch(Exception err)
                {
                    ProcessingInfos += $"重命名出错 :{err.Message}\n";
                }
                
                return true;
            }
            catch (Exception er)
            {
                ProcessingInfos += $"解析错误 {ID} : {er.Message}\n";
            }
            return false;
        }
        public bool DecodeFile(string filepath, string outputpath)
        {
            if (!filepath.EndsWith("uc!"))
            {
                ProcessingInfos += $"{filepath}并不是网易云音乐缓存文件，已跳过\n";
                return false;
            }
            if (File.Exists(filepath))
            {
                ProcessingInfos += $"{filepath}开始解码\n";
                FileStream FS = new FileStream(filepath, FileMode.Open, FileAccess.Read);
                BinaryReader SR = new BinaryReader(FS);
                var Data = SR.ReadBytes((int)FS.Length);
                var Length = FS.Length;
                SR.Close();
                FS.Close();
                string FileName = System.IO.Path.GetFileNameWithoutExtension(filepath);
                string FilePathName = System.IO.Path.Combine(outputpath, FileName);
                for (int i = 0; i < Length; i++)
                {
                    Data[i] = (byte)(Data[i] ^ 0xa3);
                }
                FileStream OFS = new FileStream(FilePathName, FileMode.OpenOrCreate, FileAccess.Write);
                BinaryWriter BW = new BinaryWriter(OFS);
                BW.Write(Data);
                BW.Close();
                OFS.Close();
                ProcessingInfos += $"{filepath}解码完毕\n";
                if (AutoRename)
                    Rename(FilePathName);
                return true;

            }
            return false;
        }

        public string OpenFloder()
        {
            CommonOpenFileDialog ops = new CommonOpenFileDialog();
            ops.IsFolderPicker = true;
            ops.Multiselect = false;

            if(ops.ShowDialog() == CommonFileDialogResult.Ok)
            {
                return ops.FileName;
            }
            return "";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SelectedOutputPath =OpenFloder();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            SelectedInputPath  = OpenFloder();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            ProcessingInfos = "";
            ProcessRun();
        }
    }
}
