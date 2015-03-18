using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LBXamarinSDK;
using Microsoft.CSharp;

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
            Console.WriteLine("Writing server Json definition to D:\\debug.txt");
            System.IO.StreamWriter file = new System.IO.StreamWriter("D:\\debug.txt");
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

        /*
         * This function is called through Edge.JS by lb-xm.js. 
         * A Json definitions is passed and a DLL or CS code for the SDK is created as a result.
         */
        public async Task<object> Invoke(object input)
        {
            // Get the DLL output path and json definition of the server
            string jsonModel = ((Object[]) input)[0].ToString();
            //WriteDefinitionsDebug(jsonModel);

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

            // Create dynamic code from templates
            dynamicModelsTemplate.Initialize();
            dynamicReposTemplate.Initialize();
            constantCode.Initialize();

            string code = constantCode.TransformText() + dynamicReposTemplate.TransformText() +
                          dynamicModelsTemplate.TransformText();

            // Determine: Compile DLL or output CS
            var compileFlag = ((Object[]) input)[2];
            if (compileFlag != null && compileFlag.ToString() == "c")
            {
                string outputPath = "SDK.dll";
                if (((Object[]) input)[1] != null)
                {
                    outputPath = ((Object[])input)[1].ToString();
                }
                Console.WriteLine("Compiling...");
                string currentPath = ((Object[]) input)[3].ToString();
                return await Compile(code, outputPath, currentPath);
            }
            else
            {
                Console.WriteLine("Writing CS file: LBXamarinSDK.cs...");
                System.IO.StreamWriter file = new System.IO.StreamWriter("LBXamarinSDK.cs");
                file.Write(code);
                file.Close();
                return true;
            }
        }
    }

}

/*
// For debugging.
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
}
   
*/