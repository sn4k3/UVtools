using Avalonia;

namespace UVtools.WPF.Extensions
{
    public static class PrimitivesExtensions
    {
        public static bool IsEmpty(this Point point)
        {
            return point.X == 0 && point.Y == 0;
        }
    }
}
