using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LBXamarinSDK;
using LBXamarinSDK.LBRepo;
using Microsoft.CSharp;
using Newtonsoft.Json.Linq;
using RestSharp.Portable;

/*
namespace LBXamarinSDKGenerator
{
    public class Startup
    {
        public async Task<object> Invoke(object input)
        {
            string jsonModel = ((Object[]) input)[0].ToString();
            
            //System.IO.StreamWriter file = new System.IO.StreamWriter("D:\\debug.txt");
            //file.Write(jsonModel);
            //file.Close();
            

            string outputPath = ((Object[])input)[1].ToString();

            var dynamicModelsTemplate = new DynamicModels();
            var dynamicReposTemplate = new DynamicRepos();
            var constantCode = new HardcodedModels();

            dynamicModelsTemplate.Session = new Dictionary<string, object>();
            dynamicModelsTemplate.Session["jsonModel"] = jsonModel;

            dynamicReposTemplate.Session = new Dictionary<string, object>();
            dynamicReposTemplate.Session["jsonModel"] = jsonModel;

            dynamicModelsTemplate.Initialize();
            dynamicReposTemplate.Initialize();
            
            string code = constantCode.TransformText() + dynamicReposTemplate.TransformText() +
                          dynamicModelsTemplate.TransformText();

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

            if (result.Errors.Count == 0)
            {
                Console.WriteLine("Loopback Xamarin SDK DLL compiled successfully.");
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

*/

namespace LBXamarinSDK
{
    public class DebugProgram
    {
        private static void Main(string[] args)
        {
            // Some change in the code, Blarg blarg, blarg.
            Gateway.SetServerBaseURL(new Uri("http://10.0.0.27:3000/api/"));
            Gateway.SetDebugMode(true);

            Trainer h = new Trainer()
            {
                email = "g@gmail.com",address = "fefe"
            };
            
            Console.ReadKey();
        }
    }
}
    
