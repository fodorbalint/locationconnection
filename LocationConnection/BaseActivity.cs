﻿/* Inherited by:
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
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;

namespace LocationConnection
{
	public class BaseActivity : AppCompatActivity
	{
		public View MainLayout;
		private ChatReceiver chatReceiver;
		public CommonMethods c;

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
		protected static float XPxPerIn;
		protected static float XDpPerIn;
		protected static float DpWidth;

		bool initializeError = false;

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

			if (!(this is ListActivity) && !ListActivity.initialized)
			{
				initializeError = true;
				c.LogActivity(LocalClassName.Split(".")[1] + " Not initialized");

				c.ReportErrorSilent("Initialization error");

				Intent i = new Intent(this, typeof(ListActivity));
				i.SetFlags(ActivityFlags.ReorderToFront); //ListActivity must be recreated.
				StartActivity(i);
			}
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

			c.LogActivity(LocalClassName.Split(".")[1] + " OnResume");

			//When opening app, Android sometimes resumes an Activity while the static variables are cleared out, resulting in error
			if (!ListActivity.initialized && !initializeError)
			{
				c.LogActivity(LocalClassName.Split(".")[1] + " Not initialized");

				c.ReportErrorSilent("Initialization error");
				
				Intent i = new Intent(this, typeof(ListActivity));
				i.SetFlags(ActivityFlags.ReorderToFront); //ListActivity must be recreated.
				StartActivity(i);
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

		public void GetScreenMetrics()
		{
			Android.Util.DisplayMetrics metrics = new Android.Util.DisplayMetrics();
			WindowManager.DefaultDisplay.GetMetrics(metrics);

			screenWidth = metrics.WidthPixels;
			screenHeight = metrics.HeightPixels;
			pixelDensity = metrics.Density;
			XPxPerIn = metrics.Xdpi;
			XDpPerIn = metrics.Xdpi / pixelDensity;
			DpWidth = screenWidth / pixelDensity;

			if (DpWidth >= 360)
			{
				Settings.DisplaySize = 1;
			}
			else
			{
				Settings.DisplaySize = 0;
			}

			c.LogActivity(" screenWidth " + screenWidth + " screenHeight " + screenHeight + " pixelDensity " + pixelDensity
				+ " XPxPerIn " + XPxPerIn + " XDpPerIn " + XDpPerIn + " DpWidth " + DpWidth);
		}

		protected void CheckIntent()
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
			if (c.IsLoggedIn())
			{
				if (!(Intent.Extras is null) && !(Intent.Extras.GetString("google.message_id") is null))
				{
					int sep1Pos;
					int senderID = 0;

					string type = Intent.Extras.GetString("type");
					string content = Intent.Extras.GetString("content");
					c.LogActivity("Intent received: " + type);
					switch (type)
					{
						case "sendMessage":
							sep1Pos = content.IndexOf('|');
							int sep2Pos = content.IndexOf('|', sep1Pos + 1);
							senderID = int.Parse(content.Substring(sep1Pos + 1, sep2Pos - sep1Pos - 1));
							break;
						case "matchProfile":
						case "rematchProfile":
						case "unmatchProfile":
							sep1Pos = content.IndexOf('|');
							senderID = int.Parse(content.Substring(0, sep1Pos));
							break;
					}

					Intent i = new Intent(this, typeof(ChatOneActivity));
					i.SetFlags(ActivityFlags.ReorderToFront);
					IntentData.senderID = senderID;
					StartActivity(i);
				}
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
			if (locationTime < unixTimestamp - Constants.LocationKeepTime)
			{
				List<string> newLines = new List<string>();
				for(int i=1; i < lines.Length; i++)
				{
					string line = lines[i];
					sep1Pos = line.IndexOf("|");
					locationTime = long.Parse(line.Substring(0, sep1Pos));
					if (locationTime >= unixTimestamp - Constants.LocationKeepTime)
					{
						newLines.Add(line);
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
		}

		public void TruncateSystemLog()
		{
			CultureInfo provider = CultureInfo.InvariantCulture;
			string format = @"yyyy-MM-dd HH\:mm\:ss.fff";
			DateTime dt = DateTime.UtcNow;

			string[] lines = File.ReadAllLines(c.logFile);
			string firstLine = lines[0];
			int sep1Pos = firstLine.IndexOf(" ");
			int sep2Pos = firstLine.IndexOf(" ", sep1Pos + 1);
			DateTime logTime = DateTime.ParseExact(firstLine.Substring(0, sep2Pos), format, provider);

			if (dt.Subtract(logTime).TotalSeconds > Constants.SystemLogKeepTime)
			{
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
				}
				File.WriteAllLines(c.logFile, newLines);
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

		protected bool IsUpdatingTo(int targetID)
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

		protected void RemoveUpdatesTo(int targetID)
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