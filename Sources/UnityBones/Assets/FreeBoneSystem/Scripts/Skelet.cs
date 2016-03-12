using UnityEngine;

namespace FreeBoneSystem
{
	[ExecuteInEditMode]
	public class Skelet: MonoBehaviour
	{
		void Start()
		{
			
		}
		void LateUpdate()
		{
			if (!Application.isPlaying)
				Start();
		}

		void Update()
		{
			foreach (Bone b in gameObject.GetComponentsInChildren<Bone>())
			{
				BoneIK ik = b.GetComponent<BoneIK>();
				if (ik != null)
					ik.resolveSK2D();
			}
			//EditorUpdate();
		}
/*
#if UNITY_EDITOR
		void OnEnable()
		{
			//EditorApplication.update += EditorUpdate;
		}

		void OnDisable()
		{
			//EditorApplication.update -= EditorUpdate;
		}
#endif*/
	}
}
