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
            DynamicLinks.PerformDiagnostics(null);
			
			return true;
		}

        public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
        {
            return OpenUrl(app, url, null, null);
        }

        public override bool OpenUrl(UIApplication application, NSUrl url, string sourceApplication, NSObject annotation)
        {
            Console.WriteLine($"I have received a URL through a custom scheme! {url.AbsoluteString}");
            var dynamicLink = DynamicLinks.SharedInstance?.FromCustomSchemeUrl(url);
            if(dynamicLink != null)
            {
                HandleIncomingDynamicLink(dynamicLink);
                return true;
            }

            return false;
        }

        public override bool ContinueUserActivity(UIApplication application, NSUserActivity userActivity, UIApplicationRestorationHandler completionHandler)
        {
            if(userActivity.WebPageUrl != null)
            {
                Console.WriteLine($"Incoming URL is {userActivity.WebPageUrl}");
                bool linkHandled = DynamicLinks.SharedInstance.HandleUniversalLink(userActivity.WebPageUrl, (dynamicLink, error) =>
                {
                    if (error != null)
                    {
                        Console.WriteLine($"Found an error! {error.LocalizedDescription}");
                    }
                    else if (dynamicLink != null)
                    {
                        Console.WriteLine(dynamicLink.Url?.AbsoluteString);
                        HandleIncomingDynamicLink(dynamicLink);
                    }
                });

                if (linkHandled)
                {
                    Console.WriteLine("Link handled");
                }
                else
                {
                    Console.WriteLine("Link not handled");
                }
            }

            return false;
        }

        private void HandleIncomingDynamicLink(DynamicLink dynamicLink)
        {
            if(dynamicLink.Url == null)
            {
                Console.WriteLine("That's weird.  My dynamic link object has no URL.");
            }
            else
            {
                Console.WriteLine($"Your incoming link parameter is {dynamicLink.Url.AbsoluteString}");
                var components = new NSUrlComponents(dynamicLink.Url, resolveAgainstBaseUrl: false);
                if (components.QueryItems != null)
                {
                    foreach(var queryItem in components.QueryItems)
                    {
                        Console.WriteLine($"Parameter {queryItem.Name} has a value of {queryItem.Value}");
                    }
                    Console.WriteLine($"Dynamic link match type is {dynamicLink.MatchType}");
                }
            }
        }
    }
}