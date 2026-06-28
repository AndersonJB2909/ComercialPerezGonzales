using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MahApps.Metro.Controls;
using MahApps.Metro.IconPacks;

namespace ComercialPerezGonzales.Views.Shared;

public partial class AppDialogWindow : MetroWindow
{
    public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

    public AppDialogWindow()
    {
        InitializeComponent();
    }

    public void Configure(
        string message,
        string title,
        MessageBoxButton buttons,
        MessageBoxImage icon)
    {
        TitleText.Text = title;
        MessageText.Text = message;

        // --- Icon & accent color ---
        switch (icon)
        {
            case MessageBoxImage.Error:
                DialogIcon.Kind = PackIconMaterialKind.CloseCircleOutline;
                DialogIcon.Foreground = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
                IconBorder.Background = new SolidColorBrush(Color.FromRgb(0x2D, 0x10, 0x10));
                GlowBrush = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
                break;

            case MessageBoxImage.Warning:
                DialogIcon.Kind = PackIconMaterialKind.AlertOutline;
                DialogIcon.Foreground = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B));
                IconBorder.Background = new SolidColorBrush(Color.FromRgb(0x2D, 0x20, 0x00));
                GlowBrush = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B));
                break;

            case MessageBoxImage.Question:
                DialogIcon.Kind = PackIconMaterialKind.HelpCircleOutline;
                DialogIcon.Foreground = new SolidColorBrush(Color.FromRgb(0x38, 0xBD, 0xF8));
                IconBorder.Background = new SolidColorBrush(Color.FromRgb(0x0C, 0x20, 0x38));
                GlowBrush = new SolidColorBrush(Color.FromRgb(0x38, 0xBD, 0xF8));
                break;

            default: // Information
                DialogIcon.Kind = PackIconMaterialKind.InformationOutline;
                DialogIcon.Foreground = new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E));
                IconBorder.Background = new SolidColorBrush(Color.FromRgb(0x0A, 0x28, 0x18));
                GlowBrush = new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E));
                break;
        }

        // --- Buttons ---
        ButtonPanel.Children.Clear();
        bool isQuestion = buttons == MessageBoxButton.YesNo || buttons == MessageBoxButton.YesNoCancel;

        if (buttons == MessageBoxButton.YesNo || buttons == MessageBoxButton.YesNoCancel)
        {
            if (buttons == MessageBoxButton.YesNoCancel)
            {
                var cancelBtn = MakeButton("Cancelar", "DialogSecondaryBtn", MessageBoxResult.Cancel);
                ButtonPanel.Children.Add(cancelBtn);
            }

            var noBtn = MakeButton("No", "DialogSecondaryBtn", MessageBoxResult.No);
            var yesBtn = MakeButton("Sí", icon == MessageBoxImage.Error || icon == MessageBoxImage.Warning
                ? "DialogDangerBtn" : "DialogPrimaryBtn", MessageBoxResult.Yes);
            yesBtn.IsDefault = true;
            ButtonPanel.Children.Add(noBtn);
            ButtonPanel.Children.Add(yesBtn);
        }
        else
        {
            // OK only
            var okBtn = MakeButton("Aceptar", icon == MessageBoxImage.Error
                ? "DialogDangerBtn" : "DialogPrimaryBtn", MessageBoxResult.OK);
            okBtn.IsDefault = true;
            okBtn.IsCancel = true;
            ButtonPanel.Children.Add(okBtn);
        }
    }

    private Button MakeButton(string text, string styleKey, MessageBoxResult result)
    {
        var btn = new Button
        {
            Content = text,
            Style = (Style)Resources[styleKey],
            Margin = new Thickness(8, 0, 0, 0)
        };
        btn.Click += (_, _) =>
        {
            Result = result;
            Close();
        };
        return btn;
    }
}
