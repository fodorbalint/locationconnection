using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using FFImageLoading;
using Android.Gms.Common;
using Firebase.Messaging;
using Firebase.Iid;
using Android.Util;
using Android.Views.InputMethods;
using Android.Support.Constraints;
using System.Globalization;
using Java.Interop;

namespace LocationConnection
{
	[Activity(MainLauncher = false, ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
	public class ChatOneActivity : BaseActivity, AbsListView.IOnScrollListener
	{
		public new View MainLayout;
		Android.Support.V7.Widget.Toolbar PageToolbar;
		ConstraintLayout ChatViewProfile;
		public ListView ChatMessageWindow;		
		IMenuItem MenuFriend, MenuLocationUpdates;
		ImageButton ChatOneBack, ChatSendMessage;
		ImageView ChatTargetImage;
		TextView TargetName, MatchDate, UnmatchDate;
		public TextView NoMessages;
		public EditText ChatEditMessage;

		public Resources res;
		InputMethodManager imm;
		ChatMessageWindowAdapter adapter;
		List<MessageItem> messageItems;
		string earlyDelivery;

		public int messageOwn;
		public int messageTarget;
		bool menuCreated;
		bool dataLoadStarted;
		public int currentLastVisibleItem;

		protected override void OnCreate(Bundle savedInstanceState)
		{
			try
			{
				base.OnCreate(savedInstanceState);
				if (!ListActivity.initialized) { return; }

				if (Settings.DisplaySize == 1)
				{
					SetContentView(Resource.Layout.activity_chatone_normal);
					messageOwn = Resource.Drawable.message_own_normal;
					messageTarget = Resource.Drawable.message_target_normal;
				}
				else
				{
					SetContentView(Resource.Layout.activity_chatone_small);
					messageOwn = Resource.Drawable.message_own_small;
					messageTarget = Resource.Drawable.message_target_small;
				}

				MainLayout = FindViewById<ConstraintLayout>(Resource.Id.MainLayout);
				PageToolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.PageToolbar);
				ChatViewProfile = FindViewById<ConstraintLayout>(Resource.Id.ChatViewProfile);
				ChatOneBack = FindViewById<ImageButton>(Resource.Id.ChatOneBack);
				ChatTargetImage = FindViewById<ImageView>(Resource.Id.ChatTargetImage);
				TargetName = FindViewById<TextView>(Resource.Id.TargetName);
				MatchDate = FindViewById<TextView>(Resource.Id.MatchDate);
				UnmatchDate = FindViewById<TextView>(Resource.Id.UnmatchDate);
				ChatMessageWindow = FindViewById<ListView>(Resource.Id.ChatMessageWindow);
				NoMessages = FindViewById<TextView>(Resource.Id.NoMessages);
				ChatEditMessage = FindViewById<EditText>(Resource.Id.ChatEditMessage);
				ChatSendMessage = FindViewById<ImageButton>(Resource.Id.ChatSendMessage);

				imm = (InputMethodManager)GetSystemService(Context.InputMethodService);
				c.view = MainLayout;
				res = Resources;
				menuCreated = false;

				SetSupportActionBar(PageToolbar);

				ChatOneBack.Click += ChatOneBack_Click;
				ChatViewProfile.Click += ChatViewProfile_Click;
				ChatSendMessage.Click += ChatSendMessage_Click;

				ChatMessageWindow.SetOnScrollListener(this);
				MainLayout.ViewTreeObserver.AddOnGlobalLayoutListener(new KeyboardListener(this));
			}
			catch (Exception ex)
			{
				c.ReportError(ex.Message + System.Environment.NewLine + ex.StackTrace);
			}
		}

		public void OnScroll(AbsListView lw, int firstVisibleItem, int visibleItemCount, int totalItemCount)
		{
		}

		public void OnScrollStateChanged(AbsListView view, [GeneratedEnum] ScrollState scrollState) //called when users starts or stops scrolling, and when scolling animation stops
		{
			if (scrollState == ScrollState.Idle)
			{
				currentLastVisibleItem = ChatMessageWindow.LastVisiblePosition;
			}
		}

		protected override async void OnResume()
		{
			try {
				base.OnResume();
				if (!ListActivity.initialized) { return; }

				MainLayout.RequestFocus();

				if (!(Session.SnackMessage is null))
				{
					c.SnackStr(res.GetString((int)Session.SnackMessage).Replace("[name]", Session.CurrentMatch.TargetName), null);
					Session.SnackMessage = null;
				}

				dataLoadStarted = false;

				if (menuCreated) //menu is not yet created after OnCreate.
				{
					SetMenu();
				}

				string responseString;
				if (!(IntentData.senderID is null))
				{
					dataLoadStarted = true;
										
					responseString = await c.MakeRequest("action=loadmessages&ID=" + Session.ID + "&SessionID=" + Session.SessionID + "&TargetID=" + (int)IntentData.senderID);
					if (menuCreated) //erase data only if location update menu was set.
					{
						IntentData.senderID = null;
					}

					if (responseString.Substring(0, 2) == "OK")
					{
						LoadMessages(responseString, false);
					}
					else if (responseString == "ERROR_MatchNotFound")
					{
						Session.SnackMessage = Resource.String.MatchNotFound;
						OnBackPressed();
					}
					else {
						c.ReportError(responseString);
					}
				}
				else
				{
					dataLoadStarted = true;
					LoadHeader();

					responseString = await c.MakeRequest("action=loadmessages&ID=" + Session.ID + "&SessionID=" + Session.SessionID + "&MatchID=" + Session.CurrentMatch.MatchID);

					if (responseString.Substring(0, 2) == "OK")
					{
						LoadMessages(responseString, true);
					}
					else
					{
						c.ReportError(responseString);
					}
				}
			}
			catch (Exception ex)
			{
				c.ReportError(ex.Message + System.Environment.NewLine + ex.StackTrace);
			}
		}

		public async void RefreshPage()
		{
			MainLayout.RequestFocus();
			ChatEditMessage.Text = "";

			string responseString = await c.MakeRequest("action=loadmessages&ID=" + Session.ID + "&SessionID=" + Session.SessionID + "&TargetID=" + (int)IntentData.senderID);

			IntentData.senderID = null;

			if (responseString.Substring(0, 2) == "OK")
			{
				LoadMessages(responseString, false);
			}
			else if (responseString == "ERROR_MatchNotFound")
			{
				Session.SnackMessage = Resource.String.MatchNotFound;
				OnBackPressed();
			}
			else
			{
				c.ReportError(responseString);
			}
		}

		public override bool OnCreateOptionsMenu(IMenu menu)
		{
			MenuInflater.Inflate(Resource.Menu.menu_chatone, menu);
			MenuLocationUpdates = menu.FindItem(Resource.Id.MenuLocationUpdates);
			MenuFriend = menu.FindItem(Resource.Id.MenuFriend);
			menuCreated = true;

			SetMenu();

			return base.OnCreateOptionsMenu(menu);
		}

		private void SetMenu()
		{
			int targetID;
			if (IntentData.senderID != null) //click from an in-app notification
			{
				targetID = (int)IntentData.senderID;
				if (dataLoadStarted)
				{
					IntentData.senderID = null;
				}
			}
			else
			{
				targetID = (int)Session.CurrentMatch.TargetID;
			}

			if ((bool)Session.UseLocation && c.IsLocationEnabled())
			{
				MenuLocationUpdates.SetVisible(true);
				if (IsUpdatingTo(targetID))
				{
					MenuLocationUpdates.SetTitle(Resource.String.MenuStopLocationUpdates);
				}
				else
				{
					MenuLocationUpdates.SetTitle(Resource.String.MenuStartLocationUpdates);
				}
			}
			else
			{
				MenuLocationUpdates.SetVisible(false);
			}

			if (!(Session.CurrentMatch is null))
			{
				if (Session.CurrentMatch.UnmatchDate is null)
				{
					MenuFriend.SetVisible(true);
				}
				else
				{
					MenuFriend.SetVisible(false);
				}
			}
		}

		public override bool OnOptionsItemSelected(IMenuItem item)
		{
			int id = item.ItemId;

			if (!(Session.CurrentMatch is null) && !(Session.CurrentMatch.Friend is null)) //if data loaded, whether coming from intent or chat list
			{
				switch (id)
				{
					case Resource.Id.MenuLocationUpdates:
						if (IsUpdatingTo((int)Session.CurrentMatch.TargetID))
						{
							StopRealTimeLocation();
						}
						else
						{
							if (Session.LocationAccuracy == 0 || Session.InAppLocationRate > 60)
							{
								ChangeSettings();
							}
							else
							{
								StartRealTimeLocation();
							}
						}
						break;
					case Resource.Id.MenuFriend:
						AddFriend();
						break;
					case Resource.Id.MenuUnmatch:
						Unmatch();
						break;

				}
			}
			else
			{
				c.Snack(Resource.String.ChatOneDataLoading, null);
			}

			return base.OnOptionsItemSelected(item);
		}

		private void LoadHeader()
		{
			TargetName.Text = Session.CurrentMatch.TargetName;
			string url;
			if (Constants.isTestDB)
			{
				url = Constants.HostName + Constants.UploadFolderTest + "/" + Session.CurrentMatch.TargetID + "/" + Constants.SmallImageSize + "/" + Session.CurrentMatch.TargetPicture;
			}
			else
			{
				url = Constants.HostName + Constants.UploadFolder + "/" + Session.CurrentMatch.TargetID + "/" + Constants.SmallImageSize + "/" + Session.CurrentMatch.TargetPicture;
			}
			
			ImageService im = new ImageService();
			im.LoadUrl(url).LoadingPlaceholder(Constants.loadingImage, FFImageLoading.Work.ImageSource.CompiledResource).ErrorPlaceholder(Constants.noImage, FFImageLoading.Work.ImageSource.CompiledResource).Into(ChatTargetImage);
		}

		private void LoadMessages(string responseString, bool merge)
		{
			responseString = responseString.Substring(3);

			if (!merge)
			{
				ServerParser<MatchItem> parser = new ServerParser<MatchItem>(responseString);
				Session.CurrentMatch = parser.returnCollection[0];
				LoadHeader();
			}
			else
			{
				//we need to add the new properties to the existing MatchItem.
				MatchItem sessionMatchItem = Session.CurrentMatch;
				ServerParser<MatchItem> parser = new ServerParser<MatchItem>(responseString);
				MatchItem mergeMatchItem = parser.returnCollection[0];
				Type type = typeof(MatchItem);
				FieldInfo[] fieldInfos = type.GetFields();
				foreach (FieldInfo field in fieldInfos)
				{
					object value = field.GetValue(mergeMatchItem);
					if (value != null)
					{
						field.SetValue(sessionMatchItem, value);
					}
				}
			}
			
			DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds((long)Session.CurrentMatch.MatchDate).ToLocalTime();
			MatchDate.Text = res.GetString(Resource.String.Matched) + ": " + dt.ToString("dd MMMM yyyy HH:mm");
			if (!(Session.CurrentMatch.UnmatchDate is null))
			{
				dt = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds((long)Session.CurrentMatch.UnmatchDate).ToLocalTime();
				UnmatchDate.Text = res.GetString(Resource.String.Unmatched) + ": " + dt.ToString("dd MMMM yyyy HH:mm");
				MenuFriend.SetVisible(false);
			}
			else
			{
				UnmatchDate.Text = "";
				MenuFriend.SetVisible(true);
			}

			if ((bool)Session.CurrentMatch.Active)
			{
				ChatViewProfile.Click += ChatViewProfile_Click;
				if ((bool)Session.UseLocation && c.IsLocationEnabled())
				{
					MenuLocationUpdates.SetVisible(true);
				}
				else
				{
					MenuLocationUpdates.SetVisible(false);
				}
				ChatEditMessage.Enabled = true;
				ChatSendMessage.Enabled = true;
				ChatSendMessage.ImageAlpha = 255;
			}
			else
			{
				MenuLocationUpdates.SetVisible(false);
				ChatEditMessage.Enabled = false;
				ChatSendMessage.Enabled = false;
				ChatSendMessage.ImageAlpha = 128;
			}

			if (!(bool)Session.CurrentMatch.Friend)
			{
				MenuFriend.SetTitle(Resource.String.MenuAddFriend);
			}
			else
			{
				MenuFriend.SetTitle(Resource.String.MenuRemoveFriend);
			}

			messageItems = new List<MessageItem>();
			if (Session.CurrentMatch.Chat.Length != 0)
			{
				NoMessages.Visibility = ViewStates.Gone;
				foreach (string item in Session.CurrentMatch.Chat)
				{
					AddMessageItem(item);
				}
			}
			else
			{
				NoMessages.Visibility = ViewStates.Visible;
			}
			adapter = new ChatMessageWindowAdapter(this, messageItems);
			ChatMessageWindow.Adapter = adapter; //Without using list, 213 item created in 646 ms / 385 ms on subsequent loading
			ScrollToBottom(false);
		}

		public void AddMessageItem(string messageItem)
		{
			int sep1Pos = messageItem.IndexOf('|');
			int sep2Pos = messageItem.IndexOf('|', sep1Pos + 1);
			int sep3Pos = messageItem.IndexOf('|', sep2Pos + 1);
			int sep4Pos = messageItem.IndexOf('|', sep3Pos + 1);
			int sep5Pos = messageItem.IndexOf('|', sep4Pos + 1);

			MessageItem item = new MessageItem
			{
				MessageID = int.Parse(messageItem.Substring(0, sep1Pos)),
				SenderID = int.Parse(messageItem.Substring(sep1Pos + 1, sep2Pos - sep1Pos - 1)),
				SentTime = long.Parse(messageItem.Substring(sep2Pos + 1, sep3Pos - sep2Pos - 1)),
				SeenTime = long.Parse(messageItem.Substring(sep3Pos + 1, sep4Pos - sep3Pos - 1)),
				ReadTime = long.Parse(messageItem.Substring(sep4Pos + 1, sep5Pos - sep4Pos - 1)),
				Content = CommonMethods.UnescapeBraces(messageItem.Substring(sep5Pos + 1))
			};
			messageItems.Add(item);
		}

		public void AddMessageItemOne(string messageItem)
		{
			NoMessages.Visibility = ViewStates.Gone;
			AddMessageItem(messageItem);
			adapter.NotifyDataSetChanged();
			ScrollToBottom(true);
		}

		public void UpdateMessageItem(string meta) // MessageID|SentTime|SeenTime|ReadTime 
		{
			//situation: sending two chats at the same time.
			//both parties will be their message first (it is faster to get a response from a server than the server sending a cloud message to the recipient)
			//but for one person their message is actually the second.
			//if someone sends 2 messages within 2 seconds, the tags may be the same. What are the consequences? In practice it is not a situation we have to deal with.

			int sep1Pos = meta.IndexOf('|');
			int sep2Pos = meta.IndexOf('|', sep1Pos + 1);
			int sep3Pos = meta.IndexOf('|', sep2Pos + 1);

			int messageID = int.Parse(meta.Substring(0, sep1Pos));
			long sentTime = long.Parse(meta.Substring(sep1Pos + 1, sep2Pos - sep1Pos - 1));
			long seenTime = long.Parse(meta.Substring(sep2Pos + 1, sep3Pos - sep2Pos - 1));
			long readTime = long.Parse(meta.Substring(sep3Pos + 1));

			int messageIndex = messageID - 1;

			if (messageIndex >= messageItems.Count) //message exists
			{
				earlyDelivery = meta;
				return;
			}
			MessageItem item = messageItems[messageIndex];

			if (item.MessageID == messageID) //normal case
			{
				item.SentTime = sentTime;
				item.SeenTime = seenTime;
				item.ReadTime = readTime;
			}
			else //two messages were sent at the same time from both parties, and for one, the order of the two messages may be the other way, if the server response was faster than google cloud.
			{
				messageIndex = messageIndex - 1;
				item = messageItems[messageIndex];
				if (item.MessageID == messageID)
				{
					item.SentTime = sentTime;
					item.SeenTime = seenTime;
					item.ReadTime = readTime;
				}
			}
			adapter.NotifyDataSetChanged();
		}

		public void UpdateStatus(int senderID, bool active, long? unmatchDate)
		{
			if (senderID == Session.CurrentMatch.TargetID)
			{
				Session.CurrentMatch.Active = active;
				Session.CurrentMatch.UnmatchDate = unmatchDate;

				if (!(unmatchDate is null))
				{
					DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds((long)Session.CurrentMatch.UnmatchDate).ToLocalTime();
					UnmatchDate.Text = res.GetString(Resource.String.Unmatched) + ": " + dt.ToString("dd MMMM yyyy HH:mm");
					MenuFriend.SetVisible(false);
				}
				else
				{
					UnmatchDate.Text = "";
					MenuFriend.SetVisible(true);
				}

				if (active)
				{
					ChatEditMessage.Enabled = true;
					ChatSendMessage.Enabled = true;
					ChatSendMessage.ImageAlpha = 255;
				}
				else
				{
					ChatEditMessage.Enabled = false;
					ChatSendMessage.Enabled = false;
					ChatSendMessage.ImageAlpha = 128;
				}
			}
		}

		private async void ChatSendMessage_Click(object sender, EventArgs e)
		{
			string message = ChatEditMessage.Text;
			imm.HideSoftInputFromWindow(ChatEditMessage.WindowToken, 0);

			if (message.Length != 0)
			{
				ChatSendMessage.Enabled = false; //to prevent mulitple clicks			
				string responseString = await c.MakeRequest("action=sendmessage&ID=" + Session.ID + "&SessionID=" + Session.SessionID + "&MatchID=" + Session.CurrentMatch.MatchID + "&message=" + c.UrlEncode(message));
				if (responseString.Substring(0, 2) == "OK")
				{
					ChatEditMessage.Text = "";
					MainLayout.RequestFocus();

					string messageItem;
					if (earlyDelivery is null)
					{
						responseString = responseString.Substring(3);
						int sep1Pos = responseString.IndexOf("|");
						int sep2Pos = responseString.IndexOf("|", sep1Pos + 1);
						int messageID = int.Parse(responseString.Substring(0, sep1Pos));
						long sentTime = long.Parse(responseString.Substring(sep1Pos + 1, sep2Pos - sep1Pos - 1));
						string newRate = responseString.Substring(sep2Pos + 1);
						messageItem = messageID + "|" + Session.ID + "|" + sentTime + "|0|0|" + message;
						if (newRate != "")
						{
							Session.ResponseRate = float.Parse(newRate, CultureInfo.InvariantCulture);
						}
					}
					else
					{
						messageItem = earlyDelivery + "|" + message;
						earlyDelivery = null;
					}

					AddMessageItemOne(messageItem);
				}
				else if (responseString.Substring(0, 6) == "ERROR_")
				{
					c.SnackStr(res.GetString(Resources.GetIdentifier(responseString.Substring(6), "string", PackageName)).Replace("[name]", Session.CurrentMatch.TargetName), null);
				}
				else
				{
					c.ReportError(responseString);
				}
				ChatSendMessage.Enabled = true;
			}
		}

		private async void ChangeSettings()
		{
			string result = await c.DisplayCustomDialog("", res.GetString(Resource.String.ChangeUpdateCriteria),
									res.GetString(Resource.String.DialogYes), res.GetString(Resource.String.DialogNo));
			if (result == res.GetString(Resource.String.DialogYes))
			{
				string requestStringBase = "action=updatesettings&ID=" + Session.ID + "&SessionID=" + Session.SessionID;
				string requestStringAdd = "";
				if (Session.LocationAccuracy == 0)
				{
					requestStringAdd += "&LocationAccuracy=1";
				}
				if (Session.InAppLocationRate > 60)
				{
					requestStringAdd += "&InAppLocationRate=60";
				}

				string responseString = await c.MakeRequest(requestStringBase + requestStringAdd);
				if (responseString.Substring(0, 2) == "OK")
				{
					if (responseString.Length > 2) //a change happened
					{
						c.LogActivity("ChatOne changed settings: " + responseString);
						c.LoadCurrentUser(responseString);
						StartRealTimeLocation();
					}
				}
				else
				{
					c.ReportError(responseString);
				}
			}
		}

		private void StartRealTimeLocation()
		{
			if ((bool)Session.CurrentMatch.Friend)
			{
				if (Session.LocationShare < 1)
				{
					c.SnackStr(res.GetString(Resource.String.EnableLocationLevelFriend).Replace("[name]", Session.CurrentMatch.TargetName),2);
					return;
				}
			}
			else
			{
				if (Session.LocationShare < 2)
				{
					c.SnackStr(res.GetString(Resource.String.EnableLocationLevelMatch).Replace("[name]", Session.CurrentMatch.TargetName)
						.Replace("[sex]", (Session.CurrentMatch.Sex == 0) ? res.GetString(Resource.String.SexHer) : res.GetString(Resource.String.SexHim)), 3);
					return;
				}
			}
			AddUpdatesTo((int)Session.CurrentMatch.TargetID);
			MenuLocationUpdates.SetTitle(Resource.String.MenuStopLocationUpdates);

			StopLocationUpdates();
			StartLocationUpdates((int)Session.InAppLocationRate * 1000);
			c.Snack(Resource.String.LocationUpdatesToStart, null);
		}

		private void StopRealTimeLocation()
		{
			RemoveUpdatesTo((int)Session.CurrentMatch.TargetID);
			MenuLocationUpdates.SetTitle(Resource.String.MenuStartLocationUpdates);
			c.Snack(Resource.String.LocationUpdatesToEnd, null);
			EndLocationShare((int)Session.CurrentMatch.TargetID);
		}

		private async void AddFriend()
		{
			long unixTimestamp = c.Now();
			if (!(bool)Session.CurrentMatch.Friend)
			{
				string responseString = await c.MakeRequest("action=addfriend&ID=" + Session.ID + "&target=" + Session.CurrentMatch.TargetID
		+ "&time=" + unixTimestamp + "&SessionID=" + Session.SessionID);
				if (responseString == "OK")
				{
					Session.CurrentMatch.Friend = true;
					c.Snack(Resource.String.FriendAdded, null);
					MenuFriend.SetTitle(Resource.String.MenuRemoveFriend);
				}
				else
				{
					c.ReportError(responseString);
				}
			}
			else
			{
				string responseString = await c.MakeRequest("action=removefriend&ID=" + Session.ID + "&target=" + Session.CurrentMatch.TargetID
		+ "&time=" + unixTimestamp + "&SessionID=" + Session.SessionID);
				if (responseString == "OK")
				{
					Session.CurrentMatch.Friend = false;
					c.Snack(Resource.String.FriendRemoved, null);
					MenuFriend.SetTitle(Resource.String.MenuAddFriend);
				}
				else
				{
					c.ReportError(responseString);
				}
			}
		}

		private async void Unmatch()
		{
			string displayText;
			if (Session.CurrentMatch.TargetID == 0)
			{
				displayText = res.GetString(Resource.String.DialogUnmatchDeleted);
			}
			else
			{
				displayText = (Session.CurrentMatch.UnmatchDate is null) ? res.GetString(Resource.String.DialogUnmatchMatched) : res.GetString(Resource.String.DialogUnmatchUnmatched);
				displayText = displayText.Replace("[name]", Session.CurrentMatch.TargetName);
				displayText = displayText.Replace("[sex]", (Session.CurrentMatch.Sex == 0) ? res.GetString(Resource.String.SexShe) : res.GetString(Resource.String.SexHe));
			}
			
			string dialogResponse = await c.DisplayCustomDialog(res.GetString(Resource.String.ConfirmAction), displayText,
				res.GetString(Resource.String.DialogOK), res.GetString(Resource.String.DialogCancel));
			if (dialogResponse == res.GetString(Resource.String.DialogOK))
			{
				if (IsUpdatingTo((int)Session.CurrentMatch.TargetID)) //user could have gone to the background, clearing out the list of people to receive updates from.
				{
					RemoveUpdatesTo((int)Session.CurrentMatch.TargetID);
				}
					
				long unixTimestamp = c.Now();
				string responseString = await c.MakeRequest("action=unmatch&ID=" + Session.ID + "&target=" + Session.CurrentMatch.TargetID
					+ "&time=" + unixTimestamp + "&SessionID=" + Session.SessionID);
				if (responseString == "OK")
				{
					if (!(ListActivity.listProfiles is null))
					{
						foreach (Profile item in ListActivity.listProfiles)
						{
							if (item.ID == Session.CurrentMatch.TargetID)
							{
								item.UserRelation = 0;
							}
						}
					}
					if (!(ListActivity.viewProfiles is null))
					{
						foreach (Profile item in ListActivity.viewProfiles)
						{
							if (item.ID == Session.CurrentMatch.TargetID)
							{
								item.UserRelation = 0;
							}
						}
					}
					Session.CurrentMatch = null;
					OnBackPressed();
				}
				else
				{
					c.ReportError(responseString);
				}
			}
		}

		private void ChatViewProfile_Click(object sender, EventArgs e)
		{
			Intent i = new Intent(this, typeof(ProfileViewActivity));
			i.SetFlags(ActivityFlags.ReorderToFront);
			IntentData.targetID = (int)Session.CurrentMatch.TargetID;
			IntentData.pageType = "standalone";		
			StartActivity(i);
		}		

		private void ChatOneBack_Click(object sender, EventArgs e)
		{
			OnBackPressed();
		}

		public bool IsBottom()
		{
			return currentLastVisibleItem == adapter.Count - 1;
		}

		public void ScrollToBottom(bool animated)
		{
			if (animated)
			{
				ChatMessageWindow.SmoothScrollToPosition(adapter.Count - 1);
			}
			else
			{
				ChatMessageWindow.SetSelection(adapter.Count - 1);
				currentLastVisibleItem = adapter.Count - 1;
			}
		}
	}

	public class KeyboardListener : Java.Lang.Object, ViewTreeObserver.IOnGlobalLayoutListener
	{
		ChatOneActivity context;

		public KeyboardListener(ChatOneActivity context)
		{
			this.context = context;
		}
		public void OnGlobalLayout()
		{
			Rect r = new Rect();
			context.MainLayout.GetWindowVisibleDisplayFrame(r);
			int screenHeight = context.MainLayout.RootView.Height;
			int keyboardHeight = screenHeight - r.Bottom;

			if (keyboardHeight > screenHeight * 0.15 && context.IsBottom())
			{
				context.ScrollToBottom(true); //list is no longer scrollable if no animation is used.
			}

			context.currentLastVisibleItem = context.ChatMessageWindow.LastVisiblePosition; //enables to change it by scrolling when keyboard is open
		}
	}
}