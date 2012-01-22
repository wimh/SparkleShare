# Build on windows

* Install [.NET Framework 4.0](http://www.microsoft.com/download/en/details.aspx?id=17851) (if not installed yet)

* Install [msysGit](http://code.google.com/p/msysgit/downloads/detail?name=Git-1.7.8-preview20111206.exe)
  *  I recommend you to install in `C:\msysgit`
  *  Use default settings for all other questions during installation

* _Clone step_: Open a Git console (available in Start Menu > Git > Git Bash). On the command line write

        cd /c
        git clone -b gettext-cs git://github.com/serras/SparkleShare.git
        cd SparkleShare
        git submodule update --init

* This way you will get the SparkleShare source code in `C:\SparkleShare`

* Download [CefSharp-0.3.1.7z](https://github.com/downloads/chillitom/CefSharp/CefSharp-0.3.1.7z)
  * Copy `avcodec-52.dll`, `avformat-52.dll`, `avutil-50.dll`, `CefSharp.dll`, `icudt42.dll` and `libcef.dll` from the 7z file in `CefSharp-0.3.1\Release\` to `c:\SparkleShare\bin` (create that directory if it does not exist)

* Copy the entire contents of the msysGit folder to `C:\SparkleShare\bin\msysgit`

* _Build step_: Open a command shell (available in Start Menu > Accessories > Command Prompt) and execute

        C:
        cd C:\SparkleShare
        cd SparkleShare\Windows
        build

* `C:\SparkleShare\bin` should now contain `SparkleLib.dll` and `SparkleShare.exe`, apart from folders `plugins`, `po` and `msysgit`

* If you want to build the Windows installer download and install [WiX 3.6](http://wix.sourceforge.net/)

* _Installer build step_: Then open a command shell and write almost the same as before, but with `installer` at the end

        C:
        cd C:\SparkleShare
        cd SparkleShare\Windows
        build installer

Now, each time you would like to get the latest changes open a Git console and run

        cd /c/SparkleShare
        git pull
        git submodule update

and then run the build step and/or build installer step in the command shell.
