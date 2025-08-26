using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Neon_music
{
    public partial class MainWindow : Window
    {
        private static readonly Mutex mutex = new Mutex(true, "Neon_music_single_instance_mutex");
        private DispatcherTimer videoTimer = new DispatcherTimer();

        public MainWindow()
        {
            if (!mutex.WaitOne(TimeSpan.Zero, true))
                Environment.Exit(0);

            InitializeComponent();

            Loaded += MainWindow_Loaded;
            IntroVideo.MediaEnded += IntroVideo_MediaEnded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            
            sender ??= this;

            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "Neonmusic.config");
            if (!File.Exists(configPath))
                Environment.Exit(0);

            var configLines = File.ReadAllLines(configPath)
                .Select(line => line?.Trim() ?? string.Empty) 
                .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                .ToList();

            ApplyTitleBarSettings(configLines);
            ApplyWindowSettings(configLines);

            string kpgg = GetConfigValue(configLines, ".-Kpgg") ?? "yes";

            if (kpgg.Equals("no", StringComparison.OrdinalIgnoreCase))
            {
                await EndIntroVideoAndLoadWebView();
                return;
            }

            string videoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "Load-source", "Video", "Neonmusic.mp4");
            if (!File.Exists(videoPath))
            {
                await EndIntroVideoAndLoadWebView();
                return;
            }

            IntroVideo.Source = new Uri(videoPath);
            IntroVideo.Play();

            videoTimer.Interval = TimeSpan.FromSeconds(5);
            videoTimer.Tick += VideoTimer_Tick;
            videoTimer.Start();
        }

        private async void VideoTimer_Tick(object? sender, EventArgs e)
        {
            videoTimer.Stop();
            await EndIntroVideoAndLoadWebView();
        }

        private async void IntroVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            await EndIntroVideoAndLoadWebView();
        }

        private async void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            videoTimer.Stop();
            IntroVideo.Stop();
            await EndIntroVideoAndLoadWebView();
        }

        private async System.Threading.Tasks.Task EndIntroVideoAndLoadWebView()
        {
            IntroGrid.Visibility = Visibility.Collapsed;
            webView.Visibility = Visibility.Visible;

            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "Neonmusic.config");
            var configLines = File.ReadAllLines(configPath)
                .Select(line => line?.Trim() ?? string.Empty)
                .ToList();

            string htmlRelativePath = ReadSingleTopicPath(configLines) ?? string.Empty;
            if (string.IsNullOrEmpty(htmlRelativePath))
            {
                Application.Current.Shutdown();
                return;
            }

            string hmsValue = GetConfigValue(configLines, ".-hms") ?? "1";

            await webView.EnsureCoreWebView2Async();
            if (webView.CoreWebView2 == null)
            {
                Application.Current.Shutdown();
                return;
            }
            webView.CoreWebView2.NavigationStarting += (s, e) =>
            {
                LoadingOverlay.Visibility = Visibility.Visible;
            };
            webView.CoreWebView2.NavigationCompleted += (s, e) =>
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            };

            if (hmsValue == "1")
            {
                string assetsFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
                webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "appassets.local",
                    assetsFolderPath,
                    CoreWebView2HostResourceAccessKind.Allow);
                webView.CoreWebView2.AddHostObjectToScript("bridge", new JsBridge());
                string htmlVirtualUri = "https://appassets.local/" + htmlRelativePath.Replace('\\', '/');
                webView.Source = new Uri(htmlVirtualUri);
            }
            else if (hmsValue == "2")
            {
                string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", htmlRelativePath);
                if (File.Exists(htmlPath))
                {
                    string htmlContent = File.ReadAllText(htmlPath);
                    webView.CoreWebView2.NavigateToString(htmlContent);
                }
                else
                {
                    Application.Current.Shutdown();
                }
            }
        }


        private void ApplyTitleBarSettings(System.Collections.Generic.List<string> lines)
        {
            string bg = GetConfigValue(lines, ".-TitleBackground") ?? string.Empty;
            if (!string.IsNullOrEmpty(bg))
            {
                if ((bg.Length == 6 || bg.Length == 8) && bg.All(c => "0123456789ABCDEFabcdef".Contains(c)))
                {
                    try
                    {
                        var color = (Color)ColorConverter.ConvertFromString("#" + bg);
                        TitleBar.Background = new SolidColorBrush(color);
                    }
                    catch { }
                }
                else
                {
                    string imgPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", bg));
                    if (File.Exists(imgPath))
                    {
                        ImageBrush brush = new ImageBrush(new BitmapImage(new Uri(imgPath)));
                        brush.Stretch = Stretch.Fill;
                        TitleBar.Background = brush;
                    }
                }
            }

            string titleText = GetConfigValue(lines, ".-TitleText") ?? string.Empty;
            if (!string.IsNullOrEmpty(titleText))
            {
                TitleTextBlock.Text = titleText;
                string textColor = GetConfigValue(lines, ".-TitleTextColor") ?? string.Empty;
                if (!string.IsNullOrEmpty(textColor) &&
                    (textColor.Length == 6 || textColor.Length == 8) &&
                    textColor.All(c => "0123456789ABCDEFabcdef".Contains(c)))
                {
                    try
                    {
                        var color = (Color)ColorConverter.ConvertFromString("#" + textColor);
                        TitleTextBlock.Foreground = new SolidColorBrush(color);
                    }
                    catch { }
                }
            }

            SetButtonContent(MinimizeBtn, GetConfigValue(lines, ".-MinimizeBtn"));
            SetButtonContent(MaximizeBtn, GetConfigValue(lines, ".-MaximizeBtn"));
            SetButtonContent(CloseBtn, GetConfigValue(lines, ".-CloseBtn"));

            string maximizeBtn1 = GetConfigValue(lines, ".-HideMaximizeBtn") ?? string.Empty;
            if (maximizeBtn1.Equals("yes", StringComparison.OrdinalIgnoreCase))
            {
                MaximizeBtn.Visibility = Visibility.Collapsed;
                ResizeMode = ResizeMode.CanMinimize;
                MaximizeBtn.IsEnabled = false;
            }
            else
            {
                MaximizeBtn.Visibility = Visibility.Visible;
                ResizeMode = ResizeMode.CanResize;
                MaximizeBtn.IsEnabled = true;
            }
        }

        private void SetButtonContent(System.Windows.Controls.Button btn, string? content)
        {
         
            string buttonContent = content ?? string.Empty;

            if (string.IsNullOrEmpty(buttonContent))
                return;

            string imgPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", buttonContent));

            if (File.Exists(imgPath))
            {
                var img = new System.Windows.Controls.Image()
                {
                    Source = new BitmapImage(new Uri(imgPath)),
                    Stretch = Stretch.Uniform,
                };
                btn.Content = img;
            }
            else
            {
                btn.Content = buttonContent;
            }
        }

        private string ReadSingleTopicPath(System.Collections.Generic.List<string> lines)
        {
            var topicLines = lines.Where(line => line.StartsWith(".-Topic=", StringComparison.OrdinalIgnoreCase)).ToList();
            return topicLines.Count == 1 ? topicLines[0].Substring(".-Topic=".Length).Trim() : string.Empty;
        }

        private void ApplyWindowSettings(System.Collections.Generic.List<string> lines)
        {
            ResizeMode = ResizeMode.CanMinimize;

            int width = ParseIntOrDefault(GetConfigValue(lines, ".-width"), 400);
            int height = ParseIntOrDefault(GetConfigValue(lines, ".-height"), 600);

            Width = width;
            Height = height;

            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
            Top = (SystemParameters.PrimaryScreenHeight - Height) / 2;
        }

        private string GetConfigValue(System.Collections.Generic.List<string> lines, string key)
        {
            var line = lines.FirstOrDefault(l => l.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase));
            if (line == null) return string.Empty;

            int idx = line.IndexOf('=');
            return idx >= 0 ? line.Substring(idx + 1).Trim() : string.Empty;
        }

        private int ParseIntOrDefault(string? s, int def)
        {
            if (int.TryParse(s, out int val))
                return val;
            return def;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleWindowMaximize();
            }
            else
            {
                DragMove();
            }
        }

        private void ToggleWindowMaximize()
        {
            if (MaximizeBtn.IsEnabled)
            {
                if (WindowState == WindowState.Normal)
                    WindowState = WindowState.Maximized;
                else
                    WindowState = WindowState.Normal;
            }
        }

        private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MaximizeBtn.IsEnabled)
            {
                ToggleWindowMaximize();
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
