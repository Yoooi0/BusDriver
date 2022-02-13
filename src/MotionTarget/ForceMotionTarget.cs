using DebugUtils;
using Leap.Unity;
using Leap.Unity.Infix;
using System.Collections.Generic;
using System.Linq;
using BusDriver.UI;
using BusDriver.Utils;
using UnityEngine;
using System;

namespace BusDriver.MotionTarget
{
    public class ForceMotionTarget : AbstractMotionTarget
    {
        private Rigidbody _target;

        private JSONStorableStringChooser TargetChooser;

        public override event EventHandler<TargetChangedEventArgs> TargetChanged;

        protected override void CreateCustomUI(IUIBuilder builder)
        {
            TargetChooser = builder.CreateScrollablePopup("MotionSource:Force:Target", "Select Target", null, null, TargetChooserCallback, true);
            FindAtoms();
        }

        public override void DestroyUI(IUIBuilder builder)
        {
            base.DestroyUI(builder);
            builder.Destroy(TargetChooser);
        }

        public override void Apply(Transform origin, Vector3 offset, Quaternion rotation)
        {
            if (_target == null || origin == null)
                return;

            var sourceRotation = origin.rotation * rotation;
            var sourcePosition = origin.position + origin.rotation * offset;

            _target.AddForce((sourcePosition - _target.position) * _target.mass / Time.fixedDeltaTime, ForceMode.Impulse);

            ApplyTorque(_target.transform.up, sourceRotation.GetUp());
            ApplyTorque(_target.transform.right, sourceRotation.GetRight());
            ApplyTorque(_target.transform.forward, sourceRotation.GetForward());

            DebugDraw.DrawTransform(_target.transform, 3);
            DebugDraw.DrawTransform(sourcePosition, sourceRotation.GetUp(), sourceRotation.GetRight(), sourceRotation.GetForward(), 3);
        }

        private string lastSceneAtomUid;
        private string lastSceneTargetName;
        public override void OnSceneChanging()
        {
            lastSceneAtomUid = Atom?.uid;
            lastSceneTargetName = _target?.name;

            AtomChooserCallback(null);
        }

        public override void OnSceneChanged()
        {
            base.AtomChooserCallback(lastSceneAtomUid);
            FindTargets(lastSceneTargetName);
        }

        private void ApplyTorque(Vector3 from, Vector3 to)
        {
            var axis = Vector3.Cross(from.normalized, to.normalized);
            var angle = Mathf.Asin(axis.magnitude);

            var angularVelocityDelta = axis.normalized * angle / Time.fixedDeltaTime;
            var intertiaRotation = _target.transform.rotation * _target.inertiaTensorRotation;
            var torque = intertiaRotation * Vector3.Scale(_target.inertiaTensor, Quaternion.Inverse(intertiaRotation) * angularVelocityDelta);
            _target.AddTorque(torque, ForceMode.Impulse);
        }

        protected override void AtomChooserCallback(string s)
        {
            base.AtomChooserCallback(s);
            FindTargets();
        }

        private void FindTargets(string defaultTarget = "None")
        {
            if (Atom == null)
            {
                TargetChooser.choices = new List<string>() { "None" };
                TargetChooserCallback(null);
                return;
            }

            var targets = Atom.forceReceivers.Select(c => c.name).ToList();
            targets.Insert(0, "None");

            TargetChooser.choices = targets;
            TargetChooserCallback(defaultTarget);
        }

        protected void TargetChooserCallback(string s)
        {
            _target = Atom?.forceReceivers?.FirstOrDefault(c => string.Equals(s, c.name, StringComparison.OrdinalIgnoreCase))?.GetComponent<Rigidbody>();
            ResetOriginCallback();
            TargetChooser.valNoCallback = _target == null ? "None" : s;
        }

        protected override void ResetOriginCallback()
        {
            if (TargetChanged != null)
                TargetChanged(this, new TargetChangedEventArgs(_target?.transform));
        }
    }
}
