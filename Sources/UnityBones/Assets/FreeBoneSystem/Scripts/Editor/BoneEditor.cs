using UnityEngine;
using UnityEditor;

namespace FreeBoneSystem.Editor
{
	[CustomEditor(typeof(Bone))]
	[CanEditMultipleObjects]
	public class BoneEditor : UnityEditor.Editor
	{
		private const float HandleSize = 0.04f;
		private const float PickSize = 0.06f;

		static Bone _bone;
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
			BoneColor = serializedObject.FindProperty("BoneColor");
			LimitAngles = serializedObject.FindProperty("Limit");
			SceneView.onSceneGUIDelegate += BoneUpdate;
			_bone = (Bone)target;
		}

		void OnDisable()
		{
			SceneView.onSceneGUIDelegate -= BoneUpdate;
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
				Create();
			}
			if (GUILayout.Button(new GUIContent("Add IK")))
			{
				AddIk();
			}
			EditorGUILayout.EndHorizontal();
			serializedObject.ApplyModifiedProperties();
		}

		void BoneUpdate(SceneView sceneview)
		{
			ShowPoint();
		}

		[MenuItem("Bones/Bone")]
		public static Bone Create()
		{
			GameObject bone = new GameObject("Bone");
			Undo.RegisterCreatedObjectUndo(bone, "Add child bone");
			bone.AddComponent<Bone>();

			if (Selection.activeGameObject != null)
			{
				GameObject selectedBone = Selection.activeGameObject;
				bone.transform.parent = selectedBone.transform;

				if (selectedBone.GetComponent<Bone>() != null)
				{
					Bone p = selectedBone.GetComponent<Bone>();
					bone.transform.position = p.Head;
					bone.transform.localRotation = Quaternion.Euler(0, 0, 0);
				}
			}

			Selection.activeGameObject = bone;

			return bone.GetComponent<Bone>();
		}

		[MenuItem("Bones/BoneIk")]
		public static BoneIK AddIk()
		{
			GameObject bone;
			if (Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<Bone>() != null)
				bone = Selection.activeGameObject;
			else
				bone = new GameObject("Bone");

			if (bone.GetComponent<BoneIK>() != null)
			{
				Debug.LogError("Bone IK already exists on current bone.");
				return null;
			}

			Undo.RegisterCreatedObjectUndo(bone, "Add bone IK");
			bone.AddComponent<BoneIK>();

			if (bone != Selection.activeGameObject)
			{
				GameObject selectedBone = Selection.activeGameObject;
				bone.transform.parent = selectedBone.transform;

				if (selectedBone.GetComponent<Bone>() != null)
				{
					Bone p = selectedBone.GetComponent<Bone>();
					bone.transform.position = p.Head;
					bone.transform.localRotation = Quaternion.Euler(0, 0, 0);
				}
			}

			Selection.activeGameObject = bone;

			return bone.GetComponent<BoneIK>();
		}

		private Vector3 ShowPoint()
		{
			Vector3 point = _bone.Head;
			float size = HandleUtility.GetHandleSize(point);
			if (_bone == Selection.activeGameObject)
			{
				size *= 2f;
			}
			Handles.color = Color.white;
			if (Handles.Button(point, _bone.transform.rotation, size * HandleSize, size * PickSize, Handles.DotCap))
			{
				//_selectedIndex = index;
				Repaint();
			}

			if (true)
			{
				EditorGUI.BeginChangeCheck();
				point = Handles.DoPositionHandle(point, _bone.transform.rotation);
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(_bone, "Move Head");
					EditorUtility.SetDirty(_bone);
					_bone.Head = _bone.transform.InverseTransformPoint(point);
				}
			}
			return point;
		}


		[DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
		static void DrawBone(Bone scr, GizmoType gizmoType)
		{
			if (scr.gameObject == Selection.activeGameObject)
			{
				Handles.color = Color.yellow;
			}
			else
			{
				Color c = scr.BoneColor;
				c.a = 1;
				Handles.color = c;
			}

			int div = 5;
			Vector3 v = Quaternion.AngleAxis(45, Vector3.forward) * (((Vector3)scr.Head - scr.transform.position) / div);
			Handles.DrawLine(scr.transform.position, scr.transform.position + v);
			Handles.DrawLine(scr.transform.position + v, scr.Head);

			v = Quaternion.AngleAxis(-45, Vector3.forward) * (((Vector3)scr.Head - scr.transform.position) / div);
			Handles.DrawLine(scr.transform.position, scr.transform.position + v);
			Handles.DrawLine(scr.transform.position + v, scr.Head);

			Handles.DrawLine(scr.transform.position, scr.Head);

			Handles.color = new Color(Handles.color.r, Handles.color.g, Handles.color.b, 0.5f);
		}
	}
}
