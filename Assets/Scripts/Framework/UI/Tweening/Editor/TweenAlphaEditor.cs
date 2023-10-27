#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TweenAlpha))]
public class TweenAlphaEditor : UITweenerEditor
{
	public override void OnInspectorGUI ()
	{
		GUILayout.Space(6f);
		EditorGUIUtility.labelWidth = 120f;

		TweenAlpha tw = target as TweenAlpha;
		GUI.changed = false;

		var from = EditorGUILayout.Slider("From", tw.from, 0f, 1f);
		var to = EditorGUILayout.Slider("To", tw.to, 0f, 1f);

		var ds = tw.autoCleanup;
		var pn = tw.colorProperty;

		if (tw.GetComponent<MeshRenderer>() != null)
		{
			ds = EditorGUILayout.Toggle("Auto-cleanup", tw.autoCleanup);
			pn = EditorGUILayout.TextField("Color Property", tw.colorProperty);
		}

		if (GUI.changed)
		{
			//NGUIEditorTools.RegisterUndo("Tween Change", tw);
			tw.from = from;
			tw.to = to;
			tw.autoCleanup = ds;
			tw.colorProperty = pn;
			//NGUITools.SetDirty(tw);
		}

		DrawCommonProperties();
	}
}
#endif
