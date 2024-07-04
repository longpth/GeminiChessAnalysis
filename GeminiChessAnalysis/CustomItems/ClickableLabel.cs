using System.Windows.Input;
using Xamarin.Forms;

namespace GeminiChessAnalysis.CustomItems
{
    public class ClickableLabel : Label
    {
        public static readonly BindableProperty CommandProperty = BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(ClickableLabel), null);
        public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(ClickableLabel), null);
        public static readonly BindableProperty IsVisibleAndClickableProperty = BindableProperty.Create(nameof(IsVisibleAndClickable), typeof(bool), typeof(ClickableLabel), true, propertyChanged: OnIsVisibleAndClickableChanged);


        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public object CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        public bool IsVisibleAndClickable
        {
            get { return (bool)GetValue(IsVisibleAndClickableProperty); }
            set { SetValue(IsVisibleAndClickableProperty, value); }
        }

        private static void OnIsVisibleAndClickableChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var clickableLabel = (ClickableLabel)bindable;
            bool isVisibleAndClickable = (bool)newValue;

            clickableLabel.Opacity = isVisibleAndClickable ? 1 : 0;
            clickableLabel.UpdateGestureRecognizers(isVisibleAndClickable);
        }

        private readonly TapGestureRecognizer tapGestureRecognizer;

        public ClickableLabel()
        {
            tapGestureRecognizer = new TapGestureRecognizer();
            tapGestureRecognizer.Tapped += (s, e) =>
            {
                if (Command != null && Command.CanExecute(CommandParameter))
                {
                    Command.Execute(CommandParameter);
                }
            };
            this.GestureRecognizers.Add(tapGestureRecognizer);
        }

        private void UpdateGestureRecognizers(bool isVisibleAndClickable)
        {
            if (isVisibleAndClickable)
            {
                if (!this.GestureRecognizers.Contains(tapGestureRecognizer))
                {
                    this.GestureRecognizers.Add(tapGestureRecognizer);
                }
            }
            else
            {
                if (this.GestureRecognizers.Contains(tapGestureRecognizer))
                {
                    this.GestureRecognizers.Remove(tapGestureRecognizer);
                }
            }
        }
    }
}
