
using Content.Shared.EntityEffects;

namespace Content.Shared.Morgue.Components;

/// <summary>
/// Applies these effects to an entity right before it is ashed, by a crematorium or by heat
/// </summary>
[RegisterComponent]
public sealed partial class EntityEffectOnAshComponent : Component
{
    /// <summary>
    /// The effects to apply to the ashed entity.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Effects;
}
