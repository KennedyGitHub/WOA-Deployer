﻿using Deployer.Core.Compiler;
using Deployer.Core.Deployers;
using Deployer.Core.Requirements;
using Deployer.Core.Scripting;
using Grace.DependencyInjection;
using Iridio.Binding;
using Iridio.Common;
using Iridio.Parsing;
using Iridio.Runtime;
using System;
using System.Threading.Tasks;
using Zafiro.Core;
using Zafiro.Core.Files;
using Zafiro.Core.FileSystem;
using Zafiro.Core.Net4x;
using Zafiro.Core.Patterns.Either;
using Binder = Iridio.Binding.Binder;

namespace Deployer.Core
{
    public class WoaDeployer
    {
        private readonly BrandNewDeployer deployer;
        private readonly IOperationProgress operationProgress = new OperationProgress();
        private readonly IOperationContext operationContext = new OperationContext();
        
        public WoaDeployer(Action<IExportRegistrationBlock> exportRegistrationBlock)
        {
            deployer = GetDeployer(exportRegistrationBlock);
        }

        private BrandNewDeployer GetDeployer(Action<IExportRegistrationBlock> exportRegistrationBlock)
        {
            var container = new DependencyInjectionContainer();

            container.Configure(c =>
            {
                exportRegistrationBlock(c);
                FunctionDependencies.Configure(c);
                c.Export<FileSystemOperations>().As<IFileSystemOperations>().Lifestyle.Singleton();
                c.Export<Preprocessor>().As<IPreprocessor>().Lifestyle.Singleton();
                c.Export<Parser>().As<IParser>().Lifestyle.Singleton();
                c.Export<Binder>().As<IBinder>().Lifestyle.Singleton();
                c.Export<DeployerCompiler>().As<IDeployerCompiler>().Lifestyle.Singleton();
                c.Export<Downloader>().As<IDownloader>().Lifestyle.Singleton();
                c.ExportFactory<string, IFileSystemOperations, IDownloader, IZafiroFile>((path, fo, dl) => new ZafiroFile(new Uri(path), fo, dl));
                c.Export<IridioRequirementsAnalyzer>().As<IRequirementsAnalyzer>().Lifestyle.Singleton();
                c.Export<ScriptRunner>().As<IScriptRunner>().Lifestyle.Singleton();
                c.ExportFactory(() => operationProgress).As<IOperationProgress>().Lifestyle.Singleton();
                c.ExportFactory(() => operationContext).As<IOperationContext>().Lifestyle.Singleton();

                foreach (var taskType in Function.Types)
                {
                    c.ExportFactory((Func<Type, object> locator) => new Function(taskType, locator))
                        .As<IFunction>()
                        .As<IFunctionDeclaration>()
                        .Lifestyle.Singleton();
                }
            });

            return container.Locate<BrandNewDeployer>();
        }

        public IOperationProgress OperationProgress => operationProgress;
        public IOperationContext OperationContext => operationContext;
        public IObservable<string> Messages => deployer.Messages;

        public Task<Either<DeployError, Success>> Run(string s)
        {
            return deployer.Run(s);
        }
    }
}