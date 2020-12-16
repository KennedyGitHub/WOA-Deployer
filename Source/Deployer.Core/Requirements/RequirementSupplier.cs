﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using Zafiro.Core.Patterns.Either;
using Zafiro.UI;
using IPopup = Zafiro.UI.IPopup;

namespace Deployer.Core.Requirements
{
    public class RequirementSupplier : IRequirementSupplier
    {
        private readonly Func<ResolveSettings, IRequirementSolver> solverFactory;
        private readonly IPopup popup;
        private readonly Func<string, IHaveDataContext> contentFactory;

        public RequirementSupplier(Func<ResolveSettings, IRequirementSolver> solverFactory, IPopup popup, Func<string, IHaveDataContext> contentFactory)
        {
            this.solverFactory = solverFactory;
            this.popup = popup;
            this.contentFactory = contentFactory;
        }

        public async Task<Either<ErrorList, IEnumerable<FulfilledRequirement>>> Satisfy(IEnumerable<MissingRequirement> requirements)
        {
            if (!requirements.Any())
            {
                return Either.Success<ErrorList, IEnumerable<FulfilledRequirement>>(Enumerable.Empty<FulfilledRequirement>());
            }

            var individualSuppliers = requirements.Select(Supplier).ToList();
            var vm = new DependenciesModel2(individualSuppliers);

            await popup.ShowAsModal(contentFactory("Requirements"), vm,
                c =>
                {
                    c.View.Title = "Please, specify the following information";
                    c.AddOption(new Zafiro.UI.Option("OK", ReactiveCommand.Create(() =>
                    {
                        c.Model.Continue = true;
                        c.View.Close();
                    }, c.Model.IsValid)));
                });

            if (vm.Continue)
            {
                return Either.Success<ErrorList, IEnumerable<FulfilledRequirement>>(Extract(vm));
            }

            return Either.Error<ErrorList, IEnumerable<FulfilledRequirement>>(new ErrorList("Operation cancelled"));
        }

        private IEnumerable<FulfilledRequirement> Extract(DependenciesModel2 vm)
        {
            return vm.Solvers.SelectMany(Extract);
        }

        private IEnumerable<FulfilledRequirement> Extract(IRequirementSolver vm)
        {
            return vm.FulfilledRequirements();
        }

        private IRequirementSolver Supplier(MissingRequirement missingRequirement)
        {
            return solverFactory(new ResolveSettings(missingRequirement.Key, missingRequirement.Kind));
        }
    }
}