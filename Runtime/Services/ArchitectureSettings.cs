using System;
using System.Collections.Generic;
using UnityEngine;

namespace com.ktgame.core
{
	public class ArchitectureSettings : ServiceSettingsSingleton<ArchitectureSettings>
	{
		public override string PackageName => "com.ktgame.core";

		[SerializeField]
		[Tooltip("Services initialized in the background: Architecture.IsInitialized (and so the first scene) does not wait for them. " +
		         "They still initialize one after another in Priority order. Use the service or interface name, e.g. FirebaseService or IFirebaseService. " +
		         "Wait for them with IArchitecture.WaitForBackgroundServicesAsync() before using them. Empty = everything blocks (default).")]
		private List<string> backgroundServices = new List<string>();

		public IReadOnlyList<string> BackgroundServices => backgroundServices;

		public bool IsBackground(Type serviceType, Type implementationType)
		{
			if (backgroundServices == null || backgroundServices.Count == 0)
			{
				return false;
			}

			foreach (var entry in backgroundServices)
			{
				if (string.IsNullOrWhiteSpace(entry))
				{
					continue;
				}

				var name = entry.Trim();
				if (name == serviceType.Name || name == serviceType.FullName
				    || name == implementationType.Name || name == implementationType.FullName)
				{
					return true;
				}
			}

			return false;
		}
	}
}
