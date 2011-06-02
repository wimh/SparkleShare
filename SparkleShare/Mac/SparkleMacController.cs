//   SparkleShare, a collaboration and sharing tool.
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
using System.Diagnostics;
using System.IO;

using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using SparkleLib;

namespace SparkleShare {

	public class SparkleMacController : SparkleController {

        // We have to use our own custom made folder watcher, as
        // System.IO.FileSystemWatcher fails watching subfolders on Mac
        private SparkleMacWatcher watcher = new SparkleMacWatcher (SparklePaths.SparklePath);


        public SparkleMacController () : base ()
        {
            watcher.Changed += delegate (string path) {
                string repo_name;

                if (path.Contains ("/"))
                    repo_name = path.Substring (0, path.IndexOf ("/"));
                else
                    repo_name = path;

                // Ignore changes in the root of each subfolder, these
                // are already handled bu the repository
                if (Path.GetFileNameWithoutExtension (path).Equals (repo_name))
                    return;

                repo_name = repo_name.Trim ("/".ToCharArray ());
                FileSystemEventArgs args = new FileSystemEventArgs (WatcherChangeTypes.Changed,
                    Path.Combine (SparklePaths.SparklePath, path), Path.GetFileName (path));

                foreach (SparkleRepoBase repo in Repositories) {
                    if (repo.Name.Equals (repo_name))
                        repo.OnFileActivity (this, args);
                }
            };
        }


		public override void EnableSystemAutostart ()
		{
			// N/A
		}


		public override void InstallLauncher ()
		{
			// N/A
		}

		
		// Adds the SparkleShare folder to the user's
		// list of bookmarked places
		public override void AddToBookmarks ()
		{
			// TODO
		}
		

		// Creates the SparkleShare folder in the user's home folder
		public override bool CreateSparkleShareFolder ()
		{
			if (!Directory.Exists (SparklePaths.SparklePath)) {
				Directory.CreateDirectory (SparklePaths.SparklePath);
				return true;
			} else {
				return false;
			}
		}

		
		// Opens the SparkleShare folder or an (optional) subfolder
		public override void OpenSparkleShareFolder (string subfolder)
		{
			string folder = Path.Combine (SparklePaths.SparklePath, subfolder);
			folder.Replace (" ", "\\ "); // Escape space-characters			
			
			NSWorkspace.SharedWorkspace.OpenFile (folder);
		}
		
		
		public override string EventLogHTML
		{
			get {
				string resource_path = NSBundle.MainBundle.ResourcePath;
				string html_path     = Path.Combine (resource_path, "HTML", "event-log.html");
				
				StreamReader reader = new StreamReader (html_path);
				string html = reader.ReadToEnd ();
				reader.Close ();
				
				return html;
			}
		}

		
		public override string DayEntryHTML
		{
			get {
				string resource_path = NSBundle.MainBundle.ResourcePath;
				string html_path     = Path.Combine (resource_path, "HTML", "day-entry.html");
				
				StreamReader reader = new StreamReader (html_path);
				string html = reader.ReadToEnd ();
				reader.Close ();
				
				return html;
			}
		}
		
	
		public override string EventEntryHTML
		{
			get {
				string resource_path = NSBundle.MainBundle.ResourcePath;
				string html_path     = Path.Combine (resource_path, "HTML", "event-entry.html");
				
				StreamReader reader = new StreamReader (html_path);
				string html = reader.ReadToEnd ();
				reader.Close ();
				
				return html;
			}
		}
	}
}
