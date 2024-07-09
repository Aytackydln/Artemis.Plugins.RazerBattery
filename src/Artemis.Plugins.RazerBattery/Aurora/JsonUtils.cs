using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Artemis.Plugins.RazerBattery.Aurora;

[JsonSerializable(typeof(RazerDevices))]
internal partial class OnlineSettingsSourceGenerationContext : JsonSerializerContext;

public class JsonUtils
{
    public const string RazerDevices = "RazerDevices.json";
    public static readonly string RazerDeviceInfoLocalCache = Path.Combine(".", RazerDevices);

    public static async Task<RazerDevices> GetRazerDeviceInfo()
    {
        return await ParseLocalJson<RazerDevices>(RazerDeviceInfoLocalCache);
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        TypeInfoResolverChain = { OnlineSettingsSourceGenerationContext.Default }
    };

    private static Task<T> ParseLocalJson<T>(string cachePath) where T : new()
    {
        using var stream = GetJsonStream(cachePath);

        return JsonSerializer.DeserializeAsync<T>(stream, JsonSerializerOptions)
            .AsTask()
            .ContinueWith(t => t.Status switch
            {
                TaskStatus.RanToCompletion => t.Result ?? new T(),
                _ => new T(),
            });
    }

    private static Stream GetJsonStream(string cachePath)
    {
        return File.Exists(cachePath) ? File.Open(cachePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite) : new MemoryStream(Encoding.UTF8.GetBytes(string.Empty));
    }
}