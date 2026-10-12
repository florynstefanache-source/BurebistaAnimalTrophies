using System.Security.Cryptography;
using System.Text;
namespace BurebistaAnimalTrophies;
public static class Rules {
 public static readonly string[] Species={"Wolf","Cougar","Bear","Deer","Moose"};
 public static readonly string[] Names={"Lobo","Puma","Oso","Ciervo","Alce"};
 public static readonly float[] Weights={.8f,.9f,1.8f,1.6f,3.2f};
 public static readonly int[] Minutes={90,90,120,150,180};
 public static string Gear(int kind,bool ready)=>"GEAR_BureAnimal"+Species[kind]+(ready?"Trophy":"RawSkull");
 public static bool Parse(string name,out int kind,out bool ready){
  name=(name??"").Replace("(Clone)","").Trim();
  for(int i=0;i<Species.Length;i++)for(int r=0;r<2;r++)if(string.Equals(name,Gear(i,r==1),StringComparison.OrdinalIgnoreCase)){kind=i;ready=r==1;return true;}
  kind=-1;ready=false;return false;
 }
 public static int Roll(string salt,string corpse){
  var hash=SHA256.HashData(Encoding.UTF8.GetBytes(salt+"|"+corpse));
  // The lower half of the uint range gives exactly 50 percent.
  uint sample=System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(hash);
  return sample<0x80000000u?1:0;
 }
}
public class MountRecord {
 public string Id{get;set;}="";public string Scene{get;set;}="";public string Wall{get;set;}="";
 public float X{get;set;}public float Y{get;set;}public float Z{get;set;}
 public float Qx{get;set;}public float Qy{get;set;}public float Qz{get;set;}public float Qw{get;set;}=1;
}
public class TrophyState {
 public int Version{get;set;}=1;public string Salt{get;set;}= "BurebistaAnimalTrophies-v1";
 public HashSet<string> Processed{get;set;}=new();public List<MountRecord> Mounts{get;set;}=new();
}
