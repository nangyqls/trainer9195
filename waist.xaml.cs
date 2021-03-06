﻿using Newtonsoft.Json;
using Microsoft.Kinect.VisualGestureBuilder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using WindowsPreview.Kinect;
using MySql.Data.MySqlClient;
using System.Data.SqlClient;

// 빈 페이지 항목 템플릿에 대한 설명은 http://go.microsoft.com/fwlink/?LinkId=234238 에 나와 있습니다.

namespace DetectVGBGesture
{
    /// <summary>
    /// 자체에서 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class waist : Page
    {
        KinectSensor _kinect;
        MultiSourceFrameReader _multiFrameReader;
        IDisposable _multiFrameSubscription;

        private DispatcherTimer timer;
        private int basetime;


        Body[] _bodies = new Body[6];
        byte[] _colorPixels;
        uint _bytesPerPixel;

        bool _colorFrameProcessed;
        bool _bodiesProcessed;
        double cntRsult = 0;
        int icntR = 0;

        WriteableBitmap _writeableBitmap;
        VisualGestureBuilderDatabase _gestureDatabase;
        VisualGestureBuilderFrameSource _gestureFrameSource;
        VisualGestureBuilderFrameReader _gestureFrameReader;

        Gesture _saluteProgress;
        Gesture _salute;
        string conn;
        MySqlConnection connect;

        string trainNameCur = "허리";
        string trainTypeCur = "허리디스크";
        string writerCur;
        string yn = "Y";

        public waist()
        {
            this.InitializeComponent();
            Loaded += MainPage_Loaded;
//            Unloaded += MainPage_Unloaded;

            this.NavigationCacheMode = NavigationCacheMode.Required;
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(10000000);
            timer.Tick += timer_Tick;
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var parameters = (user)e.Parameter;
            writerCur = parameters.uID;
        }

        void timer_Tick(object sender, object e)
        {
            basetime = basetime - 1;
            if (basetime == 0)
            {
                timer.Stop();

            }
        }
        private void done()
        {
            basetime = 5;
            //            if (Progress.Value == 1)
            //            {
            timer.Start();
            //           }

        }

        void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_multiFrameSubscription != null)
                _multiFrameSubscription.Dispose();

            _kinect.Close();
            _kinect = null;

            _multiFrameReader.Dispose();
            _multiFrameReader = null;
        }

        void MainPage_Loaded(object sender, RoutedEventArgs evt)
        {
            _kinect = KinectSensor.GetDefault();

            // We need at least body data in order to get the tracking ID for face tracking..
            _multiFrameReader = _kinect.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.Color);

            var multiFrames = Observable.FromEvent<MultiSourceFrameArrivedEventArgs>(
                ev => { _multiFrameReader.MultiSourceFrameArrived += (s, e) => ev(e); },
                ev => { _multiFrameReader.MultiSourceFrameArrived -= (s, e) => ev(e); })
                .SubscribeOn(TaskPoolScheduler.Default)
                .ObserveOn(TaskPoolScheduler.Default);

            _multiFrameSubscription = multiFrames.Subscribe(OnMultiFrame);

            // create the colorFrameDescription from the ColorFrameSource using rgba format
            var colorFrameDescription = _kinect.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);

            _writeableBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height);

            // rgba is 4 bytes per pixel
            _bytesPerPixel = colorFrameDescription.BytesPerPixel;

            // allocate space to put the pixels to be rendered
            _colorPixels = new byte[colorFrameDescription.Width * colorFrameDescription.Height * _bytesPerPixel];

            _gestureDatabase = new VisualGestureBuilderDatabase(@"Gestures/waist.gbd");
            _gestureFrameSource = new VisualGestureBuilderFrameSource(_kinect, 0);

            // Add all gestures in the database to the framesource..
            _gestureFrameSource.AddGestures(_gestureDatabase.AvailableGestures);

            foreach (var gesture in _gestureDatabase.AvailableGestures)
            {
                if (gesture.Name == "salute")
                {
                    _salute = gesture;
                }
                if (gesture.Name == "saluteProgress")
                {
                    _saluteProgress = gesture;
                }
            }

            _gestureFrameReader = _gestureFrameSource.OpenReader();
            _gestureFrameReader.IsPaused = true;
            _gestureFrameReader.FrameArrived += reader_FrameArrived;

            _kinect.Open();
        }

        void reader_FrameArrived(VisualGestureBuilderFrameReader sender, VisualGestureBuilderFrameArrivedEventArgs args)
        {

            using (var frame = args.FrameReference.AcquireFrame())
            {
                if (frame != null && frame.DiscreteGestureResults != null)
                {
                    var result = frame.DiscreteGestureResults[_salute];

                    if (result.Detected == true)
                    {
                        var progressResult = frame.ContinuousGestureResults[_saluteProgress];
                        Progress.Value = progressResult.Progress;


                        if (Progress.Value == 1)
                        {
                            done();
                        }
                        cntRsult += 0.05;
                    }
                    else
                    {
                        Progress.Value = 0.0;
                    }
                    icntR = (int)cntRsult / 3;
                    GestureText.Text = result.Detected ? "성공(동작 2초간 유지!)" : "동작을 수행하세요";
                    GestureText1.Text = icntR.ToString();
                    ConfidenceText.Text = result.Confidence.ToString();
                }
            }
        }

        /*     public void Delay(int ms)
             {
                 int time = Environment.TickCount;

                 do
                 {
                     if (Environment.TickCount - time >= ms)
                         return;
                 } while (true);
             }*/

        /*  static void Sleep(int ms)
          {
              new System.Threading.ManualResetEvent(false).WaitOne(ms);
          } */






        private void OnMultiFrame(MultiSourceFrameArrivedEventArgs args)
        {
            _bodiesProcessed = _colorFrameProcessed = false;

            using (var multiSourceFrame = args.FrameReference.AcquireFrame())
            {
                if (multiSourceFrame != null)
                {
                    if (!_gestureFrameSource.IsTrackingIdValid)
                    {
                        // For each skeleton being tracked get the ID and tell the face trackers to track that ID
                        using (var bodyFrame = multiSourceFrame.BodyFrameReference.AcquireFrame())
                        {
                            if (bodyFrame != null)
                            {
                                bodyFrame.GetAndRefreshBodyData(_bodies);
                                _bodiesProcessed = true;
                            }
                        }
                    }

                    using (var color = multiSourceFrame.ColorFrameReference.AcquireFrame())
                    {
                        if (color != null)
                        {
                            FrameDescription colorFrameDescription = color.FrameDescription;

                            if (color.RawColorImageFormat == ColorImageFormat.Bgra)
                            {
                                color.CopyRawFrameDataToArray(_colorPixels);
                            }
                            else
                            {
                                color.CopyConvertedFrameDataToArray(_colorPixels, ColorImageFormat.Bgra);
                            }

                            _colorFrameProcessed = true;
                        }
                    }
                }

                if (_bodiesProcessed == true)
                {
                    foreach (var body in _bodies)
                    {
                        if (body != null && body.IsTracked)
                        {
                            _gestureFrameSource.TrackingId = body.TrackingId;
                            _gestureFrameReader.IsPaused = false;
                            break;
                        }
                    }
                }

                if (_colorFrameProcessed == true)
                {
                    Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        _colorPixels.CopyTo(_writeableBitmap.PixelBuffer);
                        _writeableBitmap.Invalidate();
                        MyImage.Source = _writeableBitmap;
                    });
                }
            }
        }

        private void db_connection()
        {
            try
            {
                conn = "server=localhost; port=3306; database=health; user id=root; password=1234; SSLMode = None;";
                //conn = "server=127.0.0.1; port=3306; database=health; user id=root; password=123456; SSLMode = None;";  // port:3306
                connect = new MySqlConnection(conn);
                connect.Open();
            }
            catch (MySqlException e)
            {
                throw;
            }
        }

        private async void InsertNew(string name, string type, string cnt)
        {
            using (MySqlConnection connection = new MySqlConnection(conn))
            {
                db_connection();
                string sql = "Insert into train (trainName, trainType,trainCnt,writer,writedate,useYN) values (@name, @type, @cnt,@writ,now(),@yn)";
                MySqlCommand insertCmd = new MySqlCommand(sql, connect);
                insertCmd.Parameters.AddWithValue("@name", trainNameCur);
                insertCmd.Parameters.AddWithValue("@Type", trainTypeCur);
                insertCmd.Parameters.AddWithValue("@cnt", GestureText1.Text);
                insertCmd.Parameters.AddWithValue("@writ", writerCur);
                insertCmd.Parameters.AddWithValue("@yn", yn);


                if (insertCmd.ExecuteNonQuery() > 0)
                {

                    MessageDialog Correct = new MessageDialog("---------DB 저장완료---------");
                    await Correct.ShowAsync();
                    connect.Close();
                }
                else
                {
                    MessageDialog Correct = new MessageDialog("---------DB 저장실패---------");
                    await Correct.ShowAsync();
                    connect.Close();
                }
            }

        }

        private void SaveDB_Click(object sender, RoutedEventArgs e)
        {
            InsertNew(trainNameCur, trainTypeCur, GestureText1.Text);
        }

        private void back_button(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }
    }
}

