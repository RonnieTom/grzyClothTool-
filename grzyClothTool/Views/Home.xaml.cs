using grzyClothTool.Constants;
using grzyClothTool.Helpers;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static grzyClothTool.Controls.CustomMessageBox;

namespace grzyClothTool.Views
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>
    public partial class Home : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        private List<string> _patreonList;
        public List<string> PatreonList
        {
            get => _patreonList;
            set
            {
                _patreonList = value;
                OnPropertyChanged(nameof(PatreonList));
            }
        }

        public List<string> TranslatorsList { get; } = ["RONNIE_T", "QQ 3547376520"];

        private string _latestVersion;
        public string LatestVersion
        {
            get => _latestVersion;
            set
            {
                _latestVersion = value;
                OnPropertyChanged(nameof(LatestVersion));
            }
        }

        private List<string> _changelogHighlights;
        public List<string> ChangelogHighlights
        {
            get => _changelogHighlights;
            set
            {
                _changelogHighlights = value;
                OnPropertyChanged(nameof(ChangelogHighlights));
            }
        }

        private List<ToolInfo> _otherTools;
        public List<ToolInfo> OtherTools
        {
            get => _otherTools;
            set
            {
                _otherTools = value;
                OnPropertyChanged(nameof(OtherTools));
            }
        }

        private ObservableCollection<RecentProject> _recentlyOpened;
        public ObservableCollection<RecentProject> RecentlyOpened
        {
            get => _recentlyOpened;
            set
            {
                _recentlyOpened = value;
                OnPropertyChanged(nameof(RecentlyOpened));
                OnPropertyChanged(nameof(ShowNoRecentProjects));
            }
        }

        public bool ShowNoRecentProjects => RecentlyOpened == null || RecentlyOpened.Count == 0;

        private readonly List<string> didYouKnowStrings = [
            "您可以打开任何现有附加包，它将加载所有属性，例如高跟鞋或帽子。",
            "您可以在未完成时导出现有项目，稍后导入以继续工作。",
            "设置中有开关可启用深色主题。",
            "3D 预览中有「实时纹理」功能？它允许您实时查看纹理在模型上的效果，即使在更改后也是如此。",
            "您可以按 SHIFT + DEL 立即删除选中的 Drawable，无需弹出确认框。",
            "您可以按 CTRL + DEL 立即用预留 Drawable 替换选中的 Drawable。",
            "您可以预留 Drawable，稍后将其更换为真实模型。",
            "通过 Patreon 每月支持我将加快工具的开发速度！",
            "您可以将鼠标悬停在警告图标上，查看 Drawable 或纹理的问题。",
        ];

        public string RandomDidYouKnow => didYouKnowStrings[new Random().Next(0, didYouKnowStrings.Count)];

        public Home()
        {
            InitializeComponent();
            DataContext = this;

            OtherTools = [
                new ToolInfo
                {
                    Name = "grzyOptimizer",
                    Description = "优化 YDD 模型，在保持视觉质量的同时减少多边形和顶点数量。",
                    Url = GlobalConstants.GRZY_TOOLS_URL
                },
                new ToolInfo
                {
                    Name = "grzyTattooTool",
                    Description = "创建和编辑纹身，支持预览并快速生成 FiveM 附加包资源。",
                    Url = GlobalConstants.GRZY_TOOLS_URL
                }
            ];

            LoadRecentProjects();

            Loaded += Home_Loaded;
        }

        private void LoadRecentProjects()
        {
            var recentProjects = PersistentSettingsHelper.Instance.RecentlyOpenedProjects;
            var validProjects = recentProjects.Where(p => File.Exists(p.FilePath)).ToList();
            
            if (validProjects.Count != recentProjects.Count)
            {
                PersistentSettingsHelper.Instance.RecentlyOpenedProjects = validProjects;
            }
            
            RecentlyOpened = new ObservableCollection<RecentProject>(validProjects);
        }

        private async void Home_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await FetchPatreons();
            } 
            catch
            {
                PatreonList = ["获取 Patreon 支持者列表失败"];
            }

            try
            {
                await FetchLatestRelease();
            }
            catch
            {
                LatestVersion = "无法获取版本";
                ChangelogHighlights = ["加载更新日志摘要失败"];
            }
        }

        private async Task FetchPatreons()
        {
            var url = $"{GlobalConstants.GRZY_TOOLS_URL}/grzyClothTool/patreons";

            var response = await App.httpClient.GetAsync(url).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                PatreonList = JsonSerializer.Deserialize<List<string>>(content);
            }
        }

        private async Task FetchLatestRelease()
        {
            var url = "https://api.github.com/repos/grzybeek/grzyClothTool/releases/latest";

            App.httpClient.DefaultRequestHeaders.UserAgent.Clear();
            App.httpClient.DefaultRequestHeaders.Add("User-Agent", "grzyClothTool");

            var response = await App.httpClient.GetAsync(url).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var release = JsonSerializer.Deserialize<JsonElement>(content);
                
                await Dispatcher.InvokeAsync(() =>
                {
                    if (release.TryGetProperty("tag_name", out var tagName))
                    {
                        LatestVersion = tagName.GetString();
                    }

                    if (release.TryGetProperty("body", out var body))
                    {
                        ChangelogHighlights = ParseChangelogHighlights(body.GetString());
                    }
                });
            }
        }

        private static List<string> ParseChangelogHighlights(string changelogBody)
        {
            if (string.IsNullOrWhiteSpace(changelogBody))
                return ["暂无更新日志"];

            var highlights = new List<string>();
            var lines = changelogBody.Split(['\r', '\n'], StringSplitOptions.None);
            var inChangelogSection = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.Contains("Changelog", StringComparison.OrdinalIgnoreCase))
                {
                    inChangelogSection = true;
                    continue;
                }

                if (inChangelogSection && string.IsNullOrWhiteSpace(trimmedLine))
                {
                    continue;
                }

                if (inChangelogSection && trimmedLine.StartsWith("##"))
                {
                    break;
                }

                if (inChangelogSection &&
                    (trimmedLine.StartsWith("-") || trimmedLine.StartsWith("*") || trimmedLine.StartsWith("•")))
                {
                    var cleanLine = trimmedLine.TrimStart('-', '*', '•', ' ').Trim();
                    if (!string.IsNullOrWhiteSpace(cleanLine))
                    {
                        highlights.Add(cleanLine);

                        if (highlights.Count >= 10)
                            break;
                    }
                }
            }

            return highlights.Count > 0 ? highlights : ["查看完整更新日志了解详情"];
        }

        private void ViewChangelog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/grzybeek/grzyClothTool/releases",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开更新日志：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenAllTools_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = GlobalConstants.GRZY_TOOLS_URL,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开网站：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenToolUrl_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string url)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"无法打开 URL：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void CreateNew_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainProjectsFolder = PersistentSettingsHelper.Instance.MainProjectsFolder;
                if (string.IsNullOrEmpty(mainProjectsFolder))
                {
                    Show("请先在设置中配置主项目文件夹。", 
                         "需要配置", 
                         CustomMessageBoxButtons.OKOnly, 
                         CustomMessageBoxIcon.Warning);
                    return;
                }

                if (!Directory.Exists(mainProjectsFolder))
                {
                    Show($"主项目文件夹不存在：{mainProjectsFolder}\n\n请在设置中更新。", 
                         "文件夹未找到", 
                         CustomMessageBoxButtons.OKOnly, 
                         CustomMessageBoxIcon.Warning);
                    return;
                }

                bool nameAccepted = false;
                string projectName = string.Empty;

                while (!nameAccepted)
                {
                    var (result, textBoxValue) = Show("为您的项目选择一个名称", 
                                                       "项目名称", 
                                                       CustomMessageBoxButtons.OKCancel, 
                                                       CustomMessageBoxIcon.None, 
                                                       true);

                    if (result != CustomMessageBoxResult.OK || string.IsNullOrWhiteSpace(textBoxValue))
                    {
                        return;
                    }

                    projectName = textBoxValue.Trim();

                    if (projectName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    {
                        Show("项目名称包含无效字符，请选择其他名称。", 
                             "无效名称", 
                             CustomMessageBoxButtons.OKOnly, 
                             CustomMessageBoxIcon.Warning);
                        continue;
                    }

                    var projectFolder = Path.Combine(mainProjectsFolder, projectName);
                    if (Directory.Exists(projectFolder))
                    {
                        var autoSavePath = Path.Combine(projectFolder, "autosave.json");
                        if (File.Exists(autoSavePath))
                        {
                            var openExisting = Show(
                                $"名为「{projectName}」的项目已存在。\n\n是否要打开它？",
                                "项目已存在",
                                CustomMessageBoxButtons.YesNo,
                                CustomMessageBoxIcon.Question);

                            if (openExisting == CustomMessageBoxResult.Yes)
                            {
                                await SaveHelper.LoadSaveFileAsync(autoSavePath);
                                LoadRecentProjects();
                                MainWindow.NavigationHelper.Navigate("Project");
                                return;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            var useFolder = Show(
                                $"名为「{projectName}」的文件夹已存在，但其中没有存档文件。\n\n是否要在此文件夹中创建新项目？",
                                "文件夹已存在",
                                CustomMessageBoxButtons.YesNo,
                                CustomMessageBoxIcon.Question);

                            if (useFolder == CustomMessageBoxResult.Yes)
                            {
                                nameAccepted = true;
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }
                    else
                    {
                        nameAccepted = true;
                    }
                }

                var finalProjectFolder = Path.Combine(mainProjectsFolder, projectName);
                Directory.CreateDirectory(finalProjectFolder);

                var assetsFolder = Path.Combine(finalProjectFolder, GlobalConstants.ASSETS_FOLDER_NAME);
                Directory.CreateDirectory(assetsFolder);

                MainWindow.AddonManager.ProjectName = projectName;
                MainWindow.AddonManager.CreateAddon();

                var newProjectAutoSavePath = Path.Combine(finalProjectFolder, "autosave.json");
                PersistentSettingsHelper.Instance.AddRecentProject(
                    newProjectAutoSavePath,
                    projectName,
                    drawableCount: 0,
                    addonCount: 1
                );
                
                LoadRecentProjects();

                LogHelper.Log($"已创建新项目: {projectName}，路径: {finalProjectFolder}");
                MainWindow.NavigationHelper.Navigate("Project");
            }
            catch (Exception ex)
            {
                LogHelper.Log($"创建新项目失败: {ex.Message}", Views.LogType.Error);
                Show($"创建新项目失败：{ex.Message}", 
                     "错误", 
                     CustomMessageBoxButtons.OKOnly, 
                     CustomMessageBoxIcon.Error);
            }
        }

        private async void OpenAddon_Click(object sender, RoutedEventArgs e)
        {
            var success = await MainWindow.Instance.OpenAddonAsync(true);
            if (success)
            {
                MainWindow.NavigationHelper.Navigate("Project");
            }
        }

        private async void ImportProject_Click(object sender, RoutedEventArgs e)
        {
            var success = await MainWindow.Instance.ImportProjectAsync(true);
            if (success)
            {
                MainWindow.NavigationHelper.Navigate("Project");
            }
        }

        private async void OpenSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new()
                {
                    Title = "打开存档文件",
                    Filter = "存档文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    if (!SaveHelper.CheckUnsavedChangesMessage())
                    {
                        return;
                    }

                    await SaveHelper.LoadSaveFileAsync(openFileDialog.FileName);
                    LoadRecentProjects();
                    MainWindow.NavigationHelper.Navigate("Project");
                }
            }
            catch (Exception ex)
            {
                Show($"加载存档失败：{ex.Message}", 
                     "错误", 
                     CustomMessageBoxButtons.OKOnly, 
                     CustomMessageBoxIcon.Error);
            }
        }

        private async void RecentProject_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string filePath)
            {
                try
                {
                    if (!File.Exists(filePath))
                    {
                        Show("此存档文件已不存在。", 
                             "文件未找到", 
                             CustomMessageBoxButtons.OKOnly, 
                             CustomMessageBoxIcon.Warning);
                        
                        var recentProjects = PersistentSettingsHelper.Instance.RecentlyOpenedProjects;
                        recentProjects.RemoveAll(p => p.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
                        PersistentSettingsHelper.Instance.RecentlyOpenedProjects = recentProjects;
                        LoadRecentProjects();
                        return;
                    }

                    if (!SaveHelper.CheckUnsavedChangesMessage())
                    {
                        return;
                    }

                    await SaveHelper.LoadSaveFileAsync(filePath);
                    LoadRecentProjects();
                    MainWindow.NavigationHelper.Navigate("Project");
                }
                catch (Exception ex)
                {
                    Show($"加载存档失败：{ex.Message}", 
                         "错误", 
                         CustomMessageBoxButtons.OKOnly, 
                         CustomMessageBoxIcon.Error);
                }
            }
        }

        private void RemoveRecentProject_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            
            if (sender is Button button && button.Tag is string filePath)
            {
                try
                {
                    var project = PersistentSettingsHelper.Instance.RecentlyOpenedProjects
                        .FirstOrDefault(p => p.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
                    
                    var projectName = project?.ProjectName ?? "";
                    
                    var recentProjects = PersistentSettingsHelper.Instance.RecentlyOpenedProjects;
                    recentProjects.RemoveAll(p => p.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
                    PersistentSettingsHelper.Instance.RecentlyOpenedProjects = recentProjects;
                    
                    LoadRecentProjects();
                    
                    LogHelper.Log($"已从最近项目列表移除: {projectName}");
                }
                catch (Exception ex)
                {
                    LogHelper.Log($"移除最近项目失败: {ex.Message}", Views.LogType.Error);
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.Close();
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ToolInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
    }
}
