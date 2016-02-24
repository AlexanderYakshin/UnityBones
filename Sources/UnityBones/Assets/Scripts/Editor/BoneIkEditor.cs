using UnityEngine;
using UnityEditor;
using Assets.Bones.Scripts;

namespace Assets.Bones
{
	[CustomEditor(typeof(BoneIK))]
	public class BoneIkEditor: Editor
	{
		private Bone _bone;
		private BoneIK _boneIk;

		private SerializedProperty IkTarget;
		private SerializedProperty ShowAngleLimits;
		private SerializedProperty ChainLength;
		void OnEnable()
		{
			// Setup the SerializedProperties.
			IkTarget = serializedObject.FindProperty("Target");
			ShowAngleLimits = serializedObject.FindProperty("ShowAngleLimits");
			ChainLength = serializedObject.FindProperty("chainLength");
			_boneIk = (BoneIK)target;
			_bone = _boneIk.GetComponent<Bone>();
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(ChainLength);
			EditorGUILayout.PropertyField(IkTarget);
			EditorGUILayout.PropertyField(ShowAngleLimits);

			EditorGUILayout.BeginHorizontal();
			if (IkTarget.objectReferenceValue == null && GUILayout.Button(new GUIContent("Create Target")))
			{
				BoneIK.CreateTarget();
			}
			EditorGUILayout.EndHorizontal();
			serializedObject.ApplyModifiedProperties();

			DrawDefaultInspector();
		}
	}
}
