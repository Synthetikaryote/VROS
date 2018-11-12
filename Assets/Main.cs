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
        await this.LoadDirectory(this.directoryPath);
    }

    // Update is called once per frame
    void Update()
    {

    }

    async Task LoadDirectory(string directory)
    {
        var files = Directory.GetFiles(directory).ToList();
        var count = files.Count;
        var rows = 4;
        var rowHeight = 0.6f;
        var width = 0.4f;
        await Task.WhenAll(files.Select(async (path, i) =>
        {
            var texture = await LoadTextureFromPath(path);
            var scale = width / texture.width;
            var halfCount = count / 2;
            var sideIndex = i % halfCount;
            var columns = halfCount / rows;
            var column = sideIndex % columns;
            var row = sideIndex / columns;
            var position = new Vector3((1f + row * 0.01f) * (i < halfCount ? -1f : 1f), 0.7f + rows * 0.5f * rowHeight - row * rowHeight, column * width);
            var angles = new Vector3(0f, i < halfCount ? -90f : 90f, 0f);
            CreateImage(texture, Path.GetFileName(path), position, angles, scale);
        }));
    }

    async Task<Texture2D> LoadTextureFromPath(string path)
    {
        var www = UnityWebRequestTexture.GetTexture($"file://{path}");
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
        return downloadHandler.texture;
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
