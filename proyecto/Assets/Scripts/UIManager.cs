using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour
{
    public GameObject mundoJuego;
    public UIDocument inicioSesion;
    public UIDocument menu;
    public UIDocument juego;
    public UIDocument ajustes;
    public UIDocument tutorial;

    private Dictionary<string, UIDocument> pantallas;
    private Coroutine rutinaActual;

    void Awake()
    {
        AudioListener.volume = PlayerPrefs.GetFloat("volumen", 1f);
        if (PlayerPrefs.GetInt("calidad_baja", 0) == 1 && QualitySettings.names.Length > 0)
            QualitySettings.SetQualityLevel(0);
    }

    void Start()
    {
        pantallas = new Dictionary<string, UIDocument>
        {
            { "InicioSesion", inicioSesion }, { "Menu", menu }, { "Juego", juego },
            { "Ajustes", ajustes }, { "Tutorial", tutorial }
        };

        if (mundoJuego == null)
            Debug.LogError("UIManager: falta asignar 'Mundo Juego' en el Inspector. Sin él no aparecen los meteoritos ni el espacio.");

        if (mundoJuego != null && mundoJuego.GetComponentInChildren<AsteroidSpawner>(true) == null)
            mundoJuego.AddComponent<AsteroidSpawner>();
        if (mundoJuego != null && mundoJuego.GetComponent<SpaceEnvironmentBuilder>() == null)
            mundoJuego.AddComponent<SpaceEnvironmentBuilder>();

        // Siempre se pide iniciar sesión al abrir el juego; el último usuario queda escrito en el campo.
        Mostrar("InicioSesion");
    }

    public void Mostrar(string nombre)
    {
        if (rutinaActual != null) StopCoroutine(rutinaActual);
        rutinaActual = StartCoroutine(MostrarRutina(nombre));
    }

    IEnumerator MostrarRutina(string nombre)
    {
        foreach (var par in pantallas)
            if (par.Value != null) par.Value.gameObject.SetActive(false);

        if (!pantallas.TryGetValue(nombre, out UIDocument doc) || doc == null)
        {
            Debug.LogWarning("Pantalla no encontrada: " + nombre);
            yield break;
        }

        doc.gameObject.SetActive(true);
        yield return null;
        Conectar(doc, nombre);

        if (mundoJuego != null)
        {
            bool juegoActivo = nombre == "Juego";
            mundoJuego.SetActive(juegoActivo);
            if (juegoActivo)
                mundoJuego.GetComponentInChildren<AsteroidSpawner>(true)?.ReiniciarSesion();
        }

        Time.timeScale = nombre == "Juego" ? 1f : 0f;
    }

    void Conectar(UIDocument doc, string nombre)
    {
        VisualElement root = doc.rootVisualElement;
        if (root == null) return;
        // La marca va en el contenido clonado del UXML: si Unity lo reconstruye al reactivar la pantalla,
        // la marca desaparece y los botones nuevos se vuelven a conectar.
        VisualElement contenido = root.childCount > 0 ? root[0] : root;
        if (contenido.userData is true) return;
        contenido.userData = true;

        root.Query<Button>().ForEach(boton =>
        {
            if (boton.name.StartsWith("Ir_"))
            {
                string destino = boton.name.Substring(3);
                boton.clicked += () => Mostrar(destino);
            }
            else if (boton.name == "Entrar") boton.clicked += Entrar;
            else if (boton.name == "BotonInvitado") boton.clicked += EntrarInvitado;
            else if (boton.name == "BotonCrearCuenta") boton.clicked += CrearCuenta;
            else if (boton.name == "Salir") boton.clicked += Salir;
            else if (boton.name.StartsWith("BotonNivel"))
            {
                int nivel = int.Parse(boton.name.Replace("BotonNivel", ""));
                boton.clicked += () => SeleccionarNivel(nivel, root);
            }
            else if (boton.name == "BotonPerfil") boton.clicked += MostrarPerfil;
        });

        TextField usuario = root.Q<TextField>("TextFieldUsua");
        TextField clave = root.Q<TextField>("TextFieldContra");
        if (usuario != null) usuario.value = PlayerPrefs.GetString("usuario", "");
        if (clave != null) clave.isPasswordField = true;

        Label paciente = root.Q<Label>("NombrePaciente");
        if (paciente != null) paciente.text = PlayerPrefs.GetString("usuario", "Paciente 001");

        ConfigurarTutorial(root);
        ConfigurarAjustes(root);
        SeleccionarNivelGuardado(root);
    }

    void Entrar()
    {
        if (inicioSesion != null)
        {
            TextField usuario = inicioSesion.rootVisualElement.Q<TextField>("TextFieldUsua");
            if (usuario != null)
                PlayerPrefs.SetString("usuario", string.IsNullOrWhiteSpace(usuario.value) ? "Paciente 001" : usuario.value.Trim());
        }
        PlayerPrefs.SetInt("sesion", 1);
        PlayerPrefs.Save();
        Mostrar("Menu");
    }

    void EntrarInvitado() { PlayerPrefs.SetString("usuario", "Invitado"); PlayerPrefs.SetInt("sesion", 1); PlayerPrefs.Save(); Mostrar("Menu"); }
    void CrearCuenta() { Entrar(); }
    void MostrarPerfil() { if (menu != null) { Label label = menu.rootVisualElement.Q<Label>("NombrePaciente"); if (label != null) label.text = PlayerPrefs.GetString("usuario", "Paciente 001") + " | Sesión activa"; } }

    void SeleccionarNivel(int nivel, VisualElement root)
    {
        PlayerPrefs.SetInt("nivel", nivel); PlayerPrefs.Save(); ActualizarBotonesNivel(root, nivel);
    }

    void SeleccionarNivelGuardado(VisualElement root) { ActualizarBotonesNivel(root, PlayerPrefs.GetInt("nivel", 1)); }

    void ActualizarBotonesNivel(VisualElement root, int nivel)
    {
        for (int i = 1; i <= 3; i++)
        {
            Button boton = root.Q<Button>("BotonNivel" + i);
            if (boton != null) boton.EnableInClassList("nivel-activo", i == nivel);
        }
    }

    void ConfigurarTutorial(VisualElement root)
    {
        Toggle toggle = root.Q<Toggle>("ToggleCompletado");
        if (toggle == null) return;
        toggle.value = PlayerPrefs.GetInt("tutorial_completado", 0) == 1;
        toggle.RegisterValueChangedCallback(e => { PlayerPrefs.SetInt("tutorial_completado", e.newValue ? 1 : 0); PlayerPrefs.Save(); });
    }

    void ConfigurarAjustes(VisualElement root)
    {
        Button graficos = root.Q<Button>("GraficosVidBoton");
        Button audio = root.Q<Button>("AudioBoton");
        Button controles = root.Q<Button>("ControlesBoton");
        Button accesibilidad = root.Q<Button>("AccesBoton");
        VisualElement panelGrafico = root.Q<VisualElement>("PanelGraficos");

        Button[] pestañas = { graficos, audio, controles, accesibilidad };
        foreach (Button pestaña in pestañas)
        {
            if (pestaña == null) continue;
            pestaña.clicked += () => CambiarPestanaAjustes(pestaña, pestañas, panelGrafico, root);
        }

        Slider volumen = root.Q<Slider>("SliderVolumen");
        if (volumen != null)
        {
            volumen.value = PlayerPrefs.GetFloat("volumen", 1f);
            volumen.RegisterValueChangedCallback(e => { AudioListener.volume = e.newValue; PlayerPrefs.SetFloat("volumen", e.newValue); PlayerPrefs.Save(); });
        }

        ConfigurarSlider(root, "BrilloSlider", 42f, 0f, 100f, "brillo", value => RenderSettings.ambientIntensity = Mathf.Lerp(0.35f, 1.5f, value / 100f));
        ConfigurarSlider(root, "ContrasSlider", 42f, 0f, 100f, "contraste", null);
        ConfigurarSlider(root, "GammaSlider", 42f, 0f, 100f, "gamma", null);

        Toggle bajos = root.Q<Toggle>("ToggleBajos");
        if (bajos != null)
        {
            bajos.value = PlayerPrefs.GetInt("calidad_baja", 0) == 1;
            bajos.RegisterValueChangedCallback(e => { QualitySettings.SetQualityLevel(e.newValue ? 0 : Mathf.Clamp(QualitySettings.GetQualityLevel(), 1, Mathf.Max(1, QualitySettings.names.Length - 1))); PlayerPrefs.SetInt("calidad_baja", e.newValue ? 1 : 0); PlayerPrefs.Save(); });
        }

        CannonController cannon = mundoJuego != null ? mundoJuego.GetComponentInChildren<CannonController>(true) : null;
        Slider suavizado = root.Q<Slider>("SuavizadoSensor");
        Slider zona = root.Q<Slider>("ZonaMuertaSensor");
        if (suavizado == null && root.Q<VisualElement>("PanelGraficos") != null)
        {
            suavizado = new Slider("Suavizado sensor", 1f, 100f) { name = "SuavizadoSensor", value = 12f }; zona = new Slider("Zona muerta", 0f, 20f) { name = "ZonaMuertaSensor", value = 3f }; root.Q<VisualElement>("PanelGraficos").Add(suavizado); root.Q<VisualElement>("PanelGraficos").Add(zona);
        }
        if (cannon != null && suavizado != null)
        {
            suavizado.value = PlayerPrefs.GetFloat("suavizado_sensor", cannon.suavizadoMovimiento * 100f);
            suavizado.RegisterValueChangedCallback(e => { cannon.suavizadoMovimiento = Mathf.Clamp(e.newValue / 100f, 0.01f, 1f); PlayerPrefs.SetFloat("suavizado_sensor", e.newValue); PlayerPrefs.Save(); });
        }
        if (cannon != null && zona != null)
        {
            zona.value = PlayerPrefs.GetFloat("zona_muerta_sensor", cannon.zonaMuertaYaw);
            zona.RegisterValueChangedCallback(e => { cannon.zonaMuertaPitch = e.newValue; cannon.zonaMuertaYaw = e.newValue; PlayerPrefs.SetFloat("zona_muerta_sensor", e.newValue); PlayerPrefs.Save(); });
        }
    }

    void CambiarPestanaAjustes(Button activa, Button[] pestañas, VisualElement panelGrafico, VisualElement root)
    {
        foreach (Button pestaña in pestañas)
        {
            if (pestaña != null)
                pestaña.EnableInClassList("pestana-activa", pestaña == activa);
        }

        if (panelGrafico == null) return;
        VisualElement informacion = root.Q<VisualElement>("PanelPestanaDinamico");
        if (informacion == null)
        {
            informacion = new VisualElement { name = "PanelPestanaDinamico" };
            root.Add(informacion);
        }
        informacion.Clear();

        if (activa.name == "GraficosVidBoton" || activa.name == "AccesBoton")
        {
            informacion.style.display = DisplayStyle.None;
            panelGrafico.style.display = DisplayStyle.Flex;
            return;
        }

        panelGrafico.style.display = DisplayStyle.None;
        informacion.style.display = DisplayStyle.Flex;
        if (activa.name == "AudioBoton")
        {
            Slider volumen = new Slider("Volumen maestro", 0f, 1f) { value = PlayerPrefs.GetFloat("volumen", 1f) };
            volumen.RegisterValueChangedCallback(e => { AudioListener.volume = e.newValue; PlayerPrefs.SetFloat("volumen", e.newValue); PlayerPrefs.Save(); });
            informacion.Add(volumen);
        }
        else if (activa.name == "ControlesBoton")
        {
            informacion.Add(new Label("Movimiento alternativo: W A S D"));
            informacion.Add(new Label("Disparo alternativo: Barra espaciadora"));
            informacion.Add(new Label("Aumentar fuerza: Flecha arriba"));
            informacion.Add(new Label("Reducir fuerza: Flecha abajo"));
            Button calibrar = new Button(() => Debug.Log("Calibración activa: mantén el ESP32 quieto durante seis segundos.")) { text = "Calibrar orientación" };
            informacion.Add(calibrar);
        }
    }

    void ConfigurarSlider(VisualElement root, string nombre, float defecto, float minimo, float maximo, string clave, System.Action<float> aplicar)
    {
        Slider slider = root.Q<Slider>(nombre);
        if (slider == null) return;
        slider.lowValue = minimo; slider.highValue = maximo;
        slider.value = PlayerPrefs.GetFloat(clave, defecto);
        if (aplicar != null) aplicar(slider.value);
        slider.RegisterValueChangedCallback(e => { if (aplicar != null) aplicar(e.newValue); PlayerPrefs.SetFloat(clave, e.newValue); PlayerPrefs.Save(); });
    }

    void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
