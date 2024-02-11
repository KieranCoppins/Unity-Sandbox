Shader "Custom/SquadronShader"
{
    Properties
    {
        _TeamA("Team A", Color) = (1, .0, .0, 1)
        _TeamB("Team B", Color) = (0, .0, 1, 1)
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdadd_fullshadows

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc" // for _LightColor0

            struct SquadronMemberData
            {
                float4x4 mat;
                float3 velocity;
                
                int team;
                int targetId;
                int targetedByCount;

                int dead;
                float lastShotTime;

                int id;
            };

            struct v2_f
            {
                float4 vertex : SV_POSITION;
                fixed4 diff : COLOR0; // diffuse lighting color
                float4 color: COLOR1;
            };

            StructuredBuffer<SquadronMemberData> members;

            float4 _TeamA;
            float4 _TeamB;

            float4 _EngineOffset;

            v2_f vert(const appdata_base i, const uint instance_id: SV_InstanceID)
            {
                v2_f o;
                
                const float4 pos = mul(members[instance_id].mat, i.vertex);
                o.vertex = UnityObjectToClipPos(pos);

                // Lighting
                half3 worldNormal = UnityObjectToWorldNormal(i.normal);
                half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
                o.diff = nl * _LightColor0;
                o.diff.rgb += ShadeSH9(half4(worldNormal,1));

                if (members[instance_id].team == 1) {
                    o.color = _TeamA;
                } else {
                    o.color = _TeamB;
                }

                return o;
            }

            fixed4 frag(v2_f i) : SV_Target
            {
                // multiply by lighting
                return i.color * i.diff;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}