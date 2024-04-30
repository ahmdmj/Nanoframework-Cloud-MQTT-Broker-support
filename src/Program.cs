using nanoFramework.M2Mqtt.Messages;
using nanoFramework.M2Mqtt;
using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Net.NetworkInformation;
using System.Text;

namespace NFMqttClientApp
{
	public class Program
	{
		private const string c_SSID = "fh_f0f258";
		private const string c_AP_PASSWORD = "17102002";

		private static MqttClient device = null;

		private const string c_DEVICEID = "client3-authn-ID";
		private const string c_USERNAME = c_DEVICEID;

		private const string c_BROKERHOSTNAME = "mqtt-trjt-test.southeastasia-1.ts.eventgrid.azure.net";
		private const int c_PORT = 8883;
		private const string c_PUB_TOPIC = "telemetry/fromnanoframework";
		private const string c_SUB_TOPIC = "telemetry/#";

		public static void Main()
		{
			//// ROOT TLS cert is taken from DigiCert Global Root G3: https://www.digicert.com/kb/digicert-root-certificates.htm

			Debug.WriteLine("Hello from nanoFramework MQTT cLient!");

			// Wait for Wifi/network to connect (temp)
			SetupAndConnectNetwork();

			var caCert = new X509Certificate(ca_cert);
			var clientCert = new X509Certificate2(client_cert, client_key, string.Empty);
			device = new MqttClient(c_BROKERHOSTNAME, c_PORT, true, caCert, clientCert, MqttSslProtocols.TLSv1_2);

			device.ProtocolVersion = MqttProtocolVersion.Version_5;
			
			TryToConnect();

			device.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;
			device.MqttMsgSubscribed += Client_MqttMsgSubscribed;
			device.ConnectionOpened += Client_ConnectionOpened;
			device.ConnectionClosed += Client_ConnectionClosed;
			device.ConnectionClosedRequest += Client_ConnectionClosedRequest;
			device.MqttMsgUnsubscribed += Client_MqttMsgUnsubscribed;

			device.Subscribe(new[] { c_SUB_TOPIC }, new[] { MqttQoSLevel.AtLeastOnce });

			var counter = 0;

			while (true)
			{
				string payload = $"{{\"counter\":{counter++}}}";
				device.Publish(c_PUB_TOPIC, Encoding.UTF8.GetBytes(payload), "application/json; charset=utf-8", null);
				Debug.WriteLine($"Message sent: {payload}");
				Thread.Sleep(10000);
			}
		}

		private static void TryToConnect()
		{
			bool connected = false;

			while (!connected)
			{
				try
				{
					var resultConnect = device.Connect(c_DEVICEID, c_USERNAME, "");

					if (resultConnect != MqttReasonCode.Success)
					{
						Debug.WriteLine($"MQTT ERROR connecting: {resultConnect}");
//						device.Disconnect();

						Thread.Sleep(1000);
					}
					else
					{
						Debug.WriteLine(">>> Device is connected");
						connected = true;
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"MQTT ERROR Exception '{ex.Message}'");
//					device.Disconnect();

					Thread.Sleep(1000);
				}
			}
		}

		private static void Client_ConnectionClosedRequest(object sender, ConnectionClosedRequestEventArgs e)
		{
			Debug.WriteLine("Client_ConnectionClosedRequest");
		}

		private static void Client_ConnectionClosed(object sender, EventArgs e)
		{
			Debug.WriteLine("Client_ConnectionClosed");

			TryToConnect();
		}

		private static void Client_ConnectionOpened(object sender, ConnectionOpenedEventArgs e)
		{
			Debug.WriteLine("Client_ConnectionOpened");
		}

		private static void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
		{
			Debug.WriteLine("Client_MqttMsgPublishReceived");

			Debug.WriteLine($"Topic: {e.Topic}");

			var message = Encoding.UTF8.GetString(e.Message, 0, e.Message.Length);

			Debug.WriteLine(message);
		}

		private static void Client_MqttMsgSubscribed(object sender, MqttMsgSubscribedEventArgs e)
		{
			Debug.WriteLine("Client_MqttMsgSubscribed");

			Debug.WriteLine($"Message identifier: {e.MessageId} of subscribed topic");
		}

		private static void Client_MqttMsgUnsubscribed(object sender, MqttMsgUnsubscribedEventArgs e)
		{
			Debug.WriteLine("Client_MqttMsgUnsubscribed");
		}

		public static void SetupAndConnectNetwork()
		{
			NetworkInterface[] nis = NetworkInterface.GetAllNetworkInterfaces();
			if (nis.Length > 0)
			{
				// get the first interface
				NetworkInterface ni = nis[0];

				if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
				{
					// network interface is Wi-Fi
					Debug.WriteLine("Network connection is: Wi-Fi");

					Wireless80211Configuration wc = Wireless80211Configuration.GetAllWireless80211Configurations()[ni.SpecificConfigId];
					if (wc.Ssid != c_SSID && wc.Password != c_AP_PASSWORD)
					{
						// have to update Wi-Fi configuration
						wc.Ssid = c_SSID;
						wc.Password = c_AP_PASSWORD;
						wc.SaveConfiguration();
					}
					else
					{   // Wi-Fi configuration matches
					}
				}
				else
				{
					// network interface is Ethernet
					Debug.WriteLine("Network connection is: Ethernet");

					ni.EnableDhcp();
				}

				// wait for DHCP to complete
				WaitIP();
			}
			else
			{
				throw new NotSupportedException("ERROR: there is no network interface configured.\r\nOpen the 'Edit Network Configuration' in Device Explorer and configure one.");
			}
		}

		static void WaitIP()
		{
			Debug.WriteLine("Waiting for IP...");

			while (true)
			{
				NetworkInterface ni = NetworkInterface.GetAllNetworkInterfaces()[0];
				if (ni.IPv4Address != null && ni.IPv4Address.Length > 0)
				{
					if (ni.IPv4Address[0] != '0')
					{
						Debug.WriteLine($"We have an IP: {ni.IPv4Address}");
						break;
					}
				}

				Thread.Sleep(500);
			}
		}

		#region Certificates (include BEGIN and END)
		private const string client_cert = @"-----BEGIN CERTIFICATE-----
MIIB+DCCAZ6gAwIBAgIRAIoUyyjuyhXWlw1LPXUboZswCgYIKoZIzj0EAwIwTjEd
MBsGA1UEChMUTXF0dEJsb2dBcHBTYW1wbGVzQ0ExLTArBgNVBAMTJE1xdHRCbG9n
QXBwU2FtcGxlc0NBIEludGVybWVkaWF0ZSBDQTAeFw0yNDA0MjgxNDU3MDZaFw0y
NDA4MDYxNDU3MDFaMBsxGTAXBgNVBAMTEGNsaWVudDMtYXV0aG4tSUQwWTATBgcq
hkjOPQIBBggqhkjOPQMBBwNCAAQ3vMvNhmwtNluxEzWGeffr62wP4ND0OEDfl+vk
qVbfVPXyBwet+nvzuWzX2q8JuyHOvMqxLeGTd4+4eGPskNHIo4GPMIGMMA4GA1Ud
DwEB/wQEAwIHgDAdBgNVHSUEFjAUBggrBgEFBQcDAQYIKwYBBQUHAwIwHQYDVR0O
BBYEFFAlH9v43DVAQHMQWT488lqWfsnEMB8GA1UdIwQYMBaAFLK49M9EsK2kgNpc
cO0hD1AW1FssMBsGA1UdEQQUMBKCEGNsaWVudDMtYXV0aG4tSUQwCgYIKoZIzj0E
AwIDSAAwRQIgeTLItVRBnKSHvRMmAfB1APAwTvh+uxUKsB5Uh/m/6ncCIQCLykh0
UBbXb51MdRx+YBMOddU8qm7TNtkYIL/hO+43EA==
-----END CERTIFICATE-----";

		private const string client_key = @"-----BEGIN EC PRIVATE KEY-----
MHcCAQEEIL04eUDaPD3yJS55RLdkjB6ONYdu4Zhj9uuQGW9pwHiUoAoGCCqGSM49
AwEHoUQDQgAEN7zLzYZsLTZbsRM1hnn36+tsD+DQ9DhA35fr5KlW31T18gcHrfp7
87ls19qvCbshzrzKsS3hk3ePuHhj7JDRyA==
-----END EC PRIVATE KEY-----";

		private const string ca_cert = @"-----BEGIN CERTIFICATE-----
MIIB+DCCAZ6gAwIBAgIQBbFAmj9ESJcCEZD/vwmyMDAKBggqhkjOPQQDAjBGMR0w
GwYDVQQKExRNcXR0QmxvZ0FwcFNhbXBsZXNDQTElMCMGA1UEAxMcTXF0dEJsb2dB
cHBTYW1wbGVzQ0EgUm9vdCBDQTAeFw0yNDA0MjgxNDM3NTNaFw0zNDA0MjYxNDM3
NTNaME4xHTAbBgNVBAoTFE1xdHRCbG9nQXBwU2FtcGxlc0NBMS0wKwYDVQQDEyRN
cXR0QmxvZ0FwcFNhbXBsZXNDQSBJbnRlcm1lZGlhdGUgQ0EwWTATBgcqhkjOPQIB
BggqhkjOPQMBBwNCAASqqcaQem7tUNDSMcbF9eOD04+eAaHgg0Ki8y4WNVMqXEjs
SH1GMwyfSEPVrdOHzhblZnt3q3h9YU9GWS2ZkyYpo2YwZDAOBgNVHQ8BAf8EBAMC
AQYwEgYDVR0TAQH/BAgwBgEB/wIBADAdBgNVHQ4EFgQU06muC9A5QOeai8oTUWXZ
C+QTszAwHwYDVR0jBBgwFoAUSsaUV/JgF1oaTVQl7nqh8vnnzi4wCgYIKoZIzj0E
AwIDSAAwRQIgSuZZ/sVBiEBW4aHNH8z2SsCi0j8wO3pmrtliWLfJVcYCIQC41HI1
73o/00I++ICRGg4/jYnV9mOoXwZbGvj1qQ/ezw==
-----END CERTIFICATE-----";
		#endregion

	}
}
