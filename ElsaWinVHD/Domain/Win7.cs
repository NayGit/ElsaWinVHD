using Monitor.Core.Utilities;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Globalization;
using ElsaWinVHD.Domain.pInvoke;
using System.Collections.Specialized;

#if !NET35
using System.ServiceProcess;
#endif

namespace ElsaWinVHD.Domain
{
    public class Win7 : INet35Win7
    {
        public string ElsaWin_Dir { get; private set; }

        public string ElsaWin_DirMount { get; private set; }

        public Win7(string elsaWin_Dir, string elsaWin_dirMount)
        {
            ElsaWin_Dir = elsaWin_Dir;
            ElsaWin_DirMount = elsaWin_dirMount;

            volume = new Volume();
        }

        private const int cLen = 20;

        private Volume volume;
        private Medo.IO.VirtualDisk disk;

        public async Task<string> DiskPartAttach(string path, string selectName)
        {
#if !NET35
            await Task.Run(() => {
                disk = new Medo.IO.VirtualDisk(path);
                disk.Open();

                disk.Attach(Medo.IO.VirtualDiskAttachOptions.None | Medo.IO.VirtualDiskAttachOptions.PermanentLifetime);
                var physicalDrive = disk.GetAttachedPath();


                int driveNumber; // \\\\.\\PhysicalDrive2 --> 2
                if ((physicalDrive != null) && physicalDrive.StartsWith(@"\\.\PHYSICALDRIVE", StringComparison.InvariantCultureIgnoreCase) && int.TryParse(physicalDrive.Substring(17), NumberStyles.Integer, CultureInfo.InvariantCulture, out driveNumber))
                {
                    StringCollection v = volume.GetVolumes();
                    foreach(var v1 in v)
                    {
                        int diskNumber;
                        long startingOffset;
                        long extentLength;
                        if (volume.GetExtentInfo(v1.TrimEnd('\\'), out diskNumber, out startingOffset, out extentLength))
                        {
                            if(driveNumber == diskNumber)
                            {
                                volume.ChangeDriveLetter(v1, $@"{ElsaWin_DirMount}{selectName}\");
                                break;
                            }
                        }
                    }
                }

                disk.Close();
                disk = null;
            });

            return "DiskPart Attach: ".PadRight(cLen) + $@"{path} --> {ElsaWin_DirMount}{selectName}\" + Environment.NewLine;
#else
            return string.Empty;
#endif


        }

        public async Task<string> DiskPartDetach(string path, string selectName)
        {
#if !NET35
            await Task.Run(() => {
                disk = new Medo.IO.VirtualDisk(path);
                disk.Open();

                disk.Detach();

                disk.Close();
                disk = null;
            });

            return "DiskPart Detach: ".PadRight(cLen) + $"{path}" + Environment.NewLine;
#else
            return string.Empty;
#endif
        }

        public async Task<string> MkLinkCreate(string[] dir, string selectName)
        {
#if !NET35
            await Task.Run(() => {
                foreach (var d in dir)
                {
                    JunctionPoint.Create($@"{ ElsaWin_Dir}\{d}", $@"{ElsaWin_DirMount}{selectName}\{d}", true /*don't overwrite*/);
                }
            });

            return "MkLink Create: ".PadRight(cLen) + $@"{ElsaWin_Dir}\ {StringToReturn(dir)}" + Environment.NewLine;
#else
            return string.Empty;
#endif
        }

        public async Task<string> MkLinkDelete(string[] dir)
        {
#if !NET35
            await Task.Run(() => {
                foreach (var d in dir)
                {
                    DirectoryInfo di = new DirectoryInfo($@"{ ElsaWin_Dir}\{d}");
                    if (di.Exists)
                    {
                        di.Delete();
                    }
                }
            });

            return "MkLink Delete: ".PadRight(cLen) + $@"{ElsaWin_Dir}\ {StringToReturn(dir)}" + Environment.NewLine;
#else
            return string.Empty;
#endif
        }

        public async Task<string> ServiceStart()
        {
#if !NET35
            string[] s = { "LcSvrAuf", "LcSvrAdm", "LcSvrHis", "LcSvrDba", "LcSvrPas", "LcSvrSaz" };

            await Task.Run(() => {

                foreach (string n in s)
                {
                    using (var serviceController = new ServiceController(n))
                    {
                        if (serviceController.Status != ServiceControllerStatus.Running)
                        {
                            serviceController.Start();
                            serviceController.WaitForStatus(ServiceControllerStatus.Running);
                        }
                    }
                }
            });

            return "Service Start: ".PadRight(cLen) + $"{StringToReturn(s)}" + Environment.NewLine;
#else
            return string.Empty;
#endif
        }

        public async Task<string> ServiceStop()
        {
#if !NET35
            string[] s = { "LcSvrAuf", "LcSvrAdm", "LcSvrHis", "LcSvrDba", "LcSvrPas", "LcSvrSaz" };

            await Task.Run(() => {
                foreach (string n in s)
                {
                    using (var serviceController = new ServiceController(n))
                    {
                        if (serviceController.Status != ServiceControllerStatus.Stopped)
                        {
                            serviceController.Stop();
                            serviceController.WaitForStatus(ServiceControllerStatus.Stopped);
                        }
                    }
                }
            });

            return "Service Stop: ".PadRight(cLen) + $"{StringToReturn(s)}" + Environment.NewLine;
#else
            return string.Empty;
#endif
        }

        private string StringToReturn(string[] s)
        {
            string r = string.Empty;
            foreach (var s1 in s)
                r += Environment.NewLine + "   " + s1;
            return r;
        }
    }
}
