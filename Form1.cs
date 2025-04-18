using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using System.Configuration;
using System.IO;
using Microsoft.Win32;
using System.Reflection;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;

namespace DesktopUhr
{
    public partial class Form1 : Form
    {
        private System.Windows.Forms.Timer clockTimer;
        private Label timeLabel;
        private bool moveMode = false;
        private bool isMouseDown = false;
        private Point lastMousePosition;
        private NotifyIcon trayIcon;
        private Icon appIcon;

        // Aktuelle Version der App
        private readonly Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
        private string currentVersionDisplay => currentVersion.ToString(currentVersion.Build == 0 ? 3 : 4);
        private const string GITHUB_REPO = "LonoxX/Desktop-Clock";

        public Form1()
        {
            InitializeComponent();

            // Load the custom icon
            LoadCustomIcon();

            // Set the form icon
            if (appIcon != null)
            {
                this.Icon = appIcon;
            }

            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.BackColor = Color.Black;
            this.Size = new Size(200, 70);
            this.Opacity = 0.8;
            this.Cursor = Cursors.Default;

            this.StartPosition = FormStartPosition.CenterScreen;

            LoadPosition();

            timeLabel = new Label
            {
                Font = new Font("Arial", 24, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false
            };
            this.Controls.Add(timeLabel);

            clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            clockTimer.Tick += Timer_Tick;
            clockTimer.Start();

            this.MouseDown += new MouseEventHandler(Form1_MouseDown);
            this.MouseMove += new MouseEventHandler(Form1_MouseMove);
            this.MouseUp += new MouseEventHandler(Form1_MouseUp);

            timeLabel.MouseDown += new MouseEventHandler(Form1_MouseDown);
            timeLabel.MouseMove += new MouseEventHandler(Form1_MouseMove);
            timeLabel.MouseUp += new MouseEventHandler(Form1_MouseUp);

            CreateContextMenu();
            CreateTrayIcon();

            UpdateTime();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateTime();
        }

        private void UpdateTime()
        {
            DateTime localTime = DateTime.Now;
            string dateFormat = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
            string timeFormat = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern;

            timeLabel.Text = localTime.ToString(dateFormat + "\n" + timeFormat);
            this.Size = timeLabel.PreferredSize + new Size(20, 20);
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && moveMode)
            {
                isMouseDown = true;
                lastMousePosition = e.Location;
            }
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMouseDown && moveMode)
            {
                Point newLocation;
                if (sender is Label)
                {
                    newLocation = new Point(
                        this.Location.X + e.X,
                        this.Location.Y + e.Y
                    );
                }
                else
                {
                    newLocation = new Point(
                        this.Location.X + e.X - lastMousePosition.X,
                        this.Location.Y + e.Y - lastMousePosition.Y
                    );
                }

                this.Location = newLocation;
            }
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isMouseDown = false;

                if (moveMode)
                {
                    SavePosition();
                }
            }
        }

        private void CreateContextMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();

            var moveItem = new ToolStripMenuItem("Enable Moving");
            moveItem.Click += (s, e) =>
            {
                moveMode = !moveMode;
                moveItem.Text = moveMode ? "Disable Moving" : "Enable Moving";
                this.Cursor = moveMode ? Cursors.SizeAll : Cursors.Default;
            };

            var colorItem = new ToolStripMenuItem("Change Color");
            colorItem.Click += (s, e) =>
            {
                ColorDialog colorDialog = new ColorDialog();
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    timeLabel.ForeColor = colorDialog.Color;
                }
            };

            var fontSizeItem = new ToolStripMenuItem("Change Font Size");
            fontSizeItem.Click += (s, e) =>
            {
                string input = Interaction.InputBox("New font size:", "Font Size", timeLabel.Font.Size.ToString());
                if (int.TryParse(input, out int size))
                {
                    timeLabel.Font = new Font("Arial", size, FontStyle.Bold);
                    UpdateTime();
                }
            };

            var savePositionItem = new ToolStripMenuItem("Save Position");
            savePositionItem.Click += (s, e) =>
            {
                SavePosition();
                MessageBox.Show("The current position has been saved.", "Position Saved",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            bool isAutostart = IsInAutostart();
            var autostartItem = new ToolStripMenuItem(isAutostart ? "Disable Autostart" : "Enable Autostart");
            autostartItem.Click += (s, e) =>
            {
                bool newStatus = !IsInAutostart();
                SetAutostart(newStatus);
                autostartItem.Text = newStatus ? "Disable Autostart" : "Enable Autostart";

                string message = newStatus
                    ? "The clock will now start automatically with Windows."
                    : "The clock will no longer start automatically with Windows.";

                MessageBox.Show(message, "Autostart", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => Application.Exit();
            var checkUpdateItem = new ToolStripMenuItem("Check for Updates");
            checkUpdateItem.Click += async (s, e) =>
            {
                await Task.Run(() => CheckForUpdatesAsync(showNoUpdateMessage: true));
            };

            menu.Items.Add(moveItem);
            menu.Items.Add(colorItem);
            menu.Items.Add(fontSizeItem);
            menu.Items.Add(savePositionItem);
            menu.Items.Add(autostartItem);
            menu.Items.Add(checkUpdateItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exitItem);

            this.ContextMenuStrip = menu;
        }

        private void CreateTrayIcon()
        {
            trayIcon = new NotifyIcon
            {
                Icon = appIcon ?? SystemIcons.Application,
                Visible = true,
                Text = "Desktop Clock",
                ContextMenuStrip = this.ContextMenuStrip
            };

            trayIcon.DoubleClick += (s, e) =>
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
            };
        }

        private void LoadCustomIcon()
        {
            try
            {
                string iconPath = Path.Combine(AppContext.BaseDirectory, "Resources", "clock.ico");

                if (File.Exists(iconPath))
                {
                    appIcon = new Icon(iconPath);
                    Console.WriteLine("Custom icon loaded successfully from: " + iconPath);
                }
                else
                {
                    Console.WriteLine("Icon file not found at: " + iconPath);
                    appIcon = SystemIcons.Application; // Just use default system icon as fallback
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading custom icon: " + ex.Message);
                appIcon = SystemIcons.Application; // Just use default system icon as fallback
            }
        }

        private void LoadPosition()
        {
            try
            {
                int x = Convert.ToInt32(ConfigurationManager.AppSettings["WindowPositionX"]);
                int y = Convert.ToInt32(ConfigurationManager.AppSettings["WindowPositionY"]);

                Console.WriteLine($"Trying to load position: X={x}, Y={y}");

                if (x >= 0 && y >= 0)
                {
                    Rectangle screenBounds = Screen.GetBounds(Point.Empty);
                    if (x < screenBounds.Width - 50 && y < screenBounds.Height - 50)
                    {
                        this.StartPosition = FormStartPosition.Manual;
                        this.Location = new Point(x, y);
                        Console.WriteLine($"Position loaded: X={x}, Y={y}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading position: " + ex.Message);
            }
        }

        private void SavePosition()
        {
            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                if (config.AppSettings.Settings["WindowPositionX"] != null)
                {
                    config.AppSettings.Settings["WindowPositionX"].Value = this.Location.X.ToString();
                }
                else
                {
                    config.AppSettings.Settings.Add("WindowPositionX", this.Location.X.ToString());
                }

                if (config.AppSettings.Settings["WindowPositionY"] != null)
                {
                    config.AppSettings.Settings["WindowPositionY"].Value = this.Location.Y.ToString();
                }
                else
                {
                    config.AppSettings.Settings.Add("WindowPositionY", this.Location.Y.ToString());
                }

                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");

                Console.WriteLine($"Position saved: X={this.Location.X}, Y={this.Location.Y}");
                Console.WriteLine($"Configuration file: {config.FilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving position: " + ex.Message);
            }
        }

        private bool IsInAutostart()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false))
                {
                    return key?.GetValue("DesktopUhr") != null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error checking autostart status: " + ex.Message);
                return false;
            }
        }

        private void SetAutostart(bool enable)
        {
            try
            {
                string exePath;

                if (Environment.ProcessPath != null)
                {
                    exePath = Environment.ProcessPath;
                }
                else
                {
                    exePath = Path.Combine(AppContext.BaseDirectory, "DesktopUhr.exe");
                }

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            key.SetValue("DesktopUhr", $"\"{exePath}\"");
                        }
                        else
                        {
                            key.DeleteValue("DesktopUhr", false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error changing autostart status: " + ex.Message);
                MessageBox.Show($"Error changing autostart status: {ex.Message}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SavePosition();

            if (trayIcon != null)
            {
                trayIcon.Visible = false;
            }

            base.OnFormClosing(e);
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);

            SavePosition();
        }

        // Prüft auf Updates beim Start der App
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Task.Run(() => CheckForUpdatesAsync(showNoUpdateMessage: false));
        }

        // Prüft auf Updates von GitHub
        private async Task CheckForUpdatesAsync(bool showNoUpdateMessage = true)
        {
            try
            {
                var latestVersion = await GetLatestReleaseVersionAsync();
                if (latestVersion != null && latestVersion > currentVersion)
                {
                    // Auf UI-Thread wechseln für Benachrichtigung
                    this.Invoke((System.Windows.Forms.MethodInvoker)(() =>
                    {
                        ShowUpdateNotification(latestVersion);
                    }));
                }
                else if (showNoUpdateMessage)
                {
                    this.Invoke((System.Windows.Forms.MethodInvoker)(() =>
                    {
                        MessageBox.Show($"Sie haben bereits die neueste Version ({currentVersion}).",
                            "Keine Updates verfügbar", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Prüfen auf Updates: {ex.Message}");
                if (showNoUpdateMessage)
                {
                    this.Invoke((System.Windows.Forms.MethodInvoker)(() =>
                    {
                        MessageBox.Show($"Fehler beim Prüfen auf Updates: {ex.Message}",
                            "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
            }
        }

        // Ruft die neueste Version von GitHub ab
        private async Task<Version> GetLatestReleaseVersionAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                // GitHub API benötigt einen User-Agent Header
                client.DefaultRequestHeaders.Add("User-Agent", "Desktop-Clock-Update-Checker");

                string url = $"https://api.github.com/repos/{GITHUB_REPO}/releases/latest";
                var response = await client.GetStringAsync(url);

                using (JsonDocument doc = JsonDocument.Parse(response))
                {
                    string tagName = doc.RootElement.GetProperty("tag_name").GetString();

                    // "v1.0.0" zu "1.0.0" konvertieren
                    if (tagName.StartsWith("v"))
                    {
                        tagName = tagName.Substring(1);
                    }

                    return new Version(tagName);
                }
            }
        }

        // Zeigt eine Update-Benachrichtigung an
        private void ShowUpdateNotification(Version newVersion)
        {
            var result = MessageBox.Show(
                $"Eine neue Version ({newVersion}) ist verfügbar. Ihre aktuelle Version ist {currentVersionDisplay}.\n\n" +
                "Möchten Sie die neue Version herunterladen?",
                "Update verfügbar",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result == DialogResult.Yes)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"https://github.com/{GITHUB_REPO}/releases/latest",
                    UseShellExecute = true
                });
            }
        }
    }
}
