using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Dodad.XSplitscreen.Components
{
	public class CreditsController : MonoBehaviour
	{
		public static bool ShowCredits;
		private static float Speed = 50f;

		private Dictionary<string, List<string>> _contributors = new();
		private Dictionary<string, List<Color>> _colors = new();
		private List<CreditRoller> _rollers = new();
		private GameObject _rollerPrefab;
		private CanvasGroup _group;
		private readonly List<Color> _defaultColors = new List<Color>()
		{
					new Color(1,1,1,1),
					new Color(0.8f, 0.8f, 0.8f, 1f)
		};

		private string _category;
		private int _index;
		private float _spawnTimer;
		private float _spawnDelay = .9f;
		private float _maxY;

		private void Awake()
		{
			InitializeContributors();
			InitializeRollerPrefab();
			InitializeReferences();
			InitializeColors();
			NextRoller();
		}

		private void Start()
		{
			_maxY = transform.parent.GetComponent<RectTransform>().rect.height / 2f;
		}

		private void Update()
		{
			if (SplitscreenMenuController.MainInput == null)
				return;

			if (SplitscreenMenuController.MainInput.MouseLeft || SplitscreenMenuController.MainInput.East || SplitscreenMenuController.MainInput.South)
				ShowCredits = false;

			if (ShowCredits)
			{
				_group.alpha = Mathf.Lerp(_group.alpha, 1, Time.deltaTime * 20f);

				_spawnTimer -= Time.deltaTime;

				if (_spawnTimer <= 0)
				{
					_spawnTimer = _spawnDelay;

					SpawnRoller();
					NextRoller();
				}

				Roll();
			}
			else
			{
				_group.alpha = Mathf.Lerp(_group.alpha, 0, Time.deltaTime * 20f);
			}
		}

		private void InitializeReferences()
		{
			_group = GetComponent<CanvasGroup>();
		}

		private void InitializeRollerPrefab()
		{
			_rollerPrefab = Instantiate(Plugin.Resources.LoadAsset<GameObject>("RollerPrefab.prefab"));
			_rollerPrefab.gameObject.AddComponent<CreditRoller>();
			_rollerPrefab.transform.SetParent(transform);
			_rollerPrefab.transform.localScale = Vector3.one;
		}

		private void Roll()
		{
			_rollers.RemoveAll(x => x == null);

			var toDestroy = new List<GameObject>();

			foreach (var roll in _rollers)
			{
				if (roll == null)
					continue;

				var rect = roll.Rect;
				var currentPos = rect.anchoredPosition;

				if (currentPos.y >= _maxY - 0.01f)
				{
					toDestroy.Add(rect.gameObject);
					continue;
				}

				var targetPos = new Vector2(currentPos.x, _maxY);
				rect.anchoredPosition = Vector2.MoveTowards(currentPos, targetPos, Time.deltaTime * Speed);
			}

			foreach (var obj in toDestroy)
			{
				if(obj != null)
					Destroy(obj);
			}
		}

		private void SpawnRoller()
		{
			if (_rollerPrefab == null)
				return;

			var rect = transform.GetComponent<RectTransform>();

			var roller = Instantiate(_rollerPrefab).GetComponent<CreditRoller>();
			roller.transform.SetParent(transform);
			roller.transform.localScale = Vector3.one;
			roller.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
			roller.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -(_maxY));
			
			roller.Title = _category;
			roller.Content = _contributors[_category][_index];

			if (_colors.TryGetValue(_category, out var colors))
				roller.SetColors(colors);
			else
				roller.SetColors(_defaultColors);

			_rollers.Add(roller);

			roller.gameObject.SetActive(true);
		}

		private void NextRoller()
		{
			if(_category == null)
			{
				_category = _contributors.Keys.First();
			}
			else
			{
				_index++;

				if(_index == _contributors[_category].Count)
				{
					_index = 0;

					var keys = _contributors.Keys.ToArray();

					for (int e = 0; e < keys.Length; e++)
					{
						if (keys[e] == _category)
						{
							if (e + 1 == keys.Length)
							{
								_category = keys[0];
								_spawnTimer = 20f;
							}
							else
								_category = keys[e + 1];

							break;
						}
					}
				}
			}
		}

		private void InitializeContributors()
		{
			AddContributor("Creator", "dodad");
			AddContributor("Art", "Claymaver (https://linktr.ee/claymaver)");
			AddContributor("Art", "dodad");
			AddContributor("Programming", "Narl");
			AddContributor("Programming", "AncientHeroX");
			AddContributor("Programming", "dodad");
			AddContributor("Technical Support", "iDeathHD");
			AddContributor("Technical Support", "Many users in the Risk of Rain 2 Modding Discord");
			AddContributor("Translations", "LuizFilipeRN");
			AddContributor("Translations", "technochips");
			AddContributor("Special Support", "KubeRoot");
			AddContributor("Special Support", "Bloodgem64");
			AddContributor("Donators", "NickmitdemKopf");
			AddContributor("Donators", "BluJay");
			AddContributor("Donators", "yabuggin");
			AddContributor("Donators", "bungee4kiwi");
			AddContributor("Testers", "Seff");
			AddContributor("Testers", "Baby");
			AddContributor("Testers", "kwiki");
			AddContributor("Testers", "MemexJota");
			AddContributor("Testers", "PKPotential");
			AddContributor("Testers", "ThatBlueRacc");
			AddContributor("Testers", "Kaiben");
			AddContributor("Testers", "The*real_douchcanoe*");
			AddContributor("Testers", "KCaptainawesome");
			AddContributor("Testers", "O_Linny\\_/O");
			AddContributor("Testers", "noahwubs");
			AddContributor("Testers", "Hansei");
			AddContributor("Testers", "Aristhma");
			AddContributor("Testers", "Re-Class May");
			AddContributor("Testers", "AdaM");
			AddContributor("Testers", "Engi");
			AddContributor("Testers", "Coked Out Monkey");
			AddContributor("Testers", "Wumble");
			AddContributor("Testers", "God of Heck");
			AddContributor("Testers", "Wiism");
			AddContributor("Testers", "instasnipe");
			AddContributor("Testers", "Mo");
			AddContributor("Testers", "billredm");
			AddContributor("Testers", "TebZ");
			AddContributor("Testers", "TestMario");
			AddContributor("Testers", "combatcommando");
			AddContributor("Testers", "Cpl.Xanius Norarte");
			AddContributor("Testers", "fr4n");
			AddContributor("Testers", "IsaacShadowShade");
			AddContributor("Testers", "vennece");
			AddContributor("Testers", "Cerealbowl");
			AddContributor("Testers", "ULTIMATE LOOP");
			AddContributor("Testers", "Mufasawasa");
			AddContributor("Discord Enthusian", "Seraph14162");
			AddContributor("Discord Enthusian", "The Rat Bastard");
			AddContributor("Discord Enthusian", "solewalker");
			AddContributor("Discord Enthusian", "tunapocalypse7");
			AddContributor("Discord Enthusian", "minipants");
			AddContributor("Discord Enthusian", "KingBalda");
			AddContributor("Discord Enthusian", "Cactris");
			AddContributor("Discord Enthusian", "The Snoc guy");
			AddContributor("Discord Enthusian", "Engineer664");
			AddContributor("Discord Enthusian", "Saik");
			AddContributor("Discord Enthusian", "manimdead");
			AddContributor("Discord Enthusian", "Turts");
			AddContributor("Discord Enthusian", "foo4u2");
			AddContributor("Discord Enthusian", "JazzHandsFan");
			AddContributor("Discord Enthusian", "NativeBananas");
			AddContributor("Discord Enthusian", "Eternal");
			AddContributor("Discord Enthusian", "FRED XVI");
			AddContributor("Discord Enthusian", "KrypticSound");
			AddContributor("Discord Enthusian", "Xana");
			AddContributor("Discord Enthusian", "SamuJD");
			AddContributor("Discord Enthusian", "Tal");
			AddContributor("Discord Enthusian", "Dp258");
			AddContributor("Discord Enthusian", "Khemed");
			AddContributor("Discord Enthusian", "Daniel");
			AddContributor("Discord Enthusian", "nova");
			AddContributor("Discord Enthusian", "Sentient FBI");
			AddContributor("Discord Enthusian", "Quakeinboots");
			AddContributor("Discord Enthusian", "Andersonjsc01");
			AddContributor("Discord Enthusian", "soren5943");
			AddContributor("Discord Enthusian", "Wooden_Whale");
			AddContributor("Discord Enthusian", "Five");
			AddContributor("Discord Enthusian", "Xavier Montagne");
			AddContributor("Discord Enthusian", "Hidden");
			AddContributor("Discord Enthusian", "Bell");
			AddContributor("Discord Enthusian", "SlinShady");
			AddContributor("Discord Enthusian", "Hunabaktu");
			AddContributor("Discord Enthusian", "Maie");
			AddContributor("Discord Enthusian", "Dofne");
			AddContributor("Discord Enthusian", "Elvenide");
			AddContributor("Discord Enthusian", "tinmar");
			AddContributor("Discord Enthusian", "TeriosKey");
			AddContributor("Discord Enthusian", "Friday");
			AddContributor("Discord Enthusian", "Spiffo");
			AddContributor("Discord Enthusian", "Chiefkeefafghanistan");
			AddContributor("Discord Enthusian", "Wheat");
			AddContributor("Discord Enthusian", "jnl_eu");
			AddContributor("Discord Enthusian", "ButterGirl (Joyful)");
			AddContributor("Discord Enthusian", "Warpedlgr");
			AddContributor("Discord Enthusian", "Depa!");
			AddContributor("Discord Enthusian", "JLotts");
			AddContributor("Discord Enthusian", "honeybunny39");
			AddContributor("Discord Enthusian", "Harry");
			AddContributor("Discord Enthusian", "MaJester");
			AddContributor("Discord Enthusian", "2020 not going well");
			AddContributor("Discord Enthusian", "TatoTheWave");
			AddContributor("Discord Enthusian", "Roomi");
			AddContributor("Discord Enthusian", "Arqueiro");
			AddContributor("Discord Enthusian", "centrion25");
			AddContributor("Discord Enthusian", "Gamingtime");
			AddContributor("Discord Enthusian", "JaksusTheDragon");
			AddContributor("Discord Enthusian", "RideOrDie");
			AddContributor("Discord Enthusian", "Deftpaws");
			AddContributor("Discord Enthusian", "squidward");
			AddContributor("Discord Enthusian", "DeDe");
			AddContributor("Discord Enthusian", "Send Help");
			AddContributor("Discord Enthusian", "DeliShoes");
			AddContributor("Discord Enthusian", "LoganTheHuge");
			AddContributor("Discord Enthusian", "Bigge");
			AddContributor("Discord Enthusian", "LisaMawhile");
			AddContributor("Discord Enthusian", "spaguesty");
			AddContributor("Discord Enthusian", "Marduk");
			AddContributor("Discord Enthusian", "Matt");
			AddContributor("Discord Enthusian", "Benjar07");
			AddContributor("Discord Enthusian", "Skibblah");
			AddContributor("Discord Enthusian", "CloudKave");
			AddContributor("Discord Enthusian", "sorosoro");
			AddContributor("Discord Enthusian", "novakidflash");
			AddContributor("Discord Enthusian", "petri");
			AddContributor("Discord Enthusian", "Darkvud");
			AddContributor("Discord Enthusian", "danistupid832");
			AddContributor("Discord Enthusian", "LimeCell");
			AddContributor("Discord Enthusian", "Anako-Vokun");
			AddContributor("Discord Enthusian", "Epic-Azfar");
			AddContributor("Discord Enthusian", "Mechanist");
			AddContributor("Discord Enthusian", "ikidebomber");
			AddContributor("Discord Enthusian", "acanthi");
			AddContributor("Discord Enthusian", "TheHumanBean7");
			AddContributor("Discord Enthusian", "firstername");
			AddContributor("Discord Enthusian", "Xicromix");
			AddContributor("Discord Enthusian", "zaid123");
			AddContributor("Discord Enthusian", "printerjoe");
			AddContributor("Discord Enthusian", "Ava<3");
			AddContributor("Discord Enthusian", "Doppio");
			AddContributor("Discord Enthusian", "Jes Right#2222");
			AddContributor("Discord Enthusian", "chowiekomba");
			AddContributor("Discord Enthusian", "Napolocreed");
			AddContributor("Discord Enthusian", "Barrel7error");
			AddContributor("Discord Enthusian", "TebZ");
			AddContributor("Discord Enthusian", "Darquirks");
			AddContributor("Discord Enthusian", "peetsa");
			AddContributor("Discord Enthusian", "Mo");
			AddContributor("Discord Enthusian", "KING");
			AddContributor("Discord Enthusian", "skeleton man");
			AddContributor("Discord Enthusian", "Kaiben");
			AddContributor("Discord Enthusian", "NotGoodToast");
			AddContributor("Discord Enthusian", "Nito");
			AddContributor("Discord Enthusian", "AKA");
			AddContributor("Discord Enthusian", "MaxAnthony125");
			AddContributor("Discord Enthusian", "eggboiboi");
			AddContributor("Discord Enthusian", "RoXas");
			AddContributor("Discord Enthusian", "E.N.D.O.K.");
			AddContributor("Special Thanks", "All the supportive Risk of Rain 2 splitscreen enjoyers - thank you!");

			var keys = _contributors.Keys.ToArray();

			foreach (var key in keys)
				_contributors[key] = _contributors[key].OrderBy(x => x).ToList();
		}

		private void InitializeColors()
		{
			_colors.Add("Creator", new List<Color>()
			{
				new Color(1f, 0.97f, 0.36f, 1f),
				new Color(0.92f, 0.77f, 0.16f, 1f),
				new Color(0.72f, 0.57f, 0.00f, 1f),
				new Color(0.72f, 0.57f, 0.00f, 1f),
			});
			_colors.Add("Art", new List<Color>()
			{
				new Color(0.8f, 0.55f, 0.91f, 1f),
				new Color(0.6f, 0.35f, 0.71f, 1f),
				new Color(0.4f, 0.15f, 0.51f, 1f),
				new Color(0.4f, 0.15f, 0.51f, 1f),
			});
			_colors.Add("Programming", new List<Color>()
			{
				new Color(0.8f, 0.55f, 0.91f, 1f),
				new Color(0.6f, 0.35f, 0.71f, 1f),
				new Color(0.4f, 0.15f, 0.51f, 1f),
				new Color(0.4f, 0.15f, 0.51f, 1f),
			});
			_colors.Add("Technical Support", new List<Color>()
			{
				new Color(0.8f, 0.55f, 0.91f, 1f),
				new Color(0.6f, 0.35f, 0.71f, 1f),
				new Color(0.4f, 0.15f, 0.51f, 1f),
				new Color(0.4f, 0.15f, 0.51f, 1f),
			});
			_colors.Add("Translations", new List<Color>()
			{
				new Color(0.8f, 0.55f, 0.91f, 1f),
				new Color(0.6f, 0.35f, 0.71f, 1f),
				new Color(0.4f, 0.15f, 0.51f, 1f),
				new Color(0.4f, 0.15f, 0.51f, 1f),
			});
			_colors.Add("Special Support", new List<Color>()
			{
				new Color(0.8f, 0.55f, 0.91f, 1f),
				new Color(0.6f, 0.35f, 0.71f, 1f),
				new Color(0.4f, 0.15f, 0.51f, 1f),
				new Color(0.4f, 0.15f, 0.51f, 1f),
			});
			_colors.Add("Donators", new List<Color>()
			{
				new Color(1f, 0.38f, 0.59f, 1f),
				new Color(0.87f, 0.18f, 0.39f, 1f),
				new Color(0.67f, 0.0f, 0.19f, 1f),
				new Color(0.67f, 0.0f, 0.19f, 1f),
			});
			_colors.Add("Testers", new List<Color>()
			{
				new Color(0.49f, 0.78f, 1f, 1f),
				new Color(0.29f, 0.58f, 0.85f, 1f),
				new Color(0.09f, 0.38f, 0.65f, 1f),
				new Color(0.09f, 0.38f, 0.65f, 1f),
			});
			_colors.Add("Discord Enthusian", new List<Color>()
			{
				new Color(0.48f, 0.99f, 0.65f, 1f),
				new Color(0.28f, 0.79f, 0.45f, 1f),
				new Color(0.08f, 0.59f, 0.25f, 1f),
				new Color(0.08f, 0.59f, 0.25f, 1f),
			});
			_colors.Add("Special Thanks", new List<Color>()
			{
				new Color(1f, 0f, 0f, 1f),     // Red
				new Color(1f, 0.5f, 0f, 1f),   // Orange
				new Color(1f, 1f, 0f, 1f),     // Yellow
				new Color(0f, 1f, 0f, 1f),     // Green
				new Color(0f, 1f, 1f, 1f),     // Cyan
				new Color(0f, 0f, 1f, 1f),     // Blue
				new Color(0.5f, 0f, 1f, 1f),   // Indigo
				new Color(1f, 0f, 1f, 1f),     // Magenta
				new Color(1f, 0f, 0.5f, 1f),   // Rose
			});
		}
		
		private void AddContributor(string key, string value)
		{
			_contributors.TryAdd(key, new List<string>());
			_contributors[key].Add(value);
		}
	}
}
