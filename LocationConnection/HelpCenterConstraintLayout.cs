using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Runtime;
using Android.Support.Constraints;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace LocationConnection
{
	public class HelpCenterConstraintLayout : ConstraintLayout
	{
		HelpCenterActivity context;

		public HelpCenterConstraintLayout(Context context, IAttributeSet attrs) : base(context, attrs)
		{
			Initialize(context);
		}

		public HelpCenterConstraintLayout(Context context, IAttributeSet attrs, int defStyle) :	base(context, attrs, defStyle)
		{
			Initialize(context);
		}

		private void Initialize(Context context)
		{
			this.context = (HelpCenterActivity)context;
		}

		protected override void OnConfigurationChanged(Configuration newConfig)
		{
			//newConfig.ScreenWidthDp gives 2557 instead of 2560 and 1598 instead of 1600

			context.GetScreenMetrics(false);

			int pic = context.PosToPic(context.TutorialFrame.ScrollX);

			for (int i = 0; i < context.TutorialFrame.ChildCount; i++)
			{
				context.TutorialFrame.GetChildAt(i).LayoutParameters.Width = BaseActivity.screenWidth;
			}

			int newScrollX = pic * BaseActivity.screenWidth;
			context.TutorialFrame.ScrollX = newScrollX;

			base.OnConfigurationChanged(newConfig);
		}
	}
}