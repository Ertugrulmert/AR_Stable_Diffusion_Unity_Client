using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dummiesman;

public class LoadObject : MonoBehaviour
{
    public GameObject meshObject;
    private bool meshLoaded;


    // Start is called before the first frame update
    void Start()
    {
        meshLoaded = false;

    }

    public bool isMeshLoaded() { 
        return meshLoaded;
    }


    public GameObject getObject()
    {
        return meshObject;
    }

    // Update is called once per frame
    //void Update()
    //{
    //
    //}

    // public void LoadGameObject(string filepath)
    public async void LoadGameObject(string mesh_filepath, string mat_filepath)
    {

        meshObject = new OBJLoader().Load(mesh_filepath, mat_filepath);
        //meshObject.transform.localScale = new Vector3(-1, 1, 1); // set the position of parent model. Reverse X to show properly 
        //FitOnScreen();
        DoublicateFaces();
        meshLoaded = true;
    }


    public void DoublicateFaces()
    {
        for (int i = 0; i < meshObject.GetComponentsInChildren<Renderer>().Length; i++) //Loop through the model children
        {
            // Get oringal mesh components: vertices, normals triangles and texture coordinates 
            Mesh mesh = meshObject.GetComponentsInChildren<MeshFilter>()[i].mesh;
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

}
