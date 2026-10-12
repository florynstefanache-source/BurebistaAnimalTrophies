using System.Reflection;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
namespace BurebistaAnimalTrophies;
// Catch the text before native gear lookup rejects unregistered custom assets.
[HarmonyPatch]
static class TrophyConsoleInput {
 static uConsole.DebugCommand callback;
 internal static void Register(){
  callback??=Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<uConsole.DebugCommand>((Action)Command);
  if(uConsole.CommandAlreadyRegistered("bure_skull"))uConsole.UnRegisterCommand("bure_skull");
  uConsole.RegisterCommand("bure_skull",TrophyLanguage.Text("bure_skull Wolf|Cougar|Bear|Deer|Moose: recibir un cráneo sin preparar"),callback);
  MelonLogger.Msg("Comando de prueba registrado: bure_skull Wolf. Menú: Ctrl+Alt+F11.");
 }
 static void Command(){
  if(!uConsole.NextParameterExists()){uConsole.Log(TrophyLanguage.Text("Uso: bure_skull Wolf|Cougar|Bear|Deer|Moose"));return;}
  string key=uConsole.GetString();int kind=Array.FindIndex(Rules.Species,s=>s.Equals(key,StringComparison.OrdinalIgnoreCase));
  if(kind>=0)key=Rules.Gear(kind,false);
  Handle("bure_skull "+key);
 }
 static IEnumerable<MethodBase> TargetMethods()=>typeof(uConsole).GetMethods().Where(m=>(m.Name=="RunCommand"||m.Name=="RunCommandSilent")&&m.GetParameters().Length==1&&m.GetParameters()[0].ParameterType==typeof(string));
 static bool Prefix(string __0)=>Handle(__0);
 internal static bool Handle(string __0){
  var args=(__0??"").Split((char[])null,StringSplitOptions.RemoveEmptyEntries);
  if(args.Length<2||(!args[0].Equals("add",StringComparison.OrdinalIgnoreCase)&&!args[0].Equals("gear_add",StringComparison.OrdinalIgnoreCase)&&!args[0].Equals("bure_skull",StringComparison.OrdinalIgnoreCase)))return true;
  string key=args[1];if(args[0].Equals("bure_skull",StringComparison.OrdinalIgnoreCase)){int species=Array.FindIndex(Rules.Species,s=>s.Equals(key,StringComparison.OrdinalIgnoreCase));if(species>=0)key=Rules.Gear(species,false);}
  if(!key.StartsWith("GEAR_",StringComparison.OrdinalIgnoreCase))key="GEAR_"+key;
  if(!Rules.Parse(key,out int kind,out bool ready))return true;
  try{
   int count=1;if(args.Length>2&&(!int.TryParse(args[2],out count)||count<1||count>100))throw new Exception(TrophyLanguage.Text("Cantidad válida: de 1 a 100."));
   MelonLogger.Msg("Comando de cráneo reconocido: "+Rules.Gear(kind,ready));
   for(int i=0;i<count;i++)Items.Give(kind,ready);
   uConsole.Log("[Trofeos] Entregado: "+count+" x "+Items.Label(kind,ready));
  }catch(Exception e){MelonLogger.Error("Comando de cráneo: "+e);uConsole.Log("[Trofeos] ERROR: "+e.Message);}
  return false;
 }
}
