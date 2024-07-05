# GeminiChessAnalysis

GeminiChessAnalysis is a Xamarin.Forms application designed to enhance chess game analysis and learning, currently run as an Anrdoid app. It leverages powerful chess engines and provides an intuitive interface for users to study and improve their chess skills.

## Features

- Detailed game analysis using the Stockfish chess engine.
- Audio feedback for moves and alerts.
- Intuitive user interface for reviewing past games and exploring different game scenarios.

## Dependencies

GeminiChessAnalysis relies on several external libraries and services:

- **[Google Gemini API](https://ai.google.dev/gemini-api/docs/api-key?_gl=1*o4ctee*_up*MQ..&gclid=CjwKCAjwkJm0BhBxEiwAwT1AXEfQ0c5BQKAGt9Dpi7pfbCwEQbTkt__TiEfG6nGyTevezueKfnA3qxoCFicQAvD_BwE):** Utilized for fetching detailed chess explanations, enhancing the learning and analysis experience. This API provides insights into chess moves and strategies, making it easier for users to understand and improve their gameplay.
- **[Stockfish 16.1](https://github.com/official-stockfish/Stockfish):** A powerful open-source chess engine used for game analysis and move suggestions.
- **[Newtonsoft.Json (13.0.3)](https://www.nuget.org/packages/Newtonsoft.Json/13.0.3):** A popular high-performance JSON framework for .NET, used for data serialization and deserialization.
- **[Xam.Plugin.SimpleAudioPlayer (1.6.0)](https://www.nuget.org/packages/Xam.Plugin.SimpleAudioPlayer/1.6.0):** A simple audio player plugin for Xamarin applications, used for playing audio feedback.
- **[SkiaSharp (2.88.8)](https://www.nuget.org/packages/SkiaSharp/2.88.8):** SkiaSharp is a cross-platform 2D graphics API for .NET platforms based on Google's Skia Graphics Library. It provides a comprehensive 2D API that can be used across mobile, server and desktop models to render images.
- Chess piece images are taken from https://github.com/lichess-org/lila

## Installation

To run GeminiChessAnalysis, you will need to have Visual Studio with Xamarin.Forms support installed. Follow these steps to get started:

1. Clone the repository to your local machine: https://github.com/longpth/GeminiChessAnalysis.git
2. Open the `GeminiChessAnalysis.sln` solution file in Visual Studio.
3. Restore the NuGet packages by right-clicking on the solution and selecting "Restore NuGet Packages."
4. Set the `GeminiChessAnalysis` project as the startup project.
5. Choose the target platform (Android/iOS) and run the application.

## Usage

After launching GeminiChessAnalysis, you can:

- Analyze your chess games by inputting the moves into the application.
- Explore different game scenarios by interacting with the chessboard.
- Receive audio feedback for moves to enhance your learning experience.

## Contributing

Contributions to GeminiChessAnalysis are welcome! Please feel free to submit pull requests or open issues to suggest improvements or report bugs.

## License

GeminiChessAnalysis is released under the GPLv3.
