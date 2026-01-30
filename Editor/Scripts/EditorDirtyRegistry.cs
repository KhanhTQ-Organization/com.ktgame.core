using System.Collections.Generic;

namespace com.ktgame.core.editor
{
	public static class EditorDirtyRegistry
	{
		private static readonly List<IEditorDirtyHandler> _handlers = new();

		public static void Register(IEditorDirtyHandler handler)
		{
			if (!_handlers.Contains(handler))
				_handlers.Add(handler);
		}

		public static void SetDirtyAll()
		{
			foreach (var handler in _handlers)
			{
				handler.SetDirty();
			}
		}
	}
}