using System.Collections.Generic;
using System.Text;

namespace UVtools.Core.Objects;

public record ComparisonItem(string PropertyName, object? ValueA, object? ValueB);

public class FileFormatComparison
{
	public List<ComparisonItem> Global { get; init; } = new();
	public Dictionary<uint, List<ComparisonItem>> Layers { get; init; } = new();

	public void Clear()
	{
		Global.Clear();
		Layers.Clear();
	}

	public override string ToString()
	{
		if (Global.Count == 0 && Layers.Count == 0)
		{
			return "Files matches";
		}

		var sb = new StringBuilder();
		
		if (Global.Count > 0)
		{
			sb.AppendLine($"[Global #{Global.Count}]");
			foreach (var item in Global)
			{
				sb.AppendLine($"{item.PropertyName}: {item.ValueA} != {item.ValueB}");
			}
		}
		if (Layers.Count > 0)
		{
			sb.AppendLine();
			sb.AppendLine($"[Layers #{Layers.Count}]");
			foreach (var layer in Layers)
			{
				if(layer.Value.Count == 0) continue;
				sb.AppendLine($"(Layer {layer.Key} #{layer.Value.Count})");
				foreach (var item in layer.Value)
				{
					sb.AppendLine($"{item.PropertyName}: {item.ValueA} != {item.ValueB}");
				}
				sb.AppendLine();
			}
		}

		return sb.ToString();
	}
}