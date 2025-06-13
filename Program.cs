using Silk.NET.Windowing;

namespace SpaceSim
{
    class Program
    {
        static void Main(string[] args)
        {
            var window = WindowManager.CreateWindow();
            WindowManager.Run(window);
        }
    }
}