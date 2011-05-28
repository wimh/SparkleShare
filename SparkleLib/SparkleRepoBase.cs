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
using System.IO;
using System.Text.RegularExpressions;
using System.Timers;
using System.Xml;

namespace SparkleLib {

    public enum SyncStatus {
        Idle,
        SyncUp,
        SyncDown,
        Error
    }


    public abstract class SparkleRepoBase {

        public readonly SparkleBackend Backend;
        public readonly string LocalPath;
        public readonly string Name;

        protected SyncStatus status;
        protected bool is_buffering  = false;
        protected bool is_polling    = true;
        protected bool server_online = true;

        private Timer local_timer = new Timer () { Interval = 250 };
        private Timer remote_timer = new Timer () { Interval = 60000 };
        private FileSystemWatcher watcher;
        private SparkleListenerBase listener;
        private List <double> sizebuffer = new List<double> ();
        private bool has_changed   = false;
        private Object change_lock = new Object ();

        public abstract bool AnyDifferences { get; }
        public abstract string Identifier { get; }
        public abstract string CurrentRevision { get; }
        public abstract bool SyncUp ();
        public abstract bool SyncDown ();
        public abstract bool HasUnsyncedChanges { get; set; }

        public delegate void SyncStatusChangedEventHandler (SyncStatus new_status);
        public event SyncStatusChangedEventHandler SyncStatusChanged;

        public delegate void NewChangeSetEventHandler (SparkleChangeSet change_set, string source_path);
        public event NewChangeSetEventHandler NewChangeSet;

        public delegate void ConflictResolvedEventHandler ();
        public event ConflictResolvedEventHandler ConflictResolved;

        public delegate void ChangesDetectedEventHandler ();
        public event ChangesDetectedEventHandler ChangesDetected;


        public SparkleRepoBase (string path, SparkleBackend backend)
        {
            LocalPath = path;
            Name      = Path.GetFileName (LocalPath);
            Backend   = backend;

            SyncStatusChanged += delegate (SyncStatus status) {
                this.status = status;
            };

            if (CurrentRevision == null) {
                CreateInitialChangeSet ();
                HasUnsyncedChanges = true;
            }

            CreateWatcher ();
            CreateListener ();

            this.local_timer.Elapsed += delegate (object o, ElapsedEventArgs args) {
                CheckForChanges ();
            };

            this.remote_timer.Elapsed += delegate {
                if (this.is_polling) {
                    if (CheckForRemoteChanges ())
                        SyncDownBase ();
                }

                if (this.is_polling && !this.listener.IsConnecting && !this.listener.IsConnected)
                    this.listener.Connect ();

                // In the unlikely case that we haven't synced up our
                // changes or the server was down, sync up again
                if (HasUnsyncedChanges)
                    SyncUpBase ();
            };

            this.remote_timer.Start ();
            this.local_timer.Start ();

            // Sync up everything that changed
            // since we've been offline
            if (AnyDifferences) {
                DisableWatching ();
                SyncUpBase ();

                while (HasUnsyncedChanges)
                    SyncUpBase ();
                EnableWatching ();
            }
        }


        // Create an initial change set when the
        // user has fetched an empty remote folder
        public virtual void CreateInitialChangeSet ()
        {
            string file_path = Path.Combine (LocalPath, "SparkleShare.txt");
            TextWriter writer = new StreamWriter (file_path);
            writer.WriteLine (":)");
            writer.Close ();
        }


        public bool ServerOnline {
            get {
                return this.server_online;
            }
        }


        public SyncStatus Status {
            get {
                return this.status;
            }
        }


        public string Domain {
            get {
                Regex regex = new Regex (@"(@|://)([a-z0-9\.]+)(/|:)");
                Match match = regex.Match (SparkleConfig.DefaultConfig.GetUrlForFolder (Name));

                if (match.Success)
                    return match.Groups [2].Value;
                else
                    return null;
            }
        }


        protected void OnConflictResolved ()
        {
            HasUnsyncedChanges = true;

            if (ConflictResolved != null)
                ConflictResolved ();
        }


        public virtual bool CheckForRemoteChanges () // HasRemoteChanges { get; } ?
        {
            return true;
        }


        public virtual List<SparkleChangeSet> GetChangeSets (int count) {
            return null;
        }


        public virtual bool UsesNotificationCenter {
            get {
                return true;
            }
        }


        public string RemoteName {
            get {
                string url = SparkleConfig.DefaultConfig.GetUrlForFolder (Name);
                return Path.GetFileNameWithoutExtension (url);
            }
        }


        public bool IsBuffering {
            get {
                return this.is_buffering;
            }
        }


        public bool IsPolling {
            get {
                return this.is_polling;
            }
        }





        // Disposes all resourses of this object
        public void Dispose ()
        {
            this.remote_timer.Dispose ();
            this.local_timer.Dispose ();
            this.listener.Dispose ();
        }


        private void CreateWatcher ()
        {
            this.watcher = new FileSystemWatcher (LocalPath) {
                IncludeSubdirectories = true,
                EnableRaisingEvents   = true,
                Filter                = "*"
            };

            this.watcher.Changed += new FileSystemEventHandler (OnFileActivity);
            this.watcher.Created += new FileSystemEventHandler (OnFileActivity);
            this.watcher.Deleted += new FileSystemEventHandler (OnFileActivity);
            this.watcher.Renamed += new RenamedEventHandler (OnFileActivity);
        }


        private void CreateListener ()
        {
            NotificationServerType server_type;
            if (UsesNotificationCenter)
                server_type = NotificationServerType.Central;
            else
                server_type = NotificationServerType.Own;

            this.listener = SparkleListenerFactory.CreateIrcListener (Domain, Identifier, server_type);

            // Stop polling when the connection to the irc channel is succesful
            this.listener.Connected += delegate {
                this.is_polling = false;

                // Check for changes manually one more time
                if (CheckForRemoteChanges ())
                    SyncDownBase ();

                // Push changes that were made since the last disconnect
                if (HasUnsyncedChanges)
                    SyncUpBase ();
            };

            // Start polling when the connection to the irc channel is lost
            this.listener.Disconnected += delegate {
                SparkleHelpers.DebugInfo (Name, "Falling back to polling");
                this.is_polling = true;
            };

            // Fetch changes when there is a message in the irc channel
            this.listener.RemoteChange += delegate (SparkleAnnouncement announcement) {
                string identifier = Identifier;

                if (announcement.FolderIdentifier == identifier &&
                    !announcement.Message.Equals (CurrentRevision)) {
                    if ((Status != SyncStatus.SyncUp)   &&
                        (Status != SyncStatus.SyncDown) &&
                        !this.is_buffering) {

                        while (this.listener.HasQueueDownAnnouncement (identifier))
                            SyncDownBase ();
                    }
                }
            };

            // Start listening
            if (!this.listener.IsConnected && !this.listener.IsConnecting)
                this.listener.Connect ();
            else
                this.is_polling = false;
        }


        private void CheckForChanges ()
        {
            lock (this.change_lock) {
                if (this.has_changed) {
                    if ( this.sizebuffer.Count >= 4)
                         this.sizebuffer.RemoveAt (0);
                        
                    DirectoryInfo dir_info = new DirectoryInfo (LocalPath);
                     this.sizebuffer.Add (CalculateFolderSize (dir_info));

                    if ( this.sizebuffer [0].Equals (this.sizebuffer [1]) &&
                         this.sizebuffer [1].Equals (this.sizebuffer [2]) &&
                         this.sizebuffer [2].Equals (this.sizebuffer [3])) {

                        SparkleHelpers.DebugInfo ("Local", "[" + Name + "] Changes have settled.");
                        this.is_buffering = false;
                        this.has_changed  = false;
                        
                        DisableWatching ();
                        while (AnyDifferences)
                            SyncUpBase ();
                        EnableWatching ();
                    }
                }
            }
        }


        // Starts a timer when something changes
        private void OnFileActivity (object o, FileSystemEventArgs fse_args)
        {
            if (fse_args.Name.StartsWith (".git/") || fse_args.Name.StartsWith (".hg/"))
                return;

            WatcherChangeTypes wct = fse_args.ChangeType;

            if (AnyDifferences) {
                this.is_buffering = true;

                // Only fire the event if the timer has been stopped.
                // This prevents multiple events from being raised whilst "buffering".
                if (!this.has_changed) {
                    if (ChangesDetected != null)
                        ChangesDetected ();
                }

                SparkleHelpers.DebugInfo ("Event", "[" + Name + "] " + wct.ToString () + " '" + fse_args.Name + "'");
                SparkleHelpers.DebugInfo ("Local", "[" + Name + "] Changes found, checking if settled.");
                
                this.remote_timer.Stop ();

                lock (this.change_lock) {
                    this.has_changed = true;
                }
            }
        }


        private void SyncUpBase ()
        {
            try {
                this.local_timer.Stop ();
                this.remote_timer.Stop ();

                SparkleHelpers.DebugInfo ("SyncUp", "[" + Name + "] Initiated");

                if (SyncStatusChanged != null)
                    SyncStatusChanged (SyncStatus.SyncUp);

                if (SyncUp ()) {
                    SparkleHelpers.DebugInfo ("SyncUp", "[" + Name + "] Done");

                    HasUnsyncedChanges = false;

                    if (SyncStatusChanged != null)
                        SyncStatusChanged (SyncStatus.Idle);

                    this.listener.AnnounceBase (new SparkleAnnouncement (Identifier, CurrentRevision));

                } else {
                    SparkleHelpers.DebugInfo ("SyncUp", "[" + Name + "] Error");

                    HasUnsyncedChanges = true;
                    SyncDownBase ();

                    if (SyncUp ()) {
                        HasUnsyncedChanges = false;

                        if (SyncStatusChanged != null)
                            SyncStatusChanged (SyncStatus.Idle);

                        this.listener.AnnounceBase (new SparkleAnnouncement (Identifier, CurrentRevision));

                    } else {
                        if (SyncStatusChanged != null)
                            SyncStatusChanged (SyncStatus.Error);
                    }
                }

            } finally {
                this.remote_timer.Start ();
                this.local_timer.Start ();
            }
        }


        private void SyncDownBase ()
        {
            SparkleHelpers.DebugInfo ("SyncDown", "[" + Name + "] Initiated");
            this.remote_timer.Stop ();

            if (SyncStatusChanged != null)
                SyncStatusChanged (SyncStatus.SyncDown);

            if (SyncDown ()) {
                SparkleHelpers.DebugInfo ("SyncDown", "[" + Name + "] Done");
                this.server_online = true;

                if (SyncStatusChanged != null)
                    SyncStatusChanged (SyncStatus.Idle);

                // There could be changes from a
                // resolved conflict
                if (HasUnsyncedChanges)
                    SyncUp ();

            } else {
                SparkleHelpers.DebugInfo ("SyncDown", "[" + Name + "] Error");
                this.server_online = false;

                if (SyncStatusChanged != null)
                    SyncStatusChanged (SyncStatus.Error);
            }

            if (SyncStatusChanged != null)
                SyncStatusChanged (SyncStatus.Idle);

            this.remote_timer.Start ();

            if (NewChangeSet != null)
                NewChangeSet (GetChangeSets (1) [0], LocalPath);
        }


        public void DisableWatching ()
        {
            this.watcher.EnableRaisingEvents = false;
        }


        public void EnableWatching ()
        {
            this.watcher.EnableRaisingEvents = true;
        }


        // Recursively gets a folder's size in bytes
        private double CalculateFolderSize (DirectoryInfo parent)
        {
            if (!System.IO.Directory.Exists (parent.ToString ()))
                return 0;

            double size = 0;

            // Ignore the temporary 'rebase-apply' directory. This prevents potential
            // crashes when files are being queried whilst the files have already been deleted.
            if (parent.Name.Equals ("rebase-apply"))
                return 0;

            foreach (FileInfo file in parent.GetFiles()) {
                if (!file.Exists)
                    return 0;

                size += file.Length;
            }

            foreach (DirectoryInfo directory in parent.GetDirectories())
                size += CalculateFolderSize (directory);

            return size;
        }
    }
}
