using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WindowSorter.Model.Settings {
    /// <summary>
    /// 設定ファイルの読み書きを管理
    /// </summary>
    public static class SettingsService {
        public static SettingsData Current { get; set; } = new SettingsData();

        private const string FileName = "settings.json";

        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions {
            WriteIndented = true, // 読みやすくインデント
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // 日本語をエスケープしない
            Converters = { new JsonStringEnumConverter() } // Enum を文字列名で扱う
        };

        /// <summary>
        /// 設定ファイルのフルパスを取得
        /// </summary>
        private static string GetFilePath() {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName);
        }

        /// <summary>
        /// 設定を保存
        /// </summary>
        public static void Save(SettingsData data) {
            Current = data;
            Save();
        }

        /// <summary>
        /// 設定を保存
        /// </summary>
        public static void Save() {
            string filePath = GetFilePath();
            string json = JsonSerializer.Serialize(Current, Options);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// 設定を読み込む
        /// </summary>
        public static void Load() {
            string filePath = GetFilePath();

            if (!File.Exists(filePath)) {
                Current = new SettingsData();
            }

            try {
                string json = File.ReadAllText(filePath);
                SettingsData settings = JsonSerializer.Deserialize<SettingsData>(json, Options);
                Current = settings ?? new SettingsData();
            } catch (Exception) {
                // TODO : 雑なので後で直す
                Current = new SettingsData();
            }
        }
    }
}
