using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Application = System.Windows.Application;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace WindowSorter.Core {
    public static class ColorManager {

        // 色を適用する
        public static void Apply(string selectedColorHex, string backgroundColorHex) {

            // 枠の色と背景色はそのまま
            Color selectedColor;
            Color backgroundColor;
            try {
                selectedColor = (Color)ColorConverter.ConvertFromString(selectedColorHex);
                backgroundColor = (Color)ColorConverter.ConvertFromString(backgroundColorHex);
            } catch {
                // TODO: デフォルト値をSettingsDataからとれるようにする
                selectedColor = Color.FromRgb(128, 128, 255);
                backgroundColor = Color.FromRgb(32, 32, 32);
            }

            // 背景色から、他の色を自動設定
            bool isDark = GetLuma(backgroundColor) < 128;

            // *** 文字色 ***
            Color primaryText = isDark ? primaryText = Color.FromRgb(240, 240, 240) : Color.FromRgb(12, 12, 12);
            Color secondaryText = isDark ? ShiftLuminance(primaryText, -0.33) : ShiftLuminance(primaryText, 0.33);

            // *** 検索背景色 ***
            Color searchBg = isDark ? ShiftLuminance(backgroundColor, 0.15) : ShiftLuminance(backgroundColor, 0.15);

            // *** セパレータ ***
            var separator = secondaryText;

            // *** タイトル背景色 ***
            Color titleBg = isDark ? ShiftLuminance(backgroundColor, -0.15) : ShiftLuminance(backgroundColor, 0.15);

            Application.Current.Resources["SelectedBrush"] = new SolidColorBrush(selectedColor);
            Application.Current.Resources["WindowBackgroundBrush"] = new SolidColorBrush(backgroundColor);
            Application.Current.Resources["PrimaryTextBrush"] = new SolidColorBrush(primaryText);
            Application.Current.Resources["SecondaryTextBrush"] = new SolidColorBrush(secondaryText);
            Application.Current.Resources["SearchBoxBrush"] = new SolidColorBrush(searchBg);
            Application.Current.Resources["SeparatorBrush"] = new SolidColorBrush(separator);
            Application.Current.Resources["TitleBarBackgroundBrush"] = new SolidColorBrush(titleBg);
        }

        private static int GetLuma(Color c) {
            return (int)Math.Ceiling((double)(c.R * 299 + c.G * 587 + c.B * 114) / 1000.0);
        }

        // delta ... -1.0 to 1.0
        private static Color ShiftLuminance(Color color, double delta) {
            // RGB -> [0, 1]
            float r = color.R / 255f;
            float g = color.G / 255f;
            float b = color.B / 255f;

            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            float h, s, l;

            // *** RGB -> HSL ***
            l = (max + min) / 2f;

            if (max == min) {
                h = s = 0;
            } else {
                float d = max - min;
                s = l > 0.5f ? d / (2f - max - min) : d / (max + min);

                if (max == r) h = (g - b) / d + (g < b ? 6 : 0);
                else if (max == g) h = (b - r) / d + 2;
                else h = (r - g) / d + 4;

                h /= 6;
            }

            // *** 明度の調整 ***
            l = Math.Clamp(l + (float)delta, 0, 1);

            // *** HSL -> RGB ***
            if (s == 0) {
                r = g = b = l;
            } else {
                float q = l < 0.5f ? l * (1f + s) : l + s - l * s;
                float p = 2f * l - q;
                r = HueToRgb(p, q, h + 1f / 3f);
                g = HueToRgb(p, q, h);
                b = HueToRgb(p, q, h - 1f / 3f);
            }

            return Color.FromRgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }

        private static float HueToRgb(float p, float q, float t) {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1f / 6f) return p + (q - p) * 6f * t;
            if (t < 1f / 2f) return q;
            if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6f;
            return p;
        }
    }
}
