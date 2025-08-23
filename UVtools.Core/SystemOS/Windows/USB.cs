/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace UVtools.Core.SystemOS.Windows;

public static class USB
{
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr SecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile
    );

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool DeviceIoControl(
        IntPtr hDevice,
        uint dwIoControlCode,
        IntPtr lpInBuffer,
        uint nInBufferSize,
        IntPtr lpOutBuffer,
        uint nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped
    );

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool DeviceIoControl(
        IntPtr hDevice,
        uint dwIoControlCode,
        byte[] lpInBuffer,
        uint nInBufferSize,
        IntPtr lpOutBuffer,
        uint nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped
    );

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);

    const uint GENERIC_READ = 0x80000000;
    const uint GENERIC_WRITE = 0x40000000;
    const int FILE_SHARE_READ = 0x1;
    const int FILE_SHARE_WRITE = 0x2;
    const int FSCTL_LOCK_VOLUME = 0x00090018;
    const int FSCTL_DISMOUNT_VOLUME = 0x00090020;
    const int IOCTL_STORAGE_EJECT_MEDIA = 0x2D4808;
    const int IOCTL_STORAGE_MEDIA_REMOVAL = 0x002D4804;

    /// <summary>
    /// Get the USB handler
    /// </summary>
    /// <param name="driveLetter">This should be the drive letter. Format: F:/, C:/..</param>
    public static IntPtr GetUSBHandler(string driveLetter)
    {
        var filename = @"\\.\" + driveLetter[0] + ":";
        return CreateFile(filename, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, 0x3, 0, IntPtr.Zero);
    }

    /// <summary>
    /// Eject an USB drive
    /// </summary>
    /// <param name="driveLetter">This should be the drive letter. Format: F:/, C:/..</param>
    public static bool USBEject(string driveLetter)
    {
        return Eject(GetUSBHandler(driveLetter));
    }

    public static bool Eject(IntPtr handle)
    {
        var result = false;

        if (LockVolume(handle) && DismountVolume(handle))
        {
            PreventRemovalOfVolume(handle, false);
            result = AutoEjectVolume(handle);
        }
        CloseHandle(handle);
        return result;
    }

    private static bool LockVolume(IntPtr handle, uint attempts = 10)
    {
        uint byteReturned;

        for (uint i = 0; i < attempts; i++)
        {
            if (DeviceIoControl(handle, FSCTL_LOCK_VOLUME, IntPtr.Zero, 0, IntPtr.Zero, 0, out byteReturned, IntPtr.Zero))
            {
                return true;
            }
            Thread.Sleep(500);
        }
        return false;
    }

    private static bool PreventRemovalOfVolume(IntPtr handle, bool prevent)
    {
        byte[] buf = new byte[1];
        uint retVal;

        if (prevent) buf[0] = 1;
        return DeviceIoControl(handle, IOCTL_STORAGE_MEDIA_REMOVAL, buf, 1, IntPtr.Zero, 0, out retVal, IntPtr.Zero);
    }

    private static bool DismountVolume(IntPtr handle)
    {
        uint byteReturned;
        return DeviceIoControl(handle, FSCTL_DISMOUNT_VOLUME, IntPtr.Zero, 0, IntPtr.Zero, 0, out byteReturned, IntPtr.Zero);
    }

    private static bool AutoEjectVolume(IntPtr handle)
    {
        uint byteReturned;
        return DeviceIoControl(handle, IOCTL_STORAGE_EJECT_MEDIA, IntPtr.Zero, 0, IntPtr.Zero, 0, out byteReturned, IntPtr.Zero);
    }

    private static bool CloseVolume(IntPtr handle)
    {
        return CloseHandle(handle);
    }
}