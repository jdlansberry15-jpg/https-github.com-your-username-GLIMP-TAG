using UnityEngine;

/// <summary>
/// Creates a simple original forest practice arena from built-in primitives.
/// Attach to an empty GameObject and press Play in a configured Unity scene.
/// </summary>
public class GlimpTagForestArena : MonoBehaviour
{
    public int treeCount = 14;
    public float arenaRadius = 22f;
    public int randomSeed = 17;

    private void Start()
    {
        Random.InitState(randomSeed);
        BuildGround();
        BuildCentralTreehouse();
        BuildTrees();
        BuildRockBoundary();
        BuildCaveMarker();
    }

    private void BuildGround()
    {
        CreatePrimitive("Forest Floor", PrimitiveType.Cylinder,
            new Vector3(0f, -0.55f, 0f),
            new Vector3(arenaRadius * 2f, 0.5f, arenaRadius * 2f),
            new Color(0.22f, 0.34f, 0.18f));
    }

    private void BuildCentralTreehouse()
    {
        CreatePrimitive("Treehouse Trunk", PrimitiveType.Cylinder,
            new Vector3(0f, 3f, 0f), new Vector3(1.5f, 6f, 1.5f),
            new Color(0.30f, 0.18f, 0.09f));

        CreatePrimitive("Treehouse Platform", PrimitiveType.Cube,
            new Vector3(0f, 5.5f, 0f), new Vector3(7f, 0.45f, 5f),
            new Color(0.42f, 0.25f, 0.12f));

        CreatePrimitive("Lookout Roof", PrimitiveType.Cube,
            new Vector3(0f, 8f, 0f), new Vector3(5f, 0.35f, 4f),
            new Color(0.24f, 0.16f, 0.10f));

        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f * Mathf.Deg2Rad;
            Vector3 start = new Vector3(Mathf.Cos(angle) * 1.1f, 3.2f, Mathf.Sin(angle) * 1.1f);
            Vector3 end = new Vector3(Mathf.Cos(angle) * 4f, 5.2f, Mathf.Sin(angle) * 3f);
            CreateBeam("Treehouse Branch", start, end, 0.35f,
                new Color(0.32f, 0.19f, 0.09f));
        }
    }

    private void BuildTrees()
    {
        for (int i = 0; i < treeCount; i++)
        {
            Vector2 circle = Random.insideUnitCircle.normalized * Random.Range(9f, arenaRadius - 3f);
            float height = Random.Range(5f, 9f);
            Vector3 basePosition = new Vector3(circle.x, 0f, circle.y);

            CreatePrimitive("Forest Tree Trunk", PrimitiveType.Cylinder,
                basePosition + Vector3.up * (height * 0.5f),
                new Vector3(Random.Range(0.7f, 1.2f), height * 0.5f, Random.Range(0.7f, 1.2f)),
                new Color(0.29f, 0.17f, 0.08f));

            CreatePrimitive("Forest Canopy", PrimitiveType.Sphere,
                basePosition + Vector3.up * (height + 1.2f),
                Vector3.one * Random.Range(3f, 4.5f),
                new Color(0.12f, 0.32f, 0.14f));

            CreateBeam("Climbable Branch", basePosition + Vector3.up * (height * 0.55f),
                basePosition + new Vector3(Random.Range(-3f, 3f), height * 0.8f, Random.Range(-3f, 3f)),
                0.3f, new Color(0.32f, 0.20f, 0.10f));
        }
    }

    private void BuildRockBoundary()
    {
        for (int i = 0; i < 24; i++)
        {
            float angle = i * Mathf.PI * 2f / 24f;
            Vector3 position = new Vector3(Mathf.Cos(angle) * arenaRadius, 1.2f,
                Mathf.Sin(angle) * arenaRadius);
            GameObject rock = CreatePrimitive("Boundary Rock", PrimitiveType.Sphere,
                position, new Vector3(3f, 2.5f, 3f), new Color(0.30f, 0.32f, 0.30f));
            rock.transform.rotation = Random.rotation;
        }
    }

    private void BuildCaveMarker()
    {
        CreatePrimitive("Cave Entrance", PrimitiveType.Cube,
            new Vector3(0f, 1.5f, arenaRadius - 1.5f),
            new Vector3(7f, 3f, 2f), new Color(0.12f, 0.12f, 0.13f));
        CreatePrimitive("Cave Opening", PrimitiveType.Cube,
            new Vector3(0f, 1.1f, arenaRadius - 0.35f),
            new Vector3(3f, 2.2f, 0.3f), new Color(0.04f, 0.04f, 0.05f));
    }

    private GameObject CreatePrimitive(string objectName, PrimitiveType type,
        Vector3 position, Vector3 scale, Color color)
    {
        GameObject item = GameObject.CreatePrimitive(type);
        item.name = objectName;
        item.transform.SetParent(transform);
        item.transform.position = position;
        item.transform.localScale = scale;

        Renderer renderer = item.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = color;
        }
        return item;
    }

    private void CreateBeam(string objectName, Vector3 start, Vector3 end,
        float thickness, Color color)
    {
        Vector3 direction = end - start;
        GameObject beam = CreatePrimitive(objectName, PrimitiveType.Cube,
            (start + end) * 0.5f,
            new Vector3(thickness, thickness, direction.magnitude), color);
        beam.transform.rotation = Quaternion.LookRotation(direction.normalized);
    }
}
