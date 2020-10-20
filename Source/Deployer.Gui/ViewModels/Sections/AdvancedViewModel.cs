﻿using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Deployer.Core;
using Deployer.Core.Deployers;
using Deployer.Core.Interaction;
using Deployer.Gui.ViewModels.Common;
using Grace.DependencyInjection.Attributes;
using Iridio;
using Iridio.Common;
using Iridio.Runtime;
using ReactiveUI;
using Zafiro.Core.Patterns.Either;
using Zafiro.Core.UI;

namespace Deployer.Gui.ViewModels.Sections
{
    [Metadata("Name", "Advanced")]
    [Metadata("Order", 2)]
    public class AdvancedViewModel : ReactiveObject, ISection
    {
        private readonly BrandNewDeployer deployer;
        private readonly IDialogService dialogService;
        private readonly CompositeDisposable disposables = new CompositeDisposable();
        private readonly IFilePicker filePicker;

        public AdvancedViewModel(BrandNewDeployer deployer, IDialogService dialogService,
            IFilePicker filePicker, OperationProgressViewModel operationProgress)
        {
            this.deployer = deployer;
            this.dialogService = dialogService;
            this.filePicker = filePicker;
            OperationProgress = operationProgress;

            ConfigureCommands();

            IsBusyObservable = RunScript.IsExecuting;
        }

        public OperationProgressViewModel OperationProgress { get; }

        public ReactiveCommand<Unit, Either<DeployError, Success>> RunScript { get; set; }

        public IObservable<bool> IsBusyObservable { get; }

        private void ConfigureCommands()
        {
            ConfigureRunCommand();
        }

        private void ConfigureRunCommand()
        {
            RunScript = RunScriptCommand(deployer, filePicker);

            RunScript
                .Subscribe(either => either
                    .MapRight(success =>
                        dialogService.Notice("Execution finished", "The script has been executed successfully"))
                    .Handle(errors =>
                        dialogService.Notice("Execution failed", $"The script execution has failed {errors}"))
                    .DisposeWith(disposables));

            dialogService.HandleExceptionsFromCommand(RunScript,
                exception => ("Script execution failed", exception.Message));
        }

        private static ReactiveCommand<Unit, Either<DeployError, Success>> RunScriptCommand(BrandNewDeployer deployer, IFilePicker filePicker)
        {
            return ReactiveCommand.CreateFromObservable(() =>
            {
                var filter = new FileTypeFilter("Deployer Script", "*.ds", "*.txt");
                return filePicker
                    .Open("Select a script", new[] {filter})
                    .Where(x => x != null)
                    .SelectMany(file => Observable.FromAsync(() => deployer.Run(file.Source.LocalPath)));
            });
        }
    }
}