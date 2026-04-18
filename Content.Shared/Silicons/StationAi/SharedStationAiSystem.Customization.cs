using Content.Shared.Holopad;
using Content.Shared.Intellicard;
using Content.Shared.Mobs;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Silicons.StationAi;

public abstract partial class SharedStationAiSystem
{
    private ProtoId<StationAiCustomizationGroupPrototype> _stationAiCoreCustomGroupProtoId = "StationAiCoreIconography";
    private ProtoId<StationAiCustomizationGroupPrototype> _stationAiHologramCustomGroupProtoId = "StationAiHolograms";

    private readonly SpriteSpecifier.Rsi _stationAiRebooting = new(new ResPath("Mobs/Silicon/station_ai.rsi"), "ai_fuzz");

    private void InitializeCustomization()
    {
        SubscribeLocalEvent<StationAiCoreComponent, StationAiCustomizationMessage>(OnStationAiCustomization);

        SubscribeLocalEvent<StationAiCustomizationComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<StationAiCustomizationComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<StationAiCustomizationComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnStationAiCustomization(Entity<StationAiCoreComponent> entity, ref StationAiCustomizationMessage args)
    {
        if (!_protoManager.Resolve(args.GroupProtoId, out var groupPrototype) || !_protoManager.Resolve(args.CustomizationProtoId, out var customizationProto))
            return;

        if (!TryGetHeld((entity, entity.Comp), out var held))
            return;

        if (!TryComp<StationAiCustomizationComponent>(held, out var stationAiCustomization))
            return;

        if (stationAiCustomization.ProtoIds.TryGetValue(args.GroupProtoId, out var protoId) && protoId == args.CustomizationProtoId)
            return;

        stationAiCustomization.ProtoIds[args.GroupProtoId] = args.CustomizationProtoId;

        Dirty(held.Value, stationAiCustomization);

        // Update hologram
        if (groupPrototype.Category == StationAiCustomizationType.Hologram)
            UpdateHolographicAvatar((held.Value, stationAiCustomization));

        // Update core iconography
        if (groupPrototype.Category == StationAiCustomizationType.CoreIconography && TryComp<StationAiHolderComponent>(entity, out var stationAiHolder))
            UpdateAppearance((entity, stationAiHolder));
    }

    private void OnPlayerAttached(Entity<StationAiCustomizationComponent> ent, ref PlayerAttachedEvent args)
    {
        var state = _mobState.IsDead(ent) ? StationAiState.Dead : StationAiState.Occupied;
        SetStationAiState(ent, state);
    }

    private void OnPlayerDetached(Entity<StationAiCustomizationComponent> ent, ref PlayerDetachedEvent args)
    {
        var state = _mobState.IsDead(ent) ? StationAiState.Dead : StationAiState.Rebooting;
        SetStationAiState(ent, state);
    }

    protected virtual void OnMobStateChanged(Entity<StationAiCustomizationComponent> ent, ref MobStateChangedEvent args)
    {
        var state = (args.NewMobState == MobState.Dead) ? StationAiState.Dead : StationAiState.Rebooting;
        SetStationAiState(ent, state);
    }

    protected void SetStationAiState(Entity<StationAiCustomizationComponent> ent, StationAiState state)
    {
        if (ent.Comp.State != state)
        {
            ent.Comp.State = state;
            Dirty(ent);

            var ev = new StationAiCustomizationStateChanged(state);
            RaiseLocalEvent(ent, ref ev);
        }

        if (_containers.TryGetContainingContainer(ent.Owner, out var container) &&
             TryComp<StationAiHolderComponent>(container.Owner, out var holder))
        {
            UpdateAppearance((container.Owner, holder));
        }
    }

    private void UpdateHolographicAvatar(Entity<StationAiCustomizationComponent> entity)
    {
        if (!TryComp<HolographicAvatarComponent>(entity, out var avatar))
            return;

        if (!entity.Comp.ProtoIds.TryGetValue(_stationAiHologramCustomGroupProtoId, out var protoId))
            return;

        if (!_protoManager.Resolve(protoId, out var prototype))
            return;

        if (!prototype.LayerData.TryGetValue(StationAiState.Hologram.ToString(), out var layerData))
            return;

        avatar.LayerData = [layerData];
        Dirty(entity, avatar);
    }

    private void CustomizeAppearance(Entity<StationAiCoreComponent> entity, StationAiState state)
    {
        var stationAi = GetInsertedAI(entity);

        if (!TryComp<StationAiCustomizationComponent>(stationAi, out var stationAiCustomization) ||
            !TryGetCustomizedAppearanceData((stationAi.Value, stationAiCustomization), out var layerData) ||
            !layerData.TryGetValue(state.ToString(), out var stateData))
        {
            return;
        }

        // This data is handled manually in the client StationAiSystem
        _appearance.SetData(entity.Owner, StationAiVisualLayers.Icon, stateData);
    }
    private void CustomizeAppearanceCard(Entity<IntellicardComponent> entity, StationAiState state)
    {
        var stationAi = GetCardInsertedAI(entity);
        Log.Log(LogLevel.Info, "entity acquired");
        if (!TryComp<StationAiCustomizationComponent>(stationAi, out var stationAiCustomization) ||
            !TryGetCustomizedAppearanceData((stationAi.Value, stationAiCustomization), out var layerData, returnCardValue: true) ||
            !layerData.TryGetValue(state.ToString(), out var stateData))
        {
            Log.Log(LogLevel.Info, "well we failed, fuck");
            Log.Log(LogLevel.Info, "custom comp " + HasComp<StationAiCustomizationComponent>(stationAi));
            Log.Log(LogLevel.Info, "state: " + state.ToString());
            return;
        }
        Log.Log(LogLevel.Info, "info card set yes yay");
        // This data is handled manually in the client StationAiSystem
        _appearance.SetData(entity.Owner, StationAiVisualLayers.Icon, stateData);
    }

    /// <summary>
    /// Returns a dictionary containing the station AI's appearance for different states.
    /// </summary>
    /// <param name="entity">The station AI.</param>
    /// <param name="layerData">The appearance data, indexed by possible AI states.</param>
    /// <param name="returnCardValue">If set to true, this returns the designated appearance data for the intellicarded AI instead of the normal core data.</param>
    /// <returns>True if the apperance data was found.</returns>
    public bool TryGetCustomizedAppearanceData(Entity<StationAiCustomizationComponent> entity, [NotNullWhen(true)] out Dictionary<string, PrototypeLayerData>? layerData, bool returnCardValue = false)
    {
        layerData = null;
        // I removed the count check here since I believe its redundent?
        if (!entity.Comp.ProtoIds.TryGetValue(_stationAiCoreCustomGroupProtoId, out var protoId) ||
           !_protoManager.Resolve(protoId, out var prototype)
           )
        {
            Log.Log(LogLevel.Info, "failed cuz of cant get this, terrible, i am sad");
            return false;
        }
        Log.Log(LogLevel.Info, "protoid was "+protoId);
        Log.Log(LogLevel.Info, "should be a-okay");
        if (returnCardValue)
        {
            layerData = prototype.CardLayerData;
            Log.Log(LogLevel.Info, "k " + string.Join("\n", prototype.CardLayerData));
        }
        else
        {
            layerData = prototype.LayerData;
        }

        return true;
    }
}
