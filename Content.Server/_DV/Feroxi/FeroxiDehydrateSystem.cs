using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Metabolism;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Server._DV.Feroxi;

public sealed class FeroxiDehydrateSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly SatiationSystem _satiation = default!;

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<FeroxiDehydrateComponent, SatiationComponent>();

        while (query.MoveNext(out var uid, out var feroxiDehydrate, out var satiation))
        {
            var currentThirst = _satiation.GetValueOrNull(
                (uid, satiation),
                SatiationSystem.Thirst);

            if (currentThirst == null)
                continue;

            var shouldBeDehydrated = currentThirst <= feroxiDehydrate.DehydrationThreshold;

            if (feroxiDehydrate.Dehydrated != shouldBeDehydrated)
                UpdateDehydrationStatus((uid, feroxiDehydrate), shouldBeDehydrated);
        }
    }

    /// <summary>
    /// Checks and changes the lungs when meeting the threshold for a swap of metabolizer
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="shouldBeDehydrated"></param>
    private void UpdateDehydrationStatus(Entity<FeroxiDehydrateComponent> ent, bool shouldBeDehydrated)
    {
        ent.Comp.Dehydrated = shouldBeDehydrated;

        _body.TryGetOrgansWithComponent<LungComponent>(ent.Owner, out var organs);
        foreach (var entity in organs)
        {
            if (!TryComp<MetabolizerComponent>(entity, out var metabolizer) || metabolizer.MetabolizerTypes == null)
            {
                continue;
            }
            //Changing the metabolizer to the appropriate value based
            var newMetabolizer = shouldBeDehydrated ? ent.Comp.DehydratedMetabolizer : ent.Comp.HydratedMetabolizer;
            metabolizer.MetabolizerTypes!.Clear();
            metabolizer.MetabolizerTypes.Add(newMetabolizer);
        }
    }
}
