using System.Reflection;
using Il2Cpp;
using UnityEngine;
namespace BurebistaAnimalTrophies;
public partial class Main {
 FieldInfo sharedHints;
 void DrawTrophyHelp(){
  // Read the existing F9 switch; do not bind or toggle F9 a second time.
  if(sharedHints==null){var assembly=AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a=>a.GetName().Name=="BurebistaFishingShelter");sharedHints=assembly?.GetType("BurebistaFishingShelter.Main")?.GetField("ShowHints",BindingFlags.Public|BindingFlags.Static);}
  if(sharedHints==null||sharedHints.GetValue(null) is not bool visible||!visible||GameManager.GetPlayerManagerComponent()==null||Time.timeScale<=0||trophyMenu||InterfaceManager.GetInstance().AnyOverlayPanelEnabled())return;
  float width=Mathf.Min(620,Screen.width-20);
  GUI.Box(new Rect(Screen.width-width-10,Screen.height-100,width,88),TrophyLanguage.Text("TROFEOS — F9 muestra/oculta esta ayuda"));
  GUI.Label(new Rect(Screen.width-width,Screen.height-76,width-20,60),TrophyLanguage.Text("Ctrl + Alt + F11: menú | 1 Lobo · 2 Puma · 3 Oso · 4 Ciervo · 5 Alce\nEnter: fijar · ↑/↓: distancia · ←/→: girar · Esc: cancelar\nPara recoger: apunta al trofeo, abre el menú y pulsa 6."));
 }
}
