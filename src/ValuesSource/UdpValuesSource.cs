using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using BusDriver.Utils;
using BusDriver.UI;
using UnityEngine;

namespace BusDriver.ValuesSource
{
    public class UdpValuesSource : AbstractValuesSource
    {
        private UdpClient _server;
        private int _port = 8889;

        private JSONStorableString PortInputBox;

        protected override void CreateCustomUI(IUIBuilder builder)
        {
            PortInputBox = builder.CreateTextField("ValuesSource:Udp:Port", _port.ToString(), 50, s =>
            {
                var result = 0;
                if (int.TryParse(s, out result))
                    _port = result;
                else
                    SuperController.LogMessage($"Failed to parse port number from: {s}");
            }, canInput: true);
        }

        public override void DestroyUI(IUIBuilder builder)
        {
            builder.Destroy(PortInputBox);
            base.DestroyUI(builder);
        }

        protected override void Start()
        {
            if (_server != null)
                return;

            try
            {
                _server = new UdpClient();
                _server.ExclusiveAddressUse = false;
                _server.Client.Blocking = false;
                _server.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _server.Client.Bind(new IPEndPoint(IPAddress.Loopback, _port));
            }
            catch(Exception e)
            {
                SuperController.LogError(e.ToString());
            }

            SuperController.LogMessage($"Upd started on port: {_port}");
        }

        protected override void Stop()
        {
            if (_server == null)
                return;

            _server.Close();
            _server = null;

            SuperController.LogMessage("Upd stopped");
        }

        private byte[] ReceiveLatest()
        {
            var endpoint = new IPEndPoint(IPAddress.Any, 0);
            var bytes = default(byte[]);
            while (true)
            {
                try
                {
                    var received = _server.Receive(ref endpoint);
                    if (received == null)
                        break;

                    bytes = received;
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode == SocketError.WouldBlock)
                        break;
                }
            }

            return bytes;
        }

        public override void Update()
        {
            if (_server == null)
                return;

            try
            {
                var bytes = ReceiveLatest();
                if (bytes == null)
                    return;

                var line = Encoding.ASCII.GetString(bytes);
                Parser?.Parse(line, Values);
            }
            catch (SocketException e)
            {
                SuperController.LogError(e.ToString());
            }
        }
    }
}
