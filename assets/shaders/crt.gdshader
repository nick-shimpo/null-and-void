shader_type canvas_item;

// CRT Terminal Effect Shader
// Creates scanlines, vignette, glow, and film grain for authentic terminal feel

uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;

// Effect strengths (adjustable in inspector)
uniform float scanline_strength : hint_range(0.0, 1.0) = 0.12;
uniform float scanline_frequency : hint_range(100.0, 1000.0) = 400.0;
uniform float vignette_strength : hint_range(0.0, 1.0) = 0.25;
uniform float glow_strength : hint_range(0.0, 0.5) = 0.08;
uniform float noise_strength : hint_range(0.0, 0.1) = 0.015;
uniform float aberration_strength : hint_range(0.0, 0.01) = 0.001;

void fragment() {
    vec2 uv = SCREEN_UV;

    // Chromatic aberration (slight RGB split)
    float r = texture(SCREEN_TEXTURE, uv + vec2(aberration_strength, 0.0)).r;
    float g = texture(SCREEN_TEXTURE, uv).g;
    float b = texture(SCREEN_TEXTURE, uv - vec2(aberration_strength, 0.0)).b;
    vec4 color = vec4(r, g, b, 1.0);

    // Scanlines - horizontal dark bands
    float scanline = sin(uv.y * scanline_frequency * PI) * 0.5 + 0.5;
    scanline = pow(scanline, 1.5); // Sharper scanlines
    color.rgb -= scanline_strength * (1.0 - scanline);

    // Vignette - darken edges
    vec2 vig_uv = uv * (1.0 - uv.yx);
    float vig = vig_uv.x * vig_uv.y * 15.0;
    vig = pow(vig, vignette_strength);
    color.rgb *= vig;

    // Phosphor glow - slight bloom on bright pixels
    float luminance = dot(color.rgb, vec3(0.299, 0.587, 0.114));
    color.rgb += glow_strength * color.rgb * luminance;

    // Film grain noise - animated static
    float noise = fract(sin(dot(uv + fract(TIME * 0.1), vec2(12.9898, 78.233))) * 43758.5453);
    color.rgb += (noise - 0.5) * noise_strength;

    // Slight green tint boost for terminal feel
    color.g *= 1.02;

    COLOR = color;
}
