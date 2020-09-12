/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace UVtools.Core.Extensions
{
    /*
     * Original code for CursoResourceLoader from:
     * 
     * https://psycodedeveloper.wordpress.com/2013/05/26/how-to-load-an-embedded-cursor-from-resources-in-c-windows-forms/
     * 
     */

    public static class CursorResourceLoader
    {
        #region Methods

        public static Cursor LoadEmbeddedCursor(byte[] cursorResource, int imageIndex = 0)
        {
            var resourceHandle = GCHandle.Alloc(cursorResource, GCHandleType.Pinned);
            var iconImage = IntPtr.Zero;
            var cursorHandle = IntPtr.Zero;

            try
            {
                var header = (IconHeader)Marshal.PtrToStructure(resourceHandle.AddrOfPinnedObject(), typeof(IconHeader));

                if (imageIndex >= header.count)
                    throw new ArgumentOutOfRangeException("imageIndex");

                var iconInfoPtr = resourceHandle.AddrOfPinnedObject() + Marshal.SizeOf(typeof(IconHeader)) + imageIndex * Marshal.SizeOf(typeof(IconInfo));
                var info = (IconInfo)Marshal.PtrToStructure(iconInfoPtr, typeof(IconInfo));

                iconImage = Marshal.AllocHGlobal(info.size + 4);
                Marshal.WriteInt16(iconImage + 0, info.hotspot_x);
                Marshal.WriteInt16(iconImage + 2, info.hotspot_y);
                Marshal.Copy(cursorResource, info.offset, iconImage + 4, info.size);

                cursorHandle = NativeMethods.CreateIconFromResource(iconImage, info.size + 4, false, 0x30000);
                return new Cursor(cursorHandle);
            }
            finally
            {
                if (cursorHandle != IntPtr.Zero)
                    NativeMethods.DestroyIcon(cursorHandle);

                if (iconImage != IntPtr.Zero)
                    Marshal.FreeHGlobal(iconImage);

                if (resourceHandle.IsAllocated)
                    resourceHandle.Free();
            }
        }

        #endregion Methods

        #region Native Methods

        static class NativeMethods
        {
            [DllImportAttribute("user32.dll", CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DestroyIcon(IntPtr hIcon);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr CreateIconFromResource(IntPtr pbIconBits, int dwResSize, bool fIcon, int dwVer);
        }

        #endregion Native Methods

        #region Native Structures

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        struct IconHeader
        {
            [FieldOffset(0)]
            public short reserved;

            [FieldOffset(2)]
            public short type;

            [FieldOffset(4)]
            public short count;
        }

        /// <summary>Union structure for icons and cursors.</summary>
        /// <remarks>For icons, field offset 4 is used for planes and field offset 6 for
        /// bits-per-pixel, while for cursors field offset 4 is used for the x coordinate
        /// of the hotspot, and field offset 6 is used for the y coordinate.</remarks>
        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        struct IconInfo
        {
            [FieldOffset(0)]
            public byte width;

            [FieldOffset(1)]
            public byte height;

            [FieldOffset(2)]
            public byte colors;

            [FieldOffset(3)]
            public byte reserved;

            [FieldOffset(4)]
            public short planes;

            [FieldOffset(6)]
            public short bpp;

            [FieldOffset(4)]
            public short hotspot_x;

            [FieldOffset(6)]
            public short hotspot_y;

            [FieldOffset(8)]
            public int size;

            [FieldOffset(12)]
            public int offset;
        }

        #endregion Native Structures
    }
}
