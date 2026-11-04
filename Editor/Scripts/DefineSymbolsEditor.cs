using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace com.ktgame.core.editor
{
	[InitializeOnLoad]
	public class DefineSymbolsEditor
	{
		public static void AddDefineSymbol(string symbol)
		{
			AddDefineSymbol(symbol, SelectedTarget);
		}

		public static void RemoveDefineSymbol(string symbol)
		{
			RemoveDefineSymbol(symbol, SelectedTarget);
		}

		public static bool HasDefineSymbol(string symbol)
		{
			return HasDefineSymbol(symbol, SelectedTarget);
		}

		public static void AddDefineSymbol(string symbol, NamedBuildTarget target)
		{
			var symbols = GetSymbols(target);
			if (symbols.Contains(symbol))
				return;

			symbols.Add(symbol);
			PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", symbols));
		}

		public static void RemoveDefineSymbol(string symbol, NamedBuildTarget target)
		{
			var symbols = GetSymbols(target);
			if (!symbols.Remove(symbol))
				return;

			PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", symbols));
		}

		public static bool HasDefineSymbol(string symbol, NamedBuildTarget target)
		{
			return GetSymbols(target).Contains(symbol);
		}

		private static NamedBuildTarget SelectedTarget =>
			NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
		
		private static List<string> GetSymbols(NamedBuildTarget target)
		{
			return PlayerSettings.GetScriptingDefineSymbols(target)
				.Split(';')
				.Select(s => s.Trim())
				.Where(s => s.Length > 0)
				.ToList();
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
