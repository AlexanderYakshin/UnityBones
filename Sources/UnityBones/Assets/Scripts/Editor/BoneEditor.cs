using UnityEngine;
using UnityEditor;
using Assets.Bones.Scripts;

namespace Assets.Bones
{
	[CustomEditor(typeof(Bone))]
	[CanEditMultipleObjects]
	public class BoneEditor : Editor
	{
		Bone _bone;
		SerializedProperty BoneLength;
		SerializedProperty LocalHead;
		SerializedProperty SnapToParent;
		SerializedProperty BoneColor;
		SerializedProperty LimitAngles;

		private Transform _boneHaedPositionHelper;
		private bool _targetObjectHelperOn;

		void OnEnable()
		{
			// Setup the SerializedProperties.
			BoneLength = serializedObject.FindProperty("Length");
			LocalHead = serializedObject.FindProperty("_head");
			SnapToParent = serializedObject.FindProperty("_snapToParent");
			BoneColor = serializedObject.FindProperty("_boneColor");
			LimitAngles = serializedObject.FindProperty("Limit");
			SceneView.onSceneGUIDelegate = BoneUpdate;
			_bone = (Bone)target;
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.SelectableLabel("Length: " + _bone.Length);
			LocalHead.vector2Value = EditorGUILayout.Vector2Field("Head", LocalHead.vector2Value);
			EditorGUILayout.PropertyField(SnapToParent);
			BoneColor.colorValue = EditorGUILayout.ColorField("Color", BoneColor.colorValue);

			EditorGUILayout.LabelField("Angle Limit");

			EditorGUILayout.BeginHorizontal();
			var min = EditorGUILayout.FloatField("Min", LimitAngles.vector2Value.x);
			var max = EditorGUILayout.FloatField("Max", LimitAngles.vector2Value.y);
			EditorGUILayout.EndHorizontal();

			if (min < 0)
			{
				min = min + 360;
			}
			if (max < 0)
			{
				max = max + 360;
			}

			LimitAngles.vector2Value = new Vector2(min, max);

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button(new GUIContent("Add child")))
			{
				Bone.Create();
			}
			if (GUILayout.Button(new GUIContent("Add IK")))
			{
				Bone.AddIk();
			}
			EditorGUILayout.EndHorizontal();
			serializedObject.ApplyModifiedProperties();
		}

		void BoneUpdate(SceneView sceneview)
		{
			Event e = Event.current;
			if (e.keyCode == KeyCode.B && e.type == EventType.keyDown && !_targetObjectHelperOn)
			{

				if (!_targetObjectHelperOn)
				{
					_targetObjectHelperOn = true;
					if (_boneHaedPositionHelper == null)
					{
						_boneHaedPositionHelper = new GameObject(_bone.gameObject.name + "_PositionHelper").transform;
						_boneHaedPositionHelper.transform.parent = _bone.transform;
					}
					_boneHaedPositionHelper.position = _bone.Head;
					Selection.activeObject = _boneHaedPositionHelper;
				}
			}
			else if (e.keyCode == KeyCode.B && e.type == EventType.KeyUp)
			{
				_targetObjectHelperOn = false;
				if (_boneHaedPositionHelper != null)
					DestroyImmediate(_boneHaedPositionHelper.gameObject);
			}

			if (_targetObjectHelperOn && _boneHaedPositionHelper != null)
			{
				serializedObject.Update();
				_bone.Head = _boneHaedPositionHelper.localPosition;
				serializedObject.ApplyModifiedProperties();
			}
		}

	}
}
