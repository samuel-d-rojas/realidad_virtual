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

    private const string PUERTO = "COM6";
    private const int BAUDRATE = 115200;

    private const int UMBRAL_EMG = 4000;
    private const float TIEMPO_ENTRE_DISPAROS = 0.5f;

    private int valorADC = 0;
    private float mpuPitch = 0f;
    private float mpuYaw = 0f;

    private bool emgListoParaDisparar = true;
    private float ultimoDisparo = -Mathf.Infinity;

    private AudioSource audioSource;
    private SerialPort serialPort;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        serialPort = new SerialPort(
            PUERTO,
            BAUDRATE
        );

        serialPort.ReadTimeout = 2;
        serialPort.NewLine = "\n";

        try
        {
            serialPort.Open();
            serialPort.DiscardInBuffer();

            Debug.Log("=================================");
            Debug.Log("ESP32 CONECTADO");
            Debug.Log("Puerto: " + PUERTO);
            Debug.Log("Baudrate: " + BAUDRATE);
            Debug.Log("Umbral EMG: " + UMBRAL_EMG);
            Debug.Log("=================================");
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                "No se pudo abrir " +
                PUERTO +
                ": " +
                e.Message
            );
        }
    }

    void Update()
    {
        LeerESP32();

        float yawRotation =
            -mpuYaw *
            yawSensitivity;

        if (cannonYaw != null)
        {
            cannonYaw.localRotation =
                Quaternion.Euler(
                    0f,
                    yawRotation,
                    0f
                );
        }

        float pitchRotation =
            mpuPitch *
            pitchSensitivity;

        pitchRotation =
            Mathf.Clamp(
                pitchRotation,
                minPitch,
                maxPitch
            );

        if (cannonPitch != null)
        {
            cannonPitch.localRotation =
                Quaternion.Euler(
                    pitchRotation,
                    0f,
                    0f
                );
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

                Debug.Log(
                    "ADC: " +
                    valorADC +
                    " | Pitch: " +
                    mpuPitch.ToString(
                        "F2",
                        CultureInfo.InvariantCulture
                    ) +
                    " | Yaw: " +
                    mpuYaw.ToString(
                        "F2",
                        CultureInfo.InvariantCulture
                    )
                );

                if (
                    valorADC <
                    UMBRAL_EMG
                )
                {
                    emgListoParaDisparar =
                        true;
                }

                if (
                    valorADC >=
                    UMBRAL_EMG
                    &&
                    emgListoParaDisparar
                    &&
                    Time.time -
                    ultimoDisparo
                    >=
                    TIEMPO_ENTRE_DISPAROS
                )
                {
                    Shoot();

                    ultimoDisparo =
                        Time.time;

                    emgListoParaDisparar =
                        false;

                    Debug.Log(
                        "================================="
                    );

                    Debug.Log(
                        "¡¡ DISPARO POR EMG !!"
                    );

                    Debug.Log(
                        "ADC = " +
                        valorADC
                    );

                    Debug.Log(
                        "UMBRAL = " +
                        UMBRAL_EMG
                    );

                    Debug.Log(
                        "================================="
                    );
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

    void Shoot()
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

        Rigidbody rb =
            projectile.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.AddForce(
                projectileSpawn.up *
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

