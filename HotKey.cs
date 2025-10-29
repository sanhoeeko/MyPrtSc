using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Globalization;

namespace MyPrtSc
{
    /// <summary>
    /// 热键处理类，负责虚拟键码的映射和管理
    /// </summary>
    public class HotKeyManager
    {
        // 特殊键名称映射（用于处理别名或特殊情况）
        private static readonly Dictionary<string, string> keyNameMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // 基本功能键别名，分L/R的键默认映射到左侧键
            { "PAGEUP", "Prior" },
            { "PAGEDOWN", "Next" },
            { "ESC", "Escape" },
            { "ENTER", "Return" },
            { "BACKSPACE", "Back" },
            { "PRTSC", "Snapshot" },
            { "WIN", "LWin" },
            { "WINDOWS", "LWin" },
            { "SHIFT", "LShiftKey" },
            { "ALT", "LMenu" },
            { "CONTROL", "LControlKey" },
            { "CTRL", "LControlKey" }
        };
        
        /// <summary>
        /// 根据配置字符串获取对应的虚拟键码
        /// </summary>
        /// <param name="hotkeyStr">配置文件中的热键字符串</param>
        /// <param name="virtualKeyCode">输出参数：对应的虚拟键码</param>
        /// <param name="keyName">输出参数：键的名称</param>
        /// <returns>是否成功解析热键</returns>
        public static bool ParseHotkey(string hotkeyStr, out int virtualKeyCode, out string keyName)
        {
            virtualKeyCode = 0;
            keyName = null;
            
            if (string.IsNullOrEmpty(hotkeyStr))
                return false;
            
            hotkeyStr = hotkeyStr.Trim();
            
            // 检查是否有特殊映射
            if (keyNameMappings.ContainsKey(hotkeyStr))
            {
                hotkeyStr = keyNameMappings[hotkeyStr];
            }
            
            // 使用Enum.TryParse解析Keys枚举，设置ignoreCase为true实现大小写无关解析
            if (Enum.TryParse<Keys>(hotkeyStr, true, out Keys parsedKey))
            {
                virtualKeyCode = (int)parsedKey;
                keyName = parsedKey.ToString();
                return true;
            }
            
            return false;
        }
    }
}