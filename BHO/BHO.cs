using System;
using System.Collections.Generic;
using System.Text;
using SHDocVw;
using mshtml;
using System.IO;
using Microsoft.Win32;
using System.Runtime.InteropServices;


namespace BHO_HelloWorld
{

    [
        ComVisible(true),
        Guid("8a194578-81ea-4850-9911-13ba2d71efbd"),
        ClassInterface(ClassInterfaceType.None)
    ]
    public class BHO : IObjectWithSite
    {
        WebBrowser webBrowser;
        HTMLDocument document;
        PcsdkRecog pc_sdk = new PcsdkRecog();
        mshtml.HTMLDocument doc;
        // Тут ловим код жеста и реагируем на него
        int counter_ = 0;
        List<FacePosition> mFacePositionsSequence;
        int mSecuenceLength;
        FacePosition f_pos;
        private void ReceiveResult(List<PXCMPoint3DF32> message4)
        {
            if (message4.Count != 6)
                return;

            PXCMPoint3DF32 eye_left_outer = message4[0];
            PXCMPoint3DF32 eye_left_inner = message4[1];
            PXCMPoint3DF32 eye_right_outer = message4[2];
            PXCMPoint3DF32 eye_right_inner = message4[3];
            PXCMPoint3DF32 eye_mouth_left = message4[4];
            PXCMPoint3DF32 eye_mouth_right = message4[5];

            PXCMPoint3DF32 eye_left = GetCenter(eye_left_outer, eye_left_inner);
            PXCMPoint3DF32 eye_right = GetCenter(eye_right_outer, eye_right_inner);
            PXCMPoint3DF32 mouth = GetCenter(eye_mouth_left, eye_mouth_right);

            f_pos = new FacePosition(eye_left, eye_right, mouth);



            
            GestureDetector.instance.AddPosition(f_pos);
            int res;
            string res2;
            GestureDetector.instance.Process(out res, out res2);


            if (flag == false)
                return;
            if (res==100)
            flag = false;

            webBrowser.StatusBar = true;
            counter_++;
            webBrowser.StatusText = counter_.ToString() +",res: " + res.ToString() + ", res2: " + res2;
        }
        bool flag = true;


        public void OnDocumentComplete(object pDisp, ref object URL)
        {
            flag = true;
            document = (HTMLDocument)webBrowser.Document;
            doc = document as mshtml.HTMLDocument;

            webBrowser.StatusBar = false;
            //webBrowser.StatusText = "qwe";
            //System.Windows.Forms.MessageBox.Show("");
            pc_sdk.Start();
            pc_sdk.MyNameCallback += new PcsdkRecog.MyNameDelegate(ReceiveResult);
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
                
            }
            else
            {
                webBrowser.DocumentComplete -= new DWebBrowserEvents2_DocumentCompleteEventHandler(this.OnDocumentComplete);
                webBrowser.BeforeNavigate2 -= new DWebBrowserEvents2_BeforeNavigate2EventHandler(this.OnBeforeNavigate2);
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
