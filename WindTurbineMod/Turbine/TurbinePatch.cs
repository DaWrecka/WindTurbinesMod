namespace WindTurbinesMod.WindTurbine
{
    using System;
    using System.Collections.Generic;
    using System.Collections;
    using SMLHelper.V2.Assets;
    using SMLHelper.V2.Crafting;
    using SMLHelper.V2.Handlers;
    using SMLHelper.V2.Utility;
    using Logger = QModManager.Utility.Logger;
    using UnityEngine;
#if SUBNAUTICA
    using RecipeData = SMLHelper.V2.Crafting.TechData;
    using Sprite = Atlas.Sprite;
#endif

    public class TurbinePatch : Buildable
    {
        public const string NameID = "turbine";

        private GameObject prefab;

        internal TurbinePatch() : base(NameID, "Wind Turbine", "High efficiency generator that runs on wind. Only works above water.")
        {

        }

        protected override RecipeData GetBlueprintRecipe()
        {
            return new RecipeData()
            {
                craftAmount = 1,
                Ingredients = new List<Ingredient>(new Ingredient[3]
                             {
                                 new Ingredient(QPatch.turbinePole, 1),
                                 new Ingredient(QPatch.turbineBlade, 3),
                                 new Ingredient(QPatch.turbineGenerator, 1)
                             })
            };
        }

        public override IEnumerator GetGameObjectAsync(IOut<GameObject> gameObject)
        {
            if (prefab == null)
            {
                Logger.Log(Logger.Level.Debug, "TurbinePatch.GetGameObjectAsync: start");
                AssetBundleRequest request = QPatch.bundle.LoadAssetAsync<GameObject>("turbineprefab.prefab");
                yield return request;
                prefab = request.asset as GameObject;

                //Need a tech tag for most prefabs
                var techTag = prefab.AddComponent<TechTag>();
                techTag.type = TechType;

                // Set prefab identifier, not sure what this does
                var prefabId = prefab.AddComponent<PrefabIdentifier>();
                prefabId.ClassId = ClassID;

                //A collider for the turbine pole and builder tool
                var collider = prefab.AddComponent<BoxCollider>();
                collider.size = new Vector3(2f, 17f, 2f);
                collider.center = new Vector3(0f, 9f, 0f);

                //Update all shaders
                var renderers = prefab.GetComponentsInChildren<Renderer>();
                var shader = Shader.Find("MarmosetUBER");

                foreach (var renderer in renderers)
                {
                    foreach (Material mat in renderer.materials)
                    {
                        mat.shader = shader;
                        mat.SetTexture("_Specular", mat.mainTexture);
                        if (mat.name.StartsWith("Mat5"))
                        {
                            mat.EnableKeyword("MARMO_EMISSION");
                            mat.SetVector("_EmissionColor", new Color(1f, 0.3f, 0f) * 1f);
                            mat.SetVector("_Illum_ST", new Vector4(1.0f, 1.0f, 0.0f, 0.0f));
                        }
                    }
                }

                var skyApplier = prefab.AddComponent<SkyApplier>();
                skyApplier.renderers = renderers;
                skyApplier.anchorSky = Skies.Auto;

                // Add constructable - This prefab normally isn't constructed.
                Constructable constructible = prefab.AddComponent<Constructable>();
                constructible.allowedInBase = false;
                constructible.allowedInSub = false;
                constructible.allowedOutside = true;
                constructible.allowedOnCeiling = false;
                constructible.allowedOnGround = true;
                constructible.allowedOnWall = false;
                constructible.allowedOnConstructables = true;
                constructible.techType = this.TechType;
                constructible.rotationEnabled = true;
                constructible.placeDefaultDistance = 6f;
                constructible.placeMinDistance = 0.5f;
                constructible.placeMaxDistance = 15f;
                constructible.surfaceType = VFXSurfaceTypes.metal;
                constructible.model = prefab.FindChild("Pole");
                constructible.forceUpright = true;

                prefab.FindChild("Blade Parent").AddComponent<Light>();

                var bounds = prefab.AddComponent<ConstructableBounds>();
                WindTurbine turbine = prefab.AddComponent<WindTurbine>();

                GameObject lightEmitter = new GameObject("Light Emitter");
                lightEmitter.transform.parent = prefab.transform;
                lightEmitter.transform.localPosition = new Vector3(0f, 2f, 0f);
                var light = lightEmitter.AddComponent<Light>();
                light.intensity = 1f;
                light.range = 20f;
                light.lightShadowCasterMode = LightShadowCasterMode.Everything;

                AssetBundleRequest request2 = QPatch.bundle.LoadAssetAsync<AudioClip>("turbineloop");
                yield return request2;
                turbine.soundLoop = request2.asset as AudioClip;
                CoroutineTask<GameObject> task = CraftData.GetPrefabForTechTypeAsync(TechType.SolarPanel);
                yield return task;

                PowerRelay powerRelay = task.GetResult().GetComponent<PowerRelay>();
                if (powerRelay != null)
                    turbine.relayPrefab = powerRelay;
                turbine.Activate();
                Logger.Log(Logger.Level.Debug, "TurbinePatch.GetGameObjectAsync: end");
            }

            gameObject.Set(prefab);
        }

#if SUBNAUTICA
        public override GameObject GetGameObject()
        {
            if (prefab == null)
            {
                prefab = QPatch.bundle.LoadAsset<GameObject>("turbineprefab.prefab");

                //Need a tech tag for most prefabs
                var techTag = prefab.AddComponent<TechTag>();
                techTag.type = TechType;

                // Set prefab identifier, not sure what this does
                var prefabId = prefab.AddComponent<PrefabIdentifier>();
                prefabId.ClassId = ClassID;

                //A collider for the turbine pole and builder tool
                var collider = prefab.AddComponent<BoxCollider>();
                collider.size = new Vector3(2f, 17f, 2f);
                collider.center = new Vector3(0f, 9f, 0f);

                //Update all shaders
                var renderers = prefab.GetComponentsInChildren<Renderer>();
                var shader = Shader.Find("MarmosetUBER");

                foreach (var renderer in renderers)
                {
                    foreach (Material mat in renderer.materials)
                    {
                        mat.shader = shader;
                        mat.SetTexture("_Specular", mat.mainTexture);
                        if(mat.name.StartsWith("Mat5"))
                        {
                            mat.EnableKeyword("MARMO_EMISSION");
                            mat.SetVector("_EmissionColor", new Color(1f, 0.3f, 0f) * 1f);
                            mat.SetVector("_Illum_ST", new Vector4(1.0f, 1.0f, 0.0f, 0.0f));
                        }
                    }
                }

                var skyApplier = prefab.AddComponent<SkyApplier>();
                skyApplier.renderers = renderers;
                skyApplier.anchorSky = Skies.Auto;

                // Add constructable - This prefab normally isn't constructed.
                Constructable constructible = prefab.AddComponent<Constructable>();
                constructible.allowedInBase = false;
                constructible.allowedInSub = false;
                constructible.allowedOutside = true;
                constructible.allowedOnCeiling = false;
                constructible.allowedOnGround = true;
                constructible.allowedOnWall = false;
                constructible.allowedOnConstructables = true;
                constructible.techType = this.TechType;
                constructible.rotationEnabled = true;
                constructible.placeDefaultDistance = 6f;
                constructible.placeMinDistance = 0.5f;
                constructible.placeMaxDistance = 15f;
                constructible.surfaceType = VFXSurfaceTypes.metal;
                constructible.model = prefab.FindChild("Pole");
                constructible.forceUpright = true;

                prefab.FindChild("Blade Parent").AddComponent<Light>();

                var bounds = prefab.AddComponent<ConstructableBounds>();
                WindTurbine turbine = prefab.AddComponent<WindTurbine>();

                GameObject lightEmitter = new GameObject("Light Emitter");
                lightEmitter.transform.parent = prefab.transform;
                lightEmitter.transform.localPosition = new Vector3(0f, 2f, 0f);
                var light = lightEmitter.AddComponent<Light>();
                light.intensity = 1f;
                light.range = 20f;
                light.lightShadowCasterMode = LightShadowCasterMode.Everything;

                turbine.soundLoop = QPatch.bundle.LoadAsset<AudioClip>("turbineloop");
                PowerRelay powerRelay = CraftData.GetPrefabForTechType(TechType.SolarPanel).GetComponent<PowerRelay>();
                if(powerRelay != null)
                    turbine.relayPrefab = powerRelay;
                turbine.Activate();
            }
            return prefab;
        }
#endif

        protected override Sprite GetItemSprite()
        {
            return QPatch.GetSprite("TurbineIcon");
        }

        public override TechGroup GroupForPDA => TechGroup.ExteriorModules;
        public override TechCategory CategoryForPDA => TechCategory.ExteriorModule;
        public override PDAEncyclopedia.EntryData EncyclopediaEntryData
        {
            get
            {
                PDAEncyclopedia.EntryData entry = new PDAEncyclopedia.EntryData();
                entry.key = "WindTurbine";
                entry.path = "Tech/Power";
                entry.nodes = new[] { "Tech", "Power" };
                entry.unlocked = false;
                return entry;
            }
        }
    }
}
