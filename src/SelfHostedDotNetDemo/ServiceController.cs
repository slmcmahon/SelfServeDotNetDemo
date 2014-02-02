using System;
using System.Diagnostics;
using System.Management;
using System.Security.Principal;
using System.ServiceModel;
using System.ServiceModel.Description;
using NetFwTypeLib;

namespace SelfHostedJSONExample
{
    public class ServiceController
    {
        private INetFwProfile _profile;
        private INetFwOpenPort _portClass;
        private INetFwMgr _firewallManager;

        private readonly int _port;
        private readonly string _password;
        private readonly string _ssid;

        public event EventHandler<StatusEventArgs> StatusEvent;

        public ServiceController(int port, string ssid, string password)
        {
            _port = port;
            _password = password;
            _ssid = ssid;
        }

        public static string UserName
        {
            get
            {
                return WindowsIdentity.GetCurrent().Name.Split('\\')[1];
            }
        }

        public bool IsRunning { get; set; }

        public void Start()
        {
            EnableMiniPort();
            AddFirewallPortException();

            var cmd = String.Format("http add urlacl url=http://+:{0} user={1}\\{2}", _port, Environment.MachineName, UserName);
            ExecuteNetShCommand(cmd);
            OnStatusEvent(0, "Added http url permission.");

            cmd = String.Format("wlan set hostednetwork mode=allow ssid={0} key={1}", _ssid, _password);
            ExecuteNetShCommand(cmd);
            OnStatusEvent(0, "Configured hosted network.");

            ExecuteNetShCommand("wlan start hostednetwork");
            OnStatusEvent(0, "Started network.");

            try
            {
                var host = new ServiceHost(typeof(Service), new Uri(String.Format("http://0.0.0.0:{0}", _port)));
                host.AddServiceEndpoint(typeof(IService), new WebHttpBinding(), "").Behaviors.Add(new WebHttpBehavior());
                host.Description.Behaviors.Add(new ServiceMetadataBehavior { HttpGetEnabled = true });
                host.Open();
                OnStatusEvent(0, String.Format("Now serving on port {0}", _port));
                IsRunning = true;
            }
            catch (Exception ex)
            {
                OnStatusEvent(2, String.Format("Failed to start serving. Exception: {0}", ex.Message));
                IsRunning = false;
            }
        }

        public void Stop()
        {
            if (IsRunning)
            {
                ExecuteNetShCommand("wlan stop hostednetwork");
                OnStatusEvent(0, "Service is now disabled.");
                _profile.GloballyOpenPorts.Remove(_port, NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP);
                OnStatusEvent(0, String.Format("Removed firewall rule for port {0}", _port));
                IsRunning = false;
            }
        }

        private void EnableMiniPort()
        {
            var qry = new ObjectQuery("select deviceid, productname, netconnectionstatus from win32_networkadapter");
            using (var searcher = new ManagementObjectSearcher(qry))
            {
                using (var results = searcher.Get())
                {
                    foreach (var result in results)
                    {
                        var name = result["ProductName"];
                        if (name == null || String.Compare(name.ToString(), "Microsoft Virtual WiFi Miniport Adapter", true) != 0)
                        {
                            continue;
                        }

                        string stat = result["NetConnectionStatus"].ToString();
                        OnStatusEvent(0, String.Format("Found miniport adapter.  Status {0}", stat.Equals("2") ? "On" : "Off"));
                        // NetConnectionStatus: 0 = disabled / 2 = enabled
                        if (!stat.Equals("2"))
                        {
                            OnStatusEvent(0, "Enabling miniport.");
                            ((ManagementObject)result).InvokeMethod("Enable", null);
                        }
                        break;
                    }
                }
            }
        }

        private void ExecuteNetShCommand(string command)
        {
            var p = new Process();
            p.StartInfo.FileName = "netsh.exe";
            p.StartInfo.Arguments = command;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
        }

        private void AddFirewallPortException()
        {
            // must add reference to c:\windows\sytem32\FirewallAPI.dll
            Type NetFwMgrType = Type.GetTypeFromProgID("HNetCfg.FwMgr", false);
            _firewallManager = (INetFwMgr)Activator.CreateInstance(NetFwMgrType);

            Type tPortClass = Type.GetTypeFromProgID("HNetCfg.FWOpenPort");
            _portClass = (INetFwOpenPort)Activator.CreateInstance(tPortClass);

            _profile = _firewallManager.LocalPolicy.CurrentProfile;

            _portClass.Scope = NET_FW_SCOPE_.NET_FW_SCOPE_ALL;
            _portClass.Enabled = true;
            _portClass.Name = String.Format("TMP_{0}", _port);
            _portClass.Port = _port;

            _portClass.Protocol = NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP;

            _profile.GloballyOpenPorts.Add(_portClass);
            OnStatusEvent(0, String.Format("Adding firewall rule to allow incoming connections on port {0}", _port));
        }

        private void OnStatusEvent(int severity, string detail)
        {
            var evt = StatusEvent;
            if (evt != null)
            {
                evt(this, new StatusEventArgs(severity, detail));
            }
        }
    }
}
