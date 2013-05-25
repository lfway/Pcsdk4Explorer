using System;
using System.Collections.Generic;
using System.Text;
using SHDocVw;
using mshtml;
using System.IO;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace Pcsdk4Explorer
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
        //mshtml.HTMLDocument doc;
        int counter_ = 0;
        
        //bool allow_receive_result = true;

        private void ReceiveResult(int code)
        {
            /*bool allowed = false;
            lock (this)
            {
                counter_++;
                allowed = allow_receive_result;
            }
            if (allowed == false)
                return;*/

            webBrowser.StatusText = counter_.ToString();

            if (code > 0)
            {
                object vZoom2 = 130;
                object vZoom1 = 100;

                try
                {
                    if (code == 1)
                        webBrowser.GoBack();
                    if (code == 2)
                        webBrowser.GoForward();
                    if (code == 3)
                        webBrowser.ExecWB(OLECMDID.OLECMDID_OPTICAL_ZOOM, OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT, ref vZoom2, IntPtr.Zero);
                    if (code == 4)
                        webBrowser.ExecWB(OLECMDID.OLECMDID_OPTICAL_ZOOM, OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT, ref vZoom1, IntPtr.Zero);
                  //  if(code == 3 || code == 4)
                       // lock (this)
                        //{
                            //allow_receive_result = false;
                        //}

                }
                catch
                {
                   // lock (this)
                   // {
                   //     allow_receive_result = true;
                   // }
                }
            }
        }

        bool FirstRun = true;
        public void OnDocumentComplete(object pDisp, ref object URL)
        {
            lock (this)
            {
                if (FirstRun == true)
                {
                    webBrowser.Navigate("http://lenta.ru");
                    webBrowser.StatusBar = true;
                    pc_sdk.Start();
                    pc_sdk.MyNameCallback += new PcsdkRecog.MyNameDelegate(ReceiveResult);
                }
                else
                {

                }
                //allow_receive_result = true;
                //System.Threading.Thread.Sleep(10);
                FirstRun = false;
            }
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
                webBrowser = (WebBrowser)site;
                webBrowser.DocumentComplete += new DWebBrowserEvents2_DocumentCompleteEventHandler(this.OnDocumentComplete);
                webBrowser.BeforeNavigate2 += new DWebBrowserEvents2_BeforeNavigate2EventHandler(this.OnBeforeNavigate2);
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

    }
}
