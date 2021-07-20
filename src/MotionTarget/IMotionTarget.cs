using System;
using BusDriver.UI;
using UnityEngine;

namespace BusDriver.MotionTarget
{
    public interface IMotionTarget : IUIProvider
    {
        void Apply(Vector3 offset, Quaternion rotation);
        void OnSceneChanging();
        void OnSceneChanged();
        void Dispose();
    }
}
