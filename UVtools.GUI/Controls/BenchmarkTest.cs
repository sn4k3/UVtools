namespace UVtools.GUI.Controls
{
    public sealed class BenchmarkTest
    {
        public const string DEVCPU = "Intel® Core™ i9-9900K @ 3.60 GHz";
        public const string DEVRAM = "G.SKILL Trident Z 32GB DDR4-3200MHz CL14";

        public BenchmarkTest(string name, string functionName, float devSingleThreadResult = 0, float devMultiThreadResult = 0)
        {
            Name = name;
            FunctionName = functionName;
            DevSingleThreadResult = devSingleThreadResult;
            DevMultiThreadResult = devMultiThreadResult;
        }

        public string Name { get; }
        public string FunctionName { get; }

        public float DevSingleThreadResult { get; }
        public float DevMultiThreadResult { get; }

        public override string ToString()
        {
            return $"{Name}";
        }
    }
}
