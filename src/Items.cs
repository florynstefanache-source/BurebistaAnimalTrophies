using System.Reflection;
using HarmonyLib;
using Il2Cpp;
using Il2CppTLD.Gear;
using Il2CppTLD.IntBackedUnit;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object=UnityEngine.Object;
namespace BurebistaAnimalTrophies;
internal static class Items {
 internal const string Native="GEAR_HighQualityTools";
 static readonly Dictionary<string,GearItem> templates=new();
 static readonly Dictionary<string,GearItemData> data=new();
 internal static string Label(int kind,bool ready)=>(ready?TrophyLanguage.Text("Trofeo de cráneo: "):TrophyLanguage.Text("Cráneo sin preparar: "))+TrophyLanguage.SpeciesName(kind);
 internal static GearItem Template(int kind,bool ready){
  string name=Rules.Gear(kind,ready);if(templates.TryGetValue(name,out var value)&&value!=null)return value;
  // Resolve visual dependencies before caching a template, so a failed load
  // cannot leave a half configured tools item in the recipe registry.
  _=Models.Bounds(kind,ready);var iconTexture=Models.Icon(kind);
  var native=GearItem.LoadGearItemPrefab(Native);if(native==null)throw new Exception(TrophyLanguage.Text("Falta plantilla nativa para los trofeos"));
  var root=Object.Instantiate(native.gameObject);root.SetActive(false);Object.DontDestroyOnLoad(root);
  var gear=root.GetComponent<GearItem>();
  if(gear.m_ToolsItem!=null)Object.DestroyImmediate(gear.m_ToolsItem);
  gear.m_ToolsItem=null;
  var itemData=Object.Instantiate(native.m_GearItemData);itemData.name=name+"Data";Object.DontDestroyOnLoad(itemData);
  itemData.m_PrefabReference=new AssetReferenceGearItem(name);
  itemData.m_BaseWeight=ItemWeight.FromKilograms(Rules.Weights[kind]+(ready?.35f:0));
  itemData.m_LocalizedName=new LocalizedString{m_LocalizationID=name+"Name"};
  itemData.m_LocalizedDescription=new LocalizedString{m_LocalizationID=name+"Description"};
  var icon=ScriptableObject.CreateInstance<GearItemInventoryIconSimpleData>();icon.m_Icon=iconTexture;icon.hideFlags|=HideFlags.DontUnloadUnusedAsset;itemData.m_IconData=icon;
  data[name]=itemData;templates[name]=gear;Configure(gear,kind,ready);return gear;
 }
 internal static void Configure(GearItem gear,int kind,bool ready){
  if(gear==null)return;string name=Rules.Gear(kind,ready);if(!data.ContainsKey(name))Template(kind,ready);
  gear.name=name;gear.m_GearItemData=data[name];gear.WeightKG=ItemWeight.FromKilograms(Rules.Weights[kind]+(ready?.35f:0));
  if(gear.m_ToolsItem!=null)Object.DestroyImmediate(gear.m_ToolsItem);
  gear.m_ToolsItem=null;gear.m_StackableItem=null;gear.m_Harvest=null;
  gear.m_DefaultLocalScale=Vector3.one;
  var existing=gear.transform.Find("BureAnimalTrophyVisual");if(existing!=null){CacheRenderers(gear,existing.gameObject);return;}
  foreach(var renderer in gear.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
  foreach(var collider in gear.GetComponentsInChildren<Collider>(true))collider.enabled=false;
  var visual=Models.Visual(kind,ready);visual.name="BureAnimalTrophyVisual";visual.transform.SetParent(gear.transform,false);
  var bounds=Models.Bounds(kind,ready);var box=gear.gameObject.AddComponent<BoxCollider>();box.center=bounds.center;box.size=bounds.size;
  CacheRenderers(gear,visual);
 }
 static void CacheRenderers(GearItem gear,GameObject visual){gear.m_MeshRenderers=visual.GetComponentsInChildren<MeshRenderer>(true).ToArray();gear.m_SkinnedMeshRenderers=visual.GetComponentsInChildren<SkinnedMeshRenderer>(true).ToArray();}
 internal static void WorldVisual(GearItem gear){
  if(gear==null||!Rules.Parse(gear.name,out int kind,out bool ready))return;Configure(gear,kind,ready);
  if(gear.m_InPlayerInventory)return;
  var visual=gear.transform.Find("BureAnimalTrophyVisual");if(visual==null)return;
  gear.transform.localScale=Vector3.one;visual.gameObject.SetActive(true);
  foreach(var renderer in gear.GetComponentsInChildren<Renderer>(true))renderer.enabled=renderer.transform.IsChildOf(visual);
  var ownCollider=gear.GetComponents<BoxCollider>().LastOrDefault();
  foreach(var collider in gear.GetComponentsInChildren<Collider>(true))collider.enabled=collider==ownCollider;
 }
 internal static GearItem Give(int kind,bool ready,float condition=1f){
  var player=GameManager.GetPlayerManagerComponent();if(player==null)throw new Exception(TrophyLanguage.Text("Carga una partida antes de entregar cráneos"));
  var gear=Object.Instantiate(Template(kind,ready).gameObject).GetComponent<GearItem>();
  gear.gameObject.SetActive(true);Configure(gear,kind,ready);gear.SetNormalizedHP(Mathf.Clamp01(condition),false);
  ObjectGuid.MaybeAttachObjectGuidAddOrReplace(gear.gameObject,Guid.NewGuid().ToString());
  var added=player.AddItemToPlayerInventory(gear,false,true);
  if(added==null||!added.m_InPlayerInventory){Object.Destroy(gear.gameObject);throw new Exception(TrophyLanguage.Text("No se pudo añadir el cráneo al inventario"));}
  MelonLoader.MelonLogger.Msg("Entregado: "+Rules.Gear(kind,ready));return added;
 }
 internal static void Texts(){
  var table=Localization.s_CurrentLanguageStringTable;if(table==null)return;
  for(int k=0;k<5;k++)for(int r=0;r<2;r++)foreach(string suffix in new[]{"Name","Description"}){
   string key=Rules.Gear(k,r==1)+suffix;var entry=table.GetEntryFromKey(key)??table.AddEntryForKey(key);string value=Text(k,r==1,suffix=="Name");
   for(int i=0;i<entry.m_Languages.Count;i++)entry.m_Languages[i]=value;
  }
 }
 internal static string Text(int k,bool ready,bool title)=>title?Label(k,ready):ready?TrophyLanguage.Text("Cráneo natural montado en una placa de madera. Puedes soltarlo al suelo con Dejar. Para colgarlo desde la mochila: Ctrl+Alt+F11, elige la especie. Enter: fijar; flechas: ajustar. Ctrl+Alt+F11 y 6: recoger."):TrophyLanguage.Text("Cráneo recuperado al despiezar un animal. Prepara el trofeo en un banco de trabajo con herramientas, madera recuperada y tripa curada.");
}
[HarmonyPatch(typeof(GearItem),nameof(GearItem.Drop))]
static class TrophyDroppedVisual {static void Postfix(GearItem __instance,GearItem __result){Items.WorldVisual(__result??__instance);}}
[HarmonyPatch(typeof(PlayerManager),nameof(PlayerManager.AddItemCONSOLE))]
static class TrophyConsoleDelivery {
 static bool Prefix(string name,int count,float normalizedCondition){
  string key=(name??"").Trim();if(!key.StartsWith("GEAR_",StringComparison.OrdinalIgnoreCase))key="GEAR_"+key;
  if(!Rules.Parse(key,out int kind,out bool ready))return true;
  try{for(int i=0;i<Math.Clamp(count,1,100);i++)Items.Give(kind,ready,normalizedCondition);HUDMessage.AddMessage(TrophyLanguage.Text("Añadido: ")+Items.Label(kind,ready));}
  catch(Exception e){MelonLoader.MelonLogger.Error("No se pudo entregar el cráneo: "+e);HUDMessage.AddMessage(TrophyLanguage.Text("No se pudo entregar el cráneo. Consulta el registro del mod."));}
  return false;
 }
}
[HarmonyPatch(typeof(GearItem),nameof(GearItem.LoadGearItemPrefab))]
static class TrophyPrefab {static bool Prefix(string name,ref GearItem __result){if(!Rules.Parse(name,out int k,out bool r))return true;__result=Items.Template(k,r);return false;}}
[HarmonyPatch]
static class TrophyFactory {
 static IEnumerable<MethodBase> TargetMethods()=>typeof(GearItem).GetMethods().Where(m=>(m.Name=="InstantiateGearItem"||m.Name=="InstantiateAndDeserializeGearItem")&&m.GetParameters().Length>0&&m.GetParameters()[0].ParameterType==typeof(string));
 static void Prefix(ref string __0,out string __state){__state=Rules.Parse(__0,out _,out _)?__0:null;if(__state!=null)__0=Items.Native;}
 static void Postfix(GearItem __result,string __state){if(__state!=null&&Rules.Parse(__state,out int k,out bool r))Items.Configure(__result,k,r);}
}
[HarmonyPatch]
static class TrophyReferenceFactory {
 static IEnumerable<MethodBase> TargetMethods()=>typeof(GearItem).GetMethods().Where(m=>(m.Name=="InstantiateGearItem"||m.Name=="InstantiateAndDeserializeGearItem")&&m.GetParameters().Length>0&&m.GetParameters()[0].ParameterType==typeof(AssetReference));
 static void Prefix(ref AssetReference __0,out string __state){var key=__0?.RuntimeKey?.ToString();__state=Rules.Parse(key,out _,out _)?key:null;if(__state!=null)__0=GearItem.LoadGearItemPrefab(Items.Native).m_GearItemData.m_PrefabReference;}
 static void Postfix(GearItem __result,string __state){if(__state!=null&&Rules.Parse(__state,out int k,out bool r))Items.Configure(__result,k,r);}
}
[HarmonyPatch(typeof(GearItem),nameof(GearItem.CreateSaveData))]
static class TrophySave {static void Postfix(GearItem __instance,GearSaveData __result){if(__result!=null&&Rules.Parse(__instance.name,out _,out _))__result.m_PrefabName=__instance.name.Replace("(Clone)","").Trim();}}
[HarmonyPatch(typeof(LocalizedString),nameof(LocalizedString.Text))]
static class TrophyText {static bool Prefix(LocalizedString __instance,ref string __result){string key=__instance.m_LocalizationID??"";bool title=key.EndsWith("Name");string suffix=title?"Name":"Description";if(!key.EndsWith(suffix)||!Rules.Parse(key[..^suffix.Length],out int k,out bool r))return true;__result=Items.Text(k,r,title);return false;}}

