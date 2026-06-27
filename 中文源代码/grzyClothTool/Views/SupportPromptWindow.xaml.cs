using System.Windows;

namespace grzyClothTool.Views;

public partial class SupportPromptWindow : Window
{
    public SupportPromptWindow()
    {
        InitializeComponent();
    }

    private void Cancel_MyBtnClickEvent(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Support_MyBtnClickEvent(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
