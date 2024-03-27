using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MaskMaker : MonoBehaviour
{
    public enum Type
    {
        Gradient,
        Step
    }
    public enum Shape
    {
        Rect,
        Triangle
    }

    [System.Serializable]
    public struct ColorData
    {
        public Vector2 Vertice1;
        public Vector2 Vertice2;
        public Vector2 Vertice3;
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
        public List<ShapeParameters> MaskParameters;
    }

    [SerializeField] private MaskMakerParameters _params;
    [SerializeField] private Shader _maskShader;
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
        var rectList = _params.MaskParameters.Where(p => p.Shape == Shape.Rect).ToList();
        SetBuffer(ref _rectBuffer, rectList, "RectMaskBuffer");

        var triList = _params.MaskParameters.Where(p => p.Shape == Shape.Triangle).ToList();
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
        _maskMaterial.SetBuffer("_RectShapeBuffer", _rectBuffer ?? null);
        _maskMaterial.SetBuffer("_TriShapeBuffer", _triBuffer ?? null);
        Graphics.Blit(null, destination, _maskMaterial);
    }

    private void OnDestroy()
    {
        _rectBuffer?.Release();
        _triBuffer?.Release();

        if (Application.isEditor)
            Material.DestroyImmediate(_maskMaterial);
        else
            Material.Destroy(_maskMaterial);
        _maskMaterial = null;
    }
}