using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace WpfApp5
{

    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        BitmapImage read_image= new BitmapImage();
        BitmapImage bitmapImage;
        int width, height, stride;
        WriteableBitmap writeableBitmap;
        PixelColor[,] pixel_Closing,pixel_ori;
        PixelHSV[,] hsv_image;
        bool inverse_enable = false;
        public class PointBitmap
        {
            Bitmap source = null;
            IntPtr Iptr = IntPtr.Zero;
            BitmapData bitmapData = null;

            public int Depth { get; private set; }
            public int Width { get; private set; }
            public int Height { get; private set; }

            public PointBitmap(Bitmap source)
            {
                this.source = source;
            }

            public void LockBits()
            {
                try
                {
                    // Get width and height of bitmap
                    Width = source.Width;
                    Height = source.Height;

                    // get total locked pixels count
                    int PixelCount = Width * Height;

                    // Create rectangle to lock
                    var rect = new System.Drawing.Rectangle(0, 0, Width, Height);

                    // get source bitmap pixel format size
                    Depth = System.Drawing.Bitmap.GetPixelFormatSize(source.PixelFormat);

                    // Check if bpp (Bits Per Pixel) is 8, 24, or 32
                    if (Depth != 8 && Depth != 24 && Depth != 32)
                    {
                        throw new ArgumentException("Only 8, 24 and 32 bpp images are supported.");
                    }

                    // Lock bitmap and return bitmap data
                    bitmapData = source.LockBits(rect, ImageLockMode.ReadWrite,
                                                 source.PixelFormat);

                    //得到首地址
                    unsafe
                    {
                        Iptr = bitmapData.Scan0;
                        //二維影象迴圈

                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            public void UnlockBits()
            {
                try
                {
                    source.UnlockBits(bitmapData);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            public System.Drawing.Color GetPixel(int x, int y)
            {
                unsafe
                {
                    byte* ptr = (byte*)Iptr;
                    ptr = ptr + bitmapData.Stride * y;
                    ptr += Depth * x / 8;
                    System.Drawing.Color c = System.Drawing.Color.Empty;
                    if (Depth == 32)
                    {
                        int a = ptr[3];
                        int r = ptr[2];
                        int g = ptr[1];
                        int b = ptr[0];
                        c = System.Drawing.Color.FromArgb(a, r, g, b);
                    }
                    else if (Depth == 24)
                    {
                        int r = ptr[2];
                        int g = ptr[1];
                        int b = ptr[0];
                        c = System.Drawing.Color.FromArgb(r, g, b);
                    }
                    else if (Depth == 8)
                    {
                        int r = ptr[0];
                        c = System.Drawing.Color.FromArgb(r, r, r);
                    }
                    return c;
                }
            }

            public void SetPixel(int x, int y, System.Drawing.Color c)
            {
                unsafe
                {
                    byte* ptr = (byte*)Iptr;
                    ptr = ptr + bitmapData.Stride * y;
                    ptr += Depth * x / 8;
                    if (Depth == 32)
                    {
                        ptr[3] = c.A;
                        ptr[2] = c.R;
                        ptr[1] = c.G;
                        ptr[0] = c.B;
                    }
                    else if (Depth == 24)
                    {
                        ptr[2] = c.R;
                        ptr[1] = c.G;
                        ptr[0] = c.B;
                    }
                    else if (Depth == 8)
                    {
                        ptr[2] = c.R;
                        ptr[1] = c.G;
                        ptr[0] = c.B;
                    }
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog of = new OpenFileDialog
            {
                Filter = "Image Files(*.BMP;*.JPG;*.GIF)|*.BMP;*.JPG;*.GIF|All files (*.*)|*.*",
                FilterIndex = 2
            };
            of.ShowDialog();
            String filename = of.FileName.ToString();
            if (filename != "")
            {
                try
                {
                    read_image = new BitmapImage(new Uri(of.FileName, UriKind.RelativeOrAbsolute));
                    image1.Source = read_image;
                    bitmapImage = read_image.Clone();
                    width = bitmapImage.PixelWidth;
                    height = bitmapImage.PixelHeight;
                    stride = (width * bitmapImage.Format.BitsPerPixel + 7) / 8;
                    pixel_Closing = GetPixels(bitmapImage);
                    pixel_ori = (PixelColor[,])pixel_Closing.Clone();
                    writeableBitmap = new WriteableBitmap(width, height, bitmapImage.DpiX, bitmapImage.DpiY, PixelFormats.Bgra32, bitmapImage.Palette);
                    Dilation(ref pixel_Closing, 7);
                    Erosion(ref pixel_Closing, 7);
                    hsv_image = Transfer_to_HSV(pixel_Closing);

                }
                catch { }
            }

            //button2_Click();
        }
        private PixelHSV[,] Transfer_to_HSV(PixelColor[,] pixel)
        {
            var height = pixel.GetLength(1);
            var width = pixel.GetLength(0);
            double max, min;
            PixelHSV[,] result = new PixelHSV[width, height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    max = Math.Max( Math.Max(pixel[x, y].Red, pixel[x, y].Green), pixel[x, y].Blue);
                    min= Math.Min(Math.Min(pixel[x, y].Red, pixel[x, y].Green), pixel[x, y].Blue);
                    result[x, y].Value = max / 255;
                    if (max == min)
                        result[x, y].Hue = 0;
                    else if (max == pixel[x, y].Red && pixel[x, y].Green >= pixel[x, y].Blue)
                        result[x, y].Hue = 60 * (pixel[x, y].Green - pixel[x, y].Blue) / ((max - min)) + 0;
                    else if (max == pixel[x, y].Red && pixel[x, y].Green < pixel[x, y].Blue)
                        result[x, y].Hue = 60 * (pixel[x, y].Green - pixel[x, y].Blue) / ((max - min)) + 360;
                    else if (max == pixel[x, y].Green )
                        result[x, y].Hue = 60 * (pixel[x, y].Blue - pixel[x, y].Red) / ((max - min)) + 120;
                    else if (max == pixel[x, y].Blue)
                        result[x, y].Hue = 60 * (pixel[x, y].Red - pixel[x, y].Green) / ((max - min)) + 240;
                    if (max == 0)
                        result[x, y].Saturation = 0;
                    else
                        result[x, y].Saturation = 1 - (min / max)/255;
                }
            return result;
        }

    public PixelColor[,] GetPixels( BitmapSource source)
        {
            if (source.Format != PixelFormats.Bgra32)
                source = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);

            int width = source.PixelWidth;
            int height = source.PixelHeight;
            PixelColor[,] result = new PixelColor[width, height];
            BitmapSourceHelper.CopyPixels(source, result, width * source.Format.BitsPerPixel / 8, 0);
            return result;
        }
#if true
        public unsafe static void PutPixels(WriteableBitmap bitmap, PixelColor[,] pixels, int x, int y)
        {
            int width = pixels.GetLength(0);
            int height = pixels.GetLength(1);
            var stride = width * ((bitmap.Format.BitsPerPixel + 7) / 8);
            byte[] pixel = new byte[height * stride];

            Parallel.For(0, height, n =>
            {
                int m;

                for (m = 0; m < width; m++)
                {
                    pixel[(n * width + m) * 4 + 0] = pixels[m, n].Blue;
                    pixel[(n * width + m) * 4 + 1] = pixels[m, n].Green;
                    pixel[(n * width + m) * 4 + 2] = pixels[m, n].Red;
                    pixel[(n * width + m) * 4 + 3] = pixels[m, n].Alpha;
                }
            });

            fixed (byte* buffer = &pixel[0])
                bitmap.WritePixels(
                new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight),
                (IntPtr)(buffer),
                width * height * sizeof(PixelColor),
                stride);
        }
#else

        public void PutPixels(WriteableBitmap bitmap, PixelColor[,] pixels, int x, int y)
        {
            int width = pixels.GetLength(0);
            int height = pixels.GetLength(1);
            var stride =  width *((bitmap.Format.BitsPerPixel + 7) / 8);
            byte[] pixel = new byte[height * stride];

            Parallel.For(0, height, n =>
            {
                int m;

                for (m = 0; m < width; m++)
                {
                        pixel[(n * width + m) * 4 + 0] = pixels[m, n].Blue;
                        pixel[(n * width + m) * 4 + 1] = pixels[m, n].Green;
                        pixel[(n * width + m) * 4 + 2] = pixels[m, n].Red; 
                        pixel[(n * width + m) * 4 + 3] = pixels[m , n].Alpha;
                }
            });


            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixel, width * 4, x, y);
        }
#endif
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Show_HSV_picture();

        }

        private void Show_HSV_picture()
        {
            if (read_image.UriSource != null)
            {
                var slider_hue_lower = Slider_Hue_Lower.Value;
                var slider_hue_upper = Slider_Hue_Upper.Value > slider_hue_lower ? Slider_Hue_Upper.Value : (slider_hue_lower);
                var slider_sat_lower = Slider_Satuation_Lower.Value;
                var slider_sat_upper = Slider_Satuation_Upper.Value > slider_sat_lower ? Slider_Satuation_Upper.Value : (slider_sat_lower);
                var slider_value_lower = Slider_Value_Lower.Value;
                var slider_value_upper = Slider_Value_Upper.Value > slider_value_lower ? Slider_Value_Upper.Value : (slider_value_lower);

                PixelColor[,] pixel_temp = (PixelColor[,])pixel_ori.Clone();

                Parallel.For(0, height, y =>
                {
                    int x;
                    for (x = 0; x < width; x++)
                    {
                        if (hsv_image[x, y].Hue >= slider_hue_lower && hsv_image[x, y].Hue <= slider_hue_upper && hsv_image[x, y].Saturation >= slider_sat_lower && hsv_image[x, y].Saturation <= slider_sat_upper && hsv_image[x, y].Value >= slider_value_lower && hsv_image[x, y].Value <= slider_value_upper)
                        {
                            if (inverse_enable)
                            {
                                pixel_temp[x, y].Red = 0;
                                pixel_temp[x, y].Green = 0;
                                pixel_temp[x, y].Blue = 0;
                            }
                        }
                        else
                        {
                            if (!inverse_enable)
                            {
                                pixel_temp[x, y].Red = 0;
                                pixel_temp[x, y].Green = 0;
                                pixel_temp[x, y].Blue = 0;
                            }
                        }
                    }
                });


                //writeableBitmap.WritePixels(new Int32Rect(0, 0, width, height), GetPixels(bitmapImage), stride, 0);
                PutPixels(writeableBitmap, pixel_temp, 0, 0);
                image1.Source = writeableBitmap;

            }
        }

        void Erosion(ref PixelColor[,] pixels, int ksize)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            PixelColor[,] temp = (PixelColor[,]) pixels.Clone();
            PixelColor[,] temp1 = pixels;
            Parallel.For(0, pixels.GetLength(0) - 1, x =>
            {
                for (int y = 0; y < temp1.GetLength(1); y++)
                {
                    temp1[x, y] = Erosion_Crossdot(temp, x, y, ksize);
                }
            });
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            Console.WriteLine("RunTime Erosion" + elapsedTime);

        }
        PixelColor Erosion_Crossdot(PixelColor[,] temp, int x, int y, int ksize)
        {
            int k, l, m, n;
            byte checkpixel_R = 255;
            byte checkpixel_G = 255;
            byte checkpixel_B = 255;
            var weidth = temp.GetLength(0);
            var height = temp.GetLength(1);
            for (int i = 0; i < ksize; i++)
            {
                k = (i - ksize / 2);
                for (int j = 0; j < ksize; j++)
                {
                    l = (j - ksize / 2);
                    m = ((x - k) >= 0 && (x - k) < weidth) ? (x - k) : x + k;
                    n = ((y - l) >= 0 && (y - l) < height) ? (y - l) : y + l;
                    checkpixel_R = Math.Min(checkpixel_R, temp[m, n].Red);
                    checkpixel_G = Math.Min(checkpixel_G, temp[m, n].Green);
                    checkpixel_B = Math.Min(checkpixel_B, temp[m, n].Blue);
                }
            }
            return new PixelColor { Blue = checkpixel_B, Green = checkpixel_G, Red = checkpixel_R, Alpha = temp[x, y].Alpha };
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            inverse_enable = inverse_enable^true;
            Show_HSV_picture();
        }

        void Dilation(ref PixelColor[,] pixels, int ksize)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            PixelColor[,] temp = (PixelColor[,])pixels.Clone();
            PixelColor[,] temp1 = pixels;
            Parallel.For(0, pixels.GetLength(0) - 1, x =>
              {
                  for (int y = 0; y < temp.GetLength(1); y++)
                  {
                      temp1[x,y]=Dilation_Crossdot(temp, x, y, ksize);
                  }
              });
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            Console.WriteLine("RunTime " + elapsedTime);

        }
        PixelColor Dilation_Crossdot( PixelColor[,] temp, int x, int y, int ksize)
        {
            int k, l, m, n;
            byte checkpixel_R = 0;
            byte checkpixel_G = 0;
            byte checkpixel_B = 0;
            var weidth = temp.GetLength(0);
            var height = temp.GetLength(1);
            for (int i = 0; i < ksize; i++)
            {
                k = (i - ksize / 2);
                for (int j = 0; j < ksize; j++)
                {

                    l = (j - ksize / 2);
                    m = ((x - k) >= 0 && (x - k) < weidth) ? (x - k) : x + k;
                    n = ((y - l) >= 0 && (y - l) < height) ? (y - l) : y + l;
                    checkpixel_R = Math.Max(checkpixel_R, temp[m, n].Red);
                    checkpixel_G = Math.Max(checkpixel_G, temp[m, n].Green);
                    checkpixel_B = Math.Max(checkpixel_B, temp[m, n].Blue);

                }
            }
            return new PixelColor { Blue = checkpixel_B,Green = checkpixel_G,Red = checkpixel_R,   Alpha= temp [x,y].Alpha};
        }

    }

    public struct PixelHSV
    {
        public double Hue;
        public double Saturation;
        public double Value;
    }
    public struct PixelRGB
    {
        public double Red;
        public double Green;
        public double Blue;
    }
    [StructLayout(LayoutKind.Explicit)]
    public struct PixelColor
    {
        [FieldOffset(0)] public byte Blue;
        [FieldOffset(1)] public byte Green;
        [FieldOffset(2)] public byte Red;
        [FieldOffset(3)] public byte Alpha;
    }
    public static class BitmapSourceHelper
    {
#if true
            public unsafe static void CopyPixels(this BitmapSource source, PixelColor[,] pixels, int stride, int offset)
            {
            int width = pixels.GetLength(0);
            int height = pixels.GetLength(1);
            byte[] pixel = new byte[height * stride];
            fixed (byte* buffer = &pixel[0])
            source.CopyPixels(
            new Int32Rect(0, 0, source.PixelWidth, source.PixelHeight),
            (IntPtr)(buffer + offset),
            width * height * sizeof(PixelColor),
            stride);
            Parallel.For(0, height, n =>
            {
                int m;

                for (m = 0; m < width; m++)
                {
                    pixels[m, n].Blue = pixel[(n * width + m) * 4 + 0] ;
                    pixels[m, n].Green = pixel[(n * width + m) * 4 + 1];
                    pixels[m, n].Red = pixel[(n * width + m) * 4 + 2];
                    pixels[m, n].Alpha = pixel[(n * width + m) * 4 + 3];
                }
            });

        }
#else
        public static void CopyPixels(this BitmapSource source, PixelColor[,] pixels, int stride, int offset)
        {
            var height = source.PixelHeight;
            var width = source.PixelWidth;
            var pixelBytes = new byte[height * width * 4];
            source.CopyPixels(pixelBytes, stride, 0);
            int y0 = offset / width;
            int x0 = offset - width * y0;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    pixels[x + x0, y + y0] = new PixelColor
                    {
                        Blue = pixelBytes[(y * width + x) * 4 + 0],
                        Green = pixelBytes[(y * width + x) * 4 + 1],
                        Red = pixelBytes[(y * width + x) * 4 + 2],
                        Alpha = pixelBytes[(y * width + x) * 4 + 3],
                    };
        }
#endif
    }

}
