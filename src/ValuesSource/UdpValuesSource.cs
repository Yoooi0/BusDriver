using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using BusDriver.UI;

namespace BusDriver.ValuesSource
{
    public class UdpValuesSource : AbstractValuesSource
    {
        private readonly byte[] _readBuffer;
        private UdpClient _server;
        private int _port = 8889;
        private EndPoint _receiveEndpoint;

        private JSONStorableString PortInputBox;

        public UdpValuesSource()
        {
            _readBuffer = new byte[1024];
            _receiveEndpoint = new IPEndPoint(IPAddress.Any, 0);
        }

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

        private string ReceiveLatest()
        {
            var received = -1;
            while (true)
            {
                try
                {
                    received = _server.Client.ReceiveFrom(_readBuffer, ref _receiveEndpoint);
                    if (received <= 0)
                        break;
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode == SocketError.WouldBlock)
                        break;
                }
            }

            if(received <= 0)
                return null;

            return Encoding.ASCII.GetString(_readBuffer, 0, received);
        }

        public override void Update()
        {
            if (_server == null)
                return;

            try
            {
                var data = ReceiveLatest();
                UpdateValues(data);
            }
            catch (SocketException e)
            {
                SuperController.LogError(e.ToString());
            }
        }
    }
}
