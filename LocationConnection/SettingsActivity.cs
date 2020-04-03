using System;
using System.IO;
using System.Timers;
using Android.App;
using Android.Content;
using Android.Gms.Maps;
using Android.OS;
using Android.Support.Constraints;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;

namespace LocationConnection
{
	[Activity(MainLauncher = false, ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
	public class SettingsActivity : BaseActivity
	{
		ImageButton SettingsBack;
		CheckBox CheckMatchInApp, CheckMessageInApp, CheckUnmatchInApp, CheckRematchInApp,
			CheckMatchBackground, CheckMessageBackground, CheckUnmatchBackground, CheckRematchBackground;
		SeekBar MapIconSize, MapRatio, InAppLocationRate, BackgroundLocationRate;
		TextView MapIconSizeValue, MapRatioValue, InAppLocationRateValue, BackgroundLocationRateValue;
		Switch BackgroundLocation;
		RadioButton SmallDisplaySize, NormalDisplaySize, LocationAccuracyPrecise, LocationAccuracyBalanced;
		Button LocationHistoryButton;

		TextView SettingsFormCaption;
		EditText MessageEdit;
		Button MessageSend, ProgramLogButton;
		TextView ProgramLogIncluded;

		InputMethodManager imm;

		protected override void OnCreate(Bundle savedInstanceState)
		{
			try { 
				base.OnCreate(savedInstanceState);
				if (!ListActivity.initialized) { return; }

				if (Settings.DisplaySize == 1)
				{
					SetContentView(Resource.Layout.activity_settings_normal);
				}
				else
				{
					SetContentView(Resource.Layout.activity_settings_small);
				}

				MainLayout = FindViewById<ScrollView>(Resource.Id.MainLayout);
				SettingsBack = FindViewById<ImageButton>(Resource.Id.SettingsBack);
				CheckMatchInApp = FindViewById<CheckBox>(Resource.Id.CheckMatchInApp);
				CheckMessageInApp = FindViewById<CheckBox>(Resource.Id.CheckMessageInApp);
				CheckUnmatchInApp = FindViewById<CheckBox>(Resource.Id.CheckUnmatchInApp);
				CheckRematchInApp = FindViewById<CheckBox>(Resource.Id.CheckRematchInApp);
				CheckMatchBackground = FindViewById<CheckBox>(Resource.Id.CheckMatchBackground);
				CheckMessageBackground = FindViewById<CheckBox>(Resource.Id.CheckMessageBackground);
				CheckUnmatchBackground = FindViewById<CheckBox>(Resource.Id.CheckUnmatchBackground);
				CheckRematchBackground = FindViewById<CheckBox>(Resource.Id.CheckRematchBackground);
				SmallDisplaySize = FindViewById<RadioButton>(Resource.Id.SmallDisplaySize);
				NormalDisplaySize = FindViewById<RadioButton>(Resource.Id.NormalDisplaySize);
				MapIconSize = FindViewById<SeekBar>(Resource.Id.MapIconSize);
				MapIconSizeValue = FindViewById<TextView>(Resource.Id.MapIconSizeValue);
				MapRatio = FindViewById<SeekBar>(Resource.Id.MapRatio);
				MapRatioValue = FindViewById<TextView>(Resource.Id.MapRatioValue);			
				LocationAccuracyPrecise = FindViewById<RadioButton>(Resource.Id.LocationAccuracyPrecise);
				LocationAccuracyBalanced = FindViewById<RadioButton>(Resource.Id.LocationAccuracyBalanced);
				BackgroundLocation = FindViewById<Switch>(Resource.Id.BackgroundLocation);
				InAppLocationRate = FindViewById<SeekBar>(Resource.Id.InAppLocationRate);
				InAppLocationRateValue = FindViewById<TextView>(Resource.Id.InAppLocationRateValue);
				BackgroundLocationRate = FindViewById<SeekBar>(Resource.Id.BackgroundLocationRate);
				BackgroundLocationRateValue = FindViewById<TextView>(Resource.Id.BackgroundLocationRateValue);
				LocationHistoryButton = FindViewById<Button>(Resource.Id.LocationHistoryButton);

				SettingsFormCaption = FindViewById<TextView>(Resource.Id.SettingsFormCaption);
				MessageEdit = FindViewById<EditText>(Resource.Id.MessageEdit);
				MessageSend = FindViewById<Button>(Resource.Id.MessageSend);
				ProgramLogIncluded = FindViewById<TextView>(Resource.Id.ProgramLogIncluded);
				ProgramLogButton = FindViewById<Button>(Resource.Id.ProgramLogButton);
			
				c.view = MainLayout;
				imm = (InputMethodManager)GetSystemService(Context.InputMethodService);
				Window.SetSoftInputMode(SoftInput.AdjustResize);

				SmallDisplaySize.Click += SmallDisplaySize_Click;
				NormalDisplaySize.Click += NormalDisplaySize_Click;
				MapIconSize.Max = MapIconSizeValToProgress(Constants.MapIconSizeMax);
				MapRatio.Max = MapRatioValToProgress(Constants.MapRatioMax);			
				InAppLocationRate.Max = InAppLocationRateValToProgress(Constants.InAppLocationRateMax);
				BackgroundLocationRate.Max = BackgroundLocationRateValToProgress(Constants.BackgroundLocationRateMax);

				SettingsBack.Click += SettingsBack_Click;
				MapIconSize.ProgressChanged += MapIconSize_ProgressChanged;
				MapRatio.ProgressChanged += MapRatio_ProgressChanged;
				BackgroundLocation.Click += BackgroundLocation_Click;
				InAppLocationRate.ProgressChanged += InAppLocationRate_ProgressChanged;
				BackgroundLocationRate.ProgressChanged += BackgroundLocationRate_ProgressChanged;
				LocationHistoryButton.Click += LocationHistoryButton_Click;
				SettingsFormCaption.Click += SettingsFormCaption_Click;
				MessageEdit.Click += MessageEdit_Click;
				MessageSend.Click += MessageSend_Click;
				ProgramLogButton.Click += ProgramLogButton_Click;
			}
			catch (Exception ex)
			{
				c.ReportErrorSilent(ex.Message + System.Environment.NewLine + ex.StackTrace);
			}
		}
		private void NormalDisplaySize_Click(object sender, EventArgs e)
		{
			/*Recreate();
			Intent i = new Intent(this, typeof(SettingsActivity));
			i.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTask);
			i.PutExtra("recreate", "true");
			StartActivity(i);*/
		}

		private void SmallDisplaySize_Click(object sender, EventArgs e)
		{
			/*Recreate();
			Intent i = new Intent(this, typeof(SettingsActivity));
			i.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTask);
			i.PutExtra("recreate", "true");
			StartActivity(i);*/
		}

		protected override void OnResume()
		{
			try
			{
				base.OnResume();
				if (!ListActivity.initialized) { return; }

				SmallDisplaySize.Checked = (Settings.DisplaySize == 1) ? false : true;
				NormalDisplaySize.Checked = (Settings.DisplaySize == 1) ? true : false;
				MapIconSize.Progress = MapIconSizeValToProgress((int)Settings.MapIconSize);
				MapIconSizeValue.Text = Settings.MapIconSize.ToString();
				MapRatio.Progress = MapRatioValToProgress((float)Settings.MapRatio);
				MapRatioValue.Text = Math.Round((float)Settings.MapRatio, 2).ToString();

				if (!c.IsLocationEnabled() || !(bool)Session.UseLocation)
				{
					LocationAccuracyPrecise.Enabled = false;
					LocationAccuracyBalanced.Enabled = false;
					BackgroundLocation.Enabled = false;
					InAppLocationRate.Enabled = false;
					BackgroundLocationRate.Enabled = false;
				}
				else
				{
					LocationAccuracyPrecise.Enabled = true;
					LocationAccuracyBalanced.Enabled = true;
					InAppLocationRate.Enabled = true;
					if (c.IsLoggedIn())
					{
						BackgroundLocation.Enabled = true;
						BackgroundLocationRate.Enabled = true;
					}
					else
					{
						BackgroundLocation.Enabled = false;
						BackgroundLocationRate.Enabled = false;
					}
				}

				if (c.IsLoggedIn())
				{
					CheckMatchInApp.Enabled = true;
					CheckMessageInApp.Enabled = true;
					CheckUnmatchInApp.Enabled = true;
					CheckRematchInApp.Enabled = true;
					CheckMatchBackground.Enabled = true;
					CheckMessageBackground.Enabled = true;
					CheckUnmatchBackground.Enabled = true;
					CheckRematchBackground.Enabled = true;

					CheckMatchInApp.Checked = (bool)Session.MatchInApp;
					CheckMessageInApp.Checked = (bool)Session.MessageInApp;
					CheckUnmatchInApp.Checked = (bool)Session.UnmatchInApp;
					CheckRematchInApp.Checked = (bool)Session.RematchInApp;
					CheckMatchBackground.Checked = (bool)Session.MatchBackground;
					CheckMessageBackground.Checked = (bool)Session.MessageBackground;
					CheckUnmatchBackground.Checked = (bool)Session.UnmatchBackground;
					CheckRematchBackground.Checked = (bool)Session.RematchBackground;

					BackgroundLocation.Checked = (bool)Session.BackgroundLocation;
					if (BackgroundLocation.Checked)
					{
						BackgroundLocationRate.Enabled = true;
					}
					else
					{
						BackgroundLocationRate.Enabled = false;
					}

					LocationAccuracyPrecise.Checked = (Session.LocationAccuracy == 0) ? false : true;
					LocationAccuracyBalanced.Checked = (Session.LocationAccuracy == 0) ? true : false;
					InAppLocationRate.Progress = InAppLocationRateValToProgress((int)Session.InAppLocationRate);
					InAppLocationRateValue.Text = GetTimeString((int)Session.InAppLocationRate);
					BackgroundLocationRate.Progress = BackgroundLocationRateValToProgress((int)Session.BackgroundLocationRate);
					BackgroundLocationRateValue.Text = GetTimeString((int)Session.BackgroundLocationRate);
				}
				else
				{
					CheckMatchInApp.Enabled = false;
					CheckMessageInApp.Enabled = false;
					CheckUnmatchInApp.Enabled = false;
					CheckRematchInApp.Enabled = false;
					CheckMatchBackground.Enabled = false;
					CheckMessageBackground.Enabled = false;
					CheckUnmatchBackground.Enabled = false;
					CheckRematchBackground.Enabled = false;

					CheckMatchInApp.Checked = false;
					CheckMessageInApp.Checked = false;
					CheckUnmatchInApp.Checked = false;
					CheckRematchInApp.Checked = false;
					CheckMatchBackground.Checked = false;
					CheckMessageBackground.Checked = false;
					CheckUnmatchBackground.Checked = false;
					CheckRematchBackground.Checked = false;
	
					BackgroundLocation.Checked = false;
					LocationAccuracyPrecise.Checked = (Settings.LocationAccuracy == 0) ? false : true;
					LocationAccuracyBalanced.Checked = (Settings.LocationAccuracy == 0) ? true : false;
					InAppLocationRate.Progress = InAppLocationRateValToProgress((int)Settings.InAppLocationRate);
					InAppLocationRateValue.Text = GetTimeString((int)Settings.InAppLocationRate);
					BackgroundLocationRate.Progress = 0;
					BackgroundLocationRateValue.Text = "";
				}
			}
			catch (Exception ex)
			{
				c.ReportErrorSilent(ex.Message + System.Environment.NewLine + ex.StackTrace);
			}
		}

		protected override async void OnPause()
		{
			base.OnPause();

			c.CW("Settings OnPause start");

			if (!ListActivity.initialized) { return; }

			bool changed = false;

			if (SmallDisplaySize.Checked && Settings.DisplaySize == 1)
			{
				Settings.DisplaySize = 0;
				changed = true;
			}
			else if (NormalDisplaySize.Checked && Settings.DisplaySize == 0)
			{
				Settings.DisplaySize = 1;
				changed = true;
			}

			if (Settings.MapIconSize != MapIconSizeProgressToVal(MapIconSize.Progress))
			{
				Session.LastDataRefresh = null;
				Settings.MapIconSize = MapIconSizeProgressToVal(MapIconSize.Progress);
				changed = true;
			}
			if (Settings.MapRatio != MapRatioProgressToVal(MapRatio.Progress))
			{
				Settings.MapRatio = MapRatioProgressToVal(MapRatio.Progress);
				changed = true;
			}

			if (c.IsLoggedIn())
			{
				string requestStringBase = "action=updatesettings&ID=" + Session.ID + "&SessionID=" + Session.SessionID;
				string requestStringAdd = "";

				if (CheckMatchInApp.Checked != Session.MatchInApp)
				{
					requestStringAdd += "&MatchInApp=" + CheckMatchInApp.Checked;
				}
				if (CheckMessageInApp.Checked != Session.MessageInApp)
				{
					requestStringAdd += "&MessageInApp=" + CheckMessageInApp.Checked;
				}
				if (CheckUnmatchInApp.Checked != Session.UnmatchInApp)
				{
					requestStringAdd += "&UnmatchInApp=" + CheckUnmatchInApp.Checked;
				}
				if (CheckRematchInApp.Checked != Session.RematchInApp)
				{
					requestStringAdd += "&RematchInApp=" + CheckRematchInApp.Checked;
				}
				if (CheckMatchBackground.Checked != Session.MatchBackground)
				{
					requestStringAdd += "&MatchBackground=" + CheckMatchBackground.Checked;
				}
				if (CheckMessageBackground.Checked != Session.MessageBackground)
				{
					requestStringAdd += "&MessageBackground=" + CheckMessageBackground.Checked;
				}
				if (CheckUnmatchBackground.Checked != Session.UnmatchBackground)
				{
					requestStringAdd += "&UnmatchBackground=" + CheckUnmatchBackground.Checked;
				}
				if (CheckRematchBackground.Checked != Session.RematchBackground)
				{
					requestStringAdd += "&RematchBackground=" + CheckRematchBackground.Checked;
				}

				if (BackgroundLocation.Checked != Session.BackgroundLocation)
				{
					requestStringAdd += "&BackgroundLocation=" + BackgroundLocation.Checked;
				}

				if (LocationAccuracyPrecise.Checked && Session.LocationAccuracy == 0)
				{
					requestStringAdd += "&LocationAccuracy=1";
				}
				else if (LocationAccuracyBalanced.Checked && Session.LocationAccuracy == 1)
				{
					requestStringAdd += "&LocationAccuracy=0";
				}

				if (InAppLocationRateProgressToVal(InAppLocationRate.Progress) != Session.InAppLocationRate)
				{
					requestStringAdd += "&InAppLocationRate=" + InAppLocationRateProgressToVal(InAppLocationRate.Progress);
				}
				if (BackgroundLocationRateProgressToVal(BackgroundLocationRate.Progress) != Session.BackgroundLocationRate)
				{
					requestStringAdd += "&BackgroundLocationRate=" + BackgroundLocationRateProgressToVal(BackgroundLocationRate.Progress);
				}

				if (requestStringAdd != "") //if the form was changed
				{
					string responseString = await c.MakeRequest(requestStringBase + requestStringAdd);
					if (responseString.Substring(0, 2) == "OK")
					{
						if (responseString.Length > 2) //a change happened
						{
							c.LoadCurrentUser(responseString);
						}
					}
					else
					{
						//since ListActivity onResume is already called now, this will display in the next page.
						//alert dialog does not show
						c.ReportErrorSnackNext(responseString);
					}
				}
			}
			else
			{
				if (LocationAccuracyPrecise.Checked && Settings.LocationAccuracy == 0)
				{
					Settings.LocationAccuracy = 1;
					changed = true;
				}
				else if (LocationAccuracyBalanced.Checked && Settings.LocationAccuracy == 1)
				{
					Settings.LocationAccuracy = 0;
					changed = true;
				}

				if (InAppLocationRateProgressToVal(InAppLocationRate.Progress) != Settings.InAppLocationRate)
				{
					Settings.InAppLocationRate = InAppLocationRateProgressToVal(InAppLocationRate.Progress);
					changed = true;
				}
				//location updates will restart by the changed value
			}

			if (changed)
			{
				c.SaveSettings();
			}
			c.CW("Settings OnPause end");
		}

		public override void OnBackPressed()
		{
			bool changed = false;
			if (SmallDisplaySize.Checked && Settings.DisplaySize == 1 || NormalDisplaySize.Checked && Settings.DisplaySize == 0)
			{
				changed = true;
			}

			if (!changed) //resume ListActivity
			{
				base.OnBackPressed();
			}
			else //create all new activities
			{
				c.LogActivity("Changing display size");
				Intent i = new Intent(this, typeof(ListActivity));
				i.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);
				StartActivity(i);
			}
		}

		private void SettingsBack_Click(object sender, EventArgs e)
		{
			OnBackPressed();
		}

		//setMin cannot be used before API 26 (Android 8). Even then, incorrect behaviour results, with maximum value cannot exceeding max - min.
		private int MapIconSizeValToProgress(int value) // value range: 10 - 200, slider 0 - 190
		{
			return value - 10;
		}

		private int MapIconSizeProgressToVal(int progress)
		{
			return progress + 10;
		}
		private int MapRatioValToProgress(float value) // 0.46 - 2.17, slider 0 - 171
		{
			return (int)((value - Constants.MapRatioMin) * 100);
		}

		private float MapRatioProgressToVal(int progress)
		{
			return (float)Math.Round((float)progress / 100 + Constants.MapRatioMin, 2);
		}

		private int InAppLocationRateValToProgress(int value) // 15 - 300, slider 0 - 57
		{
			return (value - 15) / 5;
		}

		private int InAppLocationRateProgressToVal(int progress)
		{
			return progress * 5 + 15;
		}

		private int BackgroundLocationRateValToProgress(int value) // 300 - 3600, slider 0 - 55
		{
			return (value - 300) / 60;
		}

		private int BackgroundLocationRateProgressToVal(int progress)
		{
			return progress * 60 + 300;
		}

		private void MapIconSize_ProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e)
		{
			MapIconSizeValue.Text = MapIconSizeProgressToVal(MapIconSize.Progress).ToString();
		}

		private void MapRatio_ProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e)
		{
			MapRatioValue.Text = MapRatioProgressToVal(MapRatio.Progress).ToString();
		}

		private void BackgroundLocation_Click(object sender, EventArgs e)
		{
			if (BackgroundLocation.Checked)
			{
				BackgroundLocationRate.Enabled = true;
			}
			else
			{
				BackgroundLocationRate.Enabled = false;
			}
		}

		private void InAppLocationRate_ProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e)
		{
			InAppLocationRateValue.Text = GetTimeString(InAppLocationRateProgressToVal(InAppLocationRate.Progress));
		}

		private void BackgroundLocationRate_ProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e)
		{
			BackgroundLocationRateValue.Text = GetTimeString(BackgroundLocationRateProgressToVal(BackgroundLocationRate.Progress));
		}

		private void LocationHistoryButton_Click(object sender, EventArgs e)
		{
			bool changed = false;
			if (SmallDisplaySize.Checked && Settings.DisplaySize == 1 || NormalDisplaySize.Checked && Settings.DisplaySize == 0)
			{
				changed = true;
			}

			Intent i = new Intent(this, typeof(LocationActivity));
			if (!changed)
			{
				i.SetFlags(ActivityFlags.ReorderToFront);
			}
			StartActivity(i);
		}

		private void SettingsFormCaption_Click(object sender, EventArgs e)
		{
			if (MessageEdit.Visibility == ViewStates.Gone)
			{
				SetFormVisible();
				MessageEdit.RequestFocus();
				//fullscroll page?
			}
			else
			{
				SetFormHidden();
				imm.HideSoftInputFromWindow(MessageEdit.WindowToken, 0);
				MainLayout.RequestFocus();
			}
		}

		private void MessageEdit_Click(object sender, EventArgs e)
		{
			Timer t = new Timer();
			t.Interval = 200; //needs around 70 ms, but have seen 100 too.
			t.Elapsed += T_Elapsed;
			t.Start();
		}

		private void T_Elapsed(object sender, ElapsedEventArgs e)
		{
			((Timer)sender).Stop();
			this.RunOnUiThread(() => { ((ScrollView)MainLayout).FullScroll(FocusSearchDirection.Down); });
		}

		private async void MessageSend_Click(object sender, EventArgs e)
		{
			imm.HideSoftInputFromWindow(MessageEdit.WindowToken, 0);
			if (MessageEdit.Text != "")
			{
				MessageSend.Enabled = false;

				string url = "action=reporterror&ID=" + Session.ID + "&SessionID=" + Session.SessionID;
				string content = "Content=" + c.UrlEncode(MessageEdit.Text + System.Environment.NewLine
					+ "Android version: " + Build.VERSION.SdkInt + " " + Build.VERSION.Sdk + " " + System.Environment.NewLine + Build.VERSION.BaseOs + System.Environment.NewLine + File.ReadAllText(CommonMethods.logFile));
				string responseString = await c.MakeRequest(url, "POST", content);

				if (responseString == "OK")
				{
					MessageEdit.Text = "";
					SetFormHidden();
					MainLayout.RequestFocus();
					c.Snack(Resource.String.SettingsSent, null);
				}
				else
				{
					c.ReportError(responseString);
				}
				MessageSend.Enabled = true;
			}
		}

		private void ProgramLogButton_Click(object sender, EventArgs e)
		{
			c.LogAlert(File.ReadAllText(CommonMethods.logFile));
		}

		private void SetFormVisible()
		{
			MessageEdit.Visibility = ViewStates.Visible;
			MessageSend.Visibility = ViewStates.Visible;
			ProgramLogIncluded.Visibility = ViewStates.Visible;
			ProgramLogButton.Visibility = ViewStates.Visible;
			SetScrollTimer();
		}

		private void SetFormHidden()
		{
			MessageEdit.Visibility = ViewStates.Gone;
			MessageSend.Visibility = ViewStates.Gone;
			ProgramLogIncluded.Visibility = ViewStates.Gone;
			ProgramLogButton.Visibility = ViewStates.Gone;
		}
		public void SetScrollTimer()
		{
			Timer t = new Timer();
			t.Interval = 1;
			t.Elapsed += TS_Elapsed;
			t.Start();
		}

		private void TS_Elapsed(object sender, ElapsedEventArgs e)
		{
			((Timer)sender).Stop();
			this.RunOnUiThread(() => { ((ScrollView)MainLayout).FullScroll(FocusSearchDirection.Down); });
		}

		private string GetTimeString(int seconds)
		{
			if (seconds == 0)
			{
				return "";
			}

			string hour = Resources.GetString(Resource.String.Hour);
			string min = Resources.GetString(Resource.String.ShortMinute);
			string mins = Resources.GetString(Resource.String.ShortMinutes);
			string sec = "s";
			string secs = "s";

			string str = "";

			TimeSpan ts = TimeSpan.FromSeconds(seconds);
			bool showSeconds = true;

			if (ts.Hours > 0)
			{
				str += ts.Hours + " " + hour + " ";
			}

			if (ts.Minutes > 1)
			{
				str += ts.Minutes + " " + mins + " ";
				if (ts.Minutes >= 5)
				{
					showSeconds = false;
				}
			}
			else if (ts.Minutes > 0)
			{
				str += ts.Minutes + " " + min + " ";
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

			return str.Substring(0, str.Length - 1);
		}
	}
}