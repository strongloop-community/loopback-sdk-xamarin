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
        private const string outputFolder = "output";
        /**
        * Debug Tool
        */
        public void WriteDefinitionsDebug(string jsonModel)
        {
            string outputPath = outputFolder + ("/lb-xm-def-" + DateTime.Now + ".txt").Replace(" ", "-").Replace(":", ".");
            Console.WriteLine(">> Writing server definition to " + outputPath);
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
                if(outputPath != null)
                    Console.WriteLine(">> Loopback Xamarin SDK DLL: " + outputPath + " compiled successfully.");
                return true;
            }
            else
            {
                if (outputPath != null)
                {
                    Console.WriteLine(">> Loopback Xamarin SDK DLL Compilation errors:");
                    foreach (var err in result.Errors)
                    {
                        Console.WriteLine(">> " + err.ToString());
                    }
                }   
                return false;
            }
        }


        /**
         * Handles errors of unsupported features in the SDK.
         * The convention is {.{.error message.}.} in the code.
         */
        public bool handleUnsupported(string code)
        {
            Regex errRegex = new Regex(@"\{\.\{\.([a-zA-Z0-9 \(\)\.\,\'\?_\-\:]+)\.\}\.\}");
            if (errRegex.IsMatch(code)) 
            {
                foreach (Match errMatch in errRegex.Matches(code))
                    Console.WriteLine(">> " + errMatch.Groups[1].Value);
                Console.WriteLine(">> You can force SDK creation with the flag 'force'.");
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
            
            // Process flags
            var flags = new HashSet<string>()
            {
                (((Object[])input)[1] ?? "").ToString(),
                (((Object[])input)[2] ?? "").ToString(),
                (((Object[])input)[4] ?? "").ToString(),
                (((Object[])input)[5] ?? "").ToString(),
                (((Object[])input)[6] ?? "").ToString()
            };

            if (flags.Contains("debug"))
            {
                WriteDefinitionsDebug(jsonModel);
            }

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

            if (flags.Contains("force"))
            {
                Console.WriteLine(">> Forcing SDK creation...");
                dynamicReposTemplate.Session["force"] = true;
            }
            else
            {
                dynamicReposTemplate.Session["force"] = false;
            }

            // Create dynamic code from templates
            dynamicModelsTemplate.Initialize();
            dynamicReposTemplate.Initialize();
            constantCode.Initialize();

            string code = constantCode.TransformText() + dynamicReposTemplate.TransformText() +
                          dynamicModelsTemplate.TransformText();

            if(!flags.Contains("force") && !handleUnsupported(code))
            {
                return false;
            }

            Directory.CreateDirectory(outputFolder);
            string currentPath = ((Object[])input)[3].ToString();
            if (flags.Contains("dll"))
            {
                Console.WriteLine(">> Compiling...");
                return await Compile(code, outputFolder + "/LBXamarinSDK.dll", currentPath);
            }
            else
            {
                if (flags.Contains("check") && !await Compile(code, null, currentPath))
                {
                    Console.WriteLine(">> Check: Failed.");
                    return false;
                }
                else
                {
                    Console.WriteLine(">> Check: Successful.");
                }
                Console.WriteLine(">> Writing CS file: " + outputFolder + "/LBXamarinSDK.cs...");
                System.IO.StreamWriter file = new System.IO.StreamWriter(outputFolder + "/LBXamarinSDK.cs");
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

