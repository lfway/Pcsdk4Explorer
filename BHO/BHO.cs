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
        PcsdkRecog rec_ = new PcsdkRecog();

        private delegate int MySuperDelegate(int Param);

        public void MyDelegate()
        {
            int qwe = 234234;
            document = (HTMLDocument)webBrowser.Document;
            int i = 0;
            foreach (IHTMLInputElement tempElement in document.getElementsByTagName("INPUT"))
            {
                System.Windows.Forms.MessageBox.Show(
                    tempElement.name != null ?
                        tempElement.name :
                        "it sucks, no name, try id" + ((IHTMLElement)tempElement).id
                    );
                if (++i == 2) break;
            }
            //SomeDelegateTwo sd = SquareNumber;
            //s
        }

        private void ReceiveResult(string message4)
        {
            System.Windows.Forms.MessageBox.Show(message4);
        } 

        public void OnDocumentComplete(object pDisp, ref object URL)
        {
            document = (HTMLDocument)webBrowser.Document;
            int i = 0;
            /*foreach (IHTMLInputElement tempElement in document.getElementsByTagName("INPUT"))
            {
                System.Windows.Forms.MessageBox.Show(
                    tempElement.name != null ? 
                        tempElement.name : 
                        "it sucks, no name, try id" + ((IHTMLElement)tempElement).id
                    );
                if (++i == 5) break;
            }*/
            //rec_.Start();
            //rec_.Start2(delegate() { MyDelegate(); });
            //rec_.MyNameCallback += new PcsdkRecog.MyNameDelegate(ReceiveResult);

            //PcsdkRecog pc_sdk = new PcsdkRecog();
            rec_.Start();
            rec_.MyNameCallback += new PcsdkRecog.MyNameDelegate(ReceiveResult);
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
