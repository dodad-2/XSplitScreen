using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class UISquare : Graphic
{
	[Min(0f)]
	public float thickness = 2f;
	public Color outlineColor = Color.black;

	/// <summary>
	/// Call this method to trigger a redraw (for example, after changing thickness or outlineColor).
	/// </summary>
	public void Redraw()
	{
		SetVerticesDirty();
	}

	public override void OnRectTransformDimensionsChange()
	{
		base.OnRectTransformDimensionsChange();
		Redraw();
	}

	public override void OnPopulateMesh(VertexHelper vh)
	{
		vh.Clear();

		Rect rect = rectTransform.rect;
		float left = rect.xMin;
		float right = rect.xMax;
		float top = rect.yMax;
		float bottom = rect.yMin;

		float t = Mathf.Clamp(thickness, 0f, Mathf.Min(rect.width, rect.height) / 2f);

		// Outer rectangle (matches the rectTransform's borders)
		Vector2[] outer = new Vector2[4] {
			new Vector2(left, bottom),   // Bottom Left
            new Vector2(left, top),      // Top Left
            new Vector2(right, top),     // Top Right
            new Vector2(right, bottom)   // Bottom Right
        };
		// Inner rectangle (inset by thickness)
		Vector2[] inner = new Vector2[4] {
			new Vector2(left + t, bottom + t),
			new Vector2(left + t, top - t),
			new Vector2(right - t, top - t),
			new Vector2(right - t, bottom + t)
		};

		UIVertex vert = UIVertex.simpleVert;

		// Outer vertices
		vert.color = outlineColor;
		for (int i = 0; i < 4; ++i)
		{
			vert.position = outer[i];
			vh.AddVert(vert);
		}

		// Inner vertices
		vert.color = color;
		for (int i = 0; i < 4; ++i)
		{
			vert.position = inner[i];
			vh.AddVert(vert);
		}

		// Add triangles for the outline
		for (int i = 0; i < 4; ++i)
		{
			int next = (i + 1) % 4;
			vh.AddTriangle(i, next, 4 + next);
			vh.AddTriangle(i, 4 + next, 4 + i);
		}
	}
}