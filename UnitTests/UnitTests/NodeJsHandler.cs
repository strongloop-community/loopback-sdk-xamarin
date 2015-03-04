using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UnitTests
{
    class NodeJsHandler
    {
        public static string ServerPath = "";
        private static Thread serverThread;

        private const string NodeExecutable = "node";
        private const string SdkGeneratorPath = " ../../../../lb-xm/bin/lb-xm.js";
        private const string CompileFlag = "c";
        private const string Extension = ".dll";

        // Creates an SDK for a server
        private static void CreateSdkFor(string serverPath, string dllName)
        {
            File.Delete(dllName + Extension);
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.FileName = NodeExecutable;
            startInfo.Arguments = SdkGeneratorPath + " " + serverPath + " " + CompileFlag + " " + dllName + ".dll";
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
        }

        // Runs a server
        private static void RunServer()
        {
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.FileName = NodeExecutable;
            startInfo.Arguments = ServerPath;
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
        }

        // Runs a server in the background, creates an SDK for the server and returns it as a loaded dll
        public static Assembly StartTest(string serverPath, string testName)
        {
            // Create SDK for server and load it
            CreateSdkFor(serverPath, testName);
            if (!File.Exists(testName + Extension))
                throw new Exception();
            Assembly loadedDll = Assembly.LoadFile(Directory.GetCurrentDirectory() + "/" + testName + Extension);

            // Set correct IP in the SDK gateway
            var gateway = Activator.CreateInstance(loadedDll.GetType("LBXamarinSDK.Gateway"));
            var setUrlMethod = loadedDll.GetType("LBXamarinSDK.Gateway").GetMethod("SetServerBaseURL");
            var firstOrDefault = Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            if (firstOrDefault != null)
            {
                string adrStr = "http://" + firstOrDefault.ToString() + ":3000/api/";
                var adrUri = new Uri(adrStr);
                setUrlMethod.Invoke(gateway, new object[] { adrUri });
            }

            // Run server on its own thread
            ServerPath = serverPath;
            serverThread = new Thread(RunServer);
            serverThread.Start();
            return loadedDll;
        }

        // Kill running server
        public static void EndTest()
        {
            serverThread.Abort();
            Process.GetProcessesByName("node").First().Kill();
        }
    }
}
