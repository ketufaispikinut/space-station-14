using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Solution;

/// <summary>
/// Spills the provided solution onto the floor
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class SpillSolutionEntityEffectSystem : EntityEffectSystem<TransformComponent, SpillSolution>
{
    [Dependency] private SharedPuddleSystem _puddle = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<SpillSolution> args)
    {
        _puddle.TrySpillAt(entity.Owner, args.Effect.Solution, out _);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class SpillSolution : EntityEffectBase<SpillSolution>
{
    /// <summary>
    ///     The solution to spill
    /// </summary>
    [DataField(required: true)]
    public Content.Shared.Chemistry.Components.Solution Solution;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-spill-solution"
        );
}
