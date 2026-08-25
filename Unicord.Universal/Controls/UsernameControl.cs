using System;
using Unicord.Universal.Models.User;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Unicord.Universal.Controls
{
    public sealed class UsernameControl : Control
    {
        private Grid _rootGrid;
        private Border _guildTagContainer;
        private Image _guildTagBadge;
        private TextBlock _guildTagText;

        public UserViewModel User
        {
            get => (UserViewModel)GetValue(UserProperty);
            set => SetValue(UserProperty, value);
        }

        public static readonly DependencyProperty UserProperty =
            DependencyProperty.Register(
                "User",
                typeof(UserViewModel),
                typeof(UsernameControl),
                new PropertyMetadata(null, OnUserChanged));

        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        public static readonly DependencyProperty IconSizeProperty =
            DependencyProperty.Register("IconSize", typeof(double), typeof(UsernameControl), new PropertyMetadata(16));

        public UsernameControl()
        {
            this.DefaultStyleKey = typeof(UsernameControl);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var authorName = GetTemplateChild("authorName") as FrameworkElement;
            _rootGrid = authorName == null ? null : VisualTreeHelper.GetParent(authorName) as Grid;

            EnsureGuildTagVisual();
            UpdateGuildTag();
        }

        private static void OnUserChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            ((UsernameControl)dependencyObject).UpdateGuildTag();
        }

        private void EnsureGuildTagVisual()
        {
            if (_rootGrid == null || _guildTagContainer != null)
                return;

            while (_rootGrid.ColumnDefinitions.Count < 3)
                _rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _guildTagBadge = new Image
            {
                Width = Math.Max(10, IconSize - 2),
                Height = Math.Max(10, IconSize - 2),
                Margin = new Thickness(0, 0, 3, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Stretch = Stretch.Uniform
            };

            _guildTagText = new TextBlock
            {
                FontSize = Math.Max(10, FontSize - 2),
                FontWeight = Windows.UI.Text.FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 48,
                Foreground = GetBrush("TextFillColorSecondaryBrush", Colors.LightGray)
            };

            var content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            content.Children.Add(_guildTagBadge);
            content.Children.Add(_guildTagText);

            _guildTagContainer = new Border
            {
                Margin = new Thickness(6, 0, 0, 0),
                Padding = new Thickness(4, 1, 4, 1),
                CornerRadius = new CornerRadius(4),
                Background = GetBrush("CardBackgroundFillColorDefaultBrush", Color.FromArgb(28, 255, 255, 255)),
                BorderBrush = GetBrush("CardStrokeColorDefaultBrush", Color.FromArgb(32, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
                Child = content,
                Visibility = Visibility.Collapsed
            };

            Grid.SetColumn(_guildTagContainer, 2);
            _rootGrid.Children.Add(_guildTagContainer);
        }

        private void UpdateGuildTag()
        {
            if (_guildTagContainer == null)
                return;

            var primaryGuild = User?.User?.PrimaryGuild;
            var tag = primaryGuild?.Tag?.Trim();

            if (primaryGuild?.IdentityEnabled != true || string.IsNullOrWhiteSpace(tag))
            {
                _guildTagContainer.Visibility = Visibility.Collapsed;
                _guildTagText.Text = string.Empty;
                _guildTagBadge.Source = null;
                return;
            }

            _guildTagText.Text = tag;
            _guildTagBadge.Source = null;
            _guildTagBadge.Visibility = Visibility.Collapsed;

            if (!string.IsNullOrWhiteSpace(primaryGuild.BadgeUrl))
            {
                try
                {
                    _guildTagBadge.Source = new BitmapImage(new Uri(primaryGuild.BadgeUrl));
                    _guildTagBadge.Visibility = Visibility.Visible;
                }
                catch (UriFormatException)
                {
                    // A malformed badge identifier should never break username rendering.
                    _guildTagBadge.Source = null;
                    _guildTagBadge.Visibility = Visibility.Collapsed;
                }
            }

            ToolTipService.SetToolTip(_guildTagContainer, $"Server tag: {tag}");
            _guildTagContainer.Visibility = Visibility.Visible;
        }

        private static Brush GetBrush(string resourceKey, Color fallbackColor)
        {
            if (Application.Current?.Resources?[resourceKey] is Brush brush)
                return brush;

            return new SolidColorBrush(fallbackColor);
        }
    }
}
