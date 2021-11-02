using System.Threading.Tasks;

namespace ElsaWinVHD.Domain
{
    public interface INet35Win7
    {
        string ElsaWin_Dir { get; }
        string ElsaWin_DirMount { get; }
        Task<string> DiskPartAttach(string path, string selectName);
        Task<string> DiskPartDetach(string path, string selectName);

        Task<string> ServiceStart();
        Task<string> ServiceStop();

        Task<string> MkLinkCreate(string[] dir, string selectName);
        Task<string> MkLinkDelete(string[] dir);
    }
}
