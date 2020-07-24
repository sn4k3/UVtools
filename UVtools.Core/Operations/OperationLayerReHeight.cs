
namespace UVtools.Core.Operations
{
    public sealed class OperationLayerReHeight
    {
        public bool IsMultiply { get; }
        public bool IsDivision => !IsMultiply;
        public byte Modifier { get; }
        public decimal LayerHeight { get; }
        public uint LayerCount { get; }

        public OperationLayerReHeight(bool isMultiply, byte modifier, decimal layerHeight, uint layerCount)
        {
            IsMultiply = isMultiply;
            Modifier = modifier;
            LayerHeight = layerHeight;
            LayerCount = layerCount;
        }

        public override string ToString()
        {
            return (IsMultiply ? 'x' : '÷') +$" {Modifier} → {LayerCount} layers at {LayerHeight}mm";
        }
    }
}
