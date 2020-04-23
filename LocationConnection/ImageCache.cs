using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
    public class ImageCache
    {
        BaseActivity context;
        public static volatile List<string> imagesInProgress = new List<string>(); //using list is unreliable, when I add  103 to 1|4|7|104|6, it becames 1|4|7|104|6|6. Sometimes an empty stirng is present. Adding volatile did not help. 
        public static volatile Dictionary<ImageView, string> imageViewToLoadLater = new Dictionary<ImageView, string>();

        public ImageCache(BaseActivity context)
        {
            this.context = context;
        }

        public async Task<Bitmap> LoadBitmap(string userID, string picture) { //used for the map only

            string saveName = userID + "_" + Constants.SmallImageSize.ToString() + "_" + picture;

            if (Exists(saveName)) //images when loaded from the cache does not always appear 
            {
                await Task.Delay(100); //without delay, not all pictures appear. A minimum of 70 ms required, 60 ms can fail.
                return Load(saveName);

            }
            else
            {
                string url;
                if (Constants.isTestDB)
                {
                    url = Constants.HostName + Constants.UploadFolderTest + "/" + userID + "/" + Constants.SmallImageSize.ToString() + "/" + picture;
                }
                else
                {
                    url = Constants.HostName + Constants.UploadFolder + "/" + userID + "/" + Constants.SmallImageSize.ToString() + "/" + picture;
                }

                byte[] bytes = null;

                var task = CommonMethods.GetImageDataFromUrlAsync(url);
                CancellationTokenSource cts = new CancellationTokenSource();

                if (await Task.WhenAny(task, Task.Delay(Constants.RequestTimeout, cts.Token)) == task)
                {
                    cts.Cancel();
                    bytes = await task;
                }

                if (bytes != null)
                {
                    Save(saveName, bytes);
                    return BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length);
                }
                else
                {
                    return BitmapFactory.DecodeResource(context.Resources, Resource.Drawable.noimage);
                }
            }
        }

        public async Task LoadImage(ImageView imageView, string userID, string picture, bool isLarge = false, bool temp = false)
        {
            try
            {
                string subFolder;

                if (isLarge)
                {
                    subFolder = Constants.LargeImageSize.ToString();
                }
                else
                {
                    subFolder = Constants.SmallImageSize.ToString();
                }

                string saveName = userID + "_" + subFolder + "_" + picture;
                string imgID = imageView.ToString().Split("{")[1].Substring(0, 7);

                if (Exists(saveName))
                {
                    if (context is ProfileViewActivity && (((ProfileViewActivity)context).currentID.ToString() != userID || ((ProfileViewActivity)context).cancelImageLoading))
                    {
                        return;
                    }

                    //context.c.CW("Exists " + userID + " at " + imgID);
                    //context.c.LogActivity("Exists " + userID + " at " + imgID);

                    if (imageViewToLoadLater.ContainsKey(imageView))
                    {
                        imageViewToLoadLater.Remove(imageView);
                    }

                    context.RunOnUiThread(() => {
                        imageView.SetImageBitmap(Load(saveName)); //takes 3-4 ms
                        imageView.Alpha = 0;
                        imageView.Animate().Alpha(1).SetDuration(context.tweenTime).Start();
                    });
                }
                else
                {
                    if (context is ProfileViewActivity && (((ProfileViewActivity)context).currentID.ToString() != userID || ((ProfileViewActivity)context).cancelImageLoading))
                    {
                        return;
                    }

                    context.RunOnUiThread(() => {
                        imageView.SetImageResource(Resource.Drawable.loadingimage);
                    });

                    string url;
                    if (!temp)
                    {
                        if (Constants.isTestDB)
                        {
                            url = Constants.HostName + Constants.UploadFolderTest + "/" + userID + "/" + subFolder + "/" + picture;
                        }
                        else
                        {
                            url = Constants.HostName + Constants.UploadFolder + "/" + userID + "/" + subFolder + "/" + picture;
                        }
                    }
                    else
                    {
                        if (Constants.isTestDB)
                        {
                            url = Constants.HostName + Constants.TempUploadFolderTest + "/" + userID + "/" + subFolder + "/" + picture;
                        }
                        else
                        {
                            url = Constants.HostName + Constants.TempUploadFolder + "/" + userID + "/" + subFolder + "/" + picture;
                        }
                    }

                    if (imagesInProgress.IndexOf(saveName) != -1)
                    {
                        //context.c.CW("Cancelled loading " + userID + " at " + imgID); 
                        //context.c.LogActivity("Cancelled loading " + userID + " at " + imgID);

                        //For a chatlist with 3 items, the 4 imageViews are used. The first is called 13 times (with all 3 IDs), the second called once, the third once, and the fourth 24 times (with all 3 IDs).
                        imageViewToLoadLater[imageView] = saveName;
                        return;
                    }

                    //context.c.CW("Requesting " + userID + " at " + imgID); //+ " arr " + string.Join('|', imagesInProgress) if used at Completed, "Collection was modified; enumeration operation may not execute" error may occur.
                    //context.c.LogActivity("Requesting " + userID + " at " + imgID);

                    imagesInProgress.Add(saveName);

                    byte[] bytes = null;

                    var task = CommonMethods.GetImageDataFromUrlAsync(url);
                    CancellationTokenSource cts = new CancellationTokenSource();

                    if (await Task.WhenAny(task, Task.Delay(Constants.RequestTimeout, cts.Token)) == task)
                    {
                        cts.Cancel();
                        bytes = await task;
                    }

                    if (imagesInProgress.IndexOf(saveName) != -1)
                    {
                        imagesInProgress.Remove(saveName);
                    }

                    //context.c.CW("Completed " + userID + " at " + imgID);
                    //context.c.LogActivity("Completed " + userID + " at " + imgID);

                    if (bytes != null)
                    {
                        Save(saveName, bytes);
                        Bitmap bmp = BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length);

                        if (context is ProfileViewActivity && (((ProfileViewActivity)context).currentID.ToString() != userID || ((ProfileViewActivity)context).cancelImageLoading))
                        {
                            return;
                        }
                        context.RunOnUiThread(() =>
                        {
                            bool found = false;

                            //string str = "Bitmap loaded: " + userID + " original " + imageView.ToString().Split("{")[1].Substring(0,7);
                            foreach (KeyValuePair<ImageView, string> pair in imageViewToLoadLater)
                            {
                                //there might be more than one ImageView that set this ID
                                if (pair.Value == saveName)
                                {
                                    found = true;
                                    pair.Key.SetImageBitmap(bmp);
                                    //str += " newer " + pair.Key.ToString().Split("{")[1].Substring(0, 7);
                                }
                            }

                            if (!found)
                            {
                                imageView.SetImageBitmap(bmp);
                            }

                            //context.c.CW(str);
                            //context.c.LogActivity(str);

                        });
                    }
                    else
                    {
                        if (context is ProfileViewActivity && (((ProfileViewActivity)context).currentID.ToString() != userID || ((ProfileViewActivity)context).cancelImageLoading))
                        {
                            return;
                        }
                        context.RunOnUiThread(() =>
                        {
                            if (isLarge)
                            {
                                imageView.SetImageResource(Resource.Drawable.noimage_hd);
                            }
                            else
                            {
                                imageView.SetImageResource(Resource.Drawable.noimage);
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                context.c.ReportErrorSilent(ex.Message + System.Environment.NewLine + ex.StackTrace);
            }
        }

        private void Save(string imageName, byte[] data)
        {
            
            string fileName = System.IO.Path.Combine(CommonMethods.cacheFolder, imageName);
            try
            {
                using FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate);
                fs.Write(data, 0, data.Length);
                //context.c.CW("Cache saving " + fileName);
                //context.c.LogActivity("Cache saving " + fileName);
            }
            catch (Exception ex)
            {
                try
                {
                    context.c.LogActivity(" Error saving image: " + ex.Message);
                    context.c.CW("Error saving image: " + ex.Message);
                }
                catch
                {
                }
            }
        }

        private Bitmap Load(string imageName)
        {
            string fileName = System.IO.Path.Combine(CommonMethods.cacheFolder, imageName);
            //context.c.CW("Cache loading " + fileName);
            //context.c.LogActivity("Cache loading " + fileName);
            return BitmapFactory.DecodeFile(fileName);
        }

        public bool Exists(string imageName)
        {
            string fileName = System.IO.Path.Combine(CommonMethods.cacheFolder, imageName);
            //context.c.CW("Cache exists? " + fileName + " " + File.Exists(fileName));
            //context.c.LogActivity("Cache exists? " + fileName + " " + File.Exists(fileName));
            return File.Exists(fileName);
        }
    }
}