// Distinct, review-only geometry for the two final ordinary Ashbound NPC archetypes.
// The procedural study is not an approved production sculpt or gameplay prefab.
// Primitive surface helpers come from the existing Avarin study builder.
export function finalPair(key, {loft, oval, wire, sheet}) {
  if (key === "AfterechoSeer") {
    // Avarin mantle and temple-face remain visible. No creature and no soul device.
    loft("Seer_OffsetLeftShoulder","salvage",[
      [-.133,1.445,-.052,.033,.025],[-.214,1.390,-.081,.085,.041],
      [-.223,1.320,-.052,.039,.028]],26);
    const drape=(t,v)=>[-.225+.292*v+.020*Math.sin(t*3+v*8),1.315-.475*t,
      -.068-.027*Math.sin(Math.PI*t)+.038*Math.sin(4*v)];
    sheet("Seer_TatteredSignalDrape","garment",drape,30,12,.009);
    for(let i=0;i<5;i++) {
      const f=.07+i*.215;
      wire("Seer_DrapeConduit_"+i,"seam",
        Array.from({length:15},(_,k)=>drape(k/14,f)),.0039);
    }
    // The two offset baffles are exposed capture hardware, not sensory biology.
    loft("Seer_RightReflectorArm","salvage",[
      [.159,1.335,-.125,.023,.025],[.288,1.468,-.153,.027,.032],
      [.354,1.582,-.173,.021,.022]],20);
    sheet("Seer_RightFieldPetal","hardware",(t,v)=>[
      .312+.118*v+.010*Math.sin(Math.PI*t),
      1.43+.215*t-.080*v,
      -.174-.029*Math.sin(Math.PI*t)],22,12,.010);
    wire("Seer_RightPetalRim","seam",Array.from({length:23},(_,i)=>{
      const t=i/22;
      return [.430+.010*Math.sin(Math.PI*t),1.350+.215*t,
        -.174-.029*Math.sin(Math.PI*t)];
    }),.006);
    loft("Seer_LeftBentReflectorArm","salvage",[
      [-.156,1.360,-.130,.024,.021],[-.263,1.445,-.168,.030,.029],
      [-.300,1.474,-.160,.018,.017]],18);
    sheet("Seer_LeftBrokenPanel","hardware",(t,v)=>[
      -.257-.065*v+.016*Math.sin(t*6),
      1.410+.150*t-.055*v,-.169-.020*Math.sin(t*3)],19,11,.009);
    oval("Seer_OverloadedCaptureCassette","salvage",
      [.087,1.066,-.163],[.102,.157,.054],26);
    for(let i=0;i<8;i++) {
      const a=i*Math.PI/4;
      wire("Seer_InterferenceConduit_"+i,"hardware",[
        [.087+.083*Math.cos(a),1.064+.123*Math.sin(a),-.202],
        [.087+.078*Math.cos(a+.12),1.064+.116*Math.sin(a+.12),-.239],
        [.087+.059*Math.cos(a),1.064+.090*Math.sin(a),-.246]],.0049);
    }
    for(let s of [-1,1]) {
      const side=s<0?"L":"R";
      loft("Seer_ForearmStabilizer_"+side,"salvage",[
        [s*.238,1.124,.069,.039,.029],[s*.267,1.006,.111,.059,.038],
        [s*.255,.920,.132,.036,.025]],22);
      wire("Seer_EquipmentTether_"+side,"hardware",[
        [s*.193,1.345,-.110],[s*.235,1.187,-.173],
        [s*.277,1.045,-.179],[s*.250,.935,.153]],.008);
      for(let j=0;j<3;j++){
        const y=1.028+j*.029;
        wire("Seer_BraceRib_"+side+"_"+j,"seam",[
          [s*.210,y,.125],[s*.262,y+.014,.153],
          [s*.302,y+.021,.110]],.0036);
      }
    }
    // Physical misalignment readouts communicate Afterecho's harmful instability.
    for(let j=0;j<5;j++) {
      const y=.83+j*.045;
      wire("Seer_AfterechoReadout_"+j,"seam",[
        [-.090,y,.153],[-.036+j*.016,y+.014,.167],
        [.079+j*.009,y-.007,.142]],.0032);
    }
  } else if (key === "CampWitness") {
    // Ordinary camp support observer. Neither boss nor creature handler.
    loft("Witness_LeftLayeredShoulder","salvage",[
      [-.148,1.452,-.044,.047,.031],[-.246,1.388,-.042,.117,.062],
      [-.264,1.303,.030,.061,.044]],28);
    loft("Witness_RightOpenShoulder","salvage",[
      [.150,1.429,-.057,.039,.025],[.216,1.378,-.071,.065,.048],
      [.211,1.309,-.027,.031,.028]],24);
    sheet("Witness_OffsetFieldOvercoat","garment",(t,v)=>[
      -.292+.524*v-.017*Math.sin(Math.PI*t),
      1.229-.541*t,
      -.016+.052*Math.sin(Math.PI*t)+.037*Math.sin(3.5*v)],26,15,.010);
    for(let i=0;i<6;i++){
      const v=.05+i*.18;
      wire("Witness_CoatRepairSeam_"+i,"seam",
        Array.from({length:15},(_,k)=>{
          const t=k/14;
          return [-.292+.524*v-.017*Math.sin(Math.PI*t),
            1.229-.541*t,.001+.052*Math.sin(Math.PI*t)+.037*Math.sin(3.5*v)];
        }),.0036);
    }
    // Physical campaign-gear registry rather than a spirit/animal artifact.
    loft("Witness_RightInventoryHinge","dark",[
      [.188,1.284,.092,.022,.017],[.292,1.118,.096,.030,.023],
      [.287,.968,.084,.022,.020]],20);
    oval("Witness_RightRegistrySatchel","salvage",
      [.275,.903,.034],[.132,.172,.079],26);
    sheet("Witness_RegistryFacePlate","salvage",(t,v)=>[
      .191+.160*v,.817+.176*t,.109+.012*Math.sin(Math.PI*t)],17,14,.010);
    for(let k=0;k<5;k++) {
      const y=.835+k*.028;
      wire("Witness_RegistryMarks_"+k,"seam",[
        [.222,y,.128],[.270,y+.004,.125],
        [.323-(k%2)*.045,y,.123]],.0033);
    }
    oval("Witness_BackSignalRelay","salvage",
      [-.140,1.129,-.162],[.105,.107,.058],27);
    for(let j=0;j<6;j++) {
      const a=j*Math.PI/3;
      wire("Witness_RelayChannel_"+j,"hardware",[
        [-.14+.071*Math.cos(a),1.129+.073*Math.sin(a),-.209],
        [-.14+.045*Math.cos(a),1.129+.050*Math.sin(a),-.239],
        [-.14+.021*Math.cos(a),1.129+.021*Math.sin(a),-.221]],.005);
    }
    loft("Witness_LeftSupportArmBracket","salvage",[
      [-.242,1.136,.048,.052,.040],[-.286,1.045,.090,.086,.054],
      [-.289,.954,.105,.057,.037]],24);
    sheet("Witness_FoldedCampSignalPlate","salvage",(t,v)=>[
      -.36+.163*v,.977+.243*t,
      .181+.013*Math.sin(t*Math.PI)+.014*v],26,12,.012);
    for(const v of [.02,.49,.98]){
      wire("Witness_PlateChannel_"+Math.round(v*100),"hardware",
        Array.from({length:17},(_,i)=>{
          const t=i/16;
          return [-.36+.163*v,.977+.243*t,
            .189+.013*Math.sin(t*Math.PI)+.014*v];
        }),.004);
    }
    loft("Witness_HipLoadWeb","dark",[
      [-.20,1.144,-.030,.031,.018],[-.240,.964,-.045,.027,.020],
      [.212,.894,-.010,.033,.021]],18);
    oval("Witness_LeftCampPack","salvage",
      [-.270,.830,-.058],[.123,.142,.077],28);
    for(let j=0;j<4;j++){
      wire("Witness_CampPackStrap_"+j,"seam",[
        [-.325+j*.024,.910,-.032],
        [-.335+j*.024,.811,-.001],
        [-.311+j*.023,.742,-.027]],.0048);
    }
  }
}
