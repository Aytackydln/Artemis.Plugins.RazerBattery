using System.Collections.Generic;
using Artemis.Core.Modules;
using Artemis.Plugins.RazerBattery.Aurora;
using Artemis.Plugins.RazerBattery.Model;
using JetBrains.Annotations;

namespace Artemis.Plugins.RazerBattery;

[UsedImplicitly]
public class RazerBatteryModule : Module<RazerBatteryDataModel>
{
    public static RazerBatteryFetcher? BatteryFetcher { get; private set; }
    
    public override void Enable()
    {
        SettingsUpdater.Update().Wait();
        BatteryFetcher = new RazerBatteryFetcher();
    }

    public override void Disable()
    {
        BatteryFetcher?.Dispose();
        BatteryFetcher = null;
    }

    public override void Update(double deltaTime)
    {
    }

    public override List<IModuleActivationRequirement>? ActivationRequirements => null;
}