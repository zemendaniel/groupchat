using System.Collections.ObjectModel;
using System.Net.Sockets;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using groupchat.core;

namespace groupchat.gui;

// todo: save last nic and last key to file, do symmetric encryption

public partial class MainWindow : Window
{
    private Chat? chat;
    private readonly ObservableCollection<ChatMessage> messages = [];
    
    public MainWindow()
    {
        InitializeComponent();
        
        NicknameBox.AttachedToVisualTree += (s, e) =>
        {
            NicknameBox.Focus();
        };
        MessagesList.ItemsSource = messages;
        var adapters = NetUtils.GetEthernetAdapters();
        AdapterComboBox.ItemsSource = adapters;
        AdapterComboBox.SelectedIndex = 0; 
        
        Closing += async (s, e) =>
        {
            if (chat == null) return;
            await chat.Dispose();
        };
    }
    
    private void StartChat_Click(object? sender, RoutedEventArgs e)
    {
        ErrorText.Text = "";
        var nickname = NicknameBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(nickname))
        {
            ErrorText.Text = "[ERROR] Nickname is empty.";
            return;
        }

        if (AdapterComboBox.SelectedItem is not AdapterInfo selectedAdapter)
            return;

        var ip = selectedAdapter.IP;
        var broadcast = selectedAdapter.Broadcast;
        
        try
        {
            chat = new Chat(
                (message, type) => Dispatcher.UIThread.Post(() => { AddMessage(message, type); }),
                nickname, broadcast!, ip!
            );
        }
        catch (SocketException ex) when (ex.ErrorCode == 10048) // port already used
        {
            ErrorText.Text = $"[ERROR] Port {Chat.Port} is already in use. Have you already started the application?";
            return;
        }
        
        StartupPanel.IsVisible = false;
        ChatPanel.IsVisible = true;
        
        InputBox.Focus();
    }

    private void AddMessage(string message, MessageType type)
    {
        var isAtBottom = MessagesScrollViewer.Offset.Y + MessagesScrollViewer.Viewport.Height >= 
                          MessagesScrollViewer.Extent.Height - 1; // small tolerance

        messages.Add(new ChatMessage { Text = message, Type = type });

        if (isAtBottom || type == MessageType.Own)
        {
            Dispatcher.UIThread.Post(() =>
            {
                MessagesScrollViewer.Offset = new Avalonia.Vector(
                    MessagesScrollViewer.Offset.X,
                    MessagesScrollViewer.Extent.Height - MessagesScrollViewer.Viewport.Height
                );
            }, DispatcherPriority.Render); 
        }
    }
    private void TitleBar_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }
    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void MinimizeButtonClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void InputBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (chat == null) 
            return;
        if (e.Key != Key.Enter)
            return;
        if (string.IsNullOrWhiteSpace(InputBox.Text)) return;
        
        var msg = InputBox.Text.Trim();
        _ = chat.SendAsync(msg);   
        InputBox.Text = "";
    }

    private void NicknameBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            StartChat_Click(sender, e);
    }
}

public class ChatMessage
{
    public required string Text { get; init; } 
    public MessageType Type { get; init; }
    
    public IBrush Color => Type switch
    {
        MessageType.Own => Brushes.CadetBlue,      // subtle but readable blue
        MessageType.Remote => Brushes.DarkSeaGreen, // muted green
        MessageType.Info => Brushes.LightSlateGray, // readable gray
        _ => Brushes.White                          // default white
    };
}