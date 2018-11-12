using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class Main : MonoBehaviour
{
    public string directoryPath;

    // Use this for initialization
    async void Start()
    {
        await this.LoadDirectory2(this.directoryPath);
    }

    // Update is called once per frame
    void Update()
    {

    }

    async Task LoadDirectory(string directory)
    {
        var files = Directory.GetFiles(directory).ToList();
        var pixelHeight = 8096;
        var minImageHeight = 1024;
        var maxImageHeight = 2048;
        var scale = 0.0004f;
        var z = 0;
        var y = pixelHeight;
        var colWidth = 0;
        foreach (var file in files)
        {
            var texture = await LoadTextureFromPath(file);
            if (texture == null)
            {
                continue;
            }

            // new column?
            if (y - texture.height < 0)
            {
                y = pixelHeight;
                z += colWidth;
                colWidth = 0;
            }
            var imageScale = 1f;
            if (texture.height > maxImageHeight)
                imageScale = maxImageHeight / (float)texture.height;
            if (texture.height < minImageHeight)
                imageScale = minImageHeight / (float)texture.height;
            var w = Mathf.FloorToInt(texture.width * imageScale);
            var h = Mathf.FloorToInt(texture.height * imageScale);
            CreateImage(texture, Path.GetFileName(file),
                new Vector3(-1f, 1f + (y - h / 2 - pixelHeight / 2) * scale, (z + w / 2) * scale),
                new Vector3(0f, -90f, 0f),
                imageScale * scale);
            colWidth = Mathf.Max(colWidth, w);
            y -= h;
        }
    }

    async Task LoadDirectory2(string directory)
    {
        var files = Directory.GetFiles(directory).ToList();
        var scale = 0.0005f;
        var xs = new float[16];
        foreach (var file in files)
        {
            var texture = await LoadTextureFromPath(file);

            var base2 = Mathf.RoundToInt(Mathf.Log(texture.height, 2f));
            var h = Mathf.FloorToInt(Mathf.Pow(2f, base2));
            var s = h / (float)texture.height;
            var w = texture.width * s;

            CreateImage(texture, Path.GetFileName(file),
                new Vector3(-1, (h + h / 2) * scale - 0.5f, (xs[base2] + w * 0.5f) * scale),
                new Vector3(0f, -90f, 0f), scale * s * 0.9f);
            xs[base2] += w;
        }

    }

    async Task<Texture2D> LoadTextureFromPath(string path)
    {
        path = Path.Combine(Path.GetDirectoryName(path), WWW.EscapeURL(Path.GetFileName(path)));
        var www = UnityWebRequestTexture.GetTexture($@"file://{path}");
        await www.SendWebRequest();
        if (www.isNetworkError)
        {
            Debug.LogError($"error loading {path}: {www.error}");
            return null;
        }
        var downloadHandler = www.downloadHandler as DownloadHandlerTexture;
        while (!downloadHandler.isDone)
        {
            await new WaitForSecondsRealtime(0.1f);
        }
        var texture = downloadHandler.texture;
        if (texture == null)
        {
            Debug.LogWarning($"couldn't load {path}");
        }
        return texture;
    }

    GameObject CreateImage(Texture2D texture, string name, Vector3 position, Vector3 eulerAngles, float scale)
    {
        var go = new GameObject(name);
        var meshFilter = go.AddComponent<MeshFilter>();
        
        var hw = texture.width * 0.5f;
        var hh = texture.height * 0.5f;
        var transform = go.GetComponent<Transform>();
        transform.position = position;
        transform.forward = Vector3.forward;
        transform.localScale = Vector3.one * scale;
        transform.localEulerAngles = eulerAngles;
        meshFilter.mesh = new Mesh()
        {
            vertices = new Vector3[]
            {
                new Vector3(-hw, -hh, 0),
                new Vector3(hw, -hh, 0),
                new Vector3(-hw, hh, 0),
                new Vector3(hw, hh, 0)
            },
            triangles = new int[] { 0, 2, 1, 2, 3, 1 },
            normals = new Vector3[]
            {
                -Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward
            },
            uv = new Vector2[]
            {
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1)
            }
        };
        var meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.material.shader = Shader.Find("Unlit/Texture");
        meshRenderer.material.SetTexture("_MainTex", texture);
        return go;
    }
}
