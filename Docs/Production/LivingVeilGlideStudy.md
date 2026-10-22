# Living Veil — glide study 08

## Player feedback and response

The Unity review found the idle motion convincing, but deployment/retraction felt
robotic and the protective canopy was slightly too large. This pass preserves both
idle curves and the resting shape. Protective lobe radial reach is reduced by 8%;
the root radius, heights, five lobes and partner scale remain unchanged. Overall
span remains within the existing 4–6 m canon range. This is the user's requested
presentation adjustment, not a canon revision.

The previous lift/unfold/translate/descend stages are replaced by overlapping
movement. Horizontal drift progresses throughout the transition, unfolding overlaps
lift, and descent begins while lateral movement continues. Peak added lift is below
1 m instead of the former 1.35 m plateau. A small, endpoint-faded breathing wave
softens transition motion. Deployment remains 3.6 s; retraction takes 4.1 s with a
monotonic timing offset along the same checked route. The partner stays fixed.

These are deterministic editor-study curves, not a production rig, cloth simulation
or gameplay timing change. The accepted idle sine curves remain identical; the
protective idle naturally uses the slightly smaller protective geometry.

## Review

Open **Ninefold > Art > Living Veil Motion Review** and confirm **Glide study 08**,
then reload the source and review Deploy and Retract. Check normal playback speed,
not only scrubbing. The static review is **Glide study 08 / Unity review 08**.

New static assets use `LivingVeil08_<fingerprint>/LivingVeilStudy08.prefab` and
`LivingVeilProtective08_<fingerprint>/LivingVeilProtectiveStudy08.prefab` beneath
`Assets/Ninefold/ArtReview/`, with `LivingVeilReview08.unity` scenes. Previous review
copies are retained. Source ID, GUIDs, partner mesh, texture files and topology
counts (126 parts, 47,136 vertices, 93,768 triangles) are unchanged.

## Validation

Python and C# sample the same curves, with a regenerated motion reference fixture.
Tests guard against delayed drift, excess lift and losing slower retraction.
Motion validation samples 121 frames in each transition and 33 per idle (308 total),
checking fixed partner, expanded partner clearance, triangle area, outward volume,
finite bounds, displacement continuity, loop endpoints and the retimed return path.
Both source poses remain reproducible and pass geometry/surface/UV validation.

Actual-mesh contact frames were inspected locally. CI renders all four motion GIFs
and contact sheets. These checks are sampled, not exhaustive self-collision proof.
Native Unity playback acceptance remains required, especially for perceived weight,
transparent lobe overlaps and movement at actual speed. No final visual or mobile
performance approval is implied. UI/gameplay and the shared preview API are untouched.
