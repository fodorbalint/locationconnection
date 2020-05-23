//section: explanation to icons
//on mobile textbox disappears on typing, it gets so small.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Android.Animation;
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
		public TouchConstraintLayout TutorialFrame;
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

			float width = Math.Min(dpWidth, screenHeight / pixelDensity);

			string url = "action=tutorial&OS=Android&dpWidth=" + width;
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

		float touchStartX, touchStartY;
		float touchCurrentX, touchCurrentY;
		float currentOffsetX, currentOffsetY;
		int startScrollX, totalScroll, startPic;
		float prevPos;
		long prevTime;
		float prevSpeedX, speedX;
		Stopwatch stw = new Stopwatch();

		int clickTime = 300;
		int swipeMinDistance = 15;
		double swipeMinSpeed = 0.2;
		Timer scrollTimer;
		float startValue, endValue, timeValue, middleTime, topSpeed, acceleration;
		ObjectAnimator animator;

		public bool ScrollDown(MotionEvent e)
		{
			if (!(animator is null) && animator.IsRunning || !(scrollTimer is null) && scrollTimer.Enabled)
			{
				return false;
			}

			touchStartX = e.GetX();
			touchStartY = e.GetY();
			touchCurrentX = touchStartX;
			touchCurrentY = touchStartY;
			currentOffsetX = touchCurrentX - touchStartX; //when clicking, move event may not get triggered.
			currentOffsetY = touchCurrentY - touchStartY;

			startScrollX = TutorialFrame.ScrollX;
			totalScroll = TutorialFrame.Width * (tutorialDescriptions.Count - 1);
			startPic = PosToPic(TutorialFrame.ScrollX);

			stw.Restart();
			prevPos = touchStartX;
			prevTime = 0;
			prevSpeedX = 0;
			speedX = 0;

			return true;
		}
		public bool ScrollMove(MotionEvent e)
		{
			touchCurrentX = e.GetX();
			touchCurrentY = e.GetY();

			if (touchCurrentX != prevPos) //Move event gets triggered when we programatically scroll the view, even though the finger didn't move.
			{
				currentOffsetX = touchCurrentX - touchStartX;

				int value = (int)(startScrollX - currentOffsetX);
				if (value >= 0 && value <= totalScroll)
				{
					TutorialFrame.ScrollX = value;
				}
				else if (value > totalScroll)
				{
					TutorialFrame.ScrollX = totalScroll;
				}
				else
				{
					TutorialFrame.ScrollX = 0;
				}

				int newTutorial = (int)Math.Round((float)TutorialFrame.ScrollX / TutorialFrame.Width);

				if (newTutorial != currentTutorial)
				{
					TutorialText.Text = tutorialDescriptions[newTutorial];
					TutorialNavText.Text = newTutorial + 1 + " / " + tutorialDescriptions.Count;
					currentTutorial = newTutorial;
				}

				long currentTime = stw.ElapsedMilliseconds;
				long intervalTime = currentTime - prevTime;

				prevSpeedX = speedX; //the last speed value is often much less than it should be, we use the one before the last.
				speedX = (touchCurrentX - prevPos) / intervalTime;
				prevPos = touchCurrentX;
				prevTime = currentTime;
			}
			return false;
		}
		public bool ScrollUp()
		{
			stw.Stop();

			if (stw.ElapsedMilliseconds < clickTime && Math.Abs(currentOffsetX) < swipeMinDistance * pixelDensity && Math.Abs(currentOffsetY) < swipeMinDistance * pixelDensity)
			{
				startPic = PosToPic(startScrollX);
				int newPos;
				int newIndex;

				if (touchStartX + currentOffsetX >= screenWidth / 2) //next
				{
					if (startPic == tutorialDescriptions.Count - 1)
					{
						newIndex = 0;
					}
					else
					{
						newIndex = startPic + 1;
					}
				}
				else //previous
				{
					if (startPic == 0)
					{
						newIndex = tutorialDescriptions.Count - 1;
					}
					else
					{
						newIndex = startPic - 1;
					}
				}

				newPos = PicToPos(newIndex);
				TutorialFrame.ScrollX = newPos;

				TutorialText.Text = tutorialDescriptions[newIndex];
				TutorialNavText.Text = newIndex + 1 + " / " + tutorialDescriptions.Count;
				currentTutorial = newIndex;

				return false;
			}

			int currentPic = PosToPic(TutorialFrame.ScrollX);

			speedX = (Math.Abs(speedX) > Math.Abs(prevSpeedX)) ? speedX : prevSpeedX;

			if (Math.Abs(speedX) > swipeMinSpeed * pixelDensity)
			{
				if (currentOffsetX < -swipeMinDistance * pixelDensity && speedX < 0)
				{
					int newPos = PicToPos(currentPic + 1);
					if (newPos <= totalScroll)
					{
						float remainingDistance = (TutorialFrame.Width - TutorialFrame.ScrollX % TutorialFrame.Width);
						long estimatedTime = -(long)(remainingDistance / speedX * 2);

						var v = -speedX;
						var s = remainingDistance;
						var t = tweenTime;

						double x1 = (4 * s + Math.Sqrt(16 * s * s - 8 * t * v * (2 * s - t * v))) / (4 * t);
						double t1 = (x1 - v) / (2 * x1 - v) * t;

						stw.Restart();
						scrollTimer = new Timer();
						scrollTimer.Interval = 1; // 1000 per framerate would work too, but it is 16.666. Settings 17, motion is ok, but sometimes jumps.

						if (estimatedTime <= tweenTime)
						{
							scrollTimer.Elapsed += T_Elapsed;
							startValue = TutorialFrame.ScrollX;
							endValue = newPos;
							timeValue = estimatedTime;
						}
						else
						{
							scrollTimer.Elapsed += T2_Elapsed;
							startValue = TutorialFrame.ScrollX;
							endValue = newPos;
							timeValue = tweenTime;
							middleTime = (float)t1;
							//speeds are positive
							speedX = -speedX;
							topSpeed = (float)x1;
							acceleration = (float)((x1 - v) / t1);
						}
						scrollTimer.Start();

						TutorialText.Text = tutorialDescriptions[currentPic + 1];
						TutorialNavText.Text = currentPic + 2 + " / " + tutorialDescriptions.Count;
						currentTutorial = currentPic + 1;
					}

				}
				else if (currentOffsetX > swipeMinDistance * pixelDensity && speedX > 0)
				{
					int newPos = PicToPos(currentPic);
					if (newPos >= 0)
					{
						float remainingDistance = TutorialFrame.ScrollX % TutorialFrame.Width;
						long estimatedTime = (long)(remainingDistance / speedX * 2);

						var v = speedX;
						var s = remainingDistance;
						var t = tweenTime;
						double x1 = (4 * s + Math.Sqrt(16 * s * s - 8 * t * v * (2 * s - t * v))) / (4 * t);
						double t1 = (x1 - v) / (2 * x1 - v) * t;

						stw.Restart();
						scrollTimer = new Timer();
						scrollTimer.Interval = 1;

						if (estimatedTime <= tweenTime)
						{
							scrollTimer.Elapsed += T_Elapsed;
							startValue = TutorialFrame.ScrollX;
							endValue = newPos;
							timeValue = estimatedTime;
						}
						else
						{
							scrollTimer.Elapsed += T2_Elapsed;
							startValue = TutorialFrame.ScrollX;
							endValue = newPos;
							timeValue = tweenTime;
							middleTime = (float)t1;
							//speeds are negative
							speedX = -speedX;
							topSpeed = -(float)x1;
							acceleration = -(float)((x1 - v) / t1);
						}
						scrollTimer.Start();

						TutorialText.Text = tutorialDescriptions[currentPic];
						TutorialNavText.Text = currentPic + 1 + " / " + tutorialDescriptions.Count;
						currentTutorial = currentPic;
					}
				}
				else
				{
					//not enough distance, pull image to closest border
					double remainder = TutorialFrame.ScrollX % TutorialFrame.Width;
					int newPos;
					int newIndex;
					if (remainder < TutorialFrame.Width / 2)
					{
						newIndex = currentPic;
					}
					else
					{
						newIndex = currentPic + 1;
					}
					newPos = PicToPos(newIndex);

					animator = ObjectAnimator.OfInt(TutorialFrame, "ScrollX", newPos);
					animator.SetDuration(tweenTime);
					animator.Start();

					if (startPic != newIndex)
					{
						TutorialText.Text = tutorialDescriptions[newIndex];
						TutorialNavText.Text = newIndex + 1 + " / " + tutorialDescriptions.Count;
						currentTutorial = newIndex;
					}
				}
			}
			else
			{
				//not enough speed, pull image to closest border
				double remainder = TutorialFrame.ScrollX % TutorialFrame.Width;
				int newPos;
				int newIndex;
				if (remainder < TutorialFrame.Width / 2)
				{
					newIndex = currentPic;
				}
				else
				{
					newIndex = currentPic + 1;
				}
				newPos = PicToPos(newIndex);

				animator = ObjectAnimator.OfInt(TutorialFrame, "ScrollX", newPos);
				animator.SetDuration(tweenTime);
				animator.Start();

				if (startPic != newIndex)
				{
					TutorialText.Text = tutorialDescriptions[newIndex];
					TutorialNavText.Text = newIndex + 1 + " / " + tutorialDescriptions.Count;
					currentTutorial = newIndex;
				}
			}
			return false;
		}

		private void T_Elapsed(object sender, ElapsedEventArgs e) //decelerate
		{
			long millis = stw.ElapsedMilliseconds;
			if (millis < timeValue)
			{
				//decelerate
				float currentSpeed = (1 - millis / timeValue) * speedX;
				float avgSpeed = (speedX + currentSpeed) / 2;
				TutorialFrame.ScrollX = (int)(startValue - avgSpeed * millis);
			}
			else
			{
				stw.Stop();
				scrollTimer.Stop();
				TutorialFrame.ScrollX = (int)endValue;
			}
		}

		private void T2_Elapsed(object sender, ElapsedEventArgs e) //accelerate - decelerate
		{
			long millis = stw.ElapsedMilliseconds;
			if (millis < middleTime)
			{
				float currentSpeed = speedX + acceleration * millis;
				float elapsedDistance = (speedX + currentSpeed) / 2 * millis;
				TutorialFrame.ScrollX = (int)(startValue + elapsedDistance);
				//ProfileImageScroll.Invalidate();
			}
			else if (millis < timeValue)
			{
				float currentSpeed = topSpeed - acceleration * (millis - middleTime);
				float elapsedDistance = (speedX + topSpeed) / 2 * middleTime + (topSpeed + currentSpeed) / 2 * (millis - middleTime);
				TutorialFrame.ScrollX = (int)(startValue + elapsedDistance);
				//ProfileImageScroll.Invalidate();
			}
			else
			{
				stw.Stop();
				scrollTimer.Stop();
				TutorialFrame.ScrollX = (int)endValue;
			}
		}

		public int PosToPic(double pos)
		{
			double remainder = pos % TutorialFrame.Width;
			return (int)((pos - remainder) / TutorialFrame.Width);
		}

		private int PicToPos(int pic)
		{
			return TutorialFrame.Width * pic;
		}
	}
}