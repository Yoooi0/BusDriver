using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BusDriver.Utils;

namespace BusDriver.ValuesSource.Parser
{
    //TODO: handle I/S modifier
    public class TCodeParser : IValuesParser
    {
        private readonly Regex _regex = new Regex(@"([L|R][0|1|2])(\d+)([I|S]\d+)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public bool Parse(string data, IDictionary<int, float> values)
        {
            var matches = _regex.Matches(data).OfType<Match>();
            if (!matches.Any())
                return false;

            foreach (var match in matches)
            {
                var axisName = match.Groups[1].Value;
                var axisValue = match.Groups[2].Value;

                var value = int.Parse(axisValue) / ((float)Math.Pow(10, axisValue.Length) - 1);
                var axis = -1;
                if (DeviceAxis.TryParse(axisName, out axis))
                    values[axis] = value;
            }

            return true;
        }
    }
}
