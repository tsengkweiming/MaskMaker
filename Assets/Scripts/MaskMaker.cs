using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

[System.Serializable]
public enum Shape
{
    Rect,
    Triangle
}

[System.Serializable]
public struct ColorData
{
    public Vector2 Vertex1;
    public Vector2 Vertex2;
    public Vector2 Vertex3;
    [Range(0, 1)] public float Alpha;
    [Range(0, 1)] public float BlackLevel;
    [Range(0, 1)] public float WhiteLevel;
}

[System.Serializable]
public class ShapeParameters
{
    public Shape Shape;
    public ColorData ColorData;
}

[System.Serializable]
public class MaskMakerParameters
{
    public bool Enable;
    public List<ShapeParameters> ShapeParameters;
    [Range(0, 1)] public float Invert;
}

[RequireComponent(typeof(Camera))]
public class MaskMaker : MonoBehaviour
{
    [SerializeField] private Shader _maskShader;
    [SerializeField] private MaskMakerParameters _params;
    private Material _maskMaterial;
    private GraphicsBuffer _rectBuffer;
    private GraphicsBuffer _triBuffer;

    public MaskMakerParameters Params { get => _params; set => _params = value; }

    // Start is called before the first frame update
    void Start()
    {
        _maskMaterial = new Material(_maskShader);
        UpdateData();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateData();
    }

    private void UpdateData()
    {
        var rectList = _params.ShapeParameters.Where(p => p.Shape == Shape.Rect).ToList();
        var triList = _params.ShapeParameters.Where(p => p.Shape == Shape.Triangle).ToList();
        SetBuffer(ref _rectBuffer, rectList, "RectMaskBuffer");
        SetBuffer(ref _triBuffer, triList, "TriMaskBuffer");
    }

    void SetBuffer(ref GraphicsBuffer buffer, List<ShapeParameters> shapes, string name = "")
    {
        if (buffer?.count != shapes.Count)
        {
            buffer?.Release();
            buffer = null;
            if (shapes.Count > 0)
            {
                buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, shapes.Count, Marshal.SizeOf(typeof(ColorData)));
                buffer.name = name;
            }
        }
        buffer?.SetData(shapes?.Select(l => l.ColorData)?.ToArray());
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!_params.Enable)
        {
            Graphics.Blit(source, destination);
            return;
        }

        _maskMaterial.SetInt("_RectShapeCount",  _rectBuffer?.count ?? 0);
        _maskMaterial.SetInt("_TriShapeCount", _triBuffer?.count ?? 0);
        _maskMaterial.SetFloat("_Invert", _params.Invert);
        _maskMaterial.SetBuffer("_RectShapeBuffer", _rectBuffer ?? null);
        _maskMaterial.SetBuffer("_TriShapeBuffer", _triBuffer ?? null);
        Graphics.Blit(null, destination, _maskMaterial);
    }

    private void OnDestroy()
    {
        _rectBuffer?.Release();
        _rectBuffer = null;
        _triBuffer?.Release();
        _triBuffer = null;

        if (Application.isEditor)
            Material.DestroyImmediate(_maskMaterial);
        else
            Material.Destroy(_maskMaterial);
        _maskMaterial = null;
    }
}