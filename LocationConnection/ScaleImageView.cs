using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace LocationConnection
{
	public class ScaleImageView : ImageView
	{
		private ProfilePage context;
		private ScaleGestureDetector detector;
		public float scaleFactor;
		public float intrinsicWidth;
		public float intrinsicHeight;
		public Bitmap bm;

		float touchStartX, touchStartY;
		float prevTouchX, prevTouchY;
		float startCenterX, startCenterY;
		public float xDist, yDist;
		bool outOfFrameX, outOfFrameY;
		private bool moveAllowed;
		private bool showScaleError;
		private bool showMoveError;

		public ScaleImageView(Context context, IAttributeSet attrs) : base(context, attrs)
		{
			Initialize(context);
		}

		public ScaleImageView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
		{
			Initialize(context);
		}

		public void Initialize(Context context)
		{
			this.context = (ProfilePage)context;
		}

		public void SetContent(Bitmap bm)
		{
			this.bm = bm;
			context.c.Log("SetContent bm " + bm + " this.bm " + this.bm);
			Invalidate();
			showScaleError = true;
			showMoveError = true;
			//it is important to set the detector here, not in Initialize.
			//With this sequence: RegisterActivity -> Home button -> Open app (ListActivity) -> ProfileViewActivity -> Back -> RegisterActivity -> Open file selector, we would get and ObjectDisposedException
			//Only happens when I install the app from apk or Play Store (not though VS Release), because pressing the home button and opening the app recreates the ListActivity, but not in latter case.
			//a disposed object would throw exception on logging detector (converting it to string), and detector is null gives false 
			detector = new ScaleGestureDetector(Context, new ScaleListener(this));
		}

		//out of frame image is allowed to come closer. Image in frame is not allowed to go out, only by pinching action.
		public override bool OnTouchEvent(MotionEvent e)
		{
			try
			{
				try
				{
					detector.OnTouchEvent(e);
				}
				catch (Exception ex)
				{
					if (showScaleError)
					{ 
						showScaleError = false;
						context.c.ErrorAlert(context.res.GetString(Resource.String.ImageScaleError));
						context.c.ReportErrorSilent("OnTouchEvent ImageScaleError " + ex);
					}
				}


				if (e.PointerCount > 1)
				{
					moveAllowed = false;
				}

				switch (e.Action)
				{
					case MotionEventActions.Down:

						prevTouchX = touchStartX = e.GetX();
						prevTouchY = touchStartY = e.GetY();
						startCenterX = xDist + Width / 2;
						startCenterY = yDist + Height / 2;

						outOfFrameY = IsOutOfFrameY();
						outOfFrameX = IsOutOfFrameX();

						moveAllowed = true;

						break;
					case MotionEventActions.Move:
						if (!moveAllowed)
						{
							return true;
						}

						float evX = e.GetX();// + currentCenterX - startCenterX; //coordinates relative to the image's original position
						float evY = e.GetY();// + currentCenterY - startCenterY;

						float newxDist = startCenterX + (evX - touchStartX) / scaleFactor - Width / 2;
						float newyDist = startCenterY + (evY - touchStartY) / scaleFactor - Height / 2;

						//context.c.CW("ImageEditor_Touch Move " + newxDist + " " + startCenterX + " " + touchStartX + " " + );

						if (outOfFrameY && (yDist <= 0 && newyDist < yDist || yDist > 0 && newyDist > yDist)) //out of frame, new distance is greater than previous
						{
							touchStartY += evY - prevTouchY;
						}
						else if (outOfFrameY) //new distance is smaller
						{
							if (yDist <= 0 && newyDist > (intrinsicHeight - context.ImageEditorFrameBorder.Height / scaleFactor) / 2) //making sure not to go out of frame the opposite end. (when the image is scaled back to 1:1, and moved fast, it can happen)
							{
								yDist = (intrinsicHeight - context.ImageEditorFrameBorder.Height / scaleFactor) / 2;
								touchStartY += newyDist - (intrinsicHeight - context.ImageEditorFrameBorder.Height / scaleFactor) / 2; //moving start touch position, so an opposite move will react immediately
							}
							else if (yDist > 0 && newyDist < -(intrinsicHeight - context.ImageEditorFrameBorder.Height / scaleFactor) / 2)
							{
								yDist = -(intrinsicHeight - context.ImageEditorFrameBorder.Height / scaleFactor) / 2;
								touchStartY += newyDist - -(intrinsicHeight - context.ImageEditorFrameBorder.Height / scaleFactor) / 2;
							}
							else
							{
								yDist = newyDist;
							}

							outOfFrameY = IsOutOfFrameY();
						}
						else
						{
							yDist = newyDist;

							if (yDist <= 0 && (-yDist + context.ImageEditorFrameBorder.Height / scaleFactor / 2) > intrinsicHeight / 2) //going out of frame too high
							{

								yDist = -(intrinsicHeight - context.ImageEditorFrameBorder.Height / scaleFactor) / 2;
								touchStartY += newyDist - -(intrinsicHeight - context.ImageEditorFrameBorder.Height / scaleFactor) / 2;
							}
							else if (yDist > 0 && (yDist + context.ImageEditorFrameBorder.Height / scaleFactor / 2) > intrinsicHeight / 2) //going out of frame too low
							{
								yDist = (intrinsicHeight - context.ImageEditorFrameBorder.Height / scaleFactor) / 2;
								touchStartY += newyDist - (intrinsicHeight - context.ImageEditorFrameBorder.Height / scaleFactor) / 2;
							}
							// else in frame 
						}


						if (outOfFrameX && (xDist <= 0 && newxDist < xDist || xDist > 0 && newxDist > xDist)) //out of frame, new is distance greater than previous
						{
							touchStartX += evX - prevTouchX;
						}
						else if (outOfFrameX)
						{
							if (xDist <= 0 && newxDist > (intrinsicWidth - context.ImageEditorFrameBorder.Width / scaleFactor) / 2) //making sure not to go out of frame the opposite end. (when the image is scaled back to 1:1, and moved fast, it can happen)
							{
								xDist = (intrinsicWidth - context.ImageEditorFrameBorder.Width / scaleFactor) / 2;
								touchStartX += newxDist - (intrinsicWidth - context.ImageEditorFrameBorder.Width / scaleFactor) / 2; //moving start touch position, so an opposite move will react immediately
							}
							else if (xDist > 0 && newxDist < -(intrinsicWidth - context.ImageEditorFrameBorder.Width / scaleFactor) / 2)
							{
								xDist = -(intrinsicWidth - context.ImageEditorFrameBorder.Width / scaleFactor) / 2;
								touchStartX += newxDist - -(intrinsicWidth - context.ImageEditorFrameBorder.Width / scaleFactor) / 2; //moving start touch position, so an opposite move will react immediately
							}
							else
							{
								xDist = newxDist;
							}

							outOfFrameX = IsOutOfFrameX();
						}
						else
						{
							xDist = newxDist;

							if (xDist <= 0 && (-xDist + context.ImageEditorFrameBorder.Width / scaleFactor / 2) > intrinsicWidth / 2) //going out of frame too left
							{
								xDist = -(intrinsicWidth - context.ImageEditorFrameBorder.Width / scaleFactor) / 2;
								touchStartX += newxDist - -(intrinsicWidth - context.ImageEditorFrameBorder.Width / scaleFactor) / 2; //moving start touch position, so an opposite move will react immediately
							}
							else if (xDist > 0 && (xDist + context.ImageEditorFrameBorder.Width / scaleFactor / 2) > intrinsicWidth / 2) //going out of frame too right
							{
								xDist = (intrinsicWidth - context.ImageEditorFrameBorder.Width / scaleFactor) / 2;
								touchStartX += newxDist - (intrinsicWidth - context.ImageEditorFrameBorder.Width / scaleFactor) / 2;
							}
							// else in frame
						}

						prevTouchX = evX;
						prevTouchY = evY;

						//context.c.CW("ImageEditor_Touch Move " + touchStartX + " " + touchStartY + " " + xDist + " " + yDist + " " + outOfFrameX + " " + newxDist);

						break;
				}
				Invalidate();
			}
			catch (Exception ex1)
			{
				if (showMoveError) //to make sure, it is only called once
				{
					showMoveError = false;
					context.c.ErrorAlert(context.res.GetString(Resource.String.ImageMoveError));
					context.c.ReportErrorSilent("OnTouchEvent ImageMoveError: action " + e.Action + " x " + e.GetX() + " y " + e.GetY() + " count " + e.PointerCount + " --- " + ex1);
				}
			}

			return true;
		}

		protected override async void OnDraw(Canvas canvas)
		{
			base.OnDraw(canvas); 
			
			try
			{
				//context.c.CW("OnDraw canvas + " + canvas + "scaleFactor " + scaleFactor + " Width " + Width + " Height " + Height + " bmWidth " + bm.Width + " bmHeight " + bm.Height + " intrinsicWidth " + intrinsicWidth + " intrinsicHeight " + intrinsicHeight);

				canvas.Save();
				canvas.Translate(-(scaleFactor - 1) * canvas.Width / 2, -(scaleFactor - 1) * canvas.Height / 2);
				canvas.Scale(scaleFactor, scaleFactor);

				Rect frameToDraw = new Rect(0, 0, bm.Width, bm.Height);
				RectF whereToDraw = new RectF((float)Width / 2 - intrinsicWidth / 2 + xDist, (float)Height / 2 - intrinsicHeight / 2 + yDist, (float)Width / 2 - intrinsicWidth / 2 + xDist + intrinsicWidth, (float)Height / 2 - intrinsicHeight / 2 + yDist + intrinsicHeight);

				//context.c.CW("Where to x " + ((float)Width / 2 - intrinsicWidth / 2 + xDist) + " y " + ((float)Height / 2 - intrinsicHeight / 2 + yDist) + " right " + ((float)Width / 2 - intrinsicWidth / 2 + xDist + intrinsicWidth) + " bottom " + ((float)Height / 2 - intrinsicHeight / 2 + yDist + intrinsicHeight) + " BorderY " + (context.ImageEditorFrameBorder.GetY() - context.ImageEditor.GetY()) + " BorderH " + context.ImageEditorFrameBorder.Height + " scaleFactor " + scaleFactor);

				Paint paint = new Paint
				{
					AntiAlias = true
				};
				canvas.DrawBitmap(bm, frameToDraw, whereToDraw, paint);
				canvas.Restore();

				//context.c.CW("OnDraw scaleFactor " + scaleFactor + " Width " + Width + " Height " + Height + " bmWidth " + bm.Width + " bmHeight " + bm.Height + " intrinsicWidth " + intrinsicWidth + " intrinsicHeight " + intrinsicHeight);
			}
			catch (Exception ex)
			{
				//Java.Lang.RuntimeException: Canvas: trying to draw too large(127844352bytes) bitmap.
				if (ex is Java.Lang.RuntimeException)
				{
					context.c.Log("OnDraw bitmap too large");
					if (bm is null) //bm may be null now, but it was okay up until canvas.DrawBitmap();
					{
						context.c.Log("Bitmap is null, ProfilePage.bm: " + ProfilePage.bm);
						bm = await ProfilePage.GetBitmap(context);
						if (bm is null) //should not happen, because the file was loaded successfully before
						{
							context.rc.CloseEditor(); //error is displayed in the function
							return;
						}
					}

					//will be halved at each error until the image is of manageable size. Hope that there are no other RuntimeException types here, but just in that case, we will exit if width falls below 1000; 

					ProfilePage.bmWidth /= 2;
					ProfilePage.bmHeight /= 2;

					if (ProfilePage.bmWidth < 1000 && ProfilePage.bmHeight < 1000)
					{
						context.c.Log("New dimensions under threshold: " + ProfilePage.bmWidth + " x " + ProfilePage.bmHeight);
						context.rc.CloseEditor();
						await context.c.ErrorAlert(context.res.GetString(Resource.String.OtherBitmapError).Replace("[error]", ex.Message));
						context.c.ReportErrorSilent("OnDraw error " + ex.Message + " " + ex.StackTrace);
						return;
					}

					context.c.Log("Scaling bitmap to half: " + ProfilePage.bmWidth + " x " + ProfilePage.bmHeight);
					SetContent(Bitmap.CreateScaledBitmap(bm, ProfilePage.bmWidth, ProfilePage.bmHeight, false));
				}
				else
				{
					context.rc.CloseEditor();
					await context.c.ErrorAlert(context.res.GetString(Resource.String.OtherBitmapError).Replace("[error]", ex.Message));
					context.c.ReportErrorSilent("OnDraw error " + ex.Message + " " + ex.StackTrace);
				}
			}
		}

		public bool IsOutOfFrameY() //0.001 is to take account for the comparison of float numbers. Touch move resolution is larger.
		{
			if (yDist <= 0)
			{
				//context.c.CW("IsOutofFrameY " + (-yDist + context.ImageEditorFrameBorder.Height / scaleFactor / 2) + " " + intrinsicHeight / 2);
			}
			else
			{
				//context.c.CW("IsOutofFrameY " + (yDist + context.ImageEditorFrameBorder.Height / scaleFactor / 2) + " " + intrinsicHeight / 2);
			}
			
			if (yDist <= 0 && (-yDist + context.ImageEditorFrameBorder.Height / scaleFactor / 2) > intrinsicHeight / 2 + 0.001 || yDist > 0 && (yDist + context.ImageEditorFrameBorder.Height / scaleFactor / 2) > intrinsicHeight / 2 + 0.001)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public bool IsOutOfFrameX()
		{
			if (xDist <= 0)
			{
				//context.c.CW("IsOutofFrameX " + (-xDist + context.ImageEditorFrameBorder.Width / scaleFactor / 2) + " " + intrinsicWidth / 2);
			}
			else
			{
				//context.c.CW("IsOutofFrameX " + (xDist + context.ImageEditorFrameBorder.Width / scaleFactor / 2) + " " + intrinsicWidth / 2);
			}

			if (xDist <= 0 && (-xDist + context.ImageEditorFrameBorder.Width / scaleFactor / 2) > intrinsicWidth / 2 + 0.001 || xDist > 0 && (xDist + context.ImageEditorFrameBorder.Width / scaleFactor / 2) > intrinsicWidth / 2 + 0.001)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		private class ScaleListener : ScaleGestureDetector.SimpleOnScaleGestureListener
		{
			private readonly ScaleImageView view;

			public ScaleListener(ScaleImageView view)
			{
				this.view = view;
			}

			public override bool OnScale(ScaleGestureDetector detector)
			{
				view.scaleFactor *= detector.ScaleFactor;
				view.scaleFactor = Math.Max(1f, Math.Min(view.scaleFactor, 3f));
				view.Invalidate();
				return true;
			}
		}
	}	
}