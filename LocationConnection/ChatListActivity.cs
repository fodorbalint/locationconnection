//does converview interfere with position when clicked?

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
using Android.Support.Design.Widget;
using Android.Support.Constraints;
using System.Timers;
using System.Reflection;

namespace LocationConnection
{
	[Activity(MainLauncher = false, ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
	public class ChatListActivity : BaseActivity
	{
		ImageButton MenuList;
		TextView NoofMatches, NoMatch;
		public ListView ChatUserList;
		View RippleMain, MenuListBg;

		Android.Content.Res.Resources res;

		public List<MatchItem> matchList;
		ChatUserListAdapter adapter;

		bool rippleRunning;
		int tweenTime = 300;
		Timer rippleTimer;

		int statusRoundBackground;

		protected override void OnCreate(Bundle savedInstanceState)
		{
			try
			{
				base.OnCreate(savedInstanceState);
				if (!ListActivity.initialized) { return; }

				if (Settings.DisplaySize == 1)
				{
					SetContentView(Resource.Layout.activity_chatlist_normal);

					statusRoundBackground = Resource.Drawable.status_round_background_normal;
				}
				else
				{
					SetContentView(Resource.Layout.activity_chatlist_small);

					statusRoundBackground = Resource.Drawable.status_round_background_small;
				}

				MainLayout = FindViewById<ConstraintLayout>(Resource.Id.MainLayout);
				NoMatch = FindViewById<TextView>(Resource.Id.NoMatch);
				ChatUserList = FindViewById<ListView>(Resource.Id.ChatUserList); 
				MenuListBg = FindViewById<View>(Resource.Id.MenuListBg);
				NoofMatches = FindViewById<TextView>(Resource.Id.NoofMatches);
				RippleMain = FindViewById<View>(Resource.Id.RippleMain);
				MenuList = FindViewById<ImageButton>(Resource.Id.MenuList);

				if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
				{
					MenuListBg.SetBackgroundResource(statusRoundBackground);
				}

				c.view = MainLayout;
				res = Resources;

				ChatUserList.ItemClick += ChatUserList_ItemClick;
				MenuList.Click += MenuList_Click;
				MenuList.Touch += MenuList_Touch;
			}
			catch (Exception ex)
			{
				c.ReportErrorSilent(ex.Message + System.Environment.NewLine + ex.StackTrace);
			}
		}

		protected async override void OnResume()
		{
			try
			{
				base.OnResume();
				if (!ListActivity.initialized) { return; }

				string responseString = await c.MakeRequest("action=loadmessagelist&ID=" + Session.ID + "&SessionID=" + Session.SessionID);
				if (responseString.Substring(0, 2) == "OK")
				{
					responseString = responseString.Substring(3);
					if (responseString != "")
					{
						NoMatch.Visibility = ViewStates.Gone;
						ServerParser<MatchItem> parser = new ServerParser<MatchItem>(responseString);
						matchList = parser.returnCollection;
						adapter = new ChatUserListAdapter(this, matchList);
						ImageCache.imagesInProgress = new List<string>();
						ChatUserListAdapter.loadCount = 0;
						ChatUserList.Adapter = adapter;
						NoofMatches.Text = (matchList.Count == 1) ? "1 " + res.GetString(Resource.String.ChatListMatch) : matchList.Count + " " + res.GetString(Resource.String.ChatListMatches);
					}
					else
					{
						matchList = new List<MatchItem>();
						adapter = new ChatUserListAdapter(this, matchList);
						ChatUserList.Adapter = adapter;
						NoMatch.Visibility = ViewStates.Visible;
						NoofMatches.Text = "";
					}
				}
				else
				{
					c.ReportError(responseString);
				}
				
			}
			catch (Exception ex)
			{
				c.ReportErrorSilent(ex.Message + System.Environment.NewLine + ex.StackTrace);
			}
		}

		public void InsertMessage(string meta, string body)
		{
			long unixTimestamp = c.Now();

			int sep1Pos = meta.IndexOf('|');
			int sep2Pos = meta.IndexOf('|', sep1Pos + 1);
			int sep3Pos = meta.IndexOf('|', sep2Pos + 1);

			int messageID = int.Parse(meta.Substring(0, sep1Pos));
			int senderID = int.Parse(meta.Substring(sep1Pos + 1, sep2Pos - sep1Pos - 1));
			long sentTime = long.Parse(meta.Substring(sep2Pos + 1, sep3Pos - sep2Pos - 1));
			long seenTime = unixTimestamp;
			long readTime = 0;

			for (int i = 0; i < matchList.Count; i++)
			{
				if (matchList[i].TargetID == senderID)
				{
					if (matchList[i].Chat.Length == 3)
					{
						matchList[i].Chat[0] = matchList[i].Chat[1];
						matchList[i].Chat[1] = matchList[i].Chat[2];
						matchList[i].Chat[2] = messageID + "|" + senderID + "|" + sentTime + "|" + seenTime + "|" + readTime + "|" + body;
					}
					else
					{
						List<string> chatList = new List<string>(matchList[i].Chat);
						chatList.Add(messageID + "|" + senderID + "|" + sentTime + "|" + seenTime + "|" + readTime + "|" + body);
						matchList[i].Chat = chatList.ToArray();
					}

					adapter.NotifyDataSetChanged();
					c.MakeRequest("action=messagedelivered&ID=" + Session.ID + "&SessionID=" + Session.SessionID + "&MatchID=" + matchList[i].MatchID + "&MessageID=" + messageID + "&Status=Seen");
				}
			}
		}

		public void AddMatchItem(MatchItem item)
		{
			matchList.Insert(0, item);
			adapter.NotifyDataSetChanged();

			NoMatch.Visibility = ViewStates.Gone;
			NoofMatches.Text = (matchList.Count == 1) ? "1 " + res.GetString(Resource.String.ChatListMatch) : matchList.Count + " " + res.GetString(Resource.String.ChatListMatches);
		}

		public void UpdateMatchItem(int matchID, bool active, long? unmatchDate)
		{
			for (int i = 0; i < matchList.Count; i++)
			{
				if (matchList[i].MatchID == matchID)
				{
					matchList[i].Active = active;
					matchList[i].UnmatchDate = unmatchDate;
				}
			}
			adapter.NotifyDataSetChanged();
		}

		private void MenuList_Click(object sender, EventArgs e)
		{
			OnBackPressed();
		}

		private void MenuList_Touch(object sender, View.TouchEventArgs e)
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

		private void ChatUserList_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
		{
			ClickListItem(e.Position);
		}

		public void ClickListItem(int index)
		{
			//if writing Session.CurrentMatch = matchList[index], changes in Session when loading Chat One would also apply to matchList
			Session.CurrentMatch = (MatchItem)CommonMethods.Clone(matchList[index]);
			
			Intent i = new Intent(this, typeof(ChatOneActivity));
			i.SetFlags(ActivityFlags.ReorderToFront);
			StartActivity(i);
		}
	}
}