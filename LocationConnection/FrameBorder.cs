using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace LocationConnection
{
	public class FrameBorder : View
	{
		ProfilePage context;

		public FrameBorder(Context context, IAttributeSet attrs) :
			base(context, attrs)
		{
			this.context = (ProfilePage)context;
		}

		public FrameBorder(Context context, IAttributeSet attrs, int defStyle) :
			base(context, attrs, defStyle)
		{
			this.context = (ProfilePage)context;
		}

		/* testing sequence:
		- Open RegisterActivity in portrait
		- Bring keyboard up
		- Select image (frame is right)
		- Select another image, and rotate gallery meanwhile (frame is smaller than picture)
		- Select another image while staying in landscape (frame is right now)
		*/

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec) // we need to only detect size changes from OnResume or screen rotation, not from opening the keyboard
		{
			base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
			
			if (!context.c.IsKeyboardOpen(context.MainLayout))
			{
				context.c.LogActivity("OnMeasure setting var, Width " + Width + " MeasuredWidth " + MeasuredWidth);
				context.imageEditorFrameBorderWidth = MeasuredWidth;
			}
			else
			{
				context.c.LogActivity("OnMeasure not setting var, Width " + Width + " MeasuredWidth " + MeasuredWidth);
			}
		}
			   
		protected override void OnConfigurationChanged(Configuration newConfig) //When changing orientation while in the file picker dialog: On Android 8 and 9, it is gets called once just before OnActivityResult, and once again after OnResume. On Android 10, it is not called after OnResume.
																				//OnMeasure is called up until 30 ms after, but ImageEditorFrameBorder still has the old size at that time.
		{
			base.OnConfigurationChanged(newConfig);

			context.c.LogActivity("OnConfigurationChanged newW " + newConfig.ScreenWidthDp * BaseActivity.pixelDensity + " newH " + newConfig.ScreenHeightDp * BaseActivity.pixelDensity + " border width " + context.ImageEditorFrameBorder.Width + " variable " + context.imageEditorFrameBorderWidth);
		}
	}
}