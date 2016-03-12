using UnityEngine;

namespace FreeBoneSystem
{


	[ExecuteInEditMode]
	public class BoneIK : MonoBehaviour
	{
		public Transform Target;
		public bool ShowAngleLimits = true;
		[Range(0.01f, 1)]
		public float damping = 1;
		public bool FreezeTarget;
		public Vector2 SavedHeadPosition;


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
		public Vector2 EndPosition
		{
			get { return CurrentBone.Head; }
		}
		private Bone CurrentBone
		{
			get { return _currentBone; }
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

		[SerializeField]
		private int chainLength = 0;

		private Bone _currentBone;

		void Start()
		{
			_currentBone = GetComponent<Bone>();
		}

		void Update()
		{
			if (chainLength < 0)
				chainLength = 0;
		}

		public void resolveSK2D()
		{
			for (int i = 0; i < 5; i++)
			{
				Transform node = transform;
				var chainCounter = ChainLength;

				while (chainCounter > 0)
				{
					RotateTowardsTarget(node);

					if (node.parent == null)
						break;
					var bonenode = node.parent.GetComponent<Bone>();
					if (bonenode == null)
						break;

					node = node.parent;
					chainCounter--;
				}
			}
		}

		void LateUpdate()
		{
			if (!Application.isPlaying)
				Start();

			if (Target == null)
				return;

			/*int i = 0;

			while (i < 5)
			{
				resolveSK2D();
				i++;
			}*/
		}

		private Vector2 HandlePosition(Transform nodeTransform, Transform previousNodeTransform, Vector2 diff)
		{
			var bone = nodeTransform.GetComponent<Bone>();
			var resultDif = diff;
			var target = Target.position;
			if (previousNodeTransform == null && bone != null)
			{
				resultDif = target - (Vector3)bone.Head;
			}

			nodeTransform.position += (Vector3)resultDif;
			return resultDif;
		}

		void RotateTowardsTarget(Transform nodeTransform)
		{
			var bone = nodeTransform.GetComponent<Bone>();
			Vector2 toTarget = ((FreezeTarget ? (Vector3)SavedHeadPosition : Target.position) - bone.transform.position).normalized;
			Vector2 toEnd = (EndPosition - (Vector2)bone.transform.position).normalized;

			// Calculate how much we should rotate to get to the target
			float angle = SignedAngle(toEnd, toTarget);

			// "Slows" down the IK solving
			angle *= damping;

			// Wanted angle for rotation
			angle = -(angle - nodeTransform.eulerAngles.z);

			float parentRotation = nodeTransform.parent ? nodeTransform.parent.eulerAngles.z : 0;
			angle -= parentRotation;
			angle = ClampAngle(nodeTransform.eulerAngles.z, angle, bone.Limit);
			angle += parentRotation;

			//nodeTransform.rotation = Quaternion.RotateTowards(nodeTransform.rotation, Quaternion.Euler(0, 0, angle), 0.1f);
			nodeTransform.rotation = Quaternion.Euler(0, 0, angle);
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
