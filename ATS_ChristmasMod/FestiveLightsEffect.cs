using System.Linq;
using Eremite.Buildings;
using Eremite.Controller.Effects;
using Eremite.Model.Effects;
using Eremite.Services;
using UniRx;

namespace JingleBellHollow;

public class DecorationPlacedHook : HookLogic
{
    public static HookLogicType ID;
    
    public DecorationTier tier;
    public int amount;

    public bool IsMatching(Building building)
    {
        return building is Decoration decoration && decoration.model.tier == tier;
    }

    public override bool HasImpactOn(BuildingModel building)
    {
        return building is DecorationModel decoration && decoration.tier == tier;
    }

    public override string GetAmountText()
    {
        return amount.ToString();
    }

    public override HookLogicType Type => ID;
}

public class DecorationOwnedTracker : HookTracker<DecorationPlacedHook>
{
    public DecorationOwnedTracker(HookState hookState, DecorationPlacedHook model, HookedEffectModel effectModel, HookedEffectState effectState)
        : base(hookState, model, effectModel, effectState)
    {

    }

    public void OnBuildingPlaced(Building building)
    {
        if (model.IsMatching(building))
        {
            Plugin.Log.LogInfo("OnBuildingPlaced " + building.name + " and is matching");
            Decoration d = building as Decoration;
            Update(d.model.decorationScore);
        }
        else
        {
            Plugin.Log.LogInfo("OnBuildingPlaced " + building.name);
        }
    }

    public void OnBuildingRemoved(Building building)
    {
        if (model.IsMatching(building))
        {
            Plugin.Log.LogInfo("OnBuildingRemoved " + building.name + " and is matching");
            Decoration d = building as Decoration;
            Update(-d.model.decorationScore);
        }
        else
        {
            Plugin.Log.LogInfo("OnBuildingRemoved " + building.name);
        }
    }

    public void SetAmount(int amount)
    {
        Update(amount - hookState.totalAmount);
    }

    private void Update(int amount)
    {
        hookState.totalAmount += amount;
        hookState.currentAmount += amount;
        while (hookState.currentAmount >= model.amount)
        {
            Fire();
            hookState.currentAmount -= model.amount;
        }
        while (hookState.currentAmount < 0)
        {
            Revert();
            hookState.currentAmount += model.amount;
        }
    }
}

public class DecorationsOwnedHooksMonitor : HookMonitor<DecorationPlacedHook, DecorationOwnedTracker>
{
    public override void AddHandle(DecorationOwnedTracker tracker)
    {
        tracker.handle.Add(ObservableExtensions.Subscribe(Serviceable.GameBlackboardService.BuildingFinished, tracker.OnBuildingPlaced));
        tracker.handle.Add(ObservableExtensions.Subscribe(Serviceable.GameBlackboardService.FinishedBuildingRemoved, tracker.OnBuildingRemoved));
    }

    public override DecorationOwnedTracker CreateTracker(HookState state, DecorationPlacedHook model, HookedEffectModel effectModel, HookedEffectState effectState)
    {
        return new DecorationOwnedTracker(state, model, effectModel, effectState);
    }

    public override void InitValue(DecorationOwnedTracker tracker)
    {
        tracker.SetAmount(GetInitValueFor(tracker.model));
    }

    public override int GetInitValueFor(DecorationPlacedHook model)
    {
        return Serviceable.BuildingsService.Buildings.Values.Count(model.IsMatching);
    }

    public override int GetInitProgressFor(DecorationPlacedHook model)
    {
        return GetInitValueFor(model) % model.amount;
    }

    public override int GetFiredAmountPreviewFor(DecorationPlacedHook model)
    {
        return GetInitValueFor(model) / model.amount;
    }
}