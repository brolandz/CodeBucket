using System;
using UIKit;
using System.Linq;
using System.Reactive.Linq;
using CodeBucket.Core.ViewModels.Commits;
using Humanizer;
using CodeBucket.Core.Utils;
using CodeBucket.DialogElements;
using BitbucketSharp.Models;
using ReactiveUI;
using CodeBucket.ViewControllers.Comments;

namespace CodeBucket.ViewControllers.Commits
{
    public class CommitViewController : PrettyDialogViewController<CommitViewModel>
    {
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            TableView.RowHeight = UITableView.AutomaticDimension;
            TableView.EstimatedRowHeight = 80f;

            HeaderView.SetImage(null, Images.Avatar);

            var detailsSection = new Section();
            var diffButton = new StringElement(AtlassianIcon.ListAdd.ToImage());
            var removedButton = new StringElement(AtlassianIcon.ListRemove.ToImage());
            var modifiedButton = new StringElement(AtlassianIcon.Edit.ToImage());
            var allChangesButton = new StringElement("All Changes", AtlassianIcon.Devtoolssidediff.ToImage());
            detailsSection.Add(new [] { diffButton, removedButton, modifiedButton, allChangesButton });

            var additions = ViewModel.WhenAnyValue(x => x.DiffAdditions);
            diffButton.BindClick(ViewModel.GoToAddedFiles);
            diffButton.BindDisclosure(additions.Select(x => x > 0));
            diffButton.BindCaption(additions.Select(x => $"{x} Added"));

            var deletions = ViewModel.WhenAnyValue(x => x.DiffDeletions);
            removedButton.BindClick(ViewModel.GoToRemovedFiles);
            removedButton.BindDisclosure(deletions.Select(x => x > 0));
            removedButton.BindCaption(deletions.Select(x => $"{x} Removed"));

            var modifications = ViewModel.WhenAnyValue(x => x.DiffModifications);
            modifiedButton.BindClick(ViewModel.GoToModifiedFiles);
            modifiedButton.BindDisclosure(modifications.Select(x => x > 0));
            modifiedButton.BindCaption(modifications.Select(x => $"{x} Modified"));

            allChangesButton.BindClick(ViewModel.GoToAllFiles);

            var split = new SplitButtonElement();
            var comments = split.AddButton("Comments", "-");
            var participants = split.AddButton("Participants", "-");
            var approvals = split.AddButton("Approvals", "-");
            var headerSection = new Section { split };

            var actionButton = new UIBarButtonItem(UIBarButtonSystemItem.Action);
            NavigationItem.RightBarButtonItem = actionButton;
            OnActivation(d => actionButton.BindCommand(ViewModel.ShowMenuCommand).AddTo(d));

            ViewModel.AddCommentCommand.Subscribe(_ => AddCommentTapped());

            var detailSection = new Section();
            var detailsElement = new MultilinedElement();
            detailSection.Add(detailsElement);

            var approvalSection = new Section("Approvals");
            var approveElement = new LoaderButtonElement("Approve", AtlassianIcon.Approve.ToImage());
            approveElement.Accessory = UITableViewCellAccessory.None;
            approveElement.BindLoader(ViewModel.ToggleApproveButton);
            approveElement.BindCaption(ViewModel.WhenAnyValue(x => x.Approved).Select(x => x ? "Unapprove" : "Approve"));

            var commentsSection = new Section("Comments");
            var addCommentElement = new StringElement("Add Comment", AtlassianIcon.Addcomment.ToImage());
            addCommentElement.Clicked.Subscribe(_ => AddCommentTapped());

            if (ViewModel.ShowRepository)
            {
                var repo = new StringElement(ViewModel.Repository) { 
                    Accessory = UITableViewCellAccessory.DisclosureIndicator, 
                    Lines = 1, 
                    TextColor = StringElement.DefaultDetailColor,
                    Image = AtlassianIcon.Devtoolsrepository.ToImage()
                };

                detailSection.Add(repo);
                repo.Clicked.BindCommand(ViewModel.GoToRepositoryCommand);
            }

            ViewModel.WhenAnyValue(x => x.Commit).IsNotNull().Subscribe(x => {
                participants.Text = x?.Participants?.Count.ToString() ?? "-";
                approvals.Text = x?.Participants?.Count(y => y.Approved).ToString() ?? "-";

                var titleMsg = (ViewModel.Commit.Message ?? string.Empty).Split(new [] { '\n' }, 2).FirstOrDefault();
                var avatarUrl = ViewModel.Commit.Author?.User?.Links?.Avatar?.Href;

                HeaderView.SubText = titleMsg ?? "Commited " + (ViewModel.Commit.Date).Humanize();
                HeaderView.SetImage(new Avatar(avatarUrl).ToUrl(128), Images.Avatar);
                RefreshHeaderView();

                var user = x.Author?.User?.DisplayName ?? x.Author.Raw ?? "Unknown";
                detailsElement.Caption = user;
                detailsElement.Details = x.Message;
            });

            this.WhenAnyValue(x => x.ViewModel.Approvals)
                .Subscribe(x =>
                {
                    var elements = x.Select(y => new UserElement(y)).OfType<Element>();
                    approvalSection.Reset(elements.Concat(new[] { approveElement }));
                });

            ViewModel.Comments
                     .CountChanged
                     .Select(x => x.ToString())
                     .StartWith("-")
                     .Subscribe(x => comments.Text = x);

            ViewModel.Comments
                     .ChangedObservable()
                     .Select(x => x.Select(y => new CommentElement(y)).OfType<Element>())
                     .Subscribe(x => commentsSection.Reset(x.Concat(new[] { addCommentElement })));

            ViewModel.WhenAnyValue(x => x.Commit)
                .IsNotNull()
                .Take(1)
                .Subscribe(_ => Root.Reset(headerSection, detailsSection, approvalSection, commentsSection));

            ViewModel.GoToAddedFiles.Select(_ => ChangesetModel.FileType.Added).Subscribe(GoToFiles);
            ViewModel.GoToRemovedFiles.Select(_ => ChangesetModel.FileType.Removed).Subscribe(GoToFiles);
            ViewModel.GoToModifiedFiles.Select(_ => ChangesetModel.FileType.Modified).Subscribe(GoToFiles);
            ViewModel.GoToAllFiles.Subscribe(_ => GoToAllFiles());
        }

        private void GoToFiles(ChangesetModel.FileType commitFileType)
        {
            var files = ViewModel.CommitFiles.Where(x => x.Type == commitFileType);
            var viewController = new CommitFileChangesViewController(files);
            viewController.Title = commitFileType.ToString();
            NavigationController.PushViewController(viewController, true);
        }

        private void GoToAllFiles()
        {
            var viewController = new CommitFileChangesViewController(ViewModel.CommitFiles);
            viewController.Title = "All Changes";
            NavigationController.PushViewController(viewController, true);
        }

        void AddCommentTapped()
        {
            var newCommentVC = new NewCommentViewController();
            newCommentVC.ViewModel = ViewModel.NewCommentViewModel;
            newCommentVC.OnActivation(d =>
            {
                newCommentVC.WhenAnyValue(x => x.ViewModel.DismissCommand)
                            .Switch()
                            .Subscribe(_ => DismissViewController(true, null))
                            .AddTo(d);
            });

            PresentViewController(new ThemedNavigationController(newCommentVC), true, null);
        }
    }
}
