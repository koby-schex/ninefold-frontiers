# World Five anatomy and civic architecture 04

This continues silhouette study 03 without approving either model as final art.
The v26.0 archive, retained anatomy entries, Production Foundations 1–8 and locked
World Five visual master remain controlling. Unseen contours, module layouts and
surface microdetail are implementation proposals, not additions to locked lore.

## Changes

Riftwalker's existing torso, limbs and counterweight gain broad tissue planes
and shallow flexure relief, with tapered upper-limb profiles and shallow torso
folds traced directly from its surface. A waist web connects the offset body to the two
limb roots. Ankles use a darker plum flexure material, while the counterweight
has a thinner profile. Sensory pits are placed from actual crest-mesh vertices
and embedded slightly into that surface. Layered margins have staggered tips.
The creature retains two ground contacts, its separate partner, compact scale,
broken non-emissive channels and a counterweight clear of the floor.

Burdenbeast gains shaped relief in muscular limbs and dorsal supports. Its civic
platform now has distinct proposed archive terraces, care vaults and open shelter
canopies with folded panels. Arched insets replace the repeated rectangular
panels on enclosed modules. The load platform and stowed Convergence equipment
remain separate from the organism. Eight short limbs and broad pads remain.
The overall architecture still needs finer treatment against the locked master.

Four local tissue materials gain deterministic color, normal and finish maps.
They preserve the established slate-umber / charcoal / plum palette and are
nonmetallic. Unity imports color in sRGB, normals as normal maps and finish as
linear data; meshes get tangents. These projected study maps are not a finished
sculpt bake. Flat-material comparison sheets show geometry only; normal-map
appearance requires Unity review.

## Review and integration

Existing review menus create static study-04 scenes under
`Assets/Ninefold/ArtReview/<Unit>04_<fingerprint>/`, with `<Unit>Study04.prefab`
and `<Unit>Review04.unity`. Older generated scenes and metadata are preserved.
Walking remains paused. No UI, battle registration, enemy models or gameplay
files are modified. Review changes against the mesh sheet at
`Docs/Review/WorldFiveAnatomy04/Comparison.png`; it compares the immediately prior
main revision with current geometry using identical materials and framing.

Validation covers finite closed geometry, limb/contact counts, scale, partner
clearance, source/map reproduction, normal-map shape and nonmetallic finish
channels. Existing C# source-import checks remain active. CI does not compile the
Unity editor windows; native compilation, lighting, seams and device performance
remain unverified. Passing checks is not a claim of visual or canon approval.
