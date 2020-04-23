using Android.Animation;
using Android.App;
using Android.Content;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Graphics;
using Android.OS;
using Android.Support.Constraints;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Timers;

namespace LocationConnection
{
	[Activity(MainLauncher = false, ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
	public class ProfileViewActivity : BaseActivity, IOnMapReadyCallback
	{
		public ConstraintLayout ScrollLayout, ProfileImageContainer, Footer;
		LinearLayout MapContainer, CircleContainer;
		Android.Support.V7.Widget.Toolbar MenuContainer;
		View EditSpacer, HeaderBackground, PercentProgress, MapTopSeparator, MapBottomSeparator, NavigationSpacer,
			EditSelfHeader, RippleImageEditBack, RippleImage, RippleImageNext, RippleImagePrev;
		TextView Name, Username, ResponseRate, LastActiveDate, RegisterDate, Description, LocationTime, DistanceText;
		public TouchConstraintLayout ProfileImageScroll;
		SupportMapFragment ProfileViewMap;
		ImageButton EditSelfBack, BackButton, PreviousButton, HideButton, LikeButton, NextButton;
		Button EditSelf, MapStreet, MapSatellite;

		Android.Content.Res.Resources res;
		bool mapLoaded;
		bool userLoaded;
		bool mapSet;
		GoogleMap thisMap;
		Marker thisMarker;
		byte pageType;
		Profile displayUser;
		float navigationSpacerHeight;
		float paddingSelfPage;
		float paddingProfilePage;
		float percentProgressWidth;
		float buttonElevation;
		float counterCircleSize;

		//image scrolling
		int clickTime = 300;
		int swipeMinDistance = 15;
		double swipeMinSpeed = 0.2;
		float decelerationRate = 0.003f; // for vertical scrolling, dp/ms^2

		float touchStartX;
		float touchStartY;
		int startScrollX;
		int totalScroll;
		bool isTouchDown;
		public bool horizontalCancelled;
		public int currentScrollX;
		public Stopwatch stw = new Stopwatch();
		float prevPos;
		long prevTime;
		float prevSpeedX;
		float speedX;
		float prevSpeedY;
		float speedY;

		int startPic;
		float touchCurrentX;
		float touchCurrentY;
		float currentOffsetX;
		float currentOffsetY;		

		int startScrollY;
		int totalScrollHeight;
		float prevDiffY;

		public ObjectAnimator animator;

		public Timer scrollTimer;
		float startValue;
		float endValue;
		float timeValue;
		float middleTime;
		float topSpeed;
		float acceleration;

		bool verticalEnabled;

		Timer rippleTimer, refreshTimer;
		int refreshFrequency = 1000;
		ImageButton pressTarget;
		bool rippleRunning;

		List<View> counterCircles;

		int icLike;
		int icHide;
		int icLiked;
		int icRefresh;
		int icChatOne;
		int counterCircle;
		int counterCircleSelected;

		public bool cancelImageLoading;
		public int currentID;

		LocationReceiver locationReceiver;

		int footerHeight, mapHeight;

		protected override void OnCreate(Bundle savedInstanceState)
        {
			try {
				base.OnCreate(savedInstanceState);
				if (!ListActivity.initialized) { return; }

				if (Settings.DisplaySize == 1)
				{
					SetContentView(Resource.Layout.activity_profileview_normal);
					icLike = Resource.Drawable.ic_like_normal;
					icHide = Resource.Drawable.ic_hide_normal;
					icLiked = Resource.Drawable.ic_liked_normal;
					icRefresh = Resource.Drawable.ic_reinstate_normal;
					icChatOne = Resource.Drawable.ic_chat_one_normal;
					counterCircle = Resource.Drawable.counterCircle_normal;
					counterCircleSelected = Resource.Drawable.counterCircle_selected_normal;

					navigationSpacerHeight = float.Parse(Resources.GetString(Resource.String.navigationSpacerHeightNormal), CultureInfo.InvariantCulture); //Resource.Dimen gives 2.131165E+09
					paddingSelfPage = float.Parse(Resources.GetString(Resource.String.paddingSelfPageNormal), CultureInfo.InvariantCulture);
					paddingProfilePage = float.Parse(Resources.GetString(Resource.String.paddingProfilePageNormal), CultureInfo.InvariantCulture);
					percentProgressWidth = float.Parse(Resources.GetString(Resource.String.percentProgressWidthNormal), CultureInfo.InvariantCulture);
					buttonElevation = float.Parse(Resources.GetString(Resource.String.buttonElevationNormal), CultureInfo.InvariantCulture);
					counterCircleSize = float.Parse(Resources.GetString(Resource.String.counterCircleSizeNormal), CultureInfo.InvariantCulture);
				}
				else
				{
					SetContentView(Resource.Layout.activity_profileview_small);
					icLike = Resource.Drawable.ic_like_small;
					icHide = Resource.Drawable.ic_hide_small;
					icLiked = Resource.Drawable.ic_liked_small;
					icRefresh = Resource.Drawable.ic_reinstate_small;
					icChatOne = Resource.Drawable.ic_chat_one_small;
					counterCircle = Resource.Drawable.counterCircle_small;
					counterCircleSelected = Resource.Drawable.counterCircle_selected_small;

					navigationSpacerHeight = float.Parse(Resources.GetString(Resource.String.navigationSpacerHeightSmall), CultureInfo.InvariantCulture);
					paddingSelfPage = float.Parse(Resources.GetString(Resource.String.paddingSelfPageSmall), CultureInfo.InvariantCulture);
					paddingProfilePage = float.Parse(Resources.GetString(Resource.String.paddingProfilePageSmall), CultureInfo.InvariantCulture);
					percentProgressWidth = float.Parse(Resources.GetString(Resource.String.percentProgressWidthSmall), CultureInfo.InvariantCulture);
					buttonElevation = float.Parse(Resources.GetString(Resource.String.buttonElevationSmall), CultureInfo.InvariantCulture);
					counterCircleSize = float.Parse(Resources.GetString(Resource.String.counterCircleSizeSmall), CultureInfo.InvariantCulture);
				}

				MainLayout = FindViewById<ProfileViewConstraintLayout>(Resource.Id.MainLayout);
				ScrollLayout = FindViewById<ConstraintLayout>(Resource.Id.ScrollLayout);
				EditSpacer = FindViewById<View>(Resource.Id.EditSpacer);
				HeaderBackground = FindViewById<View>(Resource.Id.HeaderBackground);
				Name = FindViewById<TextView>(Resource.Id.Name);
				Username = FindViewById<TextView>(Resource.Id.Username);
				PercentProgress = FindViewById<View>(Resource.Id.PercentProgress);
				ResponseRate = FindViewById<TextView>(Resource.Id.ResponseRate);
				LastActiveDate = FindViewById<TextView>(Resource.Id.LastActiveDate);
				RegisterDate = FindViewById<TextView>(Resource.Id.RegisterDate);
				ProfileImageContainer = FindViewById<ConstraintLayout>(Resource.Id.ProfileImageContainer);
				ProfileImageScroll = FindViewById<TouchConstraintLayout>(Resource.Id.ProfileImageScroll);
				CircleContainer = FindViewById<LinearLayout>(Resource.Id.CircleContainer);
				MenuContainer = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.MenuContainer);
				Footer = FindViewById<ConstraintLayout>(Resource.Id.Footer);
				Description = FindViewById<TextView>(Resource.Id.Description);
				LocationTime = FindViewById<TextView>(Resource.Id.LocationTime);
				DistanceText = FindViewById<TextView>(Resource.Id.DistanceText);
				MapContainer = FindViewById<LinearLayout>(Resource.Id.MapContainer);
				ProfileViewMap = (SupportMapFragment)SupportFragmentManager.FindFragmentById(Resource.Id.ProfileViewMap);
				ProfileViewMap.GetMapAsync(this);
				MapTopSeparator= FindViewById<View>(Resource.Id.MapTopSeparator);
				MapBottomSeparator = FindViewById<View>(Resource.Id.MapBottomSeparator);
				MapStreet = FindViewById<Button>(Resource.Id.MapStreet);
				MapSatellite = FindViewById<Button>(Resource.Id.MapSatellite);
				NavigationSpacer = FindViewById<View>(Resource.Id.NavigationSpacer);
				EditSelfHeader = FindViewById<View>(Resource.Id.EditSelfHeader);
				EditSelfBack = FindViewById<ImageButton>(Resource.Id.EditSelfBack);
				EditSelf = FindViewById<Button>(Resource.Id.EditSelf);
				BackButton = FindViewById<ImageButton>(Resource.Id.BackButton);
				PreviousButton = FindViewById<ImageButton>(Resource.Id.PreviousButton);
				HideButton = FindViewById<ImageButton>(Resource.Id.HideButton);
				LikeButton = FindViewById<ImageButton>(Resource.Id.LikeButton);
				NextButton = FindViewById<ImageButton>(Resource.Id.NextButton);
				RippleImageEditBack = FindViewById<View>(Resource.Id.RippleImageEditBack);
				RippleImageNext = FindViewById<View>(Resource.Id.RippleImageNext);
				RippleImagePrev = FindViewById<View>(Resource.Id.RippleImagePrev);

				mapLoaded = false;
				ProfileViewMap.GetMapAsync(this);
				c.view = MainLayout;
				res = Resources;
				locationReceiver = new LocationReceiver();
				SetSupportActionBar(MenuContainer);
				SupportActionBar.SetDisplayShowTitleEnabled(false);

				MapStreet.Click += MapStreet_Click;
				MapSatellite.Click += MapSatellite_Click;
				ScrollLayout.Touch += ScrollLayout_Touch;
				Footer.LayoutChange += Footer_LayoutChange;
				MapContainer.LayoutChange += MapContainer_LayoutChange;
				ScrollLayout.LayoutChange += ScrollLayout_LayoutChange;
				
				EditSelfBack.Click += EditSelfBack_Click;
				EditSelf.Click += EditSelf_Click;
				BackButton.Click += BackButton_Click;
				PreviousButton.Click += PreviousButton_Click;
				NextButton.Click += NextButton_Click;
				HideButton.Click += HideButton_Click;
				LikeButton.Click += LikeButton_Click;

				EditSelfBack.Touch += EditSelfBack_Touch;
				BackButton.Touch += Button_Touch;
				NextButton.Touch += Button_Touch;
				LikeButton.Touch += Button_Touch;
				HideButton.Touch += Button_Touch;
				PreviousButton.Touch += Button_Touch;
			}
			catch (Exception ex)
			{
				c.ReportErrorSilent(ex.Message + System.Environment.NewLine + ex.StackTrace);
			}
		}

		protected override void OnResume()
		{
			try
			{
				base.OnResume();
				if (!ListActivity.initialized) { return; }

				mapSet = false;
				userLoaded = false;
				ProfileImageScroll.ScrollX = 0;
				footerHeight = -1;
				mapHeight = -1;
				cancelImageLoading = false;

				c.CW("OnResume IntentData.profileViewPageType " + IntentData.profileViewPageType);
				pageType = IntentData.profileViewPageType;

				switch (pageType)
				{
					case Constants.ProfileViewType_Self:

						if ((bool)Session.UseLocation && c.IsLocationEnabled())
						{
							RegisterReceiver(locationReceiver, new IntentFilter("balintfodor.locationconnection.LocationReceiver"));
						}
						ShowEditSpacer();

						EditSelfHeader.Visibility = ViewStates.Visible;
						EditSelfBack.Visibility = ViewStates.Visible;
						EditSelf.Visibility = ViewStates.Visible;

						BackButton.Visibility = ViewStates.Gone;

						((ConstraintLayout.LayoutParams)Name.LayoutParameters).LeftMargin = (int)(paddingSelfPage * pixelDensity);
						((ConstraintLayout.LayoutParams)Username.LayoutParameters).LeftMargin = (int)(paddingSelfPage * pixelDensity);

						PreviousButton.Visibility = ViewStates.Gone;
						HideButton.Visibility = ViewStates.Gone;
						LikeButton.Visibility = ViewStates.Gone;
						NextButton.Visibility = ViewStates.Gone;

						Session.CurrentMatch = null;
						userLoaded = true;

						LoadSelf();
						HideNavigationSpacer();

						break;

					case Constants.ProfileViewType_List:

						HideEditSpacer();

						EditSelfHeader.Visibility = ViewStates.Gone;
						EditSelfBack.Visibility = ViewStates.Gone;
						EditSelf.Visibility = ViewStates.Gone;

						BackButton.Visibility = ViewStates.Visible;

						((ConstraintLayout.LayoutParams)Name.LayoutParameters).LeftMargin = (int)(paddingProfilePage * pixelDensity);
						((ConstraintLayout.LayoutParams)Username.LayoutParameters).LeftMargin = (int)(paddingProfilePage * pixelDensity);

						PreviousButton.Visibility = ViewStates.Visible;
						NextButton.Visibility = ViewStates.Visible;
						if (c.IsLoggedIn())
						{
							HideButton.Visibility = ViewStates.Visible;
							LikeButton.Visibility = ViewStates.Visible;
						}
						else
						{
							HideButton.Visibility = ViewStates.Gone;
							LikeButton.Visibility = ViewStates.Gone;
						}

						if (ListActivity.viewProfiles.Count > Constants.MaxResultCount)
						{
							c.LogActivity("Error: ListActivity.viewProfiles.Count is greater than " + Constants.MaxResultCount + ": " + ListActivity.viewProfiles.Count);
						}

						if (ListActivity.viewIndex >= ListActivity.viewProfiles.Count) //when blocking a user from chat window, but returning to profileview list
                        {
							OnBackPressed();
							return;
                        }

						PrevLoadAction();
						NextLoadAction();
						displayUser = ListActivity.viewProfiles[ListActivity.viewIndex];
						
						Session.CurrentMatch = null;
						userLoaded = true;
						LoadUser();
						break;

					case Constants.ProfileViewType_Standalone: //coming from chat, we already know this is a match, Userrelation=3.

						if (!(IntentData.targetID is null)) //due to back button navigation, this activity may resume and pause invisibly
						{
							LoadStandalone((int)IntentData.targetID);
							IntentData.targetID = null;
						}
						break;
					default:
						break;
				}

				c.CW("OnResume ended");
				
				refreshTimer = new Timer();
				refreshTimer.Interval = refreshFrequency;
				refreshTimer.Elapsed += RefreshTimer_Elapsed;
				refreshTimer.Start();
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

			cancelImageLoading = true;

			if (pageType == Constants.ProfileViewType_Self && (bool)Session.UseLocation && c.IsLocationEnabled())
			{
				UnregisterReceiver(locationReceiver);
			}
			if (!(thisMap is null) && thisMap.MapType != Settings.ProfileViewMapType)
			{
				Settings.ProfileViewMapType = (byte)thisMap.MapType;
				c.SaveSettings();
			}
			refreshTimer.Stop();
		}

		public override bool OnCreateOptionsMenu(IMenu menu)
		{
			if (c.IsLoggedIn())
			{
				MenuInflater.Inflate(Resource.Menu.menu_profileview, menu);
			}
			return base.OnCreateOptionsMenu(menu);
		}

		public override bool OnOptionsItemSelected(IMenuItem item)
		{
			switch (item.ItemId)
			{
				case Resource.Id.MenuReport:
					Report();
					break;
				case Resource.Id.MenuBlock:
					Block();
					break;
			}
			return base.OnOptionsItemSelected(item);
		}

		private async void Report()
		{
			string dialogResponse = await c.DisplayCustomDialog(res.GetString(Resource.String.ConfirmAction), res.GetString(Resource.String.ReportDialogText),
				res.GetString(Resource.String.DialogYes), res.GetString(Resource.String.DialogNo));

			if (dialogResponse == res.GetString(Resource.String.DialogYes))
			{
				string responseString = await c.MakeRequest("action=reportprofileview&ID=" + Session.ID + "&SessionID=" + Session.SessionID + "&TargetID=" + displayUser.ID);
				if (responseString.Substring(0, 2) == "OK")
				{
					c.Snack(Resource.String.UserReported);
				}
				else
				{
					c.ReportError(responseString);
				}
			}
		}

		private async void Block()
		{
			string dialogResponse = await c.DisplayCustomDialog(res.GetString(Resource.String.ConfirmAction), res.GetString(Resource.String.BlockDialogText),
				res.GetString(Resource.String.DialogYes), res.GetString(Resource.String.DialogNo));

			if (dialogResponse == res.GetString(Resource.String.DialogYes))
			{
				if (IsUpdatingTo(displayUser.ID))
				{
					RemoveUpdatesTo(displayUser.ID);
				}
				if (IsUpdatingFrom(displayUser.ID))
				{
					RemoveUpdatesFrom(displayUser.ID);
				}

				long unixTimestamp = c.Now();
				string responseString = await c.MakeRequest("action=blockprofileview&ID=" + Session.ID + "&SessionID=" + Session.SessionID + "&TargetID=" + displayUser.ID + "&time=" + unixTimestamp);
				if (responseString.Substring(0, 2) == "OK")
				{
					if (pageType == Constants.ProfileViewType_List) {
						ListActivity.viewProfiles.RemoveAt(ListActivity.viewIndex);
						if (ListActivity.viewIndex >= 0 && ListActivity.viewIndex < ListActivity.listProfiles.Count)
						{
							ListActivity.listProfiles.RemoveAt(ListActivity.viewIndex);
						}
						ListActivity.viewIndex--;
						ListActivity.absoluteIndex--;
						ListActivity.totalResultCount--;
						NextButton_Click(null, null);
					}
					else
					{
						if (!(ListActivity.listProfiles is null))
						{
							for (int i = 0; i < ListActivity.listProfiles.Count; i++)
							{
								if (ListActivity.listProfiles[i].ID == displayUser.ID)
								{
									ListActivity.listProfiles.RemoveAt(i);
									break;
								}
							}
						}
						if (!(ListActivity.viewProfiles is null))
						{
							for (int i = 0; i < ListActivity.viewProfiles.Count; i++)
							{
								if (ListActivity.viewProfiles[i].ID == displayUser.ID)
								{
									ListActivity.viewProfiles.RemoveAt(i);
									break;
								}
							}
						}
						IntentData.blockedID = displayUser.ID;
						OnBackPressed();
					}					
					
				}
				else
				{
					c.ReportError(responseString);
				}
			}
		}

		private void LoadStandalone(int targetID)
		{
			HideEditSpacer();

			EditSelfHeader.Visibility = ViewStates.Gone;
			EditSelfBack.Visibility = ViewStates.Gone;
			EditSelf.Visibility = ViewStates.Gone;

			BackButton.Visibility = ViewStates.Visible;

			((ConstraintLayout.LayoutParams)Name.LayoutParameters).LeftMargin = (int)(paddingProfilePage * pixelDensity);
			((ConstraintLayout.LayoutParams)Username.LayoutParameters).LeftMargin = (int)(paddingProfilePage * pixelDensity);

			PreviousButton.Visibility = ViewStates.Gone;
			NextButton.Visibility = ViewStates.Gone;
			HideButton.Visibility = ViewStates.Gone;
			LikeButton.Visibility = ViewStates.Visible;

			string latitudeStr = (Session.Latitude is null) ? "" : ((double)Session.Latitude).ToString(CultureInfo.InvariantCulture);
			string longitudeStr = (Session.Longitude is null) ? "" : ((double)Session.Longitude).ToString(CultureInfo.InvariantCulture);

			string responseString = c.MakeRequestSync("action=getuserdata&ID=" + Session.ID + "&target=" + targetID
		+ "&SessionID=" + Session.SessionID + "&Latitude=" + latitudeStr + "&Longitude=" + longitudeStr); //if we used await c.MakeRequest here, the OnResume would return, and the map would set to world view before user data is loaded.
			if (responseString.Substring(0, 2) == "OK")
			{
				responseString = responseString.Substring(3);
				ServerParser<Profile> parser = new ServerParser<Profile>(responseString);
				displayUser = parser.returnCollection[0];

				UserLocationData data = GetLocationData(targetID);
                if (data != null)
                {
					displayUser.Latitude = data.Latitude;
					displayUser.Longitude = data.Longitude;
					displayUser.LocationTime = data.LocationTime;
					if (!(displayUser.Distance is null))
					{
						float distance = CalculateDistance((double)Session.Latitude, (double)Session.Longitude, data.Latitude, data.Longitude);
						displayUser.Distance = distance;
					}
				}
				
				userLoaded = true;
				LoadUser();
			}
			else if (responseString.Substring(0, 6) == "ERROR_") // UserPassive, MatchNotFound or UserNotAvailable 
			{
				Session.SnackMessage = res.GetString(Resources.GetIdentifier(responseString.Substring(6), "string", PackageName));
				if (responseString.Substring(6) == "UserNotAvailable")
				{
					IntentData.blockedID = targetID;
				}
				OnBackPressed();
			}
			else
			{
				c.ReportError(responseString);
			}
		}

		private void ShowEditSpacer()
		{
			EditSpacer.Visibility = ViewStates.Visible;
		}

		private void HideEditSpacer()
		{
			EditSpacer.Visibility = ViewStates.Gone;
		}

		private void ShowMap()
		{
			MapContainer.LayoutParameters.Height = (int)(screenWidth * Settings.MapRatio);
			MapContainer.Visibility = ViewStates.Visible;

			MapTopSeparator.Visibility = ViewStates.Visible;
			MapStreet.Visibility = ViewStates.Visible;
			MapSatellite.Visibility = ViewStates.Visible;
		}

		private void HideMap()
		{
			MapContainer.Visibility = ViewStates.Gone;

			MapTopSeparator.Visibility = ViewStates.Gone;
			MapStreet.Visibility = ViewStates.Gone;
			MapSatellite.Visibility = ViewStates.Gone;
		}

		private void ShowNavigationSpacer()
		{
			NavigationSpacer.LayoutParameters.Height = (int)(navigationSpacerHeight * pixelDensity);
			MapBottomSeparator.Visibility = ViewStates.Visible;
		}

		private void HideNavigationSpacer()
		{
			NavigationSpacer.LayoutParameters.Height = 0;
			MapBottomSeparator.Visibility = ViewStates.Gone;
		}

		private void Footer_LayoutChange(object sender, View.LayoutChangeEventArgs e)
		{
			if (footerHeight != Footer.Height)
			{
				c.CW("Footer_LayoutChange");
				SetHeight();
				footerHeight = Footer.Height;
			}
		}

		private void MapContainer_LayoutChange(object sender, View.LayoutChangeEventArgs e)
		{
			int newMapHeight = (MapContainer.Visibility == ViewStates.Visible) ? MapContainer.Height : 0;
			if (mapHeight != newMapHeight)
			{
				c.CW("MapContainer_LayoutChange");
				SetHeight();
				mapHeight = newMapHeight;
			}
		}

		private void ScrollLayout_LayoutChange(object sender, View.LayoutChangeEventArgs e)
		{
			int newMapHeight = (MapContainer.Visibility == ViewStates.Visible) ? MapContainer.Height : 0;
			if (footerHeight != Footer.Height || mapHeight != newMapHeight)
			{
				c.CW("ScrollLayout_LayoutChange");
				SetHeight();
				footerHeight = Footer.Height;
				mapHeight = newMapHeight;
			}
		}

		private void SetHeight()
		{
			int currentScrollHeight = GetScrollHeight();

			Rect r = new Rect();
			MainLayout.GetWindowVisibleDisplayFrame(r);
			int screenHeight = MainLayout.RootView.Height;
			int keyboardHeight = screenHeight - r.Bottom;
			c.CW("SetHeight currentScrollHeight " + currentScrollHeight + " MainLayout.Height " + MainLayout.Height + " " + keyboardHeight); //for when returning from profile edit with the keyboard open			

			totalScrollHeight = currentScrollHeight - MainLayout.Height - keyboardHeight;

			if (totalScrollHeight < 0)
			{
				totalScrollHeight = 0;
			}
			if (ScrollLayout.ScrollY > totalScrollHeight)
			{
				ScrollLayout.ScrollY = totalScrollHeight;
			}
			if (pageType != Constants.ProfileViewType_Self)
			{
				CastShadows(ScrollLayout.ScrollY);
			}

			if (currentScrollHeight < MainLayout.Height + keyboardHeight)
			{
				int value = screenWidth + MainLayout.Height + keyboardHeight - currentScrollHeight;

				var p = new ConstraintLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, value);
				p.TopToBottom = Resource.Id.HeaderBackground;
				p.BottomToTop = Resource.Id.Footer;
				ProfileImageContainer.LayoutParameters = p;

				//The following does not work when activity is only resumed (only refreshtimer makes it work):
				//ProfileImageContainer.LayoutParameters.Height = ProfileImageScroll.Height + MainLayout.Height - currentScrollHeight;
			}
			else
			{
				ProfileImageContainer.LayoutParameters.Height = screenWidth;
			}
		}

		private int GetScrollHeight() {
			return ((EditSpacer.Visibility == ViewStates.Visible) ? EditSpacer.Height : 0) + HeaderBackground.Height + screenWidth
				+ Footer.Height + ((MapContainer.Visibility == ViewStates.Visible) ? MapContainer.LayoutParameters.Height : 0) + NavigationSpacer.LayoutParameters.Height;
		}

		private void EditSelfBack_Touch(object sender, View.TouchEventArgs e)
		{
			if (e.Event.Action == MotionEventActions.Down && !rippleRunning)
			{
				RippleImageEditBack.Alpha = 1;

				RippleImageEditBack.Animate().ScaleX(2f).ScaleY(2f).SetDuration(tweenTime / 2).Start();
				rippleTimer = new Timer();
				rippleTimer.Interval = tweenTime / 2;
				rippleTimer.Elapsed += T_Elapsed01;
				rippleTimer.Start();
				rippleRunning = true;
			}
			e.Handled = false;
		}

		private void T_Elapsed01(object sender, ElapsedEventArgs e)
		{
			rippleTimer.Stop();
			RunOnUiThread(() => {
				RippleImageEditBack.Animate().Alpha(0).SetDuration(tweenTime / 2).Start();
			});
			rippleTimer.Interval = tweenTime / 2;
			rippleTimer.Elapsed += T_Elapsed02;
			rippleTimer.Start();
		}

		private void T_Elapsed02(object sender, ElapsedEventArgs e)
		{
			rippleTimer.Stop();
			RunOnUiThread(() => {
				RippleImageEditBack.ScaleX = 1;
				RippleImageEditBack.ScaleY = 1;
			});
			rippleRunning = false;
		}

		private void Button_Touch(object sender, View.TouchEventArgs e)
		{
			if (e.Event.Action == MotionEventActions.Down && !rippleRunning)
			{
				pressTarget = (ImageButton)sender;
				if (pressTarget==BackButton || pressTarget == PreviousButton || pressTarget==HideButton)
				{
					AnimateRipple(pressTarget.GetX(), pressTarget.GetY(), pressTarget.Width, pressTarget.Height, false);
				}
				else
				{
					AnimateRipple(pressTarget.GetX(), pressTarget.GetY(), pressTarget.Width, pressTarget.Height, true);
				}
			}
			e.Handled = false;
		}

		private void AnimateRipple(float x, float y, int srcW, int srcH, bool isNext)
		{
			if (isNext)
			{
				RippleImage = RippleImageNext;
			}
			else
			{
				RippleImage = RippleImagePrev;
			}

			RippleImage.Alpha = 1;
			RippleImage.SetX(x + (srcW - RippleImage.Width) / 2);
			RippleImage.SetY(y + (srcH - RippleImage.Height) / 2);

			RippleImage.Animate().ScaleX(3f).ScaleY(3f).SetDuration(tweenTime / 2).Start();
			rippleTimer = new Timer();
			rippleTimer.Interval = tweenTime / 2;
			rippleTimer.Elapsed += T_Elapsed1;
			rippleTimer.Start();
			rippleRunning = true;
		}
		private void T_Elapsed1(object sender, ElapsedEventArgs e)
		{
			rippleTimer.Stop();
			RunOnUiThread(() => {
				RippleImage.Animate().Alpha(0).SetDuration(tweenTime / 2).Start();
			});
			rippleTimer.Interval = tweenTime / 2;
			rippleTimer.Elapsed += T_Elapsed2;
			rippleTimer.Start();
		}

		private void T_Elapsed2(object sender, ElapsedEventArgs e)
		{
			rippleTimer.Stop();
			RunOnUiThread(() => {
				RippleImage.ScaleX = 1;
				RippleImage.ScaleY = 1;
			});
			rippleRunning = false;
		}

		private void BackButton_Click(object sender, EventArgs e)
		{
			OnBackPressed();
		}

		private void EditSelfBack_Click(object sender, EventArgs e)
		{
			OnBackPressed();
		}

		private void EditSelf_Click(object sender, EventArgs e)
		{
			Intent i = new Intent(this, typeof(ProfileEditActivity));
			i.SetFlags(ActivityFlags.ReorderToFront);
			StartActivity(i);
		}

		public void OnMapReady(GoogleMap map) //will be called later than LoadUser?
		{
			mapLoaded = true;
			map.UiSettings.ZoomControlsEnabled = true;
			
			map.MapType = (int)Settings.ProfileViewMapType;
			if (Settings.ProfileViewMapType == GoogleMap.MapTypeNormal)
			{
				MapStreet.SetBackgroundResource(Resource.Drawable.maptype_activeLeft);
				MapSatellite.SetBackgroundResource(Resource.Drawable.maptype_passiveRight);
			}
			else
			{
				MapStreet.SetBackgroundResource(Resource.Drawable.maptype_passiveLeft);
				MapSatellite.SetBackgroundResource(Resource.Drawable.maptype_activeRight);
			}

			thisMap = map;
			if (userLoaded)
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

		private void LoadSelf()
		{
			try {
				ProfileImageScroll.RemoveAllViews();
				if (!(counterCircles is null))
				{
					for (int i = 0; i < counterCircles.Count; i++)
					{
						CircleContainer.RemoveView(counterCircles[i]);
					}
				}
				counterCircles = new List<View>();

				Username.Text = Session.Username;
				Name.Text = Session.Name;
				Description.Text = Session.Description;
				SetPercentProgress((float)Session.ResponseRate);
				ResponseRate.Text = Math.Round((float)Session.ResponseRate * 100).ToString() + "%";
				LastActiveDate.Text = c.GetTimeDiffStr(Session.LastActiveDate, true);
				DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds((long)Session.RegisterDate).ToLocalTime();
				if (dt.Date == DateTime.Today)
				{
					RegisterDate.Text = dt.ToString("HH:mm");
				}
				else
				{
					RegisterDate.Text = dt.ToString("d MMMM yyyy");
				}

				LoadEmptyPictures(Session.Pictures.Length);

				if (mapLoaded)
				{
					SetMap();
				}

				currentID = (int)Session.ID;

				AddCircles(Session.Pictures.Length);

				System.IO.DirectoryInfo di = new DirectoryInfo(CacheDir.AbsolutePath);
				foreach (FileInfo file in di.GetFiles())
				{
					file.Delete();
				}

				//Unlike on iOS, Task does not get cancelled here by calling cts.Cancel() where cts is CancellationTokenSource
				Task.Run(async () =>
				{
					for (int i = 0; i < Session.Pictures.Length; i++)
					{
						if (cancelImageLoading)
						{
							c.CW("Cancelling task at self currentID " + currentID + " cancelImageLoading " + cancelImageLoading);
							break;
						}
						await LoadPicture(Session.ID.ToString(), Session.Pictures[i], i, true);
					}
				});
			}
			catch (Exception ex)
			{
				c.ReportErrorSilent(ex.Message + System.Environment.NewLine + ex.StackTrace);
			}
		}

		private void LoadUser()
		{
			try
			{
				if (c.IsLoggedIn())
				{
					switch (displayUser.UserRelation)
					{
						case 0: //default
							LikeButton.SetImageResource(icLike);
							LikeButton.Enabled = true;
							TooltipCompat.SetTooltipText(LikeButton, res.GetString(Resource.String.Like));
							//LikeButton.TooltipText = res.GetString(Resource.String.Like);
							LikeButton.Visibility = ViewStates.Visible;

							HideButton.SetImageResource(icHide);
							TooltipCompat.SetTooltipText(HideButton, res.GetString(Resource.String.Hide));
							//HideButton.TooltipText = res.GetString(Resource.String.Hide);
							HideButton.Visibility = ViewStates.Visible;
							break;
						case 1: //hid
							LikeButton.SetImageResource(icLike);
							LikeButton.Enabled = true;
							TooltipCompat.SetTooltipText(LikeButton, res.GetString(Resource.String.Like));
							//LikeButton.TooltipText = res.GetString(Resource.String.Like);
							LikeButton.Visibility = ViewStates.Gone;

							HideButton.SetImageResource(icRefresh);
							TooltipCompat.SetTooltipText(HideButton, res.GetString(Resource.String.Reinstate));
							//HideButton.TooltipText = res.GetString(Resource.String.Reinstate);
							HideButton.Visibility = ViewStates.Visible;
							break;
						case 2: //liked
							LikeButton.SetImageResource(icLiked);
							LikeButton.Enabled = false;
							TooltipCompat.SetTooltipText(LikeButton, res.GetString(Resource.String.Liked));
							//LikeButton.TooltipText = res.GetString(Resource.String.Liked);
							LikeButton.Visibility = ViewStates.Visible;

							HideButton.SetImageResource(icHide);
							TooltipCompat.SetTooltipText(HideButton, res.GetString(Resource.String.Hide));
							//HideButton.TooltipText = res.GetString(Resource.String.Hide);
							HideButton.Visibility = ViewStates.Visible;
							break;
						case 3: //match
						case 4: //friend
							LikeButton.SetImageResource(icChatOne);
							LikeButton.Enabled = true;
							TooltipCompat.SetTooltipText(LikeButton, res.GetString(Resource.String.Match));
							//LikeButton.TooltipText = res.GetString(Resource.String.Match);
							LikeButton.Visibility = ViewStates.Visible;

							HideButton.SetImageResource(icHide);
							TooltipCompat.SetTooltipText(HideButton, res.GetString(Resource.String.Hide));
							//HideButton.TooltipText = res.GetString(Resource.String.Hide);
							HideButton.Visibility = ViewStates.Gone;
							break;
						default:
							break;
					}
				}

				ProfileImageScroll.RemoveAllViews();
				if (!(counterCircles is null))
				{
					for (int i = 0; i < counterCircles.Count; i++)
					{
						CircleContainer.RemoveView(counterCircles[i]);
					}
				}

				counterCircles = new List<View>();

				Username.Text = displayUser.Username;
				Name.Text = displayUser.Name;
				Description.Text = displayUser.Description;
				SetPercentProgress(displayUser.ResponseRate);
				ResponseRate.Text = Math.Round(displayUser.ResponseRate * 100).ToString() + "%";
				LastActiveDate.Text = c.GetTimeDiffStr(displayUser.LastActiveDate, true);
				DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(displayUser.RegisterDate).ToLocalTime();
				if (dt.Date == DateTime.Today)
				{
					RegisterDate.Text = dt.ToString("HH:mm");
				}
				else
				{
					RegisterDate.Text = dt.ToString("d MMMM yyyy");
				}

				LoadEmptyPictures(displayUser.Pictures.Length);

				if (mapLoaded)
				{
					SetMap();
				}

				Profile profile = (Profile)CommonMethods.Clone(displayUser); //displayUser changes when pressing Next or Previous button
				currentID = profile.ID;

				AddCircles(displayUser.Pictures.Length);

				//c.CW("Starting task at userID " + currentID.ToString() + " cancelImageLoading " + cancelImageLoading);
				
				Task.Run(async () =>
				{
					for (int i = 0; i < profile.Pictures.Length; i++)
					{
						if (cancelImageLoading || profile.ID != currentID)
						{
							c.CW("Cancelling task at userID " + profile.ID.ToString() + " currentID " + currentID + " cancelImageLoading " + cancelImageLoading);
							break;
						}
						await LoadPicture(profile.ID.ToString(), profile.Pictures[i], i, false);
					}
				});
			}
			catch (Exception ex)
			{
				c.ReportErrorSilent(ex.Message + System.Environment.NewLine + ex.StackTrace);
			}
		}

		private void SetMap()
		{
			try
			{
				if (mapSet)
				{
					return;
				}
				mapSet = true;

				if (pageType == Constants.ProfileViewType_Self)
				{
					if (Session.Latitude != null && Session.Longitude != null && Session.LocationTime != null) //location available
					{
						LatLng location = new LatLng((double)Session.Latitude, (double)Session.Longitude);
						CameraPosition.Builder builder = CameraPosition.InvokeBuilder();
						builder.Target(location);
						builder.Zoom(16);

						CameraPosition cameraPosition = builder.Build();
						CameraUpdate cameraUpdate = CameraUpdateFactory.NewCameraPosition(cameraPosition);
						thisMap.MoveCamera(cameraUpdate);

						if (!(thisMarker is null))
						{
							thisMarker.Remove();
						}

						MarkerOptions markerOptions = new MarkerOptions();
						markerOptions.SetPosition(new LatLng((double)Session.Latitude, (double)Session.Longitude));
						markerOptions.SetTitle(Session.Name);

						thisMarker = thisMap.AddMarker(markerOptions);

						LocationTime.Text = res.GetString(Resource.String.ProfileViewLocation) + " " + c.GetTimeDiffStr(Session.LocationTime, false);
						LocationTime.Visibility = ViewStates.Visible;
						ShowMap();
					}
					else
					{
						LocationTime.Visibility = ViewStates.Gone; //distance is not shown on self page		
						HideMap();
					}
					DistanceText.Visibility = ViewStates.Gone;
					HideNavigationSpacer();
				}
				else //list or standalone
				{
					if (displayUser.Latitude != null && displayUser.Longitude != null && displayUser.LocationTime != null) //location available
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

						LatLng location = new LatLng((double)displayUser.Latitude, (double)displayUser.Longitude);
						CameraPosition.Builder builder = CameraPosition.InvokeBuilder();
						builder.Target(location);
						builder.Zoom(16);

						CameraPosition cameraPosition = builder.Build();
						CameraUpdate cameraUpdate = CameraUpdateFactory.NewCameraPosition(cameraPosition);
						thisMap.MoveCamera(cameraUpdate);

						if (!(thisMarker is null))
						{
							thisMarker.Remove();
						}

						MarkerOptions markerOptions = new MarkerOptions();
						markerOptions.SetPosition(new LatLng((double)displayUser.Latitude, (double)displayUser.Longitude));

						thisMarker = thisMap.AddMarker(markerOptions);

						LocationTime.Text = res.GetString(Resource.String.ProfileViewLocation) + " " + c.GetTimeDiffStr(displayUser.LocationTime, false);
						LocationTime.Visibility = ViewStates.Visible;

						if (!(displayUser.Distance is null))
						{
							DistanceText.Visibility = ViewStates.Visible;
							DistanceText.Text = displayUser.Distance + " km " + res.GetString(Resource.String.ProfileViewAway);
						}
						else
						{
							DistanceText.Visibility = ViewStates.Gone;
						}
						ShowMap();
					}
					else
					{
						if (!(displayUser.Distance is null))
						{
							LocationTime.Visibility = ViewStates.Gone;
							DistanceText.Visibility = ViewStates.Visible;
							DistanceText.Text = res.GetString(Resource.String.ProfileViewDistance) + " " + c.GetTimeDiffStr(displayUser.LocationTime, false) + ": " + displayUser.Distance + " km";
						}
						else
						{
							LocationTime.Visibility = ViewStates.Gone;
							DistanceText.Visibility = ViewStates.Gone;
						}
						HideMap();
					}
					ShowNavigationSpacer();
				}
			}
			catch (Exception ex)
			{
				c.ReportErrorSilent(ex.Message + System.Environment.NewLine + ex.StackTrace);
			}
		}

		private void RefreshTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			this.RunOnUiThread(() => { 
				switch (pageType)
				{
					case Constants.ProfileViewType_Self:
						LastActiveDate.Text = c.GetTimeDiffStr(Session.LastActiveDate, true);
						if (Session.Latitude != null && Session.Longitude != null && Session.LocationTime != null)
						{
							if (MapContainer.Visibility == ViewStates.Gone)
							{
								mapSet = false;
								SetMap();
								SetHeight();
							}
							else
							{
								LocationTime.Text = res.GetString(Resource.String.ProfileViewLocation) + " " + c.GetTimeDiffStr(Session.LocationTime, false);
							}							
						}
						break;
					case Constants.ProfileViewType_List:
					case Constants.ProfileViewType_Standalone:
						if (!(displayUser is null)) //in case user is passive
						{
							LastActiveDate.Text = c.GetTimeDiffStr(displayUser.LastActiveDate, true);
							if (displayUser.Latitude != null && displayUser.Longitude != null && displayUser.LocationTime != null)
							{
								LocationTime.Text = res.GetString(Resource.String.ProfileViewLocation) + " " + c.GetTimeDiffStr(displayUser.LocationTime, false);
							}
						}
						break;
				}
			});
		}

		private void SetPercentProgress(float responseRate)
		{
			PercentProgress.LayoutParameters.Width = (int)Math.Round(percentProgressWidth * pixelDensity * responseRate);
			byte red = (byte)(192 - (byte)Math.Round(192 * responseRate));
			byte green = (byte)Math.Round(192 * responseRate);
			byte blue = 0;
			Color color = Color.ParseColor("#" + red.ToString("X2") + green.ToString("X2") + blue.ToString("X2"));
			PercentProgress.SetBackgroundColor(color);
		}

		private void AddCircles(int count)
		{
			for(int i=0; i < count; i++)
			{
				View v = new View(this);

				/*
				Originally circles were added directly to ProfileImageContainer, but when pressing Next / Previous, the circles would align to the left, except the last one which is in the middle between the second to last and the parent.
				Layout would only change when a picture is loaded.
				
				v.Id = 2000 + i;
				ConstraintLayout.LayoutParams p = new ConstraintLayout.LayoutParams((int)(counterCircleSize * pixelDensity), (int)(counterCircleSize * pixelDensity));
				if (i == 0)
				{
					p.LeftToLeft = ConstraintLayout.LayoutParams.ParentId;
					p.HorizontalChainStyle = ConstraintLayout.LayoutParams.ChainPacked;
					v.SetBackgroundResource(counterCircleSelected);
				}
				else
				{
					((ConstraintLayout.LayoutParams)counterCircles[i - 1].LayoutParameters).RightToLeft = 2000 + i;
					p.LeftToRight = 2000 + i - 1;
					v.SetBackgroundResource(counterCircle);
				}
				if (i == count - 1)
				{
					p.RightToRight = ConstraintLayout.LayoutParams.ParentId;
				}
				p.BottomToBottom = Resource.Id.ProfileImageScroll;
				p.BottomMargin = (int)(2 * pixelDensity);
				p.LeftMargin = (int)(1.5 * pixelDensity);
				p.RightMargin = (int)(1.5 * pixelDensity);
				v.LayoutParameters = p;
				*/

				LinearLayout.LayoutParams p = new LinearLayout.LayoutParams((int)(counterCircleSize * pixelDensity), (int)(counterCircleSize * pixelDensity));
				p.BottomMargin = (int)(2 * pixelDensity);
				p.LeftMargin = (int)(1.5 * pixelDensity);
				p.RightMargin = (int)(1.5 * pixelDensity);
				v.LayoutParameters = p;

				if (i == 0)
				{
					v.SetBackgroundResource(counterCircleSelected);
				}
				else
				{
					v.SetBackgroundResource(counterCircle);
				}

				v.Alpha = 0.8f;
				counterCircles.Add(v);
				CircleContainer.AddView(v);
			}
		}

		private void LoadEmptyPictures(int count)
		{
			for (int index = 0; index < count; index++)
			{
				ImageView ProfileImage = new ImageView(this);
				ProfileImage.Id = 1000 + index;
				ConstraintLayout.LayoutParams p = new ConstraintLayout.LayoutParams(0, ViewGroup.LayoutParams.MatchParent);
				p.DimensionRatio = "1:1";
				if (index == 0)
				{
					p.LeftToLeft = Resource.Id.ProfileImageScroll;
				}
				else
				{
					p.LeftToRight = 1000 + index - 1;
				}
				ProfileImage.LayoutParameters = p;
				ProfileImage.SetImageResource(Resource.Drawable.loadingimage);
				ProfileImageScroll.AddView(ProfileImage);
			}
		}

		private async Task LoadPicture(string folder, string picture, int index, bool usecache)
		{
			ImageView ProfileImage = (ImageView)ProfileImageScroll.GetChildAt(index);
			if (usecache)
			{
				ImageCache im = new ImageCache(this);
				await im.LoadImage(ProfileImage, folder, picture, true);
			}
			else
			{
				string url;
				if (Constants.isTestDB)
				{
					url = Constants.HostName + Constants.UploadFolderTest + "/" + folder + "/" + Constants.LargeImageSize + "/" + picture;
				}
				else
				{
					url = Constants.HostName + Constants.UploadFolder + "/" + folder + "/" + Constants.LargeImageSize + "/" + picture;
				}

				//c.LogActivity("LoadPicture start ID " + folder + " index " + index );
				//c.CW("LoadPicture start index " + index + " ID " + folder);

				Bitmap im = null;

				var task = CommonMethods.GetImageBitmapFromUrlAsync(url);
				System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource();

				if (await Task.WhenAny(task, Task.Delay(Constants.RequestTimeout, cts.Token)) == task)
				{
					cts.Cancel();
					im = await task;
				}

				//c.LogActivity("LoadPicture end ID " + folder + " index "+ index + " im null " + (im is null) + " currentID " + currentID + " cancelImageLoading " + cancelImageLoading);
				//c.CW("LoadPicture end index " + index + " ID " + folder + " im null " + (im is null) + " currentID " + currentID + " cancelImageLoading " + cancelImageLoading);
				
				if (im is null)
				{
					if (cancelImageLoading || folder != currentID.ToString())
					{
						return;
					}
					RunOnUiThread(() => {
						ProfileImage.SetImageResource(Resource.Drawable.noimage_hd);
					});
				}
				else
				{
					if (cancelImageLoading || folder != currentID.ToString())
					{
						return;
					}
					RunOnUiThread(() => {
						ProfileImage.SetImageBitmap(im);
					});
				}
			}
		}

		private void ScrollLayout_Touch(object sender, View.TouchEventArgs e)
		{
			switch (e.Event.Action)
			{
				case MotionEventActions.Down:
					if (!(animator is null) && animator.IsRunning || !(scrollTimer is null) && scrollTimer.Enabled)
					{
						return;
					}
					verticalEnabled = false;
					if (totalScrollHeight > 0)
					{
						stw.Restart();
						prevPos = touchStartY = e.Event.GetY();
						startScrollY = ScrollLayout.ScrollY;
						prevTime = 0;
						prevSpeedY = 0;
						speedY = 0;
						verticalEnabled = true;
					}
					break;

				case MotionEventActions.Move:
					if (verticalEnabled)
					{
						touchCurrentY = e.Event.GetY();
						VerticalMove(false);
					}
					break;

				case MotionEventActions.Up:
					if (verticalEnabled)
					{
						VerticalUp();
					}
					break;
			}
		}

		public bool ScrollDown(MotionEvent e)
		{
			if (!(animator is null) && animator.IsRunning || !(scrollTimer is null) && scrollTimer.Enabled)
			{
				return false;
			}
			//if animator is running, ScrollMove will still run. Timer OK.

			ProfileImageScroll.RequestDisallowInterceptTouchEvent(true);
			touchStartX = e.GetX();
			touchStartY = e.GetY();
			touchCurrentX = touchStartX;
			touchCurrentY = touchStartY;
			currentOffsetX = touchCurrentX - touchStartX; //when clicking, move event may not get triggered.
			currentOffsetY = touchCurrentY - touchStartY;

			startScrollX = ProfileImageScroll.ScrollX;
			totalScroll = ProfileImageScroll.Height * (counterCircles.Count - 1);
			startPic = PosToPic(ProfileImageScroll.ScrollX);

			isTouchDown = true;
			horizontalCancelled = false;
			verticalEnabled = false;
			stw.Restart();
			prevDiffY = 0;
			prevPos = touchStartX;
			prevTime = 0;
			prevSpeedX = 0;
			speedX = 0;
			prevSpeedY = 0;
			speedY = 0;

			return true;
		}

		public bool ScrollMove(MotionEvent e)
		{
			if (!isTouchDown)
			{
				return false;
			}

			touchCurrentX = e.GetX();
			touchCurrentY = e.GetY();
			
			if (horizontalCancelled && verticalEnabled)
			{
				VerticalMove(true);
			}
			else if (!horizontalCancelled)
			{
				if (touchCurrentX != prevPos) //Move event gets triggered when we programatically scroll the view, even though the finger didn't move.
				{
					currentOffsetX = touchCurrentX - touchStartX;
					currentOffsetY = touchCurrentY - touchStartY;

					float ratio = currentOffsetX / currentOffsetY;
					if (Math.Abs(ratio) > 1) //horizontal move
					{
						int value = (int)(startScrollX - currentOffsetX);
						if (value >= 0 && value <= totalScroll)
						{
							ProfileImageScroll.ScrollX = value;
						}
						else if (value > totalScroll)
						{
							ProfileImageScroll.ScrollX = totalScroll;
						}
						else
						{
							ProfileImageScroll.ScrollX = 0;
						}

						long currentTime = stw.ElapsedMilliseconds;
						long intervalTime = currentTime - prevTime;

						prevSpeedX = speedX; //the last speed value is often much less than it should be, we use the one before the last.
						speedX = (touchCurrentX - prevPos) / intervalTime;
						prevPos = touchCurrentX;
						prevTime = currentTime;
					}
					else if (Math.Abs(currentOffsetY) > swipeMinDistance * pixelDensity)  //starting vertical scroll after a threshold
					{
						horizontalCancelled = true;
						ScrollRestore();
						prevPos = touchStartY = touchCurrentY;
						startScrollY = ScrollLayout.ScrollY;
						if (totalScrollHeight > 0)
						{
							verticalEnabled = true;
						}
					}
				}
			}
			return false;
		}

		public void VerticalMove(bool fromHorizontal)
		{
			long currentTime = stw.ElapsedMilliseconds;

			if  (fromHorizontal)
			{
				touchCurrentY = touchCurrentY + prevDiffY;
			}

			currentOffsetY = touchCurrentY - touchStartY; //moving HorizontalScrollView affects the touch point.

			int scrollValue = (int)(startScrollY - currentOffsetY);

			if (scrollValue <= 0)
			{
				touchStartY -= scrollValue; //move origin point, so when move direction changes, scroll will start immediately
				scrollValue = 0;
			}
			else if (scrollValue >= totalScrollHeight)
			{
				touchStartY -= scrollValue - totalScrollHeight;
				scrollValue = totalScrollHeight;
			}
			CastShadows(scrollValue);

			ScrollLayout.ScrollY = scrollValue;
			if (fromHorizontal)
			{
				prevDiffY = startScrollY - scrollValue;
			}
			long intervalTime = currentTime - prevTime;

			prevSpeedY = speedY; //the last speed value is often much less than it should be, we use the one before the last.
			speedY = (touchCurrentY - prevPos) / intervalTime;
			
			prevPos = touchCurrentY;
			prevTime = currentTime;
		}

		public void CastShadows(int scrollValue)
		{
			if (totalScrollHeight == 0 || scrollValue <= 0)
			{
				BackButton.Elevation = 0;
			}
			else
			{
				BackButton.Elevation = buttonElevation * pixelDensity;
			}
		}

		public void VerticalUp()
		{
			verticalEnabled = false;

			speedY = (Math.Abs(speedY) > Math.Abs(prevSpeedY)) ? speedY : prevSpeedY;

			scrollTimer = new Timer();
			scrollTimer.Interval = 1;
			startValue = ScrollLayout.ScrollY;

			float maxDistance = speedY / (decelerationRate * pixelDensity) * speedY / 2;
			if (speedY > 0)
			{
				if (maxDistance > startValue) //scroll to top fast
				{
					endValue = 0;
					timeValue = startValue / (speedY / 2);
					scrollTimer.Elapsed += ScrollTimer2_Elapsed; //decelerate faster
				}
				else
				{
					scrollTimer.Elapsed += ScrollTimer_Elapsed; //decelarate using constant rate
				}
			}
			else
			{
				if (maxDistance > totalScrollHeight - startValue)
				{
					endValue = totalScrollHeight;
					timeValue = (totalScrollHeight - startValue) / (-speedY / 2);
					scrollTimer.Elapsed += ScrollTimer2_Elapsed;
				}
				else
				{
					scrollTimer.Elapsed += ScrollTimer_Elapsed;
				}
			}
			stw.Restart();
			scrollTimer.Start();
		}

		public void ScrollRestore()
		{
			animator = ObjectAnimator.OfInt(ProfileImageScroll, "ScrollX", startScrollX);
			animator.SetDuration(tweenTime);
			animator.Start();
		}

		public bool ScrollUp()
		{
			ProfileImageScroll.RequestDisallowInterceptTouchEvent(false);
			if (!isTouchDown)
			{
				return false;
			}
			if (horizontalCancelled && verticalEnabled)
			{
				isTouchDown = false;
				VerticalUp();
				return false;
			}

			stw.Stop();

			int currentPic = PosToPic(ProfileImageScroll.ScrollX);
			View circle;

			//click, move to next or previous image
			if (stw.ElapsedMilliseconds < clickTime && Math.Abs(currentOffsetX) < swipeMinDistance * pixelDensity && Math.Abs(currentOffsetY) < swipeMinDistance * pixelDensity)
			{
				currentPic = PosToPic(startScrollX);
				int newPos;
				int newIndex;

				if (touchStartX + currentOffsetX >= screenWidth / 2) //next
				{
					if (currentPic == counterCircles.Count - 1)
					{
						newIndex = 0;
					}
					else
					{
						newIndex = currentPic + 1;
					}
				}
				else //previous
				{
					if (currentPic == 0)
					{
						newIndex = counterCircles.Count - 1;
					}
					else
					{
						newIndex = currentPic - 1;
					}
				}

				newPos = PicToPos(newIndex);
				ProfileImageScroll.ScrollX = newPos;

				circle = counterCircles[startPic];
				circle.SetBackgroundResource(counterCircle);
				circle = counterCircles[newIndex];
				circle.SetBackgroundResource(counterCircleSelected);

				isTouchDown = false;
				return false;
			}

			isTouchDown = false;
			stw.Stop();
			
			speedX = (Math.Abs(speedX) > Math.Abs(prevSpeedX)) ? speedX : prevSpeedX;

			if (Math.Abs(speedX) > swipeMinSpeed * pixelDensity)
			{
				if (currentOffsetX < -swipeMinDistance * pixelDensity && speedX < 0)
				{
					int newPos = PicToPos(currentPic + 1);					
					if (newPos <= totalScroll)
					{
						float remainingDistance = (ProfileImageScroll.Height - ProfileImageScroll.ScrollX % ProfileImageScroll.Height);
						long estimatedTime = -(long)(remainingDistance /speedX * 2);

						var v = -speedX;
						var s = remainingDistance;
						var t = tweenTime;

						/*

						Calculating acceleration and maximum speed given the start speed, the distance, and the time required to complete it.

						speed
						|            2x  
						| 
						|
						|
						|
						|
						|
						|
						|
						|   x (v max)
						|   /\
						|  /  \
						| /    \
						|/      \
						| v      \
						|         \
						|          \
						|     s     \
						|            \
						|             \
						|              \
						|______________________time
							 t1       t

						t1 = (x-v)/(2*x-v)*t
						s = (v+x)/2*t1+x/2*(t-t1) 
						s = (v+x)/2 * (x-v)/(2*x-v)*t + x/2*t - x/2*(x-v)/(2*x-v)*t
						2*s/t = (v+x) * (x-v)/(2*x-v) + x - x*(x-v)/(2*x-v)
						2*s/t * (2*x-v) = (v+x) * (x-v) + x*x
						4*s*x - 2*s*v = 2*t*x^2 - t*v^2
						0 = 2*t*x^2 - 4*s*x + 2*s*v - t*v^2   
						x = (4*s +- sqrt(16*s*s - 8*t*v*(2*s - t*v))) / (4*t)
						*/

						//quadratic formula

						double x1 = (4 * s + Math.Sqrt(16 * s * s - 8 * t * v * (2 * s - t * v))) / (4 * t);
						double t1 = (x1 - v) / (2 * x1 - v) * t;

						stw.Restart();
						scrollTimer = new Timer();
						scrollTimer.Interval = 1; // 1000 per framerate would work too, but it is 16.666. Settings 17, motion is ok, but sometimes jumps.

						if (estimatedTime <= tweenTime)
						{
							scrollTimer.Elapsed += T_Elapsed;
							startValue = ProfileImageScroll.ScrollX;
							endValue = newPos;
							timeValue = estimatedTime;							
						}
						else
						{
							scrollTimer.Elapsed += T2_Elapsed;
							startValue = ProfileImageScroll.ScrollX;
							endValue = newPos;
							timeValue = tweenTime;
							middleTime = (float)t1;
							//speeds are positive
							speedX = -speedX;
							topSpeed = (float)x1;
							acceleration = (float)((x1 - v) / t1);
						}
						scrollTimer.Start();

						circle = counterCircles[startPic];
						circle.SetBackgroundResource(counterCircle);
						circle = counterCircles[currentPic + 1];
						circle.SetBackgroundResource(counterCircleSelected);
					}

				}
				else if (currentOffsetX > swipeMinDistance * pixelDensity && speedX > 0)
				{
					int newPos = PicToPos(currentPic);
					if (newPos >= 0)
					{
						float remainingDistance = ProfileImageScroll.ScrollX % ProfileImageScroll.Height;
						long estimatedTime = (long)(remainingDistance /speedX * 2);

						var v = speedX;
						var s = remainingDistance;
						var t = tweenTime;
						double x1 = (4 * s + Math.Sqrt(16 * s * s - 8 * t * v * (2 * s - t * v))) / (4 * t);
						double t1 = (x1 - v) / (2 * x1 - v) * t;

						stw.Restart();
						scrollTimer = new Timer();
						scrollTimer.Interval = 1;

						if (estimatedTime <= tweenTime)
						{
							scrollTimer.Elapsed += T_Elapsed;
							startValue = ProfileImageScroll.ScrollX;
							endValue = newPos;
							timeValue = estimatedTime;
						}
						else
						{
							scrollTimer.Elapsed += T2_Elapsed;
							startValue = ProfileImageScroll.ScrollX;
							endValue = newPos;
							timeValue = tweenTime;
							middleTime = (float)t1;
							//speeds are negative
							speedX = -speedX;
							topSpeed = -(float)x1;
							acceleration = -(float)((x1 - v) / t1);
						}
						scrollTimer.Start();

						circle = counterCircles[startPic];
						circle.SetBackgroundResource(counterCircle);
						circle = counterCircles[currentPic];
						circle.SetBackgroundResource(counterCircleSelected);
					}
				}
				else
				{
					//pull image to closest border
					double remainder = ProfileImageScroll.ScrollX % ProfileImageScroll.Height;
					int newPos;
					int newIndex;
					if (remainder < ProfileImageScroll.Height / 2)
					{
						newIndex = currentPic;
					}
					else
					{
						newIndex = currentPic + 1;
					}
					newPos = PicToPos(newIndex);

					animator = ObjectAnimator.OfInt(ProfileImageScroll, "ScrollX", newPos);
					animator.SetDuration(tweenTime);
					animator.Start();

					if (startPic != newIndex)
					{
						circle = counterCircles[startPic];
						circle.SetBackgroundResource(counterCircle);
						circle = counterCircles[newIndex];
						circle.SetBackgroundResource(counterCircleSelected);
					}
				}
			}
			else
			{
				double remainder = ProfileImageScroll.ScrollX % ProfileImageScroll.Height;
				int newPos;
				int newIndex;
				if (remainder < ProfileImageScroll.Height / 2)
				{
					newIndex = currentPic;
				}
				else
				{
					newIndex = currentPic + 1;
				}
				newPos = PicToPos(newIndex);

				animator = ObjectAnimator.OfInt(ProfileImageScroll, "ScrollX", newPos);
				animator.SetDuration(tweenTime);
				animator.Start();

				if (startPic != newIndex)
				{
					circle = counterCircles[startPic];
					circle.SetBackgroundResource(counterCircle);
					circle = counterCircles[newIndex];
					circle.SetBackgroundResource(counterCircleSelected);
				}
			}
			return false;
		}

		private void ScrollTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			int scrollValue;
			float currentSpeed;
			long millis = stw.ElapsedMilliseconds;
			if (speedY > 0)
			{
				currentSpeed = speedY - millis * decelerationRate * pixelDensity;
				if (currentSpeed < 0)
				{
					currentSpeed = 0;
					stw.Stop();
					scrollTimer.Stop();
				}
				float distance = (speedY + currentSpeed) / 2 * millis;
				scrollValue = (int)(startValue - distance);
			}
			else
			{
				currentSpeed = speedY + millis * decelerationRate * pixelDensity;
				if (currentSpeed > 0)
				{
					currentSpeed = 0;
					stw.Stop();
					scrollTimer.Stop();
				}
				float distance = (speedY + currentSpeed) / 2 * millis;
				scrollValue = (int)(startValue - distance);
			}

			if (scrollValue <= 0)
			{
				scrollValue = 0;
				stw.Stop();
				scrollTimer.Stop();
				CastShadows(scrollValue);
			}
			else if (scrollValue >= totalScrollHeight)
			{
				scrollValue = totalScrollHeight;
				stw.Stop();
				scrollTimer.Stop();
			}
			ScrollLayout.ScrollY = scrollValue;
;		}

		private void ScrollTimer2_Elapsed(object sender, ElapsedEventArgs e)
		{
			long millis = stw.ElapsedMilliseconds;
			float currentSpeed = speedY * (1 - millis / timeValue);
			ScrollLayout.ScrollY = (int)(startValue - (speedY + currentSpeed) / 2 * millis);
			if (millis >= timeValue)
			{
				scrollTimer.Stop();
				ScrollLayout.ScrollY = (int)endValue;
				CastShadows((int)endValue);
			}
		}


		private void T_Elapsed(object sender, ElapsedEventArgs e) //decelerate
		{
			long millis = stw.ElapsedMilliseconds;
			if (millis < timeValue)
			{
				//decelerate
				float currentSpeed = (1 - millis / timeValue) * speedX;
				float avgSpeed = (speedX + currentSpeed) / 2;
				ProfileImageScroll.ScrollX = (int)(startValue - avgSpeed * millis);
			}
			else
			{
				stw.Stop();
				scrollTimer.Stop();
				ProfileImageScroll.ScrollX = (int)endValue;
			}
		}

		private void T2_Elapsed(object sender, ElapsedEventArgs e) //accelerate - decelerate
		{
			long millis = stw.ElapsedMilliseconds;
			if (millis < middleTime)
			{
				float currentSpeed = speedX + acceleration * millis;
				float elapsedDistance = (speedX + currentSpeed) / 2 * millis;
				ProfileImageScroll.ScrollX = (int)(startValue + elapsedDistance);
				//ProfileImageScroll.Invalidate();
			}
			else if (millis < timeValue)
			{
				float currentSpeed = topSpeed - acceleration * (millis - middleTime);
				float elapsedDistance = (speedX + topSpeed) / 2 * middleTime + (topSpeed + currentSpeed) / 2 * (millis - middleTime);
				ProfileImageScroll.ScrollX = (int)(startValue + elapsedDistance);
				//ProfileImageScroll.Invalidate();
			}
			else
			{
				stw.Stop();
				scrollTimer.Stop();
				ProfileImageScroll.ScrollX = (int)endValue;				
			}
		}

		public int PosToPic(double pos)
		{
			double remainder = pos % ProfileImageScroll.Height;
			return (int)((pos - remainder) / ProfileImageScroll.Height);
		}

		private int PicToPos(int pic)
		{
			return ProfileImageScroll.Height * pic;
		}

		private void PreviousButton_Click(object sender, EventArgs e)
		{
			ListActivity.viewIndex--;
			ListActivity.absoluteIndex--;

			c.CW("PreviousButton_Click viewIndex " + ListActivity.viewIndex + " absoluteIndex " + ListActivity.absoluteIndex + " viewProfiles.Count " + ListActivity.viewProfiles.Count + " listProfiles.Count " + ListActivity.listProfiles.Count);

			if (ListActivity.viewIndex >= 0)
			{
				PrevLoadAction();
				displayUser = ListActivity.viewProfiles[ListActivity.viewIndex];
				mapSet = false;
				ProfileImageScroll.ScrollX = 0;
				//c.CW("Loading previous user");
				LoadUser();
			}
			else
			{
				ListActivity.absoluteIndex++;
				//loading may be in progress, new list will be shown.
				OnBackPressed();
			}
		}

		private void NextButton_Click(object sender, EventArgs e)
		{
			ListActivity.viewIndex++;
			ListActivity.absoluteIndex++;

			c.CW("NextButton_Click viewIndex " + ListActivity.viewIndex + " absoluteIndex " + ListActivity.absoluteIndex + " viewProfiles.Count " + ListActivity.viewProfiles.Count + " listProfiles.Count " + ListActivity.listProfiles.Count);

			if (ListActivity.viewIndex < ListActivity.viewProfiles.Count)
			{
				NextLoadAction();
				displayUser = ListActivity.viewProfiles[ListActivity.viewIndex];
				mapSet = false;
				ProfileImageScroll.ScrollX = 0;
				//c.CW("Loading next user");
				LoadUser();
			}
			else
			{
				ListActivity.absoluteIndex--;
				//loading may be in progress, new list will be shown.
				OnBackPressed();
			}
		}

		private void PrevLoadAction()
		{
			//c.LogActivity("PrevLoadAction viewIndex " + ListActivity.viewIndex + " absoluteIndex " + ListActivity.absoluteIndex + " absoluteStartIndex " + ListActivity.absoluteStartIndex + " ResultsFrom " + Session.ResultsFrom + " view count " + ListActivity.viewProfiles.Count);
			//c.CW("PrevLoadAction viewIndex " + ListActivity.viewIndex + " absoluteIndex " + ListActivity.absoluteIndex + " absoluteStartIndex " + ListActivity.absoluteStartIndex + " ResultsFrom " + Session.ResultsFrom + " viewProfiles.Count " + ListActivity.viewProfiles.Count + " totalResultCount " + ListActivity.totalResultCount);
			if (ListActivity.viewIndex == 0 && ListActivity.absoluteFirstIndex > 0)
			{
				Session.ResultsFrom = ListActivity.absoluteIndex - Constants.MaxResultCount + 1;
				//c.LogActivity("Prev2 ResultsFrom " + Session.ResultsFrom);
				//c.CW("Prev2 ResultsFrom " + Session.ResultsFrom);
				ListActivity.addResultsBefore = true;
				if (Session.LastSearchType == Constants.SearchType_Filter)
				{
					Task.Run(() => ListActivity.thisInstance.LoadList());
				}
				else
				{
					Task.Run(() => ListActivity.thisInstance.LoadListSearch());
				}
			}
		}

		private void NextLoadAction()
		{
			//c.LogActivity("NextLoadAction viewIndex " + ListActivity.viewIndex + " absoluteIndex " + ListActivity.absoluteIndex + " absoluteStartIndex " + ListActivity.absoluteStartIndex + " ResultsFrom " + Session.ResultsFrom + " view count " + ListActivity.viewProfiles.Count);
			//c.CW("NextLoadAction viewIndex " + ListActivity.viewIndex + " absoluteIndex " + ListActivity.absoluteIndex + " absoluteStartIndex " + ListActivity.absoluteStartIndex + " ResultsFrom " + Session.ResultsFrom + " viewProfiles.Count " + ListActivity.viewProfiles.Count + " totalResultCount " + ListActivity.totalResultCount);
			if (ListActivity.viewIndex == ListActivity.viewProfiles.Count - 1 && ListActivity.totalResultCount > ListActivity.absoluteIndex + 1) //list will be loaded
			{
				Session.ResultsFrom = ListActivity.absoluteIndex + 2;
				//c.LogActivity("Next2 ResultsFrom " + Session.ResultsFrom);
				//c.CW("Next2 ResultsFrom " + Session.ResultsFrom);
				ListActivity.addResultsAfter = true;
				if (Session.LastSearchType == Constants.SearchType_Filter)
				{
					Task.Run(() => ListActivity.thisInstance.LoadList());
				}
				else
				{
					Task.Run(() => ListActivity.thisInstance.LoadListSearch());
				}
			}
		}

		public override void OnBackPressed()
		{
			if (Session.ListType == "hid")
			{
				Session.ResultsFrom = 1;
				Session.LastDataRefresh = null;
				ListActivity.listProfiles = null;
			}
			base.OnBackPressed();
		}

		private async void LikeButton_Click(object sender, EventArgs e)
		{
			if (pageType == Constants.ProfileViewType_Standalone)
			{
				IntentData.senderID = displayUser.ID; //we could have gotten on this profile page from another chat by clicking on a notification.
				OnBackPressed();
				return;
			}

			if (displayUser.UserRelation == 2)
			{
				return;
			}
			else if (displayUser.UserRelation != 3 && displayUser.UserRelation != 4) //not a match yet
			{
				long unixTimestamp = c.Now();
				string responseString = await c.MakeRequest("action=like&ID=" + Session.ID + "&target=" + displayUser.ID
				+ "&time=" + unixTimestamp + "&SessionID=" + Session.SessionID);
				if (responseString.Substring(0, 2) == "OK")
				{
					string matchIDStr = responseString.Substring(3);
					if (matchIDStr != "")
					{
						Session.CurrentMatch = new MatchItem();
						Session.CurrentMatch.MatchID = int.Parse(matchIDStr);
						Session.CurrentMatch.TargetID = displayUser.ID;
						Session.CurrentMatch.TargetUsername = displayUser.Username;
						Session.CurrentMatch.TargetName = displayUser.Name;
						Session.CurrentMatch.TargetPicture = displayUser.Pictures[0];

						displayUser.UserRelation = 3;
						TooltipCompat.SetTooltipText(LikeButton, res.GetString(Resource.String.Match));
						//LikeButton.TooltipText = res.GetString(Resource.String.Match);
						LikeButton.SetImageResource(icChatOne);
						LikeButton.Enabled = true;

						HideButton.Visibility = ViewStates.Gone;

						string dialogResponse = await c.DisplayCustomDialog("", res.GetString(Resource.String.DialogMatch),
							res.GetString(Resource.String.DialogYes), res.GetString(Resource.String.DialogNo));
						if (dialogResponse == res.GetString(Resource.String.DialogYes))
						{
							Intent i = new Intent(this, typeof(ChatOneActivity));
							i.SetFlags(ActivityFlags.ReorderToFront);
							StartActivity(i);
						}
					}
					else
					{
						displayUser.UserRelation = 2;
						TooltipCompat.SetTooltipText(LikeButton, res.GetString(Resource.String.Liked));
						//LikeButton.TooltipText = res.GetString(Resource.String.Liked);
						LikeButton.SetImageResource(icLiked);
						LikeButton.Enabled = false;

						if (pageType == Constants.ProfileViewType_List)
						{
							NextButton_Click(null, null);
						}
					}
				}
				else
				{
					c.ReportError(responseString);
				}
			}
			else // already a match, opening chat window
			{
				if (pageType == Constants.ProfileViewType_List) //a previously gotten match, we are coming from list, not chat
				{
					string responseString = await c.MakeRequest("action=requestmatchid&ID=" + Session.ID + "&SessionID=" + Session.SessionID + "&target=" + displayUser.ID);
					if (responseString.Substring(0, 2) == "OK")
					{
						Session.CurrentMatch = new MatchItem();
						Session.CurrentMatch.MatchID = int.Parse(responseString.Substring(3));
						Session.CurrentMatch.TargetID = displayUser.ID;
						Session.CurrentMatch.TargetUsername = displayUser.Username;
						Session.CurrentMatch.TargetName = displayUser.Name;
						Session.CurrentMatch.TargetPicture = displayUser.Pictures[0];

						Intent i = new Intent(this, typeof(ChatOneActivity));
						i.SetFlags(ActivityFlags.ReorderToFront);
						StartActivity(i);
					}
					else
					{
						c.ReportError(responseString);
					}
				}
				else
				{
					Intent i = new Intent(this, typeof(ChatOneActivity));
					i.SetFlags(ActivityFlags.ReorderToFront);
					StartActivity(i);
				}
			}
		}

		private async void HideButton_Click(object sender, EventArgs e)
		{
			long unixTimestamp = c.Now();
			if (displayUser.UserRelation == 0 || displayUser.UserRelation == 2)
			{
				string responseString = await c.MakeRequest("action=hide&ID=" + Session.ID + "&target=" + displayUser.ID
				+ "&time=" + unixTimestamp + "&SessionID=" + Session.SessionID);
				if (responseString == "OK")
				{
					displayUser.UserRelation = 1;
					TooltipCompat.SetTooltipText(HideButton, res.GetString(Resource.String.Reinstate));
					//HideButton.TooltipText = res.GetString(Resource.String.Reinstate);
					HideButton.SetImageResource(icRefresh);

					LikeButton.Visibility = ViewStates.Gone;

					if (Session.ListType != "hid")
					{
						ListActivity.viewProfiles.RemoveAt(ListActivity.viewIndex);
						int listIndex = ListActivity.viewIndex - (ListActivity.absoluteStartIndex - ListActivity.absoluteFirstIndex); //we subtract from viewIndex the number of items that were loaded to add before listProfiles
						{
							if (listIndex >= 0 && listIndex < ListActivity.listProfiles.Count) // check the cases where an item was removed from the below or above added list
							{
								ListActivity.listProfiles.RemoveAt(listIndex);
							}
						}
						
						ListActivity.viewIndex--;
						ListActivity.absoluteIndex--;
						ListActivity.totalResultCount--;
						NextButton_Click(null, null);
					}
				}
				else if (responseString.Substring(0, 6) == "ERROR_") //IsAMatch
				{
					string sex = (displayUser.Sex == 0) ? res.GetString(Resource.String.SexHer) : res.GetString(Resource.String.SexHim);
					c.SnackStr(res.GetString(Resources.GetIdentifier(responseString.Substring(6), "string", PackageName)).Replace("[name]", displayUser.Name).Replace("[sex]", sex));
				}
				else
				{
					c.ReportError(responseString);
				}
			}
			else if (displayUser.UserRelation == 1) //we are in Hid list
			{
				string responseString = await c.MakeRequest("action=unhide&ID=" + Session.ID + "&target=" + displayUser.ID
				+ "&time=" + unixTimestamp + "&SessionID=" + Session.SessionID);
				if (responseString == "OK")
				{
					displayUser.UserRelation = 0;
					TooltipCompat.SetTooltipText(LikeButton, res.GetString(Resource.String.Like));
					//LikeButton.TooltipText = res.GetString(Resource.String.Like);
					LikeButton.Visibility = ViewStates.Visible;

					TooltipCompat.SetTooltipText(HideButton, res.GetString(Resource.String.Hide));
					//HideButton.TooltipText = res.GetString(Resource.String.Hide);
					HideButton.SetImageResource(icHide);
				}
				else
				{
					c.ReportError(responseString);
				}
			}
		}

		public void AddNewMatch(int senderID, MatchItem item)
		{
			if (pageType != Constants.ProfileViewType_Self && displayUser.ID == senderID)
			{
				Session.CurrentMatch = item;
				ListActivity.viewProfiles[ListActivity.viewIndex].UserRelation = 3;

				TooltipCompat.SetTooltipText(LikeButton, res.GetString(Resource.String.Match));
				//LikeButton.TooltipText = res.GetString(Resource.String.Match);
				LikeButton.SetImageResource(icChatOne);
				LikeButton.Enabled = true;

				HideButton.Visibility = ViewStates.Gone;
			}
		}

		public void UpdateStatus(int senderID, bool isMatch, int? matchID)
		{
			if (pageType == Constants.ProfileViewType_List && displayUser.ID == senderID)
			{
				if (isMatch) //start userrelation 2
				{
					Session.CurrentMatch = new MatchItem();
					Session.CurrentMatch.MatchID = matchID;
					Session.CurrentMatch.TargetID = displayUser.ID;
					Session.CurrentMatch.TargetUsername = displayUser.Username;
					Session.CurrentMatch.TargetName = displayUser.Name;
					Session.CurrentMatch.TargetPicture = displayUser.Pictures[0];

					ListActivity.viewProfiles[ListActivity.viewIndex].UserRelation = 3;

					TooltipCompat.SetTooltipText(LikeButton, res.GetString(Resource.String.Match));
					//LikeButton.TooltipText = res.GetString(Resource.String.Match);
					LikeButton.SetImageResource(icChatOne);
					LikeButton.Enabled = true;

					HideButton.Visibility = ViewStates.Gone;
				}
				else //start userrelation 3 or 4.
				{
					ListActivity.viewProfiles[ListActivity.viewIndex].UserRelation = 2;

					TooltipCompat.SetTooltipText(LikeButton, res.GetString(Resource.String.Liked));
					//LikeButton.TooltipText = res.GetString(Resource.String.Liked);
					LikeButton.SetImageResource(icLiked);
					LikeButton.Enabled = false;

					HideButton.SetImageResource(icHide);
					TooltipCompat.SetTooltipText(HideButton, res.GetString(Resource.String.Hide));
					//HideButton.TooltipText = res.GetString(Resource.String.Hide);
					HideButton.Visibility = ViewStates.Visible;
				}
			}
			else if (pageType == Constants.ProfileViewType_Standalone)
			{
				ChatReceiver.AddUpdateMatch(senderID, isMatch);
			}

			if (pageType == Constants.ProfileViewType_Standalone && displayUser.ID == senderID)
			{
				if (isMatch)
				{
					displayUser.UserRelation = 3;

					TooltipCompat.SetTooltipText(LikeButton, res.GetString(Resource.String.Match));
					//LikeButton.TooltipText = res.GetString(Resource.String.Match);
					LikeButton.SetImageResource(icChatOne);
					LikeButton.Enabled = true;

					HideButton.Visibility = ViewStates.Gone;
				}
				else
				{
					displayUser.UserRelation = 2;

					TooltipCompat.SetTooltipText(LikeButton, res.GetString(Resource.String.Liked));
					//LikeButton.TooltipText = res.GetString(Resource.String.Liked);
					LikeButton.SetImageResource(icLiked);
					LikeButton.Enabled = false;

					HideButton.SetImageResource(icHide);
					TooltipCompat.SetTooltipText(HideButton, res.GetString(Resource.String.Hide));
					//HideButton.TooltipText = res.GetString(Resource.String.Hide);
					HideButton.Visibility = ViewStates.Visible;
				}
			}
		}

		public void UpdateLocationStart(int senderID, string message)
		{
			if (pageType != Constants.ProfileViewType_Self && displayUser.ID == senderID)
			{
				c.SnackStr(message);
			}
			else {

				c.SnackAction(message, Resource.String.ShowReceived, new Action<View>(delegate (View obj) {
					if (pageType == Constants.ProfileViewType_Self && (bool)Session.UseLocation && c.IsLocationEnabled())
					{
						UnregisterReceiver(locationReceiver);
					}
					mapSet = false;
					ProfileImageScroll.ScrollX = 0;
					pageType = Constants.ProfileViewType_Standalone;
					LoadStandalone(senderID);
					int currentScrollHeight = GetScrollHeight();
					totalScrollHeight = currentScrollHeight - MainLayout.Height;
				}));
			}
		}

		public void UpdateLocation(int senderID, long time, double latitude, double longitude)
		{
			if (pageType != Constants.ProfileViewType_Self && displayUser.ID == senderID)
			{
				displayUser.LastActiveDate = time;
				displayUser.Latitude = latitude;
				displayUser.Longitude = longitude;
				displayUser.LocationTime = time;

				LastActiveDate.Text = c.GetTimeDiffStr(time, true);

				LatLng location = new LatLng(latitude, longitude);
				thisMap.MoveCamera(CameraUpdateFactory.NewLatLng(location));

				if (!(thisMarker is null))
				{
					thisMarker.Remove();
				}
				MarkerOptions markerOptions = new MarkerOptions();
				markerOptions.SetPosition(location);
				thisMarker = thisMap.AddMarker(markerOptions);

				LocationTime.Text = res.GetString(Resource.String.ProfileViewLocation) + " " + c.GetTimeDiffStr(time, false);
				LocationTime.Visibility = ViewStates.Visible;

				if (!(displayUser.Distance is null))
				{
					float distance = CalculateDistance((double)Session.Latitude, (double)Session.Longitude, latitude, longitude);
					displayUser.Distance = distance;
					DistanceText.Text = distance + " km " + res.GetString(Resource.String.ProfileViewAway);
				}
			}
		}

		public void UpdateLocationSelf(long time, double latitude, double longitude)
		{
			Session.LastActiveDate = time;
			Session.Latitude = latitude;
			Session.Longitude = longitude;
			Session.LocationTime = time;

			LastActiveDate.Text = c.GetTimeDiffStr(time, true);

			LatLng location = new LatLng(latitude, longitude);
			thisMap.MoveCamera(CameraUpdateFactory.NewLatLng(location));

			if (!(thisMarker is null))
			{
				thisMarker.Remove();
			}
			MarkerOptions markerOptions = new MarkerOptions();
			markerOptions.SetPosition(location);
			thisMarker = thisMap.AddMarker(markerOptions);

			LocationTime.Text = res.GetString(Resource.String.ProfileViewLocation) + " " + c.GetTimeDiffStr(time, false);
			LocationTime.Visibility = ViewStates.Visible;
		}

		private float CalculateDistance(double lat1, double long1, double lat2, double long2)
		{
			return (float)Math.Round(6371 * Math.Acos(
			Math.Cos(Math.PI / 180 * lat1) * Math.Cos(Math.PI / 180 * lat2) * Math.Cos(Math.PI / 180 * long2 - Math.PI / 180 * long1)
			+ Math.Sin(Math.PI / 180 * lat1) * Math.Sin(Math.PI / 180 * lat2)
			), 1);
		}
	}
}