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
//   along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Diagnostics;
using System.IO;
using Mono.Unix;

namespace SparkleLib {

	public interface ISparklePaths
	{
		string GitPath { get; }

		string HomePath { get; }

		string SparklePath { get; }

		string SparkleTmpPath { get; }

		string SparkleConfigPath { get; }

		string SparkleKeysPath { get; }

		string SparkleInstallPath { get; }

		string SparkleLocalIconPath { get; }

		string SparkleIconPath { get; }
	}

	public class SparklePaths : ISparklePaths
	{

		public string GitPath { get { return GetGitPath (); } }

		public string HomePath { get { return GetHomePath (); } }

		public string SparklePath { get { return Path.Combine (HomePath, "SparkleShare"); } }

		public string SparkleTmpPath { get { return Path.Combine (SparklePath, ".tmp"); } }

		public string SparkleConfigPath { get { return SparkleHelpers.CombineMore (HomePath, ".config", "sparkleshare"); } }

		public string SparkleKeysPath { get { return SparkleHelpers.CombineMore (HomePath, ".config", "sparkleshare"); } }

		public string SparkleInstallPath { get { return SparkleHelpers.CombineMore (Defines.PREFIX, "sparkleshare"); } }

		public string SparkleLocalIconPath { get { return SparkleHelpers.CombineMore (SparkleConfigPath, "icons", "hicolor"); } }

		public string SparkleIconPath
		{
			get
			{
				return SparkleHelpers.CombineMore (Defines.DATAROOTDIR, "sparkleshare", "icons");
			}
		}


		private static string GetHomePath ()
		{
			if (SparklePlatform.IsWindows) {
				return Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);
			}

			// Asume Unix
			UnixUserInfo UnixUserInfo = new UnixUserInfo (UnixEnvironment.UserName);

			return UnixUserInfo.HomeDirectory;
		}

		private static string GetGitPath ()
		{

			if (SparklePlatform.IsWindows)
				return "git";

			Process process = new Process ();

			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.FileName = "which";
			process.StartInfo.Arguments = "git";
			process.Start ();

			string git_path = process.StandardOutput.ReadToEnd ().Trim ();

			if (!string.IsNullOrEmpty (git_path))
				return git_path;
			else
				return null;

		}

	}

}
