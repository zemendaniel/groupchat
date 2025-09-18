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
    }

    public void Init(string nickname)
    {
        chat = new Chat(
            (message, type) => Dispatcher.UIThread.Post(() =>
            {
                messages.Add(new ChatMessage { Text = message, Type = type });
            }), nickname); 
    }

    private async void Send_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(InputBox.Text)) return;
        var msg = InputBox.Text.Trim();
        await chat.SendAsync(msg);   
        InputBox.Text = "";
    }

    private void LoginButton_Click(object? sender, RoutedEventArgs e)
    {
        var nickname = NicknameBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(nickname))
            return;
        
        LoginPanel.IsVisible = false;
        ChatPanel.IsVisible = true;
        
        chat = new Chat(
            (message, type) => Dispatcher.UIThread.Post(() =>
            {
                messages.Add(new ChatMessage { Text = message, Type = type });
            }),
            nickname
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