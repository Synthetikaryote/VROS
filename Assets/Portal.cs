using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

public class Portal : MonoBehaviour {
    public TextMeshPro textMeshPro;
    public MeshRenderer highlight;

    public string Text
    {
        get { return textMeshPro.text; }
        set { textMeshPro.text = value; }
    }

    public bool Highlight
    {
        get { return highlight.enabled; }
        set { highlight.enabled = value; }
    }

    public string FilePath;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
