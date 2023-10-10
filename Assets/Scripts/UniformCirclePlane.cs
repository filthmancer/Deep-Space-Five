using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class UniformCirclePlane : MonoBehaviour
{

    public int resolution = 4;
    public GravityField Field;

    public static float GravityVisualResolution = -1000;
    public Material lineMaterial;
    private Mesh mesh;
    private Texture2D texture;

    // Use this for initialization
    void Start()
    {
        GetComponent<MeshFilter>().mesh = GenerateCircleMesh(resolution);
        GetComponent<MeshRenderer>().material.mainTexture = GenerateTexture();
    }



    // Update is called once per frame
    void Update()
    {

    }

    // Get the index of point number 'x' in circle number 'c'
    static int GetPointIndex(int c, int x)
    {
        if (c < 0) return 0; // In case of center point
        x = x % ((c + 1) * 6); // Make the point index circular
                               // Explanation: index = number of points in previous circles + central point + x
                               // hence: (0+1+2+...+c)*6+x+1 = ((c/2)*(c+1))*6+x+1 = 3*c*(c+1)+x+1

        return (3 * c * (c + 1) + x + 1);
    }

    public float CalculateGravityVisualDepth(int circleRange, out float ratio)
    {
        ratio = (float)circleRange / (float)resolution;
        ratio = Field.GravityCurve.Evaluate(ratio);
        return ratio * GravityVisualResolution;
    }



    public Mesh GenerateCircleMesh(int res)
    {

        float d = 1f / res;
        float uvScale = 2F;//-d * (res + 1);

        var vtc = new List<Vector3>();
        var uvs = new List<Vector2>();
        vtc.Add(new Vector3(0, 0, CalculateGravityVisualDepth(0, out float ratio))); // Start with only center point
        var initUV = new Vector2(1F, 1F);
        uvs.Add(initUV / uvScale);
        //uvs.Add((Vector2)vtc[0] / uvScale);
        var tris = new List<int>();
        float lastVisRatio = 1.0F;

        // First pass => build vertices
        for (int circ = 0; circ < res; ++circ)
        {
            float angleStep = (Mathf.PI * 2f) / ((circ + 1) * 6);

            float visDepth = CalculateGravityVisualDepth(circ, out float visRatio);

            // int subRings = (int)Mathf.Abs((visRatio - lastVisRatio) / 0.1F);
            // Debug.Log("SUBRINGS: " + subRings);
            // lastVisRatio = visRatio;
            // for (int sub = 0; sub < subRings; sub++)
            // {
            //     float subRes = d;
            for (int point = 0; point < (circ + 1) * 6; ++point)
            {
                vtc.Add(new Vector3(
                    Mathf.Cos(angleStep * point) * d * (circ + 1),
                    Mathf.Sin(angleStep * point) * d * (circ + 1),
                    visDepth));

                var vtcuv = (Vector2)vtc[vtc.Count - 1];
                uvs.Add((initUV + vtcuv) / uvScale);
            }
            // }

        }

        // Second pass => connect vertices into triangles
        for (int circ = 0; circ < res; ++circ)
        {
            for (int point = 0, other = 0; point < (circ + 1) * 6; ++point)
            {
                if (point % (circ + 1) != 0)
                {
                    // Create 2 triangles
                    tris.Add(GetPointIndex(circ - 1, other + 1));
                    tris.Add(GetPointIndex(circ - 1, other));
                    tris.Add(GetPointIndex(circ, point));
                    tris.Add(GetPointIndex(circ, point));
                    tris.Add(GetPointIndex(circ, point + 1));
                    tris.Add(GetPointIndex(circ - 1, other + 1));
                    ++other;
                }
                else
                {
                    // Create 1 inverse triangle
                    tris.Add(GetPointIndex(circ, point));
                    tris.Add(GetPointIndex(circ, point + 1));
                    tris.Add(GetPointIndex(circ - 1, other));
                    // Do not move to the next point in the smaller circle
                }
            }
        }

        // Create the mesh
        var m = new Mesh();
        m.SetVertices(vtc);
        m.SetTriangles(tris, 0);
        m.SetUVs(0, uvs);
        m.RecalculateNormals();
        m.UploadMeshData(false);

        mesh = m;
        return m;

    }


    public Color lineColor = new Color(1, 1, 1, 0.5F);
    public int textureSize = 512;

    private Vector2[] uvs;

    Texture2D GenerateTexture()
    {
        // Create the texture and set its pixel data to all transparent
        texture = new Texture2D(textureSize, textureSize, TextureFormat.ARGB32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        Color[] pixels = texture.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color(0, 0, 0, 0);
        }
        texture.SetPixels(pixels);
        texture.Apply();
        // Get the UVs of the mesh
        uvs = mesh.uv;
        var triangles = mesh.triangles;

        // Draw lines between each connected pair of vertices
        for (int i = 0; i < triangles.Length; i += 3)
        {
            DrawLine(uvs[triangles[i]], uvs[triangles[i + 1]], lineColor);
            DrawLine(uvs[triangles[i + 1]], uvs[triangles[i + 2]], lineColor);
            DrawLine(uvs[triangles[i + 2]], uvs[triangles[i]], lineColor);
        }

        // Draw lines between each pair of vertices
        // for (int i = 0; i < mesh.vertexCount; i++)
        // {
        //     if (i + 1 < mesh.vertexCount) DrawLine(uvs[i], uvs[i + 1], lineColor);
        //     // DrawLine(uvs[i], uvs[i + 2], lineColor);
        //     // for (int j = i + 1; j < mesh.vertexCount; j++)
        //     // {
        //     // }
        // }

        // Apply the changes to the texture
        texture.Apply();
        return texture;
    }

    private void DrawLine(Vector2 uv1, Vector2 uv2, Color color)
    {
        int x1 = (int)(uv1.x * textureSize);
        int y1 = (int)(uv1.y * textureSize);
        int x2 = (int)(uv2.x * textureSize);
        int y2 = (int)(uv2.y * textureSize);

        int dx = Mathf.Abs(x2 - x1);
        int dy = Mathf.Abs(y2 - y1);
        int sx = x1 < x2 ? 1 : -1;
        int sy = y1 < y2 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            texture.SetPixel(x1, y1, color);
            texture.SetPixel(x1 - 1, y1, color);
            texture.SetPixel(x1 + 1, y1, color);
            texture.SetPixel(x1, y1 - 1, color);
            texture.SetPixel(x1, y1 + 1, color);
            texture.SetPixel(x1 - 2, y1, color);
            texture.SetPixel(x1 + 2, y1, color);
            if (x1 == x2 && y1 == y2) break;
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x1 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y1 += sy;
            }
        }
    }
}