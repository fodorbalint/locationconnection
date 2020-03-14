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
	class SettingsDefault
	{
		//------ View options not stored in userdata
		public const byte DisplaySize = 1;

		public const bool IsMapView = false;
		public const bool SearchOpen  = false;
		public const bool FiltersOpen = false;
		public const bool GeoFiltersOpen = false;

		public static int MapIconSize = 50;
		public static float MapRatio = 0.63f; //10 : 16, 0.625

		public const int ListMapType = 1;
		public const int ProfileViewMapType = 1;
		public const int LocationMapType = 1;

		//------ For not logged-in users

		public const string SearchIn = "all";

		public const string ListType = "public";
		public const string SortBy  = "LastActiveDate";
		public const string OrderBy = "desc";
		public const bool GeoFilter = false;
		public const bool GeoSourceOther = true;

		public const int DistanceLimit = 50;
		public const int ResultsFrom = 1;
		
		public const byte LocationAccuracy = 0;
		public const int InAppLocationRate = 60;

		//Constants

		public const string HostName = "https://locationconnection.appspot.com/";
	}
}