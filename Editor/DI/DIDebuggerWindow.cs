using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using com.ktgame.core.manager;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace com.ktgame.core.di.editor
{
	public class DiDebuggerWindow : EditorWindow
	{
		private const string ContainerIcon = "PreMatCylinder";
		private const string ResolverIcon = "d_NetworkAnimator Icon";
		private const string InstanceIcon = "d_Prefab Icon";

		private const float ToolbarHeight = 21f;
		private const double FrameBudgetMs = 16.7d;
		private const double HitchMs = 50d;

		private enum Tab
		{
			Dependencies,
			Startup
		}

		private enum StartupCategory
		{
			Services,
			Managers
		}

		private enum StartupSort
		{
			Order,
			Sync,
			Total,
			LongestFrame
		}

		private static readonly string[] TabNames = { "Dependencies", "Startup" };
		private static readonly string[] CategoryNames = { "Services (Global)", "Managers (Scene)" };
		private static readonly string[] SortNames = { "Order", "Sync (blocks frame)", "Total", "Longest frame" };

		[NonSerialized] private bool _isInitialized;
		[SerializeField] private TreeViewState _treeViewState;
		[SerializeField] private MultiColumnHeaderState _multiColumnHeaderState;
		[SerializeField] private Tab _tab = Tab.Dependencies;
		[SerializeField] private StartupCategory _startupCategory = StartupCategory.Services;
		[SerializeField] private StartupSort _startupSort = StartupSort.Order;

		private int _id = -1;
		private SearchField _searchField;
		private Vector2 _bindingStackTraceScrollPosition;
		private Vector2 _startupScrollPosition;
		private ArchitectureInitReport _startupReport;
		private List<ServiceInitRecord> _startupRows = new List<ServiceInitRecord>();
		private bool _startupHasBackground;
		private int _selectedSceneIndex;
		private SceneManagersInitReport _currentManagerReport;
		private List<ManagerInitRecord> _managerRows = new List<ManagerInitRecord>();
		private IArchitecture _architecture;
		private bool _startupLive;

		private MultiColumnTreeView TreeView { get; set; }
		private Rect SearchBarRect => new Rect(20f, ToolbarHeight + 10f, position.width - 40f, 20f);
		private Rect MultiColumnTreeViewRect => new Rect(20, ToolbarHeight + 30f, position.width - 40, position.height - 200 - ToolbarHeight);

		private void OnEnable()
		{
			SceneManager.sceneLoaded += OnSceneLoaded;
			SceneManager.sceneUnloaded += OnSceneUnloaded;
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
			EditorApplication.update += OnEditorUpdate;
		}

		private void OnDisable()
		{
			SceneManager.sceneLoaded -= OnSceneLoaded;
			SceneManager.sceneUnloaded -= OnSceneUnloaded;
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			EditorApplication.update -= OnEditorUpdate;
		}

		// The Startup tab only repaints while a report is still changing, so the window does not slow down
		// the game it is measuring. One last repaint is done when the report settles.
		private void OnEditorUpdate()
		{
			if (_tab != Tab.Startup)
			{
				_startupLive = false;
				return;
			}

			var live = Application.isPlaying && IsStartupLive();
			if (live || _startupLive)
			{
				Repaint();
			}

			_startupLive = live;
		}

		private bool IsStartupLive()
		{
			if (_startupCategory == StartupCategory.Services)
			{
				var report = ArchitectureInitReport.Last;
				return report == null || !report.Settled;
			}

			var reports = SceneManagersInitReport.AllReports;
			if (reports.Count == 0)
			{
				return true;
			}

			for (var i = 0; i < reports.Count; i++)
			{
				if (!reports[i].Settled)
				{
					return true;
				}
			}

			return false;
		}

		private async void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			await Task.Yield();
			Refresh();
		}

		private async void OnSceneUnloaded(Scene scene)
		{
			await Task.Yield();
			Refresh();
		}

		private async void OnPlayModeStateChanged(PlayModeStateChange playModeStateChange)
		{
			if (playModeStateChange == PlayModeStateChange.ExitingPlayMode || playModeStateChange == PlayModeStateChange.EnteredEditMode)
			{
				_architecture = null;
				_isInitialized = false;
				Repaint();
				return;
			}

			await Task.Yield();
			Refresh();
		}

		private static bool IsAlive(IArchitecture architecture)
		{
			return architecture is UnityEngine.Object unityObject ? unityObject != null : architecture != null;
		}

		private void InitIfNeeded()
		{
			if (!_isInitialized)
			{
				_treeViewState ??= new TreeViewState();

				bool firstInit = _multiColumnHeaderState == null;
				var headerState = MultiColumnTreeView.CreateDefaultMultiColumnHeaderState();
				if (MultiColumnHeaderState.CanOverwriteSerializedFields(_multiColumnHeaderState, headerState))
					MultiColumnHeaderState.OverwriteSerializedFields(_multiColumnHeaderState, headerState);
				_multiColumnHeaderState = headerState;

				var multiColumnHeader = new MultiColumnHeader(headerState)
				{
					canSort = false,
					height = MultiColumnHeader.DefaultGUI.minimumHeight
				};

				if (firstInit)
					multiColumnHeader.ResizeToFit();

				var treeModel = new TreeModel<MyTreeElement>(GetData());

				TreeView = new MultiColumnTreeView(_treeViewState, multiColumnHeader, treeModel);
				TreeView.SetExpanded(treeModel.Root.Id, true);

				_searchField = new SearchField();
				_searchField.downOrUpArrowKeyPressed += TreeView.SetFocusAndEnsureSelectedItem;

				_isInitialized = true;
			}
		}

		private static List<(IResolver, Type)> BuildMatrix(Injector injector)
		{
			var resolvers = injector.ResolversByContract.Values
				.Select(r => r)
				.ToHashSet();

			return resolvers.Select(resolver => (resolver, GetContract(resolver, injector))).ToList();
		}

		private static Type GetContract(IResolver resolver, Injector injector)
		{
			foreach (var pair in injector.ResolversByContract)
			{
				if (pair.Value == resolver)
				{
					return pair.Key;
				}
			}

			return null;
		}

		private void BuildDataRecursively(MyTreeElement parent, Injector injector)
		{
			if (injector == null)
			{
				return;
			}

			var containerTreeElement = new MyTreeElement("Dependency Container", parent.Depth + 1, ++_id, ContainerIcon, () => string.Empty,
				Array.Empty<string>(),
				string.Empty, injector.GetDebugProperties().BuildCallsite, kind: string.Empty);
			parent.Children.Add(containerTreeElement);
			containerTreeElement.Parent = parent;

			foreach (var pair in BuildMatrix(injector))
			{
				var resolverTreeElement = new MyTreeElement(
					string.Join(", ", pair.Item2.GetName()),
					containerTreeElement.Depth + 1,
					++_id,
					ResolverIcon,
					() => pair.Item1.GetDebugProperties().Resolutions.ToString(),
					new[] { pair.Item2.GetName() },
					pair.Item1.Lifetime.ToString(),
					pair.Item1.GetDebugProperties().BindingCallsite,
					kind: pair.Item1.GetType().Name.Replace("Singleton", string.Empty).Replace("Transient", string.Empty).Replace("Resolver", string.Empty)
				);

				foreach (var (instance, callsite) in pair.Item1.GetDebugProperties().Instances)
				{
					var instanceTreeElement = new MyTreeElement(
						instance.GetType().GetName(),
						resolverTreeElement.Depth + 1,
						++_id,
						InstanceIcon,
						() => string.Empty,
						Array.Empty<string>(),
						string.Empty,
						callsite,
						string.Empty
					);

					instanceTreeElement.SetParent(resolverTreeElement);
				}

				resolverTreeElement.SetParent(containerTreeElement);
			}
		}

		private IList<MyTreeElement> GetData()
		{
			var root = new MyTreeElement("Root", -1, ++_id, ContainerIcon, () => string.Empty, Array.Empty<string>(), string.Empty, null, string.Empty);
			if (IsAlive(_architecture))
			{
				BuildDataRecursively(root, (Injector)_architecture.Injector);
			}

			var list = new List<MyTreeElement>();
			TreeElementUtility.TreeToList(root, list);
			return list;
		}

		private void OnGUI()
		{
			_tab = (Tab)GUILayout.Toolbar((int)_tab, TabNames, EditorStyles.toolbarButton, GUILayout.Height(ToolbarHeight));

			if (_tab == Tab.Startup)
			{
				PresentStartup();
				return;
			}

			if (!Application.isPlaying)
			{
				EditorGUILayout.HelpBox("Enter Play Mode to inspect the dependency container.", MessageType.Info);
				return;
			}
			
			if (Event.current.type == EventType.Layout && !IsAlive(_architecture))
			{
				_isInitialized = false;
				_architecture = FindObjectsOfType<MonoBehaviour>().OfType<IArchitecture>().FirstOrDefault();
			}

			if (!IsAlive(_architecture))
			{
				EditorGUILayout.HelpBox("Waiting for the architecture...", MessageType.Info);
				Repaint();
				return;
			}

			Repaint();
			InitIfNeeded();
			PresentDebuggerEnabled();
			GUILayout.FlexibleSpace();
			PresentStatusBar();
		}

		private void Refresh(PlayModeStateChange _ = default)
		{
			_isInitialized = false;
			if (Application.isPlaying && IsAlive(_architecture))
			{
				InitIfNeeded();
			}

			Repaint();
		}

		private void SearchBar(Rect rect)
		{
			TreeView.searchString = _searchField.OnGUI(rect, TreeView.searchString);
			GUILayoutUtility.GetRect(rect.width, rect.height);
		}

		private void DoTreeView(Rect rect)
		{
			TreeView.OnGUI(rect);
			GUILayoutUtility.GetRect(rect.width, rect.height);
		}

		private void PresentDebuggerEnabled()
		{
			SearchBar(SearchBarRect);
			DoTreeView(MultiColumnTreeViewRect);

			GUILayout.Space(16);

			using (new GUILayout.HorizontalScope())
			{
				GUILayout.Space(16);

				using (new GUILayout.VerticalScope())
				{
					_bindingStackTraceScrollPosition = GUILayout.BeginScrollView(_bindingStackTraceScrollPosition);

					PresentCallSite();

					GUILayout.EndScrollView();
					GUILayout.Space(16);
				}

				GUILayout.Space(16);
			}
		}

		private void PresentStatusBar()
		{
			using (new EditorGUILayout.HorizontalScope(Styles.AppToolbar))
			{
				GUILayout.FlexibleSpace();

				var refreshIcon = EditorGUIUtility.IconContent("d_TreeEditor.Refresh");
				refreshIcon.tooltip = "Forces Tree View to Refresh";

				if (GUILayout.Button(refreshIcon, Styles.StatusBarIcon, GUILayout.Width(25)))
				{
					Refresh();
				}
			}
		}

		private void PresentCallSite()
		{
			var selection = TreeView.GetSelection();

			if (selection == null || selection.Count == 0)
			{
				return;
			}

			var item = TreeView.Find(selection.Single());

			if (item?.Callsite == null)
			{
				return;
			}

			foreach (var callSite in item.Callsite)
			{
				PresentStackFrame(callSite.ClassName, callSite.FunctionName, callSite.Path, callSite.Line);
			}
		}

		private static void PresentStackFrame(string className, string functionName, string path, int line)
		{
			using (new GUILayout.HorizontalScope())
			{
				GUILayout.Label($"{className}:{functionName}()  →", Styles.StackTrace);

				if (PresentLinkButton($"{path}:{line}"))
				{
					UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(path, line);
				}
			}
		}

		private static bool PresentLinkButton(string label, params GUILayoutOption[] options)
		{
			var position = GUILayoutUtility.GetRect(new GUIContent(label), Styles.Hyperlink, options);
			position.y -= 3;
			Handles.color = Styles.Hyperlink.normal.textColor;
			Handles.DrawLine(new Vector3(position.xMin + (float)EditorStyles.linkLabel.padding.left, position.yMax),
				new Vector3(position.xMax - (float)EditorStyles.linkLabel.padding.right, position.yMax));
			Handles.color = Color.white;
			EditorGUIUtility.AddCursorRect(position, MouseCursor.Link);
			return GUI.Button(position, label, Styles.Hyperlink);
		}

		#region Startup

		private void PresentStartup()
		{
			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
			{
				_startupCategory = (StartupCategory)GUILayout.Toolbar((int)_startupCategory, CategoryNames, EditorStyles.toolbarButton, GUILayout.Height(ToolbarHeight));
			}

			if (_startupCategory == StartupCategory.Services)
			{
				PresentStartupServices();
			}
			else
			{
				PresentStartupManagers();
			}
		}

		private void PresentStartupServices()
		{
			if (Event.current.type == EventType.Layout)
			{
				_startupReport = ArchitectureInitReport.Last;
				_startupRows = _startupReport != null ? SortRecords(_startupReport.Services).ToList() : new List<ServiceInitRecord>();
				_startupHasBackground = _startupReport != null && _startupReport.HasBackground;
			}

			var report = _startupReport;
			if (report == null)
			{
				EditorGUILayout.HelpBox("No startup recorded yet. Enter Play Mode: the startup timings of the services appear here " +
				                        "and stay visible after Play Mode stops.", MessageType.Info);
				return;
			}

			_startupScrollPosition = EditorGUILayout.BeginScrollView(_startupScrollPosition);
			GUILayout.Space(6);

			var state = report.Finished
				? Application.isPlaying ? "finished" : "finished (last Play Mode run)"
				: Application.isPlaying ? "running..." : "stopped before finishing";
			EditorGUILayout.LabelField($"{report.ArchitectureName} startup — {state}", EditorStyles.boldLabel);

			PresentSummaryRow("Ready (Architecture.IsInitialized)",
				report.InitializedDone ? $"{report.InitializedMs:F1} ms over {report.InitializedFrames} frame(s)" : $"running {report.ElapsedMs:F0} ms",
				report.InitializedDone ? (Color?)null : Color.yellow);

			if (_startupHasBackground)
			{
				PresentSummaryRow("Background services ready",
					report.BackgroundDone ? $"{report.BackgroundMs:F1} ms over {report.BackgroundFrames} frame(s)" : $"running {report.ElapsedMs:F0} ms",
					report.BackgroundDone ? (Color?)null : Color.yellow);
			}

			PresentSummaryRow("Longest frame while starting",
				report.LongestFrameMs > 0 ? $"{report.LongestFrameMs:F1} ms (frame {report.LongestFrameNumber})" : "-",
				GetCostColor(report.LongestFrameMs));

			GUILayout.Space(4);
			EditorGUILayout.LabelField("Before services (block the first frame)", EditorStyles.miniBoldLabel);
			PresentSummaryRow("Scan types (reflection)", $"{ArchitectureInitReport.ScanTypesMs:F1} ms", GetCostColor(ArchitectureInitReport.ScanTypesMs));
			PresentSummaryRow("Find service types", $"{report.ScanServicesMs:F1} ms", GetCostColor(report.ScanServicesMs));
			PresentSummaryRow("Create services", $"{report.CreateMs:F1} ms", GetCostColor(report.CreateMs));
			PresentSummaryRow("Inject services", $"{report.InjectMs:F1} ms", GetCostColor(report.InjectMs));
			PresentSummaryRow("OnPostInitialize", $"{report.PostInitializeMs:F1} ms", GetCostColor(report.PostInitializeMs));

			GUILayout.Space(8);
			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.LabelField("Services", EditorStyles.miniBoldLabel, GUILayout.Width(60));
				GUILayout.FlexibleSpace();
				EditorGUILayout.LabelField("Sort", GUILayout.Width(30));
				var newSort = (StartupSort)EditorGUILayout.Popup((int)_startupSort, SortNames, GUILayout.Width(150));
				if (newSort != _startupSort)
				{
					_startupSort = newSort;
					_startupRows = SortRecords(report.Services).ToList();
				}
			}

			PresentServiceHeader();
			foreach (var record in _startupRows)
			{
				PresentServiceRow(record);
			}

			GUILayout.Space(8);
			EditorGUILayout.HelpBox(
				"Sync = work done before OnInitialize returned: it blocks the frame (yellow > 16.7 ms, red > 50 ms).\n" +
				"Max frame = longest frame while the service was running: also catches main-thread work after the first await, which Sync misses. " +
				"Services running at the same time (Background + Main) share the same frames.\n" +
				"Total = until OnInitialize finished, waiting (network, SDK) included. Waiting does not freeze the game when Frames is high.\n" +
				"A long Total with few Frames means some frames were very long while that service was running.",
				MessageType.None);

			EditorGUILayout.EndScrollView();
		}

		private void PresentStartupManagers()
		{
			var reports = SceneManagersInitReport.AllReports;
			if (reports.Count == 0 && SceneManagersInitReport.Last == null)
			{
				EditorGUILayout.HelpBox("No scene managers recorded yet. Enter Play Mode and load a scene: the startup timings of the scene managers appear here " +
				                        "and stay visible after Play Mode stops.", MessageType.Info);
				return;
			}

			if (Event.current.type == EventType.Layout)
			{
				if (_selectedSceneIndex < 0 || _selectedSceneIndex >= reports.Count)
				{
					_selectedSceneIndex = reports.Count > 0 ? reports.Count - 1 : 0;
				}

				_currentManagerReport = reports.Count > 0 ? reports[_selectedSceneIndex] : SceneManagersInitReport.Last;
				_managerRows = _currentManagerReport != null ? SortManagerRecords(_currentManagerReport.Managers).ToList() : new List<ManagerInitRecord>();
			}

			var report = _currentManagerReport;
			if (report == null)
			{
				EditorGUILayout.HelpBox("No scene manager report available.", MessageType.Info);
				return;
			}

			_startupScrollPosition = EditorGUILayout.BeginScrollView(_startupScrollPosition);
			GUILayout.Space(6);

			if (reports.Count > 1)
			{
				using (new EditorGUILayout.HorizontalScope())
				{
					EditorGUILayout.LabelField("Scene:", GUILayout.Width(50));
					var sceneNames = reports.Select(r => r.SceneName).ToArray();
					var newIndex = EditorGUILayout.Popup(_selectedSceneIndex, sceneNames, GUILayout.Width(200));
					if (newIndex != _selectedSceneIndex)
					{
						_selectedSceneIndex = newIndex;
						_currentManagerReport = reports[_selectedSceneIndex];
						_managerRows = SortManagerRecords(_currentManagerReport.Managers).ToList();
					}
					GUILayout.FlexibleSpace();
				}
				GUILayout.Space(4);
			}

			var state = report.Finished
				? Application.isPlaying ? "finished" : "finished (last Play Mode run)"
				: Application.isPlaying ? "running..." : "stopped before finishing";
			EditorGUILayout.LabelField($"{report.SceneName} Managers startup — {state}", EditorStyles.boldLabel);

			PresentSummaryRow("Ready (All managers initialized)",
				report.Finished ? $"{report.TotalInitMs:F1} ms over {report.TotalFrames} frame(s)" : $"running {report.ElapsedMs:F0} ms",
				report.Finished ? (Color?)null : Color.yellow);

			PresentSummaryRow("Binding & Resolve (OnInstallManagers)",
				$"{report.InstallBindingMs:F1} ms",
				GetCostColor(report.InstallBindingMs));

			PresentSummaryRow("Longest frame while starting",
				report.LongestFrameMs > 0 ? $"{report.LongestFrameMs:F1} ms (frame {report.LongestFrameNumber})" : "-",
				GetCostColor(report.LongestFrameMs));

			PresentSummaryRow("Total managers registered",
				report.Managers.Count.ToString(),
				null);

			GUILayout.Space(8);
			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.LabelField("Managers", EditorStyles.miniBoldLabel, GUILayout.Width(70));
				GUILayout.FlexibleSpace();
				EditorGUILayout.LabelField("Sort", GUILayout.Width(30));
				var newSort = (StartupSort)EditorGUILayout.Popup((int)_startupSort, SortNames, GUILayout.Width(150));
				if (newSort != _startupSort)
				{
					_startupSort = newSort;
					_managerRows = SortManagerRecords(report.Managers).ToList();
				}
			}

			PresentManagerHeader();
			foreach (var record in _managerRows)
			{
				PresentManagerRow(record);
			}

			GUILayout.Space(8);
			EditorGUILayout.HelpBox(
				"Sync = work done before Initialize() returned: it blocks the frame (yellow > 16.7 ms, red > 50 ms).\n" +
				"Max frame = longest frame while the manager was running: also catches main-thread work after the first await, which Sync misses.\n" +
				"Total = until Initialize() finished (async I/O, load config, network included).\n" +
				"A long Total with few Frames means some frames were very long while that manager was running.",
				MessageType.None);

			EditorGUILayout.EndScrollView();
		}

		private IEnumerable<ManagerInitRecord> SortManagerRecords(IReadOnlyList<ManagerInitRecord> records)
		{
			switch (_startupSort)
			{
				case StartupSort.Sync:
					return records.OrderByDescending(r => r.SyncMs);
				case StartupSort.Total:
					return records.OrderByDescending(r => r.ElapsedMs);
				case StartupSort.LongestFrame:
					return records.OrderByDescending(r => r.LongestFrameMs);
				default:
					return records.OrderBy(r => r.Order);
			}
		}

		private static void PresentManagerHeader()
		{
			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
			{
				GUILayout.Label("Manager", EditorStyles.miniBoldLabel, GUILayout.MinWidth(170));
				GUILayout.Label("Contract", EditorStyles.miniBoldLabel, GUILayout.Width(140));
				GUILayout.Label("Priority", EditorStyles.miniBoldLabel, GUILayout.Width(50));
				GUILayout.Label("Start", EditorStyles.miniBoldLabel, GUILayout.Width(70));
				GUILayout.Label("Sync", EditorStyles.miniBoldLabel, GUILayout.Width(70));
				GUILayout.Label("Max frame", EditorStyles.miniBoldLabel, GUILayout.Width(75));
				GUILayout.Label("Total", EditorStyles.miniBoldLabel, GUILayout.Width(80));
				GUILayout.Label("Frames", EditorStyles.miniBoldLabel, GUILayout.Width(50));
			}
		}

		private static void PresentManagerRow(ManagerInitRecord record)
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				var name = new GUIContent(record.ManagerName, record.ContractName);
				var nameStyle = record.Failed ? ColoredLabel(Color.red) : EditorStyles.label;
				GUILayout.Label(record.Failed ? new GUIContent(record.ManagerName + " (failed)", record.ContractName) : name, nameStyle, GUILayout.MinWidth(170));
				GUILayout.Label(record.ContractName ?? "-", GUILayout.Width(140));
				GUILayout.Label(record.Priority.ToString(), GUILayout.Width(50));
				GUILayout.Label($"{record.StartMs:F0} ms", GUILayout.Width(70));

				var syncColor = GetCostColor(record.SyncMs);
				GUILayout.Label($"{record.SyncMs:F1} ms", syncColor.HasValue ? ColoredLabel(syncColor.Value) : EditorStyles.label, GUILayout.Width(70));
				PresentLongestFrame(record.LongestFrameMs);

				if (record.Completed)
				{
					GUILayout.Label($"{record.TotalMs:F1} ms", GUILayout.Width(80));
					GUILayout.Label(record.Frames.ToString(), GUILayout.Width(50));
				}
				else
				{
					GUILayout.Label($"{record.ElapsedMs:F0} ms...", ColoredLabel(Color.yellow), GUILayout.Width(80));
					GUILayout.Label("-", GUILayout.Width(50));
				}
			}
		}

		private IEnumerable<ServiceInitRecord> SortRecords(IReadOnlyList<ServiceInitRecord> records)
		{
			switch (_startupSort)
			{
				case StartupSort.Sync:
					return records.OrderByDescending(r => r.SyncMs);
				case StartupSort.Total:
					return records.OrderByDescending(r => r.ElapsedMs);
				case StartupSort.LongestFrame:
					return records.OrderByDescending(r => r.LongestFrameMs);
				default:
					return records.OrderBy(r => r.Background).ThenBy(r => r.Order);
			}
		}

		private static void PresentServiceHeader()
		{
			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
			{
				GUILayout.Label("Service", EditorStyles.miniBoldLabel, GUILayout.MinWidth(170));
				GUILayout.Label("Group", EditorStyles.miniBoldLabel, GUILayout.Width(80));
				GUILayout.Label("Priority", EditorStyles.miniBoldLabel, GUILayout.Width(50));
				GUILayout.Label("Start", EditorStyles.miniBoldLabel, GUILayout.Width(70));
				GUILayout.Label("Sync", EditorStyles.miniBoldLabel, GUILayout.Width(70));
				GUILayout.Label("Max frame", EditorStyles.miniBoldLabel, GUILayout.Width(75));
				GUILayout.Label("Total", EditorStyles.miniBoldLabel, GUILayout.Width(80));
				GUILayout.Label("Frames", EditorStyles.miniBoldLabel, GUILayout.Width(50));
			}
		}

		private static void PresentServiceRow(ServiceInitRecord record)
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				var name = new GUIContent(record.ServiceName, record.ContractName);
				var nameStyle = record.Failed ? ColoredLabel(Color.red) : EditorStyles.label;
				GUILayout.Label(record.Failed ? new GUIContent(record.ServiceName + " (failed)", record.ContractName) : name, nameStyle, GUILayout.MinWidth(170));
				GUILayout.Label(record.Background ? "Background" : "Main", GUILayout.Width(80));
				GUILayout.Label(record.Priority.ToString(), GUILayout.Width(50));
				GUILayout.Label($"{record.StartMs:F0} ms", GUILayout.Width(70));

				var syncColor = GetCostColor(record.SyncMs);
				GUILayout.Label($"{record.SyncMs:F1} ms", syncColor.HasValue ? ColoredLabel(syncColor.Value) : EditorStyles.label, GUILayout.Width(70));
				PresentLongestFrame(record.LongestFrameMs);

				if (record.Completed)
				{
					GUILayout.Label($"{record.TotalMs:F1} ms", GUILayout.Width(80));
					GUILayout.Label(record.Frames.ToString(), GUILayout.Width(50));
				}
				else
				{
					GUILayout.Label($"{record.ElapsedMs:F0} ms...", ColoredLabel(Color.yellow), GUILayout.Width(80));
					GUILayout.Label("-", GUILayout.Width(50));
				}
			}
		}

		private static void PresentLongestFrame(double ms)
		{
			var color = GetCostColor(ms);
			GUILayout.Label(ms > 0 ? $"{ms:F1} ms" : "-", color.HasValue ? ColoredLabel(color.Value) : EditorStyles.label, GUILayout.Width(75));
		}

		private static void PresentSummaryRow(string label, string value, Color? color)
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.LabelField(label, GUILayout.Width(230));
				EditorGUILayout.LabelField(value, color.HasValue ? ColoredLabel(color.Value) : EditorStyles.label);
			}
		}

		private static Color? GetCostColor(double ms)
		{
			if (ms > HitchMs)
			{
				return new Color(1f, 0.35f, 0.3f);
			}

			if (ms > FrameBudgetMs)
			{
				return new Color(1f, 0.8f, 0.2f);
			}

			return null;
		}

		private static readonly Dictionary<Color, GUIStyle> ColoredStyles = new Dictionary<Color, GUIStyle>();

		private static GUIStyle ColoredLabel(Color color)
		{
			if (!ColoredStyles.TryGetValue(color, out var style))
			{
				style = new GUIStyle(EditorStyles.label);
				style.normal.textColor = color;
				ColoredStyles[color] = style;
			}

			return style;
		}

		#endregion
	}
}
