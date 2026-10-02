Shader "IceFishing/TutorialFrost"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0.03, 0.08, 0.14, 0.55)
        _Hole ("Hole UV (xmin,ymin,xmax,ymax)", Vector) = (0.12, 0.18, 0.88, 0.82)
        _HoleSoft ("Hole Soft", Float) = 0.01
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _Color;
            float4 _Hole;
            float _HoleSoft;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float soft = max(0.001, _HoleSoft);
                float insideX = smoothstep(_Hole.x - soft, _Hole.x + soft, i.uv.x)
                    * (1.0 - smoothstep(_Hole.z - soft, _Hole.z + soft, i.uv.x));
                float insideY = smoothstep(_Hole.y - soft, _Hole.y + soft, i.uv.y)
                    * (1.0 - smoothstep(_Hole.w - soft, _Hole.w + soft, i.uv.y));
                float inside = saturate(insideX * insideY);
                float alpha = (1.0 - inside) * _Color.a;
                return fixed4(_Color.rgb, alpha) * i.color;
            }
            ENDCG
        }
    }
}
