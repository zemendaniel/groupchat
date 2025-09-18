using Avalonia.Controls;
using Avalonia.Interactivity;

namespace groupchat.gui;

public partial class NicknameWindow : Window
{
    public string? Nickname { get; private set; }

    public NicknameWindow()
    {
        InitializeComponent();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        // Ensure the TextBox field exists
        if (!string.IsNullOrWhiteSpace(NicknameBox.Text))
        {
            Nickname = NicknameBox.Text.Trim();
            Close(Nickname); // close the window and return the value
        }
    }
}