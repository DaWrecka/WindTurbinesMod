﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using SMLHelper.V2.Handlers;
using SMLHelper.V2.Utility;
using SMLHelper.V2.Crafting;
using QModManager.API.ModLoading;
#if SUBNAUTICA
using RecipeData = SMLHelper.V2.Crafting.TechData;
using Sprite = Atlas.Sprite;
#endif

namespace WindTurbinesMod
{
    [QModCore]
    public class QPatch
    {
        public const string modName = "WindTurbinesMod";

        public static TechType turbineBlade;
        public static TechType turbineGenerator;
        public static TechType turbinePole;
        public static AssetBundle bundle;
        public static WindTurbineConfig config;
        public static string mainDirectory;
        public static string assetsFolder;

        static void LoadAssetBundle()
        {
            bundle = AssetBundle.LoadFromFile(Path.Combine(assetsFolder, "windturbineassets"));
        }

        static void LoadConfig()
        {
            string configPath = Path.Combine(mainDirectory, "config.txt");
            if (File.Exists(configPath))
            {
                using(StreamReader sr = new StreamReader(configPath))
                {
                    string json = sr.ReadToEnd();
                    config = JsonUtility.FromJson<WindTurbineConfig>(json);
                }
            }
        }

        [QModPatch]
        public static void Patch()
        {
            mainDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            assetsFolder = Path.Combine(mainDirectory, "Assets");
            LoadAssetBundle();
            LoadConfig();
            //patch crafting recipes
            //is there a more efficient way of doing this?
            turbineBlade = TechTypeHandler.AddTechType("TurbineBlade", "Turbine Blade", "Necessary component in constructing a wind turbine. Large and lightweight for maximum aerodynamics.", GetSprite("TurbineBlade"));
            turbineGenerator = TechTypeHandler.AddTechType("TurbineGenerator", "Turbine Generator", "Necessary component in constructing a wind turbine. Converts mechanical energy of the blades into usable electricity.", GetSprite("Generator"));
            turbinePole = TechTypeHandler.AddTechType("TurbinePole", "Turbine Base", "Necessary component in constructing a wind turbine. Supports the large structure.", GetSprite("TurbinePole"));

            var turbine = new WindTurbine.TurbinePatch();
            turbine.Patch();
        }

        [Obsolete]
        private static readonly MethodInfo addJsonPropertyInfo = typeof(CraftDataHandler).GetMethod("AddJsonProperty", BindingFlags.NonPublic | BindingFlags.Static);

        [Obsolete]
        public static void AddJsonProperty(TechType techType, string key, JsonValue newValue)
        {
            addJsonPropertyInfo.Invoke(null, new object[] { techType, key, newValue });
        }
        public static void SetItemSize(TechType techType, int width, int height)
        {
            AddJsonProperty(techType, "itemSize", new JsonValue
                {
                    {
                        TechData.propertyX,
                        new JsonValue(width)
                    },
                    {
                        TechData.propertyY,
                        new JsonValue(height)
                    }
                }
            );
        }

        [QModPostPatch]
        public void PostPatch()
        {
            //patch crafting recipes
            //is there a more efficient way of doing this?
            var techDataBlade = new RecipeData()
            {
                craftAmount = 3,
                Ingredients = new List<Ingredient>()
                {
                  new Ingredient(TechType.Titanium, 3)
                }
            };
            CraftDataHandler.SetTechData(turbineBlade, techDataBlade);
            CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, turbineBlade, new string[] { "Resources", "Electronics" });
            KnownTechHandler.SetAnalysisTechEntry(TechType.WiringKit, new TechType[] { turbineBlade });
            CraftDataHandler.SetItemSize(turbineBlade, new Vector2int(2, 1));
            //SetItemSize(turbineBlade, 2, 1);

            var techDataGen = new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>()
                {
                  new Ingredient(TechType.WiringKit, 1),
                  new Ingredient(TechType.PowerCell, 1),
                  new Ingredient(TechType.Lubricant, 1)
                }
            };
            CraftDataHandler.SetTechData(turbineGenerator, techDataGen);
            KnownTechHandler.SetAnalysisTechEntry(TechType.WiringKit, new TechType[] { turbineGenerator });
            CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, turbineGenerator, new string[] { "Resources", "Electronics" } );
            CraftDataHandler.SetItemSize(turbineGenerator, new Vector2int(2, 2));
            //SetItemSize(turbineBlade, 2, 2);

            var techDataPole = new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>()
                {
                  new Ingredient(TechType.Titanium, 4)
                }
            };
            CraftDataHandler.SetTechData(turbinePole, techDataPole);
            KnownTechHandler.SetAnalysisTechEntry(TechType.WiringKit, new TechType[] { turbinePole });
            CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, turbinePole, new string[] { "Resources", "Electronics" } );
            CraftDataHandler.SetItemSize(turbinePole, new Vector2int(1, 2));
            //SetItemSize(turbineBlade, 1, 2);

            //Add the databank entry.
            LanguageHandler.SetLanguageLine("Ency_WindTurbine", "Wind Turbine");
            LanguageHandler.SetLanguageLine("EncyDesc_WindTurbine", string.Format("A large generator suspended by 17.5 meter tall pole. The lightweight blades are rotated by the planet's strong air currents and efficiently converts the force into electrical energy. The generator contains a large internal battery that can hold up to {0} units of power. Unlike solar panels, these operate at roughly the same efficiency throughout the day. Orientation does not appear to affect power output. However certain places seem to simply have more wind than others. Power output also increases with altitude.", config.MaxPower));

            //This just isn't working for now. Maybe another update?
            //var windTool = new WindTool.WindToolPatch();
            //windTool.Patch();
        }

        public static Sprite GetSprite(string name)
        {
            return ImageUtils.LoadSpriteFromFile(@"./QMods/" + modName + "/Assets/" + name + ".png");
        }
    }
}