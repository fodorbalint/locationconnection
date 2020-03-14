using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Views;
using Android.Widget;

namespace LocationConnection
{
	[BroadcastReceiver(Enabled = true, Exported = false)]
	public class LocationReceiver : BroadcastReceiver
	{
		public override void OnReceive(Context context, Intent intent)
		{
			long time = intent.GetLongExtra("time", 0);
			double latitude = intent.GetDoubleExtra("latitude", 0);
			double longitude = intent.GetDoubleExtra("longitude", 0);

			if (context is LocationActivity)
			{
				((LocationActivity)context).AddItem(time, latitude, longitude);
			}
			else if (context is ProfileViewActivity)
			{
				((ProfileViewActivity)context).UpdateLocationSelf(time, latitude, longitude);
			}
			
		}
	}
}