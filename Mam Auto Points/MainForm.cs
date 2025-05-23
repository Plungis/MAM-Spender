using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.IO;
using System.Text.Json;

namespace MAMAutoPoints
{
    public class MainForm : Form
    {
        private const int ContentWidth = 760;

        // UI Controls
        private TextBox textBoxLog = null!;
        private TextBox textBoxPointsBuffer = null!;
        private CheckBox checkBoxBuyVip = null!;
        private TextBox textBoxNextRun = null!;
        private Label labelTotalGB = null!;
        private Label labelCumulativePointsValue = null!;
        private Label labelNextRunCountdown = null!;
        private TextBox textBoxCookieFile = null!;
        private Button buttonBrowseCookie = null!;
        private Button buttonEditCookie = null!;
        private Button buttonCreateCookie = null!;
        private Button buttonRun = null!;
        private Button buttonPause = null!;
        private Button buttonExit = null!;
        private Button buttonHelpCookie = null!;
        private System.Windows.Forms.Timer timerCountdown = null!;
        private DateTime? nextRunTime = null;
        private int cumulativePointsSpent = 0;
        private int cumulativeUploadGB = 0;
        private bool automationRunning = false;
        private bool paused = false;

        private NotifyIcon notifyIcon = null!;
        private bool enableMinimizeToTray = true;

        // Toggles
        private CheckBox checkBoxStartWithWindows = null!;
        private CheckBox checkBoxMinimizeTray = null!;
        private CheckBox errorNotificationCheckBox = null!;
        private bool sendErrorNotifications = false;

        // Config persistence
        private readonly string _configPath = Path.Combine(Path.GetTempPath(), "MAMAutoPointsConfig.json");
        private AppConfig _config = new AppConfig();
        private class AppConfig
        {
            public bool SendErrorNotifications { get; set; }
            public bool StartWithWindows { get; set; }
            public bool MinimizeToTray { get; set; }
            public string CookieFilePath { get; set; } = string.Empty;
        }

        // Layout containers
        private Panel panelContent = null!;
        private TableLayoutPanel tableLayoutMain = null!;
        private GroupBox groupBoxUserInfo = null!;
        private GroupBox groupBoxSettings = null!;
        private GroupBox groupBoxTotals = null!;
        private GroupBox groupBoxSystemSettings = null!;
        private GroupBox groupBoxCookieSettings = null!;
        private GroupBox groupBoxAppControls = null!;

        // User info labels
        private Label labelUserName = null!;
        private Label labelVipExpires = null!;
        private Label labelDownloaded = null!;
        private Label labelUploaded = null!;
        private Label labelRatio = null!;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // Form properties
            this.MinimumSize = new Size(875, 750);
            this.Size = new Size(875, 750);
            this.Text = "MAM Auto Points";
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.AutoScroll = true;

            // Container panel
            panelContent = new Panel
            {
                Width = ContentWidth,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            this.Controls.Add(panelContent);

            // Log textbox
            textBoxLog = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 10),
                BackColor = Color.Black,
                ForeColor = Color.White,
                Width = ContentWidth,
                Height = 150,
                Location = new Point(0, 10)
            };
            panelContent.Controls.Add(textBoxLog);

            // Main layout
            tableLayoutMain = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 4,
                AutoSize = true,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Location = new Point(0, textBoxLog.Bottom + 10),
                Width = ContentWidth
            };
            tableLayoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 160));
            tableLayoutMain.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tableLayoutMain.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tableLayoutMain.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panelContent.Controls.Add(tableLayoutMain);

            // Row 0: User Information
            groupBoxUserInfo = new GroupBox
            {
                Text = "User Information",
                AutoSize = false,
                Width = ContentWidth,
                Height = 160,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White
            };
            // Username
            var lblUserNameTitle = new Label
            {
                Text = "Username:",
                Location = new Point(10, 25),
                AutoSize = true,
                ForeColor = Color.LightBlue
            };
            groupBoxUserInfo.Controls.Add(lblUserNameTitle);
            labelUserName = new Label
            {
                Text = "N/A",
                Location = new Point(100, 25),
                AutoSize = true,
                ForeColor = Color.LightBlue
            };
            groupBoxUserInfo.Controls.Add(labelUserName);
            // VIP Expires
            var lblVipExpiresTitle = new Label
            {
                Text = "VIP Expires:",
                Location = new Point(10, 50),
                AutoSize = true,
                ForeColor = Color.LightGreen
            };
            groupBoxUserInfo.Controls.Add(lblVipExpiresTitle);
            labelVipExpires = new Label
            {
                Text = "N/A",
                Location = new Point(100, 50),
                AutoSize = true,
                ForeColor = Color.LightGreen
            };
            groupBoxUserInfo.Controls.Add(labelVipExpires);
            // Downloaded
            var lblDownloadedTitle = new Label
            {
                Text = "Downloaded:",
                Location = new Point(10, 75),
                AutoSize = true,
                ForeColor = Color.LightCoral
            };
            groupBoxUserInfo.Controls.Add(lblDownloadedTitle);
            labelDownloaded = new Label
            {
                Text = "N/A",
                Location = new Point(100, 75),
                AutoSize = true,
                ForeColor = Color.LightCoral
            };
            groupBoxUserInfo.Controls.Add(labelDownloaded);
            // Uploaded
            var lblUploadedTitle = new Label
            {
                Text = "Uploaded:",
                Location = new Point(380, 25),
                AutoSize = true,
                ForeColor = Color.LightCoral
            };
            groupBoxUserInfo.Controls.Add(lblUploadedTitle);
            labelUploaded = new Label
            {
                Text = "N/A",
                Location = new Point(480, 25),
                AutoSize = true,
                ForeColor = Color.LightCoral
            };
            groupBoxUserInfo.Controls.Add(labelUploaded);
            // Ratio
            var lblRatioTitle = new Label
            {
                Text = "Ratio:",
                Location = new Point(380, 50),
                AutoSize = true,
                ForeColor = Color.Plum
            };
            groupBoxUserInfo.Controls.Add(lblRatioTitle);
            labelRatio = new Label
            {
                Text = "N/A",
                Location = new Point(480, 50),
                AutoSize = true,
                ForeColor = Color.Plum
            };
            groupBoxUserInfo.Controls.Add(labelRatio);
            // Lotto button
            var btnLotto = new Button
            {
                Text = "Play MAM Lotto",
                Size = new Size(140, 30),
                Location = new Point(10, 105),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.CornflowerBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnLotto.Click += (s, e) =>
                Process.Start(new ProcessStartInfo("https://www.myanonamouse.net/play_lotto.php") { UseShellExecute = true });
            groupBoxUserInfo.Controls.Add(btnLotto);
            // Donate button
            var btnDonate = new Button
            {
                Text = "Millionaires Club",
                Size = new Size(160, 30),
                Location = new Point(160, 105),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.CornflowerBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnDonate.Click += (s, e) =>
                Process.Start(new ProcessStartInfo("https://www.myanonamouse.net/millionaires/donate.php") { UseShellExecute = true });
            groupBoxUserInfo.Controls.Add(btnDonate);
            tableLayoutMain.Controls.Add(groupBoxUserInfo, 0, 0);
            tableLayoutMain.SetColumnSpan(groupBoxUserInfo, 2);

            // Row 1: General Settings
            groupBoxSettings = new GroupBox
            {
                Text = "General Settings",
                AutoSize = true,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White
            };
            checkBoxBuyVip = new CheckBox
            {
                Text = "Buy Max VIP?",
                Location = new Point(10, 20),
                AutoSize = true,
                Checked = true,
                ForeColor = Color.LightGreen
            };
            groupBoxSettings.Controls.Add(checkBoxBuyVip);
            var lblPointsBuff = new Label
            {
                Text = "Points Buffer:",
                Location = new Point(10, 50),
                AutoSize = true,
                ForeColor = Color.LightBlue
            };
            groupBoxSettings.Controls.Add(lblPointsBuff);
            textBoxPointsBuffer = new TextBox
            {
                Text = "10000",
                Width = 100,
                Location = new Point(150, 50),
                BackColor = Color.Black,
                ForeColor = Color.White
            };
            groupBoxSettings.Controls.Add(textBoxPointsBuffer);
            var lblNextRun = new Label
            {
                Text = "Next Run Delay (hours):",
                Location = new Point(10, 80),
                AutoSize = true,
                ForeColor = Color.Plum
            };
            groupBoxSettings.Controls.Add(lblNextRun);
            textBoxNextRun = new TextBox
            {
                Text = "12",
                Width = 100,
                Location = new Point(150, 80),
                BackColor = Color.Black,
                ForeColor = Color.White
            };
            groupBoxSettings.Controls.Add(textBoxNextRun);
            tableLayoutMain.Controls.Add(groupBoxSettings, 0, 1);

            // Row 1: Totals
            groupBoxTotals = new GroupBox
            {
                Text = "Totals",
                AutoSize = true,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White
            };
            var lblTotalGB = new Label
            {
                Text = "Total GB Bought:",
                Location = new Point(10, 25),
                AutoSize = true
            };
            groupBoxTotals.Controls.Add(lblTotalGB);
            labelTotalGB = new Label
            {
                Text = "0",
                Location = new Point(180, 25),
                AutoSize = true
            };
            groupBoxTotals.Controls.Add(labelTotalGB);
            var lblCum = new Label
            {
                Text = "Cumulative Points Spent:",
                Location = new Point(10, 55),
                AutoSize = true
            };
            groupBoxTotals.Controls.Add(lblCum);
            labelCumulativePointsValue = new Label
            {
                Text = "0",
                Location = new Point(180, 55),
                AutoSize = true
            };
            groupBoxTotals.Controls.Add(labelCumulativePointsValue);
            var lblNext = new Label
            {
                Text = "Next Run In:",
                Location = new Point(10, 85),
                AutoSize = true
            };
            groupBoxTotals.Controls.Add(lblNext);
            labelNextRunCountdown = new Label
            {
                Text = "",
                Location = new Point(180, 85),
                AutoSize = true
            };
            groupBoxTotals.Controls.Add(labelNextRunCountdown);
            tableLayoutMain.Controls.Add(groupBoxTotals, 1, 1);

            // Row 2: System Settings
            groupBoxSystemSettings = new GroupBox
            {
                Text = "System Settings",
                AutoSize = true,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White
            };
            checkBoxStartWithWindows = new CheckBox
            {
                Text = "Start with Windows",
                Location = new Point(10, 25),
                AutoSize = true,
                ForeColor = Color.LightGreen
            };
            checkBoxStartWithWindows.CheckedChanged += StartWithWindowsChanged;
            groupBoxSystemSettings.Controls.Add(checkBoxStartWithWindows);
            checkBoxMinimizeTray = new CheckBox
            {
                Text = "Minimize to System Tray",
                Location = new Point(200, 25),
                AutoSize = true,
                ForeColor = Color.LightGreen
            };
            checkBoxMinimizeTray.CheckedChanged += MinimizeTrayChanged;
            groupBoxSystemSettings.Controls.Add(checkBoxMinimizeTray);
            errorNotificationCheckBox = new CheckBox
            {
                Text = "Enable Error Notifications",
                Location = new Point(10, 55),
                AutoSize = true,
                ForeColor = Color.LightCoral
            };
            errorNotificationCheckBox.CheckedChanged += ErrorNotificationChanged;
            groupBoxSystemSettings.Controls.Add(errorNotificationCheckBox);
            tableLayoutMain.Controls.Add(groupBoxSystemSettings, 0, 2);

            // Row 2: Cookie Settings
            groupBoxCookieSettings = new GroupBox
            {
                Text = "Cookie Settings",
                AutoSize = true,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White
            };
            var lblCookie = new Label
            {
                Text = "Cookies File:",
                Location = new Point(10, 25),
                AutoSize = true,
                ForeColor = Color.Orange
            };
            groupBoxCookieSettings.Controls.Add(lblCookie);
            textBoxCookieFile = new TextBox
            {
                Text = "",
                Width = 200,
                Location = new Point(110, 22),
                BackColor = Color.Black,
                ForeColor = Color.White
            };
            textBoxCookieFile.TextChanged += CookieFilePathChanged;
            groupBoxCookieSettings.Controls.Add(textBoxCookieFile);
            buttonBrowseCookie = new Button
            {
                Text = "Select File",
                Size = new Size(100, 30),
                Location = new Point(10, 60),
                BackColor = Color.DimGray,
                ForeColor = Color.White
            };
            buttonBrowseCookie.Click += (s, e) =>
            {
                using var ofd = new OpenFileDialog { Filter = "Cookie Files (*.cookies)|*.cookies|All Files (*.*)|*.*" };
                if (ofd.ShowDialog() == DialogResult.OK)
                    textBoxCookieFile.Text = ofd.FileName;
            };
            groupBoxCookieSettings.Controls.Add(buttonBrowseCookie);
            buttonEditCookie = new Button
            {
                Text = "Edit Cookie",
                Size = new Size(100, 30),
                Location = new Point(120, 60),
                BackColor = Color.DimGray,
                ForeColor = Color.White
            };
            buttonEditCookie.Click += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo(textBoxCookieFile.Text) { UseShellExecute = true }); }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            };
            groupBoxCookieSettings.Controls.Add(buttonEditCookie);
            buttonCreateCookie = new Button
            {
                Text = "Create my Cookie!",
                Size = new Size(120, 30),
                Location = new Point(230, 60),
                BackColor = Color.DimGray,
                ForeColor = Color.White
            };
            buttonCreateCookie.Click += (s, e) =>
            {
                var id = Microsoft.VisualBasic.Interaction.InputBox("Enter security string:", "Create Cookie", "");
                if (!string.IsNullOrEmpty(id))
                {
                    using var sfd = new SaveFileDialog { Filter = "Cookie Files (*.cookies)|*.cookies|All Files (*.*)|*.*", FileName = "MAM.cookies" };
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllText(sfd.FileName, id);
                        textBoxCookieFile.Text = sfd.FileName;
                    }
                }
            };
            groupBoxCookieSettings.Controls.Add(buttonCreateCookie);
            tableLayoutMain.Controls.Add(groupBoxCookieSettings, 1, 2);

            // Row 3: Application Controls
            groupBoxAppControls = new GroupBox
            {
                Text = "Application Controls",
                AutoSize = true,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White
            };
            buttonRun = new Button
            {
                Text = "Run Script",
                Size = new Size(100, 30),
                Location = new Point(10, 20),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.DimGray,
                ForeColor = Color.White
            };
            buttonRun.Click += (s, e) =>
            {
                if (paused)
                {
                    paused = false;
                    buttonPause.Text = "Pause";
                    AppendLog("Resuming automation.");
                }
                if (!int.TryParse(textBoxPointsBuffer.Text, out int pb))
                {
                    MessageBox.Show("Invalid Points Buffer.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!int.TryParse(textBoxNextRun.Text, out int nr))
                {
                    MessageBox.Show("Invalid Next Run Delay.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                bool vip = checkBoxBuyVip.Checked;
                string cf = textBoxCookieFile.Text;
                if (automationRunning) { AppendLog("Already running."); return; }
                Task.Run(async () =>
                {
                    automationRunning = true;
                    try
                    {
                        await AutomationService.RunAutomationAsync(cf, pb, vip, nr,
                            AppendLog, UpdateUserInformation, UpdateTotals);
                    }
                    catch (Exception ex)
                    {
                        AppendLog("Error: " + ex.Message);
                        if (sendErrorNotifications)
                            notifyIcon.ShowBalloonTip(5000, "MAM Auto Points – Error", ex.Message, ToolTipIcon.Error);
                    }
                    finally
                    {
                        automationRunning = false;
                        nextRunTime = DateTime.Now.AddHours(nr);
                    }
                });
            };
            groupBoxAppControls.Controls.Add(buttonRun);

            buttonPause = new Button
            {
                Text = "Pause",
                Size = new Size(100, 30),
                Location = new Point(120, 20),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.DimGray,
                ForeColor = Color.White
            };
            buttonPause.Click += (s, e) =>
            {
                paused = !paused;
                buttonPause.Text = paused ? "Resume" : "Pause";
                AppendLog(paused ? "Paused." : "Resumed.");
            };
            groupBoxAppControls.Controls.Add(buttonPause);

            buttonExit = new Button
            {
                Text = "Exit",
                Size = new Size(100, 30),
                Location = new Point(230, 20),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.DimGray,
                ForeColor = Color.White
            };
            buttonExit.Click += (s, e) => this.Close();
            groupBoxAppControls.Controls.Add(buttonExit);

            buttonHelpCookie = new Button
            {
                Text = "Instructions",
                Size = new Size(150, 30),
                Location = new Point(340, 20),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.DimGray,
                ForeColor = Color.White
            };
            buttonHelpCookie.Click += (s, e) =>
                MessageBox.Show("Instructions:\n1. ...\n2. ...", "Instructions");
            groupBoxAppControls.Controls.Add(buttonHelpCookie);

            tableLayoutMain.Controls.Add(groupBoxAppControls, 0, 3);
            tableLayoutMain.SetColumnSpan(groupBoxAppControls, 2);

            // Center content
            CenterContent();

            // Tray icon
            notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = false,
                Text = "MAM Auto Points"
            };
            var trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Show", null, (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; });
            trayMenu.Items.Add("Exit", null, (s, e) => Application.Exit());
            notifyIcon.ContextMenuStrip = trayMenu;
            notifyIcon.DoubleClick += (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; };

            // Timer
            timerCountdown = new System.Windows.Forms.Timer { Interval = 1000 };
            timerCountdown.Tick += TimerCountdown_Tick;
            timerCountdown.Start();

            // Load config
            LoadConfig();
        }

        private void UpdateUserInformation(AutomationService.UserSummary summary)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<AutomationService.UserSummary>(UpdateUserInformation), summary);
                return;
            }
            labelUserName.Text = summary.Username;
            labelVipExpires.Text = summary.VipExpires;
            labelDownloaded.Text = summary.Downloaded;
            labelUploaded.Text = summary.Uploaded;
            labelRatio.Text = summary.Ratio;
        }

        private void CookieFilePathChanged(object? sender, EventArgs e)
        {
            _config.CookieFilePath = textBoxCookieFile.Text;
            SaveConfig();
            AppendLog("Cookie file path saved: " + textBoxCookieFile.Text);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            CenterContent();
            this.ClientSize = new Size(ContentWidth + 20, this.ClientSize.Height);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            CenterContent();
            if (WindowState == FormWindowState.Minimized && enableMinimizeToTray)
            {
                Hide();
                notifyIcon.Visible = true;
                notifyIcon.ShowBalloonTip(3000, "MAM Auto Points", "Minimized to tray.", ToolTipIcon.Info);
            }
        }

        private void CenterContent()
        {
            int leftOffset = (this.ClientSize.Width - ContentWidth) / 2;
            if (panelContent != null)
                panelContent.Left = leftOffset;
        }

        private void AppendLog(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(AppendLog), message);
                return;
            }
            textBoxLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }

        private void UpdateTotals(int gbBought, int pointsSpent)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int, int>(UpdateTotals), gbBought, pointsSpent);
                return;
            }
            cumulativeUploadGB += gbBought;
            cumulativePointsSpent += pointsSpent;
            labelTotalGB.Text = cumulativeUploadGB.ToString();
            labelCumulativePointsValue.Text = cumulativePointsSpent.ToString();
        }

        private void StartWithWindowsChanged(object? sender, EventArgs e)
        {
            bool enable = checkBoxStartWithWindows.Checked;
            try
            {
                using var rk = Registry.CurrentUser.OpenSubKey(
                    "Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (enable)
                    rk.SetValue("MAMAutoPoints", Application.ExecutablePath);
                else
                    rk.DeleteValue("MAMAutoPoints", false);

                _config.StartWithWindows = enable;
                SaveConfig();
                AppendLog("Start with Windows " + (enable ? "enabled." : "disabled."));
            }
            catch (Exception ex)
            {
                AppendLog("Failed to update startup setting: " + ex.Message);
            }
        }

        private void MinimizeTrayChanged(object? sender, EventArgs e)
        {
            enableMinimizeToTray = checkBoxMinimizeTray.Checked;
            _config.MinimizeToTray = enableMinimizeToTray;
            SaveConfig();
            AppendLog("Minimize to tray " + (enableMinimizeToTray ? "enabled." : "disabled."));
        }

        private void ErrorNotificationChanged(object? sender, EventArgs e)
        {
            sendErrorNotifications = errorNotificationCheckBox.Checked;
            AppendLog("Error notifications " + (sendErrorNotifications ? "enabled." : "disabled."));
            SaveConfig();
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    var cfg = JsonSerializer.Deserialize<AppConfig>(json);
                    if (cfg != null) _config = cfg;
                }
            }
            catch { }

            // Restore cookie path
            textBoxCookieFile.TextChanged -= CookieFilePathChanged;
            textBoxCookieFile.Text = _config.CookieFilePath;
            textBoxCookieFile.TextChanged += CookieFilePathChanged;

            // Restore toggles
            errorNotificationCheckBox.CheckedChanged -= ErrorNotificationChanged;
            sendErrorNotifications = _config.SendErrorNotifications;
            errorNotificationCheckBox.Checked = sendErrorNotifications;
            errorNotificationCheckBox.CheckedChanged += ErrorNotificationChanged;

            checkBoxStartWithWindows.CheckedChanged -= StartWithWindowsChanged;
            checkBoxStartWithWindows.Checked = _config.StartWithWindows;
            checkBoxStartWithWindows.CheckedChanged += StartWithWindowsChanged;

            checkBoxMinimizeTray.CheckedChanged -= MinimizeTrayChanged;
            checkBoxMinimizeTray.Checked = _config.MinimizeToTray;
            enableMinimizeToTray = _config.MinimizeToTray;
            checkBoxMinimizeTray.CheckedChanged += MinimizeTrayChanged;
        }

        private void SaveConfig()
        {
            try
            {
                _config.SendErrorNotifications = sendErrorNotifications;
                _config.StartWithWindows = checkBoxStartWithWindows.Checked;
                _config.MinimizeToTray = checkBoxMinimizeTray.Checked;
                _config.CookieFilePath = textBoxCookieFile.Text;
                var json = JsonSerializer.Serialize(_config);
                File.WriteAllText(_configPath, json);
            }
            catch { }
        }

        private void TimerCountdown_Tick(object? sender, EventArgs e)
        {
            if (nextRunTime.HasValue)
            {
                var rem = nextRunTime.Value - DateTime.Now;
                labelNextRunCountdown.Text = rem.TotalSeconds > 0
                    ? $"{rem.Hours:D2}:{rem.Minutes:D2}:{rem.Seconds:D2}" : "Ready";
                if (rem.TotalSeconds <= 0 && !automationRunning)
                    buttonRun.PerformClick();
            }
            else labelNextRunCountdown.Text = "";
        }
    }
}
