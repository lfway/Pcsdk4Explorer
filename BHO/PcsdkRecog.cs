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
    class PcsdkRecog
    {
        public PcsdkRecog()
        {

        }

        public delegate void delegatename(string message2);
        public PcsdkRecog(ref delegatename mydelegate)
        {
            mydelegate("qwe");
        }


        public delegate void MyNameDelegate(string message);
        public event MyNameDelegate MyNameCallback;
        private void SendResult(string message_to_send = "default")
        {
            MyNameCallback(message_to_send);
        } 

        pxcmStatus sts;
        PXCMSession session;
        PXCMBase fanalysis;

        PXCMFaceAnalysis fa;
        PXCMFaceAnalysis.Detection detection;
        PXCMFaceAnalysis.Attribute face_attribute;
        PXCMFaceAnalysis.Detection.ProfileInfo dinfo;
        PXCMFaceAnalysis.Attribute.ProfileInfo attribute_dinfo;
        PXCMFaceAnalysis.ProfileInfo pf = new PXCMFaceAnalysis.ProfileInfo();
        PXCMFaceAnalysis.Landmark landmark;
        PXCMFaceAnalysis.Landmark.ProfileInfo lpi;

        UtilMCapture capture;

        BackgroundWorker bw1 = new BackgroundWorker();

        public void Start2(Action action11)
        {
            action11();
        }

        public void Start()
        {
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
                    System.Threading.Thread.Sleep(10);

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

                    System.Threading.Thread.Sleep(10);
                    foreach (PXCMScheduler.SyncPoint s in sps) if (s != null) s.Dispose();
                    //foreach (PXCMImage i in images) if (i != null) i.Dispose();
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

        [HandleProcessCorruptedStateExceptions]
        void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            int fid;
            uint fidx = 0;
            fa.QueryFace(fidx, out fid, out timeStamp);

            PXCMFaceAnalysis.Detection.Data face_data;
            detection.QueryData(fid, out face_data);
            PXCMRectU32 q = face_data.rectangle;
            if (q.w > 0)
            {
                SendResult("Face detected");
            }
            //pictureBox1.Height = bmp.Height;
            //pictureBox1.Width = bmp.Width;
            //pictureBox1.Image = bmp;
        }
    }
}
