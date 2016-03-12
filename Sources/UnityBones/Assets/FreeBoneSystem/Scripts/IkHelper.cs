using UnityEngine;

namespace FreeBoneSystem
{
	public enum GizmosType
	{
		Rectangular,
		Circular
	}
	public class IkHelper : MonoBehaviour
	{
		public GizmosType GizmosType;
		public Color GizmosColor = Color.green;
	}
}
