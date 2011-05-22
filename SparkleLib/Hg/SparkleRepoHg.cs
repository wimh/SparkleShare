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
using System.Text.RegularExpressions;

namespace SparkleLib {

    public class SparkleRepoHg : SparkleRepoBase {

        public SparkleRepoHg (string path, SparkleBackend backend) :
            base (path, backend) { }


        public override string Url {
            get {
                string repo_config_file_path = SparkleHelpers.CombineMore (LocalPath, ".hg", "hgrc");
                Regex regex = new Regex (@"default = (.+)");

                foreach (string line in File.ReadAllLines (repo_config_file_path)) {
                    Match match = regex.Match (line);

                    if (match.Success)
                        return match.Groups [1].Value.TrimEnd ();
                }

                return null;
            }
        }


        public override string Identifier {
            get {
                SparkleHg hg = new SparkleHg (LocalPath, "log -r : --limit 1 --template \"{node}\"");
                hg.Start ();
                hg.WaitForExit ();

                return hg.StandardOutput.ReadToEnd ();
            }
        }


        public override string CurrentRevision {
            get {
                SparkleHg hg = new SparkleHg (LocalPath, "log --limit 1 --template \"{node}\"");
                hg.Start ();
                hg.WaitForExit ();

                return hg.StandardOutput.ReadToEnd ();
            }
        }


        public override bool CheckForRemoteChanges ()
        {
            return true; // Mercurial doesn't have a way to check for the remote hash
        }


        public override bool SyncUp ()
        {
            Add ();

            string message = FormatCommitMessage ();
            Commit (message);

            SparkleHg hg = new SparkleHg (LocalPath, "push");

            hg.Start ();
            hg.WaitForExit ();

            if (hg.ExitCode == 0) {
                return true;
                //FetchRebaseAndPush ();TODO
            } else {
                return false;
            }
        }


        public override bool SyncDown ()
        {
            SparkleHg hg = new SparkleHg (LocalPath, "pull");

            hg.Start ();
            hg.WaitForExit ();

            if (hg.ExitCode == 0) {
                Merge ();
                return true;
            } else {
                return false;
            }
        }


        public override bool AnyDifferences {
            get {
                SparkleHg hg = new SparkleHg (LocalPath, "status");
                hg.Start ();
                hg.WaitForExit ();

                string output = hg.StandardOutput.ReadToEnd ().TrimEnd ();
                string [] lines = output.Split ("\n".ToCharArray ());

                foreach (string line in lines) {
                    if (line.Length > 1 && !line [1].Equals (" "))
                        return true;
                }

                return false;
            }
        }


        public override bool HasUnsyncedChanges {
            get {
                string unsynced_file_path = SparkleHelpers.CombineMore (LocalPath,
                    ".hg", "has_unsynced_changes");

                return File.Exists (unsynced_file_path);
            }

            set {
                string unsynced_file_path = SparkleHelpers.CombineMore (LocalPath,
                    ".hg", "has_unsynced_changes");

                if (value) {
                    if (!File.Exists (unsynced_file_path))
                        File.Create (unsynced_file_path);
                } else {
                    File.Delete (unsynced_file_path);
                }
            }
        }


        // Stages the made changes
        private void Add ()
        {
            SparkleHg hg = new SparkleHg (LocalPath, "addremove --quiet");
            hg.Start ();
            hg.WaitForExit ();

            SparkleHelpers.DebugInfo ("Hg", "[" + Name + "] Changes staged");
        }


        // Commits the made changes
        private void Commit (string message)
        {
            if (!AnyDifferences)
                return;

            SparkleHg hg = new SparkleHg (LocalPath, "commit -m '" + message + "'");
            hg.Start ();
            hg.WaitForExit ();

            SparkleHelpers.DebugInfo ("Commit", "[" + Name + "] " + message);
        }


        // Merges the fetched changes
        private void Merge ()
        {
            DisableWatching ();

            if (AnyDifferences) {
                Add ();

                string commit_message = FormatCommitMessage ();
                Commit (commit_message);
            }

            SparkleHg hg = new SparkleHg (LocalPath, "update");

            hg.Start ();
            hg.WaitForExit ();

            EnableWatching ();
        }


        // Returns a list of the latest change sets
        // TODO: Method needs to be made a lot faster
        public override List<SparkleChangeSet> GetChangeSets (int count)
        {
            if (count < 1)
                count = 30;

            List <SparkleChangeSet> change_sets = new List <SparkleChangeSet> ();

            string style_file_path = SparkleHelpers.CombineMore (LocalPath, ".hg", "log.style");
            SparkleHg hg_log = new SparkleHg (LocalPath, "log --limit " + count + " --style " + style_file_path);
            Console.OutputEncoding = System.Text.Encoding.Unicode;
            hg_log.Start ();

            // Reading the standard output HAS to go before
            // WaitForExit, or it will hang forever on output > 4096 bytes
            string output = hg_log.StandardOutput.ReadToEnd ();
            hg_log.WaitForExit ();

            string [] lines       = output.Split ("\n".ToCharArray ());
            List <string> entries = new List <string> ();

            int j = 0;
            string entry = "", last_entry = "";
            foreach (string line in lines) {
                if (line.StartsWith ("changeset:") && j > 0) {
                    entries.Add (entry);
                    entry = "";
                }

                entry += line + "\n";
                j++;

                last_entry = entry;
            }

            entries.Add (last_entry);

            Regex regex = new Regex (@"changeset: ([a-z0-9]{40})\n" +
                                      "(.+) <(.+)>\n" +
                                      "([0-9]{4})-([0-9]{2})-([0-9]{2}) ([0-9]{2}):([0-9]{2}) .([0-9]{4})\n" +
                                      "", RegexOptions.Compiled);

            // TODO: Need to optimise for speed
            foreach (string log_entry in entries) {

                bool is_merge_commit = false;

                Match match = regex.Match (log_entry);

                if (match.Success) {
                    SparkleChangeSet change_set = new SparkleChangeSet ();

                    change_set.Revision  = match.Groups [1].Value;
                    change_set.UserName  = match.Groups [2].Value;
                    change_set.UserEmail = match.Groups [3].Value;
                    change_set.IsMerge   = is_merge_commit;

                    change_set.Timestamp = new DateTime (int.Parse (match.Groups [4].Value),
                        int.Parse (match.Groups [5].Value), int.Parse (match.Groups [6].Value),
                        int.Parse (match.Groups [7].Value), int.Parse (match.Groups [8].Value), 0);

                    string [] entry_lines = log_entry.Split ("\n".ToCharArray ());

                    foreach (string entry_line in entry_lines) {
                        if (entry_line.StartsWith (":")) {

                            string change_type = entry_line [37].ToString ();
                            string file_path   = entry_line.Substring (39);
                            string to_file_path;

                            if (change_type.Equals ("A")) {
                                change_set.Added.Add (file_path);
                            } else if (change_type.Equals ("M")) {
                                change_set.Edited.Add (file_path);
                            } else if (change_type.Equals ("D")) {
                                change_set.Deleted.Add (file_path);
                            } else if (change_type.Equals ("R")) {
                                int tab_pos  = entry_line.LastIndexOf ("\t");
                                file_path    = entry_line.Substring (42, tab_pos - 42);
                                to_file_path = entry_line.Substring (tab_pos + 1);

                                change_set.MovedFrom.Add (file_path);
                                change_set.MovedTo.Add (to_file_path);
                            }
                        }
                    }

                    change_sets.Add (change_set);
                }
            }

            return change_sets;
        }


        // Creates a pretty commit message based on what has changed
        private string FormatCommitMessage () // TODO
        {
            return "SparkleShare Hg";
        }


        public override void CreateInitialChangeSet ()
        {
            base.CreateInitialChangeSet ();
            Add ();

            string message = FormatCommitMessage ();
            Commit (message);
        }


        new public static bool IsRepo (string path)
        {
            return System.IO.Directory.Exists (Path.Combine (path, ".hg"));
        }


        public override bool UsesNotificationCenter
        {
            get {
                string file_path = SparkleHelpers.CombineMore (LocalPath, ".hg", "disable_notification_center");
                return !File.Exists (file_path);
            }
        }
    }
}
