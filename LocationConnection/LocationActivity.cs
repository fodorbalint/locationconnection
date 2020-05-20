using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Timers;
using Android.App;
using Android.Content;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Support.Constraints;
using Android.Support.V4.Content;
using Android.Views;
using Android.Widget;

namespace LocationConnection
{
	[Activity(MainLauncher = false, ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
	public class LocationActivity : BaseActivity, IOnMapReadyCallback
	{
		ImageButton LocationBack;
		public SupportMapFragment LocationHistoryMap;
		Button MapStreet, MapSatellite;
		public ListView LocationHistoryList;
		View RippleLocation;

		List<LocationItem> locationList;
		bool mapLoaded;
		GoogleMap thisMap;
		Marker circle;
		int selectedPos;
		LocationListAdapter adapter;
		LocationReceiver locationReceiver;
		int circleSize = 30;

		bool rippleRunning;
		Timer rippleTimer;

		int icMapmarker;

		protected override void OnCreate(Bundle savedInstanceState)
		{
			try { 
				base.OnCreate(savedInstanceState);
				if (!ListActivity.initialized) { return; }

				if (Settings.DisplaySize == 1)
				{
					SetContentView(Resource.Layout.activity_location_normal);
					icMapmarker = Resource.Drawable.ic_mapmarker_normal;
				}
				else
				{
					SetContentView(Resource.Layout.activity_location_small);
					icMapmarker = Resource.Drawable.ic_mapmarker_small;
				}

				MainLayout = FindViewById<ConstraintLayout>(Resource.Id.MainLayout);
				LocationHistoryMap = (SupportMapFragment)SupportFragmentManager.FindFragmentById(Resource.Id.LocationHistoryMap);
				MapStreet = FindViewById<Button>(Resource.Id.MapStreet);
				MapSatellite = FindViewById<Button>(Resource.Id.MapSatellite);
				LocationHistoryList = FindViewById<ListView>(Resource.Id.LocationHistoryList);
				RippleLocation= FindViewById<View>(Resource.Id.RippleLocation);
				LocationBack = FindViewById<ImageButton>(Resource.Id.LocationBack);

				LocationHistoryMap.GetMapAsync(this);
				c.view = MainLayout;
				mapLoaded = false;
				locationReceiver = new LocationReceiver();

				LocationBack.Click += LocationBack_Click;
				LocationBack.Touch += LocationBack_Touch;
				LocationHistoryList.ItemClick += LocationHistoryList_ItemClick;
				MapStreet.Click += MapStreet_Click;
				MapSatellite.Click += MapSatellite_Click;
			}
			catch (Exception ex)
			{
				c.ReportErrorSilent(ex.Message + System.Environment.NewLine + ex.StackTrace);
			}
		}

		protected override void OnResume()
		{
			try { 
				base.OnResume();
				if (!ListActivity.initialized) { return; }

				RegisterReceiver(locationReceiver, new IntentFilter("balintfodor.locationconnection.LocationReceiver"));

				locationList = new List<LocationItem>();

				if (File.Exists(c.locationLogFile))
				{
					string[] fileLines = File.ReadAllLines(c.locationLogFile);

					for(int i = fileLines.Length - 1; i >= 0; i--)
					{
						string line = fileLines[i];
						LocationItem item = new LocationItem();

						int sep1Pos = line.IndexOf('|');
						int sep2Pos = line.IndexOf('|', sep1Pos + 1);
						int sep3Pos = line.IndexOf('|', sep2Pos + 1);

						item.time = long.Parse(line.Substring(0, sep1Pos));
						item.latitude = double.Parse(line.Substring(sep1Pos + 1, sep2Pos - sep1Pos - 1), CultureInfo.InvariantCulture);
						item.longitude = double.Parse(line.Substring(sep2Pos + 1, sep3Pos - sep2Pos - 1), CultureInfo.InvariantCulture);
						item.inApp = (line.Substring(sep3Pos + 1) == "0") ? false : true;
						item.isSelected = false;
						locationList.Add(item);
					}
					locationList[0].isSelected = true;
					selectedPos = 0;

					if (mapLoaded)
					{
						thisMap.Clear();
						SetMap();
					}
				}
				else
				{
					c.Snack(Resource.String.NoLocationRecords);
				}

				adapter = new LocationListAdapter(this, locationList);
				LocationHistoryList.Adapter = adapter;
			}
			catch (Exception ex)
			{
				c.ReportErrorSilent(ex.Message + System.Environment.NewLine + ex.StackTrace);
			}
		}

		protected override void OnPause()
		{
			base.OnPause();
			if (!ListActivity.initialized) { return; }

			UnregisterReceiver(locationReceiver);

			if (!(thisMap is null) && thisMap.MapType != Settings.LocationMapType)
			{
				Settings.LocationMapType = (byte)thisMap.MapType;
				c.SaveSettings();
			}
		}

		public void OnMapReady(GoogleMap map) //called after onresume
		{
			mapLoaded = true;
			map.UiSettings.ZoomControlsEnabled = true;
			map.SetPadding(0, (int)(42 * pixelDensity), 0, 0);

			map.MapType = (int)Settings.LocationMapType;
			if (Settings.LocationMapType == GoogleMap.MapTypeNormal)
			{
				MapStreet.SetBackgroundResource(Resource.Drawable.maptype_activeLeft);
				MapSatellite.SetBackgroundResource(Resource.Drawable.maptype_passiveRight);
			}
			else
			{
				MapStreet.SetBackgroundResource(Resource.Drawable.maptype_passiveLeft);
				MapSatellite.SetBackgroundResource(Resource.Drawable.maptype_activeRight);
			}
			MapStreet.Visibility = ViewStates.Visible;
			MapSatellite.Visibility = ViewStates.Visible;

			thisMap = map;
			if (!(locationList is null) && locationList.Count > 0)
			{
				SetMap();
			}
		}

		private void MapStreet_Click(object sender, EventArgs e)
		{
			if (mapLoaded)
			{
				thisMap.MapType = GoogleMap.MapTypeNormal;
				MapStreet.SetBackgroundResource(Resource.Drawable.maptype_activeLeft);
				MapSatellite.SetBackgroundResource(Resource.Drawable.maptype_passiveRight);
			}
		}

		private void MapSatellite_Click(object sender, EventArgs e)
		{
			if (mapLoaded)
			{
				thisMap.MapType = GoogleMap.MapTypeHybrid;
				MapStreet.SetBackgroundResource(Resource.Drawable.maptype_passiveLeft);
				MapSatellite.SetBackgroundResource(Resource.Drawable.maptype_activeRight);
			}
		}

		public void SetMap()
		{
			double latitude = locationList[0].latitude;
			double longitude = locationList[0].longitude;

			LatLng location = new LatLng(latitude, longitude);

			MoveMap(location, true);
			AddCircle(location);

			for(int i = locationList.Count - 1; i > 0; i--)
			{
				long unixTimestamp = c.Now();
				long time = locationList[i - 1].time;
				float ratio = ((float)unixTimestamp - time) / Constants.LocationKeepTime;
				Color color = Color.Argb(255, (int)(ratio * 255), (int)((1 - ratio) * 255), 0);

				AddLine(new LatLng(locationList[i].latitude, locationList[i].longitude), new LatLng(locationList[i - 1].latitude, locationList[i - 1].longitude), color);
			}
		}

		private void AddLine(LatLng location1, LatLng location2, Color color)
		{
			try
			{
				PolylineOptions p = new PolylineOptions();
				p.Add(location1, location2);
				p.InvokeWidth(3 * pixelDensity);
				p.InvokeJointType(JointType.Bevel);
				p.InvokeColor(Color.Black);
				thisMap.AddPolyline(p);

				p = new PolylineOptions();
				p.Add(location1, location2);
				p.InvokeWidth(2 * pixelDensity);
				p.InvokeJointType(JointType.Bevel);
				p.InvokeColor(color);
				thisMap.AddPolyline(p);
			}
			catch
			{
			}
		}

		private void AddCircle(LatLng location)
		{
			try
			{
				if (!(circle is null))
				{
					circle.Remove();
				}

				Drawable drawable = ContextCompat.GetDrawable(this, icMapmarker);

				Bitmap bitmap = Bitmap.CreateBitmap((int)(circleSize * pixelDensity), (int)(circleSize * pixelDensity), Bitmap.Config.Argb8888);
				Canvas canvas = new Canvas(bitmap);
				drawable.SetBounds(0, 0, canvas.Width, canvas.Height);
				drawable.Draw(canvas);

				MarkerOptions markerOptions = new MarkerOptions();
				markerOptions.SetPosition(location);
				markerOptions.SetIcon(BitmapDescriptorFactory.FromBitmap(bitmap));
				markerOptions.Anchor(0.5f, 0.5f);
				circle = thisMap.AddMarker(markerOptions);
			}
			catch
			{
			}
		}

		private void MoveMap(LatLng location, bool isFirst)
		{
			try
			{
				if (isFirst)
				{
					thisMap.MoveCamera(CameraUpdateFactory.NewLatLngZoom(location, 14));
				}
				else
				{
					thisMap.MoveCamera(CameraUpdateFactory.NewLatLng(location));
				}
			}
			catch
			{
			}
		}

		private void LocationBack_Click(object sender, EventArgs e)
		{
			OnBackPressed();
		}

		private void LocationBack_Touch(object sender, View.TouchEventArgs e)
		{
			if (e.Event.Action == MotionEventActions.Down && !rippleRunning)
			{
				RippleLocation.Alpha = 1;

				RippleLocation.Animate().ScaleX(2f).ScaleY(2f).SetDuration(tweenTime / 2).Start();
				rippleTimer = new Timer();
				rippleTimer.Interval = tweenTime / 2;
				rippleTimer.Elapsed += T_Elapsed1;
				rippleTimer.Start();
				rippleRunning = true;
			}
			e.Handled = false;
		}

		private void T_Elapsed1(object sender, ElapsedEventArgs e)
		{
			rippleTimer.Stop();
			RunOnUiThread(() => {
				RippleLocation.Animate().Alpha(0).SetDuration(tweenTime / 2).Start();
			});
			rippleTimer.Interval = tweenTime / 2;
			rippleTimer.Elapsed += T_Elapsed2;
			rippleTimer.Start();
		}

		private void T_Elapsed2(object sender, ElapsedEventArgs e)
		{
			rippleTimer.Stop();
			RunOnUiThread(() => {
				RippleLocation.ScaleX = 1;
				RippleLocation.ScaleY = 1;
			});
			rippleRunning = false;
		}

		private void LocationHistoryList_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
		{
			if (e.Position != selectedPos)
			{
				locationList[selectedPos].isSelected = false;
				locationList[e.Position].isSelected = true;
				selectedPos = e.Position;

				LatLng location = new LatLng(locationList[e.Position].latitude, locationList[e.Position].longitude);
				MoveMap(location, false);
				AddCircle(location);
			}
			else
			{
				if (locationList[selectedPos].isSelected == false)
				{
					locationList[selectedPos].isSelected = true;

					LatLng location = new LatLng(locationList[e.Position].latitude, locationList[e.Position].longitude);
					MoveMap(location, false);
					AddCircle(location);
				}
				else
				{
					locationList[selectedPos].isSelected = false;
					if (!(circle is null))
					{
						circle.Remove();
					}
				}
			}
			adapter.NotifyDataSetChanged();
		}

		public void AddItem(long time, double latitude, double longitude)
		{
			LatLng location = new LatLng(latitude, longitude);
			LocationItem item = new LocationItem();
			item.time = time;
			item.latitude = latitude;
			item.longitude = longitude;
			item.inApp = true;

			if (selectedPos == 0 && locationList.Count > 0)
			{
				item.isSelected = true;
				locationList[selectedPos].isSelected = false;
				AddCircle(location);
				MoveMap(location, false);
			}
			else if (locationList.Count == 0)
			{
				item.isSelected = true;
				AddCircle(location);
				MoveMap(location, true);
			}
			else
			{
				item.isSelected = false;
				selectedPos++;
			}

			locationList.Insert(0, item);
			adapter.NotifyDataSetChanged();

			if (locationList.Count >= 2)
			{
				AddLine(location, new LatLng(locationList[1].latitude, locationList[1].longitude), Color.Argb(255, 0, 255, 0));
			}
		}
	}
}