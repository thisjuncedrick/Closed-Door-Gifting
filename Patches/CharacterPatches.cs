namespace Closed_Door_Gifting.Patches
{
  internal static class CharacterPatch
  {
    public static bool IsGiftingThroughDoor { get; set; } = false;

    public static bool DoEmote_Prefix()
    {
      if (IsGiftingThroughDoor)
        return false;
      
      return true;
    }
  }
}
