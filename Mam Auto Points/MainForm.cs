using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace MAMAutoPoints
{
    public class MainForm : Form
    {
        // Fixed content width for all major sections.
        private const int ContentWidth = 760;

        // --- UI Controls ---
        // Code box (log)
        private TextBox textBoxLog = null!;

        // Settings controls (General Settings)
        private TextBox textBoxPointsBuffer = null!;
        private CheckBox checkBoxBuyVip = null!;
        private TextBox textBoxNextRun = null!;

        // Totals controls
        private Label labelTotalGB = null!;
        private Label labelCumulativePointsValue = null!;
        private Label labelNextRunCountdown = null!;

        // Cookie controls
        private TextBox textBoxCookieFile = null!;
        private Button buttonBrowseCookie = null!;
        private Button buttonEditCookie = null!;
        private Button buttonCreateCookie = null!;

        // Application Controls buttons
        private Button buttonRun = null!;
        private Button buttonPause = null!;
        private Button buttonExit = null!;
        private Button buttonHelpCookie = null!;

        // Timer & automation fields (fully qualified)
        private System.Windows.Forms.Timer timerCountdown = null!;
        private DateTime? nextRunTime = null;
        private int cumulativePointsSpent = 0;
        private int cumulativeUploadGB = 0;
        private bool automationRunning = false;
        private bool paused = false;

        // NotifyIcon for system tray
        private NotifyIcon notifyIcon = null!;
        private bool enableMinimizeToTray = true;

        // --- Layout Containers ---
        // Container panel for fixed-width content.
        private Panel panelContent = null!;
        // Main TableLayoutPanel for grouping sections.
        private TableLayoutPanel tableLayoutMain = null!;

        // GroupBoxes for various sections
        private GroupBox groupBoxUserInfo = null!;
        private GroupBox groupBoxSettings = null!;
        private GroupBox groupBoxTotals = null!;
        private GroupBox groupBoxSystemSettings = null!;
        private GroupBox groupBoxCookieSettings = null!;
        private GroupBox groupBoxAppControls = null!;

        // Additional labels for User Info
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
            // Set form properties.
            this.MinimumSize = new Size(875, 750);
            this.Size = new Size(875, 750);
            this.Text = "MAM Auto Points";
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.AutoScroll = true;

            // Create a container panel for fixed-width content.
            panelContent = new Panel
            {
                Width = ContentWidth,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            this.Controls.Add(panelContent);

            // Create the log (code box) with fixed width.
            textBoxLog = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 10),
                BackColor = Color.Black,
                ForeColor = Color.White,
                Width = ContentWidth,
                Height = 150
            };
            textBoxLog.Location = new Point(0, 10);
            panelContent.Controls.Add(textBoxLog);

            // Create the main TableLayoutPanel for settings.
            tableLayoutMain = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 4, // Row 0: User Info; Row 1: General Settings & Totals; Row 2: System Settings & Cookie Settings; Row 3: Application Controls.
                AutoSize = true,
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };
            tableLayoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            // Define rows:
            tableLayoutMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 160)); // User Information row (fixed height)
            tableLayoutMain.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // General Settings & Totals.
            tableLayoutMain.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // System Settings & Cookie Settings.
            tableLayoutMain.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // Application Controls.
            tableLayoutMain.Width = ContentWidth;
            tableLayoutMain.Location = new Point(0, textBoxLog.Bottom + 10);
            panelContent.Controls.Add(tableLayoutMain);

            // --- Row 0: User Information (spanning both columns) ---
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
            // Add User Information labels.
            Label lblUserNameTitle = new Label
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
            Label lblVipExpiresTitle = new Label
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
            Label lblDownloadedTitle = new Label
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
            Label lblUploadedTitle = new Label
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
            Label lblRatioTitle = new Label
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
            // Add buttons for Lotto and Donate.
            Button btnLotto = new Button
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
            {
                Process.Start(new ProcessStartInfo("https://www.myanonamouse.net/play_lotto.php") { UseShellExecute = true });
            };
            groupBoxUserInfo.Controls.Add(btnLotto);
            Button btnDonate = new Button
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
            {
                Process.Start(new ProcessStartInfo("https://www.myanonamouse.net/millionaires/donate.php") { UseShellExecute = true });
            };
            groupBoxUserInfo.Controls.Add(btnDonate);
            tableLayoutMain.Controls.Add(groupBoxUserInfo, 0, 0);
            tableLayoutMain.SetColumnSpan(groupBoxUserInfo, 2);

            // --- Row 1: General Settings and Totals ---
            // Left: General Settings GroupBox.
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
            Label lblPointsBuffer = new Label
            {
                Text = "Points Buffer:",
                Location = new Point(10, 50),
                AutoSize = true,
                ForeColor = Color.LightBlue
            };
            groupBoxSettings.Controls.Add(lblPointsBuffer);
            textBoxPointsBuffer = new TextBox
            {
                Text = "10000",
                Width = 100,
                Location = new Point(150, 50),
                BackColor = Color.Black,
                ForeColor = Color.White
            };
            groupBoxSettings.Controls.Add(textBoxPointsBuffer);
            Label lblNextRunDelay = new Label
            {
                Text = "Next Run Delay (hours):",
                Location = new Point(10, 80),
                AutoSize = true,
                ForeColor = Color.Plum
            };
            groupBoxSettings.Controls.Add(lblNextRunDelay);
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

            // Right: Totals GroupBox.
            groupBoxTotals = new GroupBox
            {
                Text = "Totals",
                AutoSize = true,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White
            };
            Label lblTotalGBTitle = new Label
            {
                Text = "Total GB Bought:",
                Location = new Point(10, 25),
                AutoSize = true
            };
            groupBoxTotals.Controls.Add(lblTotalGBTitle);
            labelTotalGB = new Label
            {
                Text = "0",
                Location = new Point(180, 25),
                AutoSize = true
            };
            groupBoxTotals.Controls.Add(labelTotalGB);
            Label lblCumulativePointsTitle = new Label
            {
                Text = "Cumulative Points Spent:",
                Location = new Point(10, 55),
                AutoSize = true
            };
            groupBoxTotals.Controls.Add(lblCumulativePointsTitle);
            labelCumulativePointsValue = new Label
            {
                Text = "0",
                Location = new Point(180, 55),
                AutoSize = true
            };
            groupBoxTotals.Controls.Add(labelCumulativePointsValue);
            Label lblNextRunInTitle = new Label
            {
                Text = "Next Run In:",
                Location = new Point(10, 85),
                AutoSize = true
            };
            groupBoxTotals.Controls.Add(lblNextRunInTitle);
            labelNextRunCountdown = new Label
            {
                Text = "",
                Location = new Point(180, 85),
                AutoSize = true
            };
            groupBoxTotals.Controls.Add(labelNextRunCountdown);
            tableLayoutMain.Controls.Add(groupBoxTotals, 1, 1);

            // --- Row 2: System Settings and Cookie Settings ---
            // Left: System Settings GroupBox.
            groupBoxSystemSettings = new GroupBox
            {
                Text = "System Settings",
                AutoSize = true,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White
            };
            CheckBox chkAutoStart = new CheckBox
            {
                Text = "Start with Windows",
                Location = new Point(10, 25),
                AutoSize = true,
                ForeColor = Color.LightGreen
            };
            chkAutoStart.CheckedChanged += (s, e) => { /* AutoStart logic here */ };
            groupBoxSystemSettings.Controls.Add(chkAutoStart);
            CheckBox chkMinimizeTray = new CheckBox
            {
                Text = "Minimize to System Tray",
                Location = new Point(200, 25),
                AutoSize = true,
                Checked = true,
                ForeColor = Color.LightGreen
            };
            chkMinimizeTray.CheckedChanged += (s, e) => { /* Minimize-to-tray logic here */ };
            groupBoxSystemSettings.Controls.Add(chkMinimizeTray);
            tableLayoutMain.Controls.Add(groupBoxSystemSettings, 0, 2);

            // Right: Cookie Settings GroupBox.
            groupBoxCookieSettings = new GroupBox
            {
                Text = "Cookie Settings",
                AutoSize = true,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White
            };
            Label lblCookieFile = new Label
            {
                Text = "Cookies File:",
                Location = new Point(10, 25),
                AutoSize = true,
                ForeColor = Color.Orange
            };
            groupBoxCookieSettings.Controls.Add(lblCookieFile);
            textBoxCookieFile = new TextBox
            {
                Text = "",
                Width = 200,
                Location = new Point(110, 22),
                BackColor = Color.Black,
                ForeColor = Color.White
            };
            groupBoxCookieSettings.Controls.Add(textBoxCookieFile);
            buttonBrowseCookie = new Button
            {
                Text = "Select File",
                Size = new Size(100, 30),
                Location = new Point(10, 60),
                BackColor = Color.DimGray,
                ForeColor = Color.White
            };
            buttonBrowseCookie.Click += (s, e) => {
                using (OpenFileDialog ofd = new OpenFileDialog { Filter = "Cookie Files (*.cookies)|*.cookies|All Files (*.*)|*.*" })
                {
                    if (ofd.ShowDialog() == DialogResult.OK)
                        textBoxCookieFile.Text = ofd.FileName;
                }
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
            buttonEditCookie.Click += (s, e) => {
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
            buttonCreateCookie.Click += (s, e) => {
                string uniqueId = Microsoft.VisualBasic.Interaction.InputBox("Enter your security string:", "Create Cookie", "");
                if (!string.IsNullOrEmpty(uniqueId))
                {
                    using (SaveFileDialog sfd = new SaveFileDialog())
                    {
                        sfd.Filter = "Cookie Files (*.cookies)|*.cookies|All Files (*.*)|*.*";
                        sfd.FileName = "MAM.cookies";
                        if (sfd.ShowDialog() == DialogResult.OK)
                        {
                            System.IO.File.WriteAllText(sfd.FileName, uniqueId);
                            textBoxCookieFile.Text = sfd.FileName;
                        }
                    }
                }
            };
            groupBoxCookieSettings.Controls.Add(buttonCreateCookie);
            tableLayoutMain.Controls.Add(groupBoxCookieSettings, 1, 2);

            // --- Row 3: Application Controls ---
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
            buttonRun.Click += (s, e) => { /* Run script logic */ };
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
            buttonPause.Click += (s, e) => { /* Pause logic */ };
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
            buttonExit.Click += (s, e) => { this.Close(); };
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
            buttonHelpCookie.Click += (s, e) => {
                MessageBox.Show("Instructions:\n1. ...\n2. ...", "Instructions");
            };
            groupBoxAppControls.Controls.Add(buttonHelpCookie);
            tableLayoutMain.Controls.Add(groupBoxAppControls, 0, 3);
            tableLayoutMain.SetColumnSpan(groupBoxAppControls, 2);

            // Center the container panel.
            CenterContent();

            // Setup system tray NotifyIcon.
            notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = false,
                Text = "MAM Auto Points"
            };
            var trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Show", null, (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; });
            trayMenu.Items.Add("Exit", null, (s, e) => { Application.Exit(); });
            notifyIcon.ContextMenuStrip = trayMenu;
            notifyIcon.DoubleClick += (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; };

            // Setup timer for automation countdown using System.Windows.Forms.Timer.
            timerCountdown = new System.Windows.Forms.Timer { Interval = 1000 };
            timerCountdown.Tick += (s, e) =>
            {
                if (nextRunTime.HasValue)
                {
                    TimeSpan remaining = nextRunTime.Value - DateTime.Now;
                    labelNextRunCountdown.Text = remaining.TotalSeconds > 0 ?
                        string.Format("{0:D2}:{1:D2}:{2:D2}", remaining.Hours, remaining.Minutes, remaining.Seconds) :
                        "Ready";
                    if (remaining.TotalSeconds <= 0 && !automationRunning)
                    {
                        nextRunTime = null;
                        buttonRun.PerformClick();
                    }
                }
                else
                {
                    labelNextRunCountdown.Text = "";
                }
            };
            timerCountdown.Start();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // Ensure panelContent is not null.
            if (panelContent == null)
            {
                panelContent = new Panel
                {
                    Width = ContentWidth,
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                this.Controls.Add(panelContent);
            }
            CenterContent();
            // Set form's client width based on content width plus padding.
            this.ClientSize = new Size(ContentWidth + 20, this.ClientSize.Height);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            CenterContent();
            if (this.WindowState == FormWindowState.Minimized && enableMinimizeToTray)
            {
                this.Hide();
                notifyIcon.Visible = true;
                notifyIcon.ShowBalloonTip(3000, "MAM Auto Points", "Application minimized to tray.", ToolTipIcon.Info);
            }
        }

        private void CenterContent()
        {
            // Center the container panel horizontally.
            int leftOffset = (this.ClientSize.Width - ContentWidth) / 2;
            if (panelContent != null)
            {
                panelContent.Left = leftOffset;
            }
        }
    }
}