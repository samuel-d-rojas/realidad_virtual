using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class AsteroidSpawner : MonoBehaviour
{
    public Vector3 areaMin = new Vector3(-7f, 1.5f, 14f);
    public Vector3 areaMax = new Vector3(7f, 7f, 22f);
    public float intervaloInicial = 2.8f;
    public float intervaloMinimo = 1.8f;
    public int maximoAsteroides = 6;
    public float velocidadMinima = 0.65f;
    public float velocidadMaxima = 1.35f;

    [Tooltip("Arrastra aquí Metorito.fbx. Si queda vacío se usan esferas.")]
    public GameObject modeloMeteorito;

    private readonly List<Asteroid> asteroides = new List<Asteroid>();
    private float siguienteAsteroide;
    private float intervalo;
    private int destruidos;
    private int fallados;
    private int combo;

    private UIDocument hud;

    void Start()
    {
        AplicarNivel(PlayerPrefs.GetInt("nivel", 1));
        intervalo = intervaloInicial;
        siguienteAsteroide = Time.time + intervalo;
        BuscarHUD();
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy || Time.timeScale <= 0f)
            return;

        asteroides.RemoveAll(a => a == null);
        if (Time.time >= siguienteAsteroide && asteroides.Count < maximoAsteroides)
        {
            CrearAsteroide();
            intervalo = Mathf.Max(intervaloMinimo, intervaloInicial - destruidos * 0.04f);
            siguienteAsteroide = Time.time + intervalo;
        }

        ActualizarHUD();
    }

    void BuscarHUD()
    {
        UIDocument[] documentos = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
        foreach (UIDocument documento in documentos)
        {
            if (documento.gameObject.name == "Juego")
            {
                hud = documento;
                return;
            }
        }
    }

    void CrearAsteroide()
    {
        Vector3 posicion = new Vector3(
            Random.Range(areaMin.x, areaMax.x),
            Random.Range(areaMin.y, areaMax.y),
            Random.Range(areaMin.z, areaMax.z));

        GameObject asteroide;
        if (modeloMeteorito != null)
        {
            // El modelo se centra y se lleva a 1 unidad dentro de un contenedor; el contenedor tiene el collider.
            asteroide = new GameObject("AsteroideEspacial");
            GameObject modelo = Instantiate(modeloMeteorito, asteroide.transform, false);
            foreach (Collider colliderModelo in modelo.GetComponentsInChildren<Collider>())
                Destroy(colliderModelo);
            ModeloUtil.Encajar(modelo.transform, 1f);
            asteroide.AddComponent<SphereCollider>().radius = 0.5f;
            asteroide.transform.rotation = Random.rotation;
        }
        else
        {
            asteroide = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            asteroide.name = "AsteroideEspacial";
        }

        asteroide.transform.position = posicion;
        float tamano = Random.Range(0.55f, 1.85f);
        asteroide.transform.localScale = new Vector3(
            tamano * Random.Range(0.78f, 1.18f),
            tamano * Random.Range(0.78f, 1.18f),
            tamano * Random.Range(0.78f, 1.18f));

        Renderer renderer = asteroide.GetComponent<Renderer>();
        if (modeloMeteorito == null && renderer != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader);
            Color roca = new Color(Random.Range(0.25f, 0.48f), Random.Range(0.22f, 0.38f), Random.Range(0.28f, 0.50f));
            material.color = roca;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", roca);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", Random.Range(0.02f, 0.25f));
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", Random.Range(0.08f, 0.38f));
            renderer.material = material;
        }

        TrailRenderer trail = asteroide.AddComponent<TrailRenderer>();
        trail.time = Random.Range(0.18f, 0.38f);
        trail.startWidth = Mathf.Clamp(tamano * 0.18f, 0.08f, 0.32f);
        trail.endWidth = 0f;
        trail.minVertexDistance = 0.08f;
        trail.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default"));
        trail.startColor = new Color(0.45f, 0.65f, 1f, 0.55f);
        trail.endColor = new Color(0.05f, 0.15f, 0.45f, 0f);

        SphereCollider collider = asteroide.GetComponent<SphereCollider>();
        if (collider != null)
            collider.isTrigger = true;

        Asteroid componente = asteroide.AddComponent<Asteroid>();
        componente.velocidad = Random.Range(velocidadMinima, velocidadMaxima);
        componente.Inicializar(ObtenerDestino(), this);
        asteroides.Add(componente);
    }

    Vector3 ObtenerDestino()
    {
        GameObject nave = GameObject.Find("NaveEspacial");
        if (nave != null)
            return nave.transform.position;
        GameObject cannon = GameObject.Find("Cannon");
        return cannon != null ? cannon.transform.position + Vector3.forward * 2f : Vector3.zero;
    }

    public void AsteroideDestruido(GameObject asteroide)
    {
        destruidos++;
        combo++;
        ActualizarHUD();
    }

    public void AsteroideNoAlcanzado()
    {
        fallados++;
        combo = 0;
        ActualizarHUD();
    }

    void ActualizarHUD()
    {
        if (hud == null || hud.rootVisualElement == null)
            BuscarHUD();
        if (hud == null || hud.rootVisualElement == null)
            return;

        Label comboLabel = hud.rootVisualElement.Q<Label>("ComboValor");
        if (comboLabel != null)
            comboLabel.text = combo.ToString("D2");

        Label multiplicador = hud.rootVisualElement.Q<Label>("Multiplicador");
        if (multiplicador != null)
        {
            int x = combo >= 8 ? 4 : combo >= 4 ? 2 : 1;
            multiplicador.text = "X" + x;
        }

        Label mision = hud.rootVisualElement.Q<Label>("MisionTexto");
        if (mision != null)
            mision.text = "Asteroides destruidos: " + destruidos + " | Fallados: " + fallados;
    }

    public void ReiniciarSesion()
    {
        AplicarNivel(PlayerPrefs.GetInt("nivel", 1));
        destruidos = 0;
        fallados = 0;
        combo = 0;
        foreach (Asteroid asteroide in asteroides)
        {
            if (asteroide != null)
                Destroy(asteroide.gameObject);
        }
        asteroides.Clear();
        intervalo = intervaloInicial;
        siguienteAsteroide = Time.time + intervalo;
        ActualizarHUD();
    }

    void AplicarNivel(int nivel)
    {
        nivel = Mathf.Clamp(nivel, 1, 3);
        switch (nivel)
        {
            case 1:
                intervaloInicial = 3.2f;
                maximoAsteroides = 4;
                velocidadMaxima = 1.0f;
                break;
            case 2:
                intervaloInicial = 2.7f;
                maximoAsteroides = 5;
                velocidadMaxima = 1.25f;
                break;
            default:
                intervaloInicial = 2.2f;
                maximoAsteroides = 6;
                velocidadMaxima = 1.45f;
                break;
        }
        intervalo = intervaloInicial;
        siguienteAsteroide = Time.time + intervalo;
    }
}
