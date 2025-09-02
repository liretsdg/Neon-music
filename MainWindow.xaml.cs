using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Neon_music
{
    public partial class MainWindow : Window
    {
        private static readonly Mutex _instanceMutex = new Mutex(true, "Neon_music_single_instance_mutex");
        private readonly DispatcherTimer _videoTimer = new DispatcherTimer();
        private bool _isMaximized;
        private string? _originalMaximizeContent;
        private string? _recoverMaximizeContent;
        private bool _isAlwaysOnTop;
        private string? _originalAlwaysOnTopContent;
        private string? _recoverAlwaysOnTopContent;
        private const string ConfigFileName = "Neonmusic.config";
        private const string ConfigDir = "assets";
        private string _configFullPath;

        public MainWindow()
        {
            if (!_instanceMutex.WaitOne(TimeSpan.Zero, true))
                Environment.Exit(0);

            InitializeComponent();
            _configFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigDir, ConfigFileName);

            Loaded += MainWindow_Loaded;
            IntroVideo.MediaEnded += IntroVideo_MediaEnded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            sender ??= this;

            if (!File.Exists(_configFullPath))
                Environment.Exit(0);

            var configLines = ReadConfigLines();
            ApplyTitleBarSettings(configLines);
            ApplyWindowSettings(configLines);

            var kpgg = GetConfigValue(configLines, ".-Kpgg") ?? "yes";
            if (kpgg.Equals("no", StringComparison.OrdinalIgnoreCase))
            {
                await EndIntroVideoAndLoadWebView();
                return;
            }

            var videoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigDir, "Load-source", "Video", "Neonmusic.mp4");
            if (!File.Exists(videoPath))
            {
                await EndIntroVideoAndLoadWebView();
                return;
            }

            IntroVideo.Source = new Uri(videoPath);
            IntroVideo.Play();

            _videoTimer.Interval = TimeSpan.FromSeconds(5);
            _videoTimer.Tick += VideoTimer_Tick;
            _videoTimer.Start();
        }
        private async void VideoTimer_Tick(object? sender, EventArgs e)
        {
            _videoTimer.Stop();
            await EndIntroVideoAndLoadWebView();
        }

        private async void IntroVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            await EndIntroVideoAndLoadWebView();
        }

        private async void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            _videoTimer.Stop();
            IntroVideo.Stop();
            await EndIntroVideoAndLoadWebView();
        }

        private async System.Threading.Tasks.Task EndIntroVideoAndLoadWebView()
        {
            IntroGrid.Visibility = Visibility.Collapsed;
            webView.Visibility = Visibility.Visible;

            if (!File.Exists(_configFullPath))
            {
                Application.Current.Shutdown();
                return;
            }

            var configLines = ReadConfigLines();
            var htmlRelativePath = ReadSingleTopicPath(configLines);
            if (string.IsNullOrEmpty(htmlRelativePath))
            {
                Application.Current.Shutdown();
                return;
            }

            var hmsValue = GetConfigValue(configLines, ".-hms") ?? "1";
            await webView.EnsureCoreWebView2Async();
            if (webView.CoreWebView2 == null)
            {
                Application.Current.Shutdown();
                return;
            }

            webView.CoreWebView2.NavigationCompleted += (s, e) =>
            {
                if (!e.IsSuccess) Application.Current.Shutdown();
            };

            if (hmsValue == "1")
            {
                var assetsFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigDir);
                webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "appassets.local",
                    assetsFolderPath,
                    CoreWebView2HostResourceAccessKind.Allow);
                webView.CoreWebView2.AddHostObjectToScript("bridge", new JsBridge());

                var htmlVirtualUri = $"https://appassets.local/{htmlRelativePath.Replace('\\', '/')}";
                webView.Source = new Uri(htmlVirtualUri);
            }
            else if (hmsValue == "2")
            {
                var htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigDir, htmlRelativePath);
                if (File.Exists(htmlPath))
                {
                    var htmlContent = File.ReadAllText(htmlPath);
                    webView.CoreWebView2.NavigateToString(htmlContent);
                }
                else
                {
                    Application.Current.Shutdown();
                }
            }
        }

        private void ApplyTitleBarSettings(List<string> lines)
        {
            var bgConfig = GetConfigValue(lines, ".-TitleBackground");
            if (!string.IsNullOrEmpty(bgConfig))
            {
                if ((bgConfig.Length == 6 || bgConfig.Length == 8) && bgConfig.All(c => "0123456789ABCDEFabcdef".Contains(c)))
                {
                    try
                    {
                        var color = (Color)ColorConverter.ConvertFromString($"#{bgConfig}");
                        TitleBar.Background = new SolidColorBrush(color);
                    }
                    catch { }
                }
                else
                {
                    var imgPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigDir, bgConfig));
                    if (File.Exists(imgPath))
                    {
                        var brush = new ImageBrush(new BitmapImage(new Uri(imgPath))) { Stretch = Stretch.Fill };
                        TitleBar.Background = brush;
                    }
                }
            }

            var titleText = GetConfigValue(lines, ".-TitleText");
            if (!string.IsNullOrEmpty(titleText))
            {
                TitleTextBlock.Text = titleText;
                var textColor = GetConfigValue(lines, ".-TitleTextColor");
                if (!string.IsNullOrEmpty(textColor) &&
                    (textColor.Length == 6 || textColor.Length == 8) &&
                    textColor.All(c => "0123456789ABCDEFabcdef".Contains(c)))
                {
                    try
                    {
                        var color = (Color)ColorConverter.ConvertFromString($"#{textColor}");
                        TitleTextBlock.Foreground = new SolidColorBrush(color);
                    }
                    catch { }
                }
            }

            SetButtonContent(MinimizeBtn, GetConfigValue(lines, ".-MinimizeBtn"));
            _originalMaximizeContent = GetConfigValue(lines, ".-MaximizeBtn");
            _recoverMaximizeContent = GetConfigValue(lines, ".-MaximizeBtnRecover");
            SetButtonContent(MaximizeBtn, _originalMaximizeContent);
            SetButtonContent(CloseBtn, GetConfigValue(lines, ".-CloseBtn"));

            _originalAlwaysOnTopContent = GetConfigValue(lines, ".-AlwaysOnTopText");
            _recoverAlwaysOnTopContent = GetConfigValue(lines, ".-AlwaysOnTopTextRecover");
            var alwaysOnTopEnabled = GetConfigValue(lines, ".-AlwaysOnTopEnabled") ?? "no";

            AlwaysOnTopBtn.Visibility = alwaysOnTopEnabled.Equals("yes", StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
            if (AlwaysOnTopBtn.Visibility == Visibility.Visible)
                SetButtonContent(AlwaysOnTopBtn, _originalAlwaysOnTopContent);

            var hideMaximize = GetConfigValue(lines, ".-HideMaximizeBtn") ?? "no";
            if (hideMaximize.Equals("yes", StringComparison.OrdinalIgnoreCase))
            {
                MaximizeBtn.Visibility = Visibility.Collapsed;
                MaximizeBtn.IsEnabled = false;
                ResizeMode = ResizeMode.CanMinimize;
            }
            else
            {
                MaximizeBtn.Visibility = Visibility.Visible;
                MaximizeBtn.IsEnabled = true;
                ResizeMode = ResizeMode.CanResize;
            }
        }
        private void SetButtonContent(Button btn, string? content)
        {
            if (string.IsNullOrEmpty(content)) return;

            content = content.Trim();
            var imgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigDir, content);
            if (File.Exists(imgPath))
            {
                btn.Content = new Image
                {
                    Source = new BitmapImage(new Uri(imgPath)),
                    Stretch = Stretch.Uniform
                };
            }
            else
            {
                btn.Content = content;
            }
        }

        private string ReadSingleTopicPath(List<string> lines)
        {
            var topicLines = lines.Where(line => line.StartsWith(".-Topic=", StringComparison.OrdinalIgnoreCase)).ToList();
            return topicLines.Count == 1 ? topicLines[0].Substring(".-Topic=".Length).Trim() : string.Empty;
        }

        private void ApplyWindowSettings(List<string> lines)
        {
            ResizeMode = ResizeMode.CanMinimize;
            var width = ParseIntOrDefault(GetConfigValue(lines, ".-width"), 400);
            var height = ParseIntOrDefault(GetConfigValue(lines, ".-height"), 600);

            Width = width;
            Height = height;
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
            Top = (SystemParameters.PrimaryScreenHeight - Height) / 2;
        }

        private List<string> ReadConfigLines()
        {
            return File.ReadAllLines(_configFullPath)
                .Select(line => line?.Trim() ?? string.Empty)
                .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                .ToList();
        }
        private string? GetConfigValue(List<string> lines, string key)
        {
            var line = lines.FirstOrDefault(l => l.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase));
            if (line == null) return null;

            var idx = line.IndexOf('=');
            return idx >= 0 ? line.Substring(idx + 1).Trim() : null;
        }
        private int ParseIntOrDefault(string? s, int def)
        {
            return int.TryParse(s, out var val) ? val : def;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                ToggleWindowMaximize();
            else
                DragMove();
        }

        private void ToggleWindowMaximize()
        {
            if (!MaximizeBtn.IsEnabled) return;

            if (!_isMaximized)
            {
                var workArea = SystemParameters.WorkArea;
                Left = workArea.Left;
                Top = workArea.Top;
                Width = workArea.Width;
                Height = workArea.Height;
                if (!string.IsNullOrEmpty(_recoverMaximizeContent))
                    SetButtonContent(MaximizeBtn, _recoverMaximizeContent);

                _isMaximized = true;
            }
            else
            {
                if (File.Exists(_configFullPath))
                {
                    var configLines = ReadConfigLines();
                    ApplyWindowSettings(configLines);
                }

                if (!string.IsNullOrEmpty(_originalMaximizeContent))
                    SetButtonContent(MaximizeBtn, _originalMaximizeContent);

                _isMaximized = false;
            }
        }

        private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeBtn_Click(object sender, RoutedEventArgs e)
        {
            ToggleWindowMaximize();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AlwaysOnTopBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!AlwaysOnTopBtn.IsEnabled) return;

            if (!_isAlwaysOnTop)
            {
                Topmost = true;
                _isAlwaysOnTop = true;
                if (!string.IsNullOrEmpty(_recoverAlwaysOnTopContent))
                    SetButtonContent(AlwaysOnTopBtn, _recoverAlwaysOnTopContent);
            }
            else
            {
                Topmost = false;
                _isAlwaysOnTop = false;
                if (!string.IsNullOrEmpty(_originalAlwaysOnTopContent))
                    SetButtonContent(AlwaysOnTopBtn, _originalAlwaysOnTopContent);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _instanceMutex?.ReleaseMutex();
            base.OnClosed(e);
        }
    }
}
