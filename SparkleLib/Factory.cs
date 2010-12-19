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
using System.Linq;
using Ninject;
using Ninject.Parameters;
using Ninject.Activation;

namespace SparkleLib
{

	public interface IFactory<T>
	{
		T Get ();
	}

	public interface IFactory<T, P1>
	{
		T Get (P1 p1);
	}

	public interface IFactory<T, P1, P2>
	{
		T Get (P1 p1, P2 p2);
	}

	public interface IFactory<T, P1, P2, P3>
	{
		T Get (P1 p1, P2 p2, P3 p3);
	}

	public class Factory<T> : IFactory<T>
	{
		private readonly IKernel Kernel;

		public Factory (IKernel Kernel)
		{
			this.Kernel = Kernel;
		}

		public T Get ()
		{
			return Kernel.Get<T> ();
		}
	}

	public class Factory<T, P1> : IFactory<T, P1>
	{
		private readonly IKernel Kernel;
		private readonly string n1;

		public Factory (IKernel Kernel, string n1)
		{
			this.Kernel = Kernel;
			this.n1 = n1;
		}

		public T Get (P1 p1)
		{
			return Kernel.Get<T> (new ConstructorArgument (n1, p1));
		}
	}

	public class Factory<T, P1, P2, P3> : IFactory<T, P1, P2, P3>
	{
		private readonly IKernel Kernel;
		private readonly string n1;
		private readonly string n2;
		private readonly string n3;

		public Factory (IKernel Kernel, string n1, string n2, string n3)
		{
			this.Kernel = Kernel;
			this.n1 = n1;
			this.n2 = n2;
			this.n3 = n3;
		}

		public T Get (P1 p1, P2 p2, P3 p3)
		{
			return Kernel.Get<T> (new ConstructorArgument (n1, p1), new ConstructorArgument (n2, p2), new ConstructorArgument (n3, p3));
		}
	}

	public class ConstructorInstanceFactory<T, P1> : IFactory<T, P1>
	{
		private readonly IKernel Kernel;
		private readonly string n1;

		public ConstructorInstanceFactory (IKernel Kernel, string n1)
		{
			this.Kernel = Kernel;
			this.n1 = n1;
		}

		public object Scope (IContext Context)
		{
			var parameter = Context.Parameters.OfType<ConstructorArgument> ().Where (p => p.Name == n1).SingleOrDefault ();
			return parameter != null ? parameter.GetValue (Context, null) : "";
		}

		public T Get (P1 p1)
		{
			return Kernel.Get<T> (new ConstructorArgument (n1, p1));
		}
	}

}