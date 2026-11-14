using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LocalModAssistant.Statics
{
    public static class FilePath
    {
        public const string SettingFileName = "Setting.json";
        public const string ModsDirectoryName = "Mods";
        /// <summary>BSIPA が次回起動時にインストールフォルダへ反映するフォルダ (インストールフォルダからの相対パス)</summary>
        public static readonly string PendingDirectoryName = Path.Combine("IPA", "Pending");
        /// <summary>インストール対象とするアーカイブの拡張子</summary>
        public static readonly string[] ArchiveExtensions = { ".zip", ".7z" };
    }
}
