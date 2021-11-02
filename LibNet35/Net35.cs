using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace LibNet35
{
    public class Net35 : INet35Win7
    {
        public string ElsaWin_Dir { get; private set; }

        public string ElsaWin_DirMount { get; private set; }

        public Net35(string elsaWin_Dir, string elsaWin_dirMount)
        {
            ElsaWin_Dir = elsaWin_Dir;
            ElsaWin_DirMount = elsaWin_dirMount;
        }

        public async Task<string> DiskPartAttach(string path, string selectName)
        {
            string[] WriteLine = new string[]
            {
                $"SELECT VDISK FILE='{path}'",
                "attach vdisk",
                "select part 1",
                $@"assign mount='{ElsaWin_DirMount}{selectName}\'"
            };
            await AllProcess("diskpart.exe", WriteLine);

            return string.Empty;
        }

        public async Task<string> DiskPartDetach(string path, string selectName)
        {
            string tmpSingle = $"SELECT VDISK FILE='{path}',";
            tmpSingle += "select part 1,";
            tmpSingle += $@"remove mount='{ElsaWin_DirMount}{selectName}\',";    ///?????????? \',
            tmpSingle += "DETACH VDISK";
            await AllProcess("diskpart.exe", tmpSingle.Split(','));

            return string.Empty;
        }

        public async Task<string> MkLinkCreate(string[] dir, string selectName)
        {
            string[] WriteLine = new string[dir.Length];

            for (int i = 0; i < dir.Length; i++)
                WriteLine[i] = $@"mklink / j ""{ElsaWin_Dir}\{dir[i]}"" ""{ElsaWin_DirMount}{selectName}\{dir[i]}\""";

            await AllProcess("cmd.exe", WriteLine);

            return string.Empty;
        }

        public async Task<string> MkLinkDelete(string[] dir)
        {
            string[] WriteLine = new string[dir.Length];

            for (int i = 0; i < dir.Length; i++)
                WriteLine[i] = $@"RD ""{ElsaWin_Dir}\{dir[i]}""";

            await AllProcess("cmd.exe", WriteLine);

            return string.Empty;
        }

        public async Task<string> ServiceStart()
        {
            string[] WriteLine = new string[]
            {
                "(call sc start LcSvrAuf",
                "call sc start LcSvrAdm",
                "call sc start LcSvrHis",
                "call sc start LcSvrDba",
                "call sc start LcSvrPas",
                "call sc start LcSvrSaz)"
            };

            await AllProcess("cmd.exe", WriteLine);

            await Task.Factory.StartNew(() => Thread.Sleep(2500));
            
            return string.Empty;
        }

        public async Task<string> ServiceStop()
        {
            string[] WriteLine = new string[]
            {
                "(call sc stop LcSvrAuf",
                "call sc stop LcSvrAdm",
                "call sc stop LcSvrHis",
                "call sc stop LcSvrDba",
                "call sc stop LcSvrPas",
                "call sc stop LcSvrSaz)"
            };

            await AllProcess("cmd.exe", WriteLine);

            await Task.Factory.StartNew(() => Thread.Sleep(2500));

            return string.Empty;
        }



        private async Task<string> AllProcess(string _Filename, string[] _WriteLine)
        {
            string InfoCommand = string.Empty;

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

#if !NET35
                await p.WaitForExitAsync();
                using var reader = new StreamReader(p.StandardOutput.BaseStream, Encoding.GetEncoding(CultureInfo.CurrentUICulture.TextInfo.OEMCodePage));
                InfoCommand += reader.ReadToEnd();
#else
                var tcs = new TaskCompletionSource<object>();
                p.EnableRaisingEvents = true;
                p.Exited += (s, e) => tcs.TrySetResult(null);
                await tcs.Task;

                InfoCommand += EncToUTF8(p.StandardOutput.ReadToEnd(), p.StandardOutput.CurrentEncoding);
#endif
            }
            catch (Win32Exception e)
            {
                MessageBox.Show(e.Message, "Administrator???", MessageBoxButton.OK, MessageBoxImage.Error);
                //WindowClose();
            }

            return string.Empty;
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
    }
}
