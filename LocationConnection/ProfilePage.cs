using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
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

		public static int imageEditorFrameBorderWidth;

		public List<string> uploadedImages;		
		public bool imagesUploading;
		public bool imagesDeleting;
		public Resources res;
		public static bool imageEditorOpen;
		static float sizeRatio;
		static Bitmap bm;

		public static string selectedFileStr, selectedImageName;
		public RegisterCommonMethods rc;
		public float lastScale;
		public InputMethodManager imm;
		public Timer t;
		public bool active;

		public bool saveData; //Huawei Y6 fix

		public abstract void SaveRegData();

		protected override void OnResume()
		{
			base.OnResume();

			active = true;
			if (!(ImageEditorFrameBorder is null))
			{
				CommonMethods.LogActivityStatic("OnResume border width " + ImageEditorFrameBorder.Width + " variable " + imageEditorFrameBorderWidth);
			}
			else //On Huawei Y6 this happens. RegisterActivity.OnResume will crash if the views are not set.
			{
				CommonMethods.LogActivityStatic("OnResume ImageEditorFrameBorder is null");
			}	
		}

		protected override void OnPause()
		{
			base.OnPause();

			active = false;
		}

		protected async override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
		{
			base.OnActivityResult(requestCode, resultCode, data); 
			
			try
			{
				if (requestCode == 1 && resultCode == Result.Ok)
				{
					if (imagesUploading) //can happen if we click on the upload button twice fast enough
					{
						return;
					}
					Android.Net.Uri selectedFile = data.Data;

					/*
					 Images and gallery selection: see comment after function

					 File does not exist - using Emulator Downloads folder / fix #1
					 selectedFile:
						content://com.android.providers.downloads.documents/document/raw%3A%2Fstorage%2Femulated%2F0%2FDownload%2F....jpg
					 selectedFile.Path:
						/document.raw:/storage/emulated/0/Download/....jpg

					OnePlus 8 Pro: Files / fix #2
					selectedFile: content://com.android.externalstorage.documents/document/primary:DCIM/Camera/Marco/OnePlus 8 PRO Benjamin Rasmussen Macro Vertical 1.jpg
					selectedFile.Path: /document/primary:DCIM/Camera/Marco/OnePlus 8 PRO Benjamin Rasmussen Macro Vertical 1.jpg
					0:primary:DCIM/Camera/Marco/OnePlus 8 PRO Benjamin Rasmussen Macro Vertical 1.jpg; 1:image/jpeg; 2:OnePlus 8 PRO Benjamin Rasmussen Macro Vertical 1.jpg; 3:1590403763000; 4:16711; 5:2413083;

					File does not exist - using SD card / fix #3
					 selectedFile:
						content://com.android.externalstorage.documents/document/E910-4E32%3APictures%2F....jpg;
					 selectedFile.Path:
						/document/E910-4E32:Pictures/....jpg;False;False

					 File exists - using Total Commander
					 selectedFile:
						content://com.ghisler.android.TotalCommander.files/storage/emulated/0/Documents/....jpg;
					 selectedFile.Path:
						/storage/emulated/0/Documents/....jpg;False;True
					 */

					string path = selectedFile.Path;

					c.LogActivity("selectedFile: " + selectedFile + " selectedFile.Path: " + selectedFile.Path);
					if (path.IndexOf(":") != -1) //fix #1
					{
						int colonPos = path.IndexOf(":");
						path = path.Substring(colonPos + 1);
					}
					if (!File.Exists(path))
					{
						path = selectedFile.Path.Replace("/document/primary:", "/storage/emulated/0/"); // fix #2
						if (!File.Exists(path))
						{
							string str = Regex.Replace(selectedFile.Path, @"/document/([A-Z\d]{4}-[A-Z\d]{4}):", "/storage/$1/"); // fix #3
							if (!File.Exists(str))
							{

								try
								{
									selectedFileStr = GetPathToImage(selectedFile);
								}
								catch (Exception ex)
								{
									c.LogActivity("UploadImagePathNotFound: " + ex.Message + " " + ex.StackTrace.Replace("\n", " "));
									c.ReportError(res.GetString(Resource.String.UploadImagePathNotFound));
									return;
								}
								c.LogActivity("Image resolved to " + selectedFileStr);
							}
							else
							{
								selectedFileStr = str;
								c.LogActivity("Image resolved by fix 3 to " + selectedFileStr);
							}
						}
						else
						{
							selectedFileStr = path;
							c.LogActivity("Image resolved by fix 2 to " + selectedFileStr);
						}

					}
					else
					{
						selectedFileStr = path;
						c.LogActivity("Image path exists " + selectedFileStr);
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

					c.CW("Image width " + bm.Width + " height " + bm.Height + " orientation " + orientation);
					c.LogActivity("Image width " + bm.Width + " height " + bm.Height + " orientation " + orientation);

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

					c.LogActivity("Image rotated if needed, sizeRatio: " + sizeRatio);

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
			catch (Exception ex)
			{
				c.ReportErrorSilent("OnActivityResult error: " + ex.Message + " " + ex.StackTrace);
			}
		}

		/*
		Picture selected from Images (Android 8-10):

		selectedFile: content://com.android.providers.media.documents/document/image:812
		selectedFile.Path: /document/image:812
		0:image:812; 1:image/jpeg; 2:20200524_135011.jpg; 3:1590321011000; 4:16389; 5:1804976;
		document_id image:812
		Image resolved to /storage/emulated/0/DCIM/Camera/20200524_135011.jpg

		Picture from Gallery (Android 10)

		selectedFile: content://media/external/images/media/812
		selectedFile.Path: /external/images/media/812
		0:; 1:; 2:; 3:; 4:; 5:90; 6:2208; 7:0; 8:Camera; 9:; 10:external_primary; 11:1590321011; 12:; 13:20200524_135011.jpg; 14:1590321011458; 15:image/jpeg; 16:812; 17:/storage/emulated/0/DCIM/Camera/20200524_135011.jpg; 18:; 19:1804976; 20:20200524_135011; 21:2944; 22:; 23:0; 24:1920763097; 25:; 26:0; 27:1590321011; 28:; 29:DCIM; 30:Camera; 31:; 32:; 33:-1739773001; 34:DCIM/Camera/;
		Data at index 17

		Android 9

		selectedFile: content://media/external/images/media/73412
		selectedFile.Path: /external/images/media/73412
		0:73412; 1:/storage/emulated/0/DCIM/Camera/IMG_20200522_112801.jpg; 2:1852812; 3:IMG_20200522_112801.jpg; 4:image/jpeg; 5:IMG_20200522_112801; 6:1590139682; 7:; 8:1590139682; 9:; 10:; 11:; 12:; 13:; 14:1590139681685; 15:0; 16:-5615316856834233471; 17:-1739773001; 18:Camera; 19:2336; 20:4160; 21:; 22:; 23:; 24:; 25:; 26:; 27:0; 28:; 29:0; 30:; 31:; 32:1;
		Data at index 1

		Android 8

		selectedFile: content://media/external/images/media/7137
		selectedFile.Path: /external/images/media/7137
		0:7137; 1:/storage/emulated/0/DCIM/Camera/20200524_143124.jpg; 2:3728745; 3:20200524_143124.jpg; 4:image/jpeg; 5:20200524_143124; 6:1590323484; 7:1590323484; 8:; 9:; 10:; 11:; 12:; 13:1590323484314; 14:90; 15:541715872148713776; 16:-1739773001; 17:Camera; 18:5312; 19:2988; 20:65537; 21:65537; 22:0; 23:0; 24:20200524_143124.jpg; 25:0; 26:0; 27:; 28:;
		Data at index 1

		Local image selected from Google Photos (Android 10):

		selectedFile: content://com.google.android.apps.photos.contentprovider/-1/1/content://media/external/images/media/812/ORIGINAL/NONE/image/jpeg/656313712
		selectedFile.Path: /-1/1/content://media/external/images/media/812/ORIGINAL/NONE/image/jpeg/656313712
		0:812; 1:20200524_135011.jpg; 2:1804976; 3:image/jpeg; 4:/storage/emulated/0/DCIM/Camera/20200524_135011.jpg; 5:90; 6:1590321011458; 7:; 8:; 9:;

		Downloaded or local image from Google Photos (tested in Android 9)

		selectedFile: content://com.google.android.apps.photos.contentprovider/0/1/mediakey:/local%3A3f57f21b-00be-4969-b719-475b722aa5f0/ORIGINAL/NONE/image/jpeg/1274422508
		selectedFile.Path: /0/1/mediakey:/local:3f57f21b-00be-4969-b719-475b722aa5f0/ORIGINAL/NONE/image/jpeg/1274422508
		0:0; 1:20200522_120306.jpg; 2:2038799; 3:image/jpeg; 4:; 5:0; 6:1590141785000; 7:55.7179; 8:12.4513; 9:;
		Data at index 4, empty

		Downloads do not work (tested in Android 9)

		selectedFile: content://com.android.providers.downloads.documents/document/6087
		selectedFile.Path: /document/6087
		0:6087; 1:image/png; 2:chroma-444.png; 3:chroma-444.png; 4:1579346983185; 5:71; 6:25326;
		Id is not found in second query

		*/

		public string GetPathToImage(Android.Net.Uri uri)
		{
			string path;
			string document_id = null;

			using (var c1 = ContentResolver.Query(uri, null, null, null, null))
			{
				if (c1.Count == 0) //the image has just been deleted.
				{
					throw new Exception("The image does not exist.");
				}
				c1.MoveToFirst();

				string row = "";
				for(int i = 0; i < c1.ColumnCount;  i++)
				{
					row += i + ":" + c1.GetString(i) + "; ";
				}				

				c.LogActivity(row);

				int colIndex = c1.GetColumnIndex(Android.Provider.MediaStore.Images.Media.InterfaceConsts.Data);
				c.LogActivity("Data col index: " + colIndex);

				if (colIndex != -1) //image picked from gallery
				{
					path = c1.GetString(colIndex);
					if (path != null)
					{
						return path;
					}
					else //Google Photos
					{
						//This is not it, the second query will return 0 rows.
						//document_id = uri.Path.Substring(uri.Path.LastIndexOf("/") + 1);
						throw new Exception("Data is null");
					}
				}

				//if (document_id is null)
				//{
					document_id = c1.GetString(0); //We don't know what the 0 column stands for in terms of InterfaceConsts. Android.Provider.MediaStore.Images.Media.InterfaceConsts.Id is not it.
				//}

				if (document_id is null)
				{
					throw new Exception("document_id is null");
				}

				//online services do not work, no ID can be extracted.
				//Google Photos: /0/1/mediakey:/local:e5c12407-e2c5-47db-b5ca-c7d757cb5e4c/ORIGINAL/NONE/image/jpeg/145854146
				//OneDrive: /Drive/ID/1/Item/RID/6028F4288AEF832B!6520/Stream/1/Property/3B 2018-01-05 (00).png
				c.LogActivity("GetPathToImage document_id " + document_id); //from gallery: 73412 | from images: image:73412

				document_id = document_id.Substring(document_id.LastIndexOf(":") + 1);
			}

			

			// The projection contains the columns we want to return in our query.
			string selection = Android.Provider.MediaStore.Images.Media.InterfaceConsts.Id + " =? "; //_id=?
			using (var cursor = ContentResolver.Query(Android.Provider.MediaStore.Images.Media.ExternalContentUri, new string[] { Android.Provider.MediaStore.Images.Media.InterfaceConsts.Data }, selection, new string[] { document_id }, null))
			{
				if (cursor.Count == 0)
				{
					throw new Exception("Id is invalid.");
				}
				cursor.MoveToFirst();
				path = cursor.GetString(0);
			}
			return path;
		}

		public void AdjustImage()
		{
			c.LogActivity("AdjustImage border width " + ImageEditorFrameBorder.Width + " variable " + imageEditorFrameBorderWidth);

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

		/*public int timerCounter;
		public void Timer_Elapsed(object sender, ElapsedEventArgs e) //it takes 30-50 ms from OnResume start / OnConfiguration changed for the layout to get the new values.
		{
			timerCounter++;
			if (timerCounter > 20)
			{
				((Timer)sender).Stop();
			}

			c.LogActivity("Timer_Elapsed " + timerCounter + " border width " + ImageEditorFrameBorder.Width);
			//imageEditorFrameBorderWidth = ImageEditorFrameBorder.Width;
		}*/
	}
}