using UnityEditor;
using UnityEngine;

namespace FreeBoneSystem.Editor
{
	[CustomEditor(typeof(IkHelper))]
	public class IkHelperEditor:UnityEditor.Editor
	{
		[DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
		public static void DrawIKHelperGizmo(IkHelper scr, GizmoType gizmoType)
		{
			float handleSize = HandleUtility.GetHandleSize(scr.transform.position) / 4f;
			var color = Handles.color;
			Handles.color = scr.GizmosColor;
			var rect = new Rect(scr.transform.position.x - handleSize / 2f, scr.transform.position.y - handleSize / 2f, handleSize,
				handleSize);
			if (scr.GizmosType == GizmosType.Rectangular)
			{
				Handles.DrawSolidRectangleWithOutline(
					rect,
					new Color(0f, 0f, 0f, 0f),
					scr.GizmosColor);

				Handles.DrawLine(new Vector3(scr.transform.position.x - handleSize / 2f, scr.transform.position.y),
					new Vector3(scr.transform.position.x + handleSize / 2f, scr.transform.position.y));
				Handles.DrawLine(new Vector3(scr.transform.position.x, scr.transform.position.y - handleSize / 2f),
					new Vector3(scr.transform.position.x, scr.transform.position.y + handleSize / 2f));
			}
			else
			{
				handleSize = HandleUtility.GetHandleSize(scr.transform.position) / 6f;
				Handles.DrawWireDisc(scr.transform.position, Vector3.back, handleSize);

				Handles.DrawLine(new Vector3(scr.transform.position.x - handleSize, scr.transform.position.y),
					new Vector3(scr.transform.position.x + handleSize, scr.transform.position.y));
				Handles.DrawLine(new Vector3(scr.transform.position.x, scr.transform.position.y - handleSize),
					new Vector3(scr.transform.position.x, scr.transform.position.y + handleSize));
			}

			Handles.color = color;
		}
	}
}
