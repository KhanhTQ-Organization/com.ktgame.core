using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace com.ktgame.core.editor
{
	[InitializeOnLoad]
	public class DefineSymbolsEditor
	{
		static DefineSymbolsEditor()
		{
			if (Directory.Exists(Application.dataPath + "/MaxSdk"))
				AddDefineSymbol(DefineSymbolName.DS_MAX_INSTALLED);
			else
				RemoveDefineSymbol(DefineSymbolName.DS_MAX_INSTALLED);

			if (Directory.Exists(Application.dataPath + "/GoogleMobileAds"))
			{
				AddDefineSymbol(DefineSymbolName.DS_GMA_INSTALLED);

				if (GetGmaVersion() > new Version("9.6.0"))
					AddDefineSymbol(DefineSymbolName.DS_GMA_GREATER_9_6_0_INSTALLED);
			}
			else
			{
				RemoveDefineSymbol(DefineSymbolName.DS_GMA_INSTALLED);
				RemoveDefineSymbol(DefineSymbolName.DS_GMA_GREATER_9_6_0_INSTALLED);
			}

			if (PackageDependenceEditor.IsPackageInstalled("com.google.firebase"))
				AddDefineSymbol(DefineSymbolName.DS_FIREBASE_INSTALLED);
			else
				RemoveDefineSymbol(DefineSymbolName.DS_FIREBASE_INSTALLED);

			if (Directory.Exists(Application.dataPath + "/Appsflyer"))
				AddDefineSymbol(DefineSymbolName.DS_APPSFLYER_INSTALLED);
			else
				RemoveDefineSymbol(DefineSymbolName.DS_APPSFLYER_INSTALLED);

			if (Directory.Exists(Application.dataPath + "/Adverty5"))
				AddDefineSymbol(DefineSymbolName.DS_ADVERTY_INSTALLED);
			else
				RemoveDefineSymbol(DefineSymbolName.DS_ADVERTY_INSTALLED);

			if (Directory.Exists(Application.dataPath + "/Spine"))
				AddDefineSymbol(DefineSymbolName.DS_SPINE_INSTALLED);
			else
				RemoveDefineSymbol(DefineSymbolName.DS_SPINE_INSTALLED);
		}

		public static void AddDefineSymbol(string symbol)
		{
			var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
			var currentSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

			if (currentSymbols.Contains(symbol))
				return;

			var newSymbols = string.IsNullOrEmpty(currentSymbols) ? symbol : $"{currentSymbols};{symbol}";
			PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, newSymbols);
		}

		public static void RemoveDefineSymbol(string symbol)
		{
			var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
			var currentSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

			if (!currentSymbols.Contains(symbol))
				return;

			var newSymbols = string.Join(";", currentSymbols.Split(';').Where(s => s != symbol));
			PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, newSymbols);
		}

		public static bool HasDefineSymbol(string symbol)
		{
			var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
			var currentSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

			return currentSymbols.Split(';').Contains(symbol);
		}

		private static Version GetGmaVersion()
		{
			Version version = new Version("9.6.0");

			string className = "GoogleMobileAds.Api.MobileAds";

			var mobileAds = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(assembly => assembly.GetTypes())
				.FirstOrDefault(t => t.FullName == className);

			if (mobileAds == null)
				return version;

			var loadMethod = mobileAds.GetMethod("GetVersion",
				BindingFlags.Static |
				BindingFlags.Public |
				BindingFlags.NonPublic);

			if (loadMethod != null)
			{
				version = (Version)loadMethod.Invoke(null, null);
			}

			return version;
		}
	}
}
