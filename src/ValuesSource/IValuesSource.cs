using System;
using BusDriver.UI;

namespace BusDriver.ValuesSource
{
    public interface IValuesSource : IUIProvider, IDisposable
    {
        void Update();
        float GetValue(int axis);
    }
}
