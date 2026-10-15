using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour
{
    [Header("Arrastra aquí cada UIDocument")]
    [Header("Mundo 3D (se activa solo en Juego)")]
    public GameObject mundoJuego;
    public UIDocument inicioSesion;
    public UIDocument menu;
    public UIDocument juego;
    public UIDocument ajustes;
    public UIDocument tutorial;

    private Dictionary<string, UIDocument> pantallas;
    private Coroutine rutinaActual;

    

    void Start()
    {
        pantallas = new Dictionary<string, UIDocument>
        {
            { "InicioSesion", inicioSesion },
            { "Menu", menu },
            { "Juego", juego },
            { "Ajustes", ajustes },
            { "Tutorial", tutorial }
        };

        PlayerPrefs.DeleteKey("sesion"); // quita esta línea cuando termines de probar
        string inicial = PlayerPrefs.GetInt("sesion", 0) == 1 ? "Menu" : "InicioSesion";
        Mostrar(inicial);
    }


    public void Mostrar(string nombre)
    {
        if (rutinaActual != null) StopCoroutine(rutinaActual);
        rutinaActual = StartCoroutine(MostrarRutina(nombre));
    }

    IEnumerator MostrarRutina(string nombre)
    {
        Debug.Log("Mostrando: " + nombre);

        // 1. Apaga todas las pantallas
        foreach (var par in pantallas)
        {
            if (par.Value != null)
                par.Value.gameObject.SetActive(false);
        }

        // 2. Enciende solo la elegida
        if (!pantallas.ContainsKey(nombre) || pantallas[nombre] == null)
        {
            Debug.LogWarning("No existe o no está asignada la pantalla: " + nombre);
            yield break;
        }

        var doc = pantallas[nombre];
        doc.gameObject.SetActive(true);

        // 3. Espera un frame a que se construya la UI y conecta los botones
        yield return null;
        Conectar(doc, nombre);

        // El mundo 3D solo existe mientras estás en la pantalla Juego
        if (mundoJuego != null)
            mundoJuego.SetActive(nombre == "Juego");


        // El juego 3D corre solo en la pantalla Juego
        Time.timeScale = (nombre == "Juego") ? 1f : 0f;
    }

    void Conectar(UIDocument doc, string nombrePantalla)
    {
        var root = doc.rootVisualElement;

        root.Query<Button>().ForEach(boton =>
        {
            Debug.Log("Botón '" + boton.name + "' en " + nombrePantalla);

            if (boton.name.StartsWith("Ir_"))
            {
                string destino = boton.name.Substring(3);
                boton.clicked += () => Mostrar(destino);
            }
            else if (boton.name == "Entrar")
            {
                boton.clicked += () =>
                {
                    PlayerPrefs.SetInt("sesion", 1);
                    Mostrar("Menu");
                };
            }
            else if (boton.name == "Salir")
            {
                boton.clicked += Salir;
            }
        });

        var slider = root.Q<Slider>("SliderVolumen");

        if (slider != null)
        {
            slider.value = AudioListener.volume;
            slider.RegisterValueChangedCallback(e => AudioListener.volume = e.newValue);
        }
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