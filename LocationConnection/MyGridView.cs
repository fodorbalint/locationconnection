using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace LocationConnection
{
	public class MyGridView : GridView
	{
		public MyGridView(Context context, IAttributeSet attrs) : base(context, attrs) { }

		public MyGridView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle) { }		

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			int expandSpec = MeasureSpec.MakeMeasureSpec(MeasuredSizeMask, MeasureSpecMode.AtMost);
			base.OnMeasure(widthMeasureSpec, expandSpec);
		}
	}
}