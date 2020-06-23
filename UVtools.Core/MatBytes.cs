using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Emgu.CV;
using Emgu.CV.CvEnum;

namespace UVtools.Core
{
    public sealed class MatBytes : IDisposable
    {
        private bool m_IsDisposed;
        /// <summary>
        /// Gets the byte structure of this Mat
        /// </summary>
        public byte[] Bytes;

        /// <summary>
        /// Gets the <see cref="Mat"/>
        /// </summary>
        public Mat Mat { get; }

        /// <summary>
        /// Gets the <see cref="GCHandle"/> for the allocated bytes
        /// </summary>
        public GCHandle Handle { get; }

        public byte this[int index]
        {
            get => Bytes[index];
            set => Bytes[index] = value;
        }

        public byte this[int x, int y]
        {
            get => Bytes[y * Mat.Width + x];
            set => Bytes[y * Mat.Width + x] = value;
        }

        public MatBytes(Mat mat) : this(mat.Size, (byte) mat.NumberOfChannels, mat.Depth)
        {
            var s = mat.DataPointer;
            
        }

        public MatBytes(Size size, byte channels = 1, DepthType depth = DepthType.Cv8U)
        {
            Bytes = new byte[size.Width * size.Height * channels];
            Handle = GCHandle.Alloc(Bytes, GCHandleType.Pinned);
            Mat = new Mat(size, depth, channels, Handle.AddrOfPinnedObject(), size.Width * channels);
        }

        public MatBytes(Size size, out byte[] bytes, byte channels = 1, DepthType depth = DepthType.Cv8U) : this(size, channels, depth)
        {
            bytes = Bytes;
        }

        public void Free()
        {
            Handle.Free();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (m_IsDisposed)
                return;

            if (isDisposing)
            {
                Mat?.Dispose();
                Free();
            }
            m_IsDisposed = true;
        }

    }
}
