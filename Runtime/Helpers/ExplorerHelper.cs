using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;

public class ExplorerHelper
{
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || MACOS
    private const bool IsMac = true;
#else
	private const bool IsMac = false;
#endif

	[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
	private static extern int SHParseDisplayName(string name,
		IntPtr bindingContext,
		out IntPtr pidl,
		uint sfgaoIn,
		out uint psfgaoOut);

	[DllImport("shell32.dll")]
	private static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder,
		uint cidl,
		[In, MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl,
		uint dwFlags);

	[DllImport("ole32.dll")]
	private static extern void CoTaskMemFree(IntPtr ptr);

	public static void OpenAndSelectFile(string folderPath, string fileName)
	{
		if (!Directory.Exists(folderPath))
			return;

		string filePath = Path.Combine(folderPath, fileName);

		if (IsMac)
		{
			// macOS: dùng AppleScript để reveal file trong Finder
			string script = $"tell application \"Finder\" to reveal POSIX file \"{filePath}\"";
			RunAppleScript(script);

			// Đồng thời focus Finder
			RunAppleScript("tell application \"Finder\" to activate");
		}
		else
		{
			SHParseDisplayName(folderPath, IntPtr.Zero, out IntPtr pidlFolder, 0, out _);
			SHParseDisplayName(filePath, IntPtr.Zero, out IntPtr pidlFile, 0, out _);

			IntPtr[] fileArray = { pidlFile };
			SHOpenFolderAndSelectItems(pidlFolder, (uint)fileArray.Length, fileArray, 0);

			if (pidlFolder != IntPtr.Zero)
				CoTaskMemFree(pidlFolder);
			if (pidlFile != IntPtr.Zero)
				CoTaskMemFree(pidlFile);
		}
	}

	private static void RunAppleScript(string script)
	{
		ProcessStartInfo startInfo = new ProcessStartInfo
		{
			FileName = "osascript",
			Arguments = $"-e \"{script}\"",
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true
		};
		Process.Start(startInfo);
	}
}