using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyPrtSc
{
    public class GbkValidator
    {
        private static Encoding gbk, shiftJis;
        static GbkValidator()
        {
            // 注册编码支持（.NET Core需要）
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // 获取编码器（带异常回退）
            try
            {
                gbk = Encoding.GetEncoding("GBK",
                    new EncoderExceptionFallback(),
                    new DecoderExceptionFallback());

                shiftJis = Encoding.GetEncoding("Shift_JIS",
                    new EncoderExceptionFallback(),
                    new DecoderExceptionFallback());
            }
            catch (ArgumentException)
            {
                throw new Exception("系统编码支持不全，需要GBK/Shift_JIS");
            }
        }

        public static string ConvertGbkJisIfJis(string input)
        {
            if (IsValidChineseGbk(input)) { return input; } else
            {
                byte[] gbkBytes = gbk.GetBytes(input);
                string decoded = shiftJis.GetString(gbkBytes);
                return decoded;
            }
        }

        public static bool IsValidChineseGbk(string input)
        {
            // 转换为GBK字节序列
            byte[] gbkBytes;
            try
            {
                gbkBytes = gbk.GetBytes(input);
            }
            catch (EncoderFallbackException)
            {
                return true; // 无法编码为GBK，视为合法中文
            }

            // 尝试用Shift_JIS解码
            string decoded;
            try
            {
                decoded = shiftJis.GetString(gbkBytes);
            }
            catch (DecoderFallbackException)
            {
                return true; // 解码失败说明不是Shift_JIS来源
            }

            // 检查日文假名
            foreach (char c in decoded)
            {
                if (IsJapaneseKana(c))
                {
                    return false; // 发现假名，判定为乱码
                }
            }

            return true; // 未发现假名，可能为合法中文
        }

        private static bool IsJapaneseKana(char c)
        {
            // 平假名范围：3040-309F，片假名：30A0-30FF
            return (c >= '\u3040' && c <= '\u309F') ||
                   (c >= '\u30A0' && c <= '\u30FF');
        }
    }
}
