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
using AndroidX.Lifecycle;
using Java.Interop;

namespace LocationConnection
{
	[Application]
	public class LocationConnectionApp : Application, ILifecycleObserver
	{
		[Lifecycle.Event.OnStop]
		[Export]
		public void Stopped()
		{
			CommonMethods.LogStatic("Entered background");

			if (!string.IsNullOrEmpty(BaseActivity.locationUpdatesTo))
			{
				BaseActivity.EndLocationShare();
				BaseActivity.locationUpdatesTo = null; //stop real-time location updates when app goes to background
			}
			BaseActivity.locationUpdatesFrom = null;
			BaseActivity.locationUpdatesFromData = null;

			BaseActivity.StopLocationUpdates();
		}

		[Lifecycle.Event.OnStart]
		[Export]
		public void Started()
		{
			CommonMethods.LogStatic("Entered foreground");
		}

		public LocationConnectionApp(IntPtr handle, Android.Runtime.JniHandleOwnership ownerShip) : base(handle, ownerShip)
		{
		}

		public override void OnCreate()
		{
			base.OnCreate();

			CommonMethods.LogStatic("Application OnCreate");
			ProcessLifecycleOwner.Get().Lifecycle.AddObserver(this);
		}
	}
}