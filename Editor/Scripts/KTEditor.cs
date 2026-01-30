using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor.Build.Reporting;
using UnityEditor;
using UnityEngine;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;

namespace com.ktgame.core.editor
{
	public static class KTEditor
	{
		public const string RootMenuName = "KT" + "/";
		public static string ProjectPath => Path.GetDirectoryName(Application.dataPath);
		public static string BuildPath => string.Concat(ProjectPath, "/Build/");
		private const string PackageName = "com.ktgame.core";
		public static string SdkPath => $"Packages/{PackageName}/";
		public static KTSettingSO Setting =>
			AssetDatabase.LoadAssetAtPath<KTSettingSO>($"Packages/{PackageName}/Editor/Resources/KTSettings.asset");

		public static void BuildAndroid(bool isAab = false)
		{
			if (isAab)
			{
				if (!PlayerSettings.Android.useCustomKeystore)
				{
					EditorUtility.DisplayDialog("Build Warning",
						"Please check the release keystore again.",
						"Ok");
					return;
				}
			}
			
			if (PlayerSettings.Android.useCustomKeystore)
			{
				PlayerSettings.Android.keyaliasPass = Setting.KeyaliasPasswordAndroid;
				PlayerSettings.Android.keystorePass = Setting.KeyaliasPasswordAndroid;
			}
			
			string fileNameExtension = isAab ? "aab" : "apk";
			EditorUserBuildSettings.buildAppBundle = isAab;
			EditorUserBuildSettings.androidCreateSymbols = isAab ? AndroidCreateSymbols.Public : AndroidCreateSymbols.Disabled;
			PlayerSettings.Android.splitApplicationBinary = isAab;
			
			string nameProject = PlayerSettings.productName;
			
			nameProject = string.Concat(nameProject.Where(c => c is >= 'A' and <= 'Z'));
			
			var fileName =
				$"{nameProject}_v{PlayerSettings.Android.bundleVersionCode}_{DateTime.Now:yyyy''MM''dd'_'HH''mm}.{fileNameExtension}";
			
			var path = $"Build/{fileNameExtension.ToUpper()}/{fileName}";
			var report =
				BuildPipeline.BuildPlayer(
					(from scene in EditorBuildSettings.scenes where scene.enabled select scene.path).ToArray(), path,
					BuildTarget.Android, BuildOptions.None);
			
			var summary = report.summary;
			
			if (summary.result == BuildResult.Succeeded)
			{
				path = string.Concat(BuildPath, fileNameExtension.ToUpper()).Replace('/', '\\');
				ExplorerHelper.OpenAndSelectFile(path, fileName);
			}
		}

		static AddRequest _request;

		public static bool IsImportedPackage(string name)
		{
			if (!File.Exists("Packages/manifest.json"))
				return false;

			string jsonText = File.ReadAllText("Packages/manifest.json");

			return jsonText.Contains(name);
		}

		public static void ImportUnityPackage(string name)
		{
			if (_request != null)
				return;

			_request = Client.Add(name);
			EditorApplication.update += Progress;
		}

		static void Progress()
		{
			if (_request.IsCompleted)
			{
				if (_request.Status == StatusCode.Success)
					UnityEngine.Debug.Log("Installed: " + _request.Result.packageId);
				else if (_request.Status >= StatusCode.Failure)
					UnityEngine.Debug.Log(_request.Error.message);

				EditorApplication.update -= Progress;
				_request = null;
			}
		}

		public static GUIStyle TextStyle(int fontSize, TextAnchor alignment, FontStyle fontStyle, Color color)
		{
			GUIStyle styleTitle = new GUIStyle
			{
				fontSize = fontSize,
				alignment = alignment,
				fontStyle = fontStyle
			};
			styleTitle.normal.textColor = color;

			return styleTitle;
		}

		public static GUIStyle TextStyle(GUIStyle styleTitle, int fontSize, TextAnchor alignment, FontStyle fontStyle,
			Color color)
		{
			styleTitle.fontSize = fontSize;
			styleTitle.alignment = alignment;
			styleTitle.fontStyle = fontStyle;
			styleTitle.normal.textColor = color;

			return styleTitle;
		}

		public static Texture GetIcon(string name)
		{
			return AssetDatabase.LoadAssetAtPath<Texture>($"Assets/_Lamurai_/Editor/Textures/{name}.png");
		}

		public static Texture2D GetIconComponent(string name)
		{
			return AssetDatabase.LoadAssetAtPath<Texture2D>($"{SdkPath}Editor/Textures/Icons/{name}.png");
		}
	}
}