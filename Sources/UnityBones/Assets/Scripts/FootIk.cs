﻿using System;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Assets.Bones.Scripts
{
	[ExecuteInEditMode]
	public class FootIk : MonoBehaviour
	{
		[HideInInspector]
		public float influence = 1.0f;
		public int chainLength = 0;
		public Transform Target;
		public bool ShowAngleLimits = true;
		private Bone _currentBone
		{
			get { return GetComponent<Bone>(); }
		}
		[Range(0.01f, 1)]
		public float damping = 1;
		public Transform RootBone
		{
			get
			{
				Transform root = null;

				if (chainLength == 0)
				{
					root = transform.root;
				}
				else
				{
					int n = chainLength;
					root = transform;
					while (n-- > 0)
					{
						if (root.parent == null)
							break;
						else
							root = root.parent;
					}
				}
				return root;
			}
		}
		public Vector2 endPosition
		{
			get { return _currentBone.Head; }
		}
		private int ChainLength
		{
			get
			{
				if (chainLength > 0)
					return chainLength;
				else
				{
					int n = 0;
					var parent = transform.parent;
					while (parent != null && parent.gameObject.GetComponent<Bone>() != null)
					{
						n++;
						parent = parent.parent;
					}
					return n + 1;
				}
			}
		}

		[Serializable]
		public class FootParameters
		{
			public Transform StartPoint;
			public Transform EndPoint;
		}

		public FootParameters _currentFootParameters;

		public bool FreezeTarget;
		public Vector2 SavedHeadPosition;

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

		// Use this for initialization
		void Start()
		{

		}

		void Update()
		{
			//_currentBone = GetComponent<Bone>();
			if (chainLength < 0)
				chainLength = 0;
		}

		/**
		 * Code ported from the Gamemaker tool SK2D by Drifter
		 * http://gmc.yoyogames.com/index.php?showtopic=462301
		 * 
		 **/

		public void resolveSK2D()
		{
			Transform node = transform;
			var chainCounter = ChainLength;

			while (chainCounter > 0)
			{
				RotateTowardsTarget(node);
				var bonenode = node.parent.GetComponent<Bone>();
				if (bonenode == null)
					break;

				node = node.parent;
				chainCounter--;
			}
		}

		private void RotateFoot(RaycastHit2D footHit)
		{
			var needVector = Quaternion.Euler(0, 0, -90) * footHit.normal;
			var angleToRotate = Vector2.Angle(GetFootEndPoint() - footHit.point,
				(Vector2)needVector);
			if (Mathf.Abs(angleToRotate) > 80f)
				return;

				Vector2 vec = _currentBone._head + (_currentBone.Head - (Vector2)_currentFootParameters.StartPoint.position);
				vec = Quaternion.Euler(0, 0, angleToRotate) * vec;
				FreezeTarget = true;
				SavedHeadPosition = (Vector2)transform.position + vec;
		}

		void LateUpdate()
		{
			if (!Application.isPlaying)
				Start();

			if (Target == null)
				return;

			int i = 0;

			while (i < 5)
			{
				resolveSK2D();
				i++;
			}
		}

		void RotateTowardsTarget(Transform nodeTransform)
		{
			var bone = nodeTransform.GetComponent<Bone>();
			Vector2 toTarget = ((FreezeTarget ? (Vector3)SavedHeadPosition : Target.position) - bone.transform.position).normalized;
			Vector2 toEnd = (endPosition - (Vector2)bone.transform.position).normalized;

			// Calculate how much we should rotate to get to the target
			float angle = SignedAngle(toEnd, toTarget);

			// Flip sign if character is turned around
			//angle *= Mathf.Sign(bone.transform.root.localScale.x);

			// "Slows" down the IK solving
			angle *= damping;

			// Wanted angle for rotation
			angle = -(angle - nodeTransform.eulerAngles.z);
			/*if (Mathf.Abs(angle)< 1f)
				return;*/
			float parentRotation = nodeTransform.parent ? nodeTransform.parent.eulerAngles.z : 0;
			angle -= parentRotation;
			angle = ClampAngle(nodeTransform.eulerAngles.z, angle, bone.Limit);
			angle += parentRotation;
			var currentAngle = nodeTransform.rotation.eulerAngles.z;
			var angleToRotate = 0f;
			float sign = 1f;
			if (bone.Limit.x > bone.Limit.y &&
				(currentAngle > angle && angle <= bone.Limit.y && currentAngle >= bone.Limit.x ||
				angle > currentAngle && angle >= bone.Limit.y && currentAngle <= bone.Limit.x))
			{
				angleToRotate = 360f - currentAngle + angle;
				//angleToRotate = 360f;
				sign = currentAngle > angle ? 1f : -1f;
			}
			else
			{
				angleToRotate = Mathf.Abs(currentAngle - angle);
				//angleToRotate = angle;
				sign = Mathf.Sign(Mathf.DeltaAngle(currentAngle, angle));
			}
			var savedRotation = nodeTransform.rotation;
			nodeTransform.rotation = Quaternion.RotateTowards(nodeTransform.rotation, Quaternion.Euler(0, 0, angle), 1f);

			var footHit = Physics2D.Linecast(GetFootStartPoint(), GetFootEndPoint(), LayerMask.GetMask("Ground"));

			if (!FreezeTarget && footHit.collider != null)
			{
				nodeTransform.rotation = savedRotation;
			}
			
		}

		void OnDrawGizmos()
		{
			if (ShowAngleLimits && Selection.activeGameObject == gameObject || Selection.activeGameObject == Target.gameObject)
			{
				var nodes = GetComponentsInParent<Bone>().ToList();
				nodes.Add(GetComponent<Bone>());
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
					
					Handles.DrawLine(position, position + Target.transform.parent.position);
				}

			}

				if (FreezeTarget)
				{
					Handles.DrawSolidDisc(SavedHeadPosition, Vector3.forward, 0.05f);
				}
		}

		public float SignedAngle(Vector3 a, Vector3 b)
		{
			float angle = Vector3.Angle(a, b);
			float sign = Mathf.Sign(Vector3.Dot(Vector3.back, Vector3.Cross(a, b)));

			return angle * sign;
		}

		float ClampAngle(float currentAngle, float angle, Vector2 limits)
		{
			angle = Mathf.Abs((angle % 360) + 360) % 360;
			bool throwZero = limits.x > limits.y;

			if (!throwZero && (angle >= limits.x && angle <= limits.y) ||
				throwZero && (angle >= limits.x && angle <= 360f ||
				  angle >= 0 && angle <= limits.y))
				return angle;

			if (Mathf.Abs(currentAngle - limits.x) < 0.01f || Mathf.Abs(currentAngle - limits.y) < 0.01f)
				return currentAngle;

			if (!throwZero)
			{
				var maxMinDif = Mathf.Min(Mathf.Abs(360 - limits.y + angle), Mathf.Abs(angle - limits.y));
				var minMinDif = Mathf.Min(Mathf.Abs(360 - angle + limits.x), Mathf.Abs(angle - limits.x));
				return minMinDif < maxMinDif ? limits.x : limits.y;
			}
			else
			{
				var maxMinDif = Mathf.Min(Mathf.Abs(360 - limits.x + angle), Mathf.Abs(angle - limits.x));
				var minMinDif = Mathf.Min(Mathf.Abs(360 - limits.y + angle), Mathf.Abs(angle - limits.y));
				return minMinDif < maxMinDif ? limits.y : limits.x;
			}
		}

		private Vector2 GetFootStartPoint()
		{
			return _currentFootParameters.StartPoint.position;
		}

		private Vector2 GetFootEndPoint()
		{
			return _currentFootParameters.EndPoint.position;
		}
	}
}