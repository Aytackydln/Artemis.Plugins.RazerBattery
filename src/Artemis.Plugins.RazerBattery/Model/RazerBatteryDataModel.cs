using Artemis.Core.Modules;
using JetBrains.Annotations;

namespace Artemis.Plugins.RazerBattery.Model;

[PublicAPI]
public class RazerBatteryDataModel : DataModel
{
    public double MouseBatteryLevel => RazerBatteryModule.BatteryFetcher?.MouseBatteryPercentage ?? -1;
}
