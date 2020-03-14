/*
 Used when opening ProfileViewActivity.
 When using ActivityFlags.ReorderToFront, the activity will still have its previous intent, which will cause malfunction if we open:
 List -> ProfileView -> ChatOne -> ProfileView
 ClearTop calls OnCreate
 SingleTop calls OnCreate, and creates multiple instances of the activity we pressed back from.
 ReorderToFront calls OnCreate only if activity does not exist in the back stack.
 */
namespace LocationConnection
{
	public class IntentData
	{
		public static string pageType;
		public static int? targetID;
		public static bool logout;
		public static bool authError;
		public static string error;
		public static int? senderID;
	}
}