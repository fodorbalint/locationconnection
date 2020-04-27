using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace LocationConnection
{
	public interface TouchActivity
	{
		public bool ScrollDown(MotionEvent e);
		public bool ScrollMove(MotionEvent e);
		public bool ScrollUp();

	}
}