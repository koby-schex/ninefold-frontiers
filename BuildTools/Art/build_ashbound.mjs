// Deterministic Ashbound static OBJ/MTL studies, not production art.
// Run: node BuildTools/Art/build_ashbound.mjs
import fs from 'node:fs';
import path from 'node:path';
import { finalPair } from './ashbound_final_pair_geometry.mjs';
const DATA={
  "RiftForager": {
    "id": "ashbound-rift-forager",
    "role": "fast route scavenger",
    "bulk": 0.88,
    "stride": 0.07,
    "colors": {
      "skin": [
        0.44,
        0.49,
        0.51
      ],
      "mantle": [
        0.33,
        0.43,
        0.47
      ],
      "garment": [
        0.22,
        0.25,
        0.29
      ],
      "salvage": [
        0.37,
        0.41,
        0.4
      ],
      "hardware": [
        0.71,
        0.43,
        0.29
      ],
      "seam": [
        0.65,
        0.66,
        0.61
      ],
      "dark": [
        0.14,
        0.18,
        0.2
      ],
      "eye": [
        0.75,
        0.76,
        0.68
      ]
    }
  },
  "PartedStriker": {
    "id": "ashbound-parted-striker",
    "role": "close-quarters pressure",
    "bulk": 1.16,
    "stride": 0.17,
    "colors": {
      "skin": [
        0.44,
        0.49,
        0.51
      ],
      "mantle": [
        0.33,
        0.43,
        0.47
      ],
      "garment": [
        0.19,
        0.21,
        0.27
      ],
      "salvage": [
        0.43,
        0.39,
        0.36
      ],
      "hardware": [
        0.78,
        0.32,
        0.22
      ],
      "seam": [
        0.65,
        0.66,
        0.61
      ],
      "dark": [
        0.14,
        0.18,
        0.2
      ],
      "eye": [
        0.75,
        0.76,
        0.68
      ]
    }
  }
,
  "SeamCastigator": {
    "id":"ashbound-seam-castigator",
    "role":"ranged fracture-line disruption",
    "bulk":0.83,
    "stride":0.085,
    "colors":{
      "skin":[0.43,0.48,0.50],
      "mantle":[0.30,0.42,0.45],
      "garment":[0.17,0.23,0.29],
      "salvage":[0.35,0.41,0.45],
      "hardware":[0.72,0.46,0.31],
      "seam":[0.68,0.68,0.61],
      "dark":[0.12,0.16,0.19],
      "eye":[0.72,0.78,0.72]
    }
  },
  "AnchorWrecker": {
    "id":"ashbound-anchor-wrecker",
    "role":"slow armored defensive-position disruption",
    "bulk":1.43,
    "stride":0.035,
    "colors":{
      "skin":[0.43,0.48,0.50],
      "mantle":[0.32,0.43,0.47],
      "garment":[0.25,0.24,0.26],
      "salvage":[0.46,0.40,0.37],
      "hardware":[0.76,0.36,0.27],
      "seam":[0.61,0.58,0.55],
      "dark":[0.15,0.17,0.19],
      "eye":[0.74,0.76,0.69]
    }
  },
  "AfterechoSeer": {
    "id":"ashbound-afterecho-seer",
    "role":"fragile long-range distortion and sensory pressure",
    "bulk":0.76,
    "stride":0.11,
    "colors":{
      "skin":[0.42,0.47,0.49],
      "mantle":[0.30,0.41,0.48],
      "garment":[0.18,0.21,0.28],
      "salvage":[0.31,0.34,0.41],
      "hardware":[0.55,0.43,0.63],
      "seam":[0.66,0.70,0.75],
      "dark":[0.13,0.16,0.22],
      "eye":[0.73,0.77,0.80]
    }
  },
  "CampWitness": {
    "id":"ashbound-camp-witness",
    "role":"camp-support observation and surveillance",
    "bulk":0.98,
    "stride":0.025,
    "colors":{
      "skin":[0.44,0.48,0.50],
      "mantle":[0.36,0.42,0.43],
      "garment":[0.28,0.27,0.25],
      "salvage":[0.41,0.43,0.39],
      "hardware":[0.59,0.48,0.33],
      "seam":[0.67,0.65,0.58],
      "dark":[0.17,0.18,0.19],
      "eye":[0.78,0.76,0.69]
    }
  }

};
function add(a,b){return a.map((x,i)=>x+b[i])}

function sub(a,b){return a.map((x,i)=>x-b[i])}

function mul(a,s){return a.map(x=>x*s)}

function cross(a,b){return [a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]]}

function dot(a,b){return a.reduce((s,x,i)=>s+x*b[i],0)}

function unit(a){return mul(a,1/Math.max(1e-10,Math.sqrt(dot(a,a))))}

function makeUnit(key){
 const spec=DATA[key],parts=[];function part(name,mat,verts,faces){if(verts.some(v=>v.some(n=>!Number.isFinite(n))))throw Error(name+" invalid vertex");if(faces.length===0)throw Error(name+" zero faces");parts.push({name,mat,verts,faces})}
 function loft(name,mat,controls,sides=24){
  const rings=[];for(let i=0;i<controls.length-1;i++){let a=controls[Math.max(i-1,0)],b=controls[i],c=controls[i+1],d=controls[Math.min(i+2,controls.length-1)];for(let n=0;n<4;n++){let t=n/4;let row=[];for(let j=0;j<5;j++){row.push(Math.max(j>=3?.0009:-1e6,.5*(2*b[j]+(-a[j]+c[j])*t+(2*a[j]-5*b[j]+4*c[j]-d[j])*t*t+(-a[j]+3*b[j]-3*c[j]+d[j])*t*t*t)))}rings.push(row)}}rings.push(controls[controls.length-1]);const verts=[],faces=[];let last=null;for(let i=0;i<rings.length;i++){const center=rings[i].slice(0,3);const axis=unit(sub(rings[Math.min(i+1,rings.length-1)].slice(0,3),rings[Math.max(0,i-1)].slice(0,3)));const guide=Math.abs(axis[2])<.87?[0,0,1]:[0,1,0];const a=last?unit(sub(last,mul(axis,dot(last,axis)))):unit(cross(axis,guide)),b=unit(cross(axis,a));last=a;for(let j=0;j<sides;j++){const theta=2*Math.PI*j/sides;verts.push(add(center,add(mul(a,rings[i][3]*Math.cos(theta)),mul(b,rings[i][4]*Math.sin(theta)))))}if(i){for(let j=0;j<sides;j++){let x=(i-1)*sides+j,y=(i-1)*sides+(j+1)%sides,z=y+sides,w=x+sides;faces.push([x,y,z],[x,z,w])}}}for(const [i,reverse] of [[0,true],[rings.length-1,false]]){const cap=verts.length;verts.push(rings[i].slice(0,3));for(let j=0;j<sides;j++){const a=i*sides+j,b=i*sides+(j+1)%sides;faces.push(reverse?[cap,b,a]:[cap,a,b])}}part(name,mat,verts,faces);
 }
 function oval(name,mat,c,r,sides=30){const rings=[];for(let i=0;i<15;i++){const t=-Math.PI/2+.025+(Math.PI-.05)*i/14;rings.push([...add(c,[0,r[1]*Math.sin(t),0]),r[0]*Math.cos(t),r[2]*Math.cos(t)])}loft(name,mat,rings,sides)}
 function wire(name,mat,points,r=.005){loft(name,mat,points.map(p=>[...p,r,r]),10)}
 function sheet(name,mat,fn,nu=28,nv=8,thickness=.006){const verts=[],faces=[],n=nu*nv;for(let side of [-1,1])for(let i=0;i<nu;i++)for(let j=0;j<nv;j++){let p=fn(i/(nu-1),j/(nv-1));verts.push([p[0],p[1],p[2]+side*thickness/2])}for(let k=0;k<2;k++)for(let i=0;i<nu-1;i++)for(let j=0;j<nv-1;j++){let a=k*n+i*nv+j,b=a+nv,c=b+1,d=a+1;faces.push(k?[a,c,b]:[a,b,c],k?[a,d,c]:[a,c,d])}const edge=[];for(let i=0;i<nu;i++)edge.push(i*nv);for(let j=1;j<nv;j++)edge.push((nu-1)*nv+j);for(let i=nu-2;i>=0;i--)edge.push(i*nv+nv-1);for(let j=nv-2;j>=1;j--)edge.push(j);for(let i=0;i<edge.length;i++){let a=edge[i],b=edge[(i+1)%edge.length];faces.push([a,b,b+n],[a,b+n,a+n])}part(name,mat,verts,faces)}
 const bulk=spec.bulk,stride=spec.stride,heavy=key==="PartedStriker";
 // Avarin anatomy: bipedal torso, living crescent sensory mantle, temple-face, no helmet substitution.
 loft("Avarin_Torso","garment",[[0,.98,-.015,.140*bulk,.10],[0,1.17,-.022,.182*bulk,.12],[0,1.37,-.028,.200*bulk,.126],[0,1.46,-.050,.127,.095]],34);
 loft("Avarin_Pelvis","garment",[[0,.77,-.03,.126*bulk,.098],[0,.93,-.022,.163*bulk,.12],[0,1.06,-.02,.148*bulk,.11]],28);
 loft("Avarin_Neck","skin",[[0,1.44,-.015,.060,.050],[0,1.56,-.013,.064,.056]],24);
 oval("Avarin_TempleFace","skin",[0,1.67,.013],[.106,.152,.101],34);
 wire("Avarin_ForeheadRidge","skin",[[0,1.71,.108],[0,1.738,.092],[0,1.754,.071]],.010);
 wire("Avarin_Mouth","dark",[[-.028,1.597,.101],[0,1.590,.109],[.028,1.597,.101]],.003);
 for(let s of [-1,1]){const side=s<0?"L":"R",g=stride*(s<0?-.26:1);
  loft("Avarin_Leg_"+side,"garment",[[s*.102,.91,-.024,.094*bulk,.094],[s*.12,.67,g*.22,.083,.080],[s*.133,.48,g*.52,.073,.071],[s*.147,.29,g*.76,.054,.060],[s*.154,.107,g,.050,.053]],28);
  oval("Avarin_Foot_"+side,"dark",[s*.158,.063,.091+g],[.067,.061,.145],24);
  loft("Avarin_Arm_"+side,"garment",[[s*.180*bulk,1.405,-.025,.087,.074],[s*.245*bulk,1.23,.018,.073,.060],[s*(heavy?.284:.262),1.05,.072,.052,.050],[s*.252,.894,.129,.036,.040]],24);
  loft("Avarin_Hand_"+side,"skin",[[s*.252,.897,.125,.029,.023],[s*.253,.850,.154,.026,.025],[s*.25,.825,.167,.015,.014]],18);
  oval("Avarin_Eye_"+side,"eye",[s*.044,1.68,.110],[.019,.011,.008],16);
  wire("Avarin_EyeUpper_"+side,"skin",[[s*.027,1.683,.111],[s*.045,1.692,.111],[s*.061,1.682,.108]],.004);
  loft("Avarin_TempleRelief_"+side,"seam",[[s*.073,1.714,.077,.006,.003],[s*.10,1.673,.076,.009,.004],[s*.083,1.627,.090,.006,.003]],14);
 }
 const mantle=(t,v)=>{let angle=.12+(Math.PI-.24)*t,r=.158+.151*v;return [r*Math.cos(angle),1.49+r*.89*Math.sin(angle),-.115-.031*v+.012*Math.sin(3*angle)]};
 sheet("Avarin_LivingCrescentMantle","mantle",mantle,44,9,.008);
 wire("Avarin_CrescentRim","seam",Array.from({length:37},(_,i)=>mantle(i/36,1)),.0065);
 for(let s of [-1,1])wire("Avarin_MantleSensingRibbon_"+(s<0?"L":"R"),"mantle",[[s*.256,1.51,-.16],[s*.280,1.405,-.176],[s*.304,1.30,-.162]],.006);
 // Salvaged split fabric and engine hardware; Ashbound have no bonded partners.
 loft("Salvage_ChestBraid","dark",[[-.15,1.41,.072,.028,.018],[-.060,1.23,.115,.033,.020],[.132,1.035,.081,.026,.013]],16);
 loft("Salvage_WaistHousing","salvage",[[0,.967,.101,.15*bulk,.031],[0,1.019,.107,.16*bulk,.036]],24);
 sheet("Salvage_SplitFieldApron","garment",(t,v)=>[-.16+.32*v+.012*Math.sin(5*v+2*t),.968-.33*t,.03+.113*t+.016*Math.sin(Math.PI*t)],26,14,.009);
 for(let s of [-1,1]){let side=s<0?"L":"R";
  loft("Salvage_ShinShell_"+side,"salvage",[[s*.146,.365,.044,.046,.021],[s*.152,.267,.059,.055,.025],[s*.161,.150,.073,.034,.019]],22);
  wire("Salvage_BootEdge_"+side,"seam",[[s*.11,.105,.17],[s*.154,.08,.205],[s*.209,.09,.174]],.004);
 }
 oval("Wake_EngineeredCaptureHousing","salvage",[heavy?.15:-.15,1.16,-.134],[.082,.119,.052],24);
 for(let k=0;k<3;k++){let x=heavy?.15:-.15,y=1.10+.066*k;wire("Wake_Conduit_"+k,"hardware",[[x-.075,y,-.183],[x,y+.01,-.20],[x+.075,y,-.183]],.008)}
 wire("Wake_ReturnFeed","dark",[[heavy?.15:-.15,1.07,-.18],[heavy?.23:-.20,.94,-.176],[heavy?.15:-.12,.82,.023]],.011);
 if(key==="RiftForager"){
  loft("Forager_OffsetShoulder","salvage",[[.184,1.435,-.021,.052,.025],[.265,1.36,.012,.096,.058],[.278,1.25,.056,.052,.032]],30);
  oval("Forager_LowRouteSatchel","salvage",[-.220,1.002,-.07],[.102,.156,.082],28);
  loft("Forager_SatchelTie","dark",[[-.28,1.10,-.121,.023,.015],[-.161,1.324,-.137,.019,.012],[-.035,1.41,.001,.023,.011]],14);
  loft("Forager_Probe","salvage",[[.278,.89,.225,.024,.023],[.320,1.05,.375,.017,.016],[.348,1.238,.452,.012,.012]],18);
  oval("Forager_SensorEnd","hardware",[.350,1.241,.453],[.029,.036,.026],18);
  for(let k=0;k<5;k++){let x=-.32-.024*k;wire("Forager_TrailTags_"+k,"seam",[[x,.94,-.119],[x-.018,.82-k*.018,-.112],[x+.014,.78-k*.018,-.103]],.005)}
  for(let k=0;k<6;k++){let x=-.132+.048*k;wire("Forager_FieldRepairs_"+k,"seam",[[x,.940,.109],[x+.006,.814,.151],[x-.018,.71,.178]],.0027)}
 }else if(key==="PartedStriker"){
  loft("Striker_OverbuiltRightShoulder","salvage",[[.18,1.47,-.033,.054,.035],[.288,1.383,-.029,.145,.076],[.315,1.252,.052,.077,.041]],32);
  loft("Striker_RightForearmPlate","salvage",[[.30,1.115,.072,.057,.046],[.324,1.024,.105,.091,.082],[.318,.912,.133,.058,.049]],30);
  loft("Striker_ImpactRidge","seam",[[.345,1.118,.13,.020,.013],[.348,1.004,.176,.031,.023],[.316,.914,.190,.015,.012]],18);
  loft("Striker_BrokenLeftShoulder","salvage",[[-.18,1.46,-.01,.044,.027],[-.252,1.366,-.022,.090,.044],[-.273,1.299,.014,.039,.020]],26);
  sheet("Striker_AsymmetricSalvageDrape","salvage",(t,v)=>[-.346+.189*v,1.360-.305*t,.005+.024*Math.sin(t*Math.PI)],22,10,.007);
  loft("Striker_AfterechoStabilizer","salvage",[[.152,1.245,-.17,.047,.036],[.174,1.162,-.181,.065,.044],[.149,1.052,-.15,.040,.031]],26);
  for(let k=0;k<4;k++){let z=-.14-k*.014;wire("Striker_StabilizerVane_"+k,"hardware",[[.059+k*.030,1.243,z],[.071+k*.025,1.156,z-.037],[.081+k*.019,1.06,z]],.006)}
  oval("Striker_ImpactNode","hardware",[.345,1.01,.169],[.028,.045,.028],22);
  for(let k=0;k<6;k++){let x=-.131+k*.051;wire("Striker_ApronSeam_"+k,"seam",[[x,.943,.118],[x+.013,.831,.162],[x-.014,.70,.178]],.003)}

 }else if(key==="SeamCastigator"){
  // Thin ranged specialist. External capture lattice is NOT a staff, gun or body organ.
  // Distinctive split high shoulder-shawl, asymmetric shoulder mast, and exposed coils.
  const fold=(t,v)=>[-.26+.47*v+.019*Math.sin(4*v+t),1.38-.31*t,-.053+.07*Math.sin(Math.PI*t)+.027*v];
  sheet("Castigator_BrokenFieldShawl","salvage",fold,26,13,.010);
  for(let q of [.02,.25,.5,.75,.98]){
    wire("Castigator_ShawlSeam_"+Math.round(q*100),"seam",
      Array.from({length:13},(_,i)=>fold(i/12,q)),.0045);
  }
  loft("Castigator_HighRightShoulder","salvage",[
    [.15,1.455,-.044,.040,.025],[.250,1.40,-.067,.075,.045],
    [.296,1.294,-.027,.042,.035]],26);
  oval("Castigator_BackFractureSpool","salvage",[-.12,1.145,-.158],[.099,.133,.053],26);
  for(let i=0;i<7;i++){
    const a=i*Math.PI*2/7;
    wire("Castigator_SpoolChannel_"+i,"hardware",[
      [-.12+.081*Math.cos(a),1.145+.09*Math.sin(a),-.205],
      [-.12+.069*Math.cos(a),1.145+.071*Math.sin(a),-.221],
      [-.12+.046*Math.cos(a),1.145+.045*Math.sin(a),-.232]],.005);
  }
  loft("Castigator_ArmCaptureBrace","salvage",[
    [.250,1.15,.063,.053,.034],[.300,1.053,.119,.080,.056],
    [.314,.947,.164,.067,.043]],28);
  loft("Castigator_OpenArcFrame","hardware",[
    [.308,.976,.135,.042,.028],[.320,1.083,.214,.057,.044],
    [.317,1.171,.265,.042,.021]],20);
  for(let i=0;i<7;i++){
    const y=.977+i*.025;
    wire("Castigator_FieldLattice_"+i,"seam",[
      [.283,y,.181],[.341,y+.014,.221],[.361,y+.026,.182]],.004);
  }
  for(let i=0;i<4;i++){
    wire("Castigator_TraceLead_"+i,"hardware",[
      [-.21+i*.012,1.32,-.157],[-.19+i*.008,1.15,-.220],
      [-.17+i*.018,1.012,-.192]],.008);
  }
  // Offset counterweight keeps profile recognizably equipment-based at phone scale.
  oval("Castigator_LeftBalanceWeight","salvage",[-.263,.875,-.088],[.100,.117,.080],26);
  loft("Castigator_CounterweightWeb","dark",[
    [-.223,1.21,-.100,.019,.015],[-.270,1.04,-.123,.027,.021],
    [-.283,.854,-.119,.017,.011]],16);
  for(let i=0;i<5;i++){
    const t=i/4;
    wire("Castigator_FieldRepair_"+i,"seam",[
      [-.157+t*.314,.895,.148],[-.166+t*.315,.832,.155],
      [-.145+t*.307,.735,.171]],.0033);
  }
 }else if(key==="AnchorWrecker"){
  // Dense breaker: massive salvage bracing over an ordinary 1.82m Avarin.
  // The wide impact pad is a contact tool for unstable anchors, not a firearm.
  loft("Wrecker_LeftLoadShoulder","salvage",[
    [-.19,1.49,-.026,.053,.039],[-.308,1.39,-.036,.157,.086],
    [-.366,1.265,-.019,.092,.056]],36);
  loft("Wrecker_RightCounterShoulder","salvage",[
    [.187,1.468,-.066,.045,.025],[.288,1.37,-.089,.092,.056],
    [.286,1.28,-.050,.060,.034]],30);
  loft("Wrecker_BroadTorsoHarness","dark",[
    [0,1.07,.103,.209,.029],[0,1.24,.108,.248,.053],
    [0,1.382,.089,.226,.036]],36);
  for(let s of [-1,1]){
    const side=s<0?"L":"R";
    for(let i=0;i<4;i++){
      const y=1.10+i*.07;
      loft("Wrecker_RibPlate_"+side+"_"+i,"salvage",[
        [s*.105,y,.136,.084,.027],[s*.201,y+.026,.078,.094,.032],
        [s*.265,y+.044,.005,.040,.021]],20);
      wire("Wrecker_RibCrack_"+side+"_"+i,"seam",[
        [s*.091,y+.015,.161],[s*.168,y+.032,.131],
        [s*.241,y+.057,.057]],.005);
    }
    loft("Wrecker_LegBrace_"+side,"salvage",[
      [s*.135,.725,.012,.066,.040],[s*.162,.489,.061,.092,.071],
      [s*.167,.276,.072,.074,.046],[s*.173,.128,.082,.045,.027]],28);
  }
  // A left-mounted short, broad ram delivers close pressure without a rifle profile.
  loft("Wrecker_LeftContactBrace","salvage",[
    [-.288,1.13,.095,.070,.059],[-.315,.985,.171,.127,.097],
    [-.327,.878,.208,.135,.105]],34);
  loft("Wrecker_BluntAnchorPad","hardware",[
    [-.333,.889,.203,.142,.081],[-.333,.845,.283,.157,.093],
    [-.333,.795,.294,.140,.075]],36);
  for(let i=0;i<5;i++){
    const x=-.45+i*.059;
    wire("Wrecker_RamReinforcement_"+i,"seam",[
      [x,.923,.293],[x,.853,.362],[x,.786,.313]],.0075);
  }
  // Weighted cassettes are engineered Arcana containment, not sacrificial souls.
  oval("Wrecker_LeftBallastCassette","salvage",[-.270,.946,-.155],[.155,.186,.095],30);
  oval("Wrecker_RightBallastCassette","salvage",[.230,1.037,-.157],[.105,.145,.077],28);
  for(let i=0;i<6;i++){
    const y=.838+i*.049;
    wire("Wrecker_CaptureVent_"+i,"hardware",[
      [-.378,y,-.176],[-.320,y+.016,-.240],
      [-.202,y,-.232]],.008);
  }
  for(let i=0;i<4;i++){
    const x=-.11+i*.066;
    loft("Wrecker_WaistLatch_"+i,"seam",[
      [x,.88,.083,.014,.010],[x,.934,.163,.018,.012],
      [x,.994,.127,.014,.012]],14);
  }
 }
 finalPair(key,{loft,oval,wire,sheet});
 const pts=parts.flatMap(p=>p.verts);
 const bounds=[[0,1,2].map(k=>Math.min(...pts.map(p=>p[k]))),[0,1,2].map(k=>Math.max(...pts.map(p=>p[k])))];
 let offset=1;
 const obj=["# NINEFOLD / ASHBOUND — static anatomy and salvage study 01","# Proposal only; not approved canon or runtime battle art","# Meters, +Y up, +Z forward","mtllib "+key+".mtl"];
 for(const p of parts){obj.push("o "+p.name,"usemtl "+p.mat,"s 1");for(const pt of p.verts)obj.push("v "+pt.map(x=>x.toFixed(6)).join(" "));for(const f of p.faces)obj.push("f "+f.map(n=>n+offset).join(" "));offset+=p.verts.length}
 const mtl=["# Proposed material swatches, not locked final colors"];
 for(const [mat,col] of Object.entries(spec.colors)){mtl.push("newmtl "+mat,"Kd "+col.map(x=>x.toFixed(5)).join(" "),"Ns 34.0","d 1.0","")}
 const manifest={id:spec.id,model:key,revision:"ashbound-enemy-art-study-01",canon_source:"Docs/Canon/WorldFiveAshboundCanonAddendum01.md",status:"provisional static 3D source; not approved for runtime",units:"meters",up:"Y",forward:"Z",bipedal_avarin:true,living_crescent_mantle:true,bonded_creature_count:0,role:spec.role,parts:parts.length,vertices:offset-1,triangles:parts.reduce((n,p)=>n+p.faces.length,0),bounds,rigged:false,mobile_certified:false,limitations:["No rig, authored motion, production UVs, texture maps or LODs","Review the anatomy against locked Avarin World Five showcase","Afterecho equipment / salvage silhouettes are proposals, not new biological lore","Not registered to any playable mission or gameplay visual catalog"]};
 if(!(bounds[1][1]>1.75&&bounds[1][1]<1.95)||bounds[0][1]<-.01)throw Error("Avarin envelope invalid: "+JSON.stringify(bounds));
 if(manifest.triangles<14000)throw Error("insufficient geometry "+manifest.triangles);
 return {obj:obj.join("\n")+"\n",mtl:mtl.join("\n")+"\n",manifest:JSON.stringify(manifest,null,2)+"\n"}
}
for(const key of Object.keys(DATA)){
  const art=makeUnit(key),dest=path.join('SourceArt','Ashbound',key);
  fs.mkdirSync(dest,{recursive:true});
  for(const [extension,content] of [['obj',art.obj],['mtl',art.mtl],['json',art.manifest]]){
    const name=extension==='json'?'manifest.json':key+'.'+extension;
    fs.writeFileSync(path.join(dest,name),content,'utf8');
  }
  console.log(key+' verified: '+JSON.parse(art.manifest).triangles+' triangles');
}
