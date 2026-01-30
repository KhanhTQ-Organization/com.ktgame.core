using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

namespace com.ktgame.core.editor
{
    public class EnumGenerator
    {
        /// <summary>
        /// Adds a new key to an enum class located by its file name.
        /// </summary>
        /// <param name="className">The name of the script file that contains the enum.</param>
        /// <param name="newEnumKey">The new key to be added to the enum.</param>
        public static void Generate(string className, string newEnumKey)
        {
            string[] guids = AssetDatabase.FindAssets($"{className} t:Script");
            if (guids.Length == 0)
            {
                throw new FileNotFoundException($"Script file containing class '{className}' not found.");
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            if (!File.Exists(path))
            {
                throw new IOException($"Unable to read file at path: {path}");
            }

            var lines = File.ReadAllLines(path);
            var output = new List<string>();
            var existingKeys = new HashSet<string>();
            int insertIndex = -1;

            // Regex to match enum keys, with or without a comma
            Regex enumKeyRegex = new Regex(@"^\s*(\w+)\s*,?\s*$");

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                output.Add(line);

                Match match = enumKeyRegex.Match(line);
                if (match.Success)
                {
                    string key = match.Groups[1].Value;
                    existingKeys.Add(key);

                    // Ensure the last enum entry has a trailing comma
                    if (i + 1 < lines.Length && lines[i + 1].Trim() == "}" && !line.Trim().EndsWith(","))
                    {
                        output[i] = line + ",";
                    }
                }

                if (insertIndex == -1 && line.Trim() == "}")
                {
                    insertIndex = output.Count - 1;
                }
            }

            if (existingKeys.Contains(newEnumKey))
            {
                throw new InvalidOperationException($"The key '{newEnumKey}' already exists in enum '{className}'.");
            }

            if (insertIndex == -1)
            {
                throw new InvalidDataException("Could not locate the enum closing bracket to insert the new key.");
            }

            output.Insert(insertIndex, $"\t\t{newEnumKey},");
            File.WriteAllLines(path, output);
            AssetDatabase.Refresh();
        }
    }
}