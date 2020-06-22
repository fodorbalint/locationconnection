using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using Android;
using Android.Animation;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.Constraints;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Views;
using Android.Views.Animations;
using Android.Views.InputMethods;
using Android.Widget;

namespace LocationConnection
{
	[Activity(MainLauncher = false, ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
	public class ProfileEditActivity : ProfilePage
	{
		public ConstraintLayout EditAccountDataSection, EditChangePasswordSection, EditLocationSettingsSection;
		Button Save, Cancel, DeactivateAccount, DeleteAccount;
		public TextView EditAccountData, EditChangePassword, EditLocationSettings, EditMoreOptions;
		Switch Women, Men;
		public EditText EditOldPassword, EditNewPassword, EditConfirmPassword;
		int checkFormMessage;

		protected override void OnCreate(Bundle savedInstanceState)
		{
			try { 
				base.OnCreate(savedInstanceState);

				c.CW("OnCreate savedInstanceState " + (savedInstanceState is null));



				//Test reset
				/*
				ListActivity.initialized = false;
				savedInstanceState = new Bundle();
				savedInstanceState.PutString("Session", c.SerializeSession());

				ListActivity.initialized = false;
				Type type = typeof(Session);
				FieldInfo[] fieldInfo = type.GetFields();
				foreach (FieldInfo field in fieldInfo)
				{
					field.SetValue(null, null);
				}
				*/



				if (!ListActivity.initialized && !(savedInstanceState is null))
				{
					c.LogActivity("ProfileEditActivity not initialized, restoring");

					ListActivity.initialized = true;
					IntentData.profileViewPageType = Constants.ProfileViewType_Self;
					GetScreenMetrics(true);
					c.LoadSettings(false);
					c.DeSerializeSession(savedInstanceState.GetString("Session"));
					imageEditorFrameBorderWidth = savedInstanceState.GetInt("imageEditorFrameBorderWidth");
				}
				else if (!ListActivity.initialized && savedInstanceState is null)
				{
					c.LogActivity(LocalClassName.Split(".")[1] + " Not initialized");

					c.ReportErrorSilent("Initialization error");

					Intent i = new Intent(this, typeof(ListActivity));
					i.SetFlags(ActivityFlags.ReorderToFront);
					StartActivity(i);
				}

				if (Settings.DisplaySize == 1)
				{
					SetContentView(Resource.Layout.activity_profileedit_normal);
				}
				else
				{
					SetContentView(Resource.Layout.activity_profileedit_small);
				}

				//ProfilePage start

				MainScroll = FindViewById<TouchScrollView>(Resource.Id.MainScroll);
				MainLayout = FindViewById<ConstraintLayout>(Resource.Id.MainLayout);
				Email = FindViewById<EditText>(Resource.Id.Email);
				Username = FindViewById<EditText>(Resource.Id.Username);
				CheckUsername = FindViewById<Button>(Resource.Id.CheckUsername);
				Name = FindViewById<EditText>(Resource.Id.Name);
				ImagesUploaded = FindViewById<ImageFrameLayout>(Resource.Id.ImagesUploaded);
				Images = FindViewById<Button>(Resource.Id.Images);
				ImagesProgressText = FindViewById<TextView>(Resource.Id.ImagesProgressText);
				LoaderCircle = FindViewById<ImageView>(Resource.Id.LoaderCircle);
				ImagesProgress = FindViewById<ProgressBar>(Resource.Id.ImagesProgress);			
				Description = FindViewById<EditText>(Resource.Id.Description);

				UseLocationSwitch = FindViewById<Switch>(Resource.Id.UseLocationSwitch);
				LocationShareAll = FindViewById<Switch>(Resource.Id.LocationShareAll);
				LocationShareLike = FindViewById<Switch>(Resource.Id.LocationShareLike);
				LocationShareMatch = FindViewById<Switch>(Resource.Id.LocationShareMatch);
				LocationShareFriend = FindViewById<Switch>(Resource.Id.LocationShareFriend);
				LocationShareNone = FindViewById<Switch>(Resource.Id.LocationShareNone);

				DistanceShareAll = FindViewById<Switch>(Resource.Id.DistanceShareAll);
				DistanceShareLike = FindViewById<Switch>(Resource.Id.DistanceShareLike);
				DistanceShareMatch = FindViewById<Switch>(Resource.Id.DistanceShareMatch);
				DistanceShareFriend = FindViewById<Switch>(Resource.Id.DistanceShareFriend);
				DistanceShareNone = FindViewById<Switch>(Resource.Id.DistanceShareNone);

				ImageEditorFrame = FindViewById<View>(Resource.Id.ImageEditorFrame);
				ImageEditorFrameBorder = FindViewById<View>(Resource.Id.ImageEditorFrameBorder);
				ImageEditor = FindViewById<ScaleImageView>(Resource.Id.ImageEditor);
				ImageEditorControls = FindViewById<LinearLayout>(Resource.Id.ImageEditorControls);
				ImageEditorCancel = FindViewById<ImageButton>(Resource.Id.ImageEditorCancel);
				ImageEditorOK = FindViewById<ImageButton>(Resource.Id.ImageEditorOK);
				TopSeparator = FindViewById<View>(Resource.Id.TopSeparator);

				//ProfilePage end

				Women = FindViewById<Switch>(Resource.Id.Women);
				Men = FindViewById<Switch>(Resource.Id.Men);

				EditAccountData = FindViewById<TextView>(Resource.Id.EditAccountData);
				EditChangePassword = FindViewById<TextView>(Resource.Id.EditChangePassword);
				EditLocationSettings = FindViewById<TextView>(Resource.Id.EditLocationSettings);
				EditMoreOptions = FindViewById<TextView>(Resource.Id.EditMoreOptions);
				EditAccountDataSection = FindViewById<ConstraintLayout>(Resource.Id.EditAccountDataSection);
				EditChangePasswordSection = FindViewById<ConstraintLayout>(Resource.Id.EditChangePasswordSection);
				EditLocationSettingsSection = FindViewById<ConstraintLayout>(Resource.Id.EditLocationSettingsSection);

				EditOldPassword = FindViewById<EditText>(Resource.Id.EditOldPassword);
				EditNewPassword = FindViewById<EditText>(Resource.Id.EditNewPassword);
				EditConfirmPassword = FindViewById<EditText>(Resource.Id.EditConfirmPassword);

				Save = FindViewById<Button>(Resource.Id.Save);
				Cancel = FindViewById<Button>(Resource.Id.Cancel);

				DeactivateAccount = FindViewById<Button>(Resource.Id.DeactivateAccount);
				DeleteAccount = FindViewById<Button>(Resource.Id.DeleteAccount);

				ImagesUploaded.numColumns = 3; //it does not get passed in the layout file
				ImagesUploaded.tileSpacing = 2;
				ImagesUploaded.SetTileSize();
				
				c.view = MainLayout;
				rc = new RegisterCommonMethods(this);
				res = Resources;
				imm = (InputMethodManager)GetSystemService(InputMethodService);

				Women.Click += Women_Click;
				Men.Click += Men_Click;

				EditAccountData.Click += EditAccountData_Click;
				EditChangePassword.Click += EditChangePassword_Click;
				EditLocationSettings.Click += EditLocationSettings_Click;
				EditMoreOptions.Click += EditMoreOptions_Click;

				Save.Click += Save_Click;
				Cancel.Click += Cancel_Click;
				DeactivateAccount.Click += DeactivateAccount_Click;
				DeleteAccount.Click += DeleteAccount_Click;
			}
			catch (Exception ex)
			{
				c.ReportErrorSilent(ex.Message + System.Environment.NewLine + ex.StackTrace);
			}
		}

		protected override void OnResume() //will be called after opening the file selector too
		{
			try {
				base.OnResume();

				if (!ListActivity.initialized) { return; }

				EditAccountDataSection.Visibility = ViewStates.Gone;
				EditChangePasswordSection.Visibility = ViewStates.Gone;
				EditLocationSettingsSection.Visibility = ViewStates.Gone;
				DeactivateAccount.Visibility = ViewStates.Gone;
				DeleteAccount.Visibility = ViewStates.Gone;
				ImagesProgress.Progress = 0;
				MainScroll.ScrollY = 0;

				MainLayout.RequestFocus();
				Images.Enabled = true; 
				SetSexChoice();
				Email.Text = Session.Email;
				Username.Text = Session.Username;
				Name.Text = Session.Name;
				uploadedImages = new List<string>(Session.Pictures);

				ImagesUploaded.RemoveAllViews();
				ImagesUploaded.RefitImagesContainer();
				ImagesUploaded.drawOrder = new List<int>();

				ImageCache.imagesInProgress = new List<string>();
				ImageCache.imageViewToLoadLater = new Dictionary<ImageView, string>();

				int i = 0;
				foreach (string image in uploadedImages)
				{
					ImagesUploaded.AddPicture(image, i);
					i++;
				}

				if (!imagesUploading)
				{
					if (uploadedImages.Count > 1)
					{
						ImagesProgressText.Text = res.GetString(Resource.String.ImagesRearrange);
					}
					else
					{
						ImagesProgressText.Text = "";
					}
				}				

				Description.Text = Session.Description;

				UseLocationSwitch.Checked = (bool)Session.UseLocation;
				rc.EnableLocationSwitches((bool)Session.UseLocation);
				rc.SetLocationShareLevel((byte)Session.LocationShare);
				rc.SetDistanceShareLevel((byte)Session.DistanceShare);

				if ((bool)Session.ActiveAccount)
				{
					DeactivateAccount.Text = res.GetString(Resource.String.DeactivateAccount);
				}
				else
				{
					DeactivateAccount.Text = res.GetString(Resource.String.ActivateAccount);
				}

				if (selectedFile != null)
				{
					OnResumeEnd();
				}

				c.LogActivity("OnResume end");
			}
			catch (Exception ex)
			{
				c.ReportErrorSilent(ex.Message + System.Environment.NewLine + ex.StackTrace);
			}
		}

		protected override void OnPause()
		{
			base.OnPause();

			c.CW("ProfileEdit OnPause");
			imm.HideSoftInputFromWindow(Description.WindowToken, 0);
		}

		protected override void OnSaveInstanceState(Bundle outState) //called after OnPause, and if we deleted the account, called after MainActivity.OnResume is finished.
		{
			base.OnSaveInstanceState(outState);

			if (!IntentData.logout && c.IsLoggedIn()) //for when the user deletes the account.
			{
				outState.PutString("Session", c.SerializeSession());
			}
			outState.PutInt("imageEditorFrameBorderWidth", imageEditorFrameBorderWidth);
		}

		private void SetSexChoice()
		{
			switch (Session.SexChoice)
			{
				case 0:
					Women.Checked = true;
					Men.Checked = false;
					break;
				case 1:
					Women.Checked = false;
					Men.Checked = true;
					break;
				case 2:
					Women.Checked = true;
					Men.Checked = true;
					break;
			}
		}

		private int GetSexChoice()
		{
			if (Women.Checked && !Men.Checked)
			{
				return 0;
			}
			else if (!Women.Checked && Men.Checked)
			{
				return 1;
			}
			return 2;
		}

		public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
		{
			if (requestCode == 1) //Storage
			{
				if ((grantResults.Length == 1) && (grantResults[0] == Permission.Granted))
				{
					rc.SelectImage();
				}
				else
				{
					c.Snack(Resource.String.StorageNotGranted);
				}
			}
			else if (requestCode == 2) //Location
			{
				if ((grantResults.Length == 1) && (grantResults[0] == Permission.Granted))
				{
					Timer t = new Timer();
					t.Interval = 1;
					t.Elapsed += T_Elapsed;
					t.Start();
				}
				else
				{
					c.Snack(Resource.String.LocationNotGranted);
				}
			}
			else
			{
				base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
			}
		}

		private void T_Elapsed(object sender, ElapsedEventArgs e)
		{
			((Timer)sender).Stop();			
			this.RunOnUiThread(() => {
				UseLocationSwitch.Checked = true;
				rc.EnableLocationSwitches(true);
			});	
		}

		private void Women_Click(object sender, EventArgs e)
		{
			if (!Women.Checked) //at least one sex has to be selected
			{
				Men.Checked = true;
			}
		}

		private void Men_Click(object sender, EventArgs e)
		{
			if (!Men.Checked)
			{
				Women.Checked = true;
			}
		}

		private void EditAccountData_Click(object sender, EventArgs e)
		{
			if (EditAccountDataSection.Visibility == ViewStates.Gone)
			{
				EditAccountDataSection.Visibility = ViewStates.Visible;
				EditChangePasswordSection.Visibility = ViewStates.Gone;
				EditLocationSettingsSection.Visibility = ViewStates.Gone;
				DeactivateAccount.Visibility = ViewStates.Gone;
				DeleteAccount.Visibility = ViewStates.Gone;
			}
			else
			{
				EditAccountDataSection.Visibility = ViewStates.Gone;
			}
		}

		private void EditChangePassword_Click(object sender, EventArgs e)
		{
			if (EditChangePasswordSection.Visibility == ViewStates.Gone)
			{
				EditAccountDataSection.Visibility = ViewStates.Gone;
				EditChangePasswordSection.Visibility = ViewStates.Visible;
				EditLocationSettingsSection.Visibility = ViewStates.Gone;
				DeactivateAccount.Visibility = ViewStates.Gone;
				DeleteAccount.Visibility = ViewStates.Gone;
			}
			else
			{
				EditChangePasswordSection.Visibility = ViewStates.Gone;
			}
		}

		private void EditLocationSettings_Click(object sender, EventArgs e)
		{
			if (EditLocationSettingsSection.Visibility == ViewStates.Gone)
			{
				EditAccountDataSection.Visibility = ViewStates.Gone;
				EditChangePasswordSection.Visibility = ViewStates.Gone;
				EditLocationSettingsSection.Visibility = ViewStates.Visible;
				DeactivateAccount.Visibility = ViewStates.Gone;
				DeleteAccount.Visibility = ViewStates.Gone;
			}
			else
			{
				EditLocationSettingsSection.Visibility = ViewStates.Gone;
			}
		}

		private void EditMoreOptions_Click(object sender, EventArgs e)
		{
			if (DeactivateAccount.Visibility == ViewStates.Gone)
			{
				EditAccountDataSection.Visibility = ViewStates.Gone;
				EditChangePasswordSection.Visibility = ViewStates.Gone;
				EditLocationSettingsSection.Visibility = ViewStates.Gone;
				DeactivateAccount.Visibility = ViewStates.Visible;
				DeleteAccount.Visibility = ViewStates.Visible;
				SetScrollTimer();
			}
			else
			{
				DeactivateAccount.Visibility = ViewStates.Gone;
				DeleteAccount.Visibility = ViewStates.Gone;
			}
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
			this.RunOnUiThread(() => {
				MainScroll.FullScroll(FocusSearchDirection.Down);
				MainLayout.RequestFocus(); //Description text gets focus without this
			});
		}

		private async void Save_Click(object sender, EventArgs e) //Update intro, sex and open section data. Reset closed section data
		{
			if (CheckFields())
			{
				Save.Enabled = false;

				//not visible form fields do not get saved, but there is no need to reload the form, since we are exiting the activity on successful save.
				string requestStringBase = "action=profileedit&ID=" + Session.ID + "&SessionID=" + Session.SessionID;
				string requestStringAdd = "";
				if (Description.Text != Session.Description)
				{
					requestStringAdd += "&Description=" + c.UrlEncode(Description.Text);
				}
				if (GetSexChoice() != Session.SexChoice)
				{
					requestStringAdd += "&SexChoice=" + GetSexChoice();
				}
				if (EditAccountDataSection.Visibility == ViewStates.Visible)
				{
					if (Email.Text.Trim() != Session.Email)
					{
						requestStringAdd += "&Email=" + c.UrlEncode(Email.Text.Trim());
					}
					if (Username.Text.Trim() != Session.Username)
					{
						requestStringAdd += "&Username=" + c.UrlEncode(Username.Text.Trim());
					}
					if (Name.Text.Trim() != Session.Name)
					{
						requestStringAdd += "&Name=" + c.UrlEncode(Name.Text.Trim());
					}
				}
				if (EditChangePasswordSection.Visibility == ViewStates.Visible)
				{
					requestStringAdd += "&OldPassword=" + c.UrlEncode(EditOldPassword.Text.Trim()) + "&Password=" + c.UrlEncode(EditNewPassword.Text.Trim());
				}
				if (EditLocationSettingsSection.Visibility == ViewStates.Visible)
				{
					if (UseLocationSwitch.Checked != Session.UseLocation)
					{
						requestStringAdd += "&UseLocation=" + UseLocationSwitch.Checked;
					}
					int locationShare = rc.GetLocationShareLevel();
					int distanceShare = rc.GetDistanceShareLevel();
					if (locationShare != Session.LocationShare)
					{
						requestStringAdd += "&LocationShare=" + locationShare;
					}
					if (distanceShare != Session.DistanceShare)
					{
						requestStringAdd += "&DistanceShare=" + distanceShare;
					}
				}

				if (requestStringAdd != "") //if the form was changed
				{
					string responseString = await c.MakeRequest(requestStringBase + requestStringAdd);
					if (responseString.Substring(0, 2) == "OK")
					{
						if (responseString.Length > 2) //a change happened
						{
							bool locationEnabled = false;
							bool locationDisabled = false;

							if (!(bool)Session.UseLocation && UseLocationSwitch.Checked) //location got turned on
							{
								locationEnabled = true;
							}
							else if ((bool)Session.UseLocation && !UseLocationSwitch.Checked)
							{
								locationDisabled = true;
							}

							if (GetSexChoice() != Session.SexChoice)
							{
								ListActivity.listProfiles.Clear();
								Session.LastDataRefresh = null;
							}
							c.LoadCurrentUser(responseString);

							if (locationEnabled)
							{
								StartLocationUpdates();
							}
							else if (locationDisabled)
							{
								StopLocationUpdates();
								if (!string.IsNullOrEmpty(locationUpdatesTo))
								{
									EndLocationShare();
									locationUpdatesTo = null;
								}
								Session.Latitude = null;
								Session.Longitude = null;
								Session.LocationTime = null;
							}
						}
						Session.SnackMessage = res.GetString(Resource.String.SettingsUpdated);
						
						Save.Enabled = true;
						OnBackPressed();
					}
					else if (responseString.Substring(0, 6) == "ERROR_")
					{
						c.Snack(Resources.GetIdentifier(responseString.Substring(6), "string", PackageName));
					}
					else
					{
						c.ReportError(responseString);
					}
				}
				else
				{
					Save.Enabled = true;
					OnBackPressed();
				}
				Save.Enabled = true;
			}
			else
			{
				c.Snack(checkFormMessage);
			}
		}

		private bool CheckFields()
		{
			if (Description.Text.Trim() == "")
			{
				checkFormMessage = Resource.String.DescriptionEmpty;
				Description.RequestFocus();
				return false;
			}
			if (Description.Text.Substring(Description.Text.Length-1) == "\\")
			{
				checkFormMessage = Resource.String.DescriptionBackslash;
				return false;
			}

			if (EditAccountDataSection.Visibility == ViewStates.Visible) {
				if (Email.Text.Trim() == "")
				{
					checkFormMessage = Resource.String.EmailEmpty;
					Email.RequestFocus();
					return false;
				}
				//If the extension is long, the regex will freeze the app.
				int lastDotPos = Email.Text.LastIndexOf(".");
				if (lastDotPos < Email.Text.Length - 5)
				{
					checkFormMessage = Resource.String.EmailWrong;
					return false;
				}
				Regex regex = new Regex(Constants.EmailFormat); //when the email extension is long, it will take ages for the regex to finish
				if (!regex.IsMatch(Email.Text))
				{
					checkFormMessage = Resource.String.EmailWrong;
					Email.RequestFocus();
					return false;
				}
				if (Username.Text.Trim() == "")
				{
					checkFormMessage = Resource.String.UsernameEmpty;
					Username.RequestFocus();
					return false;
				}
				if (Username.Text.Trim().Substring(Username.Text.Trim().Length - 1) == "\\")
				{
					checkFormMessage = Resource.String.UsernameBackslash;
					return false;
				}
				if (Name.Text.Trim() == "")
				{
					checkFormMessage = Resource.String.NameEmpty;
					Name.RequestFocus();
					return false;
				}
				if (Name.Text.Trim().Substring(Name.Text.Trim().Length - 1) == "\\")
				{
					checkFormMessage = Resource.String.NameBackslash;
					return false;
				}
			}

			if (EditChangePasswordSection.Visibility == ViewStates.Visible)
			{
				if (EditOldPassword.Text.Trim().Length < 6)
				{
					checkFormMessage = Resource.String.PasswordShort;
					EditOldPassword.RequestFocus();
					return false;
				}
				if (EditNewPassword.Text.Trim().Length < 6)
				{
					checkFormMessage = Resource.String.PasswordShort;
					EditNewPassword.RequestFocus();
					return false;
				}
				if (EditOldPassword.Text.Trim() == EditNewPassword.Text.Trim())
				{
					checkFormMessage = Resource.String.PasswordNotChanged;
					EditNewPassword.RequestFocus();
					return false;
				}
				if (EditNewPassword.Text.Trim() != EditConfirmPassword.Text.Trim())
				{
					checkFormMessage = Resource.String.ConfirmPasswordNoMatch;
					EditConfirmPassword.RequestFocus();
					return false;
				}
			}
			return true;
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			OnBackPressed();
		}

		private async void DeactivateAccount_Click(object sender, EventArgs e)
		{
			if ((bool)Session.ActiveAccount)
			{
				string dialogResponse = await c.DisplayCustomDialog(res.GetString(Resource.String.ConfirmAction), res.GetString(Resource.String.DialogDeactivate),
					res.GetString(Resource.String.DialogOK), res.GetString(Resource.String.DialogCancel));
				if (dialogResponse == res.GetString(Resource.String.DialogOK))
				{
					DeactivateAccount.Enabled = false;
					string url = "action=deactivateaccount&ID=" + Session.ID + "&SessionID=" + Session.SessionID;
					if (!string.IsNullOrEmpty(locationUpdatesTo))
					{
						url += "&LocationUpdates=" + locationUpdatesTo;
						locationUpdatesTo = null;
					}
					string responseString = await c.MakeRequest(url);
					if (responseString == "OK")
					{
						Session.ActiveAccount = false;
						c.Snack(Resource.String.Deactivated);
						DeactivateAccount.Text = res.GetString(Resource.String.ActivateAccount);
					}
					else
					{
						c.ReportError(responseString);
					}
					DeactivateAccount.Enabled = true;
				}
			}
			else
			{
				DeactivateAccount.Enabled = false;
				string responseString = await c.MakeRequest("action=activateaccount&ID=" + Session.ID + "&SessionID=" + Session.SessionID);
				if (responseString == "OK")
				{
					Session.ActiveAccount = true;
					c.Snack(Resource.String.Activated);
					DeactivateAccount.Text = res.GetString(Resource.String.DeactivateAccount);
				}
				else
				{
					c.ReportError(responseString);
				}
				DeactivateAccount.Enabled = true;
			}
		}

		private async void DeleteAccount_Click(object sender, EventArgs e)
		{
			string dialogResponse = await c.DisplayCustomDialog(res.GetString(Resource.String.ConfirmAction), res.GetString(Resource.String.DialogDelete),
				res.GetString(Resource.String.DialogOK), res.GetString(Resource.String.DialogCancel));
			if (dialogResponse == res.GetString(Resource.String.DialogOK))
			{
				DeleteAccount.Enabled = false;
				string url = "action=deleteaccount&ID=" + Session.ID + "&SessionID=" + Session.SessionID;
				if (!string.IsNullOrEmpty(locationUpdatesTo))
				{
					url += "&LocationUpdates=" + locationUpdatesTo;
					locationUpdatesTo = null;
				}
				string responseString = await c.MakeRequest(url);
				if (responseString == "OK")
				{
					Intent i = new Intent(this, typeof(MainActivity));
					i.SetFlags(ActivityFlags.ReorderToFront);
					IntentData.logout = true;
					StartActivity(i);
				}
				else
				{
					c.ReportError(responseString);
				}
				DeleteAccount.Enabled = true;
			}
		}

		public override void SaveRegData() { }
	}
}