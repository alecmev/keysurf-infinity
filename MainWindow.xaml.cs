using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using Microsoft.Win32;

namespace BrightCerulean.KeySurfInfinity
{
    public partial class MainWindow : Window
    {
        private delegate void AppendStatusDelegate(string text, bool newLine);

        private static MainWindow _instance;
        public static MainWindow Instance
        {
            get { return _instance; }
        }

        private Worker _worker = new Worker();
        private Thread _workerThread;

        private NotifyIcon _tray = new NotifyIcon();

        private string _username = "";
        public string Username
        {
            get { return _username; }
        }

        private string _password = "";
        public string Password
        {
            get { return _password; }
        }

        private string _filePath;

        public MainWindow()
        {
            _instance = this;

            InitializeComponent();

            _filePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "KeySurfInfinity.txt");
            FileStream tmpStream = new FileStream(_filePath, FileMode.OpenOrCreate);
            StreamReader tmpReader = new StreamReader(tmpStream);
            string tmpString = tmpReader.ReadToEnd().Trim();
            string[] tmpLines = tmpString.Split('\n');

            if (tmpLines.Length == 2)
            {
                _username = tmpLines[0].Trim();
                username.Text = _username;
                _password = tmpLines[1].Trim();
                password.Password = _password;
            }

            tmpStream.Close();

            SystemEvents.PowerModeChanged += PowerModeChanged;
            _tray.Icon = new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream("BrightCerulean.KeySurfInfinity.KeySurfInfinity.ico"));
            _tray.MouseDoubleClick += TrayMouseDoubleClick;
            var tmpMenuItem = new System.Windows.Forms.MenuItem();
            tmpMenuItem.Index = 0;
            tmpMenuItem.Text = "Quit";
            tmpMenuItem.Click += new EventHandler(QuitClick);
            _tray.ContextMenu = new System.Windows.Forms.ContextMenu(new System.Windows.Forms.MenuItem[] { tmpMenuItem });
            _workerThread = new Thread(_worker.Work) { IsBackground = true };
            _workerThread.Start();
        }

        private void QuitClick(object sender, EventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume)
                Reload();
        }

        private void Reload()
        {
            _worker.Stop();
            _worker = new Worker();
            _workerThread = new Thread(_worker.Work) { IsBackground = true };
            _workerThread.Start();
        }

        private void AppendStatus(string text, bool newLine)
        {
            while (status.Text.Length > 2048)
            {
                var newLineIndex = status.Text.IndexOf('\n');
                status.Text = status.Text.Remove(0, newLineIndex != -1 ? newLineIndex + 1 : status.Text.Length);
            }

            var tmpTime = "[" + DateTime.Now.ToString("HH:mm:ss") + "] ";
            status.Text += (status.Text.Length > 0 ? (newLine ? "\n" + tmpTime : " - ") : "" + tmpTime) + text;

            if (!status.IsMouseOver)
                status.ScrollToEnd();
        }

        public void AppendStatusSafe(string text, bool newLine = true)
        {
            Dispatcher.BeginInvoke(
                new AppendStatusDelegate(AppendStatus),
                DispatcherPriority.Normal,
                new object[] { text, newLine });
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _workerThread.Abort();
        }

        private void TrayMouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Normal;
        }

        private void WindowStateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == System.Windows.WindowState.Minimized)
                this.ShowInTaskbar = false;
            else if (this.WindowState == System.Windows.WindowState.Normal)
                this.ShowInTaskbar = true;
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            _tray.Visible = true;
            if (_username != "" && _password != "")
                this.WindowState = System.Windows.WindowState.Minimized;
        }

        private void PingClick(object sender, RoutedEventArgs e)
        {
            Reload();
        }

        private void SaveClick(object sender, RoutedEventArgs e)
        {
            _username = username.Text;
            _password = password.Password;

            FileStream tmpStream = new FileStream(_filePath, FileMode.Create);
            StreamWriter tmpWriter = new StreamWriter(tmpStream);
            tmpWriter.WriteLine(_username);
            tmpWriter.Write(_password);
            tmpWriter.Close();
            tmpStream.Close();

            Reload();
        }
    }
}
