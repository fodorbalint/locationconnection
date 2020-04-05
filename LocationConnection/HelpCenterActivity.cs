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
	public class HelpCenterActivity : BaseActivity
	{
		ImageButton HelpCenterBack;
		ScrollView QuestionsScroll;
		LinearLayout QuestionsContainer;
		TextView HelpCenterFormCaption;
		Button MessageSend;
		EditText MessageEdit;
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
				QuestionsScroll = FindViewById<ScrollView>(Resource.Id.QuestionsScroll);
				QuestionsContainer = FindViewById<LinearLayout>(Resource.Id.QuestionsContainer);
				HelpCenterFormCaption = FindViewById<TextView>(Resource.Id.HelpCenterFormCaption);
				MessageEdit = FindViewById<EditText>(Resource.Id.MessageEdit);
				MessageSend = FindViewById<Button>(Resource.Id.MessageSend);

				MessageEdit.Visibility = ViewStates.Gone;
				MessageSend.Visibility = ViewStates.Gone;

				HelpCenterBack.Click += HelpCenterBack_Click;
				HelpCenterFormCaption.Click += HelpCenterFormCaption_Click;
				MessageSend.Click += MessageSend_Click;

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