using Microsoft.Kinect;
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

namespace Kinectv2wpf
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        // Kinect SDK
        KinectSensor kinect;
        MultiSourceFrameReader multReader;

        CoordinateMapper mapper;


        // Color
        FrameDescription colorFrameDesc;
        ColorImageFormat colorFormat = ColorImageFormat.Bgra;
        byte[] colorBuffer;

        // Depth
        FrameDescription depthFrameDesc;
        ushort[] depthBuffer;

        // BodyIndex
        byte[] bodyIndexBuffer;


        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // kinectを開く
                kinect = KinectSensor.GetDefault();
                if (kinect == null)
                {
                    throw new Exception("Kinectを開けません");
                }
                kinect.Open();

                mapper = kinect.CoordinateMapper;

                // カラー画像の情報を作成する（BGRAフォーマット）
                colorFrameDesc = kinect.ColorFrameSource.CreateFrameDescription(colorFormat);
                colorBuffer = new byte[colorFrameDesc.LengthInPixels * colorFrameDesc.BytesPerPixel];

                // Depthデータの情報を取得する
                depthFrameDesc = kinect.DepthFrameSource.FrameDescription;
                depthBuffer = new ushort[depthFrameDesc.LengthInPixels];

                // BodyIndexデータの情報を取得する
                var bodyIndexFrameDecs = kinect.BodyIndexFrameSource.FrameDescription;
                bodyIndexBuffer = new byte[bodyIndexFrameDecs.LengthInPixels];

                // フレームリーダーを開く
                multReader = kinect.OpenMultiSourceFrameReader(
                    FrameSourceTypes.Color |
                    FrameSourceTypes.Depth |
                    FrameSourceTypes.BodyIndex );

                multReader.MultiSourceFrameArrived += multReader_MultiSourceFrameArrived;

            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Close();
            }
        }

        void multReader_MultiSourceFrameArrived( object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var multiFrame = e.FrameReference.AcquireFrame();
            if (multiFrame == null)
            {
                return;
            }

            // 各種データを取得する
            UpdateColorFrame(multiFrame);
            UpdateBodyIndexFrame(multiFrame);
            UpdateDepthFrame(multiFrame);

            // それぞれの座標系で描画する
            if (IsColorCoodinate.IsChecked == true)
            {
                DrawColorCoodinate();
            }
            else
            {
                DrawDepthCoodinate();
            }
        }

        // カラー画像更新処理
        private void UpdateColorFrame(MultiSourceFrame multiFrame)
        {
            // カラーフレームを取得する
            using (var colorFrame = multiFrame.ColorFrameReference.AcquireFrame())
            {
                if (colorFrame == null)
                {
                    return;
                }

                // BGRAデータを取得
                colorFrame.CopyConvertedFrameDataToArray(colorBuffer, ColorImageFormat.Bgra);
            }
        }

        // BodyIndex更新処理
        private void UpdateBodyIndexFrame(MultiSourceFrame multiFrame)
        {
            using (var bodyIndexFrame = multiFrame.BodyIndexFrameReference.AcquireFrame())
            {
                if (bodyIndexFrame == null)
                {
                    return;
                }

                // ボディインデックスデータを取得する
                bodyIndexFrame.CopyFrameDataToArray(bodyIndexBuffer);
            }
        }

        // Depth更新処理
        private void UpdateDepthFrame(MultiSourceFrame multiFrame)
        {
            using (var depthFrame = multiFrame.DepthFrameReference.AcquireFrame())
            {
                if (depthFrame == null)
                {
                    return;
                }

                // Depthデータを取得
                depthFrame.CopyFrameDataToArray(depthBuffer);
            }
        }


        private void DrawColorCoodinate()
        {
            // カラー画像の解像度でデータを作る
            var colorImageBuffer = new byte[colorFrameDesc.LengthInPixels * colorFrameDesc.BytesPerPixel];

            // カラー座標系に対応するDepth座標系の一覧を取得する
            var depthSpace = new DepthSpacePoint[colorFrameDesc.LengthInPixels];
            mapper.MapColorFrameToDepthSpace(depthBuffer, depthSpace);

            // 並列で処理する
            Parallel.For(0, colorFrameDesc.LengthInPixels, i =>
            {
                int depthX = (int)depthSpace[i].X;
                int depthY = (int)depthSpace[i].Y;
                if ((depthX < 0) || (depthFrameDesc.Width <= depthX) || 
                    (depthY < 0) || (depthFrameDesc.Height <= depthY) )
                {
                    return;
                }

                // Depth座標系のインデックス
                int depthIndex = (depthY * depthFrameDesc.Width) + depthX;
                int bodyIndex = bodyIndexBuffer[depthIndex];

                // 人を検出した位置だけ色を付ける
                if (bodyIndex == 255)
                {
                    return;
                }

                // カラー画像を設定する
                int colorImageIndex = (int)(i * colorFrameDesc.BytesPerPixel);
                colorImageBuffer[colorImageIndex + 0] = colorBuffer[colorImageIndex + 0];
                colorImageBuffer[colorImageIndex + 1] = colorBuffer[colorImageIndex + 1];
                colorImageBuffer[colorImageIndex + 2] = colorBuffer[colorImageIndex + 2];
            });

            ImageColor.Source = BitmapSource.Create(
                colorFrameDesc.Width, colorFrameDesc.Height, 96, 96,
                PixelFormats.Bgr32, null, colorImageBuffer,
                (int)(colorFrameDesc.Width * colorFrameDesc.BytesPerPixel));
        }

        private void DrawDepthCoodinate()
        {
            // Depth画像の解像度でデータを作る
            var colorImageBuffer = new byte[depthFrameDesc.LengthInPixels * colorFrameDesc.BytesPerPixel];

            // Depth座標系に対応するカラー座標系の一覧を取得する
            var colorSpace = new ColorSpacePoint[depthFrameDesc.LengthInPixels];
            mapper.MapDepthFrameToColorSpace(depthBuffer, colorSpace);

            // 並列で処理する
            Parallel.For(0, depthFrameDesc.LengthInPixels, i =>
            {
                int colorX = (int)colorSpace[i].X;
                int colorY = (int)colorSpace[i].Y;
                if ((colorX < 0) || (colorFrameDesc.Width <= colorX) || (colorY < 0) || (colorFrameDesc.Height <= colorY))
                {
                    return;
                }

                // カラー座標系のインデックス
                int colorIndex = (colorY * colorFrameDesc.Width) + colorX;
                int bodyIndex = bodyIndexBuffer[i];

                // 人を検出した位置だけ色を塗る
                if (bodyIndex == 255)
                {
                    return;
                }

                // カラー画像を設定する
                int colorImageIndex = (int)(i * colorFrameDesc.BytesPerPixel);
                int colorBufferIndex = (int)(colorIndex * colorFrameDesc.BytesPerPixel);
                colorImageBuffer[colorImageIndex + 0] = colorBuffer[colorBufferIndex + 0];
                colorImageBuffer[colorImageIndex + 1] = colorBuffer[colorBufferIndex + 1];
                colorImageBuffer[colorImageIndex + 2] = colorBuffer[colorBufferIndex + 2];
            });

            ImageColor.Source = BitmapSource.Create(
                depthFrameDesc.Width, depthFrameDesc.Height, 96, 96,
                PixelFormats.Bgr32, null, colorImageBuffer,
                (int)(depthFrameDesc.Width * colorFrameDesc.BytesPerPixel) );
        }

    }
}
