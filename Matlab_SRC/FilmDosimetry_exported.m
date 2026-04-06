classdef FilmDosimetry_exported < matlab.apps.AppBase

    % Properties that correspond to app components
    properties (Access = public)
        FilmDosimetryUIFigure       matlab.ui.Figure
        FigureGrid                  matlab.ui.container.GridLayout
        CalibrationGrid             matlab.ui.container.GridLayout
        CenterPanelGrid             matlab.ui.container.GridLayout
        RightPanelGrid              matlab.ui.container.GridLayout
        DicomGrid                   matlab.ui.container.GridLayout
        AnalysisGrid                matlab.ui.container.GridLayout
        FileMenu                    matlab.ui.container.Menu
        ImportMenu                  matlab.ui.container.Menu
        CalibrationImportMenu_2     matlab.ui.container.Menu
        FilmtifMenu_2               matlab.ui.container.Menu
        DosetxtMenu_2               matlab.ui.container.Menu
        DicomImportMenu_2           matlab.ui.container.Menu
        RDDosedcmMenu               matlab.ui.container.Menu
        AnalysisImportMenu          matlab.ui.container.Menu
        PlanDosedcmMenu             matlab.ui.container.Menu
        FilmDosetxtMenu             matlab.ui.container.Menu
        ExportMenu                  matlab.ui.container.Menu
        CalibrationExportMenu       matlab.ui.container.Menu
        Film2DosetxtMenu            matlab.ui.container.Menu
        XYProfiletxtMenu            matlab.ui.container.Menu
        DicomExportMenu             matlab.ui.container.Menu
        PlaneDosedcmMenu            matlab.ui.container.Menu
        RotatedDicomDosedcomMenu    matlab.ui.container.Menu
        AnalysisExportMenu          matlab.ui.container.Menu
        CurrentFilmDosetxtMenu      matlab.ui.container.Menu
        ToolsMenu                   matlab.ui.container.Menu
        DicomDummyMenu              matlab.ui.container.Menu
        SynchonicityMenu            matlab.ui.container.Menu
        StarShotsMenu               matlab.ui.container.Menu
        JawSizeMenu                 matlab.ui.container.Menu
        CalibrationToolsMenu        matlab.ui.container.Menu
        OpenCalibrationToolsMenu    matlab.ui.container.Menu
        TabGroup                    matlab.ui.container.TabGroup
        CalibrationTab              matlab.ui.container.Tab
        Panel_14                    matlab.ui.container.Panel
        Panel_37                    matlab.ui.container.Panel
        NotReadyLabel               matlab.ui.control.Label
        StatusLabel                 matlab.ui.control.Label
        CustomConfigDropDown        matlab.ui.control.DropDown
        CustomConfigDropDownLabel   matlab.ui.control.Label
        RefreshListButton           matlab.ui.control.Button
        TextArea                    matlab.ui.control.TextArea
        CalbrationInfoLabel         matlab.ui.control.Label
        ProfileToolsLabel           matlab.ui.control.Label
        ButtonGroup_2               matlab.ui.container.ButtonGroup
        FilmProfileButton           matlab.ui.control.Button
        ManualButton                matlab.ui.control.RadioButton
        CenterButton                matlab.ui.control.RadioButton
        NormalizeCheckBox           matlab.ui.control.CheckBox
        UIAxes9                     matlab.ui.control.UIAxes
        UIAxes9_2                   matlab.ui.control.UIAxes
        Panel_20                    matlab.ui.container.Panel
        CalibrationFigure           matlab.ui.control.UIAxes
        Panel_28                    matlab.ui.container.Panel
        ContrastButton              matlab.ui.control.Button
        ContrastSlider              matlab.ui.control.RangeSlider
        ContrastSliderLabel         matlab.ui.control.Label
        Panel_27                    matlab.ui.container.Panel
        ClearXButton                matlab.ui.control.Button
        cGyEditFieldLabel           matlab.ui.control.Label
        cGyEditField                matlab.ui.control.NumericEditField
        AreaButton                  matlab.ui.control.Button
        DistanceButton              matlab.ui.control.Button
        ROIDoseButton               matlab.ui.control.Button
        MeasurementLabel            matlab.ui.control.Label
        Panel_29                    matlab.ui.container.Panel
        UpdateFilmDoseButton        matlab.ui.control.Button
        ConverttodoseButton         matlab.ui.control.Button
        PickFilmCenterButton_2      matlab.ui.control.Button
        ManuallyAlignButton         matlab.ui.control.Button
        AutoCenterButton            matlab.ui.control.Button
        AutoAlignImageButton        matlab.ui.control.Button
        Film2DoseLabel              matlab.ui.control.Label
        TransformationLabel         matlab.ui.control.Label
        Panel_30                    matlab.ui.container.Panel
        FlipVButton                 matlab.ui.control.Button
        FilpHButton                 matlab.ui.control.Button
        Panel_26                    matlab.ui.container.Panel
        ManualCropButton            matlab.ui.control.Button
        HeightCC                    matlab.ui.control.NumericEditField
        HeightLabel                 matlab.ui.control.Label
        CenterCropButton            matlab.ui.control.Button
        WidthCC                     matlab.ui.control.NumericEditField
        WidthLabel                  matlab.ui.control.Label
        CCWButton                   matlab.ui.control.Button
        CWButton                    matlab.ui.control.Button
        Panel_31                    matlab.ui.container.Panel
        Panel_22                    matlab.ui.container.Panel
        Panel_2                     matlab.ui.container.Panel
        SizeEditField               matlab.ui.control.NumericEditField
        ROIFilterButton             matlab.ui.control.Button
        ROILabel                    matlab.ui.control.Label
        MedianLabel                 matlab.ui.control.Label
        MedianFilterButton          matlab.ui.control.Button
        MedianSizeEditField         matlab.ui.control.NumericEditField
        MedianSizeLabel             matlab.ui.control.Label
        NoiseLabel                  matlab.ui.control.Label
        FilterNoiseButton           matlab.ui.control.Button
        FilterNoiseEditField        matlab.ui.control.NumericEditField
        NoiseThresholdLabel         matlab.ui.control.Label
        SmoothLabel                 matlab.ui.control.Label
        SmoothButton                matlab.ui.control.Button
        SmoothDropDown_2            matlab.ui.control.DropDown
        SmoothWIn_2                 matlab.ui.control.NumericEditField
        InterpLabel                 matlab.ui.control.Label
        InterpolationButton         matlab.ui.control.Button
        DropDown_7                  matlab.ui.control.DropDown
        EditField_5                 matlab.ui.control.NumericEditField
        FiltersLabel                matlab.ui.control.Label
        Panel_32                    matlab.ui.container.Panel
        ColorMapDropDown_3          matlab.ui.control.DropDown
        ColorMapDropDown_3Label     matlab.ui.control.Label
        DicomDoseViewerTab          matlab.ui.container.Tab
        RotatedDoseAlignmentPanel   matlab.ui.container.Panel
        RotatedPlane                matlab.ui.control.DropDown
        OGPlane                     matlab.ui.control.DropDown
        vButton                     matlab.ui.control.Button
        Label_12                    matlab.ui.control.Label
        Label_10                    matlab.ui.control.Label
        Button_11                   matlab.ui.control.Button
        StepsizepixelEditField      matlab.ui.control.NumericEditField
        Button_10                   matlab.ui.control.Button
        Button_12                   matlab.ui.control.Button
        Label_11                    matlab.ui.control.Label
        Label_9                     matlab.ui.control.Label
        Panel_7                     matlab.ui.container.Panel
        RotateButton                matlab.ui.control.Button
        YawEditField                matlab.ui.control.NumericEditField
        YawEditFieldLabel           matlab.ui.control.Label
        RollEditField               matlab.ui.control.NumericEditField
        RollEditFieldLabel          matlab.ui.control.Label
        PitchEditField              matlab.ui.control.NumericEditField
        PitchEditFieldLabel         matlab.ui.control.Label
        RotatedFigure               matlab.ui.control.UIAxes
        OriginalFigure              matlab.ui.control.UIAxes
        DosePlaneCalculatorPanel    matlab.ui.container.Panel
        Panel_4                     matlab.ui.container.Panel
        SendPlaneforAnalysisButton  matlab.ui.control.Button
        DropDown_3                  matlab.ui.control.DropDown
        UpdateLabel                 matlab.ui.control.Label
        CalculateButton             matlab.ui.control.Button
        EditField_11                matlab.ui.control.NumericEditField
        Label_22                    matlab.ui.control.Label
        ZmmEditField                matlab.ui.control.NumericEditField
        Label_21                    matlab.ui.control.Label
        EditField_10                matlab.ui.control.NumericEditField
        ZmmEditFieldLabel           matlab.ui.control.Label
        EditField_9                 matlab.ui.control.NumericEditField
        Label_20                    matlab.ui.control.Label
        YmmEditField                matlab.ui.control.NumericEditField
        Label_19                    matlab.ui.control.Label
        EditField_8                 matlab.ui.control.NumericEditField
        YmmEditFieldLabel           matlab.ui.control.Label
        Panel_5                     matlab.ui.container.Panel
        FrameEditField              matlab.ui.control.NumericEditField
        FrameEditFieldLabel         matlab.ui.control.Label
        RowEditField                matlab.ui.control.NumericEditField
        RowEditFieldLabel           matlab.ui.control.Label
        ColumnEditField             matlab.ui.control.NumericEditField
        ColumnEditFieldLabel        matlab.ui.control.Label
        EditField_7                 matlab.ui.control.NumericEditField
        Label_18                    matlab.ui.control.Label
        XmmEditField                matlab.ui.control.NumericEditField
        Label_17                    matlab.ui.control.Label
        EditField_6                 matlab.ui.control.NumericEditField
        XmmEditFieldLabel           matlab.ui.control.Label
        SystemLabel                 matlab.ui.control.Label
        Panel_13                    matlab.ui.container.Panel
        Button_7                    matlab.ui.control.Button
        XYSlider                    matlab.ui.control.Slider
        Button_4                    matlab.ui.control.Button
        GyEditFieldLabel            matlab.ui.control.Label
        GyEditField                 matlab.ui.control.NumericEditField
        XYFigure                    matlab.ui.control.UIAxes
        Panel_12                    matlab.ui.container.Panel
        Button_6                    matlab.ui.control.Button
        XZSlider                    matlab.ui.control.Slider
        Button_3                    matlab.ui.control.Button
        GyEditField_2Label          matlab.ui.control.Label
        GyEditField_2               matlab.ui.control.NumericEditField
        XZFigure                    matlab.ui.control.UIAxes
        Panel_11                    matlab.ui.container.Panel
        Button_5                    matlab.ui.control.Button
        YZSlider                    matlab.ui.control.Slider
        Button_2                    matlab.ui.control.Button
        GyEditField_3Label          matlab.ui.control.Label
        GyEditField_3               matlab.ui.control.NumericEditField
        YZFigure                    matlab.ui.control.UIAxes
        Panel_33                    matlab.ui.container.Panel
        Button_9                    matlab.ui.control.Button
        MRNEditField_2              matlab.ui.control.EditField
        MRNLabel_2                  matlab.ui.control.Label
        MaxPlaneDoseCheckBox        matlab.ui.control.CheckBox
        ColorMapDropDown_2          matlab.ui.control.DropDown
        ColorMapDropDown_2Label     matlab.ui.control.Label
        AnalysisTab                 matlab.ui.container.Tab
        Panel_19                    matlab.ui.container.Panel
        ResultPanel                 matlab.ui.container.Panel
        EditField_4                 matlab.ui.control.NumericEditField
        Label_13                    matlab.ui.control.Label
        DoseScaledEditField         matlab.ui.control.NumericEditField
        DoseScaledLabel             matlab.ui.control.Label
        EditField_3                 matlab.ui.control.NumericEditField
        EditField2                  matlab.ui.control.NumericEditField
        YShiftsmmEditField          matlab.ui.control.NumericEditField
        YShiftsmmEditFieldLabel     matlab.ui.control.Label
        DDDTALabel                  matlab.ui.control.Label
        XShiftsmmEditField          matlab.ui.control.NumericEditField
        XShiftsmmEditFieldLabel     matlab.ui.control.Label
        GammaToolsPanel             matlab.ui.container.Panel
        GammaButton                 matlab.ui.control.Button
        DropDown_8                  matlab.ui.control.DropDown
        SingalEditField             matlab.ui.control.NumericEditField
        SingalLabel                 matlab.ui.control.Label
        DTAmmEditField              matlab.ui.control.NumericEditField
        DTAmmLabel                  matlab.ui.control.Label
        DDEditField                 matlab.ui.control.NumericEditField
        DDLabel                     matlab.ui.control.Label
        UIAxes6                     matlab.ui.control.UIAxes
        Panel_18                    matlab.ui.container.Panel
        Panel_36                    matlab.ui.container.Panel
        ClearXButton_2              matlab.ui.control.Button
        FxEditField                 matlab.ui.control.NumericEditField
        FxLabel                     matlab.ui.control.Label
        OffcenterProfileButton      matlab.ui.control.Button
        CenterProfileButton         matlab.ui.control.Button
        ScaleFilmDoseButton         matlab.ui.control.Button
        ScaleVal                    matlab.ui.control.NumericEditField
        RightButton_2               matlab.ui.control.Button
        DownButton_2                matlab.ui.control.Button
        LeftButton_2                matlab.ui.control.Button
        StepSizeLabel               matlab.ui.control.Label
        StepsizepixelEditField_2    matlab.ui.control.NumericEditField
        PerformGammaCheckBox        matlab.ui.control.CheckBox
        UpButton_2                  matlab.ui.control.Button
        UIAxes3                     matlab.ui.control.UIAxes
        UIAxes2                     matlab.ui.control.UIAxes
        Label_15                    matlab.ui.control.Label
        Label_16                    matlab.ui.control.Label
        Panel_17                    matlab.ui.container.Panel
        Slider_2                    matlab.ui.control.RangeSlider
        FigFilmDose                 matlab.ui.control.UIAxes
        Panel_16                    matlab.ui.container.Panel
        Slider                      matlab.ui.control.RangeSlider
        FigDicomDose                matlab.ui.control.UIAxes
        Panel_34                    matlab.ui.container.Panel
        Button_21                   matlab.ui.control.Button
        AllFileName                 matlab.ui.control.EditField
        PlanLabel                   matlab.ui.control.Label
        MRNEditField                matlab.ui.control.EditField
        MRNLabel                    matlab.ui.control.Label
        ColorMapDropDown            matlab.ui.control.DropDown
        ColorMapDropDownLabel       matlab.ui.control.Label
        AboutTab                    matlab.ui.container.Tab
        v12Label_4                  matlab.ui.control.Label
        Label_23                    matlab.ui.control.Label
        SGRGHLabel                  matlab.ui.control.Label
        FilmDosimetryLabel          matlab.ui.control.Label
    end


    properties (Access = private)
        calib_film                                                        
        TiffDPI                                                            
        first_fit; second_fit;third_fit; Fit_type; delta                    
        Film_dose; TPS_dose;                                               
        ImagePP = []; Grid_scaling = 1; SliceThickness = 1;                 
        PixelSpacing_Y = 1; PixelSpacing_X = 1;                            
        ImageOrent; NumRows = 512; NumColumns = 512;NumFrames = 250;        
        DicomInfo; PlaneInfo;                                              
        window_size = 1;      
        DicomVolume ; RotateDicomVolume = []; saveVolume;  PlanPushed;
        slider_current1 = 1;slider_current2 = 1;slider_current3 = 1;
        yzPlane;xyPlane;xzPlane;
        fx = 1; 
        X_dicom_cm; Y_dicom_cm; Z_dicom_cm;x_pro, y_pro;
        new_center;
        Xshift_counter = 0; Yshift_counter = 0;
        Path;
        VolRotStatus = 'false';
        gammamap;
        combinedX; combinedY;
        DialogApp;
        Post_pitch = 0;Post_yaw = 0; Post_roll = 0;
        ColMap = 'bone';
        TPSInterpolation; FilmInterpolation;     
        OriginalFilm;
        ProjectRoot;
    end

    properties (Access = public)
        CurrentPointGraphic; % Description
    end

    methods (Access = private)

        function [ X_pos, Y_pos] = ProfilePoint(~, UIAxes)
                % Create the crosshair on the UIAxes4
                h = drawcrosshair(UIAxes, 'LineWidth', 0.5, 'Color', 'Red');
                
                % Add listener to update position when the crosshair is moved
                addlistener(h, 'MovingROI', @(src, evt) updateCrosshairPosition(app, src));
                
                % Get the initial position of the crosshair intersection
                Position = h.Position;
                X_pos = Position(1);
                Y_pos = Position(2);
            
                % Nested function to update the crosshair position
                function updateCrosshairPosition(~, crosshair)
                    Position = crosshair.Position;
                end
        end

        function TPSPlot(~, UIAxes, xy_dis, xyProfile)
            plot(UIAxes,xy_dis,  xyProfile, 'r-');

        end

        function FilmPlot(~, UIAxes1, xy_dis, xyProfile1)
            plot(UIAxes1,xy_dis, xyProfile1, 'b-');

        end

        function GraphTickMode(~, UIAx, Title)
            UIAx.Title.String = Title;
            UIAx.XTickMode = 'auto';
            UIAx.YTickMode = 'auto';
            UIAx.XTickLabelMode = 'auto';
            UIAx.YTickLabelMode = 'auto';
            axis(UIAx, 'on');
            UIAx.Box = 'on';
            UIAx.XLimitMethod= 'tight';
            UIAx.YLimitMethod= 'tight';
        end

        % Function to delete text label
        function deleteText(~, src)
            delete(src); % Delete the text label
        end        
    end

    methods (Access = public)
        
        function updateFits(app, one_fit, two_fit, three_fit, opt_delta, channel, fittype, degree, fitname)
            if strcmp(fittype, 'Red') || strcmp(fittype, 'Green') || strcmp(fittype, 'Blue')
                app.first_fit = one_fit;
            elseif strcmp(fittype, 'Red/Blue') || strcmp(fittype, 'Green/Blue') || strcmp(fittype, 'Red | Green | Blue')
                app.first_fit = one_fit;
                app.second_fit = two_fit;
                app.third_fit = three_fit;
            end 

            formatted_text = sprintf('Channel:          %s | %s\nDegree of fit:    %d\nCalibration file: %s',  channel, fittype, degree, fitname);
            app.TextArea.Value = formatted_text;
            app.NotReadyLabel.Text = 'Ready!';
            app.NotReadyLabel.BackgroundColor = [0 1 0]; % Bright Green
            app.Fit_type = fittype;
            app.delta = opt_delta;
        end

        % Value changed function: CustomConfigDropDown
        function CustomConfigDropDownValueChanged(app, event)
            fileName = app.CustomConfigDropDown.Value;
            if strcmp(fileName, '--- Select ---') || isempty(fileName)
                return;
            end
            
            % Load custom config
            filePath = fullfile(app.ProjectRoot, 'configs', fileName);
            app.loadSelectedConfig(filePath, fileName);
        end

        % Button pushed function: RefreshListButton
        function RefreshListButtonPushed(app, event)
            app.scanConfigs();
        end

        function loadSelectedConfig(app, filePath, label)
            if ~isfile(filePath)
                msgbox(sprintf('File not found: %s', filePath), 'Error', 'error');
                return;
            end
            
            try
                [channel, fittype, degree, first_fit, second_fit, third_fit, delta_opt] = fn_readCalFile(filePath);
                app.updateFits(first_fit, second_fit, third_fit, delta_opt, channel, fittype, degree, label);
            catch ME
                msgbox(sprintf('Failed to load %s:\n%s', label, ME.message), 'Load Error', 'error');
            end
        end

        function scanConfigs(app)
            configPath = fullfile(app.ProjectRoot, 'configs');
            if ~isfolder(configPath)
                mkdir(configPath);
            end
            
            % List all .txt files
            files = dir(fullfile(configPath, '*.txt'));
            fileList = {files.name};
            
            % Exclude internal system configs from customs
            sysFiles = {'CalibConfig_Active.txt', 'CalibConfig_Default.txt'};
            customFiles = setdiff(fileList, sysFiles);
            
            if isempty(customFiles)
                app.CustomConfigDropDown.Items = {'--- No Custom Configs ---'};
                app.CustomConfigDropDown.Value = '--- No Custom Configs ---';
            else
                app.CustomConfigDropDown.Items = [{'--- Select ---'}, customFiles];
                app.CustomConfigDropDown.Value = '--- Select ---';
            end
        end
    end


    % Callbacks that handle component events
    methods (Access = private)

        % Code that executes after component creation
        function startupFcn(app)
            % clc; clearvars; close all; % Removed: this deletes the 'app' handle
            
            % Initialize ProjectRoot based on the location of this file
            app.ProjectRoot = fileparts(mfilename('fullpath'));
            
            % --- ROBUST PATH MANAGEMENT ---
            % To avoid shadowing issues where MATLAB loads older versions of 
            % dlgapps from backup folders, we explicitly manage the path.
            
            % 1. Identify and remove any project-related backup folders from current path
            currPath = path;
            pathCells = strsplit(currPath, pathsep);
            isBackup = ~cellfun(@isempty, strfind(pathCells, 'backup')) & ...
                       ~cellfun(@isempty, strfind(pathCells, app.ProjectRoot));
            if any(isBackup)
                rmpath(pathCells{isBackup});
            end

            % 2. Add essential subfolders to the BEGINNING of the MATLAB path
            addpath(char(app.ProjectRoot), '-begin');
            addpath(fullfile(char(app.ProjectRoot), 'configs'), '-begin');
            addpath(fullfile(char(app.ProjectRoot), 'dlgapps'), '-begin');
            addpath(fullfile(char(app.ProjectRoot), 'functions'), '-begin');
            addpath(fullfile(char(app.ProjectRoot), 'ml_resources'), '-begin');
            rehash path; 

            % Populate dropdowns
            app.scanConfigs();

            % --- AUTO-LOAD ACTIVE CALIBRATION CONFIG ---
            currentPath = fullfile(app.ProjectRoot, 'configs', 'CalibConfig_Active.txt');
            if isfile(currentPath)
                try
                    rehash; 
                    [channel, fittype, degree, first_fit, second_fit, third_fit, delta_opt] = fn_readCalFile(currentPath);
                    app.updateFits(first_fit, second_fit, third_fit, delta_opt, channel, fittype, degree, 'Active Config');
                    app.TextArea.Value = sprintf('Auto-loaded active calibration:\n%s', currentPath);
                catch ME
                    disp(['Auto-load failed: ' ME.message]);
                end
            end
        end



        % Button pushed function: CWButton
        function CWButtonPushed(app, event)
            app.calib_film = imrotate(app.calib_film, 90);
            
            % Update spatial center after rotation
            Img_size = size(app.calib_film);
            app.new_center = [(Img_size(2)+1)/2, (Img_size(1)+1)/2];
            fn_mainImageDisplay(app.calib_film, app.CalibrationFigure, app.ColMap, app.TiffDPI, app.new_center);

            app.HeightCC.Value = Img_size(1);
            app.WidthCC.Value = Img_size(2);
        end

        % Button pushed function: CCWButton
        function CCWButtonPushed(app, event)
            app.calib_film = imrotate(app.calib_film, 270);
            
            % Update spatial center after rotation
            Img_size = size(app.calib_film);
            app.new_center = [(Img_size(2)+1)/2, (Img_size(1)+1)/2];
            fn_mainImageDisplay(app.calib_film, app.CalibrationFigure, app.ColMap, app.TiffDPI, app.new_center);

            app.HeightCC.Value = Img_size(1);
            app.WidthCC.Value = Img_size(2);
        end

        % Button pushed function: FilpHButton
        function FilpHButtonPushed(app, event)
            app.calib_film = fliplr(app.calib_film);
            fn_mainImageDisplay(app.calib_film, app.CalibrationFigure, app.ColMap, app.TiffDPI, app.new_center);
        end

        % Button pushed function: FlipVButton
        function FlipVButtonPushed(app, event)
            app.calib_film = flipud(app.calib_film);
            fn_mainImageDisplay(app.calib_film, app.CalibrationFigure, app.ColMap, app.TiffDPI, app.new_center);
        end

        % Button pushed function: ManuallyAlignButton
        function ManuallyAlignButtonPushed(app, event)
            % 1. Prompt for Left Marker
            response = questdlg('Please select the LEFT marker!', 'Marker Selection', 'OK', 'Cancel', 'OK');
            if strcmp(response, 'Cancel') || isempty(response), return; end
            p1 = drawcrosshair(app.CalibrationFigure, 'LineWidth', 0.5, 'Color', 'Red');
            L = p1.Position;
            hold(app.CalibrationFigure, 'on');
            plot(app.CalibrationFigure, L(1), L(2), 'r+', 'MarkerSize', 10, 'LineWidth', 1.5, 'HandleVisibility', 'off');

            % 2. Prompt for Right Marker
            response = questdlg('Please select the RIGHT marker!', 'Marker Selection', 'OK', 'Cancel', 'OK');
            if strcmp(response, 'Cancel') || isempty(response), return; end
            p2 = drawcrosshair(app.CalibrationFigure, 'LineWidth', 0.5, 'Color', 'Blue');
            R = p2.Position;
            plot(app.CalibrationFigure, R(1), R(2), 'b+', 'MarkerSize', 10, 'LineWidth', 1.5, 'HandleVisibility', 'off');

            % 3. Prompt for Top Marker
            response = questdlg('Please select the TOP marker!', 'Marker Selection', 'OK', 'Cancel', 'OK');
            if strcmp(response, 'Cancel') || isempty(response), return; end
            p3 = drawcrosshair(app.CalibrationFigure, 'LineWidth', 0.5, 'Color', 'Green');
            T = p3.Position;
            plot(app.CalibrationFigure, T(1), T(2), 'g+', 'MarkerSize', 10, 'LineWidth', 1.5, 'HandleVisibility', 'off');

            % --- MATHEMATICAL ALIGNMENT (3-Point Physics Logic) ---
            % Current angle of the film (Roll)
            angle = atan2(R(2) - L(2), R(1) - L(1)) * (180 / pi);
            
            % Calculate the intersection point (Isocenter)
            % This is the X of the Top marker and the Y of the segment connecting Left/Right
            isoX = T(1);
            if R(1) ~= L(1)
                slope = (R(2) - L(2)) / (R(1) - L(1));
                isoY = L(2) + slope * (T(1) - L(1));
            else
                isoY = (L(2) + R(2)) / 2;
            end
            
            % Show the calculated Isocenter to the user
            plot(app.CalibrationFigure, isoX, isoY, 'yx', 'MarkerSize', 14, 'LineWidth', 2, 'HandleVisibility', 'off');
            drawnow;
            pause(0.5); % Brief pause so the user can see the yellow 'x'

            % Perform the Combined Transformation (Rotation + Centering)
            [rows, cols, ~] = size(app.calib_film);
            
            % 1. Define transformation matrices for Rotation + Centering
            % Translate I to Origin
            T1 = [1 0 0; 0 1 0; -isoX -isoY 1];
            % Rotate to 0.0
            theta_rad = -angle * pi / 180;
            R_mat = [cos(theta_rad) sin(theta_rad) 0; -sin(theta_rad) cos(theta_rad) 0; 0 0 1];
            
            % To calculate the tightest possible final canvas, 
            % we first determine how far the corners of the original image map from the origin
            corners = [1, 1, 1; cols, 1, 1; 1, rows, 1; cols, rows, 1];
            transCorners = corners * (T1 * R_mat);
            
            % The required canvas size to avoid clipping is twice the max distance from origin
            maxH = max(abs(transCorners(:,1)));
            maxV = max(abs(transCorners(:,2)));
            
            % Total width and height (with a 2% buffer for precision)
            newW = ceil(2 * maxH * 1.02);
            newH = ceil(2 * maxV * 1.02);
            newCenter = [(newW+1)/2, (newH+1)/2];

            % 3. Translate Origin to the middle of our new tight canvas
            T2 = [1 0 0; 0 1 0; newCenter(1) newCenter(2) 1];
            
            tform = affine2d(T1 * R_mat * T2);
            outputView = imref2d([newH, newW]);

            % Execute Transformation
            app.calib_film = imwarp(app.calib_film, tform, 'OutputView', outputView);
            
            % Clean up and display (using mm axes centered on isocenter)
            delete(allchild(app.CalibrationFigure)); % Remove all old markers and previous image
            hold(app.CalibrationFigure, 'off');
            app.new_center = newCenter; % Persist the clinical center for dose conversion
            fn_mainImageDisplay(app.calib_film, app.CalibrationFigure, app.ColMap, app.TiffDPI, app.new_center);

            % Set HeightCC and WidthCC to a standard clinical crop size (e.g., 150mm) 
            % to make it easy for the user to "un-pad"
            if ~isempty(app.TiffDPI)
                standard_px = round(150 * app.TiffDPI / 25.4); 
                app.HeightCC.Value = min(standard_px, newH);
                app.WidthCC.Value = min(standard_px, newW);
            else
                app.HeightCC.Value = rows; % Default back to original height if no DPI
                app.WidthCC.Value = cols;
            end
        end

        % Button pushed function: AutoAlignImageButton
        function AutoAlignImageButtonPushed(app, event)
            app.calib_film = fn_AutoImageAlign(app.calib_film);
            fn_mainImageDisplay(app.calib_film, app.CalibrationFigure, app.ColMap);

            new_size = size(app.calib_film);
            app.HeightCC.Value = new_size(1);
            app.WidthCC.Value = new_size(2);
        end

        % Button pushed function: ConverttodoseButton
        function ConverttodoseButtonPushed(app, event)
            if strcmp(app.NotReadyLabel.Text, 'Not Ready!')
                msgbox('No Calibration files loaded!');
                return;
            end
            
             Red_channel = double(app.calib_film(:,:,1));
             Red_norm= -log10(Red_channel./ 65535 + eps);
             Green_channel = double(app.calib_film(:,:,2));
             Green_norm = -log10(Green_channel ./ 65535 + eps);
             Blue_channel = double(app.calib_film(:,:,3));
             Blue_norm = -log10(Blue_channel ./ 65535 + eps);

            switch app.Fit_type
                case 'Red'
                   app.calib_film = polyval(app.first_fit,  Red_norm);
                case 'Green'
                   app.calib_film = polyval(app.first_fit,  Green_norm);
                case 'Blue'
                    app.calib_film = polyval(app.first_fit,  Blue_norm);
                case 'Red/Blue'
                    Red_blue_corr = Red_norm ./ Blue_norm;
                    first_film = polyval(app.first_fit,  Red_blue_corr);
                    app.calib_film = polyval(app.second_fit, first_film);
                case 'Green/Blue'
                    Green_blue_corr = Green_norm ./ Blue_norm;
                    first_film = polyval(app.first_fit,  Green_blue_corr);
                    app.calib_film = polyval(app.second_fit, first_film);
                case 'Red | Green | Blue'
                    Red_dose = polyval(app.first_fit, Red_norm);
                    Green_dose = polyval(app.second_fit, Green_norm );
                    Blue_dose = polyval(app.third_fit, Blue_norm);
                    final_dose = cat(3, Red_dose, Green_dose, Blue_dose);
                    final_dose = mean(final_dose, 3);

                    app.calib_film = final_dose  .* app.delta;
                    
            end 
            
            app.FilmInterpolation = app.EditField_5.Value;
            % Maintain spatial coordinates (mm axes) after dose conversion
            fn_mainImageDisplay((app.calib_film), app.CalibrationFigure, app.ColMap, app.TiffDPI, app.new_center);   
        end

        % Button pushed function: ROIDoseButton
        function ROIDoseButtonPushed(app, event)
            ROI = drawrectangle(app.CalibrationFigure, 'LineWidth', 0.5);
            X1 = round(ROI.Position(1));
            Y1 = round(ROI.Position(2));
            X2 = round(X1 + ROI.Position(3) - 1);
            Y2 = round(Y1 + ROI.Position(4) - 1);

            mean_channel = mean2(app.calib_film(Y1:Y2, X1:X2));
            Std_channel = std2(app.calib_film(Y1:Y2, X1:X2));
            app.cGyEditField.Value = round(double(mean_channel));

            % Create a label for the ROI
            label_text = [num2str(app.cGyEditField.Value), ' ± ', num2str(round(Std_channel,2)),' cGy'];
            label_x = X1 + 0.5 * ROI.Position(3); % X coordinate for label
            label_y = Y1 + 0.5 * ROI.Position(4); % Y coordinate for label
            text_handle = text(label_x, label_y, label_text, 'Parent', app.CalibrationFigure, 'Color', 'w', 'FontSize', 13, 'FontWeight', 'bold', 'EdgeColor', 'b');

            % Store the text handle in the ROI UserData for deletion later
            ROI.UserData = text_handle;

            % Set the buttondown function to allow deletion of text label
            set(text_handle, 'ButtonDownFcn', @(src, event) deleteText(app, src));
        end

        % Button pushed function: CenterProfileButton
        function CenterProfileButtonPushed(app, event)
            cla(app.UIAxes3);
            cla(app.UIAxes2);

            pixelspacing = [app.PixelSpacing_X, app.PixelSpacing_Y];

            [tpsX, tpsY, filmX, filmY] = fn_AugmentedProfileExtraction(app.TPS_dose,  app.Film_dose, pixelspacing, [0, 0]);
            
            if ~isempty(tpsX)
                plot(app.UIAxes2, tpsX(:, 1), tpsX(:, 2), 'r-', 'LineWidth',0.5);
                hold(app.UIAxes2, 'on');
            end
            
            if ~isempty(filmX)
                plot(app.UIAxes2, filmX(:, 1), filmX(:, 2), 'b-',  'LineWidth',0.5);
            end
            hold(app.UIAxes2, 'off');
            
            if ~isempty(tpsY)
                plot(app.UIAxes3, tpsY(:, 1), tpsY(:, 2), 'r-',  'LineWidth',0.5);
                hold(app.UIAxes3, 'on');
            end
            
            if ~isempty(filmY)
                plot(app.UIAxes3, filmY(:, 1), filmY(:, 2), 'b-',  'LineWidth',0.5);
            end
            hold(app.UIAxes3, 'off');
        end

        % Button pushed function: OffcenterProfileButton
        function OffcenterProfileButtonPushed(app, event)
            [Pos1, Pos2] = ProfilePoint(app, app.FigFilmDose);
            Position = [Pos1, Pos2];

            cla(app.UIAxes3);
            cla(app.UIAxes2);

            pixelspacing = [app.PixelSpacing_X, app.PixelSpacing_Y];

            [tpsX, tpsY, filmX, filmY] = fn_AugmentedProfileExtraction(app.TPS_dose,  app.Film_dose, pixelspacing, Position);
            
            if ~isempty(tpsX)
                plot(app.UIAxes2, tpsX(:, 1), tpsX(:, 2), 'r-', 'LineWidth',0.5);
                hold(app.UIAxes2, 'on');
            end
            
            if ~isempty(filmX)
                plot(app.UIAxes2, filmX(:, 1), filmX(:, 2), 'b-',  'LineWidth',0.5);
            end
            hold(app.UIAxes2, 'off');
            
            if ~isempty(tpsY)
                plot(app.UIAxes3, tpsY(:, 1), tpsY(:, 2), 'r-',  'LineWidth',0.5);
                hold(app.UIAxes3, 'on');
            end
            
            if ~isempty(filmY)
                plot(app.UIAxes3, filmY(:, 1), filmY(:, 2), 'b-',  'LineWidth',0.5);
            end
            hold(app.UIAxes3, 'off');
        end

        % Button pushed function: CalculateButton
        function CalculateButtonPushed(app, event)
            if strcmp(app.VolRotStatus, 'true')
                Volume = app.RotateDicomVolume;                
            else 
                Volume = app.DicomVolume;
            end

            New_size = size(Volume);
            app.YZSlider.Limits = [1, double(New_size(2))];
            app.XZSlider.Limits = [1, double(New_size(1))];
            app.XYSlider.Limits= [1, double(New_size(3))];

            app.ColumnEditField.Value = abs(round(fn_IPPNormalizer(double(app.XmmEditField.Value),  double(app.X_dicom_cm), double([0 app.NumColumns]))));
            app.RowEditField.Value = abs(round(fn_IPPNormalizer(double(app.YmmEditField.Value), double(app.Y_dicom_cm), double([0 app.NumRows]))));
            app.FrameEditField.Value =   abs(round(fn_IPPNormalizer(double(app.ZmmEditField.Value),  double(app.Z_dicom_cm),double([0 app.NumFrames]))));

            app.yzPlane = fn_PlaneNavigator(Volume, 'YZ', app.ColumnEditField.Value, app.YZFigure, app.ColMap);
            app.xzPlane = fn_PlaneNavigator(Volume, 'XZ', app.RowEditField.Value, app.XZFigure, app.ColMap);
            app.xyPlane = fn_PlaneNavigator(Volume, 'XY', app.FrameEditField.Value, app.XYFigure, app.ColMap);

            app.YZSlider.Value =(app.ColumnEditField.Value);
            app.XZSlider.Value = (app.RowEditField.Value);
            app.XYSlider.Value =(app.FrameEditField.Value);
        end

        % Button pushed function: UpdateFilmDoseButton
        function UpdateFilmDoseButtonPushed(app, event)
            app.Film_dose = fn_AugmentFilmDose(app.calib_film, app.TiffDPI, app.FilmInterpolation);
            fn_mainImageDisplay(app.Film_dose, app.FigFilmDose, app.ColMap);

            app.Xshift_counter = 0; app.Yshift_counter = 0;
            app.XShiftsmmEditField.Value = 0;app.YShiftsmmEditField.Value = 0;
            app.DoseScaledEditField.Value = 0;            
        end

        % Button pushed function: SendPlaneforAnalysisButton
        function SendPlaneforAnalysisButtonPushed(app, event)
            %Asking user for the input to correct for the numeber of
            %fractions in dose file to scale the dose properly
            prompt = {'Enter the number of fraction'};
            frac = inputdlg(prompt);
            app.fx = str2double(frac);
            if isempty(frac)
                msgbox("Need to input the number of fraction for correct dosimetry!");
                return;
            end
            app.yzPlane = app.yzPlane * app.Grid_scaling * 10 * 1/app.fx;
            app.xzPlane = app.xzPlane * app.Grid_scaling * 10 * 1/app.fx;
            app.xyPlane = app.xyPlane * app.Grid_scaling * 10 * 1/app.fx;

            app.PlaneInfo = app.DicomInfo;
            pixelspacing = [app.PixelSpacing_X, app.PixelSpacing_Y];

            switch app.DropDown_3.Value
                case 'XZ'
                    
                    %Using augment function to index the dose file using
                    %pixel spacing, interpolation and image size 
                    app.TPS_dose = fn_AugmentPlanDose(app.xzPlane, pixelspacing, app.TPSInterpolation, app.ColumnEditField.Value,  app.NumFrames - app.FrameEditField.Value) ;

                    %Updating the dicom info for the export, for later use
                    app.PlaneInfo.PixelSpacing(1) = app.PixelSpacing_X * 1/app.TPSInterpolation;
                    app.PlaneInfo.PixelSpacing(2) = app.SliceThickness * 1/app.TPSInterpolation;                    
                    PlaneSize = size(app.TPS_dose(2:end, 2:end));
                    app.PlaneInfo.Rows = PlaneSize(2);
                    app.PlaneInfo.Columns = PlaneSize(1);
                    app.PlaneInfo.DoseGridScaling = app.DicomInfo.DoseGridScaling * 1/app.fx;
                    app.PlaneInfo.ImagePositionPatient = [app.ColumnEditField.Value; app.NumFrames -  app.FrameEditField.Value; app.TPSInterpolation];
            
                    %Displaying the image with color map
                    fn_mainImageDisplay(app.TPS_dose, app.FigDicomDose, app.ColMap);

                case 'YZ'

                    %Using augment function to index the dose file using
                    %pixel spacing, interpolation and image size
                    app.TPS_dose = fn_AugmentPlanDose(app.yzPlane, pixelspacing, app.TPSInterpolation,  app.RowEditField.Value, app.NumFrames - app.FrameEditField.Value);

                    %Updating the dicom info for the export, for later use
                    app.PlaneInfo.PixelSpacing(1) = app.PixelSpacing_Y * 1/app.TPSInterpolation;
                    app.PlaneInfo.PixelSpacing(2) = app.SliceThickness * 1/app.TPSInterpolation;                    
                    PlaneSize = size(app.TPS_dose(2:end, 2:end));
                    app.PlaneInfo.Rows = PlaneSize(2);
                    app.PlaneInfo.Columns = PlaneSize(1);
                    app.PlaneInfo.DoseGridScaling = app.DicomInfo.DoseGridScaling * 1/app.fx;
                    app.PlaneInfo.ImagePositionPatient = [app.RowEditField.Value; app.NumFrames - app.FrameEditField.Value;app.TPSInterpolation];

                    %Displaying the image with color map
                    fn_mainImageDisplay(app.TPS_dose, app.FigDicomDose, app.ColMap);

                case 'XY'
                    %Using augment function to index the dose file using
                    %pixel spacing, interpolation and image size
                    app.TPS_dose = fn_AugmentPlanDose(app.xyPlane, pixelspacing,app.TPSInterpolation, app.ColumnEditField.Value, app.RowEditField.Value) ;

                    %Updating the dicom info for the export, for later use
                    app.PlaneInfo.PixelSpacing(1) = app.PixelSpacing_X* 1/app.TPSInterpolation;
                    app.PlaneInfo.PixelSpacing(2) = app.PixelSpacing_Y* 1/app.TPSInterpolation;
                    PlaneSize = size(app.TPS_dose(2:end, 2:end));
                    app.PlaneInfo.Rows = PlaneSize(2);
                    app.PlaneInfo.Columns = PlaneSize(1);
                    app.PlaneInfo.DoseGridScaling = app.DicomInfo.DoseGridScaling * 1/app.fx;
                    app.PlaneInfo.ImagePositionPatient =  [app.ColumnEditField.Value;app.RowEditField.Value;app.TPSInterpolation];

                    %Displaying the image with color map
                    fn_mainImageDisplay(app.TPS_dose, app.FigDicomDose, app.ColMap);
            end

            %Reset the values
            app.Xshift_counter = 0; app.Yshift_counter = 0;
            app.XShiftsmmEditField.Value = 0;app.YShiftsmmEditField.Value = 0;
            app.DoseScaledEditField.Value = 0;
            app.AllFileName.Value =  app.DicomInfo.PatientID;

            app.FxEditField.Value = app.fx;
        end

        % Button pushed function: ROIFilterButton
        function ROIFilterButtonPushed(app, event)
            % Draw ROI
            ROI = drawrectangle('Parent', app.CalibrationFigure);
            
            % Get dimensions of calib_film
            [rows, cols, chh] = size(app.calib_film);
            
            if chh == 1  % Single-channel (grayscale) check
                % Create a copy of calib_film to apply the mask
                masked_film = zeros(rows, cols);
            
                % Get the ROI position (x, y, width, height) and convert to integer indices
                pos = round(ROI.Position);
                x = pos(1);
                y = pos(2);
                width = pos(3);
                height = pos(4);
            
                % Extract the values inside the ROI and place them in masked_film
                masked_film(y:y+height-1, x:x+width-1) = app.calib_film(y:y+height-1, x:x+width-1);
            
                % Update calib_film with masked_film
                app.calib_film = masked_film;
            else
                return;
            end
            fn_mainImageDisplay(app.calib_film, app.CalibrationFigure, app.ColMap);
        end

        % Button pushed function: FilterNoiseButton
        function FilterNoiseButtonPushed(app, event)
            app.calib_film(isnan(app.calib_film) | isinf(app.calib_film) | abs(app.calib_film) >app.FilterNoiseEditField.Value) = 1 ;
            fn_mainImageDisplay(app.calib_film, app.CalibrationFigure, app.ColMap);
        end

        % Button pushed function: ScaleFilmDoseButton
        function ScaleFilmDoseButtonPushed(app, event)
            scaling_factor = (100 + double(app.ScaleVal.Value))/ 100 ;

            input = sprintf('Scaling the film dose by %s. Continue?', num2str(scaling_factor));
            answer = questdlg(input,"Dose scaling", "Yes","No", "No");
            switch answer
                case 'Yes'
                    f_dose = app.Film_dose(2:end, 2:end) .* scaling_factor;
                    app.Film_dose = fn_AugmentFilmDose(f_dose, app.TiffDPI,app.FilmInterpolation);
                    CenterProfileButtonPushed(app);
                case 'No'
                    return;
            end

            current_val = app.DoseScaledEditField.Value ;
            new_val = current_val + app.ScaleVal.Value;
            app.DoseScaledEditField.Value = new_val;                    
        end

        % Button pushed function: MedianFilterButton
        function MedianFilterButtonPushed(app, event)
            if numel(size(app.calib_film)) > 2
                R = app.calib_film(:, :, 1);
                G = app.calib_film(:, :, 2);
                B = app.calib_film(:, :, 3);

                % Apply median filter with user-specified window size
                sz = round(app.MedianSizeEditField.Value);
                R_filtered = medfilt2(R, [sz sz]);
                G_filtered = medfilt2(G, [sz sz]);
                B_filtered = medfilt2(B, [sz sz]);

                % Combine the channels back into one image
                new_Image = cat(3, R_filtered, G_filtered, B_filtered);
            else
                sz = round(app.MedianSizeEditField.Value);
                new_Image = medfilt2(app.calib_film, [sz sz]);
            end

            % Update the image display
            app.calib_film = new_Image;
            fn_mainImageDisplay(app.calib_film, app.CalibrationFigure, app.ColMap, app.TiffDPI, app.new_center);
        end

        % Button pushed function: RotateButton
        function RotateButtonPushed(app, event)
           Pitch = app.PitchEditField.Value;
           Yaw = app.YawEditField.Value;
           Roll =  app.RollEditField.Value;

           if Pitch == 0
               Pitch = Pitch - app.Post_pitch;
           end           
           if Yaw == 0
               Yaw = Yaw - app.Post_yaw;
           end
           if Roll == 0
               Roll = Roll - app.Post_roll;
           end
           
            if strcmp(app.VolRotStatus, 'true')
                Volume = app.RotateDicomVolume;                
            else 
                Volume = (app.DicomVolume);
            end
            app.new_center = [app.ColumnEditField.Value, app.RowEditField.Value, app.FrameEditField.Value];
            app.RotateDicomVolume =  Volume;  

            app.RotateDicomVolume= fn_DicomVRotate_Adv(app.RotateDicomVolume, 'Pitch', Pitch,  app.new_center);
            app.RotateDicomVolume = fn_DicomVRotate_Adv(app.RotateDicomVolume, 'Yaw', Yaw,  app.new_center);
            app.RotateDicomVolume= fn_DicomVRotate_Adv(app.RotateDicomVolume, 'Roll',Roll,  app.new_center);

            app.Post_pitch = Pitch;
            app.Post_yaw = Yaw;
            app.Post_roll = Roll;

            app.VolRotStatus = 'true';

            CalculateButtonPushed(app);
        end

        % Button pushed function: GammaButton
        function GammaButtonPushed(app, event)
            DD = app.DDEditField.Value;
            DTA = app.DTAmmEditField.Value;
            PercentSignal = app.SingalEditField.Value; 

            %Gamma evaluation between reference and evaluated plane
            [app.gammamap, pass] = fn_Gamma_evaluation(app.TPS_dose, app.Film_dose,app.DropDown_8.Value, DTA, DD, PercentSignal);

            % Display the gamma map in mm axes
            fn_mainImageDisplay(app.gammamap, app.UIAxes6, app.ColMap, app.TiffDPI, app.new_center);

            app.EditField_3.Value = double(DD);
            app.EditField2.Value = double(DTA);
            app.EditField_4.Value = double(pass);
            if double(pass) < 90
                app.EditField_4.BackgroundColor = 'Red';
                app.EditField_4.FontColor = 'Black';
            else
                app.EditField_4.BackgroundColor = 'Green';
                app.EditField_4.FontColor = 'Black';
            end
        end

        % Value changed function: RotatedPlane
        function RotatedPlaneValueChanged(app, event)
             switch app.RotatedPlane.Value
                case 'YZ'
                    yzPla = fn_PlaneNavigator(app.RotateDicomVolume,'YZ',app.YZSlider.Value, app.RotatedFigure, app.ColMap);

                    [~, linearIndex] = max(yzPla(:));
                    [row, col] = ind2sub(size(yzPla), linearIndex);
                    app.Label_11.Text = mat2str(row);
                    app.Label_12.Text = mat2str(col);
                case 'XZ'

                    xzPla = fn_PlaneNavigator(app.RotateDicomVolume,'XZ',app.XZSlider.Value, app.RotatedFigure, app.ColMap);

                    [~, linearIndex] = max(xzPla(:));
                    [row, col] = ind2sub(size(xzPla), linearIndex);
                    app.Label_11.Text = mat2str(row);
                    app.Label_12.Text = mat2str(col);
                case 'XY'

                    xyPla = fn_PlaneNavigator(app.RotateDicomVolume,'XY',app.XYSlider.Value, app.RotatedFigure, app.ColMap);

                    [~, linearIndex] = max(xyPla(:));
                    [row, col] = ind2sub(size(app.xyPlane), linearIndex);
                    app.Label_11.Text = mat2str(row);
                    app.Label_12.Text = mat2str(col);
             end
        end

        % Value changed function: OGPlane
        function OGPlaneValueChanged(app, event)
            switch app.OGPlane.Value
                case 'YZ'
                    yzPla = fn_PlaneNavigator(app.DicomVolume,'YZ',app.YZSlider.Value,  app.OriginalFigure, app.ColMap);

                    [~, linearIndex] = max(yzPla(:));
                    [row, col] = ind2sub(size(yzPla), linearIndex);
                    app.Label_9.Text = mat2str(row);
                    app.Label_10.Text = mat2str(col);

                case 'XZ'

                    xzPla = fn_PlaneNavigator(app.DicomVolume,'XZ',app.XZSlider.Value,  app.OriginalFigure, app.ColMap);

                    [~, linearIndex] = max(xzPla(:));
                    [row, col] = ind2sub(size(xzPla), linearIndex);
                    app.Label_9.Text = mat2str(row);
                    app.Label_10.Text = mat2str(col);
                case 'XY'
                    xyPla = fn_PlaneNavigator(app.DicomVolume,'XY',app.XYSlider.Value,  app.OriginalFigure, app.ColMap);

                    [~, linearIndex] = max(xyPla(:));
                    [row, col] = ind2sub(size(xyPla), linearIndex);
                    app.Label_9.Text = mat2str(row);
                    app.Label_10.Text = mat2str(col);
            end
        end

        % Button pushed function: Button_12
        function Button_12Pushed(app, event)
            switch app.RotatedPlane.Value
                case 'YZ'
                    app.RotateDicomVolume = imtranslate(app.RotateDicomVolume, [0, 0,  round(app.StepsizepixelEditField.Value)]);
                    app.RotatedPlaneValueChanged(app);
                case 'XZ'
                    app.RotateDicomVolume = imtranslate(app.RotateDicomVolume, [0 , 0, round(app.StepsizepixelEditField.Value)]);
                    app.RotatedPlaneValueChanged(app);
                case 'XY'
                    app.RotateDicomVolume = imtranslate(app.RotateDicomVolume, [0,  -round(app.StepsizepixelEditField.Value), 0]);
                    app.RotatedPlaneValueChanged(app);
            end
        end

        % Button pushed function: vButton
        function vButtonPushed(app, event)
            switch app.RotatedPlane.Value
                case 'YZ'
                    app.RotateDicomVolume = imtranslate(app.RotateDicomVolume, [0, 0,  -round(app.StepsizepixelEditField.Value)]);
                    app.RotatedPlaneValueChanged(app);
                case 'XZ'
                    app.RotateDicomVolume = imtranslate(app.RotateDicomVolume, [0 , 0, -round(app.StepsizepixelEditField.Value)]);
                    app.RotatedPlaneValueChanged(app);
                case 'XY'
                    app.RotateDicomVolume = imtranslate(app.RotateDicomVolume, [0,  round(app.StepsizepixelEditField.Value), 0]);
                    app.RotatedPlaneValueChanged(app);
            end
        end

        % Button pushed function: Button_11
        function Button_11Pushed(app, event)
            switch app.RotatedPlane.Value
                case 'YZ'
                    app.RotateDicomVolume = imtranslate(app.RotateDicomVolume, [0,  -round(app.StepsizepixelEditField.Value, 0)]);
                    app.RotatedPlaneValueChanged(app);
                case 'XZ'
                    app.RotateDicomVolume = imtranslate(app.RotateDicomVolume, [ round(app.StepsizepixelEditField.Value), 0,0]);
                    app.RotatedPlaneValueChanged(app);
                case 'XY'
                    app.RotateDicomVolume = imtranslate(app.RotateDicomVolume, [round(app.StepsizepixelEditField.Value), 0, 0]);
                    app.RotatedPlaneValueChanged(app);
            end
        end

        % Button pushed function: Button_10
        function Button_10Pushed(app, event)
            switch app.RotatedPlane.Value
                case 'YZ'
                    app.RotateDicomVolume = imtranslate(app.RotateDicomVolume, [0,  round(app.StepsizepixelEditField.Value, 0)]);
                    app.RotatedPlaneValueChanged(app);
                case 'XZ'
                    app.RotateDicomVolume = imtranslate(app.RotateDicomVolume, [ -round(app.StepsizepixelEditField.Value), 0,0]);
                    app.RotatedPlaneValueChanged(app);
                case 'XY'
                    app.RotateDicomVolume = imtranslate(app.RotateDicomVolume, [  -round(app.StepsizepixelEditField.Value), 0,0]);
                   app.RotatedPlaneValueChanged(app);
            end
        end

        % Button pushed function: FilmProfileButton
        function FilmProfileButtonPushed(app, event)
            film_size = size(app.calib_film);

            center = app.ButtonGroup_2.SelectedObject.Text;

            if strcmp(center, 'Center')
                app.x_pro = round(film_size(2) * 0.5);
                app.y_pro = round(film_size(1) * 0.5);
            else
                [app.x_pro, app.y_pro] = ProfilePoint(app, app.CalibrationFigure);
            end

            [x_profile, y_profile, dis_x, dis_y] = fn_DoseProfile(app.calib_film, app.x_pro, app.y_pro, 'Film');

            Dose_Unit = 'Dose (cGy)';
            if app.NormalizeCheckBox.Value
                %Normaling X and Y profile
                x_max = max(x_profile(:)); x_min = min(x_profile(:));
                y_max = max(y_profile(:)); y_min = min(y_profile(:));
                x_profile = (x_profile - x_min) /(x_max - x_min) * 100;
                y_profile = (y_profile - y_min) /(y_max - y_min) * 100;
                Dose_Unit = 'Dose (%)';
            end
            
            dis_x = dis_x .* (2.54/app.TiffDPI);
            dis_y = dis_y .* (2.54/app.TiffDPI);
            app.combinedX = [dis_x(:), x_profile(:)];
            app.combinedY = [dis_y(:), y_profile(:)];

            plot(app.UIAxes9,dis_x, x_profile);
            app.UIAxes9.YLabel.String = Dose_Unit;
            app.UIAxes9.XLabel.String = 'Distance (cm)';
            app.UIAxes9.XTickMode = 'auto';
            app.UIAxes9.YTickMode = 'auto';

            plot(app.UIAxes9_2,dis_y, y_profile);
            app.UIAxes9_2.YLabel.String = Dose_Unit;
            app.UIAxes9_2.XLabel.String = 'Distance (cm)';
            app.UIAxes9_2.XTickMode = 'auto';
            app.UIAxes9_2.YTickMode = 'auto';
        end

        % Button pushed function: ManualCropButton
        function ManualCropButtonPushed(app, event)
            rectangle = drawrectangle(app.CalibrationFigure);
            
            % Save the crop offset to update our isocenter
            cropPos = rectangle.Position;
            app.calib_film = imcrop((app.calib_film), cropPos);
            
            % Update center: subtract the crop's top-left from the current center
            app.new_center(1) = app.new_center(1) - cropPos(1) + 1;
            app.new_center(2) = app.new_center(2) - cropPos(2) + 1;

            new_size = size(app.calib_film);
            app.HeightCC.Value = new_size(1);
            app.WidthCC.Value = new_size(2);
            
            fn_mainImageDisplay(app.calib_film, app.CalibrationFigure, app.ColMap, app.TiffDPI, app.new_center);
        end

        % Button pushed function: UpButton_2
        function UpButton_2Pushed(app, event)
            f_dose = imtranslate(app.Film_dose(2:end, 2:end), [0, -(app.StepsizepixelEditField_2.Value)], 'OutputView','full', 'FillValues',1);
            app.Film_dose = fn_AugmentFilmDose(f_dose, app.TiffDPI,app.FilmInterpolation);
            
            % Maintain mm axes in Analysis Window
            f_size = size(f_dose);
            f_center = [(f_size(2)+1)/2, (f_size(1)+1)/2];
            fn_mainImageDisplay(f_dose, app.FigFilmDose, app.ColMap, app.TiffDPI, f_center);

            if app.PerformGammaCheckBox.Value == 1
                GammaButtonPushed(app);
            end

            X_ini = (app.YShiftsmmEditField.Value);
            X_final = X_ini + app.StepsizepixelEditField_2.Value * (25.4 /app.TiffDPI);
            app.YShiftsmmEditField.Value = X_final;

            CenterProfileButtonPushed(app);
        end

        % Button pushed function: DownButton_2
        function DownButton_2Pushed(app, event)
            f_dose = imtranslate(app.Film_dose(2:end, 2:end), [0, (app.StepsizepixelEditField_2.Value)], 'OutputView','full', 'FillValues',1);
            app.Film_dose = fn_AugmentFilmDose(f_dose, app.TiffDPI,app.FilmInterpolation);

            % Maintain mm axes in Analysis Window
            f_size = size(f_dose);
            f_center = [(f_size(2)+1)/2, (f_size(1)+1)/2];
            fn_mainImageDisplay(f_dose, app.FigFilmDose, app.ColMap, app.TiffDPI, f_center);            

            if app.PerformGammaCheckBox.Value == 1
                GammaButtonPushed(app);
            end

            X_ini = (app.YShiftsmmEditField.Value);
            X_final = X_ini - app.StepsizepixelEditField_2.Value * (25.4 /app.TiffDPI);
            app.YShiftsmmEditField.Value = X_final;

            CenterProfileButtonPushed(app);
        end

        % Button pushed function: LeftButton_2
        function LeftButton_2Pushed(app, event)
            f_dose = imtranslate(app.Film_dose(2:end,2:end), [-(app.StepsizepixelEditField_2.Value), 0], 'OutputView','full', 'FillValues',1);
            app.Film_dose = fn_AugmentFilmDose(f_dose, app.TiffDPI,app.FilmInterpolation);

            % Maintain mm axes in Analysis Window
            f_size = size(f_dose);
            f_center = [(f_size(2)+1)/2, (f_size(1)+1)/2];
            fn_mainImageDisplay(f_dose, app.FigFilmDose, app.ColMap, app.TiffDPI, f_center);

            if app.PerformGammaCheckBox.Value == 1
                GammaButtonPushed(app);
            end

            X_ini = (app.XShiftsmmEditField.Value);
            X_final = X_ini - app.StepsizepixelEditField_2.Value * (25.4 /app.TiffDPI);
            app.XShiftsmmEditField.Value = X_final;

            CenterProfileButtonPushed(app);
        end

        % Button pushed function: RightButton_2
        function RightButton_2Pushed(app, event)
            f_dose = imtranslate(app.Film_dose(2:end, 2:end), [(app.StepsizepixelEditField_2.Value), 0], 'OutputView','full', 'FillValues',1);
            app.Film_dose = fn_AugmentFilmDose(f_dose, app.TiffDPI,app.FilmInterpolation);

            % Maintain mm axes in Analysis Window
            f_size = size(f_dose);
            f_center = [(f_size(2)+1)/2, (f_size(1)+1)/2];
            fn_mainImageDisplay(f_dose, app.FigFilmDose, app.ColMap, app.TiffDPI, f_center);


            if app.PerformGammaCheckBox.Value == 1
                GammaButtonPushed(app);
            end

            X_ini = (app.XShiftsmmEditField.Value);
            X_final = X_ini + app.StepsizepixelEditField_2.Value * (25.4 /app.TiffDPI);
            app.XShiftsmmEditField.Value = X_final;

            CenterProfileButtonPushed(app);
        end

        % Button pushed function: ClearXButton
        function ClearXButtonPushed(app, event)
        allComponents = findall(app.CalibrationFigure);
        
        % Iterate through each component and delete it, except the axes and the image
        for i = 1:length(allComponents)
            % Exclude the axes and image from deletion
            if ~isa(allComponents(i), 'matlab.graphics.axis.Axes') && ...
               ~isa(allComponents(i), 'matlab.graphics.primitive.Image')
                delete(allComponents(i));
            end
        end
        end

        % Button pushed function: InterpolationButton
        function InterpolationButtonPushed(app, event)
            if numel(size(app.calib_film)) > 2
                msgbox("Please load dose file");
                return;
            end
        
            % Store original film if not already done
            if isempty(app.OriginalFilm)
                app.OriginalFilm = app.calib_film;
            end
        
            method = app.DropDown_7.Value;
            density = app.EditField_5.Value;
        
            app.calib_film = fn_DoseSampling(app.OriginalFilm, density, method);
        
            fn_mainImageDisplay(app.calib_film, app.CalibrationFigure, app.ColMap);
            app.FilmInterpolation = density;
        
            FilmProfileButtonPushed(app);
        
            new_size = size(app.calib_film);
            app.HeightCC.Value = new_size(1);
            app.WidthCC.Value = new_size(2);
        end

        % Value changing function: YZSlider
        function YZSliderValueChanging(app, event)
            app.slider_current1 = round(event.Value);
            app.ColumnEditField.Value = app.slider_current1;
            app.XmmEditField.Value = fn_InverseIPPNormalizer(double(app.ColumnEditField.Value) ,double(app.X_dicom_cm), double([0 app.NumColumns]) );
            if strcmp(app.VolRotStatus, 'false')
                volume = app.DicomVolume;
            else
                volume = app.RotateDicomVolume;
            end
            app.yzPlane = fn_PlaneNavigator(volume, 'YZ', app.slider_current1, app.YZFigure, app.ColMap);
            
            if app.MaxPlaneDoseCheckBox.Value == 1
                max_dose = max(double(app.yzPlane(:))) * app.Grid_scaling;
                app.GyEditField_3.Value = max_dose;
            end
        end

        % Value changing function: XZSlider
        function XZSliderValueChanging(app, event)
            app.slider_current2 = round(event.Value);
            app.RowEditField.Value = app.slider_current2;
            app.YmmEditField.Value = fn_InverseIPPNormalizer(double(app.RowEditField.Value) ,double(app.Y_dicom_cm), double([0 app.NumRows]) );
            if strcmp(app.VolRotStatus, 'false')
                volume = app.DicomVolume;
            else
                volume = app.RotateDicomVolume;
            end
            app.xzPlane = fn_PlaneNavigator(volume, 'XZ', app.slider_current2, app.XZFigure, app.ColMap);
            if app.MaxPlaneDoseCheckBox.Value == 1
                max_dose = max(double(app.xzPlane(:))) * app.Grid_scaling;
                app.GyEditField_2.Value = max_dose;
            end
        end

        % Value changing function: XYSlider
        function XYSliderValueChanging(app, event)
            app.slider_current3 = round(event.Value);
            app.FrameEditField.Value = app.slider_current3;
            app.ZmmEditField.Value = fn_InverseIPPNormalizer(double(app.FrameEditField.Value) ,double(app.Z_dicom_cm), double([0 app.NumFrames]) );
            if strcmp(app.VolRotStatus, 'false')
                volume = app.DicomVolume;
            else
                volume = app.RotateDicomVolume;
            end
            app.xyPlane = fn_PlaneNavigator(volume, 'XY', app.slider_current3, app.XYFigure, app.ColMap);
            if app.MaxPlaneDoseCheckBox.Value == 1
                max_dose = max(double(app.xyPlane(:))) * app.Grid_scaling;
                app.GyEditField.Value = max_dose;
            end
        end

        % Button pushed function: CenterCropButton
        function CenterCropButtonPushed(app, event)
            Img_size = size(app.calib_film);
            
            % Use currently entered values (HeightCC, WidthCC)
            % If they are in mm (logic: user likely thinks in mm if centered)
            % Or pixels. For now, we use the value directly.
            target_size = [app.HeightCC.Value, app.WidthCC.Value];

            % Ensure target_size is within bounds
            target_size(1) = min(target_size(1), Img_size(1));
            target_size(2) = min(target_size(2), Img_size(2));

            % Perform the center crop
            r = centerCropWindow2d(Img_size(1:2), target_size);
            app.calib_film = imcrop(app.calib_film, r);

            % Update display with new center (center of the cropped image)
            new_size = size(app.calib_film);
            app.new_center = [(new_size(2)+1)/2, (new_size(1)+1)/2];
            
            delete(allchild(app.CalibrationFigure)); % Clear all old markers/plots
            hold(app.CalibrationFigure, 'off');
            fn_mainImageDisplay(app.calib_film, app.CalibrationFigure, app.ColMap, app.TiffDPI, app.new_center);

            app.HeightCC.Value = new_size(1);
            app.WidthCC.Value = new_size(2);
        end

        % Button pushed function: Button_21
        function Button_8Pushed(app, event)
            [filename, pathname] = uiputfile('*.pdf', 'Save Report As');

            if isempty(app.AllFileName.Value)
                prompt = 'Enter Plan ID: ';
                frac = inputdlg(prompt);
                Plan_ID = {frac(1)};
            else
                Plan_ID = app.AllFileName.Value;
            end

            pause(0.5);
            h = waitbar(0, "Preparing the pdf. Please wait!");

            waitbar( 0.1, h, "Looking for the directory!");
            % Get the full path of the currently running script or function
            currentFile = mfilename('fullpath');
            % Extract the directory path from the full path
            [currentFolder, ~, ~] = fileparts(currentFile);

            waitbar( 0.2, h, "Reading the information!");
            MRN_ID = {app.DicomInfo.PatientID}; 
            DD = app.DDEditField.Value;
            DTA = app.DTAmmEditField.Value;
            Compar1 = app.DropDown_8.Value;
            ShiftsX = app.XShiftsmmEditField.Value;
            ShiftsY = app.YShiftsmmEditField.Value;
            PassingRate = app.EditField_4.Value;

            waitbar( 0.3, h, "Preparing figures!");
            %Contrast matching the images 
            tps_min = app.Slider.Value(1);
            tps_max = app.Slider.Value(2);
            film_min = app.Slider_2.Value(1);
            film_max = app.Slider_2.Value(2);  
            tps_dose = imadjust(mat2gray(app.TPS_dose), [tps_min tps_max]);
            film_dose = imadjust(mat2gray(app.Film_dose), [film_min, film_max]);

            waitbar( 0.4, h, "Saving TPS figure!");
            % Save the TPS plane
            fig = figure('visible', 'off');
            imshow(tps_dose, []); 
            colormap(app.ColMap);
            xlim('tight');
            ylim('tight');
            filePath1 = fullfile(currentFolder, 'TPSPlane.jpg');
            saveas(fig, filePath1);
            close(fig);
            
            % Resize Film_dose to match TPS_dose size
            [tpsRows, tpsCols] = size(app.TPS_dose);
            filmNormalized = imresize(film_dose, [tpsRows, tpsCols]);          
                        
            waitbar( 0.5, h, "Saving Film figure!");
            % Save the Film plane
            fig = figure('visible', 'off');
            imshow(filmNormalized, []);
            colormap(app.ColMap);
            xlim('tight');
            ylim('tight');
            filePath2 = fullfile(currentFolder, 'FilmPlane.jpg');
            saveas(fig, filePath2);
            close(fig);

            [tpsX, tpsY, filmX, filmY] = fn_AugmentedProfileExtraction(app.TPS_dose, app.Film_dose,...
                [app.PixelSpacing_X, app.PixelSpacing_Y], [0,0]);
            
            waitbar( 0.6, h, "Saving X-Profiles!");
            fig = figure('visible','off');
            plot( tpsX(:, 1), tpsX(:, 2), 'r-', 'LineWidth',0.5); 
            hold on;
            plot( filmX(:, 1), filmX(:, 2), 'b-', 'LineWidth',0.5); 
            legend('TPS', 'Film', 'FontSize', 12);
            title('[ X-Profile ]', 'FontSize', 18, 'FontWeight','bold'); 
            xlabel(' Distance (mm)', 'FontSize', 14,'FontWeight','bold'); 
            ylabel('Dose (cGy)', 'FontSize', 14,'FontWeight','bold');
            xlim('tight');
            ylim('tight');

            % Access the current axes and set the FontSize property for the ticks
            ax = gca;
            ax.FontSize = 15; 
            hold off;
            filePath3 = fullfile(currentFolder, 'XProfile.jpg');
            saveas(fig, filePath3);
            close(fig);

            waitbar( 0.6, h, "Saving Y-Profiles!");
            fig = figure('visible','off');
            plot( tpsY(:, 1), tpsY(:, 2), 'r-', 'LineWidth',0.5);
            hold on;
            plot( filmY(:, 1), filmY(:, 2), 'b-', 'LineWidth',0.5);
            legend('TPS', 'Film', 'FontSize', 12);
            title('[ Y-Profile ]', 'FontSize', 18, 'FontWeight','bold');
            xlabel(' Distance (mm)', 'FontSize', 14,'FontWeight','bold'); 
            ylabel('Dose (cGy)', 'FontSize', 14,'FontWeight','bold'); 
            xlim('tight');
            ylim('tight');

            % Access the current axes and set the FontSize property for the ticks
            ax = gca;
            ax.FontSize = 15; 
            hold off;
            filePath4 = fullfile(currentFolder, 'YProfile.jpg');
            saveas(fig, filePath4);
            close(fig);
            fig = figure('visible','off');
            imshow(app.gammamap);
            colormap(app.ColMap);
            xlim('tight');
            ylim('tight');
            filePath5 = fullfile(currentFolder, 'GammaMap.jpg');

            saveas(fig, filePath5);
            close(fig);

            waitbar(0.7, h, "Merging the pdf!");
            
            fn_PDFPrinter(MRN_ID, Plan_ID,filePath1, filePath2, filePath3, filePath4, ...
                DD, DTA, Compar1,  ShiftsX,ShiftsY,PassingRate,filePath5)

            waitbar(0.8, h, "Cleaning up!");
            delete TPSPlane.jpg;
            delete FilmPlane.jpg;
            delete XProfile.jpg;
            delete YProfile.jpg;
            delete GammaMap.jpg;

            %Rename the file and then move it to the desired location as per
            %user input
            waitbar(0.9, h, "Moving the file!");
            close(h);
            movefile('MyReport.pdf', filename);
            movefile(filename, pathname);
            
            %Open the default/selected directory
            winopen(pathname);            
        end

        % Button pushed function: AutoCenterButton
        function AutoCenterButtonPushed(app, event)
            app.calib_film = fn_AutoCenter(app.calib_film);
            
            % Recalculate center after auto-translation
            new_size = size(app.calib_film);
            app.new_center = [(new_size(2)+1)/2, (new_size(1)+1)/2];
            fn_mainImageDisplay(app.calib_film, app.CalibrationFigure, app.ColMap, app.TiffDPI, app.new_center);

            app.HeightCC.Value = new_size(1);
            app.WidthCC.Value = new_size(2);
        end

        % Button pushed function: DistanceButton
        function DistanceButtonPushed(app, event)
        fn_drawDynamicLineAndDisplayDistance(app.CalibrationFigure, app.TiffDPI);
        end

        % Button pushed function: AreaButton
        function AreaButtonPushed(app, event)
           ROI = drawrectangle(app.CalibrationFigure, 'MarkerSize', 0.1);
            X1 = round(ROI.Position(1));
            Y1 = round(ROI.Position(2));
            X2 = round(X1 + ROI.Position(3) - 1);
            Y2 = round(Y1 + ROI.Position(4) - 1);
            
            % Converting the position into area
            width_cm = abs((X2 - X1) * 2.54 / app.TiffDPI);  % Width in cm
            height_cm = abs((Y2 - Y1) * 2.54 / app.TiffDPI); % Height in cm
        
            Area = width_cm * height_cm; % Area in square cm
        
            % Create a label for the ROI
            label_text = [num2str(Area), ' cm^2'];
            label_x = X1 + 0.5 * ROI.Position(3); % X coordinate for label
            label_y = Y1 + 0.5 * ROI.Position(4); % Y coordinate for label
            text_handle = text(label_x, label_y, label_text, 'Parent', app.CalibrationFigure, 'Color', 'w', 'FontSize', 13, 'FontWeight', 'bold', 'EdgeColor', 'b');
        
            % Store the text handle in the ROI UserData for deletion later
            ROI.UserData = text_handle;
        
            % Set the buttondown function to allow deletion of text label
            set(text_handle, 'ButtonDownFcn', @(src, event) deleteText(app, src));
        end

        % Button pushed function: Button_5
        function Button_5Pushed(app, event)
            if app.YZSlider.Value > 0 && app.YZSlider.Value < app.NumColumns
                app.YZSlider.Value = app.YZSlider.Value  + 1;
                app.ColumnEditField.Value = app.YZSlider.Value;
                app.XmmEditField.Value = fn_InverseIPPNormalizer(double(app.ColumnEditField.Value) ,double(app.X_dicom_cm), double([0 app.NumColumns]) );
                if strcmp(app.VolRotStatus, 'false')
                    volume = app.DicomVolume;
                else
                    volume = app.RotateDicomVolume;
                end
                app.yzPlane = fn_PlaneNavigator(volume, 'YZ', app.YZSlider.Value, app.YZFigure, app.ColMap);
                if app.MaxPlaneDoseCheckBox.Value == 1
                    max_dose = max(double(app.yzPlane(:))) * app.Grid_scaling;
                    app.GyEditField_3.Value = max_dose;
                end
            else
                return;
            end
        end

        % Button pushed function: Button_2
        function Button_2Pushed(app, event)
            app.YZSlider.Value = app.YZSlider.Value  -1;
            if app.YZSlider.Value >= 0 && app.YZSlider.Value <= app.NumColumns                
                app.ColumnEditField.Value = app.YZSlider.Value;
                app.XmmEditField.Value = fn_InverseIPPNormalizer(double(app.ColumnEditField.Value) ,double(app.X_dicom_cm), double([0 app.NumColumns]) );
                if strcmp(app.VolRotStatus, 'false')
                    volume = app.DicomVolume;
                else
                    volume = app.RotateDicomVolume;
                end
                app.yzPlane = fn_PlaneNavigator(volume, 'YZ', app.YZSlider.Value, app.YZFigure, app.ColMap);
                if app.MaxPlaneDoseCheckBox.Value == 1
                    max_dose = max(double(app.yzPlane(:))) * app.Grid_scaling;
                    app.GyEditField_3.Value = max_dose;
                end
            else
                return;
            end            
        end

        % Button pushed function: Button_6
        function Button_6Pushed(app, event)
           app.XZSlider.Value = app.XZSlider.Value + 1;
            if app.XZSlider.Value >= 0 && app.XZSlider.Value <= app.NumRows                
                app.RowEditField.Value = app.XZSlider.Value;
                app.YmmEditField.Value = fn_InverseIPPNormalizer(double(app.RowEditField.Value) ,double(app.Y_dicom_cm), double([0 app.NumRows]) );
                if strcmp(app.VolRotStatus, 'false')
                    volume = app.DicomVolume;
                else
                    volume = app.RotateDicomVolume;
                end
                app.xzPlane = fn_PlaneNavigator(volume, 'XZ', app.XZSlider.Value, app.XZFigure, app.ColMap);
                if app.MaxPlaneDoseCheckBox.Value == 1
                    max_dose = max(double(app.xzPlane(:))) * app.Grid_scaling;
                    app.GyEditField_2.Value = max_dose;
                end
            else
                return;
            end
        end

        % Button pushed function: Button_3
        function Button_3Pushed(app, event)
            app.XZSlider.Value = app.XZSlider.Value - 1;
            if app.XZSlider.Value >= 0 && app.XZSlider.Value <= app.NumRows
                
                app.RowEditField.Value = app.XZSlider.Value;
                app.YmmEditField.Value = fn_InverseIPPNormalizer(double(app.RowEditField.Value) ,double(app.Y_dicom_cm), double([0 app.NumRows]) );
                if strcmp(app.VolRotStatus, 'false')
                    volume = app.DicomVolume;
                else
                    volume = app.RotateDicomVolume;
                end
                app.xzPlane = fn_PlaneNavigator(volume, 'XZ', app.XZSlider.Value, app.XZFigure, app.ColMap);
                if app.MaxPlaneDoseCheckBox.Value == 1
                    max_dose = max(double(app.xzPlane(:))) * app.Grid_scaling;
                    app.GyEditField_2.Value = max_dose;
                end
            else
                return;
            end
        end

        % Button pushed function: Button_7
        function Button_7Pushed(app, event)
            app.XYSlider.Value = app.XYSlider.Value + 1; 
            if app.XYSlider.Value >= 0 && app.XYSlider.Value <= app.NumFrames
                
                app.FrameEditField.Value =  app.XYSlider.Value;
                app.ZmmEditField.Value = fn_InverseIPPNormalizer(double(app.FrameEditField.Value) ,double(app.Z_dicom_cm), double([0 app.NumFrames]) );
                if strcmp(app.VolRotStatus, 'false')
                    volume = app.DicomVolume;
                else
                    volume = app.RotateDicomVolume;
                end
                app.xyPlane = fn_PlaneNavigator(volume, 'XY',  app.XYSlider.Value, app.XYFigure, app.ColMap);
                if app.MaxPlaneDoseCheckBox.Value == 1
                    max_dose = max(double(app.xyPlane(:))) * app.Grid_scaling;
                    app.GyEditField.Value = max_dose;
                end
             else
                 return;
            end
        end

        % Button pushed function: Button_4
        function Button_4Pushed(app, event)
           app.XYSlider.Value = app.XYSlider.Value -1;
            if app.XYSlider.Value >= 0 && app.XYSlider.Value <= app.NumFrames
                
                app.FrameEditField.Value =  app.XYSlider.Value;
                app.ZmmEditField.Value = fn_InverseIPPNormalizer(double(app.FrameEditField.Value) ,double(app.Z_dicom_cm), double([0 app.NumFrames]) );
                if strcmp(app.VolRotStatus, 'false')
                    volume = app.DicomVolume;
                else
                    volume = app.RotateDicomVolume;
                end
                app.xyPlane = fn_PlaneNavigator(volume, 'XY',  app.XYSlider.Value, app.XYFigure, app.ColMap);
                if app.MaxPlaneDoseCheckBox.Value == 1
                    max_dose = max(double(app.xyPlane(:))) * app.Grid_scaling;
                    app.GyEditField.Value = max_dose;
                end
            else
                return;
            end
        end

        % Close request function: FilmDosimetryUIFigure
        function MainAppCloseRequest(app, event)
            delete(app)
            delete(app.DialogApp)            
        end

        % Button pushed function: SmoothButton
        function SmoothButtonPushed(app, event)
            app.calib_film = fn_MatrixSmooth(app.calib_film, app.SmoothDropDown_2.Value, app.SmoothWIn_2.Value);    
            fn_mainImageDisplay(app.calib_film, app.CalibrationFigure, app.ColMap, app.TiffDPI, app.new_center);
            FilmProfileButtonPushed(app);

            new_size = size(app.calib_film);
            app.HeightCC.Value = new_size(1);
            app.WidthCC.Value = new_size(2);
        end

        % Button pushed function: Button_9
        function Button_9Pushed(app, event)
           if ~isempty(app.DicomInfo)
            app.DialogApp = DicomInfoApp_exported(app, app.DicomInfo);
           else
               msgbox('No dicom file loaded!')
           end
        end

        % Button pushed function: CalibrationTools
        function CalibrationToolsPushed(app, event)
            app.DialogApp = CalibrationTools_exported(app, [], app.ProjectRoot); %#ok<ADPROPLC>
        end


        % Value changed function: ColorMapDropDown
        function ColorMapDropDownValueChanged(app, event)
            app.ColMap = app.ColorMapDropDown.Value;
            colormap(app.FigFilmDose, app.ColMap);
            colormap(app.FigDicomDose, app.ColMap);
            colormap(app.UIAxes6, app.ColMap);
        end

        % Value changed function: ColorMapDropDown_3
        function ColorMapDropDown_3ValueChanged(app, event)
            app.ColMap = app.ColorMapDropDown_3.Value;
            colormap(app.CalibrationFigure, app.ColMap);
        end

        % Value changed function: ColorMapDropDown_2
        function ColorMapDropDown_2ValueChanged(app, event)
            app.ColMap = app.ColorMapDropDown_2.Value;
            colormap(app.XZFigure, app.ColMap);
            colormap(app.YZFigure, app.ColMap);
            colormap(app.XYFigure, app.ColMap);
            colormap(app.OriginalFigure, app.ColMap);
            colormap(app.RotatedFigure, app.ColMap);
        end

        % Button pushed function: ContrastButton
        function ContrastButtonPushed(app, event)
            [~, ~, chanls] = size(app.calib_film);

            if ~isempty(app.calib_film) && chanls == 1
                imshow(app.calib_film, []);
                imcontrast;
            end
        end

        % Button pushed function: PickFilmCenterButton_2
        function PickFilmCenterButton_2Pushed(app, event)
            [Pos1, Pos2] = ProfilePoint(app, app.CalibrationFigure);
            Position = [Pos1, Pos2];

            % Translate and update the UI figure
            app.calib_film = fn_MatTransfor(app.calib_film, Position);
            
            % Update center after pick-and-transform
            new_size = size(app.calib_film);
            app.new_center = [(new_size(2)+1)/2, (new_size(1)+1)/2];
            fn_mainImageDisplay(app.calib_film, app.CalibrationFigure, app.ColMap, app.TiffDPI, app.new_center);

            app.HeightCC.Value = new_size(1);
            app.WidthCC.Value = new_size(2);
        end

        % Menu selected function: DosetxtMenu_2
        function DosetxtMenu_2Selected(app, event)
            [DoseMatrix, app.TiffDPI, app.FilmInterpolation,  X_Size, Y_Size] =  fn_TextToDoseParser(app.Path);
            if ~isempty(DoseMatrix)
                app.calib_film = double(DoseMatrix);
                app.HeightCC.Value = Y_Size;
                app.WidthCC.Value = X_Size;

                % Show the image in mm axes starting from corner
                app.new_center = [1, 1];
                fn_mainImageDisplay(app.calib_film, app.CalibrationFigure, app.ColMap, app.TiffDPI, app.new_center);
            else
                return;
            end
        end

        % Menu selected function: FilmtifMenu_2
        function FilmtifMenu_2Selected(app, event)
            [data2, path2] = uigetfile({'*.tif'}, 'Please select the image file', app.Path);
            if isequal(data2, 0)
                return;
            else
                app.Path = path2;
            end
            fullFilePath = fullfile(path2, data2);
            app.calib_film = imread(fullFilePath);
            
            film_info = imfinfo(fullFilePath);

            if film_info.SamplesPerPixel > 3
                app.calib_film = app.calib_film(:, : , 1:3);
            end
            
            app.TiffDPI = fn_DPICalculator(fullFilePath);
            app.new_center = [1, 1]; % Initial corner origin (Global Restoration)
            fn_mainImageDisplay(app.calib_film, app.CalibrationFigure, app.ColMap, app.TiffDPI, app.new_center);

            Img_size = size(app.calib_film);
            app.HeightCC.Value = Img_size(1);
            app.WidthCC.Value = Img_size(2);
            
            CWButtonPushed(app);
            FlipVButtonPushed(app);
        end

        % Menu selected function: RDDosedcmMenu
        function RDDosedcmMenuSelected(app, event)
            app.DicomVolume = [];
            app.RotateDicomVolume = [];

            [file,path] = uigetfile({'*.dcm'}, 'Please select the TPS dose file', app.Path);

            if isequal(file,0)
                return;
            else
                app.Path = path;
                Dicom_file = fullfile(path, file);

                prompt = {'Selected file: ', 'Interp Method: (linear, nearest, cubic)', 'Grid: ' };
                dlgtitle = 'RTDose Import';
                fieldsize = [1 50; 1 40; 1 5];
                definput = {Dicom_file , 'linear', '1' };
                answer = inputdlg(prompt,dlgtitle,fieldsize,definput);
                if isempty(answer)
                    return;
                end
            end

            app.DicomInfo = dicominfo(Dicom_file);
            original_volume = dicomread(app.DicomInfo);
            app.DicomVolume = fn_VolumeInterp(app.DicomInfo, answer{2}, str2double(answer{3}));
            DcmVol_size = size(app.DicomVolume);
            app.TPSInterpolation = str2double(answer{3});
            
            %Patient Info
            app.MRNEditField_2.Value =  app.DicomInfo.PatientID;
            
            % Image properties
            app.PixelSpacing_Y = app.DicomInfo.PixelSpacing(1);
            app.PixelSpacing_X = app.DicomInfo.PixelSpacing(2);
            if isempty(app.DicomInfo.SliceThickness)
                app.SliceThickness = app.DicomInfo.PixelSpacing(1);
            end

            if ~isfield(app.DicomInfo ,'DoseGridScaling')
                app.Grid_scaling = 1;
            else
                app.Grid_scaling = app.DicomInfo.DoseGridScaling;
            end

            if ~isfield(app.DicomInfo, 'ImagePositionPatient')
                app.ImagePP = [0;0;0]; 
            else
                app.ImagePP = app.DicomInfo.ImagePositionPatient(:);
            end
            
            if ~isfield(app.DicomInfo, 'ImageOrientationPatient')
                app.ImageOrent = [1;0;0;0;1;0];
            else
                app.ImageOrent = app.DicomInfo.ImageOrientationPatient(:);
            end

            % Volume dimensions
            app.NumRows = DcmVol_size(1);
            app.NumColumns = DcmVol_size(2);
            app.NumFrames = DcmVol_size(3);

            if ~isfield(app.DicomInfo, 'Manufacturer')
                app.SystemLabel.Text = 'Unknown';
            else
                app.SystemLabel.Text = app.DicomInfo.Manufacturer;
            end            

            PixelSpacing = [app.PixelSpacing_X app.PixelSpacing_Y app.SliceThickness];
            [app.X_dicom_cm, app.Y_dicom_cm, app.Z_dicom_cm] = fn_REF3DExtent(original_volume, app.ImagePP, app.ImageOrent, PixelSpacing);     
            
            app.XmmEditField.Value = double(round(mean(app.X_dicom_cm(:)), 2));
            app.YmmEditField.Value = double(round(mean(app.Y_dicom_cm(:)), 2));
            app.ZmmEditField.Value = double(round(mean(app.Z_dicom_cm(:)), 2));

            app.EditField_6.Value = (app.X_dicom_cm(1));
            app.EditField_7.Value = (app.X_dicom_cm(2));
            app.EditField_8.Value = (app.Y_dicom_cm(1));
            app.EditField_9.Value = (app.Y_dicom_cm(2));
            app.EditField_10.Value = (app.Z_dicom_cm(1));
            app.EditField_11.Value = (app.Z_dicom_cm(2));
            
            app.VolRotStatus = 'false';
            app.CalculateButtonPushed(app);            
        end

        % Menu selected function: PlanDosedcmMenu
        function PlanDosedcmMenuSelected(app, event)
             % Specify the folder containing DICOM files
            [file, path] = uigetfile({'*.dcm'}, 'Please select the film dose file', app.Path);

            if isequal(file,0)
                return;
            else
                dicomFolder = fullfile(path, file);
                app.Path = path;
            end
            TPS_info = dicominfo(dicomFolder);
            app.DicomInfo.PatientID = TPS_info.PatientID;
            app.Grid_scaling = TPS_info.DoseGridScaling;
            app.PixelSpacing_X = TPS_info.PixelSpacing(2);
            app.PixelSpacing_Y = TPS_info.PixelSpacing(1);
            img_center = TPS_info.ImagePositionPatient(:);

            %Patient Info
            app.MRNEditField.Value =  app.DicomInfo.PatientID;
            app.TPSInterpolation = img_center(3);

            %checking if its volume or plane
            plane = squeeze(dicomread(dicomFolder));
            if (size(plane, 3) < 3)
                % Read DICOM volume and information
                app.TPS_dose = fn_AugmentPlanDose(double(plane), [app.PixelSpacing_X, app.PixelSpacing_Y],app.TPSInterpolation, img_center(1), img_center(2));
                
                % Global Restoration: Show DICOM in mm axes relative to its top-left
                virtualDPI = 25.4 / app.PixelSpacing_X;
                fn_mainImageDisplay(app.TPS_dose, app.FigDicomDose, app.ColMap, virtualDPI, [1, 1]);
            else
                h = msgbox("Not a 2D Plan dose!");
                waitfor(h);
                return;
            end

            app.NumRows = TPS_info.Rows;
            app.NumColumns = TPS_info.Columns;
            app.PlanPushed = 1;

            %Reset the values
            app.Xshift_counter = 0; app.Yshift_counter = 0;
            app.XShiftsmmEditField.Value = 0;app.YShiftsmmEditField.Value = 0;
            app.DoseScaledEditField.Value = 0;
        end

        % Menu selected function: FilmDosetxtMenu
        function FilmDosetxtMenuSelected(app, event)
            [DoseMatrix, app.TiffDPI,app.FilmInterpolation, ~, ~, filename] =  fn_TextToDoseParser(app.Path);
            if ~isempty(DoseMatrix)
                    
                app.Film_dose = fn_AugmentFilmDose(DoseMatrix, app.TiffDPI, app.FilmInterpolation);
                app.AllFileName.Value = filename;
    
                % Show the image in mm axes
                app.new_center = [1, 1];
                fn_mainImageDisplay(app.Film_dose(2:end, 2:end), app.FigFilmDose, app.ColMap, app.TiffDPI, app.new_center);
    
                %Reset the values
                app.Xshift_counter = 0; app.Yshift_counter = 0;
                app.XShiftsmmEditField.Value = 0;app.YShiftsmmEditField.Value = 0;
                app.DoseScaledEditField.Value = 0;
            else
                return;
            end
        end

        % Menu selected function: Film2DosetxtMenu
        function Film2DosetxtMenuSelected(app, event)
            saving_film = round(app.calib_film);
            Date = datetime("today");
            film_info = {'Date: ', 'DPI: ', 'Interpolation: ','X Res: ','Y Res: '};
            film_data =  {Date, num2str(app.TiffDPI), num2str(app.EditField_5.Value), num2str(app.WidthCC.Value), num2str(app.HeightCC.Value)};         

            [filename, pathname] = uiputfile({'.txt'}, 'Please select', app.Path);          
            if filename ~= 0       
                app.Path = pathname;
                file_name = fullfile(pathname,filename);  

                txt_ID = fopen(file_name, 'w');
                if txt_ID == -1
                    return;
                end

                for i = 1: numel(film_info)
                    fprintf(txt_ID, '%s%s\n', film_info{i}, film_data{i});
                end
                
                fprintf(txt_ID, '\n');
                fprintf(txt_ID, '\n');
                fprintf(txt_ID, 'Array Start:');
                fprintf(txt_ID, '\n');
                f = waitbar(0, 'Saving the film dose!');
                for i = 1:size(saving_film, 1)
                    waitbar(i/size(saving_film, 2), f, 'Saving the film dose!');
                    fprintf(txt_ID,'%0.0f\t', saving_film(i,:)); 
                    fprintf(txt_ID, '\n');
                end

                waitbar(1, f, 'Done!'); 
                fprintf(txt_ID, '\n');
                fprintf(txt_ID, ':Array End');
                fclose(txt_ID);
            else
            end
        end

        % Menu selected function: XYProfiletxtMenu
        function XYProfiletxtMenuSelected(app, event)
            % Prompt user to select a location to save the data
            [filename, pathname] = uiputfile({'*.txt'}, 'Please select', app.Path);

            % Check if user selected a location
            if filename ~= 0

                app.Path = pathname;
                 % Extract film profiles
                [x_profile, y_profile] = fn_DoseProfile(app.calib_film, app.x_pro, app.y_pro, 'Film', 1, 1, 1);
    
                dis_X = numel(x_profile) *2.54/ app.TiffDPI;
                dis_x = linspace(-dis_X * 0.5, dis_X * 0.5, numel(x_profile)); 
    
                % Calculate distance range for y_profile
                dis_Y = numel(y_profile) *2.54/ app.TiffDPI;
                dis_y = linspace(-dis_Y * 0.5, dis_Y * 0.5, numel(y_profile));
    
                % Construct matrices for export
                X_mat = [dis_x(:), x_profile(:)];
                Y_mat = [dis_y(:), y_profile(:)];


                % Construct file names
                X_file_name = ['XProfile_', filename];
                Y_file_name = ['YProfile_', filename];

                % Write data to files
                writematrix(X_mat, fullfile(pathname, X_file_name));
                writematrix(Y_mat, fullfile(pathname, Y_file_name));
            else
                % If export is canceled
                msgbox('Export canceled!');
            end
        end

        % Menu selected function: PlaneDosedcmMenu
        function PlaneDosedcmMenuSelected(app, event)
            normalzied_data = uint32(app.TPS_dose(2:end, 2:end));
            app.PlaneInfo.DosePlane = app.DropDown_3.Value;
            
            [filename, pathname] = uiputfile({'*.dcm'}, 'Please select', app.Path) ;
            if filename ~= 0
                app.Path = pathname;
                file_name = fullfile(pathname, filename);
                try
                    dicomwrite(normalzied_data,file_name,app.PlaneInfo, 'CreateMode', 'copy' );
                    msgbox('Export sucessful!');
                catch
                end
            else
            end           
        end

        % Menu selected function: RotatedDicomDosedcomMenu
        function RotatedDicomDosedcomMenuSelected(app, event)
            app.saveVolume = app.RotateDicomVolume;
            app.DicomInfo.ImagePositionPatient = app.ImagePP;            

            [filename, pathname] = uiputfile({'*.dcm'}, 'Please select', app.Path);
            if filename ~= 0
                app.Path = pathname;
                [rows, cols, frames] = size(app.saveVolume);
                file_name = fullfile(pathname, filename);
                rotatedVolume = reshape(app.saveVolume, rows, cols, 1, frames);
                try
                    dicomwrite(rotatedVolume,file_name,app.DicomInfo, 'CreateMode', 'copy' );
                catch
                end
            else
                return;
            end
            msgbox('Export sucessful!');
        end

        % Menu selected function: CurrentFilmDosetxtMenu
        function CurrentFilmDosetxtMenuSelected(app, event)
            saving_film = app.Film_dose;
            [filename, pathname] = uiputfile({'.txt'}, 'Please select', app.Path);

            if filename ~= 0
                app.Path = pathname;
                [~, name, ext] = fileparts(filename);
                file_name = fullfile(pathname, [name,'_DPI', num2str(app.TiffDPI), ext]);
                try
                    writematrix(saving_film, file_name );
                    msgbox('Film Dose saved!');
                catch
                end
            else
            end      
        end

        % Value changing function: Slider
        function SliderValueChanging(app, event)
            if ~isempty(app.TPS_dose)
                fn_UpdateContrast(app.Slider, app.FigDicomDose, app.TPS_dose, event.Value, app.ColMap);
            end             
        end

        % Value changing function: Slider_2
        function Slider_2ValueChanging(app, event)
            if ~isempty(app.Film_dose)
                fn_UpdateContrast(app.Slider_2, app.FigFilmDose, app.Film_dose, event.Value, app.ColMap);
            end             
        end

        % Button pushed function: ClearXButton_2
        function ClearXButton_2Pushed(app, event)
        allComponents = findall(app.FigFilmDose);
        
        % Iterate through each component and delete it, except the axes and the image
        for i = 1:length(allComponents)
            % Exclude the axes and image from deletion
            if ~isa(allComponents(i), 'matlab.graphics.axis.Axes') && ...
               ~isa(allComponents(i), 'matlab.graphics.primitive.Image')
                delete(allComponents(i));
            end
        end
        end

        % Menu selected function: DicomDummyMenu
        function DicomDummyMenuSelected(app, event)
            app.DialogApp = DicomDummy_exported(app, app.Path);
        end

        % Menu selected function: SynchonicityMenu
        function SynchonicityMenuSelected(app, event)
            app.DialogApp = Synchro_exported(app, app.Path);
        end

        % Menu selected function: StarShotsMenu
        function StarShotsMenuSelected(app, event)
            app.DialogApp = StartShots_exported(app, app.Path);
        end

        % Value changing function: ContrastSlider
        function ContrastSliderValueChanging(app, event)
            changingValue = event.Value;
            if ~isempty(app.calib_film)
                fn_UpdateContrast(app.ContrastSlider, app.CalibrationFigure, app.calib_film,changingValue,app.ColMap);
            end  
        end

        % Menu selected function: JawSizeMenu
        function JawSizeMenuSelected(app, event)
            if isempty(app.calib_film) || size(app.calib_film, 3) > 2
                h = msgbox("No profile detected!");
                waitfor(h);
                return;
            end

            wtbar = waitbar(0, 'Please wait...');
            FWHM_dose = fn_AugmentFilmDose(app.calib_film, app.TiffDPI, app.FilmInterpolation);
            pause(0.5);
            waitbar(0.25, wtbar, 'Reading the film dose');
            app.DialogApp = FWHM_JawSize_exported(app, FWHM_dose, wtbar);
        end
    end

    % Component initialization
    methods (Access = private)

        % Create UIFigure and components
        function createComponents(app)

            % Get the file path for locating images
            pathToMLAPP = fileparts(mfilename('fullpath'));

            % Create FilmDosimetryUIFigure and hide until all components are created
            app.FilmDosimetryUIFigure = uifigure('Visible', 'off');
            app.FilmDosimetryUIFigure.NumberTitle = 'on';
            app.FilmDosimetryUIFigure.Position = [100 100 1439 893];
            app.FilmDosimetryUIFigure.Name = 'FilmDosimetry';
            app.FilmDosimetryUIFigure.AutoResizeChildren = 'off';
            app.FilmDosimetryUIFigure.Icon = fullfile(pathToMLAPP, 'dlgapps', 'dlgapps_resources', 'icons8-medical-80.png');
            app.FilmDosimetryUIFigure.CloseRequestFcn = createCallbackFcn(app, @MainAppCloseRequest, true);
            % Start maximized when supported
            if isprop(app.FilmDosimetryUIFigure, 'WindowState')
                app.FilmDosimetryUIFigure.WindowState = 'maximized';
            end

            % Create FileMenu
            app.FileMenu = uimenu(app.FilmDosimetryUIFigure);
            app.FileMenu.Text = 'File';

            % Create ImportMenu
            app.ImportMenu = uimenu(app.FileMenu);
            app.ImportMenu.Text = 'Import';

            % Create CalibrationImportMenu_2
            app.CalibrationImportMenu_2 = uimenu(app.ImportMenu);
            app.CalibrationImportMenu_2.Text = 'Calibration Import';

            % Create FilmtifMenu_2
            app.FilmtifMenu_2 = uimenu(app.CalibrationImportMenu_2);
            app.FilmtifMenu_2.MenuSelectedFcn = createCallbackFcn(app, @FilmtifMenu_2Selected, true);
            app.FilmtifMenu_2.Text = 'Film(*.tif)';

            % Create DosetxtMenu_2
            app.DosetxtMenu_2 = uimenu(app.CalibrationImportMenu_2);
            app.DosetxtMenu_2.MenuSelectedFcn = createCallbackFcn(app, @DosetxtMenu_2Selected, true);
            app.DosetxtMenu_2.Text = 'Dose(*.txt)';

            % Create DicomImportMenu_2
            app.DicomImportMenu_2 = uimenu(app.ImportMenu);
            app.DicomImportMenu_2.Text = 'Dicom Import';

            % Create RDDosedcmMenu
            app.RDDosedcmMenu = uimenu(app.DicomImportMenu_2);
            app.RDDosedcmMenu.MenuSelectedFcn = createCallbackFcn(app, @RDDosedcmMenuSelected, true);
            app.RDDosedcmMenu.Text = 'RD Dose (*.dcm)';

            % Create AnalysisImportMenu
            app.AnalysisImportMenu = uimenu(app.ImportMenu);
            app.AnalysisImportMenu.Text = 'Analysis Import';

            % Create PlanDosedcmMenu
            app.PlanDosedcmMenu = uimenu(app.AnalysisImportMenu);
            app.PlanDosedcmMenu.MenuSelectedFcn = createCallbackFcn(app, @PlanDosedcmMenuSelected, true);
            app.PlanDosedcmMenu.Text = 'Plan Dose (*.dcm)';

            % Create FilmDosetxtMenu
            app.FilmDosetxtMenu = uimenu(app.AnalysisImportMenu);
            app.FilmDosetxtMenu.MenuSelectedFcn = createCallbackFcn(app, @FilmDosetxtMenuSelected, true);
            app.FilmDosetxtMenu.Text = 'Film Dose (*.txt)';

            % Create ExportMenu
            app.ExportMenu = uimenu(app.FileMenu);
            app.ExportMenu.Text = 'Export';

            % Create CalibrationExportMenu
            app.CalibrationExportMenu = uimenu(app.ExportMenu);
            app.CalibrationExportMenu.Text = 'Calibration Export';

            % Create Film2DosetxtMenu
            app.Film2DosetxtMenu = uimenu(app.CalibrationExportMenu);
            app.Film2DosetxtMenu.MenuSelectedFcn = createCallbackFcn(app, @Film2DosetxtMenuSelected, true);
            app.Film2DosetxtMenu.Text = 'Film2Dose (*.txt)';

            % Create XYProfiletxtMenu
            app.XYProfiletxtMenu = uimenu(app.CalibrationExportMenu);
            app.XYProfiletxtMenu.MenuSelectedFcn = createCallbackFcn(app, @XYProfiletxtMenuSelected, true);
            app.XYProfiletxtMenu.Text = 'X-Y Profile (*.txt)';

            % Create DicomExportMenu
            app.DicomExportMenu = uimenu(app.ExportMenu);
            app.DicomExportMenu.Text = 'Dicom Export';

            % Create PlaneDosedcmMenu
            app.PlaneDosedcmMenu = uimenu(app.DicomExportMenu);
            app.PlaneDosedcmMenu.MenuSelectedFcn = createCallbackFcn(app, @PlaneDosedcmMenuSelected, true);
            app.PlaneDosedcmMenu.Text = 'Plane Dose (*.dcm)';

            % Create RotatedDicomDosedcomMenu
            app.RotatedDicomDosedcomMenu = uimenu(app.DicomExportMenu);
            app.RotatedDicomDosedcomMenu.MenuSelectedFcn = createCallbackFcn(app, @RotatedDicomDosedcomMenuSelected, true);
            app.RotatedDicomDosedcomMenu.Text = 'Rotated Dicom Dose (*.dcom)';

            % Create AnalysisExportMenu
            app.AnalysisExportMenu = uimenu(app.ExportMenu);
            app.AnalysisExportMenu.Text = 'Analysis Export';

            % Create CurrentFilmDosetxtMenu
            app.CurrentFilmDosetxtMenu = uimenu(app.AnalysisExportMenu);
            app.CurrentFilmDosetxtMenu.MenuSelectedFcn = createCallbackFcn(app, @CurrentFilmDosetxtMenuSelected, true);
            app.CurrentFilmDosetxtMenu.Text = 'Current Film Dose (*.txt)';

            % Create ToolsMenu
            app.ToolsMenu = uimenu(app.FilmDosimetryUIFigure);
            app.ToolsMenu.Text = 'Tools';

            % Create DicomDummyMenu
            app.DicomDummyMenu = uimenu(app.ToolsMenu);
            app.DicomDummyMenu.MenuSelectedFcn = createCallbackFcn(app, @DicomDummyMenuSelected, true);
            app.DicomDummyMenu.Text = 'DicomDummy';

            % Create SynchonicityMenu
            app.SynchonicityMenu = uimenu(app.ToolsMenu);
            app.SynchonicityMenu.MenuSelectedFcn = createCallbackFcn(app, @SynchonicityMenuSelected, true);
            app.SynchonicityMenu.Text = 'Synchonicity';

            % Create StarShotsMenu
            app.StarShotsMenu = uimenu(app.ToolsMenu);
            app.StarShotsMenu.MenuSelectedFcn = createCallbackFcn(app, @StarShotsMenuSelected, true);
            app.StarShotsMenu.Text = 'StarShots';

            % Create JawSizeMenu
            app.JawSizeMenu = uimenu(app.ToolsMenu);
            app.JawSizeMenu.MenuSelectedFcn = createCallbackFcn(app, @JawSizeMenuSelected, true);
            app.JawSizeMenu.Text = 'JawSize';

            % Create CalibrationToolsMenu (parent – no direct callback)
            app.CalibrationToolsMenu = uimenu(app.FilmDosimetryUIFigure);
            app.CalibrationToolsMenu.Text = 'Calibration';

            % Create OpenCalibrationToolsMenu (child submenu item)
            app.OpenCalibrationToolsMenu = uimenu(app.CalibrationToolsMenu);
            app.OpenCalibrationToolsMenu.MenuSelectedFcn = createCallbackFcn(app, @CalibrationToolsPushed, true);
            app.OpenCalibrationToolsMenu.Text = 'Open Calibration Tools...';

            % Create FigureGrid - makes TabGroup fill the entire window
            app.FigureGrid = uigridlayout(app.FilmDosimetryUIFigure, [1, 1]);
            app.FigureGrid.Padding = [14 5 10 0];

            % Create TabGroup
            app.TabGroup = uitabgroup(app.FigureGrid);
            app.TabGroup.TabLocation = 'bottom';

            % Create CalibrationTab
            app.CalibrationTab = uitab(app.TabGroup);
            app.CalibrationTab.Title = 'Calibration';
            app.CalibrationTab.BackgroundColor = [0.91, 0.92, 0.94];

            % Create DicomDoseViewerTab
            app.DicomDoseViewerTab = uitab(app.TabGroup);
            app.DicomDoseViewerTab.Title = 'Dicom Dose Viewer';
            app.DicomDoseViewerTab.BackgroundColor = [0.902 0.902 0.902];

            % Create AnalysisTab
            app.AnalysisTab = uitab(app.TabGroup);
            app.AnalysisTab.Title = 'Analysis';
            app.AnalysisTab.BackgroundColor = [0.902 0.902 0.902];

            % Create AboutTab
            app.AboutTab = uitab(app.TabGroup);
            app.AboutTab.Title = 'About';
            app.AboutTab.BackgroundColor = [0.9412 0.9412 0.9412];

            % --- CALIBRATION TAB CONTENT ---
            % Create CalibrationGrid
            app.CalibrationGrid = uigridlayout(app.CalibrationTab, [2, 3]);
            app.CalibrationGrid.RowHeight = {30, '1x'};
            app.CalibrationGrid.ColumnWidth = {349, '1x', 341};
            app.CalibrationGrid.Padding = [10 10 10 10];

            % Row 1: Top Bar (Color Map)
            app.Panel_32 = uipanel(app.CalibrationGrid);
            app.Panel_32.Layout.Row = 1;
            app.Panel_32.Layout.Column = [1 3];
            app.Panel_32.BackgroundColor = [0.91, 0.92, 0.94];
            panel32Grid = uigridlayout(app.Panel_32, [1 2]);
            panel32Grid.ColumnWidth = {'fit', 100};
            panel32Grid.Padding = [5 2 5 2];

            app.ColorMapDropDown_3Label = uilabel(panel32Grid);
            app.ColorMapDropDown_3Label.HorizontalAlignment = 'right';
            app.ColorMapDropDown_3Label.Text = 'Color Map';

            app.ColorMapDropDown_3 = uidropdown(panel32Grid);
            app.ColorMapDropDown_3.Items = {'jet', 'parula', 'hsv', 'hot', 'cool', 'spring', 'summer', 'bone', 'gray', 'sky', 'lines', 'flag', 'white', 'prism'};
            app.ColorMapDropDown_3.ValueChangedFcn = createCallbackFcn(app, @ColorMapDropDown_3ValueChanged, true);
            app.ColorMapDropDown_3.Value = 'bone';

            % Row 2, Column 1: Settings Panel
            app.CenterPanelGrid = uigridlayout(app.CalibrationGrid, [3, 1]); % Left column inner grid
            app.CenterPanelGrid.Layout.Row = 2;
            app.CenterPanelGrid.Layout.Column = 1;
            app.CenterPanelGrid.RowHeight = {'1x', '1x', '1x'};
            app.CenterPanelGrid.Padding = [0 0 0 0];

            % 1. Calibration Info Panel
            app.Panel_37 = uipanel(app.CenterPanelGrid);
            app.Panel_37.Title = 'Calibration Info';
            app.Panel_37.FontWeight = 'bold';
            app.Panel_37.Layout.Row = 1;
            app.Panel_37.BackgroundColor = [0.91, 0.92, 0.94];
            infoGrid = uigridlayout(app.Panel_37, [3, 3]);
            infoGrid.RowHeight = {22, 22, '1x'};
            infoGrid.ColumnWidth = {'fit', '1x', 80};
            infoGrid.Padding = [5 5 5 5];

            % Row 1: Custom Config dropdown + Refresh button
            app.CustomConfigDropDownLabel = uilabel(infoGrid);
            app.CustomConfigDropDownLabel.Layout.Row = 1;
            app.CustomConfigDropDownLabel.Layout.Column = 1;
            app.CustomConfigDropDownLabel.HorizontalAlignment = 'right';
            app.CustomConfigDropDownLabel.Text = 'Custom:';
            app.CustomConfigDropDown = uidropdown(infoGrid);
            app.CustomConfigDropDown.Layout.Row = 1;
            app.CustomConfigDropDown.Layout.Column = 2;
            app.CustomConfigDropDown.Items = {'--- Select ---'};
            app.CustomConfigDropDown.ValueChangedFcn = createCallbackFcn(app, @CustomConfigDropDownValueChanged, true);
            app.CustomConfigDropDown.Value = '--- Select ---';
            app.RefreshListButton = uibutton(infoGrid, 'push');
            app.RefreshListButton.Layout.Row = 1;
            app.RefreshListButton.Layout.Column = 3;
            app.RefreshListButton.ButtonPushedFcn = createCallbackFcn(app, @RefreshListButtonPushed, true);
            app.RefreshListButton.Text = '⟳ Refresh';

            % Row 2: Status
            app.StatusLabel = uilabel(infoGrid);
            app.StatusLabel.Layout.Row = 2;
            app.StatusLabel.Layout.Column = 1;
            app.StatusLabel.HorizontalAlignment = 'right';
            app.StatusLabel.Text = 'Status:';
            app.NotReadyLabel = uilabel(infoGrid);
            app.NotReadyLabel.Layout.Row = 2;
            app.NotReadyLabel.Layout.Column = [2 3];
            app.NotReadyLabel.BackgroundColor = [1 0 0];
            app.NotReadyLabel.HorizontalAlignment = 'center';
            app.NotReadyLabel.FontWeight = 'bold';
            app.NotReadyLabel.Text = 'Not Ready!';

            % Row 3: TextArea
            app.TextArea = uitextarea(infoGrid);
            app.TextArea.Layout.Row = 3;
            app.TextArea.Layout.Column = [1 3];

            % 2. Transform and Align Panel
            app.Panel_30 = uipanel(app.CenterPanelGrid);
            app.Panel_30.Title = 'Transform and Align';
            app.Panel_30.FontWeight = 'bold';
            app.Panel_30.Layout.Row = 2;
            app.Panel_30.BackgroundColor = [0.91, 0.92, 0.94];
            transGrid = uigridlayout(app.Panel_30, [4, 4]);
            transGrid.RowHeight = {30, 30, 30, 30};
            transGrid.ColumnWidth = {'fit', 60, '1x', '1x'};
            transGrid.Padding = [5 5 5 5];

            app.CWButton = uibutton(transGrid, 'push');
            app.CWButton.Layout.Row = 1;
            app.CWButton.Layout.Column = 1;
            app.CWButton.ButtonPushedFcn = createCallbackFcn(app, @CWButtonPushed, true);
            app.CWButton.Text = 'CW';
            app.CCWButton = uibutton(transGrid, 'push');
            app.CCWButton.Layout.Row = 1;
            app.CCWButton.Layout.Column = 2;
            app.CCWButton.ButtonPushedFcn = createCallbackFcn(app, @CCWButtonPushed, true);
            app.CCWButton.Text = 'CCW';
            app.FilpHButton = uibutton(transGrid, 'push');
            app.FilpHButton.Layout.Row = 1;
            app.FilpHButton.Layout.Column = 3;
            app.FilpHButton.ButtonPushedFcn = createCallbackFcn(app, @FilpHButtonPushed, true);
            app.FilpHButton.Text = 'Flip H';
            app.FlipVButton = uibutton(transGrid, 'push');
            app.FlipVButton.Layout.Row = 1;
            app.FlipVButton.Layout.Column = 4;
            app.FlipVButton.ButtonPushedFcn = createCallbackFcn(app, @FlipVButtonPushed, true);
            app.FlipVButton.Text = 'Flip V';

            % Row 2: Alignment Tools (Moved from Dose & Tools)
            app.AutoAlignImageButton = uibutton(transGrid, 'push');
            app.AutoAlignImageButton.Layout.Row = 2;
            app.AutoAlignImageButton.Layout.Column = 1;
            app.AutoAlignImageButton.ButtonPushedFcn = createCallbackFcn(app, @AutoAlignImageButtonPushed, true);
            app.AutoAlignImageButton.Text = 'Auto Align';
            
            app.AutoCenterButton = uibutton(transGrid, 'push');
            app.AutoCenterButton.Layout.Row = 2;
            app.AutoCenterButton.Layout.Column = 2;
            app.AutoCenterButton.ButtonPushedFcn = createCallbackFcn(app, @AutoCenterButtonPushed, true);
            app.AutoCenterButton.Text = 'Auto Center';

            app.ManuallyAlignButton = uibutton(transGrid, 'push');
            app.ManuallyAlignButton.Layout.Row = 2;
            app.ManuallyAlignButton.Layout.Column = 3;
            app.ManuallyAlignButton.ButtonPushedFcn = createCallbackFcn(app, @ManuallyAlignButtonPushed, true);
            app.ManuallyAlignButton.Text = 'Manual Align';

            app.PickFilmCenterButton_2 = uibutton(transGrid, 'push');
            app.PickFilmCenterButton_2.Layout.Row = 2;
            app.PickFilmCenterButton_2.Layout.Column = 4;
            app.PickFilmCenterButton_2.ButtonPushedFcn = createCallbackFcn(app, @PickFilmCenterButton_2Pushed, true);
            app.PickFilmCenterButton_2.Text = 'Pick Center';

            app.WidthLabel = uilabel(transGrid);
            app.WidthLabel.Layout.Row = 3;
            app.WidthLabel.Layout.Column = 1;
            app.WidthLabel.Text = 'Width:';
            app.WidthCC = uieditfield(transGrid, 'numeric');
            app.WidthCC.Layout.Row = 3;
            app.WidthCC.Layout.Column = 2;
            app.CenterCropButton = uibutton(transGrid, 'push');
            app.CenterCropButton.Layout.Row = 3;
            app.CenterCropButton.Layout.Column = [3 4];
            app.CenterCropButton.ButtonPushedFcn = createCallbackFcn(app, @CenterCropButtonPushed, true);
            app.CenterCropButton.Text = 'Center Crop';

            app.HeightLabel = uilabel(transGrid);
            app.HeightLabel.Layout.Row = 4;
            app.HeightLabel.Layout.Column = 1;
            app.HeightLabel.Text = 'Height:';
            app.HeightCC = uieditfield(transGrid, 'numeric');
            app.HeightCC.Layout.Row = 4;
            app.HeightCC.Layout.Column = 2;
            app.ManualCropButton = uibutton(transGrid, 'push');
            app.ManualCropButton.Layout.Row = 4;
            app.ManualCropButton.Layout.Column = [3 4];
            app.ManualCropButton.ButtonPushedFcn = createCallbackFcn(app, @ManualCropButtonPushed, true);
            app.ManualCropButton.Text = 'Manual Crop';

            % 3. Filters Panel
            app.Panel_31 = uipanel(app.CenterPanelGrid);
            app.Panel_31.Title = 'Filters';
            app.Panel_31.FontWeight = 'bold';
            app.Panel_31.Layout.Row = 3;
            app.Panel_31.BackgroundColor = [0.91, 0.92, 0.94];
            filterGrid = uigridlayout(app.Panel_31, [5, 4]);
            filterGrid.RowHeight = {30, 30, 30, 30, 30};
            filterGrid.ColumnWidth = {'1x', 'fit', 'fit', 'fit'};
            filterGrid.Padding = [10 5 10 5];

            % Row 1: ROI
            app.ROILabel = uilabel(filterGrid);
            app.ROILabel.Layout.Row = 1;
            app.ROILabel.Layout.Column = 1;
            app.ROILabel.FontAngle = 'italic';
            app.ROILabel.Text = 'Background noise rem...';
            app.ROIFilterButton = uibutton(filterGrid, 'push');
            app.ROIFilterButton.Layout.Row = 1;
            app.ROIFilterButton.Layout.Column = 2;
            app.ROIFilterButton.ButtonPushedFcn = createCallbackFcn(app, @ROIFilterButtonPushed, true);
            app.ROIFilterButton.Text = 'ROI Filter';
            
            % ROI Size Label
            roiSizeLabel = uilabel(filterGrid);
            roiSizeLabel.Layout.Row = 1;
            roiSizeLabel.Layout.Column = 3;
            roiSizeLabel.HorizontalAlignment = 'right';
            roiSizeLabel.Text = 'Size:';

            app.SizeEditField = uieditfield(filterGrid, 'numeric');
            app.SizeEditField.Layout.Row = 1;
            app.SizeEditField.Layout.Column = 4;
            app.SizeEditField.Value = 1;

            % Row 2: Median
            app.MedianLabel = uilabel(filterGrid);
            app.MedianLabel.Layout.Row = 2;
            app.MedianLabel.Layout.Column = 1;
            app.MedianLabel.FontAngle = 'italic';
            app.MedianLabel.Text = 'Despeckle/Point noise';
            app.MedianFilterButton = uibutton(filterGrid, 'push');
            app.MedianFilterButton.Layout.Row = 2;
            app.MedianFilterButton.Layout.Column = 2;
            app.MedianFilterButton.ButtonPushedFcn = createCallbackFcn(app, @MedianFilterButtonPushed, true);
            app.MedianFilterButton.Text = 'Median';
            app.MedianSizeLabel = uilabel(filterGrid);
            app.MedianSizeLabel.Layout.Row = 2;
            app.MedianSizeLabel.Layout.Column = 3;
            app.MedianSizeLabel.HorizontalAlignment = 'right';
            app.MedianSizeLabel.Text = 'Size:';
            app.MedianSizeEditField = uieditfield(filterGrid, 'numeric');
            app.MedianSizeEditField.Layout.Row = 2;
            app.MedianSizeEditField.Layout.Column = 4;
            app.MedianSizeEditField.Value = 3;

            % Row 3: Noise
            app.NoiseLabel = uilabel(filterGrid);
            app.NoiseLabel.Layout.Row = 3;
            app.NoiseLabel.Layout.Column = 1;
            app.NoiseLabel.FontAngle = 'italic';
            app.NoiseLabel.Text = 'Adaptive reduction';
            app.FilterNoiseButton = uibutton(filterGrid, 'push');
            app.FilterNoiseButton.Layout.Row = 3;
            app.FilterNoiseButton.Layout.Column = 2;
            app.FilterNoiseButton.ButtonPushedFcn = createCallbackFcn(app, @FilterNoiseButtonPushed, true);
            app.FilterNoiseButton.Text = 'Noise F.';
            app.NoiseThresholdLabel = uilabel(filterGrid);
            app.NoiseThresholdLabel.Layout.Row = 3;
            app.NoiseThresholdLabel.Layout.Column = 3;
            app.NoiseThresholdLabel.HorizontalAlignment = 'right';
            app.NoiseThresholdLabel.Text = 'Thresh:';
            app.FilterNoiseEditField = uieditfield(filterGrid, 'numeric');
            app.FilterNoiseEditField.Layout.Row = 3;
            app.FilterNoiseEditField.Layout.Column = 4;
            app.FilterNoiseEditField.Value = 3000;

            % Row 4: Smooth
            app.SmoothLabel = uilabel(filterGrid);
            app.SmoothLabel.Layout.Row = 4;
            app.SmoothLabel.Layout.Column = 1;
            app.SmoothLabel.FontAngle = 'italic';
            app.SmoothLabel.Text = 'Edge softening';
            app.SmoothButton = uibutton(filterGrid, 'push');
            app.SmoothButton.Layout.Row = 4;
            app.SmoothButton.Layout.Column = 2;
            app.SmoothButton.ButtonPushedFcn = createCallbackFcn(app, @SmoothButtonPushed, true);
            app.SmoothButton.Text = 'Smooth';
            app.SmoothDropDown_2 = uidropdown(filterGrid);
            app.SmoothDropDown_2.Layout.Row = 4;
            app.SmoothDropDown_2.Layout.Column = 3;
            app.SmoothDropDown_2.Items = {'None', 'Average', 'Median', 'Gaussian', 'Lowess', 'Loess', 'Rlowess', 'Rloess'};
            app.SmoothDropDown_2.Value = 'Gaussian';
            app.SmoothWIn_2 = uieditfield(filterGrid, 'numeric');
            app.SmoothWIn_2.Layout.Row = 4;
            app.SmoothWIn_2.Layout.Column = 4;
            app.SmoothWIn_2.Value = 1;

            % Row 5: Interpolation
            app.InterpLabel = uilabel(filterGrid);
            app.InterpLabel.Layout.Row = 5;
            app.InterpLabel.Layout.Column = 1;
            app.InterpLabel.FontAngle = 'italic';
            app.InterpLabel.Text = 'Density upsampling';
            app.InterpolationButton = uibutton(filterGrid, 'push');
            app.InterpolationButton.Layout.Row = 5;
            app.InterpolationButton.Layout.Column = 2;
            app.InterpolationButton.ButtonPushedFcn = createCallbackFcn(app, @InterpolationButtonPushed, true);
            app.InterpolationButton.Text = 'Interp.';
            app.DropDown_7 = uidropdown(filterGrid);
            app.DropDown_7.Layout.Row = 5;
            app.DropDown_7.Layout.Column = 3;
            app.DropDown_7.Items = {'linear', 'spline', 'cubic'};
            app.EditField_5 = uieditfield(filterGrid, 'numeric');
            app.EditField_5.Layout.Row = 5;
            app.EditField_5.Layout.Column = 4;
            app.EditField_5.Value = 1;

            % Row 2, Column 2: Visualization Panel
            app.Panel_20 = uipanel(app.CalibrationGrid);
            app.Panel_20.Layout.Row = 2;
            app.Panel_20.Layout.Column = 2;
            app.Panel_20.BackgroundColor = [0.91, 0.92, 0.94];
            vizGrid = uigridlayout(app.Panel_20, [1, 1]);
            app.CalibrationFigure = uiaxes(vizGrid);
            app.CalibrationFigure.Box = 'on';

            % Row 2, Column 3: Analysis Panel
            app.RightPanelGrid = uigridlayout(app.CalibrationGrid, [4, 1]);
            app.RightPanelGrid.Layout.Row = 2;
            app.RightPanelGrid.Layout.Column = 3;
            app.RightPanelGrid.RowHeight = {120, '1x', '1x', '1x'};
            app.RightPanelGrid.Padding = [0 0 0 0];

            % 1. Profile Controls
            app.ButtonGroup_2 = uibuttongroup(app.RightPanelGrid);
            app.ButtonGroup_2.Title = 'Profile Tools';
            app.ButtonGroup_2.Layout.Row = 1;
            app.ButtonGroup_2.BackgroundColor = [0.91, 0.92, 0.94];

            app.NormalizeCheckBox = uicheckbox(app.ButtonGroup_2);
            app.NormalizeCheckBox.Text = 'Normalize';
            app.NormalizeCheckBox.Position = [41 70 76 22];

            app.FilmProfileButton = uibutton(app.ButtonGroup_2, 'push');
            app.FilmProfileButton.ButtonPushedFcn = createCallbackFcn(app, @FilmProfileButtonPushed, true);
            app.FilmProfileButton.Text = 'Film Profile';
            app.FilmProfileButton.Position = [204 15 84 34];

            app.CenterButton = uiradiobutton(app.ButtonGroup_2);
            app.CenterButton.Text = 'Center';
            app.CenterButton.Value = true;
            app.CenterButton.Position = [43 41 66 22];

            app.ManualButton = uiradiobutton(app.ButtonGroup_2);
            app.ManualButton.Text = 'Manual';
            app.ManualButton.Position = [42 15 70 22];

            % 2. Y Profile
            app.UIAxes9_2 = uiaxes(app.RightPanelGrid);
            app.UIAxes9_2.Layout.Row = 2;
            title(app.UIAxes9_2, 'Y Profile');
            app.UIAxes9_2.Box = 'on';

            % 3. X Profile
            app.UIAxes9 = uiaxes(app.RightPanelGrid);
            app.UIAxes9.Layout.Row = 3;
            title(app.UIAxes9, 'X Profile');
            app.UIAxes9.Box = 'on';

            % 4. Film2Dose & Measurement
            app.Panel_29 = uipanel(app.RightPanelGrid);
            app.Panel_29.Title = 'Dose & Tools';
            app.Panel_29.Layout.Row = 4;
            app.Panel_29.BackgroundColor = [0.91, 0.92, 0.94];
            doseGrid = uigridlayout(app.Panel_29, [4, 2]);
            doseGrid.RowHeight = {30, 30, 30, 45}; 
            doseGrid.Padding = [5 5 5 5];

            % Row 1: Dose Calculation
            app.ConverttodoseButton = uibutton(doseGrid, 'push');
            app.ConverttodoseButton.Layout.Row = 1;
            app.ConverttodoseButton.Layout.Column = 1;
            app.ConverttodoseButton.ButtonPushedFcn = createCallbackFcn(app, @ConverttodoseButtonPushed, true);
            app.ConverttodoseButton.BackgroundColor = [0.8 0.9 0.7];
            app.ConverttodoseButton.Text = 'Convert to Dose';
            app.UpdateFilmDoseButton = uibutton(doseGrid, 'push');
            app.UpdateFilmDoseButton.Layout.Row = 1;
            app.UpdateFilmDoseButton.Layout.Column = 2;
            app.UpdateFilmDoseButton.ButtonPushedFcn = createCallbackFcn(app, @UpdateFilmDoseButtonPushed, true);
            app.UpdateFilmDoseButton.Text = 'Update Dose';

            % Row 2: ROI Measurements
            app.ROIDoseButton = uibutton(doseGrid, 'push');
            app.ROIDoseButton.Layout.Row = 2;
            app.ROIDoseButton.Layout.Column = 1;
            app.ROIDoseButton.ButtonPushedFcn = createCallbackFcn(app, @ROIDoseButtonPushed, true);
            app.ROIDoseButton.Text = 'ROI Dose';
            app.cGyEditField = uieditfield(doseGrid, 'numeric');
            app.cGyEditField.Layout.Row = 2;
            app.cGyEditField.Layout.Column = 2;
            app.cGyEditField.Editable = 'off';

            % Row 3-4: Contrast Control
            app.ContrastSlider = uislider(doseGrid, 'range');
            app.ContrastSlider.Layout.Row = 3;
            app.ContrastSlider.Layout.Column = [1 2];
            app.ContrastSlider.ValueChangingFcn = createCallbackFcn(app, @ContrastSliderValueChanging, true);
            app.ContrastButton = uibutton(doseGrid, 'push');
            app.ContrastButton.Layout.Row = 4;
            app.ContrastButton.Layout.Column = [1 2];
            app.ContrastButton.ButtonPushedFcn = createCallbackFcn(app, @ContrastButtonPushed, true);
            app.ContrastButton.Text = 'Contrast';

            % Create DicomGrid - responsive layout for Dicom Dose Viewer tab
            app.DicomGrid = uigridlayout(app.DicomDoseViewerTab, [3, 3]);
            app.DicomGrid.RowHeight = {30, '1x', 326};
            app.DicomGrid.ColumnWidth = {'1x', '1x', '1x'};
            app.DicomGrid.RowSpacing = 5;
            app.DicomGrid.ColumnSpacing = 5;
            app.DicomGrid.Padding = [11 17 11 5];

            % Create Panel_33
            app.Panel_33 = uipanel(app.DicomGrid);
            app.Panel_33.Layout.Row = 1;
            app.Panel_33.Layout.Column = [1 3];
            app.Panel_33.BackgroundColor = [0.8 0.8 0.8];
            dicomTopGrid = uigridlayout(app.Panel_33,[1 2]);
            dicomTopGrid.RowHeight = {'fit'};
            dicomTopGrid.ColumnWidth = {'fit','fit'};
            dicomTopGrid.ColumnSpacing = 6;
            dicomTopGrid.Padding = [6 4 6 4];

            % Create ColorMapDropDown_2Label
            app.ColorMapDropDown_2Label = uilabel(dicomTopGrid);
            app.ColorMapDropDown_2Label.HorizontalAlignment = 'right';
            app.ColorMapDropDown_2Label.Layout.Row = 1;
            app.ColorMapDropDown_2Label.Layout.Column = 1;
            app.ColorMapDropDown_2Label.Position = [4 5 55 18];
            app.ColorMapDropDown_2Label.Text = 'Color Map';

            % Create ColorMapDropDown_2
            app.ColorMapDropDown_2 = uidropdown(dicomTopGrid);
            app.ColorMapDropDown_2.Items = {'jet', 'parula', 'hsv', 'hot', 'cool', 'spring', 'summer', 'bone', 'gray', 'sky', 'lines', 'flag', 'white', 'prism'};
            app.ColorMapDropDown_2.ValueChangedFcn = createCallbackFcn(app, @ColorMapDropDown_2ValueChanged, true);
            app.ColorMapDropDown_2.Layout.Row = 1;
            app.ColorMapDropDown_2.Layout.Column = 2;
            app.ColorMapDropDown_2.Position = [60 5 70 20];
            app.ColorMapDropDown_2.FontSize = 10;
            app.ColorMapDropDown_2.Value = 'bone';

            % Create MaxPlaneDoseCheckBox
            app.MaxPlaneDoseCheckBox = uicheckbox(app.Panel_33);
            app.MaxPlaneDoseCheckBox.Text = 'Max Plane Dose';
            app.MaxPlaneDoseCheckBox.Position = [158 6 124 22];

            % Create MRNLabel_2
            app.MRNLabel_2 = uilabel(app.Panel_33);
            app.MRNLabel_2.FontWeight = 'bold';
            app.MRNLabel_2.Position = [1154 8 36 13];
            app.MRNLabel_2.Text = 'MRN:';

            % Create MRNEditField_2
            app.MRNEditField_2 = uieditfield(app.Panel_33, 'text');
            app.MRNEditField_2.BackgroundColor = [0.9412 0.9412 0.9412];
            app.MRNEditField_2.Position = [1195 5 152 18];

            % Create Button_9
            app.Button_9 = uibutton(app.Panel_33, 'push');
            app.Button_9.ButtonPushedFcn = createCallbackFcn(app, @Button_9Pushed, true);
            app.Button_9.Icon = fullfile(pathToMLAPP, 'ml_resources', 'Graphics', 'icons8-info-50.png');
            app.Button_9.Tooltip = {'Dicom Info'};
            app.Button_9.Position = [1355 5 24 21];
            app.Button_9.Text = '';

            % Create Panel_11
            app.Panel_11 = uipanel(app.DicomGrid);
            app.Panel_11.Layout.Row = 2;
            app.Panel_11.Layout.Column = 1;
            app.Panel_11.BackgroundColor = [0.8 0.8 0.8];

            % Create YZFigure
            app.YZFigure = uiaxes(app.Panel_11);
            app.YZFigure.AmbientLightColor = [0.902 0.902 0.902];
            app.YZFigure.XAxisLocation = 'origin';
            app.YZFigure.XTick = [];
            app.YZFigure.YAxisLocation = 'origin';
            app.YZFigure.YTick = [];
            app.YZFigure.Box = 'on';
            app.YZFigure.TickDir = 'none';
            app.YZFigure.Position = [3 67 432 395];

            % Create GyEditField_3
            app.GyEditField_3 = uieditfield(app.Panel_11, 'numeric');
            app.GyEditField_3.Position = [360 395 40 22];

            % Create GyEditField_3Label
            app.GyEditField_3Label = uilabel(app.Panel_11);
            app.GyEditField_3Label.HorizontalAlignment = 'right';
            app.GyEditField_3Label.Position = [403 395 28 22];
            app.GyEditField_3Label.Text = '(Gy)';

            % Create Button_2
            app.Button_2 = uibutton(app.Panel_11, 'push');
            app.Button_2.ButtonPushedFcn = createCallbackFcn(app, @Button_2Pushed, true);
            app.Button_2.FontSize = 18;
            app.Button_2.FontWeight = 'bold';
            app.Button_2.Position = [3 11 26 33];
            app.Button_2.Text = '<';

            % Create YZSlider
            app.YZSlider = uislider(app.Panel_11);
            app.YZSlider.ValueChangingFcn = createCallbackFcn(app, @YZSliderValueChanging, true);
            app.YZSlider.Position = [37 43 354 3];

            % Create Button_5
            app.Button_5 = uibutton(app.Panel_11, 'push');
            app.Button_5.ButtonPushedFcn = createCallbackFcn(app, @Button_5Pushed, true);
            app.Button_5.FontSize = 18;
            app.Button_5.FontWeight = 'bold';
            app.Button_5.Position = [406 11 26 33];
            app.Button_5.Text = '>';

            % Create Panel_12
            app.Panel_12 = uipanel(app.DicomGrid);
            app.Panel_12.Layout.Row = 2;
            app.Panel_12.Layout.Column = 2;
            app.Panel_12.BackgroundColor = [0.8 0.8 0.8];

            % Create XZFigure
            app.XZFigure = uiaxes(app.Panel_12);
            app.XZFigure.XAxisLocation = 'origin';
            app.XZFigure.XTick = [];
            app.XZFigure.YAxisLocation = 'origin';
            app.XZFigure.YTick = [];
            app.XZFigure.Box = 'on';
            app.XZFigure.TickDir = 'none';
            app.XZFigure.Position = [3 67 432 395];

            % Create GyEditField_2
            app.GyEditField_2 = uieditfield(app.Panel_12, 'numeric');
            app.GyEditField_2.Position = [360 395 40 22];

            % Create GyEditField_2Label
            app.GyEditField_2Label = uilabel(app.Panel_12);
            app.GyEditField_2Label.HorizontalAlignment = 'right';
            app.GyEditField_2Label.Position = [403 395 28 22];
            app.GyEditField_2Label.Text = '(Gy)';

            % Create Button_3
            app.Button_3 = uibutton(app.Panel_12, 'push');
            app.Button_3.ButtonPushedFcn = createCallbackFcn(app, @Button_3Pushed, true);
            app.Button_3.FontSize = 18;
            app.Button_3.FontWeight = 'bold';
            app.Button_3.Position = [3 11 26 33];
            app.Button_3.Text = '<';

            % Create XZSlider
            app.XZSlider = uislider(app.Panel_12);
            app.XZSlider.ValueChangingFcn = createCallbackFcn(app, @XZSliderValueChanging, true);
            app.XZSlider.Position = [37 43 354 3];

            % Create Button_6
            app.Button_6 = uibutton(app.Panel_12, 'push');
            app.Button_6.ButtonPushedFcn = createCallbackFcn(app, @Button_6Pushed, true);
            app.Button_6.FontSize = 18;
            app.Button_6.FontWeight = 'bold';
            app.Button_6.Position = [406 11 26 33];
            app.Button_6.Text = '>';

            % Create Panel_13
            app.Panel_13 = uipanel(app.DicomGrid);
            app.Panel_13.Layout.Row = 2;
            app.Panel_13.Layout.Column = 3;
            app.Panel_13.BackgroundColor = [0.8 0.8 0.8];

            % Create XYFigure
            app.XYFigure = uiaxes(app.Panel_13);
            app.XYFigure.XAxisLocation = 'origin';
            app.XYFigure.XTick = [];
            app.XYFigure.YAxisLocation = 'origin';
            app.XYFigure.YTick = [];
            app.XYFigure.Box = 'on';
            app.XYFigure.TickDir = 'none';
            app.XYFigure.Position = [3 67 432 395];

            % Create GyEditField
            app.GyEditField = uieditfield(app.Panel_13, 'numeric');
            app.GyEditField.Position = [360 395 40 22];

            % Create GyEditFieldLabel
            app.GyEditFieldLabel = uilabel(app.Panel_13);
            app.GyEditFieldLabel.HorizontalAlignment = 'right';
            app.GyEditFieldLabel.Position = [403 395 28 22];
            app.GyEditFieldLabel.Text = '(Gy)';

            % Create Button_4
            app.Button_4 = uibutton(app.Panel_13, 'push');
            app.Button_4.ButtonPushedFcn = createCallbackFcn(app, @Button_4Pushed, true);
            app.Button_4.FontSize = 18;
            app.Button_4.FontWeight = 'bold';
            app.Button_4.Position = [3 11 26 33];
            app.Button_4.Text = '<';

            % Create XYSlider
            app.XYSlider = uislider(app.Panel_13);
            app.XYSlider.ValueChangingFcn = createCallbackFcn(app, @XYSliderValueChanging, true);
            app.XYSlider.Position = [37 43 354 3];

            % Create Button_7
            app.Button_7 = uibutton(app.Panel_13, 'push');
            app.Button_7.ButtonPushedFcn = createCallbackFcn(app, @Button_7Pushed, true);
            app.Button_7.FontSize = 18;
            app.Button_7.FontWeight = 'bold';
            app.Button_7.Position = [406 11 26 33];
            app.Button_7.Text = '>';

            % Create DosePlaneCalculatorPanel
            app.DosePlaneCalculatorPanel = uipanel(app.DicomGrid);
            app.DosePlaneCalculatorPanel.Layout.Row = 3;
            app.DosePlaneCalculatorPanel.Layout.Column = 1;
            app.DosePlaneCalculatorPanel.TitlePosition = 'centertop';
            app.DosePlaneCalculatorPanel.Title = 'Dose Plane Calculator';
            app.DosePlaneCalculatorPanel.BackgroundColor = [0.8 0.8 0.8];
            app.DosePlaneCalculatorPanel.FontWeight = 'bold';
            app.DosePlaneCalculatorPanel.Scrollable = 'on';
            app.DosePlaneCalculatorPanel.FontSize = 15;

            % Create Panel_4
            app.Panel_4 = uipanel(app.DosePlaneCalculatorPanel);
            app.Panel_4.BackgroundColor = [0.902 0.902 0.902];
            app.Panel_4.Position = [16 26 462 261];

            % Create SystemLabel
            app.SystemLabel = uilabel(app.Panel_4);
            app.SystemLabel.Position = [17 221 162 22];
            app.SystemLabel.Text = 'System';

            % Create XmmEditFieldLabel
            app.XmmEditFieldLabel = uilabel(app.Panel_4);
            app.XmmEditFieldLabel.HorizontalAlignment = 'right';
            app.XmmEditFieldLabel.Position = [29 190 44 22];
            app.XmmEditFieldLabel.Text = 'X (mm)';

            % Create EditField_6
            app.EditField_6 = uieditfield(app.Panel_4, 'numeric');
            app.EditField_6.ValueDisplayFormat = '%.2f';
            app.EditField_6.Editable = 'off';
            app.EditField_6.BackgroundColor = [0.902 0.902 0.902];
            app.EditField_6.Position = [90 190 50 22];

            % Create Label_17
            app.Label_17 = uilabel(app.Panel_4);
            app.Label_17.FontWeight = 'bold';
            app.Label_17.Position = [144 190 11 22];
            app.Label_17.Text = '>';

            % Create XmmEditField
            app.XmmEditField = uieditfield(app.Panel_4, 'numeric');
            app.XmmEditField.ValueDisplayFormat = '%.2f';
            app.XmmEditField.Position = [161 190 45 22];

            % Create Label_18
            app.Label_18 = uilabel(app.Panel_4);
            app.Label_18.FontWeight = 'bold';
            app.Label_18.Position = [215 190 11 22];
            app.Label_18.Text = '<';

            % Create EditField_7
            app.EditField_7 = uieditfield(app.Panel_4, 'numeric');
            app.EditField_7.ValueDisplayFormat = '%.2f';
            app.EditField_7.Editable = 'off';
            app.EditField_7.BackgroundColor = [0.902 0.902 0.902];
            app.EditField_7.Position = [227 190 53 22];

            % Create Panel_5
            app.Panel_5 = uipanel(app.Panel_4);
            app.Panel_5.BorderType = 'none';
            app.Panel_5.BackgroundColor = [0.8 0.8 0.8];
            app.Panel_5.Position = [305 114 142 112];

            % Create ColumnEditFieldLabel
            app.ColumnEditFieldLabel = uilabel(app.Panel_5);
            app.ColumnEditFieldLabel.HorizontalAlignment = 'right';
            app.ColumnEditFieldLabel.Position = [15 78 46 22];
            app.ColumnEditFieldLabel.Text = 'Column';

            % Create ColumnEditField
            app.ColumnEditField = uieditfield(app.Panel_5, 'numeric');
            app.ColumnEditField.ValueDisplayFormat = '%.0f';
            app.ColumnEditField.Position = [65 78 55 22];

            % Create RowEditFieldLabel
            app.RowEditFieldLabel = uilabel(app.Panel_5);
            app.RowEditFieldLabel.HorizontalAlignment = 'right';
            app.RowEditFieldLabel.Position = [12 46 32 22];
            app.RowEditFieldLabel.Text = ' Row';

            % Create RowEditField
            app.RowEditField = uieditfield(app.Panel_5, 'numeric');
            app.RowEditField.ValueDisplayFormat = '%.0f';
            app.RowEditField.Position = [65 46 55 22];

            % Create FrameEditFieldLabel
            app.FrameEditFieldLabel = uilabel(app.Panel_5);
            app.FrameEditFieldLabel.HorizontalAlignment = 'right';
            app.FrameEditFieldLabel.Position = [15 13 40 22];
            app.FrameEditFieldLabel.Text = 'Frame';

            % Create FrameEditField
            app.FrameEditField = uieditfield(app.Panel_5, 'numeric');
            app.FrameEditField.ValueDisplayFormat = '%.0f';
            app.FrameEditField.Position = [64 11 56 22];

            % Create YmmEditFieldLabel
            app.YmmEditFieldLabel = uilabel(app.Panel_4);
            app.YmmEditFieldLabel.HorizontalAlignment = 'right';
            app.YmmEditFieldLabel.Position = [27 158 44 22];
            app.YmmEditFieldLabel.Text = 'Y (mm)';

            % Create EditField_8
            app.EditField_8 = uieditfield(app.Panel_4, 'numeric');
            app.EditField_8.ValueDisplayFormat = '%.2f';
            app.EditField_8.Editable = 'off';
            app.EditField_8.BackgroundColor = [0.902 0.902 0.902];
            app.EditField_8.Position = [90 159 50 22];

            % Create Label_19
            app.Label_19 = uilabel(app.Panel_4);
            app.Label_19.FontWeight = 'bold';
            app.Label_19.Position = [144 159 12 22];
            app.Label_19.Text = '>';

            % Create YmmEditField
            app.YmmEditField = uieditfield(app.Panel_4, 'numeric');
            app.YmmEditField.ValueDisplayFormat = '%.2f';
            app.YmmEditField.Position = [161 159 45 22];

            % Create Label_20
            app.Label_20 = uilabel(app.Panel_4);
            app.Label_20.FontWeight = 'bold';
            app.Label_20.Position = [216 159 10 22];
            app.Label_20.Text = '<';

            % Create EditField_9
            app.EditField_9 = uieditfield(app.Panel_4, 'numeric');
            app.EditField_9.ValueDisplayFormat = '%.2f';
            app.EditField_9.Editable = 'off';
            app.EditField_9.BackgroundColor = [0.902 0.902 0.902];
            app.EditField_9.Position = [227 159 53 22];

            % Create ZmmEditFieldLabel
            app.ZmmEditFieldLabel = uilabel(app.Panel_4);
            app.ZmmEditFieldLabel.HorizontalAlignment = 'right';
            app.ZmmEditFieldLabel.Position = [27 123 44 22];
            app.ZmmEditFieldLabel.Text = 'Z (mm)';

            % Create EditField_10
            app.EditField_10 = uieditfield(app.Panel_4, 'numeric');
            app.EditField_10.ValueDisplayFormat = '%.2f';
            app.EditField_10.Editable = 'off';
            app.EditField_10.BackgroundColor = [0.902 0.902 0.902];
            app.EditField_10.Position = [90 124 49 22];

            % Create Label_21
            app.Label_21 = uilabel(app.Panel_4);
            app.Label_21.FontWeight = 'bold';
            app.Label_21.Position = [144 124 12 22];
            app.Label_21.Text = '>';

            % Create ZmmEditField
            app.ZmmEditField = uieditfield(app.Panel_4, 'numeric');
            app.ZmmEditField.ValueDisplayFormat = '%.2f';
            app.ZmmEditField.Position = [161 123 45 22];

            % Create Label_22
            app.Label_22 = uilabel(app.Panel_4);
            app.Label_22.FontWeight = 'bold';
            app.Label_22.Position = [216 124 11 22];
            app.Label_22.Text = '<';

            % Create EditField_11
            app.EditField_11 = uieditfield(app.Panel_4, 'numeric');
            app.EditField_11.ValueDisplayFormat = '%.2f';
            app.EditField_11.Editable = 'off';
            app.EditField_11.BackgroundColor = [0.902 0.902 0.902];
            app.EditField_11.Position = [227 123 53 22];

            % Create CalculateButton
            app.CalculateButton = uibutton(app.Panel_4, 'push');
            app.CalculateButton.ButtonPushedFcn = createCallbackFcn(app, @CalculateButtonPushed, true);
            app.CalculateButton.BackgroundColor = [1 1 1];
            app.CalculateButton.FontSize = 18;
            app.CalculateButton.Position = [29 66 100 37];
            app.CalculateButton.Text = 'Calculate!';

            % Create UpdateLabel
            app.UpdateLabel = uilabel(app.Panel_4);
            app.UpdateLabel.FontWeight = 'bold';
            app.UpdateLabel.Position = [15 16 41 22];
            app.UpdateLabel.Text = 'Plane:';

            % Create DropDown_3
            app.DropDown_3 = uidropdown(app.Panel_4);
            app.DropDown_3.Items = {'XZ', 'YZ', 'XY'};
            app.DropDown_3.Position = [71 13 78 29];
            app.DropDown_3.Value = 'XZ';

            % Create SendPlaneforAnalysisButton
            app.SendPlaneforAnalysisButton = uibutton(app.Panel_4, 'push');
            app.SendPlaneforAnalysisButton.ButtonPushedFcn = createCallbackFcn(app, @SendPlaneforAnalysisButtonPushed, true);
            app.SendPlaneforAnalysisButton.BackgroundColor = [1 1 1];
            app.SendPlaneforAnalysisButton.FontSize = 14;
            app.SendPlaneforAnalysisButton.Position = [165 13 163 31];
            app.SendPlaneforAnalysisButton.Text = 'Send Plane for Analysis';

            % Create RotatedDoseAlignmentPanel
            app.RotatedDoseAlignmentPanel = uipanel(app.DicomGrid);
            app.RotatedDoseAlignmentPanel.Layout.Row = 3;
            app.RotatedDoseAlignmentPanel.Layout.Column = [2 3];
            app.RotatedDoseAlignmentPanel.TitlePosition = 'centertop';
            app.RotatedDoseAlignmentPanel.Title = 'Rotated Dose Alignment';
            app.RotatedDoseAlignmentPanel.BackgroundColor = [0.8 0.8 0.8];
            app.RotatedDoseAlignmentPanel.FontWeight = 'bold';
            app.RotatedDoseAlignmentPanel.FontSize = 15;

            % Create OriginalFigure
            app.OriginalFigure = uiaxes(app.RotatedDoseAlignmentPanel);
            title(app.OriginalFigure, 'Original')
            app.OriginalFigure.XTick = [];
            app.OriginalFigure.YTick = [];
            app.OriginalFigure.Box = 'on';
            app.OriginalFigure.Position = [226 20 277 272];

            % Create RotatedFigure
            app.RotatedFigure = uiaxes(app.RotatedDoseAlignmentPanel);
            title(app.RotatedFigure, 'Rotated')
            app.RotatedFigure.XTick = [];
            app.RotatedFigure.YTick = [];
            app.RotatedFigure.Box = 'on';
            app.RotatedFigure.Position = [571 20 275 272];

            % Create Panel_7
            app.Panel_7 = uipanel(app.RotatedDoseAlignmentPanel);
            app.Panel_7.Position = [16 119 150 162];

            % Create PitchEditFieldLabel
            app.PitchEditFieldLabel = uilabel(app.Panel_7);
            app.PitchEditFieldLabel.HorizontalAlignment = 'right';
            app.PitchEditFieldLabel.Position = [24 128 32 22];
            app.PitchEditFieldLabel.Text = 'Pitch';

            % Create PitchEditField
            app.PitchEditField = uieditfield(app.Panel_7, 'numeric');
            app.PitchEditField.Position = [71 128 46 22];

            % Create RollEditFieldLabel
            app.RollEditFieldLabel = uilabel(app.Panel_7);
            app.RollEditFieldLabel.HorizontalAlignment = 'right';
            app.RollEditFieldLabel.Position = [30 97 26 22];
            app.RollEditFieldLabel.Text = 'Roll';

            % Create RollEditField
            app.RollEditField = uieditfield(app.Panel_7, 'numeric');
            app.RollEditField.Position = [71 97 46 22];

            % Create YawEditFieldLabel
            app.YawEditFieldLabel = uilabel(app.Panel_7);
            app.YawEditFieldLabel.HorizontalAlignment = 'right';
            app.YawEditFieldLabel.Position = [29 63 27 22];
            app.YawEditFieldLabel.Text = 'Yaw';

            % Create YawEditField
            app.YawEditField = uieditfield(app.Panel_7, 'numeric');
            app.YawEditField.Position = [70 63 46 22];

            % Create RotateButton
            app.RotateButton = uibutton(app.Panel_7, 'push');
            app.RotateButton.ButtonPushedFcn = createCallbackFcn(app, @RotateButtonPushed, true);
            app.RotateButton.BackgroundColor = [1 1 1];
            app.RotateButton.FontSize = 14;
            app.RotateButton.Position = [24 13 79 33];
            app.RotateButton.Text = 'Rotate';

            % Create Label_9
            app.Label_9 = uilabel(app.RotatedDoseAlignmentPanel);
            app.Label_9.Position = [226 269 25 22];
            app.Label_9.Text = '_';

            % Create Label_11
            app.Label_11 = uilabel(app.RotatedDoseAlignmentPanel);
            app.Label_11.Position = [572 265 25 22];
            app.Label_11.Text = '_';

            % Create Button_12
            app.Button_12 = uibutton(app.RotatedDoseAlignmentPanel, 'push');
            app.Button_12.ButtonPushedFcn = createCallbackFcn(app, @Button_12Pushed, true);
            app.Button_12.BackgroundColor = [1 1 1];
            app.Button_12.FontWeight = 'bold';
            app.Button_12.Position = [75 78 25 23];
            app.Button_12.Text = '^';

            % Create Button_10
            app.Button_10 = uibutton(app.RotatedDoseAlignmentPanel, 'push');
            app.Button_10.ButtonPushedFcn = createCallbackFcn(app, @Button_10Pushed, true);
            app.Button_10.BackgroundColor = [1 1 1];
            app.Button_10.FontSize = 14;
            app.Button_10.FontWeight = 'bold';
            app.Button_10.Position = [46 51 25 25];
            app.Button_10.Text = '<';

            % Create StepsizepixelEditField
            app.StepsizepixelEditField = uieditfield(app.RotatedDoseAlignmentPanel, 'numeric');
            app.StepsizepixelEditField.HorizontalAlignment = 'center';
            app.StepsizepixelEditField.Position = [78 56 19 17];
            app.StepsizepixelEditField.Value = 1;

            % Create Button_11
            app.Button_11 = uibutton(app.RotatedDoseAlignmentPanel, 'push');
            app.Button_11.ButtonPushedFcn = createCallbackFcn(app, @Button_11Pushed, true);
            app.Button_11.BackgroundColor = [1 1 1];
            app.Button_11.FontWeight = 'bold';
            app.Button_11.Position = [105 52 25 23];
            app.Button_11.Text = '>';

            % Create Label_10
            app.Label_10 = uilabel(app.RotatedDoseAlignmentPanel);
            app.Label_10.Position = [478 47 25 22];
            app.Label_10.Text = '_';

            % Create Label_12
            app.Label_12 = uilabel(app.RotatedDoseAlignmentPanel);
            app.Label_12.Position = [833 43 25 22];
            app.Label_12.Text = '_';

            % Create vButton
            app.vButton = uibutton(app.RotatedDoseAlignmentPanel, 'push');
            app.vButton.ButtonPushedFcn = createCallbackFcn(app, @vButtonPushed, true);
            app.vButton.BackgroundColor = [1 1 1];
            app.vButton.FontWeight = 'bold';
            app.vButton.Position = [76 27 22 22];
            app.vButton.Text = 'v';

            % Create OGPlane
            app.OGPlane = uidropdown(app.RotatedDoseAlignmentPanel);
            app.OGPlane.Items = {'XY', 'YZ', 'XZ'};
            app.OGPlane.ValueChangedFcn = createCallbackFcn(app, @OGPlaneValueChanged, true);
            app.OGPlane.FontWeight = 'bold';
            app.OGPlane.BackgroundColor = [0.8 0.8 0.8];
            app.OGPlane.Position = [165 20 59 27];
            app.OGPlane.Value = 'XY';

            % Create RotatedPlane
            app.RotatedPlane = uidropdown(app.RotatedDoseAlignmentPanel);
            app.RotatedPlane.Items = {'XY', 'YZ', 'XZ'};
            app.RotatedPlane.ValueChangedFcn = createCallbackFcn(app, @RotatedPlaneValueChanged, true);
            app.RotatedPlane.FontWeight = 'bold';
            app.RotatedPlane.BackgroundColor = [0.8 0.8 0.8];
            app.RotatedPlane.Position = [511 18 60 28];
            app.RotatedPlane.Value = 'XY';


            % Create AnalysisGrid - responsive layout for Analysis tab
            app.AnalysisGrid = uigridlayout(app.AnalysisTab, [3, 2]);
            app.AnalysisGrid.RowHeight = {30, '1x', '1x'};
            app.AnalysisGrid.ColumnWidth = {'1x', '1x'};
            app.AnalysisGrid.RowSpacing = 5;
            app.AnalysisGrid.ColumnSpacing = 5;
            app.AnalysisGrid.Padding = [11 10 11 5];

            % Create Panel_34
            app.Panel_34 = uipanel(app.AnalysisGrid);
            app.Panel_34.Layout.Row = 1;
            app.Panel_34.Layout.Column = [1 2];
            app.Panel_34.BackgroundColor = [0.8 0.8 0.8];

            analysisTopGrid = uigridlayout(app.Panel_34,[1 2]);
            analysisTopGrid.RowHeight = {'fit'};
            analysisTopGrid.ColumnWidth = {'fit','fit'};
            analysisTopGrid.ColumnSpacing = 6;
            analysisTopGrid.Padding = [6 4 6 4];

            % Create ColorMapDropDownLabel
            app.ColorMapDropDownLabel = uilabel(analysisTopGrid);
            app.ColorMapDropDownLabel.HorizontalAlignment = 'right';
            app.ColorMapDropDownLabel.Layout.Row = 1;
            app.ColorMapDropDownLabel.Layout.Column = 1;
            app.ColorMapDropDownLabel.Position = [4 5 55 18];
            app.ColorMapDropDownLabel.Text = 'Color Map';

            % Create ColorMapDropDown
            app.ColorMapDropDown = uidropdown(analysisTopGrid);
            app.ColorMapDropDown.Items = {'jet', 'parula', 'hsv', 'hot', 'cool', 'spring', 'summer', 'bone', 'gray', 'sky', 'lines', 'flag', 'white', 'prism'};
            app.ColorMapDropDown.ValueChangedFcn = createCallbackFcn(app, @ColorMapDropDownValueChanged, true);
            app.ColorMapDropDown.Layout.Row = 1;
            app.ColorMapDropDown.Layout.Column = 2;
            app.ColorMapDropDown.Position = [60 5 70 20];
            app.ColorMapDropDown.FontSize = 10;
            app.ColorMapDropDown.Value = 'bone';

            % Create MRNLabel
            app.MRNLabel = uilabel(app.Panel_34);
            app.MRNLabel.FontWeight = 'bold';
            app.MRNLabel.Position = [941 9 36 13];
            app.MRNLabel.Text = 'MRN:';

            % Create MRNEditField
            app.MRNEditField = uieditfield(app.Panel_34, 'text');
            app.MRNEditField.BackgroundColor = [0.9412 0.9412 0.9412];
            app.MRNEditField.Position = [979 6 152 18];

            % Create PlanLabel
            app.PlanLabel = uilabel(app.Panel_34);
            app.PlanLabel.FontWeight = 'bold';
            app.PlanLabel.Position = [1143 10 34 13];
            app.PlanLabel.Text = 'Plan:';

            % Create AllFileName
            app.AllFileName = uieditfield(app.Panel_34, 'text');
            app.AllFileName.FontSize = 11;
            app.AllFileName.FontWeight = 'bold';
            app.AllFileName.BackgroundColor = [0.9412 0.9412 0.9412];
            app.AllFileName.Position = [1181 7 153 18];

            % Create Button_21
            app.Button_21 = uibutton(app.Panel_34, 'push');
            app.Button_21.ButtonPushedFcn = createCallbackFcn(app, @Button_8Pushed, true);
            app.Button_21.Icon = fullfile(pathToMLAPP, 'ml_resources', 'Graphics', 'icons8-pdf-100.png');
            app.Button_21.BackgroundColor = [1 1 1];
            app.Button_21.FontColor = [0.149 0.149 0.149];
            app.Button_21.Position = [1354 6 22 21];
            app.Button_21.Text = '';

            % Create Panel_16
            app.Panel_16 = uipanel(app.AnalysisGrid);
            app.Panel_16.Layout.Row = 2;
            app.Panel_16.Layout.Column = 1;
            app.Panel_16.BackgroundColor = [0.8 0.8 0.8];

            % Use a grid inside the panel so axes/slider resize with the tab
            dicomGrid = uigridlayout(app.Panel_16, [1 2]);
            dicomGrid.RowHeight = {'1x'};
            dicomGrid.ColumnWidth = {'1x', 40};
            dicomGrid.RowSpacing = 5;
            dicomGrid.ColumnSpacing = 5;
            dicomGrid.Padding = [10 10 10 10];

            % Create FigDicomDose
            app.FigDicomDose = uiaxes(dicomGrid);
            app.FigDicomDose.AmbientLightColor = [0.651 0.651 0.651];
            app.FigDicomDose.GridLineWidth = 1;
            app.FigDicomDose.MinorGridLineWidth = 0.1;
            app.FigDicomDose.MinorGridLineStyle = 'none';
            app.FigDicomDose.XAxisLocation = 'top';
            app.FigDicomDose.XTick = [];
            app.FigDicomDose.YAxisLocation = 'origin';
            app.FigDicomDose.YTick = [];
            app.FigDicomDose.LineWidth = 1;
            app.FigDicomDose.Box = 'on';
            app.FigDicomDose.Layout.Row = 1;
            app.FigDicomDose.Layout.Column = 1;

            % Create Slider
            app.Slider = uislider(dicomGrid, 'range');
            app.Slider.Limits = [0 1];
            app.Slider.Orientation = 'vertical';
            app.Slider.ValueChangingFcn = createCallbackFcn(app, @SliderValueChanging, true);
            app.Slider.Layout.Row = 1;
            app.Slider.Layout.Column = 2;
            app.Slider.Value = [0 1];

            % Create Panel_17
            app.Panel_17 = uipanel(app.AnalysisGrid);
            app.Panel_17.Layout.Row = 2;
            app.Panel_17.Layout.Column = 2;
            app.Panel_17.BackgroundColor = [0.8 0.8 0.8];

            % Grid for film axes/slider
            filmGrid = uigridlayout(app.Panel_17, [1 2]);
            filmGrid.RowHeight = {'1x'};
            filmGrid.ColumnWidth = {'1x', 40};
            filmGrid.RowSpacing = 5;
            filmGrid.ColumnSpacing = 5;
            filmGrid.Padding = [10 10 10 10];

            % Create FigFilmDose
            app.FigFilmDose = uiaxes(filmGrid);
            zlabel(app.FigFilmDose, 'Z')
            app.FigFilmDose.GridLineWidth = 1;
            app.FigFilmDose.MinorGridLineWidth = 0.1;
            app.FigFilmDose.MinorGridLineStyle = 'none';
            app.FigFilmDose.XAxisLocation = 'top';
            app.FigFilmDose.XTick = [];
            app.FigFilmDose.YAxisLocation = 'origin';
            app.FigFilmDose.YTick = [];
            app.FigFilmDose.LineWidth = 1;
            app.FigFilmDose.Box = 'on';
            app.FigFilmDose.Layout.Row = 1;
            app.FigFilmDose.Layout.Column = 1;

            % Create Slider_2
            app.Slider_2 = uislider(filmGrid, 'range');
            app.Slider_2.Limits = [0 1];
            app.Slider_2.Orientation = 'vertical';
            app.Slider_2.ValueChangingFcn = createCallbackFcn(app, @Slider_2ValueChanging, true);
            app.Slider_2.Layout.Row = 1;
            app.Slider_2.Layout.Column = 2;
            app.Slider_2.Value = [0 1];

            % Create Label_16
            app.Label_16 = uilabel(dicomGrid);
            app.Label_16.FontSize = 14;
            app.Label_16.FontWeight = 'bold';
            app.Label_16.Layout.Row = 1;
            app.Label_16.Layout.Column = 1;
            app.Label_16.Text = '';

            % Create Label_15
            app.Label_15 = uilabel(filmGrid);
            app.Label_15.FontSize = 14;
            app.Label_15.FontWeight = 'bold';
            app.Label_15.Layout.Row = 1;
            app.Label_15.Layout.Column = 1;
            app.Label_15.Text = '';

            % Create Panel_18
            app.Panel_18 = uipanel(app.AnalysisGrid);
            app.Panel_18.Layout.Row = 3;
            app.Panel_18.Layout.Column = 1;
            app.Panel_18.BackgroundColor = [0.8 0.8 0.8];

            % Grid for profile displays and controls (2x2 equal sections)
            profileGrid = uigridlayout(app.Panel_18, [2 2]);
            profileGrid.RowHeight = {'2x', '1x'};
            profileGrid.ColumnWidth = {'1x', '1x'};
            profileGrid.RowSpacing = 8;
            profileGrid.ColumnSpacing = 10;
            profileGrid.Padding = [8 8 8 8];

            % Create UIAxes2
            app.UIAxes2 = uiaxes(profileGrid);
            title(app.UIAxes2, 'Profile Y')
            ylabel(app.UIAxes2, 'Dose (cGy)')
            zlabel(app.UIAxes2, 'Z')
            app.UIAxes2.Box = 'on';
            app.UIAxes2.FontSize = 14;
            app.UIAxes2.Layout.Row = 1;
            app.UIAxes2.Layout.Column = 1;

            % Create UIAxes3
            app.UIAxes3 = uiaxes(profileGrid);
            title(app.UIAxes3, 'Profile X')
            ylabel(app.UIAxes3, 'Dose (cGy)')
            zlabel(app.UIAxes3, 'Z')
            app.UIAxes3.Box = 'on';
            app.UIAxes3.FontSize = 14;
            app.UIAxes3.Layout.Row = 1;
            app.UIAxes3.Layout.Column = 2;

            % Controls grid (D-pad) occupies bottom-left quadrant
            controlGrid = uigridlayout(profileGrid, [4 5]);
            controlGrid.Layout.Row = 2;
            controlGrid.Layout.Column = 1;
            controlGrid.RowHeight = {28, 28, 28, '1x'};
            controlGrid.ColumnWidth = {30,30,30,30,'1x'};
            controlGrid.RowSpacing = 6;
            controlGrid.ColumnSpacing = 8;
            controlGrid.Padding = [6 4 6 4];

            % Create UpButton_2
            app.UpButton_2 = uibutton(controlGrid, 'push');
            app.UpButton_2.ButtonPushedFcn = createCallbackFcn(app, @UpButton_2Pushed, true);
            app.UpButton_2.FontWeight = 'bold';
            app.UpButton_2.FontSize = 12;
            app.UpButton_2.Layout.Row = 1;
            app.UpButton_2.Layout.Column = 3;
            app.UpButton_2.Text = '^';

            % Create StepsizepixelEditField_2
            app.StepsizepixelEditField_2 = uieditfield(controlGrid, 'numeric');
            app.StepsizepixelEditField_2.ValueDisplayFormat = '%.1f';
            app.StepsizepixelEditField_2.HorizontalAlignment = 'center';
            app.StepsizepixelEditField_2.Layout.Row = 2;
            app.StepsizepixelEditField_2.Layout.Column = 3;
            app.StepsizepixelEditField_2.Value = 1;

            % Step size label
            app.StepSizeLabel = uilabel(controlGrid);
            app.StepSizeLabel.HorizontalAlignment = 'right';
            app.StepSizeLabel.Text = 'Step';
            app.StepSizeLabel.Layout.Row = 2;
            app.StepSizeLabel.Layout.Column = 1;

            % Create LeftButton_2
            app.LeftButton_2 = uibutton(controlGrid, 'push');
            app.LeftButton_2.ButtonPushedFcn = createCallbackFcn(app, @LeftButton_2Pushed, true);
            app.LeftButton_2.FontWeight = 'bold';
            app.LeftButton_2.FontSize = 12;
            app.LeftButton_2.Layout.Row = 2;
            app.LeftButton_2.Layout.Column = 2;
            app.LeftButton_2.Text = '<';

            % Create DownButton_2
            app.DownButton_2 = uibutton(controlGrid, 'push');
            app.DownButton_2.ButtonPushedFcn = createCallbackFcn(app, @DownButton_2Pushed, true);
            app.DownButton_2.FontWeight = 'bold';
            app.DownButton_2.FontSize = 12;
            app.DownButton_2.Layout.Row = 3;
            app.DownButton_2.Layout.Column = 3;
            app.DownButton_2.Text = 'v';

            % Create RightButton_2
            app.RightButton_2 = uibutton(controlGrid, 'push');
            app.RightButton_2.ButtonPushedFcn = createCallbackFcn(app, @RightButton_2Pushed, true);
            app.RightButton_2.FontWeight = 'bold';
            app.RightButton_2.FontSize = 12;
            app.RightButton_2.Layout.Row = 2;
            app.RightButton_2.Layout.Column = 4;
            app.RightButton_2.Text = '>';

            % Create Panel_36
            % Profile tools panel occupies bottom-right quadrant
            app.Panel_36 = uipanel(profileGrid);
            app.Panel_36.Layout.Row = 2;
            app.Panel_36.Layout.Column = 2;
            app.Panel_36.TitlePosition = 'centertop';
            app.Panel_36.Title = 'Profile Tools';
            toolGrid = uigridlayout(app.Panel_36, [3 6]);
            toolGrid.RowHeight = {'fit','fit','fit'};
            toolGrid.ColumnWidth = {70,90,90,90,'fit','fit'};
            toolGrid.RowSpacing = 6;
            toolGrid.ColumnSpacing = 6;
            toolGrid.Padding = [4 4 4 4];
            app.Panel_36.Scrollable = 'on';

            % Create ScaleVal
            app.ScaleVal = uieditfield(toolGrid, 'numeric');
            app.ScaleVal.Limits = [-100 100];
            app.ScaleVal.ValueDisplayFormat = '%.2f';
            app.ScaleVal.Layout.Row = 1;
            app.ScaleVal.Layout.Column = 1;

            % Create ScaleFilmDoseButton
            app.ScaleFilmDoseButton = uibutton(toolGrid, 'push');
            app.ScaleFilmDoseButton.ButtonPushedFcn = createCallbackFcn(app, @ScaleFilmDoseButtonPushed, true);
            app.ScaleFilmDoseButton.BackgroundColor = [1 1 1];
            app.ScaleFilmDoseButton.Layout.Row = 1;
            app.ScaleFilmDoseButton.Layout.Column = 2;
            app.ScaleFilmDoseButton.Text = 'Scale Film Dose (%)';

            % Create CenterProfileButton
            app.CenterProfileButton = uibutton(toolGrid, 'push');
            app.CenterProfileButton.ButtonPushedFcn = createCallbackFcn(app, @CenterProfileButtonPushed, true);
            app.CenterProfileButton.BackgroundColor = [1 1 1];
            app.CenterProfileButton.Layout.Row = 2;
            app.CenterProfileButton.Layout.Column = 2;
            app.CenterProfileButton.Text = 'Center Profile';

            % Create OffcenterProfileButton
            app.OffcenterProfileButton = uibutton(toolGrid, 'push');
            app.OffcenterProfileButton.ButtonPushedFcn = createCallbackFcn(app, @OffcenterProfileButtonPushed, true);
            app.OffcenterProfileButton.BackgroundColor = [1 1 1];
            app.OffcenterProfileButton.Layout.Row = 2;
            app.OffcenterProfileButton.Layout.Column = 3;
            app.OffcenterProfileButton.Text = 'Offcenter Profile';

            % Create FxLabel
            app.FxLabel = uilabel(toolGrid);
            app.FxLabel.HorizontalAlignment = 'right';
            app.FxLabel.Layout.Row = 3;
            app.FxLabel.Layout.Column = 1;
            app.FxLabel.Text = 'Fx:';

            % Create FxEditField
            app.FxEditField = uieditfield(toolGrid, 'numeric');
            app.FxEditField.Layout.Row = 3;
            app.FxEditField.Layout.Column = 2;
            app.FxEditField.Value = 1;

            % Create ClearXButton_2
            app.ClearXButton_2 = uibutton(toolGrid, 'push');
            app.ClearXButton_2.ButtonPushedFcn = createCallbackFcn(app, @ClearXButton_2Pushed, true);
            app.ClearXButton_2.Layout.Row = 3;
            app.ClearXButton_2.Layout.Column = 3;
            app.ClearXButton_2.Text = 'Clear (X)';

            % Create Panel_19
            app.Panel_19 = uipanel(app.AnalysisGrid);
            app.Panel_19.Layout.Row = 3;
            app.Panel_19.Layout.Column = 2;
            app.Panel_19.BackgroundColor = [0.8 0.8 0.8];

            % Grid to keep gamma axis/tools aligned (tools stacked on the right)
            gammaGrid = uigridlayout(app.Panel_19, [1 2]);
            gammaGrid.RowHeight = {'1x'};
            gammaGrid.ColumnWidth = {'1x', 'fit'};
            gammaGrid.RowSpacing = 8;
            gammaGrid.ColumnSpacing = 10;
            gammaGrid.Padding = [8 8 8 8];

            % Create UIAxes6
            app.UIAxes6 = uiaxes(gammaGrid);
            zlabel(app.UIAxes6, 'Z')
            app.UIAxes6.GridLineWidth = 1;
            app.UIAxes6.MinorGridLineWidth = 1;
            app.UIAxes6.MinorGridLineStyle = 'none';
            app.UIAxes6.XTick = [];
            app.UIAxes6.YTick = [];
            app.UIAxes6.ZTick = [];
            app.UIAxes6.BoxStyle = 'full';
            app.UIAxes6.LineWidth = 1;
            app.UIAxes6.ClippingStyle = 'rectangle';
            app.UIAxes6.Box = 'on';
            app.UIAxes6.TickDir = 'none';
            app.UIAxes6.Layout.Row = 1;
            app.UIAxes6.Layout.Column = 1;

            % Right stack for gamma controls/results
            gammaSide = uigridlayout(gammaGrid, [2 1]);
            gammaSide.Layout.Row = 1;
            gammaSide.Layout.Column = 2;
            gammaSide.RowHeight = {'1x','1x'};
            gammaSide.ColumnWidth = {'fit'};
            gammaSide.RowSpacing = 8;
            gammaSide.Padding = [0 0 0 0];

            % Create GammaToolsPanel
            app.GammaToolsPanel = uipanel(gammaSide);
            app.GammaToolsPanel.TitlePosition = 'centertop';
            app.GammaToolsPanel.Title = 'Gamma Tools';
            app.GammaToolsPanel.BackgroundColor = [0.8 0.8 0.8];
            app.GammaToolsPanel.FontWeight = 'bold';
            app.GammaToolsPanel.Layout.Row = 1;
            app.GammaToolsPanel.Layout.Column = 1;

            % Grid inside Gamma Tools to add breathing room
            gammaToolsGrid = uigridlayout(app.GammaToolsPanel, [5 2]);
            gammaToolsGrid.RowHeight = {'fit','fit','fit','fit','fit'};
            gammaToolsGrid.ColumnWidth = {85,85};
            gammaToolsGrid.RowSpacing = 6;
            gammaToolsGrid.ColumnSpacing = 8;
            gammaToolsGrid.Padding = [8 8 8 8];

            % Move Perform Gamma toggle here
            app.PerformGammaCheckBox = uicheckbox(gammaToolsGrid);
            app.PerformGammaCheckBox.Text = 'Perform Gamma';
            app.PerformGammaCheckBox.FontSize = 11;
            app.PerformGammaCheckBox.Layout.Row = 1;
            app.PerformGammaCheckBox.Layout.Column = [1 2];

            % Create DDLabel
            app.DDLabel = uilabel(gammaToolsGrid);
            app.DDLabel.HorizontalAlignment = 'right';
            app.DDLabel.FontWeight = 'bold';
            app.DDLabel.FontSize = 11;
            app.DDLabel.Layout.Row = 2;
            app.DDLabel.Layout.Column = 1;
            app.DDLabel.Text = 'DD (%):';

            % Create DDEditField
            app.DDEditField = uieditfield(gammaToolsGrid, 'numeric');
            app.DDEditField.FontSize = 11;
            app.DDEditField.Layout.Row = 2;
            app.DDEditField.Layout.Column = 2;
            app.DDEditField.Value = 2;

            % Create DTAmmLabel
            app.DTAmmLabel = uilabel(gammaToolsGrid);
            app.DTAmmLabel.HorizontalAlignment = 'right';
            app.DTAmmLabel.FontWeight = 'bold';
            app.DTAmmLabel.FontSize = 11;
            app.DTAmmLabel.Layout.Row = 3;
            app.DTAmmLabel.Layout.Column = 1;
            app.DTAmmLabel.Text = 'DTA (mm):';

            % Create DTAmmEditField
            app.DTAmmEditField = uieditfield(gammaToolsGrid, 'numeric');
            app.DTAmmEditField.FontSize = 11;
            app.DTAmmEditField.Layout.Row = 3;
            app.DTAmmEditField.Layout.Column = 2;
            app.DTAmmEditField.Value = 2;

            % Create SingalLabel
            app.SingalLabel = uilabel(gammaToolsGrid);
            app.SingalLabel.HorizontalAlignment = 'right';
            app.SingalLabel.FontWeight = 'bold';
            app.SingalLabel.FontSize = 11;
            app.SingalLabel.Layout.Row = 4;
            app.SingalLabel.Layout.Column = 1;
            app.SingalLabel.Text = 'Singal (%) >=';

            % Create SingalEditField
            app.SingalEditField = uieditfield(gammaToolsGrid, 'numeric');
            app.SingalEditField.FontSize = 11;
            app.SingalEditField.Layout.Row = 4;
            app.SingalEditField.Layout.Column = 2;
            app.SingalEditField.Value = 10;

            % Create DropDown_8
            app.DropDown_8 = uidropdown(gammaToolsGrid);
            app.DropDown_8.Items = {'Absolute', 'Relative'};
            app.DropDown_8.FontSize = 11;
            app.DropDown_8.Layout.Row = 5;
            app.DropDown_8.Layout.Column = 1;
            app.DropDown_8.Value = 'Absolute';

            % Create GammaButton
            app.GammaButton = uibutton(gammaToolsGrid, 'push');
            app.GammaButton.ButtonPushedFcn = createCallbackFcn(app, @GammaButtonPushed, true);
            app.GammaButton.BackgroundColor = [0.4353 0.8706 0.4627];
            app.GammaButton.FontSize = 12;
            app.GammaButton.FontWeight = 'bold';
            app.GammaButton.Layout.Row = 5;
            app.GammaButton.Layout.Column = 2;
            app.GammaButton.Text = 'Gamma ';

            % Create ResultPanel
            app.ResultPanel = uipanel(gammaSide);
            app.ResultPanel.TitlePosition = 'centertop';
            app.ResultPanel.Title = 'Result';
            app.ResultPanel.BackgroundColor = [0.8 0.8 0.8];
            app.ResultPanel.FontWeight = 'bold';
            app.ResultPanel.FontSize = 14;
            app.ResultPanel.Layout.Row = 2;
            app.ResultPanel.Layout.Column = 1;
            app.ResultPanel.Scrollable = 'on';

            % Create XShiftsmmEditFieldLabel
            app.XShiftsmmEditFieldLabel = uilabel(app.ResultPanel);
            app.XShiftsmmEditFieldLabel.HorizontalAlignment = 'right';
            app.XShiftsmmEditFieldLabel.FontSize = 9;
            app.XShiftsmmEditFieldLabel.Position = [9 120 60 22];
            app.XShiftsmmEditFieldLabel.Text = 'X Shifts (mm)';

            % Create XShiftsmmEditField
            app.XShiftsmmEditField = uieditfield(app.ResultPanel, 'numeric');
            app.XShiftsmmEditField.ValueDisplayFormat = '%.2f';
            app.XShiftsmmEditField.FontSize = 9;
            app.XShiftsmmEditField.Position = [83 120 38 19];

            % Create DDDTALabel
            app.DDDTALabel = uilabel(app.ResultPanel);
            app.DDDTALabel.Position = [144 116 57 22];
            app.DDDTALabel.Text = '(DD/DTA)';

            % Create YShiftsmmEditFieldLabel
            app.YShiftsmmEditFieldLabel = uilabel(app.ResultPanel);
            app.YShiftsmmEditFieldLabel.HorizontalAlignment = 'right';
            app.YShiftsmmEditFieldLabel.FontSize = 9;
            app.YShiftsmmEditFieldLabel.Position = [11 96 59 22];
            app.YShiftsmmEditFieldLabel.Text = 'Y Shifts (mm)';

            % Create YShiftsmmEditField
            app.YShiftsmmEditField = uieditfield(app.ResultPanel, 'numeric');
            app.YShiftsmmEditField.ValueDisplayFormat = '%.2f';
            app.YShiftsmmEditField.FontSize = 9;
            app.YShiftsmmEditField.Position = [83 96 38 19];

            % Create EditField2
            app.EditField2 = uieditfield(app.ResultPanel, 'numeric');
            app.EditField2.Editable = 'off';
            app.EditField2.FontSize = 9;
            app.EditField2.BackgroundColor = [0.8 0.8 0.8];
            app.EditField2.Position = [144 97 33 18];

            % Create EditField_3
            app.EditField_3 = uieditfield(app.ResultPanel, 'numeric');
            app.EditField_3.Editable = 'off';
            app.EditField_3.FontSize = 9;
            app.EditField_3.BackgroundColor = [0.8 0.8 0.8];
            app.EditField_3.Position = [185 96 31 19];

            % Create DoseScaledLabel
            app.DoseScaledLabel = uilabel(app.ResultPanel);
            app.DoseScaledLabel.HorizontalAlignment = 'right';
            app.DoseScaledLabel.FontSize = 9;
            app.DoseScaledLabel.Position = [6 70 73 22];
            app.DoseScaledLabel.Text = 'Dose Scaled( %)';

            % Create DoseScaledEditField
            app.DoseScaledEditField = uieditfield(app.ResultPanel, 'numeric');
            app.DoseScaledEditField.ValueDisplayFormat = '%.2f';
            app.DoseScaledEditField.FontSize = 9;
            app.DoseScaledEditField.Position = [83 70 38 19];

            % Create Label_13
            app.Label_13 = uilabel(app.ResultPanel);
            app.Label_13.HorizontalAlignment = 'right';
            app.Label_13.FontSize = 9;
            app.Label_13.Position = [128 72 28 22];
            app.Label_13.Text = '%';

            % Create EditField_4
            app.EditField_4 = uieditfield(app.ResultPanel, 'numeric');
            app.EditField_4.Editable = 'off';
            app.EditField_4.FontSize = 9;
            app.EditField_4.Position = [162 75 44 18];


            % Create FilmDosimetryLabel
            app.FilmDosimetryLabel = uilabel(app.AboutTab);
            app.FilmDosimetryLabel.HorizontalAlignment = 'center';
            app.FilmDosimetryLabel.FontSize = 48;
            app.FilmDosimetryLabel.Position = [546 452 316 84];
            app.FilmDosimetryLabel.Text = 'FilmDosimetry';

            % Create SGRGHLabel
            app.SGRGHLabel = uilabel(app.AboutTab);
            app.SGRGHLabel.HorizontalAlignment = 'center';
            app.SGRGHLabel.FontSize = 18;
            app.SGRGHLabel.Interpreter = 'tex';
            app.SGRGHLabel.Position = [652 439 76 24];
            app.SGRGHLabel.Text = 'SGRGH';

            % Create Label_23
            app.Label_23 = uilabel(app.AboutTab);
            app.Label_23.HorizontalAlignment = 'center';
            app.Label_23.FontSize = 18;
            app.Label_23.Interpreter = 'tex';
            app.Label_23.Position = [654 396 74 24];
            app.Label_23.Text = '© 2024';

            % Create v12Label_4
            app.v12Label_4 = uilabel(app.AboutTab);
            app.v12Label_4.HorizontalAlignment = 'center';
            app.v12Label_4.Position = [676 417 30 22];
            app.v12Label_4.Text = 'v.1.2';

            % Show the figure after all components are created
            app.FilmDosimetryUIFigure.Visible = 'on';
        end
    end

    % App creation and deletion
    methods (Access = public)

        % Construct app
        function app = FilmDosimetry_exported

            % Create UIFigure and components
            createComponents(app)

            % Register the app with App Designer
            registerApp(app, app.FilmDosimetryUIFigure)

            % Execute the startup function
            runStartupFcn(app, @startupFcn)

            if nargout == 0
                clear app
            end
        end

        % Code that executes before app deletion
        function delete(app)

            % Delete UIFigure when app is deleted
            delete(app.FilmDosimetryUIFigure)
        end
    end
end
