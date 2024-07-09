﻿using Artemis.Plugins.RazerBattery.Aurora;

namespace Artemis.Plugins.RazerBattery;

using System;
using System.Linq;
using System.Threading;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;

public class RazerBatteryFetcher : IDisposable
{
    private const int HidReqSetReport = 0x09;
    private const int HidReqGetReport = 0x01; // Add GET_REPORT request
    private const int UsbTypeClass = 0x20;
    private const int UsbRecipInterface = 0x01;
    private const int UsbDirOut = 0x00;
    private const int UsbDirIn = 0x80; // Direction IN for reading
    private const int UsbTypeRequestOut = UsbTypeClass | UsbRecipInterface | UsbDirOut;
    private const int UsbTypeRequestIn = UsbTypeClass | UsbRecipInterface | UsbDirIn;
    private const int RazerUsbReportLen = 90; // Example length, set this according to actual length

    public double MouseBatteryPercentage { get; private set; }
    private readonly Timer _timer;

    public RazerBatteryFetcher()
    {
        _timer = new Timer(_ => UpdateBattery(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20));
    }

    private byte[] GenerateMessage(RazerMouseHidInfo mouseHidInfo)
    {
        var tid = byte.Parse(mouseHidInfo.TransactionId.Split('x')[1], System.Globalization.NumberStyles.HexNumber);
        var header = new byte[] { 0x00, tid, 0x00, 0x00, 0x00, 0x02, 0x07, 0x80 };

        var crc = 0;
        for (var i = 2; i < header.Length; i++)
        {
            crc ^= header[i];
        }

        var data = new byte[80];
        var crcData = new byte[] { (byte)crc, 0 };

        return header.Concat(data).Concat(crcData).ToArray();
    }

    private void UpdateBattery()
    {
        const int vendorId = 0x1532;
        
        Mutex mutex = new(false, "Global\\RazerLinkReadWriteGuardMutex");

        try
        {
            if (!mutex.WaitOne(TimeSpan.FromMilliseconds(2000), false))
            {
                mutex.Dispose();
                return;
            }
        }
        catch (AbandonedMutexException)
        {
            //continue
        }

        var res = GetValue(vendorId);
        mutex.Dispose();

        if (res == null)
        {
            return;
        }

        MouseBatteryPercentage = res[9] / 255.0 * 100;
    }

    private byte[]? GetValue(int vendorId)
    {
        var mouseDictionary = SettingsUpdater.RazerDeviceInfo.MouseHidInfos;

        using var context = new UsbContext();
        var usbDevice = context.Find(d =>
            d.VendorId == vendorId &&
            mouseDictionary.ContainsKey(GetDeviceProductKeyString(d)));
        if (usbDevice == null)
        {
            return null;
        }

        var mouseHidInfo = mouseDictionary[GetDeviceProductKeyString(usbDevice)];
        var msg = GenerateMessage(mouseHidInfo);

        usbDevice.Open();
        RazerSendControlMsg(usbDevice, msg, 0x09, 200, 2000);
        var res = RazerReadResponseMsg(usbDevice, 0x01);
        usbDevice.Close();
        usbDevice.Dispose();
        return res;
    }

    private static string GetDeviceProductKeyString(IUsbDevice device)
    {
        return "0x"+device.ProductId.ToString("X4");
    }

    private static void RazerSendControlMsg(IUsbDevice usbDev, byte[] data, uint reportIndex, int waitMin, int waitMax)
    {
        const ushort value = 0x300;

        var setupPacket = new UsbSetupPacket(UsbTypeRequestOut, HidReqSetReport, value, (ushort)reportIndex, (ushort)data.Length);

        // Send USB control message
        var transferredLength = data.Length;
        var ec = usbDev.ControlTransfer(setupPacket, data, 0, transferredLength);
        if (ec == 0)
        {
            return;
        }

        // Wait
        var waitTime = new Random().Next(waitMin, waitMax);
        Thread.Sleep(waitTime);
    }

    private static byte[]? RazerReadResponseMsg(IUsbDevice usbDev, uint reportIndex)
    {
        const ushort value = 0x300;
        var responseBuffer = new byte[RazerUsbReportLen];

        var setupPacket = new UsbSetupPacket(UsbTypeRequestIn, HidReqGetReport, value, (ushort)reportIndex, (ushort)responseBuffer.Length);

        // Receive USB control message
        var transferredLength = responseBuffer.Length;
        var ec = usbDev.ControlTransfer(setupPacket, responseBuffer, 0, transferredLength);
        if (ec == 0)
        {
            return null;
        }

        return transferredLength != responseBuffer.Length ? null : responseBuffer;
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}