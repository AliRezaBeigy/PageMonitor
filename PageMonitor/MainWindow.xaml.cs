using System;
using System.IO;
using System.Windows;
using System.Threading;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;

namespace PageMonitor
{
    public partial class MainWindow : Window
    {
        public Rect ChatBoxRect { get; }
        public Rect PageNumberRect { get; }
        private string? CurrentPage { get; set; }
        private System.Timers.Timer Timer { get; }


        public MainWindow(Rect pageNumberRect, Rect chatBoxRect)
        {
            InitializeComponent();
            ChatBoxRect = chatBoxRect;
            PageNumberRect = pageNumberRect;

            Timer = new System.Timers.Timer();
            Timer.Elapsed += new System.Timers.ElapsedEventHandler((_, _) => CaptureJob());
            Timer.Interval = 1000;

            UpdateImage(Capture(pageNumberRect), pageNumberImage);
            UpdateImage(Capture(ChatBoxRect), chatBoxImage);
        }

        private void minimuzeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Owner.Close();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void UpdateImage(System.Drawing.Bitmap bitmap, Image image)
        {
            using var memory = new MemoryStream();

            bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
            memory.Position = 0;

            var bitmapimage = new BitmapImage();
            bitmapimage.BeginInit();
            bitmapimage.StreamSource = memory;
            bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapimage.EndInit();

            image.Source = bitmapimage;
        }

        public System.Drawing.Bitmap Capture(Rect rect)
        {
            var rectangle = new System.Drawing.Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);

            var result = new System.Drawing.Bitmap(rectangle.Width, rectangle.Height);

            using (var g = System.Drawing.Graphics.FromImage(result))
                g.CopyFromScreen(new System.Drawing.Point(rectangle.Left, rectangle.Top), System.Drawing.Point.Empty, rectangle.Size);

            return result;
        }

        private void reselectButton_Click(object sender, RoutedEventArgs e)
        {
            ((SelectArea)Owner).ResetState();
            Owner.Show();
            Close();
        }

        private void CaptureJob()
        {
            var fileName = Path.GetTempPath() + "page_monitor.png";

            using (var fileStream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                System.Drawing.Bitmap bitmap = Capture(PageNumberRect);
                bitmap.Save(fileStream, System.Drawing.Imaging.ImageFormat.Png);
                Dispatcher.BeginInvoke(new ThreadStart(() => UpdateImage(bitmap, pageNumberImage)));
            }

            var processStartInfo = new ProcessStartInfo()
            {
                WorkingDirectory = Path.Join(Environment.CurrentDirectory, "Tesseract"),
                FileName = Path.Join(Environment.CurrentDirectory, "Tesseract", "tesseract.exe"),
                Arguments = $"--psm 6 {fileName} stdout nobatch",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                var proc = Process.Start(processStartInfo);
                var pageNumber = "";
                while (!proc?.StandardOutput.EndOfStream ?? false)
                {
                    pageNumber = proc?.StandardOutput.ReadLine()?.Trim();
                }

                Dispatcher.BeginInvoke(new ThreadStart(() =>
                {
                    statusText.Text = $"Current Page: {pageNumber}";
                }));

                if(!string.IsNullOrWhiteSpace(pageNumber))
                {
                    if (pageNumber != CurrentPage)
                        SendPageNumberToChat(pageNumber);
                    CurrentPage = pageNumber;
                }
            } catch { }
        }

        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int smIndex);

        private void SendPageNumberToChat(string? pageNumber)
        {
            var x = ChatBoxRect.X + (ChatBoxRect.Width / 2);
            var y = ChatBoxRect.Y + (ChatBoxRect.Height / 2);

            var simulator = new GregsStack.InputSimulatorStandard.InputSimulator();
            simulator.Mouse.MoveMouseToPositionOnVirtualDesktop((x * 65536) / GetSystemMetrics(0), (y * 65536) / GetSystemMetrics(1));

            Thread.Sleep(500);

            simulator.Mouse.LeftButtonClick();

            for (int i = 0; i < 10; i++)
                simulator.Keyboard.KeyPress(GregsStack.InputSimulatorStandard.Native.VirtualKeyCode.BACK);

            simulator.Keyboard.TextEntry(pageNumber);
            simulator.Keyboard.KeyPress(GregsStack.InputSimulatorStandard.Native.VirtualKeyCode.RETURN);
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            switch(startButton.Content)
            {
                case "Start":
                    Timer.Enabled = true;
                    startButton.Content = "Stop";
                    break;
                case "Stop":
                    Timer.Enabled = false;
                    startButton.Content = "Start";
                    break;
            }
        }
    }
}
