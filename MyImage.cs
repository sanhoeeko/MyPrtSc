using MyPrtSc.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace MyPrtSc
{
    public class MyImage
    {
        private static bool optipng_initialized = false;
        private static int optipng_level = 1;
        public static string tempDir, tempImagePath, optipngPath;

        private static Rectangle RectangleMul(Rectangle r, double ratio)
        {
            return new Rectangle(
                (int)Math.Round(r.Left * ratio),
                (int)Math.Round(r.Top * ratio),
                (int)Math.Round(r.Width * ratio),
                (int)Math.Round(r.Height * ratio)
            );
        }

        public static Bitmap CropImage(Image source, Rectangle cropArea, double dpi_ratio)
        {
            // 创建目标位图
            int physicalWidth = (int)Math.Round(cropArea.Width * dpi_ratio);
            int physicalHeight = (int)Math.Round(cropArea.Height * dpi_ratio);
            var target = new Bitmap(physicalWidth, physicalHeight);

            // 绘制和裁剪，注意绘制过程中不能做任何插值，否则损失画质
            using (var g = Graphics.FromImage(target))
            {
                g.DrawImage(source,
                    new Rectangle(0, 0, target.Width, target.Height),
                    RectangleMul(cropArea, dpi_ratio),
                    GraphicsUnit.Pixel);
            }

            return target;
        }

        public static async Task SaveImage(Image image, string outputPath, bool IfOptimize)
        {
            if (IfOptimize) await new MyImage().OptimizeImageAsync(image, outputPath);
            else image.Save(outputPath, ImageFormat.Png);
        }

        public async Task OptimizeImageAsync(Image image, string outputPath)
        {
            // 预处理：丢弃alpha通道
            image = DropAlphaChannel(image);

            Directory.CreateDirectory(tempDir);
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
                tempImagePath = Path.Combine(tempDir, "temp.png");
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
                g.Clear(Color.White); // 可选：设置用于替换Alpha通道的背景色
                g.DrawImageUnscaled(source, 0, 0);
            }
            return target;
        }
    }
}
