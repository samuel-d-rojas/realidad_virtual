using UnityEngine;

public class Asteroid : MonoBehaviour
{
    public float velocidad = 1.5f;
    public float radioGiro = 24f;

    private Vector3 destino;
    private Vector3 ejeGiro;
    private float velocidadGiro;
    private AsteroidSpawner gameManager;

    public void Inicializar(Vector3 target, AsteroidSpawner manager)
    {
        destino = target;
        gameManager = manager;
        ejeGiro = Random.onUnitSphere;
        velocidadGiro = Random.Range(12f, 30f);
    }

    void Update()
    {
        transform.Rotate(ejeGiro, velocidadGiro * Time.deltaTime, Space.World);

        if (destino != Vector3.zero)
            transform.position = Vector3.MoveTowards(transform.position, destino, velocidad * Time.deltaTime);

        if (Vector3.Distance(transform.position, destino) < 0.45f)
        {
            if (gameManager != null)
                gameManager.AsteroideNoAlcanzado();
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponentInParent<Asteroid>() != null)
            return;

        if (collision.gameObject.name.Contains("Projectile") ||
            collision.gameObject.CompareTag("Projectile"))
        {
            if (gameManager != null)
                gameManager.AsteroideDestruido(gameObject);
            CrearExplosion();
            Destroy(collision.gameObject);
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.name.Contains("Projectile") || other.CompareTag("Projectile"))
        {
            if (gameManager != null)
                gameManager.AsteroideDestruido(gameObject);
            CrearExplosion();
            Destroy(other.gameObject);
            Destroy(gameObject);
        }
    }

    private void CrearExplosion()
    {
        GameObject explosion = new GameObject("ExplosionAsteroide");
        explosion.transform.position = transform.position;
        ParticleSystem particles = explosion.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.28f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.62f, 0.18f), new Color(0.35f, 0.55f, 1f));
        main.maxParticles = 120;
        var emission = particles.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 45) });
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = Mathf.Max(0.15f, transform.lossyScale.x * 0.35f);
        ParticleSystemRenderer renderer = explosion.GetComponent<ParticleSystemRenderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
        renderer.material = new Material(shader);
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        Destroy(explosion, 1.2f);
    }
}
