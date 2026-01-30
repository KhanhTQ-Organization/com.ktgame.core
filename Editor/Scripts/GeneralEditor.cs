using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace com.ktgame.core.editor
{
	public class GeneralEditor
	{
		private readonly KTSettingSO _setting;

		public GeneralEditor(KTSettingSO setting)
		{
			_setting = setting;
		}

		[PropertyOrder(-2)]
		[OnInspectorGUI]
		private void OnInspectorGUI()
		{
			Texture logo = AssetDatabase.LoadAssetAtPath<Texture>($"{KTEditor.SdkPath}Editor/Textures/logo_name.png");
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Label(logo, GUILayout.Height(100), GUILayout.Width(200));
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}

		[PropertySpace(50, 10)]
		[ShowInInspector]
		[PropertyOrder(-1)]
		[Button("Show Service Prefab")]
		public void ShowServicePrefab()
		{
			GameObject prefab = Resources.Load("Services") as GameObject;
			if (prefab != null)
			{
				Selection.activeObject = prefab;
				EditorGUIUtility.PingObject(prefab);

				string path = AssetDatabase.GetAssetPath(prefab);

				PrefabStageUtility.OpenPrefab(path);
			}
			else
			{
				Debug.LogWarning("❌ Prefab 'Services' not found in Resources folder.");
			}
		}

		[LabelText("Bundle Identifier")]
		[ShowInInspector]
		private string BundleIdentifierAndroid
		{
			get => Application.identifier;
		}

#region Android
		[TabGroup("Platform", "Android", SdfIconType.Robot)]
		[LabelText("Publisher: "), LabelWidth(150),
		 ShowInInspector, EnumPaging]
		public PublisherType PublisherAndroid
		{
			get => _setting.PublisherTypeAndroid;
			set
			{
				DefineSymbolsEditor.RemoveDefineSymbol(DefineSymbolName.DS_PUBLISHER_ABI);
				DefineSymbolsEditor.RemoveDefineSymbol(DefineSymbolName.DS_PUBLISHER_HIGAME);
				DefineSymbolsEditor.RemoveDefineSymbol(DefineSymbolName.DS_PUBLISHER_INHOUSE);
				_setting.PublisherTypeAndroid = value;
				switch (value)
				{
					case PublisherType.ABI:
						DefineSymbolsEditor.AddDefineSymbol(DefineSymbolName.DS_PUBLISHER_ABI);
						break;
					case PublisherType.HIGAME:
						DefineSymbolsEditor.AddDefineSymbol(DefineSymbolName.DS_PUBLISHER_HIGAME);
						break;
					case PublisherType.INHOUSE:
						DefineSymbolsEditor.AddDefineSymbol(DefineSymbolName.DS_PUBLISHER_INHOUSE);
						break;
				}
			}
		}

		[TabGroup("Platform", "Android", SdfIconType.Robot)]
		[HorizontalGroup("Platform/Android/Row", Width = 0.7f)]
		[VerticalGroup("Platform/Android/Row/Left")]
		[LabelText("Product Name")]
		[ShowInInspector]
		private string ProductNameAndroid
		{
			get => _setting.ProductNameAndroid;
			set
			{
				if (_setting.ProductNameAndroid != value)
				{
					_setting.ProductNameAndroid = value;
					PlayerSettings.productName = value;
					AssetDatabase.SaveAssets();
				}
			}
		}

		[VerticalGroup("Platform/Android/Row/Left")]
		[LabelText("Version Name")]
		[ShowInInspector]
		private string VersionNameAndroid
		{
			get => _setting.VersionNameAndroid;
			set
			{
				if (_setting.VersionNameAndroid != value)
				{
					_setting.VersionNameAndroid = value;
					PlayerSettings.bundleVersion = value;
					AssetDatabase.SaveAssets();
				}
			}
		}

		[VerticalGroup("Platform/Android/Row/Left")]
		[LabelText("Version Code")]
		[ShowInInspector]
		private int VersionCodeAndroid
		{
			get => _setting.VersionCodeAndroid;
			set
			{
				if (_setting.VersionCodeAndroid != value)
				{
					_setting.VersionCodeAndroid = value;
					PlayerSettings.Android.bundleVersionCode = value;
					AssetDatabase.SaveAssets();
				}
			}
		}

		// [VerticalGroup("Platform/Android/Row/Right")]
		// [LabelText("")]
		// [ShowInInspector]
		// [PreviewField(60)]
		// private Texture2D IconAndroid
		// {
		// 	get
		// 	{
		// 		var group = BuildTargetGroup.Android;
		// 		var icons = PlayerSettings.GetIconsForTargetGroup(group, IconKind.Application);
		// 		return icons is { Length: > 0 } ? icons[0] : null;
		// 	}
		// 	set
		// 	{
		// 		var group = BuildTargetGroup.Android;
		// 		int[] sizes = PlayerSettings.GetIconSizesForTargetGroup(group, IconKind.Application);
		// 		Texture2D[] icons = new Texture2D[sizes.Length];
		// 		for (int i = 0; i < sizes.Length; i++)
		// 			icons[i] = value;
		// 		PlayerSettings.SetIconsForTargetGroup(group, icons, IconKind.Application);
		// 		AssetDatabase.SaveAssets();
		// 	}
		// }

		[HorizontalGroup("Platform/Android/Keystore")]
		[PropertySpace(20)]
		[Password]
		[ShowInInspector]
		public string KeystorePassword
		{
			get => _setting.KeystorePasswordAndroid;
			set => _setting.KeystorePasswordAndroid = value;
		}

		[HorizontalGroup("Platform/Android/Keyalias")]
		[Password]
		[ShowInInspector]
		public string KeyaliasPassword
		{
			get => _setting.KeyaliasPasswordAndroid;
			set => _setting.KeyaliasPasswordAndroid = value;
		}

		[PropertySpace(10)]
		[HorizontalGroup("Platform/Android/RowBuild")]
		[ShowInInspector]
		[Button("APK")]
		private void BuildAPK()
		{
			//LamaEditor.BuildAndroid();
		}

		[PropertySpace(10)]
		[HorizontalGroup("Platform/Android/RowBuild")]
		[ShowInInspector]
		[Button("AAB")]
		private void BuildAab()
		{
			//LamaEditor.BuildAndroid(true);
		}
#endregion

#region IOS
		[TabGroup("Platform", "Ios", SdfIconType.Apple)]
		[LabelText("Publisher: "), LabelWidth(150),
		 ShowInInspector, EnumPaging]
		public PublisherType PublisherIos
		{
			get => _setting.PublisherTypeIos;
			set
			{
				DefineSymbolsEditor.RemoveDefineSymbol(DefineSymbolName.DS_PUBLISHER_ABI);
				DefineSymbolsEditor.RemoveDefineSymbol(DefineSymbolName.DS_PUBLISHER_HIGAME);
				DefineSymbolsEditor.RemoveDefineSymbol(DefineSymbolName.DS_PUBLISHER_INHOUSE);
				_setting.PublisherTypeIos = value;
				switch (value)
				{
					case PublisherType.ABI:
						DefineSymbolsEditor.AddDefineSymbol(DefineSymbolName.DS_PUBLISHER_ABI);
						break;
					case PublisherType.HIGAME:
						DefineSymbolsEditor.AddDefineSymbol(DefineSymbolName.DS_PUBLISHER_HIGAME);
						break;
					case PublisherType.INHOUSE:
						DefineSymbolsEditor.AddDefineSymbol(DefineSymbolName.DS_PUBLISHER_INHOUSE);
						break;
				}
			}
		}

		[TabGroup("Platform", "Ios", SdfIconType.Robot)]
		[HorizontalGroup("Platform/Ios/Row", Width = 0.7f)]
		[VerticalGroup("Platform/Ios/Row/Left")]
		[LabelText("Product Name")]
		[ShowInInspector]
		private string ProductNameIos
		{
			get => _setting.ProductNameIos;
			set
			{
				if (_setting.ProductNameIos != value)
				{
					_setting.ProductNameIos = value;
					PlayerSettings.productName = value;
					AssetDatabase.SaveAssets();
				}
			}
		}

		[VerticalGroup("Platform/Ios/Row/Left")]
		[LabelText("Version Name")]
		[ShowInInspector]
		private string VersionNameIos
		{
			get => _setting.VersionNameIos;
			set
			{
				if (_setting.VersionNameIos != value)
				{
					_setting.VersionNameIos = value;
					PlayerSettings.bundleVersion = value;
					AssetDatabase.SaveAssets();
				}
			}
		}

		[VerticalGroup("Platform/Ios/Row/Left")]
		[LabelText("Version Code")]
		[ShowInInspector]
		private string VersionCodeIos
		{
			get => _setting.VersionCodeIos;
			set
			{
				if (_setting.VersionCodeIos != value)
				{
					_setting.VersionCodeIos = value;
					PlayerSettings.iOS.buildNumber = value;
					AssetDatabase.SaveAssets();
				}
			}
		}

		[VerticalGroup("Platform/Ios/Row/Right")]
		[LabelText("")]
		[ShowInInspector]
		[PreviewField(60)]
		private Texture2D IconIos
		{
			get
			{
				var group = BuildTargetGroup.iOS;
				var icons = PlayerSettings.GetIconsForTargetGroup(group, IconKind.Application);
				return icons != null && icons.Length > 0 ? icons[0] : null;
			}
			set
			{
				var group = BuildTargetGroup.Android;
				int[] sizes = PlayerSettings.GetIconSizesForTargetGroup(group, IconKind.Application);
				Texture2D[] icons = new Texture2D[sizes.Length];
				for (int i = 0; i < sizes.Length; i++)
					icons[i] = value;
				PlayerSettings.SetIconsForTargetGroup(group, icons, IconKind.Application);
				AssetDatabase.SaveAssets();
			}
		}
#endregion
	}
}
