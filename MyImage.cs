using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace MyPrtSc
{
    public class MyImage
    {
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

        private static int[] modes = { 0, 1, 2, 3, 4 };
        public static Bitmap RemoveAlphaChannel(Bitmap source)
        {
            var target = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(target))
            {
                g.Clear(Color.White); // 可选：设置用于替换Alpha通道的背景色
                g.DrawImageUnscaled(source, 0, 0);
            }
            return target;
        }

        public static byte[] OptimizePng(Bitmap bmp)
        {
            byte[] smallestPng = null;
            foreach (var filterMode in modes) // 遍历所有PNG过滤模式
            {
                using (var ms = new MemoryStream())
                {
                    SaveOptimizedPng(bmp, ms, filterMode);
                    if (smallestPng == null || ms.Length < smallestPng.Length)
                    {
                        smallestPng = ms.ToArray();
                    }
                }
            }
            return smallestPng;
        }

        static void SaveOptimizedPng(Bitmap bmp, Stream stream, int filterOption)
        {
            var encoderParams = new EncoderParameters(2);

            // 设置压缩级别为最大（0-100，100对应最大压缩）
            encoderParams.Param[0] = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionLZW);

            // 设置PNG过滤模式（关键优化参数）
            encoderParams.Param[1] = new EncoderParameter(Encoder.SaveFlag, filterOption);

            var pngEncoder = GetEncoder(ImageFormat.Png);
            bmp.Save(stream, pngEncoder, encoderParams);
        }

        static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
    }
}
