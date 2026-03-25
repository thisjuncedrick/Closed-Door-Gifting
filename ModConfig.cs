using Closed_Door_Gifting.Integrations.GenericModConfigMenu;

using StardewModdingAPI;

namespace Closed_Door_Gifting
{
  internal class ModConfig
  {
    public bool UseMod { get; set; } = true;
    public bool RequireNpcMet { get; set; } = true;
    public float FriendshipMultiplier { get; set; } = 0.25f;

    public void RegisterGMCM(IModHelper helper, IManifest manifest)
    {
      var gmcm = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");

      if (gmcm is null)
        return;

      gmcm.Register(
        mod:   manifest,
        reset: () => ResetConfig(helper),
        save:  () => helper.WriteConfig(this)
      );

      gmcm.AddBoolOption(
        mod:      manifest,
        name:     () => I18n.Config_UseMod_Name(),
        tooltip:  () => I18n.Config_UseMod_Tooltip(),
        getValue: () => UseMod,
        setValue: v => UseMod = v
      );

      gmcm.AddBoolOption(
        mod:      manifest,
        name:     () => I18n.Config_RequireMet_Name(),
        tooltip:  () => I18n.Config_RequireMet_Tooltip(),
        getValue: () => RequireNpcMet,
        setValue: v => RequireNpcMet = v
      );

       gmcm.AddNumberOption(
        mod:      manifest,
        name:     () => I18n.Config_FriendshipMultiplier_Name(),
        tooltip:  () => I18n.Config_FriendshipMultiplier_Tooltip(),
        getValue: () => FriendshipMultiplier,
        setValue: v => FriendshipMultiplier = v
      );

      gmcm.AddParagraph(
        mod:  manifest,
        text: () => I18n.Config_FriendshipMultiplier_Text()
      );
    }

    private void ResetConfig(IModHelper helper)
    {
      UseMod = true;
      RequireNpcMet = true;
      FriendshipMultiplier = 0.25f;
      helper.WriteConfig(this);
    }
  }
}
