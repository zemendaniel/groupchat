using System;
using System.Linq;
using System.Text.Json;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Sockets;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using groupchat.core;

namespace groupchat.gui;

// todo: last key to file, readme

public partial class MainWindow : Window
{
    private Chat? chat;
    private readonly ObservableCollection<ChatMessage> messages = [];
    private bool isPasswordShown;
    
    public MainWindow()
    {
        InitializeComponent();

        var config = DataStore.Load();
        var adapters = NetUtils.GetEthernetAdapters();

        NicknameBox.Text = config.Nickname;
        PortSelector.Value = config.Port == 0 ? 29999 : config.Port;
        AdapterComboBox.SelectedItem = adapters.FirstOrDefault(a => a.MAC.ToString() == config.MAC) 
                                       ?? adapters.FirstOrDefault();

        MessagesList.ItemsSource = messages;
        AdapterComboBox.ItemsSource = adapters;

        Closing += async (s, e) =>
        {
            if (chat == null) return;
            await chat.Dispose();
        };

        Opened += (_, _) =>
        {
            NicknameBox.Focus();
            NicknameBox.CaretIndex = NicknameBox.Text?.Length ?? 0;
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

        var password = PasswordBox.Text;
        
        if (PortSelector.Value == null)
        {
            ErrorText.Text = "[ERROR] Port is not set.";
            return;
        }

        var port = (int)PortSelector.Value;
        if (port is < 1 or > 65535)
        {
            ErrorText.Text = "[ERROR] Port is not in valid range (1-65535).";
        }
        
        if (AdapterComboBox.SelectedItem is not AdapterInfo selectedAdapter)
            return;

        var ip = selectedAdapter.IP;
        var broadcast = selectedAdapter.Broadcast;
        
        var mac = selectedAdapter.MAC; 
        DataStore.Save(new AppData
        {
            MAC = mac.ToString(),
            Nickname = nickname,
            Port = port
        });
        
        try
        {
            chat = new Chat(
                (message, type) => Dispatcher.UIThread.Post(() => { AddMessage(message, type); }),
                nickname, broadcast, ip, port, password!
            );
        }
        catch (SocketException ex) when (ex.ErrorCode == 10048) // port already used
        {
            ErrorText.Text = $"[ERROR] Port {port} is already in use. Have you already started the application?";
            return;
        }
        
        StartupPanel.IsVisible = false;
        ChatPanel.IsVisible = true;
        FocusTextBox(InputBox);
    }
    private static void FocusTextBox(TextBox textBox)
    {
        Dispatcher.UIThread.Post(() =>
        {
            textBox.Focus();
            textBox.CaretIndex = textBox.Text?.Length ?? 0;
        }, DispatcherPriority.Background);
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

    private void ShowPasswordButton_Click(object? sender, RoutedEventArgs e)
    {
        isPasswordShown = !isPasswordShown;
        PasswordBox.PasswordChar = isPasswordShown ? '\0' : '*';
        ShowPasswordButton.Content = isPasswordShown ? "●" : "○";
    }

    private void ShowPasswordButton_OnKeyDown(object? sender, KeyEventArgs e)
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
        MessageType.Own => Brushes.CadetBlue,      
        MessageType.Remote => Brushes.DarkSeaGreen, 
        MessageType.Info => Brushes.LightSlateGray, 
        _ => Brushes.White                          
    };
}

public class AppData
{
    public string MAC { get; init; } = "";
    public string Nickname { get; init; } = "";
    public int Port { get; init; }
}

public static class DataStore
{
    private static readonly string FilePath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GroupChat", "config.json");

    public static void Save(AppData data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        
        File.WriteAllText(FilePath, JsonSerializer.Serialize(data));
    }

    public static AppData Load()
    {
        if (!File.Exists(FilePath))
            return new AppData(); 
        
        return JsonSerializer.Deserialize<AppData>(File.ReadAllText(FilePath)) ?? new AppData();
    }
}