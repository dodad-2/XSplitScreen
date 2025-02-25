using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UIMoveTest : MonoBehaviour
{
	public bool followMouse;

	public RectTransform currentTransform;
	public RectTransform target;
	public AnimationCurve acceleration;
	public Vector3 destination;
	public Vector3 origin;
	public Vector3 position;
	public Vector3 direction;
	public Vector3 distance;
	public float speed;
	public float duration;
	public float timer;
	public float totalMagnitude;
	public float currentMagnitude;
	public float curvePercent;
	public bool enableMovement;
	public bool startMove;
	public bool isMoving;

	public void Awake()
	{
		currentTransform = GetComponent<RectTransform>();
	}

	public void Update()
	{
		if (!enableMovement)
			return;

		if (followMouse)
		{
			currentTransform.position = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);
		}
		else
		{
			position = transform.position;

			if (startMove)
			{
				isMoving = true;

				origin = position;

				startMove = false;

				isMoving = true;

				timer = 0;
			}

			if (isMoving)
			{
				timer += Time.deltaTime;

				curvePercent = timer / duration;

				transform.position = Vector3.Lerp(origin, destination, acceleration.Evaluate(curvePercent));

				if (curvePercent >= 1)
				{
					transform.position = destination;
					isMoving = false;

					destination = origin;
				}
			}
			else
			{
				distance = target.position - position;

				totalMagnitude = distance.sqrMagnitude;

				if (totalMagnitude <= 0.05f)
					return;

				transform.position = Vector3.Lerp(position, target.position, Time.deltaTime * speed);
			}
		}
	}
}
