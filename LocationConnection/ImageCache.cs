using System;
using System.Collections.Generic;
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

        public ImageCache(BaseActivity context)
        {
            this.context = context;
        }

        public async Task<Bitmap> LoadBitmap(string userID, string picture) { //used for the map only

            string saveName = userID + "_" + Constants.SmallImageSize.ToString() + "_" + picture;

            if (Exists(saveName))
            {
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


                if (Exists(saveName))
                {
                    if (context is ProfileViewActivity && (((ProfileViewActivity)context).currentID.ToString() != userID || ((ProfileViewActivity)context).cancelImageLoading))
                    {
                        return;
                    }

                    context.RunOnUiThread(() => {
                        imageView.SetImageBitmap(Load(saveName));
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
                        //context.c.CW("Cancelled loading ID " + userID + " arr " + string.Join('|', imagesInProgress)); 
                        //context.c.LogActivity("Cancelled loading ID " + userID + " arr " + string.Join('|', imagesInProgress));

                        return;
                    }

                    //context.c.CW("Requesting ID " + userID + " arr " + string.Join('|', imagesInProgress));
                    //context.c.LogActivity("Requesting ID " + userID + " arr " + string.Join('|', imagesInProgress));

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

                    //context.c.CW("Completed " + userID + " arr " + string.Join('|', imagesInProgress));
                    //context.c.LogActivity("Completed " + userID + " arr " + string.Join('|', imagesInProgress));

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
                            imageView.SetImageBitmap(bmp);
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
                context.c.CW("Cache saving " + fileName);
                context.c.LogActivity("Cache saving " + fileName);
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
            context.c.CW("Cache loading " + fileName);
            context.c.LogActivity("Cache loading " + fileName);
            return BitmapFactory.DecodeFile(fileName);
        }

        public bool Exists(string imageName)
        {
            string fileName = System.IO.Path.Combine(CommonMethods.cacheFolder, imageName);
            context.c.CW("Cache exists? " + fileName + " " + File.Exists(fileName));
            context.c.LogActivity("Cache exists? " + fileName + " " + File.Exists(fileName));
            return File.Exists(fileName);
        }
    }
}