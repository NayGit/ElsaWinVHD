using ElsaWinVHD.Commands;
using ElsaWinVHD.Enum;
using ElsaWinVHD.Model;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ElsaWinVHD.Domain;

#if !NET35
using Ookii.Dialogs.Wpf;
#else
using Microsoft.WindowsAPICodePack.Dialogs;
#endif

namespace ElsaWinVHD.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private readonly INet35Win7 net35Win7;

        private readonly string ElsaWin_Dir;
        private readonly string ElsaWin_DirMount;

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
            private set => SetProperty(ref infoCommand, value);
        }

        private bool isRun;
        public bool IsRun
        {
            get => isRun;
            private set => SetProperty(ref isRun, value);
        }


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
#if !NET35
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
#else
            Title += ": Net35";
            pathKey = @"SOFTWARE\Volkswagen AG\ElsaWin\";
#endif

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

            ElsaWin_DirMount = ElsaWin_Dir + @"\!ElsaWinVHD\";
            InfoMain.Add(ElsaWin_DirMount);

#if !NET35
            net35Win7 = new Win7(ElsaWin_Dir, ElsaWin_DirMount);
#else
            net35Win7 = new Net35(ElsaWin_Dir, ElsaWin_DirMount);
#endif

            if (!Directory.Exists(ElsaWin_DirMount))
            {
                InfoMain.Add("First start...");
                Directory.CreateDirectory(ElsaWin_DirMount);

                ElsaService(ServiceStatus.Stop).GetAwaiter().GetResult();
                foreach(var dir in dir)
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

            CommandCheckVHD_Multi = new RelayCommand(p => CheckVHD_Multi(), p => !IsRun);

            CommandCheckVHD_Single = new RelayCommand(p => CheckVHD_Single((int)p), p => !IsRun);
        }

        public ICommand CommandCheckVHD_Multi { get; }
        public ICommand CommandCheckVHD_Single { get; }

#region Start Command
        private ICommand _CommandStart;
        public ICommand CommandStart => _CommandStart ?? (_CommandStart =
                    new RelayCommandAsync<int>(StartAsync, p => CanStart(p)));

        private async Task StartAsync(int p)
        {
            try
            {
                IsRun = true;
                await ELSA(ElsaWinAll[p], MountStatus.Mount);
            }
            finally
            {
                IsRun = false;
            }
        }

        private bool CanStart(int p)
        {
            return !IsRun && File.Exists(ElsaWinAll[p].Path);
        }
#endregion

#region Service Command
        private ICommand _CommandService;
        public ICommand CommandService => _CommandService ?? (_CommandService =
                    new RelayCommandAsync<object>(ServiceAsync, p => !IsRun));

        private async Task ServiceAsync(object p)
        {
            try
            {
                IsRun = true;

                if (p is bool onOff)
                {
                    InfoCommand = "";
                    if (onOff)
                    {
                        await ElsaService(ServiceStatus.Start);
                    }
                    else
                    {
                        await ElsaService(ServiceStatus.Stop);
                    }
                }
            }
            finally
            {
                IsRun = false;
            }
        }
#endregion

#region ClearAll Command
        private ICommand _CommandClearAll;
        public ICommand CommandClearAll => _CommandClearAll ?? (_CommandClearAll =
                    new RelayCommandAsync(ClearAllAsync, p => !IsRun));

        private async Task ClearAllAsync()
        {
            try
            {
                IsRun = true;
                await ELSA(null, MountStatus.Unmount);
            }
            finally
            {
                IsRun = false;
            }
        }
#endregion

        private async Task ELSA(ElsaWin elsaWin, MountStatus mountStatus)
        {
            InfoCommand = "";
            string selectName = elsaWin is null ? "" : elsaWin.SelectName;

            if (mountStatus == MountStatus.Mount && File.Exists(ElsaWin_fileSelect))
            {
                if (selectName == File.ReadAllText(ElsaWin_fileSelect))
                {
                    if (File.Exists($@"{ElsaWin_DirMount}{selectName}\{selectName}"))
                    {
                        InfoCommand = $"Start: {selectName}";
                        Process.Start($@"{ElsaWin_Dir}\bin\ElsaWin.exe");
                        WindowClose();
                        return;
                    }
                }
                else
                {
                    await ElsaService(ServiceStatus.Stop);
                    await DiskPartVHD(null, MountStatus.Unmount);
                    await ElsaMklink(selectName, MountStatus.Unmount);
                }
            }
            else
                await ElsaService(ServiceStatus.Stop);

            int check = 0;
            while (5 == 5)
            {
                await DiskPartVHD(elsaWin, mountStatus);
                if (mountStatus == MountStatus.Unmount || File.Exists($@"{ElsaWin_DirMount}{selectName}\{selectName}"))
                {
                    break;
                }
                else if (Directory.Exists($@"{ElsaWin_DirMount}{selectName}\{dir[0]}"))
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
                    InfoMain.Add($"{check}) DiskPart - {selectName} - {mountStatus}");
                }
                check++;
            }
            await ElsaMklink(selectName, mountStatus);
            ElsaSelect(selectName, mountStatus);
            await ElsaService(ServiceStatus.Start);

            if (mountStatus == MountStatus.Mount)
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

        private async Task DiskPartVHD(ElsaWin elsaWin, MountStatus mountStatus)
        {
            if (mountStatus == MountStatus.Mount)
            {
                if (!Directory.Exists($"{ElsaWin_DirMount}{elsaWin.SelectName}"))
                {
                    Directory.CreateDirectory($"{ElsaWin_DirMount}{elsaWin.SelectName}");
                }

                InfoCommand += await net35Win7.DiskPartAttach(elsaWin.Path, elsaWin.SelectName);
            }
            else
            {
                foreach (var el in ElsaWinAll)
                {
                    if (string.IsNullOrEmpty(el.Path))
                        continue;

                    InfoCommand += await net35Win7.DiskPartDetach(el.Path, el.SelectName);
                }
            }
        }

        private async Task ElsaService(ServiceStatus serviceStatus)
        {
            if (serviceStatus == ServiceStatus.Start)
            {
                InfoCommand += await net35Win7.ServiceStart();
            }
            else
            {
                InfoCommand += await net35Win7.ServiceStop();
            }
        }

        private async Task ElsaMklink(string selectName, MountStatus mountStatus)
        {
            if (mountStatus == MountStatus.Mount)
            {
                InfoCommand += await net35Win7.MkLinkCreate(dir, selectName);
            }
            else
            {
                InfoCommand += await net35Win7.MkLinkDelete(dir);
            }
        }

        private void ElsaSelect(string selectName, MountStatus mountStatus)
        {
            if (mountStatus == MountStatus.Mount)
            {
                File.WriteAllText(ElsaWin_fileSelect, selectName);
            }
            else
            {
                File.WriteAllText(ElsaWin_fileSelect, "");
            }
        }

        

#region CheckVHD
        private void CheckVHD_Multi()
        {
            InfoCommand = "";
            string path = string.Empty;

#if !NET35
            var OokiiFolderDialog = new VistaFolderBrowserDialog();
            if (OokiiFolderDialog.ShowDialog().GetValueOrDefault())
            {
                path = OokiiFolderDialog.SelectedPath;
            }
#else
            var commonFolderDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true
            };

            if (commonFolderDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                path = commonFolderDialog.FileName;
            }
#endif

            if (!string.IsNullOrEmpty(path))
            {
                bool check = false;
                int i = 0;
                foreach (var el in ElsaWinAll)
                {
                    check = CheckVHD_Path(i++, $@"{path}\{el.SelectName}.vhd") || check;
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
