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
    class Constants
    {
		public const int LocationTimeout = 5000;
		public const int DistanceLimitMax = 100;
		public const int RequestTimeout = 10000;
		public const int MaxNumPictures = 9;
		public const string UploadFolder = "userimages";
		public const string TempUploadFolder = "userimagestemp";
		public const string UploadFolderTest = "userimagestest";
		public const string TempUploadFolderTest = "userimagestesttemp";
		public const string TutorialFolder = "tutorial";
		public const int tutorialInterval = 5000;
		public const int SmallImageSize = 480;
		public const int LargeImageSize = 1440;
		public const string EmailFormat = @"^\w+([.+-]?\w+)*@[a-zA-Z0-9]+([.-]?[a-zA-Z0-9]+)*\.[a-zA-z0-9]{2,4}$";
		public const string CHANNEL_ID = "my_notification_channel";
		public const int NOTIFICATION_ID = 100;		
		public const string loadingImage = "loadingimage.png";
		public const string noImage = "noimage.png";
		public const string noImageHD = "noimage_hd.png";
		public const int DataRefreshInterval = 60; //minimum seconds
		public const int DistanceChangeRefreshDelay = 300;
		public const int MaxGoogleMapDistance = 20015;
		public const int ActivityChangeInterval = 3000; //about 2 seconds needed from resuming an activity and entering to ListActivity
		public const byte SearchType_Filter = 0;
		public const byte SearchType_Search = 1;
		public const byte MapTypeStreet = 1;
		public const byte MapTypeSatellite = 4;
		public const byte ProfileViewType_List = 0;
		public const byte ProfileViewType_Self = 1;
		public const byte ProfileViewType_Standalone = 2;
		public const int MapIconSizeMin = 10;
		public const int MapIconSizeMax = 200;
		public const float MapRatioMin = 0.46f; //0.4615f; //9 : 19.5
		public const float MapRatioMax = 2.17f; //2.1667f; //9 : 19.5
		public static int InAppLocationRateMin = 15;
		public static int InAppLocationRateMax = 300;
		public static int BackgroundLocationRateMin = 300;		
		public static int BackgroundLocationRateMax = 3600;
		public const int LocationKeepTime = 60 * 60 * 24;
		public const int SystemLogKeepTime = 60 * 5;
		public const int CacheKeepTime = 60 * 60 * 24;

		public const bool isTestDB = true;
		public const int MaxResultCount = 100;

		public const string TestDB = "&testDB";
		public const string HostName = "https://locationconnection.appspot.com/";
	}
}