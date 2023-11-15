Shader "Unlit/InvalidSpawn"
{
    SubShader
    {
        Tags { 
            "RenderType"="Transparent" 
            "Queue" = "Transparent"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct Interpolators
            {
                float4 vertex : SV_POSITION;
                float3 wPos : TEXCOORD0;
                float3 normal : TEXCOORD1;
            };
            
            Interpolators vert (appdata v)
            {
                Interpolators o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.wPos = mul(UNITY_MATRIX_M, float4(v.vertex.xyz, 1));
                o.normal = mul((float3x3)UNITY_MATRIX_M, v.normal);
                return o;
            }

            fixed4 frag (Interpolators i) : SV_Target{
                float3 dirToCam = normalize(_WorldSpaceCameraPos.xyzx - i.wPos);
                float fresnel = pow(1-dot(dirToCam, normalize(i.normal)), 2);
                fresnel = lerp( 0.1, 0.4, fresnel);
                return float4( 1, 0, 0, fresnel );
            }
            ENDCG
        }
    }
}
