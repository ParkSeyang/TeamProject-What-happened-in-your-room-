Shader "Custom/OcclusionDoubleSided"
{
    SubShader
    {
        // 일반 기하도형보다 먼저 그려지도록 큐를 조정합니다.
        Tags { "RenderType"="Opaque" "Queue"="Geometry-1" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass
        {
            Name "DepthMask"
            ZWrite On      // 뎁스 버퍼에 기록하여 뒤의 오브젝트를 가립니다.
            ZTest LEqual   // 일반적인 깊이 테스트 수행
            ColorMask 0    // 색상은 그리지 않아 투명하게 보입니다.
            Cull Off       // 앞면, 뒷면 모두 렌더링하여 어떤 시야각에서도 오클루전 유지
        }
    }
}
