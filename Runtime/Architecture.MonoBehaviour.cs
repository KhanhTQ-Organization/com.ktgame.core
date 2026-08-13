using UnityEngine;
using UnityEngine.SceneManagement;

namespace com.ktgame.core
{
	[DisallowMultipleComponent]
	public partial class Architecture : MonoBehaviour
	{
		private bool WillDestroy { get; set; }

		protected virtual void OnUpdate() { }

		protected virtual void OnFixedUpdate() { }

		protected virtual void OnLateUpdate() { }

		protected virtual void OnWillDestroy() { }

		private void Update()
		{
			if (_architecture == null)
			{
				return;
			}

			if (_architecture.IsInitialized)
			{
				if (!WillDestroy)
				{
					OnUpdate();

					for (int i = _updatables.Count - 1; i >= 0; i--)
					{
						try { _updatables[i].OnUpdate(); }
						catch (System.Exception e) { Debug.LogError($"[Architecture] Update error in {_updatables[i].GetType().Name}: {e}"); }
					}
				}
			}
		}

		private void FixedUpdate()
		{
			if (_architecture == null)
			{
				return;
			}

			if (_architecture.IsInitialized)
			{
				if (!WillDestroy)
				{
					OnFixedUpdate();

					for (int i = _fixedUpdatables.Count - 1; i >= 0; i--)
					{
						try { _fixedUpdatables[i].OnFixedUpdate(); }
						catch (System.Exception e) { Debug.LogError($"[Architecture] FixedUpdate error in {_fixedUpdatables[i].GetType().Name}: {e}"); }
					}
				}
			}
		}

		private void LateUpdate()
		{
			if (_architecture == null)
			{
				return;
			}

			if (_architecture.IsInitialized)
			{
				if (!WillDestroy)
				{
					OnLateUpdate();

					for (int i = _lateUpdatables.Count - 1; i >= 0; i--)
					{
						try { _lateUpdatables[i].OnLateUpdate(); }
						catch (System.Exception e) { Debug.LogError($"[Architecture] LateUpdate error in {_lateUpdatables[i].GetType().Name}: {e}"); }
					}
				}
			}
		}

		private void OnDestroy()
		{
			SceneManager.sceneLoaded -= OnSceneLoaded;
			SceneManager.sceneUnloaded -= OnSceneUnloaded;

			WillDestroy = true;
			for (int i = _destructibles.Count - 1; i >= 0; i--)
			{
				try
				{
					_destructibles[i].WillDestroy = true;
					_destructibles[i].OnWillDestroy();
				}
				catch (System.Exception e) { Debug.LogError($"[Architecture] OnDestroy error in {_destructibles[i].GetType().Name}: {e}"); }
			}

			OnWillDestroy();

			_initializables.Clear();
			_guis.Clear();
			_updatables.Clear();
			_fixedUpdatables.Clear();
			_lateUpdatables.Clear();
			_destructibles.Clear();
			_sceneLoads.Clear();
			_pausables.Clear();
			_focusables.Clear();
			_quitables.Clear();
#if UNITY_ANDROID || UNITY_IOS
			_lowMemories.Clear();
#endif
			_allServices.Clear();
			InjectorInternal.Dispose();

#if UNITY_ANDROID || UNITY_IOS
			Application.lowMemory -= OnLowMemory;
#endif
			Application.wantsToQuit -= OnWantsToQuit;
		}

#if UNITY_EDITOR
		private void OnDrawGizmos()
		{
			for (int i = _guis.Count - 1; i >= 0; i--)
			{
				try { _guis[i].OnGizmos(); }
				catch (System.Exception e) { Debug.LogError($"[Architecture] OnDrawGizmos error in {_guis[i].GetType().Name}: {e}"); }
			}
		}
#endif

		private void OnGUI()
		{
			for (int i = _guis.Count - 1; i >= 0; i--)
			{
				try { _guis[i].OnGUI(); }
				catch (System.Exception e) { Debug.LogError($"[Architecture] OnGUI error in {_guis[i].GetType().Name}: {e}"); }
			}
		}

		private void OnApplicationPause(bool pause)
		{
			for (int i = _pausables.Count - 1; i >= 0; i--)
			{
				try { _pausables[i].OnAppPause(pause); }
				catch (System.Exception e) { Debug.LogError($"[Architecture] OnApplicationPause error in {_pausables[i].GetType().Name}: {e}"); }
			}
		}

		private void OnApplicationFocus(bool focus)
		{
			for (int i = _focusables.Count - 1; i >= 0; i--)
			{
				try { _focusables[i].OnAppFocus(focus); }
				catch (System.Exception e) { Debug.LogError($"[Architecture] OnApplicationFocus error in {_focusables[i].GetType().Name}: {e}"); }
			}
		}

		private void OnApplicationQuit()
		{
			WillDestroy = true;
			for (int i = _destructibles.Count - 1; i >= 0; i--)
			{
				try { _destructibles[i].WillDestroy = true; }
				catch (System.Exception e) { Debug.LogError($"[Architecture] OnApplicationQuit (destructible) error in {_destructibles[i].GetType().Name}: {e}"); }
			}

			for (int i = _quitables.Count - 1; i >= 0; i--)
			{
				try { _quitables[i].OnAppQuit(); }
				catch (System.Exception e) { Debug.LogError($"[Architecture] OnApplicationQuit error in {_quitables[i].GetType().Name}: {e}"); }
			}
		}

		protected void OnSceneLoaded(Scene current, LoadSceneMode mode)
		{
			for (int i = _sceneLoads.Count - 1; i >= 0; i--)
			{
				try { _sceneLoads[i].OnSceneLoad(current.name); }
				catch (System.Exception e) { Debug.LogError($"[Architecture] OnSceneLoad error in {_sceneLoads[i].GetType().Name}: {e}"); }
			}

			if (_architecture.InjectSceneLoadedDependencies)
			{
				ResolveSceneDependencies(current);
			}
		}

		protected void OnSceneUnloaded(Scene current)
		{
			for (int i = _sceneLoads.Count - 1; i >= 0; i--)
			{
				try { _sceneLoads[i].OnSceneUnload(current.name); }
				catch (System.Exception e) { Debug.LogError($"[Architecture] OnSceneUnload error in {_sceneLoads[i].GetType().Name}: {e}"); }
			}
		}

		protected bool OnWantsToQuit()
		{
			for (int i = _initializables.Count - 1; i >= 0; i--)
			{
				try
				{
					if (_initializables[i].Initialized)
					{
						_initializables[i].Initialized = false;
					}
				}
				catch (System.Exception e) { Debug.LogError($"[Architecture] OnWantsToQuit error in {_initializables[i].GetType().Name}: {e}"); }
			}

			WillDestroy = true;
			return true;
		}

#if UNITY_ANDROID || UNITY_IOS
		protected void OnLowMemory()
		{
			for (int i = _lowMemories.Count - 1; i >= 0; i--)
			{
				try { _lowMemories[i].OnLowMemory(); }
				catch (System.Exception e) { Debug.LogError($"[Architecture] OnLowMemory error in {_lowMemories[i].GetType().Name}: {e}"); }
			}
		}
#endif
	}
}
