#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TweenTransform))]
public class TweenTransformEditor : UITweenerEditor
{
}
#endif
