using System;
using System.Collections;
using Logger = QModManager.Utility.Logger;
using UnityEngine;


namespace WindTurbinesMod.WindTurbine 
{
    public class WindTurbine : HandTarget, IHandTarget 
    {
        Constructable constructable;
        TurbineHealth health;

        public PowerSource powerSource;
        internal PowerRelay relayPrefab;

        [AssertNotNull]
        public PowerRelay relay;

        public TurbineSpin spin;

        public AudioClip soundLoop;
        public AudioSource loopSource;

        public float timeEastereggEnd = 0f;

        // Each Update(), generated energy is added to profileEnergy and DeltaTime is added to profileTime. When profileTime >= 1,
        // generationRate is set to (profileEnergy / profileTime) and both are reset to 0.
        private float profileTime = 0f;
        private float profileEnergy = 0f;
        private float generationRate = 0f;

        bool NeedsMaintenance
        {
            get
            {
                if (!QPatch.config.TurbineTakesDamage) return false;
                return health.health < 10f;
            }
        }

        public void Activate()
        {
            spin = gameObject.FindChild("Blade Parent").EnsureComponent<TurbineSpin>();
            powerSource = gameObject.EnsureComponent<PowerSource>();
            powerSource.maxPower = QPatch.config.MaxPower;
            relay = gameObject.EnsureComponent<PowerRelay>();
            relay.internalPowerSource = powerSource;
            relay.dontConnectToRelays = false;
            relay.maxOutboundDistance = 50;
            //relay.powerSystemPreviewPrefab = Resources.Load<GameObject>("Base/Ghosts/PowerSystemPreview.prefab");

            if (relayPrefab != null)
            {
                PowerFX yourPowerFX = gameObject.AddComponent<PowerFX>();

                yourPowerFX.vfxPrefab = relayPrefab.powerFX.vfxPrefab;
                yourPowerFX.attachPoint = gameObject.transform;
                relay.powerFX = yourPowerFX;
            }

#if SUBNAUTICA
            Resources.UnloadAsset(powerRelay);
#endif
            relay.UpdateConnection();

            if(QPatch.config.TurbineMakesNoise) SetupAudio();
            Logger.Log(Logger.Level.Debug, $"WindTurbine.Activate: end");
        }

        void Start()
        {
            health = gameObject.EnsureComponent<TurbineHealth>(); //gameObject.AddComponent<TurbineHealth>();
            health.SetData();
            health.health = 200f;
#if SUBNAUTICA
            PDAEncyclopedia.Add("WindTurbine", true);
#elif BELOWZERO
            PDAEncyclopedia.Add("WindTurbine", true, false);
#endif
        }

        internal void SetupAudio()
        {
            loopSource = spin.gameObject.AddComponent<AudioSource>();
            loopSource.clip = soundLoop;
            loopSource.loop = true;
            if (!loopSource.isPlaying) loopSource.Play();
            loopSource.minDistance = 4f;
            loopSource.maxDistance = 35f;
            loopSource.spatialBlend = 1f;
        }

        private float GetOceanLevel()
        {
#if SUBNAUTICA
            return Ocean.main.GetOceanLevel();
#elif BELOWZERO
            return Ocean.GetOceanLevel();
#endif
        }

        private float GetDepthScalar()
        {
            return Mathf.Clamp((spin.transform.position.y + GetOceanLevel()) / 35f, 0f, 1f);
        }

        private float GetRechargeScalar()
        {
            return this.GetDepthScalar(); //Kind of redundant to have this, for now.
        }

        private void Update()
        {
            if(health == null)
                health = GetComponent<TurbineHealth>();
            if (constructable == null)
                constructable = gameObject.GetComponent<Constructable>();

            if (NeedsMaintenance)
            {
                this.spin.spinSpeed = 0f;
                if (loopSource != null)
                    loopSource.Stop();
            }
            else if (constructable.constructed && Time.time > timeEastereggEnd)
            {
                float deltaTime = Time.deltaTime;
                if (!loopSource.isPlaying) loopSource.Play();
                float amount = this.GetRechargeScalar() * DayNightCycle.main.deltaTime * 40f * WindyMultiplier(new Vector2(transform.position.x, transform.position.z));
                float powerGen = QPatch.config.PowerProductionScale * amount / 4f;
                this.profileEnergy += powerGen;
                this.profileTime += deltaTime;
                if (profileTime >= 1)
                {
                    this.generationRate = this.profileEnergy / this.profileTime;
                    this.profileEnergy = 0f;
                    this.profileTime = 0;
                }
                this.relay.ModifyPower(powerGen, out float num);
                float damage = 200f / QPatch.config.SecondsUntilNeedMaintenance * deltaTime;
                if (QPatch.config.TurbineTakesDamage && health.health - damage > 0f)
                    health.TakeDamage(num / 15f);
                this.spin.spinSpeed = amount * 10f;
                this.loopSource.volume = Mathf.Clamp(amount, 0.6f, 1f);
            }
        }

        public static float WindyMultiplier(Vector2 position)
        {
            if (QPatch.config.PositionInfluencesPower)
            {
                return 1f + (Mathf.PerlinNoise(position.x * 0.01f, position.y * 0.01f) - 0.5f) * 0.5f;
            }
            else
            {
                return 1f;
            }
        }

#if SUBNAUTICA
        private void SetInteractText(string text1, bool translate1, string text2 = "", bool translate2 = false, HandReticle.Hand hand = HandReticle.Hand.None)
        {
            HandReticle.main.SetInteractText(text1, text2, translate1, translate2, hand);
#elif BELOWZERO
        private void SetInteractText(string text1, bool translate1, string text2 = "", bool translate2 = false, GameInput.Button hand = GameInput.Button.None)
        {
            HandReticle.main.SetText(HandReticle.TextType.Hand, text1, translate1);
            if (!String.IsNullOrEmpty(text2))
            {
                HandReticle.main.SetText(HandReticle.TextType.HandSubscript, text2, translate2);
            }
#endif
        }

        public void OnHandHover(GUIHand hand)
        {
            if(constructable == null) constructable = gameObject.GetComponent<Constructable>();
            if (constructable.constructed)
            {
                if(spin.transform.position.y > GetOceanLevel() + 1f)
                {
                    string text1 = "";
                    string text2 = "";
                    float displayTime = 0f;
                    if(NeedsMaintenance)
                    {
                        text1 = "Wind Turbine: " + Mathf.RoundToInt(this.GetRechargeScalar() * 100f * WindyMultiplier(new Vector3(transform.position.x, transform.position.z))) + "% efficiency, " + Mathf.RoundToInt(this.powerSource.GetPower()).ToString() + "/" + Mathf.RoundToInt(this.powerSource.GetMaxPower()) + " power";
                        text2 = "Needs maintenance (use repair tool)";
                        displayTime = 1.5f;
                    }
                    else
                    {
                        text1 = "Wind Turbine: " + Mathf.RoundToInt(this.GetRechargeScalar() * 100f * WindyMultiplier(new Vector3(transform.position.x, transform.position.z))) + "% efficiency, " + Mathf.RoundToInt(this.powerSource.GetPower()).ToString() + "/" + Mathf.RoundToInt(this.powerSource.GetMaxPower()) + " power";
                        displayTime = 1f;
                        text2 = $"Generation: {this.generationRate.ToString("0.00")}/s";
                    }
                    SetInteractText(text1, false, text2, false);
                    HandReticle.main.SetIcon(HandReticle.IconType.Info, displayTime);
                }
                else
                {
                    SetInteractText("Wind Turbine: 0% efficiency", false, "Notice: Blades are submerged, please relocate", false);
                    HandReticle.main.SetIcon(HandReticle.IconType.HandDeny, 1f);
                }
            }
        }

        public void OnHandClick(GUIHand hand)
        {
            spin.spinSpeed = 1000f;
            timeEastereggEnd = Time.time + 1f;
        }
    }

}