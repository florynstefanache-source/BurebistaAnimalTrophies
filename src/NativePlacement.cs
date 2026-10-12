using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;
namespace BurebistaAnimalTrophies;
// Apply only to prepared trophies; all other gear keeps the game's rules.
internal static class TrophyNativePlacement {
 internal static GearItem Current(PlayerManager player){
  var gear=player.m_ObjectToPlaceGearItem;
  return gear!=null&&Rules.Parse(gear.name,out _,out bool ready)&&ready?gear:null;
 }
}
[HarmonyPatch(typeof(PlayerManager),nameof(PlayerManager.CanPlaceCurrentPlaceable))]
static class TrophyAllowPlacement {
 static bool Prefix(PlayerManager __instance,ref bool __result){
  if(TrophyNativePlacement.Current(__instance)==null)return true;
  __result=true;return false;
 }
}
[HarmonyPatch(typeof(PlayerManager),nameof(PlayerManager.UpdatePlaceMesh))]
static class TrophyWallPreview {
 static void Postfix(PlayerManager __instance){
  var gear=TrophyNativePlacement.Current(__instance);if(gear==null)return;
  var camera=GameManager.GetMainCamera();if(camera==null)return;
  var origin=camera.transform.position;var direction=camera.transform.forward;
  foreach(var hit in Physics.RaycastAll(origin,direction,8f,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance)){
   if(hit.collider==null||hit.collider.GetComponentInParent<GearItem>()==gear||hit.distance<.15f)continue;
   // Upright shield with its rear surface against either face of a wall.
   if(Mathf.Abs(hit.normal.y)<.8f){
    gear.transform.position=hit.point+hit.normal*.006f;
    gear.transform.rotation=Quaternion.LookRotation(hit.normal,Vector3.up);
   }
   break;
  }
 }
}
[HarmonyPatch(typeof(PlayerManager),nameof(PlayerManager.PlaceMeshInWorld))]
static class TrophyRememberNativePlacement {
 static void Prefix(PlayerManager __instance,out GearItem __state){__state=TrophyNativePlacement.Current(__instance);}
 static void Postfix(GearItem __state){
  if(__state==null||__state.m_InPlayerInventory)return;
  try{Main.Instance?.RememberNativeMount(__state);}catch(Exception e){MelonLogger.Error("Guardar colocación del trofeo: "+e);HUDMessage.AddMessage(TrophyLanguage.Text("No se pudo guardar la colocación del trofeo; consulta Latest.log."));}
 }
}
public partial class Main {
 internal void RememberNativeMount(GearItem gear){
  if(!EnsureState())throw new Exception(TrophyLanguage.Text("No hay guardado activo"));
  string id=GearId(gear);if(string.IsNullOrEmpty(id)){ObjectGuid.MaybeAttachObjectGuidAndRegister(gear.gameObject,Guid.NewGuid().ToString());id=GearId(gear);}
  if(string.IsNullOrEmpty(id))throw new Exception(TrophyLanguage.Text("Falta identidad del trofeo"));
  Freeze(gear);var p=gear.transform.position;var q=gear.transform.rotation;
  state.Mounts.RemoveAll(m=>m.Id==id);
  state.Mounts.Add(new MountRecord{Id=id,Scene=Scene,Wall=WorldMount,X=p.x,Y=p.y,Z=p.z,Qx=q.x,Qy=q.y,Qz=q.z,Qw=q.w});SaveState();
  MelonLogger.Msg("Trofeo colocado con controles normales: "+gear.name+" / "+p);
 }
}
