using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectLoader : MonoBehaviour
{

    public string directoryPath;
    public bool isLoaded;
    public bool isRendered;
    FileReader.ObjectFile obj;
    FileReader.MaterialFile mtl;

    MeshFilter filter;
    MeshRenderer renderer;
    Mesh mesh;
    Material[] materials;

    public bool getLoaded()
    {
        //return isLoaded;
        return isRendered;
    }

    void Start()
    {
        directoryPath = Application.persistentDataPath + "/";
        isRendered = false;

    }

    void Awake()
    {
        directoryPath = Application.persistentDataPath + "/";
        isLoaded = true;
    }

    public void Load(string mesh_filepath, string mat_filepath)
    {
        directoryPath = Application.persistentDataPath + "/";
        //if (!isLoaded)
        //    return;

        //directoryPath = path;
        StartCoroutine(ConstructModel(mesh_filepath, mat_filepath));
    }

    IEnumerator ConstructModel(string mesh_filepath, string mat_filepath)
    {

        isLoaded = false;

        obj = FileReader.ReadObjectFile(mesh_filepath);
        mtl = FileReader.ReadMaterialFile(mat_filepath);

        if (!gameObject.TryGetComponent<MeshFilter>(out filter)) { 
            filter = gameObject.AddComponent<MeshFilter>();
            mesh = new Mesh();
            //PopulateMesh(obj, mesh);
        }
        else
        {
            filter.mesh.Clear();
            mesh = filter.mesh;
            //PopulateMesh(obj, filter.mesh);
        }

        if (!gameObject.TryGetComponent<MeshRenderer>(out renderer))
        {
            renderer = gameObject.AddComponent<MeshRenderer>();
        }

        PopulateMesh();

        //DoublicateFaces();
        DefineMaterial();

        isLoaded = true;
        isRendered = true;
        yield return null;
    }

    void PopulateMesh()
    {

        //Mesh mesh = new Mesh();
        List<int[]> triplets = new List<int[]>();
        List<int> submeshes = new List<int>();

        Debug.Log($"Inside PopulateMesh {obj.f.Count}");

        for (int i = 0; i < obj.f.Count; i += 1)
        {
            for (int j = 0; j < obj.f[i].Count; j += 1)
            {
                triplets.Add(obj.f[i][j]);
            }
            submeshes.Add(obj.f[i].Count);
        }

        Vector3[] vertices = new Vector3[triplets.Count];
        Vector3[] normals = new Vector3[triplets.Count];
        Vector2[] uvs = new Vector2[triplets.Count];

        //Color[] colors = new Color[triplets.Count];

        for (int i = 0; i < triplets.Count; i += 1)
        {
            vertices[i] = obj.v[triplets[i][0] - 1];
            //colors[i] = obj.color[triplets[i][0] - 1];

            normals[i] = obj.vn[triplets[i][2] - 1];
            if (triplets[i][1] > 0)
                uvs[i] = obj.vt[triplets[i][1] - 1];
        }

        mesh.name = "dummy"; // obj.o;
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        //mesh.colors = colors;
        mesh.subMeshCount = submeshes.Count;

        int vertex = 0;
        for (int i = 0; i < submeshes.Count; i += 1)
        {
            int[] triangles = new int[submeshes[i]];
            for (int j = 0; j < submeshes[i]; j += 1)
            {
                triangles[j] = vertex;
                vertex += 1;
            }
            mesh.SetTriangles(triangles, i);
        }

        mesh.RecalculateBounds();
        mesh.Optimize();

        filter.mesh = mesh;
    }

    void DefineMaterial()
    {
        Debug.Log($"Inside DefineMaterial {obj.usemtl.Count}");

        materials = new Material[obj.usemtl.Count];

        for (int i = 0; i < 1; i += 1) //obj.usemtl.Count; i += 1)
        {

            int index = mtl.newmtl.IndexOf(obj.usemtl[i]);

            Texture2D texture = new Texture2D(1, 1);
            texture.LoadImage(File.ReadAllBytes(directoryPath + mtl.mapKd[index]));

            materials[i] = new Material(Shader.Find("Mobile/Unlit (Supports Lightmap)"));  //Shader.Find("Diffuse"));
            materials[i].name = mtl.newmtl[index];
            materials[i].mainTexture = texture;
        }

        renderer.materials = materials;
    }

    // We no longer use this funciton for rendering, it is kept for future reference.
    public void DoublicateFaces()
    {
            // Get oringal mesh components: vertices, normals triangles and texture coordinates 
            Vector3[] vertices = mesh.vertices;
            int numOfVertices = vertices.Length;
            Vector3[] normals = mesh.normals;
            int[] triangles = mesh.triangles;
            int numOfTriangles = triangles.Length;
            Vector2[] textureCoordinates = mesh.uv;
            if (textureCoordinates.Length < numOfTriangles) //Check if mesh doesn't have texture coordinates 
            {
                textureCoordinates = new Vector2[numOfVertices * 2];
            }

            // Create a new mesh component, double the size of the original 
            Vector3[] newVertices = new Vector3[numOfVertices * 2];
            Vector3[] newNormals = new Vector3[numOfVertices * 2];
            int[] newTriangle = new int[numOfTriangles * 2];
            Vector2[] newTextureCoordinates = new Vector2[numOfVertices * 2];

            for (int j = 0; j < numOfVertices; j++)
            {
                newVertices[j] = newVertices[j + numOfVertices] = vertices[j]; //Copy original vertices to make the second half of the mew vertices array
                newTextureCoordinates[j] = newTextureCoordinates[j + numOfVertices] = textureCoordinates[j]; //Copy original texture coordinates to make the second half of the mew texture coordinates array  
                newNormals[j] = normals[j]; //First half of the new normals array is a copy original normals
                newNormals[j + numOfVertices] = -normals[j]; //Second half of the new normals array reverse the original normals
            }

            for (int x = 0; x < numOfTriangles; x += 3)
            {
                // copy the original triangle for the first half of array
                newTriangle[x] = triangles[x];
                newTriangle[x + 1] = triangles[x + 1];
                newTriangle[x + 2] = triangles[x + 2];
                // Reversed triangles for the second half of array
                int j = x + numOfTriangles;
                newTriangle[j] = triangles[x] + numOfVertices;
                newTriangle[j + 2] = triangles[x + 1] + numOfVertices;
                newTriangle[j + 1] = triangles[x + 2] + numOfVertices;
            }
            mesh.vertices = newVertices;
            mesh.uv = newTextureCoordinates;
            mesh.normals = newNormals;
            mesh.triangles = newTriangle;
    }
}