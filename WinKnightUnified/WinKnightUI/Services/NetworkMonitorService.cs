using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Diagnostics;

namespace WinKnightUI.Services
{
    /// <summary>
    /// Service for monitoring network adapters and throughput.
    /// </summary>
    public class NetworkMonitorService : IDisposable
    {
        private readonly Dictionary<string, NetworkInterfaceStats> _previousStats = new();
        private DateTime _lastUpdate = DateTime.MinValue;
        private bool _isDisposed;

        public class NetworkInterfaceInfo
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string Speed { get; set; } = string.Empty;
            public string MacAddress { get; set; } = string.Empty;
            public string IpAddress { get; set; } = string.Empty;
            public double DownloadSpeedMbps { get; set; }
            public double UploadSpeedMbps { get; set; }
            public long TotalBytesReceived { get; set; }
            public long TotalBytesSent { get; set; }
        }

        private class NetworkInterfaceStats
        {
            public long BytesReceived { get; set; }
            public long BytesSent { get; set; }
            public DateTime Timestamp { get; set; }
        }

        /// <summary>
        /// Gets information for all active network interfaces.
        /// </summary>
        public async Task<List<NetworkInterfaceInfo>> GetNetworkInterfacesAsync()
        {
            return await Task.Run(() =>
            {
                var interfaces = new List<NetworkInterfaceInfo>();

                try
                {
                    foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                    {
                        // Skip loopback and tunnel adapters
                        if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                            ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                            continue;

                        var stats = ni.GetIPv4Statistics();
                        var info = new NetworkInterfaceInfo
                        {
                            Name = ni.Name,
                            Description = ni.Description,
                            Type = ni.NetworkInterfaceType.ToString(),
                            Status = ni.OperationalStatus.ToString(),
                            Speed = FormatSpeed(ni.Speed),
                            MacAddress = FormatMacAddress(ni.GetPhysicalAddress()),
                            TotalBytesReceived = stats.BytesReceived,
                            TotalBytesSent = stats.BytesSent
                        };

                        // Get IP address
                        var ipProps = ni.GetIPProperties();
                        var ipv4 = ipProps.UnicastAddresses
                            .FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                        info.IpAddress = ipv4?.Address.ToString() ?? "N/A";

                        // Calculate speed if we have previous stats
                        if (_previousStats.TryGetValue(ni.Id, out var prevStats))
                        {
                            var timeDiff = (DateTime.Now - prevStats.Timestamp).TotalSeconds;
                            if (timeDiff > 0)
                            {
                                var bytesReceivedDiff = stats.BytesReceived - prevStats.BytesReceived;
                                var bytesSentDiff = stats.BytesSent - prevStats.BytesSent;

                                info.DownloadSpeedMbps = (bytesReceivedDiff * 8 / 1_000_000.0) / timeDiff;
                                info.UploadSpeedMbps = (bytesSentDiff * 8 / 1_000_000.0) / timeDiff;
                            }
                        }

                        // Update stats
                        _previousStats[ni.Id] = new NetworkInterfaceStats
                        {
                            BytesReceived = stats.BytesReceived,
                            BytesSent = stats.BytesSent,
                            Timestamp = DateTime.Now
                        };

                        interfaces.Add(info);
                    }

                    _lastUpdate = DateTime.Now;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"NetworkMonitorService: Error getting interfaces: {ex.Message}");
                }

                return interfaces;
            });
        }

        /// <summary>
        /// Gets the primary active network interface.
        /// </summary>
        public async Task<NetworkInterfaceInfo?> GetPrimaryInterfaceAsync()
        {
            var interfaces = await GetNetworkInterfacesAsync();
            return interfaces
                .Where(i => i.Status == "Up")
                .OrderByDescending(i => i.DownloadSpeedMbps + i.UploadSpeedMbps)
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets total network throughput across all interfaces.
        /// </summary>
        public async Task<(double DownloadMbps, double UploadMbps)> GetTotalThroughputAsync()
        {
            var interfaces = await GetNetworkInterfacesAsync();
            var download = interfaces.Where(i => i.Status == "Up").Sum(i => i.DownloadSpeedMbps);
            var upload = interfaces.Where(i => i.Status == "Up").Sum(i => i.UploadSpeedMbps);
            return (Math.Round(download, 2), Math.Round(upload, 2));
        }

        /// <summary>
        /// Checks if the system has internet connectivity.
        /// </summary>
        public async Task<bool> HasInternetConnectionAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var ping = new Ping();
                    var reply = ping.Send("8.8.8.8", 1000);
                    return reply.Status == IPStatus.Success;
                }
                catch
                {
                    return false;
                }
            });
        }

        /// <summary>
        /// Flushes the DNS cache.
        /// </summary>
        public async Task<bool> FlushDnsCacheAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "ipconfig",
                            Arguments = "/flushdns",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true
                        }
                    };
                    process.Start();
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"NetworkMonitorService: Flush DNS failed: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// Resets Winsock catalog.
        /// </summary>
        public async Task<bool> ResetWinsockAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "netsh",
                            Arguments = "winsock reset",
                            UseShellExecute = true,
                            Verb = "runas",
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"NetworkMonitorService: Winsock reset failed: {ex.Message}");
                    return false;
                }
            });
        }

        private static string FormatSpeed(long speedBps)
        {
            if (speedBps >= 1_000_000_000)
                return $"{speedBps / 1_000_000_000.0:F1} Gbps";
            if (speedBps >= 1_000_000)
                return $"{speedBps / 1_000_000.0:F0} Mbps";
            if (speedBps >= 1_000)
                return $"{speedBps / 1_000.0:F0} Kbps";
            return $"{speedBps} bps";
        }

        private static string FormatMacAddress(PhysicalAddress address)
        {
            var bytes = address.GetAddressBytes();
            return string.Join(":", bytes.Select(b => b.ToString("X2")));
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _previousStats.Clear();
        }
    }
}
