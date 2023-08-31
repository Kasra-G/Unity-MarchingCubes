Shader "Custom/NormalSurfaceShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _OceanColor ("Ocean Color", Color) = (1, 1, 1, 1)
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Lambert vertex:vert
        struct Input
        {
            float2 uv_MainTex;
            float3 customColor;
        };
        sampler2D _MainTex;
        fixed4 _Color;
        fixed4 _OceanColor;
        void vert (inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input,o);

            o.customColor = abs(v.normal);
            if (v.texcoord.y < 0) {
                o.customColor = _OceanColor;
            }

        }

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)
        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Albedo *= IN.customColor;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
