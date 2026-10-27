// Strict read-only source integrity gate. Does NOT certify artistic canon or mobile performance.
// Run: node BuildTools/Art/validate_ashbound.mjs
import fs from 'node:fs';
import path from 'node:path';

const models = {
  RiftForager: ['ashbound-rift-forager', 'Forager_OffsetShoulder'],
  PartedStriker: ['ashbound-parted-striker', 'Striker_OverbuiltRightShoulder'],
  SeamCastigator: ['ashbound-seam-castigator', 'Castigator_ArmCaptureBrace'],
  AnchorWrecker: ['ashbound-anchor-wrecker', 'Wrecker_BluntAnchorPad']
};

function requireCondition(condition, error) {
  if (!condition) throw new Error(error);
}

function audit(name, [unitId, specialty]) {
  const folder = path.join('SourceArt', 'Ashbound', name);
  const manifest = JSON.parse(fs.readFileSync(path.join(folder, 'manifest.json'), 'utf8'));
  const materials = new Set();
  for (const line of fs.readFileSync(path.join(folder, name + '.mtl'), 'utf8').split(/\r?\n/)) {
    if (line.startsWith('newmtl ')) {
      const id = line.slice(7).trim();
      requireCondition(id && !materials.has(id), name + ': repeated/blank material ' + id);
      materials.add(id);
    }
  }
  const vertices = [], faces = [], objects = new Set();
  let currentObject = null, currentMaterial = null;
  let faceCount = 0, minimumDoubleArea = Infinity;
  const low = [Infinity, Infinity, Infinity], high = [-Infinity, -Infinity, -Infinity];
  for (const line of fs.readFileSync(path.join(folder, name + '.obj'), 'utf8').split(/\r?\n/)) {
    if (line.startsWith('v ')) {
      const p = line.split(/\s+/).slice(1).map(Number);
      requireCondition(p.length === 3 && p.every(Number.isFinite), name + ': invalid vertex');
      vertices.push(p);
      for (let k = 0; k < 3; k++) { low[k] = Math.min(low[k], p[k]); high[k] = Math.max(high[k], p[k]); }
    } else if (line.startsWith('o ')) {
      const object = line.slice(2).trim();
      requireCondition(object && !objects.has(object), name + ': duplicate/empty object ' + object);
      currentObject = object; objects.add(object);
    } else if (line.startsWith('usemtl ')) {
      currentMaterial = line.slice(7).trim();
      requireCondition(materials.has(currentMaterial), name + ': undeclared material ' + currentMaterial);
    } else if (line.startsWith('f ')) {
      requireCondition(currentObject && currentMaterial, name + ': unassigned face');
      const indices = line.slice(2).split(/\s+/).map(token => Number(token));
      requireCondition(indices.length === 3 &&
        indices.every(idx => Number.isInteger(idx) && idx >= 1 && idx <= vertices.length),
        name + ': invalid triangle index');
      const [a,b,c] = indices.map(idx => vertices[idx-1]);
      const ab = b.map((v,k) => v - a[k]), ac = c.map((v,k) => v - a[k]);
      const cross = [ab[1]*ac[2]-ab[2]*ac[1],
        ab[2]*ac[0]-ab[0]*ac[2], ab[0]*ac[1]-ab[1]*ac[0]];
      const doubleArea = Math.hypot(...cross);
      requireCondition(Number.isFinite(doubleArea) && doubleArea > 1e-12,
        name + ': degenerate/nonfinite face ' + faceCount + ' (' + currentObject + ')');
      minimumDoubleArea = Math.min(minimumDoubleArea,doubleArea);
      faceCount++;
    }
  }
  requireCondition(manifest.model === name && manifest.id === unitId &&
    manifest.bipedal_avarin === true && manifest.living_crescent_mantle === true &&
    manifest.bonded_creature_count === 0 && manifest.rigged === false &&
    manifest.mobile_certified === false &&
    manifest.up === 'Y' && manifest.forward === 'Z' && manifest.units === 'meters',
    name + ': model lore/safety metadata mismatch');
  requireCondition(objects.has('Avarin_TempleFace') &&
    objects.has('Avarin_LivingCrescentMantle') && objects.has(specialty),
    name + ': missing crucial identity or anatomy mesh');
  requireCondition(Array.from(objects).every(n => !/companion|bonded.creature|trophy|skull|soul|rifle|gun/i.test(n)),
    name + ': disallowed object naming/content');
  requireCondition(objects.size === manifest.parts &&
    vertices.length === manifest.vertices && faceCount === manifest.triangles &&
    materials.size === 8, name + ': geometry counts differ from manifest');
  requireCondition(low.every((v,k) => Math.abs(v-manifest.bounds[0][k]) < .00002) &&
    high.every((v,k) => Math.abs(v-manifest.bounds[1][k]) < .00002),
    name + ': invalid physical bounds');
  requireCondition(low[1] >= -.01 && high[1] > 1.75 && high[1] < 1.95,
    name + ': outside the proposed Avarin scale envelope');
  console.log(name + ': ' + vertices.length + ' vertices, ' + faceCount +
    ' valid triangles, ' + objects.size + ' distinct named parts; min doubled area ' +
    minimumDoubleArea.toExponential(2));
}

for (const [name, expected] of Object.entries(models)) audit(name, expected);
console.log('All four Ashbound studies passed structural integrity (not visual/canon approval).');
