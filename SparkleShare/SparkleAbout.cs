//   SparkleShare, an instant update workflow to Git.
//   Copyright (C) 2010  Hylke Bons (hylkebons@gmail.com)
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
//   along with this program. If not, see (http://www.gnu.org/licenses/).


using System;
using System.Diagnostics;
using System.IO;
using System.Net;

using Gtk;
using SparkleLib;
using Mono.Unix;

namespace SparkleShare {

    public class SparkleAbout : Window {

        private Label Version;


        // Short alias for the translations
        public static string _(string s)
        {
            return Catalog.GetString (s);
        }


        public SparkleAbout () : base ("")
        {
            DefaultSize    = new Gdk.Size (360, 260);
            BorderWidth    = 0;
            IconName       = "folder-sparkleshare";
            WindowPosition = WindowPosition.Center;
            Title          = "About SparkleShare";
            Resizable      = false;

            CreateAbout ();

            SparkleShare.Controller.NewVersionAvailable += delegate (string new_version) {
                Application.Invoke (delegate {
                    Version.Markup = "<small><span fgcolor='#f57900'>A newer version (" + new_version + ") is available!</span></small>";
                    Version.ShowAll ();
                });
            };

            SparkleShare.Controller.VersionUpToDate += delegate {
                Application.Invoke (delegate {
                    Version.Markup = "<small><span fgcolor='#4e9a06'>You are running the latest version.</span></small>";
                    Version.ShowAll ();
                });
            };

            SparkleShare.Controller.CheckForNewVersion ();
        }


        private void CreateAbout ()
        {
            Gdk.Color color = Style.Foreground (StateType.Insensitive);
            string secondary_text_color = SparkleUIHelpers.GdkColorToHex (color);

            EventBox box = new EventBox ();
            box.ModifyBg (StateType.Normal, new TreeView ().Style.Base (StateType.Normal));

                Label header = new Label () {
                    Markup = "<span font_size='xx-large'>SparkleShare</span>\n<span fgcolor='" + secondary_text_color + "'><small>" + Defines.VERSION + "</small></span>",
                    Xalign = 0,
                    Xpad = 18,
                    Ypad = 18
                };

            box.Add (header);

            Version = new Label () {
                Markup = "<small>Checking for updates...</small>",
                Xalign = 0,
                Xpad   = 18,
                Ypad   = 22,
            };

            Label license = new Label () {
                Xalign = 0,
                Xpad   = 18,
                Ypad   = 22,
                LineWrap     = true,
                Wrap         = true,
                LineWrapMode = Pango.WrapMode.Word,

                Markup = "<small>Copyright © 2010–" + DateTime.Now.Year + " Hylke Bons and others\n" +
                         "\n" +
                         "SparkleShare is Free and Open Source Software. " +
                         "You are free to use, modify, and redistribute it " +
                         "under the terms of the GNU General Public License version 3 or later.</small>"
            };

            VBox vbox = new VBox (false, 0) {
                BorderWidth = 0
            };

                HButtonBox button_bar = new HButtonBox () {
                    BorderWidth = 12
                };

                Button credits_button = new Button (_("_Show Credits")) {
                    UseUnderline = true
                };

                    credits_button.Clicked += delegate {

                        Process process             = new Process ();
                        process.StartInfo.FileName  = "xdg-open";
                        process.StartInfo.Arguments = "http://www.sparkleshare.org/credits";
                        process.Start ();

                    };

                    Button website_button = new Button (_("_Visit Website")) {
                        UseUnderline = true
                    };

                    website_button.Clicked += delegate {

                        Process process = new Process ();
                        process.StartInfo.FileName = "xdg-open";
                        process.StartInfo.Arguments = "http://www.sparkleshare.org/";
                        process.Start ();

                    };

                button_bar.Add (website_button);
                button_bar.Add (credits_button);

            vbox.PackStart (box, true, true, 0);
            vbox.PackStart (Version, false, false, 0);
            vbox.PackStart (license, true, true, 0);
            vbox.PackStart (button_bar, false, false, 0);

            Add (vbox);
        }
    }
}
