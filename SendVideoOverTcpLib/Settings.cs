using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SendVideoOverTCPLib.ViewModels;

namespace SendVideoOverTCPLib;

public static class Settings
{
    public static void SaveSelectedSettings(NetworkViewModel networkViewModel)
    {
        Preferences.Set("SelectedIpAddress", networkViewModel.SelectedIpAddress);
        Preferences.Set("Port", networkViewModel.SelectedPort);
        Preferences.Set("StartHostId", networkViewModel.StartHostId);
        Preferences.Set("EndHostId", networkViewModel.EndHostId);
        Preferences.Set("TimeoutInHalfSeconds", networkViewModel.TimeoutInHalfSeconds); 
        Preferences.Set("TimeoutInMs", networkViewModel.TimeoutInMs); 
    }

    public static NetworkViewModel GetSettingNetworkViewModel()
    {
        var networkViewModel = new NetworkViewModel
        {
            SelectedIpAddress = GetSettingSelectedIPAddress(),
            SelectedPort = GetSettingSelectedPort() > 0 ? GetSettingSelectedPort() : 5000,
            StartHostId = GetSettingStartHostId(),
            EndHostId = GetSettingEndHostId(),
            TimeoutInHalfSeconds = GetSettingTimeoutInHalfSeconds(),
            TimeoutInMs = GetSettingTimeoutInMs()
        };
        return networkViewModel;
    }


    // using System.Net.NetworkInformation;

    public static int GetSettingSelectedPort()
    {
        return Preferences.Get("Port", 5000);
    }

    public static int GetSettingStartHostId()
    {
        return Preferences.Get("StartHostId", 2);
    }

    public static int GetSettingEndHostId()
    {
        return Preferences.Get("EndHostId", 20);
    }

    public static void SaveHostIdRange(int startHostId, int endHostId)
    {
        Preferences.Set("StartHostId", startHostId);
        Preferences.Set("EndHostId", endHostId);
    }

    public static string GetSettingSelectedIPAddress()
    {
        return Preferences.Get("SelectedIpAddress", string.Empty);
    }



    public static int GetSettingTimeoutInHalfSeconds()
    {
        return Preferences.Get("TimeoutInHalfSeconds", 6);
    }

    public static int GetSettingTimeoutInMs()
    {
        return Preferences.Get("GTimeoutInMs", 500);
    }

    public static void ClearAllPreferences()
    {
        Preferences.Clear();
    }

    public static void ClearPreferences()
    {
        int startHostId = GetSettingStartHostId();
        int endHostId = GetSettingEndHostId();
        Preferences.Clear();
        Preferences.Set("StartHostId", startHostId);
        Preferences.Set("EndHostId", endHostId);
    }
}
