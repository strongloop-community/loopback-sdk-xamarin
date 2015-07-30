using System;
using Android.App;
using Android.Widget;
using Android.OS;
using LBXamarinSDK;
using LBXamarinSDK.LBRepo;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using ExampleApp;

namespace Agents
{
	[Activity (Label = "Loopback SDK Usage Example", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity
	{
		bool connected = false;
		System.Timers.Timer connectedTimer;
		System.Timers.Timer dataReloadTimer;
		IList<Mission> missions = new List<Mission>();

		Button loginButton;
		Button logoutButton;
		Button missionCreateButton;
		ListView listView;
		TextView missionPriorityText;
		TextView geoLocationText;
		EditText passwordText;
		EditText missionDescriptionText;
		EditText priorityText;
		EditText latitudeText;
		EditText longitudeText;
		TextView creationStatus;
		TextView connectedStatus;
		EditText loginText;
		TextView loginStatus;

		/**
		 * Initializes all references to a particular view component
		 */
		private void initUiReferences()
		{
			loginButton = FindViewById<Button> (Resource.Id.button1);
			logoutButton = FindViewById<Button> (Resource.Id.button2);
			missionCreateButton = FindViewById<Button> (Resource.Id.button3);
			listView = FindViewById<ListView> (Resource.Id.listView1);
			loginStatus = FindViewById<TextView> (Resource.Id.textView3);
			missionPriorityText = FindViewById<TextView> (Resource.Id.textView7);
			geoLocationText = FindViewById<TextView> (Resource.Id.textView8);
			connectedStatus = FindViewById<TextView> (Resource.Id.textView5);
			creationStatus = FindViewById<TextView> (Resource.Id.textView13);
			passwordText = FindViewById<EditText> (Resource.Id.editText1);
			loginText = FindViewById<EditText> (Resource.Id.editText2);
			missionDescriptionText = FindViewById<EditText> (Resource.Id.editText3);
			priorityText = FindViewById<EditText> (Resource.Id.editText4);
			latitudeText = FindViewById<EditText> (Resource.Id.editText5);
			longitudeText = FindViewById<EditText> (Resource.Id.editText6);
		}

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			SetContentView (Resource.Layout.Main);
			initUiReferences ();

			Gateway.SetDebugMode (true);
			Gateway.SetServerBaseURL (new Uri("http://10.0.0.30:3000/api/"));

			loginButton.Click += doLogin;
			logoutButton.Click += doLogout;
			missionCreateButton.Click += createMission;

			var listAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, new List<string>());
			listView.Adapter = listAdapter;
			listView.ItemClick += delegate(object sender, AdapterView.ItemClickEventArgs e) 
			{
				displayMissionDetails(e.Id);
			};

			updateMissions (null, null);
			StartMissionUpdateTimer ();
			StartConnectedIntervalChecks ();
		}


		public void displayMissionDetails(long id)
		{
			Mission clickedOnMission = missions.ElementAt ((int)id);

			const int priorityRedThreshold = 3;

			if (clickedOnMission.priority < priorityRedThreshold) 
			{
				missionPriorityText.SetTextColor (Android.Graphics.Color.Red);
			} 
			else 
			{
				missionPriorityText.SetTextColor (Android.Graphics.Color.White);
			}

			missionPriorityText.Text = "Mission: " + clickedOnMission.description + " Priority " + clickedOnMission.priority.ToString();

			if (clickedOnMission.location != null) 
			{
				geoLocationText.Text = "The mission " + clickedOnMission.description + " will take place at coordinates (" 
					+ clickedOnMission.location.Longitude + ", " + clickedOnMission.location.Latitude + ")";
			}
		}


		public async void createMission(Object sender, EventArgs e)
		{
			Mission createdMission = new Mission () 
			{
				description = missionDescriptionText.Text
			};


			if (!string.IsNullOrEmpty(priorityText.Text)) 
			{
				createdMission.priority = int.Parse (priorityText.Text);
			} 

			if (!string.IsNullOrEmpty(longitudeText.Text) && !string.IsNullOrEmpty(latitudeText.Text))
			{
				createdMission.location = new GeoPoint () 
				{
					Latitude = int.Parse (latitudeText.Text),
					Longitude = int.Parse (longitudeText.Text)
				};
			}

			try
			{
				createdMission = await Missions.Create (createdMission);

				creationStatus.SetTextColor(Android.Graphics.Color.Green);
				creationStatus.Text = "Mission created successfully.";
				missionDescriptionText.Text = "";
				priorityText.Text = "";
				latitudeText.Text = "";
				longitudeText.Text = "";
				updateMissions (null, null);

			}
			catch(RestException exception)
			{
				creationStatus.SetTextColor(Android.Graphics.Color.Red);
				creationStatus.Text = "Creation failed.";


				if (exception.StatusCode == 401) 
				{
					Toast.MakeText (this, "Creation failed: You are not authorized to create, please log in.", ToastLength.Short).Show ();
				} 
				else if (exception.StatusCode == 422) 
				{
					Toast.MakeText (this, "Creation failed: Please make sure that you've filled all the required mission details.", ToastLength.Short).Show ();
				} 
				else 
				{
					Toast.MakeText (this, "Creation failed" + exception.StatusCode.ToString(), ToastLength.Short).Show ();
				}
			}
		}

		public async void updateMissions(object source, ElapsedEventArgs e)
		{
			RunOnUiThread (async delegate 
				{
					try
					{
						missions = await Missions.Find ();
						ArrayAdapter<string> listAdapter = listView.Adapter as ArrayAdapter<string>;
						listAdapter.Clear();
						listAdapter.AddAll(missions.Select(x => x.description).ToList());
					}
					catch(Exception)
					{
						// All loopback repositories throw RestException on failure. 
						// This is a placeholder for logic on this failure of missions.find
					}
			});
		}

		public void StartMissionUpdateTimer()
		{
			dataReloadTimer = new System.Timers.Timer(3000);
			dataReloadTimer.Elapsed += new ElapsedEventHandler(updateMissions);
			dataReloadTimer.Interval = 3000;
			dataReloadTimer.Enabled = true;
			dataReloadTimer.Start();
		}


		public void StartConnectedIntervalChecks()
		{
			connectedTimer = new System.Timers.Timer(3000);
			connectedTimer.Elapsed += new ElapsedEventHandler(updateConnectedStatus);
			connectedTimer.Interval = 3000;
			connectedTimer.Enabled = true;
			connectedTimer.Start();
		}


		public async void updateConnectedStatus(object source, ElapsedEventArgs e)
		{
			
			connected =  await Gateway.isConnected (500);
			RunOnUiThread(delegate {
				if(connected)
				{
					connectedStatus.Text = "Status: Connected to Server";
					connectedStatus.SetTextColor(Android.Graphics.Color.Green);
					UnlockEntireUI ();
				}
				else {
					connectedStatus.Text = "Status: Disconnected from Server";
					connectedStatus.SetTextColor(Android.Graphics.Color.Red);
					LockEntireUI ();
				}
			});
		}

		public void LockEntireUI()
		{
			missionCreateButton.Enabled = false;
			logoutButton.Enabled = false;
			passwordText.Enabled = false;
			loginText.Enabled = false;
			loginButton.Enabled = false;
			loginStatus.SetTextColor(Android.Graphics.Color.Red);
			loginStatus.Text = "Logged out.";
		}

		public void UnlockEntireUI()
		{
			missionCreateButton.Enabled = true;
			if(loginButton.Enabled == true || logoutButton.Enabled == false)
			{
				logoutButton.Enabled = true;
				passwordText.Enabled = true;
				loginText.Enabled = true;
				loginButton.Enabled = true;
			}

		}

		public void lockLoginUI()
		{
			loginStatus.SetTextColor(Android.Graphics.Color.Green);
			loginStatus.Text = "Logged in!";
			passwordText.Enabled = false;
			loginText.Enabled = false;
			loginButton.Enabled = false;
		}

		public void UnlockLoginUI()
		{
			loginStatus.SetTextColor(Android.Graphics.Color.Red);
			loginStatus.Text = "Logged out.";
			loginText.Text = "";
			passwordText.Text = "";
			passwordText.Enabled = true;
			loginText.Enabled = true;
			loginButton.Enabled = true;
		}

		public async void doLogin(Object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(passwordText.Text) || string.IsNullOrEmpty(loginText.Text)) 
			{
				Toast.MakeText (this, "Can't login with empty credentials.", ToastLength.Short).Show ();
				return;
			}

			try 
			{
				Operator loginCredentials = new Operator () 
				{
					password = passwordText.Text,
					email = loginText.Text
				};
				AccessToken accessToken = await Operators.login (loginCredentials);

				lockLoginUI();
				Gateway.SetAccessToken(accessToken);
			}
			catch(Exception)
			{
				Toast.MakeText (this, "Login failed.", ToastLength.Short).Show ();
				UnlockLoginUI ();
			}
		}

		public async void doLogout(Object sender, EventArgs e)
		{
			try
			{
				await Operators.logout ();
			}
			catch(RestException)
			{
				Toast.MakeText (this, "You are not logged in, how can you log out?", ToastLength.Short).Show ();
			}
			Gateway.ResetAccessToken ();
			UnlockLoginUI ();
		}
	}
}


