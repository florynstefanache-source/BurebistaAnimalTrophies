using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object=UnityEngine.Object;
namespace BurebistaAnimalTrophies;
// Keep handles alive: the game owns the meshes, textures and materials.
internal static class NativeModels {
 static readonly Dictionary<string,AsyncOperationHandle<GameObject>> handles=new();
 static readonly Dictionary<string,GameObject> visuals=new();
 static readonly Dictionary<int,Texture2D> icons=new();
 static readonly Dictionary<int,AsyncOperationHandle<Texture2D>> iconHandles=new();
 const string Art="Assets/ArtAssets/Env/Objects/";
 internal static bool Supported(int kind)=>kind==2||kind==3;
 // The mounted deer has a typo (extra ']') in its catalog address.
 // Its verified GUID key resolves the correct prefab without relying on that typo.
 static string Address(int kind,bool ready)=>kind==3&&ready?"2007ea16202d942439e696beab5d4e8e":Art+(kind==2?"OBJ_Bones/OBJ_BonesBearSkullA_Prefab.prefab":"OBJ_Bones/OBJ_BoneStagSkullC_Prefab.prefab");
 internal static Texture2D Icon(int kind){
  if(icons.TryGetValue(kind,out var texture))return texture;
  string name=kind==2?"BonesBearSkullA":"BoneStagSkullC";
  // Inventory icons are addressed by their short name, not their file path.
  var handle=Addressables.LoadAssetAsync<Texture2D>((Il2CppSystem.Object)("ico_GearItem__"+name));
  iconHandles[kind]=handle;
  texture=handle.WaitForCompletion();if(texture==null)throw new Exception("No se pudo cargar el icono original "+name);
  // The unreleased Addressables reference protects this shared game texture.
  icons[kind]=texture;return texture;
 }
 internal static GameObject Template(int kind,bool ready){
  string key=kind+":"+ready;if(visuals.TryGetValue(key,out var cached)&&cached!=null)return cached;
  string address=Address(kind,ready);
  if(!handles.TryGetValue(address,out var handle)){handle=Addressables.LoadAssetAsync<GameObject>((Il2CppSystem.Object)address);handles[address]=handle;}
  var prefab=handle.WaitForCompletion();if(prefab==null)throw new Exception("Falta modelo original: "+address);
  // Copy only rendering data. Do not instantiate native decoration behaviours,
  // GUIDs, interactions or colliders alongside our persistent GearItem.
  var root=new GameObject("NativeSkull"+kind);root.SetActive(false);Object.DontDestroyOnLoad(root);
  var shape=new GameObject("OriginalGameRenderers");shape.transform.SetParent(root.transform,false);
  foreach(var source in prefab.GetComponentsInChildren<MeshFilter>(true)){
   var renderer=source.GetComponent<MeshRenderer>();if(source.sharedMesh==null||renderer==null)continue;
   // A new object has no LODGroup: copy only the highest detail geometry,
   // otherwise both authored levels would render on top of one another.
   if(source.name.Contains("_LOD")&&!source.name.EndsWith("_LOD0"))continue;
   var part=new GameObject(source.name);part.transform.SetParent(shape.transform,false);
   var matrix=prefab.transform.worldToLocalMatrix*source.transform.localToWorldMatrix;
   part.transform.localPosition=matrix.GetColumn(3);part.transform.localRotation=matrix.rotation;part.transform.localScale=matrix.lossyScale;
   part.AddComponent<MeshFilter>().sharedMesh=source.sharedMesh;part.AddComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;
  }
  if(root.GetComponentsInChildren<MeshFilter>(true).Length==0){Object.Destroy(root);throw new Exception("Modelo original sin malla: "+address);}
  shape.transform.localScale=Vector3.one*(kind==2?.72f:.8f);
  if(kind==2&&ready)shape.transform.localRotation=Quaternion.Euler(65,0,0);
  var bounds=LocalBounds(root);
  // Native deer plaque, measured at scale .8: width .4096, height .5755,
  // center Y .0086. Anchor the plaque rather than the antler bounds.
  shape.transform.localPosition=ready?(kind==3?new Vector3(-bounds.center.x,-.0086f,.004f-bounds.min.z):new Vector3(-bounds.center.x,-bounds.center.y,.029f-bounds.min.z)):-bounds.center;
  visuals[key]=root;return root;
 }
 internal static GameObject Visual(int kind,bool ready){var root=Object.Instantiate(Template(kind,ready));root.name="NativeSkull"+kind;root.SetActive(true);return root;}
 internal static Bounds LocalBounds(GameObject root){
  bool first=true;var result=new Bounds();
  foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true)){
   if(filter.sharedMesh==null)continue;var b=filter.sharedMesh.bounds;var matrix=root.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
   for(int i=0;i<8;i++){var p=matrix.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));if(first){result=new Bounds(p,Vector3.zero);first=false;}else result.Encapsulate(p);}
  }
  return result;
 }
}
