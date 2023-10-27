#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TweenVolume))]
public class TweenVolumeEditor : UITweenerEditor
{
	public override void OnInspectorGUI ()
	{
		GUILayout.Space(6f);
		EditorGUIUtility.labelWidth = 120f;

		TweenVolume tw = target as TweenVolume;
		GUI.changed = false;

		float from = EditorGUILayout.Slider("From", tw.from, 0f, 1f);
		float to = EditorGUILayout.Slider("To", tw.to, 0f, 1f);

		if (GUI.changed)
		{
			//NGUIEditorTools.RegisterUndo("Tween Change", tw);
			tw.from = from;
			tw.to = to;
			//NGUITools.SetDirty(tw);
		}

		DrawCommonProperties();
	}
}
#endif
