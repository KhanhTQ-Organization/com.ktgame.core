namespace com.ktgame.core.editor
{
	public struct VariableEditor
	{
		public static string ExternalDependencyManagerName = "com.google.external-dependency-manager";

		public static string[] FirebasePackageName = new[]
		{
			"com.google.firebase.app",
			"com.google.firebase.analytics",
			"com.google.firebase.remote-config",
			"com.google.firebase.crashlytics"
		};

		public const string FirebaseDownloadUrl = "https://dl.google.com/games/registry/unity/{0}/{0}-{1}.tgz";

		public const string PackagesDirectory = "Packages";
		public const string GoogleDependenciesFolder = "GoogleDependencies";
	}
}
