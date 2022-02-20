using DebugUtils;
using Leap.Unity;
using Leap.Unity.Infix;
using System.Collections.Generic;
using System.Linq;
using BusDriver.UI;
using BusDriver.Utils;
using UnityEngine;
using UnityThreading;
using System;
using System.Collections;
using SimpleJSON;

namespace BusDriver.MotionTarget
{
    public class PhysicsLinkMotionTarget : AbstractMotionTarget
    {
        private FreeControllerV3 _target;
        private GameObject _source;
        private Rigidbody _sourceRigidBody;

        private JSONStorableStringChooser TargetChooser;

        public override event EventHandler<TransformEventArgs> OriginReset;

        public PhysicsLinkMotionTarget()
        {
            _source = new GameObject("_BusDriverPhysicsLinkSource", typeof(Atom), typeof(FreeControllerV3), typeof(Rigidbody));
            _sourceRigidBody = _source.GetComponent<Rigidbody>();
            _sourceRigidBody.isKinematic = true;
            _sourceRigidBody.detectCollisions = false;
            _sourceRigidBody.interpolation = RigidbodyInterpolation.Interpolate;
        }

        protected override void CreateCustomUI(IUIBuilder builder)
        {
            TargetChooser = builder.CreateScrollablePopup("MotionTarget:PhysicsLink:Target", "Select Target", null, null, TargetChooserCallback, true);
            FindAtoms();
        }

        public override void DestroyUI(IUIBuilder builder)
        {
            builder.Destroy(TargetChooser);
            base.DestroyUI(builder);
        }

        public override void RestoreConfig(JSONNode config)
        {
            base.RestoreConfig(config);
            config.Restore(TargetChooser);
            FindTargets(TargetChooser.val);
        }

        public override void StoreConfig(JSONNode config)
        {
            base.StoreConfig(config);
            config.Store(TargetChooser);
        }

        public override void Apply(Transform origin, Vector3 offset, Quaternion rotation)
        {
            if (_target == null || origin == null)
                return;

            var sourceRotation = origin.rotation * rotation;
            var sourcePosition = origin.position + origin.rotation * offset;

            _sourceRigidBody.MovePosition(sourcePosition);
            _sourceRigidBody.MoveRotation(sourceRotation);
            
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

        protected override void AtomChooserCallback(string s)
        {
            base.AtomChooserCallback(s);
            FindTargets();
        }

        protected override void RefreshButtonCallback()
        {
            base.RefreshButtonCallback();
            FindTargets(TargetChooser.val);
        }

        private void FindTargets(string defaultTarget = "None")
        {
            if (Atom == null)
            {
                TargetChooser.choices = new List<string>() { "None" };
                TargetChooserCallback(null);
                return;
            }

            var targets = Atom.freeControllers.Select(c => c.name).ToList();
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

        private void MoveOriginAndSourceToTarget()
        {
            ResetOrigin();

            if (_target != null)
            {
                _source.transform.position = _target.transform.position;
                _source.transform.rotation = _target.transform.rotation;
            }
        }

        protected void TargetChooserCallback(string s)
        {
            ResetCurrentTarget();
            _target = Atom?.freeControllers?.FirstOrDefault(c => string.Equals(s, c.name, StringComparison.OrdinalIgnoreCase));
            MoveOriginAndSourceToTarget();
            CaptureCurrentTarget();

            TargetChooser.valNoCallback = _target == null ? "None" : s;
        }

        protected override void PosePostLoadCallback()
        {
            ResetCurrentTarget();
            SuperController.singleton.StartCoroutine(PosePostLoadCoroutine());
        }

        private IEnumerator PosePostLoadCoroutine()
        {
            for(var i = 0; i < 10; i++)
                yield return new WaitForEndOfFrame();

            MoveOriginAndSourceToTarget();
            CaptureCurrentTarget();
        }

        public override void ResetOrigin()
        {
            if (OriginReset != null)
                OriginReset(this, new TransformEventArgs(_target?.transform));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            ResetCurrentTarget();
            _target = null;

            if(_source != null)
                GameObject.Destroy(_source);

            _source = null;
            _sourceRigidBody = null;
        }
    }
}
