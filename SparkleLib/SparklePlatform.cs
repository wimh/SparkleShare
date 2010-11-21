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

namespace SparkleLib {
	
	public static class SparklePlatform {

		// Detect OSX, Windows, GNOME or KDE here
		//public static string Name = "GNOME";

		/// <summary>
		/// true if any variation of Windows
		/// </summary>
		public static bool IsWindows = DetectWindows ();

		private static bool DetectWindows()
		{
			PlatformID platform = Environment.OSVersion.Platform;
			if (platform == PlatformID.Win32NT
				|| platform == PlatformID.Win32S
				|| platform == PlatformID.Win32Windows
				|| platform == PlatformID.WinCE) {
				return true;
			}
			return false;
		}
	}

}
