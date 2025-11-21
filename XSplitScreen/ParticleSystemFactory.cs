using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.ParticleSystem.PlaybackState;

namespace Dodad.XSplitscreen
{
	/// <summary>
	/// Trail types supported by the ParticleSystemFactory
	/// </summary>
	public enum TrailType
	{
		Ribbon,      // Connected trail following the object
		Particles,   // Individual particles that fall behind
		Flare,       // Glowing stationary effect
		MeshFountain // Mesh-based particle emission
	}

	/// <summary>
	/// Factory class for creating and managing particle systems used as trails
	/// </summary>
	public static class ParticleSystemFactory
	{
		private static Transform _systemContainer;

		// Cache for storing created particle systems by key
		private static readonly Dictionary<string, GameObject> _particleSystemCache = new Dictionary<string, GameObject>();

		/// <summary>
		/// Creates a new particle system with the specified configuration
		/// </summary>
		/// <param name="key">Unique identifier to store in cache</param>
		/// <param name="trailType">Type of trail effect</param>
		/// <param name="material">Material to use for particles</param>
		/// <param name="color">Main color of the trail (used if useTextureColor is false)</param>
		/// <param name="useTextureColor">If true, texture's color will be used instead of the main color</param>
		/// <param name="lifetime">Lifetime of particles in seconds</param>
		/// <param name="length">Length of the trail</param>
		/// <param name="width">Width of the trail</param>
		/// <param name="emissionRate">Rate of particle emission</param>
		/// <param name="meshAsset">Optional mesh for MeshFountain type</param>
		/// <returns>Transform of the created particle system</returns>
		public static ParticleSystem CreateParticleSystem(
			string key,
			TrailType trailType,
			Material material,
			Color color,
			bool useTextureColor = false,
			float lifetime = 1.0f,
			float length = 1.0f,
			float width = 0.1f,
			float rateOverDistance = 0f,
			float emissionRate = 10f,
			Mesh meshAsset = null,
			Material trailMaterial = null)
		{
			// Check if this configuration already exists in cache
			if (_particleSystemCache.ContainsKey(key))
			{
				Debug.LogWarning($"ParticleSystem with key '{key}' already exists in cache. Use GetParticleSystem instead.");
				return GetParticleSystem(key).GetComponent<ParticleSystem>();
			}

			if (_systemContainer == null)
			{
				_systemContainer = new GameObject("XSS Particle Systems").transform;
				UnityEngine.MonoBehaviour.DontDestroyOnLoad(_systemContainer);
			}

			// Create a new game object to hold the particle system
			GameObject particleObj = new GameObject($"ParticleSystem_{key}");
			particleObj.SetActive(false); // Disable until fully configured

			// Add and configure the particle system
			ParticleSystem particleSystem = particleObj.AddComponent<ParticleSystem>();
			ConfigureParticleSystem(particleSystem, trailType, material, color, useTextureColor, lifetime, length, width, emissionRate, rateOverDistance, meshAsset, trailMaterial);

			particleObj.transform.SetParent(_systemContainer);

			// Store in cache
			_particleSystemCache[key] = particleObj;

			return particleSystem;
		}

		public static string[] GetParticleSystemKeys() => _particleSystemCache.Keys.ToArray();

		/// <summary>
		/// Gets a clone of a cached particle system
		/// </summary>
		/// <param name="key">Key of the cached particle system</param>
		/// <returns>Transform of the cloned particle system</returns>
		public static Transform GetParticleSystem(string key)
		{
			if (!_particleSystemCache.TryGetValue(key, out GameObject cachedSystem))
			{
				Debug.LogError($"No particle system found with key '{key}'");
				return null;
			}

			// Create a clone of the cached particle system
			GameObject clone = Object.Instantiate(cachedSystem);
			clone.name = $"ParticleSystem_{key}_Clone";

			return clone.transform;
		}

		/// <summary>
		/// Configures a particle system based on the provided parameters
		/// </summary>
		private static void ConfigureParticleSystem(
			ParticleSystem particleSystem,
			TrailType trailType,
			Material material,
			Color color,
			bool useTextureColor,
			float lifetime,
			float length,
			float width,
			float emissionRate,
			float rateOverDistance,
			Mesh meshAsset,
			Material trailMaterial)
		{
			// Get the main module
			var main = particleSystem.main;
			main.startLifetime = lifetime;
			main.simulationSpace = ParticleSystemSimulationSpace.World; // Better for trails
			main.startSpeed = 0; // For trails, we control position directly
			main.startSize = width;
			main.maxParticles = Mathf.CeilToInt(emissionRate * lifetime * 1.5f); // Set a reasonable max particle limit

			if (!useTextureColor)
			{
				main.startColor = color;
			}
			else
			{
				main.startColor = Color.white; // Use white to preserve texture color
			}

			// Emission module
			var emission = particleSystem.emission;
			emission.rateOverTime = emissionRate;

			// Get renderer
			var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
			renderer.material = material;
			renderer.sortingFudge = 0; // Optimize rendering order

			// Configure based on trail type
			switch (trailType)
			{
				case TrailType.Ribbon:
					ConfigureRibbonTrail(particleSystem, renderer, length, width, trailMaterial);
					break;
				case TrailType.Particles:
					ConfigureParticleTrail(particleSystem, renderer, length, width, rateOverDistance);
					break;
				case TrailType.Flare:
					ConfigureFlare(particleSystem, renderer, width);
					break;
				case TrailType.MeshFountain:
					ConfigureMeshFountain(particleSystem, renderer, meshAsset, length);
					break;
			}

			// Optimize particle system
			OptimizeParticleSystem(particleSystem);
		}

		/// <summary>
		/// Configures a ribbon-style trail
		/// </summary>
		private static void ConfigureRibbonTrail(ParticleSystem particleSystem, ParticleSystemRenderer renderer, float length, float width, Material trailMaterial)
		{
			var trails = particleSystem.trails;
			trails.enabled = true;
			trails.mode = ParticleSystemTrailMode.Ribbon;
			trails.ratio = 1.0f;
			trails.lifetime = length;
			trails.widthOverTrail = width;
			trails.dieWithParticles = true;
			trails.sizeAffectsWidth = false;

			var main = particleSystem.main;
			main.startLifetime = length + 0.1f;

			renderer.alignment = ParticleSystemRenderSpace.View;
			renderer.renderMode = ParticleSystemRenderMode.Stretch;
			renderer.trailMaterial = trailMaterial;
		}

		/// <summary>
		/// Configures individual particles as a trail
		/// </summary>
		private static void ConfigureParticleTrail(ParticleSystem particleSystem, ParticleSystemRenderer renderer, float length, float width, float rateOverDistance)
		{
			renderer.renderMode = ParticleSystemRenderMode.Billboard;
			var emission = particleSystem.emission;
			emission.rateOverDistance = rateOverDistance;

			var velocityOverLifetime = particleSystem.velocityOverLifetime;
			velocityOverLifetime.enabled = true;
			velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
		}

		/// <summary>
		/// Configures a glowing flare effect
		/// </summary>
		private static void ConfigureFlare(ParticleSystem particleSystem, ParticleSystemRenderer renderer, float width)
		{
			var main = particleSystem.main;
			main.startLifetime = 0.5f;
			main.startSize = width * 1.5f;

			var velocityOverLifetime = particleSystem.velocityOverLifetime;
			velocityOverLifetime.enabled = false;

			var rotationOverLifetime = particleSystem.rotationOverLifetime;
			rotationOverLifetime.enabled = true;
			rotationOverLifetime.separateAxes = false;
			rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(0f, 360f);

			var sizeOverLifetime = particleSystem.sizeOverLifetime;
			sizeOverLifetime.enabled = true;
			AnimationCurve pulseCurve = new AnimationCurve(
				new Keyframe(0f, 0.8f),
				new Keyframe(0.5f, 1.2f),
				new Keyframe(1f, 0.8f)
			);
			sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, pulseCurve);

			var colorOverLifetime = particleSystem.colorOverLifetime;
			colorOverLifetime.enabled = true;
			Gradient colorGradient = new Gradient();
			colorGradient.SetKeys(
				new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
				new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(1f, 0.9f), new GradientAlphaKey(0f, 1f) }
			);
			colorOverLifetime.color = colorGradient;

			renderer.renderMode = ParticleSystemRenderMode.Billboard;
		}

		/// <summary>
		/// Configures a mesh-based particle fountain
		/// </summary>
		private static void ConfigureMeshFountain(ParticleSystem particleSystem, ParticleSystemRenderer renderer, Mesh meshAsset, float length)
		{
			if (meshAsset == null)
			{
				Debug.LogError("Mesh asset is required for MeshFountain trail type");
				return;
			}

			renderer.renderMode = ParticleSystemRenderMode.Mesh;
			renderer.mesh = meshAsset;

			var main = particleSystem.main;
			main.startSpeed = length * 0.5f;
			main.startLifetime = length / main.startSpeed.constant * 2f;

			main.startRotation3D = true;
			main.startRotationX = new ParticleSystem.MinMaxCurve(0f, 360f);
			main.startRotationY = new ParticleSystem.MinMaxCurve(0f, 360f);
			main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, 360f);

			var shape = particleSystem.shape;
			shape.enabled = true;
			shape.shapeType = ParticleSystemShapeType.Cone;
			shape.angle = 15f;

			var limitVelocity = particleSystem.limitVelocityOverLifetime;
			limitVelocity.enabled = true;
			limitVelocity.dampen = 0.1f;

			var rotationOverLifetime = particleSystem.rotationOverLifetime;
			rotationOverLifetime.enabled = true;
			rotationOverLifetime.separateAxes = true;
			rotationOverLifetime.x = new ParticleSystem.MinMaxCurve(0f, 90f);
			rotationOverLifetime.y = new ParticleSystem.MinMaxCurve(0f, 90f);
			rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(0f, 90f);
		}

		/// <summary>
		/// Applies optimization settings to the particle system
		/// </summary>
		private static void OptimizeParticleSystem(ParticleSystem particleSystem)
		{
			var noise = particleSystem.noise;
			noise.enabled = false;

			var lights = particleSystem.lights;
			lights.enabled = false;

			var collision = particleSystem.collision;
			collision.enabled = false;

			var textureSheetAnimation = particleSystem.textureSheetAnimation;
			textureSheetAnimation.enabled = false;

			var main = particleSystem.main;
			main.useUnscaledTime = false;
			main.simulationSpeed = 1.0f;
			main.cullingMode = ParticleSystemCullingMode.Automatic;
			main.prewarm = false;

			ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
			renderer.enableGPUInstancing = true;
			renderer.minParticleSize = 0.01f;
			renderer.maxParticleSize = 0.5f;
			renderer.allowRoll = false;
		}

		/// <summary>
		/// Creates all cosmetic particle systems
		/// </summary>
		public static void CreateAllParticleSystems()
		{
			CreateLemonDot();
			CreateSparkleDust();
			CreateDarkVoid();
			CreateHearts();
			CreateSkulls();
			CreateBluePulse();
		}

		private static void CreateLemonDot()
		{
			Gradient alphaGradient_A = new();
			alphaGradient_A.SetKeys(
				new GradientColorKey[]
				{
					new GradientColorKey(Color.yellow, 0f),
					new GradientColorKey(new Color(1f, 0.95f, 0.8f), 1f),
				},
				new GradientAlphaKey[]
				{
					new GradientAlphaKey(1f, 0f),
					new GradientAlphaKey(1f, 0.75f),
					new GradientAlphaKey(0f, 1f),
				}
			);

			int octaves = 16;
			float amplitude = 35f;
			float deg2rad = Mathf.PI / 180f;

			AnimationCurve seeSawCurve = new AnimationCurve();
			for (int i = 0; i <= octaves * 2; i++)
			{
				float t = (float) i / (octaves * 2);
				float angle = Mathf.Sin(t * Mathf.PI * octaves) * amplitude * deg2rad;
				seeSawCurve.AddKey(t, angle);
			}

			var matA = new Material(Plugin.Resources.LoadAsset<Material>("ParticleMat_A.mat"));
			var lemonTexture = Plugin.Resources.LoadAsset<Texture2D>("lemon.png");
			var lemonMat = new Material(matA);
			lemonMat.SetTexture("_MainTex", lemonTexture);

			var lemonSystem = ParticleSystemFactory.CreateParticleSystem(
				"LemonDot",
				TrailType.Particles,
				lemonMat,
				new Color(1f, 1f, 0.8f),
				useTextureColor: true,
				width: 0.4f,
				emissionRate: 1f,
				lifetime: 8f,
				rateOverDistance: 1f
			);

			var lemonShape = lemonSystem.shape;
			lemonShape.shapeType = ParticleSystemShapeType.Sphere;
			lemonShape.radius = 2.53f;
			lemonShape.radiusThickness = 0.18f;

			var lemonColor = lemonSystem.colorOverLifetime;
			lemonColor.enabled = true;
			lemonColor.color = alphaGradient_A;

			var lemonRotation = lemonSystem.rotationOverLifetime;
			lemonRotation.enabled = true;
			lemonRotation.z = new ParticleSystem.MinMaxCurve(1.5f, seeSawCurve);

			var sizeOverLifetime = lemonSystem.sizeOverLifetime;
			sizeOverLifetime.enabled = true;
			AnimationCurve wobbleCurve = new AnimationCurve(
				new Keyframe(0f, 1.2f),
				new Keyframe(0.2f, 1.0f),
				new Keyframe(0.5f, 1.3f),
				new Keyframe(0.8f, 1.0f),
				new Keyframe(1f, 1.2f)
			);
			sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, wobbleCurve);
		}

		private static void CreateSparkleDust()
		{
			Gradient sparkleColorGradient = new Gradient();
			sparkleColorGradient.SetKeys(
				new GradientColorKey[] {
					new GradientColorKey(new Color(1f, 0.95f, 0.8f), 0f),
					new GradientColorKey(new Color(0.8f, 0.95f, 1f), 0.5f),
					new GradientColorKey(new Color(1f, 1f, 0.9f), 1f)
				},
				new GradientAlphaKey[] {
					new GradientAlphaKey(0f, 0f),
					new GradientAlphaKey(0.8f, 0.18f),
					new GradientAlphaKey(1f, 0.3f),
					new GradientAlphaKey(1f, 0.7f),
					new GradientAlphaKey(0.3f, 0.85f),
					new GradientAlphaKey(0f, 1f)
				}
			);

			AnimationCurve twinkleSizeCurve = new AnimationCurve();
			twinkleSizeCurve.AddKey(0.000f, 0.0f);
			twinkleSizeCurve.AddKey(0.062f, 0.8f);
			twinkleSizeCurve.AddKey(0.125f, 1.2f);
			twinkleSizeCurve.AddKey(0.188f, 0.6f);
			twinkleSizeCurve.AddKey(0.250f, 1.0f);
			twinkleSizeCurve.AddKey(0.312f, 0.7f);
			twinkleSizeCurve.AddKey(0.375f, 0.8f);
			twinkleSizeCurve.AddKey(0.438f, 1.2f);
			twinkleSizeCurve.AddKey(0.500f, 0.6f);
			twinkleSizeCurve.AddKey(0.562f, 1.0f);
			twinkleSizeCurve.AddKey(0.625f, 0.7f);
			twinkleSizeCurve.AddKey(0.688f, 0.8f);
			twinkleSizeCurve.AddKey(0.750f, 1.2f);
			twinkleSizeCurve.AddKey(0.812f, 0.6f);
			twinkleSizeCurve.AddKey(0.875f, 1.0f);
			twinkleSizeCurve.AddKey(0.938f, 0.7f);
			twinkleSizeCurve.AddKey(1.000f, 0.0f);

			var sparkleMaterial = new Material(Plugin.Resources.LoadAsset<Material>("ParticleMat_A.mat"));
			sparkleMaterial.SetTexture("_MainTex", Plugin.Resources.LoadAsset<Texture2D>("flare_02.png"));

			var sparkleSystem = ParticleSystemFactory.CreateParticleSystem(
				"SparkleDust",
				TrailType.Particles,
				sparkleMaterial,
				Color.white,
				useTextureColor: true,
				lifetime: 2.1f,
				width: 0.15f,
				emissionRate: 10f,
				rateOverDistance: 1f
			);

			var main = sparkleSystem.main;
			main.startSpeed = 0.08f;
			main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.15f);

			var velocityOverLifetime = sparkleSystem.velocityOverLifetime;
			velocityOverLifetime.enabled = true;
			velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
			velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.09f, 0.18f);
			velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.13f, 0.13f);

			var sizeOverLifetime = sparkleSystem.sizeOverLifetime;
			sizeOverLifetime.enabled = true;
			sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, twinkleSizeCurve);

			var colorOverLifetime = sparkleSystem.colorOverLifetime;
			colorOverLifetime.enabled = true;
			colorOverLifetime.color = sparkleColorGradient;

			var shape = sparkleSystem.shape;
			shape.enabled = true;
			shape.shapeType = ParticleSystemShapeType.Sphere;
			shape.radius = 0.7f;
			shape.radiusThickness = 0.18f;
		}

		private static void CreateHearts()
		{
			Gradient sparkleColorGradient = new Gradient();
			sparkleColorGradient.SetKeys(
				new GradientColorKey[] {
					new GradientColorKey(new Color(1f, 0.95f, 0.8f), 0f),
					new GradientColorKey(new Color(0.8f, 0.95f, 1f), 0.5f),
					new GradientColorKey(new Color(1f, 1f, 0.9f), 1f)
				},
				new GradientAlphaKey[] {
					new GradientAlphaKey(0f, 0f),
					new GradientAlphaKey(0.8f, 0.18f),
					new GradientAlphaKey(1f, 0.3f),
					new GradientAlphaKey(1f, 0.7f),
					new GradientAlphaKey(0.3f, 0.85f),
					new GradientAlphaKey(0f, 1f)
				}
			);

			AnimationCurve twinkleSizeCurve = new AnimationCurve();
			twinkleSizeCurve.AddKey(0.1f, 0.8f);
			twinkleSizeCurve.AddKey(0.3f, 1.2f);
			twinkleSizeCurve.AddKey(0.5f, 0.6f);
			twinkleSizeCurve.AddKey(0.7f, 1.0f);
			twinkleSizeCurve.AddKey(0.9f, 0.7f);

			var sparkleMaterial = new Material(Plugin.Resources.LoadAsset<Material>("ParticleMat_A.mat"));
			sparkleMaterial.SetTexture("_MainTex", Plugin.Resources.LoadAsset<Texture2D>("heart_02.png"));

			var sparkleSystem = ParticleSystemFactory.CreateParticleSystem(
				"Hearts",
				TrailType.Particles,
				sparkleMaterial,
				Color.white,
				useTextureColor: true,
				lifetime: 1.1f,
				width: 0.25f,
				emissionRate: 4f,
				rateOverDistance: 1f
			);

			var main = sparkleSystem.main;
			main.startSpeed = 0.08f;
			main.startSize = new ParticleSystem.MinMaxCurve(0.17f, 0.25f);

			var velocityOverLifetime = sparkleSystem.velocityOverLifetime;
			velocityOverLifetime.enabled = true;
			velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
			velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.09f, 0.18f);
			velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.13f, 0.13f);

			var sizeOverLifetime = sparkleSystem.sizeOverLifetime;
			sizeOverLifetime.enabled = true;
			sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, twinkleSizeCurve);

			var colorOverLifetime = sparkleSystem.colorOverLifetime;
			colorOverLifetime.enabled = true;
			colorOverLifetime.color = sparkleColorGradient;

			var shape = sparkleSystem.shape;
			shape.enabled = true;
			shape.shapeType = ParticleSystemShapeType.Sphere;
			shape.radius = 2.7f;
			shape.radiusThickness = 0.18f;
		}

		private static void CreateSkulls()
		{
			Gradient sparkleColorGradient = new Gradient();
			sparkleColorGradient.SetKeys(
				new GradientColorKey[] {
					new GradientColorKey(new Color(1f, 0.95f, 0.8f), 0f),
					new GradientColorKey(new Color(0.8f, 0.95f, 1f), 0.5f),
					new GradientColorKey(new Color(1f, 1f, 0.9f), 1f)
				},
				new GradientAlphaKey[] {
					new GradientAlphaKey(0f, 0f),
					new GradientAlphaKey(0.8f, 0.18f),
					new GradientAlphaKey(1f, 0.3f),
					new GradientAlphaKey(1f, 0.7f),
					new GradientAlphaKey(0.3f, 0.85f),
					new GradientAlphaKey(0f, 1f)
				}
			);

			AnimationCurve twinkleSizeCurve = new AnimationCurve();
			twinkleSizeCurve.AddKey(0.000f, 0.0f);
			twinkleSizeCurve.AddKey(0.062f, 0.9f);
			twinkleSizeCurve.AddKey(0.125f, 1.1f);
			twinkleSizeCurve.AddKey(0.188f, 0.9f);
			twinkleSizeCurve.AddKey(0.250f, 1.1f);
			twinkleSizeCurve.AddKey(0.312f, 0.9f);
			twinkleSizeCurve.AddKey(0.375f, 1.0f);
			twinkleSizeCurve.AddKey(0.438f, 0.9f);
			twinkleSizeCurve.AddKey(0.500f, 1.1f);
			twinkleSizeCurve.AddKey(0.562f, 0.9f);
			twinkleSizeCurve.AddKey(0.625f, 1.1f);
			twinkleSizeCurve.AddKey(0.688f, 0.9f);
			twinkleSizeCurve.AddKey(0.750f, 1.1f);
			twinkleSizeCurve.AddKey(0.812f, 0.9f);
			twinkleSizeCurve.AddKey(0.875f, 1.1f);
			twinkleSizeCurve.AddKey(0.938f, 0.9f);
			twinkleSizeCurve.AddKey(1.000f, 0.0f);

			var sparkleMaterial = new Material(Plugin.Resources.LoadAsset<Material>("ParticleMat_A.mat"));
			sparkleMaterial.SetTexture("_MainTex", Plugin.Resources.LoadAsset<Texture2D>("skull_01.png"));

			var sparkleSystem = ParticleSystemFactory.CreateParticleSystem(
				"Skulls",
				TrailType.Particles,
				sparkleMaterial,
				Color.white,
				useTextureColor: true,
				lifetime: 1.4f,
				width: 0.5f,
				emissionRate: 4f,
				rateOverDistance: 1f
			);

			var main = sparkleSystem.main;
			main.startSpeed = 0.04f;
			main.startSize = new ParticleSystem.MinMaxCurve(0.17f, 0.25f);

			var velocityOverLifetime = sparkleSystem.velocityOverLifetime;
			velocityOverLifetime.enabled = true;
			velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(0f);
			velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0f);

			AnimationCurve upDownCurve = new AnimationCurve(
				new Keyframe(0f, 0.1f),
				new Keyframe(0.25f, -0.1f),
				new Keyframe(0.5f, 0.1f),
				new Keyframe(0.75f, -0.1f),
				new Keyframe(1f, 0.1f)
			);
			velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(1f, upDownCurve);

			var sizeOverLifetime = sparkleSystem.sizeOverLifetime;
			sizeOverLifetime.enabled = true;
			sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, twinkleSizeCurve);

			var colorOverLifetime = sparkleSystem.colorOverLifetime;
			colorOverLifetime.enabled = true;
			colorOverLifetime.color = sparkleColorGradient;

			var shape = sparkleSystem.shape;
			shape.enabled = true;
			shape.shapeType = ParticleSystemShapeType.Sphere;
			shape.radius = 2.7f;
			shape.radiusThickness = 0.18f;
		}

		private static void CreateDarkVoid()
		{
			var matA = new Material(Plugin.Resources.LoadAsset<Material>("ParticleMat_A.mat"));
			var darkTexture = Plugin.Resources.LoadAsset<Texture2D>("purplegradient_01.png");
			var darkMat = new Material(matA);
			darkMat.SetTexture("_MainTex", darkTexture);

			var darkVoid = ParticleSystemFactory.CreateParticleSystem(
				"PurpleScribble",
				TrailType.Ribbon,
				darkMat,
				new Color(0.1f, 0f, 0.2f, 0.7f),
				useTextureColor: true,
				lifetime: 2.8f,
				length: 2.5f,
				width: 0.3f,
				emissionRate: 14f,
				trailMaterial: darkMat
			);

			var colorOverLifetime = darkVoid.colorOverLifetime;
			colorOverLifetime.enabled = true;
			Gradient grad = new Gradient();
			grad.SetKeys(
				new GradientColorKey[] {
					new GradientColorKey(new Color(0.3f, 0.05f, 0.4f), 0f),
					new GradientColorKey(new Color(0f, 0f, 0f), 1f)
				},
				new GradientAlphaKey[] {
					new GradientAlphaKey(0.7f, 0f),
					new GradientAlphaKey(0.2f, 0.85f),
					new GradientAlphaKey(0f, 1f)
				}
			);
			colorOverLifetime.color = grad;
		}

		private static void CreateBluePulse()
		{
			// Blue-white gradient for celestial effect
			Gradient flareGradient = new Gradient();
			flareGradient.SetKeys(
				new GradientColorKey[] {
			new GradientColorKey(new Color(0.7f, 0.9f, 1f), 0f),
			new GradientColorKey(new Color(1f, 1f, 1f), 0.25f),
			new GradientColorKey(new Color(0.5f, 0.7f, 1f), 0.75f),
			new GradientColorKey(new Color(0.1f, 0.3f, 0.8f), 1f)
				},
				new GradientAlphaKey[] {
			new GradientAlphaKey(0f, 0f),
			new GradientAlphaKey(0.95f, 0.05f),
			new GradientAlphaKey(1f, 0.3f),
			new GradientAlphaKey(0.5f, 0.8f),
			new GradientAlphaKey(0f, 1f)
				}
			);

			AnimationCurve pulseCurve = new AnimationCurve(
				new Keyframe(0f, 0.8f),
				new Keyframe(0.5f, 1.4f),
				new Keyframe(1f, 0.8f)
			);

			var mat = new Material(Plugin.Resources.LoadAsset<Material>("ParticleMat_A.mat"));
			mat.SetTexture("_MainTex", Plugin.Resources.LoadAsset<Texture2D>("FX_Flare_3.png"));

			var flareSystem = ParticleSystemFactory.CreateParticleSystem(
				"BluePulse",
				TrailType.Flare,
				mat,
				new Color(0.95f, 0.98f, 1f, 0.9f),
				useTextureColor: true,
				lifetime: 0.9f,
				width: 0.7f,
				emissionRate: 3f
			);

			var colorOverLifetime = flareSystem.colorOverLifetime;
			colorOverLifetime.enabled = true;
			colorOverLifetime.color = flareGradient;

			var sizeOverLifetime = flareSystem.sizeOverLifetime;
			sizeOverLifetime.enabled = true;
			sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, pulseCurve);

			var shape = flareSystem.shape;
			shape.enabled = true;
			shape.shapeType = ParticleSystemShapeType.Sphere;
			shape.radius = 0.5f;
			shape.radiusThickness = 0.15f;
		}

/*		private static void CreateCrystalBurst()
		{
			Gradient crystalGradient = new Gradient();
			crystalGradient.SetKeys(
				new GradientColorKey[] {
			new GradientColorKey(new Color(0.1f, 0.9f, 1f), 0f),
			new GradientColorKey(new Color(0.7f, 0.3f, 0.9f), 0.5f),
			new GradientColorKey(new Color(0.2f, 0.2f, 0.7f), 1f)
				},
				new GradientAlphaKey[] {
			new GradientAlphaKey(1f, 0f),
			new GradientAlphaKey(0.7f, 0.7f),
			new GradientAlphaKey(0.2f, 1f)
				}
			);

			var mat = new Material(Plugin.Resources.LoadAsset<Material>("ParticleMat_A.mat"));
			mat.SetTexture("_MainTex", Plugin.Resources.LoadAsset<Texture2D>("flare_01.png"));

			var testCubePrefab = Plugin.Resources.LoadAsset<GameObject>("testCubePrefab.prefab");
			Mesh crystalMesh = testCubePrefab.GetComponentInChildren<MeshFilter>().mesh;

			var meshSystem = ParticleSystemFactory.CreateParticleSystem(
				"CrystalBurst",
				TrailType.MeshFountain,
				mat,
				new Color(0.4f, 0.9f, 1f, 1f),
				useTextureColor: true,
				lifetime: 2.8f,
				length: 7f,
				width: 0.5f,
				emissionRate: 6f,
				meshAsset: crystalMesh
			);

			var colorOverLifetime = meshSystem.colorOverLifetime;
			colorOverLifetime.enabled = true;
			colorOverLifetime.color = crystalGradient;

			var rotationOverLifetime = meshSystem.rotationOverLifetime;
			rotationOverLifetime.enabled = true;
			rotationOverLifetime.separateAxes = true;
			rotationOverLifetime.x = new ParticleSystem.MinMaxCurve(0f, 180f);
			rotationOverLifetime.y = new ParticleSystem.MinMaxCurve(0f, 180f);
			rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(0f, 180f);

			var sizeOverLifetime = meshSystem.sizeOverLifetime;
			sizeOverLifetime.enabled = true;
			AnimationCurve burstCurve = new AnimationCurve(
				new Keyframe(0f, 0.7f),
				new Keyframe(0.3f, 1.4f),
				new Keyframe(0.7f, 1.0f),
				new Keyframe(1f, 0.7f)
			);
			sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, burstCurve);

			var shape = meshSystem.shape;
			shape.enabled = true;
			shape.shapeType = ParticleSystemShapeType.Cone;
			shape.angle = 18f;
			shape.radius = 0.6f;
			shape.radiusThickness = 0.20f;
		}*/
	}
}