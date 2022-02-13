using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BusDriver.Utils;
using BusDriver.UI;
using UnityEngine;

namespace BusDriver.ValuesSource
{
    public abstract class AbstractValuesSource : IValuesSource
    {
        private readonly Regex _regex = new Regex(@"([L|R][0|1|2])(\d+)(?>([I|S])(\d+))?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Dictionary<int, Transition> _transitions;
        private readonly Dictionary<int, float> _values;

        private UIHorizontalGroup StartStopButtonGroup;

        protected AbstractValuesSource()
        {
            _values = DeviceAxis.Values.ToDictionary(a => a, a => DeviceAxis.DefaultValue(a));
            _transitions = DeviceAxis.Values.ToDictionary(a => a, a => new Transition()
            {
                StartValue = DeviceAxis.DefaultValue(a),
                EndValue = DeviceAxis.DefaultValue(a),
                StartTime = Time.fixedTime,
                EndTime = Time.fixedTime
            });
        }

        public abstract void Update();
        protected abstract void Start();
        protected abstract void Stop();

        public float GetValue(int axis) => _values[axis];
        private float GetValue(Transition transition, float time)
        {
            if (transition.StartTime == transition.EndTime)
                return transition.EndValue;

            var clampedTime = Mathf.Clamp(time, transition.StartTime, transition.EndTime);
            var t = Mathf.InverseLerp(transition.StartTime, transition.EndTime, clampedTime);
            return Mathf.Lerp(transition.StartValue, transition.EndValue, t);
        }

        protected void UpdateValues(string data)
        {
            foreach (var axis in DeviceAxis.Values)
                _values[axis] = GetValue(_transitions[axis], Time.fixedTime);

            ParseCommands(data);
        }

        private void ParseCommands(string data)
        {
            if (data == null)
                return;

            var matches = _regex.Matches(data);
            if (matches.Count <= 0)
                return;

            foreach (Match match in matches)
            {
                if(match.Groups.Count < 2)
                    continue;

                var axisName = match.Groups[1].Value;
                var axisValue = match.Groups[2].Value;

                var value = int.Parse(axisValue) / ((float)Math.Pow(10, axisValue.Length) - 1);
                var axis = -1;
                if (DeviceAxis.TryParse(axisName, out axis))
                {
                    var transition = _transitions[axis];
                    transition.StartValue = GetValue(axis);
                    transition.StartTime = Time.fixedTime;
                    transition.EndValue = value;

                    if (match.Groups.Count == 5)
                    {
                        var modifierName = match.Groups[3].Value.ToUpper();
                        var modifierValue = int.Parse(match.Groups[4].Value) / 1000.0f;

                        if (modifierName == "I")
                            transition.EndTime = transition.StartTime + modifierValue;
                        else if(modifierName == "S")
                            transition.EndTime = transition.StartTime + (transition.EndValue - transition.StartValue) / modifierValue;
                    }
                    else
                    {
                        transition.EndTime = transition.StartTime;
                    }
                }
            }

            return;
        }

        protected virtual void Dispose(bool disposing)
        {
            Stop();
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void CreateCustomUI(IUIBuilder builder) { }

        public void CreateUI(IUIBuilder builder)
        {
            CreateCustomUI(builder);

            StartStopButtonGroup = builder.CreateHorizontalGroup(510, 50, new Vector2(10, 0), 2, idx => builder.CreateButtonEx());
            var startButton = StartStopButtonGroup.items[0].GetComponent<UIDynamicButton>();
            startButton.buttonText.fontSize = 25;
            startButton.label = "Start";
            startButton.buttonColor = new Color(0.309f, 1f, 0.039f) * 0.8f;
            startButton.textColor = Color.white;
            startButton.button.onClick.AddListener(StartCallback);

            var stopButton = StartStopButtonGroup.items[1].GetComponent<UIDynamicButton>();
            stopButton.buttonText.fontSize = 25;
            stopButton.label = "Stop";
            stopButton.buttonColor = new Color(1f, 0.168f, 0.039f) * 0.8f;
            stopButton.textColor = Color.white;
            stopButton.button.onClick.AddListener(StopCallback);
        }

        public virtual void DestroyUI(IUIBuilder builder)
        {
            builder.Destroy(StartStopButtonGroup);
        }

        protected void StartCallback() => Start();
        protected void StopCallback() => Stop();

        private class Transition
        {
            public float StartValue { get; set; }
            public float EndValue { get; set; }
            public float StartTime { get; set; }
            public float EndTime { get; set; }
        }
    }
}
