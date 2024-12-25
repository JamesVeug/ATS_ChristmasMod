using System;
using System.Collections;
using ATS_API.Biomes;
using ATS_API.Buildings;
using ATS_API.Decorations;
using ATS_API.Effects;
using ATS_API.Helpers;
using ATS_API.Localization;
using ATS_API.NaturalResource;
using BepInEx;
using BepInEx.Logging;
using Eremite.Buildings;
using Eremite.Model;
using Eremite.Model.Effects;
using Eremite.Model.Effects.Hooked;
using HarmonyLib;
using UnityEngine;

namespace JingleBellHollow;

[HarmonyPatch]
[BepInPlugin(GUID, NAME, VERSION)]
public class Plugin : BaseUnityPlugin
{
    private const string GUID = "Jingle_Bell_Hollow";
    private const string NAME = "Jingle Bell Hollow";
    private const string VERSION = "1.0.1";

    public static Plugin Instance;
    public static ManualLogSource Log;

    private static AssetBundle christmasBundle;
    
    private DecorationTierBuilder festiveTier;
    private HookedEffectBuilder festiveLights;
    private static string festiveDecorationColor = ColorUtility.ToHtmlStringRGB(Color.magenta);
    private static string festival = string.Format("<color=#{0}>Festival</color>", festiveDecorationColor);

    private void Awake()
    {
        Log = Logger;
        Instance = this;
        
        // Stops Unity from destroying it for some reason. Same as Setting the BepInEx config HideManagerGameObject to true.
        gameObject.hideFlags = HideFlags.HideAndDontSave;
        

        if (!AssetBundleHelper.TryLoadAssetBundleFromFile("christmas", out christmasBundle))
        {
            Log.LogError("Failed to load asset bundle");
            return;
        }
        if (christmasBundle == null)
        {
            Log.LogError("Asset bundle is null!");
            return;
        }

        Harmony.CreateAndPatchAll(typeof(Plugin).Assembly, GUID);
        Log.LogInfo($"Asset bundle: {christmasBundle.name} is not null!");
        
        
        DecorationTierBuilder tier = new DecorationTierBuilder(GUID, "Festival");
        tier.SetDisplayName("Festival");
        tier.SetIcon("Icon_UI_DecorFestive.png");
        tier.SetColor(Color.magenta);
        tier.AddReferenceCost((5, GoodsTypes.Mat_Processed_Planks));
        festiveTier = tier;
        
        CreateEffects();
        CreateBiome();
        CreateDecorations();

        EventBus.OnInitReferences.AddListener(ChangeWorkshopModel);

        Log.LogInfo($"{NAME} v{VERSION} Plugin loaded");
    }

    private void CreateEffects()
    {
        NewHookLogicType hookLogicType = CustomHookedEffectManager.NewHookLogic<DecorationPlacedHook, DecorationsOwnedHooksMonitor>(GUID, "FestiveLightsHook");
        DecorationPlacedHook.ID = hookLogicType.ID;

        festiveLights = new HookedEffectBuilder(GUID, "Festive Lights", "Icon_Modifier_Festive_Lights.png");
        festiveLights.SetPositive(true);
        festiveLights.SetLabelKey(Keys.BiomeEffect);
        festiveLights.SetDisplayName("Festive Lights");
        festiveLights.SetDescription($"The winter's chill is the time to spread the love and get the festivities going! " +
                                     "Every {0} " + festival + " decorations on your settlement increase Global Resolve by +{1}.");
        festiveLights.SetDescriptionArgs((SourceType.Hook, Eremite.Model.Effects.Hooked.TextArgType.Amount, 0), (SourceType.HookedEffect, Eremite.Model.Effects.Hooked.TextArgType.Amount, 0));
        
        // Progress: {0}/{1}. Gained: {2}
        festiveLights.SetPreviewDescriptionKey("Effect_StatePreview_Generic_Progress&Gained");
        festiveLights.SetPreviewDescriptionArgs((HookedStateTextArg.HookedStateTextSource.ProgressInt, 0),
            (HookedStateTextArg.HookedStateTextSource.HookAmountInt, 0),
            (HookedStateTextArg.HookedStateTextSource.TotalGainIntFromHooked, 0));
        
        // Expected gain: {2}. Progress: {0}/{1}
        festiveLights.SetRetroactiveDescriptionKey("Effect_RetroPreview_Generic_Progress&Gained");
        festiveLights.SetRetroactiveDescriptionArgs((HookedStateTextArg.HookedStateTextSource.ProgressInt, 0), 
            (HookedStateTextArg.HookedStateTextSource.HookAmountInt, 0), 
            (HookedStateTextArg.HookedStateTextSource.TotalGainIntFromHooked, 0));
        
        DecorationPlacedHook hook = festiveLights.NewHook<DecorationPlacedHook>();
        hook.tier = festiveTier.Model;
        hook.amount = 8;

        festiveLights.AddHookedEffect(EffectFactory.AddHookedEffect_IncreaseResolve(festiveLights, 1, ResolveEffectType.Global));
    }

    private void CreateDecorations()
    {
        DecorationTierTypes tier = festiveTier.ID;
        
        var wreath = new DecorationBuildingBuilder(GUID, "Wreath", "Icon_Deco_Wreath.png", tier);
        wreath.SetDisplayName("Wreath");
        wreath.SetDescription($"{festival}. Celebration is important for a villagers soul.");
        wreath.AddRequiredGoods((1, GoodsTypes.Packs_Pack_Of_Luxury_Goods));
        wreath.SetFootPrint(1, 1);
        wreath.SetDecorationScore(1);
        wreath.SetCustomModel(christmasBundle.LoadAsset<GameObject>("Deco_1x1_Wreath"));
        wreath.SetScaffoldingData(new BuildingConstructionAnimationData()
        {
            unconstructedPosition = new Vector3(0, -2, 0), // Move the building down 2 metres so its underground
        });
        
        var yuleTree = new DecorationBuildingBuilder(GUID, "YuleTree", "Icon_Deco_YuleTree.png", tier);
        yuleTree.SetDisplayName("Yule Tree");
        yuleTree.SetDescriptionKey(wreath.Model.description.key);
        yuleTree.AddRequiredGoods((9, GoodsTypes.Packs_Pack_Of_Luxury_Goods));
        yuleTree.SetFootPrint(3, 3);
        yuleTree.SetDecorationScore(9);
        yuleTree.SetCustomModel(christmasBundle.LoadAsset<GameObject>("Deco_3x3_YuleTree"));
        yuleTree.SetScaffoldingData(new BuildingConstructionAnimationData()
        {
            unconstructedPosition = new Vector3(0, -7, 0), // Move the building down 6 metres so its underground
            levels = 7, // 7 levels of scaffolding
        });
        
        var snowman = new DecorationBuildingBuilder(GUID, "Snowman", "Icon_Deco_Snowman.png", tier);
        snowman.SetDisplayName("Snowman");
        snowman.SetDescriptionKey(wreath.Model.description.key);
        snowman.AddRequiredGoods((4, GoodsTypes.Packs_Pack_Of_Luxury_Goods));
        snowman.SetFootPrint(2, 2);
        snowman.SetDecorationScore(4);
        snowman.SetCustomModel(christmasBundle.LoadAsset<GameObject>("Snowperson"));
    }

    private void ChangeWorkshopModel()
    {
        WorkshopModel workshopBuildingModel = BuildingTypes.Crude_Workstation.ToBuildingModel() as WorkshopModel;
        WorkshopBuildingBuilder workshop = new WorkshopBuildingBuilder(workshopBuildingModel);
        if (AssetBundleHelper.TryGet(christmasBundle, "Crude_Workshop_Jingle", out GameObject christmasWorkshop))
        {
            workshop.SetCustomModel(christmasWorkshop);
        }
        Log.LogInfo($"{NAME} v{VERSION} ChangeWorkshopModel");
    }

    private void CreateBiome()
    {
        BiomeBuilder builder = new BiomeBuilder(GUID, "JingleBellHollow");
        builder.SetDisplayName("Jingle Bell Hollow");
        builder.SetDescription("A dark and enchanting snowy forest, with an assortment of \"old world\" yuletide decorations scattered throughout, and trees with glittering tinsel and strings of lights that glow on their heavy snow-laden boughs.\n");
        
        builder.SetTownName("South Pole");
        builder.SetTownDescription("The Queen really doesn't like Santa's power.");
        
        // Icon that appears.... I actually have no idea where 
        builder.SetIcon("TinselTown_icon.png");
        
        // Season
        builder.SetSeasonDuration(SeasonTypes.Storm, 240);
        builder.SetSeasonDuration(SeasonTypes.Clearance, 120);
        builder.SetSeasonDuration(SeasonTypes.Drizzle, 120);

        // newcomers
        builder.SetNewcomerInterval(300);
        builder.AddNewcomerRace(RaceTypes.Beaver, 50);
        builder.AddNewcomerRace(RaceTypes.Harpy, 50);
        builder.AddNewcomerRace(RaceTypes.Foxes, 50);
        builder.AddNewcomerRace(RaceTypes.Frog, 25);
        builder.AddNewcomerRace(RaceTypes.Human, 100);
        builder.AddNewcomerRace(RaceTypes.Lizard, 50);
        
        // Trade
        // builder.SetTraderForceArrivalReputationPrompt("Send for trader to arrive");
        builder.SetTraderVillagerKilledByTraderText("froze to death");
        
        // World Map texture to overlay the little hexagons
        builder.SetWorldMapTexture("TWH_WorldMapTerrain.png");

        // Starting effect
        builder.AddEffect(festiveLights.EffectType);
        builder.SetDeclinedSeasonalRewardsReward(GoodsTypes.Valuable_Amber, 2);

        // Soil
        builder.SetSoilText(BiomeBuilder.SoilGrade.Medium);
        
        // Terrain
        MaskedTerrain terrain = builder.CreateTerrain<MaskedTerrain>();
        terrain.SetTerrainBaseTexture("TinselWood_Terrain1_b.png", 50, 50);
        terrain.SetTerrainOverlayTexture("TinselWood_Terrain2_b.png", 50, 50);
        terrain.SetTerrainCliffTexture("TinselWood_Terrain3_b.png", 50, 50);
        terrain.SetTerrainBlendTexture("snowyTerrainBlend.png");
        terrain.SetWaterTexture("Winter_WorldWater.png");
        terrain.SetWaterSpeed(Vector2.zero, Vector2.zero, 0);
        terrain.SetFogTexture("Winter_Fog_Bottom.png", "Winter_Fog_Top.png");

        // FX
        builder.SetRainParticles(christmasBundle, "SnowFlakeParticles");

        // Trees / natural resources
        ChristmasTree(builder);
        TinselTree(builder);
        JingleBellTree(builder);
    }

    private void ChristmasTree(BiomeBuilder builder)
    {
        NaturalResourceBuilder resourceBuilder = builder.NewNaturalResource("SnowyTree",
            horizontalTreshold: 0.2f,
            verticalTreshold: 0.3f,
            generationThreshold: 0.0f,
            minDistanceFromOrigin: 0);
        resourceBuilder.SetDisplayName("Snowy Tree");
        resourceBuilder.SetDescription("oooh cold");
        resourceBuilder.SetCharges(2);
        resourceBuilder.SetProduction(GoodsTypes.Mat_Raw_Wood, 1);
        resourceBuilder.AddExtraProduction(GoodsTypes.Food_Raw_Berries, 1, 0.5f);
        resourceBuilder.AddExtraProduction(GoodsTypes.Food_Raw_Roots, 1, 0.3f);
        resourceBuilder.AddExtraProduction(GoodsTypes.Crafting_Dye, 1, 0.2f);
        resourceBuilder.AddGatherSound("SE_tree_small_1_r_jingle.wav");
        resourceBuilder.AddGatherSound("SE_tree_small_2_r_jingle.wav");
        resourceBuilder.AddGatherSound("SE_tree_small_3_r_jingle.wav");
        resourceBuilder.AddGatherSound("SE_tree_small_4_r_jingle.wav");
        resourceBuilder.AddGatherSound("SE_tree_small_5_r_jingle.wav");
        resourceBuilder.AddFallSound("SE_tree_small_fall_1_r_jingle.wav");
        resourceBuilder.AddFallSound("SE_tree_small_fall_2_r_jingle.wav");
        resourceBuilder.AddFallSound("SE_tree_small_fall_3_r_jingle.wav");
        
        NaturalResourcePrefabBuilder treePrefab = new NaturalResourcePrefabBuilder(GUID, "SnowyTree1");
        treePrefab.CreateNewPrefab(christmasBundle, "Tinsel_Tree_1");
        resourceBuilder.AddPrefab(treePrefab);
    }
    
    private void TinselTree(BiomeBuilder builder)
    {
        NaturalResourceBuilder resourceBuilder = builder.NewNaturalResource("TinselTree",
            horizontalTreshold: 0.2f,
            verticalTreshold: 0.3f,
            generationThreshold: 0.5f,
            minDistanceFromOrigin: 0);
        resourceBuilder.SetDisplayName("Tinsel Tree");
        resourceBuilder.SetDescription("What's inside?");
        resourceBuilder.SetCharges(4);
        resourceBuilder.SetProduction(GoodsTypes.Mat_Raw_Wood, 1);
        resourceBuilder.AddExtraProduction(GoodsTypes.Food_Raw_Berries, 1, 0.5f);
        resourceBuilder.AddExtraProduction(GoodsTypes.Food_Raw_Roots, 1, 0.3f);
        resourceBuilder.AddExtraProduction(GoodsTypes.Crafting_Dye, 1, 0.2f);
        resourceBuilder.AddGatherSound("SE_tree_big_1_r_jingle.wav");
        resourceBuilder.AddGatherSound("SE_tree_big_2_r_jingle.wav");
        resourceBuilder.AddGatherSound("SE_tree_big_3_r_jingle.wav");
        resourceBuilder.AddGatherSound("SE_tree_big_4_r_jingle.wav");
        resourceBuilder.AddGatherSound("SE_tree_big_5_r_jingle.wav");
        resourceBuilder.AddFallSound("SE_tree_big_fall_1_r_jingle.wav");
        resourceBuilder.AddFallSound("SE_tree_big_fall_2_r_jingle.wav");
        resourceBuilder.AddFallSound("SE_tree_big_fall_3_r.wav");
        
        NaturalResourcePrefabBuilder treePrefab = new NaturalResourcePrefabBuilder(GUID, "TinselTree1");
        treePrefab.CreateNewPrefab(christmasBundle, "Tinsel_Tree_2");
        resourceBuilder.AddPrefab(treePrefab);
    }
    
    private void JingleBellTree(BiomeBuilder builder)
    {
        NaturalResourceBuilder resourceBuilder = builder.NewNaturalResource("JingleBellTree",
            horizontalTreshold: 0.2f,
            verticalTreshold: 0.3f,
            generationThreshold: 0.5f,
            minDistanceFromOrigin: 0);
        resourceBuilder.SetDisplayName("Jingle Bell Tree");
        resourceBuilder.SetDescription("A favourite decoration during christmas.");
        resourceBuilder.SetCharges(2);
        resourceBuilder.SetProduction(GoodsTypes.Mat_Raw_Wood, 1);
        resourceBuilder.AddExtraProduction(GoodsTypes.Metal_Copper_Ore, 1, 0.5f);
        resourceBuilder.AddExtraProduction(GoodsTypes.Needs_Incense, 1, 0.3f);
        resourceBuilder.AddExtraProduction(GoodsTypes.Crafting_Dye, 1, 0.2f);
        resourceBuilder.AddGatherSound("SE_tree_big_1_r_jingle.wav");
        resourceBuilder.AddGatherSound("SE_tree_big_2_r_jingle.wav");
        resourceBuilder.AddGatherSound("SE_tree_big_3_r_jingle.wav");
        resourceBuilder.AddGatherSound("SE_tree_big_4_r_jingle.wav");
        resourceBuilder.AddGatherSound("SE_tree_big_5_r_jingle.wav");
        resourceBuilder.AddFallSound("SE_tree_big_fall_1_r_jingle.wav");
        resourceBuilder.AddFallSound("SE_tree_big_fall_2_r_jingle.wav");
        resourceBuilder.AddFallSound("SE_tree_big_fall_3_r.wav");
        
        NaturalResourcePrefabBuilder treePrefab = new NaturalResourcePrefabBuilder(GUID, "TinselTree3");
        treePrefab.CreateNewPrefab(christmasBundle, "Tinsel_Tree_3");
        resourceBuilder.AddPrefab(treePrefab);
    }
}