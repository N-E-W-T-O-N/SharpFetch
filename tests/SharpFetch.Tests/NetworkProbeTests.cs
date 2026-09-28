using System.Net.NetworkInformation;
using SharpFetch.Core.Models;
using SharpFetch.Platforms.Common;
using Xunit;

namespace SharpFetch.Tests;

public class NetworkProbeTests
{
    [Theory]
    [InlineData(NetworkInterfaceType.Ethernet, "Realtek PCIe GbE Family Controller", NetworkType.Ethernet)]
    [InlineData(NetworkInterfaceType.Ethernet, "Realtek Gaming 2.5GbE Family Controller", NetworkType.Ethernet)]
    [InlineData(NetworkInterfaceType.Ethernet, "Marvell AQtion 5GbE Adapter", NetworkType.Ethernet)]
    [InlineData(NetworkInterfaceType.Ethernet, "Intel(R) Ethernet Connection (7) I219-V", NetworkType.Ethernet)]
    [InlineData(NetworkInterfaceType.Ethernet, "Hyper-V Virtual Ethernet Adapter", NetworkType.Ethernet)]
    [InlineData(NetworkInterfaceType.Wireless80211, "Intel(R) Wi-Fi 6 AX200 160MHz", NetworkType.Wifi)]
    [InlineData(NetworkInterfaceType.Ethernet, "TP-Link Wireless USB Adapter", NetworkType.Wifi)]
    [InlineData(NetworkInterfaceType.Ethernet, "Huawei Mobile Broadband Module", NetworkType.Cellular)]
    [InlineData(NetworkInterfaceType.Ethernet, "Sierra Wireless EM7455 LTE-A", NetworkType.Cellular)]
    [InlineData(NetworkInterfaceType.Wwanpp, "Generic WWAN Adapter", NetworkType.Cellular)]
    [InlineData(NetworkInterfaceType.Ppp, "Dial-Up Connection", NetworkType.Modem)]
    [InlineData(NetworkInterfaceType.Ethernet, "Agere Systems HDA Modem", NetworkType.Modem)]
    [InlineData(NetworkInterfaceType.Tunnel, "WireGuard Tunnel", NetworkType.Tunnel)]
    public void MapInterfaceType_CorrectlyClassifiesAdapters(
        NetworkInterfaceType ifaceType,
        string description,
        NetworkType expectedType)
    {
        var actualType = NetworkProbe.MapInterfaceType(ifaceType, description);
        Assert.Equal(expectedType, actualType);
    }

    [Theory]
    [InlineData("vEthernet (Default Switch)", "Hyper-V Virtual Ethernet Adapter", NetworkType.Ethernet, true)]
    [InlineData("vEthernet (WSL (Hyper-V firewall))", "Hyper-V Virtual Ethernet Adapter #2", NetworkType.Ethernet, true)]
    [InlineData("docker0", "", NetworkType.Ethernet, true)]
    [InlineData("veth123456", "", NetworkType.Ethernet, true)]
    [InlineData("wg0", "WireGuard Tunnel", NetworkType.Tunnel, true)]
    [InlineData("VirtualBox Host-Only Ethernet Adapter", "VirtualBox Host-Only Network", NetworkType.Ethernet, true)]
    [InlineData("VMware Network Adapter VMnet1", "VMware Virtual Ethernet Adapter", NetworkType.Ethernet, true)]
    [InlineData("Ethernet", "Realtek PCIe GbE Family Controller", NetworkType.Ethernet, false)]
    [InlineData("Wi-Fi", "Intel(R) Wi-Fi 6 AX200 160MHz", NetworkType.Wifi, false)]
    [InlineData("eth0", "Intel(R) Ethernet Connection (7) I219-V", NetworkType.Ethernet, false)]
    public void IsVirtualInterface_CorrectlyIdentifiesVirtualAdapters(
        string name,
        string description,
        NetworkType type,
        bool expectedVirtual)
    {
        bool actualVirtual = NetworkProbe.IsVirtualInterface(name, description, type);
        Assert.Equal(expectedVirtual, actualVirtual);
    }
}
