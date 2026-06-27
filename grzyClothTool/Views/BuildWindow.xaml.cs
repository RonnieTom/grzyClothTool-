using grzyClothTool.Controls;
using grzyClothTool.Helpers;
using grzyClothTool.Models;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static grzyClothTool.Controls.CustomMessageBox;
using static grzyClothTool.Enums;

namespace grzyClothTool.Views
{
    /// <summary>
    /// Interaction logic for BuildWindow.xaml
    /// </summary>
    public partial class BuildWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string ProjectName { get; set; } = MainWindow.AddonManager.ProjectName;

        private bool _isBuilding;
        public bool IsBuilding
        {
            get => _isBuilding;
            set
            {
                if (_isBuilding != value)
                {
                    _isBuilding = value;
                    OnPropertyChanged(nameof(IsBuilding));
                }
            }
        }
        private int _progressValue;
        public int ProgressValue
        {
            get => _progressValue;
            set
            {
                if (_progressValue != value)
                {
                    _progressValue = value;
                    OnPropertyChanged(nameof(ProgressValue));
                }
            }
        }

        private bool _splitAddons;
        public bool SplitAddons
        {
            get => _splitAddons;
            set
            {
                if (_splitAddons != value)
                {
                    _splitAddons = value;
                    OnPropertyChanged(nameof(SplitAddons));
                }
            }
        }

        private bool _isWarningVisible;
        public bool IsWarningVisible
        {
            get => _isWarningVisible;
            set
            {
                if (_isWarningVisible != value)
                {
                    _isWarningVisible = value;
                    OnPropertyChanged(nameof(IsWarningVisible));
                }
            }
        }

        private string _warningMessage;
        public string WarningMessage
        {
            get => _warningMessage;
            set
            {
                if (_warningMessage != value)
                {
                    _warningMessage = value;
                    OnPropertyChanged(nameof(WarningMessage));
                }
            }
        }

        private bool _canBuild = true;
        public bool CanBuild
        {
            get => _canBuild;
            set
            {
                if (_canBuild != value)
                {
                    _canBuild = value;
                    OnPropertyChanged(nameof(CanBuild));
                }
            }
        }

        public string BuildPath { get; set; } = GetDefaultBuildPath();

        private static string GetDefaultBuildPath()
        {
            var projectName = MainWindow.AddonManager.ProjectName;
            var mainFolder = PersistentSettingsHelper.Instance.MainProjectsFolder;

            if (string.IsNullOrEmpty(projectName) || string.IsNullOrEmpty(mainFolder))
            {
                return string.Empty;
            }

            return Path.Combine(mainFolder, projectName, "build_output");
        }

        private BuildResourceType _resourceType;

        public BuildWindow()
        {
            InitializeComponent();
            DataContext = this;

            this.Loaded += Window_Loaded;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            split_addons.IsEnabled = MainWindow.AddonManager.Addons.Count > 1;

            CheckAddons();
        }

        private void CheckAddons()
        {
            if (string.IsNullOrEmpty(ProjectName))
            {
                IsWarningVisible = true;
                WarningMessage = "未加载项目。请先创建或打开项目。";
                CanBuild = false;
                return;
            }

            if (string.IsNullOrEmpty(BuildPath))
            {
                IsWarningVisible = true;
                WarningMessage = "无法确定构建路径。请检查项目设置。";
                CanBuild = false;
                return;
            }

            var allDrawablesCount = MainWindow.AddonManager.Addons.Sum(a => a.Drawables.Count);
            if (allDrawablesCount == 0)
            {
                IsWarningVisible = true;
                WarningMessage = "未找到 Drawable。请添加 Drawable 后才能构建资源。";
                CanBuild = false;
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            FocusManager.SetFocusedElement(this, this);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = IsBuilding;
        }

        private async Task BuildResource(BuildResourceHelper buildHelper)
        {
            switch (_resourceType)
            {
                case BuildResourceType.FiveM:
                    await buildHelper.BuildFiveMResource();
                    break;
                case BuildResourceType.AltV:
                    await buildHelper.BuildAltVResource();
                    break;
                case BuildResourceType.Singleplayer:
                    await buildHelper.BuildSingleplayerResource();
                    break;
                default:
                    throw new NotImplementedException($"Unsupported resource type: {_resourceType}");
            }
        }

        private async void build_MyBtnClickEvent(object sender, RoutedEventArgs e)
        {
            var error = ValidateProjectName();
            if (error != null)
            {
                MessageBox.Show(error);
                return;
            }

            if (string.IsNullOrEmpty(ProjectName) || string.IsNullOrEmpty(BuildPath))
            {
                CustomMessageBox.Show("请填写所有字段，并确保已加载项目。", "错误", CustomMessageBoxButtons.OKOnly);
                return;
            }

            var buildButton = sender as CustomButton;
            if (buildButton != null)
            {
                buildButton.IsEnabled = false; // blocking interactions - spamming button led to building multiple times/exception
            }

            int totalSteps = MainWindow.AddonManager.GetTotalDrawableAndTextureCount();

            ProgressValue = 0;
            pbBuild.Maximum = totalSteps;
            IsBuilding = true;

            await SaveHelper.SaveAsync();

            try
            {
                var timer = new Stopwatch();

                timer.Start();

                var progress = new Progress<int>(value => ProgressValue += value);
                var buildHelper = new BuildResourceHelper(ProjectName, BuildPath, progress, _resourceType, SplitAddons);

                await Task.Run(() => BuildResource(buildHelper)); // moved out of ui thread, so users don't think tool stopped responding

                timer.Stop();
                CustomMessageBox.Show($"构建完成，耗时：{timer.Elapsed}", "构建完成", CustomMessageBoxButtons.OpenFolder, BuildPath);
                LogHelper.Log($"构建完成，耗时: {timer.Elapsed}");
            }
            catch (Exception ex)
            {
                LogHelper.Log($"构建失败: {ex}", LogType.Error);
                CustomMessageBox.Show($"构建失败：\n\n{ex}", "错误", CustomMessageBoxButtons.OKOnly, CustomMessageBoxIcon.Error);
            }
            finally
            {
                ProgressValue = totalSteps; // make sure that progress bar is full

                if (buildButton != null)
                {
                    buildButton.IsEnabled = true;
                }

                IsBuilding = false;
                Close();
            }
        }

        private void RadioButton_ChangedEvent(object sender, RoutedEventArgs e)
        {
            if (sender is ModernLabelRadioButton radioButton && radioButton.IsChecked == true)
            {
                _resourceType = radioButton.Label switch
                {
                    "FiveM" => BuildResourceType.FiveM,
                    "AltV" => BuildResourceType.AltV,
                    "Singleplayer" => BuildResourceType.Singleplayer,
                    _ => throw new NotImplementedException()
                };


                // Singleplayer doesn't support splitting addons
                if (_resourceType == BuildResourceType.Singleplayer)
                {
                    SplitAddons = false;
                    split_addons.IsEnabled = false;
                }
                else if (DataContext != null) // check if DataContext exist, to prevent error (happens on initialization)
                {
                    split_addons.IsEnabled = MainWindow.AddonManager.Addons.Count > 1;
                }
            }
        }

        private string ValidateProjectName()
        {
            string result = null;

            if (string.IsNullOrEmpty(ProjectName))
            {
                result = "项目名称不能为空";
            }
            else if (ProjectName.Length < 3)
            {
                result = "项目名称至少需要 3 个字符";
            }
            else if (ProjectName.Length > 50)
            {
                result = "项目名称不能超过 50 个字符";
            }
            else if (!Regex.IsMatch(ProjectName, @"^[a-z0-9_]+$"))
            {
                result = "项目名称只能包含小写字母、数字和下划线";

            }

            return result;
        }
    }
}
