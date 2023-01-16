using System;
using System.Collections.Generic;

namespace UVtools.Core.Scripting;

public sealed class ScriptConfiguration
{
    /// <summary>
    /// Gets or sets the script name
    /// </summary>
    public string Name { get; set; } = "Unnamed script";

    /// <summary>
    /// Gets or sets the script description of what it does
    /// </summary>
    public string Description { get; set; } = "I don't know my purpose, do not run me!";

    /// <summary>
    /// Gets or sets the script author name
    /// </summary>
    public string Author { get; set; } = "Undefined";

    /// <summary>
    /// Gets or sets the script version
    /// </summary>
    public Version Version { get; set; } = new(0, 1);

    /// <summary>
    /// Gets or sets the minimum version able to run this script
    /// Scripts were introduced on v2.8
    /// </summary>
    public Version MinimumVersionToRun { get; set; } = new(2, 8, 0);

    /// <summary>
    /// List of user inputs to show on GUI for configuration of the script
    /// </summary>
    public List<ScriptBaseInput> UserInputs { get; } = new();
}