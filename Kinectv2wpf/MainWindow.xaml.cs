using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
using Microsoft.Kinect;
using System.Diagnostics;

namespace Kinectv2wpf
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        // Kinect SDK
        KinectSensor kinect;
        AudioBeamFrameReader audioBeamFrameReader;

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

                // 音声リーダーを開く
                audioBeamFrameReader = kinect.AudioSource.OpenReader();
                audioBeamFrameReader.FrameArrived += audioBeamFrameReader_FrameArrived;


            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Close();
            }
        }

        // データの取得
        void audioBeamFrameReader_FrameArrived(object sender, AudioBeamFrameArrivedEventArgs e)
        {
            using (var audioFrame = e.FrameReference.AcquireBeamFrames() as AudioBeamFrameList)
            {

                if (audioFrame == null)
                {
                    return;
                }

                for (int i = 0; i < audioFrame.Count; i++)
                {
                    using (var frame = audioFrame[i])
                    {
                        for (int j = 0; j < frame.SubFrames.Count; j++)
                        {
                            using (var subFrame = frame.SubFrames[j])
                            {

                                // 音の方向
                                LineBeamAngle.Angle = (int)(subFrame.BeamAngle * 180 / Math.PI);

                                // 音の方向の信頼性[0~1]
                                TextBeamAngleConfidence.Text = subFrame.BeamAngleConfidence.ToString();

                            }
                        }
                    }
                }
            }    
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            if (audioBeamFrameReader != null)
            {
                audioBeamFrameReader.Dispose();
                audioBeamFrameReader = null;
            }

            if (kinect != null)
            {
                kinect.Close();
                kinect = null;
            }
        }
    }
}
