using System;
using System.Drawing;
using System.Windows.Forms;
using System.Configuration;
using System.IO;
using Microsoft.Win32;
using System.Reflection;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Collections.Specialized;
using System.ComponentModel;

namespace DesktopClock
{
    public partial class Form1 : Form
    {
        #region Constants

        private const string ConfigKeyWindowPositionX = "WindowPositionX";
        private const string ConfigKeyWindowPositionY = "WindowPositionY";
        private const string ConfigKeyTextColor = "TextColor";
        private const string ConfigKeyFontSize = "FontSize";
        private const string ConfigKeyShowDate = "ShowDate";
        private const string ConfigKeyWindowOpacity = "WindowOpacity";
        private const string ConfigKeyBackgroundColor = "BackgroundColor";
        private const string ConfigKeyShowBackground = "ShowBackground";

        private const string AutostartRegistryKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string AutostartRegistryValueName = "DesktopClock";

        private const string GitHubRepo = "LonoxX/Desktop-Clock";
        private const string GitHubApiUrl = "https://api.github.com/repos/{0}/releases/latest";
        private const string GitHubReleasesUrl = "https://github.com/{0}/releases/latest";
        private const string UserAgent = "Desktop-Clock-Update-Checker";

        private const int DefaultFontSize = 24;
        private const double DefaultBackgroundOpacity = 0.1;
        private static readonly Color DefaultTimeColor = Color.White;
        private static readonly Color DefaultBackgroundColor = Color.Black;
        private static readonly Size DefaultFormSize = new Size(200, 70);
        private static readonly Size LabelPadding = new Size(20, 20);

        #endregion

        #region Fields

        private System.Windows.Forms.Timer? clockTimer;
        private Label? timeLabel;
        private NotifyIcon? trayIcon;
        private ContextMenuStrip? contextMenu;

        private Icon? appIcon;
        private SolidBrush? backgroundBrush;

        private bool moveMode = false;
        private bool isMouseDown = false;
        private Point lastScreenMousePosition;
        private bool showDate = false;
        private bool showBackground = false;
        private Color backgroundColor = DefaultBackgroundColor;
        private double backgroundOpacity = DefaultBackgroundOpacity;

        private readonly Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);
        private string CurrentVersionDisplay => currentVersion.ToString();
        private Version GitHubCompatibleVersion => new Version(currentVersion.Major, currentVersion.Minor, currentVersion.Build);

        private static readonly HttpClient updateClient = new HttpClient();

        #endregion

        #region Properties

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Color TimeColor
        {
            get { return timeLabel?.ForeColor ?? DefaultTimeColor; }
            set
            {
                if (timeLabel != null && timeLabel.ForeColor != value)
                {
                    timeLabel.ForeColor = value;
                }
            }
        }

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int FontSize
        {
            get { return (int)(timeLabel?.Font.Size ?? DefaultFontSize); }
            set
            {
                int validFontSize = Math.Max(8, Math.Min(72, value));
                if (timeLabel != null && (int)timeLabel.Font.Size != validFontSize)
                {
                    timeLabel.Font?.Dispose();
                    timeLabel.Font = new Font("Arial", validFontSize, FontStyle.Bold);
                    UpdateTime();
                }
            }
        }

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool ShowDate
        {
            get { return showDate; }
            set
            {
                if (showDate != value)
                {
                    showDate = value;
                    UpdateTime();
                }
            }
        }

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public double BackgroundOpacity
        {
            get { return backgroundOpacity; }
            set {
                backgroundOpacity = Math.Min(1.0, Math.Max(0.1, value));
                UpdateBackgroundBrush();
                if (!this.Disposing)
                {
            this.Invalidate(true);
            this.Update();
                }
            }
        }

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Color BackgroundColor
        {
            get { return backgroundColor; }
            set {
                backgroundColor = value;
                UpdateBackgroundBrush();
                if (!this.Disposing)
                {
                    this.Invalidate();
                }
            }
        }

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool ShowBackground
        {
            get { return showBackground; }
            set
            {
                if (showBackground != value)
                {
                    showBackground = value;
                    UpdateBackgroundBrush();
                    if (!this.Disposing)
                    {
                        this.Invalidate();
                    }
                }
            }
        }

        #endregion

        #region Initialization and Cleanup

        public Form1()
        {
            InitializeComponent();

            this.SetStyle(ControlStyles.SupportsTransparentBackColor |
                          ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint, true);
            LoadCustomIcon();
            SetupClockUI();
            LoadSettings();
            UpdateBackgroundBrush();
            CreateContextMenu();
            CreateTrayIcon();
            UpdateTime();

            Task.Run(() => CheckForUpdatesAsync(showNoUpdateMessage: false));
        }

        private void LoadCustomIcon()
        {
             if (appIcon != null && appIcon != SystemIcons.Application)
             {
                 appIcon.Dispose();
                 appIcon = null;
             }

            try
            {
                appIcon = SystemIcons.Application;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading icon: {ex.Message}");
                appIcon = SystemIcons.Application;
            }

            if (this.Icon == null && appIcon != null)
            {
                 this.Icon = (appIcon == SystemIcons.Application) ? appIcon : (Icon)appIcon.Clone();
            }
        }

        private void SetupClockUI()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.BackColor = Color.Black;
            this.TransparencyKey = Color.Black;
            this.Size = DefaultFormSize;
            this.Cursor = Cursors.Default;
            this.StartPosition = FormStartPosition.Manual;

            this.timeLabel = this.Controls.Find("timeLabel", true).FirstOrDefault() as Label;
            if (this.timeLabel == null)
            {
                timeLabel = new Label
                {
                    Name = "timeLabel",
                    Font = new Font("Arial", DefaultFontSize, FontStyle.Bold),
                    ForeColor = DefaultTimeColor,
                    BackColor = Color.Transparent,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = false
                };
                this.Controls.Add(timeLabel);
            }

            timeLabel.MouseDown -= FormOrLabel_MouseDown;
            timeLabel.MouseDown += FormOrLabel_MouseDown;
            timeLabel.MouseMove -= FormOrLabel_MouseMove;
            timeLabel.MouseMove += FormOrLabel_MouseMove;
            timeLabel.MouseUp -= FormOrLabel_MouseUp;
            timeLabel.MouseUp += FormOrLabel_MouseUp;
            timeLabel.Click -= TimeLabel_Click;
            timeLabel.Click += TimeLabel_Click;

            if (this.components != null)
            {
                 this.clockTimer = this.components.Components.OfType<System.Windows.Forms.Timer>()
                                        .FirstOrDefault(t => t.Site?.Name == "clockTimer");
            }

            if (this.clockTimer == null)
            {
                this.components ??= new System.ComponentModel.Container();
                clockTimer = new System.Windows.Forms.Timer(this.components)
                {
                     Interval = 1000,
                };
                clockTimer.Tick += Timer_Tick;
                clockTimer.Start();
            }
            else
            {
                 clockTimer.Tick -= Timer_Tick;
                 clockTimer.Tick += Timer_Tick;
                 if (!clockTimer.Enabled) clockTimer.Start();
            }

            this.MouseDown -= FormOrLabel_MouseDown;
            this.MouseDown += FormOrLabel_MouseDown;
            this.MouseMove -= FormOrLabel_MouseMove;
            this.MouseMove += FormOrLabel_MouseMove;
            this.MouseUp -= FormOrLabel_MouseUp;
            this.MouseUp += FormOrLabel_MouseUp;

            this.Paint -= Form1_Paint;
            this.Paint += Form1_Paint;
        }

        private void CreateContextMenu()
        {
            if (contextMenu != null && contextMenu.Site == null)
            {
                contextMenu.Dispose();
                contextMenu = null;
            }

            if (this.components != null)
            {
                 this.contextMenu = this.components.Components.OfType<ContextMenuStrip>()
                                         .FirstOrDefault(c => c.Site?.Name == "contextMenuStrip1");
            }

            if (this.contextMenu == null)
            {
                this.components ??= new System.ComponentModel.Container();
                contextMenu = new ContextMenuStrip(this.components)
                {
                };

                var moveItem = new ToolStripMenuItem("Enable Move")
                {
                     Name = "moveItem",
                     CheckOnClick = true,
                     Checked = this.moveMode
                };
                moveItem.Click += MoveItem_Click;

                var settingsItem = new ToolStripMenuItem("Settings...") { Name = "settingsItem" };
                settingsItem.Click += OpenSettingsForm;

                var checkUpdateItem = new ToolStripMenuItem("Check for Updates...") { Name = "checkUpdateItem" };
                checkUpdateItem.Click += async (s, e) => { await CheckForUpdatesAsync(showNoUpdateMessage: true); };

                var exitItem = new ToolStripMenuItem("Exit") { Name = "exitItem" };
                exitItem.Click += ExitItem_Click;

                contextMenu.Items.AddRange(new ToolStripItem[] {
                    moveItem,
                    settingsItem,
                    new ToolStripSeparator() { Name = "separator1" },
                    checkUpdateItem,
                    new ToolStripSeparator() { Name = "separator2" },
                    exitItem
                });

                 this.ContextMenuStrip = contextMenu;
            }
            else
            {
                 var moveItem = contextMenu.Items.Find("moveItem", true).FirstOrDefault() as ToolStripMenuItem;
                 if (moveItem != null)
                 {
                     moveItem.CheckOnClick = true;
                     moveItem.Checked = this.moveMode;
                     moveItem.Click -= MoveItem_Click;
                     moveItem.Click += MoveItem_Click;
                 }
                 var settingsItem = contextMenu.Items.Find("settingsItem", true).FirstOrDefault();
                 if (settingsItem != null)
                 {
                      settingsItem.Click -= OpenSettingsForm;
                      settingsItem.Click += OpenSettingsForm;
                 }
                 var checkUpdateItem = contextMenu.Items.Find("checkUpdateItem", true).FirstOrDefault();
                 if (checkUpdateItem != null)
                 {
                      checkUpdateItem.Click -= CheckUpdateItem_Click;
                      checkUpdateItem.Click += CheckUpdateItem_Click;
                 }
                 var exitItem = contextMenu.Items.Find("exitItem", true).FirstOrDefault();
                 if (exitItem != null)
                 {
                      exitItem.Click -= ExitItem_Click;
                      exitItem.Click += ExitItem_Click;
                 }
                 this.ContextMenuStrip = contextMenu;
            }

            if (trayIcon != null)
            {
                trayIcon.ContextMenuStrip = this.contextMenu;
            }
        }

        private void MoveItem_Click(object? sender, EventArgs e)
        {
             if (sender is ToolStripMenuItem moveItem)
             {
                 moveMode = moveItem.Checked;
             }
        }
        private async void CheckUpdateItem_Click(object? sender, EventArgs e)
        {
             await CheckForUpdatesAsync(showNoUpdateMessage: true);
        }
        private void ExitItem_Click(object? sender, EventArgs e)
        {
             Application.Exit();
        }

        private void CreateTrayIcon()
        {
             if (trayIcon != null && trayIcon.Site == null)
             {
                 trayIcon.Dispose();
                 trayIcon = null;
             }

             if (this.components != null)
             {
                  this.trayIcon = this.components.Components.OfType<NotifyIcon>()
                                        .FirstOrDefault(n => n.Site?.Name == "notifyIcon1");
             }

             if (this.trayIcon == null)
             {
                this.components ??= new System.ComponentModel.Container();
                trayIcon = new NotifyIcon(this.components)
                {
                     Icon = this.Icon ?? SystemIcons.Application,
                     Visible = true,
                     Text = "Desktop Clock",
                     ContextMenuStrip = this.contextMenu
                 };
                 trayIcon.DoubleClick += OpenSettingsForm;
             }
             else
             {
                  trayIcon.Icon = this.Icon ?? SystemIcons.Application;
                  trayIcon.ContextMenuStrip = this.contextMenu;
                  trayIcon.Visible = true;
                  trayIcon.DoubleClick -= OpenSettingsForm;
                  trayIcon.DoubleClick += OpenSettingsForm;
             }
        }

        #endregion

        #region Time Display Logic

        private void Timer_Tick(object? sender, EventArgs e)
        {
            UpdateTime();
        }

        private void UpdateTime()
        {
            if (timeLabel == null || timeLabel.IsDisposed || this.Disposing) return;

            DateTime localTime = DateTime.Now;
            CultureInfo currentCulture = CultureInfo.CurrentCulture;
            string timeFormat = currentCulture.DateTimeFormat.LongTimePattern;
            string dateFormat = currentCulture.DateTimeFormat.ShortDatePattern;

            string newText = showDate
                ? $"{localTime.ToString(dateFormat)}{Environment.NewLine}{localTime.ToString(timeFormat)}"
                : localTime.ToString(timeFormat);

            if (timeLabel.Text != newText)
            {
                timeLabel.Text = newText;
                UpdateWindowSize();
            }
        }

        #endregion

        #region Movement and Positioning Logic

        private void FormOrLabel_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isMouseDown = true;
                lastScreenMousePosition = MousePosition;

                if (moveMode)
                {
                    this.Cursor = Cursors.SizeAll;
                }
            }
        }

        private void FormOrLabel_MouseMove(object? sender, MouseEventArgs e)
        {
            if (isMouseDown && moveMode)
            {
                Point currentScreenMousePosition = MousePosition;
                int deltaX = currentScreenMousePosition.X - lastScreenMousePosition.X;
                int deltaY = currentScreenMousePosition.Y - lastScreenMousePosition.Y;

                if (!this.Disposing)
                {
                    this.Location = new Point(this.Location.X + deltaX, this.Location.Y + deltaY);
                }

                lastScreenMousePosition = currentScreenMousePosition;
            }
        }

        private void FormOrLabel_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isMouseDown = false;

                if (moveMode)
                {
                    this.Cursor = Cursors.Default;
                    if (!this.Disposing)
                    {
                        SavePosition();
                    }
                }
            }
        }

        private void LoadPosition()
        {
             if (this.Disposing) return;

            try
            {
                NameValueCollection settings = ConfigurationManager.AppSettings;
                string? xStr = settings[ConfigKeyWindowPositionX];
                string? yStr = settings[ConfigKeyWindowPositionY];

                if (int.TryParse(xStr, out int x) && int.TryParse(yStr, out int y))
                {
                    bool positionIsVisible = false;
                    foreach (Screen screen in Screen.AllScreens)
                    {
                        Rectangle formRect = new Rectangle(x, y, Math.Max(this.Width, 50), Math.Max(this.Height, 50));
                        if (screen.WorkingArea.IntersectsWith(formRect))
                        {
                            positionIsVisible = true;
                            break;
                        }
                    }

                    if (positionIsVisible)
                    {
                        this.StartPosition = FormStartPosition.Manual;
                        this.Location = new Point(x, y);
                    }
                    else
                    {
                        CenterWindowOnPrimaryScreen();
                    }
                }
                else
                {
                    CenterWindowOnPrimaryScreen();
                }
            }
            catch (ConfigurationErrorsException ex)
            {
                Debug.WriteLine($"Configuration error loading position: {ex.Message}");
                CenterWindowOnPrimaryScreen();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading position: {ex.Message}");
                CenterWindowOnPrimaryScreen();
            }
        }

        public void SavePosition()
        {
             if (this.Disposing) return;

            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                AppSettingsSection appSettings = config.AppSettings;

                Action<string, string> AddOrUpdateSetting = (key, value) =>
                {
                    if (appSettings.Settings[key] == null)
                        appSettings.Settings.Add(key, value);
                    else
                        appSettings.Settings[key].Value = value;
                };

                AddOrUpdateSetting(ConfigKeyWindowPositionX, this.Location.X.ToString());
                AddOrUpdateSetting(ConfigKeyWindowPositionY, this.Location.Y.ToString());

                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (ConfigurationErrorsException ex)
            {
                Debug.WriteLine($"Configuration error saving position: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving position: {ex.Message}");
            }
        }

        public void ResetPosition()
        {
            CenterWindowOnPrimaryScreen();
            SavePosition();
        }

        private void CenterWindowOnPrimaryScreen()
        {
             if (this.Disposing) return;

            if (Screen.PrimaryScreen != null)
            {
                Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
                int x = workingArea.Left + (workingArea.Width - this.Width) / 2;
                int y = workingArea.Top + (workingArea.Height - this.Height) / 2;
                this.Location = new Point(Math.max(workingArea.Left, x), Math.max(workingArea.Top, y));
            }
            else
            {
                this.StartPosition = FormStartPosition.CenterScreen;
            }
        }

        private void UpdateWindowSize()
        {
            SizeF timeSize = GetTimeSize();

            int padding = 20;
            int newWidth = (int)Math.Max(200, timeSize.Width + padding);
            int newHeight = (int)Math.Max(100, timeSize.Height + padding);

            this.ClientSize = new Size(newWidth, newHeight);

            this.timeLabel.Location = new Point(
                (newWidth - (int)timeSize.Width) / 2,
                (newHeight - (int)timeSize.Height) / 2);
        }

        private SizeF GetTimeSize()
        {
            if (timeLabel == null || timeLabel.Font == null) return new SizeF(200, 50);

            string displayText = timeLabel.Text;

            if (string.IsNullOrEmpty(displayText))
            {
                DateTime now = DateTime.Now;
                displayText = showDate
                    ? $"{now.ToShortDateString()}{Environment.NewLine}{now.ToLongTimeString()}"
                    : now.ToLongTimeString();
            }

            using (Graphics g = this.CreateGraphics())
            {
                return g.MeasureString(displayText, timeLabel.Font);
            }
        }

        #endregion

        #region Settings Logic

        private void LoadSettings()
        {
            LoadPosition();

            try
            {
                NameValueCollection settings = ConfigurationManager.AppSettings;

                if (TryParseColor(settings[ConfigKeyTextColor], out Color textColor))
                {
                    TimeColor = textColor;
                }

                if (int.TryParse(settings[ConfigKeyFontSize], out int fontSize))
                {
                    FontSize = fontSize;
                }

                if (bool.TryParse(settings[ConfigKeyShowDate], out bool showDateValue))
                {
                    ShowDate = showDateValue;
                }

                if (bool.TryParse(settings[ConfigKeyShowBackground], out bool showBackgroundValue))
                {
                    ShowBackground = showBackgroundValue;
                }

                if (double.TryParse(settings[ConfigKeyWindowOpacity], NumberStyles.Any, CultureInfo.InvariantCulture, out double opacityValue))
                {
                    BackgroundOpacity = opacityValue;
                }

                if (TryParseColor(settings[ConfigKeyBackgroundColor], out Color bgColor))
                {
                    backgroundColor = bgColor;
                }
            }
            catch (ConfigurationErrorsException ex)
            {
                Debug.WriteLine($"Configuration error loading settings: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading settings: {ex.Message}");
            }
            finally
            {
                 if (!this.Disposing)
                 {
                    UpdateTime();
                    UpdateBackgroundBrush();
                    this.Invalidate();
                 }
            }
        }

        public void SaveSettings()
        {
             if (this.Disposing) return;

            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                AppSettingsSection appSettings = config.AppSettings;

                Action<string, string> AddOrUpdateSetting = (key, value) =>
                {
                    if (appSettings.Settings[key] == null)
                        appSettings.Settings.Add(key, value);
                    else
                        appSettings.Settings[key].Value = value;
                };

                AddOrUpdateSetting(ConfigKeyTextColor, $"{TimeColor.R},{TimeColor.G},{TimeColor.B}");
                AddOrUpdateSetting(ConfigKeyFontSize, FontSize.ToString());
                AddOrUpdateSetting(ConfigKeyShowDate, ShowDate.ToString());
                AddOrUpdateSetting(ConfigKeyShowBackground, ShowBackground.ToString());
                AddOrUpdateSetting(ConfigKeyBackgroundColor, $"{BackgroundColor.R},{BackgroundColor.G},{BackgroundColor.B}");
                AddOrUpdateSetting(ConfigKeyWindowOpacity, BackgroundOpacity.ToString(CultureInfo.InvariantCulture));

                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (ConfigurationErrorsException ex)
            {
                Debug.WriteLine($"Configuration error saving settings: {ex.Message}");
                MessageBox.Show(this, $"Error saving settings:\n{ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving settings: {ex.Message}");
                MessageBox.Show(this, $"An unexpected error occurred while saving settings:\n{ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void ApplySettings()
        {
            SaveSettings();
             if (!this.Disposing)
             {
                UpdateTime();
                this.Invalidate();
             }
        }

        private bool TryParseColor(string? colorString, out Color color)
        {
            color = Color.Empty;
            if (string.IsNullOrWhiteSpace(colorString)) return false;

            string[] rgb = colorString.Split(',');
            if (rgb.Length == 3 &&
                int.TryParse(rgb[0].Trim(), out int r) &&
                int.TryParse(rgb[1].Trim(), out int g) &&
                int.TryParse(rgb[2].Trim(), out int b) &&
                r >= 0 && r <= 255 && g >= 0 && g <= 255 && b >= 0 && b <= 255)
            {
                color = Color.FromArgb(r, g, b);
                return true;
            }
            return false;
        }

        private void OpenSettingsForm(object? sender, EventArgs e)
        {
             if (this.Disposing) return;

            var existingSettingsForm = Application.OpenForms.OfType<SettingsForm>().FirstOrDefault();
            if (existingSettingsForm != null)
            {
                existingSettingsForm.Activate();
                return;
            }

            using (var settingsForm = new SettingsForm(this))
            {
                Point idealPosition = CalculateSettingsFormPosition(settingsForm);

                settingsForm.StartPosition = FormStartPosition.Manual;
                settingsForm.Location = idealPosition;

                settingsForm.ShowDialog(this);
            }
        }

        private Point CalculateSettingsFormPosition(Form settingsForm)
        {
             if (this.Disposing) return Point.Empty;

            Rectangle screenBounds = Screen.FromControl(this).WorkingArea;
            int margin = 10;

            Point position = new Point(this.Right + margin, this.Top);

            if (position.X + settingsForm.Width > screenBounds.Right)
            {
                position.X = this.Left - settingsForm.Width - margin;

                if (position.X < screenBounds.Left)
                {
                    position.X = this.Left + (this.Width - settingsForm.Width) / 2;
                    position.Y = this.Bottom + margin;

                    if (position.Y + settingsForm.Height > screenBounds.Bottom)
                    {
                        position.Y = this.Top - settingsForm.Height - margin;

                        if (position.Y < screenBounds.Top)
                        {
                            position.X = this.Right + margin;
                            position.Y = this.Top;
                            position.X = Math.Max(screenBounds.Left, Math.Min(position.X, screenBounds.Right - settingsForm.Width));
                            position.Y = Math.Max(screenBounds.Top, Math.Min(position.Y, screenBounds.Bottom - settingsForm.Height));
                        }
                    }
                    position.X = Math.Max(screenBounds.Left, Math.Min(position.X, screenBounds.Right - settingsForm.Width));
                }
            }
            position.Y = Math.Max(screenBounds.Top, Math.Min(position.Y, screenBounds.Bottom - settingsForm.Height));

            return position;
        }

        #endregion

        #region Autostart Management

        public bool IsInAutostart()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(AutostartRegistryKey, false))
                {
                    return key?.GetValue(AutostartRegistryValueName) != null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking autostart status: {ex.Message}");
                return false;
            }
        }

        public void SetAutostart(bool enable)
        {
            try
            {
                string? exePath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exePath))
                {
                    exePath = Assembly.GetExecutingAssembly().Location;
                    if (string.IsNullOrEmpty(exePath))
                    {
                       throw new InvalidOperationException("Could not determine application path.");
                    }
                }

                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(AutostartRegistryKey, true))
                {
                    if (key == null)
                    {
                        throw new InvalidOperationException("Could not open or create autostart registry key. Check permissions.");
                    }
                    else
                    {
                         SetRegistryValue(key, enable, exePath);
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                 Debug.WriteLine($"Access error changing autostart status: {ex.Message}");
                 MessageBox.Show(this, "Error: Insufficient permissions to change autostart settings.",
                                 "Permission Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error changing autostart status: {ex.Message}");
                MessageBox.Show(this, $"An unexpected error occurred while changing autostart status:\n{ex.Message}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetRegistryValue(RegistryKey key, bool enable, string exePath)
        {
             if (enable)
             {
                 key.SetValue(AutostartRegistryValueName, $"\"{exePath}\"");
             }
             else
             {
                 try
                 {
                    key.DeleteValue(AutostartRegistryValueName, false);
                 }
                 catch(ArgumentException)
                 {
                 }
             }
        }

        #endregion

        #region Update Checking Logic

        private async Task CheckForUpdatesAsync(bool showNoUpdateMessage = true)
        {
             if (this.Disposing) return;

            Action<Action> safeInvoke = (action) => {
                 if (!this.Disposing && this.IsHandleCreated)
                 {
                     try
                     {
                        this.Invoke(action);
                     }
                     catch (ObjectDisposedException) { }
                     catch (InvalidOperationException) { }
                 }
            };

            try
            {
                Version? latestVersion = await GetLatestReleaseVersionAsync();

                if (latestVersion != null)
                {
                    Version localVersionForComparison = GitHubCompatibleVersion;

                    if (latestVersion > localVersionForComparison)
                    {
                        safeInvoke(() => ShowUpdateNotification(latestVersion));
                    }
                    else if (showNoUpdateMessage)
                    {
                         safeInvoke(() =>
                            MessageBox.Show(this,
                                $"You are already using the latest version ({CurrentVersionDisplay}).",
                                "No Updates Available",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information)
                         );
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                 Debug.WriteLine($"Network error during update check: {ex.Message}");
                 if (showNoUpdateMessage)
                 {
                     safeInvoke(() =>
                        MessageBox.Show(this,
                            $"Error checking for updates: Could not connect to server.\n\nDetails: {ex.Message}",
                            "Network Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                     );
                 }
            }
            catch (JsonException ex)
            {
                 Debug.WriteLine($"JSON error during update check: {ex.Message}");
                 if (showNoUpdateMessage)
                 {
                      safeInvoke(() =>
                        MessageBox.Show(this,
                            $"Error checking for updates: Could not process server response.\n\nDetails: {ex.Message}",
                            "Processing Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                      );
                 }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"General error during update check: {ex.Message}");
                if (showNoUpdateMessage)
                {
                     safeInvoke(() =>
                        MessageBox.Show(this,
                            $"An unexpected error occurred during update check:\n{ex.Message}",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                     );
                }
            }
        }

        private async Task<Version?> GetLatestReleaseVersionAsync()
        {
            if (updateClient.DefaultRequestHeaders.UserAgent.Count == 0)
            {
                updateClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            }

            string url = string.Format(GitHubApiUrl, GitHubRepo);

            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
                {
                     HttpResponseMessage response = await updateClient.GetAsync(url, cts.Token);
                     response.EnsureSuccessStatusCode();

                     string jsonResponse = await response.Content.ReadAsStringAsync();

                     using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                     {
                         if (doc.RootElement.TryGetProperty("tag_name", out JsonElement tagNameElement) &&
                             tagNameElement.ValueKind == JsonValueKind.String)
                         {
                             string? tagName = tagNameElement.GetString();

                             if (!string.IsNullOrEmpty(tagName) && (tagName.StartsWith("v", StringComparison.OrdinalIgnoreCase)))
                             {
                                 tagName = tagName.Substring(1);
                             }

                             if (Version.TryParse(tagName, out Version? version))
                             {
                                 return version;
                             }
                             else
                             {
                                  Debug.WriteLine($"Invalid version format in tag: {tagName}");
                             }
                         }
                         else
                         {
                              Debug.WriteLine("Field 'tag_name' not found in GitHub API response.");
                         }
                     }
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || ex.CancellationToken.IsCancellationRequested)
            {
                 Debug.WriteLine($"Timeout or cancellation during update check: {ex.Message}");
                 throw new HttpRequestException("Timeout or cancellation during update check.", ex);
            }
            catch (HttpRequestException ex)
            {
                 Debug.WriteLine($"Error retrieving latest version from GitHub: {ex.StatusCode} - {ex.Message}");
                 throw;
            }
            catch (JsonException ex)
            {
                 Debug.WriteLine($"Error parsing GitHub API response: {ex.Message}");
                 throw;
            }
            catch (Exception ex)
            {
                 Debug.WriteLine($"Unexpected error retrieving latest version: {ex.Message}");
                 throw;
            }

            return null;
        }

        private void ShowUpdateNotification(Version newVersion)
        {
             if (this.Disposing) return;

            var result = MessageBox.Show(this,
                $"A new version ({newVersion}) is available.\n" +
                $"Your current version is {CurrentVersionDisplay}.\n\n" +
                "Would you like to open the download page now?",
                "Update Available",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result == DialogResult.Yes)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = string.Format(GitHubReleasesUrl, GitHubRepo),
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error opening browser: {ex.Message}");
                    MessageBox.Show(this,
                        $"Error opening download page:\n{ex.Message}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        #endregion

        #region Form Event Handlers

        private void Form1_Paint(object? sender, PaintEventArgs e)
        {
            if (ShowBackground && backgroundBrush != null)
            {
                e.Graphics.FillRectangle(backgroundBrush, this.ClientRectangle);
            }
        }

        private void UpdateBackgroundBrush()
        {
            backgroundBrush?.Dispose();
            backgroundBrush = null;

            if (ShowBackground)
            {
                int alpha = (int)(255 * Math.Min(1.0, Math.Max(0.1, backgroundOpacity)));

                backgroundBrush = new SolidBrush(Color.FromArgb(alpha, backgroundColor));

                this.FormBorderStyle = FormBorderStyle.None;
                this.BackColor = backgroundColor;
                this.TransparencyKey = Color.Empty;
                this.Opacity = backgroundOpacity;

                this.Invalidate(true);
                this.Update();
            }
            else
            {
                // When background is disabled, make form transparent, only text remains visible
                this.BackColor = Color.Black;
                this.TransparencyKey = Color.Black;
                this.Opacity = 1.0; // Reset opacity

                if (timeLabel != null)
                {
                    timeLabel.BackColor = Color.Transparent;
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
             if (e.CloseReason == CloseReason.UserClosing || e.CloseReason == CloseReason.ApplicationExitCall)
             {
                 SavePosition();
                 SaveSettings();
             }

            if (clockTimer != null && clockTimer.Site == null)
            {
                 clockTimer.Stop();
                 clockTimer.Dispose();
                 clockTimer = null;
            }

            if (trayIcon != null && trayIcon.Site == null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
                trayIcon = null;
            }

             if (contextMenu != null && contextMenu.Site == null)
             {
                 contextMenu.Dispose();
                 contextMenu = null;
             }

             if (appIcon != null && appIcon != SystemIcons.Application)
             {
                 appIcon.Dispose();
             }
             appIcon = null;

             backgroundBrush?.Dispose();
             backgroundBrush = null;

            base.OnFormClosing(e);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }

        private void TimeLabel_Click(object? sender, EventArgs e)
        {
            if (((MouseEventArgs)e).Button == MouseButtons.Right)
                return;

            if (moveMode)
                return;

            ShowDate = !ShowDate;
            ApplySettings();
        }

        #endregion
    }
}
