using System;
using System.Collections.Generic;

namespace MyPrtSc
{
    /// <summary>
    /// 热键处理类，负责虚拟键码的映射和管理
    /// </summary>
    public class HotKeyManager
    {
        // 虚拟键码常量定义
        public const int VK_SNAPSHOT = 0x2C; // PrtSc 键
        public const int VK_SPACE = 0x20;    // 空格键
        public const int VK_LEFT = 0x25;     // 左方向键
        public const int VK_UP = 0x26;       // 上方向键
        public const int VK_RIGHT = 0x27;    // 右方向键
        public const int VK_DOWN = 0x28;     // 下方向键
        public const int VK_INSERT = 0x2D;   // Insert键
        public const int VK_DELETE = 0x2E;   // Delete键
        public const int VK_HOME = 0x24;     // Home键
        public const int VK_END = 0x23;      // End键
        public const int VK_PRIOR = 0x21;    // Page Up键
        public const int VK_NEXT = 0x22;     // Page Down键
        public const int VK_SHIFT = 0x10;    // Shift键
        public const int VK_MENU = 0x12;     // Alt键
        public const int VK_CONTROL = 0x11;  // Ctrl键
        public const int VK_CAPITAL = 0x14;  // Caps Lock键
        public const int VK_ESCAPE = 0x1B;   // ESC键
        public const int VK_TAB = 0x09;      // Tab键
        public const int VK_RETURN = 0x0D;   // Enter键
        public const int VK_BACK = 0x08;     // Backspace键
        public const int VK_LWIN = 0x5B;     // 左Windows键
        public const int VK_RWIN = 0x5C;     // 右Windows键
        
        // 特殊键名称到虚拟键码的映射
        private static readonly Dictionary<string, int> specialKeyMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "SPACE", VK_SPACE },
            { "UP", VK_UP },
            { "DOWN", VK_DOWN },
            { "LEFT", VK_LEFT },
            { "RIGHT", VK_RIGHT },
            { "INSERT", VK_INSERT },
            { "DELETE", VK_DELETE },
            { "HOME", VK_HOME },
            { "END", VK_END },
            { "PAGEUP", VK_PRIOR },
            { "PAGEDOWN", VK_NEXT },
            { "SHIFT", VK_SHIFT },
            { "ALT", VK_MENU },
            { "CTRL", VK_CONTROL },
            { "CAPSLOCK", VK_CAPITAL },
            { "ESC", VK_ESCAPE },
            { "TAB", VK_TAB },
            { "ENTER", VK_RETURN },
            { "BACKSPACE", VK_BACK },
            { "WIN", VK_LWIN }
        };
        
        /// <summary>
        /// 根据配置字符串获取对应的虚拟键码
        /// </summary>
        /// <param name="hotkeyStr">配置文件中的热键字符串</param>
        /// <param name="virtualKeyCode">输出参数：对应的虚拟键码</param>
        /// <param name="keyName">输出参数：键的名称</param>
        /// <returns>是否成功解析热键</returns>
        public static bool ParseHotkey(string _hotkeyStr, out int virtualKeyCode, out string keyName)
        {
            virtualKeyCode = 0;
            keyName = null;
            
            if (string.IsNullOrEmpty(_hotkeyStr)) return false;
            string hotkeyStr = _hotkeyStr.ToUpper();
            
            // 检查是否为特殊键
            if (specialKeyMap.ContainsKey(hotkeyStr))
            {
                virtualKeyCode = specialKeyMap[hotkeyStr];
                keyName = hotkeyStr;
                return true;
            }
            // 检查是否为普通字符键
            else if (hotkeyStr.Length == 1)
            {
                virtualKeyCode = (int)char.ToUpper(hotkeyStr[0]);
                keyName = hotkeyStr.ToUpper();
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 检查给定的虚拟键码是否匹配配置的热键
        /// </summary>
        /// <param name="vkCode">当前按键的虚拟键码</param>
        /// <param name="configuredKeyCode">配置的热键虚拟键码</param>
        /// <param name="configuredKeyName">配置的热键名称</param>
        /// <returns>是否匹配</returns>
        public static bool IsHotkeyMatch(int vkCode, int configuredKeyCode, string configuredKeyName)
        {
            // 完全匹配
            if (vkCode == configuredKeyCode)
                return true;
            
            // 对于Windows键，同时检查左右Windows键
            if (configuredKeyName == "WIN" && (vkCode == VK_LWIN || vkCode == VK_RWIN))
                return true;
            
            return false;
        }
    }
}