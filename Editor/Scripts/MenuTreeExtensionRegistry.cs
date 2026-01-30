using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;

namespace com.ktgame.core.editor
{
	public static class MenuTreeExtensionRegistry
	{
		private static readonly List<IMenuTreeExtension> _extensions = new();

		public static void Register(IMenuTreeExtension extension)
		{
			if (!_extensions.Contains(extension))
			{
				_extensions.Add(extension);
			}
		}

		public static void BuildAll(OdinMenuTree tree)
		{
			foreach (var ext in _extensions)
			{
				ext.BuildMenu(tree);
			}
		}
	}
}