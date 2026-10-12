using UnityEngine;
namespace BurebistaAnimalTrophies;
internal static class NaturalPlaques {
 static Material wood;
 internal static GameObject Visual(int kind){
  if(wood==null){
   var deer=NativeModels.Template(3,true);
   foreach(var renderer in deer.GetComponentsInChildren<MeshRenderer>(true)){
    wood=renderer.sharedMaterials.FirstOrDefault(m=>m!=null&&m.name.Contains("Wood",StringComparison.OrdinalIgnoreCase));
    if(wood!=null)break;
   }
   if(wood==null)throw new Exception("No se encuentra el material de madera del ciervo");
  }
  // Use a bounded shield in metres, without trimming the native mesh or
  // inheriting nested prefab transforms from the mounted deer.
  var result=new GameObject("NaturalWoodenMount");
  result.AddComponent<MeshFilter>().sharedMesh=Models.Plaque(kind);
  result.AddComponent<MeshRenderer>().sharedMaterial=wood;
  return result;
 }
}
