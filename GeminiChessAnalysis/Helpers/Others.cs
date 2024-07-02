using GeminiChessAnalysis.ViewModels;
using Plugin.SimpleAudioPlayer;
using System.IO;
using System.Reflection;

namespace GeminiChessAnalysis.Helpers
{
    public class Others
    {
        public static void PlayAudioFile(string fileName)
        {
            var player = CrossSimpleAudioPlayer.Current;
            player.Load(fileName);
            player.Play();
        }
    }
}
