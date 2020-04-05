using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace LocationConnection
{
    class UserSearchListAdapter : BaseAdapter<string>
    {
        List<Profile> profiles;
        ListActivity context;
        static int loadCount;

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
            loadCount++;

            View view = convertView;
            if (view == null) view = context.LayoutInflater.Inflate(Resource.Layout.list_item, null);

            context.c.CW("GetView " + loadCount + " ---- " + profiles[position].ID);

            ImageView ListImage = view.FindViewById<ImageView>(Resource.Id.ListImage);
            //When switching from map to list, if during map view, I opened or closed the filters, we will sometimes see an upward / downward animation.
            ImageCache im = new ImageCache(context);
            Task.Run(() => { //takes a long time to load
                im.LoadImage(ListImage, profiles[position].ID.ToString(), profiles[position].Pictures[0]);
            });

            //Requires Xamarin.FFImageLoading

            /*ImageView ListImage = view.FindViewById<ImageView>(Resource.Id.ListImage);
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
			im.LoadUrl(url).LoadingPlaceholder(Constants.loadingImage, FFImageLoading.Work.ImageSource.CompiledResource).ErrorPlaceholder(Constants.noImage, FFImageLoading.Work.ImageSource.CompiledResource).Into(ListImage);*/

            return view;
        }      
    }
}