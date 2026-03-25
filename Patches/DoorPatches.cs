using System.Diagnostics.CodeAnalysis;

using StardewValley;

using xTile.Dimensions;

namespace Closed_Door_Gifting.Patches
{
  internal static class DoorPatches
  {
    private static readonly HashSet<string> BlockedItems = new()
    {
        "(O)458", // Bouquet
        "(O)277", // Wilted Bouquet
        "(O)460", // Mermaid's Pendant
        "(O)809"  // Movie Ticket
    };

    [SuppressMessage(
      "Style",
      "IDE0060:Remove unused parameter",
      Justification = "Required by Harmony"
    )]
    public static void PerformAction_Postfix(GameLocation __instance, string[] action, Farmer who, Location tileLocation)
    {
      if (!ModEntry.Config.UseMod) return;

      //validate context
      if (!who.IsLocalPlayer || Game1.eventUp) return;
      if (!ArgUtility.TryGet(action, 0, out string actionType, out _) || actionType != "Door") return;
      if (action.Length <= 1) return;

      // validate item
      if (who.ActiveObject is not StardewValley.Object gift) return;
      if (!gift.canBeGivenAsGift() || ItemContextTagManager.HasBaseTag(gift.QualifiedItemId, "not_giftable")) return;
      if (BlockedItems.Contains(gift.QualifiedItemId)) return;

      // validate door lock status, replicated in game,
      if (IsDoorUnlocked(action, __instance, who)) return;

      GiftHandler.AttemptDoorGift(action, __instance, gift);
    }

    private static bool IsDoorUnlocked(string[] action, GameLocation location, Farmer who)
    {
      for (int i = 1; i < action.Length; i++)
      {
        string npc = action[i];
        if (who.getFriendshipHeartLevelForNPC(npc) >= 2
            || Game1.player.mailReceived.Contains("doorUnlock" + npc)
            || (action[i] == "Sebastian" && location.IsGreenRainingHere() && Game1.year == 1))
        {
          ModEntry.Log($"{npc} door is unlocked, skipping gifting.");
          return true;
        }
      }

      return false;
    }
  }
}
