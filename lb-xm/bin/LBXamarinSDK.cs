



// Usings for all 3 templates
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RestSharp.Portable;
using LBXamarinSDK;
using LBXamarinSDK.LBRepo;
using System.Net.Http;
using System.Threading;

namespace LBXamarinSDK
{
	// Custom response for basic CRUD operations
	public class ExistenceResponse
    {
        [Newtonsoft.Json.JsonProperty("exists")]
        public bool exists { get; set; }
    }
    public class CountResponse
    {
        [Newtonsoft.Json.JsonProperty("count")]
        public int count { get; set; }
    }
	
	// Gateway: Communication with Server API
	public class Gateway
    {
        private static Uri BASE_URL = new Uri("http://10.0.0.1:3000/api/");
		
		private static RestClient _client = new RestClient {BaseUrl = BASE_URL};
        private static string _accessToken = null;
		private static bool debugMode = false;
        private static CancellationTokenSource cts = new CancellationTokenSource();
		private static int timeout = 6000;

		// Allow Console WriteLines to debug communication with server
		public static void SetDebugMode(bool isDebugMode)
		{
			debugMode = isDebugMode;
            // **** VITAL! DO NOT REMOVE! ********
            Console.WriteLine("*******************************************************************************");
            Console.WriteLine("** SDK Gateway constructor. If something doesn't work it's Chayim's fault.   **");
            Console.WriteLine("*******************************************************************************\n");
            // **** ^^^ VITAL! DO NOT REMOVE! ********
		}
		
		/*** Cancellation Token methods, define a timeout for a server request ***/
		private static void ResetCancellationToken()
		{
			cts = new CancellationTokenSource();
            cts.CancelAfter(timeout);
		}

        public static void SetTimeout(int timeoutMilliseconds = 6000)
        {
			timeout = timeoutMilliseconds;
			ResetCancellationToken();
        }
		/* *** */

		// Define server Base Url for API requests. Example: "http://10.0.0.1:3000/api/"
        public static void SetServerBaseURL(Uri baseUrl)
        {
            BASE_URL = baseUrl;
            _client.BaseUrl = baseUrl;
        }

		// Sets an access token to be added as an authorization in all future server requests
        public static void SetAccessToken(AccessToken accessToken)
        {
            if (accessToken != null)
                _accessToken = accessToken.id;
        }

		// Performs a request to determine if connected to server
        public static async Task<bool> isConnected(int timeoutMilliseconds = 6000)
		{
			SetTimeout(timeoutMilliseconds);
			cts.Token.ThrowIfCancellationRequested();
			try
			{
				var request = new RestRequest ("/", new HttpMethod ("GET"));
				var response = await _client.Execute<JObject>(request, cts.Token).ConfigureAwait(false);
				if (response != null)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			catch(Exception e)
			{
				if (debugMode)
                    Console.WriteLine("-------- >> DEBUG: Error: " + e.Message + " >>");	 
				return false;
			}
		}

		// Resets the authorization token
        public static void ResetAccessToken()
        {
            _accessToken = null;
        }
        
		// Makes a request through restSharp to server
		public static async Task<T> MakeRequest<T>(RestRequest request)
		{
            ResetCancellationToken();
			cts.Token.ThrowIfCancellationRequested();

		    try
		    {
                var response = await _client.Execute<T>(request, cts.Token).ConfigureAwait(false);
		        return response.Data;
		    }
		    catch (OperationCanceledException e)
		    {
                if (debugMode)
                    Console.WriteLine("-------- >> DEBUG: Timeout, no response from server: " + e.Message + " >>");	 
		    }
		    catch (Exception e)
		    {
				if(debugMode)
					Console.WriteLine("-------- >> DEBUG: Error performing request: " + e.Message + " >>");	     
		    }

            return default(T);
		}


		// Parses a server request then makes it through MakeRequest
        public static async Task<T> PerformRequest<T>(string APIUrl, string json, string method = "POST", IDictionary<string, string> queryStrings = null)
		{
			if(debugMode)
				Console.WriteLine("-------- >> DEBUG: Performing " + method + " request, Json: " + (string.IsNullOrEmpty(json) ? "EMPTY" : json));

		    RestRequest request = null;
            request = new RestRequest(APIUrl, new HttpMethod(method));

			// Add query parameters to the request
            if (queryStrings != null)
            {
                foreach (var query in queryStrings)
                {
                    if (!string.IsNullOrEmpty(query.Value))
                    {
                        request.AddParameter(query.Key, query.Value, ParameterType.QueryString);
                    }
                }
            }

			// Add authorization token to the request
            if (!String.IsNullOrEmpty(_accessToken))
            {
                request.AddHeader("Authorization", _accessToken);
            }

			// Add body parameters to the request
			if (method == "POST" || method == "PUT")
            {
				request.AddHeader("ContentType", "application/json");
				request.AddParameter ("application/json", json, ParameterType.RequestBody);
			}

			// Make the request, return response
			try
			{
				var response = await MakeRequest<T>(request).ConfigureAwait(false);
				return response;
			}
            catch(Exception e)
			{
                if (debugMode)
                    Console.WriteLine("-------- >> DEBUG: Error performing request: " + e.Message + " >>");	     
				return default(T);
			}     
            
		}

        // T is the expected return type, U is the input type. E.g. U is Car, T is Car
        public static async Task<T> PerformPostRequest<U, T>(U objToPost, string APIUrl, IDictionary<string, string> queryStrings = null)
        {
            var res = await PerformRequest<T>(APIUrl, JsonConvert.SerializeObject(objToPost), "POST", queryStrings).ConfigureAwait(false);
            return res;
        }

        // T is the expected return type. For example "Car" for get or "Car[]" for get all cars
        public static async Task<T> PerformGetRequest<T>(string APIUrl, IDictionary<string, string> queryStrings = null)
        {	
            var res = await PerformRequest<T>(APIUrl, "", "GET", queryStrings).ConfigureAwait(false);
            return res;
        }

        // T is the expected return type, U is the input type. E.g. U is Car, T is Car
        public static async Task<T> PerformPutRequest<U, T>(U objToPut, string APIUrl, IDictionary<string, string> queryStrings = null)
        {
            var res = await PerformRequest<T>(APIUrl, JsonConvert.SerializeObject(objToPut), "PUT", queryStrings).ConfigureAwait(false);
            return res;
        }
    }

	// Base model for all LBXamarinSDK Models
	public abstract class Model
    {
        public virtual String getID()
        {
            return "";
        }
    }

	// GeoPoint primitive loopback type
	public class GeoPoint : Model
	{
		// Must be leq than 90: TODO: Add attributes or setter limitations
		[JsonProperty("lat", NullValueHandling = NullValueHandling.Ignore)]
		public double Latitude { get; set; }

		[JsonProperty("lng", NullValueHandling = NullValueHandling.Ignore)]
		public double Longtitude { get; set; }
	}
}



// Dynamically created Repo cs file from Json

namespace LBXamarinSDK
{
    namespace LBRepo
    {
		/* CRUD Interface holds the basic CRUD operations for all models.
		   In turn, all repositories will inherit from this.
		*/
        public abstract class CRUDInterface<T> where T : Model
        {
			public static String getAPIPath(String crudMethodName)
            {
				String modelName = (typeof(T)).ToString().Substring((typeof(T)).ToString().IndexOf('.')+1);
			    crudMethodName = crudMethodName.ToLower();
				if (modelName == "AccessToken" && crudMethodName == "create") return "accessTokens";
				if (modelName == "AccessToken" && crudMethodName == "upsert") return "accessTokens";
				if (modelName == "AccessToken" && crudMethodName == "exists") return "accessTokens/:id/exists";
				if (modelName == "AccessToken" && crudMethodName == "findbyid") return "accessTokens/:id";
				if (modelName == "AccessToken" && crudMethodName == "find") return "accessTokens";
				if (modelName == "AccessToken" && crudMethodName == "findone") return "accessTokens/findOne";
				if (modelName == "AccessToken" && crudMethodName == "updateall") return "accessTokens/update";
				if (modelName == "AccessToken" && crudMethodName == "deletebyid") return "accessTokens/:id";
				if (modelName == "AccessToken" && crudMethodName == "count") return "accessTokens/count";
				if (modelName == "AccessToken" && crudMethodName == "prototype$updateattributes") return "accessTokens/:id";
				if (modelName == "RoleMapping" && crudMethodName == "create") return "RoleMappings";
				if (modelName == "RoleMapping" && crudMethodName == "upsert") return "RoleMappings";
				if (modelName == "RoleMapping" && crudMethodName == "exists") return "RoleMappings/:id/exists";
				if (modelName == "RoleMapping" && crudMethodName == "findbyid") return "RoleMappings/:id";
				if (modelName == "RoleMapping" && crudMethodName == "find") return "RoleMappings";
				if (modelName == "RoleMapping" && crudMethodName == "findone") return "RoleMappings/findOne";
				if (modelName == "RoleMapping" && crudMethodName == "updateall") return "RoleMappings/update";
				if (modelName == "RoleMapping" && crudMethodName == "deletebyid") return "RoleMappings/:id";
				if (modelName == "RoleMapping" && crudMethodName == "count") return "RoleMappings/count";
				if (modelName == "RoleMapping" && crudMethodName == "prototype$updateattributes") return "RoleMappings/:id";
				if (modelName == "Role" && crudMethodName == "create") return "Roles";
				if (modelName == "Role" && crudMethodName == "upsert") return "Roles";
				if (modelName == "Role" && crudMethodName == "exists") return "Roles/:id/exists";
				if (modelName == "Role" && crudMethodName == "findbyid") return "Roles/:id";
				if (modelName == "Role" && crudMethodName == "find") return "Roles";
				if (modelName == "Role" && crudMethodName == "findone") return "Roles/findOne";
				if (modelName == "Role" && crudMethodName == "updateall") return "Roles/update";
				if (modelName == "Role" && crudMethodName == "deletebyid") return "Roles/:id";
				if (modelName == "Role" && crudMethodName == "count") return "Roles/count";
				if (modelName == "Role" && crudMethodName == "prototype$updateattributes") return "Roles/:id";
				if (modelName == "UserCredential" && crudMethodName == "create") return "userCredentials";
				if (modelName == "UserCredential" && crudMethodName == "upsert") return "userCredentials";
				if (modelName == "UserCredential" && crudMethodName == "exists") return "userCredentials/:id/exists";
				if (modelName == "UserCredential" && crudMethodName == "findbyid") return "userCredentials/:id";
				if (modelName == "UserCredential" && crudMethodName == "find") return "userCredentials";
				if (modelName == "UserCredential" && crudMethodName == "findone") return "userCredentials/findOne";
				if (modelName == "UserCredential" && crudMethodName == "updateall") return "userCredentials/update";
				if (modelName == "UserCredential" && crudMethodName == "deletebyid") return "userCredentials/:id";
				if (modelName == "UserCredential" && crudMethodName == "count") return "userCredentials/count";
				if (modelName == "UserCredential" && crudMethodName == "prototype$updateattributes") return "userCredentials/:id";
				if (modelName == "UserIdentity" && crudMethodName == "create") return "userIdentities";
				if (modelName == "UserIdentity" && crudMethodName == "upsert") return "userIdentities";
				if (modelName == "UserIdentity" && crudMethodName == "exists") return "userIdentities/:id/exists";
				if (modelName == "UserIdentity" && crudMethodName == "findbyid") return "userIdentities/:id";
				if (modelName == "UserIdentity" && crudMethodName == "find") return "userIdentities";
				if (modelName == "UserIdentity" && crudMethodName == "findone") return "userIdentities/findOne";
				if (modelName == "UserIdentity" && crudMethodName == "updateall") return "userIdentities/update";
				if (modelName == "UserIdentity" && crudMethodName == "deletebyid") return "userIdentities/:id";
				if (modelName == "UserIdentity" && crudMethodName == "count") return "userIdentities/count";
				if (modelName == "UserIdentity" && crudMethodName == "prototype$updateattributes") return "userIdentities/:id";
				if (modelName == "Page" && crudMethodName == "create") return "pages";
				if (modelName == "Page" && crudMethodName == "upsert") return "pages";
				if (modelName == "Page" && crudMethodName == "exists") return "pages/:id/exists";
				if (modelName == "Page" && crudMethodName == "findbyid") return "pages/:id";
				if (modelName == "Page" && crudMethodName == "find") return "pages";
				if (modelName == "Page" && crudMethodName == "findone") return "pages/findOne";
				if (modelName == "Page" && crudMethodName == "updateall") return "pages/update";
				if (modelName == "Page" && crudMethodName == "deletebyid") return "pages/:id";
				if (modelName == "Page" && crudMethodName == "count") return "pages/count";
				if (modelName == "Page" && crudMethodName == "prototype$updateattributes") return "pages/:id";
				if (modelName == "Note" && crudMethodName == "create") return "notes";
				if (modelName == "Note" && crudMethodName == "upsert") return "notes";
				if (modelName == "Note" && crudMethodName == "exists") return "notes/:id/exists";
				if (modelName == "Note" && crudMethodName == "findbyid") return "notes/:id";
				if (modelName == "Note" && crudMethodName == "find") return "notes";
				if (modelName == "Note" && crudMethodName == "findone") return "notes/findOne";
				if (modelName == "Note" && crudMethodName == "updateall") return "notes/update";
				if (modelName == "Note" && crudMethodName == "deletebyid") return "notes/:id";
				if (modelName == "Note" && crudMethodName == "count") return "notes/count";
				if (modelName == "Note" && crudMethodName == "prototype$updateattributes") return "notes/:id";
				if (modelName == "Post" && crudMethodName == "create") return "posts";
				if (modelName == "Post" && crudMethodName == "upsert") return "posts";
				if (modelName == "Post" && crudMethodName == "exists") return "posts/:id/exists";
				if (modelName == "Post" && crudMethodName == "findbyid") return "posts/:id";
				if (modelName == "Post" && crudMethodName == "find") return "posts";
				if (modelName == "Post" && crudMethodName == "findone") return "posts/findOne";
				if (modelName == "Post" && crudMethodName == "updateall") return "posts/update";
				if (modelName == "Post" && crudMethodName == "deletebyid") return "posts/:id";
				if (modelName == "Post" && crudMethodName == "count") return "posts/count";
				if (modelName == "Post" && crudMethodName == "prototype$updateattributes") return "posts/:id";
				if (modelName == "Setting" && crudMethodName == "create") return "settings";
				if (modelName == "Setting" && crudMethodName == "upsert") return "settings";
				if (modelName == "Setting" && crudMethodName == "exists") return "settings/:id/exists";
				if (modelName == "Setting" && crudMethodName == "findbyid") return "settings/:id";
				if (modelName == "Setting" && crudMethodName == "find") return "settings";
				if (modelName == "Setting" && crudMethodName == "findone") return "settings/findOne";
				if (modelName == "Setting" && crudMethodName == "updateall") return "settings/update";
				if (modelName == "Setting" && crudMethodName == "deletebyid") return "settings/:id";
				if (modelName == "Setting" && crudMethodName == "count") return "settings/count";
				if (modelName == "Setting" && crudMethodName == "prototype$updateattributes") return "settings/:id";
				if (modelName == "Product" && crudMethodName == "create") return "products";
				if (modelName == "Product" && crudMethodName == "upsert") return "products";
				if (modelName == "Product" && crudMethodName == "exists") return "products/:id/exists";
				if (modelName == "Product" && crudMethodName == "findbyid") return "products/:id";
				if (modelName == "Product" && crudMethodName == "find") return "products";
				if (modelName == "Product" && crudMethodName == "findone") return "products/findOne";
				if (modelName == "Product" && crudMethodName == "updateall") return "products/update";
				if (modelName == "Product" && crudMethodName == "deletebyid") return "products/:id";
				if (modelName == "Product" && crudMethodName == "count") return "products/count";
				if (modelName == "Product" && crudMethodName == "prototype$updateattributes") return "products/:id";
				if (modelName == "Category" && crudMethodName == "create") return "categories";
				if (modelName == "Category" && crudMethodName == "upsert") return "categories";
				if (modelName == "Category" && crudMethodName == "exists") return "categories/:id/exists";
				if (modelName == "Category" && crudMethodName == "findbyid") return "categories/:id";
				if (modelName == "Category" && crudMethodName == "find") return "categories";
				if (modelName == "Category" && crudMethodName == "findone") return "categories/findOne";
				if (modelName == "Category" && crudMethodName == "updateall") return "categories/update";
				if (modelName == "Category" && crudMethodName == "deletebyid") return "categories/:id";
				if (modelName == "Category" && crudMethodName == "count") return "categories/count";
				if (modelName == "Category" && crudMethodName == "prototype$updateattributes") return "categories/:id";
				if (modelName == "Event" && crudMethodName == "create") return "events";
				if (modelName == "Event" && crudMethodName == "upsert") return "events";
				if (modelName == "Event" && crudMethodName == "exists") return "events/:id/exists";
				if (modelName == "Event" && crudMethodName == "findbyid") return "events/:id";
				if (modelName == "Event" && crudMethodName == "find") return "events";
				if (modelName == "Event" && crudMethodName == "findone") return "events/findOne";
				if (modelName == "Event" && crudMethodName == "updateall") return "events/update";
				if (modelName == "Event" && crudMethodName == "deletebyid") return "events/:id";
				if (modelName == "Event" && crudMethodName == "count") return "events/count";
				if (modelName == "Event" && crudMethodName == "prototype$updateattributes") return "events/:id";
				if (modelName == "AuthProvider" && crudMethodName == "create") return "AuthProviders";
				if (modelName == "AuthProvider" && crudMethodName == "upsert") return "AuthProviders";
				if (modelName == "AuthProvider" && crudMethodName == "exists") return "AuthProviders/:id/exists";
				if (modelName == "AuthProvider" && crudMethodName == "findbyid") return "AuthProviders/:id";
				if (modelName == "AuthProvider" && crudMethodName == "find") return "AuthProviders";
				if (modelName == "AuthProvider" && crudMethodName == "findone") return "AuthProviders/findOne";
				if (modelName == "AuthProvider" && crudMethodName == "updateall") return "AuthProviders/update";
				if (modelName == "AuthProvider" && crudMethodName == "deletebyid") return "AuthProviders/:id";
				if (modelName == "AuthProvider" && crudMethodName == "count") return "AuthProviders/count";
				if (modelName == "AuthProvider" && crudMethodName == "prototype$updateattributes") return "AuthProviders/:id";
				if (modelName == "User" && crudMethodName == "create") return "users";
				if (modelName == "User" && crudMethodName == "upsert") return "users";
				if (modelName == "User" && crudMethodName == "exists") return "users/:id/exists";
				if (modelName == "User" && crudMethodName == "findbyid") return "users/:id";
				if (modelName == "User" && crudMethodName == "find") return "users";
				if (modelName == "User" && crudMethodName == "findone") return "users/findOne";
				if (modelName == "User" && crudMethodName == "updateall") return "users/update";
				if (modelName == "User" && crudMethodName == "deletebyid") return "users/:id";
				if (modelName == "User" && crudMethodName == "count") return "users/count";
				if (modelName == "User" && crudMethodName == "prototype$updateattributes") return "users/:id";
				if (modelName == "Firm" && crudMethodName == "create") return "Firms";
				if (modelName == "Firm" && crudMethodName == "upsert") return "Firms";
				if (modelName == "Firm" && crudMethodName == "exists") return "Firms/:id/exists";
				if (modelName == "Firm" && crudMethodName == "findbyid") return "Firms/:id";
				if (modelName == "Firm" && crudMethodName == "find") return "Firms";
				if (modelName == "Firm" && crudMethodName == "findone") return "Firms/findOne";
				if (modelName == "Firm" && crudMethodName == "updateall") return "Firms/update";
				if (modelName == "Firm" && crudMethodName == "deletebyid") return "Firms/:id";
				if (modelName == "Firm" && crudMethodName == "count") return "Firms/count";
				if (modelName == "Firm" && crudMethodName == "prototype$updateattributes") return "Firms/:id";
				if (modelName == "Office" && crudMethodName == "create") return "Offices";
				if (modelName == "Office" && crudMethodName == "upsert") return "Offices";
				if (modelName == "Office" && crudMethodName == "exists") return "Offices/:id/exists";
				if (modelName == "Office" && crudMethodName == "findbyid") return "Offices/:id";
				if (modelName == "Office" && crudMethodName == "find") return "Offices";
				if (modelName == "Office" && crudMethodName == "findone") return "Offices/findOne";
				if (modelName == "Office" && crudMethodName == "updateall") return "Offices/update";
				if (modelName == "Office" && crudMethodName == "deletebyid") return "Offices/:id";
				if (modelName == "Office" && crudMethodName == "count") return "Offices/count";
				if (modelName == "Office" && crudMethodName == "prototype$updateattributes") return "Offices/:id";
				if (modelName == "MemberRole" && crudMethodName == "create") return "MemberRoles";
				if (modelName == "MemberRole" && crudMethodName == "upsert") return "MemberRoles";
				if (modelName == "MemberRole" && crudMethodName == "exists") return "MemberRoles/:id/exists";
				if (modelName == "MemberRole" && crudMethodName == "findbyid") return "MemberRoles/:id";
				if (modelName == "MemberRole" && crudMethodName == "find") return "MemberRoles";
				if (modelName == "MemberRole" && crudMethodName == "findone") return "MemberRoles/findOne";
				if (modelName == "MemberRole" && crudMethodName == "updateall") return "MemberRoles/update";
				if (modelName == "MemberRole" && crudMethodName == "deletebyid") return "MemberRoles/:id";
				if (modelName == "MemberRole" && crudMethodName == "count") return "MemberRoles/count";
				if (modelName == "MemberRole" && crudMethodName == "prototype$updateattributes") return "MemberRoles/:id";
				if (modelName == "Agreement" && crudMethodName == "create") return "Agreements";
				if (modelName == "Agreement" && crudMethodName == "upsert") return "Agreements";
				if (modelName == "Agreement" && crudMethodName == "exists") return "Agreements/:id/exists";
				if (modelName == "Agreement" && crudMethodName == "findbyid") return "Agreements/:id";
				if (modelName == "Agreement" && crudMethodName == "find") return "Agreements";
				if (modelName == "Agreement" && crudMethodName == "findone") return "Agreements/findOne";
				if (modelName == "Agreement" && crudMethodName == "updateall") return "Agreements/update";
				if (modelName == "Agreement" && crudMethodName == "deletebyid") return "Agreements/:id";
				if (modelName == "Agreement" && crudMethodName == "count") return "Agreements/count";
				if (modelName == "Agreement" && crudMethodName == "prototype$updateattributes") return "Agreements/:id";
				if (modelName == "AgreementType" && crudMethodName == "create") return "AgreementTypes";
				if (modelName == "AgreementType" && crudMethodName == "upsert") return "AgreementTypes";
				if (modelName == "AgreementType" && crudMethodName == "exists") return "AgreementTypes/:id/exists";
				if (modelName == "AgreementType" && crudMethodName == "findbyid") return "AgreementTypes/:id";
				if (modelName == "AgreementType" && crudMethodName == "find") return "AgreementTypes";
				if (modelName == "AgreementType" && crudMethodName == "findone") return "AgreementTypes/findOne";
				if (modelName == "AgreementType" && crudMethodName == "updateall") return "AgreementTypes/update";
				if (modelName == "AgreementType" && crudMethodName == "deletebyid") return "AgreementTypes/:id";
				if (modelName == "AgreementType" && crudMethodName == "count") return "AgreementTypes/count";
				if (modelName == "AgreementType" && crudMethodName == "prototype$updateattributes") return "AgreementTypes/:id";
				if (modelName == "Status" && crudMethodName == "create") return "Statuses";
				if (modelName == "Status" && crudMethodName == "upsert") return "Statuses";
				if (modelName == "Status" && crudMethodName == "exists") return "Statuses/:id/exists";
				if (modelName == "Status" && crudMethodName == "findbyid") return "Statuses/:id";
				if (modelName == "Status" && crudMethodName == "find") return "Statuses";
				if (modelName == "Status" && crudMethodName == "findone") return "Statuses/findOne";
				if (modelName == "Status" && crudMethodName == "updateall") return "Statuses/update";
				if (modelName == "Status" && crudMethodName == "deletebyid") return "Statuses/:id";
				if (modelName == "Status" && crudMethodName == "count") return "Statuses/count";
				if (modelName == "Status" && crudMethodName == "prototype$updateattributes") return "Statuses/:id";
				if (modelName == "Country" && crudMethodName == "create") return "Countries";
				if (modelName == "Country" && crudMethodName == "upsert") return "Countries";
				if (modelName == "Country" && crudMethodName == "exists") return "Countries/:id/exists";
				if (modelName == "Country" && crudMethodName == "findbyid") return "Countries/:id";
				if (modelName == "Country" && crudMethodName == "find") return "Countries";
				if (modelName == "Country" && crudMethodName == "findone") return "Countries/findOne";
				if (modelName == "Country" && crudMethodName == "updateall") return "Countries/update";
				if (modelName == "Country" && crudMethodName == "deletebyid") return "Countries/:id";
				if (modelName == "Country" && crudMethodName == "count") return "Countries/count";
				if (modelName == "Country" && crudMethodName == "prototype$updateattributes") return "Countries/:id";
				if (modelName == "Currency" && crudMethodName == "create") return "Currencies";
				if (modelName == "Currency" && crudMethodName == "upsert") return "Currencies";
				if (modelName == "Currency" && crudMethodName == "exists") return "Currencies/:id/exists";
				if (modelName == "Currency" && crudMethodName == "findbyid") return "Currencies/:id";
				if (modelName == "Currency" && crudMethodName == "find") return "Currencies";
				if (modelName == "Currency" && crudMethodName == "findone") return "Currencies/findOne";
				if (modelName == "Currency" && crudMethodName == "updateall") return "Currencies/update";
				if (modelName == "Currency" && crudMethodName == "deletebyid") return "Currencies/:id";
				if (modelName == "Currency" && crudMethodName == "count") return "Currencies/count";
				if (modelName == "Currency" && crudMethodName == "prototype$updateattributes") return "Currencies/:id";
				if (modelName == "Specialty" && crudMethodName == "create") return "Specialities";
				if (modelName == "Specialty" && crudMethodName == "upsert") return "Specialities";
				if (modelName == "Specialty" && crudMethodName == "exists") return "Specialities/:id/exists";
				if (modelName == "Specialty" && crudMethodName == "findbyid") return "Specialities/:id";
				if (modelName == "Specialty" && crudMethodName == "find") return "Specialities";
				if (modelName == "Specialty" && crudMethodName == "findone") return "Specialities/findOne";
				if (modelName == "Specialty" && crudMethodName == "updateall") return "Specialities/update";
				if (modelName == "Specialty" && crudMethodName == "deletebyid") return "Specialities/:id";
				if (modelName == "Specialty" && crudMethodName == "count") return "Specialities/count";
				if (modelName == "Specialty" && crudMethodName == "prototype$updateattributes") return "Specialities/:id";
				if (modelName == "FilingType" && crudMethodName == "create") return "FilingTypes";
				if (modelName == "FilingType" && crudMethodName == "upsert") return "FilingTypes";
				if (modelName == "FilingType" && crudMethodName == "exists") return "FilingTypes/:id/exists";
				if (modelName == "FilingType" && crudMethodName == "findbyid") return "FilingTypes/:id";
				if (modelName == "FilingType" && crudMethodName == "find") return "FilingTypes";
				if (modelName == "FilingType" && crudMethodName == "findone") return "FilingTypes/findOne";
				if (modelName == "FilingType" && crudMethodName == "updateall") return "FilingTypes/update";
				if (modelName == "FilingType" && crudMethodName == "deletebyid") return "FilingTypes/:id";
				if (modelName == "FilingType" && crudMethodName == "count") return "FilingTypes/count";
				if (modelName == "FilingType" && crudMethodName == "prototype$updateattributes") return "FilingTypes/:id";
				if (modelName == "Filing" && crudMethodName == "create") return "Filings";
				if (modelName == "Filing" && crudMethodName == "upsert") return "Filings";
				if (modelName == "Filing" && crudMethodName == "exists") return "Filings/:id/exists";
				if (modelName == "Filing" && crudMethodName == "findbyid") return "Filings/:id";
				if (modelName == "Filing" && crudMethodName == "find") return "Filings";
				if (modelName == "Filing" && crudMethodName == "findone") return "Filings/findOne";
				if (modelName == "Filing" && crudMethodName == "updateall") return "Filings/update";
				if (modelName == "Filing" && crudMethodName == "deletebyid") return "Filings/:id";
				if (modelName == "Filing" && crudMethodName == "count") return "Filings/count";
				if (modelName == "Filing" && crudMethodName == "prototype$updateattributes") return "Filings/:id";
				if (modelName == "Comment" && crudMethodName == "create") return "Comments";
				if (modelName == "Comment" && crudMethodName == "upsert") return "Comments";
				if (modelName == "Comment" && crudMethodName == "exists") return "Comments/:id/exists";
				if (modelName == "Comment" && crudMethodName == "findbyid") return "Comments/:id";
				if (modelName == "Comment" && crudMethodName == "find") return "Comments";
				if (modelName == "Comment" && crudMethodName == "findone") return "Comments/findOne";
				if (modelName == "Comment" && crudMethodName == "updateall") return "Comments/update";
				if (modelName == "Comment" && crudMethodName == "deletebyid") return "Comments/:id";
				if (modelName == "Comment" && crudMethodName == "count") return "Comments/count";
				if (modelName == "Comment" && crudMethodName == "prototype$updateattributes") return "Comments/:id";
				if (modelName == "Invoice" && crudMethodName == "create") return "Invoices";
				if (modelName == "Invoice" && crudMethodName == "upsert") return "Invoices";
				if (modelName == "Invoice" && crudMethodName == "exists") return "Invoices/:id/exists";
				if (modelName == "Invoice" && crudMethodName == "findbyid") return "Invoices/:id";
				if (modelName == "Invoice" && crudMethodName == "find") return "Invoices";
				if (modelName == "Invoice" && crudMethodName == "findone") return "Invoices/findOne";
				if (modelName == "Invoice" && crudMethodName == "updateall") return "Invoices/update";
				if (modelName == "Invoice" && crudMethodName == "deletebyid") return "Invoices/:id";
				if (modelName == "Invoice" && crudMethodName == "count") return "Invoices/count";
				if (modelName == "Invoice" && crudMethodName == "prototype$updateattributes") return "Invoices/:id";
				if (modelName == "Notification" && crudMethodName == "create") return "Notifications";
				if (modelName == "Notification" && crudMethodName == "upsert") return "Notifications";
				if (modelName == "Notification" && crudMethodName == "exists") return "Notifications/:id/exists";
				if (modelName == "Notification" && crudMethodName == "findbyid") return "Notifications/:id";
				if (modelName == "Notification" && crudMethodName == "find") return "Notifications";
				if (modelName == "Notification" && crudMethodName == "findone") return "Notifications/findOne";
				if (modelName == "Notification" && crudMethodName == "updateall") return "Notifications/update";
				if (modelName == "Notification" && crudMethodName == "deletebyid") return "Notifications/:id";
				if (modelName == "Notification" && crudMethodName == "count") return "Notifications/count";
				if (modelName == "Notification" && crudMethodName == "prototype$updateattributes") return "Notifications/:id";
				if (modelName == "Rate" && crudMethodName == "create") return "Rates";
				if (modelName == "Rate" && crudMethodName == "upsert") return "Rates";
				if (modelName == "Rate" && crudMethodName == "exists") return "Rates/:id/exists";
				if (modelName == "Rate" && crudMethodName == "findbyid") return "Rates/:id";
				if (modelName == "Rate" && crudMethodName == "find") return "Rates";
				if (modelName == "Rate" && crudMethodName == "findone") return "Rates/findOne";
				if (modelName == "Rate" && crudMethodName == "updateall") return "Rates/update";
				if (modelName == "Rate" && crudMethodName == "deletebyid") return "Rates/:id";
				if (modelName == "Rate" && crudMethodName == "count") return "Rates/count";
				if (modelName == "Rate" && crudMethodName == "prototype$updateattributes") return "Rates/:id";
				if (modelName == "FilingSet" && crudMethodName == "create") return "FilingSets";
				if (modelName == "FilingSet" && crudMethodName == "upsert") return "FilingSets";
				if (modelName == "FilingSet" && crudMethodName == "exists") return "FilingSets/:id/exists";
				if (modelName == "FilingSet" && crudMethodName == "findbyid") return "FilingSets/:id";
				if (modelName == "FilingSet" && crudMethodName == "find") return "FilingSets";
				if (modelName == "FilingSet" && crudMethodName == "findone") return "FilingSets/findOne";
				if (modelName == "FilingSet" && crudMethodName == "updateall") return "FilingSets/update";
				if (modelName == "FilingSet" && crudMethodName == "deletebyid") return "FilingSets/:id";
				if (modelName == "FilingSet" && crudMethodName == "count") return "FilingSets/count";
				if (modelName == "FilingSet" && crudMethodName == "prototype$updateattributes") return "FilingSets/:id";
				if (modelName == "FilingGroup" && crudMethodName == "create") return "FilingGroups";
				if (modelName == "FilingGroup" && crudMethodName == "upsert") return "FilingGroups";
				if (modelName == "FilingGroup" && crudMethodName == "exists") return "FilingGroups/:id/exists";
				if (modelName == "FilingGroup" && crudMethodName == "findbyid") return "FilingGroups/:id";
				if (modelName == "FilingGroup" && crudMethodName == "find") return "FilingGroups";
				if (modelName == "FilingGroup" && crudMethodName == "findone") return "FilingGroups/findOne";
				if (modelName == "FilingGroup" && crudMethodName == "updateall") return "FilingGroups/update";
				if (modelName == "FilingGroup" && crudMethodName == "deletebyid") return "FilingGroups/:id";
				if (modelName == "FilingGroup" && crudMethodName == "count") return "FilingGroups/count";
				if (modelName == "FilingGroup" && crudMethodName == "prototype$updateattributes") return "FilingGroups/:id";
				if (modelName == "Version" && crudMethodName == "create") return "Versions";
				if (modelName == "Version" && crudMethodName == "upsert") return "Versions";
				if (modelName == "Version" && crudMethodName == "exists") return "Versions/:id/exists";
				if (modelName == "Version" && crudMethodName == "findbyid") return "Versions/:id";
				if (modelName == "Version" && crudMethodName == "find") return "Versions";
				if (modelName == "Version" && crudMethodName == "findone") return "Versions/findOne";
				if (modelName == "Version" && crudMethodName == "updateall") return "Versions/update";
				if (modelName == "Version" && crudMethodName == "deletebyid") return "Versions/:id";
				if (modelName == "Version" && crudMethodName == "count") return "Versions/count";
				if (modelName == "Version" && crudMethodName == "prototype$updateattributes") return "Versions/:id";
				if (modelName == "UploadedFile" && crudMethodName == "create") return "UploadedFiles";
				if (modelName == "UploadedFile" && crudMethodName == "upsert") return "UploadedFiles";
				if (modelName == "UploadedFile" && crudMethodName == "exists") return "UploadedFiles/:id/exists";
				if (modelName == "UploadedFile" && crudMethodName == "findbyid") return "UploadedFiles/:id";
				if (modelName == "UploadedFile" && crudMethodName == "find") return "UploadedFiles";
				if (modelName == "UploadedFile" && crudMethodName == "findone") return "UploadedFiles/findOne";
				if (modelName == "UploadedFile" && crudMethodName == "updateall") return "UploadedFiles/update";
				if (modelName == "UploadedFile" && crudMethodName == "deletebyid") return "UploadedFiles/:id";
				if (modelName == "UploadedFile" && crudMethodName == "count") return "UploadedFiles/count";
				if (modelName == "UploadedFile" && crudMethodName == "prototype$updateattributes") return "UploadedFiles/:id";
				if (modelName == "NotificationType" && crudMethodName == "create") return "NotificationTypes";
				if (modelName == "NotificationType" && crudMethodName == "upsert") return "NotificationTypes";
				if (modelName == "NotificationType" && crudMethodName == "exists") return "NotificationTypes/:id/exists";
				if (modelName == "NotificationType" && crudMethodName == "findbyid") return "NotificationTypes/:id";
				if (modelName == "NotificationType" && crudMethodName == "find") return "NotificationTypes";
				if (modelName == "NotificationType" && crudMethodName == "findone") return "NotificationTypes/findOne";
				if (modelName == "NotificationType" && crudMethodName == "updateall") return "NotificationTypes/update";
				if (modelName == "NotificationType" && crudMethodName == "deletebyid") return "NotificationTypes/:id";
				if (modelName == "NotificationType" && crudMethodName == "count") return "NotificationTypes/count";
				if (modelName == "NotificationType" && crudMethodName == "prototype$updateattributes") return "NotificationTypes/:id";
                return "Error - no known CRUD path.";
            }

            // All the basic CRUD: Hardcoded
            public static async Task<T> Create(T theModel)
            {
                String APIPath = getAPIPath("Create");
                var response = await Gateway.PerformPostRequest<T, T>(theModel, APIPath).ConfigureAwait(false);
                return response;
            }

            public static async Task<T> Upsert(T theModel)
            {
                String APIPath = getAPIPath("Upsert");
                var response = await Gateway.PerformPutRequest<T, T>(theModel, APIPath).ConfigureAwait(false);
                return response;
            }

            public static async Task<bool> Exists(string ID)
            {
                String APIPath = getAPIPath("Exists");
                APIPath = APIPath.Replace(":id", ID);
                var response = await Gateway.PerformGetRequest<ExistenceResponse>(APIPath).ConfigureAwait(false);
                if (response != null)
                    return response.exists;
                else
                    return false;
            }

            public static async Task<T> FindById(String ID)
            {
                String APIPath = getAPIPath("FindById");
                APIPath = APIPath.Replace(":id", ID);
                var response = await Gateway.PerformGetRequest<T>(APIPath).ConfigureAwait(false);
                return response;
            }

            public static async Task<IList<T>> Find(string filter = "")
            {
                String APIPath = getAPIPath("Find");
                IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				queryStrings.Add("filter", filter);
                var response = await Gateway.PerformGetRequest<T[]>(APIPath, queryStrings).ConfigureAwait(false);
                if (response != null)
                    return response.ToList();
                else
                    return default(IList<T>);
            }

            public static async Task<T> FindOne(string filter = "")
            {
                String APIPath = getAPIPath("FindOne");
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				queryStrings.Add("filter", filter);
                var response = await Gateway.PerformGetRequest<T>(APIPath, queryStrings).ConfigureAwait(false);
                return response;
            }

            public static async Task UpdateAll(T updateModel, string whereFilter)
            {
				String APIPath = getAPIPath("UpdateAll");
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				queryStrings.Add("where", whereFilter);
                var response = await Gateway.PerformPostRequest<T, string>(updateModel, APIPath, queryStrings).ConfigureAwait(false);
            }

            public static async Task DeleteById(String ID)
            {
				String APIPath = getAPIPath("DeleteById");
                APIPath = APIPath.Replace(":id", ID);
                var response = await Gateway.PerformRequest<string>(APIPath, "", "DELETE").ConfigureAwait(false);
            }

            public static async Task<int> Count(string whereFilter = "")
            {
                String APIPath = getAPIPath("Count");
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				queryStrings.Add("where", whereFilter);
                var response = await Gateway.PerformGetRequest<CountResponse>(APIPath, queryStrings).ConfigureAwait(false);
                if (response != null)
                    return response.count;
                else
                    return -1;
            }

            public static async Task<T> UpdateById(String ID, T update)
            {
                String APIPath = getAPIPath("prototype$updateAttributes");
                APIPath = APIPath.Replace(":id", ID);
                var response = await Gateway.PerformPutRequest<T, T>(update, APIPath).ConfigureAwait(false);
                return response;
            }
        }

		// Dynamic repositories for all Dynamic models:
		public class AccessTokens : CRUDInterface<AccessToken>
		{
			
			public static async Task<User> getUser(string id, bool refresh = default(bool))
			{
				string APIPath = "accessTokens/:id/user";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<AccessToken> findByIdForuser(string id, string fk)
			{
				string APIPath = "users/:id/accessTokens/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<AccessToken>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdForuser(string id, string fk)
			{
				string APIPath = "users/:id/accessTokens/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<AccessToken> updateByIdForuser(string id, string fk, AccessToken data)
			{
				string APIPath = "users/:id/accessTokens/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<AccessToken>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<IList<AccessToken>> getForuser(string id, string filter = default(string))
			{
				string APIPath = "users/:id/accessTokens";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<AccessToken[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<AccessToken> createForuser(string id, AccessToken data)
			{
				string APIPath = "users/:id/accessTokens";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<AccessToken>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteForuser(string id)
			{
				string APIPath = "users/:id/accessTokens";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countForuser(string id, string where = default(string))
			{
				string APIPath = "users/:id/accessTokens/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
		}
		public class RoleMappings : CRUDInterface<RoleMapping>
		{
			
			public static async Task<Role> getRole(string id, bool refresh = default(bool))
			{
				string APIPath = "RoleMappings/:id/role";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<RoleMapping> findByIdForRole(string id, string fk)
			{
				string APIPath = "Roles/:id/principals/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<RoleMapping>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdForRole(string id, string fk)
			{
				string APIPath = "Roles/:id/principals/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<RoleMapping> updateByIdForRole(string id, string fk, RoleMapping data)
			{
				string APIPath = "Roles/:id/principals/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<RoleMapping>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<IList<RoleMapping>> getForRole(string id, string filter = default(string))
			{
				string APIPath = "Roles/:id/principals";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<RoleMapping[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<RoleMapping> createForRole(string id, RoleMapping data)
			{
				string APIPath = "Roles/:id/principals";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<RoleMapping>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteForRole(string id)
			{
				string APIPath = "Roles/:id/principals";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countForRole(string id, string where = default(string))
			{
				string APIPath = "Roles/:id/principals/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
		}
		public class Roles : CRUDInterface<Role>
		{
			
			public static async Task<RoleMapping> findByIdPrincipals(string id, string fk)
			{
				string APIPath = "Roles/:id/principals/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<RoleMapping>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdPrincipals(string id, string fk)
			{
				string APIPath = "Roles/:id/principals/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<RoleMapping> updateByIdPrincipals(string id, string fk, RoleMapping data)
			{
				string APIPath = "Roles/:id/principals/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<RoleMapping>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<IList<RoleMapping>> getPrincipals(string id, string filter = default(string))
			{
				string APIPath = "Roles/:id/principals";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<RoleMapping[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<RoleMapping> createPrincipals(string id, RoleMapping data)
			{
				string APIPath = "Roles/:id/principals";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<RoleMapping>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deletePrincipals(string id)
			{
				string APIPath = "Roles/:id/principals";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countPrincipals(string id, string where = default(string))
			{
				string APIPath = "Roles/:id/principals/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<Role> getForRoleMapping(string id, bool refresh = default(bool))
			{
				string APIPath = "RoleMappings/:id/role";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Role> findByIdForuser(string id, string fk)
			{
				string APIPath = "users/:id/roles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdForuser(string id, string fk)
			{
				string APIPath = "users/:id/roles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Role> updateByIdForuser(string id, string fk, Role data)
			{
				string APIPath = "users/:id/roles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<RoleMapping> linkForuser(string id, string fk, RoleMapping data)
			{
				string APIPath = "users/:id/roles/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<RoleMapping>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task unlinkForuser(string id, string fk)
			{
				string APIPath = "users/:id/roles/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<bool> existsForuser(string id, string fk)
			{
				string APIPath = "users/:id/roles/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<bool>() : default(bool);
			}
			
			public static async Task<IList<Role>> getForuser(string id, string filter = default(string))
			{
				string APIPath = "users/:id/roles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Role[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Role> createForuser(string id, Role data)
			{
				string APIPath = "users/:id/roles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteForuser(string id)
			{
				string APIPath = "users/:id/roles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countForuser(string id, string where = default(string))
			{
				string APIPath = "users/:id/roles/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
		}
		public class UserCredentials : CRUDInterface<UserCredential>
		{
			
			public static async Task<User> getUser(string id, bool refresh = default(bool))
			{
				string APIPath = "userCredentials/:id/user";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<UserCredential> findByIdForuser(string id, string fk)
			{
				string APIPath = "users/:id/credentials/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdForuser(string id, string fk)
			{
				string APIPath = "users/:id/credentials/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<UserCredential> updateByIdForuser(string id, string fk, UserCredential data)
			{
				string APIPath = "users/:id/credentials/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<IList<UserCredential>> getForuser(string id, string filter = default(string))
			{
				string APIPath = "users/:id/credentials";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<UserCredential[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<UserCredential> createForuser(string id, UserCredential data)
			{
				string APIPath = "users/:id/credentials";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteForuser(string id)
			{
				string APIPath = "users/:id/credentials";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countForuser(string id, string where = default(string))
			{
				string APIPath = "users/:id/credentials/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
		}
		public class UserIdentities : CRUDInterface<UserIdentity>
		{
			
			public static async Task<User> getUser(string id, bool refresh = default(bool))
			{
				string APIPath = "userIdentities/:id/user";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<UserIdentity> findByIdForuser(string id, string fk)
			{
				string APIPath = "users/:id/identities/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdForuser(string id, string fk)
			{
				string APIPath = "users/:id/identities/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<UserIdentity> updateByIdForuser(string id, string fk, UserIdentity data)
			{
				string APIPath = "users/:id/identities/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<IList<UserIdentity>> getForuser(string id, string filter = default(string))
			{
				string APIPath = "users/:id/identities";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<UserIdentity[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<UserIdentity> createForuser(string id, UserIdentity data)
			{
				string APIPath = "users/:id/identities";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteForuser(string id)
			{
				string APIPath = "users/:id/identities";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countForuser(string id, string where = default(string))
			{
				string APIPath = "users/:id/identities/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
		}
		public class Pages : CRUDInterface<Page>
		{
			
			public static async Task<string> html(string id = default(string))
			{
				string APIPath = "pages/html";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("id", id != null ? id.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<string>() : default(string);
			}
		}
		public class Notes : CRUDInterface<Note>
		{
		}
		public class Posts : CRUDInterface<Post>
		{
		}
		public class Settings : CRUDInterface<Setting>
		{
		}
		public class Products : CRUDInterface<Product>
		{
			
			public static async Task<Category> getCategory(string id, bool refresh = default(bool))
			{
				string APIPath = "products/:id/category";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Category>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Product> findByIdForCategory(string id, string fk)
			{
				string APIPath = "categories/:id/products/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Product>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdForCategory(string id, string fk)
			{
				string APIPath = "categories/:id/products/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Product> updateByIdForCategory(string id, string fk, Product data)
			{
				string APIPath = "categories/:id/products/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Product>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<IList<Product>> getForCategory(string id, string filter = default(string))
			{
				string APIPath = "categories/:id/products";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Product[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Product> createForCategory(string id, Product data)
			{
				string APIPath = "categories/:id/products";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Product>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteForCategory(string id)
			{
				string APIPath = "categories/:id/products";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countForCategory(string id, string where = default(string))
			{
				string APIPath = "categories/:id/products/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
		}
		public class Categories : CRUDInterface<Category>
		{
			
			public static async Task<Product> findByIdProducts(string id, string fk)
			{
				string APIPath = "categories/:id/products/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Product>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdProducts(string id, string fk)
			{
				string APIPath = "categories/:id/products/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Product> updateByIdProducts(string id, string fk, Product data)
			{
				string APIPath = "categories/:id/products/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Product>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<IList<Product>> getProducts(string id, string filter = default(string))
			{
				string APIPath = "categories/:id/products";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Product[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Product> createProducts(string id, Product data)
			{
				string APIPath = "categories/:id/products";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Product>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteProducts(string id)
			{
				string APIPath = "categories/:id/products";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countProducts(string id, string where = default(string))
			{
				string APIPath = "categories/:id/products/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<Category> getForProduct(string id, bool refresh = default(bool))
			{
				string APIPath = "products/:id/category";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Category>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class Events : CRUDInterface<Event>
		{
		}
		public class AuthProviders : CRUDInterface<AuthProvider>
		{
		}
		public class Users : CRUDInterface<User>
		{
			
			public static async Task<AccessToken> findByIdAccessTokens(string id, string fk)
			{
				string APIPath = "users/:id/accessTokens/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<AccessToken>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdAccessTokens(string id, string fk)
			{
				string APIPath = "users/:id/accessTokens/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<AccessToken> updateByIdAccessTokens(string id, string fk, AccessToken data)
			{
				string APIPath = "users/:id/accessTokens/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<AccessToken>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<UserIdentity> findByIdIdentities(string id, string fk)
			{
				string APIPath = "users/:id/identities/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdIdentities(string id, string fk)
			{
				string APIPath = "users/:id/identities/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<UserIdentity> updateByIdIdentities(string id, string fk, UserIdentity data)
			{
				string APIPath = "users/:id/identities/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<UserCredential> findByIdCredentials(string id, string fk)
			{
				string APIPath = "users/:id/credentials/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdCredentials(string id, string fk)
			{
				string APIPath = "users/:id/credentials/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<UserCredential> updateByIdCredentials(string id, string fk, UserCredential data)
			{
				string APIPath = "users/:id/credentials/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Firm> getFirm(string id, bool refresh = default(bool))
			{
				string APIPath = "users/:id/firm";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Firm>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Office> getOffice(string id, bool refresh = default(bool))
			{
				string APIPath = "users/:id/office";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Office>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<MemberRole> getMemberRole(string id, bool refresh = default(bool))
			{
				string APIPath = "users/:id/memberRole";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<MemberRole>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Agreement> findByIdAgreements(string id, string fk)
			{
				string APIPath = "users/:id/agreements/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Agreement>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdAgreements(string id, string fk)
			{
				string APIPath = "users/:id/agreements/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Agreement> updateByIdAgreements(string id, string fk, Agreement data)
			{
				string APIPath = "users/:id/agreements/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Agreement>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<MemberRole> linkAgreements(string id, string fk, MemberRole data)
			{
				string APIPath = "users/:id/agreements/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<MemberRole>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task unlinkAgreements(string id, string fk)
			{
				string APIPath = "users/:id/agreements/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<bool> existsAgreements(string id, string fk)
			{
				string APIPath = "users/:id/agreements/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<bool>() : default(bool);
			}
			
			public static async Task<Notification> findByIdNotifications(string id, string fk)
			{
				string APIPath = "users/:id/notifications/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Notification>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdNotifications(string id, string fk)
			{
				string APIPath = "users/:id/notifications/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Notification> updateByIdNotifications(string id, string fk, Notification data)
			{
				string APIPath = "users/:id/notifications/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Notification>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Role> findByIdRoles(string id, string fk)
			{
				string APIPath = "users/:id/roles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdRoles(string id, string fk)
			{
				string APIPath = "users/:id/roles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Role> updateByIdRoles(string id, string fk, Role data)
			{
				string APIPath = "users/:id/roles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<RoleMapping> linkRoles(string id, string fk, RoleMapping data)
			{
				string APIPath = "users/:id/roles/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<RoleMapping>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task unlinkRoles(string id, string fk)
			{
				string APIPath = "users/:id/roles/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<bool> existsRoles(string id, string fk)
			{
				string APIPath = "users/:id/roles/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<bool>() : default(bool);
			}
			
			public static async Task<IList<AccessToken>> getAccessTokens(string id, string filter = default(string))
			{
				string APIPath = "users/:id/accessTokens";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<AccessToken[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<AccessToken> createAccessTokens(string id, AccessToken data)
			{
				string APIPath = "users/:id/accessTokens";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<AccessToken>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteAccessTokens(string id)
			{
				string APIPath = "users/:id/accessTokens";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countAccessTokens(string id, string where = default(string))
			{
				string APIPath = "users/:id/accessTokens/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<IList<UserIdentity>> getIdentities(string id, string filter = default(string))
			{
				string APIPath = "users/:id/identities";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<UserIdentity[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<UserIdentity> createIdentities(string id, UserIdentity data)
			{
				string APIPath = "users/:id/identities";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteIdentities(string id)
			{
				string APIPath = "users/:id/identities";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countIdentities(string id, string where = default(string))
			{
				string APIPath = "users/:id/identities/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<IList<UserCredential>> getCredentials(string id, string filter = default(string))
			{
				string APIPath = "users/:id/credentials";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<UserCredential[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<UserCredential> createCredentials(string id, UserCredential data)
			{
				string APIPath = "users/:id/credentials";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteCredentials(string id)
			{
				string APIPath = "users/:id/credentials";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countCredentials(string id, string where = default(string))
			{
				string APIPath = "users/:id/credentials/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<IList<Agreement>> getAgreements(string id, string filter = default(string))
			{
				string APIPath = "users/:id/agreements";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Agreement[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Agreement> createAgreements(string id, Agreement data)
			{
				string APIPath = "users/:id/agreements";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Agreement>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteAgreements(string id)
			{
				string APIPath = "users/:id/agreements";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countAgreements(string id, string where = default(string))
			{
				string APIPath = "users/:id/agreements/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<IList<Notification>> getNotifications(string id, string filter = default(string))
			{
				string APIPath = "users/:id/notifications";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Notification[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Notification> createNotifications(string id, Notification data)
			{
				string APIPath = "users/:id/notifications";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Notification>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteNotifications(string id)
			{
				string APIPath = "users/:id/notifications";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countNotifications(string id, string where = default(string))
			{
				string APIPath = "users/:id/notifications/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<IList<Role>> getRoles(string id, string filter = default(string))
			{
				string APIPath = "users/:id/roles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Role[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Role> createRoles(string id, Role data)
			{
				string APIPath = "users/:id/roles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteRoles(string id)
			{
				string APIPath = "users/:id/roles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countRoles(string id, string where = default(string))
			{
				string APIPath = "users/:id/roles/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<JObject> login(User credentials, string include = default(string))
			{
				string APIPath = "users/login";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(credentials);
				queryStrings.Add("include", include != null ? include.ToString() : null);
				var response = await Gateway.PerformRequest<JObject>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task logout()
			{
				string APIPath = "users/logout";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task confirm(string uid = default(string), string token = default(string), string redirect = default(string))
			{
				string APIPath = "users/confirm";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("uid", uid != null ? uid.ToString() : null);
				queryStrings.Add("token", token != null ? token.ToString() : null);
				queryStrings.Add("redirect", redirect != null ? redirect.ToString() : null);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task resetPassword(User options)
			{
				string APIPath = "users/reset";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(options);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<User> getForaccessToken(string id, bool refresh = default(bool))
			{
				string APIPath = "accessTokens/:id/user";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<User> getForuserCredential(string id, bool refresh = default(bool))
			{
				string APIPath = "userCredentials/:id/user";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<User> getForuserIdentity(string id, bool refresh = default(bool))
			{
				string APIPath = "userIdentities/:id/user";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<User> findByIdForFirm(string id, string fk)
			{
				string APIPath = "Firms/:id/members/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdForFirm(string id, string fk)
			{
				string APIPath = "Firms/:id/members/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<User> updateByIdForFirm(string id, string fk, User data)
			{
				string APIPath = "Firms/:id/members/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Office> linkForFirm(string id, string fk, Office data)
			{
				string APIPath = "Firms/:id/members/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Office>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task unlinkForFirm(string id, string fk)
			{
				string APIPath = "Firms/:id/members/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<bool> existsForFirm(string id, string fk)
			{
				string APIPath = "Firms/:id/members/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<bool>() : default(bool);
			}
			
			public static async Task<IList<User>> getForFirm(string id, string filter = default(string))
			{
				string APIPath = "Firms/:id/members";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<User[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<User> createForFirm(string id, User data)
			{
				string APIPath = "Firms/:id/members";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteForFirm(string id)
			{
				string APIPath = "Firms/:id/members";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countForFirm(string id, string where = default(string))
			{
				string APIPath = "Firms/:id/members/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<User> findByIdForOffice(string id, string fk)
			{
				string APIPath = "Offices/:id/members/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdForOffice(string id, string fk)
			{
				string APIPath = "Offices/:id/members/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<User> updateByIdForOffice(string id, string fk, User data)
			{
				string APIPath = "Offices/:id/members/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<IList<User>> getForOffice(string id, string filter = default(string))
			{
				string APIPath = "Offices/:id/members";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<User[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<User> createForOffice(string id, User data)
			{
				string APIPath = "Offices/:id/members";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteForOffice(string id)
			{
				string APIPath = "Offices/:id/members";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countForOffice(string id, string where = default(string))
			{
				string APIPath = "Offices/:id/members/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<User> getForAgreement(string id, bool refresh = default(bool))
			{
				string APIPath = "Agreements/:id/memberA";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<User> getForAgreement1(string id, bool refresh = default(bool))
			{
				string APIPath = "Agreements/:id/memberB";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<User> getForFiling(string id, bool refresh = default(bool))
			{
				string APIPath = "Filings/:id/contactA";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<User> getForFiling1(string id, bool refresh = default(bool))
			{
				string APIPath = "Filings/:id/contactB";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<User> getForInvoice(string id, bool refresh = default(bool))
			{
				string APIPath = "Invoices/:id/reciever";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<User> getForNotification(string id, bool refresh = default(bool))
			{
				string APIPath = "Notifications/:id/receiver";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<User> getForNotification1(string id, bool refresh = default(bool))
			{
				string APIPath = "Notifications/:id/sender";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class Firms : CRUDInterface<Firm>
		{
			
			public static async Task<Office> findByIdOffice(string id, string fk)
			{
				string APIPath = "Firms/:id/office/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Office>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdOffice(string id, string fk)
			{
				string APIPath = "Firms/:id/office/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Office> updateByIdOffice(string id, string fk, Office data)
			{
				string APIPath = "Firms/:id/office/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Office>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<User> findByIdMembers(string id, string fk)
			{
				string APIPath = "Firms/:id/members/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdMembers(string id, string fk)
			{
				string APIPath = "Firms/:id/members/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<User> updateByIdMembers(string id, string fk, User data)
			{
				string APIPath = "Firms/:id/members/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Office> linkMembers(string id, string fk, Office data)
			{
				string APIPath = "Firms/:id/members/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Office>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task unlinkMembers(string id, string fk)
			{
				string APIPath = "Firms/:id/members/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<bool> existsMembers(string id, string fk)
			{
				string APIPath = "Firms/:id/members/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<bool>() : default(bool);
			}
			
			public static async Task<Agreement> findByIdAgreements(string id, string fk)
			{
				string APIPath = "Firms/:id/agreements/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Agreement>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdAgreements(string id, string fk)
			{
				string APIPath = "Firms/:id/agreements/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Agreement> updateByIdAgreements(string id, string fk, Agreement data)
			{
				string APIPath = "Firms/:id/agreements/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Agreement>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<IList<Office>> getOffice(string id, string filter = default(string))
			{
				string APIPath = "Firms/:id/office";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Office[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Office> createOffice(string id, Office data)
			{
				string APIPath = "Firms/:id/office";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Office>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteOffice(string id)
			{
				string APIPath = "Firms/:id/office";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countOffice(string id, string where = default(string))
			{
				string APIPath = "Firms/:id/office/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<IList<User>> getMembers(string id, string filter = default(string))
			{
				string APIPath = "Firms/:id/members";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<User[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<User> createMembers(string id, User data)
			{
				string APIPath = "Firms/:id/members";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteMembers(string id)
			{
				string APIPath = "Firms/:id/members";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countMembers(string id, string where = default(string))
			{
				string APIPath = "Firms/:id/members/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<IList<Agreement>> getAgreements(string id, string filter = default(string))
			{
				string APIPath = "Firms/:id/agreements";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Agreement[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Agreement> createAgreements(string id, Agreement data)
			{
				string APIPath = "Firms/:id/agreements";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Agreement>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteAgreements(string id)
			{
				string APIPath = "Firms/:id/agreements";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countAgreements(string id, string where = default(string))
			{
				string APIPath = "Firms/:id/agreements/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<Firm> getForuser(string id, bool refresh = default(bool))
			{
				string APIPath = "users/:id/firm";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Firm>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Firm> getForOffice(string id, bool refresh = default(bool))
			{
				string APIPath = "Offices/:id/firm";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Firm>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Firm> getForAgreement(string id, bool refresh = default(bool))
			{
				string APIPath = "Agreements/:id/firmA";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Firm>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Firm> getForAgreement1(string id, bool refresh = default(bool))
			{
				string APIPath = "Agreements/:id/firmB";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Firm>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class Offices : CRUDInterface<Office>
		{
			
			public static async Task<Firm> getFirm(string id, bool refresh = default(bool))
			{
				string APIPath = "Offices/:id/firm";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Firm>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<User> findByIdMembers(string id, string fk)
			{
				string APIPath = "Offices/:id/members/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdMembers(string id, string fk)
			{
				string APIPath = "Offices/:id/members/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<User> updateByIdMembers(string id, string fk, User data)
			{
				string APIPath = "Offices/:id/members/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Agreement> findByIdAgreements(string id, string fk)
			{
				string APIPath = "Offices/:id/agreements/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Agreement>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdAgreements(string id, string fk)
			{
				string APIPath = "Offices/:id/agreements/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Agreement> updateByIdAgreements(string id, string fk, Agreement data)
			{
				string APIPath = "Offices/:id/agreements/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Agreement>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<FilingGroup> getFilingGroup(string id, bool refresh = default(bool))
			{
				string APIPath = "Offices/:id/filingGroup";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<FilingGroup>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<IList<User>> getMembers(string id, string filter = default(string))
			{
				string APIPath = "Offices/:id/members";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<User[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<User> createMembers(string id, User data)
			{
				string APIPath = "Offices/:id/members";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteMembers(string id)
			{
				string APIPath = "Offices/:id/members";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countMembers(string id, string where = default(string))
			{
				string APIPath = "Offices/:id/members/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<IList<Agreement>> getAgreements(string id, string filter = default(string))
			{
				string APIPath = "Offices/:id/agreements";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Agreement[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Agreement> createAgreements(string id, Agreement data)
			{
				string APIPath = "Offices/:id/agreements";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Agreement>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteAgreements(string id)
			{
				string APIPath = "Offices/:id/agreements";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countAgreements(string id, string where = default(string))
			{
				string APIPath = "Offices/:id/agreements/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<Office> getForuser(string id, bool refresh = default(bool))
			{
				string APIPath = "users/:id/office";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Office>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Office> findByIdForFirm(string id, string fk)
			{
				string APIPath = "Firms/:id/office/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Office>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdForFirm(string id, string fk)
			{
				string APIPath = "Firms/:id/office/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Office> updateByIdForFirm(string id, string fk, Office data)
			{
				string APIPath = "Firms/:id/office/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Office>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<IList<Office>> getForFirm(string id, string filter = default(string))
			{
				string APIPath = "Firms/:id/office";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Office[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Office> createForFirm(string id, Office data)
			{
				string APIPath = "Firms/:id/office";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Office>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteForFirm(string id)
			{
				string APIPath = "Firms/:id/office";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countForFirm(string id, string where = default(string))
			{
				string APIPath = "Firms/:id/office/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<Office> getForAgreement(string id, bool refresh = default(bool))
			{
				string APIPath = "Agreements/:id/officeA";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Office>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Office> getForAgreement1(string id, bool refresh = default(bool))
			{
				string APIPath = "Agreements/:id/officeB";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Office>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Office> getForFilingGroup(string id, bool refresh = default(bool))
			{
				string APIPath = "FilingGroups/:id/office";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Office>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class MemberRoles : CRUDInterface<MemberRole>
		{
			
			public static async Task<MemberRole> getForuser(string id, bool refresh = default(bool))
			{
				string APIPath = "users/:id/memberRole";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<MemberRole>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class Agreements : CRUDInterface<Agreement>
		{
			
			public static async Task<Firm> getFirmA(string id, bool refresh = default(bool))
			{
				string APIPath = "Agreements/:id/firmA";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Firm>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Firm> getFirmB(string id, bool refresh = default(bool))
			{
				string APIPath = "Agreements/:id/firmB";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Firm>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Office> getOfficeA(string id, bool refresh = default(bool))
			{
				string APIPath = "Agreements/:id/officeA";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Office>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Office> getOfficeB(string id, bool refresh = default(bool))
			{
				string APIPath = "Agreements/:id/officeB";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Office>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<User> getMemberA(string id, bool refresh = default(bool))
			{
				string APIPath = "Agreements/:id/memberA";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<User> getMemberB(string id, bool refresh = default(bool))
			{
				string APIPath = "Agreements/:id/memberB";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<AgreementType> getAgreementType(string id, bool refresh = default(bool))
			{
				string APIPath = "Agreements/:id/agreementType";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<AgreementType>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Status> getStatus(string id, bool refresh = default(bool))
			{
				string APIPath = "Agreements/:id/status";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Status>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<FilingType> getFilingTypeA(string id, bool refresh = default(bool))
			{
				string APIPath = "Agreements/:id/filingTypeA";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<FilingType>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<FilingType> getFilingTypeB(string id, bool refresh = default(bool))
			{
				string APIPath = "Agreements/:id/filingTypeB";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<FilingType>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Filing> findByIdFilings(string id, string fk)
			{
				string APIPath = "Agreements/:id/filings/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Filing>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdFilings(string id, string fk)
			{
				string APIPath = "Agreements/:id/filings/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Filing> updateByIdFilings(string id, string fk, Filing data)
			{
				string APIPath = "Agreements/:id/filings/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Filing>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Comment> findByIdComments(string id, string fk)
			{
				string APIPath = "Agreements/:id/comments/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Comment>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdComments(string id, string fk)
			{
				string APIPath = "Agreements/:id/comments/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Comment> updateByIdComments(string id, string fk, Comment data)
			{
				string APIPath = "Agreements/:id/comments/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Comment>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Invoice> findByIdInvoices(string id, string fk)
			{
				string APIPath = "Agreements/:id/invoices/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Invoice>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdInvoices(string id, string fk)
			{
				string APIPath = "Agreements/:id/invoices/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Invoice> updateByIdInvoices(string id, string fk, Invoice data)
			{
				string APIPath = "Agreements/:id/invoices/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Invoice>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Rate> getRateA(string id, bool refresh = default(bool))
			{
				string APIPath = "Agreements/:id/rateA";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Rate>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Rate> getRateB(string id, bool refresh = default(bool))
			{
				string APIPath = "Agreements/:id/rateB";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Rate>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<IList<Filing>> getFilings(string id, string filter = default(string))
			{
				string APIPath = "Agreements/:id/filings";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Filing[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Filing> createFilings(string id, Filing data)
			{
				string APIPath = "Agreements/:id/filings";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Filing>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteFilings(string id)
			{
				string APIPath = "Agreements/:id/filings";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countFilings(string id, string where = default(string))
			{
				string APIPath = "Agreements/:id/filings/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<IList<Comment>> getComments(string id, string filter = default(string))
			{
				string APIPath = "Agreements/:id/comments";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Comment[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Comment> createComments(string id, Comment data)
			{
				string APIPath = "Agreements/:id/comments";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Comment>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteComments(string id)
			{
				string APIPath = "Agreements/:id/comments";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countComments(string id, string where = default(string))
			{
				string APIPath = "Agreements/:id/comments/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<IList<Invoice>> getInvoices(string id, string filter = default(string))
			{
				string APIPath = "Agreements/:id/invoices";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Invoice[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Invoice> createInvoices(string id, Invoice data)
			{
				string APIPath = "Agreements/:id/invoices";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Invoice>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteInvoices(string id)
			{
				string APIPath = "Agreements/:id/invoices";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countInvoices(string id, string where = default(string))
			{
				string APIPath = "Agreements/:id/invoices/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<Agreement> findByIdForuser(string id, string fk)
			{
				string APIPath = "users/:id/agreements/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Agreement>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdForuser(string id, string fk)
			{
				string APIPath = "users/:id/agreements/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Agreement> updateByIdForuser(string id, string fk, Agreement data)
			{
				string APIPath = "users/:id/agreements/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Agreement>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<MemberRole> linkForuser(string id, string fk, MemberRole data)
			{
				string APIPath = "users/:id/agreements/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<MemberRole>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task unlinkForuser(string id, string fk)
			{
				string APIPath = "users/:id/agreements/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<bool> existsForuser(string id, string fk)
			{
				string APIPath = "users/:id/agreements/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<bool>() : default(bool);
			}
			
			public static async Task<IList<Agreement>> getForuser(string id, string filter = default(string))
			{
				string APIPath = "users/:id/agreements";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Agreement[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Agreement> createForuser(string id, Agreement data)
			{
				string APIPath = "users/:id/agreements";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Agreement>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteForuser(string id)
			{
				string APIPath = "users/:id/agreements";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countForuser(string id, string where = default(string))
			{
				string APIPath = "users/:id/agreements/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<Agreement> findByIdForFirm(string id, string fk)
			{
				string APIPath = "Firms/:id/agreements/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Agreement>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdForFirm(string id, string fk)
			{
				string APIPath = "Firms/:id/agreements/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Agreement> updateByIdForFirm(string id, string fk, Agreement data)
			{
				string APIPath = "Firms/:id/agreements/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Agreement>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<IList<Agreement>> getForFirm(string id, string filter = default(string))
			{
				string APIPath = "Firms/:id/agreements";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Agreement[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Agreement> createForFirm(string id, Agreement data)
			{
				string APIPath = "Firms/:id/agreements";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Agreement>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteForFirm(string id)
			{
				string APIPath = "Firms/:id/agreements";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countForFirm(string id, string where = default(string))
			{
				string APIPath = "Firms/:id/agreements/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<Agreement> findByIdForOffice(string id, string fk)
			{
				string APIPath = "Offices/:id/agreements/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Agreement>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdForOffice(string id, string fk)
			{
				string APIPath = "Offices/:id/agreements/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Agreement> updateByIdForOffice(string id, string fk, Agreement data)
			{
				string APIPath = "Offices/:id/agreements/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Agreement>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<IList<Agreement>> getForOffice(string id, string filter = default(string))
			{
				string APIPath = "Offices/:id/agreements";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Agreement[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Agreement> createForOffice(string id, Agreement data)
			{
				string APIPath = "Offices/:id/agreements";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Agreement>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteForOffice(string id)
			{
				string APIPath = "Offices/:id/agreements";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countForOffice(string id, string where = default(string))
			{
				string APIPath = "Offices/:id/agreements/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<Agreement> getForFiling(string id, bool refresh = default(bool))
			{
				string APIPath = "Filings/:id/agreement";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Agreement>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Agreement> getForInvoice(string id, bool refresh = default(bool))
			{
				string APIPath = "Invoices/:id/agreement";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Agreement>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Agreement> findByIdForRate(string id, string fk)
			{
				string APIPath = "Rates/:id/agreements/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Agreement>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdForRate(string id, string fk)
			{
				string APIPath = "Rates/:id/agreements/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Agreement> updateByIdForRate(string id, string fk, Agreement data)
			{
				string APIPath = "Rates/:id/agreements/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Agreement>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<RateAgreement> linkForRate(string id, string fk, RateAgreement data)
			{
				string APIPath = "Rates/:id/agreements/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<RateAgreement>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task unlinkForRate(string id, string fk)
			{
				string APIPath = "Rates/:id/agreements/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<bool> existsForRate(string id, string fk)
			{
				string APIPath = "Rates/:id/agreements/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<bool>() : default(bool);
			}
			
			public static async Task<IList<Agreement>> getForRate(string id, string filter = default(string))
			{
				string APIPath = "Rates/:id/agreements";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Agreement[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Agreement> createForRate(string id, Agreement data)
			{
				string APIPath = "Rates/:id/agreements";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Agreement>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteForRate(string id)
			{
				string APIPath = "Rates/:id/agreements";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countForRate(string id, string where = default(string))
			{
				string APIPath = "Rates/:id/agreements/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
		}
		public class AgreementTypes : CRUDInterface<AgreementType>
		{
			
			public static async Task<AgreementType> getForAgreement(string id, bool refresh = default(bool))
			{
				string APIPath = "Agreements/:id/agreementType";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<AgreementType>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class Statuses : CRUDInterface<Status>
		{
			
			public static async Task<Status> getForAgreement(string id, bool refresh = default(bool))
			{
				string APIPath = "Agreements/:id/status";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Status>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Status> getForFiling(string id, bool refresh = default(bool))
			{
				string APIPath = "Filings/:id/status";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Status>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Status> getForInvoice(string id, bool refresh = default(bool))
			{
				string APIPath = "Invoices/:id/status";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Status>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class Countries : CRUDInterface<Country>
		{
			
			public static async Task<Country> getForFilingSet(string id, bool refresh = default(bool))
			{
				string APIPath = "FilingSets/:id/country";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Country>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class Currencies : CRUDInterface<Currency>
		{
			
			public static async Task<Currency> getForRate(string id, bool refresh = default(bool))
			{
				string APIPath = "Rates/:id/currencies";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Currency>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class Specialities : CRUDInterface<Specialty>
		{
			
			public static async Task<Specialty> findByIdForFilingGroup(string id, string fk)
			{
				string APIPath = "FilingGroups/:id/specialties/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Specialty>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdForFilingGroup(string id, string fk)
			{
				string APIPath = "FilingGroups/:id/specialties/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Specialty> updateByIdForFilingGroup(string id, string fk, Specialty data)
			{
				string APIPath = "FilingGroups/:id/specialties/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Specialty>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<FilingGroupSpecialty> linkForFilingGroup(string id, string fk, FilingGroupSpecialty data)
			{
				string APIPath = "FilingGroups/:id/specialties/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<FilingGroupSpecialty>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task unlinkForFilingGroup(string id, string fk)
			{
				string APIPath = "FilingGroups/:id/specialties/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<bool> existsForFilingGroup(string id, string fk)
			{
				string APIPath = "FilingGroups/:id/specialties/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<bool>() : default(bool);
			}
			
			public static async Task<IList<Specialty>> getForFilingGroup(string id, string filter = default(string))
			{
				string APIPath = "FilingGroups/:id/specialties";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Specialty[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Specialty> createForFilingGroup(string id, Specialty data)
			{
				string APIPath = "FilingGroups/:id/specialties";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Specialty>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteForFilingGroup(string id)
			{
				string APIPath = "FilingGroups/:id/specialties";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countForFilingGroup(string id, string where = default(string))
			{
				string APIPath = "FilingGroups/:id/specialties/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
		}
		public class FilingTypes : CRUDInterface<FilingType>
		{
			
			public static async Task<FilingType> getForAgreement(string id, bool refresh = default(bool))
			{
				string APIPath = "Agreements/:id/filingTypeA";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<FilingType>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<FilingType> getForAgreement1(string id, bool refresh = default(bool))
			{
				string APIPath = "Agreements/:id/filingTypeB";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<FilingType>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<FilingType> getForFiling(string id, bool refresh = default(bool))
			{
				string APIPath = "Filings/:id/filingTypes";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<FilingType>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<FilingType> getForFilingSet(string id, bool refresh = default(bool))
			{
				string APIPath = "FilingSets/:id/outFilingType";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<FilingType>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<FilingType> getForFilingSet1(string id, bool refresh = default(bool))
			{
				string APIPath = "FilingSets/:id/inFilingType";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<FilingType>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class Filings : CRUDInterface<Filing>
		{
			
			public static async Task<Agreement> getAgreement(string id, bool refresh = default(bool))
			{
				string APIPath = "Filings/:id/agreement";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Agreement>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<FilingType> getFilingTypes(string id, bool refresh = default(bool))
			{
				string APIPath = "Filings/:id/filingTypes";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<FilingType>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<User> getContactA(string id, bool refresh = default(bool))
			{
				string APIPath = "Filings/:id/contactA";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<User> getContactB(string id, bool refresh = default(bool))
			{
				string APIPath = "Filings/:id/contactB";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Status> getStatus(string id, bool refresh = default(bool))
			{
				string APIPath = "Filings/:id/status";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Status>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Comment> findByIdComments(string id, string fk)
			{
				string APIPath = "Filings/:id/comments/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Comment>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdComments(string id, string fk)
			{
				string APIPath = "Filings/:id/comments/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Comment> updateByIdComments(string id, string fk, Comment data)
			{
				string APIPath = "Filings/:id/comments/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Comment>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Version> getVersion(string id, bool refresh = default(bool))
			{
				string APIPath = "Filings/:id/version";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Version>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<UploadedFile> findByIdUploadedFiles(string id, string fk)
			{
				string APIPath = "Filings/:id/uploadedFiles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UploadedFile>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdUploadedFiles(string id, string fk)
			{
				string APIPath = "Filings/:id/uploadedFiles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<UploadedFile> updateByIdUploadedFiles(string id, string fk, UploadedFile data)
			{
				string APIPath = "Filings/:id/uploadedFiles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<UploadedFile>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<IList<Comment>> getComments(string id, string filter = default(string))
			{
				string APIPath = "Filings/:id/comments";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Comment[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Comment> createComments(string id, Comment data)
			{
				string APIPath = "Filings/:id/comments";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Comment>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteComments(string id)
			{
				string APIPath = "Filings/:id/comments";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countComments(string id, string where = default(string))
			{
				string APIPath = "Filings/:id/comments/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<IList<UploadedFile>> getUploadedFiles(string id, string filter = default(string))
			{
				string APIPath = "Filings/:id/uploadedFiles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<UploadedFile[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<UploadedFile> createUploadedFiles(string id, UploadedFile data)
			{
				string APIPath = "Filings/:id/uploadedFiles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<UploadedFile>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteUploadedFiles(string id)
			{
				string APIPath = "Filings/:id/uploadedFiles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countUploadedFiles(string id, string where = default(string))
			{
				string APIPath = "Filings/:id/uploadedFiles/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<Filing> findByIdForAgreement(string id, string fk)
			{
				string APIPath = "Agreements/:id/filings/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Filing>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdForAgreement(string id, string fk)
			{
				string APIPath = "Agreements/:id/filings/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Filing> updateByIdForAgreement(string id, string fk, Filing data)
			{
				string APIPath = "Agreements/:id/filings/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Filing>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<IList<Filing>> getForAgreement(string id, string filter = default(string))
			{
				string APIPath = "Agreements/:id/filings";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Filing[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Filing> createForAgreement(string id, Filing data)
			{
				string APIPath = "Agreements/:id/filings";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Filing>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteForAgreement(string id)
			{
				string APIPath = "Agreements/:id/filings";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countForAgreement(string id, string where = default(string))
			{
				string APIPath = "Agreements/:id/filings/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<Filing> getForVersion(string id, bool refresh = default(bool))
			{
				string APIPath = "Versions/:id/previousFiling";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Filing>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Filing> getForVersion1(string id, bool refresh = default(bool))
			{
				string APIPath = "Versions/:id/updatedFiling";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Filing>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Filing> getForUploadedFile(string id, bool refresh = default(bool))
			{
				string APIPath = "UploadedFiles/:id/filing";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Filing>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class Comments : CRUDInterface<Comment>
		{
			
			public static async Task<Comment> findByIdForAgreement(string id, string fk)
			{
				string APIPath = "Agreements/:id/comments/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Comment>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdForAgreement(string id, string fk)
			{
				string APIPath = "Agreements/:id/comments/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Comment> updateByIdForAgreement(string id, string fk, Comment data)
			{
				string APIPath = "Agreements/:id/comments/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Comment>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<IList<Comment>> getForAgreement(string id, string filter = default(string))
			{
				string APIPath = "Agreements/:id/comments";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Comment[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Comment> createForAgreement(string id, Comment data)
			{
				string APIPath = "Agreements/:id/comments";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Comment>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteForAgreement(string id)
			{
				string APIPath = "Agreements/:id/comments";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countForAgreement(string id, string where = default(string))
			{
				string APIPath = "Agreements/:id/comments/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<Comment> findByIdForFiling(string id, string fk)
			{
				string APIPath = "Filings/:id/comments/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Comment>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdForFiling(string id, string fk)
			{
				string APIPath = "Filings/:id/comments/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Comment> updateByIdForFiling(string id, string fk, Comment data)
			{
				string APIPath = "Filings/:id/comments/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Comment>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<IList<Comment>> getForFiling(string id, string filter = default(string))
			{
				string APIPath = "Filings/:id/comments";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Comment[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Comment> createForFiling(string id, Comment data)
			{
				string APIPath = "Filings/:id/comments";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Comment>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteForFiling(string id)
			{
				string APIPath = "Filings/:id/comments";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countForFiling(string id, string where = default(string))
			{
				string APIPath = "Filings/:id/comments/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<Comment> findByIdForInvoice(string id, string fk)
			{
				string APIPath = "Invoices/:id/comments/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Comment>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdForInvoice(string id, string fk)
			{
				string APIPath = "Invoices/:id/comments/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Comment> updateByIdForInvoice(string id, string fk, Comment data)
			{
				string APIPath = "Invoices/:id/comments/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Comment>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<IList<Comment>> getForInvoice(string id, string filter = default(string))
			{
				string APIPath = "Invoices/:id/comments";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Comment[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Comment> createForInvoice(string id, Comment data)
			{
				string APIPath = "Invoices/:id/comments";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Comment>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteForInvoice(string id)
			{
				string APIPath = "Invoices/:id/comments";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countForInvoice(string id, string where = default(string))
			{
				string APIPath = "Invoices/:id/comments/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
		}
		public class Invoices : CRUDInterface<Invoice>
		{
			
			public static async Task<Agreement> getAgreement(string id, bool refresh = default(bool))
			{
				string APIPath = "Invoices/:id/agreement";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Agreement>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Status> getStatus(string id, bool refresh = default(bool))
			{
				string APIPath = "Invoices/:id/status";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Status>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Comment> findByIdComments(string id, string fk)
			{
				string APIPath = "Invoices/:id/comments/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Comment>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdComments(string id, string fk)
			{
				string APIPath = "Invoices/:id/comments/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Comment> updateByIdComments(string id, string fk, Comment data)
			{
				string APIPath = "Invoices/:id/comments/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Comment>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<User> getReciever(string id, bool refresh = default(bool))
			{
				string APIPath = "Invoices/:id/reciever";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<IList<Comment>> getComments(string id, string filter = default(string))
			{
				string APIPath = "Invoices/:id/comments";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Comment[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Comment> createComments(string id, Comment data)
			{
				string APIPath = "Invoices/:id/comments";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Comment>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteComments(string id)
			{
				string APIPath = "Invoices/:id/comments";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countComments(string id, string where = default(string))
			{
				string APIPath = "Invoices/:id/comments/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<Invoice> findByIdForAgreement(string id, string fk)
			{
				string APIPath = "Agreements/:id/invoices/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Invoice>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdForAgreement(string id, string fk)
			{
				string APIPath = "Agreements/:id/invoices/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Invoice> updateByIdForAgreement(string id, string fk, Invoice data)
			{
				string APIPath = "Agreements/:id/invoices/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Invoice>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<IList<Invoice>> getForAgreement(string id, string filter = default(string))
			{
				string APIPath = "Agreements/:id/invoices";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Invoice[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Invoice> createForAgreement(string id, Invoice data)
			{
				string APIPath = "Agreements/:id/invoices";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Invoice>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteForAgreement(string id)
			{
				string APIPath = "Agreements/:id/invoices";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countForAgreement(string id, string where = default(string))
			{
				string APIPath = "Agreements/:id/invoices/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
		}
		public class Notifications : CRUDInterface<Notification>
		{
			
			public static async Task<User> getReceiver(string id, bool refresh = default(bool))
			{
				string APIPath = "Notifications/:id/receiver";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<User> getSender(string id, bool refresh = default(bool))
			{
				string APIPath = "Notifications/:id/sender";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<NotificationType> getNotificationType(string id, bool refresh = default(bool))
			{
				string APIPath = "Notifications/:id/notificationType";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<NotificationType>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Notification> findByIdForuser(string id, string fk)
			{
				string APIPath = "users/:id/notifications/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Notification>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdForuser(string id, string fk)
			{
				string APIPath = "users/:id/notifications/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Notification> updateByIdForuser(string id, string fk, Notification data)
			{
				string APIPath = "users/:id/notifications/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Notification>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<IList<Notification>> getForuser(string id, string filter = default(string))
			{
				string APIPath = "users/:id/notifications";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Notification[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Notification> createForuser(string id, Notification data)
			{
				string APIPath = "users/:id/notifications";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Notification>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteForuser(string id)
			{
				string APIPath = "users/:id/notifications";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countForuser(string id, string where = default(string))
			{
				string APIPath = "users/:id/notifications/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
		}
		public class Rates : CRUDInterface<Rate>
		{
			
			public static async Task<Agreement> findByIdAgreements(string id, string fk)
			{
				string APIPath = "Rates/:id/agreements/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Agreement>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdAgreements(string id, string fk)
			{
				string APIPath = "Rates/:id/agreements/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Agreement> updateByIdAgreements(string id, string fk, Agreement data)
			{
				string APIPath = "Rates/:id/agreements/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Agreement>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<RateAgreement> linkAgreements(string id, string fk, RateAgreement data)
			{
				string APIPath = "Rates/:id/agreements/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<RateAgreement>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task unlinkAgreements(string id, string fk)
			{
				string APIPath = "Rates/:id/agreements/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<bool> existsAgreements(string id, string fk)
			{
				string APIPath = "Rates/:id/agreements/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<bool>() : default(bool);
			}
			
			public static async Task<Currency> getCurrencies(string id, bool refresh = default(bool))
			{
				string APIPath = "Rates/:id/currencies";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Currency>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<IList<Agreement>> getAgreements(string id, string filter = default(string))
			{
				string APIPath = "Rates/:id/agreements";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Agreement[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Agreement> createAgreements(string id, Agreement data)
			{
				string APIPath = "Rates/:id/agreements";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Agreement>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteAgreements(string id)
			{
				string APIPath = "Rates/:id/agreements";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countAgreements(string id, string where = default(string))
			{
				string APIPath = "Rates/:id/agreements/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<Rate> getForAgreement(string id, bool refresh = default(bool))
			{
				string APIPath = "Agreements/:id/rateA";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Rate>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Rate> getForAgreement1(string id, bool refresh = default(bool))
			{
				string APIPath = "Agreements/:id/rateB";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Rate>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Rate> getForFilingGroup(string id, bool refresh = default(bool))
			{
				string APIPath = "FilingGroups/:id/rate";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Rate>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class FilingSets : CRUDInterface<FilingSet>
		{
			
			public static async Task<Country> getCountry(string id, bool refresh = default(bool))
			{
				string APIPath = "FilingSets/:id/country";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Country>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<FilingType> getOutFilingType(string id, bool refresh = default(bool))
			{
				string APIPath = "FilingSets/:id/outFilingType";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<FilingType>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<FilingType> getInFilingType(string id, bool refresh = default(bool))
			{
				string APIPath = "FilingSets/:id/inFilingType";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<FilingType>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<FilingSet> findByIdForFilingGroup(string id, string fk)
			{
				string APIPath = "FilingGroups/:id/filingSets/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<FilingSet>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdForFilingGroup(string id, string fk)
			{
				string APIPath = "FilingGroups/:id/filingSets/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<FilingSet> updateByIdForFilingGroup(string id, string fk, FilingSet data)
			{
				string APIPath = "FilingGroups/:id/filingSets/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<FilingSet>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<FilingGroupFilingSet> linkForFilingGroup(string id, string fk, FilingGroupFilingSet data)
			{
				string APIPath = "FilingGroups/:id/filingSets/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<FilingGroupFilingSet>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task unlinkForFilingGroup(string id, string fk)
			{
				string APIPath = "FilingGroups/:id/filingSets/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<bool> existsForFilingGroup(string id, string fk)
			{
				string APIPath = "FilingGroups/:id/filingSets/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<bool>() : default(bool);
			}
			
			public static async Task<IList<FilingSet>> getForFilingGroup(string id, string filter = default(string))
			{
				string APIPath = "FilingGroups/:id/filingSets";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<FilingSet[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<FilingSet> createForFilingGroup(string id, FilingSet data)
			{
				string APIPath = "FilingGroups/:id/filingSets";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<FilingSet>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteForFilingGroup(string id)
			{
				string APIPath = "FilingGroups/:id/filingSets";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countForFilingGroup(string id, string where = default(string))
			{
				string APIPath = "FilingGroups/:id/filingSets/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
		}
		public class FilingGroups : CRUDInterface<FilingGroup>
		{
			
			public static async Task<Specialty> findByIdSpecialties(string id, string fk)
			{
				string APIPath = "FilingGroups/:id/specialties/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Specialty>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdSpecialties(string id, string fk)
			{
				string APIPath = "FilingGroups/:id/specialties/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Specialty> updateByIdSpecialties(string id, string fk, Specialty data)
			{
				string APIPath = "FilingGroups/:id/specialties/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Specialty>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<FilingGroupSpecialty> linkSpecialties(string id, string fk, FilingGroupSpecialty data)
			{
				string APIPath = "FilingGroups/:id/specialties/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<FilingGroupSpecialty>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task unlinkSpecialties(string id, string fk)
			{
				string APIPath = "FilingGroups/:id/specialties/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<bool> existsSpecialties(string id, string fk)
			{
				string APIPath = "FilingGroups/:id/specialties/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<bool>() : default(bool);
			}
			
			public static async Task<Rate> getRate(string id, bool refresh = default(bool))
			{
				string APIPath = "FilingGroups/:id/rate";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Rate>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Office> getOffice(string id, bool refresh = default(bool))
			{
				string APIPath = "FilingGroups/:id/office";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Office>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<FilingSet> findByIdFilingSets(string id, string fk)
			{
				string APIPath = "FilingGroups/:id/filingSets/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<FilingSet>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdFilingSets(string id, string fk)
			{
				string APIPath = "FilingGroups/:id/filingSets/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<FilingSet> updateByIdFilingSets(string id, string fk, FilingSet data)
			{
				string APIPath = "FilingGroups/:id/filingSets/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<FilingSet>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<FilingGroupFilingSet> linkFilingSets(string id, string fk, FilingGroupFilingSet data)
			{
				string APIPath = "FilingGroups/:id/filingSets/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<FilingGroupFilingSet>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task unlinkFilingSets(string id, string fk)
			{
				string APIPath = "FilingGroups/:id/filingSets/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<bool> existsFilingSets(string id, string fk)
			{
				string APIPath = "FilingGroups/:id/filingSets/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<bool>() : default(bool);
			}
			
			public static async Task<IList<Specialty>> getSpecialties(string id, string filter = default(string))
			{
				string APIPath = "FilingGroups/:id/specialties";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Specialty[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Specialty> createSpecialties(string id, Specialty data)
			{
				string APIPath = "FilingGroups/:id/specialties";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Specialty>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteSpecialties(string id)
			{
				string APIPath = "FilingGroups/:id/specialties";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countSpecialties(string id, string where = default(string))
			{
				string APIPath = "FilingGroups/:id/specialties/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<IList<FilingSet>> getFilingSets(string id, string filter = default(string))
			{
				string APIPath = "FilingGroups/:id/filingSets";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<FilingSet[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<FilingSet> createFilingSets(string id, FilingSet data)
			{
				string APIPath = "FilingGroups/:id/filingSets";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<FilingSet>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteFilingSets(string id)
			{
				string APIPath = "FilingGroups/:id/filingSets";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countFilingSets(string id, string where = default(string))
			{
				string APIPath = "FilingGroups/:id/filingSets/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<FilingGroup> getForOffice(string id, bool refresh = default(bool))
			{
				string APIPath = "Offices/:id/filingGroup";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<FilingGroup>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class Versions : CRUDInterface<Version>
		{
			
			public static async Task<Filing> getPreviousFiling(string id, bool refresh = default(bool))
			{
				string APIPath = "Versions/:id/previousFiling";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Filing>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Filing> getUpdatedFiling(string id, bool refresh = default(bool))
			{
				string APIPath = "Versions/:id/updatedFiling";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Filing>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Version> getForFiling(string id, bool refresh = default(bool))
			{
				string APIPath = "Filings/:id/version";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Version>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class UploadedFiles : CRUDInterface<UploadedFile>
		{
			
			public static async Task<Filing> getFiling(string id, bool refresh = default(bool))
			{
				string APIPath = "UploadedFiles/:id/filing";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Filing>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<UploadedFile> findByIdForFiling(string id, string fk)
			{
				string APIPath = "Filings/:id/uploadedFiles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UploadedFile>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdForFiling(string id, string fk)
			{
				string APIPath = "Filings/:id/uploadedFiles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<UploadedFile> updateByIdForFiling(string id, string fk, UploadedFile data)
			{
				string APIPath = "Filings/:id/uploadedFiles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<UploadedFile>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<IList<UploadedFile>> getForFiling(string id, string filter = default(string))
			{
				string APIPath = "Filings/:id/uploadedFiles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<UploadedFile[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<UploadedFile> createForFiling(string id, UploadedFile data)
			{
				string APIPath = "Filings/:id/uploadedFiles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<UploadedFile>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteForFiling(string id)
			{
				string APIPath = "Filings/:id/uploadedFiles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countForFiling(string id, string where = default(string))
			{
				string APIPath = "Filings/:id/uploadedFiles/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
		}
		public class NotificationTypes : CRUDInterface<NotificationType>
		{
			
			public static async Task<NotificationType> getForNotification(string id, bool refresh = default(bool))
			{
				string APIPath = "Notifications/:id/notificationType";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<NotificationType>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class Containers : CRUDInterface<Container>
		{
			
			public static async Task<IList<Array>> getContainers()
			{
				string APIPath = "containers";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				var response = await Gateway.PerformRequest<Array[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<JObject> createContainer(Container options)
			{
				string APIPath = "containers";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(options);
				var response = await Gateway.PerformRequest<JObject>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyContainer(string container = default(string))
			{
				string APIPath = "containers/:container";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("container", container != null ? container.ToString() : null);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<JObject> getContainer(string container = default(string))
			{
				string APIPath = "containers/:container";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("container", container != null ? container.ToString() : null);
				var response = await Gateway.PerformRequest<JObject>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<IList<Array>> getFiles(string container = default(string))
			{
				string APIPath = "containers/:container/files";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("container", container != null ? container.ToString() : null);
				var response = await Gateway.PerformRequest<Array[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<JObject> getFile(string container = default(string), string file = default(string))
			{
				string APIPath = "containers/:container/files/:file";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("container", container != null ? container.ToString() : null);
				queryStrings.Add("file", file != null ? file.ToString() : null);
				var response = await Gateway.PerformRequest<JObject>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task removeFile(string container = default(string), string file = default(string))
			{
				string APIPath = "containers/:container/files/:file";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("container", container != null ? container.ToString() : null);
				queryStrings.Add("file", file != null ? file.ToString() : null);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<JObject> upload(string req, string res)
			{
				string APIPath = "containers/:container/upload";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				var response = await Gateway.PerformRequest<JObject>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task download(string container, string file, string res)
			{
				string APIPath = "containers/:container/download/:file";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":container", (string)container);
				APIPath = APIPath.Replace(":file", (string)file);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				
			}
		}
		
	}
}







namespace LBXamarinSDK
{
	public class AccessToken : Model
	{
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		[JsonProperty ("ttl", NullValueHandling = NullValueHandling.Ignore)]
		public double ttl { get; set; }

		[JsonProperty ("created", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime created { get; set; }

		[JsonProperty ("userId", NullValueHandling = NullValueHandling.Ignore)]
		public String userId { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class RoleMapping : Model
	{
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		[JsonProperty ("principalType", NullValueHandling = NullValueHandling.Ignore)]
		public String principalType { get; set; }

		[JsonProperty ("principalId", NullValueHandling = NullValueHandling.Ignore)]
		public String principalId { get; set; }

		[JsonProperty ("roleId", NullValueHandling = NullValueHandling.Ignore)]
		public String roleId { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class Role : Model
	{
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("name", NullValueHandling = NullValueHandling.Ignore)]
		public String name { get; set; }

		[JsonProperty ("description", NullValueHandling = NullValueHandling.Ignore)]
		public String description { get; set; }

		[JsonProperty ("created", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime created { get; set; }

		[JsonProperty ("modified", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime modified { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class UserCredential : Model
	{
		[JsonProperty ("provider", NullValueHandling = NullValueHandling.Ignore)]
		public String provider { get; set; }

		[JsonProperty ("authScheme", NullValueHandling = NullValueHandling.Ignore)]
		public String authScheme { get; set; }

		[JsonProperty ("externalId", NullValueHandling = NullValueHandling.Ignore)]
		public String externalId { get; set; }

		[JsonProperty ("profile", NullValueHandling = NullValueHandling.Ignore)]
		public Object profile { get; set; }

		[JsonProperty ("credentials", NullValueHandling = NullValueHandling.Ignore)]
		public Object credentials { get; set; }

		[JsonProperty ("created", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime created { get; set; }

		[JsonProperty ("modified", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime modified { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		[JsonProperty ("userId", NullValueHandling = NullValueHandling.Ignore)]
		public String userId { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class UserIdentity : Model
	{
		[JsonProperty ("provider", NullValueHandling = NullValueHandling.Ignore)]
		public String provider { get; set; }

		[JsonProperty ("authScheme", NullValueHandling = NullValueHandling.Ignore)]
		public String authScheme { get; set; }

		[JsonProperty ("externalId", NullValueHandling = NullValueHandling.Ignore)]
		public String externalId { get; set; }

		[JsonProperty ("profile", NullValueHandling = NullValueHandling.Ignore)]
		public Object profile { get; set; }

		[JsonProperty ("credentials", NullValueHandling = NullValueHandling.Ignore)]
		public Object credentials { get; set; }

		[JsonProperty ("created", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime created { get; set; }

		[JsonProperty ("modified", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime modified { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		[JsonProperty ("userId", NullValueHandling = NullValueHandling.Ignore)]
		public String userId { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class Page : Model
	{
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("name", NullValueHandling = NullValueHandling.Ignore)]
		public String name { get; set; }

		[JsonProperty ("slug", NullValueHandling = NullValueHandling.Ignore)]
		public String slug { get; set; }

		[JsonProperty ("content", NullValueHandling = NullValueHandling.Ignore)]
		public String content { get; set; }

		[JsonProperty ("extended", NullValueHandling = NullValueHandling.Ignore)]
		public String extended { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class Note : Model
	{
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("title", NullValueHandling = NullValueHandling.Ignore)]
		public String title { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("body", NullValueHandling = NullValueHandling.Ignore)]
		public String body { get; set; }

		[JsonProperty ("created", NullValueHandling = NullValueHandling.Ignore)]
		public double created { get; set; }

		[JsonProperty ("tags", NullValueHandling = NullValueHandling.Ignore)]
		public String[] tags { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class Post : Model
	{
		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("title", NullValueHandling = NullValueHandling.Ignore)]
		public String title { get; set; }

		[JsonProperty ("content", NullValueHandling = NullValueHandling.Ignore)]
		public String content { get; set; }

		[JsonProperty ("image", NullValueHandling = NullValueHandling.Ignore)]
		public String image { get; set; }

		[JsonProperty ("created", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime created { get; set; }

		[JsonProperty ("modified", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime modified { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class Setting : Model
	{
		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("key", NullValueHandling = NullValueHandling.Ignore)]
		public String key { get; set; }

		[JsonProperty ("value", NullValueHandling = NullValueHandling.Ignore)]
		public String value { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class Product : Model
	{
		[JsonProperty ("name", NullValueHandling = NullValueHandling.Ignore)]
		public String name { get; set; }

		[JsonProperty ("categoryId", NullValueHandling = NullValueHandling.Ignore)]
		public String categoryId { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class Category : Model
	{
		[JsonProperty ("name", NullValueHandling = NullValueHandling.Ignore)]
		public String name { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class Event : Model
	{
		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("name", NullValueHandling = NullValueHandling.Ignore)]
		public String name { get; set; }

		[JsonProperty ("description", NullValueHandling = NullValueHandling.Ignore)]
		public String description { get; set; }

		[JsonProperty ("start_time", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime start_time { get; set; }

		[JsonProperty ("end_time", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime end_time { get; set; }

		[JsonProperty ("timezone", NullValueHandling = NullValueHandling.Ignore)]
		public String timezone { get; set; }

		[JsonProperty ("location", NullValueHandling = NullValueHandling.Ignore)]
		public String location { get; set; }

		[JsonProperty ("url", NullValueHandling = NullValueHandling.Ignore)]
		public String url { get; set; }

		[JsonProperty ("image", NullValueHandling = NullValueHandling.Ignore)]
		public String image { get; set; }

		[JsonProperty ("tickets", NullValueHandling = NullValueHandling.Ignore)]
		public String tickets { get; set; }

		[JsonProperty ("tags", NullValueHandling = NullValueHandling.Ignore)]
		public String[] tags { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class AuthProvider : Model
	{
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class User : Model
	{
		[JsonProperty ("realm", NullValueHandling = NullValueHandling.Ignore)]
		public String realm { get; set; }

		[JsonProperty ("username", NullValueHandling = NullValueHandling.Ignore)]
		public String username { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("password", NullValueHandling = NullValueHandling.Ignore)]
		public String password { get; set; }

		[JsonProperty ("credentials", NullValueHandling = NullValueHandling.Ignore)]
		public Object credentials { get; set; }

		[JsonProperty ("challenges", NullValueHandling = NullValueHandling.Ignore)]
		public Object challenges { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("email", NullValueHandling = NullValueHandling.Ignore)]
		public String email { get; set; }

		[JsonProperty ("emailVerified", NullValueHandling = NullValueHandling.Ignore)]
		public bool emailVerified { get; set; }

		[JsonProperty ("verificationToken", NullValueHandling = NullValueHandling.Ignore)]
		public String verificationToken { get; set; }

		[JsonProperty ("status", NullValueHandling = NullValueHandling.Ignore)]
		public String status { get; set; }

		[JsonProperty ("created", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime created { get; set; }

		[JsonProperty ("lastUpdated", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime lastUpdated { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		[JsonProperty ("firmId", NullValueHandling = NullValueHandling.Ignore)]
		public String firmId { get; set; }

		[JsonProperty ("userId", NullValueHandling = NullValueHandling.Ignore)]
		public String userId { get; set; }

		[JsonProperty ("officeId", NullValueHandling = NullValueHandling.Ignore)]
		public String officeId { get; set; }

		[JsonProperty ("memberRoleId", NullValueHandling = NullValueHandling.Ignore)]
		public String memberRoleId { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class Firm : Model
	{
		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("name", NullValueHandling = NullValueHandling.Ignore)]
		public String name { get; set; }

		[JsonProperty ("website", NullValueHandling = NullValueHandling.Ignore)]
		public String website { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("email", NullValueHandling = NullValueHandling.Ignore)]
		public String email { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("numOfAttorneys", NullValueHandling = NullValueHandling.Ignore)]
		public String numOfAttorneys { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("overallOutgoing", NullValueHandling = NullValueHandling.Ignore)]
		public double overallOutgoing { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("overallIncoming", NullValueHandling = NullValueHandling.Ignore)]
		public double overallIncoming { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class Office : Model
	{
		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("location", NullValueHandling = NullValueHandling.Ignore)]
		public GeoPoint location { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("cityName", NullValueHandling = NullValueHandling.Ignore)]
		public String cityName { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		[JsonProperty ("firmId", NullValueHandling = NullValueHandling.Ignore)]
		public String firmId { get; set; }

		[JsonProperty ("filingGroupId", NullValueHandling = NullValueHandling.Ignore)]
		public String filingGroupId { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class MemberRole : Model
	{
		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("name", NullValueHandling = NullValueHandling.Ignore)]
		public String name { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class Agreement : Model
	{
		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("creationDate", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime creationDate { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("firmAQty", NullValueHandling = NullValueHandling.Ignore)]
		public double firmAQty { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("firmBQty", NullValueHandling = NullValueHandling.Ignore)]
		public double firmBQty { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("outgoingStatA", NullValueHandling = NullValueHandling.Ignore)]
		public double outgoingStatA { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("outgoingStatB", NullValueHandling = NullValueHandling.Ignore)]
		public double outgoingStatB { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("agreementNum", NullValueHandling = NullValueHandling.Ignore)]
		public double agreementNum { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		[JsonProperty ("firmAId", NullValueHandling = NullValueHandling.Ignore)]
		public String firmAId { get; set; }

		[JsonProperty ("firmBId", NullValueHandling = NullValueHandling.Ignore)]
		public String firmBId { get; set; }

		[JsonProperty ("officeAId", NullValueHandling = NullValueHandling.Ignore)]
		public String officeAId { get; set; }

		[JsonProperty ("officeBId", NullValueHandling = NullValueHandling.Ignore)]
		public String officeBId { get; set; }

		[JsonProperty ("userId", NullValueHandling = NullValueHandling.Ignore)]
		public String userId { get; set; }

		[JsonProperty ("firmId", NullValueHandling = NullValueHandling.Ignore)]
		public String firmId { get; set; }

		[JsonProperty ("officeId", NullValueHandling = NullValueHandling.Ignore)]
		public String officeId { get; set; }

		[JsonProperty ("agreementTypeId", NullValueHandling = NullValueHandling.Ignore)]
		public String agreementTypeId { get; set; }

		[JsonProperty ("statusId", NullValueHandling = NullValueHandling.Ignore)]
		public String statusId { get; set; }

		[JsonProperty ("filingTypeAId", NullValueHandling = NullValueHandling.Ignore)]
		public String filingTypeAId { get; set; }

		[JsonProperty ("filingTypeBId", NullValueHandling = NullValueHandling.Ignore)]
		public String filingTypeBId { get; set; }

		[JsonProperty ("rateAId", NullValueHandling = NullValueHandling.Ignore)]
		public String rateAId { get; set; }

		[JsonProperty ("rateBId", NullValueHandling = NullValueHandling.Ignore)]
		public String rateBId { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class AgreementType : Model
	{
		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("name", NullValueHandling = NullValueHandling.Ignore)]
		public String name { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("type", NullValueHandling = NullValueHandling.Ignore)]
		public Object type { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class Status : Model
	{
		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("name", NullValueHandling = NullValueHandling.Ignore)]
		public String name { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class Country : Model
	{
		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("name", NullValueHandling = NullValueHandling.Ignore)]
		public String name { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class Currency : Model
	{
		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("shortName", NullValueHandling = NullValueHandling.Ignore)]
		public String shortName { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("longName", NullValueHandling = NullValueHandling.Ignore)]
		public String longName { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("symbol", NullValueHandling = NullValueHandling.Ignore)]
		public String symbol { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class Specialty : Model
	{
		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("short", NullValueHandling = NullValueHandling.Ignore)]
		public String field_short { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("longName", NullValueHandling = NullValueHandling.Ignore)]
		public String longName { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class FilingType : Model
	{
		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("shortName", NullValueHandling = NullValueHandling.Ignore)]
		public String shortName { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("longName", NullValueHandling = NullValueHandling.Ignore)]
		public String longName { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class Filing : Model
	{
		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("filingNum", NullValueHandling = NullValueHandling.Ignore)]
		public double filingNum { get; set; }

		[JsonProperty ("applicationNum", NullValueHandling = NullValueHandling.Ignore)]
		public double applicationNum { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("openingDate", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime openingDate { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("dueDate", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime dueDate { get; set; }

		[JsonProperty ("filingDirection", NullValueHandling = NullValueHandling.Ignore)]
		public String filingDirection { get; set; }

		[JsonProperty ("delayedLength", NullValueHandling = NullValueHandling.Ignore)]
		public String delayedLength { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		[JsonProperty ("agreementId", NullValueHandling = NullValueHandling.Ignore)]
		public String agreementId { get; set; }

		[JsonProperty ("filingTypesId", NullValueHandling = NullValueHandling.Ignore)]
		public String filingTypesId { get; set; }

		[JsonProperty ("userId", NullValueHandling = NullValueHandling.Ignore)]
		public String userId { get; set; }

		[JsonProperty ("statusId", NullValueHandling = NullValueHandling.Ignore)]
		public String statusId { get; set; }

		[JsonProperty ("versionId", NullValueHandling = NullValueHandling.Ignore)]
		public String versionId { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class Comment : Model
	{
		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("text", NullValueHandling = NullValueHandling.Ignore)]
		public String text { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("date", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime date { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		[JsonProperty ("agreementId", NullValueHandling = NullValueHandling.Ignore)]
		public String agreementId { get; set; }

		[JsonProperty ("filingId", NullValueHandling = NullValueHandling.Ignore)]
		public String filingId { get; set; }

		[JsonProperty ("invoiceId", NullValueHandling = NullValueHandling.Ignore)]
		public String invoiceId { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class Invoice : Model
	{
		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("dateCreated", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime dateCreated { get; set; }

		[JsonProperty ("dateSent", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime dateSent { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("amount", NullValueHandling = NullValueHandling.Ignore)]
		public double amount { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("disputeIndicator", NullValueHandling = NullValueHandling.Ignore)]
		public bool disputeIndicator { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("errorIndicator", NullValueHandling = NullValueHandling.Ignore)]
		public bool errorIndicator { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("invoiceNum", NullValueHandling = NullValueHandling.Ignore)]
		public double invoiceNum { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		[JsonProperty ("agreementId", NullValueHandling = NullValueHandling.Ignore)]
		public String agreementId { get; set; }

		[JsonProperty ("statusId", NullValueHandling = NullValueHandling.Ignore)]
		public String statusId { get; set; }

		[JsonProperty ("userId", NullValueHandling = NullValueHandling.Ignore)]
		public String userId { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class Notification : Model
	{
		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("name", NullValueHandling = NullValueHandling.Ignore)]
		public String name { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("arrivalTime", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime arrivalTime { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("message", NullValueHandling = NullValueHandling.Ignore)]
		public String message { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("read", NullValueHandling = NullValueHandling.Ignore)]
		public Object read { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		[JsonProperty ("userId", NullValueHandling = NullValueHandling.Ignore)]
		public String userId { get; set; }

		[JsonProperty ("receiverId", NullValueHandling = NullValueHandling.Ignore)]
		public String receiverId { get; set; }

		[JsonProperty ("notificationTypeId", NullValueHandling = NullValueHandling.Ignore)]
		public String notificationTypeId { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class Rate : Model
	{
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		[JsonProperty ("currenciesId", NullValueHandling = NullValueHandling.Ignore)]
		public String currenciesId { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class FilingSet : Model
	{
		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("outfilingRange", NullValueHandling = NullValueHandling.Ignore)]
		public double outfilingRange { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("infilingRange", NullValueHandling = NullValueHandling.Ignore)]
		public double infilingRange { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		[JsonProperty ("countryId", NullValueHandling = NullValueHandling.Ignore)]
		public String countryId { get; set; }

		[JsonProperty ("outFilingTypeId", NullValueHandling = NullValueHandling.Ignore)]
		public String outFilingTypeId { get; set; }

		[JsonProperty ("inFilingTypeId", NullValueHandling = NullValueHandling.Ignore)]
		public String inFilingTypeId { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class FilingGroup : Model
	{
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		[JsonProperty ("rateId", NullValueHandling = NullValueHandling.Ignore)]
		public String rateId { get; set; }

		[JsonProperty ("officeId", NullValueHandling = NullValueHandling.Ignore)]
		public String officeId { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class Version : Model
	{
		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("globalVer", NullValueHandling = NullValueHandling.Ignore)]
		public double globalVer { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("currentVer", NullValueHandling = NullValueHandling.Ignore)]
		public double currentVer { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		[JsonProperty ("previousFilingId", NullValueHandling = NullValueHandling.Ignore)]
		public String previousFilingId { get; set; }

		[JsonProperty ("updatedFilingId", NullValueHandling = NullValueHandling.Ignore)]
		public String updatedFilingId { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class UploadedFile : Model
	{
		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("file", NullValueHandling = NullValueHandling.Ignore)]
		public IList<int> file { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("name", NullValueHandling = NullValueHandling.Ignore)]
		public String name { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("dateUploaded", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime dateUploaded { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		[JsonProperty ("filingId", NullValueHandling = NullValueHandling.Ignore)]
		public String filingId { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class NotificationType : Model
	{
		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("name", NullValueHandling = NullValueHandling.Ignore)]
		public String name { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("description", NullValueHandling = NullValueHandling.Ignore)]
		public String description { get; set; }

		[JsonProperty ("triggeringEvent", NullValueHandling = NullValueHandling.Ignore)]
		public String triggeringEvent { get; set; }

		[JsonProperty ("redirect", NullValueHandling = NullValueHandling.Ignore)]
		public String redirect { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("receiver", NullValueHandling = NullValueHandling.Ignore)]
		public String receiver { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public class Container : Model
	{
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}


	// Relationship classes:
	public class RateAgreement : Model
	{
		[Newtonsoft.Json.JsonProperty("id")]
        public string id { get; set; }

		[Newtonsoft.Json.JsonProperty("rateId")]
        public string rateId { get; set; }

		[Newtonsoft.Json.JsonProperty("agreementId")]
        public string agreementId { get; set; }
	}

	public class FilingGroupSpecialty : Model
	{
		[Newtonsoft.Json.JsonProperty("id")]
        public string id { get; set; }

		[Newtonsoft.Json.JsonProperty("filingGroupId")]
        public string filingGroupId { get; set; }

		[Newtonsoft.Json.JsonProperty("specialtyId")]
        public string specialtyId { get; set; }
	}

	public class FilingGroupFilingSet : Model
	{
		[Newtonsoft.Json.JsonProperty("id")]
        public string id { get; set; }

		[Newtonsoft.Json.JsonProperty("filingGroupId")]
        public string filingGroupId { get; set; }

		[Newtonsoft.Json.JsonProperty("filingSetId")]
        public string filingSetId { get; set; }
	}




}
// Eof
