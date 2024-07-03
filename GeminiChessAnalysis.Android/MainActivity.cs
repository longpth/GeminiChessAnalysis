using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using Android.Content;
using GeminiChessAnalysis.Services;
using Android.Views;

namespace GeminiChessAnalysis.Droid
{
    [Activity(Label = "@string/app_name", Exported = true ,Icon = "@mipmap/icon", Theme = "@style/MySplash", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize, ScreenOrientation = ScreenOrientation.Portrait)]
    [IntentFilter(new[] { Intent.ActionMain }, Categories = new[] {
        Intent.CategoryLauncher
        })]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] {
        Intent.CategoryDefault
        }, DataMimeType = "text/plain")]
    [IntentFilter(new[] { Intent.ActionView }, Categories = new[] {
        Intent.CategoryDefault
        }, DataMimeType = "text/plain")]

    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.Window.RequestFeature(WindowFeatures.NoTitle); // This line will hide the title bar

            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new App());

            // Switch back to your main theme
            base.SetTheme(Resource.Style.MainTheme);

            // Handle the intent
            HandleIntent(Intent);
        }

        private void HandleIntent(Intent intent)
        {
            if (intent == null) return;

            string action = intent.Action;
            string type = intent.Type;

            if (Intent.ActionSend.Equals(action) && type != null)
            {
                if (type.Equals("text/plain"))
                {
                    HandleSendPgn(intent); // Handle PGN files being sent
                }
            }
        }

        private void HandleSendPgn(Intent intent)
        {
            var item = intent.ClipData.GetItemAt(0);
            string pgn_text = item.Text;
            MessageService.Instance.NotifySubscribers(pgn_text);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}