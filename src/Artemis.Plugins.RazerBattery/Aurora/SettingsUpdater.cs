using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Artemis.Plugins.RazerBattery.Aurora;

public static class SettingsUpdater
{
    public static RazerDevices RazerDeviceInfo { get; private set; } = new();
    
    public static async Task Update()
    {
        RazerDeviceInfo = await JsonUtils.GetRazerDeviceInfo();
        _ = Task.Run(async () =>
        {
            try
            {
                await UpdateDeviceInfo();
            }
            catch
            {
                //ignore
            }
        });
    }

    private static async Task UpdateDeviceInfo()
    {
        using (var client = new HttpClient())
        {
            var httpResponseMessage = await client.GetAsync("https://raw.githubusercontent.com/Aurora-RGB/Online-Settings/master/RazerDevices.json");
            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                return;
            }

            await httpResponseMessage.Content.CopyToAsync(File.OpenWrite(JsonUtils.RazerDeviceInfoLocalCache));
        }

        RazerDeviceInfo = await JsonUtils.GetRazerDeviceInfo();
    }
}