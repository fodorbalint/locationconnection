using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.Content;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using Android.Widget;
using Xamarin.Essentials;
using Android.Locations;
using Android.Gms.Location;
using System.Timers;
using Android.Text.Method;
using Android.Text.Util;
using System.Net.Http;
using System.Globalization;
using Java.Net;

namespace LocationConnection
{
    public class CommonMethods
    {
		public string errorFile = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "error.txt");
		public static string logFile = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "systemlog.txt");
		public string locationLogFile = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "locationlog.txt");
		private string settingsFile = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "settings.txt");
		private string defaultSettingsFile = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "defaultsettings.txt");
		public string loginSessionFile = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "loginsession.txt");
		public static string cacheFolder = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "images"); //default cache dir is unreliable, images can be deleted in short time, especially the large images.

		public View view;
        Activity context;
		Snackbar snack;		

		public int snackPermanentText;
		private float alertTextSize;
		private float logAlertTextSize;
		private float alertPadding;
		private float logAlertPadding;
		private int textSmall;

		private static HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

		public CommonMethods(Activity context)
		{
			this.context = context;
			if (!(context is null)) //CommonMethods is called with null context from Firebase OnNewToken
			{
				if (Settings.DisplaySize == 1)
				{
					alertTextSize = float.Parse(context.Resources.GetString(Resource.String.alertTextSizeNormal), CultureInfo.InvariantCulture);
					logAlertTextSize = float.Parse(context.Resources.GetString(Resource.String.logAlertTextSizeNormal), CultureInfo.InvariantCulture);
					alertPadding = float.Parse(context.Resources.GetString(Resource.String.alertPaddingNormal), CultureInfo.InvariantCulture);
					logAlertPadding = float.Parse(context.Resources.GetString(Resource.String.logAlertPaddingNormal), CultureInfo.InvariantCulture);
					textSmall = Resource.Style.TextSmallNormal;

				}
				else
				{
					alertTextSize = float.Parse(context.Resources.GetString(Resource.String.alertTextSizeSmall), CultureInfo.InvariantCulture);
					logAlertTextSize = float.Parse(context.Resources.GetString(Resource.String.logAlertTextSizeSmall), CultureInfo.InvariantCulture);
					alertPadding = float.Parse(context.Resources.GetString(Resource.String.alertPaddingSmall), CultureInfo.InvariantCulture);
					logAlertPadding = float.Parse(context.Resources.GetString(Resource.String.logAlertPaddingSmall), CultureInfo.InvariantCulture);
					textSmall = Resource.Style.TextSmallSmall;
				}
			}
		}

		public void LoadSettings(bool defaultSettings)
		{
			if (!defaultSettings)
			{
				//Load not empty values from file, the rest will be loaded from SettingsDefault
				if (File.Exists(settingsFile))
				{
					Type type = typeof(Settings);
					string[] settingLines = File.ReadAllLines(settingsFile);
					foreach (string line in settingLines)
					{
						if (line != "" && line[0] != '\'')
						{
							int pos = line.IndexOf(":");
							string key = line.Substring(0, pos);
							string value = line.Substring(pos + 1).Trim();
							if (value != "")
							{
								//CW("LoadSettings from file " + key + " --- " + value);
								FieldInfo fieldInfo = type.GetField(key);
								if (!(fieldInfo is null)) //if that setting still exists in this version of the app
								{
									Type type1 = Nullable.GetUnderlyingType(fieldInfo.FieldType) ?? fieldInfo.FieldType;
									fieldInfo.SetValue(null, Convert.ChangeType(value, type1));
								}
							}
						}
					}
				}

				Type typeS = typeof(Settings);
				Type typeSDef = typeof(SettingsDefault);

				string str= "";
				FieldInfo[] fields = typeS.GetFields();
				foreach (FieldInfo field in fields)
				{
					if (field.GetValue(null) is null)
					{
						FieldInfo defField = typeSDef.GetField(field.Name);
						if (!(defField is null)) //other address data has no default.
						{
							//CW("LoadSettings got from default " + field.Name + " --- " + defField.GetValue(null));
							field.SetValue(null, defField.GetValue(null));
						}
					}
					str += System.Environment.NewLine;
				}
			}
			else //load default settings, not used
			{
				Type typeS = typeof(Settings);
				Type typeSDef = typeof(SettingsDefault);

				FieldInfo[] fields = typeS.GetFields();
				foreach (FieldInfo field in fields)
				{
					FieldInfo defField = typeSDef.GetField(field.Name);
					if (!(defField is null)) //other address data has no default.
					{
						field.SetValue(null, defField.GetValue(null));
					}
				}
			}			
		}

		public void SaveSettings()
		{
			List<string> settingLines = new List<string>();
			Type type = typeof(Settings);
			FieldInfo[] fieldInfo = type.GetFields();
			foreach (FieldInfo field in fieldInfo)
			{
				settingLines.Add(field.Name + ": " + field.GetValue(null));
			}
			File.WriteAllLines(settingsFile, settingLines);
		}

		public void LoadCurrentUser(string responseString)
		{
			responseString = responseString.Substring(3);
			ServerParser<Session> parser = new ServerParser<Session>(responseString);
			File.WriteAllText(loginSessionFile, Session.ID + ";" + Session.SessionID);
		}

		public void ClearCurrentUser()
		{
			Type type = typeof(Session);
			FieldInfo[] fieldInfo = type.GetFields();
			foreach (FieldInfo field in fieldInfo)
			{
				field.SetValue(null, null);
			}
			
			if (File.Exists(loginSessionFile))
			{
				File.Delete(loginSessionFile);
			}
		}

		public bool IsLoggedIn()
		{
			return !string.IsNullOrEmpty(Session.SessionID);
		}

		public bool IsLocationEnabled()
		{
			return ContextCompat.CheckSelfPermission(context, Manifest.Permission.AccessFineLocation) == (int)Permission.Granted;
		}

		public bool IsOwnLocationAvailable()
		{
			return Session.Latitude != null && Session.Longitude != null;
		}

		public bool IsOtherLocationAvailable()
		{
			return Session.OtherLatitude != null && Session.OtherLongitude != null;
		}

		public string GetPathToImage(Android.Net.Uri uri)
		{
			string doc_id = "";
			using (var c1 = context.ContentResolver.Query(uri, null, null, null, null))
			{
				c1.MoveToFirst();
				string document_id = c1.GetString(0);
				doc_id = document_id.Substring(document_id.LastIndexOf(":") + 1);
			}

			string path = null;

			// The projection contains the columns we want to return in our query.
			string selection = Android.Provider.MediaStore.Images.Media.InterfaceConsts.Id + " =? "; //_id=?
			using (var cursor = context.ContentResolver.Query(Android.Provider.MediaStore.Images.Media.ExternalContentUri, null, selection, new string[] { doc_id }, null))
			{
				if (cursor == null) return path;
				var columnIndex = cursor.GetColumnIndexOrThrow(Android.Provider.MediaStore.Images.Media.InterfaceConsts.Data);
				cursor.MoveToFirst();
				path = cursor.GetString(columnIndex);
			}
			return path;
		}

		public Task<string> MakeRequest(string query, string method = "GET", string postData = null)
        {
			return Task.Run(() =>
			{
				Stopwatch stw = new Stopwatch();
				stw.Start();
				try
				{
					string url = Constants.HostName + "?" + query;
					if (Constants.isTestDB)
					{
						url += Constants.TestDB;
					}
					
					HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
					request.Timeout = Constants.RequestTimeout;
					
					if (method == "GET")
					{
						request.Method = "GET";
					}
					else
					{
						request.Method = "POST";
						byte[] byteArray = Encoding.UTF8.GetBytes(postData);
						request.ContentType = "application/x-www-form-urlencoded";
						request.ContentLength = byteArray.Length;
						Stream dataStream = request.GetRequestStream();
						dataStream.Write(byteArray, 0, byteArray.Length);
						dataStream.Close();
					}

					var response = request.GetResponse();

					stw.Stop();
					CW(stw.ElapsedMilliseconds + " " + url);

					string data = new StreamReader(response.GetResponseStream()).ReadToEnd();
					response.Close();
					if ((url.IndexOf("ID=") != -1) && IsLoggedIn())
					{
						long unixTimestamp = Now();
						Session.LastActiveDate = unixTimestamp;
					}
					if (!(snack is null) && snack.IsShown)
					{
						snack.Dismiss();
					}
					return data;

				}
				catch (Exception ex)
				{
					stw.Stop();
					if (ex is WebException)
					{
						switch (((WebException)ex).Status)
						{
							case WebExceptionStatus.ConnectFailure:
							case WebExceptionStatus.NameResolutionFailure:
							case WebExceptionStatus.SecureChannelFailure:
								return "NoNetwork";
							case WebExceptionStatus.Timeout:
								if (stw.ElapsedMilliseconds > Constants.RequestTimeout)
								{
									return "NetworkTimeout";
								}
								else //app crashed, and underlying activity was called
								{
									return ex.Message;
								}
							default:
								return ex.Message;
						}
					}
					else
					{
						return ex.Message;
					}
				}
			});          
        }

		public string MakeRequestSync(string query, string method = "GET", string postData = null)
		{
			Stopwatch stw = new Stopwatch();
			stw.Start();
			try
			{
				string url = Constants.HostName + "?" + query;
				if (Constants.isTestDB)
				{
					url += Constants.TestDB;
				}

				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
				request.Timeout = Constants.RequestTimeout;
				
				if  (method == "GET")
				{
					request.Method = "GET";
				}
				else
				{
					request.Method = "POST";
					byte[] byteArray = Encoding.UTF8.GetBytes(postData);
					request.ContentType = "application/x-www-form-urlencoded";
					request.ContentLength = byteArray.Length;
					Stream dataStream = request.GetRequestStream();
					dataStream.Write(byteArray, 0, byteArray.Length);
					dataStream.Close();
				}
				var response = request.GetResponse();
				
				stw.Stop();
				CW(stw.ElapsedMilliseconds + " " + url);

				string data = new StreamReader(response.GetResponseStream()).ReadToEnd();
				response.Close();
				if ((url.IndexOf("ID=") != -1) && IsLoggedIn())
				{
					long unixTimestamp = Now();
					Session.LastActiveDate = unixTimestamp;
				}
				if (!(snack is null) && snack.IsShown)
				{
					snack.Dismiss();
				}
				return data;
			}
			catch (Exception ex)
			{
				stw.Stop();
				if (ex is WebException)
				{
					switch(((WebException)ex).Status)
					{
						case WebExceptionStatus.ConnectFailure:
						case WebExceptionStatus.NameResolutionFailure:
						case WebExceptionStatus.SecureChannelFailure:
							return "NoNetwork";
						case WebExceptionStatus.Timeout:
							if (stw.ElapsedMilliseconds > Constants.RequestTimeout)
							{
								return "NetworkTimeout";
							}
							else //app crashed, and underlying activity was called
							{
								return ex.Message;
							}
						default:
							return ex.Message;
					}
				}
				else
				{
					return ex.Message;
				}
			}
		}

		public async Task<bool> UpdateLocationSync()
		{
			string url = "action=updatelocation&ID=" + Session.ID + "&SessionID=" + Session.SessionID
						+ "&Latitude=" + ((double)Session.Latitude).ToString(CultureInfo.InvariantCulture) + "&Longitude=" + ((double)Session.Longitude).ToString(CultureInfo.InvariantCulture) + "&LocationTime=" + Session.LocationTime + "&Background=" + !BaseActivity.isAppForeground;
			if (!string.IsNullOrEmpty(BaseActivity.locationUpdatesTo))
			{
				url += "&LocationUpdates=" + BaseActivity.locationUpdatesTo + "&Frequency=" + Session.InAppLocationRate;
				if (Session.InAppLocationRate == 0)
				{
					LogActivity("Error: location update rate is 0.");
				}
			}
			
			string responseString = await MakeRequest(url);
			if (responseString == "OK")
			{
				return true;
			}
			else
			{
				if (BaseActivity.isAppForeground)
				{
					//When logging in from another device, program crashes:
					// 'Attempt to invoke virtual method 'android.content.res.Resources android.view.View.getResources()' on a null object reference'
					if (responseString == "AUTHORIZATION_ERROR")
					{
						Intent i = new Intent(context, typeof(MainActivity));
						i.SetFlags(ActivityFlags.ReorderToFront);
						IntentData.logout = true;
						IntentData.authError = true;
						context.StartActivity(i);
					}
					else
					{
						Console.WriteLine("-----------location could not be updated, responsestring-----------" + responseString);
						/*context.RunOnUiThread(() => { //When updating from the location provider, only the caller activity can display a snack until it is paused.
							Snack(Resource.String.LocationNoUpdate, null);
						});*/
					}
				}
				else if (responseString == "AUTHORIZATION_ERROR")
				{
					((BaseActivity)context).StopLocationUpdates();	
				}
				return false;
			}
		}

		public string GetTimeDiffStr(long? pastTime, bool isShort)
		{
			Android.Content.Res.Resources res = context.Resources;
			long unixTimestamp = Now();
			if (pastTime == unixTimestamp)
			{
				if (isShort)
				{
					return res.GetString(Resource.String.Now);
				}
				else
				{
					return res.GetString(Resource.String.NowSmall);
				}
			}
			StackTrace stackTrace = new StackTrace();
			TimeSpan ts = TimeSpan.FromSeconds((long)(unixTimestamp - pastTime));

			string day = res.GetString(Resource.String.Day);
			string days = res.GetString(Resource.String.Days);
			string hour = res.GetString(Resource.String.Hour);
			string hours = res.GetString(Resource.String.Hours);
			string min, mins, sec, secs;
			if (isShort)
			{
				min = res.GetString(Resource.String.ShortMinute);
				mins = res.GetString(Resource.String.ShortMinutes);
				sec = res.GetString(Resource.String.ShortSecond);
				secs = res.GetString(Resource.String.ShortSeconds);
			}
			else
			{
				min = res.GetString(Resource.String.Minute);
				mins = res.GetString(Resource.String.Minutes);
				sec = res.GetString(Resource.String.Second);
				secs = res.GetString(Resource.String.Seconds);
			}

			string str = "";
			bool showHours = true;
			bool showMinutes = true;
			bool showSeconds = true;
			if (ts.Days > 1)
			{
				str += ts.Days + " " + days + " ";
				showHours = false;
				showMinutes = false;
				showSeconds = false;
			}
			else if (ts.Days > 0)
			{
				str += ts.Days + " " + day + " ";
				showMinutes = false;
				showSeconds = false;
			}

			if (showHours)
			{
				if (ts.Hours > 1)
				{
					str += ts.Hours + " " + hours + " ";
					showMinutes = false;
					showSeconds = false;
				}
				else if (ts.Hours > 0)
				{
					str += ts.Hours + " " + hour + " ";
					showSeconds = false;
				}
			}
			else
			{
				showMinutes = false;
				showSeconds = false;
			}

			if (showMinutes)
			{
				if (ts.Minutes > 1)
				{
					str += ts.Minutes + " " + mins + " ";
					showSeconds = false;
				}
				else if (ts.Minutes > 0)
				{
					str += ts.Minutes + " " + min + " ";
				}
			}
			else
			{
				showSeconds = false;
			}
	
			if (showSeconds)
			{
				if (ts.Seconds > 1)
				{
					str += ts.Seconds + " " + secs + " ";
				}
				else if (ts.Seconds > 0)
				{
					str += ts.Seconds + " " + sec + " ";
				}
			}
			if (str == "") //pastTime can be +1 compared to now, resulting in ts.Seconds = -1
			{
				if (isShort)
				{
					return res.GetString(Resource.String.Now);
				}
				else
				{
					return res.GetString(Resource.String.NowSmall);
				}
			}
			str += res.GetString(Resource.String.Ago);
			return str.Replace(" ", "\u00A0"); //non-breaking space
		}



		public void Snack(int messageResId)
		{
			Snackbar snack = Snackbar.Make(view, messageResId, Snackbar.LengthLong);

			View snackView = snack.View;
			TextView t = snackView.FindViewById<TextView>(Resource.Id.snackbar_text);
			t.SetTextAppearance(textSmall);
			t.SetMaxLines(5);
			snack.Show();
		}

		public void SnackStr(string message)
		{
			//Was used when the global text color was set to black. Not needed anymore, just here for future reference.
			/*SpannableStringBuilder sbb = new SpannableStringBuilder();
			sbb.Append(snackText);
			sbb.SetSpan(new ForegroundColorSpan(Color.White), 0, snackText.Length, SpanTypes.ExclusiveExclusive);*/

			Snackbar snack = Snackbar.Make(view, message, Snackbar.LengthLong);

			View snackView = snack.View;
			TextView t = snackView.FindViewById<TextView>(Resource.Id.snackbar_text);
			t.SetTextAppearance(textSmall);
			t.SetMaxLines(5);
			snack.Show();
		}

		public void SnackAction(string message, int actionText, Action<View> action) //used in ChatReceiver
		{
			Snackbar snack = Snackbar.Make(view, message, Snackbar.LengthLong).SetAction(actionText, action).SetActionTextColor(new Color(ContextCompat.GetColor(context, Resource.Color.colorAccentLight)));

			View snackView = snack.View;
			TextView t = snackView.FindViewById<TextView>(Resource.Id.snackbar_text);
			t.SetTextAppearance(textSmall);
			snack.Show();
		}

		public Snackbar SnackIndef(int messageResId) //maximum lines are 5.
		{
			Snackbar snack = Snackbar.Make(view, messageResId, Snackbar.LengthIndefinite).SetAction("OK", new Action<View>(delegate (View obj) { })).SetActionTextColor(new Color(ContextCompat.GetColor(context, Resource.Color.colorAccentLight)));

			View snackView = snack.View;
			TextView t = snackView.FindViewById<TextView>(Resource.Id.snackbar_text);
			t.SetTextAppearance(textSmall);
			t.SetMaxLines(5);
			snack.Show();
			snackPermanentText = messageResId;
			return snack;
		}

		public Snackbar SnackIndefStr(string message)
		{
			Snackbar snack = Snackbar.Make(view, message, Snackbar.LengthIndefinite).SetAction("OK", new Action<View>(delegate (View obj) { })).SetActionTextColor(new Color(ContextCompat.GetColor(context, Resource.Color.colorAccentLight)));
			View snackView = snack.View;
			TextView t = snackView.FindViewById<TextView>(Resource.Id.snackbar_text);
			t.SetTextAppearance(textSmall);
			t.SetMaxLines(5);
			snack.Show();
			return snack;
		}

		public void SnackIndefAction(string message, Action<View> action) //used in RegisterCommonMethods
		{
			Snackbar snack = Snackbar.Make(view, message, Snackbar.LengthIndefinite).SetAction("OK", action).SetActionTextColor(new Color(ContextCompat.GetColor(context, Resource.Color.colorAccentLight)));

			View snackView = snack.View;
			TextView t = snackView.FindViewById<TextView>(Resource.Id.snackbar_text);
			t.SetTextAppearance(textSmall);
			snack.Show();
		}

		public Task<string> DisplayCustomDialog(string dialogTitle, string dialogMessage, string dialogPositiveBtnLabel, string dialogNegativeBtnLabel)
		{
			var tcs = new TaskCompletionSource<string>();

			Android.App.AlertDialog.Builder alert = new Android.App.AlertDialog.Builder(context);
			alert.SetTitle(dialogTitle);
			alert.SetMessage(dialogMessage);
			alert.SetPositiveButton(dialogPositiveBtnLabel, (senderAlert, args) => {
				tcs.SetResult(dialogPositiveBtnLabel);
			});

			alert.SetNegativeButton(dialogNegativeBtnLabel, (senderAlert, args) => {
				tcs.SetResult(dialogNegativeBtnLabel);
			});

			Dialog dialog = alert.Create();
			dialog.Show();

			return tcs.Task;
		}

		public Task<string> DisplaySimpleDialog(string dialogTitle, string dialogMessage, string dialogPositiveBtnLabel)
		{
			var tcs = new TaskCompletionSource<string>();

			Android.App.AlertDialog.Builder alert = new Android.App.AlertDialog.Builder(context);
			alert.SetTitle(dialogTitle);
			alert.SetMessage(dialogMessage);
			alert.SetPositiveButton(dialogPositiveBtnLabel, (senderAlert, args) => {
				tcs.SetResult(dialogPositiveBtnLabel);
			});

			Dialog dialog = alert.Create();
			dialog.Show();

			return tcs.Task;
		}

		public Task<string> Alert(object message)
		{
			TextView msg = GetAlertText(alertTextSize, alertPadding);
			msg.Text = message.ToString();

			var tcs = new TaskCompletionSource<string>();
			Android.App.AlertDialog.Builder alert = new Android.App.AlertDialog.Builder(context);
			alert.SetView(msg);
			alert.SetPositiveButton("OK", (senderAlert, args) => {
				tcs.SetResult("OK");
			});

			Dialog dialog = alert.Create();
			dialog.Show();

			return tcs.Task;
		}

		public Task<string> AlertLinks(string dialogMessage)
		{
			TextView msg = GetAlertText(alertTextSize, alertPadding);
			SpannableString s = new SpannableString(dialogMessage);
			Linkify.AddLinks(s, MatchOptions.WebUrls | MatchOptions.EmailAddresses);
			msg.TextFormatted = s;
			msg.MovementMethod = LinkMovementMethod.Instance;

			var tcs = new TaskCompletionSource<string>();
			AlertDialog.Builder alert = new AlertDialog.Builder(context);
			alert.SetView(msg);
			alert.SetPositiveButton("OK", (senderAlert, args) => {
				tcs.SetResult("OK");
			});

			Dialog dialog = alert.Create();
			dialog.Show();

			return tcs.Task;
		}

		public Task<string> LogAlert(string dialogMessage)
		{
			dialogMessage = dialogMessage.Replace(DateTime.UtcNow.ToString(@"yyyy-MM-dd "), "");

			TextView msg = GetAlertText(logAlertTextSize, logAlertPadding);
			msg.Text = dialogMessage;
			ScrollView scroll = new ScrollView(context);
			scroll.ScrollbarFadingEnabled = false;
			scroll.AddView(msg);

			var tcs = new TaskCompletionSource<string>();
			AlertDialog.Builder alert = new AlertDialog.Builder(context);
			alert.SetView(scroll);
			alert.SetPositiveButton("OK", (senderAlert, args) => {
				tcs.SetResult("OK");
			});

			Dialog dialog = alert.Create();
			dialog.Show();

			return tcs.Task;
		}

		public Task<string> ErrorAlert(string message)
		{
			TextView msg = GetAlertText(alertTextSize, alertPadding);
			msg.Text = message;

			var tcs = new TaskCompletionSource<string>();
			Android.App.AlertDialog.Builder alert = new Android.App.AlertDialog.Builder(context);
			alert.SetTitle(context.Resources.GetString(Resource.String.ErrorEncountered));
			alert.SetView(msg);
			alert.SetPositiveButton("OK", (senderAlert, args) => {
				tcs.SetResult("OK");
			});

			Dialog dialog = alert.Create();
			dialog.Show();

			return tcs.Task;
		}

		private TextView GetAlertText(float size, float padding)
		{
			int paddingInt = (int)(padding * BaseActivity.pixelDensity);
			TextView msg = new TextView(context);
			msg.SetPadding(paddingInt, paddingInt, paddingInt, paddingInt);
			msg.SetTextColor(Color.Black);
			msg.SetTextSize(Android.Util.ComplexUnitType.Dip, size);
			return msg;
		}

		public void Msg(string message)
        {
            Toast.MakeText(context, message, ToastLength.Long).Show();
        }

        public void LogActivity(string message)
        {
            try
            {
                File.AppendAllLines(logFile, new string[] { DateTime.UtcNow.ToString(@"yyyy-MM-dd HH\:mm\:ss.fff") + "  " + message });
            }
            catch
            {
            }
        }

		public static void LogActivityStatic(string message)
		{
			try
			{
				File.AppendAllLines(logFile, new string[] { DateTime.UtcNow.ToString(@"yyyy-MM-dd HH\:mm\:ss.fff") + "  " + message });
			}
			catch
			{
			}
		}

		public void LogLocation(string message)
		{
			try
			{
				File.AppendAllLines(locationLogFile, new string[] { message });
			}
			catch
			{
			}
		}

		public void LogError(string message) //only the last record is kept, to prevent the file from growing if there is a problem with the server.
		{
			try
			{
				int ID = (Session.ID is null) ? 0 : (int)Session.ID;
				File.WriteAllLines(errorFile, new string[] { DateTime.UtcNow.ToString(@"yyyy.MM.dd. HH\:mm\:ss") + " " + message + ", ID: " + ID });
			}
			catch
			{
			}
		}

		public void ReportError(string error)
		{
			if (error == "AUTHORIZATION_ERROR")
			{
				Intent i = new Intent(context, typeof(MainActivity));
				i.SetFlags(ActivityFlags.ReorderToFront);
				IntentData.logout = true;
				IntentData.authError = true;
				context.StartActivity(i);
			}
			else if (error == "The operation has timed out.") //activity crashed, and now the underlying activity is called that throws this error
			{
				LogError(error);
				ErrorAlert(error + System.Environment.NewLine + System.Environment.NewLine + context.Resources.GetString(Resource.String.ErrorNotificationToSend));
			}
			else if (error == "NoNetwork")
			{
				snack = SnackIndef(Resource.String.NoNetwork);
			}
			else if (error == "NetworkTimeout")
			{
				snack = SnackIndef(Resource.String.NetworkTimeout);
			}
			else {
				string url = "action=reporterror&ID=" + Session.ID + "&SessionID=" + Session.SessionID;
				string content = "Content=" + UrlEncode(error + System.Environment.NewLine
					+ "Android version: " + Build.VERSION.SdkInt + " " + Build.VERSION.Sdk + " " + System.Environment.NewLine + Build.VERSION.BaseOs + System.Environment.NewLine + File.ReadAllText(logFile));
				string responseString = MakeRequestSync(url, "POST", content);
				if (responseString == "OK")
				{
					ErrorAlert(error + System.Environment.NewLine + System.Environment.NewLine + context.Resources.GetString(Resource.String.ErrorNotificationSent));
				}
				else
				{
					LogError(error);
					ErrorAlert(error + System.Environment.NewLine + System.Environment.NewLine + context.Resources.GetString(Resource.String.ErrorNotificationToSend));
				}
			}
		}

		public void ReportErrorSilent(string error)
		{
			if (error == "AUTHORIZATION_ERROR")
			{
				Intent i = new Intent(context, typeof(MainActivity));
				i.SetFlags(ActivityFlags.ReorderToFront);
				IntentData.logout = true;
				IntentData.authError = true;
				context.StartActivity(i);
			}
			else if (error != "The operation has timed out." && error != "NoNetwork" && error != "NetworkTimeout")
			{
				string url = "action=reporterror&ID=" + Session.ID + "&SessionID=" + Session.SessionID;
				string content = "Content=" + UrlEncode(error + System.Environment.NewLine
					+ "Android version: " + Build.VERSION.SdkInt + " " + Build.VERSION.Sdk + " " + System.Environment.NewLine + Build.VERSION.BaseOs + System.Environment.NewLine + File.ReadAllText(logFile));
				MakeRequestSync(url, "POST", content);
			}
		}

		public void ReportErrorSnackNext(string error)
		{
			if (error == "AUTHORIZATION_ERROR")
			{
				Intent i = new Intent(context, typeof(MainActivity));
				i.SetFlags(ActivityFlags.ReorderToFront);
				IntentData.logout = true;
				IntentData.authError = true;
				context.StartActivity(i);
			}
			else if (error == "NoNetwork")
			{
				Session.SnackMessage = context.Resources.GetString(Resource.String.NoNetwork);
			}
			else if (error == "NetworkTimeout")
			{
				Session.SnackMessage = context.Resources.GetString(Resource.String.NetworkTimeout);
			}
			else
			{
				Session.SnackMessage = error;
				string url = "action=reporterror&ID=" + Session.ID + "&SessionID=" + Session.SessionID;
				string content = "Content=" + UrlEncode(error + System.Environment.NewLine
					+ "Android version: " + Build.VERSION.SdkInt + " " + Build.VERSION.Sdk + " " + System.Environment.NewLine + Build.VERSION.BaseOs + System.Environment.NewLine + File.ReadAllText(logFile));
				MakeRequestSync(url, "POST", content);
			}
		}

		public void CW(object message)
		{
			//StackTrace stackTrace = new StackTrace();
			Console.WriteLine(System.Environment.NewLine + "----------" + message + "----------" + System.Environment.NewLine);
		}

		public string ShowClass<T>()
		{
			string str = "";
			Type type = typeof(T);
			FieldInfo[] fieldInfo = type.GetFields();
			foreach (FieldInfo field in fieldInfo)
			{
				str += field.Name + ": " + field.GetValue(null) + "\n";
			}
			return str;
		}		

		public long Now()
		{
			return (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
		}

		public string UrlEncode(string input)
		{
			if (!string.IsNullOrEmpty(input))
			{
				return input.Replace("#", "%23").Replace("&", "%26").Replace("+", "%2B");
			}
			else
			{
				return "";
			}			
		}

		public static string UnescapeBraces(string input)
		{
			return input.Replace(@"\{", "{").Replace(@"\}", "}").Replace(@"\""", @"""");
		}

        public static object Clone(object obj)
        {
			Type type = obj.GetType();
			object newObj = Activator.CreateInstance(type);

            FieldInfo[] fieldInfos = type.GetFields();
            foreach (FieldInfo field in fieldInfos)
            {
				object value = field.GetValue(obj);
                if (value != null)
                {
					field.SetValue(newObj, value);
                }
            }
			
			return newObj;
        }

		//Image loading optimization. Of 3 methods, HttpClient is the fastest.

		/*public static Bitmap GetImageBitmapFromUrl(string url)
		{
			byte[] data = GetImageDataFromUrl(url);
			if (!(data is null))
			{
				Stopwatch stw = new Stopwatch();
				stw.Start();

				Bitmap bmp = BitmapFactory.DecodeByteArray(data, 0, data.Length);

				stw.Stop();
				Console.WriteLine("------------- Decoded in " + stw.ElapsedMilliseconds + " ------------");
				LogActivityStatic("Decoded in " + stw.ElapsedMilliseconds);

				return bmp;
			}
			return null;
		}*/

		/*public static byte[] GetImageDataFromUrl(string url)
		{
			try
			{
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
				request.Timeout = Constants.RequestTimeout;
				request.Method = "GET";

				Stopwatch stw = new Stopwatch();
				stw.Start();

				var response = request.GetResponse();

				stw.Stop();
				Console.WriteLine("------------ Load in " + stw.ElapsedMilliseconds);
				LogActivityStatic("Request in " + stw.ElapsedMilliseconds);
				stw.Restart();

				byte[] data = ReadToEnd(response.GetResponseStream());

				stw.Stop();
				Console.WriteLine("------------- Read in " + stw.ElapsedMilliseconds + " -------------- ");
				LogActivityStatic("Read in " + stw.ElapsedMilliseconds);
				return data;
			}
			catch (Exception ex)
			{
				Console.WriteLine("-------- Error loading image at " + url + ": " + ex.Message);
				LogActivityStatic("Error loading image at " + url + ": " + ex.Message);

				return null;
			}
		}*/

		/*private static byte[] ReadFully(Stream input)
		{
			try
			{
				int bytesBuffer = 1024;
				byte[] buffer = new byte[bytesBuffer];
				using (MemoryStream ms = new MemoryStream())
				{
					int readBytes;
					while ((readBytes = input.Read(buffer, 0, buffer.Length)) > 0)
					{
						ms.Write(buffer, 0, readBytes);
					}
					return ms.ToArray();
				}
			}
			catch (Exception ex)
			{
				// Exception handling here:  Response.Write("Ex.: " + ex.Message);
				return null;
			}
		}*/

		//takes 1.5 - 2 times more time than using HttpClient
		//https://stackoverflow.com/questions/1080442/how-to-convert-an-stream-into-a-byte-in-c
		/*public static byte[] ReadToEnd(System.IO.Stream stream)
		{
			long originalPosition = 0;

			if (stream.CanSeek)
			{
				originalPosition = stream.Position;
				stream.Position = 0;
			}

			try
			{
				byte[] readBuffer = new byte[4096];

				int totalBytesRead = 0;
				int bytesRead;

				while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
				{
					totalBytesRead += bytesRead;

					if (totalBytesRead == readBuffer.Length)
					{
						int nextByte = stream.ReadByte();
						if (nextByte != -1)
						{
							byte[] temp = new byte[readBuffer.Length * 2];
							Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
							Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
							readBuffer = temp;
							totalBytesRead++;
						}
					}
				}

				byte[] buffer = readBuffer;
				if (readBuffer.Length != totalBytesRead)
				{
					buffer = new byte[totalBytesRead];
					Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
				}
				return buffer;
			}
			finally
			{
				if (stream.CanSeek)
				{
					stream.Position = originalPosition;
				}
			}
		}*/

		// Sometimes picture doesn't load. // Dispose(); was not tried.
		/*public static Bitmap GetImageBitmapFromUrl(string url)
		{
			try
			{
				using (var webClient = new WebClient())
				{
					var imageBytes = webClient.DownloadData(url);
					webClient.Dispose();
					if (imageBytes != null && imageBytes.Length > 0)
					{
						return BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length);
					}
				}
			}
			catch (Exception ex)
			{
				LogActivityStatic(" Error loading image: " + url + " " + ex.Message);
				Console.WriteLine("Error loading image: " + url + " " + ex.Message);
			}
			return null;
		}*/

		/*public static byte[] GetImageDataFromUrl(string url)
		{
			try
			{
				using (var webClient = new WebClient())
				{
					Stopwatch stw = new Stopwatch();
					stw.Start();

					byte[] imageBytes = webClient.DownloadData(url);

					stw.Stop();
					Console.WriteLine("------------- Load/Read in " + stw.ElapsedMilliseconds + " -------------- ");
					LogActivityStatic("Load/Read in " + stw.ElapsedMilliseconds);

					webClient.Dispose();
					return imageBytes;
				}
			}
			catch (Exception ex)
			{
				LogActivityStatic(" Error loading image: " + url + " " + ex.Message);
				Console.WriteLine("Error loading image: " + url + " " + ex.Message);
			}
			return null;
		}*/

		public static async Task<Bitmap> GetImageBitmapFromUrlAsync(string url)
		{
			try
			{
				/*Stopwatch stw = new Stopwatch();
				stw.Start();*/

				using var httpResponse = await client.GetAsync(url);

				/*stw.Stop();
				Console.WriteLine("------------- Load in " + stw.ElapsedMilliseconds + " " + url + " -------------- " + System.Environment.NewLine);
				LogActivityStatic("Load in " + stw.ElapsedMilliseconds + " " + url);
				stw.Restart();*/

				if (httpResponse.StatusCode == HttpStatusCode.OK)
				{

					byte[] result = await httpResponse.Content.ReadAsByteArrayAsync();

					/*stw.Stop();
					Console.WriteLine("------------- Read in " + stw.ElapsedMilliseconds + " -------------- " + System.Environment.NewLine);
					LogActivityStatic("Read in " + stw.ElapsedMilliseconds);*/

					httpResponse.Dispose();
					return BitmapFactory.DecodeByteArray(result, 0, result.Length);
				}
			}
			catch (Exception ex)
			{
				LogActivityStatic(" Error loading image: " + url + " " + ex.Message);
				Console.WriteLine("Error loading image: " + url + " " + ex.Message);
			}
			return null;
		}

		public static async Task<byte[]> GetImageDataFromUrlAsync(string url)
		{
			try
			{
				/*Stopwatch stw = new Stopwatch();
				stw.Start();*/

				using var httpResponse = await client.GetAsync(url);

				/*stw.Stop();
				LogActivityStatic("Load in " + stw.ElapsedMilliseconds + " " + url);
				Console.WriteLine("------------- Load in " + stw.ElapsedMilliseconds + " " + url + "-------------- " + System.Environment.NewLine);
				stw.Restart();*/

				if (httpResponse.StatusCode == HttpStatusCode.OK)
				{				
					byte[] imageBytes = await httpResponse.Content.ReadAsByteArrayAsync();

					/*stw.Stop();
					LogActivityStatic("Read in " + stw.ElapsedMilliseconds);
					Console.WriteLine("------------- Read in " + stw.ElapsedMilliseconds + " -------------- "); //most often it takes 0 ms, or just a few.*/

					httpResponse.Dispose();
					return imageBytes;
				}
			}
			catch (Exception ex)
			{
				LogActivityStatic(" Error loading image: " + url + " " + ex.Message);
				Console.WriteLine("Error loading image: " + url + " " + ex.Message);
			}
			return null;
		}

		public static void EmptyFolder(string dir)
		{
			var list = Directory.GetFiles(dir, "*");
			//Console.WriteLine("--------------- Emptyfolder: " + list.Length);
			if (list.Length > 0)
			{
				for (int i = 0; i < list.Length; i++)
				{
					File.Delete(list[i]);
					//Console.WriteLine("-------------- Deleted from folder: " + list[i]);
				}
			}
		}
		/*
		 * Alternatively, but this is async:
		var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
		using var httpResponse = await client.GetAsync(url);
		if (httpResponse.StatusCode == HttpStatusCode.OK)
		{
			byte[] result = await httpResponse.Content.ReadAsByteArrayAsync();
			return BitmapFactory.DecodeByteArray(result, 0, result.Length);
		}
		else
		{
			//Url is Invalid
			return null;
		}
		*/

		/*
		protected void CreateLocationRequest()
		{
			LocationRequest locationRequest = LocationRequest.Create();
			locationRequest.SetInterval(Constants.InAppLocationRate).SetPriority(LocationRequest.PriorityHighAccuracy);

			LocationSettingsRequest.Builder builder = new LocationSettingsRequest.Builder().AddLocationRequest(locationRequest);
			SettingsClient client = LocationServices.GetSettingsClient(this);
			Android.Gms.Tasks.Task task = client.CheckLocationSettings(builder.Build());
		}*/
	}
}