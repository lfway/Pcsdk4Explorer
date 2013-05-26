using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO;
//using ZedGraph;
//using System.Drawing.Printing;

using System.Diagnostics;
using System.Runtime.ExceptionServices;

using System.Threading;
//using System.Drawing.Imaging;

namespace Pcsdk4Explorer
{
    public class PcsdkRecog
    {
        // делегат
        //public delegate void MyNameDelegate(List<PXCMPoint3DF32> message);
        public delegate void MyNameDelegate(int code, string message);
        // событие
        public event MyNameDelegate MyNameCallback;
        // отправка детектированного события
        //private void SendResult(string message_to_send = "default")
        // private void SendResult(List<PXCMPoint3DF32> message_to_send)
        //{
        //     MyNameCallback(message_to_send);
        //}
        private void SendResult(int message_to_send, string message2 = "")
        {
            MyNameCallback(message_to_send, message2);
        }
        pxcmStatus sts;
        PXCMSession session;
        PXCMBase fanalysis;

        PXCMFaceAnalysis fa;
        PXCMFaceAnalysis.Detection detection;
        PXCMFaceAnalysis.Attribute face_attribute;
        //PXCMFaceAnalysis.Detection.ProfileInfo dinfo;
        //PXCMFaceAnalysis.Attribute.ProfileInfo attribute_dinfo;
        PXCMFaceAnalysis.ProfileInfo pf = new PXCMFaceAnalysis.ProfileInfo();
        //PXCMFaceAnalysis.Landmark landmark;
        PXCMFaceAnalysis.Landmark.ProfileInfo lpi;

        UtilMCapture capture;

        BackgroundWorker bw1 = new BackgroundWorker();

        bool started = false;
        public void Pause(bool pause = true)
        {
            if (pause == true)
            {
                bw1.DoWork -= new DoWorkEventHandler(bw_DoWork);
                bw1.ProgressChanged -= new ProgressChangedEventHandler(bw_ProgressChanged);
            }
            else
            {
                bw1.DoWork += new DoWorkEventHandler(bw_DoWork);
                bw1.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
            }
        }
        public void Stop()
        {
            bw1.DoWork -= new DoWorkEventHandler(bw_DoWork);
            bw1.ProgressChanged -= new ProgressChangedEventHandler(bw_ProgressChanged);

            detection.Dispose();
            capture.Dispose();
            session.Dispose();
        }
        public void Start()
        {
            if (started == true)
            {
                return;
            }
            started = true;
            bw1.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw1.ProgressChanged += new ProgressChangedEventHandler(bw_ProgressChanged);
            bw1.WorkerReportsProgress = true;
            bw1.WorkerSupportsCancellation = true;

            // create instance
            sts = PXCMSession.CreateInstance(out session);
            if (sts < pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                return;
            }
            //create face analyser //add implimentation for this session
            sts = session.CreateImpl(PXCMFaceAnalysis.CUID, out fanalysis);
            if (sts < pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                session.Dispose();
                return;
            }
            fa = (PXCMFaceAnalysis)fanalysis.DynamicCast(PXCMFaceAnalysis.CUID);
            fa.QueryProfile(0, out pf);
            // capturing
            capture = new UtilMCapture(session);
            // set resolution
            PXCMSizeU32 size = new PXCMSizeU32();
            size.height = 240 * 2;
            size.width = 320 * 2;
            capture.SetFilter(PXCMImage.ImageType.IMAGE_TYPE_COLOR, ref size);
            sts = capture.LocateStreams(ref pf.inputs);
            if (sts < pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                return;
            }

            detection = (PXCMFaceAnalysis.Detection)fa.DynamicCast(PXCMFaceAnalysis.Detection.CUID);
            face_attribute = (PXCMFaceAnalysis.Attribute)fa.DynamicCast(PXCMFaceAnalysis.Attribute.CUID);

            bw1.RunWorkerAsync();
        }

        PXCMImage[] images = new PXCMImage[PXCMCapture.VideoStream.STREAM_LIMIT];
        PXCMScheduler.SyncPoint[] sps = new PXCMScheduler.SyncPoint[2];
        bool device_lost = false;
        Bitmap bmp;
        ulong timeStamp;

        int turned_abs_old = 0;
        int turned_vert_abs_old = 0;
        int mWaitBackTurn = 0;
        int mWaitBackVertical = 0;

        bool head_turned_left = false;
        bool head_turned_right = false;
        bool head_turned_up = false;
        bool head_turned_down = false;
        
        bool mTurnToIncline = false;

        int dist_old = 0;

        Thread thread;

        [HandleProcessCorruptedStateExceptions]
        void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            pf.iftracking = true;

            for (int nframes = 0; ; )
            {
                try
                {
                    GC.Collect();
                    System.Threading.Thread.Sleep(5);
                    // Read Image
                    sts = capture.ReadStreamAsync(images, out sps[0]);
                    if (sts < pxcmStatus.PXCM_STATUS_NO_ERROR)
                    {
                        if (sts == pxcmStatus.PXCM_STATUS_DEVICE_LOST)
                        {
                            device_lost = true; nframes--;
                            continue;
                        }
                        break;
                    }
                    if (device_lost)
                    {
                        device_lost = false;
                    }
                    ///////////////////////////// Face Part///////////////

                    sts = fa.ProcessImageAsync(new PXCMImage[] { images[0] }, out sps[1]);
                    PXCMScheduler.SyncPoint.SynchronizeEx(sps);

                    bmp = new Bitmap((int)images[0].imageInfo.width, (int)images[0].imageInfo.height);
                    images[0].QueryBitmap(session, out bmp);

                    /////////////////////////////////////////////////////////
                    bw1.ReportProgress(0);

                    System.Threading.Thread.Sleep(45);
                    foreach (PXCMScheduler.SyncPoint s in sps) if (s != null) s.Dispose();
                }
                catch
                {
                    //MessageBox.Show("Frame capturing Error!");
                    return;
                }
            }

            fa.Dispose();
            capture.Dispose();
            session.Dispose();
            //GC.Collect();
        }

        GestureDetector mGestureDetector = new GestureDetector();

        [HandleProcessCorruptedStateExceptions]
        void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //return;
            //GC.Collect();
            int fid;
            uint fidx = 0;
            fa.QueryFace(fidx, out fid, out timeStamp);

            PXCMFaceAnalysis.Landmark.LandmarkData[] landmark_data = new PXCMFaceAnalysis.Landmark.LandmarkData[7];
            try
            {
                //Get face landmarks (eye, mouth, nose position)
                PXCMFaceAnalysis.Landmark landmark = (PXCMFaceAnalysis.Landmark)fa.DynamicCast(PXCMFaceAnalysis.Landmark.CUID);
                landmark.QueryProfile(1, out lpi);
                landmark.SetProfile(ref lpi);
                //PXCMFaceAnalysis.Landmark.LandmarkData[] landmark_data = new PXCMFaceAnalysis.Landmark.LandmarkData[7];
                sts = landmark.QueryLandmarkData(fid, PXCMFaceAnalysis.Landmark.Label.LABEL_7POINTS, landmark_data);
            }
            catch
            {
                //SendResult(0);
                return;
            }
            if (sts != pxcmStatus.PXCM_STATUS_ITEM_UNAVAILABLE)
            {
                //Do something with the landmarks
                List<PXCMPoint3DF32> face_elements = new List<PXCMPoint3DF32>();
                PXCMPoint3DF32 eye_left_outer = landmark_data[0].position;
                PXCMPoint3DF32 eye_left_inner = landmark_data[1].position;
                PXCMPoint3DF32 eye_right_outer = landmark_data[2].position;
                PXCMPoint3DF32 eye_right_inner = landmark_data[3].position;
                PXCMPoint3DF32 eye_mouth_left = landmark_data[4].position;
                PXCMPoint3DF32 eye_mouth_right = landmark_data[5].position;

                PXCMPoint3DF32 eye_left = GetCenter(eye_left_outer, eye_left_inner);
                PXCMPoint3DF32 eye_right = GetCenter(eye_right_outer, eye_right_inner);
                PXCMPoint3DF32 mouth = GetCenter(eye_left_inner, eye_mouth_right);
                FacePosition f_pos = new FacePosition(eye_left, eye_right, mouth);

                int eyes_distance = f_pos.getEyesDist();
                int res;
                int hor_ampl = 0;
                int vert_ampl = 0;
                int angle_2 = 0;
                lock (this)
                {
                    mGestureDetector.AddPosition(f_pos);
                    mGestureDetector.Process(out res);
                    angle_2 = mGestureDetector.mAmplitudeIncline;
                    hor_ampl = mGestureDetector.mAmplitudeTurnHorizontal;
                    vert_ampl = mGestureDetector.mAmplitudeVertical;
                    
                }

                //
                // Detect zoom
                //

                int zoomed = 0;
                if (eyes_distance > 110 && dist_old <= 110)
                {
                    zoomed = 3;
                }
                else
                    if (eyes_distance < 100 && dist_old >= 100)
                    {
                        zoomed = 4;
                    }
                dist_old = eyes_distance;
                if (zoomed == 3 || zoomed == 4)
                {
                    try
                    {
                        //thread = new Thread( SendResult);
                        SendResult(zoomed);
                    }
                    finally
                    {

                    }
                    goto Exit;
                    //return;
                }
                



                //
                // Detect turn
                //
                if (mWaitBackTurn > 0) mWaitBackTurn--;
                if (mWaitBackVertical > 0) mWaitBackVertical--;
                int turn_left_right = 0;
                lock (this)
                {
                    if (Math.Abs(hor_ampl) > 12)
                    {
                        if (Math.Abs(hor_ampl) < turned_abs_old)
                        {
                            if (hor_ampl < 0)
                            {
                                head_turned_right = true;
                                turn_left_right = 1;
                                turned_abs_old = 0;
                            }
                            if (hor_ampl > 0)
                            {
                                head_turned_left = true;
                                turn_left_right = 2;
                                turned_abs_old = 0;

                            }
                            if (angle_2 > 8)
                                mTurnToIncline = true;
                            else
                                mTurnToIncline = false;
                            mWaitBackTurn = 20;
                            mWaitBackVertical = 0;
                        }
                    }
                }
                turned_abs_old = Math.Abs(hor_ampl);
                if (mWaitBackTurn > 0)
                {
                    if (head_turned_right == true && head_turned_left == true)
                    {
                        lock (this)
                        {
                            head_turned_left = false;
                            head_turned_right = false;
                            mWaitBackTurn = 0;
                            turned_abs_old = 0;
                        }
                        try
                        {
                            if (turn_left_right == 1 && mTurnToIncline == false)
                            {
                                SendResult(1);
                            }
                            if (turn_left_right == 2 && mTurnToIncline == false)
                            {
                                SendResult(2);
                            }
                            if (turn_left_right > 0 && mTurnToIncline == true)
                            {
                                SendResult(5);
                            }
                        }
                        finally
                        {

                        }
                    }
                   // return;
                    goto Exit;
                }


                //
                // Detect turn up / down
                //

                int turn_up_down = 0;
                lock (this)
                {
                    if (Math.Abs(vert_ampl) > 20)
                    {
                        if (Math.Abs(vert_ampl) < turned_vert_abs_old)
                        {
                            if (vert_ampl < 0)
                            {
                                head_turned_up = true;
                                turn_up_down = 1;
                                turned_vert_abs_old = 0;
                            }
                            if (vert_ampl > 0)
                            {
                                head_turned_down = true;
                                turn_up_down = 2;
                                turned_vert_abs_old = 0;

                            }
                            mWaitBackVertical = 20;
                            mWaitBackTurn = 0;
                        }
                    }
                }
                turned_vert_abs_old = Math.Abs(vert_ampl);
                if (mWaitBackVertical > 0)
                {
                    if (head_turned_up == true && head_turned_down == true)
                    {
                        lock (this)
                        {
                            head_turned_up = false;
                            head_turned_down = false;
                            mWaitBackTurn = 0;
                            turned_vert_abs_old = 0;
                        }
                        try
                        {
                            if (turn_up_down == 1)
                            {
                                SendResult(6);
                            }
                            if (turn_up_down == 2)
                            {
                                SendResult(7);
                            }
                        }
                        finally{}
                    }
                }
                //return;
            Exit:
                string message_ = "x: " + hor_ampl.ToString() + ", y: " + vert_ampl.ToString() + ", dist: " + eyes_distance.ToString() + ", angle: " + angle_2.ToString();
                SendResult(0, message_);
            }
        }


        protected PXCMPoint3DF32 GetCenter(PXCMPoint3DF32 p1, PXCMPoint3DF32 p2)
        {
            int x2 = Math.Max((int)p1.x, (int)p2.x);
            int x1 = Math.Min((int)p1.x, (int)p2.x);
            int y2 = Math.Max((int)p1.y, (int)p2.y);
            int y1 = Math.Min((int)p1.y, (int)p2.y);

            PXCMPoint3DF32 center = new PXCMPoint3DF32();
            center.x = x1 + (x2 - x1) / 2;
            center.y = y1 + (y2 - y1) / 2;
            return center;
        }
    }
}
