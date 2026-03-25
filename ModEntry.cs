using xTile.Dimensions;

using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;

using StardewValley;
using StardewValley.Characters;

using Closed_Door_Gifting.Patches;

namespace Closed_Door_Gifting
{
  public class ModEntry : Mod
  {
#if DEBUG
    private const LogLevel DefaultLogLevel = LogLevel.Debug;
#else
    private const LogLevel DefaultLogLevel = LogLevel.Trace;
#endif

    internal static ModConfig Config = null!;
    internal static IMonitor? _monitor;

    public override void Entry(IModHelper helper)
    {
      I18n.Init(helper.Translation);
      _monitor = Monitor;
      Config = helper.ReadConfig<ModConfig>();

      var harmony = new Harmony(ModManifest.UniqueID);
      harmony.Patch(
        original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.performAction),
          new Type[] { typeof(string[]), typeof(Farmer), typeof(Location) }),
        postfix: new HarmonyMethod(typeof(DoorPatches), nameof(DoorPatches.PerformAction_Postfix))
      );
      harmony.Patch(
        original: AccessTools.Method(typeof(Character), nameof(Character.doEmote),
          new Type[] { typeof(int), typeof(bool), typeof(bool) }),
        prefix: new HarmonyMethod(typeof(CharacterPatch), nameof(CharacterPatch.DoEmote_Prefix))
      );

      helper.Events.GameLoop.GameLaunched += OnGameLaunched;
#if DEBUG
      helper.Events.Input.ButtonPressed   += OnButtonPressed;
#endif
    }

    private void OnGameLaunched(object? _sender, GameLaunchedEventArgs _e) =>
      Config.RegisterGMCM(Helper, ModManifest);

    private void OnButtonPressed(object? _sender, ButtonPressedEventArgs e)
    {
      if (!Context.IsWorldReady)
        return;
      
      if (e.Button == SButton.F5)
        Game1.warpFarmer("ScienceHouse", 7, 11, facingDirectionAfterWarp: 0);

      if (e.Button == SButton.F6)
        Game1.warpFarmer("ScienceHouse", 13, 11, facingDirectionAfterWarp: 0);

      if (e.Button == SButton.F2)
      {
        Vector2 cursorTile = e.Cursor.Tile;

        if (Game1.currentLocation.isCharacterAtTile(cursorTile) is not NPC npc || npc is Horse || npc is Junimo)
          return;

        if (!Game1.player.friendshipData.TryGetValue(npc.Name, out var friendship))
        {
          Game1.addHUDMessage(new HUDMessage($"{npc.Name}: No friendship data.", HUDMessage.error_type));
          return;
        }

        int heartPoints = friendship.Points;
        int heartLevel = heartPoints / NPC.friendshipPointsPerHeartLevel;

        Game1.addHUDMessage(new HUDMessage(
            $"{npc.displayName}: {heartLevel} hearts ({heartPoints} pts)",
            HUDMessage.newQuest_type
        ));
      }
    }

    public static void Log(string message, LogLevel level = DefaultLogLevel) =>
      _monitor?.Log(message, level);

  }
}
