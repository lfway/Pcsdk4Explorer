using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BHO_HelloWorld;
namespace Test
{
    class Program
    {

        static public void ReceiveResult(string message4)
        {
            System.Console.WriteLine(message4);
        }
        static void Main(string[] args)
        {

            PcsdkRecog pc_sdk = new PcsdkRecog();
            pc_sdk.Start();
            pc_sdk.MyNameCallback += new PcsdkRecog.MyNameDelegate(ReceiveResult);
            for (; ; )
            {
                int i = 234;
            }
        }
    }
}
