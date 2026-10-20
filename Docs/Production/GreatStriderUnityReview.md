# Great Strider Unity review 11 — textured source study 11

This opt-in Unity inspection imports the detail study 11. It creates
a reusable **static review prefab**, a neutral review scene and eight imported
textures. It does not add the Great Strider to a battle roster, rig it or claim
final art quality. Master Lore, Production Foundations 1–8 and the locked World
Five visual master remain authoritative; no canon is revised. See GreatStriderDetail.md for the bounded source refinements.

## Open it later

1. Pull the merged PR, open Unity **6000.3.21f1**, stop Play and wait for importing.
2. Open **Ninefold > Art > Great Strider Review**, or use **Great Strider visual
   review** in **Ninefold > Playtest > Launcher**.
3. Click **Open / create latest review scene**. Configure the existing foundation
   through the launcher if prompted. Canceling the normal save-scene prompt
   aborts the operation.
4. Open Game view at **540 × 960**. No Play mode is needed. Use **Tactical camera ·
   portrait**, **Full silhouette**, front/side/back or **Cradle / operator detail**.
   The underside preset adjusts only Scene view, avoiding an underground Game camera.
5. Read the **Source revision**, **Source fingerprint**, and **Imported scene**
   fields in the review window. Click **Refresh version status** after changing
   scenes. **Validate imported review assets** checks the actual Unity mesh,
   textures and membrane settings; it also runs automatically after creation.
6. Return through the launcher's existing integration-scene/playtest controls.
   Battle/profile saves are not accessed by this utility.

## Output and preservation

Output lives under `Assets/Ninefold/ArtReview/GreatStrider11_<fingerprint>/`:

- `GreatStriderStudy11.prefab`: static model with material render sections.
- `GreatStriderReview11.unity`: neutral ground, studio lights, scale marker and camera.
- Mesh assets, 12 URP Lit materials, eight PNG textures and `source-version.txt`.

The SHA-256 fingerprint covers the OBJ, MTL, source manifest, material catalog,
texture bytes and both importer source files. Different source or importer code
creates a new folder. Opening an existing matching scene preserves saved edits.
**Build fresh review copy** creates a unique additional folder without overwriting
prior assets. Old review 03/04/09/10 scenes are never silently reopened or replaced.

The prefab is generated locally by the checked-in importer. Generated Unity
assets are not prebuilt or committed by this PR. After native review they can be
committed with their metadata. Camera changes are normal local scene edits.
The window's camera controls apply only to the scene opened by this tool.

## Neutral lighting and material inspection

Review 11 imports detail study 11 and retains review 10’s neutral setup. Review 10 replaced the review scene's warm
key / blue fill and purple-tinted ambient with white directional lights, gray
ambient, gray ground and a gray camera background. Skybox, fog and environment
reflections are disabled in this scene. This makes hue comparisons easier; it
is not a calibrated color booth or final battlefield lighting.

Opening the scene and using camera presets enables Scene lighting and hides
Scene view grid, gizmos and skybox. These editor display preferences can be
restored with Unity's normal Scene view controls. Game view remains the framing
and background reference; no project-wide rendering asset is modified.

Expand **Inspect model materials** to see each model material's base tint,
albedo tile preview, opacity and smoothness. These fields are read-only; the
**Select material in Inspector** button locates the actual imported material.
White tint on a textured surface is intentional because its color is already
in the PNG. Inspector edits are local review experiments, not source changes.

The Input Manager deprecation warning is unrelated to this static art review.
The review uses editor controls and needs no Play mode. Input-system migration
belongs to a separate gameplay input change with touch/mouse regression checks.

## Import behavior

The shared C# parser accepts this generator's named, triangle-only OBJ, positive
vertex indices and optional independent texture-coordinate indices. It rejects
missing materials/UVs, nonfinite coordinates, degenerate triangles, unsupported
syntax, invalid indices and texture paths outside the expected local tile folder.

Manifest counts and six-meter Y-up/Z-forward bounds are checked before generating
assets. There is no scaling or anatomy change. Area-weighted normals are computed
on original vertex indices **before** UV seams duplicate vertices, preserving
smooth shading across seams. Opaque geometry combines by material; each closed
translucent shell gets a separate renderer so Unity can sort it independently.
This is not a final draw-call/LOD optimization.

Textures import as sRGB albedo with repeat wrapping, mipmaps, trilinear filtering
and no extra CPU-readable copy. The MTL's white tint is retained for textured
materials because their tint is already baked into the PNG. Untextured linear
source colors are converted for Unity color-property authoring. Smoothness is
an approximation of the MTL exponent, with metallic zero; no physical material
calibration is claimed.

Smoky membranes retain opacity 0.68. URP Lit alpha blending, transparent queue,
transparent keyword and disabled depth writes/shadow casting are explicitly set.
Back-face culling is retained because these are closed shells, avoiding both
walls being blended over one another. Transparency intersections and lighting
still require native visual review; this is not an order-independent shader.

The scene is not registered in build settings. No package install, pipeline
change, automatic Play, account access or game save mutation occurs. Temporary
prefab assembly uses a private preview scene. Failure during asset creation
cleans up only the newly allocated output folder.

## Validation and next native check

Repository checks run the **actual C# parser** on Linux and Windows using the
existing executable test project. Tests import all 184 parts / 40,646 source
vertices / 80,556 triangles and eight maps, verify UV splitting preserves normals,
and reject malformed geometry and unsafe paths. Repository metadata and the
existing source-geometry checks also run.

These checks do **not** compile or execute Unity Editor APIs. Unity is unavailable
in the implementation environment. The first native review must verify:

- No Console compilation/import errors; create, reopen, fresh copy and Cancel work.
- Fingerprints match and the imported-asset validation succeeds.
- All four feet and paired sails remain visible; no reversed/missing faces.
- UV seams do not introduce shading breaks; maps are neither white nor double-tinted.
- Membranes remain smoky/translucent from front, side, back and tactical views.
- The portrait presets frame the model and operator adequately.
- White studio lights and gray ground/background remove the previous lighting tint.
- Material inspection lists 12 materials, with eight texture previews; selecting a material opens the correct asset.
- Reviews 09 and 10 still open unchanged; review 11 creates a separate output folder.
- Returning to the integration scene preserves the existing battle.

Device performance, animation/rigging, final texture unwrap and battlefield
integration remain separate work. This prefab is a source-review milestone.

API references used for implementation:
- https://docs.unity3d.com/6000.0/Documentation/ScriptReference/TextureImporter.html
- https://docs.unity3d.com/6000.0/Documentation/Manual/urp/lit-shader.html
