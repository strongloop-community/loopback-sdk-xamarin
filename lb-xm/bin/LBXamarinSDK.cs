/**
 *** Hardcoded Models ***
 */

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
				Console.WriteLine("******************************");
				Console.WriteLine("** SDK Gateway Debug Mode.  **");
				Console.WriteLine("******************************\n");
			}
		}

		// Debug mode getter
		public static bool GetDebugMode()
		{
			return debugMode;
		}
		
		/*** Cancellation-Token methods, define a timeout for a server request ***/
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
				try
				{
					request.AddParameter ("application/json", JObject.Parse(json), ParameterType.RequestBody);
				}
				catch(Exception)
				{
					request.AddParameter ("application/json", json, ParameterType.RequestBody);
				}
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
	public abstract class LBModel
    {
        public virtual String getID()
        {
            return "";
        }
    }

	// Allow conversion between the return type of login methods into AccessToken, e.g. "AccessToken myAccessToken = await Users.login(someCredentials);
	// TODO: Add this jobject->class implicit conversion as a templated function for all classes inheriting from model
	public partial class AccessToken : LBModel
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
	public partial class AccessToken : LBModel
    {
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public string id { get; set; }

        [JsonProperty("ttl", NullValueHandling = NullValueHandling.Ignore)]
        public long? _ttl { get; set; }
		[JsonIgnore]
		public long ttl
		{
			get { return _ttl ?? new long(); }
			set { _ttl = value; }
		}

        [JsonProperty("created", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? _created { get; set; }
		[JsonIgnore]
		public DateTime created
		{
			get { return _created ?? new DateTime(); }
			set { _created = value; }
		}


        [JsonProperty("userID", NullValueHandling = NullValueHandling.Ignore)]
        public string userID { get; set; }

		public override String getID()
        {
            return id;
        }
    }
	// GeoPoint primitive loopback type
	public class GeoPoint : LBModel
	{
		// Must be leq than 90: TODO: Add attributes or setter limitations
		[JsonProperty("lat", NullValueHandling = NullValueHandling.Ignore)]
		public double? _Latitude { get; set; }
		[JsonIgnore]
		public double Latitude
		{
			get { return _Latitude ?? new double(); }
			set { _Latitude = value; }
		}

		[JsonProperty("lng", NullValueHandling = NullValueHandling.Ignore)]
		public double? _Longtitude { get; set; }
		[JsonIgnore]
		public double Longtitude
		{
			get { return _Longtitude ?? new double(); }
			set { _Longtitude = value; }
		}
	}
}
/**
 *** Dynamic Repositories ***
 */

namespace LBXamarinSDK
{
    namespace LBRepo
    {
		/* CRUD Interface holds the basic CRUD operations for all models.
		   In turn, all repositories will inherit from this.
		*/
        public abstract class CRUDInterface<T> where T : LBModel
        {
			private static readonly Dictionary<string, string> APIDictionary = new Dictionary<string, string>
            {
				{"user/create", "user"}, 
				{"user/upsert", "user"}, 
				{"user/exists", "user/:id/exists"}, 
				{"user/findbyid", "user/:id"}, 
				{"user/find", "user"}, 
				{"user/findone", "user/findOne"}, 
				{"user/updateall", "user/update"}, 
				{"user/deletebyid", "user/:id"}, 
				{"user/count", "user/count"}, 
				{"user/prototype$updateattributes", "user/:id"}, 
				{"trainer/create", "Trainers"}, 
				{"trainer/upsert", "Trainers"}, 
				{"trainer/exists", "Trainers/:id/exists"}, 
				{"trainer/findbyid", "Trainers/:id"}, 
				{"trainer/find", "Trainers"}, 
				{"trainer/findone", "Trainers/findOne"}, 
				{"trainer/updateall", "Trainers/update"}, 
				{"trainer/deletebyid", "Trainers/:id"}, 
				{"trainer/count", "Trainers/count"}, 
				{"trainer/prototype$updateattributes", "Trainers/:id"}, 
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
				{"paymentdata/create", "PaymentData"}, 
				{"paymentdata/upsert", "PaymentData"}, 
				{"paymentdata/exists", "PaymentData/:id/exists"}, 
				{"paymentdata/findbyid", "PaymentData/:id"}, 
				{"paymentdata/find", "PaymentData"}, 
				{"paymentdata/findone", "PaymentData/findOne"}, 
				{"paymentdata/updateall", "PaymentData/update"}, 
				{"paymentdata/deletebyid", "PaymentData/:id"}, 
				{"paymentdata/count", "PaymentData/count"}, 
				{"paymentdata/prototype$updateattributes", "PaymentData/:id"}, 
				{"usercredential/create", "userCredentials"}, 
				{"usercredential/upsert", "userCredentials"}, 
				{"usercredential/exists", "userCredentials/:id/exists"}, 
				{"usercredential/findbyid", "userCredentials/:id"}, 
				{"usercredential/find", "userCredentials"}, 
				{"usercredential/findone", "userCredentials/findOne"}, 
				{"usercredential/updateall", "userCredentials/update"}, 
				{"usercredential/deletebyid", "userCredentials/:id"}, 
				{"usercredential/count", "userCredentials/count"}, 
				{"usercredential/prototype$updateattributes", "userCredentials/:id"}, 
				{"useridentity/create", "userIdentities"}, 
				{"useridentity/upsert", "userIdentities"}, 
				{"useridentity/exists", "userIdentities/:id/exists"}, 
				{"useridentity/findbyid", "userIdentities/:id"}, 
				{"useridentity/find", "userIdentities"}, 
				{"useridentity/findone", "userIdentities/findOne"}, 
				{"useridentity/updateall", "userIdentities/update"}, 
				{"useridentity/deletebyid", "userIdentities/:id"}, 
				{"useridentity/count", "userIdentities/count"}, 
				{"useridentity/prototype$updateattributes", "userIdentities/:id"}, 
				{"authprovider/create", "AuthProviders"}, 
				{"authprovider/upsert", "AuthProviders"}, 
				{"authprovider/exists", "AuthProviders/:id/exists"}, 
				{"authprovider/findbyid", "AuthProviders/:id"}, 
				{"authprovider/find", "AuthProviders"}, 
				{"authprovider/findone", "AuthProviders/findOne"}, 
				{"authprovider/updateall", "AuthProviders/update"}, 
				{"authprovider/deletebyid", "AuthProviders/:id"}, 
				{"authprovider/count", "AuthProviders/count"}, 
				{"authprovider/prototype$updateattributes", "AuthProviders/:id"}, 
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
				{"setting/create", "settings"}, 
				{"setting/upsert", "settings"}, 
				{"setting/exists", "settings/:id/exists"}, 
				{"setting/findbyid", "settings/:id"}, 
				{"setting/find", "settings"}, 
				{"setting/findone", "settings/findOne"}, 
				{"setting/updateall", "settings/update"}, 
				{"setting/deletebyid", "settings/:id"}, 
				{"setting/count", "settings/count"}, 
				{"setting/prototype$updateattributes", "settings/:id"}, 
				{"gym/create", "Gyms"}, 
				{"gym/upsert", "Gyms"}, 
				{"gym/exists", "Gyms/:id/exists"}, 
				{"gym/findbyid", "Gyms/:id"}, 
				{"gym/find", "Gyms"}, 
				{"gym/findone", "Gyms/findOne"}, 
				{"gym/updateall", "Gyms/update"}, 
				{"gym/deletebyid", "Gyms/:id"}, 
				{"gym/count", "Gyms/count"}, 
				{"gym/prototype$updateattributes", "Gyms/:id"}, 
				{"exercise/create", "Exercises"}, 
				{"exercise/upsert", "Exercises"}, 
				{"exercise/exists", "Exercises/:id/exists"}, 
				{"exercise/findbyid", "Exercises/:id"}, 
				{"exercise/find", "Exercises"}, 
				{"exercise/findone", "Exercises/findOne"}, 
				{"exercise/updateall", "Exercises/update"}, 
				{"exercise/deletebyid", "Exercises/:id"}, 
				{"exercise/count", "Exercises/count"}, 
				{"exercise/prototype$updateattributes", "Exercises/:id"}, 
				{"workout/create", "WorkOuts"}, 
				{"workout/upsert", "WorkOuts"}, 
				{"workout/exists", "WorkOuts/:id/exists"}, 
				{"workout/findbyid", "WorkOuts/:id"}, 
				{"workout/find", "WorkOuts"}, 
				{"workout/findone", "WorkOuts/findOne"}, 
				{"workout/updateall", "WorkOuts/update"}, 
				{"workout/deletebyid", "WorkOuts/:id"}, 
				{"workout/count", "WorkOuts/count"}, 
				{"workout/prototype$updateattributes", "WorkOuts/:id"}, 
				{"exercisetype/create", "ExerciseTypes"}, 
				{"exercisetype/upsert", "ExerciseTypes"}, 
				{"exercisetype/exists", "ExerciseTypes/:id/exists"}, 
				{"exercisetype/findbyid", "ExerciseTypes/:id"}, 
				{"exercisetype/find", "ExerciseTypes"}, 
				{"exercisetype/findone", "ExerciseTypes/findOne"}, 
				{"exercisetype/updateall", "ExerciseTypes/update"}, 
				{"exercisetype/deletebyid", "ExerciseTypes/:id"}, 
				{"exercisetype/count", "ExerciseTypes/count"}, 
				{"exercisetype/prototype$updateattributes", "ExerciseTypes/:id"}, 
				{"generalactivitytype/create", "GeneralActivityTypes"}, 
				{"generalactivitytype/upsert", "GeneralActivityTypes"}, 
				{"generalactivitytype/exists", "GeneralActivityTypes/:id/exists"}, 
				{"generalactivitytype/findbyid", "GeneralActivityTypes/:id"}, 
				{"generalactivitytype/find", "GeneralActivityTypes"}, 
				{"generalactivitytype/findone", "GeneralActivityTypes/findOne"}, 
				{"generalactivitytype/updateall", "GeneralActivityTypes/update"}, 
				{"generalactivitytype/deletebyid", "GeneralActivityTypes/:id"}, 
				{"generalactivitytype/count", "GeneralActivityTypes/count"}, 
				{"generalactivitytype/prototype$updateattributes", "GeneralActivityTypes/:id"}, 
				{"maritalstatustype/create", "MaritalStatusTypes"}, 
				{"maritalstatustype/upsert", "MaritalStatusTypes"}, 
				{"maritalstatustype/exists", "MaritalStatusTypes/:id/exists"}, 
				{"maritalstatustype/findbyid", "MaritalStatusTypes/:id"}, 
				{"maritalstatustype/find", "MaritalStatusTypes"}, 
				{"maritalstatustype/findone", "MaritalStatusTypes/findOne"}, 
				{"maritalstatustype/updateall", "MaritalStatusTypes/update"}, 
				{"maritalstatustype/deletebyid", "MaritalStatusTypes/:id"}, 
				{"maritalstatustype/count", "MaritalStatusTypes/count"}, 
				{"maritalstatustype/prototype$updateattributes", "MaritalStatusTypes/:id"}, 
				{"physicaldatatype/create", "PhysicalDataTypes"}, 
				{"physicaldatatype/upsert", "PhysicalDataTypes"}, 
				{"physicaldatatype/exists", "PhysicalDataTypes/:id/exists"}, 
				{"physicaldatatype/findbyid", "PhysicalDataTypes/:id"}, 
				{"physicaldatatype/find", "PhysicalDataTypes"}, 
				{"physicaldatatype/findone", "PhysicalDataTypes/findOne"}, 
				{"physicaldatatype/updateall", "PhysicalDataTypes/update"}, 
				{"physicaldatatype/deletebyid", "PhysicalDataTypes/:id"}, 
				{"physicaldatatype/count", "PhysicalDataTypes/count"}, 
				{"physicaldatatype/prototype$updateattributes", "PhysicalDataTypes/:id"}, 
				{"preferences/create", "Preferences"}, 
				{"preferences/upsert", "Preferences"}, 
				{"preferences/exists", "Preferences/:id/exists"}, 
				{"preferences/findbyid", "Preferences/:id"}, 
				{"preferences/find", "Preferences"}, 
				{"preferences/findone", "Preferences/findOne"}, 
				{"preferences/updateall", "Preferences/update"}, 
				{"preferences/deletebyid", "Preferences/:id"}, 
				{"preferences/count", "Preferences/count"}, 
				{"preferences/prototype$updateattributes", "Preferences/:id"}, 
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

			/*
			 * Find a related item by id for credentials
			 */
			public static async Task<UserCredential> findByIdCredentials(string id, string fk)
			{
				string APIPath = "user/:id/credentials/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for credentials
			 */
			public static async Task destroyByIdCredentials(string id, string fk)
			{
				string APIPath = "user/:id/credentials/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for credentials
			 */
			public static async Task<UserCredential> updateByIdCredentials(UserCredential data, string id, string fk)
			{
				string APIPath = "user/:id/credentials/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Find a related item by id for identities
			 */
			public static async Task<UserIdentity> findByIdIdentities(string id, string fk)
			{
				string APIPath = "user/:id/identities/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for identities
			 */
			public static async Task destroyByIdIdentities(string id, string fk)
			{
				string APIPath = "user/:id/identities/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for identities
			 */
			public static async Task<UserIdentity> updateByIdIdentities(UserIdentity data, string id, string fk)
			{
				string APIPath = "user/:id/identities/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Fetches hasOne relation Preferences
			 */
			public static async Task<Preferences> getPreferences(string id, bool refresh = default(bool))
			{
				string APIPath = "user/:id/Preferences";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Preferences>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in Preferences of this model.
			 */
			public static async Task<Preferences> createPreferences(Preferences data, string id)
			{
				string APIPath = "user/:id/Preferences";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Preferences>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update Preferences of this model.
			 */
			public static async Task<Preferences> updatePreferences(Preferences data, string id)
			{
				string APIPath = "user/:id/Preferences";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Preferences>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes Preferences of this model.
			 */
			public static async Task destroyPreferences(string id)
			{
				string APIPath = "user/:id/Preferences";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Find a related item by id for accessTokens
			 */
			public static async Task<AccessToken> findByIdAccessTokens(string id, string fk)
			{
				string APIPath = "user/:id/accessTokens/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<AccessToken>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for accessTokens
			 */
			public static async Task destroyByIdAccessTokens(string id, string fk)
			{
				string APIPath = "user/:id/accessTokens/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for accessTokens
			 */
			public static async Task<AccessToken> updateByIdAccessTokens(AccessToken data, string id, string fk)
			{
				string APIPath = "user/:id/accessTokens/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<AccessToken>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Find a related item by id for roles
			 */
			public static async Task<Role> findByIdRoles(string id, string fk)
			{
				string APIPath = "user/:id/roles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for roles
			 */
			public static async Task destroyByIdRoles(string id, string fk)
			{
				string APIPath = "user/:id/roles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for roles
			 */
			public static async Task<Role> updateByIdRoles(Role data, string id, string fk)
			{
				string APIPath = "user/:id/roles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Add a related item by id for roles
			 */
			public static async Task<RoleMapping> linkRoles(RoleMapping data, string id, string fk)
			{
				string APIPath = "user/:id/roles/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<RoleMapping>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Remove the roles relation to an item by id
			 */
			public static async Task unlinkRoles(string id, string fk)
			{
				string APIPath = "user/:id/roles/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Check the existence of roles relation to an item by id
			 */
			public static async Task<bool> existsRoles(string id, string fk)
			{
				string APIPath = "user/:id/roles/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<bool>() : default(bool);
			}

			/*
			 * Queries credentials of user.
			 */
			public static async Task<IList<UserCredential>> getCredentials(string id, string filter = default(string))
			{
				string APIPath = "user/:id/credentials";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<UserCredential[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in credentials of this model.
			 */
			public static async Task<UserCredential> createCredentials(UserCredential data, string id)
			{
				string APIPath = "user/:id/credentials";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all credentials of this model.
			 */
			public static async Task deleteCredentials(string id)
			{
				string APIPath = "user/:id/credentials";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts credentials of user.
			 */
			public static async Task<double> countCredentials(string id, string where = default(string))
			{
				string APIPath = "user/:id/credentials/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}

			/*
			 * Queries identities of user.
			 */
			public static async Task<IList<UserIdentity>> getIdentities(string id, string filter = default(string))
			{
				string APIPath = "user/:id/identities";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<UserIdentity[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in identities of this model.
			 */
			public static async Task<UserIdentity> createIdentities(UserIdentity data, string id)
			{
				string APIPath = "user/:id/identities";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all identities of this model.
			 */
			public static async Task deleteIdentities(string id)
			{
				string APIPath = "user/:id/identities";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts identities of user.
			 */
			public static async Task<double> countIdentities(string id, string where = default(string))
			{
				string APIPath = "user/:id/identities/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}

			/*
			 * Queries accessTokens of user.
			 */
			public static async Task<IList<AccessToken>> getAccessTokens(string id, string filter = default(string))
			{
				string APIPath = "user/:id/accessTokens";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<AccessToken[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in accessTokens of this model.
			 */
			public static async Task<AccessToken> createAccessTokens(AccessToken data, string id)
			{
				string APIPath = "user/:id/accessTokens";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<AccessToken>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all accessTokens of this model.
			 */
			public static async Task deleteAccessTokens(string id)
			{
				string APIPath = "user/:id/accessTokens";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts accessTokens of user.
			 */
			public static async Task<double> countAccessTokens(string id, string where = default(string))
			{
				string APIPath = "user/:id/accessTokens/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}

			/*
			 * Queries roles of user.
			 */
			public static async Task<IList<Role>> getRoles(string id, string filter = default(string))
			{
				string APIPath = "user/:id/roles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Role[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in roles of this model.
			 */
			public static async Task<Role> createRoles(Role data, string id)
			{
				string APIPath = "user/:id/roles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all roles of this model.
			 */
			public static async Task deleteRoles(string id)
			{
				string APIPath = "user/:id/roles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts roles of user.
			 */
			public static async Task<double> countRoles(string id, string where = default(string))
			{
				string APIPath = "user/:id/roles/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}

			/*
			 * Login a user with username/email and password
			 */
			public static async Task<JObject> login(User credentials, string include = default(string))
			{
				string APIPath = "user/login";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(credentials);
				queryStrings.Add("include", include != null ? include.ToString() : null);
				var response = await Gateway.PerformRequest<JObject>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Logout a user with access token
			 */
			public static async Task logout()
			{
				string APIPath = "user/logout";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Confirm a user registration with email verification token
			 */
			public static async Task confirm(string uid = default(string), string token = default(string), string redirect = default(string))
			{
				string APIPath = "user/confirm";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("uid", uid != null ? uid.ToString() : null);
				queryStrings.Add("token", token != null ? token.ToString() : null);
				queryStrings.Add("redirect", redirect != null ? redirect.ToString() : null);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Reset password for a user with email
			 */
			public static async Task resetPassword(User options)
			{
				string APIPath = "user/reset";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(options);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Send an Email
			 */
			public static async Task<string> sendEmail(Json email, string id)
			{
				string APIPath = "user/:id/sendEmail";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(email);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<string>() : default(string);
			}

			/*
			 * Fetches belongsTo relation user
			 */
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

			/*
			 * Fetches belongsTo relation user
			 */
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

			/*
			 * Fetches belongsTo relation user
			 */
			public static async Task<User> getForRoleMapping(string id, bool refresh = default(bool))
			{
				string APIPath = "RoleMappings/:id/user";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class Trainers : CRUDInterface<Trainer>
		{

			/*
			 * Find a related item by id for credentials
			 */
			public static async Task<UserCredential> findByIdCredentials(string id, string fk)
			{
				string APIPath = "Trainers/:id/credentials/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for credentials
			 */
			public static async Task destroyByIdCredentials(string id, string fk)
			{
				string APIPath = "Trainers/:id/credentials/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for credentials
			 */
			public static async Task<UserCredential> updateByIdCredentials(UserCredential data, string id, string fk)
			{
				string APIPath = "Trainers/:id/credentials/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Find a related item by id for identities
			 */
			public static async Task<UserIdentity> findByIdIdentities(string id, string fk)
			{
				string APIPath = "Trainers/:id/identities/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for identities
			 */
			public static async Task destroyByIdIdentities(string id, string fk)
			{
				string APIPath = "Trainers/:id/identities/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for identities
			 */
			public static async Task<UserIdentity> updateByIdIdentities(UserIdentity data, string id, string fk)
			{
				string APIPath = "Trainers/:id/identities/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Fetches hasOne relation Preferences
			 */
			public static async Task<Preferences> getPreferences(string id, bool refresh = default(bool))
			{
				string APIPath = "Trainers/:id/Preferences";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Preferences>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in Preferences of this model.
			 */
			public static async Task<Preferences> createPreferences(Preferences data, string id)
			{
				string APIPath = "Trainers/:id/Preferences";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Preferences>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update Preferences of this model.
			 */
			public static async Task<Preferences> updatePreferences(Preferences data, string id)
			{
				string APIPath = "Trainers/:id/Preferences";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Preferences>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes Preferences of this model.
			 */
			public static async Task destroyPreferences(string id)
			{
				string APIPath = "Trainers/:id/Preferences";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Find a related item by id for accessTokens
			 */
			public static async Task<AccessToken> findByIdAccessTokens(string id, string fk)
			{
				string APIPath = "Trainers/:id/accessTokens/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<AccessToken>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for accessTokens
			 */
			public static async Task destroyByIdAccessTokens(string id, string fk)
			{
				string APIPath = "Trainers/:id/accessTokens/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for accessTokens
			 */
			public static async Task<AccessToken> updateByIdAccessTokens(AccessToken data, string id, string fk)
			{
				string APIPath = "Trainers/:id/accessTokens/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<AccessToken>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Find a related item by id for roles
			 */
			public static async Task<Role> findByIdRoles(string id, string fk)
			{
				string APIPath = "Trainers/:id/roles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for roles
			 */
			public static async Task destroyByIdRoles(string id, string fk)
			{
				string APIPath = "Trainers/:id/roles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for roles
			 */
			public static async Task<Role> updateByIdRoles(Role data, string id, string fk)
			{
				string APIPath = "Trainers/:id/roles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Add a related item by id for roles
			 */
			public static async Task<RoleMapping> linkRoles(RoleMapping data, string id, string fk)
			{
				string APIPath = "Trainers/:id/roles/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<RoleMapping>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Remove the roles relation to an item by id
			 */
			public static async Task unlinkRoles(string id, string fk)
			{
				string APIPath = "Trainers/:id/roles/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Check the existence of roles relation to an item by id
			 */
			public static async Task<bool> existsRoles(string id, string fk)
			{
				string APIPath = "Trainers/:id/roles/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<bool>() : default(bool);
			}

			/*
			 * Queries credentials of Trainer.
			 */
			public static async Task<IList<UserCredential>> getCredentials(string id, string filter = default(string))
			{
				string APIPath = "Trainers/:id/credentials";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<UserCredential[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in credentials of this model.
			 */
			public static async Task<UserCredential> createCredentials(UserCredential data, string id)
			{
				string APIPath = "Trainers/:id/credentials";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all credentials of this model.
			 */
			public static async Task deleteCredentials(string id)
			{
				string APIPath = "Trainers/:id/credentials";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts credentials of Trainer.
			 */
			public static async Task<double> countCredentials(string id, string where = default(string))
			{
				string APIPath = "Trainers/:id/credentials/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}

			/*
			 * Queries identities of Trainer.
			 */
			public static async Task<IList<UserIdentity>> getIdentities(string id, string filter = default(string))
			{
				string APIPath = "Trainers/:id/identities";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<UserIdentity[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in identities of this model.
			 */
			public static async Task<UserIdentity> createIdentities(UserIdentity data, string id)
			{
				string APIPath = "Trainers/:id/identities";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all identities of this model.
			 */
			public static async Task deleteIdentities(string id)
			{
				string APIPath = "Trainers/:id/identities";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts identities of Trainer.
			 */
			public static async Task<double> countIdentities(string id, string where = default(string))
			{
				string APIPath = "Trainers/:id/identities/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}

			/*
			 * Queries accessTokens of Trainer.
			 */
			public static async Task<IList<AccessToken>> getAccessTokens(string id, string filter = default(string))
			{
				string APIPath = "Trainers/:id/accessTokens";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<AccessToken[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in accessTokens of this model.
			 */
			public static async Task<AccessToken> createAccessTokens(AccessToken data, string id)
			{
				string APIPath = "Trainers/:id/accessTokens";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<AccessToken>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all accessTokens of this model.
			 */
			public static async Task deleteAccessTokens(string id)
			{
				string APIPath = "Trainers/:id/accessTokens";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts accessTokens of Trainer.
			 */
			public static async Task<double> countAccessTokens(string id, string where = default(string))
			{
				string APIPath = "Trainers/:id/accessTokens/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}

			/*
			 * Queries roles of Trainer.
			 */
			public static async Task<IList<Role>> getRoles(string id, string filter = default(string))
			{
				string APIPath = "Trainers/:id/roles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Role[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in roles of this model.
			 */
			public static async Task<Role> createRoles(Role data, string id)
			{
				string APIPath = "Trainers/:id/roles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all roles of this model.
			 */
			public static async Task deleteRoles(string id)
			{
				string APIPath = "Trainers/:id/roles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts roles of Trainer.
			 */
			public static async Task<double> countRoles(string id, string where = default(string))
			{
				string APIPath = "Trainers/:id/roles/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}

			/*
			 * Login a user with username/email and password
			 */
			public static async Task<JObject> login(Trainer credentials, string include = default(string))
			{
				string APIPath = "Trainers/login";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(credentials);
				queryStrings.Add("include", include != null ? include.ToString() : null);
				var response = await Gateway.PerformRequest<JObject>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Logout a user with access token
			 */
			public static async Task logout()
			{
				string APIPath = "Trainers/logout";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Confirm a user registration with email verification token
			 */
			public static async Task confirm(string uid = default(string), string token = default(string), string redirect = default(string))
			{
				string APIPath = "Trainers/confirm";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("uid", uid != null ? uid.ToString() : null);
				queryStrings.Add("token", token != null ? token.ToString() : null);
				queryStrings.Add("redirect", redirect != null ? redirect.ToString() : null);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Reset password for a user with email
			 */
			public static async Task resetPassword(Trainer options)
			{
				string APIPath = "Trainers/reset";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(options);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Find a related item by id for Trainers
			 */
			public static async Task<Trainer> findByIdForGym(string id, string fk)
			{
				string APIPath = "Gyms/:id/Trainers/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Trainer>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for Trainers
			 */
			public static async Task destroyByIdForGym(string id, string fk)
			{
				string APIPath = "Gyms/:id/Trainers/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for Trainers
			 */
			public static async Task<Trainer> updateByIdForGym(Trainer data, string id, string fk)
			{
				string APIPath = "Gyms/:id/Trainers/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Trainer>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries Trainers of Gym.
			 */
			public static async Task<IList<Trainer>> getForGym(string id, string filter = default(string))
			{
				string APIPath = "Gyms/:id/Trainers";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Trainer[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in Trainers of this model.
			 */
			public static async Task<Trainer> createForGym(Trainer data, string id)
			{
				string APIPath = "Gyms/:id/Trainers";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Trainer>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all Trainers of this model.
			 */
			public static async Task deleteForGym(string id)
			{
				string APIPath = "Gyms/:id/Trainers";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts Trainers of Gym.
			 */
			public static async Task<double> countForGym(string id, string where = default(string))
			{
				string APIPath = "Gyms/:id/Trainers/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}

			/*
			 * Fetches belongsTo relation Trainers
			 */
			public static async Task<Trainer> getForWorkOut(string id, bool refresh = default(bool))
			{
				string APIPath = "WorkOuts/:id/Trainers";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Trainer>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class Customers : CRUDInterface<Customer>
		{

			/*
			 * Fetches hasOne relation PaymentData
			 */
			public static async Task<PaymentData> getPaymentData(string id, bool refresh = default(bool))
			{
				string APIPath = "Customers/:id/PaymentData";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<PaymentData>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in PaymentData of this model.
			 */
			public static async Task<PaymentData> createPaymentData(PaymentData data, string id)
			{
				string APIPath = "Customers/:id/PaymentData";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<PaymentData>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update PaymentData of this model.
			 */
			public static async Task<PaymentData> updatePaymentData(PaymentData data, string id)
			{
				string APIPath = "Customers/:id/PaymentData";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<PaymentData>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes PaymentData of this model.
			 */
			public static async Task destroyPaymentData(string id)
			{
				string APIPath = "Customers/:id/PaymentData";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Find a related item by id for credentials
			 */
			public static async Task<UserCredential> findByIdCredentials(string id, string fk)
			{
				string APIPath = "Customers/:id/credentials/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for credentials
			 */
			public static async Task destroyByIdCredentials(string id, string fk)
			{
				string APIPath = "Customers/:id/credentials/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for credentials
			 */
			public static async Task<UserCredential> updateByIdCredentials(UserCredential data, string id, string fk)
			{
				string APIPath = "Customers/:id/credentials/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Find a related item by id for identities
			 */
			public static async Task<UserIdentity> findByIdIdentities(string id, string fk)
			{
				string APIPath = "Customers/:id/identities/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for identities
			 */
			public static async Task destroyByIdIdentities(string id, string fk)
			{
				string APIPath = "Customers/:id/identities/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for identities
			 */
			public static async Task<UserIdentity> updateByIdIdentities(UserIdentity data, string id, string fk)
			{
				string APIPath = "Customers/:id/identities/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Find a related item by id for WorkOuts
			 */
			public static async Task<WorkOut> findByIdWorkOuts(string id, string fk)
			{
				string APIPath = "Customers/:id/WorkOuts/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<WorkOut>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for WorkOuts
			 */
			public static async Task destroyByIdWorkOuts(string id, string fk)
			{
				string APIPath = "Customers/:id/WorkOuts/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for WorkOuts
			 */
			public static async Task<WorkOut> updateByIdWorkOuts(WorkOut data, string id, string fk)
			{
				string APIPath = "Customers/:id/WorkOuts/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<WorkOut>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Find a related item by id for PhysicalData
			 */
			public static async Task<PhysicalDataType> findByIdPhysicalData(string id, string fk)
			{
				string APIPath = "Customers/:id/PhysicalData/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<PhysicalDataType>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for PhysicalData
			 */
			public static async Task destroyByIdPhysicalData(string id, string fk)
			{
				string APIPath = "Customers/:id/PhysicalData/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for PhysicalData
			 */
			public static async Task<PhysicalDataType> updateByIdPhysicalData(PhysicalDataType data, string id, string fk)
			{
				string APIPath = "Customers/:id/PhysicalData/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<PhysicalDataType>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Fetches hasOne relation Preferences
			 */
			public static async Task<Preferences> getPreferences(string id, bool refresh = default(bool))
			{
				string APIPath = "Customers/:id/Preferences";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Preferences>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in Preferences of this model.
			 */
			public static async Task<Preferences> createPreferences(Preferences data, string id)
			{
				string APIPath = "Customers/:id/Preferences";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Preferences>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update Preferences of this model.
			 */
			public static async Task<Preferences> updatePreferences(Preferences data, string id)
			{
				string APIPath = "Customers/:id/Preferences";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Preferences>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes Preferences of this model.
			 */
			public static async Task destroyPreferences(string id)
			{
				string APIPath = "Customers/:id/Preferences";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Find a related item by id for accessTokens
			 */
			public static async Task<AccessToken> findByIdAccessTokens(string id, string fk)
			{
				string APIPath = "Customers/:id/accessTokens/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<AccessToken>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for accessTokens
			 */
			public static async Task destroyByIdAccessTokens(string id, string fk)
			{
				string APIPath = "Customers/:id/accessTokens/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for accessTokens
			 */
			public static async Task<AccessToken> updateByIdAccessTokens(AccessToken data, string id, string fk)
			{
				string APIPath = "Customers/:id/accessTokens/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<AccessToken>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Find a related item by id for roles
			 */
			public static async Task<Role> findByIdRoles(string id, string fk)
			{
				string APIPath = "Customers/:id/roles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for roles
			 */
			public static async Task destroyByIdRoles(string id, string fk)
			{
				string APIPath = "Customers/:id/roles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for roles
			 */
			public static async Task<Role> updateByIdRoles(Role data, string id, string fk)
			{
				string APIPath = "Customers/:id/roles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Add a related item by id for roles
			 */
			public static async Task<RoleMapping> linkRoles(RoleMapping data, string id, string fk)
			{
				string APIPath = "Customers/:id/roles/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<RoleMapping>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Remove the roles relation to an item by id
			 */
			public static async Task unlinkRoles(string id, string fk)
			{
				string APIPath = "Customers/:id/roles/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Check the existence of roles relation to an item by id
			 */
			public static async Task<bool> existsRoles(string id, string fk)
			{
				string APIPath = "Customers/:id/roles/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<bool>() : default(bool);
			}

			/*
			 * Queries credentials of Customer.
			 */
			public static async Task<IList<UserCredential>> getCredentials(string id, string filter = default(string))
			{
				string APIPath = "Customers/:id/credentials";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<UserCredential[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in credentials of this model.
			 */
			public static async Task<UserCredential> createCredentials(UserCredential data, string id)
			{
				string APIPath = "Customers/:id/credentials";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all credentials of this model.
			 */
			public static async Task deleteCredentials(string id)
			{
				string APIPath = "Customers/:id/credentials";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts credentials of Customer.
			 */
			public static async Task<double> countCredentials(string id, string where = default(string))
			{
				string APIPath = "Customers/:id/credentials/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}

			/*
			 * Queries identities of Customer.
			 */
			public static async Task<IList<UserIdentity>> getIdentities(string id, string filter = default(string))
			{
				string APIPath = "Customers/:id/identities";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<UserIdentity[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in identities of this model.
			 */
			public static async Task<UserIdentity> createIdentities(UserIdentity data, string id)
			{
				string APIPath = "Customers/:id/identities";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all identities of this model.
			 */
			public static async Task deleteIdentities(string id)
			{
				string APIPath = "Customers/:id/identities";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts identities of Customer.
			 */
			public static async Task<double> countIdentities(string id, string where = default(string))
			{
				string APIPath = "Customers/:id/identities/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}

			/*
			 * Queries WorkOuts of Customer.
			 */
			public static async Task<IList<WorkOut>> getWorkOuts(string id, string filter = default(string))
			{
				string APIPath = "Customers/:id/WorkOuts";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<WorkOut[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in WorkOuts of this model.
			 */
			public static async Task<WorkOut> createWorkOuts(WorkOut data, string id)
			{
				string APIPath = "Customers/:id/WorkOuts";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<WorkOut>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all WorkOuts of this model.
			 */
			public static async Task deleteWorkOuts(string id)
			{
				string APIPath = "Customers/:id/WorkOuts";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts WorkOuts of Customer.
			 */
			public static async Task<double> countWorkOuts(string id, string where = default(string))
			{
				string APIPath = "Customers/:id/WorkOuts/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}

			/*
			 * Queries PhysicalData of Customer.
			 */
			public static async Task<IList<PhysicalDataType>> getPhysicalData(string id, string filter = default(string))
			{
				string APIPath = "Customers/:id/PhysicalData";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<PhysicalDataType[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in PhysicalData of this model.
			 */
			public static async Task<PhysicalDataType> createPhysicalData(PhysicalDataType data, string id)
			{
				string APIPath = "Customers/:id/PhysicalData";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<PhysicalDataType>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all PhysicalData of this model.
			 */
			public static async Task deletePhysicalData(string id)
			{
				string APIPath = "Customers/:id/PhysicalData";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts PhysicalData of Customer.
			 */
			public static async Task<double> countPhysicalData(string id, string where = default(string))
			{
				string APIPath = "Customers/:id/PhysicalData/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}

			/*
			 * Queries accessTokens of Customer.
			 */
			public static async Task<IList<AccessToken>> getAccessTokens(string id, string filter = default(string))
			{
				string APIPath = "Customers/:id/accessTokens";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<AccessToken[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in accessTokens of this model.
			 */
			public static async Task<AccessToken> createAccessTokens(AccessToken data, string id)
			{
				string APIPath = "Customers/:id/accessTokens";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<AccessToken>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all accessTokens of this model.
			 */
			public static async Task deleteAccessTokens(string id)
			{
				string APIPath = "Customers/:id/accessTokens";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts accessTokens of Customer.
			 */
			public static async Task<double> countAccessTokens(string id, string where = default(string))
			{
				string APIPath = "Customers/:id/accessTokens/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}

			/*
			 * Queries roles of Customer.
			 */
			public static async Task<IList<Role>> getRoles(string id, string filter = default(string))
			{
				string APIPath = "Customers/:id/roles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Role[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in roles of this model.
			 */
			public static async Task<Role> createRoles(Role data, string id)
			{
				string APIPath = "Customers/:id/roles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all roles of this model.
			 */
			public static async Task deleteRoles(string id)
			{
				string APIPath = "Customers/:id/roles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts roles of Customer.
			 */
			public static async Task<double> countRoles(string id, string where = default(string))
			{
				string APIPath = "Customers/:id/roles/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}

			/*
			 * Login a user with username/email and password
			 */
			public static async Task<JObject> login(Customer credentials, string include = default(string))
			{
				string APIPath = "Customers/login";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(credentials);
				queryStrings.Add("include", include != null ? include.ToString() : null);
				var response = await Gateway.PerformRequest<JObject>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Logout a user with access token
			 */
			public static async Task logout()
			{
				string APIPath = "Customers/logout";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Confirm a user registration with email verification token
			 */
			public static async Task confirm(string uid = default(string), string token = default(string), string redirect = default(string))
			{
				string APIPath = "Customers/confirm";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("uid", uid != null ? uid.ToString() : null);
				queryStrings.Add("token", token != null ? token.ToString() : null);
				queryStrings.Add("redirect", redirect != null ? redirect.ToString() : null);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Reset password for a user with email
			 */
			public static async Task resetPassword(Customer options)
			{
				string APIPath = "Customers/reset";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(options);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * 
			 */
			public static async Task<double> getPIN(string id = default(string))
			{
				string APIPath = "Customers/getPIN";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				queryStrings.Add("id", id != null ? id.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}

			/*
			 * Find a related item by id for Customers
			 */
			public static async Task<Customer> findByIdForGym(string id, string fk)
			{
				string APIPath = "Gyms/:id/Customers/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Customer>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for Customers
			 */
			public static async Task destroyByIdForGym(string id, string fk)
			{
				string APIPath = "Gyms/:id/Customers/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for Customers
			 */
			public static async Task<Customer> updateByIdForGym(Customer data, string id, string fk)
			{
				string APIPath = "Gyms/:id/Customers/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Customer>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries Customers of Gym.
			 */
			public static async Task<IList<Customer>> getForGym(string id, string filter = default(string))
			{
				string APIPath = "Gyms/:id/Customers";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Customer[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in Customers of this model.
			 */
			public static async Task<Customer> createForGym(Customer data, string id)
			{
				string APIPath = "Gyms/:id/Customers";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Customer>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all Customers of this model.
			 */
			public static async Task deleteForGym(string id)
			{
				string APIPath = "Gyms/:id/Customers";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts Customers of Gym.
			 */
			public static async Task<double> countForGym(string id, string where = default(string))
			{
				string APIPath = "Gyms/:id/Customers/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
		}
		public class PaymentDatas : CRUDInterface<PaymentData>
		{

			/*
			 * Fetches hasOne relation PaymentData
			 */
			public static async Task<PaymentData> getForCustomer(string id, bool refresh = default(bool))
			{
				string APIPath = "Customers/:id/PaymentData";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<PaymentData>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in PaymentData of this model.
			 */
			public static async Task<PaymentData> createForCustomer(PaymentData data, string id)
			{
				string APIPath = "Customers/:id/PaymentData";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<PaymentData>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update PaymentData of this model.
			 */
			public static async Task<PaymentData> updateForCustomer(PaymentData data, string id)
			{
				string APIPath = "Customers/:id/PaymentData";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<PaymentData>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes PaymentData of this model.
			 */
			public static async Task destroyForCustomer(string id)
			{
				string APIPath = "Customers/:id/PaymentData";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
		}
		public class UserCredentials : CRUDInterface<UserCredential>
		{

			/*
			 * Fetches belongsTo relation user
			 */
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

			/*
			 * Find a related item by id for credentials
			 */
			public static async Task<UserCredential> findByIdForuser(string id, string fk)
			{
				string APIPath = "user/:id/credentials/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for credentials
			 */
			public static async Task destroyByIdForuser(string id, string fk)
			{
				string APIPath = "user/:id/credentials/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for credentials
			 */
			public static async Task<UserCredential> updateByIdForuser(UserCredential data, string id, string fk)
			{
				string APIPath = "user/:id/credentials/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries credentials of user.
			 */
			public static async Task<IList<UserCredential>> getForuser(string id, string filter = default(string))
			{
				string APIPath = "user/:id/credentials";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<UserCredential[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in credentials of this model.
			 */
			public static async Task<UserCredential> createForuser(UserCredential data, string id)
			{
				string APIPath = "user/:id/credentials";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all credentials of this model.
			 */
			public static async Task deleteForuser(string id)
			{
				string APIPath = "user/:id/credentials";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts credentials of user.
			 */
			public static async Task<double> countForuser(string id, string where = default(string))
			{
				string APIPath = "user/:id/credentials/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}

			/*
			 * Find a related item by id for credentials
			 */
			public static async Task<UserCredential> findByIdForTrainer(string id, string fk)
			{
				string APIPath = "Trainers/:id/credentials/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for credentials
			 */
			public static async Task destroyByIdForTrainer(string id, string fk)
			{
				string APIPath = "Trainers/:id/credentials/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for credentials
			 */
			public static async Task<UserCredential> updateByIdForTrainer(UserCredential data, string id, string fk)
			{
				string APIPath = "Trainers/:id/credentials/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries credentials of Trainer.
			 */
			public static async Task<IList<UserCredential>> getForTrainer(string id, string filter = default(string))
			{
				string APIPath = "Trainers/:id/credentials";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<UserCredential[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in credentials of this model.
			 */
			public static async Task<UserCredential> createForTrainer(UserCredential data, string id)
			{
				string APIPath = "Trainers/:id/credentials";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all credentials of this model.
			 */
			public static async Task deleteForTrainer(string id)
			{
				string APIPath = "Trainers/:id/credentials";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts credentials of Trainer.
			 */
			public static async Task<double> countForTrainer(string id, string where = default(string))
			{
				string APIPath = "Trainers/:id/credentials/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}

			/*
			 * Find a related item by id for credentials
			 */
			public static async Task<UserCredential> findByIdForCustomer(string id, string fk)
			{
				string APIPath = "Customers/:id/credentials/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for credentials
			 */
			public static async Task destroyByIdForCustomer(string id, string fk)
			{
				string APIPath = "Customers/:id/credentials/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for credentials
			 */
			public static async Task<UserCredential> updateByIdForCustomer(UserCredential data, string id, string fk)
			{
				string APIPath = "Customers/:id/credentials/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries credentials of Customer.
			 */
			public static async Task<IList<UserCredential>> getForCustomer(string id, string filter = default(string))
			{
				string APIPath = "Customers/:id/credentials";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<UserCredential[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in credentials of this model.
			 */
			public static async Task<UserCredential> createForCustomer(UserCredential data, string id)
			{
				string APIPath = "Customers/:id/credentials";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<UserCredential>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all credentials of this model.
			 */
			public static async Task deleteForCustomer(string id)
			{
				string APIPath = "Customers/:id/credentials";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts credentials of Customer.
			 */
			public static async Task<double> countForCustomer(string id, string where = default(string))
			{
				string APIPath = "Customers/:id/credentials/count";
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

			/*
			 * Fetches belongsTo relation user
			 */
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

			/*
			 * Find a related item by id for identities
			 */
			public static async Task<UserIdentity> findByIdForuser(string id, string fk)
			{
				string APIPath = "user/:id/identities/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for identities
			 */
			public static async Task destroyByIdForuser(string id, string fk)
			{
				string APIPath = "user/:id/identities/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for identities
			 */
			public static async Task<UserIdentity> updateByIdForuser(UserIdentity data, string id, string fk)
			{
				string APIPath = "user/:id/identities/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries identities of user.
			 */
			public static async Task<IList<UserIdentity>> getForuser(string id, string filter = default(string))
			{
				string APIPath = "user/:id/identities";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<UserIdentity[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in identities of this model.
			 */
			public static async Task<UserIdentity> createForuser(UserIdentity data, string id)
			{
				string APIPath = "user/:id/identities";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all identities of this model.
			 */
			public static async Task deleteForuser(string id)
			{
				string APIPath = "user/:id/identities";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts identities of user.
			 */
			public static async Task<double> countForuser(string id, string where = default(string))
			{
				string APIPath = "user/:id/identities/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}

			/*
			 * Find a related item by id for identities
			 */
			public static async Task<UserIdentity> findByIdForTrainer(string id, string fk)
			{
				string APIPath = "Trainers/:id/identities/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for identities
			 */
			public static async Task destroyByIdForTrainer(string id, string fk)
			{
				string APIPath = "Trainers/:id/identities/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for identities
			 */
			public static async Task<UserIdentity> updateByIdForTrainer(UserIdentity data, string id, string fk)
			{
				string APIPath = "Trainers/:id/identities/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries identities of Trainer.
			 */
			public static async Task<IList<UserIdentity>> getForTrainer(string id, string filter = default(string))
			{
				string APIPath = "Trainers/:id/identities";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<UserIdentity[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in identities of this model.
			 */
			public static async Task<UserIdentity> createForTrainer(UserIdentity data, string id)
			{
				string APIPath = "Trainers/:id/identities";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all identities of this model.
			 */
			public static async Task deleteForTrainer(string id)
			{
				string APIPath = "Trainers/:id/identities";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts identities of Trainer.
			 */
			public static async Task<double> countForTrainer(string id, string where = default(string))
			{
				string APIPath = "Trainers/:id/identities/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}

			/*
			 * Find a related item by id for identities
			 */
			public static async Task<UserIdentity> findByIdForCustomer(string id, string fk)
			{
				string APIPath = "Customers/:id/identities/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for identities
			 */
			public static async Task destroyByIdForCustomer(string id, string fk)
			{
				string APIPath = "Customers/:id/identities/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for identities
			 */
			public static async Task<UserIdentity> updateByIdForCustomer(UserIdentity data, string id, string fk)
			{
				string APIPath = "Customers/:id/identities/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries identities of Customer.
			 */
			public static async Task<IList<UserIdentity>> getForCustomer(string id, string filter = default(string))
			{
				string APIPath = "Customers/:id/identities";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<UserIdentity[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in identities of this model.
			 */
			public static async Task<UserIdentity> createForCustomer(UserIdentity data, string id)
			{
				string APIPath = "Customers/:id/identities";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<UserIdentity>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all identities of this model.
			 */
			public static async Task deleteForCustomer(string id)
			{
				string APIPath = "Customers/:id/identities";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts identities of Customer.
			 */
			public static async Task<double> countForCustomer(string id, string where = default(string))
			{
				string APIPath = "Customers/:id/identities/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
		}
		public class AuthProviders : CRUDInterface<AuthProvider>
		{
		}
		public class RoleMappings : CRUDInterface<RoleMapping>
		{

			/*
			 * Fetches belongsTo relation role
			 */
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

			/*
			 * Fetches belongsTo relation user
			 */
			public static async Task<User> getUser(string id, bool refresh = default(bool))
			{
				string APIPath = "RoleMappings/:id/user";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<User>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Find a related item by id for principals
			 */
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

			/*
			 * Delete a related item by id for principals
			 */
			public static async Task destroyByIdForRole(string id, string fk)
			{
				string APIPath = "Roles/:id/principals/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for principals
			 */
			public static async Task<RoleMapping> updateByIdForRole(RoleMapping data, string id, string fk)
			{
				string APIPath = "Roles/:id/principals/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<RoleMapping>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries principals of Role.
			 */
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

			/*
			 * Creates a new instance in principals of this model.
			 */
			public static async Task<RoleMapping> createForRole(RoleMapping data, string id)
			{
				string APIPath = "Roles/:id/principals";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<RoleMapping>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all principals of this model.
			 */
			public static async Task deleteForRole(string id)
			{
				string APIPath = "Roles/:id/principals";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts principals of Role.
			 */
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

			/*
			 * Find a related item by id for principals
			 */
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

			/*
			 * Delete a related item by id for principals
			 */
			public static async Task destroyByIdPrincipals(string id, string fk)
			{
				string APIPath = "Roles/:id/principals/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for principals
			 */
			public static async Task<RoleMapping> updateByIdPrincipals(RoleMapping data, string id, string fk)
			{
				string APIPath = "Roles/:id/principals/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<RoleMapping>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries principals of Role.
			 */
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

			/*
			 * Creates a new instance in principals of this model.
			 */
			public static async Task<RoleMapping> createPrincipals(RoleMapping data, string id)
			{
				string APIPath = "Roles/:id/principals";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<RoleMapping>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all principals of this model.
			 */
			public static async Task deletePrincipals(string id)
			{
				string APIPath = "Roles/:id/principals";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts principals of Role.
			 */
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

			/*
			 * Find a related item by id for roles
			 */
			public static async Task<Role> findByIdForuser(string id, string fk)
			{
				string APIPath = "user/:id/roles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for roles
			 */
			public static async Task destroyByIdForuser(string id, string fk)
			{
				string APIPath = "user/:id/roles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for roles
			 */
			public static async Task<Role> updateByIdForuser(Role data, string id, string fk)
			{
				string APIPath = "user/:id/roles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Add a related item by id for roles
			 */
			public static async Task<RoleMapping> linkForuser(RoleMapping data, string id, string fk)
			{
				string APIPath = "user/:id/roles/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<RoleMapping>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Remove the roles relation to an item by id
			 */
			public static async Task unlinkForuser(string id, string fk)
			{
				string APIPath = "user/:id/roles/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Check the existence of roles relation to an item by id
			 */
			public static async Task<bool> existsForuser(string id, string fk)
			{
				string APIPath = "user/:id/roles/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<bool>() : default(bool);
			}

			/*
			 * Queries roles of user.
			 */
			public static async Task<IList<Role>> getForuser(string id, string filter = default(string))
			{
				string APIPath = "user/:id/roles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Role[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in roles of this model.
			 */
			public static async Task<Role> createForuser(Role data, string id)
			{
				string APIPath = "user/:id/roles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all roles of this model.
			 */
			public static async Task deleteForuser(string id)
			{
				string APIPath = "user/:id/roles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts roles of user.
			 */
			public static async Task<double> countForuser(string id, string where = default(string))
			{
				string APIPath = "user/:id/roles/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}

			/*
			 * Find a related item by id for roles
			 */
			public static async Task<Role> findByIdForTrainer(string id, string fk)
			{
				string APIPath = "Trainers/:id/roles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for roles
			 */
			public static async Task destroyByIdForTrainer(string id, string fk)
			{
				string APIPath = "Trainers/:id/roles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for roles
			 */
			public static async Task<Role> updateByIdForTrainer(Role data, string id, string fk)
			{
				string APIPath = "Trainers/:id/roles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Add a related item by id for roles
			 */
			public static async Task<RoleMapping> linkForTrainer(RoleMapping data, string id, string fk)
			{
				string APIPath = "Trainers/:id/roles/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<RoleMapping>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Remove the roles relation to an item by id
			 */
			public static async Task unlinkForTrainer(string id, string fk)
			{
				string APIPath = "Trainers/:id/roles/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Check the existence of roles relation to an item by id
			 */
			public static async Task<bool> existsForTrainer(string id, string fk)
			{
				string APIPath = "Trainers/:id/roles/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<bool>() : default(bool);
			}

			/*
			 * Queries roles of Trainer.
			 */
			public static async Task<IList<Role>> getForTrainer(string id, string filter = default(string))
			{
				string APIPath = "Trainers/:id/roles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Role[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in roles of this model.
			 */
			public static async Task<Role> createForTrainer(Role data, string id)
			{
				string APIPath = "Trainers/:id/roles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all roles of this model.
			 */
			public static async Task deleteForTrainer(string id)
			{
				string APIPath = "Trainers/:id/roles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts roles of Trainer.
			 */
			public static async Task<double> countForTrainer(string id, string where = default(string))
			{
				string APIPath = "Trainers/:id/roles/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}

			/*
			 * Find a related item by id for roles
			 */
			public static async Task<Role> findByIdForCustomer(string id, string fk)
			{
				string APIPath = "Customers/:id/roles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for roles
			 */
			public static async Task destroyByIdForCustomer(string id, string fk)
			{
				string APIPath = "Customers/:id/roles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for roles
			 */
			public static async Task<Role> updateByIdForCustomer(Role data, string id, string fk)
			{
				string APIPath = "Customers/:id/roles/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Add a related item by id for roles
			 */
			public static async Task<RoleMapping> linkForCustomer(RoleMapping data, string id, string fk)
			{
				string APIPath = "Customers/:id/roles/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<RoleMapping>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Remove the roles relation to an item by id
			 */
			public static async Task unlinkForCustomer(string id, string fk)
			{
				string APIPath = "Customers/:id/roles/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Check the existence of roles relation to an item by id
			 */
			public static async Task<bool> existsForCustomer(string id, string fk)
			{
				string APIPath = "Customers/:id/roles/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<bool>() : default(bool);
			}

			/*
			 * Queries roles of Customer.
			 */
			public static async Task<IList<Role>> getForCustomer(string id, string filter = default(string))
			{
				string APIPath = "Customers/:id/roles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Role[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in roles of this model.
			 */
			public static async Task<Role> createForCustomer(Role data, string id)
			{
				string APIPath = "Customers/:id/roles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Role>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all roles of this model.
			 */
			public static async Task deleteForCustomer(string id)
			{
				string APIPath = "Customers/:id/roles";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts roles of Customer.
			 */
			public static async Task<double> countForCustomer(string id, string where = default(string))
			{
				string APIPath = "Customers/:id/roles/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}

			/*
			 * Fetches belongsTo relation role
			 */
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
		public class Settings : CRUDInterface<Setting>
		{
		}
		public class Gyms : CRUDInterface<Gym>
		{

			/*
			 * Find a related item by id for Customers
			 */
			public static async Task<Customer> findByIdCustomers(string id, string fk)
			{
				string APIPath = "Gyms/:id/Customers/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Customer>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for Customers
			 */
			public static async Task destroyByIdCustomers(string id, string fk)
			{
				string APIPath = "Gyms/:id/Customers/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for Customers
			 */
			public static async Task<Customer> updateByIdCustomers(Customer data, string id, string fk)
			{
				string APIPath = "Gyms/:id/Customers/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Customer>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Find a related item by id for Trainers
			 */
			public static async Task<Trainer> findByIdTrainers(string id, string fk)
			{
				string APIPath = "Gyms/:id/Trainers/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Trainer>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for Trainers
			 */
			public static async Task destroyByIdTrainers(string id, string fk)
			{
				string APIPath = "Gyms/:id/Trainers/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for Trainers
			 */
			public static async Task<Trainer> updateByIdTrainers(Trainer data, string id, string fk)
			{
				string APIPath = "Gyms/:id/Trainers/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Trainer>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Find a related item by id for Exercises
			 */
			public static async Task<Exercise> findByIdExercises(string id, string fk)
			{
				string APIPath = "Gyms/:id/Exercises/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Exercise>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for Exercises
			 */
			public static async Task destroyByIdExercises(string id, string fk)
			{
				string APIPath = "Gyms/:id/Exercises/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for Exercises
			 */
			public static async Task<Exercise> updateByIdExercises(Exercise data, string id, string fk)
			{
				string APIPath = "Gyms/:id/Exercises/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Exercise>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Add a related item by id for Exercises
			 */
			public static async Task<GymExercise> linkExercises(GymExercise data, string id, string fk)
			{
				string APIPath = "Gyms/:id/Exercises/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<GymExercise>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Remove the Exercises relation to an item by id
			 */
			public static async Task unlinkExercises(string id, string fk)
			{
				string APIPath = "Gyms/:id/Exercises/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Check the existence of Exercises relation to an item by id
			 */
			public static async Task<bool> existsExercises(string id, string fk)
			{
				string APIPath = "Gyms/:id/Exercises/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<bool>() : default(bool);
			}

			/*
			 * Find a related item by id for WorkOuts
			 */
			public static async Task<WorkOut> findByIdWorkOuts(string id, string fk)
			{
				string APIPath = "Gyms/:id/WorkOuts/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<WorkOut>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for WorkOuts
			 */
			public static async Task destroyByIdWorkOuts(string id, string fk)
			{
				string APIPath = "Gyms/:id/WorkOuts/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for WorkOuts
			 */
			public static async Task<WorkOut> updateByIdWorkOuts(WorkOut data, string id, string fk)
			{
				string APIPath = "Gyms/:id/WorkOuts/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<WorkOut>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Find a related item by id for ExerciseTypes
			 */
			public static async Task<ExerciseType> findByIdExerciseTypes(string id, string fk)
			{
				string APIPath = "Gyms/:id/ExerciseTypes/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<ExerciseType>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for ExerciseTypes
			 */
			public static async Task destroyByIdExerciseTypes(string id, string fk)
			{
				string APIPath = "Gyms/:id/ExerciseTypes/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for ExerciseTypes
			 */
			public static async Task<ExerciseType> updateByIdExerciseTypes(ExerciseType data, string id, string fk)
			{
				string APIPath = "Gyms/:id/ExerciseTypes/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<ExerciseType>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries Customers of Gym.
			 */
			public static async Task<IList<Customer>> getCustomers(string id, string filter = default(string))
			{
				string APIPath = "Gyms/:id/Customers";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Customer[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in Customers of this model.
			 */
			public static async Task<Customer> createCustomers(Customer data, string id)
			{
				string APIPath = "Gyms/:id/Customers";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Customer>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all Customers of this model.
			 */
			public static async Task deleteCustomers(string id)
			{
				string APIPath = "Gyms/:id/Customers";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts Customers of Gym.
			 */
			public static async Task<double> countCustomers(string id, string where = default(string))
			{
				string APIPath = "Gyms/:id/Customers/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}

			/*
			 * Queries Trainers of Gym.
			 */
			public static async Task<IList<Trainer>> getTrainers(string id, string filter = default(string))
			{
				string APIPath = "Gyms/:id/Trainers";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Trainer[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in Trainers of this model.
			 */
			public static async Task<Trainer> createTrainers(Trainer data, string id)
			{
				string APIPath = "Gyms/:id/Trainers";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Trainer>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all Trainers of this model.
			 */
			public static async Task deleteTrainers(string id)
			{
				string APIPath = "Gyms/:id/Trainers";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts Trainers of Gym.
			 */
			public static async Task<double> countTrainers(string id, string where = default(string))
			{
				string APIPath = "Gyms/:id/Trainers/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}

			/*
			 * Queries Exercises of Gym.
			 */
			public static async Task<IList<Exercise>> getExercises(string id, string filter = default(string))
			{
				string APIPath = "Gyms/:id/Exercises";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Exercise[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in Exercises of this model.
			 */
			public static async Task<Exercise> createExercises(Exercise data, string id)
			{
				string APIPath = "Gyms/:id/Exercises";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Exercise>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all Exercises of this model.
			 */
			public static async Task deleteExercises(string id)
			{
				string APIPath = "Gyms/:id/Exercises";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts Exercises of Gym.
			 */
			public static async Task<double> countExercises(string id, string where = default(string))
			{
				string APIPath = "Gyms/:id/Exercises/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}

			/*
			 * Queries WorkOuts of Gym.
			 */
			public static async Task<IList<WorkOut>> getWorkOuts(string id, string filter = default(string))
			{
				string APIPath = "Gyms/:id/WorkOuts";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<WorkOut[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in WorkOuts of this model.
			 */
			public static async Task<WorkOut> createWorkOuts(WorkOut data, string id)
			{
				string APIPath = "Gyms/:id/WorkOuts";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<WorkOut>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all WorkOuts of this model.
			 */
			public static async Task deleteWorkOuts(string id)
			{
				string APIPath = "Gyms/:id/WorkOuts";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts WorkOuts of Gym.
			 */
			public static async Task<double> countWorkOuts(string id, string where = default(string))
			{
				string APIPath = "Gyms/:id/WorkOuts/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}

			/*
			 * Queries ExerciseTypes of Gym.
			 */
			public static async Task<IList<ExerciseType>> getExerciseTypes(string id, string filter = default(string))
			{
				string APIPath = "Gyms/:id/ExerciseTypes";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<ExerciseType[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in ExerciseTypes of this model.
			 */
			public static async Task<ExerciseType> createExerciseTypes(ExerciseType data, string id)
			{
				string APIPath = "Gyms/:id/ExerciseTypes";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<ExerciseType>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all ExerciseTypes of this model.
			 */
			public static async Task deleteExerciseTypes(string id)
			{
				string APIPath = "Gyms/:id/ExerciseTypes";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts ExerciseTypes of Gym.
			 */
			public static async Task<double> countExerciseTypes(string id, string where = default(string))
			{
				string APIPath = "Gyms/:id/ExerciseTypes/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}

			/*
			 * Returns the gym name
			 */
			public static async Task<string> getGymName(string id)
			{
				string APIPath = "Gyms/:id/getGymName";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<string>() : default(string);
			}

			/*
			 * Returns object.response if the pin code belongs to the gym
			 */
			public static async Task<bool> checkPIN(string id, string pinCode = default(string))
			{
				string APIPath = "Gyms/:id/checkPIN";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("pinCode", pinCode != null ? pinCode.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<bool>() : default(bool);
			}

			/*
			 * Creates a new instance of Physical Data for a specific Customer
			 */
			public static async Task<PhysicalDataType> createPhysicalData(PhysicalDataType data, string id, string customerId)
			{
				string APIPath = "Gyms/:id/Customer/:customerId/physicalData";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":customerId", (string)customerId);
				var response = await Gateway.PerformRequest<PhysicalDataType>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Create/Update the customer payment data
			 */
			public static async Task<PaymentData> paymentData(PaymentData data, string id, string customerId = default(string))
			{
				string APIPath = "Gyms/:id/Customer/paymentData";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("customerId", customerId != null ? customerId.ToString() : null);
				var response = await Gateway.PerformRequest<PaymentData>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class Exercises : CRUDInterface<Exercise>
		{

			/*
			 * Fetches belongsTo relation ExerciseType
			 */
			public static async Task<ExerciseType> getExerciseType(string id, bool refresh = default(bool))
			{
				string APIPath = "Exercises/:id/ExerciseType";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<ExerciseType>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Find a related item by id for Exercises
			 */
			public static async Task<Exercise> findByIdForGym(string id, string fk)
			{
				string APIPath = "Gyms/:id/Exercises/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Exercise>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for Exercises
			 */
			public static async Task destroyByIdForGym(string id, string fk)
			{
				string APIPath = "Gyms/:id/Exercises/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for Exercises
			 */
			public static async Task<Exercise> updateByIdForGym(Exercise data, string id, string fk)
			{
				string APIPath = "Gyms/:id/Exercises/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Exercise>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Add a related item by id for Exercises
			 */
			public static async Task<GymExercise> linkForGym(GymExercise data, string id, string fk)
			{
				string APIPath = "Gyms/:id/Exercises/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<GymExercise>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Remove the Exercises relation to an item by id
			 */
			public static async Task unlinkForGym(string id, string fk)
			{
				string APIPath = "Gyms/:id/Exercises/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Check the existence of Exercises relation to an item by id
			 */
			public static async Task<bool> existsForGym(string id, string fk)
			{
				string APIPath = "Gyms/:id/Exercises/rel/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "HEAD", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<bool>() : default(bool);
			}

			/*
			 * Queries Exercises of Gym.
			 */
			public static async Task<IList<Exercise>> getForGym(string id, string filter = default(string))
			{
				string APIPath = "Gyms/:id/Exercises";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Exercise[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in Exercises of this model.
			 */
			public static async Task<Exercise> createForGym(Exercise data, string id)
			{
				string APIPath = "Gyms/:id/Exercises";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Exercise>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all Exercises of this model.
			 */
			public static async Task deleteForGym(string id)
			{
				string APIPath = "Gyms/:id/Exercises";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts Exercises of Gym.
			 */
			public static async Task<double> countForGym(string id, string where = default(string))
			{
				string APIPath = "Gyms/:id/Exercises/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}

			/*
			 * Find a related item by id for Exercises
			 */
			public static async Task<Exercise> findByIdForWorkOut(string id, string fk)
			{
				string APIPath = "WorkOuts/:id/Exercises/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Exercise>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for Exercises
			 */
			public static async Task destroyByIdForWorkOut(string id, string fk)
			{
				string APIPath = "WorkOuts/:id/Exercises/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for Exercises
			 */
			public static async Task<Exercise> updateByIdForWorkOut(Exercise data, string id, string fk)
			{
				string APIPath = "WorkOuts/:id/Exercises/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Exercise>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries Exercises of WorkOut.
			 */
			public static async Task<IList<Exercise>> getForWorkOut(string id, string filter = default(string))
			{
				string APIPath = "WorkOuts/:id/Exercises";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Exercise[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in Exercises of this model.
			 */
			public static async Task<Exercise> createForWorkOut(Exercise data, string id)
			{
				string APIPath = "WorkOuts/:id/Exercises";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Exercise>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all Exercises of this model.
			 */
			public static async Task deleteForWorkOut(string id)
			{
				string APIPath = "WorkOuts/:id/Exercises";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts Exercises of WorkOut.
			 */
			public static async Task<double> countForWorkOut(string id, string where = default(string))
			{
				string APIPath = "WorkOuts/:id/Exercises/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
		}
		public class WorkOuts : CRUDInterface<WorkOut>
		{

			/*
			 * Fetches belongsTo relation Trainers
			 */
			public static async Task<Trainer> getTrainers(string id, bool refresh = default(bool))
			{
				string APIPath = "WorkOuts/:id/Trainers";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Trainer>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Find a related item by id for Exercises
			 */
			public static async Task<Exercise> findByIdExercises(string id, string fk)
			{
				string APIPath = "WorkOuts/:id/Exercises/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Exercise>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for Exercises
			 */
			public static async Task destroyByIdExercises(string id, string fk)
			{
				string APIPath = "WorkOuts/:id/Exercises/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for Exercises
			 */
			public static async Task<Exercise> updateByIdExercises(Exercise data, string id, string fk)
			{
				string APIPath = "WorkOuts/:id/Exercises/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<Exercise>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries Exercises of WorkOut.
			 */
			public static async Task<IList<Exercise>> getExercises(string id, string filter = default(string))
			{
				string APIPath = "WorkOuts/:id/Exercises";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<Exercise[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in Exercises of this model.
			 */
			public static async Task<Exercise> createExercises(Exercise data, string id)
			{
				string APIPath = "WorkOuts/:id/Exercises";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Exercise>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all Exercises of this model.
			 */
			public static async Task deleteExercises(string id)
			{
				string APIPath = "WorkOuts/:id/Exercises";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts Exercises of WorkOut.
			 */
			public static async Task<double> countExercises(string id, string where = default(string))
			{
				string APIPath = "WorkOuts/:id/Exercises/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}

			/*
			 * Find a related item by id for WorkOuts
			 */
			public static async Task<WorkOut> findByIdForCustomer(string id, string fk)
			{
				string APIPath = "Customers/:id/WorkOuts/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<WorkOut>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for WorkOuts
			 */
			public static async Task destroyByIdForCustomer(string id, string fk)
			{
				string APIPath = "Customers/:id/WorkOuts/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for WorkOuts
			 */
			public static async Task<WorkOut> updateByIdForCustomer(WorkOut data, string id, string fk)
			{
				string APIPath = "Customers/:id/WorkOuts/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<WorkOut>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries WorkOuts of Customer.
			 */
			public static async Task<IList<WorkOut>> getForCustomer(string id, string filter = default(string))
			{
				string APIPath = "Customers/:id/WorkOuts";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<WorkOut[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in WorkOuts of this model.
			 */
			public static async Task<WorkOut> createForCustomer(WorkOut data, string id)
			{
				string APIPath = "Customers/:id/WorkOuts";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<WorkOut>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all WorkOuts of this model.
			 */
			public static async Task deleteForCustomer(string id)
			{
				string APIPath = "Customers/:id/WorkOuts";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts WorkOuts of Customer.
			 */
			public static async Task<double> countForCustomer(string id, string where = default(string))
			{
				string APIPath = "Customers/:id/WorkOuts/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}

			/*
			 * Find a related item by id for WorkOuts
			 */
			public static async Task<WorkOut> findByIdForGym(string id, string fk)
			{
				string APIPath = "Gyms/:id/WorkOuts/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<WorkOut>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for WorkOuts
			 */
			public static async Task destroyByIdForGym(string id, string fk)
			{
				string APIPath = "Gyms/:id/WorkOuts/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for WorkOuts
			 */
			public static async Task<WorkOut> updateByIdForGym(WorkOut data, string id, string fk)
			{
				string APIPath = "Gyms/:id/WorkOuts/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<WorkOut>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries WorkOuts of Gym.
			 */
			public static async Task<IList<WorkOut>> getForGym(string id, string filter = default(string))
			{
				string APIPath = "Gyms/:id/WorkOuts";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<WorkOut[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in WorkOuts of this model.
			 */
			public static async Task<WorkOut> createForGym(WorkOut data, string id)
			{
				string APIPath = "Gyms/:id/WorkOuts";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<WorkOut>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all WorkOuts of this model.
			 */
			public static async Task deleteForGym(string id)
			{
				string APIPath = "Gyms/:id/WorkOuts";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts WorkOuts of Gym.
			 */
			public static async Task<double> countForGym(string id, string where = default(string))
			{
				string APIPath = "Gyms/:id/WorkOuts/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
		}
		public class ExerciseTypes : CRUDInterface<ExerciseType>
		{

			/*
			 * Find a related item by id for ExerciseTypes
			 */
			public static async Task<ExerciseType> findByIdForGym(string id, string fk)
			{
				string APIPath = "Gyms/:id/ExerciseTypes/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<ExerciseType>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for ExerciseTypes
			 */
			public static async Task destroyByIdForGym(string id, string fk)
			{
				string APIPath = "Gyms/:id/ExerciseTypes/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for ExerciseTypes
			 */
			public static async Task<ExerciseType> updateByIdForGym(ExerciseType data, string id, string fk)
			{
				string APIPath = "Gyms/:id/ExerciseTypes/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<ExerciseType>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries ExerciseTypes of Gym.
			 */
			public static async Task<IList<ExerciseType>> getForGym(string id, string filter = default(string))
			{
				string APIPath = "Gyms/:id/ExerciseTypes";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<ExerciseType[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in ExerciseTypes of this model.
			 */
			public static async Task<ExerciseType> createForGym(ExerciseType data, string id)
			{
				string APIPath = "Gyms/:id/ExerciseTypes";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<ExerciseType>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all ExerciseTypes of this model.
			 */
			public static async Task deleteForGym(string id)
			{
				string APIPath = "Gyms/:id/ExerciseTypes";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts ExerciseTypes of Gym.
			 */
			public static async Task<double> countForGym(string id, string where = default(string))
			{
				string APIPath = "Gyms/:id/ExerciseTypes/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}

			/*
			 * Fetches belongsTo relation ExerciseType
			 */
			public static async Task<ExerciseType> getForExercise(string id, bool refresh = default(bool))
			{
				string APIPath = "Exercises/:id/ExerciseType";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<ExerciseType>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}
		}
		public class GeneralActivityTypes : CRUDInterface<GeneralActivityType>
		{
		}
		public class MaritalStatusTypes : CRUDInterface<MaritalStatusType>
		{
		}
		public class PhysicalDataTypes : CRUDInterface<PhysicalDataType>
		{

			/*
			 * Find a related item by id for PhysicalData
			 */
			public static async Task<PhysicalDataType> findByIdForCustomer(string id, string fk)
			{
				string APIPath = "Customers/:id/PhysicalData/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<PhysicalDataType>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Delete a related item by id for PhysicalData
			 */
			public static async Task destroyByIdForCustomer(string id, string fk)
			{
				string APIPath = "Customers/:id/PhysicalData/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Update a related item by id for PhysicalData
			 */
			public static async Task<PhysicalDataType> updateByIdForCustomer(PhysicalDataType data, string id, string fk)
			{
				string APIPath = "Customers/:id/PhysicalData/:fk";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				APIPath = APIPath.Replace(":fk", (string)fk);
				var response = await Gateway.PerformRequest<PhysicalDataType>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Queries PhysicalData of Customer.
			 */
			public static async Task<IList<PhysicalDataType>> getForCustomer(string id, string filter = default(string))
			{
				string APIPath = "Customers/:id/PhysicalData";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("filter", filter != null ? filter.ToString() : null);
				var response = await Gateway.PerformRequest<PhysicalDataType[]>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in PhysicalData of this model.
			 */
			public static async Task<PhysicalDataType> createForCustomer(PhysicalDataType data, string id)
			{
				string APIPath = "Customers/:id/PhysicalData";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<PhysicalDataType>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes all PhysicalData of this model.
			 */
			public static async Task deleteForCustomer(string id)
			{
				string APIPath = "Customers/:id/PhysicalData";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Counts PhysicalData of Customer.
			 */
			public static async Task<double> countForCustomer(string id, string where = default(string))
			{
				string APIPath = "Customers/:id/PhysicalData/count";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("where", where != null ? where.ToString() : null);
				var response = await Gateway.PerformRequest<object>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response != null ? JObject.Parse(response.ToString()).First.First.ToObject<double>() : default(double);
			}
		}
		public class Preferencess : CRUDInterface<Preferences>
		{

			/*
			 * Fetches hasOne relation Preferences
			 */
			public static async Task<Preferences> getForuser(string id, bool refresh = default(bool))
			{
				string APIPath = "user/:id/Preferences";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Preferences>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in Preferences of this model.
			 */
			public static async Task<Preferences> createForuser(Preferences data, string id)
			{
				string APIPath = "user/:id/Preferences";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Preferences>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update Preferences of this model.
			 */
			public static async Task<Preferences> updateForuser(Preferences data, string id)
			{
				string APIPath = "user/:id/Preferences";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Preferences>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes Preferences of this model.
			 */
			public static async Task destroyForuser(string id)
			{
				string APIPath = "user/:id/Preferences";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Fetches hasOne relation Preferences
			 */
			public static async Task<Preferences> getForTrainer(string id, bool refresh = default(bool))
			{
				string APIPath = "Trainers/:id/Preferences";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Preferences>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in Preferences of this model.
			 */
			public static async Task<Preferences> createForTrainer(Preferences data, string id)
			{
				string APIPath = "Trainers/:id/Preferences";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Preferences>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update Preferences of this model.
			 */
			public static async Task<Preferences> updateForTrainer(Preferences data, string id)
			{
				string APIPath = "Trainers/:id/Preferences";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Preferences>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes Preferences of this model.
			 */
			public static async Task destroyForTrainer(string id)
			{
				string APIPath = "Trainers/:id/Preferences";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}

			/*
			 * Fetches hasOne relation Preferences
			 */
			public static async Task<Preferences> getForCustomer(string id, bool refresh = default(bool))
			{
				string APIPath = "Customers/:id/Preferences";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				queryStrings.Add("refresh", refresh != null ? refresh.ToString() : null);
				var response = await Gateway.PerformRequest<Preferences>(APIPath, bodyJSON, "GET", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Creates a new instance in Preferences of this model.
			 */
			public static async Task<Preferences> createForCustomer(Preferences data, string id)
			{
				string APIPath = "Customers/:id/Preferences";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Preferences>(APIPath, bodyJSON, "POST", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Update Preferences of this model.
			 */
			public static async Task<Preferences> updateForCustomer(Preferences data, string id)
			{
				string APIPath = "Customers/:id/Preferences";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				bodyJSON = JsonConvert.SerializeObject(data);
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<Preferences>(APIPath, bodyJSON, "PUT", queryStrings).ConfigureAwait(false);
				return response;
			}

			/*
			 * Deletes Preferences of this model.
			 */
			public static async Task destroyForCustomer(string id)
			{
				string APIPath = "Customers/:id/Preferences";
				IDictionary<string, string> queryStrings = new Dictionary<string, string>();
				string bodyJSON = "";
				APIPath = APIPath.Replace(":id", (string)id);
				var response = await Gateway.PerformRequest<string>(APIPath, bodyJSON, "DELETE", queryStrings).ConfigureAwait(false);
				
			}
		}
		public class Emails : CRUDInterface<Email>
		{
		}
		
	}
}

/**
 *** Dynamic Models ***
 */

namespace LBXamarinSDK
{
	public partial class User : LBModel
	{
		[JsonProperty ("realm", NullValueHandling = NullValueHandling.Ignore)]
		public String realm { get; set; }

		[JsonProperty ("username", NullValueHandling = NullValueHandling.Ignore)]
		public String username { get; set; }

		[JsonProperty ("password", NullValueHandling = NullValueHandling.Ignore)]
		public String password { get; set; }

		[JsonProperty ("credentials", NullValueHandling = NullValueHandling.Ignore)]
		public Object credentials { get; set; }

		[JsonProperty ("challenges", NullValueHandling = NullValueHandling.Ignore)]
		public Object challenges { get; set; }

		[JsonProperty ("email", NullValueHandling = NullValueHandling.Ignore)]
		public String email { get; set; }

		[JsonIgnore]
		public bool emailVerified
		{
			get { return _emailVerified ?? new bool(); }
			set { _emailVerified = value; }
		}
		[JsonProperty ("emailVerified", NullValueHandling = NullValueHandling.Ignore)]
		private bool? _emailVerified { get; set; }

		[JsonProperty ("verificationToken", NullValueHandling = NullValueHandling.Ignore)]
		public String verificationToken { get; set; }

		[JsonProperty ("status", NullValueHandling = NullValueHandling.Ignore)]
		public String status { get; set; }

		[JsonIgnore]
		public DateTime created
		{
			get { return _created ?? new DateTime(); }
			set { _created = value; }
		}
		[JsonProperty ("created", NullValueHandling = NullValueHandling.Ignore)]
		private DateTime? _created { get; set; }

		[JsonIgnore]
		public DateTime lastUpdated
		{
			get { return _lastUpdated ?? new DateTime(); }
			set { _lastUpdated = value; }
		}
		[JsonProperty ("lastUpdated", NullValueHandling = NullValueHandling.Ignore)]
		private DateTime? _lastUpdated { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class Trainer : LBModel
	{
		[JsonProperty ("firstName", NullValueHandling = NullValueHandling.Ignore)]
		public String firstName { get; set; }

		[JsonProperty ("lastName", NullValueHandling = NullValueHandling.Ignore)]
		public String lastName { get; set; }

		[JsonProperty ("address", NullValueHandling = NullValueHandling.Ignore)]
		public String address { get; set; }

		[JsonProperty ("phone", NullValueHandling = NullValueHandling.Ignore)]
		public String phone { get; set; }

		[JsonProperty ("gymId", NullValueHandling = NullValueHandling.Ignore)]
		public String gymId { get; set; }

		[JsonProperty ("realm", NullValueHandling = NullValueHandling.Ignore)]
		public String realm { get; set; }

		[JsonProperty ("username", NullValueHandling = NullValueHandling.Ignore)]
		public String username { get; set; }

		[JsonProperty ("password", NullValueHandling = NullValueHandling.Ignore)]
		public String password { get; set; }

		[JsonProperty ("credentials", NullValueHandling = NullValueHandling.Ignore)]
		public Object credentials { get; set; }

		[JsonProperty ("challenges", NullValueHandling = NullValueHandling.Ignore)]
		public Object challenges { get; set; }

		[JsonProperty ("email", NullValueHandling = NullValueHandling.Ignore)]
		public String email { get; set; }

		[JsonIgnore]
		public bool emailVerified
		{
			get { return _emailVerified ?? new bool(); }
			set { _emailVerified = value; }
		}
		[JsonProperty ("emailVerified", NullValueHandling = NullValueHandling.Ignore)]
		private bool? _emailVerified { get; set; }

		[JsonProperty ("verificationToken", NullValueHandling = NullValueHandling.Ignore)]
		public String verificationToken { get; set; }

		[JsonProperty ("status", NullValueHandling = NullValueHandling.Ignore)]
		public String status { get; set; }

		[JsonIgnore]
		public DateTime created
		{
			get { return _created ?? new DateTime(); }
			set { _created = value; }
		}
		[JsonProperty ("created", NullValueHandling = NullValueHandling.Ignore)]
		private DateTime? _created { get; set; }

		[JsonIgnore]
		public DateTime lastUpdated
		{
			get { return _lastUpdated ?? new DateTime(); }
			set { _lastUpdated = value; }
		}
		[JsonProperty ("lastUpdated", NullValueHandling = NullValueHandling.Ignore)]
		private DateTime? _lastUpdated { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class Customer : LBModel
	{
		[JsonProperty ("firstName", NullValueHandling = NullValueHandling.Ignore)]
		public String firstName { get; set; }

		[JsonProperty ("lastName", NullValueHandling = NullValueHandling.Ignore)]
		public String lastName { get; set; }

		[JsonProperty ("address", NullValueHandling = NullValueHandling.Ignore)]
		public String address { get; set; }

		[JsonIgnore]
		public DateTime dateOfBirth
		{
			get { return _dateOfBirth ?? new DateTime(); }
			set { _dateOfBirth = value; }
		}
		[JsonProperty ("dateOfBirth", NullValueHandling = NullValueHandling.Ignore)]
		private DateTime? _dateOfBirth { get; set; }

		[JsonProperty ("mobilePhone", NullValueHandling = NullValueHandling.Ignore)]
		public String mobilePhone { get; set; }

		[JsonProperty ("homePhone", NullValueHandling = NullValueHandling.Ignore)]
		public String homePhone { get; set; }

		[JsonProperty ("maritalStatus", NullValueHandling = NullValueHandling.Ignore)]
		public String maritalStatus { get; set; }

		[JsonProperty ("generalActivity", NullValueHandling = NullValueHandling.Ignore)]
		public String generalActivity { get; set; }

		[JsonProperty ("comments", NullValueHandling = NullValueHandling.Ignore)]
		public String comments { get; set; }

		[JsonProperty ("medicalHistory", NullValueHandling = NullValueHandling.Ignore)]
		public String medicalHistory { get; set; }

		[JsonProperty ("goals", NullValueHandling = NullValueHandling.Ignore)]
		public String goals { get; set; }

		[JsonIgnore]
		public double idNumber
		{
			get { return _idNumber ?? new double(); }
			set { _idNumber = value; }
		}
		[JsonProperty ("idNumber", NullValueHandling = NullValueHandling.Ignore)]
		private double? _idNumber { get; set; }

		[JsonProperty ("PIN", NullValueHandling = NullValueHandling.Ignore)]
		public String PIN { get; set; }

		[JsonProperty ("gymId", NullValueHandling = NullValueHandling.Ignore)]
		public String gymId { get; set; }

		[JsonProperty ("realm", NullValueHandling = NullValueHandling.Ignore)]
		public String realm { get; set; }

		[JsonProperty ("username", NullValueHandling = NullValueHandling.Ignore)]
		public String username { get; set; }

		[JsonProperty ("password", NullValueHandling = NullValueHandling.Ignore)]
		public String password { get; set; }

		[JsonProperty ("credentials", NullValueHandling = NullValueHandling.Ignore)]
		public Object credentials { get; set; }

		[JsonProperty ("challenges", NullValueHandling = NullValueHandling.Ignore)]
		public Object challenges { get; set; }

		[JsonProperty ("email", NullValueHandling = NullValueHandling.Ignore)]
		public String email { get; set; }

		[JsonIgnore]
		public bool emailVerified
		{
			get { return _emailVerified ?? new bool(); }
			set { _emailVerified = value; }
		}
		[JsonProperty ("emailVerified", NullValueHandling = NullValueHandling.Ignore)]
		private bool? _emailVerified { get; set; }

		[JsonProperty ("verificationToken", NullValueHandling = NullValueHandling.Ignore)]
		public String verificationToken { get; set; }

		[JsonProperty ("status", NullValueHandling = NullValueHandling.Ignore)]
		public String status { get; set; }

		[JsonIgnore]
		public DateTime created
		{
			get { return _created ?? new DateTime(); }
			set { _created = value; }
		}
		[JsonProperty ("created", NullValueHandling = NullValueHandling.Ignore)]
		private DateTime? _created { get; set; }

		[JsonIgnore]
		public DateTime lastUpdated
		{
			get { return _lastUpdated ?? new DateTime(); }
			set { _lastUpdated = value; }
		}
		[JsonProperty ("lastUpdated", NullValueHandling = NullValueHandling.Ignore)]
		private DateTime? _lastUpdated { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class PaymentData : LBModel
	{
		[JsonIgnore]
		public DateTime lastPayment
		{
			get { return _lastPayment ?? new DateTime(); }
			set { _lastPayment = value; }
		}
		[JsonProperty ("lastPayment", NullValueHandling = NullValueHandling.Ignore)]
		private DateTime? _lastPayment { get; set; }

		[JsonIgnore]
		public DateTime expireDate
		{
			get { return _expireDate ?? new DateTime(); }
			set { _expireDate = value; }
		}
		[JsonProperty ("expireDate", NullValueHandling = NullValueHandling.Ignore)]
		private DateTime? _expireDate { get; set; }

		[JsonIgnore]
		public double method
		{
			get { return _method ?? new double(); }
			set { _method = value; }
		}
		[JsonProperty ("method", NullValueHandling = NullValueHandling.Ignore)]
		private double? _method { get; set; }

		[JsonIgnore]
		public bool expired
		{
			get { return _expired ?? new bool(); }
			set { _expired = value; }
		}
		[JsonProperty ("expired", NullValueHandling = NullValueHandling.Ignore)]
		private bool? _expired { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		[JsonProperty ("customerId", NullValueHandling = NullValueHandling.Ignore)]
		public String customerId { get; set; }

		
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class UserCredential : LBModel
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

		[JsonIgnore]
		public DateTime created
		{
			get { return _created ?? new DateTime(); }
			set { _created = value; }
		}
		[JsonProperty ("created", NullValueHandling = NullValueHandling.Ignore)]
		private DateTime? _created { get; set; }

		[JsonIgnore]
		public DateTime modified
		{
			get { return _modified ?? new DateTime(); }
			set { _modified = value; }
		}
		[JsonProperty ("modified", NullValueHandling = NullValueHandling.Ignore)]
		private DateTime? _modified { get; set; }

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
	public partial class UserIdentity : LBModel
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

		[JsonIgnore]
		public DateTime created
		{
			get { return _created ?? new DateTime(); }
			set { _created = value; }
		}
		[JsonProperty ("created", NullValueHandling = NullValueHandling.Ignore)]
		private DateTime? _created { get; set; }

		[JsonIgnore]
		public DateTime modified
		{
			get { return _modified ?? new DateTime(); }
			set { _modified = value; }
		}
		[JsonProperty ("modified", NullValueHandling = NullValueHandling.Ignore)]
		private DateTime? _modified { get; set; }

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
	public partial class AuthProvider : LBModel
	{
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class RoleMapping : LBModel
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
	public partial class Role : LBModel
	{
		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		[JsonProperty ("name", NullValueHandling = NullValueHandling.Ignore)]
		public String name { get; set; }

		[JsonProperty ("description", NullValueHandling = NullValueHandling.Ignore)]
		public String description { get; set; }

		[JsonIgnore]
		public DateTime created
		{
			get { return _created ?? new DateTime(); }
			set { _created = value; }
		}
		[JsonProperty ("created", NullValueHandling = NullValueHandling.Ignore)]
		private DateTime? _created { get; set; }

		[JsonIgnore]
		public DateTime modified
		{
			get { return _modified ?? new DateTime(); }
			set { _modified = value; }
		}
		[JsonProperty ("modified", NullValueHandling = NullValueHandling.Ignore)]
		private DateTime? _modified { get; set; }

		
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class Setting : LBModel
	{
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
	public partial class Gym : LBModel
	{
		[JsonProperty ("name", NullValueHandling = NullValueHandling.Ignore)]
		public String name { get; set; }

		[JsonProperty ("address", NullValueHandling = NullValueHandling.Ignore)]
		public String address { get; set; }

		[JsonProperty ("PIN", NullValueHandling = NullValueHandling.Ignore)]
		public String PIN { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class Exercise : LBModel
	{
		[JsonIgnore]
		public double seat
		{
			get { return _seat ?? new double(); }
			set { _seat = value; }
		}
		[JsonProperty ("seat", NullValueHandling = NullValueHandling.Ignore)]
		private double? _seat { get; set; }

		[JsonIgnore]
		public double back
		{
			get { return _back ?? new double(); }
			set { _back = value; }
		}
		[JsonProperty ("back", NullValueHandling = NullValueHandling.Ignore)]
		private double? _back { get; set; }

		[JsonIgnore]
		public double knees
		{
			get { return _knees ?? new double(); }
			set { _knees = value; }
		}
		[JsonProperty ("knees", NullValueHandling = NullValueHandling.Ignore)]
		private double? _knees { get; set; }

		[JsonIgnore]
		public double distance
		{
			get { return _distance ?? new double(); }
			set { _distance = value; }
		}
		[JsonProperty ("distance", NullValueHandling = NullValueHandling.Ignore)]
		private double? _distance { get; set; }

		[JsonIgnore]
		public double angle
		{
			get { return _angle ?? new double(); }
			set { _angle = value; }
		}
		[JsonProperty ("angle", NullValueHandling = NullValueHandling.Ignore)]
		private double? _angle { get; set; }

		[JsonIgnore]
		public bool cushion
		{
			get { return _cushion ?? new bool(); }
			set { _cushion = value; }
		}
		[JsonProperty ("cushion", NullValueHandling = NullValueHandling.Ignore)]
		private bool? _cushion { get; set; }

		[JsonIgnore]
		public double weightLeft
		{
			get { return _weightLeft ?? new double(); }
			set { _weightLeft = value; }
		}
		[JsonProperty ("weightLeft", NullValueHandling = NullValueHandling.Ignore)]
		private double? _weightLeft { get; set; }

		[JsonIgnore]
		public double weightRight
		{
			get { return _weightRight ?? new double(); }
			set { _weightRight = value; }
		}
		[JsonProperty ("weightRight", NullValueHandling = NullValueHandling.Ignore)]
		private double? _weightRight { get; set; }

		[JsonIgnore]
		public double totalWeight
		{
			get { return _totalWeight ?? new double(); }
			set { _totalWeight = value; }
		}
		[JsonProperty ("totalWeight", NullValueHandling = NullValueHandling.Ignore)]
		private double? _totalWeight { get; set; }

		[JsonIgnore]
		public DateTime duration
		{
			get { return _duration ?? new DateTime(); }
			set { _duration = value; }
		}
		[JsonProperty ("duration", NullValueHandling = NullValueHandling.Ignore)]
		private DateTime? _duration { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		[JsonProperty ("workoutId", NullValueHandling = NullValueHandling.Ignore)]
		public String workoutId { get; set; }

		[JsonProperty ("exerciseTypeId", NullValueHandling = NullValueHandling.Ignore)]
		public String exerciseTypeId { get; set; }

		
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class WorkOut : LBModel
	{
		[JsonProperty ("customerId", NullValueHandling = NullValueHandling.Ignore)]
		public String customerId { get; set; }

		[JsonProperty ("gymId", NullValueHandling = NullValueHandling.Ignore)]
		public String gymId { get; set; }

		[JsonIgnore]
		public DateTime workOutDateTime
		{
			get { return _workOutDateTime ?? new DateTime(); }
			set { _workOutDateTime = value; }
		}
		[JsonProperty ("workOutDateTime", NullValueHandling = NullValueHandling.Ignore)]
		private DateTime? _workOutDateTime { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		[JsonProperty ("trainerId", NullValueHandling = NullValueHandling.Ignore)]
		public String trainerId { get; set; }

		
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class ExerciseType : LBModel
	{
		[JsonProperty ("name", NullValueHandling = NullValueHandling.Ignore)]
		public String name { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		[JsonProperty ("gymId", NullValueHandling = NullValueHandling.Ignore)]
		public String gymId { get; set; }

		
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class GeneralActivityType : LBModel
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
	public partial class MaritalStatusType : LBModel
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
	public partial class PhysicalDataType : LBModel
	{
		[JsonIgnore]
		public DateTime date
		{
			get { return _date ?? new DateTime(); }
			set { _date = value; }
		}
		[JsonProperty ("date", NullValueHandling = NullValueHandling.Ignore)]
		private DateTime? _date { get; set; }

		[JsonIgnore]
		public double weight
		{
			get { return _weight ?? new double(); }
			set { _weight = value; }
		}
		[JsonProperty ("weight", NullValueHandling = NullValueHandling.Ignore)]
		private double? _weight { get; set; }

		[JsonIgnore]
		public double bmi
		{
			get { return _bmi ?? new double(); }
			set { _bmi = value; }
		}
		[JsonProperty ("bmi", NullValueHandling = NullValueHandling.Ignore)]
		private double? _bmi { get; set; }

		[JsonIgnore]
		public double bodyFat
		{
			get { return _bodyFat ?? new double(); }
			set { _bodyFat = value; }
		}
		[JsonProperty ("bodyFat", NullValueHandling = NullValueHandling.Ignore)]
		private double? _bodyFat { get; set; }

		[JsonIgnore]
		public double muscleMass
		{
			get { return _muscleMass ?? new double(); }
			set { _muscleMass = value; }
		}
		[JsonProperty ("muscleMass", NullValueHandling = NullValueHandling.Ignore)]
		private double? _muscleMass { get; set; }

		[JsonIgnore]
		public double activityLevel
		{
			get { return _activityLevel ?? new double(); }
			set { _activityLevel = value; }
		}
		[JsonProperty ("activityLevel", NullValueHandling = NullValueHandling.Ignore)]
		private double? _activityLevel { get; set; }

		[JsonProperty ("customerId", NullValueHandling = NullValueHandling.Ignore)]
		public String customerId { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}
	public partial class Preferences : LBModel
	{
		[JsonProperty ("emailMessaging", NullValueHandling = NullValueHandling.Ignore)]
		public String emailMessaging { get; set; }

		[JsonProperty ("emailPassword", NullValueHandling = NullValueHandling.Ignore)]
		public String emailPassword { get; set; }

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
	public partial class Email : LBModel
	{
		[JsonProperty ("to", NullValueHandling = NullValueHandling.Ignore)]
		public String to { get; set; }

		[JsonProperty ("from", NullValueHandling = NullValueHandling.Ignore)]
		public String from { get; set; }

		[JsonProperty ("subject", NullValueHandling = NullValueHandling.Ignore)]
		public String subject { get; set; }

		[JsonProperty ("text", NullValueHandling = NullValueHandling.Ignore)]
		public String text { get; set; }

		[JsonProperty ("html", NullValueHandling = NullValueHandling.Ignore)]
		public String html { get; set; }

		[JsonProperty ("id", NullValueHandling = NullValueHandling.Ignore)]
		public string id { get; set; }

		
		// This method identifies the ID field
		public override string getID()
		{
			return id;
		}
	}

	// Relationship classes:
	public class GymExercise : LBModel
	{
		[Newtonsoft.Json.JsonProperty("id")]
        public string id { get; set; }

		[Newtonsoft.Json.JsonProperty("gymId")]
        public string gymId { get; set; }

		[Newtonsoft.Json.JsonProperty("exerciseId")]
        public string exerciseId { get; set; }
	}

}
// Eof
