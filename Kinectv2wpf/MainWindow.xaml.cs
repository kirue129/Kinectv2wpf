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

        DepthFrameReader depthFrameReader;
        FrameDescription depthFrameDesc;

        // 表示用
        WriteableBitmap depthImage;
        ushort[] depthBuffer;
        byte[] depthBitmapBuffer;
        Int32Rect depthRect;
        int depthStride;

        Point depthPoint;
        const int R = 20;

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

                // 表示のためのデータを作成
                depthFrameDesc = kinect.DepthFrameSource.FrameDescription;

                // Depthリーダーを開く
                depthFrameReader = kinect.DepthFrameSource.OpenReader();
                depthFrameReader.FrameArrived += depthFrameReader_FrameArrived;

                // 表示のためにビットマップに必要なものを作成
                depthImage = new WriteableBitmap(depthFrameDesc.Width, depthFrameDesc.Height, 96, 96, PixelFormats.Gray8, null);
                depthBuffer = new ushort[depthFrameDesc.LengthInPixels];
                depthBitmapBuffer = new byte[depthFrameDesc.LengthInPixels];
                depthRect = new Int32Rect(0, 0, depthFrameDesc.Width, depthFrameDesc.Height);
                depthStride = (int)(depthFrameDesc.Width);

                ImageDepth.Source = depthImage;

                // 初期の位置表示座標(中心点)
                depthPoint = new Point(depthFrameDesc.Width / 2, depthFrameDesc.Height / 2);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Close();
            }
        }

        // 更新処理
        void depthFrameReader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            UpdateDepthFrame(e);
            DrowDepthFrame();
        }

        private void UpdateDepthFrame( DepthFrameArrivedEventArgs e)
        {
            using ( var depthFrame = e.FrameReference.AcquireFrame())
            {
                if (depthFrame == null)
                {
                    return;
                }

                // Depthデータを取得
                depthFrame.CopyFrameDataToArray(depthBuffer);
            }
        }

        private void DrowDepthFrame()
        {
            // 距離情報の表示を更新する
            UpdateDepthValue();

            // 0~8000のデータを0~65535のデータに変換する(見やすく)
            for(int i = 0 ; i < depthBuffer.Length ; i++)
            {
                depthBitmapBuffer[i] = (byte)(depthBuffer[i] % 255);
            }

            depthImage.WritePixels(depthRect, depthBuffer, depthStride, 0);            
        }



        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if ( depthFrameReader != null)
            {
                depthFrameReader.Dispose();
                depthFrameReader = null;
            }

            if (kinect != null)
            {
                kinect.Close();
                kinect = null;
            }
        }

        // クリックした座標を取得
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            depthPoint = e.GetPosition(this);
        }

        // クリックした座標の距離を表示
        private void UpdateDepthValue()
        {
            CanvasPoint.Children.Clear();

            // クリックしたポイントを表示する
            var ellipse = new Ellipse()
            {
                Width = R,
                Height = R,
                StrokeThickness = R / 4,
                Stroke = Brushes.Red,
            };
            Canvas.SetLeft(ellipse, depthPoint.X - (R / 2));
            Canvas.SetTop(ellipse, depthPoint.Y - (R / 2));
            CanvasPoint.Children.Add(ellipse);

            // クリックしたポイントのインデックスを計算する
            int depthindex = (int)((depthPoint.Y * depthFrameDesc.Width) + depthPoint.X);

            // クリックしたポインタの距離を表示する
            var text = new TextBlock()
            {
                Text = string.Format("{0}mm", depthBuffer[depthindex]),
                FontSize = 20,
                Foreground = Brushes.Green,
            };
            Canvas.SetLeft(text, depthPoint.X);
            Canvas.SetTop(text, depthPoint.Y - R);
            CanvasPoint.Children.Add(text);

        }
    }
}
