﻿using System;
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
		public int intrinsicWidth;
		public int intrinsicHeight;
		public Bitmap bm;

		float touchStartX, touchStartY;
		float prevTouchX, prevTouchY;
		float startCenterX, startCenterY;
		public float xDist, yDist;
		bool outOfFrameX, outOfFrameY;

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
			//icon = Context.Resources.GetDrawable(Resource.Drawable.logo280);
			this.context = (ProfilePage)context;
			detector = new ScaleGestureDetector(Context, new ScaleListener(this));
		}

		public void SetContent(Bitmap bm)
		{
			this.bm = bm;
			Invalidate();
		}

		//out of frame image is allowed to come closer. Image in frame is not allowed to go out, only by pinching action.
		public override bool OnTouchEvent(MotionEvent e)
		{
			detector.OnTouchEvent(e);

			switch (e.Action)
			{
				case MotionEventActions.Down:

					prevTouchX = touchStartX = e.GetX();
					prevTouchY = touchStartY = e.GetY();
					startCenterX = xDist + Width / 2;
					startCenterY = yDist + Height / 2;

					outOfFrameY = IsOutOfFrameY();
					outOfFrameX = IsOutOfFrameX();

					context.c.CW("ImageEditor_Touch Down " + startCenterX + " " + startCenterY + " " + xDist + " " + yDist + " " + outOfFrameX + " " + outOfFrameY);

					break;
				case MotionEventActions.Move:

					float evX = e.GetX();// + currentCenterX - startCenterX; //coordinates relative to the image's original position
					float evY = e.GetY();// + currentCenterY - startCenterY;

					float newxDist = startCenterX + evX - touchStartX - Width / 2;
					float newyDist = startCenterY + evY - touchStartY - Height / 2;

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

					context.c.CW("ImageEditor_Touch Move " + touchStartX + " " + touchStartY + " " + xDist + " " + yDist + " " + outOfFrameX + " " + newxDist);

					break;
			}
			Invalidate();
			return true;
			//return base.OnTouchEvent(e);
		}
		
		protected override void OnDraw(Canvas canvas)
		{
			base.OnDraw(canvas);

			//context.c.CW("OnDraw scaleFactor " + scaleFactor + " Width " + Width + " Height " + Height + " bmWidth " + bm.Width + " bmHeight " + bm.Height + " intrinsicWidth " + intrinsicWidth + " intrinsicHeight " + intrinsicHeight);

			canvas.Save();
			canvas.Translate(-(scaleFactor - 1) * canvas.Width / 2, -(scaleFactor - 1) * canvas.Height / 2);
			canvas.Scale(scaleFactor, scaleFactor);

			Rect frameToDraw = new Rect(0, 0, bm.Width, bm.Height);
			RectF whereToDraw = new RectF(Width / 2 - intrinsicWidth / 2 + xDist, Height / 2 - intrinsicHeight / 2 + yDist, Width / 2 - intrinsicWidth / 2 + xDist + intrinsicWidth, Height / 2 - intrinsicHeight / 2 + yDist + intrinsicHeight);

			context.c.CW("Where to x " + (Width / 2 - intrinsicWidth / 2 + xDist) + " y " + (Height / 2 - intrinsicHeight / 2 + yDist));

			Paint paint = new Paint();
			paint.AntiAlias = true;
			canvas.DrawBitmap(bm, frameToDraw, whereToDraw, null);

			canvas.Restore();
		}

		public bool IsOutOfFrameY()
		{
			if (yDist <= 0 && (-yDist + context.ImageEditorFrameBorder.Height / scaleFactor / 2) > intrinsicHeight / 2 || yDist > 0 && (yDist + context.ImageEditorFrameBorder.Height / scaleFactor / 2) > intrinsicHeight / 2)
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
			if (xDist <= 0 && (-xDist + context.ImageEditorFrameBorder.Width / scaleFactor / 2) > intrinsicWidth / 2 || xDist > 0 && (xDist + context.ImageEditorFrameBorder.Width / scaleFactor / 2) > intrinsicWidth / 2)
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
				Console.WriteLine("---------------- scaled to " + view.scaleFactor + "---------------");
				view.scaleFactor *= detector.ScaleFactor;
				view.scaleFactor = Math.Max(1f, Math.Min(view.scaleFactor, 3f));
				view.Invalidate();
				return true;
			}
		}
	}	
}