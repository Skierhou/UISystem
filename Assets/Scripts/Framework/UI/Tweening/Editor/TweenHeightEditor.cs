#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TweenHeight))]
public class TweenHeightEditor : UITweenerEditor
{
	public override void OnInspectorGUI ()
	{
		GUILayout.Space(6f);
		EditorGUIUtility.labelWidth = 120f;

		TweenHeight tw = target as TweenHeight;
		GUI.changed = false;

		EditorGUILayout.BeginHorizontal();
		EditorGUI.BeginDisabledGroup(tw.fromTarget != null);
		var from = EditorGUILayout.FloatField("From", tw.from);
		EditorGUI.EndDisabledGroup();
		var fc = (RectTransform)EditorGUILayout.ObjectField(tw.fromTarget, typeof(RectTransform), true, GUILayout.Width(110f));
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		EditorGUI.BeginDisabledGroup(tw.toTarget != null);
		var to = EditorGUILayout.FloatField("To", tw.to);
		EditorGUI.EndDisabledGroup();
		var tc = (RectTransform)EditorGUILayout.ObjectField(tw.toTarget, typeof(RectTransform), true, GUILayout.Width(110f));
		EditorGUILayout.EndHorizontal();

		if (from < 0) from = 0;
		if (to < 0) to = 0;

		if (GUI.changed)
		{
			//NGUIEditorTools.RegisterUndo("Tween Change", tw);
			tw.from = from;
			tw.to = to;
			tw.fromTarget = fc;
			tw.toTarget = tc;
			//NGUITools.SetDirty(tw);
		}

		DrawCommonProperties();
	}
}
#endif
