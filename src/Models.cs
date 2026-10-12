using System.Reflection;
using UnityEngine;
using Object=UnityEngine.Object;
namespace BurebistaAnimalTrophies;
internal static class Models {
 static readonly Dictionary<int,Mesh> meshes=new();static readonly Dictionary<int,Texture2D> icons=new();
 static Material bone,raw,wood;static readonly Dictionary<int,Mesh> plaques=new();
 static string Folder=>Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),"BurebistaAnimalTrophies");
 internal static Mesh Skull(int kind){
  if(meshes.TryGetValue(kind,out var mesh)&&mesh!=null)return mesh;
  using var r=new BinaryReader(File.OpenRead(Path.Combine(Folder,Rules.Species[kind]+".mesh")));
  if(r.ReadInt32()!=1)throw new Exception("Formato de modelo de cráneo no compatible");int vc=r.ReadInt32(),tc=r.ReadInt32();
  if(vc<=0||vc>65000||tc<=0||tc>1000000||tc%3!=0)throw new Exception("Modelo de cráneo inválido");
  var vertices=new Vector3[vc];var triangles=new int[tc];for(int i=0;i<vc;i++)vertices[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
  for(int i=0;i<tc;i++){triangles[i]=r.ReadInt32();if(triangles[i]<0||triangles[i]>=vc)throw new Exception("Índice de cráneo inválido");}
  mesh=new Mesh{name="BureSkull"+Rules.Species[kind]};mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
  var uv=new Vector2[vc];for(int i=0;i<vc;i++)uv[i]=new Vector2((Mathf.Atan2(vertices[i].z,vertices[i].x)+Mathf.PI)/(2*Mathf.PI),(vertices[i].y-mesh.bounds.min.y)/Mathf.Max(.001f,mesh.bounds.size.y));mesh.uv=uv;
  Object.DontDestroyOnLoad(mesh);meshes[kind]=mesh;return mesh;
 }
 internal static Texture2D Icon(int kind){
  if(NativeModels.Supported(kind))return NativeModels.Icon(kind);
  if(icons.TryGetValue(kind,out var icon)&&icon!=null)return icon;
  icon=new Texture2D(2,2,TextureFormat.RGBA32,false);if(!ImageConversion.LoadImage(icon,File.ReadAllBytes(Path.Combine(Folder,Rules.Species[kind]+".png"))))throw new Exception("Icono de trofeo inválido");
  icon.name="BureSkullIcon"+Rules.Species[kind];Object.DontDestroyOnLoad(icon);icons[kind]=icon;return icon;
 }
 static void Materials(){
  if(bone!=null)return;
  var shader=Shader.Find("Standard")??Shader.Find("Diffuse")??Shader.Find("Sprites/Default");if(shader==null)throw new Exception("No hay shader para los trofeos");
  bone=new Material(shader){name="BureIvoryBone",color=new Color(.85f,.80f,.66f,1)};
  raw=new Material(shader){name="BureUncleanBone",color=new Color(.38f,.28f,.20f,1)};
  wood=new Material(shader){name="BureTrophyWood",color=new Color(.55f,.36f,.18f,1)};
  bone.mainTexture=raw.mainTexture=Surface(false);wood.mainTexture=Surface(true);
  foreach(var mat in new[]{bone,raw,wood}){if(mat.HasProperty("_Glossiness"))mat.SetFloat("_Glossiness",.06f);Object.DontDestroyOnLoad(mat);}
 }
 static Texture2D Surface(bool grain){
  const int size=256;var texture=new Texture2D(size,size,TextureFormat.RGBA32,true){name=grain?"BureWoodGrain":"BureBonePatina",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Bilinear};var pixels=new Color[size*size];
  for(int y=0;y<size;y++)for(int x=0;x<size;x++){
   float u=x/(float)size,v=y/(float)size;
   float broad=Mathf.PerlinNoise(u*5+12.3f,v*5+4.7f),fine=Mathf.PerlinNoise(u*65+7,v*65+3);
   float tone=grain?.53f+.23f*Mathf.Sin(u*160+Mathf.PerlinNoise(u*4,v*2)*9)+.13f*fine:.78f+.15f*broad+.07f*fine;
   pixels[y*size+x]=new Color(tone,tone,grain?tone:tone*.94f,1);
  }
  texture.SetPixels(pixels);texture.Apply(true,true);Object.DontDestroyOnLoad(texture);return texture;
 }
 static float Offset(int kind)=>-Skull(kind).bounds.min.z+.025f;
 internal static Bounds Bounds(int kind,bool ready){
  if(NativeModels.Supported(kind)){var native=NativeModels.LocalBounds(NativeModels.Template(kind,ready));if(!ready)native.center+=Vector3.up*(-native.min.y);if(ready&&kind==2){native.Encapsulate(new Vector3(-PlateWidth(kind)*.55f,-PlateHeight(kind)*.55f,0));native.Encapsulate(new Vector3(PlateWidth(kind)*.55f,PlateHeight(kind)*.5f,.025f));}return native;}
  var b=Skull(kind).bounds;if(!ready){b.center+=Vector3.up*(-b.min.y);return b;}
  b.center+=Vector3.forward*Offset(kind);b.Encapsulate(new Vector3(-PlateWidth(kind)*.55f,-PlateHeight(kind)*.55f,0));b.Encapsulate(new Vector3(PlateWidth(kind)*.55f,PlateHeight(kind)*.5f,.025f));return b;
 }
 internal static float PlateWidth(int k)=>new[]{.22f,.25f,.36f,.41f,.43f}[k];internal static float PlateHeight(int k)=>new[]{.36f,.34f,.48f,.58f,.60f}[k];
 internal static Mesh Plaque(int kind){
  if(plaques.TryGetValue(kind,out var mesh))return mesh;
  var outline=new[]{new Vector2(-.5f,.5f),new Vector2(.5f,.5f),new Vector2(.55f,.3f),new Vector2(.48f,-.35f),new Vector2(0,-.55f),new Vector2(-.48f,-.35f),new Vector2(-.55f,.3f)};
  var vertices=new List<Vector3>();var triangles=new List<int>();var uv=new List<Vector2>();
  for(int ring=0;ring<3;ring++)foreach(var p in outline){float scale=ring==2?.9f:1;vertices.Add(new Vector3(p.x*PlateWidth(kind)*scale,p.y*PlateHeight(kind)*scale,ring==0?0:ring==1?.018f:.025f));uv.Add(new Vector2(p.x*scale+.5f,p.y*scale+.55f));}
  for(int i=1;i<6;i++)triangles.AddRange(new[]{0,i,i+1,14,14+i+1,14+i});
  for(int ring=0;ring<2;ring++)for(int i=0;i<7;i++){int j=(i+1)%7,a=ring*7;triangles.AddRange(new[]{a+i,a+i+7,a+j,a+j,a+i+7,a+j+7});}
  mesh=new Mesh{name="BureTrophyShield"+kind};mesh.vertices=vertices.ToArray();mesh.uv=uv.ToArray();mesh.triangles=triangles.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();Object.DontDestroyOnLoad(mesh);plaques[kind]=mesh;return mesh;
 }
 internal static GameObject Visual(int kind,bool ready){
  Materials();var root=new GameObject("BureAnimalTrophyVisual");
  if(NativeModels.Supported(kind)){var native=NativeModels.Visual(kind,ready);native.transform.SetParent(root.transform,false);if(!ready)native.transform.localPosition=Vector3.up*(-NativeModels.LocalBounds(native).min.y);}
  else{var skull=new GameObject("Skull"+Rules.Species[kind]);skull.transform.SetParent(root.transform,false);
   skull.transform.localPosition=ready?Vector3.forward*Offset(kind):Vector3.up*(-Skull(kind).bounds.min.y);
   skull.AddComponent<MeshFilter>().sharedMesh=Skull(kind);skull.AddComponent<MeshRenderer>().sharedMaterial=ready?bone:raw;
  }
  if(ready&&kind!=3){var plate=NaturalPlaques.Visual(kind);plate.transform.SetParent(root.transform,false);}
  return root;
 }
}

