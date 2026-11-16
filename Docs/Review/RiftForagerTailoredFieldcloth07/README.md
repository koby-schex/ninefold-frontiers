# Rift Forager — physical tailored fieldcloth 07 (candidate only)

**Model status:** provisional ArtReview, not approved for gameplay, canon or phone performance.

## Why this is a different pass

The Rift Forager's body in #184 has one smooth closed mesh with a painted garment material over much of its surface. That cannot create the distinct folds, hanging hems or thickness of actual layered clothing. Adding another skin normal map would make the source look less flat but would not change the silhouette. The approved World Five Ashbound addendum permits dark asymmetrical salvage and field equipment; it does not authorize added biology or new powers.

## Actual geometry changes

- **Two independent stitched fieldcloth panels**, shaped as fitted rear waist-and-hip fieldcloth tails, below the natural mantle and clear of upper-back equipment. Their different lengths, fold phase and flared hems break the anonymous symmetric bodysuit silhouette.
- Each panel has **two physical surfaces separated by ~7 mm plus a sealed perimeter**, with real triangles, UVs and cloth/trim submeshes. The drape uses a continuous bisection clearance solve against the *existing analytical body field* to prevent clipping or stair-step creases; it is not attached as arbitrary floating ovals.
- **Source-linked C# generator** `Assets/Ninefold/Editor/RiftForagerTailoredGarment.cs` feeds both the **Unity 19-bone SkinnedMeshRenderer** and the **GitHub Actions static OBJ review**, so the renders show actual authored triangles. The same existing garment and dark material swatches are reused.
- The original sensory crescent, eyes, temple, chest braid, apron and engineering/salvage pieces are untouched. Body mesh, gameplay visuals and all other faction units remain unchanged.
- Unity's rig-review folder hash includes the garment generator's bytes, making a fresh independently staged ArtReview prefab and scene. No live prefab or story changes.

## Validation / real preview

Core .NET tests validate non-degenerate manifold triangles across the closed cloth shells, bounded physical triangle count, asymmetrical hems, no body penetration in the rest pose and normalized skin weights. Repository structure checks verify the same generated source is wired into Unity, Core and the standalone exporter. The Ashbound art workflow exports the exact body **plus both actual draped panels** and counts their faces; new rear/three-quarter silhouette appears in the depth-accurate comparison artifact.

The automatic comparison still shows the original separate-anatomy OBJ as BEFORE and the current model as AFTER. To compare the incremental improvement, use the same-angle AFTER panels from #184 and this PR.

## Release blockers remain

The still-provisional Avarin face, non-photoreal material surfaces, character-scale lighting, proper cloth dynamic deformation, Unity mesh imports, joint collision in six motions, and mobile profiling **must be evaluated in Unity**. A passing CI or screenshot does not establish production-level visual quality or canon approval. No Unity AI interaction is required of the user.

Control remains with the Master Lore & World-Building Archive v26.0 and approved World Five visual masters. Do not promote this model to the live enemy catalog without explicit approval. 
