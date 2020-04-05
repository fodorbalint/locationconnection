using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Content;
using Android.Views;
using Android.Widget;
using Java.Lang;

namespace LocationConnection
{
	class LocationListAdapter : BaseAdapter<string>
	{
		List<LocationItem> items;
		LocationActivity context;

		public LocationListAdapter(LocationActivity context, List<LocationItem> items)
		{
			this.context = context;
			this.items = items;
		}
		public override long GetItemId(int position)
		{
			return position;
		}
		public override string this[int position] => items[position].time.ToString();
		public override int Count => items.Count;

		public override View GetView(int position, View convertView, ViewGroup parent)
		{
			View view = convertView;
			if (Settings.DisplaySize == 1)
			{
				if (view == null) view = context.LayoutInflater.Inflate(Resource.Layout.list_item_text_normal, null);
			}
			else
			{
				if (view == null) view = context.LayoutInflater.Inflate(Resource.Layout.list_item_text_small, null);
			}

			LocationItem item = items[position];
			
			if (item.isSelected)
			{
				if (item.inApp)
				{
					view.SetBackgroundColor(new Color(ContextCompat.GetColor(context, Resource.Color.ForegroundLocationDark)));
				}
				else
				{
					view.SetBackgroundColor(new Color(ContextCompat.GetColor(context, Resource.Color.BackgroundLocationDark)));
				}
			}
			else
			{
				if (item.inApp)
				{
					view.SetBackgroundColor(new Color(ContextCompat.GetColor(context, Resource.Color.ForegroundLocation)));
				}
				else
				{
					view.SetBackgroundColor(new Color(ContextCompat.GetColor(context, Resource.Color.BackgroundLocation)));
				}
			}

			DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(item.time).ToLocalTime();

			((TextView)view).Text = dt.ToString("HH:mm:ss");
			
			return view;
		}
	}
}