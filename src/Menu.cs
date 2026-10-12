using Il2Cpp;
using HarmonyLib;
using UnityEngine;
namespace BurebistaAnimalTrophies;
public partial class Main {
 internal bool trophyMenu;bool oldCursor;CursorLockMode oldLock;PlayerControlMode priorControl;
 static bool TrophyMenuTogglePressed()=>(Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.RightControl))&&(Input.GetKey(KeyCode.LeftAlt)||Input.GetKey(KeyCode.RightAlt))&&Input.GetKeyDown(KeyCode.F11);
 void OpenTrophyMenu(){
  var player=GameManager.GetPlayerManagerComponent();if(player==null)return;
  oldCursor=Cursor.visible;oldLock=Cursor.lockState;priorControl=player.GetControlMode();player.SetControlMode(PlayerControlMode.Locked);trophyMenu=true;
  KeepTrophyCursor();
 }
 void KeepTrophyCursor(){if(!trophyMenu)return;InputManager.ShowCursor(true);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
 public override void OnLateUpdate(){if(trophyMenu)KeepTrophyCursor();}
 void ChooseTrophy(int kind){
  var inventory=GameManager.GetInventoryComponent();if(inventory==null||inventory.GetNumGearWithName(Rules.Gear(kind,true))<1){HUDMessage.AddMessage(TrophyLanguage.Text("No tienes un trofeo preparado de ")+TrophyLanguage.SpeciesName(kind));return;}
  CloseTrophyMenu();StartPlacement(kind);
 }
 void TrophyMenuKeys(){
  KeepTrophyCursor();
  var keys=new[]{KeyCode.Alpha1,KeyCode.Alpha2,KeyCode.Alpha3,KeyCode.Alpha4,KeyCode.Alpha5};
  var pad=new[]{KeyCode.Keypad1,KeyCode.Keypad2,KeyCode.Keypad3,KeyCode.Keypad4,KeyCode.Keypad5};
  for(int kind=0;kind<5;kind++)if(Input.GetKeyDown(keys[kind])||Input.GetKeyDown(pad[kind])){ChooseTrophy(kind);return;}
  if(Input.GetKeyDown(KeyCode.Alpha6)||Input.GetKeyDown(KeyCode.Keypad6))CollectMenuTrophy();
 }
 void CollectMenuTrophy(){CloseTrophyMenu();CollectMounted();}
 void CloseTrophyMenu(){
  if(!trophyMenu)return;trophyMenu=false;var player=GameManager.GetPlayerManagerComponent();
  if(player!=null&&player.GetControlMode()==PlayerControlMode.Locked)player.SetControlMode(priorControl);
  InputManager.ShowCursor(oldCursor);Cursor.lockState=oldLock;Cursor.visible=oldCursor;
 }
 public override void OnGUI(){

  DrawTrophyHelp();
  if(placementKind>=0)GUI.Box(new Rect(Screen.width/2-330,Screen.height-115,660,80),placementHint+TrophyLanguage.Text("\n↑/↓: alejar/acercar · ←/→: girar · Esc: cancelar"));
  if(!trophyMenu)return;
  KeepTrophyCursor();
  float left=(Screen.width-520)/2f,top=(Screen.height-460)/2f;
  GUI.Box(new Rect(left,top,520,460),TrophyLanguage.Text("Trofeos de animales"));
  GUI.Label(new Rect(left+20,top+35,480,55),TrophyLanguage.Text("Elige Colocar con el ratón o pulsa el número de la especie.\nApunta donde quieras, ajusta con ↑/↓ y pulsa Enter."));
  var inventory=GameManager.GetInventoryComponent();if(inventory==null)return;
  for(int k=0;k<5;k++){
   int raw=inventory.GetNumGearWithName(Rules.Gear(k,false)),prepared=inventory.GetNumGearWithName(Rules.Gear(k,true));
   GUI.Label(new Rect(left+20,top+100+k*47,330,35),(k+1)+" — "+TrophyLanguage.SpeciesName(k)+": "+raw+TrophyLanguage.Text(" sin preparar · ")+prepared+TrophyLanguage.Text(" listos"));
   GUI.enabled=prepared>0;
   if(GUI.Button(new Rect(left+370,top+100+k*47,125,33),TrophyLanguage.Text("Colocar")))ChooseTrophy(k);
   GUI.enabled=true;
  }
  if(GUI.Button(new Rect(left+20,top+350,475,33),TrophyLanguage.Text("6 — Recoger el trofeo colgado al que apuntas")))CollectMenuTrophy();
  if(GUI.Button(new Rect(left+185,top+410,150,32),TrophyLanguage.Text("Cerrar")))CloseTrophyMenu();
 }
}
[HarmonyPatch(typeof(InputManager),nameof(InputManager.ShowCursor))]
static class TrophyMenuCursor {
 static void Prefix(ref bool __0){if(Main.Instance!=null&&Main.Instance.trophyMenu)__0=true;}
}

