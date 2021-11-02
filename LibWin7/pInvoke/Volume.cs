using Microsoft.Win32.SafeHandles;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;

namespace Medo.IO
{
    public class Volume
    {
        #region GetVolumes
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern FindVolumeSafeHandle FindFirstVolume([Out] StringBuilder lpszVolumeName, uint cchBufferLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FindNextVolume(FindVolumeSafeHandle hFindVolume, [Out] StringBuilder lpszVolumeName, uint cchBufferLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FindVolumeClose(IntPtr hFindVolume);

        private class FindVolumeSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private FindVolumeSafeHandle()
            : base(true)
            {
            }

            public FindVolumeSafeHandle(IntPtr preexistingHandle, bool ownsHandle)
            : base(ownsHandle)
            {
                SetHandle(preexistingHandle);
            }

            protected override bool ReleaseHandle()
            {
                return FindVolumeClose(handle);
            }
        }

        public StringCollection GetVolumes()
        {
            const uint bufferLength = 1024;
            StringBuilder volume = new StringBuilder((int)bufferLength, (int)bufferLength);
            StringCollection ret = new StringCollection();

            using (FindVolumeSafeHandle volumeHandle = FindFirstVolume(volume, bufferLength))
            {
                if (volumeHandle.IsInvalid)
                {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                }

                do
                {
                    ret.Add(volume.ToString());
                } while (FindNextVolume(volumeHandle, volume, bufferLength));

                return ret;
            }
        }
        #endregion

        #region GetExtentInfo
        public bool GetExtentInfo(string volumeNameWithoutSlash, out int diskNumber, out long startingOffset, out long extentLength)
        {
            var volumeHandle = NativeMethods_GetExtentInfo.CreateFile(volumeNameWithoutSlash, 0, NativeMethods_GetExtentInfo.FILE_SHARE_READ | NativeMethods_GetExtentInfo.FILE_SHARE_WRITE, IntPtr.Zero, NativeMethods_GetExtentInfo.OPEN_EXISTING, 0, IntPtr.Zero);
            if (volumeHandle.IsInvalid == false)
            {
                var de = new NativeMethods_GetExtentInfo.VOLUME_DISK_EXTENTS();
                de.NumberOfDiskExtents = 1;
                int bytesReturned = 0;
                if (NativeMethods_GetExtentInfo.DeviceIoControl(volumeHandle, NativeMethods_GetExtentInfo.IOCTL_VOLUME_GET_VOLUME_DISK_EXTENTS, IntPtr.Zero, 0, ref de, Marshal.SizeOf(de), ref bytesReturned, IntPtr.Zero))
                {
                    if (bytesReturned > 0)
                    {
                        diskNumber = de.Extents.DiskNumber;
                        startingOffset = de.Extents.StartingOffset;
                        extentLength = de.Extents.ExtentLength;
                        return true;
                    }
                }
            }

            diskNumber = 0;
            startingOffset = 0;
            extentLength = 0;
            return false;
        }

        private static class NativeMethods_GetExtentInfo
        {
            public const uint FILE_SHARE_READ = 0x1;
            public const uint FILE_SHARE_WRITE = 0x2;
            public const uint OPEN_EXISTING = 0x3;
            public const uint IOCTL_VOLUME_GET_VOLUME_DISK_EXTENTS = 0x560000;

            [StructLayout(LayoutKind.Sequential)]
            public struct DISK_EXTENT
            {
                public int DiskNumber;
                public long StartingOffset;
                public long ExtentLength;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct VOLUME_DISK_EXTENTS
            {
                public int NumberOfDiskExtents;
                public DISK_EXTENT Extents;
            }

            [DllImport("kernel32.dll", EntryPoint = "CreateFileW", SetLastError = true)]
            public static extern VolumeSafeHandle CreateFile([In()][MarshalAs(UnmanagedType.LPWStr)] string lpFileName, uint dwDesiredAccess, uint dwShareMode, [In()] IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, [In()] IntPtr hTemplateFile);

            [DllImport("kernel32.dll", EntryPoint = "DeviceIoControl", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DeviceIoControl([In()] VolumeSafeHandle hDevice, uint dwIoControlCode, [In()] IntPtr lpInBuffer, int nInBufferSize, ref VOLUME_DISK_EXTENTS lpOutBuffer, int nOutBufferSize, ref int lpBytesReturned, IntPtr lpOverlapped);


            #region SafeHandles
            [SecurityPermission(SecurityAction.Demand)]
            public class VolumeSafeHandle : SafeHandleMinusOneIsInvalid
            {

                public VolumeSafeHandle()
                    : base(true) { }


                protected override bool ReleaseHandle()
                {
                    return CloseHandle(this.handle);
                }

                public override string ToString()
                {
                    return this.handle.ToString();
                }

                [DllImport("kernel32.dll", SetLastError = true)]
                [return: MarshalAs(UnmanagedType.Bool)]
                public static extern bool CloseHandle(IntPtr hObject);
            }

            #endregion
        }
        #endregion ChangeDriveLetter

        #region ChangeDriveLetter
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetVolumeNameForVolumeMountPoint(string lpszVolumeMountPoint, [Out] StringBuilder lpszVolumeName, uint cchBufferLength);

        [DllImport("kernel32.dll")]
        private static extern bool DeleteVolumeMountPoint(string lpszVolumeMountPoint);

        [DllImport("kernel32.dll")]
        private static extern bool SetVolumeMountPoint(string lpszVolumeMountPoint, string lpszVolumeName);

        const int MAX_PATH = 1024; //260

        [DllImport("kernel32.dll", EntryPoint = "GetVolumePathNamesForVolumeNameW", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetVolumePathNamesForVolumeName([In()][MarshalAs(UnmanagedType.LPWStr)] string lpszVolumeName, [Out()][MarshalAs(UnmanagedType.LPWStr)] StringBuilder lpszVolumePathNames, int cchBufferLength, [Out()] out int lpcchReturnLength);

        //[DllImportAttribute("kernel32.dll", EntryPoint = "GetVolumePathNamesForVolumeNameW", SetLastError = true)]
        //[return: MarshalAsAttribute(UnmanagedType.Bool)]
        //public static extern Boolean GetVolumePathNamesForVolumeName([InAttribute()][MarshalAsAttribute(UnmanagedType.LPWStr)] String lpszVolumeName, [OutAttribute()][MarshalAsAttribute(UnmanagedType.LPWStr)] StringBuilder lpszVolumePathNames, Int32 cchBufferLength, [OutAttribute()] out Int32 lpcchReturnLength);


        public void ChangeDriveLetter(string volume, string path)
        {
            //StringBuilder volume = new StringBuilder(MAX_PATH);
            //if (!GetVolumeNameForVolumeMountPoint(@"Q:\O\", volume, (uint)MAX_PATH))
            //    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

            Thread.Sleep(500);

            var volumePaths = new StringBuilder(4096);
            int volumePathsLength = 0;
            if (GetVolumePathNamesForVolumeName(volume, volumePaths, volumePaths.Capacity, out volumePathsLength))
            {
                foreach (var vol in volumePaths.ToString().Split('\0'))
                {
                    if (string.IsNullOrEmpty(vol) || vol.Length == 3)
                        continue;

                    if (vol == path)
                    {
                        break;
                    }
                    else
                    {
                        if (!DeleteVolumeMountPoint(vol))
                            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }

                        if (!SetVolumeMountPoint(path, volume))
                            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                    }

                }
            }
        }
        #endregion
    }
}