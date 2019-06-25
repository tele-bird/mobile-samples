using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Firebase.DynamicLinks;
using Tasky.PortableLibrary;
using TaskyAndroid;
using TaskyAndroid.ApplicationLayer;

namespace TaskyAndroid.Screens 
{
	/// <summary>
	/// Main ListView screen displays a list of tasks, plus an [Add] button
	/// </summary>
	[Activity (MainLauncher = true, Icon="@drawable/icon")]			
	public class HomeScreen : Activity, Android.Gms.Tasks.IOnSuccessListener, Android.Gms.Tasks.IOnFailureListener
    {
        public void OnFailure(Java.Lang.Exception e)
        {
            System.Console.WriteLine($"Failed to receive Dynamic Link - exception: {e}");
        }

        public void OnSuccess(Java.Lang.Object result)
        {
            PendingDynamicLinkData pendingDynamicLinkData = (PendingDynamicLinkData)result;
            if (pendingDynamicLinkData != null)
            {
                System.Console.WriteLine($"Received Dynamic Link: {pendingDynamicLinkData.Link}");
                AlertDialog.Builder dialogBuilder = new AlertDialog.Builder(this);
                AlertDialog dialog = dialogBuilder.Create();
                dialog.SetTitle("Received Deep Link");
                dialog.SetMessage(pendingDynamicLinkData.Link.ToString());
                dialog.SetButton("OK", (c, ev) => { });
                dialog.Show();
                //ViewModel.SetDeepLink(pendingDynamicLinkData.Link.ToString());
            }
        }

        TodoItemListAdapter taskList;
		IList<TodoItem> tasks;
		Button addTaskButton;
		ListView taskListView;
		
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// set our layout to be the home screen
			SetContentView(Resource.Layout.HomeScreen);

			//Find our controls
			taskListView = FindViewById<ListView> (Resource.Id.TaskList);
			addTaskButton = FindViewById<Button> (Resource.Id.AddButton);

			// wire up add task button handler
			if(addTaskButton != null) {
				addTaskButton.Click += (sender, e) => {
					StartActivity(typeof(TodoItemScreen));
				};
			}
			
			// wire up task click handler
			if(taskListView != null) {
				taskListView.ItemClick += (object sender, AdapterView.ItemClickEventArgs e) => {
					var taskDetails = new Intent (this, typeof (TodoItemScreen));
					taskDetails.PutExtra ("TaskID", tasks[e.Position].ID);
					StartActivity (taskDetails);
				};
			}
		}
		
		protected override void OnResume ()
		{
			base.OnResume ();

            //tasks = TaskyApp.Current.TodoManager.GetTasks();

            //// create our adapter
            //taskList = new TodoItemListAdapter(this, tasks);

            ////Hook up our adapter to our ListView
            //taskListView.Adapter = taskList;

            try
            {
                FirebaseDynamicLinks.Instance.GetDynamicLink(Intent).AddOnSuccessListener(this, this).AddOnFailureListener(this, this);
            }
            catch (Exception exc)
            {
                int x = 0;
            }

        }
    }
}