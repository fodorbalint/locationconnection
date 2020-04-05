using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public class ImageCache
    {
        BaseActivity context;
        static List<string> imagesInProgress = new List<string>();

        public ImageCache(BaseActivity context)
        {
            this.context = context;
        }

        public Bitmap LoadBitmap(string userID, string picture) { //used for the map only

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

                byte[] bytes = CommonMethods.GetImageDataFromUrl(url);
                if (bytes != null)
                {
                    Save(saveName, bytes);
                    return BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length);
                }
                else
                {
                    return null;
                }
            }
        }

        public void LoadImage(ImageView imageView, string userID, string picture, bool isLarge = false, bool temp = false)
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

            context.c.CW("Cache imagesInprogress count " + imagesInProgress.Count);

            if (Exists(saveName))
            {
                if (context is ProfileViewActivity && (((ProfileViewActivity)context).currentID.ToString() != userID || ((ProfileViewActivity)context).cancelImageLoading))
                {
                    return;
                }
                context.RunOnUiThread(() => {
                    if (imageView is ImageView)
                    {
                        ((ImageView)imageView).SetImageBitmap(Load(saveName));
                    }
                    /*else if (imageView is Button)
                    {
                        ((Button)imageView).SetBackgroundImage(Load(saveName), UIControlState.Normal);
                    }*/
                    /*else if (imageView is MKAnnotationView)
                    {
                        ((MKAnnotationView)imageView).Image = Load(saveName);
                    }*/
                });
            }
            else
            {
                if (context is ProfileViewActivity && (((ProfileViewActivity)context).currentID.ToString() != userID || ((ProfileViewActivity)context).cancelImageLoading))
                {
                    return;
                }

                context.RunOnUiThread(() => {
                    if (imageView is ImageView)
                    {
                        ((ImageView)imageView).SetImageResource(Resource.Drawable.loadingimage);
                    }
                    /*else if (imageView is UIButton)
                    {
                        ((UIButton)imageView).SetBackgroundImage(UIImage.FromBundle(Constants.loadingImage), UIControlState.Normal);
                    }
                    else if (imageView is MKAnnotationView)
                    {
                        ((MKAnnotationView)imageView).Image = UIImage.FromBundle(Constants.loadingImage);
                    }*/
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
                    context.c.CW("Cache cancelled loading " + saveName);
                    return;
                }
                imagesInProgress.Add(saveName);

                byte[] bytes = CommonMethods.GetImageDataFromUrl(url);

                imagesInProgress.Remove(saveName);

                if (bytes != null)
                {
                    Save(saveName, bytes);
                    Bitmap bmp = BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length);

                    if (context is ProfileViewActivity && (((ProfileViewActivity)context).currentID.ToString() != userID || ((ProfileViewActivity)context).cancelImageLoading))
                    {
                        context.c.CW("Cache not loading new image");
                        return;
                    }
                    context.RunOnUiThread(() =>
                    {
                        if (imageView is ImageView)
                        {
                            ((ImageView)imageView).SetImageBitmap(bmp);
                        }
                        /*
                        else if (imageView is UIButton)
                        {
                            ((UIButton)imageView).SetBackgroundImage(UIImage.LoadFromData(task.Result), UIControlState.Normal);
                        }
                        else if (imageView is MKAnnotationView)
                        {
                            ((MKAnnotationView)imageView).Image = UIImage.LoadFromData(task.Result);
                        }
                        */
                    });
                }
                else
                {
                    if (context is ProfileViewActivity && (((ProfileViewActivity)context).currentID.ToString() != userID || ((ProfileViewActivity)context).cancelImageLoading))
                    {
                        context.c.CW("Cache not loading noimage");
                        return;
                    }
                    context.RunOnUiThread(() =>
                    {
                        if (imageView is ImageView)
                        {
                            if (isLarge)
                            {
                                ((ImageView)imageView).SetImageResource(Resource.Drawable.noimage_hd);
                            }
                            else
                            {
                                ((ImageView)imageView).SetImageResource(Resource.Drawable.noimage);
                            }
                        }
                        /*
                        else if (imageView is UIButton)
                        {
                            ((UIButton)imageView).SetBackgroundImage(UIImage.FromBundle(Constants.noImage), UIControlState.Normal);
                        }
                        else if (imageView is MKAnnotationView)
                        {
                            ((MKAnnotationView)imageView).Image = UIImage.FromBundle(Constants.noImage);
                        }
                        */
                    });
                }
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
            return BitmapFactory.DecodeFile(fileName);
        }

        public bool Exists(string imageName)
        {
            string fileName = System.IO.Path.Combine(CommonMethods.cacheFolder, imageName);
            //context.c.CW("----- cache Exists? " + fileName + " " + File.Exists(fileName));
            return File.Exists(fileName);
        }
    }
}