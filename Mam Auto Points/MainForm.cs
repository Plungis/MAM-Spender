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
        private const string APP_VERSION = "2.4.5wfb";

        // UI Controls
        private TextBox textBoxLog = null!;
        private TextBox textBoxPointsBuffer = null!;
        private CheckBox checkBoxBuyVip = null!;
        private CheckBox checkBoxBuyFlBeforeGb = null!;
        private ComboBox comboBoxPurchaseTier = null!; // kept for backward compat, hidden
        private TrackBar trackBarUploadGb = null!;
        private NumericUpDown numericUpDownGb = null!;
        private CheckBox checkBoxMaxAffordable = null!;
        private Label labelUploadGbValue = null!;
        private TextBox textBoxNextRun = null!;
        private Label labelTotalGB = null!;
        private CheckBox checkBoxFlOnly = null!;
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
        private Button buttonSaveSettings = null!;
        private System.Windows.Forms.Timer timerCountdown = null!;
        private DateTime? nextRunTime = null;
        private int cumulativePointsSpent = 0;
        private double cumulativeUploadGB = 0;
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
        private readonly string _configPath;
        private AppConfig _config = new AppConfig();

        private class AppConfig
        {
            public bool SendErrorNotifications { get; set; }
            public bool StartWithWindows { get; set; }
            public bool MinimizeToTray { get; set; }
            public string CookieFilePath { get; set; } = string.Empty;

            // Persist these settings too
            public bool BuyVip { get; set; } = false;
            public bool BuyFlBeforeGb { get; set; } = false;
            public bool BuyFlOnly { get; set; } = false;
            public int PointsBuffer { get; set; } = 10000;
            public int NextRunDelayMinutes { get; set; } = 15;
            public int PurchaseTier { get; set; } = 5; // legacy - migrated to CustomUploadGb/UseMaxAffordable
            public double CustomUploadGb { get; set; } = 100; // slider 1..199
            public bool UseMaxAffordable { get; set; } = false; // toggle: All I can afford

            // Persist totals across sessions
            public double CumulativeUploadGB { get; set; }
            public int CumulativePointsSpent { get; set; }

            // Last scan tracking for points/min
            public int LastScanPoints { get; set; }
            public DateTime? LastScanTime { get; set; }

            // Persist next scheduled run across sessions
            public DateTime? NextRunTimeLocal { get; set; }

            // Update notification tracking
            public string LastNotifiedVersion { get; set; } = "";
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
        private Label labelLastScanPoints = null!;
        private Label labelPointsPerMin = null!;

        public MainForm()
        {
            var baseDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MAMAutoPoints");
            Directory.CreateDirectory(baseDir);
            _configPath = Path.Combine(baseDir, "MAMAutoPointsConfig.json");

            InitializeComponent();
        }

        public class ScrollableMessageBox : Form
        {
            public ScrollableMessageBox(string title, string message)
            {
                Text = title;
                Size = new Size(800, 600);
                StartPosition = FormStartPosition.CenterParent;
                MinimizeBox = false;
                MaximizeBox = true;

                var textBox = new TextBox
                {
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Vertical,
                    Dock = DockStyle.Fill,
                    Font = new Font("Segoe UI", 10),
                    BackColor = Color.Black,
                    ForeColor = Color.White,
                    Text = message,
                    HideSelection = true
                };

                var closeButton = new Button
                {
                    Text = "Close",
                    Dock = DockStyle.Bottom,
                    Height = 35
                };

                closeButton.Click += (s, e) => Close();

                Controls.Add(textBox);
                Controls.Add(closeButton);

                // 🔹 CLEAR AUTO-SELECTION AFTER FORM SHOWS
                Shown += (s, e) =>
                {
                    textBox.SelectionStart = 0;
                    textBox.SelectionLength = 0;
                    textBox.ScrollToCaret();
                };
            }


            public static void Show(IWin32Window owner, string title, string message)
            {
                using var box = new ScrollableMessageBox(title, message);
                box.ShowDialog(owner);
            }
        }
        private void InitializeComponent()
        {
            // Form properties
            this.MinimumSize = new Size(875, 750);
            this.Size = new Size(875, 750);
            this.Text = $"MAM Auto Points v{APP_VERSION}";
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
            tableLayoutMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 200));
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
                Height = 200,
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

            // Last Scan Points
            var lblLastScanPointsTitle = new Label
            {
                Text = "Last Scan Points:",
                Location = new Point(380, 75),
                AutoSize = true,
                ForeColor = Color.Gold
            };
            groupBoxUserInfo.Controls.Add(lblLastScanPointsTitle);

            labelLastScanPoints = new Label
            {
                Text = "N/A",
                Location = new Point(510, 75),
                AutoSize = true,
                ForeColor = Color.Gold
            };
            groupBoxUserInfo.Controls.Add(labelLastScanPoints);

            // Points/Min
            var lblPointsPerMinTitle = new Label
            {
                Text = "Points/Min:",
                Location = new Point(380, 100),
                AutoSize = true,
                ForeColor = Color.Gold
            };
            groupBoxUserInfo.Controls.Add(lblPointsPerMinTitle);

            labelPointsPerMin = new Label
            {
                Text = "N/A",
                Location = new Point(510, 100),
                AutoSize = true,
                ForeColor = Color.Gold
            };
            groupBoxUserInfo.Controls.Add(labelPointsPerMin);

            // Lotto button
            var btnLotto = new Button
            {
                Text = "Play MAM Lotto",
                Size = new Size(140, 30),
                Location = new Point(10, 135),
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
                Location = new Point(160, 135),
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

            // ==========================
            // Row 1 LEFT: General Settings
            // ==========================
            groupBoxSettings = new GroupBox
            {
                Text = "General Settings",
                AutoSize = false,
                Height = 230,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White
            };

            checkBoxBuyVip = new CheckBox
            {
                Text = "Buy Max VIP?",
                Location = new Point(10, 20),
                AutoSize = true,
                Checked = false,
                ForeColor = Color.LightGreen
            };
            checkBoxBuyVip.CheckedChanged += BuyVipChanged;
            groupBoxSettings.Controls.Add(checkBoxBuyVip);

            checkBoxBuyFlBeforeGb = new CheckBox
            {
                Text = "Buy FL Wedge before GB?",
                Location = new Point(150, 20),
                AutoSize = true,
                ForeColor = Color.LightSkyBlue
            };
            checkBoxBuyFlBeforeGb.CheckedChanged += BuyFlBeforeGbChanged;
            groupBoxSettings.Controls.Add(checkBoxBuyFlBeforeGb);

            checkBoxFlOnly = new CheckBox
            {
                Text = "Buy ONLY Freeleech Wedges (no upload credit)",
                Location = new Point(10, 45),
                AutoSize = true,
                ForeColor = Color.Orange
            };
            groupBoxSettings.Controls.Add(checkBoxFlOnly);
            checkBoxFlOnly.CheckedChanged += FlOnlyChanged;

            // Hidden legacy combo for migration
            comboBoxPurchaseTier = new ComboBox { Visible = false };
            comboBoxPurchaseTier.Items.AddRange(new object[]
            {
                "500 pts  - 1 GB",
                "1,250 pts - 2.5 GB",
                "2,500 pts - 5 GB",
                "10,000 pts - 20 GB",
                "25,000 pts - 50 GB",
                "50,000 pts - 100 GB",
                "Variable - All I can afford"
            });
            comboBoxPurchaseTier.SelectedIndex = 5;

            var lblUploadGb = new Label
            {
                Text = "Min Upload GB:",
                Location = new Point(10, 75),
                AutoSize = true,
                ForeColor = Color.Gold
            };
            groupBoxSettings.Controls.Add(lblUploadGb);

            // TIER SLIDER: 500 pts=1GB ... 50000 pts=100GB + Variable (All I can afford, cap 99,999)
            trackBarUploadGb = new TrackBar
            {
                Location = new Point(115, 72),
                Width = 190,
                Height = 30,
                Minimum = 0,
                Maximum = 6,
                TickFrequency = 1,
                SmallChange = 1,
                LargeChange = 1,
                Value = 5 // default 50k / 100 GB
            };
            trackBarUploadGb.Scroll += TrackBarUploadGb_Scroll;
            trackBarUploadGb.ValueChanged += TrackBarUploadGb_Scroll;
            groupBoxSettings.Controls.Add(trackBarUploadGb);

            // Keep controls for compat but hidden - replaced by tier slider
            numericUpDownGb = new NumericUpDown
            {
                Location = new Point(315, 75),
                Width = 40,
                Minimum = 0,
                Maximum = 6,
                Value = 5,
                Visible = false
            };
            groupBoxSettings.Controls.Add(numericUpDownGb);

            checkBoxMaxAffordable = new CheckBox
            {
                Visible = false,
                Checked = false
            };
            groupBoxSettings.Controls.Add(checkBoxMaxAffordable);

            labelUploadGbValue = new Label
            {
                Text = "50,000 pts -> 100 GB",
                Location = new Point(115, 100),
                AutoSize = true,
                ForeColor = Color.LightGreen,
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };
            groupBoxSettings.Controls.Add(labelUploadGbValue);

            var lblPointsBuff = new Label
            {
                Text = "Points Buffer:",
                Location = new Point(10, 130),
                AutoSize = true,
                ForeColor = Color.LightBlue
            };
            groupBoxSettings.Controls.Add(lblPointsBuff);

            textBoxPointsBuffer = new TextBox
            {
                Text = "10000",
                Width = 100,
                Location = new Point(150, 130),
                BackColor = Color.Black,
                ForeColor = Color.White
            };
            textBoxPointsBuffer.TextChanged += PointsBufferChanged;
            groupBoxSettings.Controls.Add(textBoxPointsBuffer);

            var lblNextRun = new Label
            {
                Text = "Next Run Delay (mins):",
                Location = new Point(10, 160),
                AutoSize = true,
                ForeColor = Color.Plum
            };
            groupBoxSettings.Controls.Add(lblNextRun);

            textBoxNextRun = new TextBox
            {
                Text = "15",
                Width = 100,
                Location = new Point(150, 160),
                BackColor = Color.Black,
                ForeColor = Color.White
            };
            textBoxNextRun.TextChanged += NextRunHoursChanged;
            groupBoxSettings.Controls.Add(textBoxNextRun);

            tableLayoutMain.Controls.Add(groupBoxSettings, 0, 1);


            // ==========================
            // Row 1 RIGHT: Totals
            // ==========================
            groupBoxTotals = new GroupBox
            {
                Text = "Totals",
                AutoSize = false,
                Height = 230,
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

            var buttonResetTotals = new Button
            {
                Text = "Reset Totals",
                Size = new Size(120, 28),
                Location = new Point(10, 115),
                BackColor = Color.DarkRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            buttonResetTotals.Click += (s, e) =>
            {
                cumulativeUploadGB = 0;
                cumulativePointsSpent = 0;
                labelTotalGB.Text = "0";
                labelCumulativePointsValue.Text = "0";
                SaveConfig();
                AppendLog("Cumulative totals reset.");
            };
            groupBoxTotals.Controls.Add(buttonResetTotals);

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
            buttonRun.Click += async (s, e) =>
            {
                await StartAutomationAsync(isManualImmediate: false, flOnlyOverride: false);
            };

            var buttonRunNow = new Button
            {
                Text = "Run Script Immediately",
                Size = new Size(180, 30),
                Location = new Point(500, 20),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 140, 40),
                ForeColor = Color.White
            };

            buttonRunNow.Click += async (s, e) =>
            {
                await StartAutomationAsync(isManualImmediate: true, flOnlyOverride: false);
            };

            groupBoxAppControls.Controls.Add(buttonRunNow);
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

            buttonSaveSettings = new Button
            {
                Text = "Save Settings",
                Size = new Size(120, 30),
                Location = new Point(10, 60),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 110, 40),
                ForeColor = Color.White
            };
            buttonSaveSettings.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(textBoxCookieFile.Text))
                {
                    MessageBox.Show("No cookie file selected. Settings will still be saved, but the cookie path is empty.",
                        "Save Settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                SaveConfig();
                AppendLog("Settings saved to: " + _configPath);
                MessageBox.Show(
                    $"Settings saved to:\r\n{_configPath}",
                    "Save Settings",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            };
            groupBoxAppControls.Controls.Add(buttonSaveSettings);

            buttonHelpCookie = new Button
            {
                Text = "Instructions",
                Size = new Size(150, 30),
                Location = new Point(340, 20),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.DimGray,
                ForeColor = Color.White
            };

            // Instructions section //

            buttonHelpCookie.Click += (s, e) =>
            {
                ScrollableMessageBox.Show(
                    this,
                    "MAM Auto Points – Instructions",
                    "IMPORTANT: HOW COOKIES WORK\r\n" +
                    "--------------------------------\r\n" +
                    "Your MAM session cookie is tied to your IP address.\r\n\r\n" +

                    "Your IP may change when:\r\n" +
                    "• Your router restarts\r\n" +
                    "• Your device disconnects/reconnects to your ISP\r\n" +
                    "• Your ISP rotates addresses automatically\r\n\r\n" +

                    "When your IP changes, your cookie becomes INVALID.\r\n" +
                    "This is normal behavior.\r\n\r\n" +

                    "If the app reports a session error, simply create a new cookie.\r\n\r\n" +

                    "--------------------------------\r\n" +
                    "STEP 1: CREATE A VALID COOKIE\r\n" +
                    "--------------------------------\r\n" +
                    "You MUST do this on the SAME device that runs MAM Auto Points.\r\n\r\n" +

                    "1) Log into https://www.myanonamouse.net\r\n" +
                    "2) Open Menu → Preferences → Security\r\n" +
                    "3) Under Active Sessions:\r\n" +
                    "   • Keep the entry that says \"log out\" (this is your current session)\r\n" +
                    "   • Remove any other entries\r\n\r\n" +

                    "4) Copy your IP address shown there.\r\n" +
                    "   It will look something like:\r\n" +
                    "   XX.XXX.XXX.XX (XXXXX)\r\n\r\n" +

                    "5) Scroll to Create Session:\r\n" +
                    "   • Select the radio option \"ASN locked\"\r\n" +
                    "   • Paste your IP address into the IP field\r\n" +
                    "   • Be careful of trailing spaces\r\n\r\n" +

                    "6) Click Create\r\n" +
                    "7) Copy the LONG STRING that appears — this is your cookie value\r\n\r\n" +

                    "--------------------------------\r\n" +
                    "STEP 2: CREATE OR UPDATE COOKIE FILE\r\n" +
                    "--------------------------------\r\n" +

                    "Option A: Create My Cookie!\r\n" +
                    "• Click \"Create My Cookie!\"\r\n" +
                    "• Paste the cookie string\r\n" +
                    "• Save the file (recommended name: MAM.cookies)\r\n\r\n" +

                    "Option B: Replace Existing Cookie\r\n" +
                    "• Open your existing .cookies file\r\n" +
                    "• Replace its contents with the new cookie string\r\n" +
                    "• Save the file\r\n\r\n" +

                    "KEEP THIS FILE PRIVATE.\r\n" +
                    "Anyone with it can use your session.\r\n\r\n" +

                    "--------------------------------\r\n" +
                    "STEP 3: CONFIGURE SETTINGS\r\n" +
                    "--------------------------------\r\n" +

                    "Buy Max VIP:\r\n" +
                    "• Automatically renews VIP when 83 days or less remain\r\n\r\n" +

                    "Buy Freeleech Wedge before GB:\r\n" +
                    "• Purchases Freeleech Wedges (50,000 points each) before upload credit\r\n\r\n" +

                    "Buy Only Freeleech Wedges:\r\n" +
                    "• Only buys wedges\r\n" +
                    "• Skips upload credit entirely\r\n\r\n" +

                    "Min Upload Slider (new):\r\n" +
                    "• Slider positions (minimum upload credit):\r\n" +
                    "• 500 pts -> 1 GB | 1,250 pts -> 2.5 GB | 2,500 pts -> 5 GB\r\n" +
                    "• 10,000 pts -> 20 GB | 25,000 pts -> 50 GB | 50,000 pts -> 100 GB\r\n" +
                    "• Variable -> All I can afford = floor((min(points,99999) - buffer)/500) GB\r\n" +
                    "• Cap 99,999 pts = 199 GB max. Buffer is reserve kept after purchase\r\n" +
                    "• Slide to set minimum before buying (e.g. 20 GB needs 15k with 5k buffer)\r\n\r\n" +

                    "Next Run Delay (mins):\r\n" +
                    "• Time between automatic runs (minimum: 3 min)\r\n\r\n" +

                    "--------------------------------\r\n" +
                    "STEP 4: RUNNING THE SCRIPT\r\n" +
                    "--------------------------------\r\n" +

                    "Run Script:\r\n" +
                    "• Runs on the normal schedule\r\n\r\n" +

                    "Run Script Immediately:\r\n" +
                    "• Ignores the timer and runs now\r\n\r\n" +

                    "The script will:\r\n" +
                    "1) Validate your session\r\n" +
                    "2) Renew VIP if enabled and needed\r\n" +
                    "3) Buy Freeleech Wedges if enabled\r\n" +
                    "4) Purchase upload credit per selected tier (wedge first if enabled)\r\n" +
                    "5) Schedule the next run automatically\r\n\r\n" +

                    "--------------------------------\r\n" +
                    "NOTES & WARNINGS\r\n" +
                    "--------------------------------\r\n" +

                    "• Minimum upload is 1 GiB (500 points), max 199 GiB (capped at 99,999 pts)\r\n" +
                    "• Cost = GB * 500. Need cost + buffer to trigger\r\n" +
                    "• Purchases are irreversible\r\n" +
                    "• Freeleech Wedges cost 50,000 points each\r\n" +
                    "• If your IP changes, recreate your cookie\r\n\r\n" +

                    "This tool is NOT affiliated with MyAnonamouse."
                );
            };
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

            // Update check (run on UI thread after the form is ready)
            this.Shown += async (s, e) =>
            {
                await CheckForUpdatesAsync();
            };
        }

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("MAMAutoPoints");

                var json = await client.GetStringAsync(
                    "https://api.github.com/repos/Plungis/MAM-Spender/releases/latest");

                using var doc = JsonDocument.Parse(json);
                var latestTag = doc.RootElement
                    .GetProperty("tag_name")
                    .GetString()?
                    .TrimStart('v');

                if (string.IsNullOrWhiteSpace(latestTag))
                    return;

                if (latestTag == APP_VERSION ||
                    latestTag == _config.LastNotifiedVersion)
                    return;

                _config.LastNotifiedVersion = latestTag;
                SaveConfig();

                MessageBox.Show(
                    $"A new version of MAM Auto Points is available!\r\n\r\n" +
                    $"Current version: {APP_VERSION}\r\n" +
                    $"Latest version: {latestTag}\r\n\r\n" +
                    $"Visit GitHub to download the update.",
                    "Update Available",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                if (enableMinimizeToTray && notifyIcon != null)
                {
                    notifyIcon.ShowBalloonTip(
                        6000,
                        "MAM Auto Points Update",
                        $"New version {latestTag} available",
                        ToolTipIcon.Info);
                }
            }
            catch
            {
                // Never allow update checks to crash the app
            }
        }

        private void UpdateUserInformation(AutomationService.UserSummary summary)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<AutomationService.UserSummary>(UpdateUserInformation), summary);
                return;
            }

            if (labelUserName == null ||
                labelVipExpires == null ||
                labelDownloaded == null ||
                labelUploaded == null ||
                labelRatio == null)
                return;

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

        private void PointsBufferChanged(object? sender, EventArgs e)
        {
            if (int.TryParse(textBoxPointsBuffer.Text, out int pb))
            {
                _config.PointsBuffer = pb;
                SaveConfig();
            }
        }

        private void NextRunHoursChanged(object? sender, EventArgs e)
        {
            if (int.TryParse(textBoxNextRun.Text, out int nr))
            {
                _config.NextRunDelayMinutes = nr;
                SaveConfig();
            }
        }

        private void BuyVipChanged(object? sender, EventArgs e)
        {
            _config.BuyVip = checkBoxBuyVip.Checked;
            SaveConfig();
        }

        private void BuyFlBeforeGbChanged(object? sender, EventArgs e)
        {
            _config.BuyFlBeforeGb = checkBoxBuyFlBeforeGb.Checked;
            SaveConfig();
        }

        private void FlOnlyChanged(object? sender, EventArgs e)
        {
            SaveConfig();
        }

        private void PurchaseTierChanged(object? sender, EventArgs e)
        {
            _config.PurchaseTier = comboBoxPurchaseTier.SelectedIndex;
            SaveConfig();
            AppendLog($"Upload tier changed to: {comboBoxPurchaseTier.SelectedItem}");
        }

        private void TrackBarUploadGb_Scroll(object? sender, EventArgs e)
        {
            if (trackBarUploadGb == null) return;
            int idx = Math.Clamp(trackBarUploadGb.Value, 0, 6);
            // Map slider to tier
            _config.PurchaseTier = idx;
            // Keep legacy fields in sync for AutomationService
            var (cost, gb) = AutomationService.GetTierCostGbPublic((AutomationService.PurchaseTier)idx);
            _config.CustomUploadGb = gb;
            _config.UseMaxAffordable = idx == 6;
            if (numericUpDownGb != null) numericUpDownGb.Value = idx;
            if (checkBoxMaxAffordable != null) checkBoxMaxAffordable.Checked = idx == 6;
            UpdateUploadGbLabel();
            SaveConfig();
            AppendLog($"Min upload set to: {TierLabels[idx]}");
        }

        private void NumericUpDownGb_ValueChanged(object? sender, EventArgs e) { /* hidden compat */ }

        private void CheckBoxMaxAffordable_CheckedChanged(object? sender, EventArgs e) { /* hidden compat - tier slider handles Variable */ }

        private static readonly string[] TierLabels = new[]
        {
            "500 pts -> 1 GB",
            "1,250 pts -> 2.5 GB",
            "2,500 pts -> 5 GB",
            "10,000 pts -> 20 GB",
            "25,000 pts -> 50 GB",
            "50,000 pts -> 100 GB",
            "Variable -> All I can afford"
        };

        private void UpdateUploadGbLabel()
        {
            if (labelUploadGbValue == null || trackBarUploadGb == null) return;
            int idx = Math.Clamp(trackBarUploadGb.Value, 0, 6);
            labelUploadGbValue.Text = TierLabels[idx];
            labelUploadGbValue.ForeColor = idx == 6 ? Color.Orange : Color.LightGreen;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveConfig();
            base.OnFormClosing(e);
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
                timerCountdown.Enabled = true;
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

        private async Task StartAutomationAsync(bool isManualImmediate, bool flOnlyOverride)
        {
            if (automationRunning)
            {
                AppendLog("Already running.");
                return;
            }

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

            if (!int.TryParse(textBoxNextRun.Text, out int nr) || nr < 3)
            {
                MessageBox.Show("Invalid Next Run Delay. Minimum is 3 minutes.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool vip = checkBoxBuyVip.Checked;
            bool buyFlBeforeGb = checkBoxBuyFlBeforeGb.Checked;
            bool flOnly = checkBoxFlOnly.Checked;
            string cf = textBoxCookieFile.Text;
            int sliderIdx = trackBarUploadGb != null ? Math.Clamp(trackBarUploadGb.Value, 0, 6) : 5;
            var tier = (AutomationService.PurchaseTier)sliderIdx;
            // Legacy hidden combo kept in sync
            if (comboBoxPurchaseTier != null) comboBoxPurchaseTier.SelectedIndex = sliderIdx;
            double customGb = -1; // force tier-based path
            bool useMax = sliderIdx == 6;

            // Manual immediate run: ignore schedule gate, run now.
            // Scheduled run: only run when due (TimerTick enforces that).
            if (isManualImmediate)
                AppendLog("Manual run requested: running immediately.");

            automationRunning = true;
            try
            {
                // Run on background thread, but ALL UI updates must be marshaled inside MainForm methods.
                await Task.Run(async () =>
                {
                    await AutomationService.RunAutomationAsync(
                        cf,
                        pb,
                        vip,
                        buyFlBeforeGb,
                        flOnly || flOnlyOverride,
                        nr,
                        AppendLog,
                        UpdateUserInformation,
                        UpdateTotals,
                        OnCurrentPointsUpdated,
                        tier,
                        customGb,
                        useMax
                    );
                });
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

                // Always schedule the next run after *any* run finishes (manual or scheduled)
                nextRunTime = DateTime.Now.AddMinutes(nr);
                _config.NextRunTimeLocal = nextRunTime;
                SaveConfig();

                AppendLog($"Next run scheduled for: {nextRunTime:MMM dd, yyyy h:mm tt}");
            }
        }

        private void UpdateTotals(double gbBoughtFromService, int pointsSpent)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<double, int>(UpdateTotals), gbBoughtFromService, pointsSpent);
                return;
            }

            if (pointsSpent <= 0 && gbBoughtFromService <= 0)
            {
                AppendLog("No points spent this run — totals unchanged.");
                return;
            }

            if (gbBoughtFromService < 0)
                gbBoughtFromService = 0;

            cumulativeUploadGB += gbBoughtFromService;
            cumulativePointsSpent += Math.Max(pointsSpent, 0);

            if (labelTotalGB != null)
                labelTotalGB.Text = cumulativeUploadGB % 1 == 0 ? cumulativeUploadGB.ToString("0") : cumulativeUploadGB.ToString("0.##");

            if (labelCumulativePointsValue != null)
                labelCumulativePointsValue.Text = cumulativePointsSpent.ToString();

            _config.CumulativeUploadGB = cumulativeUploadGB;
            _config.CumulativePointsSpent = cumulativePointsSpent;
            SaveConfig();

            if (gbBoughtFromService > 0 && pointsSpent > 0)
            {
                string gbStr = gbBoughtFromService % 1 == 0 ? gbBoughtFromService.ToString("0") : gbBoughtFromService.ToString("0.##");
                AppendLog($"Confirmed purchase: {gbStr} GB for {pointsSpent} points.");
            }
            else if (gbBoughtFromService == 0 && pointsSpent > 0)
            {
                AppendLog($"Confirmed purchase: 0 GB upload credit for {pointsSpent} points (e.g., Freeleech Wedges/VIP).");
            }
            else
            {
                AppendLog("Totals updated.");
            }
        }

        private void OnCurrentPointsUpdated(int currentPoints)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int>(OnCurrentPointsUpdated), currentPoints);
                return;
            }

            if (labelLastScanPoints == null || labelPointsPerMin == null)
                return;

            labelLastScanPoints.Text = currentPoints.ToString("N0");

            // Calculate points/min
            if (_config.LastScanPoints > 0 && _config.LastScanTime.HasValue)
            {
                int pointsEarned = currentPoints - _config.LastScanPoints;
                double minsElapsed = (DateTime.Now - _config.LastScanTime.Value).TotalMinutes;

                if (pointsEarned > 0 && minsElapsed >= 1)
                {
                    double ppm = pointsEarned / minsElapsed;
                    labelPointsPerMin.Text = $"{ppm:F1}";
                    AppendLog($"Points/min: {ppm:F1} ({pointsEarned:N0} pts over {minsElapsed:F0} min)");
                }
                else
                {
                    labelPointsPerMin.Text = pointsEarned <= 0 ? "0.0" : "N/A";
                }
            }
            else
            {
                labelPointsPerMin.Text = "N/A";
            }

            // Persist as last scan for next comparison
            _config.LastScanPoints = currentPoints;
            _config.LastScanTime = DateTime.Now;
            SaveConfig();
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
            _config.SendErrorNotifications = sendErrorNotifications;
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

            // Restore totals
            cumulativeUploadGB = _config.CumulativeUploadGB;
            cumulativePointsSpent = _config.CumulativePointsSpent;

            if (labelTotalGB != null)
                labelTotalGB.Text = cumulativeUploadGB.ToString();

            if (labelCumulativePointsValue != null)
                labelCumulativePointsValue.Text = cumulativePointsSpent.ToString();

            // Restore last scan info
            if (labelLastScanPoints != null)
                labelLastScanPoints.Text = _config.LastScanPoints > 0
                    ? _config.LastScanPoints.ToString("N0") : "N/A";
            if (labelPointsPerMin != null)
                labelPointsPerMin.Text = "N/A";

            // Restore next run time
            nextRunTime = _config.NextRunTimeLocal;

            // Restore slider (tier 0..6) - handles legacy configs
            // IMPORTANT: restore the slider BEFORE any control whose change handler auto-saves
            // (cookie path, checkboxes). Those handlers call SaveConfig(), which reads the
            // trackbar; if the trackbar is still at its constructor default (5), the saved
            // tier gets clobbered back to 100 GB in memory and on disk.
            int restoreIdx = Math.Clamp(_config.PurchaseTier, 0, 6);
            // If legacy CustomUploadGb/UseMaxAffordable were used, prefer them once
            if (_config.UseMaxAffordable) restoreIdx = 6;
            else if (_config.CustomUploadGb >= 1 && _config.CustomUploadGb <= 6 && _config.PurchaseTier == 5 && _config.CustomUploadGb != 100)
            {
                // old 1..199 slider value - map to nearest tier
                if (_config.CustomUploadGb <= 1) restoreIdx = 0;
                else if (_config.CustomUploadGb <= 2.5) restoreIdx = 1;
                else if (_config.CustomUploadGb <= 5) restoreIdx = 2;
                else if (_config.CustomUploadGb <= 20) restoreIdx = 3;
                else if (_config.CustomUploadGb <= 50) restoreIdx = 4;
                else restoreIdx = 5;
            }
            if (comboBoxPurchaseTier != null)
            {
                comboBoxPurchaseTier.SelectedIndexChanged -= PurchaseTierChanged;
                comboBoxPurchaseTier.SelectedIndex = restoreIdx;
                comboBoxPurchaseTier.SelectedIndexChanged += PurchaseTierChanged;
            }
            if (trackBarUploadGb != null && labelUploadGbValue != null)
            {
                trackBarUploadGb.ValueChanged -= TrackBarUploadGb_Scroll;
                trackBarUploadGb.Scroll -= TrackBarUploadGb_Scroll;
                trackBarUploadGb.Value = restoreIdx;
                trackBarUploadGb.ValueChanged += TrackBarUploadGb_Scroll;
                trackBarUploadGb.Scroll += TrackBarUploadGb_Scroll;
                // Sync hidden compat fields
                _config.PurchaseTier = restoreIdx;
                var (cCost, cGb) = AutomationService.GetTierCostGbPublic((AutomationService.PurchaseTier)restoreIdx);
                _config.CustomUploadGb = cGb;
                _config.UseMaxAffordable = restoreIdx == 6;
                if (numericUpDownGb != null) numericUpDownGb.Value = restoreIdx;
                if (checkBoxMaxAffordable != null) checkBoxMaxAffordable.Checked = restoreIdx == 6;
                UpdateUploadGbLabel();
                AppendLog($"Loaded upload tier index {restoreIdx} ({TierLabels[restoreIdx]}) from config.");
            }

            // Restore cookie path (detach handler so it doesn't auto-save mid-restore)
            if (textBoxCookieFile != null)
            {
                textBoxCookieFile.TextChanged -= CookieFilePathChanged;
                textBoxCookieFile.Text = _config.CookieFilePath;
                textBoxCookieFile.TextChanged += CookieFilePathChanged;
            }

            // Restore general settings
            checkBoxBuyVip.CheckedChanged -= BuyVipChanged;
            checkBoxBuyVip.Checked = _config.BuyVip;
            checkBoxBuyVip.CheckedChanged += BuyVipChanged;

            checkBoxBuyFlBeforeGb.CheckedChanged -= BuyFlBeforeGbChanged;
            checkBoxBuyFlBeforeGb.Checked = _config.BuyFlBeforeGb;
            checkBoxBuyFlBeforeGb.CheckedChanged += BuyFlBeforeGbChanged;

            checkBoxFlOnly.CheckedChanged -= FlOnlyChanged;
            checkBoxFlOnly.Checked = _config.BuyFlOnly;
            checkBoxFlOnly.CheckedChanged += FlOnlyChanged;

            textBoxPointsBuffer.TextChanged -= PointsBufferChanged;
            textBoxPointsBuffer.Text = _config.PointsBuffer.ToString();
            textBoxPointsBuffer.TextChanged += PointsBufferChanged;

            textBoxNextRun.TextChanged -= NextRunHoursChanged;
            textBoxNextRun.Text = _config.NextRunDelayMinutes.ToString();
            textBoxNextRun.TextChanged += NextRunHoursChanged;

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

                _config.BuyVip = checkBoxBuyVip.Checked;
                _config.BuyFlBeforeGb = checkBoxBuyFlBeforeGb.Checked;
                _config.BuyFlOnly = checkBoxFlOnly.Checked;
                // Source of truth is the tier slider; the hidden combo/numeric are only legacy compat.
                int saveTierIdx = trackBarUploadGb != null
                    ? Math.Clamp(trackBarUploadGb.Value, 0, 6)
                    : (_config.PurchaseTier >= 0 && _config.PurchaseTier <= 6 ? _config.PurchaseTier : 5);
                _config.PurchaseTier = saveTierIdx;
                var (sCost, sGb) = AutomationService.GetTierCostGbPublic((AutomationService.PurchaseTier)saveTierIdx);
                _config.CustomUploadGb = sGb;
                _config.UseMaxAffordable = saveTierIdx == 6;
                if (comboBoxPurchaseTier != null) comboBoxPurchaseTier.SelectedIndex = saveTierIdx;
                if (numericUpDownGb != null) numericUpDownGb.Value = saveTierIdx;

                if (int.TryParse(textBoxPointsBuffer.Text, out int pb))
                    _config.PointsBuffer = pb;

                if (int.TryParse(textBoxNextRun.Text, out int nr))
                    _config.NextRunDelayMinutes = nr;

                _config.CumulativeUploadGB = cumulativeUploadGB;
                _config.CumulativePointsSpent = cumulativePointsSpent;
                _config.NextRunTimeLocal = nextRunTime;

                var json = JsonSerializer.Serialize(_config);
                File.WriteAllText(_configPath, json);
            }
            catch { }
        }
        private async void TimerCountdown_Tick(object? sender, EventArgs e)
        {
            // Hard guard for nullable analysis
            if (labelNextRunCountdown == null)
                return;

            if (!nextRunTime.HasValue)
            {
                labelNextRunCountdown.Text = "";
                return;
            }

            var rem = nextRunTime.Value - DateTime.Now;

            if (rem.TotalSeconds > 0)
            {
                int totalHours = (int)Math.Floor(rem.TotalHours);
                labelNextRunCountdown.Text =
                    $"{totalHours:D2}:{rem.Minutes:D2}:{rem.Seconds:D2}";
                return;
            }

            labelNextRunCountdown.Text = "Ready";

            // If due: run once (guarded by automationRunning)
            if (!automationRunning && !paused)
                await StartAutomationAsync(isManualImmediate: false, flOnlyOverride: false);
        }
    }
}
