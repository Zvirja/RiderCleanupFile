using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.ReSharper.Feature.Services.CodeCleanup;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Intentions.ContextActions.Cleanup;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.UI.Icons;
using JetBrains.Util;

namespace AlexPovar.RiderCleanupFile
{
  [ContextAction(Group = "C#", Name = "Cleanup this file", Description = "Run code cleanup on current file.")]
  public class CleanFileQuickAction : IContextAction
  {
    private readonly ICSharpContextActionDataProvider _dataProvider;
    private static IAnchor MyAnchor => CleanupSelectionActionsProvider.CleanupActionItemsAnchor;

    private static IconId IconId => AlteringFeatuThemedIcons.CodeCleanupOptionPage.Id;

    public CleanFileQuickAction(ICSharpContextActionDataProvider dataProvider)
    {
      _dataProvider = dataProvider;
    }

    public IEnumerable<IntentionAction> CreateBulbItems()
    {
      var settingsStore = this._dataProvider.PsiFile.GetSettingsStore();

      var codeCleanupSettingsComponent = Shell.Instance.GetComponent<CodeCleanupSettingsComponent>();

      var profiles = codeCleanupSettingsComponent.GetProfiles(settingsStore);
      var silentCleanupProfile = codeCleanupSettingsComponent.GetSilentCleanupProfile(settingsStore);
      var defaultProfile = codeCleanupSettingsComponent.GetDefaultProfile(CodeCleanup.DefaultProfileType.FULL);

      var profileToUseByDefault = silentCleanupProfile ?? defaultProfile;

      yield return new IntentionAction(new CleanupUsingParticularProfile(_dataProvider, profileToUseByDefault) { TextOverride = "Cleanup this file" }, IconId, MyAnchor);

      if (silentCleanupProfile != null)
      {
        yield return new IntentionAction(new CleanupUsingParticularProfile(_dataProvider, silentCleanupProfile), IconId, MyAnchor);
      }

      foreach (var profile in profiles.Where(x => x != silentCleanupProfile))
      {
        yield return new IntentionAction(new CleanupUsingParticularProfile(_dataProvider, profile), IconId, MyAnchor);
      }
    }

    public bool IsAvailable(IUserDataHolder cache)
    {
      if (!this._dataProvider.DocumentSelection.TextRange.IsEmpty) return false;

      //Add this action only if cursor is outside of the namespace body
      if (this._dataProvider.GetSelectedElement<INamespaceBody>() != null) return false;

      return true;
    }
  }
}
