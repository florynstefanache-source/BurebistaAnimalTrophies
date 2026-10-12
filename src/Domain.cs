using System.Numerics;
namespace BurebistaAnimalTrophies;
public static class GeometryRules {
 public static bool TriangleDistance(Vector3 origin,Vector3 direction,Vector3 a,Vector3 b,Vector3 c,out float distance){
  distance=0;var e1=b-a;var e2=c-a;var p=Vector3.Cross(direction,e2);float det=Vector3.Dot(e1,p);
  if(MathF.Abs(det)<.000001f)return false;float inverse=1/det;var t=origin-a;float u=Vector3.Dot(t,p)*inverse;
  if(u<0||u>1)return false;var q=Vector3.Cross(t,e1);float v=Vector3.Dot(direction,q)*inverse;
  if(v<0||u+v>1)return false;distance=Vector3.Dot(e2,q)*inverse;return distance>.005f;
 }
}
public static class LootLedger {
 public static bool Harvest(TrophyState state,string scope,string corpse,int kind,Action<int> grant,Action persist){
  if(string.IsNullOrEmpty(corpse)||kind<0||kind>=Rules.Species.Length||state.Processed.Contains(corpse))return false;
  state.Processed.Add(corpse);
  if(Rules.Roll(scope+"|"+state.Salt,corpse)==0){persist();return false;}
  bool granted=false;
  try{grant(kind);granted=true;persist();return true;}
  catch{if(!granted)state.Processed.Remove(corpse);throw;}
 }
}
