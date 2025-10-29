using MyPrtSc.Properties;
using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Management;

namespace MyPrtSc
{
    public partial class Form1 : Form
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;

        private static IntPtr _hookID = IntPtr.Zero;
        private static LowLevelKeyboardProc _proc;
        private readonly NotifyIcon _trayIcon;
        private int? _customHotkeyCode = null; // 存储自定义热键的虚拟键码
        private string _customHotkeyName = null; // 存储自定义热键的名称

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);


        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private AppConfig config;
        private int border = 8;

        public Form1()
        {
            InitializeComponent();

            // 读取或初始化配置文件
            config = AppConfig.Load();
            
            // 读取自定义热键配置
            LoadCustomHotkey();

            // 初始化系统托盘图标
            WindowState = FormWindowState.Minimized;
            ShowInTaskbar = false;
            _trayIcon = new NotifyIcon
            {
                Icon = Resources.icon,
                Text = "MyPrtSc",
                Visible = true,
                ContextMenu = new ContextMenu(new[]
                {
                    new MenuItem("退出", (s, e) => Application.Exit())
                })
            };

            // 安装键盘钩子
            _proc = HookCallback;
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
            
            // 加载optipng.exe
            if (config.GetString("Format") == "PNG+") MyImage.InitializeOptiPng();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            UnhookWindowsHookEx(_hookID); // 卸载钩子
            if(config.GetBool("IfOptipng")) Directory.Delete(MyImage.tempDir, true);  // 卸载optipng.exe
            _trayIcon.Dispose();
            base.OnFormClosing(e);
        }

        private void LoadCustomHotkey()
        {
            string hotkeyStr = config.GetString("HOTKEY_A");
            
            // 当热键字符串为空时，使用默认的PrtSc键
            if (string.IsNullOrEmpty(hotkeyStr))
            {
                _customHotkeyCode = (int)Keys.Snapshot;
                _customHotkeyName = "Snapshot";
                return;
            }

            // 使用HotKeyManager解析热键配置
            if (HotKeyManager.ParseHotkey(hotkeyStr, out int virtualKeyCode, out string keyName))
            {
                _customHotkeyCode = virtualKeyCode;
                _customHotkeyName = keyName;
            }
            else
            {
                // 热键解析失败，显示Windows通知
                _trayIcon.ShowBalloonTip(3000, "配置错误", $"无法识别的热键: {hotkeyStr}\n请检查配置文件中的热键设置。", ToolTipIcon.Error);
                // 解析失败时也使用默认的PrtSc键
                _customHotkeyCode = (int)Keys.Snapshot;
                _customHotkeyName = "Snapshot";
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                // 由于在LoadCustomHotkey中已经统一设置了_customHotkeyCode和_customHotkeyName（即使配置为空或解析失败）
                // 所以这里只需要直接比较即可
                if (_customHotkeyCode.HasValue && vkCode == _customHotkeyCode.Value)
                {
                    IntPtr hWnd = GetForegroundWindow();
                    string saveTitle = getSaveTitle(hWnd);
                    Screenshot(saveTitle, hWnd);
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private async void Screenshot(string saveTitle, IntPtr hWnd)
        {
            try
            {
                // 构建保存路径
                string saveDir = Path.Combine(config.GetString("BaseDir"), saveTitle);
                string format = MyImage.ParseSuffix(config.GetString("Format"));
                string fileName = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss_fff}.{format}";
                string path = Path.Combine(saveDir, fileName);

                // 确保目录存在
                if (!Directory.Exists(config.GetString("BaseDir")))
                {
                    Directory.CreateDirectory(config.GetString("BaseDir"));
                }
                if (!Directory.Exists(saveDir))
                {
                    Directory.CreateDirectory(saveDir);
                }

                Bitmap image = null;
                if (config.GetBool("IfWindowShot") && !IsFullScreen(hWnd))
                {
                    // 窗口截图
                    image = CaptureWindow(hWnd);
                }
                else
                {
                    // 全屏截图
                    image = CaptureScreen();
                }

                if (image != null) {
                    // 去除黑边
                    if (config.GetBool("IfCropImage")) image = MyImage.CropBitmap(image);

                    // 将截图复制到剪贴板
                    Clipboard.SetImage(image);

                    // 保存截图
                    await MyImage.SaveImage(image, path, config.GetString("Format"));
                    _trayIcon.ShowBalloonTip(3000, "成功", $"截图已保存至 {path}", ToolTipIcon.Info);
                }
                else throw new Exception("No image is shot!");
            }
            catch (Exception ex)
            {
                _trayIcon.ShowBalloonTip(3000, "错误", $"保存失败，发生了异常: {ex.Message}", ToolTipIcon.Error);
            }
        }

        private Bitmap CaptureRegion(int left, int top, int width, int height)
        {
            // 使用物理像素创建位图
            Bitmap bitmap = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                // 使用物理坐标进行屏幕捕获
                g.CopyFromScreen(left, top, 0, 0, bitmap.Size, CopyPixelOperation.SourceCopy);
            }
            return bitmap;
        }

        private Bitmap CaptureWindow(IntPtr hWnd)
        {
            // 获取窗口矩形（转换为物理坐标）
            Rectangle window_region = Scale(windowRectangle(hWnd), GetDpi());
            // 通用截图
            return CaptureRegion(window_region.Left, window_region.Top, window_region.Width, window_region.Height);
        }

        private Bitmap CaptureScreen()
        {
            // 获取屏幕矩形（转换为物理坐标）
            Rectangle screen_rect = Scale(screenRectangle(), GetDpi());
            // 通用截图
            return CaptureRegion(screen_rect.Left, screen_rect.Top, screen_rect.Width, screen_rect.Height);
        }

        private Rectangle screenRectangle()
        {
            return new Rectangle(
                 GetSystemMetrics(76), GetSystemMetrics(77), GetSystemMetrics(78), GetSystemMetrics(79)
            );
        }

        private Rectangle windowRectangle(IntPtr hWnd)
        {
            GetWindowRect(hWnd, out RECT rect);
            return fromRECT(rect);
        }
        
        private string getSaveTitle(IntPtr hWnd)
        {
            // 获取当前焦点窗口标题
            const int nChars = 256;
            StringBuilder windowTitle = new StringBuilder(nChars);

            string saveTitle;
            if (GetWindowText(hWnd, windowTitle, nChars) > 0)
            {
                // 过滤非法路径字符
                saveTitle = new string(windowTitle.ToString()
                    .Where(c => !Path.GetInvalidFileNameChars().Contains(c))
                    .ToArray()).Trim();

                if (string.IsNullOrEmpty(saveTitle)) saveTitle = "Untitled";
            }
            else
            {
                saveTitle = "Desktop";
            }
            // 转换乱码
            if (config.GetBool("IfAutoConvert")) saveTitle = GbkValidator.ConvertGbkJisIfJis(saveTitle);
            return saveTitle;
        }

        private bool IsFullScreen(IntPtr hWnd)
        {

            Rectangle screen_rect = screenRectangle();
            Rectangle window_rect = windowRectangle(hWnd);
            return window_rect.Left - screen_rect.Left <= border &&
                   window_rect.Top - screen_rect.Top <= border &&
                   screen_rect.Right - window_rect.Right <= border &&
                   screen_rect.Bottom - window_rect.Bottom <= border;
        }

        private Rectangle fromRECT(RECT rect)
        {
            return Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }

        private Rectangle Scale(Rectangle rect, int dpi)
        {
            return new Rectangle(
                rect.Left * dpi / 96,
                rect.Top * dpi / 96,
                rect.Width * dpi / 96,
                rect.Height * dpi / 96
            );
        }

        private int GetDpi()
        {
            int dpiResult = 96;

            // 在单独线程中执行以避免COM冲突
            System.Threading.Thread dpiThread = new System.Threading.Thread(() =>
            {
                dpiResult = _get_dpi();
            });
            dpiThread.Start();
            dpiThread.Join(); 
            
            return dpiResult;
        }

        private int _get_dpi()
        {
            try
            {
                using (ManagementClass mc = new ManagementClass("Win32_DesktopMonitor"))
                {
                    using (ManagementObjectCollection moc = mc.GetInstances())
                    {
                        foreach (ManagementObject each in moc)
                        {
                            return int.Parse((each.Properties["PixelsPerXLogicalInch"].Value.ToString()));
                        }
                    }
                }
            }
            catch {}
            return 96;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
