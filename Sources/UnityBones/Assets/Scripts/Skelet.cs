using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Assets.Bones.Scripts
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
			//EditorUpdate();
		}

#if UNITY_EDITOR
		void OnEnable()
		{
			//EditorApplication.update += EditorUpdate;
		}

		void OnDisable()
		{
			//EditorApplication.update -= EditorUpdate;
		}
#endif
	}
}
