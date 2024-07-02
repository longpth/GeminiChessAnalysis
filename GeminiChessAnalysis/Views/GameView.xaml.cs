using GeminiChessAnalysis.ViewModels;
using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace GeminiChessAnalysis.Views
{
    public partial class GameView : ContentPage
    {
        public GameView()
        {
            InitializeComponent();
            this.BindingContext = new GameViewModel();
        }
    }
}