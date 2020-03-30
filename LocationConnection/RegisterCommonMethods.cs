using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Views;
using Android.Widget;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using Android.Text;
using Android.Text.Style;
using Android.Graphics;

namespace LocationConnection
{
	/* ----- Registration / Profile Edit ----- */
	class RegisterCommonMethods<T> where T:ProfilePage
	{
		View view;
		T context;

		public RegisterCommonMethods(View view, T context)
		{
			this.view = view;		
			this.context = context;
		}

		public async void CheckUsername_Click(object sender, System.EventArgs e)
		{
			if (context.Username.Text.Trim() == "")
			{
				context.Username.RequestFocus();
				context.c.Snack(Resource.String.UsernameEmpty, null);
				return;
			}
			if (context.Username.Text.Trim() == Session.Username)
			{
				context.c.Snack(Resource.String.UsernameSame, null);
				return;
			}

			string responseString = await context.c.MakeRequest("action=usercheck&Username=" + context.Username.Text.Trim());
			if (responseString == "OK")
			{
				context.c.Snack(Resource.String.UsernameAvailable, null);
			}
			else if (responseString.Substring(0, 6) == "ERROR_")
			{
				context.c.Snack(context.Resources.GetIdentifier(responseString.Substring(6), "string", context.PackageName), null);
			}
			else
			{
				context.c.ReportError(responseString);
			}
		}

		public void Images_Click(object sender, System.EventArgs e)
		{
			if (context.uploadedImages.Count < Constants.MaxNumPictures)
			{
				if (!context.imagesUploading)
				{
					context.ImagesProgressText.Text = "";
					context.ImagesProgress.Progress = 0;

					if (ContextCompat.CheckSelfPermission(context, Manifest.Permission.ReadExternalStorage) != (int)Permission.Granted)
					{
						if (ActivityCompat.ShouldShowRequestPermissionRationale(context, Manifest.Permission.ReadExternalStorage)) //shows when the user has once denied the permission, and now requesting it again.
						{
							var requiredPermissions = new String[] { Manifest.Permission.ReadExternalStorage };
							Snackbar.Make(view, Resource.String.StorageRationale, Snackbar.LengthIndefinite)
								.SetAction("OK", new Action<View>(delegate (View obj) { ActivityCompat.RequestPermissions(context, requiredPermissions, 1); })).Show();
						}
						else
						{
							ActivityCompat.RequestPermissions(context, new String[] { Manifest.Permission.ReadExternalStorage }, 1);
						}

					}
					else
					{
						SelectImage();
					}
				}
				else
				{
					context.c.Snack(Resource.String.ImagesUploading, null);
				}
			}
			else
			{
				context.c.SnackStr(context.res.GetString(Resource.String.MaxNumImages) + " " + Constants.MaxNumPictures, null);
			}
		}

		public void SelectImage()
		{
			Intent i = new Intent();
			i.SetType("image/*");
			i.SetAction(Intent.ActionGetContent);
			context.StartActivityForResult(Intent.CreateChooser(i, "Select a picture"), 1);
		}

		public async Task UploadFile(string fileName, string regsessionid) //use Task<int> for return value
		{
			try
			{
				WebClient client = new WebClient();
				client.UploadProgressChanged += Client_UploadProgressChanged;
				client.UploadFileCompleted += Client_UploadFileCompleted;
				client.Headers.Add("Content-Type", "image/jpeg");

				string url;	
				
				if (context.c.IsLoggedIn())
				{
					url = Constants.HostName + "?action=uploadtouser&ID=" + Session.ID + "&SessionID=" + Session.SessionID;
					if (Constants.isTestDB)
					{
						url += Constants.TestDB;
					}
				}
				else
				{
					url = (regsessionid == "") ? Constants.HostName + "?action=uploadtotemp" : Constants.HostName + "?action=uploadtotemp&regsessionid=" + regsessionid;
					if (Constants.isTestDB)
					{
						url += Constants.TestDB;
					}
				}
				await client.UploadFileTaskAsync(url, fileName);
				client.UploadProgressChanged -= Client_UploadProgressChanged;
				client.UploadFileCompleted -= Client_UploadFileCompleted;
			}
			catch (Exception ex)
			{
				context.LoaderCircle.Visibility = ViewStates.Invisible;
				context.LoaderCircle.ClearAnimation();

				context.imagesUploading = false;
				context.RunOnUiThread(() => {
					context.c.ReportErrorSilent(ex.Message + System.Environment.NewLine + ex.StackTrace);
				});
			}
		}

		private void Client_UploadProgressChanged(object sender, UploadProgressChangedEventArgs e)
		{
			if (e.ProgressPercentage == 0)
			{
				context.ImagesProgressText.Text = context.res.GetString(Resource.String.ImagesProgressText);
			}
			else
			{
				context.ImagesProgressText.Text = context.res.GetString(Resource.String.ImagesProgressTextPercent) + " " + e.ProgressPercentage + "%";
			}
			context.ImagesProgress.Progress = e.ProgressPercentage;
		}

		private void Client_UploadFileCompleted(object sender, UploadFileCompletedEventArgs e)
		{
			context.LoaderCircle.Visibility = ViewStates.Invisible;
			context.LoaderCircle.ClearAnimation();

			try
			{
				string responseString = System.Text.Encoding.UTF8.GetString(e.Result);
				if (responseString.Substring(0, 2) == "OK")
				{
					responseString = responseString.Substring(3);

					string[] arr = responseString.Split(";");
					string imgName = arr[0];
					context.uploadedImages.Add(imgName);
					if (context.c.IsLoggedIn())
					{
						Session.Pictures = context.uploadedImages.ToArray();
					}
					else
					{
						Console.WriteLine("--------upload finished-----" + context);
						if (!(context is null))
						{
							Console.WriteLine("--------upload finished 2 -----" + context.ImagesProgress);
						}
						RegisterActivity.regsessionid = arr[1];
						if (!File.Exists(RegisterActivity.regSessionFile))
						{
							File.WriteAllText(RegisterActivity.regSessionFile, RegisterActivity.regsessionid);
						}
						context.SaveRegData();
					}
					context.ImagesUploaded.AddPicture(imgName, context.uploadedImages.Count - 1);

				}
				else if (responseString.Substring(0, 6) == "ERROR_")
				{
					context.c.Snack(context.Resources.GetIdentifier(responseString.Substring(6), "string", context.PackageName), null);
				}
				else
				{
					context.c.ReportError(responseString);
				}
				context.imagesUploading = false;

				context.ImagesProgress.Progress = 0;
				if (context.uploadedImages.Count > 1)
				{
					context.ImagesProgressText.Text = context.res.GetString(Resource.String.ImagesRearrange);
				}
				else
				{
					context.ImagesProgressText.Text = "";
				}
			}
			catch (Exception ex)
			{
				context.c.ReportErrorSilent(ex.Message + System.Environment.NewLine + ex.StackTrace + System.Environment.NewLine + "context: " + context);
			}
		}

		public void UseLocationSwitch_Click(object sender, EventArgs e)
		{
			if (context.UseLocationSwitch.Checked)
			{
				if (ContextCompat.CheckSelfPermission(context, Manifest.Permission.AccessFineLocation) != (int)Permission.Granted)
				{
					context.UseLocationSwitch.Checked = false;
					if (ActivityCompat.ShouldShowRequestPermissionRationale(context, Manifest.Permission.AccessFineLocation)) //shows when the user has once denied the permission, and now requesting it again.
					{
						var requiredPermissions = new String[] { Manifest.Permission.AccessFineLocation };
						string snackText = context.Resources.GetString(Resource.String.LocationRationale);
						SpannableStringBuilder sbb = new SpannableStringBuilder();
						sbb.Append(snackText);
						sbb.SetSpan(new ForegroundColorSpan(Color.White), 0, snackText.Length, SpanTypes.ExclusiveExclusive);
						Snackbar.Make(context.MainLayout, sbb, Snackbar.LengthIndefinite)
							.SetAction("OK", new Action<View>(delegate (View obj) { ActivityCompat.RequestPermissions(context, requiredPermissions, 2); })).Show();
					}
					else
					{
						ActivityCompat.RequestPermissions(context, new String[] { Manifest.Permission.AccessFineLocation }, 2);
					}
				}
				else
				{
					EnableLocationSwitches(true);
				}
			}
			else
			{
				EnableLocationSwitches(false);
			}
		}

		public void EnableLocationSwitches(bool val)
		{
			context.LocationShareAll.Enabled = val;
			context.LocationShareLike.Enabled = val;
			context.LocationShareMatch.Enabled = val;
			context.LocationShareFriend.Enabled = val;
			context.LocationShareNone.Enabled = val;

			context.DistanceShareAll.Enabled = val;
			context.DistanceShareLike.Enabled = val;
			context.DistanceShareMatch.Enabled = val;
			context.DistanceShareFriend.Enabled = val;
			context.DistanceShareNone.Enabled = val;
		}

		public void LocationShareAll_Click(object sender, EventArgs e)
		{
			if (context.LocationShareAll.Checked)
			{
				context.LocationShareLike.Checked = true;
				context.LocationShareMatch.Checked = true;
				context.LocationShareFriend.Checked = true;
				context.LocationShareNone.Checked = false;
			}
		}

		public void LocationShareLike_Click(object sender, EventArgs e)
		{
			if (context.LocationShareLike.Checked)
			{
				context.LocationShareMatch.Checked = true;
				context.LocationShareFriend.Checked = true;
				context.LocationShareNone.Checked = false;
			}
			else
			{
				context.LocationShareAll.Checked = false;
			}
		}

		public void LocationShareMatch_Click(object sender, EventArgs e)
		{
			if (context.LocationShareMatch.Checked)
			{
				context.LocationShareFriend.Checked = true;
				context.LocationShareNone.Checked = false;
			}
			else
			{
				context.LocationShareAll.Checked = false;
				context.LocationShareLike.Checked = false;
			}
		}

		public void LocationShareFriend_Click(object sender, EventArgs e)
		{
			if (context.LocationShareFriend.Checked)
			{
				context.LocationShareNone.Checked = false;
			}
			else
			{
				context.LocationShareAll.Checked = false;
				context.LocationShareLike.Checked = false;
				context.LocationShareMatch.Checked = false;
			}
		}

		public void LocationShareNone_Click(object sender, EventArgs e)
		{
			if (context.LocationShareNone.Checked)
			{
				context.LocationShareAll.Checked = false;
				context.LocationShareLike.Checked = false;
				context.LocationShareMatch.Checked = false;
				context.LocationShareFriend.Checked = false;
			}
			else
			{
				context.LocationShareFriend.Checked = true;
			}
		}

		public void DistanceShareAll_Click(object sender, EventArgs e)
		{
			if (context.DistanceShareAll.Checked)
			{
				context.DistanceShareLike.Checked = true;
				context.DistanceShareMatch.Checked = true;
				context.DistanceShareFriend.Checked = true;
				context.DistanceShareNone.Checked = false;
			}
		}

		public void DistanceShareLike_Click(object sender, EventArgs e)
		{
			if (context.DistanceShareLike.Checked)
			{
				context.DistanceShareMatch.Checked = true;
				context.DistanceShareFriend.Checked = true;
				context.DistanceShareNone.Checked = false;
			}
			else
			{
				context.DistanceShareAll.Checked = false;
			}
		}

		public void DistanceShareMatch_Click(object sender, EventArgs e)
		{
			if (context.DistanceShareMatch.Checked)
			{
				context.DistanceShareFriend.Checked = true;
				context.DistanceShareNone.Checked = false;
			}
			else
			{
				context.DistanceShareAll.Checked = false;
				context.DistanceShareLike.Checked = false;
			}
		}

		public void DistanceShareFriend_Click(object sender, EventArgs e)
		{
			if (context.DistanceShareFriend.Checked)
			{
				context.DistanceShareNone.Checked = false;
			}
			else
			{
				context.DistanceShareAll.Checked = false;
				context.DistanceShareLike.Checked = false;
				context.DistanceShareMatch.Checked = false;
			}
		}

		public void DistanceShareNone_Click(object sender, EventArgs e)
		{
			if (context.DistanceShareNone.Checked)
			{
				context.DistanceShareAll.Checked = false;
				context.DistanceShareLike.Checked = false;
				context.DistanceShareMatch.Checked = false;
				context.DistanceShareFriend.Checked = false;
			}
			else
			{
				context.DistanceShareFriend.Checked = true;
			}
		}

		public int GetLocationShareLevel()
		{
			if (context.LocationShareAll.Checked)
			{
				return 4;
			}
			else if (context.LocationShareLike.Checked)
			{
				return 3;
			}
			else if (context.LocationShareMatch.Checked)
			{
				return 2;
			}
			else if (context.LocationShareFriend.Checked)
			{
				return 1;
			}
			else
			{
				return 0;
			}
		}

		public int GetDistanceShareLevel()
		{
			if (context.DistanceShareAll.Checked)
			{
				return 4;
			}
			else if (context.DistanceShareLike.Checked)
			{
				return 3;
			}
			else if (context.DistanceShareMatch.Checked)
			{
				return 2;
			}
			else if (context.DistanceShareFriend.Checked)
			{
				return 1;
			}
			else
			{
				return 0;
			}
		}
		public void SetLocationShareLevel(int level)
		{
			switch (level)
			{
				case 0:
					context.LocationShareNone.Checked = true;
					context.LocationShareFriend.Checked = false;
					context.LocationShareMatch.Checked = false;
					context.LocationShareLike.Checked = false;
					context.LocationShareAll.Checked = false;
					break;
				case 1:
					context.LocationShareNone.Checked = false;
					context.LocationShareFriend.Checked = true;
					context.LocationShareMatch.Checked = false;
					context.LocationShareLike.Checked = false;
					context.LocationShareAll.Checked = false;
					break;
				case 2:
					context.LocationShareNone.Checked = false;
					context.LocationShareFriend.Checked = true;
					context.LocationShareMatch.Checked = true;
					context.LocationShareLike.Checked = false;
					context.LocationShareAll.Checked = false;
					break;
				case 3:
					context.LocationShareNone.Checked = false;
					context.LocationShareFriend.Checked = true;
					context.LocationShareMatch.Checked = true;
					context.LocationShareLike.Checked = true;
					context.LocationShareAll.Checked = false;
					break;
				case 4:
					context.LocationShareNone.Checked = false;
					context.LocationShareFriend.Checked = true;
					context.LocationShareMatch.Checked = true;
					context.LocationShareLike.Checked = true;
					context.LocationShareAll.Checked = true;
					break;
			}
		}

		public void SetDistanceShareLevel(int level)
		{
			switch (level)
			{
				case 0:
					context.DistanceShareNone.Checked = true;
					context.DistanceShareFriend.Checked = false;
					context.DistanceShareMatch.Checked = false;
					context.DistanceShareLike.Checked = false;
					context.DistanceShareAll.Checked = false;
					break;
				case 1:
					context.DistanceShareNone.Checked = false;
					context.DistanceShareFriend.Checked = true;
					context.DistanceShareMatch.Checked = false;
					context.DistanceShareLike.Checked = false;
					context.DistanceShareAll.Checked = false;
					break;
				case 2:
					context.DistanceShareNone.Checked = false;
					context.DistanceShareFriend.Checked = true;
					context.DistanceShareMatch.Checked = true;
					context.DistanceShareLike.Checked = false;
					context.DistanceShareAll.Checked = false;
					break;
				case 3:
					context.DistanceShareNone.Checked = false;
					context.DistanceShareFriend.Checked = true;
					context.DistanceShareMatch.Checked = true;
					context.DistanceShareLike.Checked = true;
					context.DistanceShareAll.Checked = false;
					break;
				case 4:
					context.DistanceShareNone.Checked = false;
					context.DistanceShareFriend.Checked = true;
					context.DistanceShareMatch.Checked = true;
					context.DistanceShareLike.Checked = true;
					context.DistanceShareAll.Checked = true;
					break;
			}
		}
	}
}