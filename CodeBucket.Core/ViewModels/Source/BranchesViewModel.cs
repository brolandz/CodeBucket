using System;
using BitbucketSharp.Models;
using CodeBucket.Core.ViewModels.Commits;
using ReactiveUI;
using System.Reactive.Linq;
using CodeBucket.Core.Services;
using System.Reactive;
using Splat;

namespace CodeBucket.Core.ViewModels.Source
{
    public class BranchesViewModel : BaseViewModel, ILoadableViewModel, IProvidesSearch, IListViewModel<ReferenceItemViewModel>
    {
        public IReadOnlyReactiveList<ReferenceItemViewModel> Items { get; }

        public IReactiveCommand<Unit> LoadCommand { get; }

        private string _searchText;
        public string SearchText
        {
            get { return _searchText; }
            set { this.RaiseAndSetIfChanged(ref _searchText, value); }
        }

        public BranchesViewModel(
            string username, string repository,
            IApplicationService applicationService = null)
        {
            applicationService = applicationService ?? Locator.Current.GetService<IApplicationService>();

            Title = "Branches";

            var branches = new ReactiveList<ReferenceModel>();
            Items = branches.CreateDerivedCollection(
                branch =>
                {
                    var vm = new ReferenceItemViewModel(branch.Branch);
                    vm.GoToCommand
                      .Select(_ => new CommitsViewModel(username, repository, branch.Node))
                      .Subscribe(NavigateTo);
                    return vm;
                },
                x => x.Branch.ContainsKeyword(SearchText),
                signalReset: this.WhenAnyValue(x => x.SearchText));

            LoadCommand = ReactiveCommand.CreateAsyncTask(async _ =>
            {
                var items = await applicationService.Client.Repositories.GetBranches(username, repository);
                branches.Reset(items.Values);
            });
        }
    }
}
