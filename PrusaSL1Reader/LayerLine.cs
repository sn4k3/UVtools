namespace PrusaSL1Reader
{
    /// <summary>
    /// Represents a line, only white pixels
    /// </summary>
    public class LayerLine
    {
        /// <summary>
        /// Gets the x start position
        /// </summary>
        public uint X { get; }

        /// <summary>
        /// Gets the x end position
        /// </summary>
        public uint X2 => X + Length;

        /// <summary>
        /// Gets the y position
        /// </summary>
        public uint Y { get; }

        /// <summary>
        /// Number of pixels to fill
        /// </summary>
        public uint Length { get; }

        public LayerLine(uint x, uint y, uint length)
        {
            X = x;
            Y = y;
            Length = length;
        }
    }
}
