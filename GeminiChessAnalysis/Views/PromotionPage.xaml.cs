using GeminiChessAnalysis.Models;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace GeminiChessAnalysis.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PromotionPage : ContentPage
    {
        public event Action<EnumPieceType> PromotionSelected;

        public PromotionPage(Action<EnumPieceType> onPromotionSelected, bool isWhiteTurn)
        {
            InitializeComponent();
            PromotionSelected = onPromotionSelected;
            // Set the image source based on isWhiteTurn
            string colorPrefix = isWhiteTurn ? "white" : "black";
            QueenButton.Source = ImageSource.FromFile($"{colorPrefix}_queen.png");
            RookButton.Source = ImageSource.FromFile($"{colorPrefix}_rook.png");
            BishopButton.Source = ImageSource.FromFile($"{colorPrefix}_bishop.png");
            KnightButton.Source = ImageSource.FromFile($"{colorPrefix}_knight.png");
        }

        private void OnPromoteToQueen(object sender, EventArgs e)
        {
            PromotionSelected?.Invoke(EnumPieceType.Queen);
            // Optionally close the promotion view or notify the parent view to do so
            ClosePage();
        }

        private void OnPromoteToRook(object sender, EventArgs e)
        {
            PromotionSelected?.Invoke(EnumPieceType.Rook);
            ClosePage();
        }

        private void OnPromoteToBishop(object sender, EventArgs e)
        {
            PromotionSelected?.Invoke(EnumPieceType.Bishop);
            ClosePage();
        }

        private void OnPromoteToKnight(object sender, EventArgs e)
        {
            PromotionSelected?.Invoke(EnumPieceType.Knight);
            ClosePage();
        }

        private void ClosePage()
        {
            // Close the page. This method needs to be implemented based on how you navigate.
            Navigation.PopModalAsync();
        }
    }
}
