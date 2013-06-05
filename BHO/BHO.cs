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

        static int stop = 0;
        private void ReceiveResult(int code, string message)
        {
            lock (this)
            {
                if (stop > 10)
                {
                    stop--;
                    webBrowser.StatusText = message;
                    return;
                }
                if (code == 0)
                {
                    webBrowser.StatusText = message;
                    return;
                }
                else
                {
                    object vZoom2 = 130;
                    object vZoom1 = 100;
                    try
                    {
                        if (code == 1)
                        {
                            webBrowser.GoBack();
                            stop = 15;
                        }
                        if (code == 2)
                        {
                            webBrowser.GoForward();
                            stop = 15;
                        }
                        if (code == 3)
                            webBrowser.ExecWB(OLECMDID.OLECMDID_OPTICAL_ZOOM, OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT, ref vZoom2, IntPtr.Zero);
                        if (code == 4)
                            webBrowser.ExecWB(OLECMDID.OLECMDID_OPTICAL_ZOOM, OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT, ref vZoom1, IntPtr.Zero);
                        if (code == 5)
                        {
                            webBrowser.GoHome();
                            stop = 15;
                        }
                        if (code == 6)
                        {
                            document = (HTMLDocument)webBrowser.Document;
                            document.body.scrollIntoView(true);
                        }
                        if (code == 7)
                        {
                            document = (HTMLDocument)webBrowser.Document;
                            document.body.scrollIntoView(false);
                        }

                    }
                    catch { }
                }
            }
        }

        bool FirstRun = true;
        public void OnDocumentComplete(object pDisp, ref object URL)
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
            FirstRun = false;
        }
        public void OnQuit()
        {
            pc_sdk.Stop();
        }
        public void OnBeforeNavigate2(object pDisp, ref object URL, ref object Flags, ref object TargetFrameName, ref object PostData, ref object Headers, ref bool Cancel)
        {        
            /*document = (HTMLDocument)webBrowser.Document;
            foreach (IHTMLInputElement tempElement in document.getElementsByTagName("INPUT"))
            {
                if (tempElement.type.ToLower() == "password")
                {
                    System.Windows.Forms.MessageBox.Show(tempElement.value);
                }

            }*/

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
                //webBrowser.BeforeNavigate2 += new DWebBrowserEvents2_BeforeNavigate2EventHandler(this.OnBeforeNavigate2);
                webBrowser.OnQuit += new DWebBrowserEvents2_OnQuitEventHandler(this.OnQuit);

            }
            else
            {
                webBrowser.DocumentComplete -= new DWebBrowserEvents2_DocumentCompleteEventHandler(this.OnDocumentComplete);
                //webBrowser.BeforeNavigate2 -= new DWebBrowserEvents2_BeforeNavigate2EventHandler(this.OnBeforeNavigate2);
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
