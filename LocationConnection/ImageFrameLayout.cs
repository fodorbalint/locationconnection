using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Android.Animation;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.Constraints;
using Android.Util;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;

namespace LocationConnection
{
	public class ImageFrameLayout : FrameLayout
	{
		private ProfilePage context;
		public int tileSpacing;
		public int numColumns;
		public float tileSize;
		private int tweenTime = 360;
		private int pressTime = 360;
		private int pressDistance = 25;
		public List<int> drawOrder;

		static int prevEventHashCode;		
		int removeIndex;

		float? touchStartX;
		float? touchStartY;
		Stopwatch stw;
		View currentImage;
		Timer timer;
		float scaleRatio = 1.1F;
		bool imageSelected; 
		bool imageMovable;
		public bool touchStarted;
		int timerCounter;
		float touchCurrentX;
		float touchCurrentY;
		float dragStartX;
		float dragStartY;
		int picStartX;
		int picStartY;
		int startIndexPos;

		public ImageFrameLayout(Context context, IAttributeSet attrs) :	base(context, attrs)
		{
			Initialize(context);
		}

		public ImageFrameLayout(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
		{
			Initialize(context);
		}

		private void Initialize(Context context)
		{
			this.context = (ProfilePage)context;

			drawOrder = new List<int>();
			//Width is 0 even in OnMeasure, even though I set the attribute to match_parent in the layout file.
		}

		protected override void OnConfigurationChanged(Configuration newConfig)
		{
			context.GetScreenMetrics(false);
			SetTileSize();
			Reposition();
			RefitImagesContainer();

			base.OnConfigurationChanged(newConfig);
		}

		public void SetTileSize()
		{
			tileSize = (BaseActivity.screenWidth - 20 * BaseActivity.pixelDensity - tileSpacing * (numColumns - 1) * BaseActivity.pixelDensity) / numColumns;
		}

		public void Reposition()
		{
			for (int i=0; i < ChildCount; i++)
			{
				LayoutParams p0 = new LayoutParams((int)tileSize, (int)tileSize);
				GetChildFromDrawOrder(i).LayoutParameters = p0;
				GetChildFromDrawOrder(i).SetX(GetPosX(i));
				GetChildFromDrawOrder(i).SetY(GetPosY(i));
			}
		}

		public void AddPicture(string picture, int pos)
		{
			View UploadedImageContainer;
			if (Settings.DisplaySize == 1)
			{
				UploadedImageContainer = context.LayoutInflater.Inflate(Resource.Layout.uploaded_item_normal, this, false);
			}
			else
			{
				UploadedImageContainer = context.LayoutInflater.Inflate(Resource.Layout.uploaded_item_small, this, false);
			}
			ImageView UploadedImage = UploadedImageContainer.FindViewById<ImageView>(Resource.Id.UploadedImage);
			ImageButton DeleteUploadedImage = UploadedImageContainer.FindViewById<ImageButton>(Resource.Id.DeleteUploadedImage);

			DeleteUploadedImage.Click += DeleteUploadedImage_Click;
			DeleteUploadedImage.Touch += DeleteUploadedImage_Touch;

			ImageCache im = new ImageCache(context);

			Task.Run(async () => {
				if (context is ProfileEditActivity)
				{
					await im.LoadImage(UploadedImage, Session.ID.ToString(), picture);
				}
				else
				{
					await im.LoadImage(UploadedImage, RegisterActivity.regsessionid, picture, false, true);
				}
			});					

			LayoutParams p0 = new LayoutParams((int)tileSize, (int)tileSize);
			UploadedImageContainer.LayoutParameters = p0;
			UploadedImageContainer.SetX(GetPosX(pos));
			UploadedImageContainer.SetY(GetPosY(pos));

			AddView(UploadedImageContainer);
			drawOrder.Add(pos);

			LayoutParameters.Width = (int)(BaseActivity.screenWidth - 20 * BaseActivity.pixelDensity);
			LayoutParameters.Height = (int)((pos - pos % numColumns) / numColumns * (tileSize + tileSpacing * BaseActivity.pixelDensity) + tileSize);
		}

		private void DeleteUploadedImage_Touch(object sender, TouchEventArgs e)
		{
			switch (e.Event.Action)
			{
				case MotionEventActions.Down:
					if (Settings.DisplaySize == 1)
					{
						((ImageButton)sender).SetImageResource(Resource.Drawable.ic_delete_pressed_normal);
					}
					else
					{
						((ImageButton)sender).SetImageResource(Resource.Drawable.ic_delete_pressed_small);
					}
					break;
				case MotionEventActions.Up:
				case MotionEventActions.Cancel:
					if (Settings.DisplaySize == 1)
					{
						((ImageButton)sender).SetImageResource(Resource.Drawable.ic_delete_normal);
					}
					else
					{
						((ImageButton)sender).SetImageResource(Resource.Drawable.ic_delete_small);
					}
					break;
				
			}
			e.Handled = false;
		}

		private async void DeleteUploadedImage_Click(object sender, EventArgs e)
		{
			//Problem: event firing multiple times		
			if (prevEventHashCode == e.GetHashCode())
			{
				return;
			}
			prevEventHashCode = e.GetHashCode();

			if (context.imagesUploading)
			{
				context.c.Snack(Resource.String.ImagesUploading);
				return;
			}
			else if (context.imagesDeleting)
			{
				context.c.Snack(Resource.String.ImagesDeleting);
				return;
			}

			if (context is ProfileEditActivity)
			{
				if (context.uploadedImages.Count == 1)
				{
					context.c.Snack(Resource.String.LastImageToDelete);
					return;
				}
			}
			else if (context.uploadedImages.Count == 1)
			{
				context.ImagesProgressText.Text = "";
			}

			context.imagesDeleting = true;

			Animation anim = Android.Views.Animations.AnimationUtils.LoadAnimation(context, Resource.Animation.rotate);
			context.LoaderCircle.Visibility = ViewStates.Visible;
			context.LoaderCircle.StartAnimation(anim);

			int index= GetDrawPosFromChildIndex(IndexOfChild((ConstraintLayout)((ImageButton)sender).Parent));

			if (context is ProfileEditActivity)
			{
				string responseString = await context.c.MakeRequest("action=deleteexisting&imageName=" + context.c.UrlEncode(context.uploadedImages[index]) + "&ID=" + Session.ID + "&SessionID=" + Session.SessionID);
				if (responseString.Substring(0, 2) == "OK")
				{
					context.uploadedImages.RemoveAt(index);
					Session.Pictures = context.uploadedImages.ToArray();
					RemovePicture(index);
					if (context.uploadedImages.Count == 1)
					{
						context.ImagesProgressText.Text = "";
					}
				}
				else
				{
					context.c.ReportError(responseString);
					context.imagesDeleting = false;
				}
			}
			else
			{
				string responseString = await context.c.MakeRequest("action=deletetemp&imageName=" + context.c.UrlEncode(context.uploadedImages[index]) + "&regsessionid=" + RegisterActivity.regsessionid);
				if (responseString.Substring(0, 2) == "OK")
				{
					context.uploadedImages.RemoveAt(index);
					RemovePicture(index);
					if (context.uploadedImages.Count == 1)
					{
						context.ImagesProgressText.Text = "";
					}
					else if (context.uploadedImages.Count == 0)
					{
						File.Delete(RegisterActivity.regSessionFile);
					}
				}
				else
				{
					context.c.ReportError(responseString);
					context.imagesDeleting = false;
				}
			}

			context.LoaderCircle.Visibility = ViewStates.Invisible;
			context.LoaderCircle.ClearAnimation();
		}

		private void RemovePicture(int index)
		{
			removeIndex = index;
			ObjectAnimator animator = ObjectAnimator.OfFloat(GetChildFromDrawOrder(index), "Alpha", 0);
			animator.SetDuration(tweenTime);
			animator.AnimationEnd += Animator_AnimationEnd;
			animator.Start();
		}

		private void Animator_AnimationEnd(object sender, EventArgs e)
		{
			if (removeIndex < context.uploadedImages.Count) //not the last one (uploadedImages have already been updated)
			{
				for (int i = removeIndex + 1; i <= context.uploadedImages.Count; i++)
				{
					MovePictureTo(i, i - 1);
				}
				Timer t = new Timer();
				t.Interval = tweenTime;
				t.Elapsed += T_Elapsed;
				t.Start();
				RemoveViewFromDrawOrder(removeIndex);
			}
			else
			{
				RemoveViewFromDrawOrder(removeIndex);
				RefitImagesContainer();
				context.imagesDeleting = false;
			}			
		}

		private void MovePictureTo(int from, int to)
		{
			GetChildFromDrawOrder(from).Animate().X(GetPosX(to)).Y(GetPosY(to)).SetDuration(tweenTime);
		}

		private void T_Elapsed(object sender, ElapsedEventArgs e)
		{
			((Timer)sender).Stop();
			RefitImagesContainer();
			context.imagesDeleting = false;
		}

		public void RefitImagesContainer()
		{
			if (ChildCount > 0)
			{
				int indexCount = ChildCount - 1;
				context.RunOnUiThread(() => {
					LayoutParameters.Width = (int)(BaseActivity.screenWidth - 20 * BaseActivity.pixelDensity);
					LayoutParameters.Height = (int)((indexCount - indexCount % numColumns) / numColumns * (tileSize + tileSpacing * BaseActivity.pixelDensity) + tileSize);
					RequestLayout();
				});
			}
			else
			{
				context.RunOnUiThread(() => {
					LayoutParameters.Width = (int)(BaseActivity.screenWidth - 20 * BaseActivity.pixelDensity);
					LayoutParameters.Height = 0;
					RequestLayout();
				});
			}
		}

		private int GetPosX(int pos)
		{
			return (int)(pos % numColumns * (tileSize + tileSpacing * BaseActivity.pixelDensity));
		}

		private int GetPosY(int pos)
		{
			return (int)((pos - pos % numColumns) / numColumns * (tileSize + tileSpacing * BaseActivity.pixelDensity));
		}

		private int GetIndexFromPos(float x, float y)
		{
			int posX = (int)((x - x % (tileSize + tileSpacing * BaseActivity.pixelDensity)) / (tileSize + tileSpacing * BaseActivity.pixelDensity));
			int posY = (int)((y - y % (tileSize + tileSpacing * BaseActivity.pixelDensity)) / (tileSize + tileSpacing * BaseActivity.pixelDensity));
			return posY * numColumns + posX;
		}		

		public override bool OnTouchEvent(MotionEvent e)
		{
			switch(e.Action)
			{
				case MotionEventActions.Down:
					if (touchStarted)
					{
						return base.OnTouchEvent(e);
					}
					Down(e);
					break;
				case MotionEventActions.Move:
					Move(e);
					break;
				case MotionEventActions.Up:
					Up();
					break;
			}
			return base.OnTouchEvent(e);
		}

		public void Down(MotionEvent e)
		{
			touchStartX = null;
			touchStartY = null;
			stw = new Stopwatch();
			stw.Start();
			imageMovable = false;
			imageSelected = false;
			touchStarted = true;
			if (!(timer is null) && timer.Enabled) //user started a touch before select animation finished
			{
				timer.Stop();
			}
		}

		public void Move(MotionEvent e)
		{
			touchCurrentX = e.GetX();
			touchCurrentY = e.GetY();
			if (touchStartX is null || touchStartY is null)
			{
				touchStartX = touchCurrentX;
				touchStartY = touchCurrentY;
			}

			if (stw.ElapsedMilliseconds > pressTime && Math.Abs(touchCurrentX - (float)touchStartX) < pressDistance * BaseActivity.pixelDensity && Math.Abs(touchCurrentY - (float)touchStartY) < pressDistance * BaseActivity.pixelDensity && !imageSelected)
			{
				// calculating the relative coordinates to the parent
				Rect offsetViewBounds = new Rect();
				GetDrawingRect(offsetViewBounds);

				context.MainScroll.OffsetDescendantRectToMyCoords(this, offsetViewBounds);
				int relativeTop = offsetViewBounds.Top;
				int relativeLeft = offsetViewBounds.Left;

				startIndexPos = GetIndexFromPos(touchCurrentX - relativeLeft, touchCurrentY + context.MainScroll.ScrollY - relativeTop);

				if (startIndexPos < ChildCount)
				{
					imageSelected = true;

					currentImage = GetChildFromDrawOrder(startIndexPos);
					currentImage.SetZ(1);
					currentImage.Animate().ScaleX(scaleRatio).ScaleY(scaleRatio).SetDuration(tweenTime / 2);
					timer = new Timer();
					timer.Interval = tweenTime / 2;
					timer.Elapsed += Timer_Elapsed;
					timer.Start();
					timerCounter = 0;

					picStartX = GetPosX(startIndexPos);
					picStartY = GetPosY(startIndexPos);
				}
			}
			else if (stw.ElapsedMilliseconds <= pressTime && (Math.Abs(touchCurrentX - (float)touchStartX) >= pressDistance * BaseActivity.pixelDensity ||  Math.Abs(touchCurrentY - (float)touchStartY) >= pressDistance * BaseActivity.pixelDensity))
			{
				touchStarted = false;
			}
			else if (imageMovable)
			{
				float distX = touchCurrentX - dragStartX;
				float distY = touchCurrentY - dragStartY;
				currentImage.SetX(picStartX + distX);
				currentImage.SetY(picStartY + distY);
			}
		}

		public void Up()
		{
			if (imageMovable)
			{
				imageMovable = false;

				float centerX = currentImage.GetX() + tileSize / 2;
				float centerY = currentImage.GetY() + tileSize / 2;
				
				int endIndexPos = GetIndexFromPos(centerX, centerY);
				
				if (endIndexPos < ChildCount && endIndexPos >= 0)
				{
					int endX = GetPosX(endIndexPos);
					int endY = GetPosY(endIndexPos);
					
					currentImage.Animate().X(endX).Y(endY).SetDuration(tweenTime);

					if (endIndexPos < startIndexPos)
					{
						string moveImage= context.uploadedImages[startIndexPos];
						int moveIndex = drawOrder[startIndexPos];
						for (int i = startIndexPos; i > endIndexPos; i--)
						{
							GetChildFromDrawOrder(i - 1).Animate().X(GetPosX(i)).Y(GetPosY(i)).SetDuration(tweenTime);
							context.uploadedImages[i] = context.uploadedImages[i - 1];
							drawOrder[i] = drawOrder[i - 1];
						}
						context.uploadedImages[endIndexPos] = moveImage;
						drawOrder[endIndexPos] = moveIndex;

						if (context is ProfileEditActivity)
						{
							Task.Run(async () =>
							{
								string responseString = await context.c.MakeRequest("action=updatepictures&Pictures=" + context.c.UrlEncode(string.Join("|", context.uploadedImages)) + "&ID=" + Session.ID + "&SessionID=" + Session.SessionID);
								if (responseString.Substring(0, 2) == "OK")
								{
									Session.Pictures = context.uploadedImages.ToArray();
								}
								else
								{
									context.c.ReportError(responseString);
								}
							});
						}
					}
					else if (endIndexPos > startIndexPos)
					{
						string moveImage = context.uploadedImages[startIndexPos];
						int moveIndex = drawOrder[startIndexPos];
						for (int i = startIndexPos; i < endIndexPos; i++)
						{
							GetChildFromDrawOrder(i + 1).Animate().X(GetPosX(i)).Y(GetPosY(i)).SetDuration(tweenTime);
							context.uploadedImages[i] = context.uploadedImages[i + 1];
							drawOrder[i] = drawOrder[i + 1];
						}
						context.uploadedImages[endIndexPos] = moveImage;
						drawOrder[endIndexPos] = moveIndex;

						if (context is ProfileEditActivity)
						{
							Task.Run(async () =>
							{
								string responseString = await context.c.MakeRequest("action=updatepictures&Pictures=" + context.c.UrlEncode(string.Join("|", context.uploadedImages)) + "&ID=" + Session.ID + "&SessionID=" + Session.SessionID);
								if (responseString.Substring(0, 2) == "OK")
								{
									Session.Pictures = context.uploadedImages.ToArray();
								}
								else
								{
									context.c.ReportError(responseString);
								}
							});
						}
					}
					//else remained at the same place
				}
				else //out of range, restore position
				{
					int endX = GetPosX(startIndexPos);
					int endY = GetPosY(startIndexPos);
					currentImage.Animate().X(endX).Y(endY).SetDuration(tweenTime);
				}
				WaitToEnd();
			}
			else if (imageSelected) //user can start another touch before animation is finished, because pressTime > tweenTime.
			{
				touchStarted = false;
				currentImage.SetZ(0);
			}
			else //less than pressTime time passed
			{
				touchStarted = false;
			}
		}

		private void Timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			timerCounter++;
			if (timerCounter == 1)
			{
				context.RunOnUiThread(() =>
				{
					currentImage.Animate().ScaleX(1).ScaleY(1).SetDuration(tweenTime / 2);
				});				
			}
			else
			{
				timer.Stop(); 
				if (touchStarted) //check if user lifted their finger off before animation is finished
				{
					imageMovable = true;
					dragStartX = touchCurrentX;
					dragStartY = touchCurrentY;
				}
			}
		}

		private void WaitToEnd()
		{
			timer = new Timer();
			timer.Interval = tweenTime;
			timer.Elapsed += Timer_Elapsed2;
			timer.Start();
		}

		private void Timer_Elapsed2(object sender, ElapsedEventArgs e)
		{
			timer.Stop();
			currentImage.SetZ(0);
			touchStarted = false;
		}

		private View GetChildFromDrawOrder(int i)
		{
			return GetChildAt(drawOrder[i]);
		}

		private int GetDrawPosFromChildIndex(int i)
		{
			return drawOrder.IndexOf(i);
		}

		private void RemoveViewFromDrawOrder(int i)
		{
			int removeIndex = drawOrder[i];
			RemoveViewAt(drawOrder[i]);
			drawOrder.RemoveAt(i);	
			
			for (int j = 0; j < drawOrder.Count; j++)
			{
				if (drawOrder[j] > removeIndex)
				{
					drawOrder[j]--;
				}
			}
		}
		/*
		private string getDrawOrder()
		{
			string str = "";
			for (int i = 0; i < drawOrder.Count; i++)
			{
				str += i + " " + drawOrder[i] + "; ";
			}
			return str;
		}*/
	}
}