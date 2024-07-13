using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace GeminiChessAnalysis.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FenImportPage : ContentPage
    {

        public event Action<string> FenEntered;

        public FenImportPage(Action<string> onFenEntered)
        {
            InitializeComponent();
            FenEntered = onFenEntered;
        }

        private void OnOkClicked(object sender, EventArgs e)
        {
            // Get the FEN input from the Entry
            string fen = FenEntry.Text;
            FenEntered?.Invoke(fen);
            // Close the page
            Navigation.PopAsync();
        }

        private void OnCancelClicked(object sender, EventArgs e)
        {
            // Optionally clear the FEN input
            FenEntered?.Invoke(null);
            // Close the page
            Navigation.PopAsync();
        }

    }
}