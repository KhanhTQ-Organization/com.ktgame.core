using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace com.ktgame.core
{
	internal sealed class ArchitectureInstaller
	{
		private const RuntimeInitializeLoadType InitializeLoadType = RuntimeInitializeLoadType.AfterSceneLoad;

		[RuntimeInitializeOnLoadMethod(InitializeLoadType)]
		private static void OnLoad()
		{
			var prefixes = new[] { "Assembly-CSharp", "com.ktgame" };
			var assemblies = AppDomain.CurrentDomain.GetAssemblies()
				.Where(a => prefixes.Any(p => a.FullName.StartsWith(p)));
			var architectureBaseType = typeof(Architecture<>);
			var derivedType = assemblies
				.SelectMany(assembly => assembly.GetTypes())
				.FirstOrDefault(t =>
					t.BaseType != null &&
					t.BaseType.IsGenericType &&
					t.BaseType.GetGenericTypeDefinition() == architectureBaseType);

			if (derivedType == null)
			{
				return;
			}

			var specificType = architectureBaseType.MakeGenericType(derivedType);
			var instanceProperty = specificType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
			instanceProperty?.GetValue(null);
		}
	}
}
