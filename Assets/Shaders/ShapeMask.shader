Shader "Hidden/ShapeMask"
{
    Properties
    {}
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            struct ColorData
            {
                float2 Vertex1;
                float2 Vertex2;
                float2 Vertex3;
                float Alpha;
                float BlackLevel;
                float WhiteLevel;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            StructuredBuffer<ColorData> _RectShapeBuffer;
            StructuredBuffer<ColorData> _TriShapeBuffer;
            int _RectShapeCount;
            int _TriShapeCount;
            float _Invert;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            
            //SDF methods from IQ's website https://iquilezles.org/articles/distfunctions2d/
            float sdTriangle(in float2 p, in float2 p0, in float2 p1, in float2 p2)
            {
                float2 e0 = p1 - p0, e1 = p2 - p1, e2 = p0 - p2;
                float2 v0 = p - p0, v1 = p - p1, v2 = p - p2;
                float2 pq0 = v0 - e0 * clamp(dot(v0, e0) / dot(e0, e0), 0.0, 1.0);
                float2 pq1 = v1 - e1 * clamp(dot(v1, e1) / dot(e1, e1), 0.0, 1.0);
                float2 pq2 = v2 - e2 * clamp(dot(v2, e2) / dot(e2, e2), 0.0, 1.0);
                float s = sign(e0.x * e2.y - e0.y * e2.x);
                float2 d = min(min(float2(dot(pq0, pq0), s * (v0.x * e0.y - v0.y * e0.x)),
                                 float2(dot(pq1, pq1), s * (v1.x * e1.y - v1.y * e1.x))),
                                 float2(dot(pq2, pq2), s * (v2.x * e2.y - v2.y * e2.x)));
                return -sqrt(d.x) * sign(d.y);
            }

            float sdOrientedBox(in float2 p, in float2 s, in float2 e, float th)
            {
                float l = length(e - s);
                float2 d = (e - s) / l;
                float2 q = (p - (s + e) * 0.5);
                q = mul(float2x2(d.x, d.y, -d.y, d.x), q);
                q = abs(q) - float2(l, th) * 0.5;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0);
            }

            fixed4 frag (v2f IN) : SV_Target
            {
                float box = 1;
                for (int i = 0; i < _RectShapeCount; i++)
                {
                    float2 vertex1 = saturate(_RectShapeBuffer[i].Vertex1);
                    float2 vertex2 = saturate(_RectShapeBuffer[i].Vertex2);
                    float2 vertex3 = saturate(_RectShapeBuffer[i].Vertex3);
                    float2 edge1 = vertex2 - vertex3;
                    float thickness = length(edge1);
                    float b = sdOrientedBox(IN.uv, vertex1 - edge1 / 2, vertex2 - edge1 / 2, thickness);
                    b = (b - _RectShapeBuffer[i].BlackLevel) / (_RectShapeBuffer[i].WhiteLevel - _RectShapeBuffer[i].BlackLevel);
                    box *= lerp(1, saturate(b), _RectShapeBuffer[i].Alpha);
                }
    
                float tri = 1;
                for (int j = 0; j < _TriShapeCount; j++)
                {
                    float2 tri_vertex1 = saturate(_TriShapeBuffer[j].Vertex1);
                    float2 tri_vertex2 = saturate(_TriShapeBuffer[j].Vertex2);
                    float2 tri_vertex3 = saturate(_TriShapeBuffer[j].Vertex3);
                    float t = sdTriangle(IN.uv, tri_vertex1, tri_vertex2, tri_vertex3);
                    t = (t - _TriShapeBuffer[j].BlackLevel) / (_TriShapeBuffer[j].WhiteLevel - _TriShapeBuffer[j].BlackLevel);
                    tri *= lerp(1, saturate(t), _TriShapeBuffer[j].Alpha);
                }
    
                float bg = 1;
                float4 col = bg - pow(1 - pow(tri * box, 1), 1);
                col = lerp(col, 1 - col, _Invert);
                return col;
            }
            ENDCG
        }
    }
}
