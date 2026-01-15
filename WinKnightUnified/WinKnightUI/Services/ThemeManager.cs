using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace WinKnightUI.Services
{
    /// <summary>
    /// Manages application themes with persistence to Windows Registry.
    /// Supports preset themes and custom accent colors.
    /// </summary>
    public class ThemeManager
    {
        private static ThemeManager? _instance;
        private static readonly object _lock = new();

        private const string RegistryPath = @"SOFTWARE\WinKnight\Theme";
        
        public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

        public static ThemeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new ThemeManager();
                    }
                }
                return _instance;
            }
        }

        public ThemePreset CurrentTheme { get; private set; } = ThemePreset.Dark;
        public string CustomAccentColor { get; private set; } = "#3498DB";

        private ThemeManager()
        {
            LoadThemeFromRegistry();
        }

        /// <summary>
        /// Available theme presets
        /// </summary>
        public static readonly Dictionary<ThemePreset, ThemeColors> Presets = new()
        {
            {
                ThemePreset.Dark, new ThemeColors
                {
                    Name = "Dark",
                    Background = "#0B1A31",
                    Sidebar = "#0A1828",
                    CardBackground = "#182A41",
                    SubCard = "#1A2538",
                    StatusBadge = "#1E3A5F",
                    Hover = "#1A2B42",
                    Pressed = "#122030",
                    FontPrimary = "#FFFFFF",
                    FontSecondary = "#8796A6",
                    AccentBlue = "#3498DB",
                    AccentLightBlue = "#5DADE2",
                    Success = "#2ECC71",
                    Warning = "#F1C40F",
                    Error = "#E74C3C"
                }
            },
            {
                ThemePreset.Light, new ThemeColors
                {
                    Name = "Light",
                    Background = "#F5F7FA",
                    Sidebar = "#FFFFFF",
                    CardBackground = "#FFFFFF",
                    SubCard = "#F0F3F7",
                    StatusBadge = "#E8EDF2",
                    Hover = "#E8EDF2",
                    Pressed = "#D6DDE5",
                    FontPrimary = "#1A2332",
                    FontSecondary = "#5E6C7F",
                    AccentBlue = "#2980B9",
                    AccentLightBlue = "#3498DB",
                    Success = "#27AE60",
                    Warning = "#F39C12",
                    Error = "#C0392B"
                }
            },
            {
                ThemePreset.Ocean, new ThemeColors
                {
                    Name = "Ocean",
                    Background = "#0D1B2A",
                    Sidebar = "#1B263B",
                    CardBackground = "#1B3A4B",
                    SubCard = "#1F4068",
                    StatusBadge = "#244A65",
                    Hover = "#2A5A7A",
                    Pressed = "#1A3A50",
                    FontPrimary = "#E0FBFC",
                    FontSecondary = "#98C1D9",
                    AccentBlue = "#00B4D8",
                    AccentLightBlue = "#48CAE4",
                    Success = "#06D6A0",
                    Warning = "#FFD166",
                    Error = "#EF476F"
                }
            },
            {
                ThemePreset.Forest, new ThemeColors
                {
                    Name = "Forest",
                    Background = "#1A1D21",
                    Sidebar = "#212529",
                    CardBackground = "#2D3339",
                    SubCard = "#343A40",
                    StatusBadge = "#3D4449",
                    Hover = "#495057",
                    Pressed = "#2A2E32",
                    FontPrimary = "#E9ECEF",
                    FontSecondary = "#ADB5BD",
                    AccentBlue = "#28A745",
                    AccentLightBlue = "#34CE57",
                    Success = "#20C997",
                    Warning = "#FFC107",
                    Error = "#DC3545"
                }
            },
            {
                ThemePreset.Sunset, new ThemeColors
                {
                    Name = "Sunset",
                    Background = "#1A0F0F",
                    Sidebar = "#2D1F1F",
                    CardBackground = "#3D2828",
                    SubCard = "#4A3232",
                    StatusBadge = "#5A3C3C",
                    Hover = "#6A4646",
                    Pressed = "#3A2525",
                    FontPrimary = "#FFF5F5",
                    FontSecondary = "#D9A8A8",
                    AccentBlue = "#FF6B35",
                    AccentLightBlue = "#FF8C5A",
                    Success = "#4ECDC4",
                    Warning = "#FFE66D",
                    Error = "#FF6B6B"
                }
            },
            {
                ThemePreset.Nord, new ThemeColors
                {
                    Name = "Nord",
                    Background = "#2E3440",
                    Sidebar = "#3B4252",
                    CardBackground = "#434C5E",
                    SubCard = "#4C566A",
                    StatusBadge = "#5E6779",
                    Hover = "#5E81AC",
                    Pressed = "#4C566A",
                    FontPrimary = "#ECEFF4",
                    FontSecondary = "#D8DEE9",
                    AccentBlue = "#88C0D0",
                    AccentLightBlue = "#8FBCBB",
                    Success = "#A3BE8C",
                    Warning = "#EBCB8B",
                    Error = "#BF616A"
                }
            }
        };

        /// <summary>
        /// Applies a theme preset to the application.
        /// </summary>
        public void ApplyTheme(ThemePreset preset, ResourceDictionary? resources = null)
        {
            if (!Presets.TryGetValue(preset, out var colors))
                return;

            CurrentTheme = preset;
            ApplyColors(colors, resources);
            SaveThemeToRegistry();
            
            ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(preset, colors));
        }

        /// <summary>
        /// Applies a custom accent color to the current theme.
        /// </summary>
        public void ApplyAccentColor(string hexColor, ResourceDictionary? resources = null)
        {
            if (string.IsNullOrEmpty(hexColor) || !hexColor.StartsWith("#"))
                return;

            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hexColor);
                CustomAccentColor = hexColor;

                if (resources != null)
                {
                    resources["AccentBlue"] = new SolidColorBrush(color);
                    
                    // Create a lighter version for hover states
                    var lighterColor = Color.FromArgb(
                        color.A,
                        (byte)Math.Min(255, color.R + 30),
                        (byte)Math.Min(255, color.G + 30),
                        (byte)Math.Min(255, color.B + 30)
                    );
                    resources["AccentLightBlue"] = new SolidColorBrush(lighterColor);
                }

                SaveThemeToRegistry();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ThemeManager: Invalid accent color {hexColor}: {ex.Message}");
            }
        }

        private void ApplyColors(ThemeColors colors, ResourceDictionary? resources)
        {
            if (resources == null)
            {
                // Try to get from main window
                if (Application.Current?.MainWindow != null)
                {
                    resources = Application.Current.MainWindow.Resources;
                }
                else
                {
                    return;
                }
            }

            UpdateBrush(resources, "BackgroundBrush", colors.Background);
            UpdateBrush(resources, "SidebarBrush", colors.Sidebar);
            UpdateBrush(resources, "CardBackgroundBrush", colors.CardBackground);
            UpdateBrush(resources, "SubCardBrush", colors.SubCard);
            UpdateBrush(resources, "StatusBadgeBrush", colors.StatusBadge);
            UpdateBrush(resources, "HoverBrush", colors.Hover);
            UpdateBrush(resources, "PressedBrush", colors.Pressed);
            UpdateBrush(resources, "FontPrimary", colors.FontPrimary);
            UpdateBrush(resources, "FontSecondary", colors.FontSecondary);
            UpdateBrush(resources, "AccentBlue", colors.AccentBlue);
            UpdateBrush(resources, "AccentLightBlue", colors.AccentLightBlue);
            UpdateBrush(resources, "SuccessBrush", colors.Success);
            UpdateBrush(resources, "WarningBrush", colors.Warning);
            UpdateBrush(resources, "ErrorBrush", colors.Error);
        }

        private static void UpdateBrush(ResourceDictionary resources, string key, string hexColor)
        {
            try
            {
                var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexColor));
                resources[key] = brush;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ThemeManager: Failed to update brush {key}: {ex.Message}");
            }
        }

        private void SaveThemeToRegistry()
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(RegistryPath);
                key?.SetValue("Preset", (int)CurrentTheme);
                key?.SetValue("AccentColor", CustomAccentColor);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ThemeManager: Failed to save theme: {ex.Message}");
            }
        }

        private void LoadThemeFromRegistry()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryPath);
                if (key != null)
                {
                    var presetValue = key.GetValue("Preset");
                    if (presetValue != null && Enum.TryParse<ThemePreset>(presetValue.ToString(), out var preset))
                    {
                        CurrentTheme = preset;
                    }

                    var accentValue = key.GetValue("AccentColor");
                    if (accentValue != null)
                    {
                        CustomAccentColor = accentValue.ToString() ?? "#3498DB";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ThemeManager: Failed to load theme: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets all available theme preset names.
        /// </summary>
        public static IEnumerable<string> GetThemeNames()
        {
            foreach (var preset in Presets.Values)
            {
                yield return preset.Name;
            }
        }

        /// <summary>
        /// Gets a theme preset by name.
        /// </summary>
        public static ThemePreset? GetPresetByName(string name)
        {
            foreach (var kvp in Presets)
            {
                if (kvp.Value.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Key;
                }
            }
            return null;
        }
    }

    public enum ThemePreset
    {
        Dark,
        Light,
        Ocean,
        Forest,
        Sunset,
        Nord
    }

    public class ThemeColors
    {
        public string Name { get; set; } = "Custom";
        public string Background { get; set; } = "#0B1A31";
        public string Sidebar { get; set; } = "#0A1828";
        public string CardBackground { get; set; } = "#182A41";
        public string SubCard { get; set; } = "#1A2538";
        public string StatusBadge { get; set; } = "#1E3A5F";
        public string Hover { get; set; } = "#1A2B42";
        public string Pressed { get; set; } = "#122030";
        public string FontPrimary { get; set; } = "#FFFFFF";
        public string FontSecondary { get; set; } = "#8796A6";
        public string AccentBlue { get; set; } = "#3498DB";
        public string AccentLightBlue { get; set; } = "#5DADE2";
        public string Success { get; set; } = "#2ECC71";
        public string Warning { get; set; } = "#F1C40F";
        public string Error { get; set; } = "#E74C3C";
    }

    public class ThemeChangedEventArgs : EventArgs
    {
        public ThemePreset Preset { get; }
        public ThemeColors Colors { get; }

        public ThemeChangedEventArgs(ThemePreset preset, ThemeColors colors)
        {
            Preset = preset;
            Colors = colors;
        }
    }
}
