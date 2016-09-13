﻿using System;
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

        // Bodyindex
        BodyIndexFrameReader bodyIndexFrameReader;
        FrameDescription bodyIndexFrameDesc;
        byte[] bodyIndexBuffer;

        // 表示用
        WriteableBitmap bodyIndexColorImage;
        Int32Rect bodyIndexColorRect;
        int bodyIndexColorStride;
        int bodyIndexColorBytesPerPixel = 4;
        byte[] bodyIndexColorBuffer;

        // Body
        BodyFrameReader bodyFrameReader;
        Body[] bodies;

        // Audio
        AudioBeamFrameReader audioBeamFrameReader;

        // ビーム方向のTrackingIdとそのインデックス
        ulong AudioTrackingId = ulong.MaxValue;
        int AudioTrackingIndex = -1;

        public MainWindow()
        {
            InitializeComponent();
        }

        // 初期化処理
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
                bodyIndexFrameDesc = kinect.DepthFrameSource.FrameDescription;

                // ボディインデックスデータ用のバッファ
                bodyIndexBuffer = new byte[bodyIndexFrameDesc.LengthInPixels];

                // 表示のためのビットマップに必要なものを用意
                bodyIndexColorImage = new WriteableBitmap(bodyIndexFrameDesc.Width, bodyIndexFrameDesc.Height,
                    96, 96, PixelFormats.Bgra32, null );
                bodyIndexColorRect = new Int32Rect(0, 0, bodyIndexFrameDesc.Width, bodyIndexFrameDesc.Height);
                bodyIndexColorStride = (int)(bodyIndexFrameDesc.Width * bodyIndexColorBytesPerPixel);

                // ボディインデックスデータをBGRA(カラー)データにするためのバッファ
                bodyIndexColorBuffer = new byte[bodyIndexFrameDesc.LengthInPixels * bodyIndexColorBytesPerPixel];

                ImageBodyIndex.Source = bodyIndexColorImage;

                // ボディインデックスリーダーを開く
                bodyIndexFrameReader = kinect.BodyIndexFrameSource.OpenReader();
                bodyIndexFrameReader.FrameArrived += bodyIndexFrameReader_FrameArrived;

                // Bodyを入れる配列を作る
                bodies = new Body[kinect.BodyFrameSource.BodyCount];

                // ボディリーダーを開く
                bodyFrameReader = kinect.BodyFrameSource.OpenReader();
                bodyFrameReader.FrameArrived += bodyFrameReader_FrameArrived;

               
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

        // ビーム方向からTrackingIdを取得
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

                                // ビーム方向に人がいれば、そのTrackingIdを保持する
                                if (subFrame.AudioBodyCorrelations.Count != 0)
                                {
                                    AudioTrackingId = subFrame.AudioBodyCorrelations[0].BodyTrackingId;
                                }
                                else
                                {
                                    AudioTrackingId = ulong.MaxValue;
                                }

                            }
                        }
                    }
                }
            }    
        }

        // ビーム方向のTrackingIdからBodyIndexを取得する
        void bodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            // ボディデータを取得する
            using (var bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame == null)
                {
                    return;
                }

                bodyFrame.GetAndRefreshBodyData(bodies);
            }

            // ビーム方向と一致するTrackingIdがあれば、そのインデックス(BodyIndex)を保持する
            AudioTrackingIndex = -1;
            for (int i = 0; i < bodies.Length; i++)
            {
                if(bodies[i].TrackingId == AudioTrackingId)
                {
                    AudioTrackingIndex = i;
                    break;
                }
            }
        }

        // BodyIndexのプレイヤーに着色する
        void bodyIndexFrameReader_FrameArrived(object sender, BodyIndexFrameArrivedEventArgs e)
        {
            // ボディインデックスデータを取得する
            using (var bodyIndexFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyIndexFrame == null)
                {
                    return;
                }

                bodyIndexFrame.CopyFrameDataToArray(bodyIndexBuffer);

            }

            // ボディインデックスデータをBGRAデータに変換する
            for (int i = 0; i < bodyIndexBuffer.Length; i++)
            {
                var index = bodyIndexBuffer[i];
                var colorIndex = i * 4;

                if (index != 255)
                {
                    // BodyIndexとビーム方向のTrackingIdのインデックスが一致している人の色を変える(青)
                    if (index == AudioTrackingIndex)
                    {
                        bodyIndexColorBuffer[colorIndex + 0] = 255;
                        bodyIndexColorBuffer[colorIndex + 1] = 0;
                        bodyIndexColorBuffer[colorIndex + 2] = 0;
                        bodyIndexColorBuffer[colorIndex + 3] = 255;
                    }
                    else
                    {
                        bodyIndexColorBuffer[colorIndex + 0] = 0;
                        bodyIndexColorBuffer[colorIndex + 1] = 0;
                        bodyIndexColorBuffer[colorIndex + 2] = 255;
                        bodyIndexColorBuffer[colorIndex + 3] = 255;
                    }
                }
                else
                {
                    bodyIndexColorBuffer[colorIndex + 0] = 255;
                    bodyIndexColorBuffer[colorIndex + 1] = 255;
                    bodyIndexColorBuffer[colorIndex + 2] = 255;
                    bodyIndexColorBuffer[colorIndex + 3] = 255;
                }
            }

            // ビットマップにする
            bodyIndexColorImage.WritePixels(bodyIndexColorRect, bodyIndexColorBuffer, bodyIndexColorStride, 0);
        }

        // 終了処理
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
