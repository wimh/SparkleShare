//   SparkleShare, an instant update workflow to Git.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Drawing;
using System.IO;
using System.Timers;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using Mono.Unix;

namespace SparkleShare {

	// The statusicon that stays in the
	// user's notification area
	public class SparkleStatusIcon : NSObject {

		private Timer Animation;
		private int FrameNumber;
		private string StateText;

		private NSStatusItem StatusItem;
		private NSMenu Menu;
		private NSMenuItem StateMenuItem;
		private NSMenuItem FolderMenuItem;
		private NSMenuItem [] FolderMenuItems;
		private NSMenuItem SyncMenuItem;
		private NSMenuItem NotificationsMenuItem;
		private NSMenuItem AboutMenuItem;
		
		private delegate void Task ();
		private EventHandler [] Tasks;

		
		// Short alias for the translations
		public static string _ (string s)
		{
			return Catalog.GetString (s);
		}

		
		public SparkleStatusIcon () : base ()
		{			
			
			Animation = CreateAnimation ();

			StatusItem = NSStatusBar.SystemStatusBar.CreateStatusItem (28);
			StatusItem.HighlightMode = true;
			
			
			SetNormalState ();
			CreateMenu ();
			
			Menu.Delegate = new SparkleStatusIconMenuDelegate ();
			
			
			SparkleShare.Controller.FolderSizeChanged += delegate {
				InvokeOnMainThread (delegate {
					
					if (!Animation.Enabled)
						SetNormalState ();
							
					UpdateMenu ();
					
				});
			};
			
			SparkleShare.Controller.FolderListChanged += delegate {
				InvokeOnMainThread (delegate {
					SetNormalState ();
					CreateMenu ();
				});
			};

			SparkleShare.Controller.OnIdle += delegate {
				InvokeOnMainThread (delegate {
					SetNormalState ();
					UpdateMenu ();
				});
			};

			SparkleShare.Controller.OnSyncing += delegate {
				InvokeOnMainThread (delegate {
					SetAnimationState ();
					UpdateMenu ();
				});
			};

			SparkleShare.Controller.OnError += delegate {
				 InvokeOnMainThread (delegate {
					SetNormalState (true);
					UpdateMenu ();
				});
			};

		}


		// Creates the Animation that handles the syncing animation
		private Timer CreateAnimation ()
		{

			FrameNumber = 0;

			Timer Animation = new Timer () {
				Interval = 35
			};

			Animation.Elapsed += delegate {

				if (FrameNumber < 4)
					FrameNumber++;
				else
					FrameNumber = 0;

				InvokeOnMainThread (delegate {
					
					string image_path =
						Path.Combine (NSBundle.MainBundle.ResourcePath,
							"Pixmaps", "idle" + FrameNumber + ".png");					
					
					StatusItem.Image      = new NSImage (image_path);
					StatusItem.Image.Size = new SizeF (16, 16);

					
					string alternate_image_path =
						Path.Combine (NSBundle.MainBundle.ResourcePath,
							"Pixmaps", "idle" + FrameNumber + ".png");
					
					StatusItem.AlternateImage      = new NSImage (alternate_image_path);
					StatusItem.AlternateImage.Size = new SizeF (16, 16);
					
				});

			};

			return Animation;

		}


		// Creates the menu that is popped up when the
		// user clicks the status icon
		public void CreateMenu ()
		{
			
			Menu = new NSMenu ();
			
			StateMenuItem = new NSMenuItem () {
				Title = StateText
			};
			
			Menu.AddItem (StateMenuItem);	
			Menu.AddItem (NSMenuItem.SeparatorItem);

	
			FolderMenuItem = new NSMenuItem () {
				Title = "SparkleShare"
			};

				FolderMenuItem.Activated += delegate {
					SparkleShare.Controller.OpenSparkleShareFolder ();
				};

			
				string folder_icon_path = Path.Combine (NSBundle.MainBundle.ResourcePath,
					"sparkleshare-mac.icns");
			
				FolderMenuItem.Image = new NSImage (folder_icon_path);
				FolderMenuItem.Image.Size = new SizeF (16, 16);	
			
			Menu.AddItem (FolderMenuItem);
			
			
			if (SparkleShare.Controller.Folders.Count > 0) {

				FolderMenuItems = new NSMenuItem [SparkleShare.Controller.Folders.Count];
				Tasks = new EventHandler [SparkleShare.Controller.Folders.Count];
				
				int i = 0;
				
				foreach (string path in SparkleShare.Controller.Folders) {	

					// TODO
//					if (repo.HasUnsyncedChanges)
//						folder_action.IconName = "dialog-error";
					
					NSMenuItem item = new NSMenuItem ();
					
					item.Title      = System.IO.Path.GetFileName (path);
					item.Image      = NSImage.ImageNamed ("NSFolder");
					item.Image.Size = new SizeF (16, 16);

					Tasks [i] = OpenEventLogDelegate(item.Title);
			
					FolderMenuItems [i] = item;
					FolderMenuItems [i].Activated += Tasks [i];
					Menu.AddItem (FolderMenuItems [i]);
					
					i++;
			
				};
		
			} else {
		
				FolderMenuItems = new NSMenuItem [1];

				FolderMenuItems [0] = new NSMenuItem () {	
					Title = "No Remote Folders Yet"
				};
				
				Menu.AddItem (FolderMenuItems [0]);
		
			}
				
			Menu.AddItem (NSMenuItem.SeparatorItem);

			
			SyncMenuItem = new NSMenuItem () {
				Title = "Add Remote Folder…"
			};
			
				if (!SparkleShare.Controller.FirstRun) {
			
					SyncMenuItem.Activated += delegate {
					
						InvokeOnMainThread (delegate {
	
							NSApplication.SharedApplication.ActivateIgnoringOtherApps (true);
						
							if (SparkleUI.Intro == null) {
							
								SparkleUI.Intro = new SparkleIntro ();
								SparkleUI.Intro.ShowServerForm (true);
		
							}
			
							if (!SparkleUI.Intro.IsVisible)
								SparkleUI.Intro.ShowServerForm (true);
		
							SparkleUI.Intro.OrderFrontRegardless ();
							SparkleUI.Intro.MakeKeyAndOrderFront (this);
	
						});
					
					};
				
				}

			Menu.AddItem (SyncMenuItem);

			
			Menu.AddItem (NSMenuItem.SeparatorItem);
			

			NotificationsMenuItem = new NSMenuItem ();
			
				if (SparkleShare.Controller.NotificationsEnabled)
					NotificationsMenuItem.Title = "Turn Notifications Off";
				else
					NotificationsMenuItem.Title = "Turn Notifications On";
								
				NotificationsMenuItem.Activated += delegate {
			
					SparkleShare.Controller.ToggleNotifications ();
								
					InvokeOnMainThread (delegate {
				
						if (SparkleShare.Controller.NotificationsEnabled)
							NotificationsMenuItem.Title = "Turn Notifications Off";
						else
							NotificationsMenuItem.Title = "Turn Notifications On";
													
					});
			
				};

			Menu.AddItem (NotificationsMenuItem);
			
			
			Menu.AddItem (NSMenuItem.SeparatorItem);
			
			
			AboutMenuItem = new NSMenuItem () {
				Title = "Visit Website"
			};

				AboutMenuItem.Activated += delegate {
					
					NSUrl url = new NSUrl ("http://www.sparkleshare.org/");
					NSWorkspace.SharedWorkspace.OpenUrl (url);
				
				};

			Menu.AddItem (AboutMenuItem);
										 
			StatusItem.Menu = Menu;
			StatusItem.Menu.Update ();

		}

		
		// A method reference that makes sure that opening the
		// event log for each repository works correctly
		private EventHandler OpenEventLogDelegate (string path)
		{

			return delegate { 
						
				InvokeOnMainThread (delegate {
	
					NSApplication.SharedApplication.ActivateIgnoringOtherApps (true);
					
					SparkleLog log = SparkleUI.OpenLogs.Find (delegate (SparkleLog l) {
						return l.LocalPath.Equals (path);
					});
		
					// Check whether the log is already open, create a new one if
					// that's not the case or present it to the user if it is
					if (log == null) {
						
						SparkleUI.OpenLogs.Add (new SparkleLog (path));
						SparkleUI.OpenLogs [SparkleUI.OpenLogs.Count - 1].MakeKeyAndOrderFront (this);
				
					} else {
	
						log.OrderFrontRegardless ();
						log.MakeKeyAndOrderFront (this);
						
					}
							
				});
				
			};

		}


		public void UpdateMenu ()
		{

			StateMenuItem.Title = StateText;

		}


		// The state when there's nothing going on
		private void SetNormalState ()
		{

			SetNormalState (false);

		}


		// The state when there's nothing going on
		private void SetNormalState (bool error)
		{

			Animation.Stop ();
			
			if (SparkleShare.Controller.Folders.Count == 0) {

				StateText = _("Welcome to SparkleShare!");
				InvokeOnMainThread (delegate {

					StatusItem.Image      = new NSImage (NSBundle.MainBundle.ResourcePath + "/Pixmaps/idle.png");
					StatusItem.Image.Size = new SizeF (16, 16);
						
					StatusItem.AlternateImage      = new NSImage (NSBundle.MainBundle.ResourcePath + "/Pixmaps/idle-active.png");
					StatusItem.AlternateImage.Size = new SizeF (16, 16);
					
				});
				
			} else {
			
				if (error) {

					StateText = _("Not everything is synced");
					InvokeOnMainThread (delegate {
						//Pixbuf = SparkleUIHelpers.GetIcon ("sparkleshare-syncing-error", 24);
					});

				} else {
					
					StateText = _("Up to date") + " (" + SparkleShare.Controller.FolderSize + ")";
					InvokeOnMainThread (delegate {
						
						StatusItem.Image      = new NSImage (NSBundle.MainBundle.ResourcePath + "/Pixmaps/idle.png");
						StatusItem.Image.Size = new SizeF (16, 16);
						
						StatusItem.AlternateImage      = new NSImage (NSBundle.MainBundle.ResourcePath + "/Pixmaps/idle-active.png");
						StatusItem.AlternateImage.Size = new SizeF (16, 16);
						
					});

				}

			}

		}


		// The state when animating
		private void SetAnimationState ()
		{

			StateText = _("Syncing…");

			if (!Animation.Enabled)
				Animation.Start ();

		}

	}
	
	
	public class SparkleStatusIconMenuDelegate : NSMenuDelegate {
		
		public override void MenuWillHighlightItem (NSMenu menu, NSMenuItem item) { }
	
		public override void MenuWillOpen (NSMenu menu)
		{

			InvokeOnMainThread (delegate {
				
				SparkleUI.NewEvents = 0;
				NSApplication.SharedApplication.DockTile.BadgeLabel = null;

			});

		}
		
	}

}


