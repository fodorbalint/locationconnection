using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Firebase.Messaging;

namespace LocationConnection
{
	[Service]
	[IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
	public class MyFirebaseMessagingService : FirebaseMessagingService
	{
		private string firebaseTokenFile = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "firebasetoken.txt");
		private string tokenUptoDateFile = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "tokenuptodate.txt");

		public override void OnMessageReceived(RemoteMessage message)
		{
			Intent intent = new Intent("balintfodor.locationconnection.ChatReceiver");
			intent.PutExtra("fromuser", message.Data["fromuser"]);
			intent.PutExtra("touser", message.Data["touser"]);
			intent.PutExtra("type", message.Data["type"]);
			intent.PutExtra("meta", message.Data["meta"]);
			intent.PutExtra("inapp", (int.Parse(message.Data["inapp"]) == 0) ? false : true);

			if (!(message.GetNotification() is null)) {
				intent.PutExtra("title", message.GetNotification().Title);
				intent.PutExtra("body", message.GetNotification().Body);
			}
			else if (message.Data.ContainsKey("title"))
			{
				intent.PutExtra("title", message.Data["title"]);
				intent.PutExtra("body", message.Data["body"]);
			}

			SendBroadcast(intent);
		}

		public override async void OnNewToken(string p0)
		{
			base.OnNewToken(p0);

			File.WriteAllText(firebaseTokenFile, p0);
			File.WriteAllText(tokenUptoDateFile, "False");

			CommonMethods c = new CommonMethods(null);
			if (c.IsLoggedIn()) //might never be true
			{
				string responseString = await c.MakeRequest("action=updatetoken&ID=" + Session.ID + "&SessionID=" + Session.SessionID + "&token=" + p0);
				if (responseString == "OK")
				{
					File.WriteAllText(tokenUptoDateFile, "True");
				}
				else
				{
					c.LogError("Error sending token: ID=" + Session.ID + "&SessionID=" + Session.SessionID + "&token=" + p0);
				}
			}
		}
	}
}