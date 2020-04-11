using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.Constraints;
using Android.Views;
using Android.Widget;

namespace LocationConnection
{
	class ChatUserListAdapter : BaseAdapter<string>
	{
		ChatListActivity context;
		List<MatchItem> items;

		public ChatUserListAdapter(ChatListActivity context, List<MatchItem> items)
		{
			this.context = context;
			this.items = items;
		}

		public override long GetItemId(int position)
		{
			return position;
		}

		public override string this[int position] => items[position].TargetName;

		public override int Count => items.Count;

		public override View GetView(int position, View convertView, ViewGroup parent)
		{
			View view = convertView;
			if (view == null)
			{
				if (Settings.DisplaySize == 1)
				{
					view = context.LayoutInflater.Inflate(Resource.Layout.chat_userlist_normal, null);
				}
				else
				{
					view = context.LayoutInflater.Inflate(Resource.Layout.chat_userlist_small, null);
				}
			}

			ConstraintLayout ItemMainLayout = view.FindViewById<ConstraintLayout>(Resource.Id.ItemMainLayout);
			ImageView Image = view.FindViewById<ImageView>(Resource.Id.Image);
			TextView Name = view.FindViewById<TextView>(Resource.Id.Name);
			LinearLayout ChatItems = view.FindViewById<LinearLayout>(Resource.Id.ChatItems);

			if ((bool)items[position].Active)
			{
				ItemMainLayout.SetBackgroundResource(Resource.Drawable.backgroundSelectorActive);
			}
			else
			{
				ItemMainLayout.SetBackgroundResource(Resource.Drawable.backgroundSelectorPassive);
			}

			Name.Text = items[position].TargetName;

			for (int i=0; i < ChatItems.ChildCount; i++) //when inserting a match item to first place, chat lines of the next item will be shown if we don't clear it out.
			{
				((LinearLayout)ChatItems.GetChildAt(i)).RemoveAllViews();
			}
			
			int j = 0;
			for (int i = items[position].Chat.Length-1; i >= 0; i--)
			{
				string messageItem = items[position].Chat[i];
				int sep1Pos = messageItem.IndexOf('|');
				int sep2Pos = messageItem.IndexOf('|', sep1Pos + 1);
				int sep3Pos = messageItem.IndexOf('|', sep2Pos + 1);
				int sep4Pos = messageItem.IndexOf('|', sep3Pos + 1);
				int sep5Pos = messageItem.IndexOf('|', sep4Pos + 1);
				int senderID = int.Parse(messageItem.Substring(sep1Pos + 1, sep2Pos - sep1Pos - 1));
				long readTime = long.Parse(messageItem.Substring(sep4Pos + 1, sep5Pos - sep4Pos - 1));
				string message = messageItem.Substring(sep5Pos + 1);				

				TextView t = new TextView(context);
				t.SetSingleLine();
				t.Ellipsize = Android.Text.TextUtils.TruncateAt.End;
				t.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
				t.Text = message.Replace(System.Environment.NewLine, " ");
				LinearLayout lin = (LinearLayout)ChatItems.GetChildAt(j);
				j++;
				t.SetTextColor(Color.Black);

				if (Settings.DisplaySize == 1)
				{
					t.SetTextAppearance(Resource.Style.TextSmallNormal);
				}
				else
				{
					t.SetTextAppearance(Resource.Style.TextSmallSmall);
				}

				if (senderID != Session.ID)
				{
					t.SetTypeface(null, TypefaceStyle.Bold);
					//t.Typeface = Typeface.Create("<FONT FAMILY NAME>", Android.Graphics.TypefaceStyle.Bold);
					if (readTime == 0)
					{
						lin.SetBackgroundColor(Color.ParseColor("#809dd7fb"));
					}
				}
				
				lin.AddView(t);
			}

			ImageCache im = new ImageCache(context);
			Task.Run(async () => {
				await im.LoadImage(Image, items[position].TargetID.ToString(), items[position].TargetPicture, false);
			});

			//Requires Xamarin.FFImageLoading. Nit perfect, sometimes image is not found.

			/*string url;
			if (Constants.isTestDB)
			{
				url = Constants.HostName + Constants.UploadFolderTest + "/" + items[position].TargetID + "/" + Constants.SmallImageSize + "/" + items[position].TargetPicture;
			}
			else
			{
				url = Constants.HostName + Constants.UploadFolder + "/" + items[position].TargetID + "/" + Constants.SmallImageSize + "/" + items[position].TargetPicture;
			}
			
			ImageService im = new ImageService();
			im.LoadUrl(url).LoadingPlaceholder(Constants.loadingImage, FFImageLoading.Work.ImageSource.CompiledResource).ErrorPlaceholder(Constants.noImage, FFImageLoading.Work.ImageSource.CompiledResource).Into(Image);
			*/
			return view;
		}
	}
}