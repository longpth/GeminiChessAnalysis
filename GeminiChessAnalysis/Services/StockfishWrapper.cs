using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace GeminiChessAnalysis.Services
{
    public interface IStockfish : IDisposable
    {
        void SetInput(string input);
        string GetOutput();
        void InitMyStockfish(int argc, string[] argv);
    }

    public class StockfishWrapper : IStockfish
    {
        [DllImport("libstockfishlib.so", CallingConvention = CallingConvention.Cdecl)]
        private static extern void setInput(string input);

        [DllImport("libstockfishlib.so", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr getOutput();

        [DllImport("libstockfishlib.so", CallingConvention = CallingConvention.Cdecl)]
        private static extern void initMyStockfish(int argc, string[] argv);

        public void SetInput(string input)
        {
            setInput(input);
        }

        public string GetOutput()
        {
            IntPtr ptr = getOutput();
            return Marshal.PtrToStringAnsi(ptr);
        }

        public void InitMyStockfish(int argc, string[] argv)
        {
            initMyStockfish(argc, argv);
        }

        public void Dispose()
        {
            // Implement disposal logic if needed
        }
    }
}
