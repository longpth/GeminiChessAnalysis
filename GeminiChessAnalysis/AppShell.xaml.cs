using GeminiChessAnalysis.ViewModels;
using GeminiChessAnalysis.Views;
using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace GeminiChessAnalysis
{
    public partial class AppShell : Xamarin.Forms.Shell
    {
        public AppShell()
        {
            InitializeComponent();
            //Routing.RegisterRoute(nameof(ItemDetailPage), typeof(ItemDetailPage));
            //Routing.RegisterRoute(nameof(NewItemPage), typeof(NewItemPage));
            Routing.RegisterRoute("AboutPage", typeof(AboutPage));
        }

        private async void OnMenuItemClicked(object sender, EventArgs e)
        {
            // Navigate to the About page
            await Shell.Current.GoToAsync("AboutPage");

            // Collapse the Flyout menu
            Shell.Current.FlyoutIsPresented = false;
        }
    }
}
