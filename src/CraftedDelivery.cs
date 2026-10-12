using HarmonyLib;
using Il2Cpp;
using Il2CppTLD.Gear;
namespace BurebistaAnimalTrophies;
// Only our gear results bypass the native Addressables registry. Furniture,
// decorations and every other crafting result keep their original path.
[HarmonyPatch(typeof(PlayerManager),nameof(PlayerManager.InstantiateCraftedItemInPlayerInventory))]
static class TrophyCraftedDelivery {
 static bool Prefix(ICraftingResult result,int units,float normalizedCondition,ref ICraftingResult __result){
  var gear=result?.TryCast<GearItem>();if(gear==null||!Rules.Parse(gear.name,out int kind,out bool ready))return true;
  GearItem delivered=null;
  for(int i=0;i<units;i++)delivered=Items.Give(kind,ready,normalizedCondition);
  __result=delivered?.Cast<ICraftingResult>();return false;
 }
}
[HarmonyPatch(typeof(PlayerManager),nameof(PlayerManager.InstantiateItemInPlayerInventory),new[]{typeof(GearItem),typeof(int),typeof(float),typeof(PlayerManager.InventoryInstantiateFlags)})]
static class TrophyInventoryFromTemplate {
 static bool Prefix(GearItem gearItem,int units,float normalizedCondition,ref GearItem __result){
  if(gearItem==null||!Rules.Parse(gearItem.name,out int kind,out bool ready))return true;
  for(int i=0;i<units;i++)__result=Items.Give(kind,ready,normalizedCondition);
  return false;
 }
}
[HarmonyPatch(typeof(PlayerManager),nameof(PlayerManager.InstantiateItemInPlayerInventory),new[]{typeof(AssetReferenceGearItem),typeof(int),typeof(float),typeof(PlayerManager.InventoryInstantiateFlags)})]
static class TrophyInventoryFromReference {
 static bool Prefix(AssetReferenceGearItem assetReference,int units,float normalizedCondition,ref GearItem __result){
  if(!Rules.Parse(assetReference?.RuntimeKey?.ToString(),out int kind,out bool ready))return true;
  for(int i=0;i<units;i++)__result=Items.Give(kind,ready,normalizedCondition);
  return false;
 }
}
