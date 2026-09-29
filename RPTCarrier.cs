using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace RPTCarrier
{
    internal sealed class CarrierConfig
    {
        public string Target = "";
        public string Arguments = "";
        public string WorkingDirectory = "";
        public string WindowTitleContains = "";
        public bool Embed = true;
        public string PresentationMode = "handoff";
        public bool KeepCarrierTopMost = true;
        public bool FocusGameAfterMirror = true;
        public bool MatchTargetSize = true;
        public bool HideUiWhenGameStarts = true;
        public bool AutoLaunch = false;
        public bool Fullscreen = false;
        public bool CloseWithGame = true;
        public bool ControllerProbe = true;
        public bool NwOverlayCompat = true;
        public bool NwOverlayCompatExplicit = false;
        public bool ForceNwInProcessGpu = false;
        public int WarmupMs = 2500;
        public int WindowTimeoutMs = 20000;
        public string ConfigPath = "";
    }

    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate(object sender, System.Threading.ThreadExceptionEventArgs e)
            {
                WriteCrashLog("UI", e.Exception);
                MessageBox.Show(
                    "RPT Carrierでエラーが発生しました。\r\n\r\n" + e.Exception.Message +
                    "\r\n\r\n詳細は RPTCarrier.log を確認してください。",
                    "RPT Carrier",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            };
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
            {
                WriteCrashLog("AppDomain", e.ExceptionObject as Exception);
            };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            CarrierConfig config;
            try
            {
                config = ConfigLoader.Load(args);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "RPT Carrier configuration error:\r\n\r\n" + ex.Message,
                    "RPT Carrier",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            Application.Run(new CarrierForm(config));
        }

        private static void WriteCrashLog(string source, Exception ex)
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RPTCarrier.log");
                string text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") +
                    " [CRASH " + source + "] " +
                    (ex == null ? "(unknown exception)" : ex.ToString()) +
                    Environment.NewLine;
                File.AppendAllText(path, text, Encoding.UTF8);
            }
            catch
            {
            }
        }
    }

    internal static class ConfigLoader
    {
        public static CarrierConfig Load(string[] args)
        {
            var c = new CarrierConfig();

            string defaultConfig = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "RPTCarrier.ini");

            if (File.Exists(defaultConfig))
                ReadIni(c, defaultConfig, false);

            string explicitConfig = "";
            for (int i = 0; i < args.Length; i++)
            {
                if ((args[i].Equals("--config", StringComparison.OrdinalIgnoreCase) ||
                     args[i].Equals("-c", StringComparison.OrdinalIgnoreCase)) &&
                    i + 1 < args.Length)
                {
                    explicitConfig = Normalize(args[i + 1]);
                    break;
                }
            }

            if (!String.IsNullOrWhiteSpace(explicitConfig))
                ReadIni(c, explicitConfig, true);

            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];

                if (a.Equals("--config", StringComparison.OrdinalIgnoreCase) ||
                    a.Equals("-c", StringComparison.OrdinalIgnoreCase))
                {
                    i++;
                }
                else if (a.Equals("--target", StringComparison.OrdinalIgnoreCase) ||
                         a.Equals("-t", StringComparison.OrdinalIgnoreCase))
                {
                    c.Target = NeedJoinedValue(args, ref i, a);
                }
                else if (a.StartsWith("--target=", StringComparison.OrdinalIgnoreCase))
                {
                    c.Target = Normalize(a.Substring("--target=".Length));
                }
                else if (a.Equals("--args", StringComparison.OrdinalIgnoreCase) ||
                         a.Equals("-a", StringComparison.OrdinalIgnoreCase))
                {
                    c.Arguments = NeedValue(args, ref i, a);
                }
                else if (a.Equals("--cwd", StringComparison.OrdinalIgnoreCase))
                {
                    c.WorkingDirectory = NeedJoinedValue(args, ref i, a);
                }
                else if (a.StartsWith("--cwd=", StringComparison.OrdinalIgnoreCase))
                {
                    c.WorkingDirectory = Normalize(a.Substring("--cwd=".Length));
                }
                else if (a.Equals("--window-title", StringComparison.OrdinalIgnoreCase))
                {
                    c.WindowTitleContains = NeedValue(args, ref i, a);
                }
                else if (a.Equals("--auto-launch", StringComparison.OrdinalIgnoreCase))
                {
                    c.AutoLaunch = true;
                }
                else if (a.Equals("--manual-launch", StringComparison.OrdinalIgnoreCase))
                {
                    c.AutoLaunch = false;
                }
                else if (a.Equals("--no-embed", StringComparison.OrdinalIgnoreCase))
                {
                    c.Embed = false;
                    c.PresentationMode = "separate";
                }
                else if (a.Equals("--mirror", StringComparison.OrdinalIgnoreCase))
                {
                    c.PresentationMode = "mirror";
                }
                else if (a.Equals("--embed", StringComparison.OrdinalIgnoreCase))
                {
                    c.PresentationMode = "embed";
                    c.Embed = true;
                }
                else if (a.Equals("--handoff", StringComparison.OrdinalIgnoreCase))
                {
                    c.PresentationMode = "handoff";
                    c.Embed = false;
                }
                else if (a.Equals("--separate", StringComparison.OrdinalIgnoreCase))
                {
                    c.PresentationMode = "separate";
                    c.Embed = false;
                }
                else if (a.Equals("--windowed-carrier", StringComparison.OrdinalIgnoreCase))
                {
                    c.Fullscreen = false;
                }
                else if (a.Equals("--controller-probe", StringComparison.OrdinalIgnoreCase))
                {
                    c.ControllerProbe = true;
                }
                else if (a.Equals("--no-controller-probe", StringComparison.OrdinalIgnoreCase))
                {
                    c.ControllerProbe = false;
                }
                else if (a.Equals("--nw-overlay-compat", StringComparison.OrdinalIgnoreCase))
                {
                    c.NwOverlayCompat = true;
                    c.NwOverlayCompatExplicit = true;
                }
                else if (a.Equals("--no-nw-overlay-compat", StringComparison.OrdinalIgnoreCase))
                {
                    c.NwOverlayCompat = false;
                    c.NwOverlayCompatExplicit = true;
                }
                else if (a.Equals("--force-nw-in-process-gpu", StringComparison.OrdinalIgnoreCase))
                {
                    c.NwOverlayCompat = true;
                    c.NwOverlayCompatExplicit = true;
                    c.ForceNwInProcessGpu = true;
                }
                else if (a.StartsWith("-", StringComparison.Ordinal))
                {
                    throw new ArgumentException("Unknown option: " + a);
                }
                else if (String.IsNullOrWhiteSpace(c.Target))
                {
                    c.Target = Normalize(a);
                }
                else
                {
                    throw new ArgumentException("Unexpected argument: " + a);
                }
            }

            if (!String.IsNullOrWhiteSpace(c.Target))
            {
                c.Target = Path.GetFullPath(
                    Environment.ExpandEnvironmentVariables(c.Target));

                if (!File.Exists(c.Target))
                    throw new FileNotFoundException("Target EXE was not found.", c.Target);

                if (String.IsNullOrWhiteSpace(c.WorkingDirectory))
                    c.WorkingDirectory = Path.GetDirectoryName(c.Target);
            }

            if (!String.IsNullOrWhiteSpace(c.WorkingDirectory))
            {
                c.WorkingDirectory = Path.GetFullPath(
                    Environment.ExpandEnvironmentVariables(c.WorkingDirectory));
            }

            c.PresentationMode = (c.PresentationMode ?? "handoff").Trim().ToLowerInvariant();
            if (c.PresentationMode != "handoff" &&
                c.PresentationMode != "mirror" &&
                c.PresentationMode != "embed" &&
                c.PresentationMode != "separate")
            {
                throw new ArgumentException("PresentationMode must be handoff, mirror, embed, or separate.");
            }

            return c;
        }

        private static void ReadIni(CarrierConfig c, string path, bool required)
        {
            string full;
            try
            {
                full = Path.GetFullPath(
                    Environment.ExpandEnvironmentVariables(Normalize(path)));
            }
            catch (Exception ex)
            {
                if (required)
                    throw new ArgumentException("Cannot resolve config path: " + ex.Message);
                return;
            }

            if (!File.Exists(full))
            {
                if (required)
                    throw new FileNotFoundException("Config file was not found.", full);
                return;
            }

            foreach (string raw in File.ReadAllLines(full, Encoding.UTF8))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";"))
                    continue;

                int eq = line.IndexOf('=');
                if (eq <= 0)
                    continue;

                string key = line.Substring(0, eq).Trim().ToLowerInvariant();
                string value = Normalize(line.Substring(eq + 1));

                switch (key)
                {
                    case "target":
                        c.Target = value;
                        break;
                    case "args":
                    case "arguments":
                        c.Arguments = value;
                        break;
                    case "cwd":
                    case "workingdirectory":
                        c.WorkingDirectory = value;
                        break;
                    case "windowtitlecontains":
                        c.WindowTitleContains = value;
                        break;
                    case "embed":
                        c.Embed = ParseBool(value, c.Embed);
                        if (!c.Embed && c.PresentationMode == "embed")
                            c.PresentationMode = "separate";
                        break;
                    case "presentation":
                    case "presentationmode":
                    case "mode":
                        c.PresentationMode = value.ToLowerInvariant();
                        break;
                    case "keepcarriertopmost":
                        c.KeepCarrierTopMost = ParseBool(value, c.KeepCarrierTopMost);
                        break;
                    case "focusgameaftermirror":
                        c.FocusGameAfterMirror = ParseBool(value, c.FocusGameAfterMirror);
                        break;
                    case "matchtargetsize":
                        c.MatchTargetSize = ParseBool(value, c.MatchTargetSize);
                        break;
                    case "hideuiwhengamestarts":
                        c.HideUiWhenGameStarts = ParseBool(value, c.HideUiWhenGameStarts);
                        break;
                    case "autolaunch":
                        c.AutoLaunch = ParseBool(value, c.AutoLaunch);
                        break;
                    case "fullscreen":
                        c.Fullscreen = ParseBool(value, c.Fullscreen);
                        break;
                    case "closewithgame":
                        c.CloseWithGame = ParseBool(value, c.CloseWithGame);
                        break;
                    case "controllerprobe":
                        c.ControllerProbe = ParseBool(value, c.ControllerProbe);
                        break;
                    case "nwoverlaycompat":
                        c.NwOverlayCompat = ParseBool(value, c.NwOverlayCompat);
                        break;
                    case "forcenwinprocessgpu":
                        c.ForceNwInProcessGpu = ParseBool(value, c.ForceNwInProcessGpu);
                        break;
                    case "warmupms":
                        c.WarmupMs = ParseInt(value, c.WarmupMs, 0, 60000);
                        break;
                    case "windowtimeoutms":
                        c.WindowTimeoutMs = ParseInt(value, c.WindowTimeoutMs, 1000, 120000);
                        break;
                }
            }

            c.ConfigPath = full;
        }

        private static bool ParseBool(string s, bool fallback)
        {
            bool v;
            return Boolean.TryParse(s, out v) ? v : fallback;
        }

        private static int ParseInt(string s, int fallback, int min, int max)
        {
            int v;
            if (!Int32.TryParse(s, out v))
                return fallback;
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        private static string NeedJoinedValue(string[] args, ref int index, string option)
        {
            if (index + 1 >= args.Length)
                throw new ArgumentException(option + " requires a value.");

            int start = index + 1;
            int end = start;

            while (end + 1 < args.Length && !LooksLikeOption(args[end + 1]))
                end++;

            var sb = new StringBuilder();
            for (int j = start; j <= end; j++)
            {
                if (sb.Length > 0)
                    sb.Append(" ");
                sb.Append(args[j]);
            }

            index = end;
            return Normalize(sb.ToString());
        }

        private static bool LooksLikeOption(string value)
        {
            if (String.IsNullOrEmpty(value))
                return false;

            return value.StartsWith("-", StringComparison.Ordinal);
        }

        private static string NeedValue(string[] args, ref int index, string option)
        {
            if (index + 1 >= args.Length)
                throw new ArgumentException(option + " requires a value.");

            index++;
            return Normalize(args[index]);
        }

        private static string Normalize(string value)
        {
            if (value == null)
                return "";

            string v = value.Trim();
            if (v.Length >= 2 &&
                ((v[0] == '"' && v[v.Length - 1] == '"') ||
                 (v[0] == '\'' && v[v.Length - 1] == '\'')))
            {
                v = v.Substring(1, v.Length - 2);
            }
            return v;
        }
    }

    internal sealed class CarrierForm : Form
    {
        private readonly CarrierConfig _config;
        private readonly System.Windows.Forms.Timer _renderTimer = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer _windowTimer = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer _gameWatchTimer = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer _controllerTimer = new System.Windows.Forms.Timer();

        private IntPtr _dc = IntPtr.Zero;
        private IntPtr _gl = IntPtr.Zero;
        private IntPtr _dwmThumbnail = IntPtr.Zero;

        private Panel _controlPanel;
        private Button _launchButton;
        private Button _carrierFocusButton;
        private Button _gameFocusButton;
        private Button _fullscreenButton;
        private Button _controllerTestButton;
        private Label _statusLabel;
        private Label _targetLabel;
        private Panel _controllerPanel;
        private Label _controllerStatusLabel;
        private bool _isFullscreen;
        private bool _gameViewMode;
        private FormBorderStyle _preGameBorderStyle;
        private Rectangle _preGameBounds;
        private Rectangle _windowedBounds;

        private Process _rootProcess;
        private int _rootPid;
        private int _gamePid;
        private IntPtr _gameWindow = IntPtr.Zero;
        private IntPtr _oldParent = IntPtr.Zero;
        private IntPtr _oldStyle = IntPtr.Zero;
        private IntPtr _oldExStyle = IntPtr.Zero;
        private Native.RECT _oldRect;

        private DateTime _windowSearchStarted;
        private bool _gameHidden;
        private bool _gameHasInputFocus;
        private int _mirrorX;
        private int _mirrorY;
        private int _mirrorWidth;
        private int _mirrorHeight;
        private int _sourceClientWidth;
        private int _sourceClientHeight;
        private bool _launchStarted;
        private bool _closing;
        private bool _controllerPanelVisible = true;
        private bool _xinput14Available = true;
        private bool _xinput910Available = true;
        private int _controllerUiDivider;
        private string _lastControllerEvent = "none";
        private readonly uint[] _lastXInputPacket = new uint[4];
        private readonly bool[] _lastXInputConnected = new bool[4];
        private string _logPath;

        private const int HOTKEY_LAUNCH = 0x7701;
        private const int HOTKEY_TOGGLE_GAME = 0x7702;
        private const int HOTKEY_REFIT = 0x7703;

        public CarrierForm(CarrierConfig config)
        {
            _config = config;

            Text = "RPT Carrier";
            BackColor = Color.Black;
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;

            if (_config.Fullscreen)
            {
                FormBorderStyle = FormBorderStyle.None;
                Bounds = Screen.PrimaryScreen.Bounds;
                WindowState = FormWindowState.Normal;
                _isFullscreen = true;
            }
            else
            {
                FormBorderStyle = FormBorderStyle.Sizable;
                ClientSize = new Size(1280, 760);
                _isFullscreen = false;
            }

            _windowedBounds = Bounds;
            TopMost = false;

            BuildControlUi();
            _preGameBorderStyle = FormBorderStyle;
            _preGameBounds = Bounds;

            _renderTimer.Interval = 16;
            _renderTimer.Tick += delegate { RenderHostSurface(); };

            _windowTimer.Interval = 100;
            _windowTimer.Tick += delegate { PollForGameWindow(); };

            _gameWatchTimer.Interval = 500;
            _gameWatchTimer.Tick += delegate { WatchGame(); };

            _controllerTimer.Interval = 16;
            _controllerTimer.Tick += delegate
            {
                try
                {
                    PollControllers();
                }
                catch (Exception ex)
                {
                    _controllerTimer.Stop();
                    Log("Controller probe disabled after error: " + ex);
                    if (_controllerStatusLabel != null && !_controllerStatusLabel.IsDisposed)
                        _controllerStatusLabel.Text = "Controller probe error. See RPTCarrier.log";
                }
            };

            Shown += OnShown;
            Resize += delegate { UpdatePresentation(); };
            FormClosing += OnCarrierClosing;

            _logPath = ResolveLogPath();
            Log("RPT Carrier 1.4 starting");
            Log("Config: " + (_config.ConfigPath.Length == 0 ? "(command line/defaults)" : _config.ConfigPath));
            LogEnvironment();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            try
            {
                InitOpenGL();
            }
            catch (Exception ex)
            {
                Log("OpenGL init failed: " + ex);
                MessageBox.Show(
                    "Carrier graphics surface could not be created.\r\n\r\n" + ex.Message,
                    "RPT Carrier",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                BeginInvoke(new Action(Close));
                return;
            }

            Native.RegisterHotKey(Handle, HOTKEY_LAUNCH,
                Native.MOD_CONTROL | Native.MOD_ALT | Native.MOD_NOREPEAT,
                (uint)Keys.F7);

            Native.RegisterHotKey(Handle, HOTKEY_TOGGLE_GAME,
                Native.MOD_CONTROL | Native.MOD_ALT | Native.MOD_NOREPEAT,
                (uint)Keys.F8);

            Native.RegisterHotKey(Handle, HOTKEY_REFIT,
                Native.MOD_CONTROL | Native.MOD_ALT | Native.MOD_NOREPEAT,
                (uint)Keys.F9);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (IsHandleCreated)
            {
                Native.UnregisterHotKey(Handle, HOTKEY_LAUNCH);
                Native.UnregisterHotKey(Handle, HOTKEY_TOGGLE_GAME);
                Native.UnregisterHotKey(Handle, HOTKEY_REFIT);
            }

            DestroyOpenGL();
            base.OnHandleDestroyed(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (_gameViewMode &&
                _config.PresentationMode == "mirror" &&
                m.Msg == Native.WM_MOUSEACTIVATE)
            {
                int hitTest = Native.LOWORD(m.LParam);
                if (hitTest == Native.HTCLIENT)
                {
                    m.Result = new IntPtr(Native.MA_NOACTIVATE);
                    return;
                }
            }

            if (m.Msg == Native.WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();

                if (id == HOTKEY_LAUNCH)
                {
                    LaunchTarget();
                    return;
                }

                if (id == HOTKEY_TOGGLE_GAME)
                {
                    ToggleGameOrCarrierFocus();
                    return;
                }

                if (id == HOTKEY_REFIT)
                {
                    UpdatePresentation();
                    return;
                }
            }

            if (_config.PresentationMode == "mirror" &&
                _gameWindow != IntPtr.Zero &&
                Native.IsWindow(_gameWindow) &&
                IsForwardedMouseMessage(m.Msg))
            {
                if (ForwardMirrorMouse(ref m))
                    return;
            }

            base.WndProc(ref m);
        }

        private void BuildControlUi()
        {
            _controlPanel = new Panel();
            _controlPanel.Dock = DockStyle.Top;
            _controlPanel.Height = 58;
            _controlPanel.BackColor = Color.FromArgb(34, 34, 38);
            _controlPanel.Padding = new Padding(8, 8, 8, 8);

            _launchButton = CreateUiButton(
                String.IsNullOrWhiteSpace(_config.Target) ? "EXEを選択..." : "ゲーム起動",
                112);
            _launchButton.Click += delegate
            {
                if (String.IsNullOrWhiteSpace(_config.Target))
                {
                    if (PromptForTargetExecutable())
                        UpdateUiState();
                    return;
                }

                LaunchTarget();
            };

            _carrierFocusButton = CreateUiButton("Steam / Carrier", 128);
            _carrierFocusButton.Click += delegate { FocusCarrierForSteam(); };

            _gameFocusButton = CreateUiButton("ゲーム操作", 112);
            _gameFocusButton.Click += delegate { FocusGameForInput(); };

            _fullscreenButton = CreateUiButton(_isFullscreen ? "ウィンドウ表示" : "全画面", 112);
            _fullscreenButton.Click += delegate { ToggleFullscreenMode(); };

            _controllerTestButton = CreateUiButton("コントローラーテスト", 148);
            _controllerTestButton.Click += delegate
            {
                _controllerPanelVisible = !_controllerPanelVisible;
                if (_controllerPanel != null)
                    _controllerPanel.Visible = _controllerPanelVisible;
            };

            _targetLabel = new Label();
            _targetLabel.AutoSize = false;
            _targetLabel.ForeColor = Color.Gainsboro;
            _targetLabel.TextAlign = ContentAlignment.MiddleLeft;
            _targetLabel.Width = 340;
            _targetLabel.Dock = DockStyle.Left;
            _targetLabel.Padding = new Padding(10, 0, 4, 0);
            _targetLabel.Text = String.IsNullOrWhiteSpace(_config.Target)
                ? "Target: 未設定"
                : "Target: " + Path.GetFileName(_config.Target);

            _statusLabel = new Label();
            _statusLabel.AutoSize = false;
            _statusLabel.ForeColor = Color.White;
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            _statusLabel.Dock = DockStyle.Fill;
            _statusLabel.Padding = new Padding(12, 0, 8, 0);
            _statusLabel.Text = "待機中。Steam招待はCarrier側でShift+Tab。";

            _controlPanel.Controls.Add(_statusLabel);
            _controlPanel.Controls.Add(_targetLabel);
            _controlPanel.Controls.Add(_fullscreenButton);
            _controlPanel.Controls.Add(_controllerTestButton);
            _controlPanel.Controls.Add(_gameFocusButton);
            _controlPanel.Controls.Add(_carrierFocusButton);
            _controlPanel.Controls.Add(_launchButton);

            Controls.Add(_controlPanel);
            _controlPanel.BringToFront();

            _controllerPanel = new Panel();
            _controllerPanel.Dock = DockStyle.Bottom;
            _controllerPanel.Height = 66;
            _controllerPanel.BackColor = Color.FromArgb(24, 24, 28);
            _controllerPanel.Padding = new Padding(10, 6, 10, 6);

            _controllerStatusLabel = new Label();
            _controllerStatusLabel.Dock = DockStyle.Fill;
            _controllerStatusLabel.ForeColor = Color.White;
            _controllerStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
            _controllerStatusLabel.Font = new Font(FontFamily.GenericMonospace, 9.0f);
            _controllerStatusLabel.Text =
                "Controller probe: starting...\r\n" +
                "XInput 1:[-] 2:[-] 3:[-] 4:[-]   WinMM joystick: checking...";

            _controllerPanel.Controls.Add(_controllerStatusLabel);
            Controls.Add(_controllerPanel);
            _controllerPanel.BringToFront();

            UpdateUiState();
        }

        private Button CreateUiButton(string text, int width)
        {
            var b = new Button();
            b.Text = text;
            b.Width = width;
            b.Dock = DockStyle.Left;
            b.Margin = new Padding(4);
            b.FlatStyle = FlatStyle.System;
            return b;
        }

        private void SetStatus(string text)
        {
            if (_statusLabel != null)
                _statusLabel.Text = text;
            Log("STATUS " + text);
        }

        private void UpdateUiState()
        {
            if (_launchButton != null)
            {
                _launchButton.Enabled = !_launchStarted;
                _launchButton.Text = String.IsNullOrWhiteSpace(_config.Target)
                    ? "EXEを選択..."
                    : "ゲーム起動";
            }

            if (_targetLabel != null)
            {
                _targetLabel.Text = String.IsNullOrWhiteSpace(_config.Target)
                    ? "Target: 未設定"
                    : "Target: " + Path.GetFileName(_config.Target);
            }

            bool hasGame = _gameWindow != IntPtr.Zero && Native.IsWindow(_gameWindow);

            if (_gameFocusButton != null)
                _gameFocusButton.Enabled = hasGame;

            if (_carrierFocusButton != null)
                _carrierFocusButton.Enabled = true;

            if (_fullscreenButton != null)
                _fullscreenButton.Text = _isFullscreen ? "ウィンドウ表示" : "全画面";
        }

        private void FocusCarrierForSteam()
        {
            TopMost = _config.KeepCarrierTopMost;
            Activate();
            Native.SetForegroundWindow(Handle);
            Native.SetFocus(Handle);
            _gameHasInputFocus = false;
            SetStatus("Carrier操作中。Shift+TabでSteam Overlayを開けます。");
            UpdateUiState();
        }

        private void FocusGameForInput()
        {
            if (_gameWindow == IntPtr.Zero || !Native.IsWindow(_gameWindow))
            {
                SetStatus("ゲーム画面がまだ見つかっていません。");
                return;
            }

            if (_config.PresentationMode == "embed")
            {
                if (_gameHidden)
                {
                    _gameHidden = false;
                    Native.ShowWindow(_gameWindow, Native.SW_SHOW);
                    FitEmbeddedWindow();
                }

                FocusEmbeddedGameOnce();
                _gameHasInputFocus = true;
                SetStatus("ゲーム操作中。入力はゲーム本体へ直接入ります。");
                UpdateUiState();
                return;
            }

            FocusExternalWindow(_gameWindow);
            _gameHasInputFocus = true;
            SetStatus("ゲーム操作中。");
            UpdateUiState();
        }

        private void FocusExternalWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !Native.IsWindow(hwnd))
                return;

            // Do not use AttachThreadInput here.
            // Attaching/detaching input queues resets Windows key state and can
            // make a released key/button look permanently held by the game.
            Native.ShowWindow(hwnd, Native.SW_SHOW);
            Native.SetForegroundWindow(hwnd);
        }

        private static bool IsForwardedMouseMessage(int msg)
        {
            return msg == Native.WM_MOUSEMOVE ||
                   msg == Native.WM_LBUTTONDOWN ||
                   msg == Native.WM_LBUTTONUP ||
                   msg == Native.WM_RBUTTONDOWN ||
                   msg == Native.WM_RBUTTONUP ||
                   msg == Native.WM_MBUTTONDOWN ||
                   msg == Native.WM_MBUTTONUP ||
                   msg == Native.WM_MOUSEWHEEL;
        }

        private bool ForwardMirrorMouse(ref Message m)
        {
            if (_mirrorWidth <= 0 || _mirrorHeight <= 0 ||
                _sourceClientWidth <= 0 || _sourceClientHeight <= 0)
                return false;

            int x = Native.GET_X_LPARAM(m.LParam);
            int y = Native.GET_Y_LPARAM(m.LParam);

            if (m.Msg == Native.WM_MOUSEWHEEL)
            {
                Point screen = new Point(x, y);
                Point client = PointToClient(screen);
                x = client.X;
                y = client.Y;
            }

            if (x < _mirrorX || y < _mirrorY ||
                x >= _mirrorX + _mirrorWidth ||
                y >= _mirrorY + _mirrorHeight)
                return false;

            int gx = (int)((long)(x - _mirrorX) * _sourceClientWidth / Math.Max(1, _mirrorWidth));
            int gy = (int)((long)(y - _mirrorY) * _sourceClientHeight / Math.Max(1, _mirrorHeight));

            gx = Math.Max(0, Math.Min(_sourceClientWidth - 1, gx));
            gy = Math.Max(0, Math.Min(_sourceClientHeight - 1, gy));

            if (m.Msg == Native.WM_MOUSEWHEEL)
            {
                Point screenPoint = Native.ClientToScreenPoint(_gameWindow, new Point(gx, gy));
                IntPtr wheelLParam = Native.MAKELPARAM(screenPoint.X, screenPoint.Y);
                Native.PostMessage(_gameWindow, m.Msg, m.WParam, wheelLParam);
            }
            else
            {
                IntPtr gameLParam = Native.MAKELPARAM(gx, gy);
                Native.PostMessage(_gameWindow, m.Msg, m.WParam, gameLParam);
            }

            return true;
        }



        private void ToggleFullscreenMode()
        {
            if (_gameViewMode)
                return;

            if (!_isFullscreen)
            {
                _windowedBounds = Bounds;
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Normal;
                Bounds = Screen.FromControl(this).Bounds;
                _isFullscreen = true;
                SetStatus("全画面表示。Steam Overlayを出すなら「Steam / Carrier」→ Shift+Tab。");
            }
            else
            {
                FormBorderStyle = FormBorderStyle.Sizable;
                WindowState = FormWindowState.Normal;

                if (_windowedBounds.Width >= 320 && _windowedBounds.Height >= 240)
                    Bounds = _windowedBounds;
                else
                    ClientSize = new Size(1280, 760);

                _isFullscreen = false;
                SetStatus("ウィンドウ表示。");
            }

            UpdateUiState();
            BeginInvoke(new Action(UpdatePresentation));
        }

        private int PresentationTop()
        {
            if (_controlPanel == null || !_controlPanel.Visible)
                return 0;
            return _controlPanel.Height;
        }

        private int PresentationBottom()
        {
            if (_controllerPanel == null || !_controllerPanel.Visible)
                return 0;
            return _controllerPanel.Height;
        }

        private void OnShown(object sender, EventArgs e)
        {
            _renderTimer.Start();

            if (String.IsNullOrWhiteSpace(_config.Target))
            {
                BeginInvoke(new Action(delegate
                {
                    if (!PromptForTargetExecutable())
                    {
                        SetStatus("EXEが未選択です。「EXEを選択...」から選択できます。");
                    }
                }));
            }

            if (_config.ControllerProbe)
            {
                _controllerTimer.Start();
                try
                {
                    PollControllers();
                }
                catch (Exception ex)
                {
                    _controllerTimer.Stop();
                    Log("Initial controller probe failed: " + ex);
                }
                Log("Controller probe active: XInput 0-3 + WinMM legacy joystick polling. DirectInput COM probe disabled for stability.");
            }

            Log("Carrier HWND: 0x" + Handle.ToInt64().ToString("X"));
            Log("Carrier is rendering an OpenGL swap surface.");
            Log("Presentation mode: " + _config.PresentationMode);
            Log("Hotkeys: Ctrl+Alt+F7 launch, Ctrl+Alt+F8 switch game/carrier focus, Ctrl+Alt+F9 refit.");

            Activate();
            Native.SetForegroundWindow(Handle);

            if (_config.AutoLaunch)
            {
                var warmup = new System.Windows.Forms.Timer();
                warmup.Interval = Math.Max(1, _config.WarmupMs);
                warmup.Tick += delegate
                {
                    warmup.Stop();
                    warmup.Dispose();
                    LaunchTarget();
                };
                warmup.Start();
                Log("Auto launch scheduled after " + _config.WarmupMs + " ms.");
            }
            else
            {
                Log("Manual launch mode. Use the Game Launch button after inviting.");
                SetStatus("待機中。必要なら「全画面」→「Steam / Carrier」→ Shift+Tabで招待し、その後「ゲーム起動」。");
            }
        }

        private void PollControllers()
        {
            if (!_config.ControllerProbe)
                return;

            bool[] connected = new bool[4];
            string[] summaries = new string[4];

            for (uint i = 0; i < 4; i++)
            {
                Native.XINPUT_STATE state;
                uint result = GetXInputStateCompat(i, out state);
                bool isConnected = result == Native.ERROR_SUCCESS;
                connected[i] = isConnected;

                if (isConnected)
                {
                    summaries[i] = "ON";

                    if (!_lastXInputConnected[i])
                    {
                        _lastControllerEvent = "XInput " + (i + 1) + " connected";
                        Log(_lastControllerEvent);
                    }

                    if (_lastXInputConnected[i] &&
                        state.dwPacketNumber != _lastXInputPacket[i])
                    {
                        _lastControllerEvent = DescribeXInputEvent(i, state.Gamepad);
                    }

                    _lastXInputPacket[i] = state.dwPacketNumber;
                }
                else
                {
                    summaries[i] = "--";

                    if (_lastXInputConnected[i])
                    {
                        _lastControllerEvent = "XInput " + (i + 1) + " disconnected";
                        Log(_lastControllerEvent);
                    }
                }

                _lastXInputConnected[i] = isConnected;
            }

            int legacyActive = CountLegacyJoysticks();

            _controllerUiDivider++;
            if (_controllerUiDivider >= 6)
            {
                _controllerUiDivider = 0;
                if (_controllerStatusLabel != null && !_controllerStatusLabel.IsDisposed)
                {
                    _controllerStatusLabel.Text =
                        "XInput 60Hz  1:[" + summaries[0] + "] 2:[" + summaries[1] +
                        "] 3:[" + summaries[2] + "] 4:[" + summaries[3] + "]   " +
                        "WinMM:[" + legacyActive + "]\r\n" +
                        "Last input: " + _lastControllerEvent;
                }
            }
        }

        private uint GetXInputStateCompat(uint userIndex, out Native.XINPUT_STATE state)
        {
            state = new Native.XINPUT_STATE();

            if (_xinput14Available)
            {
                try
                {
                    return Native.XInputGetState14(userIndex, out state);
                }
                catch (DllNotFoundException)
                {
                    _xinput14Available = false;
                    Log("xinput1_4.dll unavailable; falling back to xinput9_1_0.dll.");
                }
                catch (EntryPointNotFoundException)
                {
                    _xinput14Available = false;
                }
            }

            if (_xinput910Available)
            {
                try
                {
                    return Native.XInputGetState910(userIndex, out state);
                }
                catch (DllNotFoundException)
                {
                    _xinput910Available = false;
                    Log("xinput9_1_0.dll unavailable.");
                }
                catch (EntryPointNotFoundException)
                {
                    _xinput910Available = false;
                }
            }

            return Native.ERROR_DEVICE_NOT_CONNECTED;
        }

        private int CountLegacyJoysticks()
        {
            uint count = Native.joyGetNumDevs();
            int active = 0;

            for (uint id = 0; id < count; id++)
            {
                Native.JOYINFOEX info = new Native.JOYINFOEX();
                info.dwSize = (uint)Marshal.SizeOf(typeof(Native.JOYINFOEX));
                info.dwFlags = Native.JOY_RETURNALL;

                uint result = Native.joyGetPosEx(id, ref info);
                if (result == Native.MMSYSERR_NOERROR)
                    active++;
            }

            return active;
        }

        private string DescribeXInputEvent(uint slot, Native.XINPUT_GAMEPAD pad)
        {
            string buttons = Native.DescribeButtons(pad.wButtons);
            int lt = pad.bLeftTrigger;
            int rt = pad.bRightTrigger;

            return "XInput " + (slot + 1) +
                   " buttons=" + buttons +
                   " LT=" + lt +
                   " RT=" + rt +
                   " LS=(" + pad.sThumbLX + "," + pad.sThumbLY + ")" +
                   " RS=(" + pad.sThumbRX + "," + pad.sThumbRY + ")";
        }

        private void InitOpenGL()
        {
            _dc = Native.GetDC(Handle);
            if (_dc == IntPtr.Zero)
                throw new InvalidOperationException("GetDC failed.");

            Native.PIXELFORMATDESCRIPTOR pfd = new Native.PIXELFORMATDESCRIPTOR();
            pfd.nSize = (ushort)Marshal.SizeOf(typeof(Native.PIXELFORMATDESCRIPTOR));
            pfd.nVersion = 1;
            pfd.dwFlags = Native.PFD_DRAW_TO_WINDOW |
                          Native.PFD_SUPPORT_OPENGL |
                          Native.PFD_DOUBLEBUFFER;
            pfd.iPixelType = Native.PFD_TYPE_RGBA;
            pfd.cColorBits = 32;
            pfd.cDepthBits = 24;
            pfd.iLayerType = Native.PFD_MAIN_PLANE;

            int format = Native.ChoosePixelFormat(_dc, ref pfd);
            if (format == 0)
                throw new InvalidOperationException("ChoosePixelFormat failed.");

            if (!Native.SetPixelFormat(_dc, format, ref pfd))
                throw new InvalidOperationException("SetPixelFormat failed.");

            _gl = Native.wglCreateContext(_dc);
            if (_gl == IntPtr.Zero)
                throw new InvalidOperationException("wglCreateContext failed.");

            if (!Native.wglMakeCurrent(_dc, _gl))
                throw new InvalidOperationException("wglMakeCurrent failed.");

            RenderHostSurface();
        }

        private void DestroyOpenGL()
        {
            try
            {
                if (_gl != IntPtr.Zero)
                {
                    Native.wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
                    Native.wglDeleteContext(_gl);
                    _gl = IntPtr.Zero;
                }

                if (_dc != IntPtr.Zero && IsHandleCreated)
                {
                    Native.ReleaseDC(Handle, _dc);
                    _dc = IntPtr.Zero;
                }
            }
            catch
            {
            }
        }

        private void RenderHostSurface()
        {
            if (_dc == IntPtr.Zero || _gl == IntPtr.Zero || !IsHandleCreated)
                return;

            try
            {
                Native.wglMakeCurrent(_dc, _gl);

                int w = Math.Max(1, ClientSize.Width);
                int h = Math.Max(1, ClientSize.Height);

                Native.glViewport(0, 0, w, h);
                Native.glClearColor(0.015f, 0.015f, 0.02f, 1.0f);
                Native.glClear(Native.GL_COLOR_BUFFER_BIT);
                Native.SwapBuffers(_dc);
            }
            catch (Exception ex)
            {
                Log("Render error: " + ex.Message);
            }
        }

        private bool PromptForTargetExecutable()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Remote Play Togetherで起動するゲームを選択";
                dialog.Filter = "実行ファイル (*.exe)|*.exe|すべてのファイル (*.*)|*.*";
                dialog.CheckFileExists = true;
                dialog.CheckPathExists = true;
                dialog.Multiselect = false;
                dialog.RestoreDirectory = true;

                if (!String.IsNullOrWhiteSpace(_config.WorkingDirectory) &&
                    Directory.Exists(_config.WorkingDirectory))
                {
                    dialog.InitialDirectory = _config.WorkingDirectory;
                }

                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return false;

                _config.Target = Path.GetFullPath(dialog.FileName);
                _config.WorkingDirectory = Path.GetDirectoryName(_config.Target);

                bool isNw = IsNwExecutable(_config.Target);
                Log("Target selected from file dialog: " + _config.Target);
                Log("Automatic runtime detection: " + (isNw ? "NW.js" : "normal executable"));

                UpdateUiState();

                SetStatus(
                    isNw
                        ? "NW.jsを自動検出しました。互換設定を自動適用します。招待後に「ゲーム起動」を押してください。"
                        : "EXEを選択しました。招待後に「ゲーム起動」を押してください。");

                return true;
            }
        }

        private bool IsNwExecutable(string path)
        {
            if (String.IsNullOrWhiteSpace(path))
                return false;

            string fileName = Path.GetFileName(path);
            if (String.Equals(fileName, "nw.exe", StringComparison.OrdinalIgnoreCase))
                return true;

            try
            {
                FileVersionInfo vi = FileVersionInfo.GetVersionInfo(path);
                string product = vi.ProductName ?? "";
                string description = vi.FileDescription ?? "";

                if (product.IndexOf("NW.js", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    product.IndexOf("NWJS", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    description.IndexOf("NW.js", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    description.IndexOf("NWJS", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            catch
            {
            }

            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!String.IsNullOrWhiteSpace(dir) &&
                    File.Exists(Path.Combine(dir, "nw_elf.dll")))
                {
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private string BuildTargetArguments()
        {
            string args = _config.Arguments ?? "";

            if (!_config.NwOverlayCompat || !IsNwExecutable(_config.Target))
                return args;

            string compat = BuildNwOverlayArguments(_config.Target);

            if (String.IsNullOrWhiteSpace(args))
                return compat;

            // User-supplied target arguments are appended last intentionally.
            // For Chromium/NW.js switches, an explicit later value can override
            // an automatically added compatibility switch.
            return compat + " " + args;
        }

        private string BuildNwOverlayArguments(string path)
        {
            // Order matters for NW.js. Keep custom-titlebar first.
            var parts = new List<string>();
            parts.Add("--disable-windows10-custom-titlebar");

            // Chromium normally runs the Windows audio service in a separate
            // utility process. Steam Remote Play can capture the NW.js video
            // and input while missing that separate audio session, so keep
            // audio in the NW browser process for streaming compatibility.
            parts.Add("--disable-features=AudioServiceOutOfProcess");
            Log("NW audio compatibility: forcing Chromium audio service in-process.");

            int minor = -1;
            int major = -1;

            try
            {
                FileVersionInfo vi = FileVersionInfo.GetVersionInfo(path);
                major = vi.FileMajorPart;
                minor = vi.FileMinorPart;
                Log("Detected NW.js file version: " + vi.FileVersion);
            }
            catch (Exception ex)
            {
                Log("Could not read NW.js version: " + ex.Message);
            }

            bool safeForInProcessGpu =
                _config.ForceNwInProcessGpu ||
                (major == 0 && minor >= 0 && minor < 102);

            if (safeForInProcessGpu)
            {
                parts.Add("--in-process-gpu");
                parts.Add("--disable-direct-composition");
                Log("NW overlay compatibility: using in-process GPU + DirectComposition workaround.");
            }
            else
            {
                parts.Add("--disable-direct-composition");

                if (major == 0 && minor >= 102 && !_config.ForceNwInProcessGpu)
                {
                    Log("NW.js 0.102+ detected. --in-process-gpu skipped because it is known to cause blank windows on recent NW.js.");
                }
                else
                {
                    Log("NW.js version unknown. Conservative overlay flags enabled without --in-process-gpu.");
                }
            }

            return String.Join(" ", parts.ToArray());
        }

        private void LaunchTarget()
        {
            if (_launchStarted)
            {
                Log("Launch ignored because target has already been launched.");
                return;
            }

            if (String.IsNullOrWhiteSpace(_config.Target))
            {
                if (!PromptForTargetExecutable())
                    return;
            }

            _launchStarted = true;
            UpdateUiState();
            SetStatus("ゲームを起動しています...");

            try
            {
                var psi = new ProcessStartInfo();
                psi.FileName = _config.Target;
                psi.Arguments = BuildTargetArguments();
                psi.WorkingDirectory = String.IsNullOrWhiteSpace(_config.WorkingDirectory)
                    ? Path.GetDirectoryName(_config.Target)
                    : _config.WorkingDirectory;
                psi.UseShellExecute = false;

                Log("Launching target: " + psi.FileName);
                Log("Arguments: " + (psi.Arguments.Length == 0 ? "(none)" : psi.Arguments));
                Log("Working directory: " + psi.WorkingDirectory);

                _rootProcess = Process.Start(psi);
                if (_rootProcess == null)
                    throw new InvalidOperationException("Process.Start returned null.");

                _rootPid = _rootProcess.Id;
                _windowSearchStarted = DateTime.UtcNow;
                _windowTimer.Start();
                _gameWatchTimer.Start();

                Log("Root PID: " + _rootPid);
                SetStatus("ゲーム起動済み。ゲーム画面を検出しています...");
            }
            catch (Exception ex)
            {
                _launchStarted = false;
                UpdateUiState();
                SetStatus("ゲーム起動に失敗しました。");
                Log("Launch failed: " + ex);
                MessageBox.Show(
                    "Failed to start target.\r\n\r\n" + ex.Message,
                    "RPT Carrier",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void PollForGameWindow()
        {
            if (_gameWindow != IntPtr.Zero && Native.IsWindow(_gameWindow))
            {
                _windowTimer.Stop();
                return;
            }

            HashSet<int> pids = ProcessTree.GetTree(_rootPid);
            IntPtr hwnd = WindowFinder.FindBestWindow(pids, _config.WindowTitleContains);

            if (hwnd != IntPtr.Zero)
            {
                _windowTimer.Stop();
                _gameWindow = hwnd;

                uint pid;
                Native.GetWindowThreadProcessId(hwnd, out pid);
                _gamePid = unchecked((int)pid);

                Log("Game window found: HWND=0x" + hwnd.ToInt64().ToString("X") + " PID=" + _gamePid);
                Log("Window title: " + WindowFinder.GetTitle(hwnd));

                SetupPresentation();
                UpdateUiState();
                return;
            }

            if ((DateTime.UtcNow - _windowSearchStarted).TotalMilliseconds > _config.WindowTimeoutMs)
            {
                _windowTimer.Stop();
                Log("Game window search timed out.");
                MessageBox.Show(
                    "The target process started, but no suitable game window was found.\r\n\r\n" +
                    "Try WindowTitleContains=... in RPTCarrier.ini, or Embed=false.",
                    "RPT Carrier",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void SetupPresentation()
        {
            if (_config.PresentationMode == "handoff")
            {
                SetupHandoff();
                return;
            }

            if (_config.PresentationMode == "mirror")
            {
                SetupDwmMirror();
                return;
            }

            if (_config.PresentationMode == "embed")
            {
                EmbedGameWindow();
                return;
            }

            Log("PresentationMode=separate, target window left as a separate top-level window.");
            if (_config.FocusGameAfterMirror)
            {
                Native.SetForegroundWindow(_gameWindow);
                _gameHasInputFocus = true;
            }
        }

        private void SetupHandoff()
        {
            if (_gameWindow == IntPtr.Zero || !Native.IsWindow(_gameWindow))
                return;

            if (_dwmThumbnail != IntPtr.Zero)
            {
                Native.DwmUnregisterThumbnail(_dwmThumbnail);
                _dwmThumbnail = IntPtr.Zero;
            }

            TopMost = false;

            // Leave the target exactly as the game created it:
            // top-level window, original styles, original size.
            Native.ShowWindow(_gameWindow, Native.SW_SHOW);
            Native.SetWindowPos(
                _gameWindow,
                Native.HWND_TOP,
                0, 0, 0, 0,
                Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_SHOWWINDOW);

            Native.SetForegroundWindow(_gameWindow);
            _gameHasInputFocus = true;

            // Carrier has already served its purpose: establish the Steam/RPT
            // context before the target starts. Keep the process alive, but get
            // its window completely out of the way so the game remains the
            // foreground top-level application.
            ShowInTaskbar = false;
            Hide();

            Log("Handoff mode active. Carrier window hidden; target remains native top-level foreground window.");
        }

        private void SetupDwmMirror()
        {
            if (_gameWindow == IntPtr.Zero || !Native.IsWindow(_gameWindow))
                return;

            bool enabled;
            int hr = Native.DwmIsCompositionEnabled(out enabled);
            if (hr != 0 || !enabled)
            {
                Log("DWM composition unavailable, falling back to embed mode. HRESULT=0x" + hr.ToString("X8"));
                EmbedGameWindow();
                return;
            }

            if (_dwmThumbnail != IntPtr.Zero)
            {
                Native.DwmUnregisterThumbnail(_dwmThumbnail);
                _dwmThumbnail = IntPtr.Zero;
            }

            hr = Native.DwmRegisterThumbnail(Handle, _gameWindow, out _dwmThumbnail);
            if (hr != 0 || _dwmThumbnail == IntPtr.Zero)
            {
                Log("DwmRegisterThumbnail failed, HRESULT=0x" + hr.ToString("X8") + ". Falling back to embed mode.");
                _dwmThumbnail = IntPtr.Zero;
                EmbedGameWindow();
                return;
            }

            Native.SetWindowPos(
                _gameWindow,
                Native.HWND_NOTOPMOST,
                0, 0, 0, 0,
                Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);

            if (_config.KeepCarrierTopMost)
            {
                Native.SetWindowPos(
                    Handle,
                    Native.HWND_TOPMOST,
                    0, 0, 0, 0,
                    Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE | Native.SWP_SHOWWINDOW);
            }

            TopMost = _config.KeepCarrierTopMost;

            if (_config.HideUiWhenGameStarts)
                EnterGameViewMode();
            else if (_config.MatchTargetSize && !_isFullscreen)
                MatchCarrierToTargetSize();

            UpdateDwmMirror();
            Log("DWM mirror registered. Target remains a normal top-level window behind the Carrier.");
            SetStatus("ゲーム映像を表示中。ゲームを遊ぶなら「ゲーム操作」、Steamを触るなら「Steam / Carrier」。");

            if (_config.FocusGameAfterMirror)
            {
                FocusExternalWindow(_gameWindow);
                _gameHasInputFocus = true;
                Log("Game receives foreground focus once. Input queues are not attached/detached during play.");
            }
        }

        private void EnterGameViewMode()
        {
            if (_gameWindow == IntPtr.Zero || !Native.IsWindow(_gameWindow))
                return;

            Native.RECT client;
            if (!Native.GetClientRect(_gameWindow, out client))
                return;

            int gameWidth = Math.Max(1, client.Right - client.Left);
            int gameHeight = Math.Max(1, client.Bottom - client.Top);

            _sourceClientWidth = gameWidth;
            _sourceClientHeight = gameHeight;

            if (!_gameViewMode)
            {
                _preGameBorderStyle = FormBorderStyle;
                _preGameBounds = Bounds;
            }

            _gameViewMode = true;

            if (_controlPanel != null)
                _controlPanel.Visible = false;
            if (_controllerPanel != null)
                _controllerPanel.Visible = false;

            FormBorderStyle = FormBorderStyle.Sizable;
            WindowState = FormWindowState.Normal;
            TopMost = _config.KeepCarrierTopMost;

            Point clientOrigin = Native.ClientToScreenPoint(_gameWindow, new Point(0, 0));
            ClientSize = new Size(gameWidth, gameHeight);

            Rectangle wa = Screen.FromPoint(clientOrigin).WorkingArea;
            int x = Math.Max(wa.Left, Math.Min(clientOrigin.X, wa.Right - Width));
            int y = Math.Max(wa.Top, Math.Min(clientOrigin.Y, wa.Bottom - Height));
            Location = new Point(x, y);

            _isFullscreen = false;
            _windowedBounds = Bounds;

            Log("Game view mode: standard title bar with " + gameWidth + "x" + gameHeight + " client-only mirror.");
        }

        private void ExitGameViewMode()
        {
            if (!_gameViewMode)
                return;

            _gameViewMode = false;

            TopMost = false;
            FormBorderStyle = _preGameBorderStyle;
            WindowState = FormWindowState.Normal;

            if (_preGameBounds.Width >= 320 && _preGameBounds.Height >= 240)
                Bounds = _preGameBounds;
            else
                ClientSize = new Size(1280, 760);

            if (_controlPanel != null)
                _controlPanel.Visible = true;
            if (_controllerPanel != null)
                _controllerPanel.Visible = _controllerPanelVisible;

            UpdateUiState();
            SetStatus("ゲームが終了しました。");
        }

        private void MatchCarrierToTargetSize()
        {
            if (_gameWindow == IntPtr.Zero || !Native.IsWindow(_gameWindow))
                return;

            Native.RECT client;
            if (!Native.GetClientRect(_gameWindow, out client))
                return;

            int gameWidth = Math.Max(1, client.Right - client.Left);
            int gameHeight = Math.Max(1, client.Bottom - client.Top);

            _sourceClientWidth = gameWidth;
            _sourceClientHeight = gameHeight;

            int top = PresentationTop();
            ClientSize = new Size(gameWidth, gameHeight + top);

            Native.RECT gameRect;
            if (Native.GetWindowRect(_gameWindow, out gameRect))
            {
                int x = gameRect.Left;
                int y = Math.Max(0, gameRect.Top - top);
                Location = new Point(x, y);
            }

            _windowedBounds = Bounds;
            Log("Carrier matched target client size: " + gameWidth + "x" + gameHeight);
            SetStatus("元ゲームサイズ " + gameWidth + "x" + gameHeight + " で表示中。");
        }

        private void UpdateDwmMirror()
        {
            if (_dwmThumbnail == IntPtr.Zero)
                return;

            Native.RECT sourceClient;
            int srcW = 0;
            int srcH = 0;

            if (Native.GetClientRect(_gameWindow, out sourceClient))
            {
                srcW = Math.Max(1, sourceClient.Right - sourceClient.Left);
                srcH = Math.Max(1, sourceClient.Bottom - sourceClient.Top);
            }

            if (srcW <= 0 || srcH <= 0)
            {
                Native.SIZE src;
                int sizeHr = Native.DwmQueryThumbnailSourceSize(_dwmThumbnail, out src);
                srcW = Math.Max(1, src.cx);
                srcH = Math.Max(1, src.cy);
            }

            _sourceClientWidth = srcW;
            _sourceClientHeight = srcH;

            int top = PresentationTop();
            int cw = Math.Max(1, ClientSize.Width);
            int bottom = PresentationBottom();
            int ch = Math.Max(1, ClientSize.Height - top - bottom);

            double scale = Math.Min((double)cw / srcW, (double)ch / srcH);
            int dw = Math.Max(1, (int)Math.Round(srcW * scale));
            int dh = Math.Max(1, (int)Math.Round(srcH * scale));
            int x = (cw - dw) / 2;
            int y = top + (ch - dh) / 2;

            _mirrorX = x;
            _mirrorY = y;
            _mirrorWidth = dw;
            _mirrorHeight = dh;

            Native.DWM_THUMBNAIL_PROPERTIES props = new Native.DWM_THUMBNAIL_PROPERTIES();
            props.dwFlags =
                Native.DWM_TNP_RECTDESTINATION |
                Native.DWM_TNP_VISIBLE |
                Native.DWM_TNP_OPACITY |
                Native.DWM_TNP_SOURCECLIENTAREAONLY;
            props.rcDestination = new Native.RECT
            {
                Left = x,
                Top = y,
                Right = x + dw,
                Bottom = y + dh
            };
            props.opacity = 255;
            props.fVisible = true;
            props.fSourceClientAreaOnly = true;

            int hr = Native.DwmUpdateThumbnailProperties(_dwmThumbnail, ref props);
            if (hr != 0)
                Log("DwmUpdateThumbnailProperties failed: HRESULT=0x" + hr.ToString("X8"));
        }

        private void UpdatePresentation()
        {
            if (_config.PresentationMode == "mirror")
            {
                UpdateDwmMirror();
                return;
            }

            if (_config.PresentationMode == "embed")
                FitEmbeddedWindow();
        }

        private void EmbedGameWindow()
        {
            if (_gameWindow == IntPtr.Zero || !Native.IsWindow(_gameWindow))
                return;

            Native.GetWindowRect(_gameWindow, out _oldRect);
            _oldParent = Native.GetParent(_gameWindow);
            _oldStyle = Native.GetWindowLongPtr(_gameWindow, Native.GWL_STYLE);
            _oldExStyle = Native.GetWindowLongPtr(_gameWindow, Native.GWL_EXSTYLE);

            Native.RECT originalClient;
            int gameWidth = 1;
            int gameHeight = 1;
            if (Native.GetClientRect(_gameWindow, out originalClient))
            {
                gameWidth = Math.Max(1, originalClient.Right - originalClient.Left);
                gameHeight = Math.Max(1, originalClient.Bottom - originalClient.Top);
            }

            if (_config.HideUiWhenGameStarts)
                EnterGameViewMode();
            else if (_config.MatchTargetSize && !_isFullscreen)
                MatchCarrierToTargetSize();

            long style = _oldStyle.ToInt64();
            style &= ~Native.WS_POPUP;
            style &= ~Native.WS_CAPTION;
            style &= ~Native.WS_THICKFRAME;
            style &= ~Native.WS_MINIMIZEBOX;
            style &= ~Native.WS_MAXIMIZEBOX;
            style |= Native.WS_CHILD;
            style |= Native.WS_VISIBLE;

            Native.SetWindowLongPtr(_gameWindow, Native.GWL_STYLE, new IntPtr(style));
            Native.SetParent(_gameWindow, Handle);

            Native.SetWindowPos(
                _gameWindow,
                IntPtr.Zero,
                0,
                PresentationTop(),
                gameWidth,
                gameHeight,
                Native.SWP_NOZORDER | Native.SWP_FRAMECHANGED | Native.SWP_SHOWWINDOW);

            Native.ShowWindow(_gameWindow, Native.SW_SHOW);

            FocusEmbeddedGameOnce();

            _sourceClientWidth = gameWidth;
            _sourceClientHeight = gameHeight;
            _gameHasInputFocus = true;

            Log("Real game HWND embedded into Carrier. Input is delivered directly to the game window.");
            SetStatus("ゲーム本体を直接表示中。入力転送は使っていません。");
        }

        private void FocusEmbeddedGameOnce()
        {
            if (_gameWindow == IntPtr.Zero || !Native.IsWindow(_gameWindow))
                return;

            Native.SetForegroundWindow(Handle);

            uint gamePid;
            uint gameThread = Native.GetWindowThreadProcessId(_gameWindow, out gamePid);
            uint carrierThread = Native.GetCurrentThreadId();

            bool attached = false;
            try
            {
                if (gameThread != 0 && gameThread != carrierThread)
                    attached = Native.AttachThreadInput(carrierThread, gameThread, true);

                Native.SetFocus(_gameWindow);
            }
            finally
            {
                if (attached)
                    Native.AttachThreadInput(carrierThread, gameThread, false);
            }
        }

        private void FitEmbeddedWindow()
        {
            if (_gameWindow == IntPtr.Zero || !Native.IsWindow(_gameWindow) ||
                _config.PresentationMode != "embed")
                return;

            Native.SetWindowPos(
                _gameWindow,
                IntPtr.Zero,
                0,
                PresentationTop(),
                Math.Max(1, ClientSize.Width),
                Math.Max(1, ClientSize.Height - PresentationTop() - PresentationBottom()),
                Native.SWP_NOZORDER | Native.SWP_FRAMECHANGED | Native.SWP_SHOWWINDOW);
        }

        private void ToggleGameOrCarrierFocus()
        {
            if (_gameHasInputFocus)
                FocusCarrierForSteam();
            else
                FocusGameForInput();
        }

        private void WatchGame()
        {
            if (!_launchStarted)
                return;

            // Once a real game window has been found, track the PID that actually
            // owns that window. This matters for multi-process runtimes such as NW.js.
            int pid = _gamePid != 0 ? _gamePid : _rootPid;
            if (pid == 0)
                return;

            if (!ProcessTree.IsAlive(pid))
            {
                Log("Tracked game process exited: PID=" + pid);
                _gameWatchTimer.Stop();

                if (_config.PresentationMode == "handoff")
                {
                    // In handoff mode the Carrier must not remain alive after the
                    // target exits, otherwise Steam can keep the donor AppID in a
                    // running/busy state and block the next launch.
                    Log("Handoff target ended. Closing Carrier to release donor AppID.");
                    if (!_closing)
                        Close();
                    return;
                }

                ExitGameViewMode();

                if (_config.CloseWithGame && !_closing)
                    Close();
            }
        }

        private void OnCarrierClosing(object sender, FormClosingEventArgs e)
        {
            _closing = true;

            if (_dwmThumbnail != IntPtr.Zero)
            {
                Native.DwmUnregisterThumbnail(_dwmThumbnail);
                _dwmThumbnail = IntPtr.Zero;
            }

            if (_config.PresentationMode == "embed")
                RestoreGameWindow();
        }

        private void RestoreGameWindow()
        {
            if (_gameWindow == IntPtr.Zero || !Native.IsWindow(_gameWindow) || !_config.Embed)
                return;

            try
            {
                Native.ShowWindow(_gameWindow, Native.SW_SHOW);
                Native.SetParent(_gameWindow, _oldParent);
                Native.SetWindowLongPtr(_gameWindow, Native.GWL_STYLE, _oldStyle);
                Native.SetWindowLongPtr(_gameWindow, Native.GWL_EXSTYLE, _oldExStyle);

                int w = Math.Max(64, _oldRect.Right - _oldRect.Left);
                int h = Math.Max(64, _oldRect.Bottom - _oldRect.Top);

                Native.SetWindowPos(
                    _gameWindow,
                    IntPtr.Zero,
                    _oldRect.Left,
                    _oldRect.Top,
                    w,
                    h,
                    Native.SWP_NOZORDER | Native.SWP_FRAMECHANGED | Native.SWP_SHOWWINDOW);
            }
            catch
            {
            }
        }

        private string ResolveLogPath()
        {
            string basePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "RPTCarrier.log");

            try
            {
                File.AppendAllText(basePath, "");
                return basePath;
            }
            catch
            {
                return Path.Combine(Path.GetTempPath(), "RPTCarrier.log");
            }
        }

        private void LogEnvironment()
        {
            string[] names = new string[]
            {
                "SteamAppId",
                "SteamGameId",
                "SteamOverlayGameId",
                "SteamClientLaunch",
                "SteamEnv"
            };

            foreach (string n in names)
            {
                string v = Environment.GetEnvironmentVariable(n);
                Log("ENV " + n + "=" + (String.IsNullOrEmpty(v) ? "(unset)" : v));
            }
        }

        private void Log(string message)
        {
            try
            {
                string line =
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") +
                    " [PID " + Process.GetCurrentProcess().Id + "] " +
                    message +
                    Environment.NewLine;

                File.AppendAllText(_logPath, line, Encoding.UTF8);
            }
            catch
            {
            }
        }
    }

    internal static class ProcessTree
    {
        public static HashSet<int> GetTree(int rootPid)
        {
            var result = new HashSet<int>();
            if (rootPid <= 0)
                return result;

            result.Add(rootPid);

            var parentMap = new Dictionary<int, int>();
            IntPtr snapshot = Native.CreateToolhelp32Snapshot(Native.TH32CS_SNAPPROCESS, 0);
            if (snapshot == Native.INVALID_HANDLE_VALUE)
                return result;

            try
            {
                Native.PROCESSENTRY32 pe = new Native.PROCESSENTRY32();
                pe.dwSize = (uint)Marshal.SizeOf(typeof(Native.PROCESSENTRY32));

                if (Native.Process32First(snapshot, ref pe))
                {
                    do
                    {
                        parentMap[unchecked((int)pe.th32ProcessID)] =
                            unchecked((int)pe.th32ParentProcessID);
                    }
                    while (Native.Process32Next(snapshot, ref pe));
                }
            }
            finally
            {
                Native.CloseHandle(snapshot);
            }

            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (KeyValuePair<int, int> kv in parentMap)
                {
                    if (!result.Contains(kv.Key) && result.Contains(kv.Value))
                    {
                        result.Add(kv.Key);
                        changed = true;
                    }
                }
            }

            return result;
        }

        public static bool IsAlive(int pid)
        {
            if (pid <= 0)
                return false;

            try
            {
                Process p = Process.GetProcessById(pid);
                return !p.HasExited;
            }
            catch
            {
                return false;
            }
        }
    }

    internal static class WindowFinder
    {
        public static IntPtr FindBestWindow(HashSet<int> pids, string titleContains)
        {
            IntPtr best = IntPtr.Zero;
            long bestArea = 0;

            Native.EnumWindows(
                delegate (IntPtr hwnd, IntPtr lParam)
                {
                    if (!Native.IsWindowVisible(hwnd))
                        return true;

                    uint pid;
                    Native.GetWindowThreadProcessId(hwnd, out pid);
                    if (!pids.Contains(unchecked((int)pid)))
                        return true;

                    string title = GetTitle(hwnd);
                    if (!String.IsNullOrWhiteSpace(titleContains) &&
                        title.IndexOf(titleContains, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        return true;
                    }

                    Native.RECT r;
                    if (!Native.GetWindowRect(hwnd, out r))
                        return true;

                    int w = r.Right - r.Left;
                    int h = r.Bottom - r.Top;
                    if (w < 64 || h < 64)
                        return true;

                    long area = (long)w * (long)h;
                    if (area > bestArea)
                    {
                        bestArea = area;
                        best = hwnd;
                    }

                    return true;
                },
                IntPtr.Zero);

            return best;
        }

        public static string GetTitle(IntPtr hwnd)
        {
            int len = Native.GetWindowTextLength(hwnd);
            if (len <= 0)
                return "";

            var sb = new StringBuilder(len + 1);
            Native.GetWindowText(hwnd, sb, sb.Capacity);
            return sb.ToString();
        }
    }

    internal static class Native
    {
        public const int WM_MOUSEACTIVATE = 0x0021;
        public const int WM_HOTKEY = 0x0312;
        public const int HTCLIENT = 1;
        public const int MA_NOACTIVATE = 3;
        public const int WM_MOUSEMOVE = 0x0200;
        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_LBUTTONUP = 0x0202;
        public const int WM_RBUTTONDOWN = 0x0204;
        public const int WM_RBUTTONUP = 0x0205;
        public const int WM_MBUTTONDOWN = 0x0207;
        public const int WM_MBUTTONUP = 0x0208;
        public const int WM_MOUSEWHEEL = 0x020A;

        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_NOREPEAT = 0x4000;

        public const int GWL_STYLE = -16;
        public const int GWL_EXSTYLE = -20;

        public const long WS_CHILD = 0x40000000L;
        public const long WS_VISIBLE = 0x10000000L;
        public const long WS_POPUP = 0x80000000L;
        public const long WS_CAPTION = 0x00C00000L;
        public const long WS_THICKFRAME = 0x00040000L;
        public const long WS_MINIMIZEBOX = 0x00020000L;
        public const long WS_MAXIMIZEBOX = 0x00010000L;

        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_SHOWWINDOW = 0x0040;
        public const uint SWP_FRAMECHANGED = 0x0020;

        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;

        public const uint TH32CS_SNAPPROCESS = 0x00000002;
        public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        public const uint PFD_DOUBLEBUFFER = 0x00000001;
        public const uint PFD_DRAW_TO_WINDOW = 0x00000004;
        public const uint PFD_SUPPORT_OPENGL = 0x00000020;
        public const byte PFD_TYPE_RGBA = 0;
        public const sbyte PFD_MAIN_PLANE = 0;

        public const uint GL_COLOR_BUFFER_BIT = 0x00004000;

        public static readonly IntPtr HWND_TOP = new IntPtr(0);
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOACTIVATE = 0x0010;

        public const uint DWM_TNP_RECTDESTINATION = 0x00000001;
        public const uint DWM_TNP_RECTSOURCE = 0x00000002;
        public const uint DWM_TNP_OPACITY = 0x00000004;
        public const uint DWM_TNP_VISIBLE = 0x00000008;
        public const uint DWM_TNP_SOURCECLIENTAREAONLY = 0x00000010;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct PROCESSENTRY32
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExeFile;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SIZE
        {
            public int cx;
            public int cy;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DWM_THUMBNAIL_PROPERTIES
        {
            public uint dwFlags;
            public RECT rcDestination;
            public RECT rcSource;
            public byte opacity;

            [MarshalAs(UnmanagedType.Bool)]
            public bool fVisible;

            [MarshalAs(UnmanagedType.Bool)]
            public bool fSourceClientAreaOnly;
        }

        public const uint ERROR_SUCCESS = 0;
        public const uint ERROR_DEVICE_NOT_CONNECTED = 1167;
        public const uint MMSYSERR_NOERROR = 0;
        public const uint JOY_RETURNALL = 0x000000FF;

        public const ushort XINPUT_GAMEPAD_DPAD_UP = 0x0001;
        public const ushort XINPUT_GAMEPAD_DPAD_DOWN = 0x0002;
        public const ushort XINPUT_GAMEPAD_DPAD_LEFT = 0x0004;
        public const ushort XINPUT_GAMEPAD_DPAD_RIGHT = 0x0008;
        public const ushort XINPUT_GAMEPAD_START = 0x0010;
        public const ushort XINPUT_GAMEPAD_BACK = 0x0020;
        public const ushort XINPUT_GAMEPAD_LEFT_THUMB = 0x0040;
        public const ushort XINPUT_GAMEPAD_RIGHT_THUMB = 0x0080;
        public const ushort XINPUT_GAMEPAD_LEFT_SHOULDER = 0x0100;
        public const ushort XINPUT_GAMEPAD_RIGHT_SHOULDER = 0x0200;
        public const ushort XINPUT_GAMEPAD_A = 0x1000;
        public const ushort XINPUT_GAMEPAD_B = 0x2000;
        public const ushort XINPUT_GAMEPAD_X = 0x4000;
        public const ushort XINPUT_GAMEPAD_Y = 0x8000;

        [StructLayout(LayoutKind.Sequential)]
        public struct XINPUT_GAMEPAD
        {
            public ushort wButtons;
            public byte bLeftTrigger;
            public byte bRightTrigger;
            public short sThumbLX;
            public short sThumbLY;
            public short sThumbRX;
            public short sThumbRY;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct XINPUT_STATE
        {
            public uint dwPacketNumber;
            public XINPUT_GAMEPAD Gamepad;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct JOYINFOEX
        {
            public uint dwSize;
            public uint dwFlags;
            public uint dwXpos;
            public uint dwYpos;
            public uint dwZpos;
            public uint dwRpos;
            public uint dwUpos;
            public uint dwVpos;
            public uint dwButtons;
            public uint dwButtonNumber;
            public uint dwPOV;
            public uint dwReserved1;
            public uint dwReserved2;
        }

        public static string DescribeButtons(ushort buttons)
        {
            if (buttons == 0)
                return "-";

            var names = new List<string>();
            if ((buttons & XINPUT_GAMEPAD_DPAD_UP) != 0) names.Add("Up");
            if ((buttons & XINPUT_GAMEPAD_DPAD_DOWN) != 0) names.Add("Down");
            if ((buttons & XINPUT_GAMEPAD_DPAD_LEFT) != 0) names.Add("Left");
            if ((buttons & XINPUT_GAMEPAD_DPAD_RIGHT) != 0) names.Add("Right");
            if ((buttons & XINPUT_GAMEPAD_START) != 0) names.Add("Start");
            if ((buttons & XINPUT_GAMEPAD_BACK) != 0) names.Add("Back");
            if ((buttons & XINPUT_GAMEPAD_LEFT_THUMB) != 0) names.Add("L3");
            if ((buttons & XINPUT_GAMEPAD_RIGHT_THUMB) != 0) names.Add("R3");
            if ((buttons & XINPUT_GAMEPAD_LEFT_SHOULDER) != 0) names.Add("LB");
            if ((buttons & XINPUT_GAMEPAD_RIGHT_SHOULDER) != 0) names.Add("RB");
            if ((buttons & XINPUT_GAMEPAD_A) != 0) names.Add("A");
            if ((buttons & XINPUT_GAMEPAD_B) != 0) names.Add("B");
            if ((buttons & XINPUT_GAMEPAD_X) != 0) names.Add("X");
            if ((buttons & XINPUT_GAMEPAD_Y) != 0) names.Add("Y");
            return String.Join("+", names.ToArray());
        }

        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
        public static extern uint XInputGetState14(uint dwUserIndex, out XINPUT_STATE pState);

        [DllImport("xinput9_1_0.dll", EntryPoint = "XInputGetState")]
        public static extern uint XInputGetState910(uint dwUserIndex, out XINPUT_STATE pState);

        [DllImport("winmm.dll")]
        public static extern uint joyGetNumDevs();

        [DllImport("winmm.dll")]
        public static extern uint joyGetPosEx(uint uJoyID, ref JOYINFOEX pji);

        [StructLayout(LayoutKind.Sequential)]
        public struct PIXELFORMATDESCRIPTOR
        {
            public ushort nSize;
            public ushort nVersion;
            public uint dwFlags;
            public byte iPixelType;
            public byte cColorBits;
            public byte cRedBits;
            public byte cRedShift;
            public byte cGreenBits;
            public byte cGreenShift;
            public byte cBlueBits;
            public byte cBlueShift;
            public byte cAlphaBits;
            public byte cAlphaShift;
            public byte cAccumBits;
            public byte cAccumRedBits;
            public byte cAccumGreenBits;
            public byte cAccumBlueBits;
            public byte cAccumAlphaBits;
            public byte cDepthBits;
            public byte cStencilBits;
            public byte cAuxBuffers;
            public sbyte iLayerType;
            public byte bReserved;
            public uint dwLayerMask;
            public uint dwVisibleMask;
            public uint dwDamageMask;
        }

        public static int LOWORD(IntPtr value)
        {
            return unchecked((ushort)((long)value & 0xFFFF));
        }

        public static int GET_X_LPARAM(IntPtr lParam)
        {
            return unchecked((short)((long)lParam & 0xFFFF));
        }

        public static int GET_Y_LPARAM(IntPtr lParam)
        {
            return unchecked((short)(((long)lParam >> 16) & 0xFFFF));
        }

        public static IntPtr MAKELPARAM(int low, int high)
        {
            long value = ((long)(high & 0xFFFF) << 16) | (long)(low & 0xFFFF);
            return new IntPtr(value);
        }

        public static Point ClientToScreenPoint(IntPtr hwnd, Point point)
        {
            POINT p = new POINT { X = point.X, Y = point.Y };
            ClientToScreen(hwnd, ref p);
            return new Point(p.X, p.Y);
        }

        public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern int GetWindowTextLength(IntPtr hwnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int maxCount);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);

        [DllImport("user32.dll")]
        public static extern bool GetClientRect(IntPtr hwnd, out RECT rect);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool attach);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr child, IntPtr newParent);

        [DllImport("user32.dll")]
        public static extern IntPtr GetParent(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hwnd, int command);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern IntPtr SetFocus(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool ClientToScreen(IntPtr hwnd, ref POINT point);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(
            IntPtr hwnd,
            IntPtr hwndInsertAfter,
            int x,
            int y,
            int cx,
            int cy,
            uint flags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hwnd, int id);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong32(IntPtr hwnd, int index);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hwnd, int index);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hwnd, int index, int value);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hwnd, int index, IntPtr value);

        public static IntPtr GetWindowLongPtr(IntPtr hwnd, int index)
        {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr64(hwnd, index);
            return new IntPtr(GetWindowLong32(hwnd, index));
        }

        public static IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hwnd, index, value);

            return new IntPtr(SetWindowLong32(hwnd, index, value.ToInt32()));
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint processId);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern bool Process32First(IntPtr snapshot, ref PROCESSENTRY32 entry);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern bool Process32Next(IntPtr snapshot, ref PROCESSENTRY32 entry);

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr handle);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);

        [DllImport("gdi32.dll")]
        public static extern int ChoosePixelFormat(IntPtr dc, ref PIXELFORMATDESCRIPTOR pfd);

        [DllImport("gdi32.dll")]
        public static extern bool SetPixelFormat(IntPtr dc, int format, ref PIXELFORMATDESCRIPTOR pfd);

        [DllImport("gdi32.dll")]
        public static extern bool SwapBuffers(IntPtr dc);

        [DllImport("opengl32.dll")]
        public static extern IntPtr wglCreateContext(IntPtr dc);

        [DllImport("opengl32.dll")]
        public static extern bool wglDeleteContext(IntPtr rc);

        [DllImport("opengl32.dll")]
        public static extern bool wglMakeCurrent(IntPtr dc, IntPtr rc);

        [DllImport("opengl32.dll")]
        public static extern void glViewport(int x, int y, int width, int height);

        [DllImport("opengl32.dll")]
        public static extern void glClearColor(float red, float green, float blue, float alpha);

        [DllImport("opengl32.dll")]
        public static extern void glClear(uint mask);

        [DllImport("dwmapi.dll")]
        public static extern int DwmIsCompositionEnabled(
            [MarshalAs(UnmanagedType.Bool)] out bool enabled);

        [DllImport("dwmapi.dll")]
        public static extern int DwmRegisterThumbnail(
            IntPtr destinationWindow,
            IntPtr sourceWindow,
            out IntPtr thumbnail);

        [DllImport("dwmapi.dll")]
        public static extern int DwmUnregisterThumbnail(IntPtr thumbnail);

        [DllImport("dwmapi.dll")]
        public static extern int DwmUpdateThumbnailProperties(
            IntPtr thumbnail,
            ref DWM_THUMBNAIL_PROPERTIES properties);

        [DllImport("dwmapi.dll")]
        public static extern int DwmQueryThumbnailSourceSize(
            IntPtr thumbnail,
            out SIZE size);
    }
}
