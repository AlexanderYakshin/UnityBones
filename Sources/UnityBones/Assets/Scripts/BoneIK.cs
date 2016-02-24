using System;
using UnityEngine;
using System.Collections;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Assets.Bones.Scripts
{
	[ExecuteInEditMode]
	public class BoneIK : MonoBehaviour
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

		public bool CalculateFootCollisions;

		[Serializable]
		public class FootParameters
		{
			public Transform StartPoint;
			public Transform EndPoint;
			public FootIk FootBoneIk;
		}

		public FootParameters _currentFootParameters;
		public BoxCollider2D FootCollider;
		public bool FreezeTarget;
		public Vector2 SavedHeadPosition;

		private bool _alreadyRotate;

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

			if (CalculateFootCollisions)
			{
				_currentFootParameters.FootBoneIk.FreezeTarget = false;
				FreezeTarget = false;
				var footHit = Physics2D.Linecast(GetFootStartPoint(), GetFootEndPoint(), LayerMask.GetMask("Ground"));

				if (footHit.collider != null)
				{
					RotateFoot(footHit);
					Vector3 lineInters;

					var rays = Physics2D.LinecastAll(_currentBone.transform.position, Target.position);
					var vectorStartHead = _currentBone.Head - (Vector2)_currentFootParameters.StartPoint.position;
					var neededRay = rays.FirstOrDefault(ray => ray.collider != null && ray.collider == footHit.collider);
					if (neededRay.collider != null)
					{
						FreezeTarget = true;
						SavedHeadPosition = neededRay.point + vectorStartHead;
					}
					else
					{
						if (LineLineIntersection(out lineInters, _currentBone.transform.position, Target.position, GetFootStartPoint(),
						  (GetFootEndPoint() - GetFootStartPoint()).normalized * 3f)
							   || LineLineIntersection(out lineInters, _currentBone.transform.position, Target.position, GetFootEndPoint(),
								   (GetFootStartPoint() - GetFootEndPoint()).normalized * 3f))
						{
							if ((lineInters - _currentBone.transform.position).magnitude <
								((Vector3)Target.position - _currentBone.transform.position).magnitude)
							{
								FreezeTarget = true;
								SavedHeadPosition = (Vector2)lineInters + vectorStartHead;
							}
						}
					}
				}
			}

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
			if (_currentFootParameters.FootBoneIk != null)
			{
				Vector2 vec = _currentFootParameters.FootBoneIk.GetComponent<Bone>()._head + (_currentFootParameters.FootBoneIk.GetComponent<Bone>().Head - (Vector2)_currentFootParameters.StartPoint.position);
				vec = Quaternion.Euler(0, 0, angleToRotate) * vec;
				_currentFootParameters.FootBoneIk.FreezeTarget = true;
				_currentFootParameters.FootBoneIk.SavedHeadPosition = (Vector2)_currentFootParameters.FootBoneIk.transform.position + vec;
			}
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

			var halfAngleToRotate = Mathf.Ceil(angleToRotate / 2f);

			nodeTransform.rotation = Quaternion.RotateTowards(nodeTransform.rotation, Quaternion.Euler(0, 0, angle), 1f);
			//nodeTransform.rotation = Quaternion.Euler(0, 0, angle);
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

					//Vector3 toChild = FindChildNode(transform, target.endTransform).position - position;
					Handles.DrawLine(position, position + Target.transform.parent.position);
				}

			}
			if (FootCollider != null)
			{
				var pref = FootCollider.size.x / 10f;
				for (int i = 0; i < 10; i++)
				{
					var vec = i < 5 ? new Vector2((FootCollider.size.x / 2f - pref * i) * -1, 0f) : new Vector2(pref * (i - 4), 0f);
					Vector2 origin = (Vector2)FootCollider.transform.position + (Vector2)(FootCollider.transform.rotation * (FootCollider.offset + vec));

					Handles.DrawSolidDisc(origin, Vector3.forward, 0.02f);
					Handles.DrawLine(origin, origin + ((Vector2)Target.position - origin).normalized * 0.1f);
				}
			}

			if (CalculateFootCollisions)
			{
				Handles.DrawLine(GetFootStartPoint(), GetFootEndPoint());
				var footHit = Physics2D.Linecast(GetFootStartPoint(),
					GetFootEndPoint(), LayerMask.GetMask("Ground"));

				if (footHit.collider != null)
				{
					Handles.DrawSolidDisc(footHit.point, Vector3.forward, 0.05f);
					Handles.DrawLine(footHit.point, footHit.point + footHit.normal);
					var needVector = Quaternion.Euler(0, 0, -90) * footHit.normal;
					Handles.color = Color.yellow;
					Handles.DrawLine(footHit.point, footHit.point + (Vector2)needVector);
					Handles.DrawLine(footHit.point, GetFootEndPoint());
					var angleToRotate = Vector2.Angle(GetFootEndPoint() - footHit.point,
						(Vector2)needVector);
					Debug.Log(angleToRotate);
					Handles.DrawWireArc(footHit.point, Vector3.forward,
						(GetFootEndPoint() - footHit.point).normalized, angleToRotate, 0.2f);

					Handles.color = Color.blue;

					Handles.DrawLine(_currentBone.transform.position, Target.position);
					//Handles.DrawLine(GetFootStartPoint(), GetFootStartPoint() + (GetFootEndPoint() - GetFootStartPoint()).normalized * 3f);
					Handles.DrawLine(GetFootEndPoint(), GetFootStartPoint() + (GetFootStartPoint() - GetFootEndPoint()).normalized * 3f);
					Vector3 lineInters;

					Debug.Log("First: " + LineLineIntersection(out lineInters, _currentBone.transform.position, _currentBone.transform.rotation * (Vector3)_currentBone._head, GetFootStartPoint(),
						(GetFootEndPoint() - GetFootStartPoint()).normalized));

					Debug.Log("Second: " + LineLineIntersection(out lineInters, _currentBone.transform.position, _currentBone.transform.rotation * (Vector3)_currentBone._head, GetFootEndPoint(),
								 (GetFootStartPoint() - GetFootEndPoint()).normalized));
					if (LineLineIntersection(out lineInters, _currentBone.transform.position, Target.position, GetFootStartPoint(),
						(GetFootEndPoint() - GetFootStartPoint()).normalized * 3f)
							 || LineLineIntersection(out lineInters, _currentBone.transform.position, Target.position, GetFootEndPoint(),
								 (GetFootStartPoint() - GetFootEndPoint()).normalized * 3f))
					{
						Handles.color = Color.black;
						Handles.DrawSolidDisc(lineInters, Vector3.forward, 0.1f);
					}
				}
			}
			else
			{
				if (FreezeTarget)
				{
					Handles.DrawSolidDisc(SavedHeadPosition, Vector3.forward, 0.05f);
				}
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

		public static bool LineLineIntersection(out Vector3 intersection, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
		{

			intersection = Vector3.zero;

			Vector3 lineVec3 = linePoint2 - linePoint1;
			Vector3 crossVec1and2 = Vector3.Cross(lineVec1, lineVec2);
			Vector3 crossVec3and2 = Vector3.Cross(lineVec3, lineVec2);

			float planarFactor = Vector3.Dot(lineVec3, crossVec1and2);

			//Lines are not coplanar. Take into account rounding errors.
			if ((planarFactor >= 0.00001f) || (planarFactor <= -0.00001f))
			{

				return false;
			}

			//Note: sqrMagnitude does x*x+y*y+z*z on the input vector.
			float s = Vector3.Dot(crossVec3and2, crossVec1and2) / crossVec1and2.sqrMagnitude;

			if ((s >= 0.0f) && (s <= 1.0f))
			{

				intersection = linePoint1 + (lineVec1 * s);
				return true;
			}

			else
			{
				return false;
			}
		}
	}
}
