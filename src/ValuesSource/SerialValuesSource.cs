using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using BusDriver.UI;
using BusDriver.Utils;
using SimpleJSON;
using UnityEngine;

namespace BusDriver.ValuesSource
{
    public class SerialValuesSource : AbstractValuesSource
    {
        private SerialPort _serial;
        private Thread _readThread;

        private JSONStorableStringChooser ComPortChooser;
        private JSONStorableStringChooser BaudRateChooser;
        private UIDynamicButton RefreshButton;

        private JSONStorableAction StartSerialAction;
        private JSONStorableAction StopSerialAction;
        private JSONStorableAction RefreshAction;

        protected override void CreateCustomUI(IUIBuilder builder)
        {
            ComPortChooser = builder.CreatePopup("ValuesSource:Serial:ComPort", "ComPort", null, null, null);

            var baudrates = new List<string>() { "50", "75", "110", "134", "150", "200", "300", "600", "1200", "1800", "2400", "4800", "9600", "19200", "28800", "38400", "57600", "76800", "115200", "230400", "460800", "576000", "921600" };
            BaudRateChooser = builder.CreatePopup("ValuesSource:Serial:Baudrate", "Baudrate", baudrates, "115200", null, scrollable: true, rightSide: false);

            RefreshButton = builder.CreateButton("Refresh", RefreshComPorts, new Color(0, 0.75f, 1f) * 0.8f, Color.white);

            StartSerialAction = UIManager.CreateAction("Start Serial", Start);
            StopSerialAction = UIManager.CreateAction("Stop Serial", Stop);
            RefreshAction = UIManager.CreateAction("Refresh Serial", RefreshComPorts);

            RefreshComPorts();
        }

        public override void DestroyUI(IUIBuilder builder)
        {
            builder.Destroy(ComPortChooser);
            builder.Destroy(BaudRateChooser);
            builder.Destroy(RefreshButton);
            base.DestroyUI(builder);

            UIManager.RemoveAction(StartSerialAction);
            UIManager.RemoveAction(StopSerialAction);
            UIManager.RemoveAction(RefreshAction);
        }

        public override void RestoreConfig(JSONNode config)
        {
            RefreshComPorts();

            base.RestoreConfig(config);
            config.Restore(ComPortChooser);
            config.Restore(BaudRateChooser);
        }

        public override void StoreConfig(JSONNode config)
        {
            base.StoreConfig(config);
            config.Store(ComPortChooser);
            config.Restore(BaudRateChooser);
        }

        private void RefreshComPorts()
        {
            var comPorts = SerialPort.GetPortNames().ToList();
            var currentValue = ComPortChooser.val;

            ComPortChooser.choices = comPorts;
            if (!comPorts.Contains(currentValue))
                ComPortChooser.val = comPorts.FirstOrDefault();
            else
                ComPortChooser.val = currentValue;
        }

        protected override void Start()
        {
            if (_serial != null)
                return;

            try
            {
                _serial = new SerialPort(ComPortChooser.val, int.Parse(BaudRateChooser.val))
                {
                    DtrEnable = true,
                    RtsEnable = true,
                    ReadTimeout = 1000
                };

                _serial.Open();
                _readThread = new Thread(ReadCommands) { IsBackground = true };
                _readThread.Start();

                SuperController.LogMessage($"Serial started on: {ComPortChooser.val}");
            }
            catch (Exception e)
            {
                SuperController.LogError(e.ToString());
                Stop();
            }
        }

        protected override void Stop()
        {
            if (_serial == null)
                return;

            try { _serial.Close(); }
            catch { }

            _serial = null;
            _readThread = null;
            SuperController.LogMessage("Serial stopped");
        }


        private void ReadCommands()
        {
            try
            {
                while (_serial != null && _serial.IsOpen)
                {
                    try
                    {
                        ParseCommands(_serial.ReadLine());
                    }
                    catch (TimeoutException) { }
                }
            }
            catch (Exception)
            {
                Stop();
            }
        }

        public override void Update()
        {
            if (_serial == null)
                return;

            UpdateValues();
        }
    }
}