using System.Collections.Generic;

namespace BusDriver.ValuesSource.Parser
{
    public interface IValuesParser
    {
        bool Parse(string data, IDictionary<int, float> values);
    }
}
