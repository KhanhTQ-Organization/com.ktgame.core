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

			// Sync default values from PlayerSettings if they are empty
			if (string.IsNullOrEmpty(_setting.ProductNameAndroid))
				_setting.ProductNameAndroid = PlayerSettings.productName;
			if (string.IsNullOrEmpty(_setting.VersionNameAndroid))
				_setting.VersionNameAndroid = PlayerSettings.bundleVersion;
			if (_setting.VersionCodeAndroid == 0)
				_setting.VersionCodeAndroid = PlayerSettings.Android.bundleVersionCode;
			if (string.IsNullOrEmpty(_setting.KeystorePasswordAndroid))
				_setting.KeystorePasswordAndroid = PlayerSettings.Android.keystorePass;
			if (string.IsNullOrEmpty(_setting.KeyaliasPasswordAndroid))
				_setting.KeyaliasPasswordAndroid = PlayerSettings.Android.keyaliasPass;

			if (string.IsNullOrEmpty(_setting.ProductNameIos))
				_setting.ProductNameIos = PlayerSettings.productName;
			if (string.IsNullOrEmpty(_setting.VersionNameIos))
				_setting.VersionNameIos = PlayerSettings.bundleVersion;
			if (string.IsNullOrEmpty(_setting.VersionCodeIos))
				_setting.VersionCodeIos = PlayerSettings.iOS.buildNumber;
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

		[PropertySpace(SpaceBefore = 10, SpaceAfter = 10)]
		[ShowInInspector]
		[PropertyOrder(-1)]
		[Button("Show KTGame Core Package", ButtonSizes.Large, Icon = SdfIconType.Folder), GUIColor(0.3f, 0.7f, 1f)]
		public void ShowServicePrefab()
		{
			string path = "Packages/com.ktgame.core";
			Object obj = AssetDatabase.LoadAssetAtPath<Object>(path);
			if (obj != null)
			{
				Selection.activeObject = obj;
				EditorGUIUtility.PingObject(obj);
			}
			else
			{
				Debug.LogWarning($"❌ Cannot find package folder at '{path}'.");
			}
		}

		[BoxGroup("General Information")]
		[LabelText("Bundle Identifier")]
		[DisplayAsString(false)]
		[ShowInInspector]
		private string BundleIdentifierAndroid
		{
			get => Application.identifier;
		}

#region Android
		[TabGroup("Platform", "Android", SdfIconType.Robot)]
		[Title("Android Build Configurations", "Manage app info and builds", TitleAlignments.Left)]
		[BoxGroup("Platform/Android/Settings", ShowLabel = false)]
		[LabelText("Publisher:"), ShowInInspector, EnumPaging]
		[Tooltip("Select the publisher. This will automatically add corresponding scripting define symbols.")]
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

		[BoxGroup("Platform/Android/Settings")]
		[LabelText("App Icon")]
		[ShowInInspector, HideLabel]
		[PreviewField(70, ObjectFieldAlignment.Center)]
		[Tooltip("The main application icon for Android.")]
		private Texture2D IconAndroid
		{
			get
			{
				var group = BuildTargetGroup.Android;
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

		[BoxGroup("Platform/Android/Settings")]
		[LabelText("Product Name")]
		[ShowInInspector]
		[Tooltip("The display name of the application on the device.")]
		private string ProductNameAndroid
		{
			get
			{
				if (_setting.ProductNameAndroid != PlayerSettings.productName)
				{
					_setting.ProductNameAndroid = PlayerSettings.productName;
					EditorUtility.SetDirty(_setting);
				}
				return _setting.ProductNameAndroid;
			}
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

		[BoxGroup("Platform/Android/Settings")]
		[LabelText("Version Name")]
		[ShowInInspector]
		[Tooltip("The release version string (e.g. 1.0.0).")]
		private string VersionNameAndroid
		{
			get
			{
				if (_setting.VersionNameAndroid != PlayerSettings.bundleVersion)
				{
					_setting.VersionNameAndroid = PlayerSettings.bundleVersion;
					EditorUtility.SetDirty(_setting);
				}
				return _setting.VersionNameAndroid;
			}
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

		[BoxGroup("Platform/Android/Settings")]
		[LabelText("Version Code")]
		[ShowInInspector]
		[Tooltip("The internal version number. Must be incremented for every release.")]
		[MinValue(1)]
		private int VersionCodeAndroid
		{
			get
			{
				if (_setting.VersionCodeAndroid != PlayerSettings.Android.bundleVersionCode)
				{
					_setting.VersionCodeAndroid = PlayerSettings.Android.bundleVersionCode;
					EditorUtility.SetDirty(_setting);
				}
				return _setting.VersionCodeAndroid;
			}
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

		[BoxGroup("Platform/Android/Keys", ShowLabel = false)]
		[Password]
		[ShowInInspector]
		[Required("Keystore Password cannot be empty when building AAB/APK!", InfoMessageType.Warning)]
		[Tooltip("Password for the Android keystore.")]
		public string KeystorePassword
		{
			get
			{
				if (_setting.KeystorePasswordAndroid != PlayerSettings.Android.keystorePass)
				{
					_setting.KeystorePasswordAndroid = PlayerSettings.Android.keystorePass;
					EditorUtility.SetDirty(_setting);
				}
				return _setting.KeystorePasswordAndroid;
			}
			set
			{
				_setting.KeystorePasswordAndroid = value;
				PlayerSettings.Android.keystorePass = value;
			}
		}

		[BoxGroup("Platform/Android/Keys")]
		[Password]
		[ShowInInspector]
		[Required("Keyalias Password cannot be empty when building AAB/APK!", InfoMessageType.Warning)]
		[Tooltip("Password for the Android keyalias.")]
		public string KeyaliasPassword
		{
			get
			{
				if (_setting.KeyaliasPasswordAndroid != PlayerSettings.Android.keyaliasPass)
				{
					_setting.KeyaliasPasswordAndroid = PlayerSettings.Android.keyaliasPass;
					EditorUtility.SetDirty(_setting);
				}
				return _setting.KeyaliasPasswordAndroid;
			}
			set
			{
				_setting.KeyaliasPasswordAndroid = value;
				PlayerSettings.Android.keyaliasPass = value;
			}
		}

		[PropertySpace(SpaceBefore = 10, SpaceAfter = 10)]
		[ButtonGroup("Platform/Android/Builds")]
		[ShowInInspector]
		[Button(" Build APK", ButtonSizes.Large, Icon = SdfIconType.FileZip)]
		private void BuildAPK()
		{
			//LamaEditor.BuildAndroid();
		}

		[ButtonGroup("Platform/Android/Builds")]
		[ShowInInspector]
		[Button(" Build AAB", ButtonSizes.Large, Icon = SdfIconType.Box)]
		private void BuildAab()
		{
			//LamaEditor.BuildAndroid(true);
		}
#endregion

#region IOS
		[TabGroup("Platform", "Ios", SdfIconType.Apple)]
		[Title("iOS Build Configurations", "Manage app info for iOS", TitleAlignments.Left)]
		[BoxGroup("Platform/Ios/Settings", ShowLabel = false)]
		[LabelText("Publisher:"), ShowInInspector, EnumPaging]
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

		[BoxGroup("Platform/Ios/Settings")]
		[LabelText("App Icon")]
		[ShowInInspector, HideLabel]
		[PreviewField(70, ObjectFieldAlignment.Center)]
		[Tooltip("The main application icon for iOS.")]
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
				var group = BuildTargetGroup.iOS;
				int[] sizes = PlayerSettings.GetIconSizesForTargetGroup(group, IconKind.Application);
				Texture2D[] icons = new Texture2D[sizes.Length];
				for (int i = 0; i < sizes.Length; i++)
					icons[i] = value;
				PlayerSettings.SetIconsForTargetGroup(group, icons, IconKind.Application);
				AssetDatabase.SaveAssets();
			}
		}

		[BoxGroup("Platform/Ios/Settings")]
		[LabelText("Product Name")]
		[ShowInInspector]
		private string ProductNameIos
		{
			get
			{
				if (_setting.ProductNameIos != PlayerSettings.productName)
				{
					_setting.ProductNameIos = PlayerSettings.productName;
					EditorUtility.SetDirty(_setting);
				}
				return _setting.ProductNameIos;
			}
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

		[BoxGroup("Platform/Ios/Settings")]
		[LabelText("Version Name")]
		[ShowInInspector]
		private string VersionNameIos
		{
			get
			{
				if (_setting.VersionNameIos != PlayerSettings.bundleVersion)
				{
					_setting.VersionNameIos = PlayerSettings.bundleVersion;
					EditorUtility.SetDirty(_setting);
				}
				return _setting.VersionNameIos;
			}
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

		[BoxGroup("Platform/Ios/Settings")]
		[LabelText("Version Code")]
		[ShowInInspector]
		private string VersionCodeIos
		{
			get
			{
				if (_setting.VersionCodeIos != PlayerSettings.iOS.buildNumber)
				{
					_setting.VersionCodeIos = PlayerSettings.iOS.buildNumber;
					EditorUtility.SetDirty(_setting);
				}
				return _setting.VersionCodeIos;
			}
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
#endregion
	}
}
