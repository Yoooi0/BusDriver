using System;
using BusDriver.UI;
using UnityEngine;

namespace BusDriver.MotionTarget
{
    public class TargetChangedEventArgs : EventArgs
    {
        public Transform Transform { get; set; }
        public TargetChangedEventArgs(Transform transform)
        {
            Transform = transform;
        }
    }

    public interface IMotionTarget : IUIProvider
    {
        event EventHandler<TargetChangedEventArgs> TargetChanged;

        void Apply(Transform origin, Vector3 offset, Quaternion rotation);
        void OnSceneChanging();
        void OnSceneChanged();
        void Dispose();
    }
}
