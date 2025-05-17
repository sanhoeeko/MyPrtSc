using MyPrtSc.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private AppConfig config;

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
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            UnhookWindowsHookEx(_hookID); // 卸载钩子
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
                    // 延时确保系统完成截图到剪贴板
                    Timer timer = new Timer { Interval = 200 }; // 200ms 延时
                    timer.Tick += (s, e) =>
                    {
                        SaveClipboardImage();
                        timer.Stop();
                        timer.Dispose();
                    };
                    timer.Start();
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private void SaveClipboardImage()
        {
            try
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

                    if (string.IsNullOrEmpty(saveTitle))
                        saveTitle = "Untitled";
                }
                else
                {
                    saveTitle = "Desktop";
                }

                // 转换乱码
                if(config.IfAutoConvert)saveTitle= GbkValidator.ConvertGbkJisIfJis(saveTitle);

                // 构建保存路径
                string saveDir = Path.Combine(config.BaseDir, saveTitle);
                string fileName = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmssfff}.png";
                string path = Path.Combine(saveDir, fileName);

                // 确保目录存在
                if (!Directory.Exists(config.BaseDir))
                    Directory.CreateDirectory(config.BaseDir);

                if (!Directory.Exists(saveDir))
                    Directory.CreateDirectory(saveDir);

                // 保存图像（原保存逻辑）
                if (Clipboard.ContainsImage())
                {
                    using (var image = Clipboard.GetImage())
                    {
                        image.Save(path, ImageFormat.Png);
                        _trayIcon.ShowBalloonTip(3000, "成功", $"截图已保存至 {path}", ToolTipIcon.Info);
                    }
                }
            }
            catch (Exception ex)
            {
                _trayIcon.ShowBalloonTip(3000, "错误", $"保存失败，发生了异常: {ex.Message}", ToolTipIcon.Error);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
