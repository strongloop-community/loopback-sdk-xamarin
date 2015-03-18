



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
using System.Net;
using System.Net.Sockets;

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
			if(debugMode)
			{
				// **** VITAL! DO NOT REMOVE! ************
				Console.WriteLine("******************************");
				Console.WriteLine("** SDK Gateway constructor. **");
				Console.WriteLine("******************************\n");
				// **** ^^^ VITAL! DO NOT REMOVE! ********
			}
		}

		// Debug mode getter
		public static bool GetDebugMode()
		{
			return debugMode;
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

		// Define server Base Url for API requests to self local IP
		public static void SetServerBaseURLToSelf()
		{
			var firstOrDefault = Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            if (firstOrDefault != null)
            { 
                string adrStr = "http://" + firstOrDefault.ToString() + ":3000/api/";
				if (debugMode)
                    Console.WriteLine("-------- >> DEBUG: Setting Gateway URL to " + adrStr);	
                SetServerBaseURL(new Uri(adrStr));
				
            }
			else
			{
				if (debugMode)
                    Console.WriteLine("-------- >> DEBUG: Error finding self URL.");
				throw new Exception();
			}
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

	// Allow conversion between the return type of login methods into AccessToken
	// TODO: Add this jobject->class implicit conversion as a templated function for all classes inheriting from model
	public partial class AccessToken : Model
    {
        public static implicit operator AccessToken(JObject jObj)
        {
            if (jObj == null)
            {
                return null;
            }
            return JsonConvert.DeserializeObject<AccessToken>(jObj.ToString());
        }
    }

	// Access Token model
	public partial class AccessToken : Model
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string id { get; set; }

        [JsonProperty("ttl", NullValueHandling = NullValueHandling.Ignore)]
        public long ttl { get; set; }

        [JsonProperty("created", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime created { get; set; }

        [JsonProperty("userID", NullValueHandling = NullValueHandling.Ignore)]
        public string userID { get; set; }

		public override String getID()
        {
            return id;
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
			private static readonly Dictionary<string, string> APIDictionary = new Dictionary<string, string>
            {
				{"user/create", "Users"},
				{"user/upsert", "Users"},
				{"user/exists", "Users/:id/exists"},
				{"user/findbyid", "Users/:id"},
				{"user/find", "Users"},
				{"user/findone", "Users/findOne"},
				{"user/updateall", "Users/update"},
				{"user/deletebyid", "Users/:id"},
				{"user/count", "Users/count"},
				{"user/prototype$updateattributes", "Users/:id"},
				{"miniuser/create", "miniUsers"},
				{"miniuser/upsert", "miniUsers"},
				{"miniuser/exists", "miniUsers/:id/exists"},
				{"miniuser/findbyid", "miniUsers/:id"},
				{"miniuser/find", "miniUsers"},
				{"miniuser/findone", "miniUsers/findOne"},
				{"miniuser/updateall", "miniUsers/update"},
				{"miniuser/deletebyid", "miniUsers/:id"},
				{"miniuser/count", "miniUsers/count"},
				{"miniuser/prototype$updateattributes", "miniUsers/:id"},
				{"rolemapping/create", "RoleMappings"},
				{"rolemapping/upsert", "RoleMappings"},
				{"rolemapping/exists", "RoleMappings/:id/exists"},
				{"rolemapping/findbyid", "RoleMappings/:id"},
				{"rolemapping/find", "RoleMappings"},
				{"rolemapping/findone", "RoleMappings/findOne"},
				{"rolemapping/updateall", "RoleMappings/update"},
				{"rolemapping/deletebyid", "RoleMappings/:id"},
				{"rolemapping/count", "RoleMappings/count"},
				{"rolemapping/prototype$updateattributes", "RoleMappings/:id"},
				{"role/create", "Roles"},
				{"role/upsert", "Roles"},
				{"role/exists", "Roles/:id/exists"},
				{"role/findbyid", "Roles/:id"},
				{"role/find", "Roles"},
				{"role/findone", "Roles/findOne"},
				{"role/updateall", "Roles/update"},
				{"role/deletebyid", "Roles/:id"},
				{"role/count", "Roles/count"},
				{"role/prototype$updateattributes", "Roles/:id"},
				{"customer/create", "Customers"},
				{"customer/upsert", "Customers"},
				{"customer/exists", "Customers/:id/exists"},
				{"customer/findbyid", "Customers/:id"},
				{"customer/find", "Customers"},
				{"customer/findone", "Customers/findOne"},
				{"customer/updateall", "Customers/update"},
				{"customer/deletebyid", "Customers/:id"},
				{"customer/count", "Customers/count"},
				{"customer/prototype$updateattributes", "Customers/:id"},
				{"review/create", "Reviews"},
				{"review/upsert", "Reviews"},
				{"review/exists", "Reviews/:id/exists"},
				{"review/findbyid", "Reviews/:id"},
				{"review/find", "Reviews"},
				{"review/findone", "Reviews/findOne"},
				{"review/updateall", "Reviews/update"},
				{"review/deletebyid", "Reviews/:id"},
				{"review/count", "Reviews/count"},
				{"review/prototype$updateattributes", "Reviews/:id"},
				{"order/create", "Orders"},
				{"order/upsert", "Orders"},
				{"order/exists", "Orders/:id/exists"},
				{"order/findbyid", "Orders/:id"},
				{"order/find", "Orders"},
				{"order/findone", "Orders/findOne"},
				{"order/updateall", "Orders/update"},
				{"order/deletebyid", "Orders/:id"},
				{"order/count", "Orders/count"},
				{"order/prototype$updateattributes", "Orders/:id"}
			};

			protected static String getAPIPath(String crudMethodName)
            {
				Type baseType = typeof(T);
				String dictionaryKey = string.Format("{0}/{1}", baseType.Name, crudMethodName).ToLower();

				if(!APIDictionary.ContainsKey(dictionaryKey))
				{
					if(Gateway.GetDebugMode())
						Console.WriteLine("Error - no known CRUD path for " + dictionaryKey);
					throw new Exception();
				}
				return APIDictionary[dictionaryKey];
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
		public class Users : CRUDInterface<User>
		{
			
			public static async Task<JObject> login(User credentials, string include = default(string))
			{
				string APIPath = "Users/login";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(credentials);
				queryStrings.Add("include", include != null ? include.ToString() : null);
				var response = await Gateway.PerformRequest<JObject>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task logout()
			{
				string APIPath = "Users/logout";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task confirm(string uid = default(string), string token = default(string), string redirect = default(string))
			{
				string APIPath = "Users/confirm";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("uid", uid != null ? uid.ToString() : null);
				queryStrings.Add("token", token != null ? token.ToString() : null);
				queryStrings.Add("redirect", redirect != null ? redirect.ToString() : null);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task resetPassword(User options)
			{
				string APIPath = "Users/reset";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(options);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<AccessToken> findByIdAccessTokens(string id, string fk)
			{
				string APIPath = "Users/:id/accessTokens/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<AccessToken>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdAccessTokens(string id, string fk)
			{
				string APIPath = "Users/:id/accessTokens/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<AccessToken> updateByIdAccessTokens(string id, string fk, AccessToken data)
			{
				string APIPath = "Users/:id/accessTokens/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<AccessToken>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<IList<AccessToken>> getAccessTokens(string id, string filter = default(string))
			{
				string APIPath = "Users/:id/accessTokens";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<AccessToken[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<AccessToken> createAccessTokens(string id, AccessToken data)
			{
				string APIPath = "Users/:id/accessTokens";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<AccessToken>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteAccessTokens(string id)
			{
				string APIPath = "Users/:id/accessTokens";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countAccessTokens(string id, string where = default(string))
			{
				string APIPath = "Users/:id/accessTokens/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
		}
		public class MiniUsers : CRUDInterface<MiniUser>
		{
			
			public static async Task<JObject> login(MiniUser credentials, string include = default(string))
			{
				string APIPath = "miniUsers/login";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(credentials);
				queryStrings.Add("include", include != null ? include.ToString() : null);
				var response = await Gateway.PerformRequest<JObject>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task logout()
			{
				string APIPath = "miniUsers/logout";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task confirm(string uid = default(string), string token = default(string), string redirect = default(string))
			{
				string APIPath = "miniUsers/confirm";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("uid", uid != null ? uid.ToString() : null);
				queryStrings.Add("token", token != null ? token.ToString() : null);
				queryStrings.Add("redirect", redirect != null ? redirect.ToString() : null);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task resetPassword(MiniUser options)
			{
				string APIPath = "miniUsers/reset";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(options);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<AccessToken> findByIdAccessTokens(string id, string fk)
			{
				string APIPath = "miniUsers/:id/accessTokens/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<AccessToken>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdAccessTokens(string id, string fk)
			{
				string APIPath = "miniUsers/:id/accessTokens/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<AccessToken> updateByIdAccessTokens(string id, string fk, AccessToken data)
			{
				string APIPath = "miniUsers/:id/accessTokens/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<AccessToken>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Role> findByIdRoles(string id, string fk)
			{
				string APIPath = "miniUsers/:id/roles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdRoles(string id, string fk)
			{
				string APIPath = "miniUsers/:id/roles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Role> updateByIdRoles(string id, string fk, Role data)
			{
				string APIPath = "miniUsers/:id/roles/:fk";
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
				string APIPath = "miniUsers/:id/roles/rel/:fk";
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
				string APIPath = "miniUsers/:id/roles/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<bool> existsRoles(string id, string fk)
			{
				string APIPath = "miniUsers/:id/roles/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<bool>() : default(bool);
			}
			
			public static async Task<IList<AccessToken>> getAccessTokens(string id, string filter = default(string))
			{
				string APIPath = "miniUsers/:id/accessTokens";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<AccessToken[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<AccessToken> createAccessTokens(string id, AccessToken data)
			{
				string APIPath = "miniUsers/:id/accessTokens";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<AccessToken>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteAccessTokens(string id)
			{
				string APIPath = "miniUsers/:id/accessTokens";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countAccessTokens(string id, string where = default(string))
			{
				string APIPath = "miniUsers/:id/accessTokens/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<IList<Role>> getRoles(string id, string filter = default(string))
			{
				string APIPath = "miniUsers/:id/roles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Role[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Role> createRoles(string id, Role data)
			{
				string APIPath = "miniUsers/:id/roles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteRoles(string id)
			{
				string APIPath = "miniUsers/:id/roles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countRoles(string id, string where = default(string))
			{
				string APIPath = "miniUsers/:id/roles/count";
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
			
			public static async Task<Role> findByIdForminiUser(string id, string fk)
			{
				string APIPath = "miniUsers/:id/roles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdForminiUser(string id, string fk)
			{
				string APIPath = "miniUsers/:id/roles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Role> updateByIdForminiUser(string id, string fk, Role data)
			{
				string APIPath = "miniUsers/:id/roles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<RoleMapping> linkForminiUser(string id, string fk, RoleMapping data)
			{
				string APIPath = "miniUsers/:id/roles/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<RoleMapping>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task unlinkForminiUser(string id, string fk)
			{
				string APIPath = "miniUsers/:id/roles/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<bool> existsForminiUser(string id, string fk)
			{
				string APIPath = "miniUsers/:id/roles/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<bool>() : default(bool);
			}
			
			public static async Task<IList<Role>> getForminiUser(string id, string filter = default(string))
			{
				string APIPath = "miniUsers/:id/roles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Role[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Role> createForminiUser(string id, Role data)
			{
				string APIPath = "miniUsers/:id/roles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteForminiUser(string id)
			{
				string APIPath = "miniUsers/:id/roles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countForminiUser(string id, string where = default(string))
			{
				string APIPath = "miniUsers/:id/roles/count";
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
		}
		public class Customers : CRUDInterface<Customer>
		{
			
			public static async Task<Review> findByIdReviews(string id, string fk)
			{
				string APIPath = "Customers/:id/reviews/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Review>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdReviews(string id, string fk)
			{
				string APIPath = "Customers/:id/reviews/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Review> updateByIdReviews(string id, string fk, Review data)
			{
				string APIPath = "Customers/:id/reviews/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Review>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Order> findByIdOrders(string id, string fk)
			{
				string APIPath = "Customers/:id/orders/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Order>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdOrders(string id, string fk)
			{
				string APIPath = "Customers/:id/orders/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Order> updateByIdOrders(string id, string fk, Order data)
			{
				string APIPath = "Customers/:id/orders/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Order>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<IList<Review>> getReviews(string id, string filter = default(string))
			{
				string APIPath = "Customers/:id/reviews";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Review[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Review> createReviews(string id, Review data)
			{
				string APIPath = "Customers/:id/reviews";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Review>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteReviews(string id)
			{
				string APIPath = "Customers/:id/reviews";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countReviews(string id, string where = default(string))
			{
				string APIPath = "Customers/:id/reviews/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<IList<Order>> getOrders(string id, string filter = default(string))
			{
				string APIPath = "Customers/:id/orders";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Order[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Order> createOrders(string id, Order data)
			{
				string APIPath = "Customers/:id/orders";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Order>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteOrders(string id)
			{
				string APIPath = "Customers/:id/orders";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countOrders(string id, string where = default(string))
			{
				string APIPath = "Customers/:id/orders/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<IList<Customer>> getYoungFolks(string filter = default(string))
			{
				string APIPath = "Customers/youngFolks";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Customer[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Customer> createYoungFolks(Customer data)
			{
				string APIPath = "Customers/youngFolks";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Customer>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteYoungFolks()
			{
				string APIPath = "Customers/youngFolks";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countYoungFolks(string where = default(string))
			{
				string APIPath = "Customers/youngFolks/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
			
			public static async Task<Customer> getForReview(string id, bool refresh = default(bool))
			{
				string APIPath = "Reviews/:id/author";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Customer>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Customer> getForOrder(string id, bool refresh = default(bool))
			{
				string APIPath = "Orders/:id/customer";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Customer>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class Reviews : CRUDInterface<Review>
		{
			
			public static async Task<Customer> getAuthor(string id, bool refresh = default(bool))
			{
				string APIPath = "Reviews/:id/author";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Customer>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Review> findByIdForCustomer(string id, string fk)
			{
				string APIPath = "Customers/:id/reviews/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Review>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdForCustomer(string id, string fk)
			{
				string APIPath = "Customers/:id/reviews/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Review> updateByIdForCustomer(string id, string fk, Review data)
			{
				string APIPath = "Customers/:id/reviews/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Review>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<IList<Review>> getForCustomer(string id, string filter = default(string))
			{
				string APIPath = "Customers/:id/reviews";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Review[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Review> createForCustomer(string id, Review data)
			{
				string APIPath = "Customers/:id/reviews";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Review>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteForCustomer(string id)
			{
				string APIPath = "Customers/:id/reviews";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countForCustomer(string id, string where = default(string))
			{
				string APIPath = "Customers/:id/reviews/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
		}
		public class Orders : CRUDInterface<Order>
		{
			
			public static async Task<Customer> getCustomer(string id, bool refresh = default(bool))
			{
				string APIPath = "Orders/:id/customer";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Customer>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Order> findByIdForCustomer(string id, string fk)
			{
				string APIPath = "Customers/:id/orders/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Order>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task destroyByIdForCustomer(string id, string fk)
			{
				string APIPath = "Customers/:id/orders/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<Order> updateByIdForCustomer(string id, string fk, Order data)
			{
				string APIPath = "Customers/:id/orders/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Order>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<IList<Order>> getForCustomer(string id, string filter = default(string))
			{
				string APIPath = "Customers/:id/orders";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Order[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task<Order> createForCustomer(string id, Order data)
			{
				string APIPath = "Customers/:id/orders";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				bodyJSON = JsonConvert.SerializeObject(data);
				var response = await Gateway.PerformRequest<Order>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
			
			public static async Task deleteForCustomer(string id)
			{
				string APIPath = "Customers/:id/orders";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
			
			public static async Task<double> countForCustomer(string id, string where = default(string))
			{
				string APIPath = "Customers/:id/orders/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
		}
		
	}
}






namespace LBXamarinSDK
{
	public partial class User : Model
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
		public bool? emailVerified { get; set; }

		[JsonProperty ("verificationToken", NullValueHandling = NullValueHandling.Ignore)]
		public String verificationToken { get; set; }

		[JsonProperty ("status", NullValueHandling = NullValueHandling.Ignore)]
		public String status { get; set; }

		[JsonProperty ("created", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime? created { get; set; }

		[JsonProperty ("lastUpdated", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime? lastUpdated { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class MiniUser : User
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
		public bool? emailVerified { get; set; }

		[JsonProperty ("verificationToken", NullValueHandling = NullValueHandling.Ignore)]
		public String verificationToken { get; set; }

		[JsonProperty ("status", NullValueHandling = NullValueHandling.Ignore)]
		public String status { get; set; }

		[JsonProperty ("created", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime? created { get; set; }

		[JsonProperty ("lastUpdated", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime? lastUpdated { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class RoleMapping : Model
	{
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		[JsonProperty ("principalType", NullValueHandling = NullValueHandling.Ignore)]
		public String principalType { get; set; }

		[JsonProperty ("principalId", NullValueHandling = NullValueHandling.Ignore)]
		public String principalId { get; set; }

		[JsonProperty ("roleId", NullValueHandling = NullValueHandling.Ignore)]
		public double? roleId { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class Role : Model
	{
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		 // Required Field. TODO: Validate the existence of this property locally before sending to server
		[JsonProperty ("name", NullValueHandling = NullValueHandling.Ignore)]
		public String name { get; set; }

		[JsonProperty ("description", NullValueHandling = NullValueHandling.Ignore)]
		public String description { get; set; }

		[JsonProperty ("created", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime? created { get; set; }

		[JsonProperty ("modified", NullValueHandling = NullValueHandling.Ignore)]
		public DateTime? modified { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class Customer : Model
	{
		[JsonProperty ("name", NullValueHandling = NullValueHandling.Ignore)]
		public String name { get; set; }

		[JsonProperty ("age", NullValueHandling = NullValueHandling.Ignore)]
		public double? age { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class Review : Model
	{
		[JsonProperty ("product", NullValueHandling = NullValueHandling.Ignore)]
		public String product { get; set; }

		[JsonProperty ("star", NullValueHandling = NullValueHandling.Ignore)]
		public double? star { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		[JsonProperty ("authorId", NullValueHandling = NullValueHandling.Ignore)]
		public double? authorId { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class Order : Model
	{
		[JsonProperty ("description", NullValueHandling = NullValueHandling.Ignore)]
		public String description { get; set; }

		[JsonProperty ("total", NullValueHandling = NullValueHandling.Ignore)]
		public double? total { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		[JsonProperty ("customerId", NullValueHandling = NullValueHandling.Ignore)]
		public double? customerId { get; set; }

		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}


	// Relationship classes:
 // None.


}
// Eof
