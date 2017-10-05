Shader "Custom/ShaderTest"
{
	Properties{
		_Color("Main Color", Color) = (1,1,1,1)
		_MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
		_AlphaMap("Additional Alpha Map (Greyscale)", 2D) = "white" {}
		_Multiplier("Multiplier", float) = 1
	}

	SubShader {
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		LOD 200

		CGPROGRAM
#pragma surface surf Lambert alpha

		sampler2D _MainTex;
		sampler2D _AlphaMap;
		float4 _Color;
		float _Multiplier;

		struct Input {
			float2 uv_MainTex;
		};

		void surf(Input IN, inout SurfaceOutput o) {
			_Color = _Color;
			half4 c = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = c.rgb * _Multiplier;
			o.Alpha = c.a * tex2D(_AlphaMap, IN.uv_MainTex).r;
		}

		ENDCG
	}
	Fallback "Transparent/VertexLit"
}
