using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace com.ktgame.core
{
	public partial class Architecture
	{
		private readonly HashSet<IService> _pendingBackgroundServices = new HashSet<IService>();
		private bool _backgroundServicesReady;

		/// <summary>
		/// True once every service listed in <see cref="ArchitectureSettings.BackgroundServices"/> has finished initializing
		/// (also true when there is none).
		/// </summary>
		public bool BackgroundServicesReady => _backgroundServicesReady;

		private bool IsPendingBackground(object service)
		{
			return _pendingBackgroundServices.Count > 0
			       && service is IService typedService
			       && _pendingBackgroundServices.Contains(typedService);
		}
	}

	public static class ArchitectureBackgroundExtensions
	{
		/// <summary>
		/// True when the architecture is initialized and its background services are ready.
		/// </summary>
		public static bool AreBackgroundServicesReady(this IArchitecture architecture)
		{
			if (architecture == null || !architecture.IsInitialized)
			{
				return false;
			}

			return !(architecture is Architecture concrete) || concrete.BackgroundServicesReady;
		}

		/// <summary>
		/// Waits until the architecture is initialized and its background services are ready.
		/// </summary>
		public static UniTask WaitForBackgroundServicesAsync(this IArchitecture architecture, CancellationToken cancellationToken = default)
		{
			return UniTask.WaitUntil(() => architecture.AreBackgroundServicesReady(), cancellationToken: cancellationToken);
		}
	}
}
