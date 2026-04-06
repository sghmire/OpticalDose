classdef FWHM_JawSize_exported < matlab.apps.AppBase

    % Properties that correspond to app components
    properties (Access = public)
        FWHMJawSizeCalculatorUIFigure  matlab.ui.Figure
        Menu                           matlab.ui.container.Menu
        Panel_2                        matlab.ui.container.Panel
        YmmEditField                   matlab.ui.control.NumericEditField
        YmmEditFieldLabel              matlab.ui.control.Label
        XmmEditField                   matlab.ui.control.NumericEditField
        XmmEditFieldLabel              matlab.ui.control.Label
        Panel                          matlab.ui.container.Panel
        LinesYEditField                matlab.ui.control.NumericEditField
        ExtentXEditField               matlab.ui.control.NumericEditField
        ExtentYEditField               matlab.ui.control.NumericEditField
        AnalysisPanel                  matlab.ui.container.Panel
        ToolsLabel                     matlab.ui.control.Label
        CalculateButton                matlab.ui.control.Button
        MethodDropDown                 matlab.ui.control.DropDown
        MethodDropDownLabel            matlab.ui.control.Label
        XXProfileAxes                  matlab.ui.control.UIAxes
        YYProfileAxes                  matlab.ui.control.UIAxes
        SmoothingPanel                 matlab.ui.container.Panel
        MethodPanel                    matlab.ui.container.Panel
        SmoothMethodDropDownLabel      matlab.ui.control.Label
        SmoothMethodDropDown           matlab.ui.control.DropDown
        SmoothStrengthEditFieldLabel   matlab.ui.control.Label
        SmoothStrengthEditField        matlab.ui.control.NumericEditField
        XStdEditField                  matlab.ui.control.NumericEditField
        XStdLabel                      matlab.ui.control.Label
        YStdEditField                  matlab.ui.control.NumericEditField
        YStdLabel                      matlab.ui.control.Label
        AutoCenterButton               matlab.ui.control.Button
    end

    
    properties (Access = private)
        MainApp;
        FilmDose;
        filmX; filmY;

    end
    
    methods (Access = public)
        
    end
    

    % Callbacks that handle component events
    methods (Access = private)

        % Code that executes after component creation
        function startupFcn(app, mainapp, FilmDose, wtbar)
            app.MainApp = mainapp;           
            app.FilmDose = FilmDose;
            
            % Handle optional waitbar
            if nargin > 3 && ~isempty(wtbar) && ishandle(wtbar)
                H = wtbar;
                waitbar(0.5, H, 'Film dose loaded...' );
                pause(0.2);
                waitbar(0.75, H, 'Loading the film profile...' );
            else
                H = [];
            end
            
            % Initial Profile Extraction (Central)
            [~, ~, app.filmX, app.filmY] = fn_AugmentedProfileExtraction(0, app.FilmDose, [0,0], [0,0]);
            
            % Initial Plotting — filmX is the Y-direction profile (column scan), filmY is X-direction (row scan)
            plot(app.YYProfileAxes, app.filmX(:, 1), app.filmX(:, 2), 'Color', [0.5 0.5 0.5], 'LineWidth', 0.5);
            xlabel(app.YYProfileAxes, 'Distance Y (mm)'); ylabel(app.YYProfileAxes, 'Dose'); grid(app.YYProfileAxes, 'on');
            title(app.YYProfileAxes, 'Y Profile (centre)');
            
            plot(app.XXProfileAxes, app.filmY(:, 1), app.filmY(:, 2), 'Color', [0.5 0.5 0.5], 'LineWidth', 0.5);
            xlabel(app.XXProfileAxes, 'Distance X (mm)'); ylabel(app.XXProfileAxes, 'Dose'); grid(app.XXProfileAxes, 'on');
            title(app.XXProfileAxes, 'X Profile (centre)');
            
            if ~isempty(H) && ishandle(H)
                close(H);
            end
        end

        % Button pushed function: CalculateButton
        function CalculateButtonPushed(app, event)
            % 1. Apply Smoothing Pre-processing
            processed_dose = app.FilmDose;
            smoothMethod = app.SmoothMethodDropDown.Value;
            if ~strcmpi(smoothMethod, 'None')
                strength = app.SmoothStrengthEditField.Value;
                processed_dose = fn_MatrixSmooth(processed_dose, smoothMethod, strength);
            end

            % 2. Calculate FWHM with Statistics
            % fn_fullWidthHalfMax returns [dis_x, dis_y, std_x, std_y]
            % UIAxes1 = YYProfileAxes (Y-profiles), UIAxes2 = XXProfileAxes (X-profiles)
            [app.XmmEditField.Value, app.YmmEditField.Value, app.XStdEditField.Value, app.YStdEditField.Value] = ...
                fn_fullWidthHalfMax(processed_dose, ...
                app.ExtentXEditField.Value, app.ExtentYEditField.Value, ...
                app.MethodDropDown.Value, app.YYProfileAxes, app.XXProfileAxes);
        end

        % Button pushed function: AutoCenterButton
        function AutoCenterButtonPushed(app, event)
            % Align the analysis window to the peak dose center
            try
                % Extract the raw image/dose for fn_AutoCenter
                % Note: fn_AutoCenter expects a 2D image matrix or TIFF
                % Since app.FilmDose is usually augmented [0 x; y d], we pass the data part
                data_part = app.FilmDose(2:end, 2:end);
                
                % Call the robust auto-center logic from the main app's toolkit
                % (Assuming fn_AutoCenter is available as confirmed)
                % In this context, we usually want to trigger the MainApp's 
                % alignment to update the shared state.
                if ~isempty(app.MainApp)
                    app.MainApp.AutoAlignImageButtonPushed(); 
                    % Re-fetch the updated dose after alignment
                    app.FilmDose = app.MainApp.Film_dose;
                    % Refresh profiles
                    startupFcn(app, app.MainApp, app.FilmDose, waitbar(0, 'Re-centering...'));
                end
            catch ME
                uialert(app.FWHMJawSizeCalculatorUIFigure, ['Auto-Center failed: ' ME.message], 'Alignment Error');
            end
        end
    end

    % Component initialization
    methods (Access = private)

        % Create UIFigure and components
        function createComponents(app)
            pathToMLAPP = fileparts(mfilename('fullpath'));

            % Figure
            app.FWHMJawSizeCalculatorUIFigure = uifigure('Visible', 'off');
            app.FWHMJawSizeCalculatorUIFigure.Position = [100 100 693 582];
            app.FWHMJawSizeCalculatorUIFigure.Name = 'FWHM JawSize Calculator';
            app.FWHMJawSizeCalculatorUIFigure.Icon = fullfile(pathToMLAPP, 'dlgapps_resources', 'icons8-jaw-66.png');

            app.Menu = uimenu(app.FWHMJawSizeCalculatorUIFigure);
            app.Menu.Text = 'Menu';

            % ---- LEFT: Profile Axes (stacked) ----
            % Top — Y-direction profiles
            app.YYProfileAxes = uiaxes(app.FWHMJawSizeCalculatorUIFigure);
            app.YYProfileAxes.Position = [11 295 447 282];
            app.YYProfileAxes.Box = 'on';
            xlabel(app.YYProfileAxes, 'Distance Y (mm)');
            ylabel(app.YYProfileAxes, 'Dose');
            title(app.YYProfileAxes, 'Y Profile (central)');
            grid(app.YYProfileAxes, 'on');

            % Bottom — X-direction profiles
            app.XXProfileAxes = uiaxes(app.FWHMJawSizeCalculatorUIFigure);
            app.XXProfileAxes.Position = [11 8 447 282];
            app.XXProfileAxes.Box = 'on';
            xlabel(app.XXProfileAxes, 'Distance X (mm)');
            ylabel(app.XXProfileAxes, 'Dose');
            title(app.XXProfileAxes, 'X Profile (central)');
            grid(app.XXProfileAxes, 'on');

            % ---- RIGHT COLUMN (x=462, w=227) ----
            % Stack (bottom→top): Buttons | Smoothing | Y Panel | X Panel | Method
            rL = 462; rW = 227;

            % [1] Action buttons — y=8, h=50
            app.CalculateButton = uibutton(app.FWHMJawSizeCalculatorUIFigure, 'push');
            app.CalculateButton.ButtonPushedFcn = createCallbackFcn(app, @CalculateButtonPushed, true);
            app.CalculateButton.Position = [rL 8 148 50];
            app.CalculateButton.BackgroundColor = [0.35 0.68 0.27];
            app.CalculateButton.FontColor = [1 1 1];
            app.CalculateButton.FontSize = 14;
            app.CalculateButton.FontWeight = 'bold';
            app.CalculateButton.Text = char(9654) + " Calculate";

            app.AutoCenterButton = uibutton(app.FWHMJawSizeCalculatorUIFigure, 'push');
            app.AutoCenterButton.ButtonPushedFcn = createCallbackFcn(app, @AutoCenterButtonPushed, true);
            app.AutoCenterButton.Position = [rL+153 8 74 50];
            app.AutoCenterButton.Text = sprintf('Auto\nCenter');

            % [2] Smoothing panel — y=63, h=92
            app.SmoothingPanel = uipanel(app.FWHMJawSizeCalculatorUIFigure);
            app.SmoothingPanel.Title = 'Smoothing';
            app.SmoothingPanel.FontWeight = 'bold';
            app.SmoothingPanel.BackgroundColor = [0.94 0.94 0.94];
            app.SmoothingPanel.Position = [rL 63 rW 92];

            app.SmoothMethodDropDownLabel = uilabel(app.SmoothingPanel);
            app.SmoothMethodDropDownLabel.Position = [5 48 58 22];
            app.SmoothMethodDropDownLabel.HorizontalAlignment = 'right';
            app.SmoothMethodDropDownLabel.Text = 'Method:';
            app.SmoothMethodDropDown = uidropdown(app.SmoothingPanel);
            app.SmoothMethodDropDown.Items = {'None','Gaussian','Average','Median','Lowess','Loess'};
            app.SmoothMethodDropDown.Value = 'None';
            app.SmoothMethodDropDown.Position = [68 48 152 22];

            app.SmoothStrengthEditFieldLabel = uilabel(app.SmoothingPanel);
            app.SmoothStrengthEditFieldLabel.Position = [5 18 58 22];
            app.SmoothStrengthEditFieldLabel.HorizontalAlignment = 'right';
            app.SmoothStrengthEditFieldLabel.Text = 'Strength:';
            app.SmoothStrengthEditField = uieditfield(app.SmoothingPanel, 'numeric');
            app.SmoothStrengthEditField.Position = [68 18 60 22];
            app.SmoothStrengthEditField.Value = 2;

            % [3] Central Analysis Region — y=390, h=110
            app.AnalysisPanel = uipanel(app.FWHMJawSizeCalculatorUIFigure);
            app.AnalysisPanel.Title = 'Central Analysis Bound';
            app.AnalysisPanel.FontWeight = 'bold';
            app.AnalysisPanel.BackgroundColor = [0.94 0.94 0.94];
            app.AnalysisPanel.Position = [rL 390 rW 110];

            lX2 = uilabel(app.AnalysisPanel); lX2.Position = [5 55 120 18];
            lX2.HorizontalAlignment = 'right'; lX2.Text = 'Longitudinal X (mm):';
            lX2.FontWeight = 'bold'; lX2.FontColor = [0 0.1 0.6];
            app.ExtentXEditField = uieditfield(app.AnalysisPanel, 'numeric');
            app.ExtentXEditField.Position = [135 55 55 18]; app.ExtentXEditField.Value = 4;

            lY2 = uilabel(app.AnalysisPanel); lY2.Position = [5 25 120 18];
            lY2.HorizontalAlignment = 'right'; lY2.Text = 'Transverse Y (mm):';
            lY2.FontWeight = 'bold'; lY2.FontColor = [0 0.4 0];
            app.ExtentYEditField = uieditfield(app.AnalysisPanel, 'numeric');
            app.ExtentYEditField.Position = [135 25 55 18]; app.ExtentYEditField.Value = 4;

            % [4] Y Profile panel — y=275, h=105, green tint
            app.Panel_2 = uipanel(app.FWHMJawSizeCalculatorUIFigure);
            app.Panel_2.Title = 'Y Profile Results';
            app.Panel_2.FontWeight = 'bold';
            app.Panel_2.BackgroundColor = [0.92 0.98 0.93];
            app.Panel_2.Position = [rL 275 rW 105];

            lY3 = uilabel(app.Panel_2); lY3.Position = [5 45 92 28];
            lY3.HorizontalAlignment = 'right'; lY3.FontWeight = 'bold'; lY3.FontSize = 12;
            lY3.Text = 'FWHM (mm):';
            app.YmmEditField = uieditfield(app.Panel_2, 'numeric');
            app.YmmEditField.Position = [100 43 120 30];
            app.YmmEditField.ValueDisplayFormat = '%.3f';
            app.YmmEditField.FontSize = 14; app.YmmEditField.FontWeight = 'bold';
            app.YmmEditField.BackgroundColor = [0.74 0.92 0.99];

            app.YStdLabel = uilabel(app.Panel_2);
            app.YStdLabel.Position = [5 15 92 20];
            app.YStdLabel.HorizontalAlignment = 'right'; app.YStdLabel.Text = [char(177), ' SD (mm):'];
            app.YStdEditField = uieditfield(app.Panel_2, 'numeric');
            app.YStdEditField.Position = [100 15 120 20];
            app.YStdEditField.ValueDisplayFormat = '%.4f';
            app.YStdEditField.BackgroundColor = [0.94 0.94 0.94];
            app.YStdEditField.Editable = 'off';

            % [5] X Profile panel — y=160, h=105, blue tint
            app.Panel = uipanel(app.FWHMJawSizeCalculatorUIFigure);
            app.Panel.Title = 'X Profile Results';
            app.Panel.FontWeight = 'bold';
            app.Panel.BackgroundColor = [0.92 0.93 0.99];
            app.Panel.Position = [rL 160 rW 105];

            lX3 = uilabel(app.Panel); lX3.Position = [5 45 92 28];
            lX3.HorizontalAlignment = 'right'; lX3.FontWeight = 'bold'; lX3.FontSize = 12;
            lX3.Text = 'FWHM (mm):';
            app.XmmEditField = uieditfield(app.Panel, 'numeric');
            app.XmmEditField.Position = [100 48 120 30];
            app.XmmEditField.ValueDisplayFormat = '%.3f';
            app.XmmEditField.FontSize = 14; app.XmmEditField.FontWeight = 'bold';
            app.XmmEditField.BackgroundColor = [0.74 0.92 0.99];

            app.XStdLabel = uilabel(app.Panel);
            app.XStdLabel.Position = [5 15 92 20];
            app.XStdLabel.HorizontalAlignment = 'right'; app.XStdLabel.Text = [char(177), ' SD (mm):'];
            app.XStdEditField = uieditfield(app.Panel, 'numeric');
            app.XStdEditField.Position = [100 15 120 20];
            app.XStdEditField.ValueDisplayFormat = '%.4f';
            app.XStdEditField.BackgroundColor = [0.94 0.94 0.94];
            app.XStdEditField.Editable = 'off';

            % [6] Method panel — y=510, h=60 (top)
            app.MethodPanel = uipanel(app.FWHMJawSizeCalculatorUIFigure);
            app.MethodPanel.Title = 'Peak Estimation';
            app.MethodPanel.FontWeight = 'bold';
            app.MethodPanel.BackgroundColor = [0.94 0.94 0.94];
            app.MethodPanel.Position = [rL 510 rW 60];

            app.MethodDropDownLabel = uilabel(app.MethodPanel);
            app.MethodDropDownLabel.Position = [5 9 70 22];
            app.MethodDropDownLabel.HorizontalAlignment = 'right';
            app.MethodDropDownLabel.FontWeight = 'bold';
            app.MethodDropDownLabel.Text = 'Plateau est.:';
            app.MethodDropDown = uidropdown(app.MethodPanel);
            app.MethodDropDown.Items = {'Maximum', 'Mean', 'Median'};
            app.MethodDropDown.Value = 'Mean';
            app.MethodDropDown.Position = [80 9 140 22];

            % ToolsLabel kept for property compat — hidden
            app.ToolsLabel = uilabel(app.FWHMJawSizeCalculatorUIFigure);
            app.ToolsLabel.Position = [1 1 1 1];
            app.ToolsLabel.Visible = 'off';
            app.ToolsLabel.Text = '';

            % Show figure
            app.FWHMJawSizeCalculatorUIFigure.Visible = 'on';

        end
    end

    % App creation and deletion
    methods (Access = public)

        % Construct app
        function app = FWHM_JawSize_exported(varargin)

            % Create UIFigure and components
            createComponents(app)

            % Register the app with App Designer
            registerApp(app, app.FWHMJawSizeCalculatorUIFigure)

            % Execute the startup function
            runStartupFcn(app, @(app)startupFcn(app, varargin{:}))

            if nargout == 0
                clear app
            end
        end

        % Code that executes before app deletion
        function delete(app)

            % Delete UIFigure when app is deleted
            delete(app.FWHMJawSizeCalculatorUIFigure)
        end
    end
end
