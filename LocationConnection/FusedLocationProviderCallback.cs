/*
 
 Location updates should stop when
- disabling on Profile Edit page
- entering background
- user logs in with location off, but device location is enabled
Start when
- autologin
- not logged in, but location is enabled
- enabling location by clicking on map view, or current location filter
- entering foreground
Restart when
- Changing rate in Settings
- Logging in, if rate is different
Location sending to match should stop when
- Logging out
- Turning off location in Profile Edit page
- Entering background

 */
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
		readonly BaseActivity context;

		public FusedLocationProviderCallback(BaseActivity context)
		{
			this.context = context;
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

				long unixTimestamp = context.c.Now();			

				if (unixTimestamp != Session.LocationTime) //sometimes we get several location updates at the same time.
				{
					Session.Latitude = location.Latitude;
					Session.Longitude = location.Longitude;
					Session.LocationTime = unixTimestamp;

					context.c.LogLocation(unixTimestamp + "|" + ((double)Session.Latitude).ToString(CultureInfo.InvariantCulture) + "|" + ((double)Session.Longitude).ToString(CultureInfo.InvariantCulture) + "|1");

					Intent intent = new Intent("balintfodor.locationconnection.LocationReceiver");
					intent.PutExtra("time", unixTimestamp);
					intent.PutExtra("latitude", (double)Session.Latitude);
					intent.PutExtra("longitude", (double)Session.Longitude);
					context.SendBroadcast(intent);

					if (context.c.IsLoggedIn())
					{
						await context.c.UpdateLocationSync();
					}
				}

				if (!BaseActivity.firstLocationAcquired)
				{
					if (ListActivity.locationTimer != null && ListActivity.locationTimer.Enabled)
					{
						ListActivity.locationTimer.Stop();
					}

					BaseActivity.firstLocationAcquired = true;

					context.c.Log("OnLocationResult first location");

					if (BaseActivity.visibleContext is ListActivity)
					{
						((ListActivity)BaseActivity.visibleContext).LoadListStartup();
					}
				}
			}
			else
			{
				context.c.LogActivity("No location received.");
			}
		}
	}
}