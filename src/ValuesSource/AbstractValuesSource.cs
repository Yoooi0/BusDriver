using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using BusDriver.Utils;
using BusDriver.ValuesSource.Parser;
using BusDriver.UI;
using UnityEngine;

namespace BusDriver.ValuesSource
{
    public abstract class AbstractValuesSource : IValuesSource
    {
        protected Dictionary<int, float> Values { get; }
        protected IValuesParser Parser { get; private set; }

        private JSONStorableStringChooser DataParserChooser;
        private UIHorizontalGroup StartStopButtonGroup;

        protected AbstractValuesSource()
        {
            Values = DeviceAxis.Values.ToDictionary(a => a, a => DeviceAxis.DefaultValue(a));
        }

        public abstract void Update();
        protected abstract void Start();
        protected abstract void Stop();

        public float GetValue(int axis) => Values[axis];

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
            DataParserChooser = builder.CreatePopup("Plugin:ValuesSource:DataParser", "Data type", new List<string> { "TCode" }, "TCode", DataParserChooserCallback);
            DataParserChooserCallback(DataParserChooser.val);

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
            builder.Destroy(DataParserChooser);
            builder.Destroy(StartStopButtonGroup);
        }

        private void DataParserChooserCallback(string s)
        {
            if (s == "TCode")
                Parser = new TCodeParser();
            else
                Parser = null;
        }

        protected void StartCallback() => Start();
        protected void StopCallback() => Stop();
    }
}
