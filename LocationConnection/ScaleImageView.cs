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
		private Drawable icon;
		private ScaleGestureDetector detector;
		public float scaleFactor = 1f;
		public Bitmap bm;

		public ScaleImageView(Context context, IAttributeSet attrs) : base(context, attrs)
		{
			Initialize();
		}

		public ScaleImageView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
		{
			Initialize();
		}

		public void Initialize()
		{
			//icon = Context.Resources.GetDrawable(Resource.Drawable.logo280);
			detector = new ScaleGestureDetector(Context, new ScaleListener(this));
		}

		public void SetContent(Bitmap bm)
		{
			this.bm = bm;
			Invalidate();
		}

		public override bool OnTouchEvent(MotionEvent e)
		{
			detector.OnTouchEvent(e);
			Invalidate();
			return true;
			//return base.OnTouchEvent(e);
		}
		
		protected override void OnDraw(Canvas canvas)
		{
			base.OnDraw(canvas);

			Console.WriteLine("OnDraw scaleFactor " + scaleFactor + " Width " + Width + " Height " + Height + " bmWidth " + bm.Width + " bmHeight " + bm.Height);
			canvas.Save();
			canvas.Translate(-(scaleFactor - 1) * canvas.Width / 2, -(scaleFactor - 1) * canvas.Height / 2);
			canvas.Scale(scaleFactor, scaleFactor);

			Rect frameToDraw = new Rect(0, 0, bm.Width, bm.Height);
			RectF whereToDraw = new RectF(0, 0, canvas.Width, canvas.Height);
			canvas.DrawBitmap(bm, frameToDraw, whereToDraw, null);

			canvas.Restore();
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