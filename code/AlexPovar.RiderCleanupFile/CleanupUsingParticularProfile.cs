using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.CommandProcessing;
using JetBrains.Application.Progress;
using JetBrains.Application.UI.Progress;
using JetBrains.DocumentManagers.Transactions;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.CodeCleanup;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.TextControl;
using JetBrains.Util;

namespace AlexPovar.RiderCleanupFile
{
  public class CleanupUsingParticularProfile : IBulbAction, IContextAction
  {
    [NotNull] private readonly ICSharpContextActionDataProvider _dataProvider;
    private readonly CodeCleanupProfile _profile;

    public CleanupUsingParticularProfile([NotNull] ICSharpContextActionDataProvider dataProvider, CodeCleanupProfile profile)
    {
      _dataProvider = dataProvider;
      _profile = profile;
    }

    public string TextOverride { get; set; }

    public void Execute(ISolution solution, ITextControl textControl)
    {
      Shell.Instance.GetComponent<UITaskExecutor>().SingleThreaded.ExecuteTask("Cleanup file", TaskCancelable.No, progress =>
      {
        progress.TaskName = _profile.Name;
        progress.Start(1);

        using (Shell.Instance.GetComponent<ICommandProcessor>().UsingBatchTextChange("Code Cleanup"))
        {
          using (var cookie = solution.GetComponent<SolutionDocumentTransactionManager>().CreateTransactionCookie(DefaultAction.Rollback, "Code cleanup"))
          {
            var progressIndicator = NullProgressIndicator.Create();

            CodeCleanup instance = CodeCleanup.GetInstance(solution);

            int caret = -1;
            instance.Run(this._dataProvider.SourceFile, DocumentRange.InvalidRange, ref caret, _profile, NullProgressIndicator.Create());

            cookie.Commit(progressIndicator);
          }
        }

        progress.Stop();
      });

    }

    public string Text => TextOverride ?? $"Cleanup using '{_profile.Name}' profile";

    public IEnumerable<IntentionAction> CreateBulbItems()
    {
      return this.ToContextActionIntentions();
    }

    public bool IsAvailable(IUserDataHolder cache)
    {
      return true;
    }
  }
}
