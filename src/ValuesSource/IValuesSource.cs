using System;
using BusDriver.Config;
using BusDriver.UI;

namespace BusDriver.ValuesSource
{
    public interface IValuesSource : IUIProvider, IConfigProvider, IDisposable
    {
        void Update();
        float GetValue(int axis);
    }
}
