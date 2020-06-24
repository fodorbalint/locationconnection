using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
		static float sizeRatio;
		public static Bitmap bm;
		public static int bmWidth;
		public static int bmHeight;

		public static Android.Net.Uri selectedFile;
		public static string selectedFileStr, selectedImageName;
		public RegisterCommonMethods rc;
		public float lastScale;
		public InputMethodManager imm;
		public Timer t;

		public abstract void SaveRegData();

		protected override void OnResume()
		{
			base.OnResume();

			if (!(ImageEditorFrameBorder is null))
			{
				CommonMethods.LogStatic("OnResume border width " + ImageEditorFrameBorder.Width + " variable " + imageEditorFrameBorderWidth);
			}	
		}

		protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
		{
			base.OnActivityResult(requestCode, resultCode, data);

			Images.Enabled = true;
			try
			{
				if (requestCode == 1 && resultCode == Result.Ok)
				{
					if (imagesUploading) //can happen if we click on the upload button twice fast enough
					{
						return;
					}
					selectedFile = data.Data;
				}
			}
			catch (Exception ex)
			{
				c.ReportErrorSilent("OnActivityResult error: " + ex.Message + " " + ex.StackTrace);
			}
		}

		public async void OnResumeEnd()
		{
			bm = await GetBitmap(this);
			if (bm is null)
			{
				return;
			}
					   
			bmWidth = bm.Width; //will be used later in OnDraw, should image be too large.
			bmHeight = bm.Height;

			sizeRatio = (float)bmWidth / bmHeight;

			c.Log("Image rotated if needed, sizeRatio: " + sizeRatio);

			if (sizeRatio == 1)
			{
				string fileName = System.IO.Path.Combine(CommonMethods.cacheFolder, "image.jpg");
				try
				{
					FileStream stream = new FileStream(fileName, FileMode.Create);
					bm.Compress(Bitmap.CompressFormat.Jpeg, 100, stream);
					stream.Close();
				}
				catch (Exception ex)
				{
					await c.ErrorAlert(res.GetString(Resource.String.CopyImageError) + " " + ex.Message);
					c.ReportErrorSilent(res.GetString(Resource.String.CopyImageError) + " " + ex.Message);
					return;
				}

				selectedFile = null;
				rc.UploadFile(fileName, RegisterActivity.regsessionid); //works for profile edit too
			}
			else
			{
				AdjustImage();
			}
		}

		public async static Task<Bitmap> GetBitmap(BaseActivity context)
		{
			CommonMethods c = context.c;
			Resources res = context.Resources;
			ContentResolver contentResolver = context.ContentResolver;

			ExifInterface exif;
			try
			{
				exif = new ExifInterface(contentResolver.OpenInputStream(selectedFile));
			}
			catch (Exception ex)
			{
				await c.ErrorAlert(res.GetString(Resource.String.ImageLoadingError) + " " + ex.Message);
				c.ReportErrorSilent(res.GetString(Resource.String.ImageLoadingError) + " " + ex.Message);
				selectedFile = null;
				return null;
			}

			c.Log("GetBitmap exif " + exif);

			int orientation = 0;
			if (!(exif is null))
			{
				orientation = exif.GetAttributeInt(ExifInterface.TagOrientation, (int)Android.Media.Orientation.Undefined);
			}

			try //Original issue: On Android 7.1.1 (Asus ZenFone Zoom S), image loading fails for the 4th time
			{
				bm = BitmapFactory.DecodeStream(contentResolver.OpenInputStream(selectedFile));
			}
			catch (Exception ex)
			{
				if (ex is OutOfMemoryException) //does not seem to be an happen now that bm is recycled after closing the editor
				{
					await c.ErrorAlert(res.GetString(Resource.String.OutOfMemoryError));
					c.ReportErrorSilent(res.GetString(Resource.String.OutOfMemoryError) + " " + ex.Message);
					throw ex; //OnResume is now finished, exception will not be caught
				}
				else
				{
					await c.ErrorAlert(res.GetString(Resource.String.ImageLoadingError) + " " + ex.Message);
					c.ReportErrorSilent(res.GetString(Resource.String.ImageLoadingError) + " " + ex.Message);
				}
				selectedFile = null;
				return null;
			}

			c.Log("bm " + bm);

			c.Log("Image width " + bm.Width + " height " + bm.Height + " orientation " + orientation);

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

			return bm;
		}

		public void AdjustImage()
		{
			c.Log("AdjustImage border width " + ImageEditorFrameBorder.Width + " variable " + imageEditorFrameBorderWidth);

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

		public static Bitmap RotateImage (Bitmap source, float angle)
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

			c.Log("Timer_Elapsed " + timerCounter + " border width " + ImageEditorFrameBorder.Width);
			//imageEditorFrameBorderWidth = ImageEditorFrameBorder.Width;
		}*/
	}
}