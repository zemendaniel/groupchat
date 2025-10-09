using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using groupchat.core;

namespace groupchat.gui;

public partial class MainWindow : Window
{
    private Chat? chat;
    private readonly ObservableCollection<ChatMessage> messages = [];
    private bool isPasswordShown;
    private Task? iconFlashTask;
    private readonly DispatcherTimer debounceTimer;
    private bool shouldNotify;
    private CancellationTokenSource iconFlashCts = new();
    private readonly object flashLock = new();
    
    public MainWindow()
    {
        InitializeComponent();

        var (config, password) = DataStore.Load();
        var adapters = NetUtils.GetEthernetAdapters();

        NicknameBox.Text = config.Nickname;
        PasswordBox.Text = password;
        PortSelector.Value = config.Port == 0 ? 29999 : config.Port;
        AdapterComboBox.SelectedItem = adapters.FirstOrDefault(a => a.MAC.ToString() == config.MAC) 
                                       ?? adapters.FirstOrDefault();

        MessagesList.ItemsSource = messages;
        AdapterComboBox.ItemsSource = adapters;

        Closing += (s, e) =>
        {
            if (chat == null) return;
            try
            {
                _ = chat.DisposeAsync();
            }
            catch (Exception)
            {
                // ignored
            }
        };

        Opened += (_, _) =>
        {
            NicknameBox.Focus();
            NicknameBox.CaretIndex = NicknameBox.Text?.Length ?? 0;
        };
        
        debounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100) // 100ms debounce
        };
        debounceTimer.Tick += async (_, _) =>
        {
            debounceTimer.Stop();
            await UpdateUserVisibility();
        };
        IsActiveProperty.Changed.AddClassHandler<Window>(OnIsActiveChanged);
        WindowStateProperty.Changed.AddClassHandler<Window>(OnWindowStateChanged);
        Opened += async (_, _) => await UpdateUserVisibility();
        Closed += async (_, _) => await UpdateUserVisibility();
    }
    private void OnIsActiveChanged(Window sender, AvaloniaPropertyChangedEventArgs e)
    {
        DebounceUpdate();
    }

    private void OnWindowStateChanged(Window sender, AvaloniaPropertyChangedEventArgs e)
    {
        DebounceUpdate();
    }

    private void DebounceUpdate()
    {
        debounceTimer.Stop(); // reset timer
        debounceTimer.Start();
    }

    private async Task UpdateUserVisibility()
    {
        shouldNotify = !(IsActive && WindowState != WindowState.Minimized);

        if (!shouldNotify)
        {
            Task? taskToWait;
            CancellationTokenSource cts;

            lock (flashLock)
            {
                taskToWait = iconFlashTask;
                cts = iconFlashCts;

                iconFlashTask = null;
                iconFlashCts = new CancellationTokenSource();
            }

            try
            {
                await cts.CancelAsync();
                if (taskToWait != null)
                    await taskToWait;
            }
            finally
            {
                cts.Dispose();
            }
        }
    }

    
    private async Task FlashIcon(CancellationToken ct)
    {
        var icons = new[] { "sigma_urgent_orange", "sigma_urgent_red" };
        var index = 0;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                await ChangeIcon(icons[index]);
                index = (index + 1) % icons.Length;
                await Task.Delay(1000, ct);
            }
        }
        catch (TaskCanceledException)
        {
            // ignore
        }
        finally
        {
            await ChangeIcon("sigma");
        }
    }
    
    private async Task ChangeIcon(string newIcon)
    {
        await using var stream = AssetLoader.Open(new Uri($"avares://groupchat.gui/Assets/{newIcon}.png"));
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Icon = new WindowIcon(stream);
        });
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
            return;       
        }

        if (AdapterComboBox.SelectedItem is not AdapterInfo selectedAdapter)
        {
            ErrorText.Text = "[ERROR] No network adapter was found.";
            return;
        }

        var ip = selectedAdapter.IP;
        var broadcast = selectedAdapter.Broadcast;
        
        var mac = selectedAdapter.MAC; 
        DataStore.Save(new AppData
        {
            MAC = mac.ToString(),
            Nickname = nickname,
            Port = port
        }, password);
        
        try
        {
            chat = new Chat(
                (message, type) => Dispatcher.UIThread.Post(() => { AddMessage(message, type); }),
                nickname, broadcast, ip, port, password ?? ""
            );
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
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
        if (type != MessageType.Own && shouldNotify)
        {
            lock (flashLock)
            {
                if (iconFlashTask == null)
                    iconFlashTask = Task.Run(() => FlashIcon(iconFlashCts.Token), iconFlashCts.Token);
                
            }
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

