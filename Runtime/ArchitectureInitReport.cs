using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace com.ktgame.core
{
	public sealed class ServiceInitRecord
	{
		public string ServiceName { get; internal set; }
		public string ContractName { get; internal set; }
		public int Priority { get; internal set; }
		public bool Background { get; internal set; }
		public int Order { get; internal set; }
		
		public double StartMs { get; internal set; }
		public double SyncMs { get; internal set; }
		public double TotalMs { get; internal set; }

		public int Frames { get; internal set; }
		public bool Completed { get; internal set; }
		public bool Failed { get; internal set; }

		/// <summary>Total time when completed, otherwise the time spent so far.</summary>
		public double ElapsedMs => Completed ? TotalMs : Watch.Elapsed.TotalMilliseconds;

		internal int StartFrame;
		internal readonly Stopwatch Watch = new Stopwatch();
	}
	
	public sealed class ArchitectureInitReport
	{
		public static ArchitectureInitReport Last { get; private set; }

		public string ArchitectureName { get; internal set; }
		public float StartRealtime { get; internal set; }
		public int StartFrame { get; internal set; }

		public static double ScanTypesMs { get; internal set; }
		public double ScanServicesMs { get; internal set; }
		public double CreateMs { get; internal set; }
		public double InjectMs { get; internal set; }
		public double PostInitializeMs { get; internal set; }

		/// <summary>Until Architecture.IsInitialized (everything except background services).</summary>
		public double InitializedMs { get; internal set; }
		public int InitializedFrames { get; internal set; }
		public bool InitializedDone { get; internal set; }

		public bool HasBackground { get; internal set; }
		public double BackgroundMs { get; internal set; }
		public int BackgroundFrames { get; internal set; }
		public bool BackgroundDone { get; internal set; }

		/// <summary>Longest frame seen while services were still starting.</summary>
		public double LongestFrameMs { get; private set; }
		public int LongestFrameNumber { get; private set; }

		public IReadOnlyList<ServiceInitRecord> Services => _services;

		public bool Finished => InitializedDone && (!HasBackground || BackgroundDone);

		public double ElapsedMs => _watch.Elapsed.TotalMilliseconds;

		private readonly List<ServiceInitRecord> _services = new List<ServiceInitRecord>();
		private readonly Stopwatch _watch = new Stopwatch();

		internal static ArchitectureInitReport Begin(string architectureName)
		{
			var report = new ArchitectureInitReport
			{
				ArchitectureName = architectureName,
				StartRealtime = Time.realtimeSinceStartup,
				StartFrame = Time.frameCount
			};
			report._watch.Start();
			Last = report;
			return report;
		}

		internal ServiceInitRecord BeginService(string serviceName, string contractName, int priority, bool background, int order)
		{
			var record = new ServiceInitRecord
			{
				ServiceName = serviceName,
				ContractName = contractName,
				Priority = priority,
				Background = background,
				Order = order,
				StartMs = ElapsedMs,
				StartFrame = Time.frameCount
			};
			record.Watch.Start();
			_services.Add(record);
			return record;
		}

		internal static void MarkSynchronousDone(ServiceInitRecord record)
		{
			record.SyncMs = record.Watch.Elapsed.TotalMilliseconds;
		}

		internal static void EndService(ServiceInitRecord record, bool failed)
		{
			if (record.SyncMs <= 0)
			{
				record.SyncMs = record.Watch.Elapsed.TotalMilliseconds;
			}

			record.Watch.Stop();
			record.TotalMs = record.Watch.Elapsed.TotalMilliseconds;
			record.Frames = Time.frameCount - record.StartFrame;
			record.Failed = failed;
			record.Completed = true;
		}

		internal void TrackFrame(float unscaledDeltaTime)
		{
			if (Finished)
			{
				return;
			}

			var frameMs = unscaledDeltaTime * 1000d;
			if (frameMs > LongestFrameMs)
			{
				LongestFrameMs = frameMs;
				LongestFrameNumber = Time.frameCount;
			}
		}
	}
}
