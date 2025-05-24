using MyPrtSc.Properties;
using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
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
        private const int VK_SNAPSHOT = 0x2C; // PrtSc 键的虚拟键码

        private static IntPtr _hookID = IntPtr.Zero;
        private static LowLevelKeyboardProc _proc;
        private readonly NotifyIcon _trayIcon;

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

            // 初始化系统托盘图标
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
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            // 加载optipng.exe
            if (config.IfOptimizePng) MyImage.InitializeOptiPng();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            UnhookWindowsHookEx(_hookID); // 卸载钩子
            if(config.IfOptimizePng) Directory.Delete(MyImage.tempDir, true);  // 卸载optipng.exe
            _trayIcon.Dispose();
            base.OnFormClosing(e);
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                if (vkCode == VK_SNAPSHOT) // 检测 PrtSc 键
                {
                    // 第一时间获取焦点窗口范围和名称
                    Rectangle windowBounds = getRectangle();
                    string saveTitle = getSaveTitle();

                    // 延时确保系统完成截图到剪贴板
                    Timer timer = new Timer { Interval = 10 }; // 10ms 延时
                    timer.Tick += (s, e) =>
                    {
                        ProcessClipboardImage(saveTitle, windowBounds);
                        timer.Stop();
                        timer.Dispose();
                    };
                    timer.Start();
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private async void ProcessClipboardImage(string saveTitle, Rectangle windowBounds)
        {
            try
            {
                // 构建保存路径
                string saveDir = Path.Combine(config.BaseDir, saveTitle);
                string fileName = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmssfff}.png";
                string path = Path.Combine(saveDir, fileName);

                // 确保目录存在
                if (!Directory.Exists(config.BaseDir))Directory.CreateDirectory(config.BaseDir);
                if (!Directory.Exists(saveDir))Directory.CreateDirectory(saveDir);

                // 保存图像
                if (Clipboard.ContainsImage())
                {
                    using (var image = Clipboard.GetImage())
                    {
                        if(!config.IfWindowShot || IsFullScreen(windowBounds))
                        {
                            // 直接保存
                            await MyImage.SaveImage(image, path, config.IfOptimizePng);
                        }
                        else
                        {
                            // 裁剪后保存
                            await MyImage.SaveImage(
                                MyImage.CropImage(image, windowBounds, getSystemDpi()),
                                path, config.IfOptimizePng);
                        }
                        _trayIcon.ShowBalloonTip(3000, "成功", $"截图已保存至 {path}", ToolTipIcon.Info);
                    }
                }
            }
            catch (Exception ex)
            {
                _trayIcon.ShowBalloonTip(3000, "错误", $"保存失败，发生了异常: {ex.Message}", ToolTipIcon.Error);
            }
        }
        
        private string getSaveTitle()
        {
            // 获取当前焦点窗口标题
            const int nChars = 256;
            StringBuilder windowTitle = new StringBuilder(nChars);
            IntPtr hWnd = GetForegroundWindow();

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
            if (config.IfAutoConvert) saveTitle = GbkValidator.ConvertGbkJisIfJis(saveTitle);
            return saveTitle;
        }

        private bool IsFullScreen(Rectangle windowBounds)
        {
            // 全屏判断（考虑任务栏存在的情况）
            Rectangle screenBounds = Screen.PrimaryScreen.Bounds;

            // 允许 2px 的误差容限（针对某些窗口管理器边框）
            return windowBounds.Left - screenBounds.Left <= 2 &&
                   windowBounds.Top - screenBounds.Top <= 2 &&
                   screenBounds.Right - windowBounds.Right <= 2 &&
                   screenBounds.Bottom - windowBounds.Bottom <= 2;
        }

        private Rectangle getRectangle()
        {
            // 获取窗口矩形范围
            IntPtr hWnd = GetForegroundWindow();
            GetWindowRect(hWnd, out RECT _windowRect);
            Rectangle screenBounds = Screen.PrimaryScreen.Bounds;

            var windowRect = new Rectangle(
                _windowRect.Left + border,
                _windowRect.Top,
                _windowRect.Right - _windowRect.Left - border * 2,
                _windowRect.Bottom - _windowRect.Top - border);

            return Intersect(windowRect, screenBounds);
        }

        private Rectangle Intersect(Rectangle a, Rectangle b)
        {
            return new Rectangle(Math.Max(a.Left, b.Left),
                Math.Max(a.Top, b.Top),
                Math.Min(a.Right, b.Right) - Math.Max(a.Left, b.Left),
                Math.Min(a.Bottom, b.Bottom) - Math.Max(a.Top, b.Top)
            );
        }

        private int _get_dpi()
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
            return 96;
        }

        private double getSystemDpi() 
        {
            return (double)_get_dpi() / 96.0;
        }
    }
}
