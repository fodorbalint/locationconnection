//section: explanation to icons
//on mobile textbox disappears on typing, it gets so small.
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
using Android.Views.Animations;
using Android.Views.InputMethods;
using Android.Widget;

namespace LocationConnection
{
	[Activity(MainLauncher = false, ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
	public class HelpCenterActivity : BaseActivity, TouchActivity
	{
		ImageButton HelpCenterBack;
		Button OpenTutorial;
		ScrollView QuestionsScroll;
		LinearLayout QuestionsContainer;
		TextView HelpCenterFormCaption;
		Button MessageSend;
		EditText MessageEdit;

		ConstraintLayout TutorialTopBar, TutorialNavBar;
		ImageButton TutorialBack, LoadPrevious, LoadNext;
		TextView TutorialText, TutorialNavText;
		View TutorialFrameBg;
		TouchConstraintLayout TutorialFrame;
		View TutorialTopSeparator, TutorialBottomSeparator;
		ImageView LoaderCircle;

		private List<string> tutorialDescriptions;
		private List<string> tutorialPictures;
		private int currentTutorial;
		public bool cancelImageLoading;

		InputMethodManager imm;
		Android.Content.Res.Resources res;
		private int blackTextSmall;

		protected override void OnCreate(Bundle savedInstanceState)
		{
			try { 
				base.OnCreate(savedInstanceState);
				if (!ListActivity.initialized) { return; }

				if (Settings.DisplaySize == 1)
				{
					SetContentView(Resource.Layout.activity_helpcenter_normal);
					blackTextSmall = Resource.Style.BlackTextSmallNormal;
				}
				else
				{
					SetContentView(Resource.Layout.activity_helpcenter_small);
					blackTextSmall = Resource.Style.BlackTextSmallSmall;
				}

				MainLayout = FindViewById<ConstraintLayout>(Resource.Id.MainLayout);
				HelpCenterBack = FindViewById<ImageButton>(Resource.Id.HelpCenterBack);
				OpenTutorial = FindViewById<Button>(Resource.Id.OpenTutorial);
				QuestionsScroll = FindViewById<ScrollView>(Resource.Id.QuestionsScroll);
				QuestionsContainer = FindViewById<LinearLayout>(Resource.Id.QuestionsContainer);
				HelpCenterFormCaption = FindViewById<TextView>(Resource.Id.HelpCenterFormCaption);
				MessageEdit = FindViewById<EditText>(Resource.Id.MessageEdit);
				MessageSend = FindViewById<Button>(Resource.Id.MessageSend);

				TutorialTopBar = FindViewById<ConstraintLayout>(Resource.Id.TutorialTopBar);
				TutorialNavBar = FindViewById<ConstraintLayout>(Resource.Id.TutorialNavBar);
				TutorialBack = FindViewById<ImageButton>(Resource.Id.TutorialBack);
				LoadPrevious = FindViewById<ImageButton>(Resource.Id.LoadPrevious);
				LoadNext = FindViewById<ImageButton>(Resource.Id.LoadNext);
				TutorialText = FindViewById<TextView>(Resource.Id.TutorialText);
				TutorialNavText = FindViewById<TextView>(Resource.Id.TutorialNavText);
				TutorialFrameBg = FindViewById<View>(Resource.Id.TutorialFrameBg);
				TutorialFrame = FindViewById<TouchConstraintLayout>(Resource.Id.TutorialFrame);
				TutorialTopSeparator = FindViewById<View>(Resource.Id.TutorialTopSeparator);
				TutorialBottomSeparator = FindViewById<View>(Resource.Id.TutorialBottomSeparator);
				LoaderCircle = FindViewById<ImageView>(Resource.Id.LoaderCircle);

				MessageEdit.Visibility = ViewStates.Gone;
				MessageSend.Visibility = ViewStates.Gone;

				HelpCenterBack.Click += HelpCenterBack_Click;
				OpenTutorial.Click += OpenTutorial_Click;
				HelpCenterFormCaption.Click += HelpCenterFormCaption_Click;
				MessageSend.Click += MessageSend_Click;

				TutorialBack.Click += TutorialBack_Click;
				LoadPrevious.Click += LoadPrevious_Click;
				LoadNext.Click += LoadNext_Click;

				imm = (InputMethodManager)GetSystemService(Context.InputMethodService);
				Window.SetSoftInputMode(SoftInput.AdjustResize);
				c.view = MainLayout;
				res = Resources;

				firstRun = false;
			}
			catch (Exception ex)
			{
				c.ReportErrorSilent(ex.Message + System.Environment.NewLine + ex.StackTrace);
			}
		}

		protected override async void OnResume()
		{
			try { 
				base.OnResume();
				if (!ListActivity.initialized) { return; }

				string responseString = await c.MakeRequest("action=helpcenter");
				if (responseString.Substring(0, 2) == "OK")
				{
					QuestionsContainer.RemoveAllViews();
					responseString = responseString.Substring(3);
					string[] lines = responseString.Split("\t");
					int count = 0;
					foreach (string line in lines)
					{
						count++;
						TextView text = new TextView(this);
						text.Text = line;
						text.SetTextAppearance(blackTextSmall);
						text.SetPadding(10, 10, 10, 10);
						if (count % 2 == 1) //question, change font weight
						{
							var typeface = Typeface.Create("<FONT FAMILY NAME>", Android.Graphics.TypefaceStyle.Bold);
							text.Typeface = typeface;
						}
						QuestionsContainer.AddView(text);
					}
				}
				else
				{
					c.ReportError(responseString);
				}
			}
			catch (Exception ex)
			{
				c.ReportErrorSilent(ex.Message + System.Environment.NewLine + ex.StackTrace);
			}
		}

		private void HelpCenterBack_Click(object sender, EventArgs e)
		{
			OnBackPressed();
		}

		private void HelpCenterFormCaption_Click(object sender, EventArgs e)
		{
			if (MessageEdit.Visibility == ViewStates.Gone)
			{
				MessageEdit.Visibility = ViewStates.Visible;
				MessageSend.Visibility = ViewStates.Visible;
				QuestionsScroll.Visibility = ViewStates.Gone;
				MessageEdit.RequestFocus();
			}
			else
			{
				MessageEdit.Visibility = ViewStates.Gone;
				MessageSend.Visibility = ViewStates.Gone;
				QuestionsScroll.Visibility = ViewStates.Visible;
				imm.HideSoftInputFromWindow(MessageEdit.WindowToken, 0);
				MainLayout.RequestFocus();
			}
		}

		private async void MessageSend_Click(object sender, EventArgs e)
		{
			imm.HideSoftInputFromWindow(MessageEdit.WindowToken, 0);
			if (MessageEdit.Text != "")
			{
				MessageSend.Enabled = false;

				string url = "action=helpcentermessage&ID=" + Session.ID + "&SessionID=" + Session.SessionID + "&Content=" + c.UrlEncode(MessageEdit.Text);
				string responseString = await c.MakeRequest(url);
				if (responseString == "OK")
				{
					MessageEdit.Text = "";
					MessageEdit.Visibility = ViewStates.Gone;
					MessageSend.Visibility = ViewStates.Gone;
					QuestionsScroll.Visibility = ViewStates.Visible;
					MainLayout.RequestFocus();
					c.Snack(Resource.String.HelpCenterSent);
				}
				else
				{
					c.ReportError(responseString);
				}
				MessageSend.Enabled = true;
			}
		}

		private async void OpenTutorial_Click(object sender, EventArgs e)
		{
			MessageEdit.Visibility = ViewStates.Gone;
			MessageSend.Visibility = ViewStates.Gone;
			QuestionsScroll.Visibility = ViewStates.Visible;
			imm.HideSoftInputFromWindow(MessageEdit.WindowToken, 0);
			MainLayout.RequestFocus();
			
			TutorialFrame.RemoveAllViews();
			TutorialText.Text = "";
            TutorialNavText.Text = "";
			
			TutorialTopBar.Visibility = ViewStates.Visible;
			TutorialFrameBg.Visibility = ViewStates.Visible;
			TutorialFrame.Visibility = ViewStates.Visible;
			TutorialTopSeparator.Visibility = ViewStates.Visible;
			TutorialBottomSeparator.Visibility = ViewStates.Visible;
			TutorialNavBar.Visibility = ViewStates.Visible;
			OpenTutorial.Visibility = ViewStates.Gone;
			StartAnim();

			cancelImageLoading = false;

			string url = "action=tutorial&OS=Android&dpWidth=" + dpWidth;
			string responseString = await c.MakeRequest(url);
			if (responseString.Substring(0, 2) == "OK")
			{
				tutorialDescriptions = new List<string>();
				tutorialPictures = new List<string>();
				responseString = responseString.Substring(3);
				
				string[] lines = responseString.Split("\t");
				int count = 0;
				foreach (string line in lines)
				{
					count++;
					if (count % 2 == 1)
					{
						tutorialDescriptions.Add(line);
					}
					else
					{
						tutorialPictures.Add(line);
					}
				}
				
				currentTutorial = 0;
				LoadTutorial(); 
				LoadEmptyPictures(tutorialDescriptions.Count);

				await Task.Run(async () =>
				{
					for (int i = 0; i < tutorialDescriptions.Count; i++)
					{
						if (cancelImageLoading)
						{
							c.CW("Cancelling task");
							break;
						}
						await LoadPicture(i);
					}
				});
			}
			else
			{
				c.ReportError(responseString);
			}
		}
		
		private void TutorialBack_Click(object sender, EventArgs e)
		{
			TutorialTopBar.Visibility = ViewStates.Invisible;
			TutorialFrameBg.Visibility = ViewStates.Invisible;
			TutorialFrame.Visibility = ViewStates.Invisible;
			TutorialTopSeparator.Visibility = ViewStates.Invisible;
			TutorialBottomSeparator.Visibility = ViewStates.Invisible;
			TutorialNavBar.Visibility = ViewStates.Invisible;
			OpenTutorial.Visibility = ViewStates.Visible;
			LoaderCircle.Visibility = ViewStates.Gone;
			cancelImageLoading = true;
		}

		private void StartAnim()
		{
			Animation anim = Android.Views.Animations.AnimationUtils.LoadAnimation(this, Resource.Animation.rotate);
			LoaderCircle.StartAnimation(anim);
			LoaderCircle.Visibility = ViewStates.Visible;
		}

		private void StopAnim()
		{
			LoaderCircle.Visibility = ViewStates.Gone;
			LoaderCircle.ClearAnimation();
		}

		private void LoadPrevious_Click(object sender, EventArgs e)
		{
			currentTutorial--;
			if (currentTutorial < 0)
			{
				currentTutorial = tutorialDescriptions.Count - 1;
			}
			LoadTutorial();
		}

		private void LoadNext_Click(object sender, EventArgs e)
		{
			currentTutorial++;
			if (currentTutorial > tutorialDescriptions.Count - 1)
			{
				currentTutorial = 0;
			}
			LoadTutorial();
		}

		private void LoadTutorial()
		{
			TutorialText.Text = tutorialDescriptions[currentTutorial];
			TutorialNavText.Text = currentTutorial + 1 + " / " + tutorialDescriptions.Count;
			TutorialFrame.ScrollX = currentTutorial * TutorialFrame.Width;
		}

		private void LoadEmptyPictures(int count)
		{
			for (int index = 0; index < count; index++)
			{
				ImageView image = new ImageView(this)
				{
					Id = 1000 + index
				};
				ConstraintLayout.LayoutParams p = new ConstraintLayout.LayoutParams(TutorialFrame.Width, ViewGroup.LayoutParams.MatchParent); //using MatchParent for width will place the images on top of each other.
				if (index == 0)
				{
					p.LeftToLeft = Resource.Id.TutorialFrame;
				}
				else
				{
					p.LeftToRight = 1000 + index - 1;
				}
				image.LayoutParameters = p;
				TutorialFrame.AddView(image);
			}
		}
		private async Task LoadPicture(int index)
		{
			ImageView image = (ImageView)TutorialFrame.GetChildAt(index);
			
			string url = Constants.HostName + Constants.TutorialFolder + "/" + tutorialPictures[index];
			c.CW("LoadPicture " + index + " " + url);

			Bitmap im = null;

			var task = CommonMethods.GetImageBitmapFromUrlAsync(url);
			System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource();

			if (await Task.WhenAny(task, Task.Delay(Constants.RequestTimeout, cts.Token)) == task)
			{
				cts.Cancel();
				im = await task;
			}

			if (index == 0)
			{
				RunOnUiThread(() =>
				{
					StopAnim();
				});
			}

			if (im is null)
			{
				if (cancelImageLoading)
				{
					return;
				}
				RunOnUiThread(() => {
					image.SetImageResource(Resource.Drawable.noimage_hd);
				});
			}
			else
			{
				if (cancelImageLoading)
				{
					return;
				}
				RunOnUiThread(() => {
					image.SetImageResource(0);
					image.SetImageBitmap(im);
				});
			}
		}

		public bool ScrollDown(MotionEvent e)
		{
			return true;
		}
		public bool ScrollMove(MotionEvent e)
		{
			return true;
		}
		public bool ScrollUp()
		{
			return true;
		}
	}
}