using System.Collections;
using UnityEditor;

namespace com.ktgame.core.editor
{
	public class EditorCoroutine
	{
		public static EditorCoroutine Start(IEnumerator routine)
		{
			EditorCoroutine coroutine = new EditorCoroutine(routine);
			coroutine.Start();
			return coroutine;
		}

		private readonly IEnumerator routine;
		private EditorCoroutine(IEnumerator routine)
		{
			this.routine = routine;
		}

		private void Start()
		{
			EditorApplication.update += Update;
		}

		private void Stop()
		{
			EditorApplication.update -= Update;
		}

		private void Update()
		{
			if (!routine.MoveNext())
			{
				Stop();
			}
		}
	}
}
