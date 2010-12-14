using System;
using System.Drawing;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using MonoMac.WebKit;

namespace SparkleShare
{
	class MainClass
	{
		static void Main (string[] args)
		{
			NSApplication.Init ();
			NSApplication.SharedApplication.ActivateIgnoringOtherApps (true);
			NSApplication.Main (args);
		}
	}
	
	
		[MonoMac.Foundation.Register("AppDelegate")]
	public partial class AppDelegate : NSApplicationDelegate
	{
		
		//MainWindowController mainWindowController;
		NSStatusItem StatusItem;
		
		NSMenu Menu;
		NSMenuItem FolderMenuItem;
		NSMenuItem [] FolderMenuItems;
		NSMenuItem SyncMenuItem;
		NSMenuItem NotificationsMenuItem;
		NSMenuItem AboutMenuItem;
		NSMenuItem QuitMenuItem;

		NSTextField text;
		NSWindow window;
		NSButton button;
		NSButton button2;
		
		WebView web_view;
		
		
		public AppDelegate ()
		{
		}

		public override void FinishedLaunching (NSObject notification)
		{
			
		
		
			

			//	mainWindowController = new MainWindowController ();
			//	mainWindowController.Window.MakeKeyAndOrderFront (this);
			
			//			SparkleStatusIcon = new SparkleStatusIcon ();

			//		SparkleRepo repo = new SparkleRepo ("/Users/hbons/SparkleShare/SparkleShare-Test");

			StatusItem = NSStatusBar.SystemStatusBar.CreateStatusItem (32);
			
			StatusItem.Enabled             = true;
			StatusItem.Image               = NSImage.ImageNamed ("sparkleshare-idle.png");
			StatusItem.AlternateImage      = NSImage.ImageNamed ("sparkleshare-idle-focus.png");
			StatusItem.Image.Size          = new SizeF (13, 13);	
			StatusItem.AlternateImage.Size = new SizeF (13, 13);	
			StatusItem.HighlightMode = true;

			Menu = new NSMenu ();
	
			
			Menu.AddItem (new NSMenuItem () { Title="Up to date (102 ᴍʙ)", Enabled = true });			
			Menu.AddItem (NSMenuItem.SeparatorItem);
			
			
			FolderMenuItem = new NSMenuItem () {
				Title="SparkleShare", Enabled = true,
				Action = new Selector ("ddd")
			};
			
				FolderMenuItem.Activated += delegate {
					Console.WriteLine ("DDDD");	
				};
			
				FolderMenuItem.Image = NSImage.ImageNamed ("NSFolder");
				FolderMenuItem.Image.Size = new SizeF (16, 16);	

			Menu.AddItem (FolderMenuItem);
			
			FolderMenuItems = new NSMenuItem [2] {
				new NSMenuItem () { Title = "gnome-design" },
				new NSMenuItem () { Title = "tango-icons" }	
			};
			
			foreach (NSMenuItem item in FolderMenuItems) {
				
				item.Activated += delegate {
					
					
					
					
		button = new NSButton (new RectangleF (16, 12, 120, 31)) {
			Title = "Open Folder",
			BezelStyle = NSBezelStyle.Rounded
					
		};
					
				button2 = new NSButton (new RectangleF (480 - 120 - 16, 12, 120, 31)) {
			Title = "Close",
			BezelStyle = NSBezelStyle.Rounded
					
		};

		window = new NSWindow (new RectangleF (0, 0, 480, 640), (NSWindowStyle) (1 | (1 << 1) | (1 << 2) | (1 << 3)), 0, false);
	
					bool minimizeBox = true;
					bool maximizeBox = false;
window.StyleMask = (NSWindowStyle)(1 | (1 << 1) | (minimizeBox ? 4 : 1) | (maximizeBox ? 8 : 1));
					
					
		web_view = new WebView (new RectangleF (0, 12 + 31 + 16, 480, 640 - (12 + 31 + 16)), "", "");			
					web_view.MainFrameUrl = "http://www.google.nl/";
					
					
		window.ContentView.AddSubview (button);
		window.ContentView.AddSubview (button2);
				window.ContentView.AddSubview (web_view);
					
		window.MaxSize = new SizeF (480, 640);
		window.MinSize = new SizeF (480, 640);
					
					window.Title = "Recent Events in 'gnome-design'";

		window.HasShadow = true;	

		window.BackingType = NSBackingStore.Buffered;

					
					
					
	window.MakeKeyAndOrderFront (this);
					window.Center ();
					
					
					
				};
				
				item.Image = NSImage.ImageNamed ("NSFolder");
				Menu.AddItem (item);	
			};
		

			Menu.AddItem (NSMenuItem.SeparatorItem);

			
			SyncMenuItem = new NSMenuItem () {
				Title = "Sync Remote Folder..."
			};
			
				SyncMenuItem.Activated += delegate {
				
				};
			
			Menu.AddItem (SyncMenuItem);

			
			Menu.AddItem (NSMenuItem.SeparatorItem);
			

			NotificationsMenuItem = new NSMenuItem () {
				Title = "Show Notifications",
				State = NSCellStateValue.On
			};

				NotificationsMenuItem.Activated += delegate {
					
					//StatusItem.Image = NSImage.ImageNamed ("NSComputer");
				if (NotificationsMenuItem.State == NSCellStateValue.On)
	
					NotificationsMenuItem.State = NSCellStateValue.Off;
				
				else

					NotificationsMenuItem.State = NSCellStateValue.On;

				};

			Menu.AddItem (NotificationsMenuItem);
			
			
			Menu.AddItem (NSMenuItem.SeparatorItem);
			
			
			AboutMenuItem = new NSMenuItem () {
				Title = "About"
			};

				AboutMenuItem.Activated += delegate {
	
				};

			Menu.AddItem (AboutMenuItem);

			
			Menu.AddItem (NSMenuItem.SeparatorItem);

			
			QuitMenuItem = new NSMenuItem () {
				Title = "Quit"
			};
	
				QuitMenuItem.Activated += delegate {
					Environment.Exit (0);
				};
			
			Menu.AddItem (QuitMenuItem);									 

			StatusItem.Menu = Menu;

		}
	}
	
}

