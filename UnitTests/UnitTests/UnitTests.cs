using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using System.IO;
using System.Net;
using System.Net.Mime;
using System.Net.Sockets;
using System.Reflection;

namespace UnitTests
{
    [TestFixture]
    public class UnitTests
    {
        [Test]
        public void TestServer1()
        {
            // Create SDK for server and run it    
            var loadedDll = NodeJsHandler.StartTest("d:/relationsServer/server/server.js", "test1");

            // Make sure it has all the models and repositories
            List<string> models = loadedDll.GetExportedTypes().Select(t => t.ToString()).ToList();
            Assert.Contains("LBXamarinSDK.Order", models);
            Assert.Contains("LBXamarinSDK.Customer", models);
            Assert.Contains("LBXamarinSDK.Review", models);
            Assert.Contains("LBXamarinSDK.User", models);
            Assert.Contains("LBXamarinSDK.LBRepo.Orders", models);
            Assert.Contains("LBXamarinSDK.LBRepo.Customers", models);
            Assert.Contains("LBXamarinSDK.LBRepo.Reviews", models);
            Assert.Contains("LBXamarinSDK.LBRepo.Users", models);

            // Get basic CRUD methods of model "Order"
            var ordersCrud = loadedDll.GetType("LBXamarinSDK.LBRepo.Orders").BaseType;
            var createOrder = ordersCrud.GetMethod("Create");
            var upsertOrder = ordersCrud.GetMethod("Upsert");
            var orderExists = ordersCrud.GetMethod("Exists");
            var findOrderById = ordersCrud.GetMethod("FindById");
            var findOrders = ordersCrud.GetMethod("Find");
            var findOneOrder = ordersCrud.GetMethod("FindOne");
            var updateAllOrders = ordersCrud.GetMethod("UpdateAll");
            var deleteOrderById = ordersCrud.GetMethod("DeleteById");
            var countOrders = ordersCrud.GetMethod("Count");
            var updateOrderById = ordersCrud.GetMethod("UpdateById");

            // TODO: Test Basic CRUD methods of model "Order"
            var orderRepository = Activator.CreateInstance(loadedDll.GetType("LBXamarinSDK.LBRepo.Orders"));
            Assert.AreEqual(5, ((Task<int>)countOrders.Invoke(orderRepository, new object[] { null })).Result);

            // TODO: Test extended API & relations of model "Order"

            // TODO: Get basic CRUD methods of model "Review"
            // TODO: Test Basic CRUD methods of model "Review"
            // TODO: Test extended API & relations of model "Review"

            // TODO: Get basic CRUD methods of model "Customer"
            // TODO: Test Basic CRUD methods of model "Customer"
            // TODO: Test extended API & relations of model "Customer"

            NodeJsHandler.EndTest();
        }

        /*
         * TODO: Testers for the rest of the servers
         */

        // DEBUG:
        public static void Main()
        {
            // DEBUG:
            //var loadedDll = NodeJsHandler.StartTest("d:/relationsServer/server/server.js", "test1");

            //foreach (var s in loadedDll.GetExportedTypes())
            //    Console.WriteLine(s);
            
            //NodeJsHandler.EndTest();
        }
    }
}
