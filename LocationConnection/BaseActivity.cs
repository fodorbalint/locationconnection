/* Inherited by:
 * 
 * ListActivity
 * ProfileViewActivity
 * ProfileEditActivity
 * RegisterActivity
 * ChatListActivity
 * ChatOneActivity
 * HelpCenterActivity
 * SettingsActivity
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Timers;
using Android.Content;
using Android.Gms.Location;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;

namespace LocationConnection
{
	public class BaseActivity : AppCompatActivity
	{
		public static string firebaseTokenFile = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "firebasetoken.txt");
		public static string tokenUptoDateFile = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "tokenuptodate.txt");
		public static string regSessionFile = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "regsession.txt");
		public static string regSaveFile = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "regsave.txt");
		//public static string selectedImageFile = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "selectedimage.txt");
		//public static string frameBorderWidthFile = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "frameborderwidth.txt");

		public View MainLayout;
		private ChatReceiver chatReceiver;
		public CommonMethods c;
		public Snackbar snack;

		private static LocationCallback locationCallback;
		private static FusedLocationProviderClient fusedLocationProviderClient;
		private static bool isAppVisible;
		public static bool isAppForeground;
		private static Timer t;
		private static int currentLocationRate;
		public static string locationUpdatesTo;
		public static string locationUpdatesFrom;
		public static List<UserLocationData> locationUpdatesFromData;

		public static int screenWidth;
		public static int screenHeight;
		public static float pixelDensity;
		protected static float dpWidth;
		public int tweenTime = 300;

		public static bool firstRun = false;

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			c = new CommonMethods(this);
			c.CW("Created " + LocalClassName.Split(".")[1]);
			if (c.IsLoggedIn())
			{
				CheckIntent();
			}
			chatReceiver = new ChatReceiver();

			if (fusedLocationProviderClient is null)
			{
				fusedLocationProviderClient = LocationServices.GetFusedLocationProviderClient(this);
			}
			c.LogActivity(LocalClassName.Split(".")[1] + " OnCreate");
		}

		protected override void OnResume()
		{
			base.OnResume();
			c.CW("Resumed " + LocalClassName.Split(".")[1]);
			RegisterReceiver(chatReceiver, new IntentFilter("balintfodor.locationconnection.ChatReceiver"));
			
			isAppVisible = true;
			if (!(this is ListActivity) && !(this is RegisterActivity) && !(this is MainActivity))
			{
				InitLocationUpdates();
			}

			c.LogActivity(LocalClassName.Split(".")[1] + " OnResume selectedFileStr " + ProfilePage.selectedFileStr + " Session.ID " + Session.ID);

			if (!ListActivity.initialized) //When opening app, Android sometimes resumes an Activity while the static variables are cleared out, resulting in error
			{
				c.LogActivity(LocalClassName.Split(".")[1] + " Not initialized");
				
				c.ReportErrorSilent("Initialization error");

				Intent i = new Intent(this, typeof(ListActivity)); //current activity has to go through OnResume, therefore we cannot handle initialization errors in OnCreate
				StartActivity(i);
			}

			if (!(snack is null) && snack.IsShown)
			{
				snack.Dismiss();
			}

			if (!(Session.SnackMessage is null)) //ChatList: for the situation when the user is deleted, while the other is on their page, and now want to load the chat.
			{
				if (this is ChatOneActivity)
				{
					RunOnUiThread(() =>
					{
						c.SnackStr(Session.SnackMessage.Replace("[name]", Session.CurrentMatch.TargetName));
					});					
					Session.SnackMessage = null;
				}
				else
				{
					RunOnUiThread(() =>
					{
						c.SnackStr(Session.SnackMessage);
					});				
					Session.SnackMessage = null;
				}
			}

		}

		protected override void OnPause()
		{
			base.OnPause();
			c.CW("Paused " + LocalClassName.Split(".")[1]);
			UnregisterReceiver(chatReceiver);

			isAppVisible = false;
			InitLocationUpdates();

			c.LogActivity(LocalClassName.Split(".")[1] + " OnPause");
		}

		public void GetScreenMetrics(bool setDisplaySize)
		{
			Android.Util.DisplayMetrics metrics = new Android.Util.DisplayMetrics();
			WindowManager.DefaultDisplay.GetMetrics(metrics);

			screenWidth = metrics.WidthPixels;
			screenHeight = metrics.HeightPixels;
			pixelDensity = metrics.Density;
			float xPxPerIn = metrics.Xdpi;
			float xDpPerIn = metrics.Xdpi / pixelDensity;
			dpWidth = screenWidth / pixelDensity;

			if (setDisplaySize)
			{
				if (dpWidth >= 360)
				{
					Settings.DisplaySize = 1;
				}
				else
				{
					Settings.DisplaySize = 0;
				}
			}

			c.LogActivity("ScreenWidth " + screenWidth + " ScreenHeight " + screenHeight + " PixelDensity " + pixelDensity
				+ " XPxPerIn " + xPxPerIn + " XDpPerIn " + xDpPerIn + " DpWidth " + dpWidth);
		}

		protected void CheckIntent() //logged in
		{
			/*
			Key: google.delivered_priority, Value: high
			Key: google.sent_time, Value: 
			Key: google.ttl, Value: 
			Key: google.original_priority, Value: high
			Key: from, Value: 205197408276
			Key: google.message_id, Value: 0:1575318929834966%e37d5f25e37d5f25
			Key: content, Value: 33|6|1575318929|0|0|
			Key: collapse_key, Value: balintfodor.locationconnection
			*/
			
			if (!(Intent.Extras is null) && !(Intent.Extras.GetString("google.message_id") is null))
			{
				int senderID = int.Parse(Intent.Extras.GetString("fromuser"));
				int targetID = int.Parse(Intent.Extras.GetString("touser"));

				c.LogActivity("Intent received from " + senderID);

				if (targetID != Session.ID)
				{
					return;
				}

				Intent i = new Intent(this, typeof(ChatOneActivity));
				i.SetFlags(ActivityFlags.ReorderToFront);
				IntentData.senderID = senderID;
				StartActivity(i);
			}
		}

		protected void InitLocationUpdates()
		{
			if (t is null)
			{
				t = new Timer();
				t.Elapsed += Timer_Elapsed;
				t.Interval = Constants.ActivityChangeInterval;
				t.Start();
			}
		}
		
		private void Timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			t.Stop();
			t = null;

			if (isAppVisible)
			{
				isAppForeground = true;
				if (Session.UseLocation is null || !(bool)Session.UseLocation || !c.IsLocationEnabled()) //location has been turned off
				{
					StopLocationUpdates();
				}
				else
				{
					if (c.IsLoggedIn())
					{
						if (currentLocationRate != Session.InAppLocationRate)
						{
							StopLocationUpdates();
							StartLocationUpdates((int)Session.InAppLocationRate * 1000);
						}
					}
					else
					{
						if (currentLocationRate != Settings.InAppLocationRate)
						{
							StopLocationUpdates();
							StartLocationUpdates((int)Settings.InAppLocationRate * 1000);
						}
					}
				}
			}
			else
			{
				if (!string.IsNullOrEmpty(locationUpdatesTo))
				{
					EndLocationShare();
				}

				locationUpdatesTo = null; //stop real-time location updates when app goes to background
				locationUpdatesFrom = null;
				locationUpdatesFromData = null;
				isAppForeground = false;
				if (!c.IsLoggedIn() || !(bool)Session.UseLocation || !(bool)Session.BackgroundLocation || !c.IsLocationEnabled())
				{
					StopLocationUpdates();
				}
				else
				{
					if (currentLocationRate != Session.BackgroundLocationRate)
					{
						StopLocationUpdates();
						StartLocationUpdates((int)Session.BackgroundLocationRate * 1000);
					}
				}
			}
		}

		public void StopLocationUpdates()
		{
			try
			{
				RunOnUiThread(() => {
					if (!(locationCallback is null))
					{
						fusedLocationProviderClient.RemoveLocationUpdates(locationCallback);
						c.LogActivity("Location updates stopped from " + currentLocationRate + ", isAppForeground " + isAppForeground);
						currentLocationRate = 0;
					}
				});
			}
			catch (Exception ex)
			{
				c.LogActivity(ex.Message);
			}
		}

		protected void StartLocationUpdates(int interval)
		{
			try
			{
				RunOnUiThread(async () => {
					LocationRequest locationRequest = new LocationRequest()
							.SetFastestInterval((long)(interval * 0.8))
							.SetInterval(interval);
					if (Session.LocationAccuracy == 0)
					{
						locationRequest.SetPriority(LocationRequest.PriorityBalancedPowerAccuracy);
					}
					else
					{
						locationRequest.SetPriority(LocationRequest.PriorityHighAccuracy);
					}
					locationCallback = new FusedLocationProviderCallback(this);
					await fusedLocationProviderClient.RequestLocationUpdatesAsync(locationRequest, locationCallback);
					currentLocationRate = interval / 1000;
					c.LogActivity("Location updates started at " + currentLocationRate + ", isAppForeground " + isAppForeground);
				});
			}
			catch (Exception ex)
			{
				c.LogActivity(ex.Message);
			}
		}

		public void TruncateLocationLog()
		{
			long unixTimestamp = c.Now();
			string[] lines = File.ReadAllLines(c.locationLogFile);
			string firstLine = lines[0];
			int sep1Pos = firstLine.IndexOf("|");
			long locationTime = long.Parse(firstLine.Substring(0, sep1Pos));

			int j = 0;
			if (locationTime < unixTimestamp - Constants.LocationKeepTime)
			{
				j++;
				List<string> newLines = new List<string>();
				for(int i = 1; i < lines.Length; i++)
				{
					string line = lines[i];
					sep1Pos = line.IndexOf("|");
					locationTime = long.Parse(line.Substring(0, sep1Pos));
					if (locationTime >= unixTimestamp - Constants.LocationKeepTime)
					{
						newLines.Add(line);
					}
					else
					{
						j++;
					}
				}
				if (newLines.Count != 0)
				{
					File.WriteAllLines(c.locationLogFile, newLines);
				}
				else //it would write an empty string into the file, and lines[0] would throw an error
				{
					File.Delete(c.locationLogFile);
				}
			}
			if (j == 0)
			{
				c.LogActivity("Location log up to date");
			}
			else
			{
				c.LogActivity("Removed " + j + " items from location log");
			}
		}

		public void TruncateSystemLog()
		{
			try
			{
				CultureInfo provider = CultureInfo.InvariantCulture;
				string format = @"yyyy-MM-dd HH\:mm\:ss.fff";
				DateTime dt = DateTime.UtcNow;

				string[] lines = File.ReadAllLines(CommonMethods.logFile);
				string firstLine = lines[0];
				int sep1Pos = firstLine.IndexOf(" ");
				int sep2Pos = firstLine.IndexOf(" ", sep1Pos + 1);
				DateTime logTime = DateTime.ParseExact(firstLine.Substring(0, sep2Pos), format, provider);

				int j = 0;
				if (dt.Subtract(logTime).TotalSeconds > Constants.SystemLogKeepTime)
				{
					j++;
					List<string> newLines = new List<string>();
					for (int i = 1; i < lines.Length; i++)
					{
						string line = lines[i];
						sep1Pos = line.IndexOf(" ");
						sep2Pos = line.IndexOf(" ", sep1Pos + 1);
						logTime = DateTime.ParseExact(line.Substring(0, sep2Pos), format, provider);

						if (dt.Subtract(logTime).TotalSeconds <= Constants.SystemLogKeepTime)
						{
							newLines.Add(line);
						}
						else
						{
							j++;
						}
					}
					File.WriteAllLines(CommonMethods.logFile, newLines);
				}
				if (j == 0)
				{
					c.LogActivity("System log up to date");
				}
				else
				{
					c.LogActivity("Removed " + j + " items from system log");
				}
			}
			catch
			{
				c.CW("Resetting log File");
				File.WriteAllText(CommonMethods.logFile, "");
			}			
		}

		protected void EndLocationShare(int? targetID = null)
		{
			string url = "action=updatelocationend&ID=" + Session.ID + "&SessionID=" + Session.SessionID + "&LocationUpdates=";
			if (targetID is null) //stop all
			{
				url += locationUpdatesTo;
			}
			else
			{
				url += targetID;
			}
			string responseString = c.MakeRequestSync(url);
			if (responseString == "OK")
			{
			}
			else
			{
				c.ReportErrorSilent(responseString);
			}
		}

		public bool IsUpdatingTo(int targetID)
		{
			if (string.IsNullOrEmpty(locationUpdatesTo))
			{
				return false;
			}
			string[] arr = locationUpdatesTo.Split("|");
			foreach (string ID in arr)
			{
				if (ID == targetID.ToString())
				{
					return true;
				}
			}
			return false;
		}

		protected void AddUpdatesTo(int targetID)
		{
			c.LogActivity("AddUpdatesTo locationUpdatesTo:" + locationUpdatesTo);
			if (string.IsNullOrEmpty(locationUpdatesTo))
			{
				locationUpdatesTo = targetID.ToString();
			}
			else
			{
				locationUpdatesTo += "|" + targetID;
			}
		}

		public void RemoveUpdatesTo(int targetID)
		{
			string[] arr = locationUpdatesTo.Split("|");
			string returnStr = "";
			foreach (string ID in arr)
			{
				if (ID != targetID.ToString())
				{
					returnStr += ID + "|";
				}
			}
			if (returnStr.Length > 0)
			{
				returnStr = returnStr.Substring(0, returnStr.Length - 1);
			}
			locationUpdatesTo = returnStr;
		}

		public bool IsUpdatingFrom(int targetID)
		{
			c.LogActivity("Location update from " + targetID + ", existing: " + locationUpdatesFrom);
			if (string.IsNullOrEmpty(locationUpdatesFrom))
			{
				return false;
			}
			string[] arr = locationUpdatesFrom.Split("|");
			foreach (string ID in arr)
			{
				if (ID == targetID.ToString())
				{
					return true;
				}
			}
			return false;
		}

		public void AddUpdatesFrom(int targetID)
		{
			if (string.IsNullOrEmpty(locationUpdatesFrom))
			{
				locationUpdatesFrom = targetID.ToString();
			}
			else
			{
				locationUpdatesFrom += "|" + targetID;
			}
		}

		public void RemoveUpdatesFrom(int targetID)
		{
			string[] arr = locationUpdatesFrom.Split("|");
			string returnStr = "";
			foreach (string ID in arr)
			{
				if (ID != targetID.ToString())
				{
					returnStr += ID + "|";
				}
			}
			if (returnStr.Length > 0)
			{
				returnStr = returnStr.Substring(0, returnStr.Length - 1);
			}
			locationUpdatesFrom = returnStr;

			RemoveLocationData(targetID);
		}

		public void AddLocationData(int ID, double Latitude, double Longitude, long LocationTime)
        {
            if (locationUpdatesFromData is null)
            {
				locationUpdatesFromData = new List<UserLocationData>();
            }

			bool found = false;
            foreach (UserLocationData data in locationUpdatesFromData)
            {
                if (data.ID == ID)
                {
					found = true;
					data.Latitude = Latitude;
					data.Longitude = Longitude;
					data.LocationTime = LocationTime;
					break;
				}
            }
            if (!found)
            {
				locationUpdatesFromData.Add(new UserLocationData
				{
					ID = ID,
                    Latitude = Latitude,
                    Longitude = Longitude,
                    LocationTime = LocationTime
				});
            }
        }

        public void RemoveLocationData(int ID)
        {
            if (!(locationUpdatesFromData is null))
            {
				for (int i= 0; i < locationUpdatesFromData.Count; i++)
                {
                    if (locationUpdatesFromData[i].ID == ID)
                    {
						locationUpdatesFromData.RemoveAt(i);
						break;
                    }
                }
            }
        }

        public UserLocationData GetLocationData(int ID)
        {
			if (!(locationUpdatesFromData is null))
			{
				foreach (UserLocationData data in locationUpdatesFromData)
				{
					if (data.ID == ID)
					{
						return data;
					}
				}
				return null;
			}
            else
            {
				return null;
            }

		}
	}
}