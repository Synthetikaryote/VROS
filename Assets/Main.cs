using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class Main : MonoBehaviour
{
    public string directoryPath;
    public GameObject directoryPrefab;
    public Transform mosaic;
    public Transform cameraRigTransform;

    // Use this for initialization
    async void Start()
    {
        ResetPosition();
        await this.LoadDirectory(this.directoryPath);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ResetPosition()
    {
        cameraRigTransform.position = new Vector3(3f, 0f, 4f);
        mosaic.position = Vector3.zero;
    }

    public async Task LoadTopLevel()
    {
        CreatePortals(DriveInfo.GetDrives()
            .Select(x => new KeyValuePair<string, string>(x.ToString(), x.ToString())).ToList());
    }

    public async Task LoadDirectory(string directory)
    {
        directoryPath = directory;
        ClearMosaic();

        if (string.IsNullOrEmpty(directory))
        {
            await LoadTopLevel();
            return;
        }

        CreatePortals(new List<KeyValuePair<string, string>> {
            new KeyValuePair<string, string>(Directory.GetParent(directory)?.ToString(), "..")
            }.Concat(Directory.GetDirectories(directory)
                .Select(x => new KeyValuePair<string, string>(x, Path.GetFileName(x)))).ToList());

        var files = Directory.GetFiles(directory).ToList();
        var scale = 0.001f;
        var xs = new float[16];
        foreach (var file in files)
        {
            var texture = await LoadTextureFromPathAsync(file);
            if (!Application.isPlaying) break;
            if (directory != directoryPath) break;
            if (texture == null) continue;

            var base2 = Mathf.CeilToInt(Mathf.Log(texture.height, 2f));
            var h = Mathf.FloorToInt(Mathf.Pow(2f, base2));
            var s = h / (float)texture.height;
            var w = texture.width * s;

            CreateImage(texture, Path.GetFileName(file),
                new Vector3(-1, (h + h / 2) * scale + 1.5f, (xs[base2] + w * 0.5f) * scale),
                new Vector3(0f, -90f, 0f), scale * s * 0.9f);
            xs[base2] += w;
        }
    }

    void CreatePortals(List<KeyValuePair<string, string>> paths)
    {
        var eulerAngles = new Vector3(0f, -90f, 0f);
        for (int i = 0; i < paths.Count; ++i)
        {
            var go = GameObject.Instantiate(directoryPrefab, mosaic);
            var t = go.transform;
            t.localPosition = new Vector3(-1f, 1f, i * 1.25f + 0.5f);
            t.localEulerAngles = eulerAngles;
            var portal = go.GetComponent<Portal>();
            portal.FilePath = paths[i].Key;
            portal.Text = paths[i].Value;
        }
    }

    void ClearMosaic()
    {
        foreach (Transform transform in mosaic.transform)
        {
            Destroy(transform.gameObject);
        }
    }

    async Task<Texture2D> LoadTextureFromPathAsync(string filePath, bool escapePath = true)
    {
        string path = escapePath ? Path.Combine(Path.GetDirectoryName(filePath), WWW.EscapeURL(Path.GetFileName(filePath))) : filePath;
        var www = UnityWebRequestTexture.GetTexture($@"file://{path}");
        await www.SendWebRequest();

        if (www.isHttpError && www.responseCode == 404 && escapePath)
        {
            return await LoadTextureFromPathAsync(filePath, false);
        }

        if (www.error != null)
        {
            Debug.LogError($"Error loading {path}: {www.error}");
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
        transform.parent = mosaic;
        transform.localPosition = position;
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
