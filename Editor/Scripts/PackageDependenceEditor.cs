using System;
using System.Collections;
using System.IO;
using System.Net;
using UnityEditor;
using UnityEngine;

namespace com.ktgame.core.editor
{
	public class PackageDependenceEditor : UnityEditor.Editor
	{
		private static bool isRefreshing = false;

		public static void InstallPackage(string packageName, string packageVersion)
		{
			EditorCoroutine.Start(InstallPackageCoroutine(packageName, packageVersion));
		}

		private static IEnumerator InstallPackageCoroutine(string packageName, string packageVersion)
		{
			string dependenciesPath =
				Path.Combine(VariableEditor.PackagesDirectory, VariableEditor.GoogleDependenciesFolder);
			if (!Directory.Exists(dependenciesPath))
			{
				Directory.CreateDirectory(dependenciesPath);
			}

			string localPackagePath = Path.Combine(dependenciesPath, $"{packageName}-{packageVersion}.tgz");

			if (!File.Exists(localPackagePath))
			{
				string urlFirebase = string.Format(VariableEditor.FirebaseDownloadUrl, packageName, packageVersion);
				yield return EditorCoroutine.Start(DownloadPackageCoroutine(urlFirebase, localPackagePath));
			}

			AddPackageToManifest(packageName,
				Path.Combine(VariableEditor.GoogleDependenciesFolder, $"{packageName}-{packageVersion}.tgz"));
		}

		private static IEnumerator DownloadPackageCoroutine(string url, string outputPath)
		{
			Debug.Log($"Downloading package from {url}...");

			using (WebClient client = new WebClient())
			{
				yield return client.DownloadFileTaskAsync(new Uri(url), outputPath);
			}
		}

		private static IEnumerator WaitForPackageRefreshCoroutine()
		{
			while (isRefreshing)
			{
				if (!EditorApplication.isCompiling && !EditorApplication.isUpdating)
				{
					isRefreshing = false;
				}

				yield return null;
			}
		}

		public static void RefreshPackage()
		{
			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
		}

		public static bool IsPackageInstalled(string packageName)
		{
			var manifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");

			if (!File.Exists(manifestPath))
			{
				return false;
			}

			var manifestContent = File.ReadAllText(manifestPath);

			return manifestContent.Contains(packageName);
		}

		private static void AddPackageToManifest(string packageName, string packageLocalPath)
		{
			string manifestPath = Path.Combine(VariableEditor.PackagesDirectory, "manifest.json");
			if (!File.Exists(manifestPath))
			{
				Debug.LogError("manifest.json not found!");
				return;
			}

			string manifestText = File.ReadAllText(manifestPath);

			string relativePath = packageLocalPath.Replace("\\", "/");

			string packageEntry = $"\"{packageName}\": \"file:{relativePath}\"";

			if (manifestText.Contains(packageName))
			{
				Debug.LogWarning($"{packageName} already exists in manifest.json. Overwriting...");

				manifestText = System.Text.RegularExpressions.Regex.Replace(
					manifestText,
					$"\"{packageName}\"\\s*:\\s*\"[^\"]+\"",
					packageEntry
				);
			}
			else
			{
				int dependenciesIndex = manifestText.IndexOf("\"dependencies\": {");
				if (dependenciesIndex == -1)
				{
					Debug.LogError("dependencies section not found in manifest.json!");
					return;
				}

				int insertIndex = manifestText.IndexOf('{', dependenciesIndex) + 1;
				if (manifestText[insertIndex - 1] == ',')
				{
					manifestText = manifestText.Insert(insertIndex, "\n    " + packageEntry);
				}
				else
				{
					manifestText = manifestText.Insert(insertIndex, "\n    " + packageEntry + ",");
				}
			}

			File.WriteAllText(manifestPath, manifestText);
			Debug.Log("manifest.json updated.");
		}
	}
}