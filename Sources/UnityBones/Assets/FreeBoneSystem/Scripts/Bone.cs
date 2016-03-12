using UnityEngine;

namespace FreeBoneSystem
{
	[ExecuteInEditMode]
	public class Bone : MonoBehaviour
	{
		public float Length
		{
			get
			{
				return (_head).magnitude;
			}
		}

		private Bone parent;
		
		[SerializeField]
		public Vector2 _head;
		public Vector2 Head
		{
			get
			{
				return transform.TransformPoint(_head); //+ transform.rotation * (Vector3)_head);
			}
			set
			{
				_head = value;
			}
		}

		[SerializeField]
		private bool _snapToParent;
		public Color BoneColor;
		public bool ShowGizmos = true;
		private Transform BoneHaedPositionHelper;
		private bool _targetObjectHelperOn;

		public Vector2 AngleLimits = new Vector2(0f, 360f);

		public Vector2 Limit;

		void OnEnable()
		{
			Start();
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
	}
}

