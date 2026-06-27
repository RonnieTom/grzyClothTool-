using grzyClothTool.Helpers;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace grzyClothTool.Views
{
    /// <summary>
    /// Interaction logic for FirstRunSetupWindow.xaml
    /// </summary>
    public partial class FirstRunSetupWindow : Window
    {
        public bool SetupCompleted { get; private set; }

        public FirstRunSetupWindow()
        {
            InitializeComponent();
            
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string defaultFolder = Path.Combine(documentsPath, "grzyClothTool Projects");
            FolderPathTextBox.Text = defaultFolder;
            ContinueButton.IsEnabled = true;

            Closing += FirstRunSetupWindow_Closing;
        }

        private void FirstRunSetupWindow_Closing(object sender, CancelEventArgs e)
        {
            if (!SetupCompleted)
            {
                var result = System.Windows.MessageBox.Show(
                    "您必须选择主文件夹才能继续使用应用程序。\n\n这将关闭应用程序。确定吗？",
                    "需要完成设置",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
                else
                {
                    // User wants to exit the application entirely
                    System.Windows.Application.Current.Shutdown();
                }
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "选择用于存储所有项目的主文件夹（不是某个具体项目文件夹）";
            dialog.ShowNewFolderButton = true;

            if (!string.IsNullOrWhiteSpace(FolderPathTextBox.Text) && Directory.Exists(FolderPathTextBox.Text))
            {
                dialog.SelectedPath = FolderPathTextBox.Text;
            }

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FolderPathTextBox.Text = dialog.SelectedPath;
                ValidationMessage.Visibility = Visibility.Collapsed;
                ContinueButton.IsEnabled = true;
            }
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedPath = FolderPathTextBox.Text;

            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                ValidationMessage.Text = "请先选择主文件夹再继续。";
                ValidationMessage.Visibility = Visibility.Visible;
                return;
            }

            if (PersistentSettingsHelper.IsRootDrive(selectedPath))
            {
                ValidationMessage.Text = "不能使用根驱动器（例如 C:\\）作为主文件夹。请选择或创建一个子文件夹。";
                ValidationMessage.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                if (!Directory.Exists(selectedPath))
                {
                    Directory.CreateDirectory(selectedPath);
                }

                string testFile = Path.Combine(selectedPath, ".grzyClothTool_test");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);

                PersistentSettingsHelper.Instance.MainProjectsFolder = selectedPath;
                PersistentSettingsHelper.Instance.IsFirstRun = false;

                SetupCompleted = true;
                DialogResult = true;
                Close();
            }
            catch (UnauthorizedAccessException)
            {
                ValidationMessage.Text = "访问被拒绝。请选择一个您有写入权限的文件夹。";
                ValidationMessage.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                ValidationMessage.Text = $"错误：{ex.Message}";
                ValidationMessage.Visibility = Visibility.Visible;
            }
        }
    }
}
