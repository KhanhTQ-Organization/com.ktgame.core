using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using com.ktgame.foundation;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace com.ktgame.core
{
	public partial class Architecture
	{
		private readonly List<IInitializable> _initializables = new List<IInitializable>();
		private readonly List<IGUI> _guis = new List<IGUI>();
		private readonly List<IUpdatable> _updatables = new List<IUpdatable>();
		private readonly List<IFixedUpdatable> _fixedUpdatables = new List<IFixedUpdatable>();
		private readonly List<ILateUpdatable> _lateUpdatables = new List<ILateUpdatable>();
		private readonly List<ISceneLoad> _sceneLoads = new List<ISceneLoad>();
		private readonly List<IPausable> _pausables = new List<IPausable>();
		private readonly List<IFocusable> _focusables = new List<IFocusable>();
		private readonly List<IDestructible> _destructibles = new List<IDestructible>();
		private readonly List<IQuitable> _quitables = new List<IQuitable>();
#if UNITY_ANDROID || UNITY_IOS
		private readonly List<ILowMemory> _lowMemories = new List<ILowMemory>();
#endif
		private readonly Dictionary<Type, IService> _allServices = new Dictionary<Type, IService>();
		private IArchitecture _architecture;

		protected readonly List<Type> ServiceTypes = new List<Type>();

		/// <summary>Startup timings of this architecture (see Ktgame > Dependency Container Debugger > Startup).</summary>
		public ArchitectureInitReport InitReport { get; internal set; }

		private static Type[] _candidateTypes;

		// All types of Assembly-CSharp and com.ktgame.* assemblies, scanned once and shared with ArchitectureInstaller.
		internal static Type[] CandidateTypes => _candidateTypes ??= ScanCandidateTypes();

		private static Type[] ScanCandidateTypes()
		{
			var watch = System.Diagnostics.Stopwatch.StartNew();
			var prefixes = new[] { "Assembly-CSharp", "com.ktgame" };
			var types = AppDomain.CurrentDomain.GetAssemblies()
				.Where(a => prefixes.Any(p => a.FullName.StartsWith(p)))
				.SelectMany(GetLoadableTypes)
				.ToArray();
			ArchitectureInitReport.ScanTypesMs = watch.Elapsed.TotalMilliseconds;
			return types;
		}

		private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
		{
			try
			{
				return assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException e)
			{
				return e.Types.Where(t => t != null);
			}
		}

		protected void GetAllServices(IArchitecture architecture)
		{
			var watch = System.Diagnostics.Stopwatch.StartNew();
			_architecture = architecture;
			var services = CandidateTypes
				.Where(t => t.IsClass && typeof(IService).IsAssignableFrom(t))
				.ToArray();

			foreach (var service in services)
			{
				ServiceTypes.Add(service);
			}

			if (InitReport != null)
			{
				InitReport.ScanServicesMs = watch.Elapsed.TotalMilliseconds;
			}
		}

		protected async UniTask ServiceInstaller(IArchitecture architecture)
		{
			var phaseWatch = System.Diagnostics.Stopwatch.StartNew();
			var createdServices = new List<IService>();
			foreach (var service in ServiceTypes)
			{
				var attribute = service.GetCustomAttribute<ServiceAttribute>();
				if (attribute != null)
				{
					IService serviceInstance = null;
					var registered = false;
					try
					{
						serviceInstance = CreateService(service);
						registered = RegisterService(attribute.ServiceType, serviceInstance, false);
						if (registered)
						{
							createdServices.Add(serviceInstance);
						}
					}
					catch (Exception e)
					{
						Debug.LogError($"[Architecture] Failed to create service {service.Name}: {e}");
					}

					if (!registered)
					{
						DestroyRejectedService(serviceInstance);
					}
				}
			}

			if (InitReport != null)
			{
				InitReport.CreateMs = phaseWatch.Elapsed.TotalMilliseconds;
			}

			phaseWatch.Restart();
			foreach (var serviceInstance in createdServices)
			{
				try
				{
					InjectorInternal.Resolve(serviceInstance);
				}
				catch (Exception e)
				{
					Debug.LogError($"[Architecture] Inject error in {serviceInstance.GetType().Name}: {e}");
				}
			}

			if (InitReport != null)
			{
				InitReport.InjectMs = phaseWatch.Elapsed.TotalMilliseconds;
			}

			var settings = ArchitectureSettings.Instance;
			var foreground = new List<KeyValuePair<Type, IService>>();
			var background = new List<KeyValuePair<Type, IService>>();
			foreach (var service in _allServices.OrderBy(s => s.Value.Priority))
			{
				if (!(service.Value is IInitializable))
				{
					continue;
				}

				if (settings != null && settings.IsBackground(service.Key, service.Value.GetType()))
				{
					background.Add(service);
					_pendingBackgroundServices.Add(service.Value);
				}
				else
				{
					foreground.Add(service);
				}
			}

			if (background.Count > 0)
			{
				if (InitReport != null)
				{
					InitReport.HasBackground = true;
				}

				InitializeBackgroundServicesAsync(background, architecture).Forget();
			}
			else
			{
				_backgroundServicesReady = true;
			}

			for (var i = 0; i < foreground.Count; i++)
			{
				await InitializeServiceAsync(foreground[i], architecture, i, false);
			}
		}

		private async UniTaskVoid InitializeBackgroundServicesAsync(List<KeyValuePair<Type, IService>> services, IArchitecture architecture)
		{
			try
			{
				for (var i = 0; i < services.Count; i++)
				{
					await InitializeServiceAsync(services[i], architecture, i, true);
					_pendingBackgroundServices.Remove(services[i].Value);
				}
			}
			finally
			{
				_pendingBackgroundServices.Clear();
				_backgroundServicesReady = true;
				if (InitReport != null)
				{
					InitReport.BackgroundMs = InitReport.ElapsedMs;
					InitReport.BackgroundFrames = Time.frameCount - InitReport.StartFrame;
					InitReport.BackgroundDone = true;
				}
			}
		}

		private async UniTask InitializeServiceAsync(KeyValuePair<Type, IService> service, IArchitecture architecture, int index, bool background)
		{
			var initializable = (IInitializable)service.Value;
			var group = background ? "Background service" : "Service";
			Log.Info($"Initialize {group} {index}: [{service.Key.Name}] => [{service.Value.GetType().Name}]");
			var record = InitReport?.BeginService(service.Value.GetType().Name, service.Key.Name, service.Value.Priority, background, index);
			var failed = false;
			try
			{
				var task = initializable.OnInitialize(architecture);
				if (record != null)
				{
					ArchitectureInitReport.MarkSynchronousDone(record);
				}

				await task;
				initializable.Initialized = true;
			}
			catch (Exception e)
			{
				failed = true;
				Debug.LogError($"[Architecture] Initialize error in {service.Value.GetType().Name}: {e}");
			}

			if (record != null)
			{
				ArchitectureInitReport.EndService(record, failed);
			}
		}

		private IService CreateService(Type service)
		{
			if (typeof(MonoBehaviour).IsAssignableFrom(service))
			{
				var attribute = service.GetCustomAttribute<ServiceAttribute>();
				if (attribute != null && !string.IsNullOrEmpty(attribute.PrefabPath))
				{
					var prefab = Resources.Load<GameObject>(attribute.PrefabPath);
					if (prefab != null)
					{
						var inst = UnityEngine.Object.Instantiate(prefab, transform);
						inst.name = service.Name;
						var component = inst.GetComponent(service) as IService;
						if (component == null)
						{
							Debug.LogError($"[Architecture] Prefab '{attribute.PrefabPath}' has no {service.Name} component. Service skipped.");
							UnityEngine.Object.Destroy(inst);
						}

						return component;
					}
					else
					{
						Debug.LogWarning($"[Architecture] Prefab not found at path '{attribute.PrefabPath}' for service {service.Name}. Creating empty GameObject instead.");
					}
				}

				var go = new GameObject(service.Name);
				go.transform.SetParent(transform);
				return go.AddComponent(service) as IService;
			}

			if (typeof(ScriptableObject).IsAssignableFrom(service))
			{
				var obj = ScriptableObject.CreateInstance(service);
				return obj as IService;
			}

			return Activator.CreateInstance(service) as IService;
		}

		public bool HasService<TService>() where TService : IService
		{
			return _allServices.ContainsKey(typeof(TService));
		}

		public TService GetService<TService>() where TService : IService
		{
			if (_allServices.TryGetValue(typeof(TService), out var service))
			{
				return (TService)service;
			}

			return default;
		}

		public void RegisterService<TService>(TService service) where TService : IService
		{
			var type = typeof(TService);
			
			if (!RegisterService(type, service))
			{
				return;
			}

			if (service is IInitializable initializable)
			{
				InitializeRegisteredServiceAsync(initializable).Forget();
			}
		}

		private async UniTaskVoid InitializeRegisteredServiceAsync(IInitializable initializable)
		{
			try
			{
				await initializable.OnInitialize(_architecture);
				initializable.Initialized = true;
			}
			catch (Exception e)
			{
				Debug.LogError($"[Architecture] Initialize error in {initializable.GetType().Name}: {e}");
			}
		}

		public void UnregisterService<TService>() where TService : IService
		{
			var type = typeof(TService);
			if (_allServices.TryGetValue(type, out var service))
			{
				if (service is IInitializable initializable)
				{
					_initializables.Remove(initializable);
				}

				if (service is IUpdatable updatable)
				{
					_updatables.Remove(updatable);
				}

				if (service is IFixedUpdatable fixedUpdatable)
				{
					_fixedUpdatables.Remove(fixedUpdatable);
				}

				if (service is ILateUpdatable lateUpdatable)
				{
					_lateUpdatables.Remove(lateUpdatable);
				}

				if (service is IGUI gui)
				{
					_guis.Remove(gui);
				}

				if (service is ISceneLoad sceneLoad)
				{
					_sceneLoads.Remove(sceneLoad);
				}

				if (service is IPausable pausable)
				{
					_pausables.Remove(pausable);
				}

				if (service is IFocusable focusable)
				{
					_focusables.Remove(focusable);
				}

				if (service is IDestructible destructible)
				{
					_destructibles.Remove(destructible);
				}

				if (service is IQuitable quitable)
				{
					_quitables.Remove(quitable);
				}

#if UNITY_ANDROID || UNITY_IOS
				if (service is ILowMemory lowMemory)
				{
					_lowMemories.Remove(lowMemory);
				}
#endif
				_allServices.Remove(type);
			}
			else
			{
				Debug.LogError($"[{GetType().Name}] Object '{type.Name}' is not a valid service");
			}

			// if (DependencyContainer.Contains(type))
			// {
			//     DependencyContainer.Remove(type);
			// }
		}

		private bool RegisterService<TService>(Type type, TService service, bool resolveNow = true) where TService : IService
		{
			if (service == null)
			{
				Debug.LogError($"[Architecture] Service '{type?.Name}' is null and was not registered.");
				return false;
			}

			if (type == null || !typeof(IService).IsAssignableFrom(type))
			{
				Debug.LogError($"[{GetType().Name}] Object '{type?.Name}' is not a valid service");
				return false;
			}

			if (!type.IsInstanceOfType(service))
			{
				Debug.LogError($"[Architecture] {service.GetType().Name} does not implement {type.Name} declared in [Service]. Service skipped.");
				return false;
			}

			if (_allServices.TryGetValue(type, out var existingService))
			{
				Debug.LogError($"[Architecture] Duplicate service '{type.Name}': {existingService.GetType().Name} is already registered, {service.GetType().Name} skipped.");
				return false;
			}

			if (service is IInitializable initializable)
			{
				_initializables.Add(initializable);
			}

			if (service is IUpdatable updatable)
			{
				_updatables.Add(updatable);
			}

			if (service is IFixedUpdatable fixedUpdatable)
			{
				_fixedUpdatables.Add(fixedUpdatable);
			}

			if (service is ILateUpdatable lateUpdatable)
			{
				_lateUpdatables.Add(lateUpdatable);
			}

			if (service is IGUI gui)
			{
				_guis.Add(gui);
			}

			if (service is ISceneLoad sceneLoad)
			{
				_sceneLoads.Add(sceneLoad);
			}

			if (service is IPausable pausable)
			{
				_pausables.Add(pausable);
			}

			if (service is IFocusable focusable)
			{
				_focusables.Add(focusable);
			}

			if (service is IDestructible destructible)
			{
				_destructibles.Add(destructible);
			}

			if (service is IQuitable quitable)
			{
				_quitables.Add(quitable);
			}

#if UNITY_ANDROID || UNITY_IOS
			if (service is ILowMemory lowMemory)
			{
				_lowMemories.Add(lowMemory);
			}
#endif

			_allServices.Add(type, service);
			if (resolveNow)
			{
				InjectorInternal.Resolve(service);
			}
			InjectorInternal.AddSingleton(service, type);
			return true;
		}

		private static void DestroyRejectedService(IService service)
		{
			if (service is Component component && component != null)
			{
				UnityEngine.Object.Destroy(component.gameObject);
			}
			else if (service is ScriptableObject scriptableObject && scriptableObject != null)
			{
				UnityEngine.Object.Destroy(scriptableObject);
			}
		}
	}
}
