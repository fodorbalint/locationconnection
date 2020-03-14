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
	class Settings
	{
		//------ View options not stored in userdata
		public static byte? DisplaySize;

		public static bool? IsMapView;
		public static bool? SearchOpen;
		public static bool? FiltersOpen;
		public static bool? GeoFiltersOpen;

		public static int? MapIconSize;
		public static float? MapRatio;

		public static int? ListMapType;
		public static int? ProfileViewMapType;
		public static int? LocationMapType;

		//------ Stored in Settings for not-logged-in users, and in the cloud for registered users.

		public static string SearchTerm;
		public static string SearchIn;

		public static string ListType;
		public static string SortBy;
		public static string OrderBy;
		public static bool? GeoFilter;
		public static bool? GeoSourceOther;

		public static double? OtherLatitude;
		public static double? OtherLongitude;
		public static string OtherAddress;

		public static int? DistanceLimit;
		public static int? ResultsFrom;

		public static byte? LocationAccuracy;
		public static int? InAppLocationRate;
	}
}