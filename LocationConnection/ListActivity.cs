using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using Android;
using Android.Animation;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Common;
using Android.Gms.Location;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.Constraints;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Views.Animations;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidX.Core.Graphics;
//using AndroidX.Lifecycle; //I don't know how to add observer using this package.
using Android.Arch.Lifecycle;
using Java.Interop;
using Xamarin.Essentials;
using static Android.Gms.Maps.GoogleMap;
using AndroidX.Core.Content;

namespace LocationConnection
{
    [Activity(MainLauncher = true, ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class ListActivity : BaseActivity, IOnMapReadyCallback
	{
		private string googleServiceFile = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "googleservice.txt");

		IMenu pageMenu;

		ConstraintLayout FilterLayout, DistanceFilters, UseGeoContainer;
		LinearLayout SearchLayout, MapContainer;
		Android.Support.V7.Widget.Toolbar MainPageToolbar;
		TextView StatusText, NoResult, ResultSet;
		ImageButton StatusImage, OpenSearch, OpenFilters, ListView, MapView, SortBy_LastActiveDate, SortBy_ResponseRate,
			SortBy_RegisterDate, OrderBy, DistanceFiltersOpenClose, AddressOK, LoadPrevious, LoadNext, RefreshDistance, MenuChatList;
		View BottomSeparator, MenuChatListBg, MenuChatListBgCorner, RippleMain;
		Button MapStreet, MapSatellite;
		Spinner SearchIn, ListType;
		RadioButton UseGeoNo, UseGeoYes, DistanceSourceCurrent, DistanceSourceAddress;
		EditText SearchTerm, DistanceSourceAddressText, DistanceLimitInput;
		SeekBar DistanceLimit;
		SupportMapFragment mapFragment;
		GridView UserSearchList;
		ImageView ReloadPulldown;
        UserSearchListAdapter adapter;
		ImageView RefreshDistanceImage, LoaderCircle;
		InputMethodManager imm;		

		FusedLocationProviderClient fusedLocationProviderClient;
		ObjectAnimator anim_pulldown;

		public static ListActivity thisInstance;

		public static bool initialized = false;

		public static List<Profile> listProfiles;
		public static List<Profile> viewProfiles;
		private static List<Profile> newListProfiles;
		public static List<Marker> profileMarkers;
		public static int? totalResultCount;
		private bool listLoading;
		private bool mapSetting;
		private bool mapSet;
		GoogleMap thisMap;
		private bool mapLoaded;
		private bool usersLoaded;
		private bool distanceSourceAddressTextChanging;
		private bool distanceLimitChangedByCode;
		private Timer ProgressTimer;
		private bool listTypeClicked; //Itemselected gets called when seting the container visible for the first time, or when setting initial list selection
		private bool searchInClicked;
		private bool listTypeShown;
		private bool searchInShown;
		private bool recenterMap;
		private bool mapToSet;
		private bool distanceSourceCurrentClicked;
		private byte backCounter;
		private bool autoLogin;
		public static bool adapterToSet;

		//if we get location before logging in is completed, we do not need to request it again. 
		private double? localLatitude;
		private double? localLongitude;
		private long? localLocationTime;

		//pull down refresh icon
		private int? startY;
		private float loaderHeight;
		private float maxY; 
		private float diff;

		private float bottomSeparatorMargin;

		public static bool addResultsBefore;
		public static bool addResultsAfter;
		public static int absoluteStartIndex; //Session.ResultsFrom-1 (absoluteFirstIndex) when we enter ProfileView (absolute index of first item in list)
		public static int absoluteIndex; //index in the total number of results.
		public static int viewIndex; //index in list, it will change as the view list expands backwards
		public static int absoluteFirstIndex; //absolute position of first element in the list. Changes as list is expanded backwards.

		private bool rippleRunning;
        private int loaderAnimTime = 1300;
		private Timer rippleTimer;

		private bool onCreateError;

		private int spinnerItem;
		private int spinnerItemDropdown;
		private int icAscending;
		private int icDescending;
		private int iconBackgroundLight;
		private int iconBackgroundDark;
		private int statusRoundBackground;

		private Timer firstRunTimer;
		public static Timer locationTimer;

		protected override void OnCreate(Bundle savedInstanceState)
        {
            try
            {
                base.OnCreate(savedInstanceState);
				//Xamarin.Essentials.Platform.Init(this, savedInstanceState); needed?

				onCreateError = false;
				thisInstance = this;
				initialized = true;
				GetScreenMetrics(true);
				c.LoadSettings(false); //overwrites DisplaySize

				if (File.Exists(c.errorFile))
				{
					string url = "action=reporterror&ID=" + Session.ID + "&SessionID=" + Session.SessionID;
					string content = "Content=" + c.UrlEncode(File.ReadAllText(c.errorFile) + System.Environment.NewLine
						+ "Android version: " + c.AndroidInfo() + System.Environment.NewLine + File.ReadAllText(CommonMethods.logFile));
					string responseString = c.MakeRequestSync(url, "POST", content);
					if (responseString == "OK")
					{
						File.Delete(c.errorFile);
					}
				}

				IsPlayServicesAvailable();
				CreateNotificationChannel();

				if (!Directory.Exists(CommonMethods.cacheFolder)) {
					Directory.CreateDirectory(CommonMethods.cacheFolder);
				}
				else
				{
					CommonMethods.TruncateFolder(CommonMethods.cacheFolder);
				}

				if (!c.IsLoggedIn() && File.Exists(c.loginSessionFile))
				{
					autoLogin = true;
				}
				else
				{
					autoLogin = false;
				}

				if (autoLogin)
				{
					c.Log("Autologin");

					Task.Run(async () =>
					{
						Session.LastDataRefresh = null;
						Session.LocationTime = null;

						string str = File.ReadAllText(c.loginSessionFile);
						string[] strarr = str.Split(";");

						string url = "action=loginsession&ID=" + strarr[0] + "&SessionID=" + strarr[1];

						if (File.Exists(firebaseTokenFile))
						{
							if (bool.Parse(File.ReadAllText(tokenUptoDateFile)) == false)
							{
								url += "&token=" + File.ReadAllText(firebaseTokenFile);
							}
						}

						RunOnUiThread(() => {
							//will be applied after OnResume
							if (!(RefreshDistance is null) && !(ReloadPulldown is null) && !(LoaderCircle is null))
							{
								StartLoaderAnim();
							}
							if (!(ResultSet is null))
							{
								ResultSet.Visibility = ViewStates.Visible;
								c.Log("Autologin ResultSet.Text " + ResultSet.Text);
								if (ResultSet.Text == "") //only set it if "Getting location" is not being displayed already.
								{
									ResultSet.Text = res.GetString(Resource.String.LoggingIn);
								}
							}
						});

						string responseString = c.MakeRequestSync(url);

						if (responseString.Substring(0, 2) == "OK")
						{
							c.Log("Autologin logged in");
							if (File.Exists(firebaseTokenFile))
							{
								File.WriteAllText(tokenUptoDateFile, "True");
							}
							c.LoadCurrentUser(responseString);

							RunOnUiThread(() =>
							{
								try { 
									//called after task ends
									if (!(pageMenu is null))
									{
										SetLoggedInMenu();
									}
									LoggedInLayout();
									SetViews();
									//c.Log("Autologin, after setting views: listTypeClicked " + listTypeClicked + " searchInClicked " + searchInClicked);
								}
								catch (Exception ex)
								{
									if (!onCreateError)
									{
										c.ReportErrorSilent(ex.Message + System.Environment.NewLine + ex.StackTrace);
									}
								}
							});

							if ((bool)Session.UseLocation)
							{
								if (c.IsLocationEnabled())
								{
									c.Log("Autologin location enabled Session.InAppLocationRate " + Session.InAppLocationRate + " currentLocationRate " + currentLocationRate);
									if (Session.InAppLocationRate != currentLocationRate) //only restart if the logged-out rate is different than the logged-in one.
									{
										this.RunOnUiThread(() => {
											RestartLocationUpdates(); //first location may or may not exist, but most certainly it does, because location PM was present before logging in. First location update will call LoadListStartup, but if the list was refreshed just now, it does not do anything.
										});
									}

									if (!(localLatitude is null) && !(localLongitude is null) && !(localLocationTime is null)) //this has to be more recent than the loaded data
									{
										Session.Latitude = localLatitude;
										Session.Longitude = localLongitude;
										Session.LocationTime = localLocationTime;
										await c.UpdateLocationSync();

										RunOnUiThread(() => //have to run after SetViews() in order for the values not to revert to the not-logged-in value. 
										{
											recenterMap = true;
											if (Session.LastSearchType == Constants.SearchType_Filter)
											{
												LoadList();
											}
											else
											{
												LoadListSearch();
											}
											//ListType_itemselected gets called here
										});
									}
									else
									{
										//first location update will load the list, otherwise the Getting location... message will remain in the status bar until timeout
										c.Log("Autologin location unavailable");
									}
								}
								else
								{
									c.Log("Autologin LocationDisabledButUsingLocation");

									RunOnUiThread(() => {
										snack = c.SnackIndef(Resource.String.LocationDisabledButUsingLocation);
									});

									RunOnUiThread(() => //have to run after SetViews() in case user turned off location in settings while distancesource current was on. Distance filtering will be for address now.  
									{
										recenterMap = true;
										if (Session.LastSearchType == Constants.SearchType_Filter)
										{
											LoadList();
										}
										else
										{
											LoadListSearch();
										}
									});
								}
							}
							else
							{
								c.Log("Autologin logged in, not using location");

								if (locationUpdating)
								{
									StopLocationUpdates();
								}

								recenterMap = true;
								if (Session.LastSearchType == Constants.SearchType_Filter)
								{
									LoadList();
								}
								else
								{
									LoadListSearch();
								}
							}

							CheckIntent();
						}
						else if (responseString.Substring(0, 6) == "ERROR_")
						{
							RunOnUiThread(() =>
							{
								try {
									LoggedOutLayout();
									if (!(RefreshDistance is null) && !(ReloadPulldown is null) && !(LoaderCircle is null))
									{
										StopLoaderAnim();
									}
									if (!(ResultSet is null))
									{
										SetResultStatus();
									}
									string error = responseString.Substring(6);
									snack = c.SnackIndefStr(res.GetString(res.GetIdentifier(error, "string", PackageName)));
									if (error == "LoginFailed") // this is the only error we can get
									{
										File.Delete(c.loginSessionFile);
									}

									recenterMap = true;
									if (Session.LastSearchType == Constants.SearchType_Filter)
									{
										LoadList();
									}
									else
									{
										LoadListSearch();
									}
								}
								catch (Exception ex)
								{
									if (!onCreateError)
									{
										c.ReportErrorSilent(ex.Message + System.Environment.NewLine + ex.StackTrace);
									}
								}
							});
						}
						else
						{
							RunOnUiThread(() =>
							{
								try {
									LoggedOutLayout();
									if (!(RefreshDistance is null) && !(ReloadPulldown is null) && !(LoaderCircle is null))
									{
										StopLoaderAnim();
									}
									if (!(ResultSet is null))
									{
										SetResultStatus();
									}
									c.ReportError(responseString);
								}
								catch (Exception ex)
								{
									if (!onCreateError)
									{
										c.ReportErrorSilent(ex.Message + System.Environment.NewLine + ex.StackTrace);
									}
								}
							});
						}
						autoLogin = false;
					});
				}

				Stopwatch stw = new Stopwatch();
				stw.Start();
				
				if (Settings.DisplaySize == 1)
				{
					SetContentView(Resource.Layout.activity_list_normal);

					spinnerItem = Resource.Layout.spinner_item_normal;
					spinnerItemDropdown = Resource.Layout.spinner_item_dialog_normal;
					icAscending = Resource.Drawable.ic_ascending_normal;
					icDescending = Resource.Drawable.ic_descending_normal;
					iconBackgroundLight = Resource.Drawable.icon_background_light_normal;
					iconBackgroundDark = Resource.Drawable.icon_background_dark_normal;
					statusRoundBackground = Resource.Drawable.status_round_background_normal;

					loaderHeight = float.Parse(Resources.GetString(Resource.String.loaderHeightNormal), CultureInfo.InvariantCulture);
					maxY = float.Parse(Resources.GetString(Resource.String.maxYNormal), CultureInfo.InvariantCulture);
					bottomSeparatorMargin = float.Parse(Resources.GetString(Resource.String.bottomSeparatorMarginNormal), CultureInfo.InvariantCulture);
				}
				else
				{
					SetContentView(Resource.Layout.activity_list_small);

					spinnerItem = Resource.Layout.spinner_item_small;
					spinnerItemDropdown = Resource.Layout.spinner_item_dialog_small;
					icAscending = Resource.Drawable.ic_ascending_small;
					icDescending = Resource.Drawable.ic_descending_small;
					iconBackgroundLight = Resource.Drawable.icon_background_light_small;
					iconBackgroundDark = Resource.Drawable.icon_background_dark_small;
					statusRoundBackground = Resource.Drawable.status_round_background_small;

					loaderHeight = float.Parse(Resources.GetString(Resource.String.loaderHeightSmall), CultureInfo.InvariantCulture);
					maxY = float.Parse(Resources.GetString(Resource.String.maxYSmall), CultureInfo.InvariantCulture);
					bottomSeparatorMargin = float.Parse(Resources.GetString(Resource.String.bottomSeparatorMarginSmall), CultureInfo.InvariantCulture);
				}
				
				c.Log("Inflated in " + stw.ElapsedMilliseconds);

				stw.Stop();

				MainLayout = FindViewById<ConstraintLayout>(Resource.Id.MainLayout);
				MainPageToolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.MainPageToolbar);
				StatusImage = FindViewById<ImageButton>(Resource.Id.StatusImage);
				StatusText = FindViewById<TextView>(Resource.Id.StatusText);				
				OpenSearch = FindViewById<ImageButton>(Resource.Id.OpenSearch);
				OpenFilters = FindViewById<ImageButton>(Resource.Id.OpenFilters);				
				ListView = FindViewById<ImageButton>(Resource.Id.ListView);
				MapView = FindViewById<ImageButton>(Resource.Id.MapView);
				SearchLayout = FindViewById<LinearLayout>(Resource.Id.SearchLayout);
				SearchTerm = FindViewById<EditText>(Resource.Id.SearchTerm);
				SearchIn = FindViewById<Spinner>(Resource.Id.SearchIn);
				FilterLayout = FindViewById<ConstraintLayout>(Resource.Id.FilterLayout);
				ListType = FindViewById<Spinner>(Resource.Id.ListType);
				SortBy_LastActiveDate = FindViewById<ImageButton>(Resource.Id.SortBy_LastActiveDate);
				SortBy_ResponseRate = FindViewById<ImageButton>(Resource.Id.SortBy_ResponseRate);
				SortBy_RegisterDate = FindViewById<ImageButton>(Resource.Id.SortBy_RegisterDate);
				OrderBy = FindViewById<ImageButton>(Resource.Id.OrderBy);
				DistanceFiltersOpenClose = FindViewById<ImageButton>(Resource.Id.DistanceFiltersOpenClose);
				DistanceFilters = FindViewById<ConstraintLayout>(Resource.Id.DistanceFilters);
				UseGeoNo = FindViewById<RadioButton>(Resource.Id.UseGeoNo);
				UseGeoYes = FindViewById<RadioButton>(Resource.Id.UseGeoYes);
				UseGeoContainer = FindViewById<ConstraintLayout>(Resource.Id.UseGeoContainer);
				DistanceSourceCurrent = FindViewById<RadioButton>(Resource.Id.DistanceSourceCurrent);
				DistanceSourceAddress = FindViewById<RadioButton>(Resource.Id.DistanceSourceAddress);
				DistanceSourceAddressText = FindViewById<EditText>(Resource.Id.DistanceSourceAddressText);
				RefreshDistance = FindViewById<ImageButton>(Resource.Id.RefreshDistance);
				RefreshDistanceImage = FindViewById<ImageView>(Resource.Id.RefreshDistanceImage);
				AddressOK = FindViewById<ImageButton>(Resource.Id.AddressOK);
				DistanceLimitInput = FindViewById<EditText>(Resource.Id.DistanceLimitInput);
				DistanceLimit = FindViewById<SeekBar>(Resource.Id.DistanceLimit);
				NoResult = FindViewById<TextView>(Resource.Id.NoResult);
				MapContainer = FindViewById<LinearLayout>(Resource.Id.MapContainer);

				mapFragment = (SupportMapFragment)SupportFragmentManager.FindFragmentById(Resource.Id.ListViewMap);

				MapStreet = FindViewById<Button>(Resource.Id.MapStreet);
				MapSatellite = FindViewById<Button>(Resource.Id.MapSatellite);
				UserSearchList = FindViewById<GridView>(Resource.Id.UserSearchList);
				ReloadPulldown = FindViewById<ImageView>(Resource.Id.ReloadPulldown);
				ResultSet = FindViewById<TextView>(Resource.Id.ResultSet);
				LoadPrevious = FindViewById<ImageButton>(Resource.Id.LoadPrevious);
				LoadNext = FindViewById<ImageButton>(Resource.Id.LoadNext);
				LoaderCircle= FindViewById<ImageView>(Resource.Id.LoaderCircle);
				BottomSeparator = FindViewById<View>(Resource.Id.BottomSeparator);
				MenuChatListBg = FindViewById<View>(Resource.Id.MenuChatListBg);
				MenuChatListBgCorner = FindViewById<View>(Resource.Id.MenuChatListBgCorner);
				RippleMain = FindViewById<View>(Resource.Id.RippleMain);
				MenuChatList = FindViewById<ImageButton>(Resource.Id.MenuChatList);

				if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
				{
					//ReloadPulldown looks pixelated when rotated, so png is used.
					MenuChatListBg.SetBackgroundResource(statusRoundBackground);
				}

				UserSearchList.SetVerticalSpacing((int)Math.Round(2 * pixelDensity));
				UserSearchList.SetHorizontalSpacing((int)Math.Round(2 * pixelDensity));

				mapFragment.GetMapAsync(this);
				imm = (InputMethodManager)GetSystemService(Context.InputMethodService);
				c.view = MainLayout;
				fusedLocationProviderClient = LocationServices.GetFusedLocationProviderClient(this);
				ReloadPulldown.SetY(-ReloadPulldown.Height);
				SetSupportActionBar(MainPageToolbar);

				Session.LastDataRefresh = null; //when backgrounding app and foregrounding, ListActivity is recreated on Android 8 (LG G5), and the list adapter is emptied
				mapLoaded = false;
				usersLoaded = false;
                mapToSet = false;
                listLoading = false;
				adapterToSet = false;
				distanceSourceCurrentClicked = false;
				distanceLimitChangedByCode = false;

				DistanceLimit.Max = DistanceLimitValToProgress(Constants.DistanceLimitMax); //value range: 1 - DistanceLimitMax

				StatusImage.Click += StatusImage_Click;
				OpenSearch.Click += OpenSearch_Click;
				OpenFilters.Click += OpenFilters_Click;
				ListView.Click += ListView_Click;
				MapView.Click += MapView_Click;
				MapStreet.Click += MapStreet_Click;
				MapSatellite.Click += MapSatellite_Click;
				SearchTerm.KeyPress += SearchTerm_KeyPress;				
				SearchIn.ItemSelected += SearchIn_ItemSelected;
				ListType.ItemSelected += ListType_ItemSelected;
				SortBy_LastActiveDate.Click += SortBy_LastActiveDate_Click;
				SortBy_ResponseRate.Click += SortBy_ResponseRate_Click;
				SortBy_RegisterDate.Click += SortBy_RegisterDate_Click;
				OrderBy.Click += OrderBy_Click;
				DistanceFiltersOpenClose.Click += DistanceFiltersOpenClose_Click;
				UseGeoNo.Click += UseGeo_Click;
				UseGeoYes.Click += UseGeo_Click;
                RefreshDistance.Click += RefreshDistance_Click;
				DistanceSourceCurrent.Click += DistanceSource_Click;
				DistanceSourceAddress.Click += DistanceSource_Click;
				DistanceSourceAddressText.KeyPress += DistanceSourceAddressText_KeyPress;
				DistanceSourceAddressText.TextChanged += DistanceSourceAddressText_TextChanged;
				DistanceSourceAddressText.FocusChange += DistanceSourceAddressText_FocusChange;
				AddressOK.Click += AddressOK_Click;                 
				DistanceLimit.ProgressChanged += DistanceLimit_ProgressChanged;
				DistanceLimitInput.KeyPress += DistanceLimitInput_KeyPress;
				DistanceLimitInput.FocusChange += DistanceLimitInput_FocusChange;
				UserSearchList.ItemClick += UserSearchList_ItemClick;
				UserSearchList.Touch += UserSearchList_Touch;
				LoadPrevious.Click += LoadPrevious_Click;
				LoadNext.Click += LoadNext_Click;
				MenuChatList.Click += MenuChatList_Click;
				MenuChatList.Touch += MenuChatList_Touch;

				c.Log("ListActivity OnCreate end");
			}
            catch (Exception ex)
            {
				c.ReportErrorSilent(ex.Message + System.Environment.NewLine + ex.StackTrace);
				onCreateError = true;

				Intent i = new Intent(this, typeof(MainActivity));
				i.SetFlags(ActivityFlags.ReorderToFront);
				IntentData.error = ex.Message + System.Environment.NewLine + ex.StackTrace;
				StartActivity(i);
			}
        }

		protected override void OnResume()
		{
			try {
				base.OnResume();
				if (!initialized || onCreateError) { return; }

				if (File.Exists(c.locationLogFile))
				{
					TruncateLocationLog();
				}
				TruncateSystemLog();

				//c.IsLoggedIn() true does not mean, all session variables are set. Nullable object must have a value error can occur if autologin response is at the same time as ONResume.
				bool isLoggedIn = c.IsLoggedIn();
				c.Log("OnResume logged in " + isLoggedIn + " locationUpdating " + locationUpdating + " Session.LastDataRefresh " + Session.LastDataRefresh);

				if (isLoggedIn)
				{
					LoggedInLayout();
					if (!(pageMenu is null))
					{
						SetLoggedInMenu();
					}
				}
				else
				{
					LoggedOutLayout();
					if (!(pageMenu is null))
					{
						SetLoggedOutMenu();
					}
				}

				backCounter = 0;
				//when logging in with autologin, _itemSelected will fire twice, otherwise once. Once more when changing to the other view.
				searchInClicked = false;				
				listTypeClicked = false;
				searchInShown = false;
				listTypeShown = false;
				addResultsBefore = false;
				addResultsAfter = false;
				
				//c.Log("Onresume, after setting views: listTypeClicked " + listTypeClicked + " searchInClicked " + searchInClicked);
				MainLayout.RequestFocus();

				if (!(listProfiles is null) && !(newListProfiles is null) && Session.ListType != "hid") { //hid list will reload
					if (absoluteIndex < absoluteStartIndex)
					{
						//c.CW("OnResume got low range: absoluteIndex " + absoluteIndex + " absoluteStartIndex " + absoluteStartIndex + " absoluteFirstIndex " + absoluteFirstIndex + " viewIndex: " + (absoluteIndex - absoluteFirstIndex));
						do
						{
							absoluteStartIndex -= Constants.MaxResultCount;
						} while (absoluteStartIndex > absoluteIndex);

						if (absoluteStartIndex - absoluteFirstIndex + Constants.MaxResultCount > viewProfiles.Count) // this range contains less elements than MaxResultCount (could have happened, it profiles were hid)
						{
							listProfiles = viewProfiles.GetRange(absoluteStartIndex - absoluteFirstIndex, viewProfiles.Count - (absoluteStartIndex - absoluteFirstIndex));
						}
						else //range is full
						{
							listProfiles = viewProfiles.GetRange(absoluteStartIndex - absoluteFirstIndex, Constants.MaxResultCount);
						}
					}
					else if (absoluteIndex >= absoluteStartIndex + listProfiles.Count)
					{
						//c.CW("OnResume got high range: absoluteIndex " + absoluteIndex + " absoluteStartIndex " + absoluteStartIndex + " absoluteFirstIndex " + absoluteFirstIndex + " viewIndex: " + (absoluteIndex - absoluteFirstIndex));
						do
						{
							absoluteStartIndex += Constants.MaxResultCount;
						} while (absoluteStartIndex <= absoluteIndex);
						absoluteStartIndex -= Constants.MaxResultCount;

						if (absoluteStartIndex - absoluteFirstIndex + Constants.MaxResultCount > viewProfiles.Count) // this range contains less elements than MaxResultCount 
						{
							listProfiles = viewProfiles.GetRange(absoluteStartIndex - absoluteFirstIndex, viewProfiles.Count - (absoluteStartIndex - absoluteFirstIndex));
						}
						else //range is full
						{
							listProfiles = viewProfiles.GetRange(absoluteStartIndex - absoluteFirstIndex, Constants.MaxResultCount);
						}
					}
					else //we are in the original range, but could have hid profiles, so the section must be recreated.
					{
						//c.CW("OnResume in normal range: absoluteIndex " + absoluteIndex + " absoluteStartIndex " + absoluteStartIndex + " absoluteFirstIndex " + absoluteFirstIndex + " viewIndex: " + (absoluteIndex - absoluteFirstIndex));
						if (absoluteStartIndex - absoluteFirstIndex + Constants.MaxResultCount > viewProfiles.Count) // this range contains less elements than MaxResultCount 
						{
							listProfiles = viewProfiles.GetRange(absoluteStartIndex - absoluteFirstIndex, viewProfiles.Count - (absoluteStartIndex - absoluteFirstIndex));
						}
						else //range is full
						{
							listProfiles = viewProfiles.GetRange(absoluteStartIndex - absoluteFirstIndex, Constants.MaxResultCount);
						}
					}

					if ((bool)Settings.IsMapView)
					{
						mapToSet = true;
					}
					else
					{
						mapToSet = false;
					}

					Session.ResultsFrom = absoluteStartIndex + 1;
					c.Log("NewListProfiles Session.ResultsFrom " + Session.ResultsFrom + " listProfiles.Count " + listProfiles.Count + " newListProfiles.Count " + newListProfiles.Count);

					adapterToSet = true;
				}
				newListProfiles = null;

				//On LG G5, clicking the home button and opening the app again will clear the list. Not on M5.

				if (isLoggedIn)
				{
					if ((bool)Session.UseLocation && !c.IsLocationEnabled())
					{
						snack = c.SnackIndef(Resource.String.LocationDisabledButUsingLocation);
					}
				}
				else
				{
					if (!c.IsLocationEnabled())
					{
						Session.UseLocation = false;
					}
					else
					{
						Session.UseLocation = true;
					}
				}

				if (!(bool)Session.UseLocation || !c.IsLocationEnabled()) //the user might have turned off location while having current location checked
				{
					SetDistanceSourceAddress();
				}
				
				SetViews();

				if ((bool)Session.UseLocation && c.IsLocationEnabled())
				{
					if (!locationUpdating)
					{
						c.Log("Location not yet updating");
						StartLocationUpdates();
						ResultSet.Visibility = ViewStates.Visible;
						ResultSet.Text = res.GetString(Resource.String.GettingLocation);
						StartLocationTimer();
					}
					else if (isLoggedIn && Session.InAppLocationRate != currentLocationRate)
					{
						c.Log("Location updating at different frequency: Session.InAppLocationRate " + Session.InAppLocationRate + " currentLocationRate " + currentLocationRate);
						
						RestartLocationUpdates();
						if (!firstLocationAcquired) //most likely location was already acquired
						{
							ResultSet.Visibility = ViewStates.Visible;
							ResultSet.Text = res.GetString(Resource.String.GettingLocation);
							StartLocationTimer();
						}
						else
						{
							LoadListStartup();
						}
					}
					else if (!firstLocationAcquired)
					{
						c.Log("Location updating, awaiting location");
						ResultSet.Visibility = ViewStates.Visible;
						ResultSet.Text = res.GetString(Resource.String.GettingLocation);
						StartLocationTimer();
					}
					else
					{
						c.Log("Location exists");
						LoadListStartup();
					}
				}
				else
				{
					if (locationUpdating) //not logged in user didn't enable PM, or logged-in user does not use location
					{
						StopLocationUpdates();
					}

					c.Log("OnResume no location");
					LoadListStartup();
				}

				if ((!(bool)Session.UseLocation || !c.IsLocationEnabled()) && !((bool)Session.GeoFilter && (bool)Session.GeoSourceOther))
				{
					if ((bool)Settings.IsMapView)
					{
						ListView_Click(null, null);
					}
				}

				if (firstRun) //if alert is not shown, will be changed next time ListActivity is recreated.
				{
					firstRunTimer = new Timer();
					firstRunTimer.Interval = Constants.tutorialInterval;
					firstRunTimer.Elapsed += FirstRunTimer_Elapsed;
					firstRunTimer.Start();
				}

				c.Log("Onresume end");

				//ListType_ItemSelected get called here if container is visible
			}
			catch (Exception ex)
			{
				if (!onCreateError)
				{
					c.ReportErrorSilent(ex.Message + System.Environment.NewLine + ex.StackTrace);
					Intent i = new Intent(this, typeof(MainActivity));
					i.SetFlags(ActivityFlags.ReorderToFront);
					IntentData.error = ex.Message + System.Environment.NewLine + ex.StackTrace;
					StartActivity(i);
				}
			}
		}

		public void StartLocationTimer()
		{
			c.Log("Starting location timer");
			locationTimer = new Timer();
			locationTimer.Interval = Constants.LocationTimeout;
			locationTimer.Elapsed += LocationTimer_Elapsed;
			locationTimer.Start();
		}

		private void LocationTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			((Timer)sender).Stop();
			c.Log("Location timeout");
			RunOnUiThread(() => {
				LoadListStartup();
			});
		}

		public void LoadListStartup()
		{
			long unixTimestamp = c.Now();

			if (autoLogin)
			{
				c.Log("LoadListStartup autologin, locationUpdating " + locationUpdating + ", locationtime: " + Session.LocationTime);

				localLatitude = Session.Latitude;
				localLongitude = Session.Longitude;
				localLocationTime = Session.LocationTime;
			}
			else
			{
				c.Log("LoadListStartup not autologin, logged in: " + c.IsLoggedIn() + ", locationtime: " + Session.LocationTime + " Settings.IsMapView " + Settings.IsMapView + " mapToSet " + mapToSet);

				//reloading list if it expired
				if (Session.LastDataRefresh is null || Session.LastDataRefresh < unixTimestamp - Constants.DataRefreshInterval)
				{
					c.Log("LoadListStartup will load");

					recenterMap = true;
					if (Session.LastSearchType == Constants.SearchType_Filter)
					{
						Task.Run(() => LoadList());
					}
					else
					{
						Task.Run(() => LoadListSearch());
					}
				}
				else //show no result label only if list is not being reloaded, and set map with the results loaded while being in ProfileView
				{
					c.Log("List not loading, adapterToSet " + adapterToSet + "  mapLoaded " + mapLoaded + " mapToSet " + mapToSet);

					if (adapterToSet)
					{
						adapter = new UserSearchListAdapter(this, listProfiles);
						ImageCache.imagesInProgress = new List<string>();
						ImageCache.imageViewToLoadLater = new Dictionary<ImageView, string>();
						UserSearchList.Adapter = adapter;
						usersLoaded = true;
					}

					if (mapLoaded && mapToSet) //map is not loaded on startup. 
					{
						StartLoaderAnim();
						mapSet = false;
						recenterMap = true;
						SetMap();
					}
					SetResultStatus();
				}
				adapterToSet = false;
			}

		}

		protected override void OnPause()
		{
			base.OnPause();
			if (!ListActivity.initialized) { return; }

			if (!c.IsLoggedIn())
			{
				Settings.SearchTerm = Session.SearchTerm;
				Settings.SearchIn = Session.SearchIn;

				Settings.ListType = Session.ListType;
				Settings.SortBy = Session.SortBy;
				Settings.OrderBy = Session.OrderBy;
				Settings.GeoFilter = Session.GeoFilter;
				Settings.GeoSourceOther = Session.GeoSourceOther;
				Settings.OtherLatitude = Session.OtherLatitude;
				Settings.OtherLongitude = Session.OtherLongitude;
				Settings.OtherAddress = Session.OtherAddress;
				Settings.DistanceLimit = Session.DistanceLimit;
				Settings.ResultsFrom = Session.ResultsFrom;
			}
			if (!(thisMap is null) && thisMap.MapType != Settings.ListMapType)
			{
				Settings.ListMapType = (byte)thisMap.MapType;
			}
			c.SaveSettings();

			if (firstRun && !(firstRunTimer is null) && firstRunTimer.Enabled) //firstRunTimer may be null if OnResume was terminated due to error in OnCreate
			{
				firstRunTimer.Stop();
			}
		}

		private void FirstRunTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			firstRunTimer.Stop();
			RunOnUiThread(() => {
				c.SnackIndef(Resource.String.FirstRunMessage);
			});
			firstRun = false;
		}

		private void LoggedInLayout()
		{
			StatusImage.Visibility = ViewStates.Visible;
			StatusText.Visibility = ViewStates.Gone;

			ImageCache im = new ImageCache(this);
			
			Task.Run(async () => {
				await im.LoadImage(StatusImage, Session.ID.ToString(), Session.Pictures[0]);
			});
			
			MenuChatList.Visibility = ViewStates.Visible;
			MenuChatListBg.Visibility = ViewStates.Visible;
			MenuChatListBgCorner.Visibility = ViewStates.Visible;
			((ConstraintLayout.LayoutParams)BottomSeparator.LayoutParameters).RightMargin = (int)(bottomSeparatorMargin * pixelDensity);
		}

		private void LoggedOutLayout() //other address set will remain after registering, because Session is only cleared when logging out
		{
			Session.SearchTerm = Settings.SearchTerm;
			Session.SearchIn = Settings.SearchIn;

			Session.ListType = Settings.ListType;
			Session.SortBy = Settings.SortBy;
			Session.OrderBy = Settings.OrderBy;
			Session.GeoFilter = Settings.GeoFilter;
			Session.GeoSourceOther = Settings.GeoSourceOther;
			Session.OtherLatitude = Settings.OtherLatitude;
			Session.OtherLongitude = Settings.OtherLongitude;
			Session.OtherAddress = Settings.OtherAddress;
			Session.DistanceLimit = Settings.DistanceLimit;
			Session.ResultsFrom = Settings.ResultsFrom;

			StatusImage.Visibility = ViewStates.Gone;
			StatusText.Visibility = ViewStates.Visible;
			StatusText.Text = res.GetString(Resource.String.NotLoggedIn);
			MenuChatList.Visibility = ViewStates.Gone;
			MenuChatListBg.Visibility = ViewStates.Gone;
			MenuChatListBgCorner.Visibility = ViewStates.Gone;
			((ConstraintLayout.LayoutParams)BottomSeparator.LayoutParameters).RightMargin = 0;
		}

		public bool IsPlayServicesAvailable() //Newer Huawei devices do not support play store.
		{
			int resultCode = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(this);
			c.Log("IsPlayServicesAvailable resultCode: " + resultCode);
			if (resultCode != ConnectionResult.Success)
			{
				if (GoogleApiAvailability.Instance.IsUserResolvableError(resultCode))
				{
					c.Alert(res.GetString(Resource.String.GooglePlayNotAvailableWithError) + " " + GoogleApiAvailability.Instance.GetErrorString(resultCode));
					c.ReportErrorSilent("IsPlayServicesAvailable resolvable error: " + resultCode + " - " + GoogleApiAvailability.Instance.GetErrorString(resultCode));
				}
				else
				{
					if (!File.Exists(googleServiceFile))
					{
						File.WriteAllText(googleServiceFile, "");

						c.Alert(res.GetString(Resource.String.GooglePlayNotAvailableWithError) + " " + GoogleApiAvailability.Instance.GetErrorString(resultCode));
						c.ReportErrorSilent("IsPlayServicesAvailable unresolvable error: " + resultCode + " - " + GoogleApiAvailability.Instance.GetErrorString(resultCode));
					}
				}

				return false;
			}
			else
			{
				return true;
			}
		}

		void CreateNotificationChannel()
		{
			if (Build.VERSION.SdkInt < BuildVersionCodes.O)
			{
				// Notification channels are new in API 26 (and not a part of the
				// support library). There is no need to create a notification
				// channel on older versions of Android.
				return;
			}

			var channel = new NotificationChannel(Constants.CHANNEL_ID,
												  "FCM Notifications",
												  NotificationImportance.Default)
			{

				Description = "Firebase Cloud Messages appear in this channel"
			};

			var notificationManager = (NotificationManager)GetSystemService(Android.Content.Context.NotificationService);
			notificationManager.CreateNotificationChannel(channel);
		}

		public void SetViews()
		{
			if (!(bool)Settings.IsMapView || (Session.UseLocation is null || !(bool)Session.UseLocation) && !((bool)Session.GeoFilter && (bool)Session.GeoSourceOther))
			{
				UserSearchList.Visibility = ViewStates.Visible;
				MapContainer.Visibility = ViewStates.Invisible;
				MapStreet.Visibility = ViewStates.Gone;
				MapSatellite.Visibility = ViewStates.Gone;
				ListView.SetBackgroundResource(iconBackgroundLight);
				MapView.SetBackgroundResource(0);
				Settings.IsMapView = false;
			}
			else
			{
				UserSearchList.Visibility = ViewStates.Gone;
				MapContainer.Visibility = ViewStates.Visible;
				MapStreet.Visibility = ViewStates.Visible;
				MapSatellite.Visibility = ViewStates.Visible;
				ListView.SetBackgroundResource(0);
				MapView.SetBackgroundResource(iconBackgroundLight);
			}

			if (!(bool)Settings.SearchOpen)
			{
				SearchLayout.Visibility = ViewStates.Gone;
				OpenSearch.SetBackgroundResource(0);
			}
			else
			{
				Session.LastSearchType = Constants.SearchType_Search;
				SearchLayout.Visibility = ViewStates.Visible;
				OpenSearch.SetBackgroundResource(iconBackgroundLight);
				searchInClicked = true;
				searchInShown = true;
			}

			SearchTerm.Text = Session.SearchTerm;

			var adapter = ArrayAdapter.CreateFromResource(this, Resource.Array.SearchInEntries, spinnerItem);
			adapter.SetDropDownViewResource(spinnerItemDropdown);
			SearchIn.Adapter = adapter;

			string[] arr = res.GetStringArray(Resource.Array.SearchInEntries_values);
			int index = arr.ToList().IndexOf(Session.SearchIn);
			SearchIn.SetSelection(index);
			
			if (!(bool)Settings.FiltersOpen)
			{
				FilterLayout.Visibility = ViewStates.Gone;
				OpenFilters.SetBackgroundResource(0);
			}
			else
			{
				//if autologin finishes before onresume, ListType_ItemSelected runs only once, otherwise it runs twice.
				Session.LastSearchType = Constants.SearchType_Filter;
				FilterLayout.Visibility = ViewStates.Visible;
				OpenFilters.SetBackgroundResource(iconBackgroundLight);
				listTypeClicked = true;
				listTypeShown = true;
			}

			if (!(bool)Settings.GeoFiltersOpen)
			{
				DistanceFilters.Visibility = ViewStates.Gone;
				DistanceFiltersOpenClose.ScaleY = -1;
				TooltipCompat.SetTooltipText(DistanceFiltersOpenClose, res.GetString(Resource.String.DistanceFiltersOpen));
			}
			else
			{
				DistanceFilters.Visibility = ViewStates.Visible;
				DistanceFiltersOpenClose.ScaleY = 1;
				TooltipCompat.SetTooltipText(DistanceFiltersOpenClose, res.GetString(Resource.String.DistanceFiltersClose));
			}

			if (c.IsLoggedIn())
			{
				adapter = ArrayAdapter.CreateFromResource(this, Resource.Array.ListTypeEntries, spinnerItem);
				adapter.SetDropDownViewResource(spinnerItemDropdown);
				ListType.Adapter = adapter;

				arr = res.GetStringArray(Resource.Array.ListTypeEntries_values);
				index = arr.ToList().IndexOf(Session.ListType);
				ListType.SetSelection(index);
			}
			else
			{
				adapter = ArrayAdapter.CreateFromResource(this, Resource.Array.ListTypeEntriesNotLoggedIn, spinnerItem);
				adapter.SetDropDownViewResource(spinnerItemDropdown);
				ListType.Adapter = adapter;

				arr = res.GetStringArray(Resource.Array.ListTypeEntriesNotLoggedIn_values);
				index = arr.ToList().IndexOf(Session.ListType);
				ListType.SetSelection(index);
			}

			switch (Session.SortBy)
			{
				case "LastActiveDate":
					SortBy_LastActiveDate.SetBackgroundResource(iconBackgroundDark);
					SortBy_ResponseRate.SetBackgroundResource(0);
					SortBy_RegisterDate.SetBackgroundResource(0);
					break;
				case "ResponseRate":
					SortBy_LastActiveDate.SetBackgroundResource(0);
					SortBy_ResponseRate.SetBackgroundResource(iconBackgroundDark);
					SortBy_RegisterDate.SetBackgroundResource(0);
					break;
				case "RegisterDate":
					SortBy_LastActiveDate.SetBackgroundResource(0);
					SortBy_ResponseRate.SetBackgroundResource(0);
					SortBy_RegisterDate.SetBackgroundResource(iconBackgroundDark);
					break;
			}

			if (Session.OrderBy == "desc")
			{
				TooltipCompat.SetTooltipText(OrderBy, res.GetString(Resource.String.Descending));
				//OrderBy.TooltipText = res.GetString(Resource.String.Descending); //threw an error in Google's test, found out it is not supported proir to API 26.
				OrderBy.SetImageResource(icDescending);
			}
			else
			{
				TooltipCompat.SetTooltipText(OrderBy, res.GetString(Resource.String.Ascending));
				//OrderBy.TooltipText = res.GetString(Resource.String.Ascending);
				OrderBy.SetImageResource(icAscending);
			}

			if (!(bool)Session.GeoFilter)
			{
				UseGeoNo.Checked = true;
				UseGeoContainer.Visibility = ViewStates.Gone;
			}
			else
			{
				UseGeoYes.Checked = true;
				UseGeoContainer.Visibility = ViewStates.Visible;
			}

			if (!(bool)Session.GeoSourceOther && c.IsLocationEnabled())
			{
				DistanceSourceCurrent.Checked = true;
				DistanceSourceAddressText.Visibility = ViewStates.Gone;
				AddressOK.Visibility = ViewStates.Gone;
			}
			else
			{
				Session.GeoSourceOther = true;
				DistanceSourceAddress.Checked = true;
				DistanceSourceAddressText.Visibility = ViewStates.Visible;
				AddressOK.Visibility = ViewStates.Visible;
			}

			distanceSourceAddressTextChanging = true;
			if (!string.IsNullOrEmpty(Session.OtherAddress))
			{
				DistanceSourceAddressText.Text = Session.OtherAddress;
			}
			else if (!(Session.OtherLatitude is null) && !(Session.OtherLongitude is null))
			{
				
				DistanceSourceAddressText.Text = ((double)Session.OtherLatitude).ToString(CultureInfo.InvariantCulture) + ", " + ((double)Session.OtherLongitude).ToString(CultureInfo.InvariantCulture);
			}
			else
			{
				DistanceSourceAddressText.Text = ""; //text will otherwise remain after logging out
			}
			distanceSourceAddressTextChanging = false;

			distanceLimitChangedByCode = true;
			DistanceLimit.Progress = DistanceLimitValToProgress((int)Session.DistanceLimit);
			DistanceLimitInput.Text = Session.DistanceLimit.ToString();
			distanceLimitChangedByCode = false;
		}

		public override bool OnCreateOptionsMenu(IMenu menu)
		{
			pageMenu = menu;
			MenuInflater.Inflate(Resource.Menu.menu_list, menu);
			if (c.IsLoggedIn())
			{
				SetLoggedInMenu();
			}
			else
			{
				SetLoggedOutMenu();
			}
			return base.OnCreateOptionsMenu(menu);
		}

		private void SetLoggedInMenu ()
		{
			IMenuItem item = pageMenu.FindItem(Resource.Id.MenuLogOut);
			item.SetVisible(true);
			item = pageMenu.FindItem(Resource.Id.MenuLogIn);
			item.SetVisible(false);
			item = pageMenu.FindItem(Resource.Id.MenuRegister);
			item.SetVisible(false);
		}

		private void SetLoggedOutMenu ()
		{
			IMenuItem item = pageMenu.FindItem(Resource.Id.MenuLogOut);
			item.SetVisible(false);
			item = pageMenu.FindItem(Resource.Id.MenuLogIn);
			item.SetVisible(true);
			item = pageMenu.FindItem(Resource.Id.MenuRegister);
			item.SetVisible(true);
		}

		public override bool OnOptionsItemSelected(IMenuItem item)
		{
			Intent i;
			switch (item.ItemId)
			{
				case Resource.Id.MenuLogIn:
					i = new Intent(this, typeof(MainActivity));
					i.SetFlags(ActivityFlags.ReorderToFront);
					StartActivity(i);
					break;
				case Resource.Id.MenuRegister:
					i = new Intent(this, typeof(RegisterActivity));
					i.SetFlags(ActivityFlags.ReorderToFront);
					StartActivity(i);
					break;
				case Resource.Id.MenuLogOut:
					if (!string.IsNullOrEmpty(locationUpdatesTo))
					{
						EndLocationShare();
					}
					i = new Intent(this, typeof(MainActivity));
					i.SetFlags(ActivityFlags.ReorderToFront);
					IntentData.logout = true;
					StartActivity(i);
					break;
				case Resource.Id.MenuSettings:
					i = new Intent(this, typeof(SettingsActivity));
					StartActivity(i);
					break;
				case Resource.Id.MenuLocation:
					i = new Intent(this, typeof(LocationActivity));
					i.SetFlags(ActivityFlags.ReorderToFront);
					StartActivity(i);
					break;
				case Resource.Id.MenuHelpCenter:
					i = new Intent(this, typeof(HelpCenterActivity));
					i.SetFlags(ActivityFlags.ReorderToFront);
					StartActivity(i);
					break;
				case Resource.Id.MenuAbout:
					c.AlertLinks(res.GetString(Resource.String.versionInfo));
					break;
			}
			return base.OnOptionsItemSelected(item);
		}

		public override void OnBackPressed()
		{
			if (backCounter == 0)
			{
				backCounter++;
				c.Snack(Resource.String.BackPressedInfo);
			}
			else
			{
				FinishAffinity();
			}
		}

		private async Task<bool> CheckLocationSettings()
		{
			if ((Session.UseLocation is null || !(bool)Session.UseLocation || !c.IsLocationEnabled()) && !((bool)Session.GeoFilter && (bool)Session.GeoSourceOther))
			{
				//cases: - not logged in user not granted location access
				//		 - logged in user with use location setting off and not granted location access
				//		 - logged in use with use location setting off, but granted location access.
				if (!c.IsLoggedIn())
				{
					string dialogResponse = await c.DisplayCustomDialog("", res.GetString(Resource.String.MapViewNoLocation),
				res.GetString(Resource.String.DialogYes), res.GetString(Resource.String.DialogNo));
					if (dialogResponse == res.GetString(Resource.String.DialogYes))
					{
						ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.AccessFineLocation }, 2);
					}
					else
					{
						mapToSet = false;
						SetDistanceSourceAddress();
					}
				}
				else
				{
					if (!c.IsLocationEnabled())
					{
						string dialogResponse = await c.DisplayCustomDialog("", res.GetString(Resource.String.MapViewNoLocation),
				res.GetString(Resource.String.DialogYes), res.GetString(Resource.String.DialogNo));
						if (dialogResponse == res.GetString(Resource.String.DialogYes))
						{
							ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.AccessFineLocation }, 2);
						}
						else
						{
							mapToSet = false;
							SetDistanceSourceAddress();
						}
					}
					else //permission is granted, but UseLocation is off (coming from MapView_Click)
					{
						string dialogResponse = await c.DisplayCustomDialog("", res.GetString(Resource.String.MapViewNoUseLocation),
				res.GetString(Resource.String.DialogYes), res.GetString(Resource.String.DialogNo));
						if (dialogResponse == res.GetString(Resource.String.DialogYes))
						{
							if (UpdateLocationSetting(true)) {
								firstLocationAcquired = false; // it should be false to start with

								ResultSet.Visibility = ViewStates.Visible;
								ResultSet.Text = res.GetString(Resource.String.GettingLocation);

								this.RunOnUiThread(() =>
								{
									StartLocationUpdates();
								});
								
							}
						}
						else
						{
							mapToSet = false;
							SetDistanceSourceAddress();
						}
					}
				}
				return false;
			}
			else
			{
				return true;
			}
		}

		public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
		{
			if (requestCode == 2) //Location
			{
				if ((grantResults.Length == 1) && (grantResults[0] == Permission.Granted))
				{    
					if (c.IsLoggedIn())
					{
						if (c.snackPermanentText == Resource.String.LocationDisabledButUsingLocation && snack.IsShown)
						{
							snack.Dismiss();
						}

						if (distanceSourceCurrentClicked)
						{
							c.Log("PM logged in granted distanceSourceCurrentClicked");
							//it resets to address by itself
							DistanceSourceCurrent.Checked = true;
							Session.GeoSourceOther = false;
							Session.LastDataRefresh = null;
							distanceSourceCurrentClicked = false;
						}
						else
						{
							c.Log("PM logged in granted map clicked, mapToSet " + mapToSet);
						}
						Session.LocationTime = null;
						UpdateLocationSetting(true);
					}
					else
					{
						Session.UseLocation = true;
						Session.LocationTime = null;

						//OnResume will be called which starts location updates and refreshing the list

						if (distanceSourceCurrentClicked)
						{
							c.Log("PM not logged in granted distanceSourceCurrentClicked");
							//it resets to address by itself
							DistanceSourceCurrent.Checked = true;
							Settings.GeoSourceOther = false; //LoggedOutLayout will load it into Session on OnResume
							Session.LastDataRefresh = null;
							distanceSourceCurrentClicked = false;
						}
						else
						{
							c.Log("PM not logged in granted map clicked, mapToSet " + mapToSet);
						}
					}
				}
				else
				{
					mapToSet = false;
					distanceSourceCurrentClicked = false;
					SetDistanceSourceAddress();
					c.Snack(Resource.String.LocationNotGranted); //in the dialog the user choose to turn on location, but now denied it. Message needs to be shown.
					if (c.IsLoggedIn())
					{
						UpdateLocationSetting(false);
					}
				}
				//activity will resume, Session.UseLocation will be set there
			}
			else
			{
				base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
			}
		}

		public bool UpdateLocationSetting(bool useLocation)
		{
			string url = "action=profileedit&ID=" + Session.ID + "&SessionID=" + Session.SessionID + "&UseLocation=" + useLocation;
			string responseString = c.MakeRequestSync(url);
			if (responseString.Substring(0, 2) == "OK")
			{
				Session.UseLocation = useLocation;
				//OnResume will be called which starts location updates and refreshing the list

				if (useLocation && distanceSourceCurrentClicked)
				{
					Session.LastDataRefresh = null;
					distanceSourceCurrentClicked = false;
				}
				return true;
			}
			else
			{
				c.ReportError(responseString);
				return false;
			}
		}		

		private void StatusImage_Click(object sender, EventArgs e)
		{
			Intent i = new Intent(this, typeof(ProfileViewActivity));
			i.SetFlags(ActivityFlags.ReorderToFront);
			IntentData.profileViewPageType = Constants.ProfileViewType_Self;
			StartActivity(i);
		}
			   
		private void OpenFilters_Click(object sender, EventArgs e)
		{
			//situation: when clicking it for the first time in the Activity's lifecycle, the spinners' event handler get triggered, thus unnecessarily refresing the list.
			//in test, the first spinner reacted at 141 ms, the third 5 ms later.
			//we set a timer disabling the refresh up to 500 ms
			imm.HideSoftInputFromWindow(SearchTerm.WindowToken, 0);
			if (!(bool)Settings.FiltersOpen)
			{
				OpenFilters.SetBackgroundResource(iconBackgroundLight);
				OpenSearch.SetBackgroundResource(0);
				FilterLayout.Visibility = ViewStates.Visible;

				DistanceFilters.RequestLayout(); //Without this, there would be an empty space in place of UseGeoContainer in this situation:
				// As a not logged in user, have distance filtering (UseGeoContainer) open. Switch to search, and bring up the keyboard. Log in with a user who does not filter by distance. Switch from search mode to filters.

				if ((bool)Settings.SearchOpen)
				{
					SearchLayout.Visibility = ViewStates.Gone;
					Settings.SearchOpen = false;
				}
				
				if (!listTypeShown)
				{
					listTypeShown = true;
					listTypeClicked = true;
				}

				Settings.FiltersOpen = true;				
				Session.LastDataRefresh = null;
				if (Session.LastSearchType == Constants.SearchType_Search)
				{
					Session.ResultsFrom = 1;
					recenterMap = true;
					Task.Run(() => LoadList());
				}
			}
			else
			{
				OpenFilters.SetBackgroundResource(0);
				FilterLayout.Visibility = ViewStates.Gone;
				Settings.FiltersOpen = false;
			}
		}

		private void OpenSearch_Click(object sender, EventArgs e)
		{
			imm.HideSoftInputFromWindow(DistanceSourceAddressText.WindowToken, 0);
			if (!(bool)Settings.SearchOpen)
			{
				OpenFilters.SetBackgroundResource(0);
				OpenSearch.SetBackgroundResource(iconBackgroundLight);
				SearchLayout.Visibility = ViewStates.Visible;

				if ((bool)Settings.FiltersOpen)
				{
					FilterLayout.Visibility = ViewStates.Gone;
					Settings.FiltersOpen = false;
				}

				if (!searchInShown)
				{
					searchInShown = true;
					searchInClicked = true;
				}

				Settings.SearchOpen = true;
				Session.LastDataRefresh = null;
				if (Session.LastSearchType == Constants.SearchType_Filter)
				{
					Session.ResultsFrom = 1;
					recenterMap = true;
					Task.Run(() => LoadListSearch());
				}
			}
			else
			{
				OpenSearch.SetBackgroundResource(0);
				SearchLayout.Visibility = ViewStates.Gone;
				Settings.SearchOpen = false;
			}
		}

		private void ListView_Click(object sender, EventArgs e)
		{
			ListView.SetBackgroundResource(iconBackgroundLight);
			MapView.SetBackgroundResource(0);

			UserSearchList.Visibility = ViewStates.Visible;
			MapContainer.Visibility = ViewStates.Invisible;
			MapStreet.Visibility = ViewStates.Gone;
			MapSatellite.Visibility = ViewStates.Gone;
			Settings.IsMapView = false;

			SetResultStatus();
		}

		private async void MapView_Click(object sender, EventArgs e)
		{
			c.Log("MapView_Click");
			mapToSet = true;
			if (await CheckLocationSettings())
			{
				MapViewSecond();
			}
		}


		private void MapViewSecond()
		{
			c.Log("MapViewSecond mapSet " + mapSet + " mapToSet " + mapToSet);
			if (mapLoaded && usersLoaded && !(bool)Settings.IsMapView)
			{
				if (!mapSet)
				{
					StartLoaderAnim();
					recenterMap = true;
					Task.Run(() => { SetMap(); });
				}
				else
				{
					mapToSet = false;

					MapView.SetBackgroundResource(iconBackgroundLight);
					ListView.SetBackgroundResource(0);

					UserSearchList.Visibility = ViewStates.Invisible;
					MapContainer.Visibility = ViewStates.Visible;
					MapStreet.Visibility = ViewStates.Visible;
					MapSatellite.Visibility = ViewStates.Visible;
					Settings.IsMapView = true;
				}
			}
		}

		private void SearchTerm_KeyPress(object sender, View.KeyEventArgs e)
		{
			e.Handled = false;
			if (e.Event.Action == KeyEventActions.Down && e.KeyCode == Keycode.Enter)
			{
				imm.HideSoftInputFromWindow(SearchTerm.WindowToken, 0);
				string term = SearchTerm.Text.Trim();
				Session.ResultsFrom = 1;
				recenterMap = true;
				Task.Run(() => LoadListSearch());				
			}
		}

		private void SearchIn_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
		{
			if (searchInClicked)
			{
				searchInClicked = false;
				return;
			}
			imm.HideSoftInputFromWindow(SearchTerm.WindowToken, 0);
			Session.ResultsFrom = 1;
			recenterMap = true;
			Task.Run(() => LoadListSearch());
		}

		private void ListType_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
		{
			if (listTypeClicked)
			{
				listTypeClicked = false;
				return;
			}
			if (c.IsLoggedIn())
			{
				Session.ListType = res.GetStringArray(Resource.Array.ListTypeEntries_values)[ListType.SelectedItemId];
			}
			else
			{
				Session.ListType = res.GetStringArray(Resource.Array.ListTypeEntriesNotLoggedIn_values)[ListType.SelectedItemId];
			}
			Session.ResultsFrom = 1;
			recenterMap = true;
			Task.Run(() => LoadList());
		}

		private void SortBy_LastActiveDate_Click(object sender, EventArgs e)
		{
			SortBy_LastActiveDate.SetBackgroundResource(iconBackgroundDark);
			SortBy_ResponseRate.SetBackgroundResource(0);
			SortBy_RegisterDate.SetBackgroundResource(0);
			Session.SortBy = "LastActiveDate";
			Session.ResultsFrom = 1;
			recenterMap = true;
			Task.Run(() => LoadList());
		}

		private void SortBy_ResponseRate_Click(object sender, EventArgs e)
		{
			SortBy_LastActiveDate.SetBackgroundResource(0);
			SortBy_ResponseRate.SetBackgroundResource(iconBackgroundDark);
			SortBy_RegisterDate.SetBackgroundResource(0);
			Session.SortBy = "ResponseRate";
			Session.ResultsFrom = 1;
			recenterMap = true;
			Task.Run(() => LoadList());
		}
		private void SortBy_RegisterDate_Click(object sender, EventArgs e)
		{
			SortBy_LastActiveDate.SetBackgroundResource(0);
			SortBy_ResponseRate.SetBackgroundResource(0);
			SortBy_RegisterDate.SetBackgroundResource(iconBackgroundDark);
			Session.SortBy = "RegisterDate";
			Session.ResultsFrom = 1;
			recenterMap = true;
			Task.Run(() => LoadList());
		}

		private void OrderBy_Click(object sender, EventArgs e)
		{
			if (Session.OrderBy == "desc")
			{
				Session.OrderBy = "asc";
				TooltipCompat.SetTooltipText(OrderBy, res.GetString(Resource.String.Ascending));
				//OrderBy.TooltipText = res.GetString(Resource.String.Ascending);
				OrderBy.SetImageResource(icAscending);
			}
			else
			{
				Session.OrderBy = "desc";
				TooltipCompat.SetTooltipText(OrderBy, res.GetString(Resource.String.Descending));
				//OrderBy.TooltipText = res.GetString(Resource.String.Descending);
				OrderBy.SetImageResource(icDescending);
			}
			Session.ResultsFrom = 1;
			recenterMap = true;
			Task.Run(() => LoadList());
		}

		private void DistanceFiltersOpenClose_Click(object sender, EventArgs e)
		{
			if (DistanceFilters.Visibility == ViewStates.Gone)
			{
				DistanceFilters.Visibility = ViewStates.Visible;
				Settings.GeoFiltersOpen = true;
				DistanceFiltersOpenClose.ScaleY = 1;
				TooltipCompat.SetTooltipText(DistanceFiltersOpenClose, res.GetString(Resource.String.DistanceFiltersClose));
				//DistanceFiltersOpenClose.TooltipText = res.GetString(Resource.String.DistanceFiltersClose);
			}
			else
			{
				DistanceFilters.Visibility = ViewStates.Gone;
				Settings.GeoFiltersOpen = false;
				DistanceFiltersOpenClose.ScaleY = -1;
				TooltipCompat.SetTooltipText(DistanceFiltersOpenClose, res.GetString(Resource.String.DistanceFiltersOpen));
				//DistanceFiltersOpenClose.TooltipText = res.GetString(Resource.String.DistanceFiltersOpen);
			}
		}

		private void UseGeo_Click(object sender, EventArgs e)
		{
			if (UseGeoNo.Checked)
			{
				imm.HideSoftInputFromWindow(DistanceSourceAddressText.WindowToken, 0);
				UseGeoContainer.Visibility = ViewStates.Gone;
				Session.GeoFilter = false;
				if ((bool)Settings.IsMapView && (!(bool)Session.UseLocation || !c.IsLocationEnabled()))
				{
					ListView_Click(null, null);
				}
				recenterMap = true;
				Task.Run(() => LoadList());
			}
			else
			{
				UseGeoContainer.Visibility = ViewStates.Visible;
				Session.GeoFilter = true;
				if (!(bool)Session.GeoSourceOther && c.IsOwnLocationAvailable() || (bool)Session.GeoSourceOther && c.IsOtherLocationAvailable())
				{
					recenterMap = true;
					Task.Run(() => LoadList());
				}
			}
		}

		private void RefreshDistance_Click(object sender, EventArgs e)
		{
			Session.ResultsFrom = 1;
			recenterMap = true;
			Task.Run(() => LoadList());
		}

		private async void DistanceSource_Click(object sender, EventArgs e)
		{
			if (DistanceSourceCurrent.Checked)
			{
				c.Log("DistanceSource_Click current IsLocationEnabled " + c.IsLocationEnabled() + " UseLocation " + Session.UseLocation);

				imm.HideSoftInputFromWindow(DistanceSourceAddressText.WindowToken, 0);
				DistanceSourceAddressText.Visibility = ViewStates.Gone;
				AddressOK.Visibility = ViewStates.Gone;
				if (!c.IsLocationEnabled())
				{
					distanceSourceCurrentClicked = true;
					ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.AccessFineLocation }, 2);
					return;
				}

				if (!(bool)Session.UseLocation) //can only mean, user is logged in
				{
					string dialogResponse = await c.DisplayCustomDialog("", res.GetString(Resource.String.MapViewNoUseLocation),
				res.GetString(Resource.String.DialogYes), res.GetString(Resource.String.DialogNo));
					if (dialogResponse == res.GetString(Resource.String.DialogYes))
					{
						if (UpdateLocationSetting(true))
						{
							Session.GeoSourceOther = false;
							Session.LastDataRefresh = null;
							firstLocationAcquired = false; // it should be false to start with

							ResultSet.Visibility = ViewStates.Visible;
							ResultSet.Text = res.GetString(Resource.String.GettingLocation);

							StartLocationUpdates(); //first location update will load the list
						}
					}
					else
					{
						SetDistanceSourceAddress();
					}
					return;
				}
			}
			else
			{
				DistanceSourceAddressText.Visibility = ViewStates.Visible;
				AddressOK.Visibility = ViewStates.Visible;
			}

			Session.GeoSourceOther = DistanceSourceAddress.Checked;
			if (!(bool)Session.GeoSourceOther && c.IsOwnLocationAvailable() || (bool)Session.GeoSourceOther && c.IsOtherLocationAvailable())
			{
				Session.ResultsFrom = 1;
				recenterMap = true;
				await Task.Run(() => LoadList());
			}
		}

		private void SetDistanceSourceAddress()
		{
			DistanceSourceAddress.Checked = true;
			DistanceSourceAddressText.Visibility = ViewStates.Visible;
			AddressOK.Visibility = ViewStates.Visible;
			Session.GeoSourceOther = true;
		}

		private void DistanceSourceAddressText_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
		{
			MatchCoordinates(false);
		}

		private void DistanceSourceAddressText_KeyPress(object sender, View.KeyEventArgs e)
		{
			e.Handled = false;
			if (e.Event.Action == KeyEventActions.Down && e.KeyCode == Keycode.Enter)
			{
				imm.HideSoftInputFromWindow(DistanceSourceAddressText.WindowToken, 0);
				GetAddressCoordinates();
			}
		}

		private void DistanceSourceAddressText_FocusChange(object sender, View.FocusChangeEventArgs e)
		{
			if (!e.HasFocus)
			{
				MatchCoordinates(true);
				DistanceSourceAddressText.Background.ClearColorFilter();
			}
		}

		private void AddressOK_Click(object sender, EventArgs e)
		{
			imm.HideSoftInputFromWindow(DistanceSourceAddressText.WindowToken, 0);
			GetAddressCoordinates();
		}

		private bool MatchCoordinates(bool reformat)
		{
			if (distanceSourceAddressTextChanging)
			{
				return true;
			}
			distanceSourceAddressTextChanging = true;
			string lookup = DistanceSourceAddressText.Text.Trim();
			Regex regex = new Regex(@"^(-?[0-9]+(\.[0-9]+)?)[,\s]+(-?[0-9]+(\.[0-9]+)?)$"); //when the email extension is long, it will take ages for the regex to finish
			var matches = regex.Matches(lookup);
			if (matches.Count != 0)
			{
				var match = matches[0];
				double latValue= double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
				double longValue= double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
				if (latValue <= 90 && latValue >= -90 && longValue <= 180 && longValue >= -180)
				{
					Session.OtherLatitude = latValue;
					Session.OtherLongitude = longValue;
					Session.OtherAddress = null;

					DistanceSourceAddressText.Background.SetColorFilter(BlendModeColorFilterCompat.CreateBlendModeColorFilterCompat(new Color(ContextCompat.GetColor(this, Resource.Color.ColorFilterGreen)), BlendModeCompat.SrcAtop));

					if (reformat)
					{
						DistanceSourceAddressText.Text = latValue + ", " + longValue;
						DistanceSourceAddressText.ClearFocus();
					}
					distanceSourceAddressTextChanging = false;
					return true;
				}
				else
				{
					DistanceSourceAddressText.Background.ClearColorFilter();
					distanceSourceAddressTextChanging = false;
					return false;
				}
			}
			else
			{
				DistanceSourceAddressText.Background.ClearColorFilter();
			}
			distanceSourceAddressTextChanging = false;
			return false;
		}

		private async void GetAddressCoordinates()
		{
			if (DistanceSourceAddressText.Text.Trim() != "")
			{
				if (!MatchCoordinates(true))
				{
					AddressOK.Enabled = false;
					AddressOK.ImageAlpha = 128; //changes only the checkmark, not the background

					StartLoaderAnim();
					ResultSet.Visibility = ViewStates.Visible;
					ResultSet.Text = res.GetString(Resource.String.ConvertingAddress);

					string responseString = "";
					
					string lookup = DistanceSourceAddressText.Text.Trim();
					string url = "action=geocoding&Address=" + lookup + "&ID=" + Session.ID + "&SessionID=" + Session.SessionID;						
					responseString = await c.MakeRequest(url);
					if (responseString.Substring(0,2) == "OK")
					{
						responseString = responseString.Substring(3);
						int sep1Pos = responseString.IndexOf("|");
						int sep2Pos = responseString.IndexOf("|", sep1Pos + 1);

						distanceSourceAddressTextChanging = true;
						DistanceSourceAddressText.Text = Session.OtherAddress = responseString.Substring(0, sep1Pos);
						DistanceSourceAddressText.ClearFocus();
						distanceSourceAddressTextChanging = false;

						Session.OtherLatitude = double.Parse(responseString.Substring(sep1Pos + 1, sep2Pos - sep1Pos - 1), CultureInfo.InvariantCulture);
						Session.OtherLongitude = double.Parse(responseString.Substring(sep2Pos + 1), CultureInfo.InvariantCulture);

						recenterMap = true;
						await Task.Run(() => LoadList());
					}
					else if (responseString == "ZERO_RESULTS")
					{
						c.Snack(Resource.String.AddressNoResult);
					}
					else if (responseString == "OVER_QUERY_LIMIT")
					{
						c.SnackIndef(Resource.String.OverQueryLimit);
					}
					else //Network error, authorization error or other geocoding status code
					{
						c.ReportError(responseString);
					}

					SetResultStatus();
					StopLoaderAnim();

					AddressOK.Enabled = true;
					AddressOK.ImageAlpha = 255;
				}
				else
				{
					recenterMap = true;
					await Task.Run(() => LoadList());
				}
				
			}
			else
			{
				Session.OtherLatitude = null;
				Session.OtherLongitude = null;
				Session.OtherAddress = null;
			}
			DistanceSourceAddressText.Background.ClearColorFilter();

			//Android's Geocoder returns null, even with repeated requests. Must resort to Geocoding API requests, even if it has a free quota limit.
			//HERE has a free Geocoding SDK service, which might be worth looking into. Their examples are using Android Studio.

			/*int maxTryCount = 10;		
			try {
				int tryCount = 0;

				var geocoder = new Geocoder(this);
				Address location;
				do
				{
					tryCount++;
					var locations = await geocoder.GetFromLocationNameAsync(lookup, 10);
					location = locations?.FirstOrDefault();
					//c.CW(tryCount + " " + (location is null) + "---" + locations.Count());
				} while ((location is null) && tryCount < maxTryCount);

				Xamarin.Essentials.Location location;
				do
				{
					tryCount++;
					var locations = await Geocoding.GetLocationsAsync(lookup);
					location = locations?.FirstOrDefault();
					//c.CW(tryCount + " " + (location is null) + "---" + locations.Count());
				} while ((location is null) && tryCount < maxTryCount);

				await c.Alert(tryCount + " " + (location is null));

				if (location != null)
				{
					await c.Alert(location.Accuracy + " "  + location.ToString() + " " +  location.Latitude + " " + location.Longitude + " " + locations.Count());
				}
			}
			catch (Exception ex)
			{
				c.ReportErrorSilent(ex.Message + System.Environment.NewLine + ex.StackTrace);
			}*/
		}

		private void RevertInvalidAddress()
		{
			if (!(Session.OtherAddress is null))
			{
				distanceSourceAddressTextChanging = true;
				DistanceSourceAddressText.Text = Session.OtherAddress;
				distanceSourceAddressTextChanging = false;
			}
			else if (Session.OtherLatitude != null && Session.OtherLongitude != null)
			{
				distanceSourceAddressTextChanging = true;
				DistanceSourceAddressText.Text = Session.OtherLatitude + ", " + Session.OtherLongitude;
				distanceSourceAddressTextChanging = false;
			}
		}

		private void DistanceLimit_ProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e) //when autologin (after turning off location in Settings), this is called after OnCreate ends, but before OnResume
		{
			if (!distanceLimitChangedByCode && isAppVisible)
			{
				DistanceLimitInput.Text = DistanceLimitProgressToVal(DistanceLimit.Progress).ToString();
				DistanceLimitInput.ClearFocus();

				Session.DistanceLimit = DistanceLimitProgressToVal(DistanceLimit.Progress);
				if (ProgressTimer is null || !ProgressTimer.Enabled)
				{
					ProgressTimer = new Timer();
					ProgressTimer.Interval = Constants.DistanceChangeRefreshDelay;
					ProgressTimer.Elapsed += ProgressTimer_Elapsed;
					ProgressTimer.Start();
				}
				else if (!(ProgressTimer is null))
				{
					ProgressTimer.Stop();
					ProgressTimer.Start();
				}
			}
			else
			{
				distanceLimitChangedByCode = false;
			}
		}

		private void ProgressTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			c.Log("ProgressTimer_Elapsed");
			ProgressTimer.Stop();
			if ((bool)Session.GeoFilter && (!(bool)Session.GeoSourceOther && !c.IsOwnLocationAvailable() || (bool)Session.GeoSourceOther && !c.IsOtherLocationAvailable()))
			{
				return;
			}
			recenterMap = false;
			Task.Run(() => LoadList());
		}

		private void DistanceLimitInput_KeyPress(object sender, View.KeyEventArgs e)
		{
			e.Handled = false;
			if (e.Event.Action == KeyEventActions.Down && e.KeyCode == Keycode.Enter) //focuschange event is called too.
			{
				imm.HideSoftInputFromWindow(DistanceLimit.WindowToken, 0);
				int progress = int.Parse(DistanceLimitInput.Text);
				if (progress > Constants.MaxGoogleMapDistance)
				{
					return;
				}
				if (progress < 1)
				{
					return;
				}
				SetDistanceLimit();

				recenterMap = false;
				Task.Run(() => LoadList());
			}
		}

		private void DistanceLimitInput_FocusChange(object sender, View.FocusChangeEventArgs e) //focus does not change when we click on a button.
		{
			if (!e.HasFocus)
			{
				int progress = int.Parse(DistanceLimitInput.Text);
				if (progress > Constants.MaxGoogleMapDistance)
				{
					c.Alert(res.GetString(Resource.String.MaxDistanceMessage));
					return;
				}
				if (progress < 1)
				{
					c.Alert(res.GetString(Resource.String.MinDistanceMessage));
					return;
				}

				SetDistanceLimit();
			}			
		}

		private void SetDistanceLimit()
		{
			//invalid values are reverted.
			int progress = int.Parse(DistanceLimitInput.Text);

			if (progress > Constants.MaxGoogleMapDistance || progress < 1)
			{
				DistanceLimitInput.Text = Session.DistanceLimit.ToString();
				if (DistanceLimitInput.HasFocus)
				{
					DistanceLimitInput.SetSelection(DistanceLimitInput.Text.Length);
				}
				return;
			}

			//New values are set.
			if (progress != Session.DistanceLimit)
			{
				distanceLimitChangedByCode = true;
				if (progress <= Constants.DistanceLimitMax) //triggers DistanceLimit_ProgressChanged
				{
					DistanceLimit.Progress = DistanceLimitValToProgress(progress);
				}
				else
				{
					DistanceLimit.Progress = DistanceLimitValToProgress(Constants.DistanceLimitMax);
				}
				Session.DistanceLimit = progress;
			}
			DistanceLimitInput.ClearFocus();
		}

		private int DistanceLimitValToProgress(int value)
		{
			return value - 1;
		}

		private int DistanceLimitProgressToVal(int progress)
		{
			return progress + 1;
		}

		private void UserSearchList_Touch(object sender, View.TouchEventArgs e)
		{
			switch (e.Event.Action)
			{
				case MotionEventActions.Down:
					//scrollY returns 0. This is the alternative solution.
					if (listLoading || UserSearchList.FirstVisiblePosition != 0 || (!(UserSearchList.GetChildAt(0) is null) && UserSearchList.GetChildAt(0).Top < 0))
					{
						break;
					}
					startY = (int)e.Event.GetY();
					diff = 0; //without this, it can interfere with a click where only down and up are called, and therefore results in unnecessary refresh
					break;
				case MotionEventActions.Move:
					if (startY is null)
					{
						break;
					}
					
					int posY = (int)e.Event.GetY();

					diff = (posY - (int)startY) / pixelDensity;
					if (diff >= 0 && diff < maxY)
					{
						ReloadPulldown.SetY((-loaderHeight + diff / 2) * pixelDensity);
						ReloadPulldown.Alpha = diff / maxY;
						ReloadPulldown.Rotation = diff * 360 / maxY;
					}
					else if (diff >= maxY)
					{
						ReloadPulldown.SetY((maxY / 2 - loaderHeight) * pixelDensity);
						ReloadPulldown.Alpha = 1;
						ReloadPulldown.Rotation = 0;
						startY += (int)(diff - maxY);
					}
					else if (diff < 0)
					{
						ReloadPulldown.SetY(-loaderHeight * pixelDensity);
						startY += (int)diff;
					}
					break;
				case MotionEventActions.Up:
					if (startY is null)
					{
						break;
					}
					if (diff >= maxY)
					{
						Session.ResultsFrom = 1;
						recenterMap = true;
						if (Session.LastSearchType == Constants.SearchType_Filter)
						{
							Task.Run(() => LoadList());
						}
						else
						{
							Task.Run(() => LoadListSearch());
						}
					}
					else
					{
						PropertyValuesHolder propertyValuesHolderY = PropertyValuesHolder.OfFloat("Y", -loaderHeight * pixelDensity);
						PropertyValuesHolder propertyValuesHolderAlpha = PropertyValuesHolder.OfFloat("Alpha", 0);
						ObjectAnimator animator = ObjectAnimator.OfPropertyValuesHolder(ReloadPulldown, propertyValuesHolderY, propertyValuesHolderAlpha);
						animator.SetDuration(tweenTime);
						animator.Start();
					}
					startY = null;
					break;
			}
			e.Handled = false;
		}

		private void UserSearchList_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
		{
			Intent i = new Intent(this, typeof(ProfileViewActivity));
			i.SetFlags(ActivityFlags.ReorderToFront);
			IntentData.profileViewPageType = Constants.ProfileViewType_List;
			viewProfiles = new List<Profile>(listProfiles);
			viewIndex = e.Position;
			absoluteIndex = viewIndex + (int)Session.ResultsFrom - 1;
			absoluteFirstIndex = absoluteStartIndex = (int)Session.ResultsFrom - 1;
			c.CW("UserSearchList_ItemClick ResultsFrom " + Session.ResultsFrom + " viewIndex " + viewIndex + " absoluteIndex " + absoluteIndex + " absoluteStartIndex " + absoluteStartIndex + " listProfiles.Count " + listProfiles.Count);
			ShowListProfiles();
			StartActivity(i);
		}

		private void LoadPrevious_Click(object sender, EventArgs e)
		{
			LoadPrevious.Enabled = false; //to prevent repeated clicks
			Session.ResultsFrom = Session.ResultsFrom - Constants.MaxResultCount;
			if (Session.ResultsFrom < 1) //with consistent MaxResultCount it shouldn't happen.
			{
				Session.ResultsFrom = 1;
			}
			recenterMap = false;
			if (Session.LastSearchType == Constants.SearchType_Filter)
			{
				Task.Run(() => LoadList());
			}
			else
			{
				Task.Run(() => LoadListSearch());
			}			
		}

		private void LoadNext_Click(object sender, EventArgs e)
		{
			LoadNext.Enabled = false;
			Session.ResultsFrom = Session.ResultsFrom + listProfiles.Count;
			recenterMap = false;
			if (Session.LastSearchType == Constants.SearchType_Filter)
			{
				Task.Run(() => LoadList());
			}
			else
			{
				Task.Run(() => LoadListSearch());
			}
		}

		public void OnMapReady(GoogleMap map)
		{
			try
			{
				c.Log("OnMapReady, usersLoaded " + usersLoaded + " IsMapView " + Settings.IsMapView);

				mapLoaded = true;
				map.UiSettings.ZoomControlsEnabled = false;

				map.MapType = (int)Settings.ListMapType;
				if (Settings.ListMapType == MapTypeNormal)
				{
					MapStreet.SetBackgroundResource(Resource.Drawable.maptype_activeLeft);
					MapSatellite.SetBackgroundResource(Resource.Drawable.maptype_passiveRight);
				}
				else
				{
					MapStreet.SetBackgroundResource(Resource.Drawable.maptype_passiveLeft);
					MapSatellite.SetBackgroundResource(Resource.Drawable.maptype_activeRight);
				}

				map.SetOnMarkerClickListener(new MyMarkerClickListener(this));

				//without setting map again, map would reset on OnResume.
				thisMap = map;
				if (usersLoaded && mapToSet && !listLoading)
				{
					c.Log("ViewDidLayoutSubviews setting map");

					StartLoaderAnim();
					mapSet = false;
					recenterMap = true;
					Task.Run(() => SetMap());
				}
			}
			catch (Exception ex)
			{
				if (!onCreateError)
				{
					c.ReportErrorSilent(ex.Message + System.Environment.NewLine + ex.StackTrace);
				}
			}
		}

		private void MapStreet_Click(object sender, EventArgs e)
		{
			if (mapLoaded)
			{
				thisMap.MapType = MapTypeNormal;
				MapStreet.SetBackgroundResource(Resource.Drawable.maptype_activeLeft);
				MapSatellite.SetBackgroundResource(Resource.Drawable.maptype_passiveRight);
			}
		}

		private void MapSatellite_Click(object sender, EventArgs e)
		{
			if (mapLoaded)
			{
				thisMap.MapType = MapTypeHybrid;
				MapStreet.SetBackgroundResource(Resource.Drawable.maptype_passiveLeft);
				MapSatellite.SetBackgroundResource(Resource.Drawable.maptype_activeRight);
			}
		}

		private void MenuChatList_Click(object sender, EventArgs e)
		{
			Intent i = new Intent(this, typeof(ChatListActivity));
			i.SetFlags(ActivityFlags.ReorderToFront);
			StartActivity(i);
		}

		private void MenuChatList_Touch(object sender, View.TouchEventArgs e)
		{
			if (e.Event.Action == MotionEventActions.Down && !rippleRunning)
			{
				RippleMain.Alpha = 1;

				RippleMain.Animate().ScaleX(3f).ScaleY(3f).SetDuration(tweenTime / 2).Start();
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
				RippleMain.Animate().Alpha(0).SetDuration(tweenTime / 2).Start();
			});
			rippleTimer.Interval = tweenTime / 2;
			rippleTimer.Elapsed += T_Elapsed2;
			rippleTimer.Start();
		}

		private void T_Elapsed2(object sender, ElapsedEventArgs e)
		{
			rippleTimer.Stop();
			RunOnUiThread(() => {
				RippleMain.ScaleX = 1;
				RippleMain.ScaleY = 1;
			});
			rippleRunning = false;
		}

		private void StartLoaderAnim()
		{
			if (!(anim_pulldown is null) && anim_pulldown.IsRunning)
			{
				return;
			}

			RefreshDistance.Enabled = false;
			//the same animation cannot be applied to different icons, because the pivot point changes.
			Animation anim = Android.Views.Animations.AnimationUtils.LoadAnimation(this, Resource.Animation.rotate);
			Animation anim_small = Android.Views.Animations.AnimationUtils.LoadAnimation(this, Resource.Animation.rotate);
			RefreshDistanceImage.StartAnimation(anim);
			LoaderCircle.StartAnimation(anim_small);
			LoaderCircle.Visibility = ViewStates.Visible;
;
			anim_pulldown = ObjectAnimator.OfFloat(ReloadPulldown, "Rotation", 360);
			anim_pulldown.SetDuration(loaderAnimTime);
			anim_pulldown.SetInterpolator(new LinearInterpolator());
			anim_pulldown.RepeatCount = -1;
			anim_pulldown.Start();
		}

		private void StopLoaderAnim()
		{
			RefreshDistance.Enabled = true;
			RefreshDistanceImage.ClearAnimation();
			LoaderCircle.Visibility = ViewStates.Gone;
			LoaderCircle.ClearAnimation();

			anim_pulldown.Cancel();
            HidePulldown();
		}

		private void HidePulldown()
		{
			ObjectAnimator animator = ObjectAnimator.OfFloat(ReloadPulldown, "Alpha", 0);
			animator.SetDuration(tweenTime);
			animator.Start();
		}

		public void LoadList()
		{
			try
			{
				c.Log("LoadList listLoading " + listLoading + " caller " + new StackFrame(1, true).GetMethod().Name);
				
				if (listLoading)
				{
					return;
				}

				if (Session.GeoFilter is null) //in case user logged out before location timeout
                {
                    return;
                }

				//last check
				if ((bool)Session.GeoFilter && (!(bool)Session.GeoSourceOther && !c.IsOwnLocationAvailable() || (bool)Session.GeoSourceOther && !c.IsOtherLocationAvailable()))
				{
					RunOnUiThread(() => {
						SetResultStatus();
						c.Log("Exiting loadlist GeoFilter " + Session.GeoFilter + " GeoSourceOther " + Session.GeoSourceOther
							+ " own location " + c.IsOwnLocationAvailable() + " other location " + c.IsOtherLocationAvailable());
						snack = c.SnackIndef(Resource.String.GeoFilterNoLocation);
						if (ReloadPulldown.Alpha == 1)
						{
							HidePulldown();
						}
					});
					return;
				}

				if (c.snackPermanentText == Resource.String.GeoFilterNoLocation && !(snack is null) && snack.IsShown)
				{
					RunOnUiThread(() =>
					{
						snack.Dismiss();
					});
				}

				RunOnUiThread(() => {
					SetDistanceLimit();
					RevertInvalidAddress();
				});

				listLoading = true;

				RunOnUiThread(() => {
					StartLoaderAnim();
					ResultSet.Visibility = ViewStates.Visible; 
					ResultSet.Text = res.GetString(Resource.String.LoadingList);
					LoadNext.Visibility = ViewStates.Gone;
					LoadPrevious.Visibility = ViewStates.Gone;
				});

				Session.LastSearchType = Constants.SearchType_Filter;

				string latitudeStr = (Session.Latitude is null) ? "" : ((double)Session.Latitude).ToString(CultureInfo.InvariantCulture);
				string longitudeStr = (Session.Longitude is null) ? "" : ((double)Session.Longitude).ToString(CultureInfo.InvariantCulture);
				string otherLatitudeStr = (Session.OtherLatitude is null) ? "" : ((double)Session.OtherLatitude).ToString(CultureInfo.InvariantCulture);
				string otherLongitudeStr = (Session.OtherLongitude is null) ? "" : ((double)Session.OtherLongitude).ToString(CultureInfo.InvariantCulture);

				LoadResults("action=list&ID=" + Session.ID + "&SessionID=" + Session.SessionID +
					"&Latitude=" + latitudeStr + "&Longitude=" + longitudeStr +
					"&ListType=" + Session.ListType + "&SortBy=" + Session.SortBy + "&OrderBy=" + Session.OrderBy + "&GeoFilter=" + Session.GeoFilter +
					"&GeoSourceOther=" + Session.GeoSourceOther + "&OtherLatitude=" + otherLatitudeStr + "&OtherLongitude=" + otherLongitudeStr +
					"&OtherAddress=" + c.UrlEncode(Session.OtherAddress) + "&DistanceLimit=" + Session.DistanceLimit + "&ResultsFrom=" + Session.ResultsFrom);
			}
			catch (Exception ex)
			{
				if (!onCreateError)
				{
					c.ReportErrorSilent(ex.Message + System.Environment.NewLine + ex.StackTrace + System.Environment.NewLine + c.ShowClass<Session>());
				}
			}
						
		}

		public void LoadListSearch()
		{
			try
			{
				c.Log("LoadListSearch listLoading " + listLoading + " caller " + new StackFrame(1, true).GetMethod().Name);

				if (listLoading)
				{
					return;
				}

				Session.SearchTerm = SearchTerm.Text.Trim();
				Session.SearchIn = res.GetStringArray(Resource.Array.SearchInEntries_values)[SearchIn.SelectedItemId];

				listLoading = true;

				RunOnUiThread(() =>
				{
					StartLoaderAnim();
					ResultSet.Visibility = ViewStates.Visible; 
					ResultSet.Text = res.GetString(Resource.String.LoadingList);
					LoadNext.Visibility = ViewStates.Gone;
					LoadPrevious.Visibility = ViewStates.Gone;
				});

				Session.LastSearchType = Constants.SearchType_Search;

				string latitudeStr = (Session.Latitude is null) ? "" : ((double)Session.Latitude).ToString(CultureInfo.InvariantCulture);
				string longitudeStr = (Session.Longitude is null) ? "" : ((double)Session.Longitude).ToString(CultureInfo.InvariantCulture);

				LoadResults("action=listsearch&ID=" + Session.ID + "&SessionID=" + Session.SessionID
					+ "&Latitude=" + latitudeStr + "&Longitude=" + longitudeStr + "&ListType=" + Session.ListType
					+ "&SortBy=" + Session.SortBy + "&OrderBy=" + Session.OrderBy + "&SearchTerm=" + c.UrlEncode(Session.SearchTerm)
					+ "&SearchIn=" + Session.SearchIn + "&ResultsFrom=" + Session.ResultsFrom);
			}
			catch (Exception ex)
			{
				if (!onCreateError)
				{
					c.ReportErrorSilent(ex.Message + System.Environment.NewLine + ex.StackTrace + System.Environment.NewLine + c.ShowClass<Session>());
				}
			}
		}

		private void LoadResults(string url)
		{
			c.Log("LoadResults making request");

			string responseString = c.MakeRequestSync(url);
			if (responseString.Substring(0, 2) == "OK")
			{
				responseString = responseString.Substring(3);
				int sep1Pos = responseString.IndexOf("|");
				totalResultCount = int.Parse(responseString.Substring(0, sep1Pos));
				responseString = responseString.Substring(sep1Pos + 1);

				Session.LastDataRefresh = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

				if (responseString != "") //result
				{
					ServerParser<Profile> parser = new ServerParser<Profile>(responseString);

					if (addResultsAfter)
					{
						//c.CW("addResultsAfter viewIndex " + viewIndex + " absoluteFirstIndex " + absoluteFirstIndex);

						newListProfiles = parser.returnCollection;
						viewProfiles = new List<Profile>(viewProfiles.Concat(newListProfiles));
					}
					else if (addResultsBefore)
					{
						//c.CW("addResultsBefore old viewIndex " + viewIndex + " absoluteFirstIndex " + absoluteFirstIndex);

						newListProfiles = parser.returnCollection;						
						viewProfiles = new List<Profile>(newListProfiles.Concat(viewProfiles));
						viewIndex += newListProfiles.Count;
						absoluteFirstIndex -= newListProfiles.Count;

						//c.CW("addResultsBefore new viewIndex " + viewIndex + " absoluteFirstIndex " + absoluteFirstIndex);
					}
					else
					{
						//viewProfiles should not be set here, because if the click the first/last profile, background loading will start for the previous/next range, so when we go back and click another profile, a profile from the new range will be loaded.
						listProfiles = parser.returnCollection;
						adapter = new UserSearchListAdapter(this, listProfiles);						
						newListProfiles = null;

						//c.CW("normal list absoluteFirstIndex " + absoluteFirstIndex);

						RunOnUiThread(() =>
						{
							NoResult.Visibility = ViewStates.Gone;
							ImageCache.imagesInProgress = new List<string>();
							ImageCache.imageViewToLoadLater = new Dictionary<ImageView, string>();
							UserSearchList.Adapter = adapter;
						});
					}				
				}
				else if (!(addResultsAfter || addResultsBefore)) //no result; we can get empty list when unhiding profiles 
				{
					listProfiles = new List<Profile>();
					viewProfiles = null;
					adapter = new UserSearchListAdapter(this, listProfiles);
					RunOnUiThread(() =>
					{
						UserSearchList.Adapter = adapter;
					});
				}

				usersLoaded = true;
				listLoading = false;

				mapSet = false;
				if (mapLoaded && ((bool)Settings.IsMapView || mapToSet) && !(addResultsBefore || addResultsAfter)) 
				{
					SetMap();
				}
				else if (((bool)Settings.IsMapView || mapToSet) && !(addResultsBefore || addResultsAfter)) //in case OnMapReady is not called yet
				{
					mapToSet = true;
				}
				else if (!(bool)Settings.IsMapView && !mapToSet)
				{
					RunOnUiThread(() => {
						SetResultStatus();
						StopLoaderAnim();
					});
				}
				// else map is not loaded yet
			}
			else
			{
				RunOnUiThread(() => {
					LoadPrevious.Enabled = true;
					LoadNext.Enabled = true;
					listLoading = false;
					StopLoaderAnim();
					SetResultStatus();
					c.ReportError(responseString);
				});
			}
			addResultsBefore = false;
			addResultsAfter = false;
			c.Log("LoadResults end");
		}

		private void SetResultStatus()
		{
			if (listLoading || mapSetting)
			{
				return;
			}

			if (totalResultCount is null)
			{
				NoResult.Visibility = ViewStates.Gone;
				ResultSet.Visibility = ViewStates.Gone;
				LoadPrevious.Visibility = ViewStates.Gone;
				LoadNext.Visibility = ViewStates.Gone;
				return;
			}

			if (totalResultCount == 0)
			{
				if ((bool)Settings.IsMapView)
				{
					NoResult.Visibility = ViewStates.Gone;
				}
				else
				{
					NoResult.Visibility = ViewStates.Visible;
				}
				ResultSet.Visibility = ViewStates.Gone;
				LoadPrevious.Visibility = ViewStates.Gone;
				LoadNext.Visibility = ViewStates.Gone;

				return;
			}

			if (totalResultCount > 1)
			{
				ResultSet.Text = res.GetString(Resource.String.ResultsCount).Replace("[num]", totalResultCount.ToString());
				ResultSet.Visibility = ViewStates.Visible;
			}
			else
			{
				ResultSet.Text = res.GetString(Resource.String.ResultCount);
				ResultSet.Visibility = ViewStates.Visible;
			}

			if (totalResultCount > Constants.MaxResultCount)
			{
				ResultSet.Text = res.GetString(Resource.String.ResultSetInterval).Replace("[start]", Session.ResultsFrom.ToString())
			.Replace("[end]", (Session.ResultsFrom + listProfiles.Count - 1).ToString()) + " " + ResultSet.Text;
			}

			if (totalResultCount > Session.ResultsFrom - 1 + Constants.MaxResultCount)
			{
				LoadNext.Visibility = ViewStates.Visible;
				LoadNext.Enabled = true;
			}
			else
			{
				LoadNext.Visibility = ViewStates.Gone;
			}

			if (Session.ResultsFrom - 1 > 0)
			{
				LoadPrevious.Visibility = ViewStates.Visible;
				LoadPrevious.Enabled = true;
			}
			else
			{
				LoadPrevious.Visibility = ViewStates.Gone;
			}
		}

		private async void SetMap()
		{
			MarkerOptions markerOptions;

			mapToSet = false;

			if (mapSet)
			{
				return;
			}

			if ((Session.UseLocation is null || !(bool)Session.UseLocation) && !((bool)Session.GeoFilter && (bool)Session.GeoSourceOther))
			{
				RunOnUiThread(() =>
				{
					c.Snack(Resource.String.LocationNotInUse);
					StopLoaderAnim();
				});
				return;
			}

			RunOnUiThread(() =>
			{
				ResultSet.Visibility = ViewStates.Visible; 
				ResultSet.Text = res.GetString(Resource.String.SettingMap);
				mapSetting = true;
			});

			bool result;

			try //for newer Huawei phones
			{
				if ((bool)Session.GeoFilter && (bool)Session.GeoSourceOther)
				{
					result = MoveMap(Session.OtherLatitude, Session.OtherLongitude);
				}
				else
				{
					result = MoveMap(Session.Latitude, Session.Longitude);
				}
				if (!result)
				{
					RunOnUiThread(() =>
					{
						c.Snack(Resource.String.NoLocationSet);
						mapSetting = false;
						SetResultStatus();
						StopLoaderAnim();
					});
					return;
				}
			}
			catch (Exception ex)
			{
				RunOnUiThread(() =>
				{
					c.Alert(res.GetString(Resource.String.GoogleMapsNotAvailable));
					mapSetting = false;
					SetResultStatus();
					StopLoaderAnim();
				});
				c.ReportErrorSilent(ex.Message + System.Environment.NewLine + ex.StackTrace);
				return;
			}

			RunOnUiThread(() => {
				MapView.SetBackgroundResource(iconBackgroundLight);
				ListView.SetBackgroundResource(0);

				UserSearchList.Visibility = ViewStates.Gone;
				MapContainer.Visibility = ViewStates.Visible;
				MapStreet.Visibility = ViewStates.Visible;
				MapSatellite.Visibility = ViewStates.Visible;
				Settings.IsMapView = true;
				mapSet = true;
			});

			profileMarkers = new List<Marker>();
			foreach (Profile profile in listProfiles)
			{
				if (profile.Latitude != null && profile.Longitude != null && profile.LocationTime != null) //location available
				{
					ImageCache im = new ImageCache(this);
					Bitmap imageBitmap = await im.LoadBitmap(profile.ID.ToString(), profile.Pictures[0]);

					Bitmap smallMarker = Bitmap.CreateBitmap((int)(Settings.MapIconSize * pixelDensity) + 2, (int)(Settings.MapIconSize * pixelDensity) + 2, imageBitmap.GetConfig()); //Bitmap.CreateScaledBitmap(imageBitmap, (int)(Settings.MapIconSize * pixelDensity), (int)(Settings.MapIconSize * pixelDensity), false);
					Canvas canvas = new Canvas(smallMarker);
					canvas.DrawColor(Color.Black);
					canvas.DrawBitmap(imageBitmap, new Rect(0, 0, imageBitmap.Width, imageBitmap.Height), new Rect(1, 1, 1 + (int)(Settings.MapIconSize * pixelDensity), 1 + (int)(Settings.MapIconSize * pixelDensity)), null);

					LatLng location = new LatLng((double)profile.Latitude, (double)profile.Longitude);
					markerOptions = new MarkerOptions();
					markerOptions.SetPosition(location);
					markerOptions.SetTitle(profile.ID.ToString());
					markerOptions.SetIcon(BitmapDescriptorFactory.FromBitmap(smallMarker));
					markerOptions.Anchor(0.5f, 0.5f);
					this.RunOnUiThread(() =>
					{
						Marker marker = thisMap.AddMarker(markerOptions);
						profileMarkers.Add(marker);
					});
				}
			}

			this.RunOnUiThread(() =>
			{
				mapSetting = false;
				SetResultStatus();
				StopLoaderAnim();
			});
		}

		public bool MoveMap(double? latitude, double? longitude)
		{
			if (!(latitude is null) && !(longitude is null))
			{
				LatLng location = new LatLng((double)latitude, (double)longitude);

				RunOnUiThread(() =>
				{
					thisMap.Clear();

					if (recenterMap)
					{
						if (c.IsLocationEnabled())
						{
							thisMap.MyLocationEnabled = true;
							thisMap.UiSettings.MyLocationButtonEnabled = true;
						}
						else
						{
							thisMap.MyLocationEnabled = false;
							thisMap.UiSettings.MyLocationButtonEnabled = false;
						}

						if ((bool)Session.GeoFilter && Session.LastSearchType == Constants.SearchType_Filter) //no geo filter on free text search
						{
							MarkerOptions markerOptions = new MarkerOptions();
							markerOptions.SetPosition(new LatLng((double)latitude, (double)longitude));
							thisMap.AddMarker(markerOptions);

							CircleOptions circleOptions = new CircleOptions();
							circleOptions.InvokeCenter(new LatLng((double)latitude, (double)longitude));
							circleOptions.InvokeRadius((double)Session.DistanceLimit * 1000);
							circleOptions.InvokeStrokeColor(Color.Black);
							circleOptions.InvokeFillColor(Color.Argb(18, 0, 205, 0));
							circleOptions.InvokeStrokeWidth(2); // is in pixels, and floored to int. No anti-aliasing.
							thisMap.AddCircle(circleOptions);
						}

						CameraPosition.Builder builder = CameraPosition.InvokeBuilder();
						builder.Target(location);
						builder.Zoom(GetZoomLevel((double)latitude));
						CameraPosition cameraPosition = builder.Build();
						CameraUpdate cameraUpdate = CameraUpdateFactory.NewCameraPosition(cameraPosition);
						thisMap.MoveCamera(cameraUpdate);
					}
					else //change only circle radius
					{
						CircleOptions circleOptions = new CircleOptions();
						circleOptions.InvokeCenter(new LatLng((double)latitude, (double)longitude));
						circleOptions.InvokeRadius((double)Session.DistanceLimit * 1000);
						circleOptions.InvokeStrokeColor(Color.Black);
						circleOptions.InvokeFillColor(Color.Argb(18, 0, 205, 0));
						circleOptions.InvokeStrokeWidth(2);
						thisMap.AddCircle(circleOptions);
					}
				});

				return true;
			}
			else
			{
				return false;
			}
		}

		public float GetZoomLevel(double latitude)
		{
			//fact: a 360 dp wide screen displays a 220 km diameter circle at equator at zoom level 8.
			float ratio = (float) 360 / 220; // 360 / 220 would result in 1 without cast.

			float circleDpZoom8 = (float)(ratio * Session.DistanceLimit * 2 / Math.Sin(Math.PI / 180 * (90 - latitude)));
			int area = (MapContainer.Width < MapContainer.Height) ? MapContainer.Width : MapContainer.Height;
			float zoomOutRatio = circleDpZoom8 / area * pixelDensity;
			float zoomLevel = (float)(8 - Math.Log(zoomOutRatio) / Math.Log(2));

			return zoomLevel;
		}

		public void ShowListProfiles()
		{
			string str = "";
			foreach (Profile profile in listProfiles)
			{
				str += profile.ID + " ";
			}
			c.CW("ShowListProfiles " + str);
		}
	}

	public class MyMarkerClickListener : Java.Lang.Object, IOnMarkerClickListener
	{
		Context context;
		public MyMarkerClickListener(Context context)
		{
			this.context = context;
		}

		public bool OnMarkerClick(Marker marker)
		{
			if (!(marker.Title is null))
			{
				int ID = int.Parse(marker.Title);
				int index;
				for(index=0; index<ListActivity.listProfiles.Count; index++)
				{
					if (ListActivity.listProfiles[index].ID == ID)
					{
						break;
					}
				}
				Intent i = new Intent(context, typeof(ProfileViewActivity));
				i.SetFlags(ActivityFlags.ReorderToFront);
				IntentData.profileViewPageType = Constants.ProfileViewType_List;
				ListActivity.viewProfiles = new List<Profile>(ListActivity.listProfiles);
				ListActivity.viewIndex = index;
				ListActivity.absoluteIndex = index + (int)Session.ResultsFrom - 1;
				ListActivity.absoluteFirstIndex = ListActivity.absoluteStartIndex = (int)Session.ResultsFrom - 1;
				
				context.StartActivity(i);
			}
			return true;
		}
	}
}