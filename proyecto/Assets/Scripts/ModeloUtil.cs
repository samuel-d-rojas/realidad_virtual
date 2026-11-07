using System.Collections.Generic;
using UnityEngine;

// Utilidades para colocar modelos importados (Maya/FBX) sin depender de su pivote, unidades o posición original.
public static class ModeloUtil
{
    // Caja que ocupan los renderers, medida en el espacio local de 'espacio'.
    public static bool Limites(Transform espacio, IEnumerable<Renderer> renderers, out Bounds limites)
    {
        limites = new Bounds();
        bool hay = false;
        foreach (Renderer renderer in renderers)
        {
            Bounds b = renderer.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 signo = new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f);
                Vector3 punto = espacio.InverseTransformPoint(b.center + Vector3.Scale(b.extents, signo));
                if (!hay)
                {
                    limites = new Bounds(punto, Vector3.zero);
                    hay = true;
                }
                else
                {
                    limites.Encapsulate(punto);
                }
            }
        }
        return hay;
    }

    // Escala el modelo para que su lado más largo mida 'tamano' y lo centra en el origen de su padre.
    public static void Encajar(Transform modelo, float tamano)
    {
        Transform contenedor = modelo.parent;
        if (!Limites(contenedor, modelo.GetComponentsInChildren<Renderer>(), out Bounds b))
            return;

        float mayor = Mathf.Max(b.size.x, b.size.y, b.size.z);
        if (mayor > 0.0001f)
            modelo.localScale *= tamano / mayor;

        Limites(contenedor, modelo.GetComponentsInChildren<Renderer>(), out b);
        modelo.localPosition -= b.center;
    }

    public static Transform BuscarHijo(Transform raiz, string nombre)
    {
        foreach (Transform hijo in raiz.GetComponentsInChildren<Transform>(true))
        {
            if (hijo.name == nombre)
                return hijo;
        }
        return null;
    }
}
