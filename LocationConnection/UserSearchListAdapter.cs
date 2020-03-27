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
using Android.Views;
using Android.Widget;
using FFImageLoading;
using Java.Lang;

namespace LocationConnection
{
    class UserSearchListAdapter : BaseAdapter<string>
    {
        List<Profile> profiles;
        ListActivity context;

        public UserSearchListAdapter(ListActivity context, List<Profile> profiles)
        {
            this.context = context;
            this.profiles = profiles;
        }
        public override long GetItemId(int position)
        {
            return position;
        }
        public override string this[int position] => profiles[position].Username;
        public override int Count => profiles.Count;

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView;
            if (view == null) view = context.LayoutInflater.Inflate(Resource.Layout.list_item, null);
            ImageView ListImage = view.FindViewById<ImageView>(Resource.Id.ListImage);

			//TextView ListUsername = view.FindViewById<TextView>(Resource.Id.ListUsername);
			//TextView ListName = view.FindViewById<TextView>(Resource.Id.ListName);
			
			string url;
            if (Constants.isTestDB)
            {
                url = Constants.HostName + Constants.UploadFolderTest + "/" + profiles[position].ID + "/" + Constants.SmallImageSize + "/" + profiles[position].Pictures[0];
            }
            else
            {
                url = Constants.HostName + Constants.UploadFolder + "/" + profiles[position].ID + "/" + Constants.SmallImageSize + "/" + profiles[position].Pictures[0];
            }
			ImageService im = new ImageService();
			im.LoadUrl(url).LoadingPlaceholder(Constants.loadingImage, FFImageLoading.Work.ImageSource.CompiledResource).ErrorPlaceholder(Constants.noImage, FFImageLoading.Work.ImageSource.CompiledResource).Into(ListImage);
			
			/*var imageBitmap = new CommonMethods(null, null).GetImageBitmapFromUrl(url);
			try
            {
                ListImage.SetImageBitmap(imageBitmap);
            }
            catch { }*/

            //ListUsername.Text = profiles[position].Username;
            //ListName.Text = profiles[position].Name;
            return view;
        }      
    }
}