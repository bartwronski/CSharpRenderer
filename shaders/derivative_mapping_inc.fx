
// Source: http://www.rorydriscoll.com/2012/01/11/derivative-maps/


// Project the surface gradient (dhdx, dhdy) onto the surface (n, dpdx, dpdy)
float3 CalculateSurfaceGradient(float3 n, float3 dpdx, float3 dpdy, float dhdx, float dhdy)
{
    float3 r1 = cross(dpdy, n);
    float3 r2 = cross(n, dpdx);

    return (r1 * dhdx + r2 * dhdy) / dot(dpdx, r1);
}

// Move the normal away from the surface normal in the opposite surface gradient direction
float3 PerturbNormal(float3 n, float3 dpdx, float3 dpdy, float dhdx, float dhdy)
{
    return normalize(n - CalculateSurfaceGradient(n, dpdx, dpdy, dhdx, dhdy));
}

float ApplyChainRule(float dhdu, float dhdv, float dud_, float dvd_)
{
    return dhdu * dud_ + dhdv * dvd_;
}

// Calculate the surface normal using the uv-space gradient (dhdu, dhdv)
float3 CalculateSurfaceNormal(float3 position, float3 normal, float2 gradient, float2 uv)
{
    float3 dpdx = ddx_fine(position);
    float3 dpdy = ddy_fine(position);

    float dhdx = ApplyChainRule(gradient.x, gradient.y, ddx_fine(uv.x), ddx_fine(uv.y));
    float dhdy = ApplyChainRule(gradient.x, gradient.y, ddy_fine(uv.x), ddy_fine(uv.y));

    return PerturbNormal(normal, dpdx, dpdy, dhdx, dhdy);
}

