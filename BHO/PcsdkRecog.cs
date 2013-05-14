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
//using System.Drawing.Imaging;

namespace BHO_HelloWorld
{
    public class PcsdkRecog
    {
        // делегат
        public delegate void MyNameDelegate(List<PXCMPoint3DF32> message);
        // событие
        public event MyNameDelegate MyNameCallback;
        // отправка детектированного события
        //private void SendResult(string message_to_send = "default")
        private void SendResult(List<PXCMPoint3DF32> message_to_send)
        {
            MyNameCallback(message_to_send);
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
            sts = capture.LocateStreams(ref pf.inputs);
            if (sts < pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                return;
            }

            detection = (PXCMFaceAnalysis.Detection)fa.DynamicCast(PXCMFaceAnalysis.Detection.CUID);
            face_attribute = (PXCMFaceAnalysis.Attribute)fa.DynamicCast(PXCMFaceAnalysis.Attribute.CUID);

            //background worker
            
            bw1.RunWorkerAsync();
             
        }
        
        PXCMImage[] images = new PXCMImage[PXCMCapture.VideoStream.STREAM_LIMIT];
        PXCMScheduler.SyncPoint[] sps = new PXCMScheduler.SyncPoint[2];
        bool device_lost = false;
        Bitmap bmp;
        ulong timeStamp;

        [HandleProcessCorruptedStateExceptions]
        void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            pf.iftracking = true;

            for (int nframes = 0; ; )
            {
                try
                {
                    System.Threading.Thread.Sleep(20);
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

                    System.Threading.Thread.Sleep(30);
                    foreach (PXCMScheduler.SyncPoint s in sps) if (s != null) s.Dispose();
                }
                catch
                {
                    MessageBox.Show("Frame capturing Error!");
                    return;
                }
            }
            fa.Dispose();
            capture.Dispose();
            session.Dispose();
        }

        //GestureDetector mGestureDetector = new GestureDetector();
        bool flag = false;
        [HandleProcessCorruptedStateExceptions]
        void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            int fid;
            uint fidx = 0;
            fa.QueryFace(fidx, out fid, out timeStamp);

            //Get face landmarks (eye, mouth, nose position)
            PXCMFaceAnalysis.Landmark landmark = (PXCMFaceAnalysis.Landmark)fa.DynamicCast(PXCMFaceAnalysis.Landmark.CUID);
            landmark.QueryProfile(1, out lpi);
            landmark.SetProfile(ref lpi);
            PXCMFaceAnalysis.Landmark.LandmarkData[] landmark_data = new PXCMFaceAnalysis.Landmark.LandmarkData[7];
            sts = landmark.QueryLandmarkData(fid, PXCMFaceAnalysis.Landmark.Label.LABEL_7POINTS, landmark_data);

            if (sts != pxcmStatus.PXCM_STATUS_ITEM_UNAVAILABLE )
            {


                
                //Do something with the landmarks
                List<PXCMPoint3DF32> face_elements = new List<PXCMPoint3DF32>();
                PXCMPoint3DF32 eye_left_outer = landmark_data[0].position;
                PXCMPoint3DF32 eye_left_inner = landmark_data[1].position;
                PXCMPoint3DF32 eye_right_outer = landmark_data[2].position;
                PXCMPoint3DF32 eye_right_inner = landmark_data[3].position;
                PXCMPoint3DF32 eye_mouth_left = landmark_data[4].position;
                PXCMPoint3DF32 eye_mouth_right = landmark_data[5].position;
                face_elements.Add(eye_left_outer);
                face_elements.Add(eye_left_inner);
                face_elements.Add(eye_right_outer);
                face_elements.Add(eye_right_inner);
                face_elements.Add(eye_mouth_left);
                face_elements.Add(eye_mouth_right);


             //   PXCMPoint3DF32 eye_left = GetCenter(eye_left_outer, eye_left_inner);
              //  PXCMPoint3DF32 eye_right = GetCenter(eye_right_outer, eye_right_inner);
              //  PXCMPoint3DF32 mouth = GetCenter(eye_mouth_left, eye_mouth_right);

              //  FacePosition f_pos = new FacePosition(eye_left, eye_right, mouth);
               // GestureDetector.instance.AddPosition(f_pos);

                //mGestureDetector. .AddPosition(f_pos);

                //int result_gesture = 0;
                //GestureDetector.instance.Process(out result_gesture);
                //int r = mGestureDetector.GetResult();
                
                //if(r > 0)
                //    SendResult("Face detected");

                //if (flag == false)
                //{

                SendResult(face_elements);
                    //SendResult(r.ToString());
                    //flag = true;
                //}
            }
        }


        protected PXCMPoint3DF32 GetCenter(PXCMPoint3DF32 p1, PXCMPoint3DF32 p2)
        {
	        int x2 = Math.Max((int)p1.x, (int)p2.x);
	        int x1 = Math.Min((int)p1.x, (int)p2.x);
	        int y2 = Math.Max((int)p1.y, (int)p2.y);
	        int y1 = Math.Min((int)p1.y, (int)p2.y);

	        PXCMPoint3DF32 center = new PXCMPoint3DF32();
	        center.x	= x1 + (x2 - x1)/2;
	        center.y	= y1 + (y2 - y1)/2;
	        return center;
        }
    }
}
