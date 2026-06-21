using System.ComponentModel;
using Nuke.Common.Tooling;

namespace build;

[TypeConverter(typeof(TypeConverter<Configuration>))]
public sealed class Configuration : Enumeration
{
    public static readonly Configuration Debug = new() { Value = nameof(Debug) };
    public static readonly Configuration Release = new() { Value = nameof(Release) };

    public static implicit operator string(Configuration configuration) => configuration.Value;
}
