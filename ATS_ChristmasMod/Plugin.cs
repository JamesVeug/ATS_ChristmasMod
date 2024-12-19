using System;
using System.Collections;
using ATS_API.Biomes;
using ATS_API.Buildings;
using ATS_API.Decorations;
using ATS_API.Helpers;
using ATS_API.NaturalResource;
using BepInEx;
using BepInEx.Logging;
using Eremite;
using Eremite.Buildings;
using Eremite.Controller;
using Eremite.Model;
using Eremite.Services;
using Eremite.View.HUD;
using Eremite.View.HUD.Monitors;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChristmasMod;

[HarmonyPatch]
[BepInPlugin(GUID, NAME, VERSION)]
public class Plugin : BaseUnityPlugin
{
    private const string GUID = "ATS_ChristmasMod";
    private const string NAME = "ATS_ChristmasMod";
    private const string VERSION = "1.0.1";

    public static Plugin Instance;
    public static ManualLogSource Log;

    private static AssetBundle christmasBundle;

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
        CreateExampleBiome();
        CreateDecorations();

        EventBus.OnInitReferences.AddListener(ChangeWorkshopModel);

        Log.LogInfo($"{NAME} v{VERSION} Plugin loaded");
    }

    private void CreateDecorations()
    {
        DecorationTierBuilder tier = new DecorationTierBuilder(GUID, "Festival");
        tier.SetDisplayName("Festival");
        tier.SetIcon("Icon_Workshop_Upgrade_Elf.png");
        tier.SetColor(Color.magenta);
        tier.AddReferenceCost((5, GoodsTypes.Mat_Processed_Planks));
        
        string hexColor = ColorUtility.ToHtmlStringRGB(Color.magenta);
        
        var wreath = new DecorationBuildingBuilder(GUID, "Wreath", "Icon_Deco_Wreath.png", tier.ID);
        wreath.SetDisplayName("Wreath");
        wreath.SetDescription(string.Format("<color=#{0}>Festival.</color> Celebration is important for a villagers soul. Decorations are used to level up Hearths.", hexColor));
        wreath.SetLabel("Decorations");
        wreath.AddRequiredGoods((1, GoodsTypes.Mat_Processed_Planks));
        wreath.AddRequiredGoods((1, GoodsTypes.Mat_Processed_Fabric));
        wreath.AddRequiredGoods((1, GoodsTypes.Mat_Processed_Bricks));
        wreath.SetFootPrint(1, 1);
        wreath.SetDecorationScore(1);
        wreath.SetCustomModel(christmasBundle.LoadAsset<GameObject>("Deco_1x1_Wreath"));
        
        var yuleTree = new DecorationBuildingBuilder(GUID, "YuleTree", "Icon_Deco_YuleTree.png", tier.ID);
        yuleTree.SetDisplayName("Yule Tree");
        yuleTree.SetDescriptionKey(wreath.Model.description.key);
        yuleTree.SetLabel("Decorations");
        yuleTree.AddRequiredGoods((9, GoodsTypes.Mat_Processed_Planks));
        yuleTree.AddRequiredGoods((9, GoodsTypes.Mat_Processed_Fabric));
        yuleTree.AddRequiredGoods((9, GoodsTypes.Mat_Processed_Bricks));
        yuleTree.SetFootPrint(3, 3);
        yuleTree.SetDecorationScore(9);
        yuleTree.SetCustomModel(christmasBundle.LoadAsset<GameObject>("Deco_3x3_YuleTree"));
        
        var snowman = new DecorationBuildingBuilder(GUID, "Snowman", "Icon_Deco_Snowman.png", tier.ID);
        snowman.SetDisplayName("Snowman");
        snowman.SetDescriptionKey(wreath.Model.description.key);
        snowman.SetLabel("Decorations");
        snowman.AddRequiredGoods((2, GoodsTypes.Mat_Processed_Planks));
        snowman.AddRequiredGoods((2, GoodsTypes.Mat_Processed_Fabric));
        snowman.AddRequiredGoods((2, GoodsTypes.Mat_Processed_Bricks));
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

    private void CreateExampleBiome()
    {
        BiomeBuilder builder = new BiomeBuilder(GUID, "Tinselwood Hollow");
        builder.SetDisplayName("Tinselwood Hollow");
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
        // builder.AddEffect(DiamondHunterBuilder.EffectType);
        builder.SetDeclinedSeasonalRewardsReward(GoodsTypes.Valuable_Amber, 2);

        // Soil
        builder.SetSoilText(BiomeBuilder.SoilGrade.None);
        
        // Terrain
        MaskedTerrain terrain = builder.CreateTerrain<MaskedTerrain>();
        terrain.SetTerrainBaseTexture("TinselWood_Terrain1.png", 50, 50);
        terrain.SetTerrainOverlayTexture("TinselWood_Terrain4.png", 50, 50);
        terrain.SetTerrainCliffTexture("TinselWood_Terrain7.png", 50, 50);
        // builder.SetWaterTexture("desertWorldWater.png");

        // FX
        builder.SetRainParticles(christmasBundle, "SnowFlakeParticles");

        // Trees / natural resources
        ChristmasTree(builder);
        TinselTree(builder);
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
}