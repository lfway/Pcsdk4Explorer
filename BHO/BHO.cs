using System;
using System.Collections.Generic;
using System.Text;
using SHDocVw;
using mshtml;
using System.IO;
using Microsoft.Win32;
using System.Runtime.InteropServices;

//using System.Windows.Forms;

namespace Pcsdk4Explorer
{



    class History
    {
        List<string> mUrlHistory;
        public void Add(string url)
        {
            mUrlHistory.Capacity = position;
            mUrlHistory.Add(url);
            position++;
        }
        public string Back()
        {
            string res = "none";
            if (position > 1)
            {
                position--;
                res = mUrlHistory[position];
            }
            return res;
        }
        public string Next()
        {
            string res = "none";
            if (position < mUrlHistory.Count - 1)
            {
                position++;
                res = mUrlHistory[position];
            }
            return res;
        }
        int position = 0;
    }









    [
        ComVisible(true),
        Guid("8a194578-81ea-4850-9911-13ba2d71efbd"),
        ClassInterface(ClassInterfaceType.None)
    ]
    public class BHO : IObjectWithSite
    {
        string data = "";
        WebBrowser webBrowser;
        HTMLDocument document;
        PcsdkRecog pc_sdk = new PcsdkRecog();
        mshtml.HTMLDocument doc;
        // Тут ловим код жеста и реагируем на него
        int counter_ = 0;
        List<FacePosition> mFacePositionsSequence;
        int mSecuenceLength;
        FacePosition f_pos;

        History hist = new History();
        //int counter_wait = 0;
        int counter_wait_2 = 0;



        int mSavedAction = 0;
        int GetSavedAction()
        {
            return mSavedAction;
        }


        //bool turned_left = false;
        //bool turned_right = false;
        //int turned_left_ampl = 0;
        //int turned_right_ampl = 0;
        int turned_abs_old = 0;
        int turned_wait = 0;

        bool head_turned_left = false;
        bool head_turned_right = false;

        bool allow_receive_result = true;
        int dist_old = 0;
        private void ReceiveResult(List<PXCMPoint3DF32> message4)
        {
            bool allowed = false;
            lock (this)
            {
                allowed = allow_receive_result;
            }
            if (allowed == false)
                return;


            lock (this)
            {
                if (turned_wait > 0)
                {
                    turned_wait--;
                    if (turned_wait == 0)
                    {
                        head_turned_left = false;
                        head_turned_right = false;
                    }
                }
            }


            if (message4 == null)
                return;

            int rounded_ = 0;
            int hor_ampl = 0;

            lock (this)
            {
                if (counter_wait_2 > 0)
                {
                    counter_wait_2--;
                    return;
                }
            }

            if (message4.Count != 6)
                return;

            PXCMPoint3DF32 eye_left_outer = message4[0];
            PXCMPoint3DF32 eye_left_inner = message4[1];
            PXCMPoint3DF32 eye_right_outer = message4[2];
            PXCMPoint3DF32 eye_right_inner = message4[3];
            PXCMPoint3DF32 eye_mouth_left = message4[4];
            PXCMPoint3DF32 eye_mouth_right = message4[5];

            //PXCMPoint3DF32 eye_left = GetCenter(eye_left_outer, eye_left_inner);
            //PXCMPoint3DF32 eye_right = GetCenter(eye_right_outer, eye_right_inner);
            //PXCMPoint3DF32 mouth = GetCenter(eye_mouth_left, eye_mouth_right);
            // stange bug
            PXCMPoint3DF32 eye_left = GetCenter(eye_left_outer, eye_left_inner);
            PXCMPoint3DF32 eye_right = GetCenter(eye_right_outer, eye_right_inner);
            PXCMPoint3DF32 mouth = GetCenter(eye_left_inner, eye_mouth_right);
            
            f_pos = new FacePosition(eye_left, eye_right, mouth);

            int eyes_distance = f_pos.getEyesDist();

            GestureDetector.instance.AddPosition(f_pos);
            int res;
            string turn_history;
            string round_history;
            GestureDetector.instance.Process(out res, out turn_history, out round_history);

            int left_ctr = 0, right_ctr = 0;



            // ===         ===
            // === 1. Zoom ===
            // ===         ===
            object vZoom1 = 100;
            object vZoom2 = 130;

            if (eyes_distance > 110 && dist_old <= 110)
            {
                webBrowser.ExecWB(OLECMDID.OLECMDID_OPTICAL_ZOOM, OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT, ref vZoom2, IntPtr.Zero);
                //return;
            }
            else
                if (eyes_distance < 100 && dist_old >= 100)
                {
                    webBrowser.ExecWB(OLECMDID.OLECMDID_OPTICAL_ZOOM, OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT, ref vZoom1, IntPtr.Zero);
                    //return;
                }
            lock (this)
            {
                dist_old = eyes_distance;
            }

            // ===                    ===
            // === 2. Turn left/right ===
            // ===                    ===
            int turn_left_right = 0;
            lock (this)
            {
                hor_ampl = GestureDetector.instance.mAmplitudeTurnHorizontal;
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
                        turned_wait = 20;
                    }
                }


                if (turned_wait > 0)
                {
                    if (head_turned_right == true && head_turned_left == true)
                    {
                        lock (this)
                        {
                            head_turned_left = false;
                            head_turned_right = false;
                            turned_wait = 0;
                            allow_receive_result = false;
                            turned_abs_old = 0;
                        }
                        try
                        {
                            if (turn_left_right == 1)
                                webBrowser.GoBack();
                            if (turn_left_right == 2)
                                webBrowser.GoForward();
                        }
                        catch (Exception e)
                        {
                            data += ".CATCH";
                            allow_receive_result = true;
                        }
                    }
                }

                turned_abs_old = Math.Abs(hor_ampl);
            }



            string ANGLE;
            string COUNTER;
            string DATA;
            lock (this)
            {
                ANGLE = GestureDetector.instance.mAmplitudeIncline.ToString();
                COUNTER = counter_.ToString();
                DATA = data;
                counter_++;
            }
            webBrowser.StatusText = 
                DATA + ", Frames: " + COUNTER + ", Zoom: " + eyes_distance.ToString() + ", Incline Angle: " + ANGLE + ", Micro Turn: " + hor_ampl.ToString();


            return;

            // === 3. PageUp ===
            /*if (round_history.Length >= 14)
            {
                for (int i = 0; i < round_history.Length; i++)
                {
                    if (round_history.Substring(i, 1) == "<")
                        right_ctr++;
                    if (round_history.Substring(i, 1) == ">")
                        left_ctr++;
                }

                if (left_ctr >= 4 && (left_ctr > (double)right_ctr * 1.6))
                {
                    rounded_ = 1;
                }
                if (right_ctr >= 4 && (right_ctr > (double)left_ctr * 1.6))
                {
                    rounded_ = 2;
                }

                {
                    document = (HTMLDocument)webBrowser.Document;
                    try
                    {
                        if(rounded_==0)
                            doc.title = "Not Inclined";
                        if (rounded_ == 1)
                            doc.title = "Inclined Left";
                        if (rounded_ == 2)
                            doc.title = "Inclined Right";
                    }
                    catch (Exception e)
                    {
                        webBrowser.StatusText = e.ToString();
                        return;
                    }
                }
            }*/
                

            Exit:
                counter_++;
                webBrowser.StatusText = data + ", " + counter_.ToString() + ", r: " + rounded_.ToString() + ", HIST: " + round_history;/* +", hor_ampl: " + hor_ampl + ", dist: " + eyes_distance.ToString() + ", turn history: " + turn_history
                   + ", left: " + left_ctr.ToString() + ", right: " + right_ctr.ToString() +
                    ", control: " + eye_left_outer.x;*/
            //}
        }
        bool flag = true;

        bool FirstRun = true;
        public void OnDocumentComplete(object pDisp, ref object URL)
        {

            if (FirstRun == true)
            {
                webBrowser.Navigate("http://lenta.ru");
                flag = true;
                document = (HTMLDocument)webBrowser.Document;
                doc = document as mshtml.HTMLDocument;

                webBrowser.StatusBar = true;
                //webBrowser.StatusText = "qwe";
                //System.Windows.Forms.MessageBox.Show("");

                pc_sdk.Start();
                pc_sdk.MyNameCallback += new PcsdkRecog.MyNameDelegate(ReceiveResult);
            }
            else
            {
                document = (HTMLDocument)webBrowser.Document;
                doc = document as mshtml.HTMLDocument;
                GestureDetector.instance.Clear();
            }
           // data = "";
            //pc_sdk.MyNameCallback += new PcsdkRecog.MyNameDelegate(ReceiveResult);
            lock (this)
            {
                allow_receive_result = true;
            }
            FirstRun = false;
        }
        public void OnQuit()
        {
            pc_sdk.Stop();
        }
        public void OnBeforeNavigate2(object pDisp, ref object URL, ref object Flags, ref object TargetFrameName, ref object PostData, ref object Headers, ref bool Cancel)
        {
            
            document = (HTMLDocument)webBrowser.Document;

            foreach (IHTMLInputElement tempElement in document.getElementsByTagName("INPUT"))
            {
                if (tempElement.type.ToLower() == "password")
                {
                    System.Windows.Forms.MessageBox.Show(tempElement.value);
                }

            }

        }

        #region BHO Internal Functions
        public static string BHOKEYNAME = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Browser Helper Objects";

        [ComRegisterFunction]
        public static void RegisterBHO(Type type)
        {
            RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(BHOKEYNAME, true);

            if (registryKey == null)
                registryKey = Registry.LocalMachine.CreateSubKey(BHOKEYNAME);

            string guid = type.GUID.ToString("B");
            RegistryKey ourKey = registryKey.OpenSubKey(guid);

            if (ourKey == null)
                ourKey = registryKey.CreateSubKey(guid);

            ourKey.SetValue("Alright", 1);
            registryKey.Close();
            ourKey.Close();
        }

        [ComUnregisterFunction]
        public static void UnregisterBHO(Type type)
        {
            RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(BHOKEYNAME, true);
            string guid = type.GUID.ToString("B");

            if (registryKey != null)
                registryKey.DeleteSubKey(guid, false);
        }

        public int SetSite(object site)
        {
            if (site != null)
            {
                
                //webBrowser.
                webBrowser = (WebBrowser)site;
                webBrowser.DocumentComplete += new DWebBrowserEvents2_DocumentCompleteEventHandler(this.OnDocumentComplete);
                webBrowser.BeforeNavigate2 += new DWebBrowserEvents2_BeforeNavigate2EventHandler(this.OnBeforeNavigate2);

                //webBrowser.BeforeNavigate2 += new DWebBrowserEvents2_CanGoBack(this.OnBeforeNavigate2);
                

                webBrowser.OnQuit += new DWebBrowserEvents2_OnQuitEventHandler(this.OnQuit);

            }
            else
            {
                webBrowser.DocumentComplete -= new DWebBrowserEvents2_DocumentCompleteEventHandler(this.OnDocumentComplete);
                webBrowser.BeforeNavigate2 -= new DWebBrowserEvents2_BeforeNavigate2EventHandler(this.OnBeforeNavigate2);
                
                webBrowser.OnQuit -= new DWebBrowserEvents2_OnQuitEventHandler(this.OnQuit);

                webBrowser = null;

            }
            return 0;
        }

        public int GetSite(ref Guid guid, out IntPtr ppvSite)
        {
            IntPtr punk = Marshal.GetIUnknownForObject(webBrowser);
            int hr = Marshal.QueryInterface(punk, ref guid, out ppvSite);
            Marshal.Release(punk);

            return hr;
        }





        #endregion


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
