using System.Collections.Generic;
using com.ktgame.core.di;
using com.ktgame.foundation.extensions.unity;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace com.ktgame.core
{
	public partial class Architecture
	{
		protected Injector InjectorInternal;

		private void ResolveSceneDependencies(Scene scene)
		{
			using var pooledObject = ListPool<GameObject>.Get(out var rootGameObjects);
			scene.GetRootGameObjects(rootGameObjects);

			using var pooledMonoBehaviours = ListPool<MonoBehaviour>.Get(out var monoBehaviours);

			foreach (var rootGameObject in rootGameObjects)
			{
				rootGameObject.GetComponentsInChildren<MonoBehaviour>(true, monoBehaviours);
				foreach (var monoBehaviour in monoBehaviours)
				{
					if (monoBehaviour != null)
					{
						InjectorInternal.Resolve(monoBehaviour);
					}
				}
				monoBehaviours.Clear();
			}
		}
	}
}
