Shader "Hidden/Shader/VIOSOWarpBlendPP"
{
    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/FXAA.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/RTUpscale.hlsl"

    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord   : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
        return output;
    }

    // List of properties to control your post process effect
    float4x4 matView;
    float4 bBorder;
    float4 blackBias;
    float4 offsScale;
    float4 mapSize;
    TEXTURE2D_X( _texContent );
    TEXTURE2D( _texWarp );
    TEXTURE2D( _texBlend );
    TEXTURE2D( _texBlack );

    float4 tex2DLin( TEXTURE2D_X( texCnt ), float2 vPos )
    {
        float2 t = floor( vPos - 0.5 ) + 0.5; // the nearest pixel
        float2 w = vPos - t; // weight
        float4 tl = LOAD_TEXTURE2D_X( texCnt, t );;
        float4 tr = LOAD_TEXTURE2D_X( texCnt, float2( t.x + 1, t.y  ) );
        float4 bl = LOAD_TEXTURE2D_X( texCnt, float2( t.x, t.y + 1 ) );
        float4 br = LOAD_TEXTURE2D_X( texCnt, float2( t.x + 1, t.y + 1 ) );
        return lerp( lerp( tl, tr, w.x ), lerp( bl,br,w.x), w.y );
    }

    float4 tex2DBC( TEXTURE2D_X( texCnt ),
                    float2            vPos )
    {
        float2 t = floor( vPos - 0.5 ) + 0.5; // the nearest pixel
        float2 w0 = 1;
        float2 w1 = vPos - t;
        float2 w2 = w1 * w1;
        float2 w3 = w2 * w1;

        w0 = w2 - 0.5 * ( w3 + w1 );
        w1 = 1.5 * w3 - 2.5 * w2 + 1.0;
        w3 = 0.5 * ( w3 - w2 );
        w2 = 1.0 - w0 - w1 - w3;

        float2 s0 = w0 + w1;
        float2 s1 = w2 + w3;
        float2 f0 = w1 / s0;
        float2 f1 = w3 / s1;

        float2 t0 = t - 1 + f0;
        float2 t1 = t + 1 + f1;

        return
            ( tex2DLin( texCnt, t0 ) * s0.x +
              tex2DLin( texCnt, float2( t1.x, t0.y ) ) * s1.x ) * s0.y +
            ( tex2DLin( texCnt, float2( t0.x, t1.y ) ) * s0.x +
              tex2DLin( texCnt, t1 ) * s1.x ) * s1.y;
    }

    float4 CustomPostProcess(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 tx = input.texcoord.xy * mapSize.xy;
        tx.y = mapSize.y - tx.y;
        float4 tex   = LOAD_TEXTURE2D( _texWarp, tx );
        float4 blend = LOAD_TEXTURE2D( _texBlend, tx );
        float4 black = LOAD_TEXTURE2D( _texBlack, tx ) * blackBias.x;
        float4 vOut  = float4( 0,0,0,1 );
        
        if( 0.5 < blend.a ) // blend.a is 0 or 1, indicating valid warp
        {
            if( 0.5 < bBorder.x ) // border
            {
                tex.x *= 1.02;
                tex.x -= 0.01;
                tex.y *= 1.02;
                tex.y -= 0.01;
            }
            if( 0.5 < bBorder.z ) // 3D
            {
                tex = mul( tex, matView );
                tex.xy /= tex.w;
                tex.x /= 2;
                tex.y /= -2;
                tex.xy += 0.5;
            }
            tex.xy =  ( tex.xy - offsScale.xy ) * offsScale.zw * _ScreenSize.xy;
            tex.y = _ScreenSize.y - tex.y;
            
            if( 0.5 < bBorder.w ) // bicubic
            {
                vOut = tex2DBC( _texContent, tex.xy );
            }
            else
            {
                vOut = tex2DLin( _texContent, tex.xy );
            }
            if( 0.5 < bBorder.y ) // blend
               vOut.rgb *= pow( blend.rgb, 2.2 ); // try 1/2.4 to 2.4
            if( 0.5 < blackBias.w ) // blacklevel
            {
                vOut += blackBias.y * black;// offset color to get min average black
                vOut *= float4( 1,1,1,1 ) - blackBias.z * black; // scale down to avoid clipping 
                vOut = max( vOut, black ); // do lower clamp to stay above common black, upper is done anyways
            }
            vOut.a = 1;
        }
        return vOut;
    }

    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "VIOSOWarpBlend"

            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment CustomPostProcess
                #pragma vertex Vert
            ENDHLSL
        }
    }
    Fallback Off
}
