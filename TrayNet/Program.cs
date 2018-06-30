using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;
using TrayNet.Properties;

namespace TrayNet
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new MyCustomApplicationContext());
        }
    }


    public class MyCustomApplicationContext : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private WebClient webClient = new WebClient();
        private Ping ping = new Ping();

        public MyCustomApplicationContext()
        {
            // Initialize Tray Icon
            trayIcon = new NotifyIcon()
            {
                Icon = Resources.AppIcon,
                ContextMenu = new ContextMenu(new MenuItem[] {
                    new MenuItem("? ms", CopySelf(0)),
                    new MenuItem("-"),
                    new MenuItem("Copy MAC address", CopyMAC),
                    new MenuItem("-"),
                    new MenuItem("Copy Public IP", CopyPublicIP),
                    new MenuItem("Copy Local IP", CopyLocalIP),
                    new MenuItem("-"),
                    new MenuItem("Exit", Exit)
                }),
                Visible = true
            };

            new System.Threading.Timer(_ => UpdatePing(), null, 0, 5000);
        }

        void UpdatePing()
        {
            try
            {
                trayIcon.ContextMenu.MenuItems[0].Text = ping.Send("www.google.com").RoundtripTime.ToString() + " ms";
            } catch (Exception ex) {
                if (!trayIcon.ContextMenu.MenuItems[0].Text.StartsWith("Failed "))
                    trayIcon.ContextMenu.MenuItems[0].Text = "Failed to get ping. (last: " + trayIcon.ContextMenu.MenuItems[0].Text + ")";
            }
        }

        void CopyPublicIP(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText(webClient.DownloadString("https://api.ipify.org/"));
            } catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "TrayNet");
            }
        }

        void CopyLocalIP(object sender, EventArgs e)
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            Clipboard.SetText(ip.Address.ToString());
                            return;
                        }
                    }
                }
            }

            MessageBox.Show("No network adapters with an IPv4 address in the system!", "TrayNet");
        }

        void CopyMAC(object sender, EventArgs e)
        {
            Clipboard.SetText((
                from nic in NetworkInterface.GetAllNetworkInterfaces()
                where nic.OperationalStatus == OperationalStatus.Up && (nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                select nic.GetPhysicalAddress().ToString()
            ).FirstOrDefault());
        }

        EventHandler CopySelf(int id)
        {
            return (object sender, EventArgs e) =>
            {
                Clipboard.SetText(trayIcon.ContextMenu.MenuItems[id].Text);
            };
        }

        void Exit(object sender, EventArgs e)
        {
            // Hide tray icon, otherwise it will remain shown until user mouses over it
            trayIcon.Visible = false;
            Application.Exit();
        }
    }
}
