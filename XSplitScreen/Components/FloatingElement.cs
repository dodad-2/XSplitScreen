using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Dodad.XSplitscreen.Components
{
	public abstract class FloatingElement : MonoBehaviour
	{
		public RectTransform target 
		{ 
			get
			{
				if (_target == null)
					return currentTransform;

				return _target;
			}
			private set
			{
				_target = value;
			}
		}
		private RectTransform _target;

		/// <summary>
		/// Curve used in MoveTo
		/// </summary>
		public AnimationCurve acceleration 
		{ 
			get
			{
				return _acceleration;
			}
			private set
			{
				if (value == null)
				{
					_acceleration = new AnimationCurve(new Keyframe[2]
					{
						new Keyframe(0,0),
						new Keyframe(1, 1),
					});
				}
				else
					_acceleration = value;
			}
		}
		public AnimationCurve _acceleration;

		public float duration;

		//-----------------------------------------------------------------------------------------------------------

		public Vector3 destination {  get; private set; }

		//-----------------------------------------------------------------------------------------------------------

		private RectTransform currentTransform;

		private Vector3 origin;
		private Vector3 position;
		private Vector3 distance;

		private bool startMove;

		private float timer;

		//-----------------------------------------------------------------------------------------------------------

		public bool isMoving
		{
			get
			{
				return _isMoving;
			}
			private set
			{
				_isMoving = value;
			}
		}
		private bool _isMoving;

		public bool enableMovement
		{
			get
			{
				return _enableMovement;
			}
			set
			{
				_enableMovement = value;
			}
		}
		private bool _enableMovement;

		public float speed
		{
			get
			{
				return _speed;
			}
			set
			{
				_speed = value;
			}
		}
		private float _speed;

		//-----------------------------------------------------------------------------------------------------------

		public virtual void Awake()
		{
			currentTransform = GetComponent<RectTransform>();

			// Initialize acceleration
			acceleration = null;
		}

		//-----------------------------------------------------------------------------------------------------------

		public virtual void Update()
		{
			if (!enableMovement)
				return;

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

				var curvePercent = timer / duration;

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

				if (distance.sqrMagnitude <= 0.05f)
					return;

				transform.position = Vector3.Lerp(position, target.position, Time.deltaTime * speed);
			}
		}

		//-----------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Stop tracking the target and perform a move to the position
		/// </summary>
		/// <param name="destination"></param>
		public virtual void MoveTo(Vector3 destination)
		{
			startMove = true;

			this.destination = destination;

			origin = transform.position;
		}
	}
}
