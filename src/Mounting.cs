using Il2Cpp;
using MelonLoader;
using UnityEngine;
using Object=UnityEngine.Object;
namespace BurebistaAnimalTrophies;
public partial class Main {
 const string WorldMount="@world";
 int placementKind=-1;GameObject ghost;float mountRoll,mountDistance;
 string placementHint=TrophyLanguage.Text("Colocación libre");
 void StartPlacement(int kind){CancelPlacement();ghost=Models.Visual(kind,true);ghost.name="BureTrophyPlacementPreview";placementKind=kind;mountRoll=0;mountDistance=1.5f;PlacementTick();}
 void CancelPlacement(){if(ghost!=null)Object.Destroy(ghost);ghost=null;placementKind=-1;}
 void PlacementTick(){
  if(placementKind<0)return;
  if(Input.GetKeyDown(KeyCode.Escape)){CancelPlacement();return;}
  var camera=GameManager.GetMainCamera();if(camera==null)return;
  if(Input.GetKeyDown(KeyCode.LeftArrow))mountRoll-=5;
  if(Input.GetKeyDown(KeyCode.RightArrow))mountRoll+=5;
  float adjustment=(Input.GetKey(KeyCode.UpArrow)?1:0)-(Input.GetKey(KeyCode.DownArrow)?1:0);
  mountDistance=Mathf.Clamp(mountDistance+adjustment*Time.unscaledDeltaTime,.15f,8f);
  ghost.SetActive(true);
  ghost.transform.position=camera.transform.position+camera.transform.forward*mountDistance;
  ghost.transform.rotation=Quaternion.LookRotation(-camera.transform.forward,camera.transform.up)*Quaternion.Euler(0,0,mountRoll);
  placementHint=TrophyLanguage.Text("Colocación libre — Enter: fijar aquí · Distancia: ")+mountDistance.ToString("0.00")+" m";
  if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter))CommitMount();
 }
 static string GearId(GearItem gear)=>ObjectGuid.GetGuidFromGameObject(gear.gameObject,false);
 void CommitMount(){
  if(placementKind<0||ghost==null||GameManager.GetMainCamera()==null)return;
  var gear=GameManager.GetInventoryComponent().GetHighestConditionGearThatMatchesName(Rules.Gear(placementKind,true));
  if(gear==null){HUDMessage.AddMessage(TrophyLanguage.Text("Ya no tienes ese trofeo preparado"));CancelPlacement();return;}
  string id=GearId(gear);if(string.IsNullOrEmpty(id)){ObjectGuid.MaybeAttachObjectGuidAndRegister(gear.gameObject,Guid.NewGuid().ToString());id=GearId(gear);}
  if(string.IsNullOrEmpty(id))throw new Exception(TrophyLanguage.Text("El trofeo no tiene identidad persistente; no se ha colocado"));
  gear=gear.Drop(1,false,false,false)??gear;
  if(gear==null||gear.m_InPlayerInventory)throw new Exception(TrophyLanguage.Text("No se pudo sacar el trofeo de la mochila"));
  Freeze(gear);gear.transform.position=ghost.transform.position;gear.transform.rotation=ghost.transform.rotation;
  var local=gear.transform.position;var rotation=gear.transform.rotation;
  state.Mounts.RemoveAll(m=>m.Id==id);
  state.Mounts.Add(new MountRecord{Id=id,Scene=Scene,Wall=WorldMount,X=local.x,Y=local.y,Z=local.z,Qx=rotation.x,Qy=rotation.y,Qz=rotation.z,Qw=rotation.w});
  SaveState();CancelPlacement();HUDMessage.AddMessage(TrophyLanguage.Text("Trofeo colgado. Ctrl+Alt+F11 y 6 para recogerlo"));
 }
 static void Freeze(GearItem gear){var body=gear.m_RigidBody;if(body!=null){if(!body.isKinematic){body.velocity=Vector3.zero;body.angularVelocity=Vector3.zero;}body.isKinematic=true;}}
 void CollectMounted(){
  var camera=GameManager.GetMainCamera();if(camera==null)return;
  GearItem gear=null;
  foreach(var hit in Physics.RaycastAll(camera.transform.position,camera.transform.forward,4f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance)){
   var candidate=hit.collider.GetComponentInParent<GearItem>();
   if(candidate!=null&&Rules.Parse(candidate.name,out _,out bool prepared)&&prepared&&state.Mounts.Any(m=>m.Id==GearId(candidate))){gear=candidate;break;}
  }
  if(gear==null){HUDMessage.AddMessage(TrophyLanguage.Text("Apunta a un trofeo colgado cercano antes de abrir el menú y recogerlo."));return;}
  string id=GearId(gear);if(!state.Mounts.Any(m=>m.Id==id))return;
  var added=GameManager.GetPlayerManagerComponent().AddItemToPlayerInventory(gear,false,true);
  if(added!=null&&added.m_InPlayerInventory){state.Mounts.RemoveAll(m=>m.Id==id);SaveState();HUDMessage.AddMessage(TrophyLanguage.Text("Trofeo guardado en la mochila"));}
 }
 void MaintainMounts(){
  var records=state.Mounts.Where(m=>m.Scene==Scene).ToArray();if(records.Length==0)return;
  var wanted=records.Select(r=>r.Id).ToHashSet();var found=new Dictionary<string,GearItem>();
  foreach(var gear in Resources.FindObjectsOfTypeAll<GearItem>()){
   if(gear==null||!Rules.Parse(gear.name,out _,out _))continue;string id=GearId(gear);
   if(!string.IsNullOrEmpty(id)&&wanted.Contains(id))found[id]=gear;
  }
  bool changed=false;
  foreach(var record in records){
   if(!found.TryGetValue(record.Id,out var gear)||gear==null)continue;
   if(GameManager.GetPlayerManagerComponent()?.m_ObjectToPlaceGearItem==gear)continue;
   if(gear.m_InPlayerInventory||!gear.gameObject.activeInHierarchy){state.Mounts.Remove(record);changed=true;continue;}
   if(record.Wall==WorldMount){
    Freeze(gear);gear.transform.position=new Vector3(record.X,record.Y,record.Z);gear.transform.rotation=new Quaternion(record.Qx,record.Qy,record.Qz,record.Qw);continue;
   }
   var wall=GameObject.Find(record.Wall);
   if(wall==null){
    // Never parent native gear into a portable shelter: packing the shelter
    // must not destroy a decorated trophy. Return the same item, never a clone.
    var returned=GameManager.GetPlayerManagerComponent().AddItemToPlayerInventory(gear,false,true);
    if(returned!=null&&returned.m_InPlayerInventory){state.Mounts.Remove(record);changed=true;HUDMessage.AddMessage(TrophyLanguage.Text("Pared retirada: trofeo devuelto a tu mochila"));}
    continue;
   }
   Freeze(gear);gear.transform.position=wall.transform.TransformPoint(new Vector3(record.X,record.Y,record.Z));gear.transform.rotation=wall.transform.rotation*new Quaternion(record.Qx,record.Qy,record.Qz,record.Qw);
  }
  if(changed)SaveState();
 }
}

