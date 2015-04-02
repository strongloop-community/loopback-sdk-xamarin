using System;
using Android.App;
using Android.Widget;
using Android.OS;
using LBXamarinSDK;
using LBXamarinSDK.LBRepo;
using System.Timers;
using System.Collections.Generic;
using System.Linq;

namespace Agents
{
	[Activity (Label = "SDK Use Example: Missions", MainLauncher = true, Icon = "@drawable/icon")]





	public class MainActivity : Activity
	{
		System.Timers.Timer t1;
		System.Timers.Timer missionsTimer1;
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
		EditText longtitudeText;
		TextView creationStatus;
		TextView connectedStatus;
		EditText loginText;
		TextView loginStatus;

		private void initUI()
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
			longtitudeText = FindViewById<EditText> (Resource.Id.editText6);


		}

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			SetContentView (Resource.Layout.Main);
			initUI ();

			Gateway.SetServerBaseURL (new Uri("http://10.0.0.20:3000/api/"));
			Gateway.SetDebugMode (true);

			loginButton.Click += doLogin;
			logoutButton.Click += doLogout;

			missionCreateButton.Click += createMission;
			var listAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, new List<string>());


			listView.Adapter = listAdapter;
			listView.ItemClick += delegate(object sender, AdapterView.ItemClickEventArgs e) {
				displayMissionDetails(e.Id);
			};

			updateMissions (null, null);
			StartMissionUpdateTimer ();
			StartConnectedIntervalChecks ();

		}


		public void displayMissionDetails(long id)
		{
			Mission clickedOnMission = missions.ElementAt ((int)id);

			const int priorityBar = 3;
			if (clickedOnMission.priority < priorityBar) {
				missionPriorityText.SetTextColor (Android.Graphics.Color.Red);
			} else {
				missionPriorityText.SetTextColor (Android.Graphics.Color.White);
			}

			missionPriorityText.Text = "Mission: " + clickedOnMission.description + " Priority " + clickedOnMission.priority.ToString();
			if (clickedOnMission.location != null) {
				geoLocationText.Text = "The mission " + clickedOnMission.description + " will take place at coordinates (" 
									+ clickedOnMission.location.Longtitude + ", " + clickedOnMission.location.Latitude + ")";
			}
		}
			

		public async void createMission(Object sender, EventArgs e)
		{
			Mission createdMission = new Mission () {
				description = missionDescriptionText.Text
			};

			const int defaultPriority = 10;
			if (priorityText.Text != "") {
				createdMission.priority = int.Parse (priorityText.Text);
			} else {
				createdMission.priority = defaultPriority;
			}

			if (longtitudeText.Text != "" && latitudeText.Text != "") {
				createdMission.location = new GeoPoint () {
					Latitude = int.Parse (latitudeText.Text),
					Longtitude = int.Parse (longtitudeText.Text)
				};
			}

			createdMission = await Missions.Create (createdMission);

			if (createdMission != null) {

				creationStatus.SetTextColor(Android.Graphics.Color.Green);
				creationStatus.Text = "Mission created successfully.";
				missionDescriptionText.Text = "";
				priorityText.Text = "";
				latitudeText.Text = "";
				longtitudeText.Text = "";
				updateMissions (null, null);

			} else {
				creationStatus.SetTextColor(Android.Graphics.Color.Red);
				creationStatus.Text = "Creation failed. Are you authorized to create?";
			}
		}

		public async void updateMissions(object source, ElapsedEventArgs e)
		{
			RunOnUiThread (async delegate {
				missions = await Missions.Find ();

				ArrayAdapter<string> listAdapter = listView.Adapter as ArrayAdapter<string>;

				listAdapter.Clear();
				if(missions != null) {
					listAdapter.AddAll(missions.Select(x => x.description).ToList());
				}
			});
		}

		public void StartMissionUpdateTimer()
		{
			missionsTimer1 = new System.Timers.Timer(3000);
			missionsTimer1.Elapsed += new ElapsedEventHandler(updateMissions);
			missionsTimer1.Interval = 3000;
			missionsTimer1.Enabled = true;
			missionsTimer1.Start();
		}


		public void StartConnectedIntervalChecks()
		{
			t1 = new System.Timers.Timer(1000);
			t1.Elapsed += new ElapsedEventHandler(updateConnectedStatus);
			t1.Interval = 1000;
			t1.Enabled = true;
			t1.Start();
		}


		public async void updateConnectedStatus(object source, ElapsedEventArgs e)
		{
			bool connected =  await Gateway.isConnected (500);
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
			if (passwordText.Text == "" || loginText.Text == "") {
				Toast.MakeText (this, "Can't login with empty credentials.", ToastLength.Short).Show ();
				return;
			}

			try {
				Operator loginCredentials = new Operator () {
					password = passwordText.Text,
					email = loginText.Text
				};

				AccessToken accessToken = await Operators.login (loginCredentials);
				lockLoginUI();
				Gateway.SetAccessToken(accessToken);

			} catch(Exception)
			{
				UnlockLoginUI ();
			}
		}

		public async void doLogout(Object sender, EventArgs e)
		{
			await Operators.logout ();
			Gateway.ResetAccessToken ();
			UnlockLoginUI ();
		}
	}
}


