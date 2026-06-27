using System;
using System.Configuration;
using System.IO;

namespace grzyClothTool.Helpers;

public static class UserConfigHelper
{
    public static void EnsureCodeWalkerSettingsLoadable()
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                _ = CodeWalker.Properties.Settings.Default.GTAFolder;
                return;
            }
            catch (ConfigurationErrorsException ex)
            {
                if (!TryDeleteConfigFile(ex.Filename))
                {
                    DeleteGrzyClothToolUserConfigs();
                }
            }
            catch (TypeInitializationException ex) when (ex.InnerException is ConfigurationErrorsException cfgEx)
            {
                if (!TryDeleteConfigFile(cfgEx.Filename))
                {
                    DeleteGrzyClothToolUserConfigs();
                }
            }
        }
    }

    private static bool TryDeleteConfigFile(string configPath)
    {
        if (string.IsNullOrWhiteSpace(configPath) || !File.Exists(configPath))
        {
            return false;
        }

        try
        {
            File.Delete(configPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void DeleteGrzyClothToolUserConfigs()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "grzyClothTool");

        if (!Directory.Exists(root))
        {
            return;
        }

        foreach (var userConfig in Directory.EnumerateFiles(root, "user.config", SearchOption.AllDirectories))
        {
            try
            {
                File.Delete(userConfig);
            }
            catch
            {
                // ignore locked files
            }
        }
    }
}
