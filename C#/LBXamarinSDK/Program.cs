using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CSharp;
using System.IO;
using LBXamarinSDK;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

/**
 * Created at Perfected Tech, 2015
 * Perfectedtech.com
 * 
 * Comments: yehuda@perfectedtech.com
 * Complaints: chayim@perfectedtech.com
 */
namespace LBXamarinSDKGenerator
{
    public class Startup
    {
        /**
        * Debug Tool
        */
        public void WriteDefinitionsDebug(string jsonModel)
        {
            string outputPath = "D:" + ("\\lb-xmServerDefinition" + DateTime.Now + ".txt").Replace(" ", "-").Replace(":", ".");
            Console.WriteLine(">> Writing server Json definition to " + outputPath);
            System.IO.StreamWriter file = new System.IO.StreamWriter(outputPath);
            file.Write(jsonModel);
            file.Close();
        }

        /**
         * Compiles the code.
         */
        public async Task<bool> Compile(string code, string outputPath, string currentPath)
        {
            // Compile dynamic code
            CompilerParameters compilerParams = new CompilerParameters() { OutputAssembly = outputPath };
            compilerParams.ReferencedAssemblies.Add("System.dll");
            compilerParams.ReferencedAssemblies.Add("System.Core.dll");
            compilerParams.ReferencedAssemblies.Add("System.Net.Http.dll");
            compilerParams.ReferencedAssemblies.Add(currentPath + "/RestSharp.Portable.dll");
            compilerParams.ReferencedAssemblies.Add(currentPath + "/Newtonsoft.Json.dll");
            compilerParams.WarningLevel = 1;
            compilerParams.TreatWarningsAsErrors = false;
            var compiler = new CSharpCodeProvider();
            var result = compiler.CompileAssemblyFromSource(compilerParams, code);

            // Print messages 
            if (result.Errors.Count == 0)
            {
                Console.WriteLine(">> Loopback Xamarin SDK DLL: " + outputPath + " compiled successfully.");
                return true;
            }
            else
            {
                Console.WriteLine(">> Loopback Xamarin SDK DLL Compilation errors:");
                foreach (var err in result.Errors)
                {
                    Console.WriteLine(">> " + err.ToString());
                }
                return false;
            }
        }


        /**
         * Handles errors of unsopprted features in the SDK.
         * The convention is {.{.error message.}.} in the code.
         */
        public bool handleUnsupported(string code)
        {
            Regex errRegex = new Regex(@"\{\.\{\.(.+)\.\}\.\}");
            if (errRegex.IsMatch(code)) 
            {
                Match match = errRegex.Match(code);
                Console.WriteLine(">> " + match.Groups[1].Value);
                return false;
            }
            else
            {
                return true;
            }   
        }

        /*
         * This function is called through Edge.JS by lb-xm.js. 
         * A Json definitions is passed and a DLL or CS code for the SDK is created as a result.
         */
        public async Task<object> Invoke(object input)
        {
            // Get json definition of the server
            string jsonModel = ((Object[])input)[0].ToString();
            // WriteDefinitionsDebug(jsonModel);

            // Process flags
            var flags = new HashSet<string>()
            {
                (((Object[])input)[1] ?? "").ToString(),
                (((Object[])input)[2] ?? "").ToString()
            };

            // Create new templates and pass the definition Json to DynamicModels and DynamicRepos
            var dynamicModelsTemplate = new DynamicModels();
            var dynamicReposTemplate = new DynamicRepos();
            var constantCode = new HardcodedModels();

            dynamicModelsTemplate.Session = new Dictionary<string, object>();
            dynamicModelsTemplate.Session["jsonModel"] = jsonModel;
            dynamicReposTemplate.Session = new Dictionary<string, object>();
            dynamicReposTemplate.Session["jsonModel"] = jsonModel;
            constantCode.Session = new Dictionary<string, object>();
            constantCode.Session["jsonModel"] = jsonModel;
            
            if(flags.Contains("forms"))
            {
                Console.WriteLine(">> Parsing for Xamarin-Forms compatibility...");
                constantCode.Session["XamarinForms"] = true;
            }
            else
            {
                constantCode.Session["XamarinForms"] = false;
            }

            // Create dynamic code from templates
            dynamicModelsTemplate.Initialize();
            dynamicReposTemplate.Initialize();
            constantCode.Initialize();

            string code = constantCode.TransformText() + dynamicReposTemplate.TransformText() +
                          dynamicModelsTemplate.TransformText();

            if(!handleUnsupported(code))
            {
                return false;
            }

            if (flags.Contains("dll"))
            {
                Console.WriteLine(">> Compiling...");
                string currentPath = ((Object[]) input)[3].ToString();
                return await Compile(code, "LBXamarinSDK.dll", currentPath);
            }
            else
            {
                Console.WriteLine(">> Writing CS file: LBXamarinSDK.cs...");
                System.IO.StreamWriter file = new System.IO.StreamWriter("LBXamarinSDK.cs");
                file.Write(code);
                file.Close();
                return true;
            }
        }
    }

}


// For debugging.
/*
namespace LBXamarinSDK
{
    public class DebugProgram
    {
        private static void Main(string[] args)
        {

            Gateway.SetServerBaseURLToSelf();
            Gateway.SetDebugMode(true);
        }
    }
}*/

