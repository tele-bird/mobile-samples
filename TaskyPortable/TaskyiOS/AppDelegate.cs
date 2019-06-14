using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using UIKit;
using SQLite;
using Tasky.PortableLibrary;
using System.IO;
using Firebase.DynamicLinks;
using ObjCRuntime;
using System.Text;

namespace Tasky 
{
	public class Application 
	{
		// This is the main entry point of the application.
		static void Main (string[] args)
		{
			// if you want to use a different Application Delegate class from "AppDelegate"
			// you can specify it here.
			UIApplication.Main (args, null, "AppDelegate");
		}
	}

	[Register ("AppDelegate")]
	public partial class AppDelegate : UIApplicationDelegate 
	{
		// class-level declarations
		UIWindow window;
		UINavigationController navController;
		UITableViewController homeViewController;

		public static AppDelegate Current { get; private set; }
		public TodoItemManager TodoManager { get; set; }
		SQLiteConnection conn;

		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			Current = this;

			// create a new window instance based on the screen size
			window = new UIWindow (UIScreen.MainScreen.Bounds);
			
			// make the window visible
			window.MakeKeyAndVisible ();


			// Create the database file
			var sqliteFilename = "TodoItemDB.db3";
			// we need to put in /Library/ on iOS5.1 to meet Apple's iCloud terms
			// (they don't want non-user-generated data in Documents)
			string documentsPath = Environment.GetFolderPath (Environment.SpecialFolder.Personal); // Documents folder
			string libraryPath = Path.Combine (documentsPath, "..", "Library"); // Library folder
			var path = Path.Combine(libraryPath, sqliteFilename);
			conn = new SQLiteConnection(path);
			TodoManager = new TodoItemManager(conn);


			// create our nav controller
			navController = new UINavigationController ();

			// create our Todo list screen
			homeViewController = new Screens.HomeScreen ();

			// push the view controller onto the nav controller and show the window
			navController.PushViewController(homeViewController, false);
			window.RootViewController = navController;
			window.MakeKeyAndVisible ();

            Firebase.Core.App.Configure();
			
			return true;
		}

        public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
        {
            return OpenUrl(app, url, null, null);
        }

        public override bool OpenUrl(UIApplication application, NSUrl url, string sourceApplication, NSObject annotation)
        {
            var dynamicLink = DynamicLinks.SharedInstance?.FromCustomSchemeUrl(url);

            ShowMessage(ConstructMessageTitle("OpenUrl", null, dynamicLink), ConstructDebugText(url, null, dynamicLink), application.KeyWindow.RootViewController);

            if (dynamicLink == null || dynamicLink.Url == null)
            {
                return false;
            }

            //// handle the deep link here!
            ////return _deepLinkService.TryOpenDeepLink(url.AbsoluteString);

            return true;
        }

        public override bool ContinueUserActivity(UIApplication application, NSUserActivity userActivity, UIApplicationRestorationHandler completionHandler)
        {
            bool result = DynamicLinks.SharedInstance.HandleUniversalLink(userActivity.WebPageUrl, (dynamicLink, error) =>
            {
                ShowMessage(ConstructMessageTitle("ContinueUserActivity", error, dynamicLink), ConstructDebugText(userActivity.WebPageUrl, error, dynamicLink), application.KeyWindow.RootViewController);
            });

            if (!result)
            {
                ShowMessage("ContinueUserActivity", $"HandleUniversalLink returned false{Environment.NewLine}{Environment.NewLine}input: {userActivity.WebPageUrl}", application.KeyWindow.RootViewController);
            }

            return result;
        }

        public static void ShowMessage(string title, string message, UIViewController fromViewController)
        {
            var alert = UIAlertController.Create(title, message, UIAlertControllerStyle.Alert);
            alert.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, (obj) => { }));
            fromViewController.PresentViewController(alert, true, null);
        }

        private string ConstructMessageTitle(string context, NSError error, DynamicLink dynamicLink)
        {
            return context + ((error != null) ? " error" : (dynamicLink == null) || (dynamicLink.Url == null) ? " fail" : " success");
        }

        private string ConstructDebugText(NSUrl inputUrl, NSError error, DynamicLink dynamicLink)
        {
            StringBuilder sbDebugText = new StringBuilder();
            sbDebugText.AppendLine($"input: {inputUrl}");
            if (error != null)
            {
                sbDebugText.Append(error.LocalizedDescription);
            }
            else
            {
                if (dynamicLink == null)
                {
                    sbDebugText.Append("dynamicLink: null");
                }
                else
                {
                    sbDebugText.AppendLine($"{Environment.NewLine}URL: {dynamicLink.Url}");
                    sbDebugText.AppendLine($"{Environment.NewLine}MatchType: {dynamicLink.MatchType}");
                    sbDebugText.AppendLine($"{Environment.NewLine}MinimumAppVersion: {dynamicLink.MinimumAppVersion}");
                }
            }

            return sbDebugText.ToString();
        }
    }
}