using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LBXamarinSDK;
using LBXamarinSDK.LBRepo;

namespace ConsoleApplication5
{
    class Program
    {
        static void showConsoleOutput()
        {
            // Show debug outputs from the SDK
            TextWriterTraceListener writer = new TextWriterTraceListener(System.Console.Out);
            Debug.Listeners.Add(writer);
        }

        static async void helloWorld()
        {
            if (!await Gateway.isConnected())
            {
                Console.WriteLine("Server is down. Exiting.");
                return;
            }

            Customer myCustomer = new Customer()
            {
                name = "joe",
                geoLocation = new GeoPoint()
                {
                    Longitude = 0,
                    Latitude = 11
                }
            };
            Console.WriteLine("Creating a new customer:");
            myCustomer = await Customers.Create(myCustomer);
            Console.WriteLine("Customer created with ID " + myCustomer.id);
            Console.WriteLine("There are " + await Customers.Count() + " customers on the server.");

            User auth = new User()
            {
                email = "yeh@g.com",
                password = "123"
            };
            Console.WriteLine("Creating a new user:");
            try
            {
                var createdUser = await Users.Create(auth);
                Console.WriteLine("User created successfully with ID " + auth.id);
            }
            catch (RestException e)
            {
                Console.WriteLine("Could not create user. Perhaps the Email address is already in use.");
            }
            Console.WriteLine("Logging in:");

            try
            {
                AccessToken token = await Users.login(auth);
                Console.WriteLine("Success! All following requests will be done with the access token authorization of the login.");
                Console.WriteLine("Number of customers on the server: " + await Customers.Count());

            }
            catch (RestException e)
            {
                Console.WriteLine("Could nto log in.");
            }

            Console.WriteLine("Done.");
        }

        static void Main(string[] args)
        {
            showConsoleOutput();
            Gateway.SetDebugMode(true);
            Gateway.SetServerBaseURLToSelf();
            helloWorld();
            Console.ReadKey();
        }
    }
}
