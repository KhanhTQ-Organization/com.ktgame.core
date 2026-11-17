using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace com.ktgame.core.manager
{
	public sealed class ManagerInitRecord
	{
		public string ManagerName { get; set; }
		public string ContractName { get; set; }
		public int Priority { get; set; }
		public int Order { get; set; }

		public double StartMs { get; set; }
		public double SyncMs { get; set; }
		public double TotalMs { get; set; }

		public int Frames { get; set; }
		public bool Completed { get; set; }
		public bool Failed { get; set; }

		/// <summary>
		/// Longest frame seen while this manager was initializing. Unlike SyncMs it also catches
		/// main-thread work done after the first await (e.g. parsing after a file load).
		/// </summary>
		public double LongestFrameMs { get; private set; }
		public int LongestFrameNumber { get; private set; }

		public double ElapsedMs => Completed ? TotalMs : Watch.Elapsed.TotalMilliseconds;

		public int StartFrame;
		public int EndFrame = -1;
		public readonly Stopwatch Watch = new Stopwatch();

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

	public sealed class SceneManagersInitReport
	{
		public static SceneManagersInitReport Last { get; set; }
		public static List<SceneManagersInitReport> AllReports { get; } = new List<SceneManagersInitReport>();

		public string SceneName { get; set; }
		public float StartRealtime { get; set; }
		public int StartFrame { get; set; }

		public double InstallBindingMs { get; set; }
		public double TotalInitMs { get; set; }
		public int TotalFrames { get; set; }
		public bool Finished { get; set; }

		/// <summary>Longest frame seen while the managers of this scene were starting.</summary>
		public double LongestFrameMs { get; private set; }
		public int LongestFrameNumber { get; private set; }

		/// <summary>Frame in which the last manager finished. Only meaningful once <see cref="Finished"/>.</summary>
		public int FinishFrame => StartFrame + TotalFrames;

		/// <summary>
		/// Finished, and the duration of the finishing frame has been recorded (a frame's duration is only known
		/// on the next frame). Nothing in the report changes after this.
		/// </summary>
		public bool Settled => Finished && Time.frameCount > FinishFrame + 1;

		public IReadOnlyList<ManagerInitRecord> Managers => _managers;
		public double ElapsedMs => _watch.Elapsed.TotalMilliseconds;

		private readonly List<ManagerInitRecord> _managers = new List<ManagerInitRecord>();
		private readonly Stopwatch _watch = new Stopwatch();

		public static SceneManagersInitReport Begin(string sceneName)
		{
			var report = new SceneManagersInitReport
			{
				SceneName = sceneName,
				StartRealtime = Time.realtimeSinceStartup,
				StartFrame = Time.frameCount
			};
			report._watch.Start();

			var existingIndex = AllReports.FindIndex(r => r.SceneName == sceneName);
			if (existingIndex >= 0)
			{
				AllReports[existingIndex] = report;
			}
			else
			{
				AllReports.Add(report);
			}

			Last = report;
			return report;
		}

		public ManagerInitRecord BeginManager(string managerName, string contractName, int priority, int order)
		{
			var record = new ManagerInitRecord
			{
				ManagerName = managerName,
				ContractName = contractName,
				Priority = priority,
				Order = order,
				StartMs = ElapsedMs,
				StartFrame = Time.frameCount
			};
			record.Watch.Start();
			_managers.Add(record);
			return record;
		}

		public static void MarkSynchronousDone(ManagerInitRecord record)
		{
			record.SyncMs = record.Watch.Elapsed.TotalMilliseconds;
		}

		public static void EndManager(ManagerInitRecord record, bool failed)
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

		public void Finish()
		{
			_watch.Stop();
			TotalInitMs = _watch.Elapsed.TotalMilliseconds;
			TotalFrames = Time.frameCount - StartFrame;
			Finished = true;
		}

		/// <summary>Called once per frame by the Architecture: records frame times for every report still running.</summary>
		internal static void TrackAll(float unscaledDeltaTime)
		{
			for (var i = 0; i < AllReports.Count; i++)
			{
				AllReports[i].TrackFrame(unscaledDeltaTime);
			}
		}

		/// <summary>
		/// Time.unscaledDeltaTime read now is the duration of the PREVIOUS frame,
		/// so it is attributed to frame (Time.frameCount - 1) and to the managers that were running in it.
		/// </summary>
		private void TrackFrame(float unscaledDeltaTime)
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

			for (var i = 0; i < _managers.Count; i++)
			{
				_managers[i].TrackFrame(frame, frameMs);
			}
		}
	}
}
