#include <Arduino.h>
#include <Wire.h>
#include <Adafruit_MPU6050.h>
#include <Adafruit_Sensor.h>
#include <math.h>

Adafruit_MPU6050 mpu;

float pitch = 0.0;
float yaw = 0.0;

float pitchInicial = 0.0;

unsigned long tiempoAnteriorMPU = 0;
unsigned long siguienteMPU = 0;

const unsigned long PERIODO_MPU_US = 10000;

const int EMG_PIN = 34;

const unsigned long PERIODO_EMG_US = 1000;

unsigned long siguienteMuestraEMG;

void setup()
{
    Serial.begin(115200);

    Wire.begin(21, 22);

    if (!mpu.begin())
    {
        Serial.println("ERROR_MPU");

        while (1)
        {
            delay(10);
        }
    }

    mpu.setAccelerometerRange(
        MPU6050_RANGE_2_G
    );

    mpu.setGyroRange(
        MPU6050_RANGE_250_DEG
    );

    analogReadResolution(12);

    analogSetPinAttenuation(
        EMG_PIN,
        ADC_11db
    );

    Serial.println("CALIBRANDO_MPU");

    delay(6000);

    sensors_event_t aceleracion;
    sensors_event_t giro;
    sensors_event_t temperatura;

    mpu.getEvent(
        &aceleracion,
        &giro,
        &temperatura
    );

    float Ax =
        aceleracion.acceleration.x;

    float Ay =
        aceleracion.acceleration.y;

    float Az =
        aceleracion.acceleration.z;

    pitchInicial =
        atan2(
            -Ax,
            sqrt(
                Ay * Ay +
                Az * Az
            )
        )
        * 180.0 / PI;

    pitch = 0.0;
    yaw = 0.0;

    tiempoAnteriorMPU = micros();

    siguienteMPU = micros();

    siguienteMuestraEMG = micros();

    Serial.println("CALIBRACION_TERMINADA");
}

void loop()
{
    unsigned long ahora = micros();

    if (
        (long)(
            ahora -
            siguienteMPU
        ) >= 0
    )
    {
        sensors_event_t aceleracion;
        sensors_event_t giro;
        sensors_event_t temperatura;

        mpu.getEvent(
            &aceleracion,
            &giro,
            &temperatura
        );

        float Ax =
            aceleracion.acceleration.x;

        float Ay =
            aceleracion.acceleration.y;

        float Az =
            aceleracion.acceleration.z;

        float pitchAcc =
            atan2(
                -Ax,
                sqrt(
                    Ay * Ay +
                    Az * Az
                )
            )
            * 180.0 / PI;

        pitchAcc -= pitchInicial;

        float Gy =
            giro.gyro.y *
            180.0 / PI;

        float Gz =
            giro.gyro.z *
            180.0 / PI;

        unsigned long tiempoActual =
            micros();

        float dt =
            (
                tiempoActual -
                tiempoAnteriorMPU
            )
            / 1000000.0;

        tiempoAnteriorMPU =
            tiempoActual;

        pitch +=
            Gy *
            dt;

        yaw +=
            Gz *
            dt;

        pitch =
            0.98 * pitch +
            0.02 * pitchAcc;

        siguienteMPU +=
            PERIODO_MPU_US;
    }

    ahora = micros();

    if (
        (long)(
            ahora -
            siguienteMuestraEMG
        ) >= 0
    )
    {
        int adc =
            analogRead(EMG_PIN);

        Serial.print(adc);

        Serial.print("\tPitch:");

        Serial.print(
            pitch,
            3
        );

        Serial.print("\tYaw:");

        Serial.println(
            yaw,
            3
        );

        siguienteMuestraEMG +=
            PERIODO_EMG_US;
    }
}