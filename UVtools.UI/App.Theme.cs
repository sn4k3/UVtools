using Avalonia.Media;
using Avalonia.Styling;
using SukiUI;
using SukiUI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using ZLinq;

namespace UVtools.UI;

public partial class App
{
    public static SukiTheme Theme { get; private set; } = null!;

    public static List<SukiColorTheme> ThemeColors { get; } = [];

    private static void SetupTheme()
    {
        Theme = SukiTheme.GetInstance();

        ThemeColors.AddRange([
            new SukiColorTheme("UVtools", new Color(255, 102, 20, 102), new Color(255, 20, 166, 166)),
        ]);

        ThemeColors.AddRange(Theme.ColorThemes);

        ChangeBaseTheme(UserSettings.Instance.General.Theme);
        ChangeColorTheme(UserSettings.Instance.General.ThemeColor);
    }

    public static void ChangeBaseTheme(ApplicationTheme theme)
    {
        UserSettings.Instance.General.Theme = theme;
        switch (theme)
        {
            case ApplicationTheme.FluentSystem:
                Theme.ChangeBaseTheme(ThemeVariant.Default);
                break;
            case ApplicationTheme.FluentLight:
                Theme.ChangeBaseTheme(ThemeVariant.Light);
                break;
            case ApplicationTheme.FluentDark:
                Theme.ChangeBaseTheme(ThemeVariant.Dark);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(theme), theme, null);
        }
        ChangeColorTheme(UserSettings.Instance.General.ThemeColor);
    }

    public static void ChangeColorTheme(SukiColorTheme color)
    {
        UserSettings.Instance.General.ThemeColor = color.DisplayName;
        Theme.ChangeColorTheme(color);
    }

    public static void ChangeColorTheme(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName)) return;
        var color = ThemeColors.FirstOrDefault(color => color.DisplayName == displayName);
        if (color is null)
        {
            return;
        }

        ChangeColorTheme(color);
    }
}