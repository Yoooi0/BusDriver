using DebugUtils;
using Leap.Unity;
using Leap.Unity.Infix;
using System.Collections.Generic;
using System.Linq;
using BusDriver.UI;
using BusDriver.Utils;
using UnityEngine;
using UnityThreading;

namespace BusDriver.MotionTarget
{
    public class PhysicsLinkMotionTarget : AbstractMotionTarget
    {
        private FreeControllerV3 _target;
        private Vector3 _originPosition;
        private Quaternion _originRotation;
        private GameObject _source;
        private Rigidbody _sourceRigidBody;

        private JSONStorableStringChooser TargetChooser;
        private UIDynamicButton CaptureOriginButton;

        public PhysicsLinkMotionTarget()
        {
            _source = new GameObject("PhysicsLinkSource", typeof(Atom), typeof(FreeControllerV3), typeof(Rigidbody));
            _sourceRigidBody = _source.GetComponent<Rigidbody>();
            _sourceRigidBody.isKinematic = true;
            _sourceRigidBody.detectCollisions = false;
            _sourceRigidBody.interpolation = RigidbodyInterpolation.Interpolate;
        }

        protected override void CreateCustomUI(IUIBuilder builder)
        {
            TargetChooser = builder.CreateScrollablePopup("MotionSource:PhysicsLink:Target", "Select Target", null, null, TargetChooserCallback, true);
            CaptureOriginButton = builder.CreateButton("Capture Origin", CaptureOriginCallback, true);

            FindAtoms();
        }

        public override void DestroyUI(IUIBuilder builder)
        {
            base.DestroyUI(builder);

            builder.Destroy(TargetChooser);
            builder.Destroy(CaptureOriginButton);
        }

        public override void Apply(Vector3 offset, Quaternion rotation)
        {
            if (_target == null)
                return;

            var sourceRotation = _originRotation * rotation;
            var sourcePosition = _originPosition + _originRotation * offset;

            _sourceRigidBody.MovePosition(sourcePosition);
            _sourceRigidBody.MoveRotation(sourceRotation);

            DebugDraw.DrawTransform(_target.transform, 3);
            DebugDraw.DrawTransform(sourcePosition, sourceRotation.GetUp(), sourceRotation.GetRight(), sourceRotation.GetForward(), 3);
        }

        public override void OnSceneChanged()
        {
            base.AtomChooserCallback(Atom?.name);
            TargetChooserCallback(_target?.name);

            if (_target == null)
                AtomChooserCallback(null);
        }

        protected override void AtomChooserCallback(string s)
        {
            base.AtomChooserCallback(s);
            FindTargets();
        }

        private void CaptureOriginCallback()
        {
            if (_target == null)
                return;

            _source.transform.position = _originPosition = _target.transform.position;
            _source.transform.rotation = _originRotation = _target.transform.rotation;
        }

        private void FindTargets()
        {
            if (Atom == null)
            {
                TargetChooser.choices = new List<string>() { "None" };
                TargetChooserCallback(null);
                return;
            }

            var targets = Atom.freeControllers.Select(c => c.name).ToList();
            var defaultTarget = "None";
            targets.Insert(0, "None");

            TargetChooser.choices = targets;
            TargetChooserCallback(defaultTarget);
        }

        private void ResetCurrentTarget()
        {
            if (_target == null)
                return;

            _target.RestorePreLinkState();
            _target.canGrabPosition = true;
            _target.canGrabRotation = true;

            var motion = _target.GetComponent<MotionAnimationControl>();
            if (motion != null)
            {
                motion.suspendPositionPlayback = false;
                motion.suspendRotationPlayback = false;
            }

            _target = null;
        }

        private void CaptureCurrentTarget()
        {
            if (_target == null)
                return;

            _target.SelectLinkToRigidbody(_sourceRigidBody, FreeControllerV3.SelectLinkState.PositionAndRotation, true, true);
            _target.canGrabPosition = false;
            _target.canGrabRotation = false;

            var motion = _target.GetComponent<MotionAnimationControl>();
            if (motion != null)
            {
                motion.suspendPositionPlayback = true;
                motion.suspendRotationPlayback = true;
            }
        }

        protected void TargetChooserCallback(string s)
        {
            ResetCurrentTarget();

            _target = Atom?.freeControllers?.FirstOrDefault(c => string.Equals(s, c.name, System.StringComparison.OrdinalIgnoreCase));
            CaptureOriginCallback();
            CaptureCurrentTarget();

            TargetChooser.valNoCallback = _target == null ? "None" : s;
        }

        protected override void Dispose(bool disposing)
        {
            ResetCurrentTarget();

            if(_source != null)
                GameObject.Destroy(_source);

            _source = null;
            _sourceRigidBody = null;
        }
    }
}
