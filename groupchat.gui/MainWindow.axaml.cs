using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using groupchat.core;

namespace groupchat.gui;


public partial class MainWindow : Window
{
    private Chat chat;
    private readonly ObservableCollection<ChatMessage> messages = [];
    
    public MainWindow()
    {
        InitializeComponent();
        
        MessagesList.ItemsSource = messages;
        var adapters = NetUtils.GetEthernetAdapters();
        AdapterComboBox.ItemsSource = adapters;
        AdapterComboBox.SelectedIndex = 0; 
    }
    
    private async void Send_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(InputBox.Text)) return;
        var msg = InputBox.Text.Trim();
        await chat.SendAsync(msg);   
        InputBox.Text = "";
    }

    private void StartChat_Click(object? sender, RoutedEventArgs e)
    {
        var nickname = NicknameBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(nickname))
            return;

        if (AdapterComboBox.SelectedItem is not AdapterInfo selectedAdapter)
            return;

        var ip = selectedAdapter.IP;
        var broadcast = selectedAdapter.Broadcast;
        
        StartupPanel.IsVisible = false;
        ChatPanel.IsVisible = true;
        
        chat = new Chat(
            (message, type) => Dispatcher.UIThread.Post(() =>
            {
                messages.Add(new ChatMessage { Text = message, Type = type });
            }),
            nickname, broadcast!, ip!
        );
    }
}

public class ChatMessage
{
    public required string Text { get; init; } 
    public MessageType Type { get; init; }
    
    public IBrush Color => Type switch
    {
        MessageType.Own => Brushes.Blue,
        MessageType.Remote => Brushes.Green,
        MessageType.Info => Brushes.Gray,
        _ => Brushes.White
    };
}