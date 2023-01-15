// Copyright (C) 2019-2020 by HaptX Incorporated - All Rights Reserved.
// Unauthorized copying of this file via any medium is strictly prohibited.
// The contents of this file are proprietary and confidential.

Shader "HaptX/DontModify_HxHand_WeightedOpacity"
{
  SubShader
  {
    Tags {"Queue" = "Transparent" "RenderType" = "Transparent" }
    LOD 100

    ZWrite Off
    ZTest Always
    Blend SrcAlpha OneMinusSrcAlpha

    Pass
    {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag

      #include "UnityCG.cginc"

      // Values of interest provided by the platform.
      struct appdata
      {
        // The position of the vertex. We pass this through as an output of our
        // vertex shader.
        float4 vertex : POSITION;

        // Custom UV channel storing vertex index in the "U" slot.
        float2 uv2 : TEXCOORD1;
      };

      // The output of our vertex shader.
      struct v2f
      {
        // The position of the vertex, which is mandatory.
        float4 vertex : SV_POSITION;

        // The opacity of the vertex. Used in the fragment shader to compute
        // bone weighted fragment opacities.
        float opacity : TEXCOORD0;
      };

      // The color of the mesh. Replace with a texture to be more fancy.
      float4 _Color;

      // How many bones are in our skinned mesh.
      float _NumBones;

      // How many vertices are in our skinned mesh.
      float _NumVertices;

      // An artificial texture that encodes the affecting bone indices and
      // associated bone weights for each vertex in the skinned mesh.
      // Vertices are indexed with U coordinates and associated values of
      // interest are indexed by V coordinates. The pixels in the first row
      // store in their RGBA channels the bone indices of the four bones that
      // influence their vertices, but they've been compressed to fit into a
      // 0-1 range. Multiple by (_NumBones - 1) to decompress. The second row
      // stores the bone weights in a similar fashion (no decompression
      // required).
      sampler2D _BoneIndicesAndWeights;

      // The opacities of each bone. Initialized with a size of 21 because
      // that's how many bones we expect to be in our hand mesh.
      int _BoneOpacitiesLength;
      float _BoneOpacities[21];

      v2f vert(appdata v)
      {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        // We add 0.5 to target the center of the pixel to make sure that
        // floating point error doesn't send us to the wrong pixel.
        float u = (v.uv2[0] + 0.5) / _NumVertices;
        float4 boneIndices = tex2Dlod(_BoneIndicesAndWeights,
            float4(u, 0.25, 0, 0));  // 0.25 = (0 + 0.5) / 2
        float4 boneWeights = tex2Dlod(_BoneIndicesAndWeights,
            float4(u, 0.75, 0, 0));  // 0.75 = (1 + 0.5) / 2
        o.opacity =
            boneWeights[0] * _BoneOpacities[(_NumBones - 1) * boneIndices[0]] +
            boneWeights[1] * _BoneOpacities[(_NumBones - 1) * boneIndices[1]] +
            boneWeights[2] * _BoneOpacities[(_NumBones - 1) * boneIndices[2]] +
            boneWeights[3] * _BoneOpacities[(_NumBones - 1) * boneIndices[3]];
        return o;
      }

      fixed4 frag(v2f i) : SV_Target
      {
        fixed4 col = _Color;
        col.a = i.opacity;
        return col;
      }
      ENDCG
    }
  }
}
