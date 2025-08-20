using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
        private static Mutex mutex = new Mutex(true, "Neon_music_single_instance_mutex");
        private DispatcherTimer? videoTimer;

        public MainWindow()
        {
            if (!mutex.WaitOne(TimeSpan.Zero, true))
                Environment.Exit(0);

            InitializeComponent();

            Loaded += MainWindow_Loaded;
            IntroVideo.MediaEnded += IntroVideo_MediaEnded;
        }

        private async void MainWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "Neonmusic.config");
            if (!File.Exists(configPath))
                Environment.Exit(0);

            var configLines = File.ReadAllLines(configPath)
    .Select(line => line.Trim())
    .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
    .ToList();

            ApplyTitleBarSettings(configLines);
            ApplyWindowSettings(configLines);

            string videoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "Icon", "Neonmusic.mp4");
            if (!File.Exists(videoPath))
            {
                await EndIntroVideoAndLoadWebView();
                return;
            }
            IntroVideo.Source = new Uri(videoPath);
            IntroVideo.Play();
            videoTimer = new DispatcherTimer();
            videoTimer.Interval = TimeSpan.FromSeconds(5);
            videoTimer.Tick += VideoTimer_Tick;
            videoTimer.Start();
        }

        private async void VideoTimer_Tick(object? sender, EventArgs e)
        {
            if (videoTimer == null) return;
            videoTimer.Stop();
            await EndIntroVideoAndLoadWebView();
        }

        private async void IntroVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            await EndIntroVideoAndLoadWebView();
        }

        private async void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            if (videoTimer != null)
                videoTimer.Stop();

            IntroVideo.Stop();
            await EndIntroVideoAndLoadWebView();
        }

        private async System.Threading.Tasks.Task EndIntroVideoAndLoadWebView()
        {
            IntroGrid.Visibility = Visibility.Collapsed;
            webView.Visibility = Visibility.Visible;

            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "Neonmusic.config");
            var configLines = File.ReadAllLines(configPath).Select(line => line.Trim()).ToList();

            string htmlRelativePath = ReadSingleTopicPath(configLines);
            if (string.IsNullOrEmpty(htmlRelativePath))
            {
                Application.Current.Shutdown();
                return;
            }

            string assetsFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "appassets.local",
                assetsFolderPath,
                CoreWebView2HostResourceAccessKind.Allow);
            webView.CoreWebView2.AddHostObjectToScript("bridge", new JsBridge());
            string htmlVirtualUri = "https://appassets.local/" + htmlRelativePath.Replace('\\', '/');
            webView.Source = new Uri(htmlVirtualUri);
        }



        private void ApplyTitleBarSettings(System.Collections.Generic.List<string> lines)
        {
            string bg = GetConfigValue(lines, ".-TitleBackground");
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
            string titleText = GetConfigValue(lines, ".-TitleText");
            if (!string.IsNullOrEmpty(titleText))
            {
                TitleTextBlock.Text = titleText;
                string textColor = GetConfigValue(lines, ".-TitleTextColor");
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


            string title = GetConfigValue(lines, ".-TitleText");
            if (!string.IsNullOrEmpty(title))
                TitleTextBlock.Text = title;

            SetButtonContent(MinimizeBtn, GetConfigValue(lines, ".-MinimizeBtn"));
            SetButtonContent(MaximizeBtn, GetConfigValue(lines, ".-MaximizeBtn"));
            SetButtonContent(CloseBtn, GetConfigValue(lines, ".-CloseBtn"));

            string maximizeBtn1 = GetConfigValue(lines, ".-HideMaximizeBtn");
            if (maximizeBtn1 != null && maximizeBtn1.Equals("yes", StringComparison.OrdinalIgnoreCase))
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
            if (string.IsNullOrEmpty(content))
                return;

            string imgPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", content));

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
                btn.Content = content;
            }
        }

        private string? ReadSingleTopicPath(System.Collections.Generic.List<string> lines)
        {
            var topicLines = lines.Where(line => line.StartsWith(".-Topic=", StringComparison.OrdinalIgnoreCase)).ToList();
            if (topicLines.Count != 1) return null;
            return topicLines[0].Substring(".-Topic=".Length).Trim();
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

        private string? GetConfigValue(System.Collections.Generic.List<string> lines, string key)
        {
            var line = lines.FirstOrDefault(l => l.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase));
            if (line == null) return null;
            int idx = line.IndexOf('=');
            if (idx < 0) return null;
            return line.Substring(idx + 1).Trim();
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
