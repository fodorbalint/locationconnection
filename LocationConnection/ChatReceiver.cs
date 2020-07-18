using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Gms.Maps.Model;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.Content;
using Android.Views;
using Android.Widget;

namespace LocationConnection
{
	[BroadcastReceiver(Enabled = true, Exported = false)]
	public class ChatReceiver : BroadcastReceiver
	{
		Context context;

		public override void OnReceive(Context context, Intent intent)
		{
			int sep1Pos;
			int sep2Pos;
			int sep3Pos;
			int sep4Pos;
			int matchID;
			string senderName;
			string text;

			this.context = context;
			int senderID = int.Parse(intent.GetStringExtra("fromuser"));
			int targetID = int.Parse(intent.GetStringExtra("touser"));
			string type = intent.GetStringExtra("type");
			string meta = intent.GetStringExtra("meta");
			bool inApp = intent.GetBooleanExtra("inapp", false);

			((BaseActivity)context).c.Log("ChatReceiver senderID " + senderID + " type " + type + " meta " + meta + " inApp " + inApp);

			if (targetID != Session.ID)
			{
				return;
			}

			try
			{
				switch (type)
				{
					case "sendMessage":
						string title = intent.GetStringExtra("title");
						string body = intent.GetStringExtra("body");

						if (context is ChatOneActivity)
						{
							long unixTimestamp = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

							//we need to update the Read time locally for display purposes before 
							sep1Pos = meta.IndexOf('|');
							sep2Pos = meta.IndexOf('|', sep1Pos + 1);
							sep3Pos = meta.IndexOf('|', sep2Pos + 1);

							int messageID = int.Parse(meta.Substring(0, sep1Pos));
							long sentTime = long.Parse(meta.Substring(sep2Pos + 1, sep3Pos - sep2Pos - 1));
							long seenTime = unixTimestamp;
							long readTime = unixTimestamp;

							meta = messageID + "|" + senderID + "|" + sentTime + "|" + seenTime + "|" + readTime + "|";

							if (senderID != Session.ID && senderID == Session.CurrentMatch.TargetID) //for tests, you can use 2 accounts from the same device, and a sent message would appear duplicate.
							{
								((ChatOneActivity)context).AddMessageItemOne(meta + body);
								((ChatOneActivity)context).c.MakeRequest("action=messagedelivered&ID=" + Session.ID + "&SessionID=" + Session.SessionID + "&MatchID=" + Session.CurrentMatch.MatchID + "&MessageID=" + messageID + "&Status=Read");
							}
							else if (inApp && senderID != Session.ID)
							{
								((BaseActivity)context).c.SnackAction(title, Resource.String.ShowReceived, new Action<View>(delegate (View obj) { GoToChatNoOpen(senderID); }));
							}
						}
						else
						{
							if (inApp)
							{
								((BaseActivity)context).c.SnackAction(title, Resource.String.ShowReceived, new Action<View>(delegate (View obj) { GoToChat(senderID); }));
							}

							//update message list
							if (context is ChatListActivity)
							{
								((ChatListActivity)context).InsertMessage(meta, body);
							}
						}
						break;

					case "messageDelivered":
					case "loadMessages":
					case "loadMessageList":
						if (context is ChatOneActivity && senderID == Session.CurrentMatch.TargetID)
						{
							string[] updateItems = meta.Substring(1, meta.Length - 2).Split("}{");
							foreach (string item in updateItems)
							{
								((ChatOneActivity)context).UpdateMessageItem(item);
							}
						}
						break;

					case "matchProfile":
						if (inApp) //it is impossible to stand in that chat if wasn't previously a match
						{
							title = intent.GetStringExtra("title");
							if (context is ChatOneActivity)
							{
								((BaseActivity)context).c.SnackAction(title, Resource.String.ShowReceived, new Action<View>(delegate (View obj) { GoToChatNoOpen(senderID); }));
							}
							else
							{
								((BaseActivity)context).c.SnackAction(title, Resource.String.ShowReceived, new Action<View>(delegate (View obj) { GoToChat(senderID); }));
							}
						}

						if (context is ChatListActivity)
						{
							string matchItem = meta;
							ServerParser<MatchItem> parser = new ServerParser<MatchItem>(matchItem);
							((ChatListActivity)context).AddMatchItem(parser.returnCollection[0]);
						}

						AddUpdateMatch(senderID, true);
						if (context is ProfileViewActivity)
						{
							((ProfileViewActivity)context).UpdateStatus(senderID, true);
						}
						break;

					case "rematchProfile":
						sep1Pos = meta.IndexOf('|');

						matchID = int.Parse(meta.Substring(0, sep1Pos));
						bool active = bool.Parse(meta.Substring(sep1Pos + 1));

						if (inApp)
						{
							title = intent.GetStringExtra("title");
							if (context is ChatOneActivity && Session.CurrentMatch.TargetID == senderID)
							{
								((BaseActivity)context).c.SnackStr(title);
							}
							else if (context is ChatOneActivity)
							{
								((BaseActivity)context).c.SnackAction(title, Resource.String.ShowReceived, new Action<View>(delegate (View obj) { GoToChatNoOpen(senderID); }));
							}
							else
							{
								((BaseActivity)context).c.SnackAction(title, Resource.String.ShowReceived, new Action<View>(delegate (View obj) { GoToChat(senderID); }));
							}
						}

						AddUpdateMatch(senderID, true);
						if (context is ChatListActivity)
						{
							((ChatListActivity)context).UpdateMatchItem(matchID, active, null);
						}
						else if (context is ChatOneActivity)
						{
							((ChatOneActivity)context).UpdateStatus(senderID, active, null);
						}
						else if (context is ProfileViewActivity)
						{
							((ProfileViewActivity)context).UpdateStatus(senderID, true);
						}

						break;

					case "unmatchProfile":
						sep1Pos = meta.IndexOf('|');

						matchID = int.Parse(meta.Substring(0, sep1Pos));
						long unmatchDate = long.Parse(meta.Substring(sep1Pos + 1));

						if (((BaseActivity)context).IsUpdatingFrom(senderID))
						{
							((BaseActivity)context).RemoveUpdatesFrom(senderID);
						}
						if (((BaseActivity)context).IsUpdatingTo(senderID))
						{
							((BaseActivity)context).RemoveUpdatesTo(senderID);
						}

						if (inApp)
						{
							title = intent.GetStringExtra("title");
							if (context is ChatOneActivity && Session.CurrentMatch.TargetID == senderID)
							{
								((BaseActivity)context).c.SnackStr(title);
							}
							else if (context is ChatOneActivity)
							{
								((BaseActivity)context).c.SnackAction(title, Resource.String.ShowReceived, new Action<View>(delegate (View obj) { GoToChatNoOpen(senderID); }));
							}
							else
							{
								((BaseActivity)context).c.SnackAction(title, Resource.String.ShowReceived, new Action<View>(delegate (View obj) { GoToChat(senderID); }));
							}
						}

						AddUpdateMatch(senderID, false);
						if (context is ChatListActivity)
						{
							((ChatListActivity)context).UpdateMatchItem(matchID, false, unmatchDate);
						}
						else if (context is ChatOneActivity)
						{
							((ChatOneActivity)context).UpdateStatus(senderID, false, unmatchDate);
						}
						else if (context is ProfileViewActivity)
						{
							((ProfileViewActivity)context).UpdateStatus(senderID, false);
						}
						
						break;

					case "locationUpdate":
						sep1Pos = meta.IndexOf('|');
						sep2Pos = meta.IndexOf('|', sep1Pos + 1);

						senderName = meta.Substring(0, sep1Pos);
						int frequency = int.Parse(meta.Substring(sep1Pos + 1, sep2Pos - sep1Pos - 1));

						if (!((BaseActivity)context).IsUpdatingFrom(senderID))
						{
							((BaseActivity)context).AddUpdatesFrom(senderID);

							text = senderName + " " + context.Resources.GetString(Resource.String.LocationUpdatesFromStart) + " " + frequency + " s.";
							if (context is ProfileViewActivity)
							{
								((ProfileViewActivity)context).UpdateLocationStart(senderID, text);
							}
							else
							{
								((BaseActivity)context).c.SnackAction(text, Resource.String.ShowReceived, new Action<View>(delegate (View obj) { GoToProfile(senderID); }));
							}
						}

						sep3Pos = meta.IndexOf('|', sep2Pos + 1);
						sep4Pos = meta.IndexOf('|', sep3Pos + 1);

						long time = long.Parse(meta.Substring(sep2Pos + 1, sep3Pos - sep2Pos - 1));
						double latitude = double.Parse(meta.Substring(sep3Pos + 1, sep4Pos - sep3Pos - 1), CultureInfo.InvariantCulture);
						double longitude = double.Parse(meta.Substring(sep4Pos + 1), CultureInfo.InvariantCulture);

						((BaseActivity)context).AddLocationData(senderID, latitude, longitude, time);

						if (!(ListActivity.listProfiles is null))
						{
							foreach (Profile user in ListActivity.listProfiles)
							{
								if (user.ID == senderID)
								{
									user.LastActiveDate = time;
									user.Latitude = latitude;
									user.Longitude = longitude;
									user.LocationTime = time;
								}
							}
						}

						if (context is ListActivity && (bool)Settings.IsMapView)
						{
							foreach (Marker marker in ListActivity.profileMarkers)
							{
								if (marker.Title == senderID.ToString())
								{
									marker.Position = new LatLng(latitude, longitude);
								}
							}
						}
						else if (context is ProfileViewActivity)
						{
							((ProfileViewActivity)context).UpdateLocation(senderID, time, latitude, longitude);
						}
						break;

					case "locationUpdateEnd":
						senderName = meta;

						if (((BaseActivity)context).IsUpdatingFrom(senderID)) //user could have gone to the background, clearing out the list of people to receive updates from.
						{
							((BaseActivity)context).RemoveUpdatesFrom(senderID);

							text = senderName + " " + context.Resources.GetString(Resource.String.LocationUpdatesFromEnd);
							((BaseActivity)context).c.SnackStr(text);
						}
						break;
				}
			}
			catch (Exception ex)
			{
				CommonMethods c = new CommonMethods(null);
				c.ReportErrorSilent(ex.Message + System.Environment.NewLine + ex.StackTrace + System.Environment.NewLine + " Error in ChatReceiver");
			}
		}

		private void GoToChat(int senderID) {
			Intent i = new Intent(context, typeof(ChatOneActivity));
			i.SetFlags(ActivityFlags.ReorderToFront);
			IntentData.senderID = senderID;
			context.StartActivity(i);
		}

		private void GoToChatNoOpen(int senderID)
		{
			IntentData.senderID = senderID;
			((ChatOneActivity)context).RefreshPage();
		}

		private void GoToProfile(int targetID)
		{
			if (!(context is ChatOneActivity))
			{
				Session.CurrentMatch = null; //It must be set to null, otherwise when clicking the chat button, we are going back to the current activity if a chat was open before
				//currentmatch should be kept even if standing another chat, because pressing the back button from profile view should take us back to the current chat.
			}

			Intent i = new Intent(context, typeof(ProfileViewActivity));
			i.SetFlags(ActivityFlags.ReorderToFront);
			IntentData.profileViewPageType = Constants.ProfileViewType_Standalone;
			IntentData.targetID = targetID; 
			context.StartActivity(i);
		}

		public static void AddUpdateMatch(int senderID, bool isMatch)
		{
			if (!(ListActivity.viewProfiles is null))
			{
				for (int i = 0; i < ListActivity.viewProfiles.Count; i++)
				{
					if (ListActivity.viewProfiles[i].ID == senderID)
					{
						if (isMatch)
						{
							ListActivity.viewProfiles[i].UserRelation = 3;
						}
						else
						{
							ListActivity.viewProfiles[i].UserRelation = 2;
						}
						break;
					}
				}
			}

			if (!(ListActivity.listProfiles is null))
			{
				for (int i = 0; i < ListActivity.listProfiles.Count; i++)
				{
					if (ListActivity.listProfiles[i].ID == senderID)
					{
						if (isMatch)
						{
							ListActivity.listProfiles[i].UserRelation = 3;
						}
						else
						{
							ListActivity.listProfiles[i].UserRelation = 2;
						}
						break;
					}
				}
			}
		}
	}
}