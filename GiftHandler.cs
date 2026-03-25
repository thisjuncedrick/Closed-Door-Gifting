using StardewModdingAPI;

using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Characters;
using SObject = StardewValley.Object;

using Closed_Door_Gifting.Patches;

namespace Closed_Door_Gifting
{
  internal static class GiftHandler
  {
    public static void AttemptDoorGift(string[] action, GameLocation location, SObject giftItem)
    {
      NPC[] npcs = GetNPCsInTheRoom(action, location, giftItem);
      if (npcs.Length == 0) return;

      Game1.afterDialogues = () =>
      {
        if (npcs.Length == 1)
          PromptSingle(location, npcs[0], giftItem);
        else
          PromptMultiple(location, npcs, giftItem);
      };
    }

    private static void PromptSingle(GameLocation location, NPC npc, SObject giftItem)
    {
      location.createQuestionDialogue(
        I18n.Door_GiftOffer_Single(Lexicon.prependArticle(giftItem.DisplayName), npc.displayName),
        location.createYesNoResponses(),
        (_, answer) =>
        {
          if (answer == "Yes") 
            GiveGift(npc, giftItem);
        }
      );
    }

    private static void GiveGift(NPC npc, SObject giftItem)
    {
      if (!Game1.player.friendshipData.ContainsKey(npc.Name))
        Game1.player.friendshipData.Add(npc.Name, new Friendship());

      CharacterPatch.IsGiftingThroughDoor = true;
      npc.receiveGift(
        o: giftItem, 
        giver: Game1.player, 
        updateGiftLimitInfo: giftItem.QualifiedItemId != "(O)StardropTea", 
        friendshipChangeMultiplier: ModEntry.Config.FriendshipMultiplier, 
        showResponse: false
      );
      CharacterPatch.IsGiftingThroughDoor = false;

      Game1.player.reduceActiveItemByOne();
      Game1.drawObjectDialogue(I18n.Door_GiftGiven(Lexicon.prependArticle(giftItem.DisplayName), npc.displayName));

      ModEntry.Log($"Player safely left {Lexicon.prependArticle(giftItem.Name)} for {npc.Name}.", LogLevel.Info);
    }

    private static void PromptMultiple(GameLocation location, NPC[] npcs, SObject gift)
    {
      location.createQuestionDialogue(
        I18n.Door_GiftOffer_Multiple(Lexicon.prependArticle(gift.DisplayName)),
        location.createYesNoResponses(),
        (_, answer) =>
        {
          if (answer != "Yes") return;

          DelayedAction.functionAfterDelay(() =>
          {
            location.createQuestionDialogue(
              I18n.Door_GiftOffer_Multiple_Who(),
              CreateNPCChoices(npcs),
              (_, chosen) =>
              {
                if (chosen == "Leave") return;

                NPC? npc = npcs.FirstOrDefault(n => n.Name == chosen);
                if (npc is not null)
                  GiveGift(npc, gift);
              }
            );
          }, 1);
        }
      );
    }

    private static NPC[] GetNPCsInTheRoom(string[] action, GameLocation location, SObject giftItem) =>
      action.Skip(1)
          .Select(n => Game1.getCharacterFromName(n))
          .Where(n => IsNPCValid(n, location, giftItem))
          .ToArray()!;

    private static bool IsNPCValid(NPC npc, GameLocation location, SObject giftItem)
    {
      if (npc == null)
      {
        ModEntry.Log("Found a null NPC in the action array.");
        return false;
      }

      if (npc.IsInvisible)
      {
        ModEntry.Log($"{npc.Name} failed: Is Invisible.");
        return false;
      }

      if (!IsNPCBehindThisDoor(npc, location))
      {
        ModEntry.Log($"{npc.Name} failed: Wrong Location ({npc.currentLocation?.NameOrUniqueName} != {location.NameOrUniqueName}).");
        return false;
      }

      if (!npc.CanReceiveGifts())
      {
        ModEntry.Log($"{npc.Name} failed: Cannot receive gifts.");
        return false;
      }

      if (ModEntry.Config.RequireNpcMet && !Game1.player.friendshipData.ContainsKey(npc.Name))
      {
        ModEntry.Log($"{npc.Name} failed: Config requires meeting them, and player hasn't.");
        return false;
      }

      if (!CanAcceptGiftToday(npc, giftItem))
        return false;

      ModEntry.Log($"{npc.Name} PASSED all checks! Adding to list.");
      return true;
    }

    private static bool CanAcceptGiftToday(NPC npc, SObject giftItem) 
    {
      // Is the active item Stardrop Tea? If yes, accept.
      if (giftItem.QualifiedItemId == "(O)StardropTea") 
      {
        ModEntry.Log($"{npc.Name} accepted: Item is Stardrop Tea (bypasses all limits).");
        return true;
      } 

      // Did we have a frienship data for the NPC? If no, accept
      if (!Game1.player.friendshipData.TryGetValue(npc.Name, out Friendship friendship))
      {
        ModEntry.Log($"{npc.Name} accepted: No friendship data exists yet (first gift).");
        return true;
      }

      // Did we give this NPC a gift today? If yes, reject
      if (friendship.GiftsToday >= 1)
      {
        ModEntry.Log($"{npc.Name} failed: Already received a gift today.");
        return false;
      }

      // Are we married to this NPC? Is the NPC our child? Is this NPC's birthday? If yes, accept
      if (Game1.player.spouse == npc.Name)
      {
        ModEntry.Log($"{npc.Name} accepted: Spouse bypasses weekly limit.");
        return true;
      }

      if (npc is Child)
      {
        ModEntry.Log($"{npc.Name} accepted: Child bypasses weekly limit.");
        return true;
      }

      if (npc.isBirthday())
      {
        ModEntry.Log($"{npc.Name} accepted: Birthday bypasses weekly limit.");
        return true;
      }

      // Have we givent this NPC enough gift this week? If yes, reject
      if (friendship.GiftsThisWeek >= NPC.maxGiftsPerWeek)
      {
        ModEntry.Log($"{npc.Name} failed: Reached the weekly gift limit ({friendship.GiftsThisWeek}/{NPC.maxGiftsPerWeek}).");
        return false;
      }

      return true;
    }

    // Special custom helper for adjoining rooms. For instance, Harvey's door is in Hospital map, but Harvey's room is in HarveyRoom map
    // Just hardcoded check.
    private static bool IsNPCBehindThisDoor(NPC npc, GameLocation doorLocation)
    {
      if (npc.currentLocation is null)
        return false;

      string npcLoc = npc.currentLocation.NameOrUniqueName;
      string doorLoc = doorLocation.NameOrUniqueName;

      // Standard check for bedroom on the same map
      if (npcLoc == doorLoc)
        return true;

      // Harvey check
      if (doorLoc == "Hospital" && npcLoc == "HarveyRoom")
        return true;

      // TODO Add other mod specific location checks for adjoining rooms

      return false;
    }

    private static Response[] CreateNPCChoices(NPC[] npcs)
    {
      var result = new Response[npcs.Length + 1];
      for (int i = 0; i < npcs.Length; i++)
        result[i] = new Response(npcs[i].Name, npcs[i].displayName);

      result[^1] = new Response("Leave", I18n.Door_GiftOffer_Cancel());
      return result;
    }


  }
}
