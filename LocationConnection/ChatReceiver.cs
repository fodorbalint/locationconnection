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
			int senderID;
			int matchID;
			string senderName;
			string text;

			this.context = context;
			//int senderID = int.Parse()
			string type = intent.GetStringExtra("type");
			string meta = intent.GetStringExtra("meta");
			bool inApp = intent.GetBooleanExtra("inapp", false);
			
			try
			{
				((BaseActivity)context).c.LogActivity("ChatReceiver OnReceive " + type);
				
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
							senderID = int.Parse(meta.Substring(sep1Pos + 1, sep2Pos - sep1Pos - 1));
							long sentTime = long.Parse(meta.Substring(sep2Pos + 1, sep3Pos - sep2Pos - 1));
							long seenTime = unixTimestamp;
							long readTime = unixTimestamp;

							meta = messageID + "|" + senderID + "|" + sentTime + "|" + seenTime + "|" + readTime + "|";

							if (senderID != Session.ID) //for tests, you can use 2 accounts from the same device, and a sent message would appear duplicate.
							{
								((ChatOneActivity)context).NoMessages.Visibility = ViewStates.Gone;
								((ChatOneActivity)context).AddMessageItem(meta + body);
								((ChatOneActivity)context).SetScrollTimer();
								((ChatOneActivity)context).c.MakeRequest("action=messagedelivered&ID=" + Session.ID + "&SessionID=" + Session.SessionID + "&MatchID=" + Session.CurrentMatch.MatchID + "&MessageID=" + messageID + "&Status=Read");
							}
						}
						else
						{
							if (inApp)
							{
								sep1Pos = meta.IndexOf('|');
								sep2Pos = meta.IndexOf('|', sep1Pos + 1);

								int messageID = int.Parse(meta.Substring(0, sep1Pos));
								senderID = int.Parse(meta.Substring(sep1Pos + 1, sep2Pos - sep1Pos - 1));

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
						if (context is ChatOneActivity)
						{
							string[] updateItems = meta.Substring(1, meta.Length - 2).Split("}{");
							foreach (string item in updateItems)
							{
								((ChatOneActivity)context).UpdateMessageItem(item);

							}
						}
						break;

					case "matchProfile":
						sep1Pos = meta.IndexOf('|');
						senderID = int.Parse(meta.Substring(0, sep1Pos));

						if (inApp)
						{
							title = intent.GetStringExtra("title");
							((BaseActivity)context).c.SnackAction(title, Resource.String.ShowReceived, new Action<View>(delegate (View obj) { GoToChat(senderID); }));
						}

						if (context is ChatListActivity)
						{
							string matchItem = meta.Substring(sep1Pos + 1);
							ServerParser<MatchItem> parser = new ServerParser<MatchItem>(matchItem);
							((ChatListActivity)context).AddMatchItem(parser.returnCollection[0]);
						}

						if (context is ProfileViewActivity)
						{
							string matchItem = meta.Substring(sep1Pos + 1);
							ServerParser<MatchItem> parser = new ServerParser<MatchItem>(matchItem);
							((ProfileViewActivity)context).AddNewMatch(senderID, parser.returnCollection[0]);
						}
						else
						{
							AddUpdateMatch(senderID, true);
						}
						break;

					case "rematchProfile":
						sep1Pos = meta.IndexOf('|');
						sep2Pos = meta.IndexOf('|', sep1Pos + 1);

						senderID = int.Parse(meta.Substring(0, sep1Pos));
						matchID = int.Parse(meta.Substring(sep1Pos + 1, sep2Pos - sep1Pos - 1));
						bool active = bool.Parse(meta.Substring(sep2Pos + 1));

						if (inApp)
						{
							title = intent.GetStringExtra("title");
							((BaseActivity)context).c.SnackAction(title, Resource.String.ShowReceived, new Action<View>(delegate (View obj) { GoToChat(senderID); }));
						}

						if (context is ChatListActivity)
						{
							((ChatListActivity)context).UpdateMatchItem(matchID, active, null);
						}
						else if (context is ChatOneActivity)
						{
							((ChatOneActivity)context).UpdateStatus(senderID, active, null);
						}

						if (context is ProfileViewActivity)
						{
							((ProfileViewActivity)context).UpdateStatus(senderID, true, matchID);
						}
						else
						{
							AddUpdateMatch(senderID, true);
						}

						break;

					case "unmatchProfile":
						((BaseActivity)context).c.LogActivity("ChatReceiver meta " + meta);

						sep1Pos = meta.IndexOf('|');
						sep2Pos = meta.IndexOf('|', sep1Pos + 1);

						senderID = int.Parse(meta.Substring(0, sep1Pos));
						matchID = int.Parse(meta.Substring(sep1Pos + 1, sep2Pos - sep1Pos - 1));
						long unmatchDate = long.Parse(meta.Substring(sep2Pos + 1));

						if (((BaseActivity)context).IsUpdatingFrom(senderID))
						{
							((BaseActivity)context).RemoveUpdatesFrom(senderID);
						}

						if (inApp)
						{
							title = intent.GetStringExtra("title");
							((BaseActivity)context).c.SnackAction(title, Resource.String.ShowReceived, new Action<View>(delegate (View obj) { GoToChat(senderID); }));
						}

						if (context is ChatListActivity)
						{
							((ChatListActivity)context).UpdateMatchItem(matchID, false, unmatchDate);
						}
						else if (context is ChatOneActivity)
						{
							((ChatOneActivity)context).UpdateStatus(senderID, false, unmatchDate);
						}

						if (context is ProfileViewActivity)
						{
							((ProfileViewActivity)context).UpdateStatus(senderID, false, null);
						}
						else
						{
							AddUpdateMatch(senderID, false);
						}
						break;

					case "locationUpdate":
						sep1Pos = meta.IndexOf('|');
						sep2Pos = meta.IndexOf('|', sep1Pos + 1);
						sep3Pos = meta.IndexOf('|', sep2Pos + 1);

						senderID = int.Parse(meta.Substring(0, sep1Pos));
						senderName = meta.Substring(sep1Pos + 1, sep2Pos - sep1Pos - 1);
						int frequency = int.Parse(content.Substring(sep2Pos + 1, sep3Pos - sep2Pos - 1));

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

						int sep4Pos = content.IndexOf('|', sep3Pos + 1);
						int sep5Pos = content.IndexOf('|', sep4Pos + 1);

						long time = long.Parse(content.Substring(sep3Pos + 1, sep4Pos - sep3Pos - 1));
						double latitude = double.Parse(content.Substring(sep4Pos + 1, sep5Pos - sep4Pos - 1), CultureInfo.InvariantCulture);
						double longitude = double.Parse(content.Substring(sep5Pos + 1), CultureInfo.InvariantCulture);

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
						sep1Pos = content.IndexOf('|');
						senderID = int.Parse(content.Substring(0, sep1Pos));
						senderName = content.Substring(sep1Pos + 1);

						if (((BaseActivity)context).IsUpdatingFrom(senderID)) //user could have gone to the background, clearing out the list of people to receive updates from.
						{
							((BaseActivity)context).RemoveUpdatesFrom(senderID);

							text = senderName + " " + context.Resources.GetString(Resource.String.LocationUpdatesFromEnd);
							((BaseActivity)context).c.SnackStr(text, null);
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

		private void GoToProfile(int targetID)
		{
			Intent i = new Intent(context, typeof(ProfileViewActivity));
			i.SetFlags(ActivityFlags.ReorderToFront);
			IntentData.pageType = "standalone";
			IntentData.targetID = targetID;
			((BaseActivity)context).c.LogActivity("GoToProfile");
			context.StartActivity(i);
		}

		public void AddUpdateMatch(int senderID, bool isMatch)
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
				}
			}
		}
	}
}