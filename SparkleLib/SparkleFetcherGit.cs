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
using System.IO;
using System.Diagnostics;
using System.Xml;

namespace SparkleLib {

    // Sets up a fetcher that can get remote folders
    public class SparkleFetcherGit : SparkleFetcherBase {

        public SparkleFetcherGit (string remote_url, string target_folder) :
            base (remote_url, target_folder) { }


        public override bool Fetch ()
        {
            SparkleGit git = new SparkleGit (SparklePaths.SparkleTmpPath,
                "clone \"" + base.remote_url + "\" " + "\"" + base.target_folder + "\"");

            git.Start ();
            git.WaitForExit ();

            SparkleHelpers.DebugInfo ("Git", "Exit code " + git.ExitCode.ToString ());

            if (git.ExitCode != 0) {
                return false;
            } else {
                InstallConfiguration ();
                InstallExcludeRules ();
                return true;
            }
        }


        // Install the user's name and email and some config into
        // the newly cloned repository
        private void InstallConfiguration ()
        {
            string global_config_file_path = Path.Combine (SparklePaths.SparkleConfigPath, "config.xml");

            if (!File.Exists (global_config_file_path))
                return;

            string repo_config_file_path = SparkleHelpers.CombineMore (base.target_folder, ".git", "config");
            string config = String.Join (Environment.NewLine, File.ReadAllLines (repo_config_file_path));

            // Be case sensitive explicitly to work on Mac
            config = config.Replace ("ignorecase = true", "ignorecase = false");

            // Ignore permission changes
            config = config.Replace ("filemode = true", "filemode = false");

            // Add user info
            string n        = Environment.NewLine;
            XmlDocument xml = new XmlDocument();
            xml.Load (global_config_file_path);

            XmlNode node_name  = xml.SelectSingleNode ("//user/name/text()");
            XmlNode node_email = xml.SelectSingleNode ("//user/email/text()");

            // TODO: just use commands instead of messing with the config file
            config += n +
                      "[user]" + n +
                      "\tname  = " + node_name.Value + n +
                      "\temail = " + node_email.Value + n;

            // Write the config to the file
            TextWriter writer = new StreamWriter (repo_config_file_path);
            writer.WriteLine (config);
            writer.Close ();

            SparkleHelpers.DebugInfo ("Config", "Added configuration to '" + repo_config_file_path + "'");
        }


        // Add a .gitignore file to the repo
        private void InstallExcludeRules ()
        {
            string exlude_rules_file_path = SparkleHelpers.CombineMore (
                this.target_folder, ".git", "info", "exclude");

            TextWriter writer = new StreamWriter (exlude_rules_file_path);

                // gedit and emacs
                writer.WriteLine ("*~");

                // vi(m)
                writer.WriteLine (".*.sw[a-z]");
                writer.WriteLine ("*.un~");
                writer.WriteLine ("*.swp");
                writer.WriteLine ("*.swo");
                
                // KDE
                writer.WriteLine (".directory");
    
                // Mac OSX
                writer.WriteLine (".DS_Store");
                writer.WriteLine ("Icon?");
                writer.WriteLine ("._*");
                writer.WriteLine (".Spotlight-V100");
                writer.WriteLine (".Trashes");

                // Mac OSX
                writer.WriteLine ("*(Autosaved).graffle");
            
                // Windows
                writer.WriteLine ("Thumbs.db");
                writer.WriteLine ("Desktop.ini");

                // CVS
                writer.WriteLine ("*/CVS/*");
                writer.WriteLine (".cvsignore");
                writer.WriteLine ("*/.cvsignore");
                
                // Subversion
                writer.WriteLine ("/.svn/*");
                writer.WriteLine ("*/.svn/*");

            writer.Close ();
        }
    }
}
