using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.Constraints;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;

namespace LocationConnection
{
	public abstract class ProfilePage : BaseActivity
	{
		public TouchScrollView MainScroll;
		public new ConstraintLayout MainLayout;
		public ImageFrameLayout ImagesUploaded;
		public EditText Email, Username, Name, Description;
		public Button CheckUsername, Images;		
		public TextView ImagesProgressText;
		public ImageView LoaderCircle;
		public ProgressBar ImagesProgress;
		public Switch UseLocationSwitch, LocationShareAll, LocationShareLike, LocationShareMatch, LocationShareFriend, LocationShareNone;
		public Switch DistanceShareAll, DistanceShareLike, DistanceShareMatch, DistanceShareFriend, DistanceShareNone;
		public View ImageEditorFrame, ImageEditorFrameBorder;
		public ScaleImageView ImageEditor;
		public LinearLayout ImageEditorControls;
		public ImageButton ImageEditorCancel, ImageEditorOK;

		public List<string> uploadedImages;		
		public bool imagesUploading;
		public bool imagesDeleting;
		public Resources res;

		public string selectedFileStr, selectedImageName;
		public RegisterCommonMethods rc;
		public float lastScale;

		public abstract void SaveRegData();

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

				selectedImageName = selectedFileStr.Substring(selectedFileStr.LastIndexOf("/") + 1);

				if (uploadedImages.IndexOf(selectedImageName) != -1)
				{
					c.Snack(Resource.String.ImageExists);
					return;
				}

				Bitmap bm = BitmapFactory.DecodeFile(selectedFileStr);

				c.CW(bm.Width + " " + bm.Height);

				float sizeRatio = (float)bm.Width / bm.Height;

				if (sizeRatio == 1)
				{
					await rc.UploadFile(selectedFileStr, RegisterActivity.regsessionid); //works for profile edit too
				}
				else
				{
					ImageEditorControls.Visibility = ViewStates.Visible;
					ImageEditor.Visibility = ViewStates.Visible;
					ImageEditorFrame.Visibility = ViewStates.Visible;
					ImageEditorFrameBorder.Visibility = ViewStates.Visible;

					if (sizeRatio > 1)
					{
						ImageEditor.intrinsicHeight = ImageEditorFrameBorder.Height;
						ImageEditor.intrinsicWidth = (int)Math.Round(ImageEditorFrameBorder.Width * sizeRatio);
					}
					else
					{
						ImageEditor.intrinsicHeight = (int)Math.Round(ImageEditorFrameBorder.Width / sizeRatio);
						ImageEditor.intrinsicWidth = ImageEditorFrameBorder.Width;
					}
					ImageEditor.scaleFactor = 1;
					ImageEditor.xDist = 0;
					ImageEditor.yDist = 0;
					ImageEditor.SetContent(bm);					
				}
			}
		}
	}
}