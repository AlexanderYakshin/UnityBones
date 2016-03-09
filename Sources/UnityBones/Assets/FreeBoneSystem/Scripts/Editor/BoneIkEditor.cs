using System.Linq;
using UnityEngine;
using UnityEditor;

namespace FreeBoneSystem.Editor
{
	[CustomEditor(typeof(BoneIK))]
	public class BoneIkEditor: UnityEditor.Editor
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
				CreateTarget();
			}
			EditorGUILayout.EndHorizontal();
			serializedObject.ApplyModifiedProperties();

			DrawDefaultInspector();
		}

		[MenuItem("Bones/BoneIk/Create Target")]
		public static Transform CreateTarget()
		{
			bool isSceletSelected = false;
			var activeObject = Selection.activeGameObject;
			if (activeObject == null || activeObject.GetComponent<BoneIK>() == null)
				return null;

			var selectedBoneIk = activeObject.GetComponent<BoneIK>();

			var boneRootGo = selectedBoneIk.transform.root;
			var ikTransform = boneRootGo.FindChild("IK");
			if (ikTransform == null)
			{
				ikTransform = new GameObject("IK").transform;
				ikTransform.parent = boneRootGo;
			}

			var target = new GameObject(selectedBoneIk.name + "_target").transform;
			target.parent = ikTransform;

			selectedBoneIk.Target = target;
			return target;
		}

		[DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
		static void DrawBoneIKGizmo(BoneIK scr, GizmoType gizmoType)
		{
			if (scr.Target != null && scr.ShowAngleLimits && (Selection.activeGameObject == scr.gameObject || Selection.activeGameObject == scr.Target.gameObject))
			{
				var nodes = scr.GetComponentsInParent<Bone>().ToList();
				nodes.Add(scr.GetComponent<Bone>());
				foreach (var node1 in nodes)
				{
					if (node1.transform == null)
						continue;

					Transform nodetransform = node1.transform;
					Vector3 position = nodetransform.position;

					float handleSize = HandleUtility.GetHandleSize(position);
					float discSize = handleSize * 0.5f;

					float parentRotation = nodetransform.parent != null && nodetransform.parent.GetComponent<Bone>() != null ? nodetransform.parent.eulerAngles.z : 0;
					bool throwZero = node1.Limit.x > node1.Limit.y;
					var minAngle = throwZero ? node1.Limit.x - 360f : node1.Limit.x;
					var maxAngle = node1.Limit.y;
					Vector3 min = Quaternion.Euler(0, 0, minAngle + parentRotation) * node1._head.normalized;
					Vector3 max = Quaternion.Euler(0, 0, maxAngle + parentRotation) * node1._head.normalized;

					Handles.color = new Color(0, 1, 0, 0.1f);
					Handles.DrawWireDisc(position, Vector3.back, discSize);
					Handles.DrawSolidArc(position, Vector3.forward, min, maxAngle - minAngle, discSize);

					Handles.color = Color.green;
					Handles.DrawLine(position, position + min * discSize);
					Handles.DrawLine(position, position + max * discSize);
					
					Handles.DrawLine(position, position + scr.Target.transform.parent.position);
				}

			}
		}
	}
}
