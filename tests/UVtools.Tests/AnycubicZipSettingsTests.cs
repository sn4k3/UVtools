using System.Text.Json;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using Xunit;

namespace UVtools.Tests;

public class AnycubicZipSettingsTests
{
    [Fact]
    public void NewResinPropertiesOmitOptionalNames()
    {
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(
            new AnycubicZipFile.SettingsResinProperty(), JsonExtensions.SettingsIndent));

        Assert.False(json.RootElement.TryGetProperty("brand_name", out _));
        Assert.False(json.RootElement.TryGetProperty("resin_name", out _));
        Assert.False(json.RootElement.TryGetProperty("setting_name", out _));
        Assert.Equal("NFEP", json.RootElement.GetProperty("film_name").GetString());
    }

    [Theory]
    [InlineData("""{"name":"r7","density":1.2}""")]
    [InlineData("""{"name":"r7","density":1.2,"brand_name":null,"resin_name":null,"setting_name":null}""")]
    public void LegacyResinPropertiesRoundTripWithoutOptionalNames(string input)
    {
        var property = JsonSerializer.Deserialize<AnycubicZipFile.SettingsResinProperty>(input);
        Assert.NotNull(property);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(property, JsonExtensions.SettingsIndent));

        Assert.False(json.RootElement.TryGetProperty("brand_name", out _));
        Assert.False(json.RootElement.TryGetProperty("resin_name", out _));
        Assert.False(json.RootElement.TryGetProperty("setting_name", out _));
        Assert.Equal("r7", json.RootElement.GetProperty("name").GetString());
        Assert.Equal(1.2f, json.RootElement.GetProperty("density").GetSingle());
    }

    [Fact]
    public void ExplicitResinNamesArePreservedOnRoundTrip()
    {
        const string input = """{"name":"r7","brand_name":"Anycubic","resin_name":"Unknown","setting_name":""}""";
        var property = JsonSerializer.Deserialize<AnycubicZipFile.SettingsResinProperty>(input);
        Assert.NotNull(property);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(property, JsonExtensions.SettingsIndent));

        Assert.Equal("Anycubic", json.RootElement.GetProperty("brand_name").GetString());
        Assert.Equal("Unknown", json.RootElement.GetProperty("resin_name").GetString());
        Assert.Equal("", json.RootElement.GetProperty("setting_name").GetString());
        Assert.Equal("r7", json.RootElement.GetProperty("name").GetString());
    }
}
