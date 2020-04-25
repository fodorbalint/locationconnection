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
using Android.Views.Animations;

namespace LocationConnection
{
	/* ----- Registration / Profile Edit ----- */
	public class RegisterCommonMethods//<T> where T:ProfilePage
	{
		ProfilePage context;
		private WebClient client;
		float touchStartX;
		float touchStartY;

		public RegisterCommonMethods(ProfilePage context)
		{
			this.context = context;

			context.CheckUsername.Click += CheckUsername_Click;
			context.Images.Click += Images_Click;

			context.Description.Touch += Description_Touch;

			context.UseLocationSwitch.Click += UseLocationSwitch_Click;
			context.LocationShareAll.Click += LocationShareAll_Click;
			context.LocationShareLike.Click += LocationShareLike_Click;
			context.LocationShareMatch.Click += LocationShareMatch_Click;
			context.LocationShareFriend.Click += LocationShareFriend_Click;
			context.LocationShareNone.Click += LocationShareNone_Click;

			context.DistanceShareAll.Click += DistanceShareAll_Click;
			context.DistanceShareLike.Click += DistanceShareLike_Click;
			context.DistanceShareMatch.Click += DistanceShareMatch_Click;
			context.DistanceShareFriend.Click += DistanceShareFriend_Click;
			context.DistanceShareNone.Click += DistanceShareNone_Click;

			context.ImageEditor.Touch += ImageEditor_Touch;

			client = new WebClient();
			client.UploadProgressChanged += Client_UploadProgressChanged;
			client.UploadFileCompleted += Client_UploadFileCompleted;
			client.Headers.Add("Content-Type", "image/jpeg");
		}

		float prevTouchX, prevTouchY;
		float startCenterX, startCenterY;
		float xDist, yDist;
		bool outOfFrameX, outOfFrameY;

		private bool IsOutOfFrameY(float yDist)
		{
			if (yDist <= 0 && (-yDist + context.ImageEditorFrameBorder.Height / 2) > context.ImageEditor.Height / 2 || yDist > 0 && (yDist + context.ImageEditorFrameBorder.Height / 2) > context.ImageEditor.Height / 2)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		private bool IsOutOfFrameX(float xDist)
		{
			if (xDist <= 0 && (-xDist + context.ImageEditorFrameBorder.Width / 2) > context.ImageEditor.Width / 2 || xDist > 0 && (xDist + context.ImageEditorFrameBorder.Width / 2) > context.ImageEditor.Width / 2)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		//out of frame image is allowed to come closer. Image in frame is not allowed to go out, only by pinching action.
		private void ImageEditor_Touch(object sender, View.TouchEventArgs e)
		{
			switch (e.Event.Action)
			{
				case MotionEventActions.Down:
					prevTouchX = touchStartX = e.Event.GetX();
					prevTouchY = touchStartY = e.Event.GetY();
					startCenterX = context.ImageEditor.GetX() + context.ImageEditor.Width / 2;
					startCenterY = context.ImageEditor.GetY() + context.ImageEditor.Height / 2;

					xDist = startCenterX - context.ImageEditorFrameBorder.GetX() - context.ImageEditorFrameBorder.Width / 2;
					yDist = startCenterY - context.ImageEditorFrameBorder.GetY() - context.ImageEditorFrameBorder.Height / 2;

					outOfFrameY = IsOutOfFrameY(yDist);
					outOfFrameX = IsOutOfFrameX(xDist);

					
					context.c.CW("ImageEditor_Touch " + startCenterX + " " + startCenterY + " " + xDist + " " + yDist + " " + outOfFrameX + " " + outOfFrameY + " " + e.Event.PointerCount);

					break;
				case MotionEventActions.Move:
					float currentCenterX = context.ImageEditor.GetX() + context.ImageEditor.Width / 2;
					float currentCenterY = context.ImageEditor.GetY() + context.ImageEditor.Height / 2;

					//context.c.CW(" Move " + e.Event.GetX() + " " + e.Event.GetY() + " " + currentCenterX + " " + currentCenterY + " " + startCenterX + " " + startCenterY);

					float evX = e.Event.GetX() + currentCenterX - startCenterX; //coordinates relative to the image's original position
					float evY = e.Event.GetY() + currentCenterY - startCenterY;

					float newxDist = startCenterX + evX - touchStartX - context.ImageEditorFrameBorder.GetX() - context.ImageEditorFrameBorder.Width / 2;
					float newyDist = startCenterY + evY - touchStartY - context.ImageEditorFrameBorder.GetY() - context.ImageEditorFrameBorder.Height / 2;

					//context.c.CW("ImageEditor_Touch Move " + newxDist + " " + startCenterX + " " + touchStartX + " " + );

					if (outOfFrameY && (yDist <= 0 && newyDist < yDist || yDist > 0 && newyDist > yDist)) //out of frame, new distance is greater than previous
					{
						touchStartY += evY - prevTouchY;
					}
					else if (outOfFrameY) //new distance is smaller
					{
						if (yDist <= 0 && newyDist > (context.ImageEditor.Height - context.ImageEditorFrameBorder.Height) / 2) //making sure not to go out of frame the opposite end. (when the image is scaled back to 1:1, and moved fast, it can happen)
						{
							yDist = (context.ImageEditor.Height - context.ImageEditorFrameBorder.Height) / 2;
							touchStartY += newyDist - (context.ImageEditor.Height - context.ImageEditorFrameBorder.Height) / 2; //moving start touch position, so an opposite move will react immediately
						}
						else if (yDist > 0 && newyDist < -(context.ImageEditor.Height - context.ImageEditorFrameBorder.Height) / 2)
						{
							yDist = -(context.ImageEditor.Height - context.ImageEditorFrameBorder.Height) / 2;
							touchStartY += newyDist - -(context.ImageEditor.Height - context.ImageEditorFrameBorder.Height) / 2;
						}
						else
						{
							yDist = newyDist;
						}
						context.ImageEditor.SetY(context.ImageEditorFrameBorder.GetY() + context.ImageEditorFrameBorder.Height / 2 + yDist - context.ImageEditor.Height / 2);
						//context.c.CW("Setting Y " + (context.ImageEditorFrameBorder.GetY() + context.ImageEditorFrameBorder.Height / 2 + yDist - context.ImageEditor.Height / 2));

						outOfFrameY = IsOutOfFrameY(yDist);
					}
					else
					{
						yDist = newyDist;

						if (yDist <= 0 && (-yDist + context.ImageEditorFrameBorder.Height / 2) > context.ImageEditor.Height / 2) //going out of frame too high
						{
							yDist = -(context.ImageEditor.Height - context.ImageEditorFrameBorder.Height) / 2;
							touchStartY += newyDist - -(context.ImageEditor.Height - context.ImageEditorFrameBorder.Height) / 2;
						}
						else if (yDist > 0 && (yDist + context.ImageEditorFrameBorder.Height / 2) > context.ImageEditor.Height / 2) //going out of frame too low
						{
							yDist = (context.ImageEditor.Height - context.ImageEditorFrameBorder.Height) / 2;
							touchStartY += newyDist - (context.ImageEditor.Height - context.ImageEditorFrameBorder.Height) / 2;
						}
						// else in frame 
						context.ImageEditor.SetY(context.ImageEditorFrameBorder.GetY() + context.ImageEditorFrameBorder.Height / 2 + yDist - context.ImageEditor.Height / 2);
						//context.c.CW("Setting Y " + (context.ImageEditorFrameBorder.GetY() + context.ImageEditorFrameBorder.Height / 2 + yDist - context.ImageEditor.Height / 2));
					}


					if (outOfFrameX && (xDist <= 0 && newxDist < xDist || xDist > 0 && newxDist > xDist)) //out of frame, new is distance greater than previous
					{
						touchStartX += evX - prevTouchX;
					}
					else if (outOfFrameX)
					{
						if (xDist <= 0 && newxDist > (context.ImageEditor.Width - context.ImageEditorFrameBorder.Width) / 2) //making sure not to go out of frame the opposite end. (when the image is scaled back to 1:1, and moved fast, it can happen)
						{
							xDist = (context.ImageEditor.Width - context.ImageEditorFrameBorder.Width) / 2;
							touchStartX += newxDist - (context.ImageEditor.Width - context.ImageEditorFrameBorder.Width) / 2; //moving start touch position, so an opposite move will react immediately
						}
						else if (xDist > 0 && newxDist < -(context.ImageEditor.Width - context.ImageEditorFrameBorder.Width) / 2)
						{
							xDist = -(context.ImageEditor.Width - context.ImageEditorFrameBorder.Width) / 2;
							touchStartX += newxDist - -(context.ImageEditor.Width - context.ImageEditorFrameBorder.Width) / 2; //moving start touch position, so an opposite move will react immediately
						}
						else
						{
							xDist = newxDist;
						}
						context.ImageEditor.SetX(context.ImageEditorFrameBorder.GetX() + context.ImageEditorFrameBorder.Width / 2 + xDist - context.ImageEditor.Width / 2);
						//context.c.CW("Setting X " + (context.ImageEditorFrameBorder.GetX() + context.ImageEditorFrameBorder.Width / 2 + xDist - context.ImageEditor.Width / 2));

						outOfFrameX = IsOutOfFrameX(xDist);
					}
					else
					{
						xDist = newxDist;

						if (xDist <= 0 && (-xDist + context.ImageEditorFrameBorder.Width / 2) > context.ImageEditor.Width / 2) //going out of frame too left
						{
							xDist = -(context.ImageEditor.Width - context.ImageEditorFrameBorder.Width) / 2;
							touchStartX += newxDist - -(context.ImageEditor.Width - context.ImageEditorFrameBorder.Width) / 2; //moving start touch position, so an opposite move will react immediately
						}
						else if (xDist > 0 && (xDist + context.ImageEditorFrameBorder.Width / 2) > context.ImageEditor.Width / 2) //going out of frame too right
						{
							xDist = (context.ImageEditor.Width - context.ImageEditorFrameBorder.Width) / 2;
							touchStartX += newxDist - (context.ImageEditor.Width - context.ImageEditorFrameBorder.Width) / 2;
						}
						// else in frame
						context.ImageEditor.SetX(context.ImageEditorFrameBorder.GetX() + context.ImageEditorFrameBorder.Width / 2 + xDist - context.ImageEditor.Width / 2);
						//context.c.CW("Setting X " + (context.ImageEditorFrameBorder.GetX() + context.ImageEditorFrameBorder.Width / 2 + xDist - context.ImageEditor.Width / 2));
					}

					prevTouchX = evX;
					prevTouchY = evY;
					break;

				case MotionEventActions.Up:
				case MotionEventActions.Cancel:
					break;
			}
			e.Handled = false;
		}

		public async void CheckUsername_Click(object sender, System.EventArgs e)
		{
			if (context.Username.Text.Trim() == "")
			{
				context.Username.RequestFocus();
				context.c.Snack(Resource.String.UsernameEmpty);
				return;
			}
			if (context.Username.Text.Trim() == Session.Username)
			{
				context.c.Snack(Resource.String.UsernameSame);
				return;
			}

			context.CheckUsername.Enabled = false;

			string responseString = await context.c.MakeRequest("action=usercheck&Username=" + context.Username.Text.Trim());
			if (responseString == "OK")
			{
				context.c.Snack(Resource.String.UsernameAvailable);
			}
			else if (responseString.Substring(0, 6) == "ERROR_")
			{
				context.c.Snack(context.Resources.GetIdentifier(responseString.Substring(6), "string", context.PackageName));
			}
			else
			{
				context.c.ReportError(responseString);
			}

			context.CheckUsername.Enabled = true;
		}

		public void Images_Click(object sender, System.EventArgs e)
		{
			if (context.uploadedImages.Count < Constants.MaxNumPictures)
			{
				if (!context.imagesUploading && !context.imagesDeleting)
				{
					context.ImagesProgressText.Text = "";
					context.ImagesProgress.Progress = 0;

					if (ContextCompat.CheckSelfPermission(context, Manifest.Permission.ReadExternalStorage) != (int)Permission.Granted)
					{
						if (ActivityCompat.ShouldShowRequestPermissionRationale(context, Manifest.Permission.ReadExternalStorage)) //shows when the user has once denied the permission, and now requesting it again.
						{
							var requiredPermissions = new String[] { Manifest.Permission.ReadExternalStorage };

							context.c.SnackIndefAction(context.res.GetString(Resource.String.StorageRationale), new Action<View>(delegate (View obj) { ActivityCompat.RequestPermissions(context, requiredPermissions, 1); }));
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
					if (context.imagesUploading)
					{
						context.c.Snack(Resource.String.ImagesUploading);
					}
					else
					{
						context.c.Snack(Resource.String.ImagesDeleting);
					}
				}
			}
			else
			{
				context.c.SnackStr(context.res.GetString(Resource.String.MaxNumImages) + " " + Constants.MaxNumPictures + ".");
			}
		}

		public void SelectImage()
		{
			Intent i = new Intent();
			i.SetType("image/*");
			i.SetAction(Intent.ActionGetContent);
			context.StartActivityForResult(Intent.CreateChooser(i, "Select a picture"), 1);
		}
		public void ImageEditorCancel_Click(object sender, EventArgs e)
		{
			context.ImageEditorFrame.Visibility = ViewStates.Invisible;
			context.ImageEditor.Visibility = ViewStates.Invisible;
			context.ImageEditorFrameBorder.Visibility = ViewStates.Invisible;
			context.ImageEditorControls.Visibility = ViewStates.Invisible;
		}

		public async void ImageEditorOK_Click(object sender, EventArgs e)
		{
			context.ImageEditorFrame.Visibility = ViewStates.Invisible;
			context.ImageEditor.Visibility = ViewStates.Invisible;
			context.ImageEditorFrameBorder.Visibility = ViewStates.Invisible;
			context.ImageEditorControls.Visibility = ViewStates.Invisible;

			await UploadFile(context.selectedFileStr, RegisterActivity.regsessionid); //works for profile edit too
		}

		public void StartAnim()
		{
			Animation anim = Android.Views.Animations.AnimationUtils.LoadAnimation(context, Resource.Animation.rotate);
			context.LoaderCircle.Visibility = ViewStates.Visible;
			context.LoaderCircle.StartAnimation(anim);
			context.ImagesProgressText.Text = context.res.GetString(Resource.String.ImagesProgressText);
		}

		public async Task UploadFile(string fileName, string regsessionid) //use Task<int> for return value
		{
			context.imagesUploading = true;
			context.RunOnUiThread(() => { StartAnim(); });

			try
			{
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
					context.c.Snack(context.Resources.GetIdentifier(responseString.Substring(6), "string", context.PackageName));
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
				context.c.ReportErrorSilent(ex.Message + System.Environment.NewLine + ex.StackTrace);
			}
		}

		private void Description_Touch(object sender, View.TouchEventArgs e)
		{
			if (context.Description.HasFocus)
			{
				context.MainScroll.RequestDisallowInterceptTouchEvent(true);
			}
			e.Handled = false;
		}

		public void UseLocationSwitch_Click(object sender, EventArgs e)
		{
			if (context.UseLocationSwitch.Checked)
			{
				if (!context.c.IsLocationEnabled())
				{
					context.UseLocationSwitch.Checked = false;
					if (ActivityCompat.ShouldShowRequestPermissionRationale(context, Manifest.Permission.AccessFineLocation)) //shows when the user has once denied the permission, and now requesting it again.
					{
						var requiredPermissions = new String[] { Manifest.Permission.AccessFineLocation };

						context.c.SnackIndefAction(context.res.GetString(Resource.String.LocationRationale), new Action<View>(delegate (View obj) { ActivityCompat.RequestPermissions(context, requiredPermissions, 2); }));
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