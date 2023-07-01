/*
 * The .obj and .mtl files must follow Wavefront OBJ Specifications.
 *
 * OBJ Supported List
 *
 *   Vertex Data
 *     - v  Geometric vertices (not support w)
 *     - vt Texture vertices (not support w)
 *     - vn Vertex normals
 *
 *   Elements
 *     - f Face (only support triangulate faces)
 *
 *   Grouping
 *     - o Object name
 *
 *   Display
 *     - mtllib Material library (not support multiple files)
 *     - usemtl Material name
 *
 * MTL Supported List
 *
 *   Material Name
 *     - newmtl Material name
 *
 *   Texture Map
 *     - map_Kd Texture file is linked to the diffuse (not support options)
 */

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FileReader
{

    public struct ObjectFile
    {
        public string o;
        public string mtllib;
        public List<string> usemtl;
        public List<Vector3> v;
        //public List<Color> color;
        public List<Vector3> vn;
        public List<Vector2> vt;
        public List<List<int[]>> f;
    }

    public struct MaterialFile
    {
        public List<string> newmtl;
        public List<string> mapKd;
    }

    public static ObjectFile ReadObjectFile(string path)
    {

        ObjectFile obj = new ObjectFile();
        string[] lines = File.ReadAllLines(path);

        obj.usemtl = new List<string>();
        obj.v = new List<Vector3>();
        //obj.color = new List<Color>();
        obj.vn = new List<Vector3>();
        obj.vt = new List<Vector2>();
        obj.f = new List<List<int[]>>();

        foreach (string line in lines)
        {
            if (line == "" || line.StartsWith("#"))
                continue;

            //Debug.Log($"line: {line}");

            string[] token = line.Split(' ');
            switch (token[0])
            {

                case ("o"):
                    obj.o = token[1];
                    break;
                case ("mtllib"):
                    obj.mtllib = token[1];
                    break;
                case ("usemtl"):
                    obj.usemtl.Add(token[1]);
                    obj.f.Add(new List<int[]>());
                    break;
                case ("vn"):
                    obj.vn.Add(new Vector3(
                        float.Parse(token[1]),
                        float.Parse(token[2]),
                        float.Parse(token[3])));
                    break;
                case ("vt"):
                    obj.vt.Add(new Vector3(
                        float.Parse(token[1]),
                        float.Parse(token[2])));
                    break;
                case ("v"):
                    obj.v.Add(new Vector3(
                        float.Parse(token[1]),
                        float.Parse(token[2]),
                        float.Parse(token[3])));
                    //if (token.Length > 3)
                    //{
                    //    Debug.Log($"token.Length: {token.Length}, line: {line}");
                    //    obj.color.Add(new Color(
                    //        float.Parse(token[4]), // r
                    //        float.Parse(token[5]), // g
                    //        float.Parse(token[6]), // b
                    //        1));                   // a
                    //}
                    //else {
                    //    Debug.Log($"vertex no color? line: {line}");
                    //
                    //}
                    break;
                case ("f"):
                    for (int i = 1; i < 4; i += 1)
                    {
                        int[] triplet = Array.ConvertAll(token[i].Split('/'), x => {
                            if (String.IsNullOrEmpty(x))
                                return 0;
                            return int.Parse(x);
                        });
                        obj.f[obj.f.Count - 1].Add(triplet);
                    }
                    break;
            }
        }

        return obj;
    }

    public static MaterialFile ReadMaterialFile(string path)
    {
        MaterialFile mtl = new MaterialFile();
        string[] lines = File.ReadAllLines(path);

        mtl.newmtl = new List<string>();
        mtl.mapKd = new List<string>();

        foreach (string line in lines)
        {
            if (line == "" || line.StartsWith("#"))
                continue;

            string[] token = line.Split(' ');
            switch (token[0])
            {

                case ("newmtl"):
                    mtl.newmtl.Add(token[1]);
                    break;
                case ("map_Kd"):
                    mtl.mapKd.Add(token[1]);
                    break;
            }

        }

        return mtl;
    }
}
