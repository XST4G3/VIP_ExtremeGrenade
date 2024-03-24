using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Cvars.Validators;
using CounterStrikeSharp.API.Modules.Utils;
using VipCoreApi;

namespace ExtremeGrenade
{
    public sealed class Plugin : BasePlugin
    {
        public override string ModuleName => "[VIP] ExtremeGrenade";
        public override string ModuleAuthor => "xstage";
        public override string ModuleVersion => "1.0.0";

        private ExtremeGrenade? _feature;
        private IVipCoreApi? _api;

        private PluginCapability<IVipCoreApi> _pluginCapability = new("vipcore:core");

        public FakeConVar<int> ExtremeGrenadeDamage
            = new("extreme_grenade_damage", "Урон от дополнительных гранат (int, Default: 45)", 45, ConVarFlags.FCVAR_NONE);
        public FakeConVar<float> ExtremeGrenadeScale
            = new("extreme_grenade_scale", "Масштаб моделей (float, Default: 1.0)", 1.0f, ConVarFlags.FCVAR_NONE);
        public FakeConVar<int> ExtremeGrenadeRadius
            = new("extreme_grenade_radius", "Радиус поражения дополнительными гранатами (int, Default: 700)", 700, ConVarFlags.FCVAR_NONE);
        public FakeConVar<float> ExtremeGrenadeGravity
            = new("extreme_grenade_gravity", "Гравитация дополнительных гранат (float). Gravity to set (default = 1.0, half = 0.5, double = 2.0).", 1.0f, ConVarFlags.FCVAR_NONE, new RangeValidator<float>(0.5f, 2.0f));

        public override void Load(bool hotReload) => RegisterFakeConVars(typeof(ConVar));

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            _api = _pluginCapability.Get();
            if (_api == null) return;

            _api.OnCoreReady += () =>
            {
                _feature = new ExtremeGrenade(this, _api);
                _api.RegisterFeature(_feature);
            };
        }

        public override void Unload(bool hotReload)
        {
            if (_feature == null) return;

            _api?.UnRegisterFeature(_feature);
        }

        public void CreateExtremeGrenade<T>(CCSPlayerController player, Vector pos, string entityName) where T : CBaseCSGrenadeProjectile
        {
            var entity = Utilities.CreateEntityByName<T>(entityName);

            if (entity == null || !entity.IsValid) return;

            if (entity is CFlashbangProjectile flashbang)
            {
                flashbang.TimeToDetonate = (float)Random.Shared.NextDouble();
                flashbang.DetonateTime = (float)Random.Shared.NextDouble();
            }
            else
            {
                AddTimer((float)Random.Shared.NextDouble(), () =>
                {
                    entity.AcceptInput("InitializeSpawnFromWorld");
                });
            }

            entity.DispatchSpawn();

            entity.AcceptInput("FireUser1", player, player, "");

            entity.TeamNum = player.TeamNum;
            entity.Thrower.Raw = player.PlayerPawn.Raw;
            entity.Globalname = "custom";
            entity.Damage = ExtremeGrenadeDamage.Value;
            entity.DmgRadius = ExtremeGrenadeRadius.Value;

            entity.AngVelocity.X = Random.Shared.Next(-1000, 1000);
            entity.AngVelocity.Y = Random.Shared.Next(-1000, 1000);
            entity.AngVelocity.Z = Random.Shared.Next(-1000, 1000);

            if (entity.CBodyComponent != null && entity.CBodyComponent.SceneNode != null)
                entity.CBodyComponent.SceneNode.Scale = ExtremeGrenadeScale.Value;

            entity.GravityScale = ExtremeGrenadeGravity.Value;
            entity.Elasticity = 0.3f;

            var angels = new QAngle(Random.Shared.Next(-300, 300), Random.Shared.Next(-300, 300));
            var speedVec = new Vector(Random.Shared.Next(-300, 300), Random.Shared.Next(-300, 300), 300);

            entity.Teleport(pos, angels, speedVec);
        }
    }

    public class ExtremeGrenade : VipFeatureBase
    {
        public override string Feature => "ExtremeGrenade";

        private Plugin _plugin;

        public ExtremeGrenade(Plugin plugin, IVipCoreApi api): base(api)
        {
            _plugin = plugin;

            plugin.RegisterEventHandler<EventHegrenadeDetonate>(OnHegrenadeDetonate);
            plugin.RegisterEventHandler<EventFlashbangDetonate>(OnFlashbangDetonate);
        }

        private HookResult OnHegrenadeDetonate(EventHegrenadeDetonate @event, GameEventInfo info)
        {
            var player = @event.Userid;
            var entity = Utilities.GetEntityFromIndex<CBaseCSGrenadeProjectile>(@event.Entityid);

            if (player == null || !player.IsValid || !player.PlayerPawn.IsValid || entity?.Globalname == "custom")
                return HookResult.Continue;

            if (GetPlayerFeatureState(player) != IVipCoreApi.FeatureState.Enabled)
                return HookResult.Continue;

            Vector pos = new Vector(@event.X, @event.Y, @event.Z);
            for (int i = 0; i < GetFeatureValue<GrenadesCount>(player).Hegrenade; ++i)
                _plugin.CreateExtremeGrenade<CHEGrenadeProjectile>(player, pos, "hegrenade_projectile");

            return HookResult.Continue;
        }

        private HookResult OnFlashbangDetonate(EventFlashbangDetonate @event, GameEventInfo info)
        {
            var player = @event.Userid;
            var entity = Utilities.GetEntityFromIndex<CBaseCSGrenadeProjectile>(@event.Entityid);

            if (player == null || !player.IsValid || !player.PlayerPawn.IsValid || entity?.Globalname == "custom")
                return HookResult.Continue;

            if (GetPlayerFeatureState(player) != IVipCoreApi.FeatureState.Enabled)
                return HookResult.Continue;

            Vector pos = new Vector(@event.X, @event.Y, @event.Z);

            for (int i = 0; i < GetFeatureValue<GrenadesCount>(player).Flashbang; ++i)
                _plugin.CreateExtremeGrenade<CFlashbangProjectile>(player, pos, "flashbang_projectile");

            return HookResult.Continue;
        }
    }

    public class GrenadesCount
    {
        public int Hegrenade { get; set; } = 0;
        public int Flashbang { get; set; } = 0;
    }
}