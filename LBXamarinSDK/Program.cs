using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LBXamarinSDK;
using Microsoft.CSharp;
using Newtonsoft.Json.Linq;
using RestSharp.Portable;

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
            System.IO.StreamWriter file = new System.IO.StreamWriter("D:\\debug.txt");
            file.Write(jsonModel);
            file.Close();
        }

        /*
         * This function is called through Edge.JS by lb-xm.js. 
         * A Json definitions is passed and a DLL for the SDK is created as a result.
         */
        public async Task<object> Invoke(object input)
        {
            // Get the DLL output path and json definition of the server
            string jsonModel = ((Object[]) input)[0].ToString();
            string outputPath = ((Object[])input)[1].ToString();

            // Create new templates and pass the definition Json to DynamicModels and DynamicRepos
            var dynamicModelsTemplate = new DynamicModels();
            var dynamicReposTemplate = new DynamicRepos();
            var constantCode = new HardcodedModels();

            dynamicModelsTemplate.Session = new Dictionary<string, object>();
            dynamicModelsTemplate.Session["jsonModel"] = jsonModel;
            dynamicReposTemplate.Session = new Dictionary<string, object>();
            dynamicReposTemplate.Session["jsonModel"] = jsonModel;
            
            // Create dynamic code from templates
            dynamicModelsTemplate.Initialize();
            dynamicReposTemplate.Initialize();
            string code = constantCode.TransformText() + dynamicReposTemplate.TransformText() +
                          dynamicModelsTemplate.TransformText();

            // Compile dynamic code
            CompilerParameters compilerParams = new CompilerParameters() { OutputAssembly = outputPath };
            compilerParams.ReferencedAssemblies.Add("System.dll");
            compilerParams.ReferencedAssemblies.Add("System.Core.dll");
            compilerParams.ReferencedAssemblies.Add("System.Linq.dll");
            compilerParams.ReferencedAssemblies.Add("System.Net.Http.dll");
            compilerParams.ReferencedAssemblies.Add("System.Runtime.dll");
            compilerParams.ReferencedAssemblies.Add("/RestSharp.Portable.dll");
            compilerParams.ReferencedAssemblies.Add("/Newtonsoft.Json.dll");
            compilerParams.WarningLevel = 1;
            compilerParams.TreatWarningsAsErrors = false;
            var compiler = new CSharpCodeProvider();
            var result = compiler.CompileAssemblyFromSource(compilerParams, code);

            // Print messages 
            if (result.Errors.Count == 0)
            {
                Console.WriteLine("Loopback Xamarin SDK DLL: " + outputPath + " compiled successfully.");
                return true;
            }
            else
            {
                Console.WriteLine("Loopback Xamarin SDK DLL Compilation errors:");
                foreach (var err in result.Errors)
                {
                    Console.WriteLine(err.ToString());
                }
                return false;
            }
        }
    }
}


/*
namespace LBXamarinSDK
{
    public class DebugProgram
    {
        private static void Main(string[] args)
        {

            Gateway.SetServerBaseURL(new Uri("http://10.0.0.27:3000/api/"));
            Gateway.SetDebugMode(true);
            Console.ReadKey();
        }
    }
}*/
    
