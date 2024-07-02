using Xamarin.Forms;

namespace GeminiChessAnalysis.CustomItems
{
    public class BindableStackLayout : StackLayout
    {
        public static readonly BindableProperty SelectedIndexProperty = BindableProperty.Create(
            nameof(SelectedIndex),
            typeof(int),
            typeof(BindableStackLayout),
            default(int),
            propertyChanged: OnSelectedIndexChanged);

        public int SelectedIndex
        {
            get { return (int)GetValue(SelectedIndexProperty); }
            set { SetValue(SelectedIndexProperty, value); }
        }

        private static void OnSelectedIndexChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var layout = (BindableStackLayout)bindable;
            var index = (int)newValue;

            for (int i = 0; i < layout.Children.Count; i++)
            {
                var view = layout.Children[i];
                view.BackgroundColor = i == index ? Color.LightGreen : Color.Transparent;
            }
        }

        protected override void OnChildAdded(Element child)
        {
            base.OnChildAdded(child);
            OnSelectedIndexChanged(this, null, SelectedIndex);
        }
    }
}
