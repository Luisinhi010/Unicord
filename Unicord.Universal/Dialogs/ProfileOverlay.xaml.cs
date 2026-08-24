using Unicord.Universal.Models.User;
using Unicord.Universal.Services;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Unicord.Universal.Dialogs
{
    public sealed partial class ProfileOverlay : UserControl
    {
        public UserViewModel User
        {
            get => (UserViewModel)GetValue(UserProperty);
            set => SetValue(UserProperty, value);
        }

        public static readonly DependencyProperty UserProperty =
            DependencyProperty.Register("User", typeof(UserViewModel), typeof(ProfileOverlay), new PropertyMetadata(null, OnUserChanged));

        private static void OnUserChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var overlay = (ProfileOverlay)d;
            overlay.Bindings.Update();
        }

        public ProfileOverlay()
        {
            InitializeComponent();
            Loaded += ProfileOverlay_Loaded;
        }

        private void ProfileOverlay_Loaded(object sender, RoutedEventArgs e)
        {
            if (NavView != null && OverviewItem != null)
                NavView.SelectedItem = OverviewItem;

            ShowSection("overview");
        }

        private void NavView_SelectionChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            var tag = args.SelectedItemContainer?.Tag as string;
            ShowSection(tag ?? "overview");
        }

        private void ShowSection(string section)
        {
            if (OverviewContent == null || MutualServersContent == null)
                return;

            var showMutual = section == "mutual";
            OverviewContent.Visibility = showMutual ? Visibility.Collapsed : Visibility.Visible;
            MutualServersContent.Visibility = showMutual ? Visibility.Visible : Visibility.Collapsed;
        }

        private void CopyUserId_Click(object sender, RoutedEventArgs e)
        {
            if (User == null)
                return;

            var package = new DataPackage();
            package.SetText(User.Id.ToString());
            Clipboard.SetContent(package);
        }

        private void DropShadowPanel_PreviewKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Escape)
            {
                OverlayService.GetForCurrentView()
                    .CloseOverlay();
            }
        }
    }
}
