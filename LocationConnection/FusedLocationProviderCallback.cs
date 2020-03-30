using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Gms.Location;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace LocationConnection
{
	public class FusedLocationProviderCallback : LocationCallback
	{
		readonly BaseActivity activity;
		CommonMethods c;

		public FusedLocationProviderCallback(BaseActivity activity)
		{
			this.activity = activity;
			c = new CommonMethods(activity);
			c.view = activity.MainLayout;
		}

		public override void OnLocationAvailability(LocationAvailability locationAvailability)
		{
			//c.LogActivity("Location availability changed, avaliable: " + locationAvailability.IsLocationAvailable);
		}

		public override async void OnLocationResult(LocationResult result)
		{
			if (result.Locations.Any())
			{
				var location = result.Locations.First();				

				long unixTimestamp = c.Now();			

				if (unixTimestamp != Session.LocationTime) //sometimes we get several location updates at the same time.
				{
					Session.Latitude = location.Latitude;
					Session.Longitude = location.Longitude;
					Session.LocationTime = unixTimestamp;

					c.LogLocation(unixTimestamp + "|" + ((double)Session.Latitude).ToString(CultureInfo.InvariantCulture) + "|" + ((double)Session.Longitude).ToString(CultureInfo.InvariantCulture) + "|" + (BaseActivity.isAppForeground ? 1 : 0));

					Intent intent = new Intent("balintfodor.locationconnection.LocationReceiver");
					intent.PutExtra("time", unixTimestamp);
					intent.PutExtra("latitude", (double)Session.Latitude);
					intent.PutExtra("longitude", (double)Session.Longitude);
					activity.SendBroadcast(intent);

					if (c.IsLoggedIn())
					{
						Session.LastActiveDate = unixTimestamp;
						await c.UpdateLocationSync();
					}
				}				
			}
			else
			{
				c.LogActivity("No location received.");
			}
		}
	}
}