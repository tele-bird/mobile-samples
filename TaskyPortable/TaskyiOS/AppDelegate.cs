using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using UIKit;
using SQLite;
using Tasky.PortableLibrary;
using System.IO;
//using Firebase.DynamicLinks;
using ObjCRuntime;
using System.Text;
using System.Threading.Tasks;
//using Firebase.Core;

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

        NSUrl _deepLinkUrl = null;

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

            //Options.DefaultInstance.DeepLinkUrlScheme = "com.keurig.app";
            //App.Configure();
            //DynamicLinks.PerformDiagnostics(null);

            Console.WriteLine("Phil's FinishedLaunching is about to parse a deep link URL from the clipboard...");
            _deepLinkUrl = GetDeepLinkUrlFromClipboard().Result;
            if (_deepLinkUrl != null)
            {
                Console.WriteLine($"Phil's FinishedLaunching has parsed a deep link URL from the clipboard: {_deepLinkUrl}");
                Console.WriteLine($"Phil's FinishedLaunching Clipboard contents after parsing: {GetClipboardText().Result}");
                ShowMessage("FinishedLaunching found a deep link", _deepLinkUrl.ToString(), app.KeyWindow.RootViewController);
            }


            return true;
		}

        private void ShowMessage(string title, string message, UIViewController fromViewController)
        {
            var alert = UIAlertController.Create(title, message, UIAlertControllerStyle.Alert);
            alert.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, (obj) => { }));
            fromViewController.PresentViewController(alert, true, null);
        }

        private async Task<NSUrl> GetDeepLinkUrlFromClipboard()
        {
            NSUrl deepLinkUrl = null;
            string clipboardText = await GetClipboardText(false);
            Console.WriteLine($"Phil's GetDeepLinkUrlFromClipboard found on clipboard: {clipboardText}");
            NSUrl firebaseUrl = null;
            try
            {
                firebaseUrl = NSUrl.FromString(clipboardText);
                Console.WriteLine($"Phil's GetDeepLinkUrlFromClipboard parsed a Firebase URL from clipboard: {firebaseUrl}");
            }
            catch (Exception exc)
            {
                Console.WriteLine($"Phil's GetDeepLinkUrlFromClipboard caught an {exc.GetType().FullName} while parsing the clipboard contents into a URL. clipboardText: {clipboardText}");
            }
            if (firebaseUrl != null && TryParseDeepLink(firebaseUrl, out deepLinkUrl))
            {
                await GetClipboardText(true);
            }
            return deepLinkUrl;
        }

        private bool TryParseDeepLink(NSUrl firebaseUrl, out NSUrl deepLinkUrl)
        {
            deepLinkUrl = null;
            bool success = false;

            NSUrlComponents urlComponents = new NSUrlComponents(firebaseUrl, false);
            if(urlComponents.QueryItems != null)
            {
                NSUrl candidateDeepLinkUrl = null;
                Dictionary<string, string> queryItems = new Dictionary<string, string>();
                foreach (NSUrlQueryItem queryItem in urlComponents.QueryItems)
                {
                    queryItems.Add(queryItem.Name.ToLower(), queryItem.Value);
                    if (queryItem.Name.ToLower() == "link")
                    {
                        candidateDeepLinkUrl = NSUrl.FromString(queryItem.Value);
                    }
                }

                success =
                    queryItems.Keys.Contains("isi") &&
                    queryItems.Keys.Contains("ibi") &&
                    candidateDeepLinkUrl != null;

                if (success)
                {
                    deepLinkUrl = candidateDeepLinkUrl;
                }
            }

            return success;
        }

        public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
        {
            return OpenUrl(app, url, null, null);
        }

        public override bool OpenUrl(UIApplication application, NSUrl url, string sourceApplication, NSObject annotation)
        {
            Console.WriteLine($"Phil's I have received a URL through a custom scheme! {url.AbsoluteString}");
            ////var dynamicLink = DynamicLinks.SharedInstance?.FromCustomSchemeUrl(url);
            ////if (dynamicLink != null)
            ////{
            ////    HandleIncomingDynamicLink(dynamicLink);
            ////    return true;
            ////}

            return false;
        }

        private async Task<string> GetClipboardText(bool cut = false)
        {
            string clipboardText = null;
            if (Xamarin.Essentials.Clipboard.HasText)
            {
                string maybeEmptyClipboardText = await Xamarin.Essentials.Clipboard.GetTextAsync();
                maybeEmptyClipboardText = maybeEmptyClipboardText.Trim();
                if(!string.IsNullOrEmpty(maybeEmptyClipboardText))
                {
                    clipboardText = maybeEmptyClipboardText;
                    if (cut)
                    {
                        await Xamarin.Essentials.Clipboard.SetTextAsync(string.Empty);
                    }
                }
            }
            return clipboardText;
        }

        public override bool ContinueUserActivity(UIApplication application, NSUserActivity userActivity, UIApplicationRestorationHandler completionHandler)
        {
            if(userActivity.WebPageUrl != null)
            {
                bool linkHandled = false;
                Console.WriteLine($"Phil's ContinueUserActivity Incoming URL is {userActivity.WebPageUrl}");
                NSUrl deepLinkUrl = null;
                if(TryParseDeepLink(userActivity.WebPageUrl, out deepLinkUrl))
                {
                    Console.WriteLine($"Phil's ContinueUserActivity has parsed a deep link URL from the incoming URL: {deepLinkUrl}");
                    linkHandled = true;
                    ShowMessage("ContinueUserActivity found a deep link", deepLinkUrl.ToString(), application.KeyWindow.RootViewController);
                }
                ////bool linkHandled = DynamicLinks.SharedInstance.HandleUniversalLink(userActivity.WebPageUrl, (dynamicLink, error) =>
                ////{
                ////    if (error != null)
                ////    {
                ////        Console.WriteLine($"Found an error! {error.LocalizedDescription}");
                ////    }
                ////    else if (dynamicLink != null)
                ////    {
                ////        Console.WriteLine(dynamicLink.Url?.AbsoluteString);
                ////        HandleIncomingDynamicLink(dynamicLink);
                ////    }
                ////});

                if (linkHandled)
                {
                    Console.WriteLine("Phil's Link handled");
                }
                else
                {
                    Console.WriteLine("Phil's Link not handled");
                }
            }

            return false;
        }

        ////private void HandleIncomingDynamicLink(DynamicLink dynamicLink)
        ////{
        ////    if(dynamicLink.Url == null)
        ////    {
        ////        Console.WriteLine("That's weird.  My dynamic link object has no URL.");
        ////    }
        ////    else
        ////    {
        ////        Console.WriteLine($"Your incoming link parameter is {dynamicLink.Url.AbsoluteString}");
        ////        var components = new NSUrlComponents(dynamicLink.Url, resolveAgainstBaseUrl: false);
        ////        if (components.QueryItems != null)
        ////        {
        ////            foreach(var queryItem in components.QueryItems)
        ////            {
        ////                Console.WriteLine($"Parameter {queryItem.Name} has a value of {queryItem.Value}");
        ////            }
        ////            Console.WriteLine($"Dynamic link match type is {dynamicLink.MatchType}");
        ////        }
        ////    }
        ////}
    }
}