using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Timers;
using Android.Animation;
using Android.App;
using Android.Content;
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
	public class TouchConstraintLayout : ConstraintLayout
	{
		TouchActivity context;

		public TouchConstraintLayout(Context context, IAttributeSet attrs) : base(context, attrs)
		{
			Initialize(context);
		}

		public TouchConstraintLayout(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
		{
			Initialize(context);
		}

		private void Initialize(Context context)
		{
			this.context = (TouchActivity)context;
		}

		public override bool OnTouchEvent(MotionEvent e)
		{
			switch (e.Action)
			{
				case MotionEventActions.Down:
					if (context.ScrollDown(e))
					{
						return base.OnTouchEvent(e);
					}
					else
					{
						return false;
					}

				case MotionEventActions.Move:
					return context.ScrollMove(e);

				case MotionEventActions.Up:
					return context.ScrollUp();
			}
			return base.OnTouchEvent(e);
		}
	}
}