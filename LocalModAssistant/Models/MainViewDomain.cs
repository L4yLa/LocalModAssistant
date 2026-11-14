using LocalModAssistant.Interfaces;
using LocalModAssistant.Statics;
using LocalModAssistant.Views;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Mvvm;
using SharpCompress.Archives;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace LocalModAssistant.Models
{
    public class MainViewDomain : BindableBase, IMainDomain
    {
        //ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*
        #region // プロパティ
        #endregion
        //ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*
        #region // コマンド
        #endregion
        //ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*
        #region // コマンド用メソッド
        #endregion
        //ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*
        #region // オーバーライドメソッド
        #endregion
        //ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*
        #region // パブリックメソッド
        public void InstallMods()
        {
            if (!Directory.Exists(this._setting.InstallDirectory)) {
                MessageBox.Show($"インストールフォルダが見つかりません。\r\n{this._setting.InstallDirectory}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (!Directory.Exists(this._setting.ModDirectory)) {
                MessageBox.Show($"Modフォルダが見つかりません。\r\n{this._setting.ModDirectory}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 同じファイルを含むアーカイブが複数ある場合は後に展開したものが残るため、順序をファイル名順に固定する
            var archiveFiles = Directory.EnumerateFiles(this._setting.ModDirectory, "*", SearchOption.TopDirectoryOnly)
                .Where(path => FilePath.ArchiveExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (archiveFiles.Count == 0) {
                MessageBox.Show($"インストールするファイルがありません。\r\n{this._setting.ModDirectory} に zip / 7z ファイルを配置してください。", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // BSIPA は起動時に IPA\Pending の中身をインストールフォルダへ上書きコピーするため、
            // BSManager / ModAssistant と同じく Pending に配置して次回起動時に反映させる
            var pendingDirectory = Path.Combine(this._setting.InstallDirectory, FilePath.PendingDirectoryName);
            var workDirectory = Path.Combine(Path.GetTempPath(), nameof(LocalModAssistant), Guid.NewGuid().ToString("N"));
            try {
                // 1. テスト: 一時フォルダへ展開し、破損(CRC)・非対応形式・不正なパスを検出する。1件でも失敗したら何も配置しない
                var tested = new List<(string ArchiveName, string ExtractedDirectory)>();
                var testFailed = new List<string>();
                for (var i = 0; i < archiveFiles.Count; i++) {
                    var archiveName = Path.GetFileName(archiveFiles[i]);
                    var extractedDirectory = Path.Combine(workDirectory, i.ToString());
                    try {
                        Directory.CreateDirectory(extractedDirectory);
                        using (var archive = ArchiveFactory.OpenArchive(archiveFiles[i])) {
                            archive.WriteToDirectory(extractedDirectory, new ExtractionOptions { ExtractFullPath = true, Overwrite = true, CheckCrc = true });
                        }
                        tested.Add((archiveName, extractedDirectory));
                    }
                    catch (Exception e) {
                        Debug.WriteLine($"{e}");
                        testFailed.Add($"{archiveName}：{GetFailureReason(e)}");
                    }
                }
                if (testFailed.Count > 0) {
                    ShowResult("失敗しました。", testFailed, Array.Empty<string>(), MessageBoxImage.Error);
                    return;
                }

                // 2. 配置: 実行前から Pending にあったファイルを記録してから、ファイル名順にコピーする
                var preexistingFiles = Directory.Exists(pendingDirectory)
                    ? new HashSet<string>(Directory.EnumerateFiles(pendingDirectory, "*", SearchOption.AllDirectories).Select(path => Path.GetRelativePath(pendingDirectory, path)), StringComparer.OrdinalIgnoreCase)
                    : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var overwritten = new List<string>();
                var copyFailed = new List<string>();
                foreach (var (archiveName, extractedDirectory) in tested) {
                    try {
                        foreach (var directory in Directory.EnumerateDirectories(extractedDirectory, "*", SearchOption.AllDirectories)) {
                            Directory.CreateDirectory(Path.Combine(pendingDirectory, Path.GetRelativePath(extractedDirectory, directory)));
                        }
                        foreach (var sourceFile in Directory.EnumerateFiles(extractedDirectory, "*", SearchOption.AllDirectories)) {
                            var relativePath = Path.GetRelativePath(extractedDirectory, sourceFile);
                            var destinationFile = Path.Combine(pendingDirectory, relativePath);
                            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));
                            File.Copy(sourceFile, destinationFile, true);
                            if (preexistingFiles.Contains(relativePath)) {
                                overwritten.Add($"{relativePath} ({archiveName})");
                            }
                        }
                    }
                    catch (Exception e) {
                        Debug.WriteLine($"{e}");
                        copyFailed.Add($"{archiveName}：{GetFailureReason(e)}");
                    }
                }

                if (copyFailed.Count == 0) {
                    ShowResult("完了しました。", Array.Empty<string>(), overwritten, MessageBoxImage.Information);
                }
                else if (copyFailed.Count < tested.Count) {
                    ShowResult("一部失敗しました。", copyFailed, overwritten, MessageBoxImage.Warning);
                }
                else {
                    ShowResult("失敗しました。", copyFailed, overwritten, MessageBoxImage.Error);
                }
            }
            finally {
                try {
                    if (Directory.Exists(workDirectory)) {
                        Directory.Delete(workDirectory, true);
                    }
                }
                catch (Exception e) {
                    Debug.WriteLine($"{e}");
                }
            }
        }

        public void Load()
        {
            if (!File.Exists(this.filePath)) {
                if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), FilePath.ModsDirectoryName))) {
                    Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), FilePath.ModsDirectoryName));
                }
                this._setting.ModDirectory = Path.Combine(Directory.GetCurrentDirectory(), FilePath.ModsDirectoryName);
                this.Save();
            }
            else {
                dynamic setting = JsonConvert.DeserializeObject(File.ReadAllText(this.filePath));
                this._setting.InstallDirectory = setting.InstallDirectory;
                this._setting.ModDirectory = setting.ModDirectory;
            }
        }

        public void Save()
        {
            File.WriteAllText(this.filePath, JsonConvert.SerializeObject(this._setting, Formatting.Indented));
        }
        #endregion
        //ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*
        #region // プライベートメソッド
        private static void ShowResult(string headline, IReadOnlyList<string> failures, IReadOnlyList<string> overwritten, MessageBoxImage icon)
        {
            var message = new StringBuilder(headline);
            foreach (var failure in failures) {
                message.Append($"\r\n{failure}");
            }
            if (overwritten.Count > 0) {
                message.Append("\r\n\r\nPending内で上書きしたファイル：");
                foreach (var file in overwritten) {
                    message.Append($"\r\n{file}");
                }
            }
            var caption = icon == MessageBoxImage.Information ? "Info" : icon == MessageBoxImage.Warning ? "Warning" : "Error";
            MessageBox.Show(message.ToString(), caption, MessageBoxButton.OK, icon);
        }

        /// <summary>例外から利用者向けの短い失敗要因を作る (派生型を先に判定する)</summary>
        private static string GetFailureReason(Exception e)
        {
            switch (e) {
                case CryptographicException _:
                    return "パスワード付きアーカイブは非対応です";
                case InvalidFormatException _:
                case IncompleteArchiveException _:
                    return "アーカイブが破損しています";
                case ArchiveOperationException _:
                    return "アーカイブ形式を認識できません";
                case ExtractionException _:
                    return "不正なパスを含んでいます";
                case NotSupportedException _:
                    return "非対応の圧縮方式です";
                case SharpCompressException _:
                    return "アーカイブが破損しています";
                case UnauthorizedAccessException _:
                    return "書き込み権限がありません";
                case IOException _:
                    return "ファイルを読み書きできません (使用中の可能性)";
                default:
                    return $"予期しないエラー ({e.GetType().Name})";
            }
        }
        #endregion
        //ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*
        #region // メンバ変数
        ISetting _setting;
        readonly string filePath = Path.Combine(Directory.GetCurrentDirectory(), FilePath.SettingFileName);
        #endregion
        //ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*ﾟ+｡*ﾟ+｡｡+ﾟ*｡+ﾟ ﾟ+｡*
        #region // 構築・破棄
        public MainViewDomain(ISetting setting)
        {
            this._setting = setting;
        }
        #endregion
    }
}
