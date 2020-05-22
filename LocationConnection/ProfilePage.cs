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
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Support.Constraints;
using Android.Support.V7.App;
using Android.Views;
using Android.Views.InputMethods;
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
		public View TopSeparator;

		public List<string> uploadedImages;		
		public bool imagesUploading;
		public bool imagesDeleting;
		public Resources res;
		public bool imageEditorOpen;
		float sizeRatio;
		Bitmap bm;

		public string selectedFileStr, selectedImageName;
		public RegisterCommonMethods rc;
		public float lastScale;
		public InputMethodManager imm;

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

				c.LogActivity("Original selectedFile: " + selectedFile + ", selectedFile.Path: " + selectedFile.Path);
				if (path.IndexOf(":") != -1) //fix #1
				{
					int colonPos = path.IndexOf(":");
					path = path.Substring(colonPos + 1);
					c.LogActivity("Splitting at first colon");
				}
				if (!File.Exists(path))
				{
					string str = Regex.Replace(selectedFile.Path, @"/document/([A-Z\d]{4}-[A-Z\d]{4}):", "/storage/$1/"); // fix #2
					c.LogActivity("Replaced document to storage");
					if (!File.Exists(str))
					{
						try
						{
							selectedFileStr = GetPathToImage(selectedFile);
						}
						catch (Exception ex)
						{
							c.LogActivity("UploadImagePathNotFound: " + ex.Message + " " + ex.StackTrace.Replace("\n"," "));
							c.ReportError(res.GetString(Resource.String.UploadImagePathNotFound));
							return;
						}
						c.LogActivity("Image resolved to " + selectedFileStr);
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

				ExifInterface exif = new ExifInterface(selectedFileStr);
				int orientation = exif.GetAttributeInt(ExifInterface.TagOrientation, (int)Android.Media.Orientation.Undefined);

				bm = BitmapFactory.DecodeFile(selectedFileStr);

				c.CW("Image width " + bm.Width + " height " + bm.Height + " orientation " + orientation + " file " + selectedFileStr);
				c.LogActivity("Image width " + bm.Width + " height " + bm.Height + " orientation " + orientation + " file " + selectedFileStr);

				switch (orientation)
				{
					case (int)Android.Media.Orientation.Rotate90:
						bm = RotateImage(bm, 90);
						break;
					case (int)Android.Media.Orientation.Rotate180:
						bm = RotateImage(bm, 180);
						break;
					case (int)Android.Media.Orientation.Rotate270:
						bm = RotateImage(bm, 270);
						break;
				}

				sizeRatio = (float)bm.Width / bm.Height;

				if (sizeRatio == 1)
				{
					await rc.UploadFile(selectedFileStr, RegisterActivity.regsessionid); //works for profile edit too
				}
				else
				{
					//called before OnResume. If the keyboard was open, screen size is reduced.
					imageEditorOpen = true;					
				}
			}
		}

		public string GetPathToImage(Android.Net.Uri uri)
		{
			//This method only finds images from the gallery, for images it returns null
			/*var cu = ContentResolver.Query(uri, new string[] { Android.Provider.MediaStore.Images.Media.InterfaceConsts.Data }, null, null, null);
			cu.MoveToFirst();
			string p = cu.GetString(0);
			cu.Close();

			c.LogActivity("Path found early: " + p + " colIndex " + colIndex);*/


			string doc_id = "";
			using (var c1 = ContentResolver.Query(uri, null, null, null, null))
			{
				if (c1 is null)
				{
					throw new Exception("Cursor at first query is null");
				}
				c.LogActivity("First cursor colcount: " + c1.ColumnCount + " rowCount: " + c1.Count); // 6 columns for images, 33 columns for gallery. Using ID in the projection would result in an empty document_id string for images.
				c1.MoveToFirst();

				string row = "";
				for(int i = 0; i < c1.ColumnCount;  i++)
				{
					row += i + ":" + c1.GetString(i) + "; ";
				}				
				c.LogActivity(row);
				/* Images:
				 * image:73381 - image/jpeg - P1000148_cut_4-5.JPG - 1590000387000 - 131077 - 7428059
				 * Gallery:
				 * 73412 - /storage/emulated/0/DCIM/Camera/IMG_20200522_112801.jpg - 1852812 - IMG_20200522_112801.jpg - image/jpeg - IMG_20200522_112801
				 */
				string document_id = c1.GetString(0); //if the picture was just deleted, this error is thrown: Index 0 requested, with a size of 0
				
				if (document_id is null)
				{
					throw new Exception("document_id is null");
				}

				//online services do not work, no ID can be extracted.
				//Google Photos: /0/1/mediakey:/local:e5c12407-e2c5-47db-b5ca-c7d757cb5e4c/ORIGINAL/NONE/image/jpeg/145854146
				//OneDrive: /Drive/ID/1/Item/RID/6028F4288AEF832B!6520/Stream/1/Property/3B 2018-01-05 (00).png
				c.LogActivity("GetPathToImage document_id " + document_id); //from gallery: 73412 | from images: image:73412

				doc_id = document_id.Substring(document_id.LastIndexOf(":") + 1);
			}

			string path = null;

			// The projection contains the columns we want to return in our query.
			string selection = Android.Provider.MediaStore.Images.Media.InterfaceConsts.Id + " =? "; //_id=?
			using (var cursor = ContentResolver.Query(Android.Provider.MediaStore.Images.Media.ExternalContentUri, new string[] { Android.Provider.MediaStore.Images.Media.InterfaceConsts.Data }, selection, new string[] { doc_id }, null))
			{
				if (cursor is null)
				{
					throw new Exception("Cursor at second query is null");
				}
				c.LogActivity("Second cursor colcount: " + cursor.ColumnCount + " rowCount: " + cursor.Count);
				cursor.MoveToFirst();
				path = cursor.GetString(0);
			}
			return path;
		}

		public void AdjustImage()
		{
			c.CW("AdjustImage 0 imageEditorFrameBorderWidth " + imageEditorFrameBorderWidth);

			if (ImageEditorFrameBorder.Width > imageEditorFrameBorderWidth)
			{
				imageEditorFrameBorderWidth = ImageEditorFrameBorder.Width;
			}

			c.CW("AdjustImage 1 imageEditorFrameBorderWidth " + imageEditorFrameBorderWidth);

			ImageEditorControls.Visibility = ViewStates.Visible;
			TopSeparator.Visibility = ViewStates.Visible;
			ImageEditor.Visibility = ViewStates.Visible;
			ImageEditorFrame.Visibility = ViewStates.Visible;
			ImageEditorFrameBorder.Visibility = ViewStates.Visible;

			if (sizeRatio > 1)
			{
				ImageEditor.intrinsicHeight = imageEditorFrameBorderWidth;
				ImageEditor.intrinsicWidth = imageEditorFrameBorderWidth * sizeRatio;
			}
			else
			{
				ImageEditor.intrinsicHeight = imageEditorFrameBorderWidth / sizeRatio;
				ImageEditor.intrinsicWidth = imageEditorFrameBorderWidth;
			}
			ImageEditor.scaleFactor = 1;
			ImageEditor.xDist = 0;
			ImageEditor.yDist = 0;
			ImageEditor.SetContent(bm);
		}

		public Bitmap RotateImage (Bitmap source, float angle)
		{
			Matrix matrix = new Matrix();
			matrix.PostRotate(angle);
			return Bitmap.CreateBitmap(source, 0, 0, source.Width, source.Height, matrix, true);
		}
	}
}