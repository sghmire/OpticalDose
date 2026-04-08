using System;
using System.Windows.Input;

namespace FilmAnalysis
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute();
        public void Execute(object? parameter) => _execute();
    }

    public class FilmRelayCommand : ICommand
    {
        private readonly Action? _execute;
        private readonly Action<object?>? _executeWithParam;
        public FilmRelayCommand(Action execute) { _execute = execute; }
        public FilmRelayCommand(Action<object?> executeWithParam) { _executeWithParam = executeWithParam; }
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter)
        {
            if (_execute != null) _execute();
            else _executeWithParam?.Invoke(parameter);
        }
    }

    public class AppSettings
    {
        public int FixedWidth { get; set; } = 100;
        public int FixedHeight { get; set; } = 100;
        public bool LastWasFixed { get; set; } = false;
        public string CalibrationsPath { get; set; } = string.Empty;
        public double LastPlateauX { get; set; } = 20;
        public double LastPlateauY { get; set; } = 20;
        public string LastJawMethod { get; set; } = "Maximum";

        // Advanced Gamma Engine Settings
        public double GammaUncertainty { get; set; } = 2.0;
        public double GammaSearchStep { get; set; } = 0.1;
        public double GammaSmoothingSigma { get; set; } = 0.0;
        public bool GammaUseBicubic { get; set; } = true;
    }

    public class CalibrationPoint
    {
        public double Dose { get; set; }
        public double Red { get; set; }
        public double Green { get; set; }
        public double Blue { get; set; }
    }

    internal class ImageState
    {
        public double[,] Red, Green, Blue;
        public double[,] DoseMap;
        public int Width, Height;
        public double DpiX, DpiY;
        public bool ShowingDose;
        public string Description;
    }
}
