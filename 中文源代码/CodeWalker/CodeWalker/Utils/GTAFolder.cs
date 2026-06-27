using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using CodeWalker.Properties;
using Microsoft.Win32;
using CodeWalker.GameFiles;
using CodeWalker.Utils;

namespace CodeWalker
{
    public static class GTAFolder
    {
        public static string CurrentGTAFolder { get; private set; } = Settings.Default.GTAFolder;

        public static bool ValidateGTAFolder(string folder, out string failReason)
        {
            failReason = "";

            if(string.IsNullOrWhiteSpace(folder))
            {
                failReason = "未指定文件夹";
                return false;
            }

            if(!Directory.Exists(folder))
            {
                failReason = $"文件夹 \"{folder}\" 不存在";
                return false;
            }

            if(!File.Exists(folder + @"\gta5.exe"))
            {
                failReason = $"在文件夹 \"{folder}\" 中未找到 GTA5.exe";
                return false;
            }

            return true;
        }

        public static bool ValidateGTAFolder(string folder) => ValidateGTAFolder(folder, out string reason);

        public static bool IsCurrentGTAFolderValid() => ValidateGTAFolder(CurrentGTAFolder);

        public static bool UpdateGTAFolder(bool UseCurrentIfValid = false)
        {
            if(UseCurrentIfValid && IsCurrentGTAFolderValid())
            {
                return true;
            }

            string origFolder = CurrentGTAFolder;
            string folder = CurrentGTAFolder;
            SelectFolderForm f = new SelectFolderForm();

            string autoFolder = AutoDetectFolder(out string source);
            if (autoFolder != null)
            {
                f.SelectedFolder = autoFolder;
            }

            f.ShowDialog();
            if(f.Result == DialogResult.OK && Directory.Exists(f.SelectedFolder))
            {
                folder = f.SelectedFolder;
            }

            string failReason;
            if(ValidateGTAFolder(folder, out failReason))
            {
                SetGTAFolder(folder);
                if(folder != origFolder)
                {
                    MessageBox.Show($"已成功将 GTA 文件夹更改为 \"{folder}\"", "设置 GTA 文件夹", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return true;
            } else
            {
                var tryAgain = MessageBox.Show($"文件夹 \"{folder}\" 不是有效的 GTA 文件夹：\n\n{failReason}\n\n是否要尝试选择其他文件夹？", "无法设置 GTA 文件夹", MessageBoxButtons.RetryCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                if(tryAgain == DialogResult.Retry)
                {
                    return UpdateGTAFolder(false);
                } else
                {
                    return false;
                }
            }
        }

        public static bool SetGTAFolder(string folder)
        {
            if(ValidateGTAFolder(folder))
            {
                CurrentGTAFolder = folder;
                Settings.Default.GTAFolder = folder;
                Settings.Default.Save();
                return true;
            }

            return false;
        }

        public static string GetCurrentGTAFolderWithTrailingSlash() =>CurrentGTAFolder.EndsWith(@"\") ? CurrentGTAFolder : CurrentGTAFolder + @"\";

        public static bool AutoDetectFolder(out Dictionary<string, string> matches)
        {
            matches = new Dictionary<string, string>();

            if(ValidateGTAFolder(CurrentGTAFolder))
            {
                matches.Add("当前 CodeWalker 文件夹", CurrentGTAFolder);
            }

            RegistryKey baseKey32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            string steamPathValue = baseKey32.OpenSubKey(@"Software\Rockstar Games\GTAV")?.GetValue("InstallFolderSteam") as string;
            string retailPathValue = baseKey32.OpenSubKey(@"Software\Rockstar Games\Grand Theft Auto V")?.GetValue("InstallFolder") as string;
            string oivPathValue = Registry.CurrentUser.OpenSubKey(@"Software\NewTechnologyStudio\OpenIV.exe\BrowseForFolder")?.GetValue("game_path_Five_pc") as string;

            if(steamPathValue?.EndsWith("\\GTAV") == true)
            {
                steamPathValue = steamPathValue.Substring(0, steamPathValue.LastIndexOf("\\GTAV"));
            }

            if(ValidateGTAFolder(steamPathValue))
            {
                matches.Add("Steam", steamPathValue);
            }

            if(ValidateGTAFolder(retailPathValue))
            {
                matches.Add("Retail", retailPathValue);
            }

            if(ValidateGTAFolder(oivPathValue))
            {
                matches.Add("OpenIV", oivPathValue);
            }

            return matches.Count > 0;
        }

        public static string AutoDetectFolder(out string source)
        {
            source = null;

            if(AutoDetectFolder(out Dictionary<string, string> matches))
            {
                var match = matches.First();
                source = match.Key;
                return match.Value;
            }

            return null;
        }

        public static string AutoDetectFolder() => AutoDetectFolder(out string _);

        public static void UpdateSettings()
        {
            if (string.IsNullOrEmpty(Settings.Default.Key) && (GTA5Keys.PC_AES_KEY != null))
            {
                Settings.Default.Key = Convert.ToBase64String(GTA5Keys.PC_AES_KEY);
                Settings.Default.Save();
            }
        }
    }
}
