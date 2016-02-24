using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Assets.Bones.Scripts
{
	[ExecuteInEditMode]
	public class Bone : MonoBehaviour
	{
		private Bone parent;
		public float Length
		{
			get
			{
				return (_head).magnitude;
			}
		}
		[SerializeField]
		public Vector2 _head;
		public Vector2 Head
		{
			get
			{
				return (transform.position + transform.rotation * (Vector3)_head);
			}
			set
			{
				_head = value;
			}
		}

		[SerializeField]
		private bool _snapToParent;
		[SerializeField]
		private Color _boneColor;

		private Transform BoneHaedPositionHelper;
		private bool _targetObjectHelperOn;

		public Vector2 AngleLimits = new Vector2(0f, 360f);

		public Vector2 Limit;

#if UNITY_EDITOR
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
#endif
		void OnEnable()
		{
#if UNITY_EDITOR
			Start();
#endif
		}

		void Start()
		{
			if (gameObject.transform.parent != null)
				parent = gameObject.transform.parent.GetComponent<Bone>();
		}

		void Update()
		{
			if (Application.isEditor && _snapToParent && parent != null)
			{
				gameObject.transform.position = parent.Head;
			}
		}

		void OnDrawGizmos()
		{
			if (gameObject.Equals(Selection.activeGameObject))
			{
				Gizmos.color = Color.yellow;
			}
			else
			{
				Color c = _boneColor;
				c.a = 1;

				Gizmos.color = c;
			}

			int div = 5;
			Vector3 v = Quaternion.AngleAxis(45, Vector3.forward) * (((Vector3)Head - gameObject.transform.position) / div);
			Gizmos.DrawLine(gameObject.transform.position, gameObject.transform.position + v);
			Gizmos.DrawLine(gameObject.transform.position + v, Head);

			v = Quaternion.AngleAxis(-45, Vector3.forward) * (((Vector3)Head - gameObject.transform.position) / div);
			Gizmos.DrawLine(gameObject.transform.position, gameObject.transform.position + v);
			Gizmos.DrawLine(gameObject.transform.position + v, Head);

			Gizmos.DrawLine(gameObject.transform.position, Head);

			Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.5f);
		}
	}
}

