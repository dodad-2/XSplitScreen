using MonoMod.Cil;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace DoDad.XSplitScreen.Components
{
    class LemonController : UnityEngine.MonoBehaviour
    {
        #region Variables
        private const float colorSpeed = 0.2f;
        private const float colorAlpha = 0.6f;
        private const float lemonSpawnRatePerSecond = 0.5f;

        private const int maxLemons = 4;

        private static bool stayLemonFriend = false;
        private static bool openURL = true;

        private static Secret currentSecret = Secret.None;

        internal static Action onLemon;

        internal static List<HGButton> buttons = new List<HGButton>();

        internal static Sprite lemonSprite;

        internal static string token = "";

        internal static bool isLemonized = false;

        private GameObject lemonPrefab;
        private Dictionary<ColorShifter, Color> targets = new Dictionary<ColorShifter, Color>();
        private List<Lemon> lemons = new List<Lemon>();

        private Vector2 lemonFallSpeedRange = new Vector2(1, 5);
        private Vector2 lemonRockSpeedRange = new Vector2(1, 5);
        private Vector2 lemonSizeRange = new Vector2(128, 256);

        private Color[] colors;

        private Dictionary<Secret, string> secrets = new Dictionary<Secret, string>();

        private float timer = 0;
        #endregion

        #region Unity Methods
        void Awake()
        {
            colors = new Color[6];
            //lemonColors[0] = new Color(255f / 255f, 237f / 255f, 151f / 255f, 1);
            //lemonColors[1] = new Color(255f / 255f, 255f / 255f, 0f / 255f, 1);
            colors[0] = new Color(1, 0, 0, colorAlpha);
            colors[1] = new Color(1, 0, 1, colorAlpha);
            colors[2] = new Color(0, 0, 1, colorAlpha);
            colors[3] = new Color(0, 1, 1, colorAlpha);
            colors[4] = new Color(0, 1, 0, colorAlpha);
            colors[5] = new Color(1, 1, 0, colorAlpha);

            lemonSprite = Instantiate(XLibrary.Resources.GetSprite("lemon"));

            lemonPrefab = Instantiate(XLibrary.Resources.GetPrefabUI(XLibrary.Resources.UIPrefabIndex.SimpleImage));
            lemonPrefab.transform.SetParent(AssignmentScreen.overlayContainer);
            lemonPrefab.SetActive(false);
            lemonPrefab.AddComponent<Lemon>();
            var lemonImage = lemonPrefab.GetComponent<Image>();
            lemonImage.sprite = lemonSprite;
            lemonImage.raycastTarget = false;
            lemonImage.SetNativeSize();

            secrets.Add(Secret.None, "XSS_OPTION_PATREON_HOVER");
            secrets.Add(Secret.Damage, "XSS_LEMON_DAMAGE");
            secrets.Add(Secret.Speed, "XSS_LEMON_SPEED");
            secrets.Add(Secret.Health, "XSS_LEMON_HEALTH");
            secrets.Add(Secret.Shield, "XSS_LEMON_SHIELD");
        }
        void OnEnable()
        {
            if (stayLemonFriend)
            {
                onLemon.Invoke();
            }
        }
        void Update()
        {
            bool hovering = false;

            foreach (var button in buttons)
            {
                if (button.hovering)
                    hovering = true;
            }

            if (isLemonized)
            {
                timer += Time.unscaledDeltaTime;

                lemons.RemoveAll(x => x == null);

                if (timer > lemonSpawnRatePerSecond / 1f && lemons.Count < maxLemons)
                {
                    SpawnLemon();
                    timer = 0;
                }


                if (!hovering && !stayLemonFriend)
                    EndLemonization();
            }
            else
            {
                if (hovering)
                {
                    BeginLemonization();
                }
            }
        }
        void OnDestroy()
        {
            onLemon = null;
        }
        #endregion

        #region Public Methods
        public void Lemonize(Image target)
        {
            var shifter = target.GetComponent<ColorShifter>();

            if (shifter != null || target == null)
                return;

            shifter = target.gameObject.AddComponent<ColorShifter>();
            shifter.target = target;
            shifter.cycleIndex = 0;
            shifter.cycleSpeed = colorSpeed;

            targets.Add(shifter, target.color);

            shifter.colorCycles.Add(new Color[2]
                {
                        target.color,
                        target.color,
                });
        }
        #endregion

        #region Lemon
        private void SetSecretState(bool state)
        {
            if (state)
            {
                currentSecret = (Secret)UnityEngine.Random.RandomRangeInt(0, secrets.Count - 1);
                //R2API.RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients; // DISABLED BEFORE FINAL BUILD
            }
            else
            {
                currentSecret = Secret.None;
                //R2API.RecalculateStatsAPI.GetStatCoefficients -= RecalculateStatsAPI_GetStatCoefficients; // DISABLED BEFORE FINAL BUILD

            }

            //button.hoverToken = secrets[currentSecret]; // DISABLED BEFORE FINAL BUILD
        }
        /*
                private void RecalculateStatsAPI_GetStatCoefficients(RoR2.CharacterBody sender, R2API.RecalculateStatsAPI.StatHookEventArgs args)
                {
                    if (sender.teamComponent.teamIndex != RoR2.TeamIndex.Player)
                        return;

                    float mult = 0.25f;

                    switch (currentSecret)
                    {
                        case Secret.Health:
                            args.baseHealthAdd = sender.baseMaxHealth * mult;
                            break;
                        case Secret.Shield:
                            args.baseShieldAdd = sender.baseMaxShield * mult;
                            break;
                        case Secret.Speed:
                            args.baseMoveSpeedAdd = sender.baseMoveSpeed * mult;
                            break;
                        case Secret.Damage:
                            args.baseDamageAdd = sender.baseDamage * mult;
                            break;
                        case Secret.None:
                            break;
                    }
                }
        */
        public void BeginLemonization()
        {
            foreach (var pair in targets)
            {
                if (pair.Key != null)
                {
                    pair.Key.colorCycles.Clear();
                    pair.Key.colorCycles.Add(colors);
                }
            }

            isLemonized = true;

            onLemon?.Invoke();
        }
        public void EndLemonization()
        {
            foreach (var pair in targets)
            {
                if (pair.Key != null)
                {
                    pair.Key.colorCycles.Clear();
                    pair.Key.colorCycles.Add(new Color[]
                    {
                        pair.Value,
                        pair.Value,
                    });
                }
            }

            isLemonized = false;

            onLemon?.Invoke();
        }
        private void SpawnLemon()
        {
            var lemon = Instantiate(lemonPrefab).GetComponent<Lemon>();
            lemon.transform.SetParent(AssignmentScreen.overlayContainer);
            lemon.fallSpeed = UnityEngine.Random.Range(lemonFallSpeedRange.x, lemonFallSpeedRange.y);
            lemon.rockSpeed = UnityEngine.Random.Range(lemonRockSpeedRange.x, lemonRockSpeedRange.y);
            lemon.size = UnityEngine.Random.Range(lemonSizeRange.x, lemonSizeRange.y);
            lemon.BeLemon();
            lemons.Add(lemon);
        }
        #endregion

        #region Hooks
        private void SetHooks(Secret secret, bool state)
        {
        }
        #endregion

        #region Enum
        private enum Secret
        {
            None = -1,
            Damage = 0,
            Speed = 1,
            Health = 2,
            Shield = 3,
        }
        #endregion
    }
}
