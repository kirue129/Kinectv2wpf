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

        InfraredFrameReader infraredFrameReader;
        FrameDescription infraredFrameDesc;

        // 表示用
        WriteableBitmap infraredBitmap;
        ushort[] infraredBuffer;
        Int32Rect infraredRect;
        int infraredStride;

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

                // 赤外線画像リーダーを取得する
                infraredFrameDesc = kinect.InfraredFrameSource.FrameDescription;

                // 赤外線リーダーを開く
                infraredFrameReader = kinect.InfraredFrameSource.OpenReader();
                infraredFrameReader.FrameArrived += infraredFrameReader_FrameArrived;

                // 表示のためにビットマップに必要なものを作成
                infraredBuffer = new ushort[infraredFrameDesc.LengthInPixels];
                infraredBitmap = new WriteableBitmap(infraredFrameDesc.Width, infraredFrameDesc.Height, 96, 96, PixelFormats.Gray16, null);
                infraredRect = new Int32Rect(0, 0, infraredFrameDesc.Width, infraredFrameDesc.Height);
                infraredStride = infraredFrameDesc.Width * (int)infraredFrameDesc.BytesPerPixel;

                ImageColor.Source = infraredBitmap;
                

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Close();
            }
        }

        // 更新処理
        void infraredFrameReader_FrameArrived(object sender, InfraredFrameArrivedEventArgs e)
        {
            UpdateInfraredFrame(e);
            DrowInfraredFrame();
        }

        private void UpdateInfraredFrame( InfraredFrameArrivedEventArgs e)
        {
            // からあーフレームを取得する
            using ( var infraredFrame = e.FrameReference.AcquireFrame())
            {
                if (infraredFrame == null)
                {
                    return;
                }

                // 赤外線データを取得
                infraredFrame.CopyFrameDataToArray(infraredBuffer);
            }
        }

        private void DrowInfraredFrame()
        {
            infraredBitmap.WritePixels(infraredRect, infraredBuffer, infraredStride, 0);            
        }



        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if ( infraredFrameReader != null)
            {
                infraredFrameReader.Dispose();
                infraredFrameReader = null;
            }

            if (kinect != null)
            {
                kinect.Close();
                kinect = null;
            }
        }

    }
}
