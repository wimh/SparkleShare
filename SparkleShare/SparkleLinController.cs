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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using Mono.Unix;
using SparkleLib;

namespace SparkleShare {

    public class SparkleLinController : SparkleController {

        public SparkleLinController () : base ()
        {

        }


        // Creates a .desktop entry in autostart folder to
        // start SparkleShare automatically at login
        public override void EnableSystemAutostart ()
        {
            string autostart_path = SparkleHelpers.CombineMore (SparklePaths.HomePath, ".config", "autostart");
            string desktopfile_path = SparkleHelpers.CombineMore (autostart_path, "sparkleshare.desktop");

            if (!File.Exists (desktopfile_path)) {
                if (!Directory.Exists (autostart_path))
                    Directory.CreateDirectory (autostart_path);

                TextWriter writer = new StreamWriter (desktopfile_path);
                writer.WriteLine ("[Desktop Entry]\n" +
                                  "Type=Application\n" +
                                  "Name=SparkleShare\n" +
                                  "Exec=sparkleshare start\n" +
                                  "Icon=folder-sparkleshare\n" +
                                  "Terminal=false\n" +
                                  "X-GNOME-Autostart-enabled=true\n" +
                                  "Categories=Network");
                writer.Close ();

                // Give the launcher the right permissions so it can be launched by the user
                UnixFileInfo file_info = new UnixFileInfo (desktopfile_path);
                file_info.Create (FileAccessPermissions.UserReadWriteExecute);

                SparkleHelpers.DebugInfo ("Controller", "Created: " + desktopfile_path);
            }
        }
        

        // Installs a launcher so the user can launch SparkleShare
        // from the Internet category if needed
        public override void InstallLauncher ()
        {
            string apps_path = SparkleHelpers.CombineMore (SparklePaths.HomePath, ".local", "share", "applications");
            string desktopfile_path = SparkleHelpers.CombineMore (apps_path, "sparkleshare.desktop");

            if (!File.Exists (desktopfile_path)) {
                if (!Directory.Exists (apps_path))
                    Directory.CreateDirectory (apps_path);

                TextWriter writer = new StreamWriter (desktopfile_path);
                writer.WriteLine ("[Desktop Entry]\n" +
                                  "Type=Application\n" +
                                  "Name=SparkleShare\n" +
                                  "Comment=Share documents\n" +
                                  "Exec=sparkleshare start\n" +
                                  "Icon=folder-sparkleshare\n" +
                                  "Terminal=false\n" +
                                  "Categories=Network;");
                writer.Close ();

                // Give the launcher the right permissions so it can be launched by the user
                UnixFileInfo file_info = new UnixFileInfo (desktopfile_path);
                file_info.FileAccessPermissions = FileAccessPermissions.UserReadWriteExecute;

                SparkleHelpers.DebugInfo ("Controller", "Created '" + desktopfile_path + "'");
            }
        }


        // Adds the SparkleShare folder to the user's
        // list of bookmarked places
        public override void AddToBookmarks ()
        {
            string bookmarks_file_path   = Path.Combine (SparklePaths.HomePath, ".gtk-bookmarks");
            string sparkleshare_bookmark = "file://" + SparklePaths.SparklePath + " SparkleShare";

            if (File.Exists (bookmarks_file_path)) {
                StreamReader reader = new StreamReader (bookmarks_file_path);
                string bookmarks = reader.ReadToEnd ();
                reader.Close ();

                if (!bookmarks.Contains (sparkleshare_bookmark)) {
                    TextWriter writer = File.AppendText (bookmarks_file_path);
                    writer.WriteLine ("file://" + SparklePaths.SparklePath + " SparkleShare");
                    writer.Close ();
                }
            } else {
                StreamWriter writer = new StreamWriter (bookmarks_file_path);
                writer.WriteLine ("file://" + SparklePaths.SparklePath + " SparkleShare");
                writer.Close ();
            }
        }


        // Creates the SparkleShare folder in the user's home folder
        public override bool CreateSparkleShareFolder ()
        {
            if (!Directory.Exists (SparklePaths.SparklePath)) {
        
                Directory.CreateDirectory (SparklePaths.SparklePath);
                SparkleHelpers.DebugInfo ("Controller", "Created '" + SparklePaths.SparklePath + "'");

                string icon_file_path = SparkleHelpers.CombineMore (Defines.DATAROOTDIR, "icons", "hicolor",
                    "48x48", "apps", "folder-sparkleshare.png");

                string gvfs_command_path = SparkleHelpers.CombineMore (Path.VolumeSeparatorChar.ToString (),
                    "usr", "bin", "gvfs-set-attribute");

                // Add a special icon to the SparkleShare folder
                if (File.Exists (gvfs_command_path)) {
                    Process process = new Process ();

                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.UseShellExecute        = false;
                    process.StartInfo.FileName               = "gvfs-set-attribute";

                    // Clear the custom (legacy) icon path
                    process.StartInfo.Arguments = SparklePaths.SparklePath + " metadata::custom-icon ''";                                                          "";
                    process.Start ();

                    // Give the SparkleShare folder an icon name, so that it scales
                    process.StartInfo.Arguments = SparklePaths.SparklePath + " metadata::custom-icon-name 'folder-sparkleshare'";                                                          "";
                    process.Start ();
                }

                return true;
            }

            return false;
        }
        

        public override string EventLogHTML {
            get {
                string path = SparkleHelpers.CombineMore (Defines.PREFIX,
                    "share", "sparkleshare", "html", "event-log.html");
            
                return String.Join (Environment.NewLine, File.ReadAllLines (path));
            }
        }

        
        public override string DayEntryHTML {
            get {
                string path = SparkleHelpers.CombineMore (Defines.PREFIX,
                    "share", "sparkleshare", "html", "day-entry.html");
            
                return String.Join (Environment.NewLine, File.ReadAllLines (path));
            }
        }

        
        public override string EventEntryHTML {
            get {
                string path = SparkleHelpers.CombineMore (Defines.PREFIX,
                    "share", "sparkleshare", "html", "event-entry.html");
            
                return String.Join (Environment.NewLine, File.ReadAllLines (path));
            }
        }

            
        public override void OpenSparkleShareFolder (string subfolder)
        {
            string open_command_path = SparkleHelpers.CombineMore (Path.VolumeSeparatorChar.ToString (),
                "usr", "bin", "xdg-open");

            if (!File.Exists (open_command_path))
                return;

            string folder = SparkleHelpers.CombineMore (SparklePaths.SparklePath, subfolder);

            Process process = new Process ();
            process.StartInfo.Arguments = folder.Replace (" ", "\\ "); // Escape space-characters
            process.StartInfo.FileName  = "xdg-open";
            process.Start ();
        }
    }
}
