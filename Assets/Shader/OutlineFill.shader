Shader "Custom/Outline Fill" {
  Properties {
    [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 0

    _OutlineColor("Outline Color", Color) = (1, 1, 1, 1)
    _OutlineWidth("Outline Width", Range(0, 10)) = 2
  }

  SubShader {
    Tags {
      "Queue" = "Transparent+110"
      "RenderType" = "Transparent"
      "DisableBatching" = "True"
    }

    Pass
    {
      Cull Off
      ZTest On
      ZWrite Off
      Blend SrcAlpha OneMinusSrcAlpha
      ColorMask RGB

      Stencil {
        Ref 2
        Comp notequal
		Fail keep
		Pass replace
      }

      CGPROGRAM
      #include "UnityCG.cginc"

      #pragma vertex vert
      #pragma fragment frag

      struct appdata {
        float4 vertex : POSITION;
        float3 normal : NORMAL;
        float3 texCoord : TEXCOORD0;
      };

      struct v2f {
        float4 position : SV_POSITION;
        fixed4 color : COLOR;
      };

      uniform fixed4 _OutlineColor;
      uniform float _OutlineWidth;

      v2f vert(appdata input)
      {
        v2f output;

	    float4 newPos = input.vertex;

	    // normal extrusion technique
	    float3 normal = normalize(input.normal);
	    newPos += float4(normal, 0.0) * _OutlineWidth;

	    // convert to world space
	    output.position = UnityObjectToClipPos(newPos);

	    output.color = _OutlineColor;

	    return output;
      }

      fixed4 frag(v2f input) : SV_Target
      {
        return input.color;
      }
      ENDCG
    }
  }
}
