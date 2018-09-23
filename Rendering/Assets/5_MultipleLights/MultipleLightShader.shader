Shader "Custom/MultipleLightShader" {
	Properties{
	}

	SubShader{
		Pass{
			Tags{
				// The forward base pass is for the main directional light.
				"LightMode" = "ForwardBase"
			}

			// The additive pass has to add its results to the base pass, not replace it. 
			// We can instruct the GPU to do this, by changing the blend mode of the additive pass.
			Blend One One
			// The default mode is no blending, which is equivalent to One Zero. 
			// Which means the result of such pass replaces anything that was previously in the frame buffer.

			// Because writing to the depth buffer twice is not necessary, let's disable it.
			ZWrite Off

			CGPROGRAM

			#include "My Lighting.cginc"

			// spot to support spot lights
			#pragma multi_compile DIRECTIONAL POINT SPOT

			// To make sure that Unity selects the best BRDF function, we have to target at least shader level 3.0.
			#pragma target 3.0
			#pragma vertex MyVertexProgram
			#pragma fragment MyFragmentProgram

			// The correct macro is only defined when it is known that we're dealing with a point light. 
			// To indicate this, we have to #define POINT before including AutoLight. 
			//#define POINT

			//#include "UnityPBSLighting.cginc"


			ENDCG
		}
	}
}
