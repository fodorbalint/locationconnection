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
	class Session
	{
		#pragma warning disable CS0649

		//------ login data
		public static string SessionID;

		public static int? ID;
		public static byte? Sex;
		public static string Email;
		public static string Username;
		public static string Name;
		public static string[] Pictures;
		public static string Description;

		public static long? RegisterDate;
		public static long? LastActiveDate;
		public static float? ResponseRate;

		public static double? Latitude;
		public static double? Longitude;
		public static long? LocationTime;

		public static byte? SexChoice;	
		public static bool? UseLocation;
		public static bool? BackgroundLocation;
		public static byte? LocationShare;
		public static byte? DistanceShare;
		public static bool? ActiveAccount;		

		//------ Options stored in database / local settings 

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
		public static int? BackgroundLocationRate;

		//------ Logged in user only

		public static bool? MatchInApp;
		public static bool? MessageInApp;
		public static bool? UnmatchInApp;
		public static bool? RematchInApp;
		public static bool? MatchBackground;
		public static bool? MessageBackground;
		public static bool? UnmatchBackground;
		public static bool? RematchBackground;		

		//------

		public static long? LastDataRefresh;
		public static byte LastSearchType;
		public static string SnackMessage;
		public static MatchItem CurrentMatch; //not necessarily a match

		#pragma warning restore CS0649
	}
}