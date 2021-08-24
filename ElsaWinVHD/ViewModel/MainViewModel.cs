using ElsaWinVHD.Domain;
using ElsaWinVHD.Model;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace ElsaWinVHD.ViewModel
{
    public class MainViewModel : BaseViewModel
    {
        private readonly string ElsaWin_Dir;
        private readonly string ElsaWin_dirMount;

        private readonly string ElsaWin_fileSelect;
        private readonly string ElsaWin_filePathVHD;

        private readonly string[] dir = { "data", "docs", "graphics", "html", "log", "version" };

        public bool IsEnabledMain { get; }

        public bool IsCheck { get; set; }

        public ObservableCollection<ElsaWin> ElsaWinAll { get; private set; }
        public ObservableCollection<string> InfoMain { get; private set; }

        public string Title { get; private set; }

        private string infoCommand;
        public string InfoCommand
        {
            get => infoCommand;
            private set { if (infoCommand != value) { infoCommand = value; NotifyPropertyChanged("InfoCommand"); } }
        }


        public ICommand CommandCheckVHD_Multi { get; }
        public ICommand CommandCheckVHD_Single { get; }
        public ICommand CommandStart { get; }
        public ICommand CommandClearAll { get; }
        public ICommand CommandService { get; }

        public MainViewModel()
        {
            ElsaWinAll = new ObservableCollection<ElsaWin>
            {
                new ElsaWin(0, "Audi 40", "ElsaWinVHD_Audi_40", @"/Resources/ImgAudi.png"),
                new ElsaWin(1, "Audi 41", "ElsaWinVHD_Audi_41", @"/Resources/ImgAudi.png"),
                new ElsaWin(2, "Seat 40", "ElsaWinVHD_Seat_40", @"/Resources/ImgSeat.png"),
                new ElsaWin(3, "Seat 41", "ElsaWinVHD_Seat_41", @"/Resources/ImgSeat.png"),
                new ElsaWin(4, "VW 40", "ElsaWinVHD_VW_40", @"/Resources/ImgVW.png"),
                new ElsaWin(5, "VW 41", "ElsaWinVHD_VW_41", @"/Resources/ImgVW.png"),
                new ElsaWin(6, "Skoda 40", "ElsaWinVHD_Skoda_40", @"/Resources/ImgSkoda.png"),
            };

            Title = "ElsaWinVHD";
            InfoMain = new ObservableCollection<string>();

            string pathKey;
            if (Environment.Is64BitOperatingSystem)
            {
                Title += ": 64Bit";
                pathKey = @"SOFTWARE\WOW6432Node\Volkswagen AG\ElsaWin\";
            }
            else
            {
                Title += ": 32Bit";
                pathKey = @"SOFTWARE\Volkswagen AG\ElsaWin\";
            }

            RegistryKey ElsaWinDirKey = Registry.LocalMachine.OpenSubKey(pathKey + @"Directories");
            if (ElsaWinDirKey == null)
            {
                InfoMain.Add("Install ElsaWin -_-'");
                IsEnabledMain = false;
                IsCheck = true;
                return;
            }

            ElsaWin_Dir = (string)ElsaWinDirKey.GetValue("ProductDir");
            ElsaWinDirKey.Close();
            InfoMain.Add(ElsaWin_Dir);

            ElsaWin_dirMount = ElsaWin_Dir + @"\!ElsaWinVHD\";
            InfoMain.Add(ElsaWin_dirMount);

            if (!Directory.Exists(ElsaWin_dirMount))
            {
                InfoMain.Add("First start...");
                Directory.CreateDirectory(ElsaWin_dirMount);

                ElsaService(false);
                foreach(var tmp in dir)
                    FirstStartMoveDir(tmp);
            }

            ElsaWin_fileSelect = ElsaWin_Dir + @"\selectVHD";
            InfoMain.Add(ElsaWin_fileSelect);

            ElsaWin_filePathVHD = ElsaWin_Dir + @"\pathVHD";
            InfoMain.Add(ElsaWin_filePathVHD);

            if (File.Exists(ElsaWin_filePathVHD))
            {
                var path = File.ReadAllLines(ElsaWin_filePathVHD);
                if(path.Length == ElsaWinAll.Count)
                {
                    for(int i =0; i < ElsaWinAll.Count; i++)
                    {
                        ElsaWinAll[i].Path = path[i];
                    }
                }
            }

            IsEnabledMain = true;

            CommandCheckVHD_Multi = new RelayCommand(p => CheckVHD_Multi());

            CommandCheckVHD_Single = new RelayCommand(p => CheckVHD_Single((int)p));
            
            CommandStart = new RelayCommand(p => ELSA(ElsaWinAll[(int)p], true),
                                            p => p is int Id && File.Exists(ElsaWinAll[Id].Path));

            CommandClearAll = new RelayCommand(p => ClearAll());

            CommandService = new RelayCommand(p => Service(p));
        }

        private void FirstStartMoveDir(string dir)
        {
            try
            {
                Directory.Move(ElsaWin_Dir + $@"\{dir}", $@"{ElsaWin_Dir}\.{dir}_orig");
            }
            catch (DirectoryNotFoundException dirEx)
            {
                InfoMain.Add("Directory not found: " + dirEx.Message);
            }
        }

        private void ClearAll()
        {
            ELSA(null, false);
        }

        private void Service(object p)
        {
            if(p is bool onOff)
            {
                InfoCommand = "";
                ElsaService(onOff);
            }
        }

        private void ELSA(ElsaWin elsaWin, bool mount)
        {
            InfoCommand = "";
            string selectName = elsaWin is null ? "" : elsaWin.SelectName;

            if (mount && File.Exists(ElsaWin_fileSelect))
            {
                if (selectName == File.ReadAllText(ElsaWin_fileSelect))
                {
                    if (File.Exists($@"{ElsaWin_dirMount}{selectName}\{selectName}"))
                    {
                        InfoCommand = $"Start: {selectName}";
                        Process.Start($@"{ElsaWin_Dir}\bin\ElsaWin.exe");
                        WindowClose();
                        return;
                    }
                }
                else
                {
                    ElsaService(false);
                    DiskPartVHD(null, false);
                    ElsaMklink(selectName, false);
                }
            }
            else
                ElsaService(false);

            int check = 0;
            while (5 == 5)
            {
                DiskPartVHD(elsaWin, mount);
                if (mount == false || File.Exists($@"{ElsaWin_dirMount}{selectName}\{selectName}"))
                {
                    break;
                }
                else if (Directory.Exists($@"{ElsaWin_dirMount}{selectName}\{dir[0]}"))
                {
                    MessageBox.Show("diskpart.exe - Connect. Check: *.vhd???", "Error!!!", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                else
                {
                    if (check == 5)
                    {
                        MessageBox.Show("diskpart.exe - Reboot??? Check: *.vhd???", "Error!!!", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    InfoMain.Add($"{check}) DiskPart - {selectName} - {mount}");
                }
                check++;
            }
            ElsaMklink(selectName, mount);
            ElsaSelect(selectName, mount);
            ElsaService(true);

            if (mount)
            {
                Process.Start($@"{ElsaWin_Dir}\bin\ElsaWin.exe");
                WindowClose();
            }
            else
            {
                foreach (var el in ElsaWinAll)
                    el.Path = "";

                SavePath(true);
            }
        }

        private void DiskPartVHD(ElsaWin elsaWin, bool _mount)
        {
            string[] WriteLine;

            if (_mount)
            {
                if (!Directory.Exists($"{ElsaWin_dirMount}{elsaWin.SelectName}"))
                {
                    Directory.CreateDirectory($"{ElsaWin_dirMount}{elsaWin.SelectName}");
                }

                WriteLine = new string[]
                {
                    $"SELECT VDISK FILE='{elsaWin.Path}'",
                    "attach vdisk",
                    "select part 1",
                    $@"assign mount='{ElsaWin_dirMount}{elsaWin.SelectName}\'"
                };
                AllProcess("diskpart.exe", WriteLine);
            }
            else
            {
                foreach (var el in ElsaWinAll)
                {
                    if (string.IsNullOrEmpty(el.Path))
                        continue;

                    string tmpSingle = $"SELECT VDISK FILE='{el.Path}',";
                    tmpSingle += "select part 1,";
                    tmpSingle += $@"remove mount='{ElsaWin_dirMount}{el.SelectName}\',";    ///?????????? \',
                    tmpSingle += "DETACH VDISK";
                    AllProcess("diskpart.exe", tmpSingle.Split(','));
                }
            }
        }

        private void ElsaService(bool _service)
        {
            string[] WriteLine;

            if (_service)
            {
                WriteLine = new string[]
                {
                    "(call sc start LcSvrAuf",
                    "call sc start LcSvrAdm",
                    "call sc start LcSvrHis",
                    "call sc start LcSvrDba",
                    "call sc start LcSvrPas",
                    "call sc start LcSvrSaz)"
                };
            }
            else
            {
                WriteLine = new string[]
                {
                    "(call sc stop LcSvrAuf",
                    "call sc stop LcSvrAdm",
                    "call sc stop LcSvrHis",
                    "call sc stop LcSvrDba",
                    "call sc stop LcSvrPas",
                    "call sc stop LcSvrSaz)"
                };
            }

            AllProcess("cmd.exe", WriteLine);

            Thread.Sleep(2500);
        }

        private void ElsaMklink(string selectName, bool _mklink)
        {
            string[] WriteLine = new string[dir.Length];

            if (_mklink)
            {
                for (int i = 0; i < dir.Length; i++)
                    WriteLine[i] = $@"mklink / j ""{ElsaWin_Dir}\{dir[i]}"" ""{ElsaWin_dirMount}{selectName}\{dir[i]}\""";
            }
            else
            {
                for (int i = 0; i < dir.Length; i++)
                    WriteLine[i] = $@"RD ""{ElsaWin_Dir}\{dir[i]}""";
            }

            AllProcess("cmd.exe", WriteLine);
        }

        private void ElsaSelect(string selectName, bool _select)
        {
            if (_select)
            {
                File.WriteAllText(ElsaWin_fileSelect, selectName);
            }
            else
            {
                File.WriteAllText(ElsaWin_fileSelect, "");
            }
        }

        private void AllProcess(string _Filename, string[] _WriteLine)
        {
            try
            {
                Process p = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _Filename,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardInput = true,
                        WorkingDirectory = ElsaWin_Dir
                    }
                };
                p.Start();

                foreach (string __WriteLine in _WriteLine)
                {
                    p.StandardInput.WriteLine(__WriteLine);
                }

                p.StandardInput.WriteLine("exit");
                string pOutput = p.StandardOutput.ReadToEnd();
                Encoding pOutEncoding = p.StandardOutput.CurrentEncoding;
                p.WaitForExit();

                InfoCommand += EncToUTF8(pOutput, pOutEncoding);
            }
            catch (Win32Exception e)
            {
                MessageBox.Show(e.Message, "Administrator???", MessageBoxButton.OK, MessageBoxImage.Error);
                WindowClose();
            }
        }

        private static string EncToUTF8(string source, Encoding encoding)
        {
            try
            {
                if (encoding.CodePage == Encoding.GetEncoding(1251).CodePage)
                {
                    byte[] bStr = encoding.GetBytes(source);
                    return Encoding.GetEncoding(866).GetString(bStr);
                }
            }
            catch (NotSupportedException)
            {
            }
            
            return source;
        }

#region CheckVHD
        private void CheckVHD_Multi()
        {
            InfoCommand = "";

            var folderDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true
            };

            if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                bool check = false;
                int i = 0;
                foreach (var el in ElsaWinAll)
                {
                    check = CheckVHD_Path(i++, $@"{folderDialog.FileName}\{el.SelectName}.vhd") || check;
                }

                SavePath(check);
            }
        }

        private void CheckVHD_Single(int id)
        {
            InfoCommand = "";
            var openFileDialog = new OpenFileDialog
            {
                FileName = ElsaWinAll[id].SelectName,
                Filter = "VHD files (*.vhd)|*.vhd"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                var s = CheckVHD_Path(id, openFileDialog.FileName);

                SavePath(s);
            }
        }

        private bool CheckVHD_Path(int id, string pathNew)
        {
            if (File.Exists(pathNew))
            {
                ElsaWinAll[id].Path = pathNew;
                InfoCommand += $"{Environment.NewLine}{ElsaWinAll[id].Path} - OK{Environment.NewLine}";

                return true;
            }

            return false;
        }
        private void SavePath(bool check)
        {
            if (check && ElsaWin_filePathVHD != null)
            {
                string[] tmpPath = new string[ElsaWinAll.Count];
                for (int i = 0; i < ElsaWinAll.Count; i++)
                {
                    tmpPath[i] = ElsaWinAll[i].Path;
                }

                File.WriteAllLines(ElsaWin_filePathVHD, tmpPath);
            }
        }
        #endregion

        private void WindowClose()
        {
            if (!IsCheck)
            {
                foreach (Window item in Application.Current.Windows)
                {
                    if (item.DataContext == this) item.Close();
                }
            }
        }
    }
}
