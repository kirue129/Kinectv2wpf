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
                int depthIndex = (depthX * depthFrameDesc.Width) + depthX;
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

        /*
        // 更新処理
        void bodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            UpdateBodyFrame(e);
            DrowBodyFrame();
        }

        // ボディの更新
        private void UpdateBodyFrame( BodyFrameArrivedEventArgs e)
        {
            using ( var bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame == null)
                {
                    return;
                }

                // ボディデータを取得する
                bodyFrame.GetAndRefreshBodyData(bodies);
            }
        }

        // ボディの表示
        private void DrowBodyFrame()
        {
            CanvasBody.Children.Clear();

            // 追跡しているBodyのみループする
            foreach(var body in bodies.Where(b => b.IsTracked))
            {
                foreach(var joint in body.Joints)
                {
                    // 手の位置が追跡状態
                    if(joint.Value.TrackingState == TrackingState.Tracked)
                    {
                        DrawEllipse(joint.Value, 10, Brushes.Blue);

                        // 左手が追跡していたら、手の状態を表示する
                        if ( joint.Value.JointType == JointType.HandLeft)
                        {
                            DrawHandState(body.Joints[JointType.HandLeft], body.HandLeftConfidence, body.HandLeftState);
                        }
                        // 右手を追跡していたら、手の状態を表示する
                        else if (joint.Value.JointType == JointType.HandRight)
                        {
                            DrawHandState(body.Joints[JointType.HandRight], body.HandRightConfidence, body.HandRightState);
                        }
                    }
                    // 手の位置が推測状態
                    else if(joint.Value.TrackingState == TrackingState.Inferred)
                    {
                        DrawEllipse(joint.Value, 10, Brushes.Yellow);
                    }
                }
            }
        }

        private void DrawHandState(Joint joint,TrackingConfidence trackingConfidence, HandState handState)
        {
            // 手の追跡信頼性が高い
            if (trackingConfidence != TrackingConfidence.High)
            {
                return;
            }

            // 手が開いている(バー)
            if (handState == HandState.Open)
            {
                DrawEllipse(joint, 40, new SolidColorBrush(new Color()
                {
                    R = 255,
                    G = 255,
                    A = 128
                }));
            }
            // チョキのような感じ
            else if (handState == HandState.Lasso)
            {
                DrawEllipse(joint, 40, new SolidColorBrush(new Color()
                {
                    R = 255,
                    B = 255,
                    A = 128
                }));
            }
            // 手が閉じている(グー)
            else if (handState == HandState.Closed)
            {
                DrawEllipse(joint, 40, new SolidColorBrush(new Color()
                {
                    G = 255,
                    B = 255,
                    A = 128
                }));
            }
        }

        private void DrawEllipse(Joint joint, int R, Brush brush)
        {
            var ellipse = new Ellipse()
            {
                Width = R,
                Height = R,
                Fill = brush,
            };

            // カメラ座標系をDepth座標系に変換する
            var point = kinect.CoordinateMapper.MapCameraPointToDepthSpace(joint.Position);
            if((point.X < 0) || (point.Y < 0))
            {
                return;
            }

            // Depth座標系で円を配置する
            Canvas.SetLeft(ellipse, point.X - (R / 2));
            Canvas.SetTop(ellipse, point.Y - (R / 2));

            CanvasBody.Children.Add(ellipse);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if ( bodyFrameReader != null)
            {
                bodyFrameReader.Dispose();
                bodyFrameReader = null;
            }

            if (kinect != null)
            {
                kinect.Close();
                kinect = null;
            }
        }
        */

    }
}
