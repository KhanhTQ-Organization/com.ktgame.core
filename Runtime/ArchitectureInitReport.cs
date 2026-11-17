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

		/// <summary>
		/// Longest frame seen while this service was initializing. Unlike SyncMs it also catches
		/// main-thread work done after the first await (e.g. parsing after a file load).
		/// </summary>
		public double LongestFrameMs { get; private set; }
		public int LongestFrameNumber { get; private set; }

		/// <summary>Total time when completed, otherwise the time spent so far.</summary>
		public double ElapsedMs => Completed ? TotalMs : Watch.Elapsed.TotalMilliseconds;

		internal int StartFrame;
		internal int EndFrame = -1;
		internal readonly Stopwatch Watch = new Stopwatch();

		internal void TrackFrame(int frame, double frameMs)
		{
			if (frame < StartFrame || (Completed && frame > EndFrame))
			{
				return;
			}

			if (frameMs > LongestFrameMs)
			{
				LongestFrameMs = frameMs;
				LongestFrameNumber = frame;
			}
		}
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

		/// <summary>Frame in which the last service finished. Only meaningful once <see cref="Finished"/>.</summary>
		public int FinishFrame => StartFrame + Mathf.Max(InitializedFrames, HasBackground ? BackgroundFrames : 0);

		/// <summary>
		/// Finished, and the duration of the finishing frame has been recorded (a frame's duration is only known
		/// on the next frame). Nothing in the report changes after this.
		/// </summary>
		public bool Settled => Finished && Time.frameCount > FinishFrame + 1;

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
			record.EndFrame = Time.frameCount;
			record.Frames = record.EndFrame - record.StartFrame;
			record.Failed = failed;
			record.Completed = true;
		}

		/// <summary>
		/// Call once per frame. Time.unscaledDeltaTime read now is the duration of the PREVIOUS frame,
		/// so it is attributed to frame (Time.frameCount - 1) and to the services that were running in it.
		/// </summary>
		internal void TrackFrame(float unscaledDeltaTime)
		{
			if (Settled)
			{
				return;
			}

			var frame = Time.frameCount - 1;
			if (frame < StartFrame)
			{
				return;
			}

			var frameMs = unscaledDeltaTime * 1000d;
			if (frameMs > LongestFrameMs)
			{
				LongestFrameMs = frameMs;
				LongestFrameNumber = frame;
			}

			for (var i = 0; i < _services.Count; i++)
			{
				_services[i].TrackFrame(frame, frameMs);
			}
		}
	}
}
