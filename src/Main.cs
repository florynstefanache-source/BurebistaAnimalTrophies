using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using ModData;
using System.Reflection;
using System.Text.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
[assembly:MelonInfo(typeof(BurebistaAnimalTrophies.Main),"Burebista Animal Trophies","0.4.3","Burebista")]
[assembly:MelonGame("Hinterland","TheLongDark")]
namespace BurebistaAnimalTrophies;
public partial class Main:MelonMod {
 internal static Main Instance;readonly ModDataManager saved=new("BurebistaAnimalTrophies",false);
 TrophyState state=new();string slot="";bool ready,fault;float nextMaintenance,sceneReadyAt;bool recipeWarning;
 internal static string Scene=>UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
 public override void OnInitializeMelon(){Instance=this;MelonLogger.Msg("Animal Trophies 0.4.2: 50% por cadáver. Banco de trabajo: preparar. Ctrl+Alt+F11: colgar; Ctrl+Alt+F11 y 6: recoger.");}
 bool EnsureState(){
  string current=SaveGameSystem.GetCurrentSaveName();if(string.IsNullOrEmpty(current))return false;
  if(slot!=current){slot=current;ready=false;fault=false;state=new();}
  if(fault)return false;if(ready)return true;if(!saved.Save("1","ready"))return false;
  try{state=JsonSerializer.Deserialize<TrophyState>(saved.Load("state")??"{}")??new();if(state.Version!=1||state.Processed==null||state.Mounts==null||string.IsNullOrEmpty(state.Salt))throw new Exception("Estado de trofeos incompatible");ready=true;return true;}
  catch(Exception e){fault=true;MelonLogger.Error("Guardado de trofeos protegido: "+e.Message);return false;}
 }
 internal void SaveState(){if(!ready||fault||slot!=SaveGameSystem.GetCurrentSaveName())return;if(!saved.Save(JsonSerializer.Serialize(state),"state"))throw new Exception("No se ha podido guardar el estado de trofeos");}
 public override void OnSceneWasLoaded(int buildIndex,string sceneName){TrophyHarvest.Pending.Clear();CloseTrophyMenu();CancelPlacement();sceneReadyAt=Time.realtimeSinceStartup+12;nextMaintenance=0;try{TrophyConsoleInput.Register();}catch(Exception e){MelonLogger.Warning("Registro de bure_skull: "+e.Message);}}
 public override void OnApplicationQuit(){CloseTrophyMenu();CancelPlacement();}
 public override void OnUpdate(){
  try{
   if(!EnsureState())return;
   var player=GameManager.GetPlayerManagerComponent();if(player==null)return;TrophyLanguage.Refresh();
   if(Time.realtimeSinceStartup>=nextMaintenance){nextMaintenance=Time.realtimeSinceStartup+2;Items.Texts();try{Recipes.Ensure();}catch(Exception e){if(!recipeWarning){recipeWarning=true;MelonLogger.Error("Recetas: "+e);}}if(Time.realtimeSinceStartup>=sceneReadyAt)MaintainMounts();}

   if(Time.timeScale<=0)return;
   if(trophyMenu){if(player.GetControlMode()!=PlayerControlMode.Locked||InterfaceManager.GetInstance().AnyOverlayPanelEnabled()||Input.GetKeyDown(KeyCode.Escape)||TrophyMenuTogglePressed())CloseTrophyMenu();else TrophyMenuKeys();return;}
   if(player.GetControlMode()!=PlayerControlMode.Normal||InterfaceManager.GetInstance().AnyOverlayPanelEnabled()){CancelPlacement();return;}
   if(TrophyMenuTogglePressed()){CancelPlacement();OpenTrophyMenu();return;}
   PlacementTick();
  }catch(Exception e){MelonLogger.Error("Trofeos: "+e);CloseTrophyMenu();CancelPlacement();}
 }
 internal void HarvestCompleted(string id,int kind){
  if(!EnsureState())return;
  if(kind<0||kind>=Rules.Species.Length)return;
  if(string.IsNullOrEmpty(id)){MelonLogger.Warning("Despiece detectado, pero falta la identidad del cadáver.");return;}
  if(state.Processed.Contains(id)){MelonLogger.Msg("Cadáver ya comprobado: "+id);return;}
  bool granted=LootLedger.Harvest(state,slot,id,kind,k=>{Items.Give(k,false);},SaveState);
  MelonLogger.Msg("Despiece de "+TrophyLanguage.SpeciesName(kind)+": "+(granted?"cráneo entregado":"sin cráneo")+" (50 %). Cadáver: "+id);
  if(!granted)HUDMessage.AddMessage(TrophyLanguage.Text("No has recuperado un cráneo de este animal (50 % de probabilidad)."));
  if(granted)HUDMessage.AddMessage(TrophyLanguage.Text("Has recuperado un cráneo de ")+TrophyLanguage.SpeciesName(kind)+TrophyLanguage.Text(". Prepáralo en un banco de trabajo."));
 }

 internal static (string id,int kind) HarvestIdentity(BodyHarvest body){
  if(body==null||body.IsGearItem())return (null,-1);
  var ai=body.GetComponentInParent<BaseAi>();int kind=-1;
  if(ai!=null){if(ai.BaseWolf!=null)kind=0;else if(ai.Cougar!=null)kind=1;else if(ai.Bear!=null)kind=2;else if(ai.BaseDeer!=null)kind=3;else if(ai.Moose!=null)kind=4;}
  if(kind<0&&body.m_HidePrefab!=null){string name=body.m_HidePrefab.name.ToLowerInvariant();if(name.Contains("wolf"))kind=0;else if(name.Contains("cougar"))kind=1;else if(name.Contains("bear"))kind=2;else if((name.Contains("deer")||name.Contains("leather")))kind=3;else if(name.Contains("moose"))kind=4;}
  string id=ObjectGuid.GetGuidFromGameObject(body.gameObject,false);
  if(string.IsNullOrEmpty(id)&&ai!=null)id=ObjectGuid.GetGuidFromGameObject(ai.gameObject,false);
  if(string.IsNullOrEmpty(id)&&kind>=0){
   var target=ai!=null?ai.gameObject:body.gameObject;
   ObjectGuid.MaybeAttachObjectGuidAndRegister(target,Guid.NewGuid().ToString());
   id=ObjectGuid.GetGuidFromGameObject(target,false);
  }
  return (string.IsNullOrEmpty(id)?null:Scene+"|"+id,kind);
 }
}
[HarmonyPatch]
static class TrophyHarvest {
 internal static readonly Dictionary<int,(string id,int kind)> Pending=new();
 static IEnumerable<MethodBase> TargetMethods()=>typeof(Panel_BodyHarvest).GetMethods().Where(m=>m.Name=="HarvestSuccessful"||m.Name=="QuarterSuccessful");
 static void Prefix(Panel_BodyHarvest __instance,out (string id,int kind) __state){
  __state=(null,-1);try{__state=Main.HarvestIdentity(__instance.m_BodyHarvest);if(string.IsNullOrEmpty(__state.id)&&Pending.TryGetValue(__instance.GetInstanceID(),out var pending))__state=pending;}catch(Exception e){MelonLogger.Warning("No se pudo identificar el cadáver para trofeos: "+e.Message);}
 }
 static void Postfix(Panel_BodyHarvest __instance,(string id,int kind) __state){Pending.Remove(__instance.GetInstanceID());try{Main.Instance?.HarvestCompleted(__state.id,__state.kind);}catch(Exception e){MelonLogger.Error("Recogida de cráneo: "+e);HUDMessage.AddMessage(TrophyLanguage.Text("Error al entregar el cráneo; consulta el registro del mod."));}}
}
[HarmonyPatch]
static class TrophyHarvestStart {
 static IEnumerable<MethodBase> TargetMethods()=>typeof(Panel_BodyHarvest).GetMethods().Where(m=>m.Name=="StartHarvest"||m.Name=="StartQuarter");
 static void Prefix(Panel_BodyHarvest __instance){try{TrophyHarvest.Pending[__instance.GetInstanceID()]=Main.HarvestIdentity(__instance.m_BodyHarvest);}catch(Exception e){TrophyHarvest.Pending.Remove(__instance.GetInstanceID());MelonLogger.Warning("Inicio de despiece: "+e.Message);}}
}
[HarmonyPatch(typeof(SaveGameSystem),nameof(SaveGameSystem.SaveSceneData))]
static class TrophySceneSave {static void Prefix(){try{Main.Instance?.SaveState();}catch(Exception e){MelonLogger.Error("Guardado de trofeos: "+e);}}}


