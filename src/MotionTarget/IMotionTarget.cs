using System;
using BusDriver.Config;
using BusDriver.UI;
using UnityEngine;

namespace BusDriver.MotionTarget
{
    public class TransformEventArgs : EventArgs
    {
        public Transform Transform { get; set; }
        public TransformEventArgs(Transform transform)
        {
            Transform = transform;
        }
    }

    public interface IMotionTarget : IUIProvider, IConfigProvider, IDisposable
    {
        event EventHandler<TransformEventArgs> OriginReset;

        void Apply(Transform origin, Vector3 offset, Quaternion rotation);
        void ResetOrigin();
        void OnSceneChanging();
        void OnSceneChanged();
    }
}
