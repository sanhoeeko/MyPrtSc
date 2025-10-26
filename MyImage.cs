using ImageMagick;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyPrtSc
{
    public class MyImage
    {
        private static bool optipng_initialized = false;
        private static int optipng_level = 1;
        public static string tempDir, optipngPath;

        public static string magick_dll_path = Directory.GetFiles(
            Directory.GetCurrentDirectory(), "Magick.Native-Q16-*.dll").FirstOrDefault();
        public static bool use_magick = !(magick_dll_path == null);

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
                if (char.IsDigit(format[i])) numberStart = i;
                else break;
            }

            // 如果没有找到数字，返回0
            if (numberStart == -1) return 0;

            // 提取数字部分并转换为整数
            string numberStr = format.Substring(numberStart, numberEnd - numberStart + 1);
            if (int.TryParse(numberStr, out int result)) return result;
            return 0;
        }

        public static async Task SaveImage(Bitmap image, string outputPath, string format)
        {
            switch (ParseFileFormat(format))
            {
                case 0: image.Save(outputPath, ImageFormat.Png); break;
                case 1: await new MyImage().OptimizeImageAsync(image, outputPath); break;
                case 2:
                    {
                        if (use_magick) MagicSave(image, outputPath, ParseQuality(format), MagickFormat.Jpg);
                        else SaveImageAsJpeg(image, outputPath, ParseQuality(format));
                        break;
                    }
                case 3:
                    {
                        if (use_magick) MagicSave(image, outputPath, ParseQuality(format), MagickFormat.Avif);
                        else throw new Exception("无法保存为AVIF格式：MagicK DLL 缺失");
                        break;
                    }
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

        public static void SaveImageAsJpeg(Image image, string filePath, int quality)
        {
            // 获取 JPEG 编码器
            var jpegEncoder = ImageCodecInfo.GetImageEncoders().FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
            if (jpegEncoder == null) throw new Exception("无法找到 JPEG 编码器");

            // 设置编码参数
            var encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);

            // 保存图像
            image.Save(filePath, jpegEncoder, encoderParameters);
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

        public static Image DropAlphaChannel(Image image)
        {
            var target = new Bitmap(image.Width, image.Height, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(target))
            {
                g.Clear(Color.White);
                g.DrawImageUnscaled(image, 0, 0);
            }
            return target;
        }

        public static Bitmap CropBitmap(Bitmap image)
        {
            if (image == null) return null;
            if (image.Width <= 2 || image.Height <= 2) return new Bitmap(image);

            int left = 0, right = image.Width - 1;
            int top = 0, bottom = image.Height - 1;

            while (left <= right && IsSingleColorColumn(image, left)) left++;
            while (right >= left && IsSingleColorColumn(image, right)) right--;
            while (top <= bottom && IsSingleColorRow(image, top)) top++;
            while (bottom >= top && IsSingleColorRow(image, bottom)) bottom--;

            if (left > right || top > bottom) return new Bitmap(image);

            int width = right - left + 1;
            int height = bottom - top + 1;
            Bitmap croppedImage = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(croppedImage))
            {
                g.DrawImage(image, new Rectangle(0, 0, width, height),
                            new Rectangle(left, top, width, height), GraphicsUnit.Pixel);
            }

            return croppedImage;
        }

        private static bool IsSingleColorColumn(Bitmap image, int x)
        {
            Color firstColor = image.GetPixel(x, 0);
            for (int y = 1; y < image.Height; y++)
            {
                if (image.GetPixel(x, y) != firstColor) return false;
            }
            return true;
        }

        private static bool IsSingleColorRow(Bitmap image, int y)
        {
            Color firstColor = image.GetPixel(0, y);
            for (int x = 1; x < image.Width; x++)
            {
                if (image.GetPixel(x, y) != firstColor) return false;
            }
            return true;
        }
    }
}
