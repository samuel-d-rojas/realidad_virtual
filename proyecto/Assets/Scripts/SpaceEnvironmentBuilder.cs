using UnityEngine;

public class SpaceEnvironmentBuilder : MonoBehaviour
{
    private static readonly Color[] NebulaColors =
    {
        new Color(0.15f, 0.05f, 0.45f, 0.20f),
        new Color(0.05f, 0.35f, 0.70f, 0.18f),
        new Color(0.55f, 0.08f, 0.40f, 0.16f)
    };

    private void Awake()
    {
        PrepareSpaceCamera();
        RemoveTerrestrialScene();
        CreateSpaceLight();
        CreateStarFields();
        //CreateNebulae();
        CreateSpeedStreaks();
        //CreateDistantBodies();
    }

    private void PrepareSpaceCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        cam.farClipPlane = 500f;

        // Si la escena tiene un skybox (Window > Rendering > Lighting > Environment) se muestra ese fondo
        // y no se pone niebla; si no, se usa el color oscuro con niebla.
        if (RenderSettings.skybox != null)
        {
            cam.clearFlags = CameraClearFlags.Skybox;
            RenderSettings.fog = false;
        }
        else
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.005f, 0.008f, 0.035f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.008f, 0.012f, 0.050f);
            RenderSettings.fogStartDistance = 35f;
            RenderSettings.fogEndDistance = 150f;
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.12f, 0.16f, 0.32f);
        RenderSettings.ambientEquatorColor = new Color(0.06f, 0.08f, 0.14f);
        RenderSettings.ambientGroundColor = new Color(0.02f, 0.02f, 0.06f);
        RenderSettings.ambientIntensity = 0.75f;
    }

    private void RemoveTerrestrialScene()
    {
        foreach (Transform child in transform)
        {
            string lowerName = child.name.ToLowerInvariant();
            bool esNave = lowerName == "cannon" || lowerName.Contains("naveespacial") || lowerName.Contains("cannon");
            bool esSistemaEspacial = lowerName.StartsWith("nebulosa") || lowerName.StartsWith("planeta") ||
                                    lowerName.StartsWith("luna") || lowerName.StartsWith("estrella") ||
                                    lowerName.StartsWith("campoestelar") || lowerName.StartsWith("rayas") ||
                                    lowerName.StartsWith("polvo") || lowerName.StartsWith("neon") ||
                                    lowerName.StartsWith("luzyneon") || lowerName.StartsWith("luznave");

            if (!esNave && !esSistemaEspacial)
                child.gameObject.SetActive(false);
        }

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            string lowerName = renderer.name.ToLowerInvariant();
            string parentName = renderer.transform.parent != null ? renderer.transform.parent.name.ToLowerInvariant() : "";
            bool esParteNueva = lowerName.Contains("casco") || lowerName.Contains("canopy") || lowerName.Contains("ala") ||
                                lowerName.Contains("motor") || lowerName.Contains("naveespacial") || lowerName.Contains("luznave") ||
                                parentName.Contains("naveespacial") || lowerName.Contains("nebulo") || lowerName.Contains("planeta") ||
                                lowerName.Contains("luna") || lowerName.Contains("estrell");
            if (!esParteNueva)
                renderer.enabled = false;
        }
    }

    private void CreateSpaceLight()
    {
        Light[] existingLights = GetComponentsInChildren<Light>(true);
        foreach (Light light in existingLights)
        {
            if (light.name == "Directional Light") continue;
            light.gameObject.SetActive(false);
        }

        GameObject sun = CreateEmpty("EstrellaRemota");
        sun.transform.position = new Vector3(-18f, 24f, 80f);
        Light starLight = sun.AddComponent<Light>();
        starLight.type = LightType.Directional;
        starLight.color = new Color(0.62f, 0.78f, 1f);
        starLight.intensity = 1.35f;
        starLight.shadows = LightShadows.Soft;
        starLight.transform.rotation = Quaternion.LookRotation(Vector3.zero - sun.transform.position);

        GameObject blueLight = CreateEmpty("NeonAzul");
        blueLight.transform.position = new Vector3(-7f, 2f, 0f);
        Light blue = blueLight.AddComponent<Light>();
        blue.type = LightType.Point;
        blue.color = new Color(0.05f, 0.45f, 1f);
        blue.intensity = 2.8f;
        blue.range = 25f;
    }

    private void CreateStarFields()
    {
        ParticleSystem particles = CreateParticleSystem("CampoEstelar");
        var main = particles.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(20f, 40f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0.12f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.09f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.65f, 0.78f, 1f), Color.white);
        main.maxParticles = 3000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;

        var emission = particles.emission;
        emission.rateOverTime = 100f;

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 110f;
        shape.position = new Vector3(0f, 0f, 55f);

        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateUnlitMaterial(new Color(0.55f, 0.78f, 1f, 1f), 0.15f);
        particles.transform.localPosition = Vector3.zero;
    }

    private void CreateNebulae()
    {
        for (int i = 0; i < NebulaColors.Length; i++)
        {
            GameObject nebula = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            nebula.name = "Nebulosa" + (i + 1);
            nebula.transform.SetParent(transform);
            nebula.transform.position = new Vector3((i - 1) * 17f, 3f + i * 4f, 75f + i * 18f);
            nebula.transform.localScale = new Vector3(24f + i * 7f, 13f + i * 5f, 4f);
            Collider collider = nebula.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            nebula.GetComponent<Renderer>().material = CreateNebulaMaterial(NebulaColors[i]);
        }

        ParticleSystem nebulaDust = CreateParticleSystem("PolvoCosmico");
        var main = nebulaDust.main;
        main.startLifetime = 18f;
        main.startSpeed = 0.05f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.32f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.35f, 0.60f, 1f, 0.70f), new Color(1f, 0.35f, 0.75f, 0.60f));
        main.maxParticles = 700;
        var emission = nebulaDust.emission;
        emission.rateOverTime = 22f;
        var shape = nebulaDust.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(70f, 35f, 120f);
        nebulaDust.transform.position = new Vector3(0f, 0f, 70f);
        nebulaDust.GetComponent<ParticleSystemRenderer>().material = CreateUnlitMaterial(Color.white, 0.05f);
    }

    private void CreateSpeedStreaks()
    {
        ParticleSystem streaks = CreateParticleSystem("RayasHipervelocidad");
        var main = streaks.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(18f, 28f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.055f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.20f, 0.65f, 1f, 0.70f), new Color(0.90f, 0.95f, 1f, 0.90f));
        main.maxParticles = 500;
        main.gravityModifier = 0f;
        var emission = streaks.emission;
        emission.rateOverTime = 100f;
        var shape = streaks.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(35f, 18f, 1f);
        streaks.transform.position = new Vector3(0f, 0f, 18f);
        streaks.transform.rotation = Quaternion.LookRotation(Vector3.forward);
        var velocity = streaks.velocityOverLifetime;
        velocity.enabled = true;
        velocity.z = new ParticleSystem.MinMaxCurve(15f);
        var renderer = streaks.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.04f;
        renderer.lengthScale = 3f;
        renderer.material = CreateUnlitMaterial(Color.cyan, 0.02f);
    }

    private void CreateDistantBodies()
    {
        CreatePlanet("PlanetaAzul", new Vector3(-22f, 11f, 105f), 8f,
            new Color(0.12f, 0.34f, 0.72f));
        CreatePlanet("PlanetaPurpura", new Vector3(26f, -10f, 125f), 5f,
            new Color(0.38f, 0.12f, 0.55f));

        GameObject moon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        moon.name = "LunaLejana";
        moon.transform.SetParent(transform);
        moon.transform.position = new Vector3(18f, 18f, 92f);
        moon.transform.localScale = Vector3.one * 2.5f;
        Destroy(moon.GetComponent<Collider>());
        moon.GetComponent<Renderer>().material = CreateLitMaterial(new Color(0.52f, 0.56f, 0.68f), 0.05f, 0.65f);
    }

    private void CreatePlanet(string name, Vector3 position, float scale, Color color)
    {
        GameObject planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        planet.name = name;
        planet.transform.SetParent(transform);
        planet.transform.position = position;
        planet.transform.localScale = Vector3.one * scale;
        Destroy(planet.GetComponent<Collider>());
        planet.GetComponent<Renderer>().material = CreateLitMaterial(color, 0.1f, 0.75f);
    }

    private GameObject CreateEmpty(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);
        return go;
    }

    private ParticleSystem CreateParticleSystem(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);
        return go.AddComponent<ParticleSystem>();
    }

    private Material CreateLitMaterial(Color color, float metallic, float smoothness)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material material = new Material(shader);
        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        return material;
    }

    private Material CreateNebulaMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        material.color = color;
        material.renderQueue = 3000;
        return material;
    }

    private Material CreateUnlitMaterial(Color color, float emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        material.color = color;
        if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", color * Mathf.Max(0.2f, emission));
        return material;
    }
}
