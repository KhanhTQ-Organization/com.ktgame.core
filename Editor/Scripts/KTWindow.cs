using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace com.ktgame.core.editor
{
	public class KTWindow : OdinMenuEditorWindow
	{
		public static KTSettingSO Setting =>
			AssetDatabase.LoadAssetAtPath<KTSettingSO>(
				$"{KTEditor.SdkPath}Editor/Resources/KTSettings.asset");

		[MenuItem("Ktgame/Window")]
		private static void Open()
		{
			var window = GetWindow<KTWindow>("KTGame SDK");
			window.minSize = new Vector2(400, 400);
			window.maxSize = new Vector2(1000, 1080);

			window.titleContent = new GUIContent("KTGame SDK", AssetDatabase.LoadAssetAtPath<Texture>($"{KTEditor.SdkPath}Editor/Textures/logo_icon.png"));

			window.Show();
		}

		protected override OdinMenuTree BuildMenuTree()
		{
			var tree = new OdinMenuTree
			{
				Selection = { SupportsMultiSelect = false }
			};
			
			tree.Add("General", new GeneralEditor(Setting), SdfIconType.GearFill);
			Debug.Log("Tree General");
			MenuTreeExtensionRegistry.BuildAll(tree);

			return tree;
		}

		protected override void OnEndDrawEditors()
		{
			base.OnEndDrawEditors();

			if (!GUI.changed)
			{
				return;
			}

			EditorUtility.SetDirty(Setting);
			EditorDirtyRegistry.SetDirtyAll();
		}
	}
}
