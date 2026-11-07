using UnityEngine;
using UnityEngine.InputSystem;
using System.IO.Ports;
using System.Globalization;

public class CannonController : MonoBehaviour
{
    public Transform cannonYaw;
    public Transform cannonPitch;
    public Transform projectileSpawn;

    public GameObject projectilePrefab;

    public float shootForce = 20f;

    public float forceStep = 5f;
    public float minShootForce = 0f;
    public float maxShootForce = 100f;

    public float yawSensitivity = 1f;
    public float pitchSensitivity = 1f;

    public float minPitch = -20f;
    public float maxPitch = 45f;

    public AudioClip shootSound;

    [Header("Conexión ESP32")]
    public string serialPortName = "COM6";
    public bool intentarPuertosAutomaticamente = true;
    public int baudRate = 115200;

    [Header("EMG y accesibilidad")]
    public int umbralEMG = 4000;
    public float tiempoEntreDisparos = 0.5f;
    public bool permitirDisparoTeclado = true;

    [Header("Suavizado")]
    [Range(0.01f, 1f)] public float suavizadoMovimiento = 0.12f;
    public float zonaMuertaPitch = 2f;
    public float zonaMuertaYaw = 3f;
    public float sensibilidadTeclado = 20f;

    private const string PUERTO_COMPATIBLE = "COM6";
    private const int BAUDRATE = 115200;


    private int valorADC = 0;
    private float mpuPitch = 0f;
    private float mpuYaw = 0f;

    private bool emgListoParaDisparar = true;
    private float ultimoDisparo = -Mathf.Infinity;

    private AudioSource audioSource;
    private SerialPort serialPort;

    private float pitchSuavizado;
    private float yawSuavizado;

    public bool EstaConectado => serialPort != null && serialPort.IsOpen;
    public int ValorADC => valorADC;
    public float PitchActual => pitchSuavizado;
    public float YawActual => yawSuavizado;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        shootForce = PlayerPrefs.GetFloat("fuerza_disparo", shootForce);
        umbralEMG = PlayerPrefs.GetInt("umbral_emg", umbralEMG);
        tiempoEntreDisparos = PlayerPrefs.GetFloat("intervalo_disparo", tiempoEntreDisparos);
        suavizadoMovimiento = PlayerPrefs.GetFloat("suavizado_sensor", suavizadoMovimiento * 100f) / 100f;
        zonaMuertaYaw = PlayerPrefs.GetFloat("zona_muerta_sensor", zonaMuertaYaw);
        zonaMuertaPitch = zonaMuertaYaw;
        CrearNaveEspacial();
        ConectarESP32();
    }

    void CrearNaveEspacial()
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (renderer.name.ToLowerInvariant().Contains("cannon") || renderer.name.ToLowerInvariant().Contains("tank"))
                renderer.enabled = false;
        }

        Transform shipParent = cannonPitch != null ? cannonPitch : cannonYaw != null ? cannonYaw : transform;
        GameObject ship = new GameObject("NaveEspacial");
        ship.transform.SetParent(shipParent, false);
        ship.transform.localPosition = Vector3.zero;

        Material hull = CreateShipMaterial(new Color(0.16f, 0.24f, 0.38f), 0.45f, 0.78f);
        Material armor = CreateShipMaterial(new Color(0.72f, 0.82f, 0.92f), 0.65f, 0.88f);
        Material energy = CreateShipMaterial(new Color(0.05f, 0.55f, 1f), 0.25f, 0.95f, true);
        Material engine = CreateShipMaterial(new Color(0.18f, 0.85f, 1f), 0.05f, 0.92f, true);

        GameObject body = CreateShipPrimitive("Casco", ship.transform, PrimitiveType.Capsule, hull,
            new Vector3(0f, 0f, 0f), new Vector3(0.55f, 0.95f, 1.45f), new Vector3(90f, 0f, 0f));
        CreateShipPrimitive("Canopy", ship.transform, PrimitiveType.Sphere, energy,
            new Vector3(0f, 0.42f, 0.35f), new Vector3(0.58f, 0.38f, 0.72f));
        CreateShipPrimitive("AlaIzquierda", ship.transform, PrimitiveType.Cube, armor,
            new Vector3(-1.05f, 0f, -0.15f), new Vector3(1.35f, 0.07f, 0.58f));
        CreateShipPrimitive("AlaDerecha", ship.transform, PrimitiveType.Cube, armor,
            new Vector3(1.05f, 0f, -0.15f), new Vector3(1.35f, 0.07f, 0.58f));
        CreateShipPrimitive("MotorIzquierdo", ship.transform, PrimitiveType.Cylinder, engine,
            new Vector3(-0.42f, -0.05f, -1.05f), new Vector3(0.25f, 0.20f, 0.25f), new Vector3(90f, 0f, 0f));
        CreateShipPrimitive("MotorDerecho", ship.transform, PrimitiveType.Cylinder, engine,
            new Vector3(0.42f, -0.05f, -1.05f), new Vector3(0.25f, 0.20f, 0.25f), new Vector3(90f, 0f, 0f));

        GameObject spawn = new GameObject("ProjectileSpawn_Nave");
        spawn.transform.SetParent(ship.transform, false);
        spawn.transform.localPosition = new Vector3(0f, 0f, 1.55f);
        projectileSpawn = spawn.transform;

        Light navLight = new GameObject("LuzNave").AddComponent<Light>();
        navLight.transform.SetParent(ship.transform, false);
        navLight.type = LightType.Point;
        navLight.color = new Color(0.15f, 0.65f, 1f);
        navLight.range = 6f;
        navLight.intensity = 1.4f;
        navLight.transform.localPosition = new Vector3(0f, 0.25f, 0.8f);
    }

    private GameObject CreateShipPrimitive(string name, Transform parent, PrimitiveType primitive, Material material,
        Vector3 position, Vector3 scale, Vector3? rotation = null)
    {
        GameObject go = GameObject.CreatePrimitive(primitive);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        if (rotation.HasValue) go.transform.localRotation = Quaternion.Euler(rotation.Value);
        Collider collider = go.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null) renderer.material = material;
        return go;
    }

    private Material CreateShipMaterial(Color color, float metallic, float smoothness, bool emissive = false)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material material = new Material(shader);
        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (emissive && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 2.5f);
        }
        return material;
    }

    void ConectarESP32()
    {
        if (string.IsNullOrWhiteSpace(serialPortName))
            serialPortName = PUERTO_COMPATIBLE;

        string[] puertos = intentarPuertosAutomaticamente
            ? SerialPort.GetPortNames()
            : new[] { serialPortName };

        foreach (string puerto in puertos)
        {
            try
            {
                if (!string.Equals(puerto, serialPortName, System.StringComparison.OrdinalIgnoreCase)
                    && !intentarPuertosAutomaticamente)
                {
                    continue;
                }

                SerialPort candidato = new SerialPort(puerto, baudRate > 0 ? baudRate : BAUDRATE)
                {
                    ReadTimeout = 20,
                    NewLine = "\n"
                };

                candidato.Open();
                candidato.DiscardInBuffer();
                serialPort = candidato;
                serialPortName = puerto;

                Debug.Log("[FlexStrike] ESP32 conectado en " + puerto +
                          " a " + candidato.BaudRate + " baudios. Umbral EMG: " + umbralEMG);
                return;
            }
            catch (System.Exception)
            {
                // Se intenta el siguiente puerto disponible.
            }
        }

        Debug.LogWarning("[FlexStrike] No se encontró un ESP32. La navegación por hardware seguirá " +
                         "desconectada y el disparo por teclado puede activarse como acceso alternativo.");
    }

    void Update()
    {
        LeerESP32();

        float yawObjetivo = -mpuYaw * yawSensitivity;
        float pitchObjetivo = mpuPitch * pitchSensitivity;

        if (Keyboard.current != null && !EstaConectado)
        {
            float entradaYaw = 0f;
            float entradaPitch = 0f;
            if (Keyboard.current.aKey.isPressed) entradaYaw -= 1f;
            if (Keyboard.current.dKey.isPressed) entradaYaw += 1f;
            if (Keyboard.current.wKey.isPressed) entradaPitch += 1f;
            if (Keyboard.current.sKey.isPressed) entradaPitch -= 1f;
            yawObjetivo = Mathf.Lerp(yawObjetivo, entradaYaw * sensibilidadTeclado, entradaYaw == 0f ? 0f : 1f);
            pitchObjetivo = Mathf.Lerp(pitchObjetivo, entradaPitch * sensibilidadTeclado, entradaPitch == 0f ? 0f : 1f);
        }

        if (Mathf.Abs(yawObjetivo) < zonaMuertaYaw)
            yawObjetivo = 0f;

        if (Mathf.Abs(pitchObjetivo) < zonaMuertaPitch)
            pitchObjetivo = 0f;

        yawSuavizado = Mathf.Lerp(yawSuavizado, yawObjetivo, suavizadoMovimiento);
        pitchSuavizado = Mathf.Lerp(pitchSuavizado, pitchObjetivo, suavizadoMovimiento);

        if (cannonYaw != null)
        {
            cannonYaw.localRotation = Quaternion.Euler(0f, yawSuavizado, 0f);
        }

        pitchSuavizado = Mathf.Clamp(pitchSuavizado, minPitch, maxPitch);

        if (cannonPitch != null)
        {
            cannonPitch.localRotation = Quaternion.Euler(pitchSuavizado, 0f, 0f);
        }

        if (Keyboard.current != null)
        {
            if (
                Keyboard.current
                    .upArrowKey
                    .wasPressedThisFrame
            )
            {
                shootForce +=
                    forceStep;
            }

            if (
                Keyboard.current
                    .downArrowKey
                    .wasPressedThisFrame
            )
            {
                shootForce -=
                    forceStep;
            }

            shootForce =
                Mathf.Clamp(
                    shootForce,
                    minShootForce,
                    maxShootForce
                );
            PlayerPrefs.SetFloat("fuerza_disparo", shootForce);

            if (permitirDisparoTeclado &&
                Keyboard.current.spaceKey.wasPressedThisFrame &&
                Time.time - ultimoDisparo >= tiempoEntreDisparos)
            {
                Shoot();
                ultimoDisparo = Time.time;
            }
        }
    }

    void LeerESP32()
    {
        if (
            serialPort == null ||
            !serialPort.IsOpen
        )
        {
            return;
        }

        try
        {
            while (
                serialPort.BytesToRead > 0
            )
            {
                string linea;

                try
                {
                    linea =
                        serialPort.ReadLine();
                }
                catch (
                    System.TimeoutException
                )
                {
                    break;
                }

                linea =
                    linea.Trim();

                if (
                    string.IsNullOrEmpty(linea)
                )
                {
                    continue;
                }

                string[] partes =
                    linea.Split('\t');

                if (
                    partes.Length >= 1
                )
                {
                    int adcTemporal;

                    if (
                        int.TryParse(
                            partes[0],
                            out adcTemporal
                        )
                    )
                    {
                        valorADC =
                            adcTemporal;
                    }
                }

                foreach (
                    string parte in partes
                )
                {
                    if (
                        parte.StartsWith(
                            "Pitch:"
                        )
                    )
                    {
                        string valor =
                            parte.Substring(6);

                        float.TryParse(
                            valor,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out mpuPitch
                        );
                    }
                    else if (
                        parte.StartsWith(
                            "Yaw:"
                        )
                    )
                    {
                        string valor =
                            parte.Substring(4);

                        float.TryParse(
                            valor,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out mpuYaw
                        );
                    }
                }

                if (
                    valorADC <
                    umbralEMG
                )
                {
                    emgListoParaDisparar =
                        true;
                }

                if (
                    valorADC >=
                    umbralEMG
                    &&
                    emgListoParaDisparar
                    &&
                    Time.time -
                    ultimoDisparo
                    >=
                    tiempoEntreDisparos
                )
                {
                    Shoot();

                    ultimoDisparo =
                        Time.time;

                    emgListoParaDisparar =
                        false;

                    Debug.Log("[FlexStrike] Disparo por EMG. ADC=" + valorADC);
                }
            }
        }
        catch (
            System.Exception e
        )
        {
            Debug.LogWarning(
                "Error leyendo ESP32: " +
                e.Message
            );
        }
    }

    public void Shoot()
    {
        if (
            projectilePrefab == null
        )
        {
            Debug.LogWarning(
                "No has asignado projectilePrefab."
            );

            return;
        }

        if (
            projectileSpawn == null
        )
        {
            Debug.LogWarning(
                "No has asignado projectileSpawn."
            );

            return;
        }

        GameObject projectile =
            Instantiate(
                projectilePrefab,
                projectileSpawn.position,
                projectileSpawn.rotation
            );
        projectile.name = "Projectile";
        Destroy(projectile, 8f);

        Rigidbody rb =
            projectile.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.AddForce(
                projectileSpawn.forward *
                shootForce,
                ForceMode.Impulse
            );
        }
        else
        {
            Debug.LogWarning(
                "El proyectil no tiene Rigidbody."
            );
        }

        if (
            audioSource != null &&
            shootSound != null
        )
        {
            audioSource.PlayOneShot(
                shootSound
            );
        }

        Debug.Log(
            "¡¡ DISPARO !! Fuerza: " +
            shootForce.ToString("F1")
        );
    }

    void OnDestroy()
    {
        if (
            serialPort != null &&
            serialPort.IsOpen
        )
        {
            try
            {
                serialPort.Close();

                Debug.Log(
                    "Puerto serial cerrado."
                );
            }
            catch
            {
            }
        }
    }
}

