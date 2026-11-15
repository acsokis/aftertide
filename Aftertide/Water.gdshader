shader_type spatial;
render_mode cull_disabled, depth_draw_always;
uniform float storm = 0.0;
varying vec3 world_position;
void vertex(){world_position=(MODEL_MATRIX*vec4(VERTEX,1.0)).xyz;float strength=mix(.13,.55,storm);
 VERTEX.y+=sin(world_position.x*.065+TIME*1.4)*strength+sin(world_position.z*.11-TIME*1.8)*strength*.6;
 NORMAL=normalize(vec3(-cos(world_position.x*.065+TIME*1.4)*strength*.065,1.0,-cos(world_position.z*.11-TIME*1.8)*strength*.066));}
void fragment(){float fresnel=pow(1.0-clamp(dot(normalize(NORMAL),normalize(VIEW)),0.0,1.0),4.0);
 ALBEDO=mix(vec3(.018,.31,.36),vec3(.12,.47,.56),fresnel);ROUGHNESS=mix(.18,.32,storm);METALLIC=.1;
 ALPHA=mix(.64,.95,fresnel);}
