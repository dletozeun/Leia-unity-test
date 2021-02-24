/****************************************************************
*
* Copyright 2019 © Leia Inc.  All rights reserved.
*
* NOTICE:  All information contained herein is, and remains
* the property of Leia Inc. and its suppliers, if any.  The
* intellectual and technical concepts contained herein are
* proprietary to Leia Inc. and its suppliers and may be covered
* by U.S. and Foreign Patents, patents in process, and are
* protected by trade secret or copyright law.  Dissemination of
* this information or reproduction of this materials strictly
* forbidden unless prior written permission is obtained from
* Leia Inc.
*
****************************************************************
*/
using UnityEngine;

/// <summary>
/// Script that automates the LeiaCamera's focus distance
/// </summary>
namespace LeiaLoft
{
	[ExecuteInEditMode]
	public class LeiaAutoFocus : MonoBehaviour
	{
		public enum TrackingModes
		{
			Target,
			Raycast
		}
		[SerializeField] TrackingModes _trackingMode;

		public enum RaycastTypes 
		{
			Point,
			Sphere
		};

		/// <summary>
		/// The target GameObject to focus on.
		/// </summary>
		public GameObject Target
		{
			get
			{
				return _target;
			}
			set
			{
				_target = value;
			}
		}
	 
		/// <summary>
		/// Offset from the automatic focus tracking.
		/// </summary>
		public float FocusOffset
		{
			get
			{
				return _focusOffset;
			}
			set
			{
				_focusOffset = value;
			}
		}

		/// <summary>
		/// The radius of the sphere used in raycasting.
		/// </summary>
		public float SphereRadius
		{
			get
			{
				return _sphereRadius;
			}
			set
			{
				_sphereRadius = Mathf.Max(0.01f, value);
			}
		}

		/// <summary>
		/// The tracking mode used for determining focus.
		/// </summary>
		public TrackingModes TrackingMode
		{
			get
			{
				return _trackingMode;
			}
			set
			{
				if (_target != null && _trackingMode == TrackingModes.Target && value != TrackingModes.Target)
				{
					_focusOffset = _targetConvergenceDistance - GetPerpandicularDistance();
				}
				_trackingMode = value;
			}
		}

		/// <summary>
		/// The auto focus racast mode for determining focus.
		/// </summary>
		public RaycastTypes RaycastType { get; set; }

		/// <summary>
		/// The max distance the ray should check for collisions.
		/// </summary>
		public float MaxFocusDistance
		{
			get
			{
				return _maxFocusDistance;
			}
			set
			{
				_maxFocusDistance = Mathf.Max(value, 0f);
			}
		}

		/// <summary>
		/// The speed at which focus will change.
		/// </summary>
		public float FocusSpeed
		{
			get
			{
				return _focusSpeed;
			}
			set
			{
				_focusSpeed = Mathf.Clamp(value, 0.05f, 1.0f);
			}
		}

		/// <summary>
		/// Uses the Transform.LookAt function to look at the target GameObject.
		/// </summary>
		public bool LookAtTarget
		{
			get
			{
				return _lookAtTarget;
			}
			set
			{
				_lookAtTarget = value;
			}
		}

		/// <summary>
		/// The layer the auto raycast will hit
		/// </summary>
		public int TargetCollisionLayer
		{
			get
			{
				return _targetCollisionLayer;
			}
			set
			{
				_targetCollisionLayer = value;
				_targetCollisionLayerMask = 1 << _targetCollisionLayer;
			}
		}

		[SerializeField]
		private GameObject _target;

		[SerializeField]
		private bool _lookAtTarget = false;

		[SerializeField]
		private float _focusOffset = 0f;

		[SerializeField]
		private float _sphereRadius = 0.3f;

		[SerializeField]
		private float _maxFocusDistance = 1000f;

		[SerializeField]
		private float _focusSpeed = 1f;

		[SerializeField]
		private float _targetConvergenceDistance = 0f;

		[SerializeField]
		private int _targetCollisionLayer;
		private int _targetCollisionLayerMask = 1;

		private LeiaCamera _LeiaCam;

		public void Start()
		{

			_LeiaCam = GetComponent<LeiaCamera>();

			if(_LeiaCam == null)
			{
				return;
			}
			_targetCollisionLayerMask = 1 << _targetCollisionLayer;
			_targetConvergenceDistance = _LeiaCam.ConvergenceDistance;
		}

		public void Update()
		{
			if(_LeiaCam == null)
			{
				return;
			}

			if(TrackingMode == TrackingModes.Raycast)
			{
				UpdateRaycastFocus();
			}
			else if(_target != null) 
			{
				UpdateTargetTracking();
			}

			UpdateConvergenceDistance();
		}

		private void UpdateConvergenceDistance()
		{
			_LeiaCam.ConvergenceDistance += (_targetConvergenceDistance - _LeiaCam.ConvergenceDistance) * _focusSpeed;
		}

		private void UpdateTargetTracking()
		{
			_targetConvergenceDistance = GetPerpandicularDistance() + FocusOffset;

			if (_lookAtTarget)
			{
				this.transform.LookAt(_target.transform);
			}
		}
		private void UpdateRaycastFocus()
		{
			#if UNITY_EDITOR
			if (!Application.isPlaying) return;
			#endif

			RaycastHit hit;
			Ray ray = new Ray(this.transform.position, this.transform.forward);

			if(RaycastType == RaycastTypes.Sphere)
			{
				if(Physics.SphereCast(ray.origin, SphereRadius, ray.direction, out hit, _maxFocusDistance, _targetCollisionLayerMask))
				{
					SetFocusToPoint(hit.point);
				}
				else
				{
					SetFocusToPoint(ray.origin + (ray.direction * _maxFocusDistance));
				}
			}
			else
			{
				if(Physics.Raycast(ray.origin, ray.direction, out hit, _maxFocusDistance, _targetCollisionLayerMask))
				{
					SetFocusToPoint(hit.point);
				}
				else
				{
					SetFocusToPoint(ray.origin + (ray.direction * _maxFocusDistance));
				}
			}
		}

		private void SetFocusToPoint(Vector3 target)
		{
			_targetConvergenceDistance = Vector3.Distance(this.transform.position, target) + _focusOffset;
		}

		private float GetPerpandicularDistance()
		{
			return Vector3.Dot(_target.transform.position - this.transform.position, this.transform.forward) / this.transform.forward.magnitude;
		}
	}
}
