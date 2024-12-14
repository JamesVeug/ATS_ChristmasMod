using System;
using System.Collections;
using ATS_API.Biomes;
using ATS_API.Buildings;
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

        EventBus.OnInitReferences.AddListener(ChangeWorkshopModel);

        Log.LogInfo($"{NAME} v{VERSION} Plugin loaded");
    }

    private void ChangeWorkshopModel()
    {
        WorkshopModel workshopBuildingModel = BuildingTypes.Crude_Workstation.ToBuildingModel() as WorkshopModel;
        WorkshopBuildingBuilder workshop = new WorkshopBuildingBuilder(workshopBuildingModel);
        if (AssetBundleHelper.TryGet(christmasBundle, "ChristmasWorkshop", out GameObject christmasWorkshop))
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
        terrain.SetTerrainBlendTexture("snowyTerrainBlend.png");
        // builder.SetWaterTexture("desertWorldWater.png");

        // FX
        // if (AssetBundleHelper.TryGet(christmasBundle, "SnowFlake", out Material snowFlakeMaterial))
        // {
        //     builder.SetStormRaindropMaterial(snowFlakeMaterial);
        // }
        // builder.SetStormRaindropTextures("SnowFlake.png", "SnowFlake.png");

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
        treePrefab.CreateNewPrefab(christmasBundle, "ChristmasTree");
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
        treePrefab.CreateNewPrefab(christmasBundle, "TinselTree");
        resourceBuilder.AddPrefab(treePrefab);
    }
}