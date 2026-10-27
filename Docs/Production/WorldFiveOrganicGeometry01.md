# World Five — organic geometry and roster start 01

## Authority and boundaries

Source checked against `Ninefold_World_Lore_Archive_v26.0.docx` §26.6 and §26.11,
retained v25.3 World Five bonded-tradition/Apex entries and PF1–8, and the locked
`Ninefold_World5_Avarin_Continuance_Visual_Canon_Showcase_FINAL_LOCKED_v1.png`.
The image controls visual identity; written anatomy resolves limb counts and
relationships that the composed showcase does not fully expose. Hidden contours,
equipment arrangements and exact meshes below remain implementation proposals.

## Existing models

Ilyth's existing stress coverings gain broader growth contours and longitudinal
relief. Soft joint and neck tissue has shaped variation; the Avarin's existing
garment surfaces have deeper folds. The torso remains lightweight, not armored.
Olyra gains more defined core relief and root-biased membrane folds, relaxing
toward the canopy edge. Ribs and veins derive from the same deformed surface.

Topology, part names, contact groups, canopy alpha and motion timing/path equations
are retained. The Olyra reference fixture is regenerated for the new geometry;
its transition frames/durations remain the same. Sampled clearance, area and
volume checks must pass before using this geometry in motion. These refinements
do not add breathing, expressive behavior, a production rig or a final sculpt.

Review menus remain unchanged. New generated assets are in
`Assets/Ninefold/ArtReview/GreatStrider14_<fingerprint>/GreatStriderStudy14.prefab`
and `LivingVeil11_<fingerprint>/LivingVeilStudy11.prefab` or
`LivingVeilProtective11_<fingerprint>/LivingVeilProtectiveStudy11.prefab`.
Scene filenames are `GreatStriderReview14.unity` and `LivingVeilReview11.unity`.

## New standard-unit studies

| Unit | Canon constraints used | First implementation |
| --- | --- | --- |
| Burdenbeast / Tervan | Eight short muscular limbs, four per side; broad circular pads; low wide concave back; little neck; slate-umber hide and pale-silver support ridges; civic carrier with independent Avarin | Broad source body and eight contacts; stowed archive/care/shelter module silhouettes and a folded Convergence housing; walking Avarin beside it |
| Riftwalker / Vaskel | Only two locomotor limbs; asymmetric crescent torso; opposite flexible counterweight; pale sensory crescent with tiny pits and separated channels; plum-black tissue; independent partners with continuity anchors | Compact two-contact source body, raised counterweight, sensory crescent and discontinuous non-emissive inlays; Avarin beside it with a compact anchor |

These are intentionally early proportion studies, substantially below the target
master's complexity. They have flat materials, reused standing Avarin geometry,
no custom unwraps, textures, rig, animations or production LODs. Tervan's module
arrangements are placeholders for later architecture work. Its weapon is stowed
and separate from its biology; no deployed weapon pose is delivered. Vaskel's
counterweight never becomes a third locomotor limb. No teleportation effect or
free-teleport mechanic is added.

Sources are `SourceArt/Burdenbeast/Burdenbeast.obj` and
`SourceArt/Riftwalker/Riftwalker.obj`, each with MTL and manifest. Rebuild with
`python BuildTools/Art/build_worldfive_roster.py`. `--unit` selects one model.

Open **Ninefold > Art > Burdenbeast Review** or **Riftwalker Review**, then create
a fresh scene. The tool retains old copies and uses the existing validated OBJ
parser. Generated paths are `Assets/Ninefold/ArtReview/<Unit>01_<fingerprint>/`,
with `<Unit>Study01.prefab` and `<Unit>Review01.unity`; additional copies get a
unique folder suffix. Native Unity compilation/shading still needs testing.

These identifiers are art source names, not new runtime unit IDs. There is no
battle or Collection registration. True source dimensions remain canon-scaled;
gameplay presentation scale and selection/collision sizes belong to the separate
gameplay integration layer and can follow the game's normalized battlefield-size
policy without rewriting biological canon. No shared UI or gameplay files change.

## Apex preparation — Serai Venn and Nethra

The Apex is the paired identity, never a rider and mount. Serai remains Avarin,
with restrained broken silver-violet fracture markings, an altered eye and
recognizable mantle. Nethra is a low elongated four-limbed organism with dark
charcoal-plum tissue, selective pale mineral reinforcement, a calm compact head,
two deep-set eyes and several incomplete concentric biological halo arcs.
The arcs are not a crown or weapon. The ordinary halo response is restrained,
with the stable body readable rather than constantly glitching. Use the locked
paired pose, “The Quiet Before the Fracture,” when producing the first study.

Nethra's species, origin and the disappearance's truth remain unknown. No Apex
mesh is claimed in this pass. First establish the paired silhouette and halo
construction from the locked master before adding Fracture Lattice detail or
spatial effects. No invented lore should fill unseen anatomical surfaces.

## Validation and next work

New sources are checked for finite/nondegenerate geometry, outward closed parts,
expected limb/contact counts, ground contact, source reproduction, separate
partner clearance, and scale bands. C# tests import both actual sources and check
their part identities and Avarin anatomy. CI provides actual-mesh multiview sheets.
These checks do not certify self-intersection, rigging, phone performance or
fidelity to the locked master. Existing model motion checks remain active.

Next: review all four silhouettes, refine the new studies' organic proportions
and equipment, and add breathing/secondary motion to Ilyth and Olyra while
retaining fixed contacts and accepted deployment. Build the Apex after its paired
silhouette has been reconciled against the master. This is the start of expanding
the roster, not delivery of four finished units.
