using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.Constraints;
using Android.Views;
using Android.Widget;
using FFImageLoading;

namespace LocationConnection
{
	class ChatMessageWindowAdapter : BaseAdapter<string>
	{
		ChatOneActivity context;
		List<MessageItem> items;

		public ChatMessageWindowAdapter(ChatOneActivity context, List<MessageItem> items)
		{
			this.context = context;
			this.items = items;
		}

		public override long GetItemId(int position)
		{
			return position;
		}

		public override string this[int position] => items[position].Content;

		public override int Count => items.Count;

		public override View GetView(int position, View convertView, ViewGroup parent)
		{
			View view = convertView;
			if (view == null)
			{
				if (Settings.DisplaySize == 1)
				{
					view = context.LayoutInflater.Inflate(Resource.Layout.chat_messagewindow_normal, null);
				}
				else
				{
					view = context.LayoutInflater.Inflate(Resource.Layout.chat_messagewindow_small, null);
				}
			}

			MessageItem item = items[position];

			LinearLayout MainLayout = view.FindViewById<LinearLayout>(Resource.Id.MainLayout);
			LinearLayout MessageTextContainer = view.FindViewById<LinearLayout>(Resource.Id.MessageTextContainer);
			TextView MessageText = view.FindViewById<TextView>(Resource.Id.MessageText);
			View SpacerLeft = view.FindViewById<View>(Resource.Id.SpacerLeft);
			View SpacerRight = view.FindViewById<View>(Resource.Id.SpacerRight);

			int value = (int)Math.Round(10 * BaseActivity.pixelDensity);
			if (position == 0)
			{
				MainLayout.SetPadding(value, value, value, 0);
			}
			else if (position == items.Count - 1)
			{
				MainLayout.SetPadding(value, 0, value, value);
			}

			if (item.SenderID == Session.ID)
			{
				MessageTextContainer.SetHorizontalGravity(GravityFlags.Right);
				SpacerRight.Visibility = ViewStates.Gone;
				MessageText.SetBackgroundResource(context.messageOwn);
			}
			else
			{
				MessageTextContainer.SetHorizontalGravity(GravityFlags.Left);
				SpacerLeft.Visibility = ViewStates.Gone;
				MessageText.SetBackgroundResource(context.messageTarget);
			}
			MessageText.Text = item.Content;

			SetMessageTime(view, item.SentTime, item.SeenTime, item.ReadTime);

			return view;
		}

		public void SetMessageTime(View view, long sentTime, long seenTime, long readTime)
		{
			TextView TimeText = view.FindViewById<TextView>(Resource.Id.TimeText);

			DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

			DateTime sentDate = dateTime.AddSeconds(sentTime).ToLocalTime();
			if (sentDate.Date == DateTime.Today)
			{
				TimeText.Text = context.res.GetString(Resource.String.MessageStatusSent) + " " + sentDate.ToString("HH:mm");
			}
			else
			{
				if (sentDate.Year == DateTime.Now.Year)
				{
					TimeText.Text = context.res.GetString(Resource.String.MessageStatusSent) + " " + sentDate.ToString("dd MMM HH:mm");
				}
				else
				{
					TimeText.Text = context.res.GetString(Resource.String.MessageStatusSent) + " " + sentDate.ToString("dd MMM yyyy HH:mm");
				}

			}

			if (readTime != 0)
			{
				DateTime readDate = dateTime.AddSeconds(readTime).ToLocalTime();
				if (readTime < sentTime + 60)
				{
					TimeText.Text += " - " + context.res.GetString(Resource.String.MessageStatusRead);
				}
				else if (readDate.Date == sentDate.Date)
				{
					TimeText.Text += " - " + context.res.GetString(Resource.String.MessageStatusRead) + " " + readDate.ToString("HH:mm");
				}
				else
				{
					if (readDate.Year == sentDate.Year)
					{
						TimeText.Text += " - " + context.res.GetString(Resource.String.MessageStatusRead) + " " + readDate.ToString("dd MMM HH:mm");
					}
					else
					{
						TimeText.Text += " - " + context.res.GetString(Resource.String.MessageStatusRead) + " " + readDate.ToString("dd MMM yyyy HH:mm");
					}

				}
			}
			else if (seenTime != 0)
			{
				DateTime seenDate = dateTime.AddSeconds(seenTime).ToLocalTime();
				if (seenTime < sentTime + 60)
				{
					TimeText.Text += " - " + context.res.GetString(Resource.String.MessageStatusSeen);
				}
				else if (seenDate.Date == sentDate.Date)
				{
					TimeText.Text += " - " + context.res.GetString(Resource.String.MessageStatusSeen) + " " + seenDate.ToString("HH:mm");
				}
				else
				{
					if (seenDate.Year == sentDate.Year)
					{
						TimeText.Text += " - " + context.res.GetString(Resource.String.MessageStatusSeen) + " " + seenDate.ToString("dd MMM HH:mm");
					}
					else
					{
						TimeText.Text += " - " + context.res.GetString(Resource.String.MessageStatusSeen) + " " + seenDate.ToString("dd MMM yyyy HH:mm");
					}
				}
			}
		}
	}
}