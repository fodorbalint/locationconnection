using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Timers;
using Android.Animation;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;

namespace LocationConnection
{
	public class TouchScrollView : ScrollView
	{
		ProfilePage context;
		Timer t;

		public TouchScrollView(Context context, IAttributeSet attrs) : base(context, attrs) {
			Initialize(context);
		}

		public TouchScrollView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle) {
			Initialize(context);	
		}

		private void Initialize(Context context)
		{
			this.context = (ProfilePage)context;
		}

		//Originally I used this function to get the touch start coordinates, because in HorizontalScrollView's touch cancel event the ScrollView's coordinates are available. 
		//But there is a bug: sometimes when we press the back button inside the ScrollView (but outside the HorizontalScrollView), the button remains pressed, and the program freezes.
		//In debug mode, no error is thrown, output shows normal program operation.
		//As the log shows, OnInterceptTouchEvent is called twice, and OnBackPressed is never reached.
		//context.horizontalCancelled is false, so the OnTouchEvent could not have interfered with it. context.isTouchDown is also false, but there is no reason for button blocking by just setting the 3 variables.
		//
		//Solution: Get start coordinates in HorizontalScrollView's touch down, and save the latest coordinates in touch move to use them when touch cancel is called.
		//Bug is still present.
		//Solution: Move button outside of ScrollView.

		/*public override bool OnInterceptTouchEvent(MotionEvent ev)
		{
			//necessary to set the values, not in ProfileViewActivity, because we need to Y value in relation to this scrollview, this is what gets passed in HorizontalScrollView's motion Cancel.
			if (!context.isTouchDown)
			{
				context.touchStartX = ev.GetX();
				context.touchStartY = ev.GetY();
				context.startScrollX = context.ProfileImageScroll.ScrollX;
			}
			return base.OnInterceptTouchEvent(ev);
		}*/

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
			
			//At first OnResume ImageEditorFrameBorder.Width is 0, but when keyboard opens, it has a value. Parameter will not change back when keyboard closes.

			if (!(!(t is null) && t.Enabled))
			{
				context.c.LogActivity("TouchScrollView OnMeasure border width " + context.ImageEditorFrameBorder.Width + " variable " + context.imageEditorFrameBorderWidth);
				if (context.ImageEditorFrameBorder.Width > context.imageEditorFrameBorderWidth)
				{
					context.imageEditorFrameBorderWidth = context.ImageEditorFrameBorder.Width;
				}
			}
					
		}

		protected override void OnConfigurationChanged(Configuration newConfig) //When changing orientation while in the file picker dialog: On Android 8 and 9, it is gets called once just before OnActivityResult, and once again after OnResume. On Android 10, it is not called after OnResume.
		//OnMeasure is called up until 30 ms after, but ImageEditorFrameBorder still has the old size at that time.
		{
			base.OnConfigurationChanged(newConfig);
			 if (context.active && !(!(t is null) && t.Enabled))
			{
				t = new Timer();
				t.Interval = 10;
				t.Elapsed += context.T_Elapsed;
				t.Start();
			}

			context.imageEditorFrameBorderWidth = 0;
			context.c.LogActivity("OnConfigurationChanged newW " + newConfig.ScreenWidthDp * BaseActivity.pixelDensity + " newH " + newConfig.ScreenHeightDp * BaseActivity.pixelDensity + " border width " + context.ImageEditorFrameBorder.Width + " variable " + context.imageEditorFrameBorderWidth);
		}

		public override bool OnTouchEvent(MotionEvent e)
		{
			switch (e.Action) {
				case MotionEventActions.Down:
					break;
				case MotionEventActions.Move:
					if (context.ImagesUploaded.touchStarted)
					{
						context.ImagesUploaded.Move(e);
						return false;
					}
					break;

				case MotionEventActions.Up:
					if (context.ImagesUploaded.touchStarted)
					{
						context.ImagesUploaded.Up();
						return false;
					}
					break;
				default:
					break;
			}
			return base.OnTouchEvent(e);
		}		
	}
}