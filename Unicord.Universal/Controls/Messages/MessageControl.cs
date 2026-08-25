using System;
using System.Windows.Input;
using Unicord.Universal.Models.Messages;
using Windows.Devices.Input;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

namespace Unicord.Universal.Controls.Messages
{
    public class MessageControl : Control
    {
        private ImageBrush _imageBrush;
        private Grid _messageSurface;
        private Border _hoverHighlight;
        private Border _quickActions;

        #region Dependency Properties

        public MessageViewModel MessageViewModel
        {
            get { return (MessageViewModel)GetValue(MessageViewModelProperty); }
            set { SetValue(MessageViewModelProperty, value); }
        }

        public static readonly DependencyProperty MessageViewModelProperty =
            DependencyProperty.Register("MessageViewModel", typeof(MessageViewModel), typeof(MessageControl), new PropertyMetadata(null, OnPropertyChanged));

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MessageControl control && e.Property == MessageViewModelProperty)
            {
                control.OnMessageChanged(e);
            }
        }

        #endregion

        public MessageControl()
        {
            DefaultStyleKey = typeof(MessageControl);

            PointerEntered += MessageControl_PointerEntered;
            PointerExited += MessageControl_PointerExited;
            Unloaded += MessageControl_Unloaded;
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            ResetDesktopChrome();
        }

        protected virtual void OnMessageChanged(DependencyPropertyChangedEventArgs e)
        {
            ResetDesktopChrome();
            ApplyTemplate();

            if (e.NewValue is MessageViewModel message)
            {
                UpdateProfileImage(message);
            }
            else
            {
                ClearProfileImage();
            }
        }

        private void MessageControl_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            // Quick actions are a pointer affordance. Touch users keep the existing long-press/
            // context behavior without a toolbar suddenly appearing under their finger.
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
                return;

            EnsureDesktopChrome();

            if (_hoverHighlight != null)
                _hoverHighlight.Opacity = 1;

            if (_quickActions != null)
            {
                _quickActions.Opacity = 1;
                _quickActions.IsHitTestVisible = true;
            }
        }

        private void MessageControl_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            HideDesktopChrome();
        }

        private void MessageControl_Unloaded(object sender, RoutedEventArgs e)
        {
            HideDesktopChrome();
        }

        private void EnsureDesktopChrome()
        {
            if (MessageViewModel == null)
                return;

            if (_messageSurface == null)
                _messageSurface = FindDescendantByName<Grid>(this, "MainBorder");

            if (_messageSurface == null)
                return;

            if (_hoverHighlight == null)
            {
                _hoverHighlight = new Border
                {
                    Background = GetThemeBrush("SystemControlHighlightListLowBrush", Color.FromArgb(18, 255, 255, 255)),
                    Opacity = 0,
                    IsHitTestVisible = false
                };

                _messageSurface.Children.Insert(0, _hoverHighlight);
            }

            if (_quickActions == null)
            {
                var panel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 0
                };

                panel.Children.Add(CreateActionButton("\uE97A", "Reply", MessageViewModel.ReplyCommand));
                panel.Children.Add(CreateActionButton("\uE8C8", "Copy text", MessageViewModel.CopyMessageCommand));

                if (MessageViewModel.Author?.IsCurrent == true)
                    panel.Children.Add(CreateActionButton("\uE70F", "Edit message", MessageViewModel.EditCommand));

                var more = CreateActionButton("\uE712", "More actions", null);
                more.Click += MoreActions_Click;
                panel.Children.Add(more);

                _quickActions = new Border
                {
                    Background = GetThemeBrush("CardBackgroundFillColorDefaultBrush", Color.FromArgb(245, 32, 32, 32)),
                    BorderBrush = GetThemeBrush("CardStrokeColorDefaultBrush", Color.FromArgb(64, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(2),
                    Margin = new Thickness(0, -10, 8, 0),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Opacity = 0,
                    IsHitTestVisible = false,
                    Child = panel
                };

                // Children added later in the Grid render above the earlier hover highlight,
                // so no explicit z-index API is necessary (and Panel.SetZIndex is not UWP).
                _messageSurface.Children.Add(_quickActions);
            }
        }

        private Button CreateActionButton(string glyph, string tooltip, ICommand command)
        {
            var button = new Button
            {
                Width = 32,
                Height = 32,
                MinWidth = 32,
                MinHeight = 32,
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                Command = command,
                Content = new FontIcon
                {
                    Glyph = glyph,
                    FontFamily = new FontFamily("Segoe Fluent Icons"),
                    FontSize = 14
                }
            };

            ToolTipService.SetToolTip(button, tooltip);
            return button;
        }

        private void MoreActions_Click(object sender, RoutedEventArgs e)
        {
            if (_messageSurface?.ContextFlyout == null || sender is not FrameworkElement target)
                return;

            _messageSurface.ContextFlyout.ShowAt(target);
        }

        private void HideDesktopChrome()
        {
            if (_hoverHighlight != null)
                _hoverHighlight.Opacity = 0;

            if (_quickActions != null)
            {
                _quickActions.Opacity = 0;
                _quickActions.IsHitTestVisible = false;
            }
        }

        private void ResetDesktopChrome()
        {
            if (_messageSurface != null)
            {
                if (_hoverHighlight != null)
                    _messageSurface.Children.Remove(_hoverHighlight);

                if (_quickActions != null)
                    _messageSurface.Children.Remove(_quickActions);
            }

            _messageSurface = null;
            _hoverHighlight = null;
            _quickActions = null;
        }

        private static T FindDescendantByName<T>(DependencyObject root, string name) where T : FrameworkElement
        {
            if (root == null)
                return null;

            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T element && element.Name == name)
                    return element;

                var nested = FindDescendantByName<T>(child, name);
                if (nested != null)
                    return nested;
            }

            return null;
        }

        private static Brush GetThemeBrush(string key, Color fallback)
        {
            try
            {
                return Application.Current.Resources[key] as Brush ?? new SolidColorBrush(fallback);
            }
            catch
            {
                return new SolidColorBrush(fallback);
            }
        }

        private void ClearProfileImage()
        {
            if (_imageBrush == null)
            {
                var container = (Ellipse)GetTemplateChild("ImageContainer");
                if (container == null || container.Fill == null)
                    return;

                _imageBrush = (ImageBrush)container.Fill;
            }

            _imageBrush.ImageSource = null;
        }

        private void UpdateProfileImage(MessageViewModel message)
        {
            ClearProfileImage();

            if (_imageBrush == null || message.Author == null || message.Author.AvatarUrl == null)
                return;

            _imageBrush.ImageSource = new BitmapImage
            {
                UriSource = new Uri(message.Author.AvatarUrl),
                DecodePixelHeight = 64,
                DecodePixelWidth = 64,
                DecodePixelType = DecodePixelType.Physical
            };
        }
    }
}
