using Il2CppTLD.Gear;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using Object=UnityEngine.Object;
namespace BurebistaAnimalTrophies;
internal static class Recipes {
 static readonly Dictionary<int,BlueprintData> recipes=new();static bool preparing;
 internal static void Ensure(){
  var manager=BlueprintManager.s_Instance;if(manager==null||!manager.m_IsInitialized||manager.m_AllBlueprints==null||preparing)return;
  preparing=true;
  try{
   for(int kind=0;kind<5;kind++){
    string name="BureAnimalTrophyRecipe"+Rules.Species[kind];bool exists=false;
    foreach(var b in manager.m_AllBlueprints)if(b!=null&&b.name==name){exists=true;break;}
    if(exists)continue;
    if(!recipes.TryGetValue(kind,out var recipe)){
     var required=new[]{new BlueprintData.RequiredGearItem{m_Item=Items.Template(kind,false),m_Count=1},new BlueprintData.RequiredGearItem{m_Item=GearItem.LoadGearItemPrefab("GEAR_ReclaimedWoodB"),m_Count=1},new BlueprintData.RequiredGearItem{m_Item=GearItem.LoadGearItemPrefab("GEAR_GutDried"),m_Count=1}};
     if(required.Any(r=>r.m_Item==null))throw new Exception(TrophyLanguage.Text("No se encuentran los materiales de la receta"));
     var simple=GearItem.LoadGearItemPrefab("GEAR_SimpleTools")?.m_ToolsItem;var quality=GearItem.LoadGearItemPrefab("GEAR_HighQualityTools")?.m_ToolsItem;
     if(simple==null||quality==null)throw new Exception(TrophyLanguage.Text("Faltan herramientas nativas para preparar cráneos"));
     recipe=ScriptableObject.CreateInstance<BlueprintData>();recipe.name=name;
     recipe.m_CraftingAudio=new Il2CppAK.Wwise.Event{WwiseObjectReference=ScriptableObject.CreateInstance<WwiseEventReference>()};recipe.m_CraftingAudio.WwiseObjectReference.objectName="NULL_WWISEEVENT";
     recipe.m_XPModesToDisable=new Il2CppSystem.Collections.Generic.List<ExperienceModeType>();recipe.m_RequiredPowder=new Il2CppReferenceArray<BlueprintData.RequiredPowder>(0);recipe.m_RequiredLiquid=new Il2CppReferenceArray<BlueprintData.RequiredLiquid>(0);
     recipe.m_AppliedSkill=(SkillType)(-1);recipe.m_ImprovedSkill=(SkillType)(-1);recipe.m_AppearsInSurvivalOnly=true;
     recipe.SetCraftedResult(Items.Template(kind,true));recipe.m_CraftedResultCount=1;recipe.m_DurationMinutes=Rules.Minutes[kind];
     recipe.m_RequiredGear=new Il2CppReferenceArray<BlueprintData.RequiredGearItem>(required);recipe.m_RequiredCraftingLocation=CraftingLocation.Workbench;recipe.m_RequiresLight=true;
     recipe.m_RequiredTool=simple;recipe.m_OptionalTools=new Il2CppReferenceArray<ToolsItem>(new[]{quality});recipe.m_FilteredOptionalTools=new Il2CppSystem.Collections.Generic.List<ToolsItem>();recipe.InitializeOptionalTools();recipe.m_Locked=false;recipe.Locked=false;recipe.m_IgnoreLockInSurvival=true;
     Object.DontDestroyOnLoad(recipe);recipes[kind]=recipe;
    }
    manager.m_AllBlueprints.Add(recipe);
   }
  }finally{preparing=false;}
 }
}

