using System;
using System.Collections.Generic;

namespace com.ktgame.core.manager
{
	public interface IManagerInstaller
	{
		IEnumerable<IManager> Managers { get; }
		IReadOnlyDictionary<Type, IManager> Bindings { get; }

		IManagerInstaller Binding<TManager>(TManager instance) where TManager : IManager;

		IManagerInstaller Binding<TManager>(Type type, TManager instance) where TManager : IManager;

		TManager GetManager<TManager>() where TManager : IManager;

		void Clear();
	}
}
