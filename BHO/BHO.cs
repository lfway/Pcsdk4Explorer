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
        // ��� ����� ��� ����� � ��������� �� ����
        private void ReceiveResult(List<PXCMPoint3DF32> message4)
        {
            string qwe = message4.ToString();
           // if (flag == false)
          //      return;e
           // flag = false;
            //System.Windows.Forms.MessageBox.Show(qwe);
            //HTMLDocument document = (HTMLDocument)webBrowser.Document;
            //string div = "<div>" + qwe + "</div>";
            //document.body.insertAdjacentHTML("afterBegin", div);

            if (flag == false)
                return;
            flag = false;
            //doc.parentWindow.execScript("document.body.style.zoom='130%';");
            webBrowser.StatusBar = true;
        }
        bool flag = true;

        public void OnDocumentComplete(object pDisp, ref object URL)
        {
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

    }
}
