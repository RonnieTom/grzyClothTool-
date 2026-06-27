using grzyClothTool.Helpers;
using grzyClothTool.Models.Drawable;
using Material.Icons;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Threading.Tasks;
using System;

namespace grzyClothTool.Views
{
    public partial class DuplicateInspectorWindow : Window
    {
        private readonly List<DuplicateGroupViewModel> _groups = [];
        private readonly object _focusItem;

        public DuplicateInspectorWindow()
        {
            InitializeComponent();
            Owner = MainWindow.Instance;
            _focusItem = null;
            LoadAllDuplicates();
        }

        public DuplicateInspectorWindow(object sourceItem)
        {
            InitializeComponent();
            Owner = MainWindow.Instance;
            _focusItem = sourceItem;
            LoadAllDuplicates();
            FocusOnItem(sourceItem);
        }

        private void LoadAllDuplicates()
        {
            _groups.Clear();

            var allDrawableGroups = new Dictionary<string, List<GDrawable>>();
            if (MainWindow.AddonManager?.Addons != null)
            {
                foreach (var addon in MainWindow.AddonManager.Addons)
                {
                    foreach (var drawable in addon.Drawables)
                    {
                        if (!string.IsNullOrEmpty(drawable.DuplicateInfo.DuplicateGroupId))
                        {
                            var hash = drawable.DuplicateInfo.DuplicateGroupId;
                            var groupFromDetector = DuplicateDetector.GetDrawablesInGroup(hash);
                            
                            if (groupFromDetector != null && groupFromDetector.Count > 1 && !allDrawableGroups.ContainsKey(hash))
                            {
                                allDrawableGroups[hash] = groupFromDetector;
                            }
                        }
                    }
                }
            }

            foreach (var kvp in allDrawableGroups)
            {
                var group = CreateDrawableGroup(kvp.Key);
                if (group != null && group.Count > 1)
                {
                    _groups.Add(group);
                }
            }

            DuplicateGroupsControl.ItemsSource = null;
            DuplicateGroupsControl.ItemsSource = _groups;
            SubtitleText.Text = _groups.Count == 0 
                ? "未找到重复项" 
                : $"找到 {_groups.Count} 组重复项";
        }

        private DuplicateGroupViewModel CreateDrawableGroup(string hash)
        {
            var duplicates = DuplicateDetector.GetDrawablesInGroup(hash);
            if (duplicates == null || duplicates.Count <= 1)
                return null;

            var firstDrawable = duplicates[0];
            var group = new DuplicateGroupViewModel
            {
                GroupId = hash,
                GroupTitle = $"Drawable：{firstDrawable.Name}",
                GroupDescription = $"{duplicates.Count} 个相同的 Drawable",
                GroupColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(firstDrawable.DuplicateInfo.DuplicateColor)),
                Count = duplicates.Count,
                Items = []
            };

            foreach (var drawable in duplicates)
            {
                group.Items.Add(new DuplicateItemViewModel
                {
                    Name = drawable.Name,
                    Location = GetDrawableLocation(drawable),
                    Sex = drawable.SexName,
                    Item = drawable,
                    IsHighlighted = _focusItem != null && ReferenceEquals(_focusItem, drawable)
                });
            }

            group.IsHighlighted = group.Items.Any(i => i.IsHighlighted);
            return group;
        }

        private async void FocusOnItem(object item)
        {
            await Task.Delay(100);

            var targetGroup = _groups.FirstOrDefault(g => g.Items.Any(i => ReferenceEquals(i.Item, item)));
            if (targetGroup != null)
            {
                var container = DuplicateGroupsControl.ItemContainerGenerator.ContainerFromItem(targetGroup);
                if (container is FrameworkElement element)
                {
                    element.BringIntoView();
                }
            }
        }

        private static string GetDrawableLocation(GDrawable drawable)
        {
            if (MainWindow.AddonManager?.Addons == null)
                return "未知";

            for (int i = 0; i < MainWindow.AddonManager.Addons.Count; i++)
            {
                if (MainWindow.AddonManager.Addons[i].Drawables.Contains(drawable))
                {
                    return $"附加包 {i + 1}";
                }
            }

            return "未知";
        }

        private void OpenItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is DuplicateItemViewModel vm)
            {
                if (vm.Item is GDrawable drawable)
                {
                    foreach (var addon in MainWindow.AddonManager.Addons)
                    {
                        if (addon.Drawables.Contains(drawable))
                        {
                            MainWindow.AddonManager.SelectedAddon = addon;
                            
                            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                ScrollDrawableIntoView(drawable);
                            }), System.Windows.Threading.DispatcherPriority.Loaded);
                            
                            break;
                        }
                    }
                }
            }
        }

        private static void ScrollDrawableIntoView(GDrawable drawable)
        {
            try
            {
                var mainWindow = MainWindow.Instance;
                if (mainWindow == null) return;

                var projectWindow = FindVisualChild<Views.ProjectWindow>(mainWindow);
                if (projectWindow == null) return;

                var drawableList = FindVisualChild<Controls.DrawableList>(projectWindow);
                if (drawableList == null) return;

                var listBox = FindVisualChild<System.Windows.Controls.ListBox>(drawableList);
                if (listBox == null) return;

                listBox.SelectedItems.Clear();
                listBox.SelectedItem = drawable;
                listBox.ScrollIntoView(drawable);
            }
            catch (Exception ex)
            {
                LogHelper.Log($"滚动 Drawable 到可视区域时出错: {ex.Message}", Views.LogType.Warning);
            }
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild)
                {
                    return typedChild;
                }

                var result = FindVisualChild<T>(child);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is DuplicateItemViewModel vm)
            {
                var result = Controls.CustomMessageBox.Show(
                    $"确定要删除「{vm.Name}」吗？",
                    "确认删除",
                    Controls.CustomMessageBox.CustomMessageBoxButtons.OKCancel,
                    Controls.CustomMessageBox.CustomMessageBoxIcon.Warning);

                if (result == Controls.CustomMessageBox.CustomMessageBoxResult.OK)
                {
                    DeleteSingleItem(vm);
                    LoadAllDuplicates(); // refresh
                    
                    if (_groups.Count == 0)
                    {
                        Controls.CustomMessageBox.Show(
                            "所有重复项已解决！",
                            "成功",
                            Controls.CustomMessageBox.CustomMessageBoxButtons.OKOnly,
                            Controls.CustomMessageBox.CustomMessageBoxIcon.Information);
                        Close();
                    }
                }
            }
        }

        private void DeleteGroupExceptFirst_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is DuplicateGroupViewModel group)
            {
                if (group.Items.Count <= 1)
                    return;

                var result = Controls.CustomMessageBox.Show(
                    $"这将从此组中删除 {group.Items.Count - 1} 个重复项，仅保留第一个。\n\n确定吗？",
                    "确认批量删除",
                    Controls.CustomMessageBox.CustomMessageBoxButtons.OKCancel,
                    Controls.CustomMessageBox.CustomMessageBoxIcon.Warning);

                if (result == Controls.CustomMessageBox.CustomMessageBoxResult.OK)
                {
                    var itemsToDelete = group.Items.Skip(1).ToList();
                    
                    foreach (var vm in itemsToDelete)
                    {
                        DeleteSingleItem(vm);
                    }

                    SaveHelper.SetUnsavedChanges(true);
                    LoadAllDuplicates(); // refresh

                    if (_groups.Count == 0)
                    {
                        Controls.CustomMessageBox.Show(
                            $"已删除 {itemsToDelete.Count} 个重复项！\n\n所有重复项已解决！",
                            "成功",
                            Controls.CustomMessageBox.CustomMessageBoxButtons.OKOnly,
                            Controls.CustomMessageBox.CustomMessageBoxIcon.Information);
                        Close();
                    }
                    else
                    {
                        Controls.CustomMessageBox.Show(
                            $"已删除 {itemsToDelete.Count} 个重复项！",
                            "成功",
                            Controls.CustomMessageBox.CustomMessageBoxButtons.OKOnly,
                            Controls.CustomMessageBox.CustomMessageBoxIcon.Information);
                    }
                }
            }
        }

        private static void DeleteSingleItem(DuplicateItemViewModel vm)
        {
            if (vm.Item is GDrawable drawable)
            {
                foreach (var addon in MainWindow.AddonManager.Addons)
                {
                    if (addon.Drawables.Contains(drawable))
                    {
                        MainWindow.AddonManager.DeleteDrawables([drawable]);
                        break;
                    }
                }
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadAllDuplicates();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class DuplicateGroupViewModel
    {
        public string GroupId { get; set; }
        public string GroupTitle { get; set; }
        public string GroupDescription { get; set; }
        public Brush GroupColor { get; set; }
        public int Count { get; set; }
        public List<DuplicateItemViewModel> Items { get; set; }
        public bool IsHighlighted { get; set; }
    }

    public class DuplicateItemViewModel
    {
        public string Name { get; set; }
        public string Location { get; set; }
        public string Sex { get; set; }
        public Brush SexBrush => Sex?.Equals("男性", StringComparison.OrdinalIgnoreCase) == true || Sex?.Equals("male", StringComparison.OrdinalIgnoreCase) == true ? new SolidColorBrush(Color.FromRgb(0x3b, 0x82, 0xf6)) : new SolidColorBrush(Color.FromRgb(0xec, 0x48, 0x99));
        public object Item { get; set; }
        public bool IsHighlighted { get; set; }
    }
}
