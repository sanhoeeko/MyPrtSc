using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using ImageMagick;

namespace MyPrtSc
{
    public class MyImage
    {
        private static bool optipng_initialized = false;
        private static int optipng_level = 1;
        public static string tempDir, optipngPath;

        public static int ParseFileFormat(string format)
        {
            switch (format)
            {
                case "PNG": return 0;
                case "PNG+": return 1;
            }
            if (format.StartsWith("JPG")) return 2;
            if (format.StartsWith("AVIF")) return 3;
            return -1;
        }

        public static string ParseSuffix(string format)
        {
            string[] suffixes = { "png", "png", "jpg", "avif" };
            return suffixes[ParseFileFormat(format)];
        }

        public static int ParseQuality(string format)
        {
            int numberEnd = format.Length - 1;
            int numberStart = -1;

            // 从字符串末尾向前查找连续数字
            for (int i = format.Length - 1; i >= 0; i--)
            {
                if (char.IsDigit(format[i]))
                {
                    numberStart = i;
                }
                else break;
            }

            // 如果没有找到数字，返回0
            if (numberStart == -1) return 0;

            // 提取数字部分并转换为整数
            string numberStr = format.Substring(numberStart, numberEnd - numberStart + 1);
            if (int.TryParse(numberStr, out int result))
            {
                return result;
            }
            return 0;
        }

        public static async Task SaveImage(Bitmap image, string outputPath, string format)
        {
            switch (ParseFileFormat(format))
            {
                case 0: image.Save(outputPath, ImageFormat.Png); break;
                case 1: await new MyImage().OptimizeImageAsync(image, outputPath); break;
                case 2: MagicSave(image, outputPath, ParseQuality(format), MagickFormat.Jpg); break;
                case 3: MagicSave(image, outputPath, ParseQuality(format), MagickFormat.Avif); break;
                default: throw new NotImplementedException("Unsupported format!");
            }
        }

        public static void MagicSave(Bitmap image, string outputPath, int quality, MagickFormat mgkFormat)
        {
            using (var ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Bmp); // 先保存BMP到内存流
                ms.Position = 0;
                var magickImage = new MagickImage(ms)  // 用MagickImage从流读取
                {
                    Format = mgkFormat,
                    Quality = (uint)quality
                }; 
                magickImage.Write(outputPath);
            }
        }

        public async Task OptimizeImageAsync(Image image, string outputPath)
        {
            image = DropAlphaChannel(image);

            Directory.CreateDirectory(tempDir);
            string tempImagePath = Path.Combine(tempDir, Guid.NewGuid().ToString() + ".png");
            image.Save(tempImagePath, System.Drawing.Imaging.ImageFormat.Png);

            // 异步调用optipng.exe
            var tcs = new TaskCompletionSource<bool>();
            var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = optipngPath,
                Arguments = $"-o{optipng_level} -quiet \"{tempImagePath}\"",
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };

            process.Exited += (s, e) => {
                File.Move(tempImagePath, outputPath);
                tcs.SetResult(true);
            };

            process.EnableRaisingEvents = true;
            process.Start();

            await tcs.Task;
        }

        public static void InitializeOptiPng()
        {
            if (!optipng_initialized)
            {
                // 创建临时目录
                tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);

                // 从资源中提取optipng.exe
                optipngPath = Path.Combine(tempDir, "optipng.exe");
                byte[] exeBytes = Properties.Resources.optipng;
                if (exeBytes == null || exeBytes.Length == 0)
                {
                    throw new FileNotFoundException("未找到嵌入的optipng.exe资源");
                }
                File.WriteAllBytes(optipngPath, exeBytes);

                optipng_initialized = true;
            }
        }

        public static Image DropAlphaChannel(Image source)
        {
            var target = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(target))
            {
                g.Clear(Color.White);
                g.DrawImageUnscaled(source, 0, 0);
            }
            return target;
        }
    }
}
