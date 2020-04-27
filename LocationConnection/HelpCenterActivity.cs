//section: explanation to icons
//on mobile textbox disappears on typing, it gets so small.
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
		TouchConstraintLayout TutorialFrame;

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
				TutorialFrame = FindViewById<TouchConstraintLayout>(Resource.Id.TutorialFrame);

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
			}
			catch (Exception ex)
			{
				c.ReportErrorSilent(ex.Message + System.Environment.NewLine + ex.StackTrace);
			}
		}

		private void LoadNext_Click(object sender, EventArgs e)
		{
			//throw new NotImplementedException();
		}

		private void LoadPrevious_Click(object sender, EventArgs e)
		{
			//throw new NotImplementedException();
		}

		private void TutorialBack_Click(object sender, EventArgs e)
		{
			TutorialTopBar.Visibility = ViewStates.Invisible;
			TutorialFrame.Visibility = ViewStates.Invisible;
			TutorialNavBar.Visibility = ViewStates.Invisible;
			OpenTutorial.Visibility = ViewStates.Visible;
		}

		private void OpenTutorial_Click(object sender, EventArgs e)
		{
			TutorialTopBar.Visibility = ViewStates.Visible;
			TutorialFrame.Visibility = ViewStates.Visible;
			TutorialNavBar.Visibility = ViewStates.Visible;
			OpenTutorial.Visibility = ViewStates.Invisible; //it remains visible when clicked, even though TutorialFrame is on top of it in the view hierarchy
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

		private void HelpCenterBack_Click(object sender, EventArgs e)
		{
			OnBackPressed();
		}
	}
}