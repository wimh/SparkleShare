using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ninject.Modules;
using SparkleLib;

namespace SparkleShare
{
	class SparkleModule : NinjectModule 
	{

		public override void Load ()
		{
			Bind<ISparklePaths> ().To<SparklePaths> ().InSingletonScope ();
			Bind (typeof (IFactory<SparkleRepo, string>)).To (typeof (SparkleRepoFactory));
			Bind (typeof (IFactory<SparkleFetcher, string, string>)).To (typeof (SparkleFetcherFactory));

			if (Environment.OSVersion.Platform == PlatformID.Win32NT)
				Bind<SparkleController> ().To<SparkleWinController> ();

			Bind<SparkleUIHelpers> ().ToSelf ();

			Bind<SparkleUI> ().ToSelf ();
			Bind<SparkleInfobar> ().ToSelf ();
			Bind<SparkleSpinner> ().ToSelf ();
			//Bind<SparkleDialog> ().ToSelf ();
			//Bind<SparkleStatusIcon> ().ToSelf ();
			//Bind<SparkleLog> ().ToSelf ();
			//Bind<SparkleIntro> ().ToSelf ();

			Bind (typeof (IFactory<SparkleStatusIcon>)).To (typeof (Factory<SparkleStatusIcon>));

			Bind (typeof (IFactory<SparkleIntro>)).To (typeof (Factory<SparkleIntro>));
			Bind (typeof (IFactory<SparkleDialog>)).To (typeof (Factory<SparkleDialog>));

			Bind (typeof (IFactory<SparkleSpinner, int>)).To (typeof (Factory<SparkleSpinner, int>)).WithConstructorArgument ("n1", "size");
			Bind (typeof (IFactory<SparkleLog, string>)).To (typeof (ConstructorInstanceFactory<SparkleLog, string>)).WithConstructorArgument ("n1", "path");

			Bind (typeof (IFactory<SparkleInfobar, string, string, string>)).To (typeof (Factory<SparkleInfobar, string, string, string>))
								.WithConstructorArgument ("n1", "icon_name")
								.WithConstructorArgument ("n2", "title")
								.WithConstructorArgument ("n3", "text");
		}

	}
}
