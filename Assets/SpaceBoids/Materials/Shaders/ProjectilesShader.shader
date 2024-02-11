Shader "Custom/ProjectileShader"
{
    Properties
    {
        [HDR] _Color("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct ProjectileData
            {
                float4x4 mat;
                float3 velocity;

                int casterId;
                int valid;
                float spawnTime;
            };

            struct v2_f
            {
                float4 vertex : SV_POSITION;
                // float4 color: COLOR1;
				float2 uv : TEXCOORD0;
            };

            struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

            StructuredBuffer<ProjectileData> data;

            float4 _Color;
            sampler2D _MainTex;

            v2_f vert(const appdata i, const uint instance_id: SV_InstanceID)
            {
                v2_f o;

                // billboard mesh towards camera
				float3 vpos = mul((float3x3)unity_ObjectToWorld, i.vertex.xyz);
				float4 worldCoord = float4(data[instance_id].mat._m03, data[instance_id].mat._m13, data[instance_id].mat._m23, 1);
				float4 viewPos = mul(UNITY_MATRIX_V, worldCoord) + float4(vpos, 0);
				float4 outPos = mul(UNITY_MATRIX_P, viewPos);

                o.vertex = outPos;

                o.uv = i.uv.xy;
                return o;
            }

            fixed4 frag(v2_f i) : SV_Target
            {
                // Sample the texture
                fixed4 texColor = tex2D(_MainTex, i.uv);

                // Convert black to alpha
                float alpha = max(max(texColor.r, texColor.g), texColor.b);
                texColor.a = alpha;

                // Multiply the texture color by the given color
                fixed4 resultColor = texColor * _Color;

                return resultColor;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}