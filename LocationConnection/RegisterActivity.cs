/* remove non-letter char
 * acters from form, @ from username
 * image filename must not contain ; and |
 * uploading the same image twice will result in missing image when we delete one.
 * loading circle while images are loading
 * sending email for account confirmation
 * cron to remove temp picture directories
 * cron to calculate response rate, update on answering a message
*/

using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Database;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Support.Constraints;
using Android.Support.Design.Animation;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Support.V7.App;
using Android.Text;
using Android.Views;
using Android.Views.Animations;
using Android.Views.InputMethods;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using Xamarin.Essentials;

namespace LocationConnection
{
	[Activity(MainLauncher = false, ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
	public class RegisterActivity : ProfilePage
    {
		Spinner Sex;
        public EditText Password, ConfirmPassword;
		Button Register, Reset, Cancel;
		EditText EulaText;
		InputMethodManager imm;

		RegisterCommonMethods<RegisterActivity> rc;
		public BaseAdapter adapter;

		public static string regsessionid; //for use in UploadedListAdapter

        int checkFormMessage;
		private bool registerCompleted;
		private int spinnerItem;
		private int spinnerItemDropdown;

		protected override void OnCreate(Bundle savedInstanceState)
        {
			try
			{
				base.OnCreate(savedInstanceState);
				if (!ListActivity.initialized) { return; }

				if (Settings.DisplaySize == 1)
				{
					SetContentView(Resource.Layout.activity_register_normal);
					spinnerItem = Resource.Layout.spinner_item_normal;
					spinnerItemDropdown = Resource.Layout.spinner_item_dropdown_normal;
				}
				else
				{
					SetContentView(Resource.Layout.activity_register_small);
					spinnerItem = Resource.Layout.spinner_item_small;
					spinnerItemDropdown = Resource.Layout.spinner_item_dropdown_small;
				}

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

				EulaText = FindViewById<EditText>(Resource.Id.EulaText);

				//Interface end

				Sex = FindViewById<Spinner>(Resource.Id.Sex);

				Password = FindViewById<EditText>(Resource.Id.Password);
				ConfirmPassword = FindViewById<EditText>(Resource.Id.ConfirmPassword);

				Register = FindViewById<Button>(Resource.Id.Register);
				Reset = FindViewById<Button>(Resource.Id.Reset);
				Cancel = FindViewById<Button>(Resource.Id.Cancel);

				ImagesUploaded.numColumns = 5; //it does not get passed in the layout file
				ImagesUploaded.tileSpacing = 2;
				ImagesProgress.Progress = 0;
				c.view = MainLayout;
				rc = new RegisterCommonMethods<RegisterActivity>(MainLayout, this);
				res = Resources;
				imm = (InputMethodManager)GetSystemService(Context.InputMethodService);

				var adapter = ArrayAdapter.CreateFromResource(this, Resource.Array.SexEntries, spinnerItem);
				adapter.SetDropDownViewResource(spinnerItemDropdown);
				Sex.Adapter = adapter;

				if (!File.Exists(regSessionFile))
				{
					regsessionid = "";
				}
				else
				{
					regsessionid = File.ReadAllText(regSessionFile);
				}

				CheckUsername.Click += rc.CheckUsername_Click;
				Images.Click += rc.Images_Click;
				Description.Touch += Description_Touch;

				UseLocationSwitch.Click += rc.UseLocationSwitch_Click;
				LocationShareAll.Click += rc.LocationShareAll_Click;
				LocationShareLike.Click += rc.LocationShareLike_Click;
				LocationShareMatch.Click += rc.LocationShareMatch_Click;
				LocationShareFriend.Click += rc.LocationShareFriend_Click;
				LocationShareNone.Click += rc.LocationShareNone_Click;

				DistanceShareAll.Click += rc.DistanceShareAll_Click;
				DistanceShareLike.Click += rc.DistanceShareLike_Click;
				DistanceShareMatch.Click += rc.DistanceShareMatch_Click;
				DistanceShareFriend.Click += rc.DistanceShareFriend_Click;
				DistanceShareNone.Click += rc.DistanceShareNone_Click;

				EulaText.Touch += EulaText_Touch;

				Register.Click += Register_Click;
				Reset.Click += Reset_Click;
				Cancel.Click += Cancel_Click;
			}
			catch (Exception ex)
			{
				c.ReportErrorSilent(ex.Message + System.Environment.NewLine + ex.StackTrace);
			}
		}

		protected async override void OnResume() //will be called after opening the file selector and permission results
		{
			try
			{
				base.OnResume();

				if (!ListActivity.initialized) { return; }

				GetScreenMetrics(false);
				ImagesUploaded.SetTileSize();

				MainLayout.RequestFocus();

				if (!(ListActivity.listProfiles is null))
				{
					ListActivity.listProfiles.Clear();
					ListActivity.totalResultCount = null;
				}
				Session.LastDataRefresh = null;
				Session.LocationTime = null;

				registerCompleted = false;

				if (File.Exists(regSaveFile))
				{
					string content = File.ReadAllText(regSaveFile);
					string[] arr = content.Split(";");
					Sex.SetSelection(int.Parse(arr[0]));
					Email.Text = arr[1];
					Password.Text = arr[2];
					ConfirmPassword.Text = arr[3];
					Username.Text = arr[4];
					Name.Text = arr[5];
					if (arr[6] != "") //it would give one element
					{
						string[] images = arr[6].Split("|");
						uploadedImages = new List<string>(images);
					}
					else
					{
						uploadedImages = new List<string>();
					}

					if (uploadedImages.Count > 1)
					{
						ImagesProgressText.Text = res.GetString(Resource.String.ImagesRearrange);
					}
					else
					{
						ImagesProgressText.Text = "";
					}

					ImagesUploaded.RemoveAllViews();
					ImagesUploaded.RefitImagesContainer();
					ImagesUploaded.drawOrder = new List<int>();

					ImageCache.imagesInProgress = new List<string>();
					int i = 0;
					foreach (string image in uploadedImages)
					{
						ImagesUploaded.AddPicture(image, i);
						i++;
					}

					if (imagesUploading)
					{
						StartAnim();
					}

					Description.Text = arr[7];

					UseLocationSwitch.Checked = bool.Parse(arr[8]);
					rc.EnableLocationSwitches(UseLocationSwitch.Checked);
					rc.SetLocationShareLevel(byte.Parse(arr[9]));
					rc.SetDistanceShareLevel(byte.Parse(arr[10]));
				}
				else //in case we are stepping back from a successful registration
				{
					ResetForm();
				}

				string responseString = await c.MakeRequest("action=eula"); //deleting images from server
				if (responseString.Substring(0, 2) == "OK")
				{
					if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
					{
						EulaText.TextFormatted = Html.FromHtml(responseString.Substring(3), FromHtmlOptions.ModeCompact);
					}
					else
					{
						EulaText.TextFormatted = Html.FromHtml(responseString.Substring(3)); //.FromHtml("<h2>Title</h2><br><p>Description here</p>");
					}
				}
				else
				{
					c.ReportError(responseString);
				}
			}
			catch (Exception ex)
			{
				c.ReportErrorSilent(ex.Message + System.Environment.NewLine + ex.StackTrace);
			}
		}

		protected override void OnPause()
		{
			base.OnPause();
			if (!ListActivity.initialized) { return; }

			if (!registerCompleted)
			{
				SaveRegData();
			}
		}

		public override void SaveRegData()
		{
			File.WriteAllText(regSaveFile, Sex.SelectedItemId + ";" + Email.Text.Trim() + ";" + Password.Text.Trim() + ";" + ConfirmPassword.Text.Trim()
					+ ";" + Username.Text.Trim() + ";" + Name.Text.Trim()
					+ ";" + string.Join("|", uploadedImages) + ";" + Description.Text.Trim()
					+ ";" + UseLocationSwitch.Checked + ";" + rc.GetLocationShareLevel() + ";" + rc.GetDistanceShareLevel());
		}

		private void ResetForm()
		{
			Sex.SetSelection(0);
			Email.Text = "";
			Password.Text = "";
			ConfirmPassword.Text = "";
			Username.Text = "";
			Name.Text = "";
			Description.Text = "";
			uploadedImages = new List<string>();
			ImagesUploaded.RemoveAllViews();
			ImagesUploaded.RefitImagesContainer();
			Description.Text = "";

			ImagesProgressText.Text = "";
			ImagesProgress.Progress = 0;

			UseLocationSwitch.Checked = false;

			LocationShareAll.Checked = false;
			LocationShareLike.Checked = false;
			LocationShareMatch.Checked = false;
			LocationShareFriend.Checked = false;
			LocationShareNone.Checked = true;

			DistanceShareAll.Checked = false;
			DistanceShareLike.Checked = false;
			DistanceShareMatch.Checked = false;
			DistanceShareFriend.Checked = false;
			DistanceShareNone.Checked = true;

			rc.EnableLocationSwitches(false);
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

		protected async override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
		{
			base.OnActivityResult(requestCode, resultCode, data);

			if (requestCode == 1 && resultCode == Result.Ok)
			{
				if (imagesUploading) //can happen if we click on the upload button twice fast enough
				{
					return;
				}
				Android.Net.Uri selectedFile = data.Data;
				string selectedFileStr;

				/*
				 We can get info via provider - using Images
				 selectedFile:
					content://com.android.providers.media.documents/document/image%3A53287;
				 selectedFile.Path:
					/document/image:53287;False;False

				 We can get info via provider - using Gallery
				 selectedFile:
					content://media/external/images/media/33424
				 selectedFile.Path:
					/external/images/media/33424;False;False
				 
				 File does not exist - using SD card / fix #2
				 selectedFile:
					content://com.android.externalstorage.documents/document/E910-4E32%3APictures%2F....jpg;
				 selectedFile.Path:
					/document/E910-4E32:Pictures/....jpg;False;False
				 
				 File exists - using Total Commander
				 selectedFile:
					content://com.ghisler.android.TotalCommander.files/storage/emulated/0/Documents/....jpg;
				 selectedFile.Path:
					/storage/emulated/0/Documents/....jpg;False;True
				 
				 File does not exist - using Emulator Downloads folder / fix #1
				 selectedFile:
					content://com.android.providers.downloads.documents/document/raw%3A%2Fstorage%2Femulated%2F0%2FDownload%2F....jpg
				 selectedFile.Path:
					/document.raw:/storage/emulated/0/Download/....jpg
				 */

				string path = selectedFile.Path;
				if (path.IndexOf(":") != -1) //fix #1
				{
					int colonPos = path.IndexOf(":");
					path = path.Substring(colonPos + 1);
				}
				if (!File.Exists(path))
				{
					string str = Regex.Replace(selectedFile.Path, @"/document/([A-Z\d]{4}-[A-Z\d]{4}):", "/storage/$1/"); // fix #2
					if (!File.Exists(str))
					{
						try
						{
							selectedFileStr = c.GetPathToImage(selectedFile);
						}
						catch
						{
							c.LogError("UploadImagePathNotFound: selectedFile: " + selectedFile + ", selectedFile.Path: " + selectedFile.Path);
							c.ReportError(res.GetString(Resource.String.UploadImagePathNotFound));
							return;
						}
					}
					else
					{
						selectedFileStr = str;
					}
				}
				else
				{
					selectedFileStr = path;
				}
				string imageName = selectedFileStr.Substring(selectedFileStr.LastIndexOf("/") + 1);
				if (uploadedImages.IndexOf(imageName) != -1)
				{
					c.Snack(Resource.String.ImageExists);
					return;
				}
				imagesUploading = true;

				StartAnim();
				
				await rc.UploadFile(selectedFileStr, regsessionid);
			}
		}

		private void StartAnim()
		{
			Animation anim = Android.Views.Animations.AnimationUtils.LoadAnimation(this, Resource.Animation.rotate);
			LoaderCircle.Visibility = ViewStates.Visible;
			LoaderCircle.StartAnimation(anim);
			ImagesProgressText.Text = res.GetString(Resource.String.ImagesProgressText);
		}

		private void Description_Touch(object sender, View.TouchEventArgs e)
		{
			if (Description.HasFocus)
			{
				MainScroll.RequestDisallowInterceptTouchEvent(true);
			}
			e.Handled = false;
			base.OnTouchEvent(e.Event);
		}

		private void EulaText_Touch(object sender, View.TouchEventArgs e)
		{
			MainScroll.RequestDisallowInterceptTouchEvent(true);
			e.Handled = false;
			base.OnTouchEvent(e.Event);
		}

		private async void Register_Click(object sender, System.EventArgs e)
        {            
            if (CheckFields())
            {
				imm.HideSoftInputFromWindow(Email.WindowToken, 0);
				MainLayout.RequestFocus();

				Register.Enabled = false;

				int locationShare = 0;
				int distanceShare = 0;

				if (UseLocationSwitch.Checked)
				{
					locationShare = rc.GetLocationShareLevel();
					distanceShare = rc.GetDistanceShareLevel();
				}

				string url = "action=register&Sex=" + (Sex.SelectedItemId - 1) + "&Email=" + c.UrlEncode(Email.Text.Trim()) + "&Password=" + c.UrlEncode(Password.Text.Trim())
					+ "&Username=" + c.UrlEncode(Username.Text.Trim()) + "&Name=" + c.UrlEncode(Name.Text.Trim())
					+ "&Pictures=" + c.UrlEncode(string.Join("|", uploadedImages)) + "&Description=" + c.UrlEncode(Description.Text.Trim()) + "&UseLocation=" + UseLocationSwitch.Checked
					+ "&LocationShare=" + locationShare + "&DistanceShare=" + distanceShare + "&regsessionid=" + regsessionid;

				if (File.Exists(firebaseTokenFile)) //sends the token whether it was sent from this device or not
				{
					url += "&token=" + File.ReadAllText(firebaseTokenFile);
				}
				string responseString = await c.MakeRequest(url);
				if (responseString.Substring(0, 2) == "OK")
				{
					if (File.Exists(regSessionFile))
					{
						File.Delete(regSessionFile);
					}
					regsessionid = "";
					if (File.Exists(regSaveFile))
					{
						File.Delete(regSaveFile);
					}
					registerCompleted = true; //to prevent OnPause from saving form data.
					
					if (File.Exists(firebaseTokenFile))
					{
						File.WriteAllText(tokenUptoDateFile, "True");
					}

					c.LoadCurrentUser(responseString);

					Register.Enabled = true;

					Intent i = new Intent(this, typeof(ListActivity));
					i.SetFlags(ActivityFlags.ReorderToFront);
					StartActivity(i);					
				}
				else if (responseString.Substring(0, 6) == "ERROR_")
				{
					c.Snack(Resources.GetIdentifier(responseString.Substring(6), "string", PackageName));
				}
				else
                {
					c.ReportError(responseString);
				}
				Register.Enabled = true;
			}
            else
            {
                c.Snack(checkFormMessage);
            }
        }

        private bool CheckFields()
        {
			if (Sex.SelectedItemId == 0)
			{
				checkFormMessage = Resource.String.SexEmpty;
				Sex.RequestFocus();
				return false;
			}
			if (Email.Text.Trim() == "")
            {
                checkFormMessage = Resource.String.EmailEmpty;
                Email.RequestFocus();
                return false;
            }
			int lastDotPos = Email.Text.LastIndexOf(".");
			if (lastDotPos < Email.Text.Length - 5)
			{
				checkFormMessage = Resource.String.EmailWrong;
				return false;
			}
			//If the extension is long, the regex will freeze the app.
			Regex regex = new Regex(Constants.EmailFormat);
            if (!regex.IsMatch(Email.Text))
            {
                checkFormMessage = Resource.String.EmailWrong;
                Email.RequestFocus();
                return false;
            }
            if (Password.Text.Trim().Length < 6)
            {
                checkFormMessage = Resource.String.PasswordShort;
                Password.RequestFocus();
                return false;
            }
            if (Password.Text.Trim() != ConfirmPassword.Text.Trim()) {
                checkFormMessage = Resource.String.ConfirmPasswordNoMatch;
                ConfirmPassword.RequestFocus();
                return false;
            }
            if (Username.Text.Trim() == "")
            {
                checkFormMessage = Resource.String.UsernameEmpty;
                Username.RequestFocus();
                return false;
            }
            if (Name.Text.Trim() == "")
            {
                checkFormMessage = Resource.String.NameEmpty;
                Name.RequestFocus();
                return false;
            }
            if (uploadedImages.Count == 0)
            {
                checkFormMessage = Resource.String.ImagesEmpty;
                Images.RequestFocus();
                return false;
            }
            if (Description.Text.Trim() == "")
            {
                checkFormMessage = Resource.String.DescriptionEmpty;
                Description.RequestFocus();
                return false;
            }
            return true;            
        }

		private async void Reset_Click(object sender, EventArgs e)
		{
			imm.HideSoftInputFromWindow(Email.WindowToken, 0);
			MainLayout.RequestFocus();

			Reset.Enabled = false;

			if (regsessionid != "")
			{
				string responseString = await c.MakeRequest("action=deletetemp&imageName=&regsessionid=" + regsessionid); //deleting images from server
				if (responseString == "OK" || responseString == "INVALID_TOKEN")
				{
					if (File.Exists(regSessionFile))
					{
						File.Delete(regSessionFile);
					}
					regsessionid = "";
					if (File.Exists(regSaveFile))
					{
						File.Delete(regSaveFile);
					}
					ResetForm();
				}
				else
				{
					c.ReportError(responseString);
				}
			}
			else
			{
				ResetForm();
			}
			Reset.Enabled = true;
		}

		private void Cancel_Click(object sender, System.EventArgs e)
        {
			OnBackPressed();
        }
	}
}