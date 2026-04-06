classdef StartShots_exported < matlab.apps.AppBase

    % Properties that correspond to app components
    properties (Access = public)
        StartShotsUIFigure       matlab.ui.Figure
        ResultsPanel             matlab.ui.container.Panel
        NumofSpokesEditField     matlab.ui.control.NumericEditField
        NumofSpokesLabel         matlab.ui.control.Label
        CenterOffsetmmEditField  matlab.ui.control.NumericEditField
        CenterOffsetmmLabel      matlab.ui.control.Label
        ControlsPanel            matlab.ui.container.Panel
        PeakEditField            matlab.ui.control.NumericEditField
        PeakEditFieldLabel       matlab.ui.control.Label
        ChannelDropDown          matlab.ui.control.DropDown
        ChannelDropDownLabel     matlab.ui.control.Label
        PickCenterButton         matlab.ui.control.Button
        CorrectionButton         matlab.ui.control.Button
        CalculateButton          matlab.ui.control.Button
        CropButton               matlab.ui.control.Button
        DrawCircleButton         matlab.ui.control.Button
        LoadFilmButton           matlab.ui.control.Button
        UIAxes2                  matlab.ui.control.UIAxes
        UIAxes                   matlab.ui.control.UIAxes
    end

    
    properties (Access = private)
        Mainapp; % Description
        Film;
        DPI;
        Circle;
        Red_corr;
        Usr_center;
        Path;

    end
    
    methods (Access = private)        
        
        function [intersX, intersY] = intersectFinder(~, P1, P2, P6, P7)
                % Check if lines are parallel or coincident
                det1 = det([P1-P6; P7-P6]);
                det2 = det([P2-P6; P7-P6]);
                
                if abs(det1) < eps && abs(det2) < eps
                    disp('Lines are parallel or coincident');
                    intersX = NaN;
                    intersY = NaN;
                    return;
                end
                
                % Calculate intersection point
                x_numerator = det([det([P1; P6]), P1(1)-P6(1); det([P2; P7]), P2(1)-P7(1)]);
                x_denominator = det([P1(1)-P6(1), P1(2)-P6(2); P2(1)-P7(1), P2(2)-P7(2)]);
                
                y_numerator = det([det([P1; P6]), P1(2)-P6(2); det([P2; P7]), P2(2)-P7(2)]);
                y_denominator = x_denominator; % Same as for x
                
                intersX = x_numerator / x_denominator;
                intersY = y_numerator / y_denominator;
            end
        
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
    end
    

    % Callbacks that handle component events
    methods (Access = private)

        % Code that executes after component creation
        function startupFcn(app, mainapp, path)
            app.Mainapp = mainapp;
            app.Path = path;
        end

        % Button pushed function: LoadFilmButton
        function LoadFilmButtonPushed(app, event)
            [data2, path2] = uigetfile({'*.tif'}, 'Please select the image file', app.Path);
            if isequal(data2, 0)
                return;
            else
                app.Path = path2;
            end
            fullFilePath = fullfile(path2, data2);
            app.Film = imread(fullFilePath);
            app.DPI = fn_DPICalculator(fullFilePath);
            imshow(app.Film, 'Parent', app.UIAxes);
        end

        % Button pushed function: CropButton
        function CropButtonPushed(app, event)
            Rectangle = drawrectangle("Parent", app.UIAxes);
            app.Film = imcrop(app.Film, Rectangle.Position);
            imshow(app.Film , "Parent",app.UIAxes)
            colormap(app.UIAxes, "jet");            
        end

        % Button pushed function: DrawCircleButton
        function DrawCircleButtonPushed(app, event)
            app.Circle = drawcircle('Parent',app.UIAxes);
        end

        % Button pushed function: PickCenterButton
        function PickCenterButtonPushed(app, event)
            [x, y] = ProfilePoint(app, app.UIAxes);
            app.Usr_center = [x, y];
        end

        % Button pushed function: CorrectionButton
        function CorrectionButtonPushed(app, event)
            Image = double(app.Film);            
           
            Red_ch = Image(:, : ,1);
            Blue_ch = Image(:, : ,2);
            
            
            Red_norm = -log10(Red_ch ./ 65535);
            Blue_norm = -log10(Blue_ch ./ 65535);
            
            Red_corr1 = Red_norm ./Blue_norm;
            Red_corr1 = smoothdata2(Red_corr1, "movmedian", 10);
            Red_corr1(isinf(Red_corr1)) = 0;

            imshow(Red_corr1, 'Parent', app.UIAxes);
            colormap(app.UIAxes, "jet");

            app.Red_corr = Red_corr1;
        end

        % Button pushed function: CalculateButton
        function CalculateButtonPushed(app, event)
            hh = app.Circle;
            % Circle parameters
            centerX = hh.Center(1);
            centerY = hh.Center(2);
            radius = hh.Radius;
            CorrectionButtonPushed(app);
            
            % Number of points to sample along the circle
            numPoints = 1000; % For example, one point per degree
            
            % Calculate the coordinates
            theta = linspace(0, 2*pi, numPoints);
            x = centerX + radius * cos(theta);
            y = centerY + radius * sin(theta);
            
            % Interpolate to get pixel values at these coordinates
            pixelValues = interp2(double(app.Red_corr), x, y, 'linear');            
            
            plot(app.UIAxes2, theta, pixelValues);
            xlabel(app.UIAxes2, 'Angle (Radians)');
            ylabel(app.UIAxes2, 'Pixel Intensity');
            title(app.UIAxes2, 'Profile of the Circle');
            
            [~, plocs] = findpeaks(pixelValues, "MinPeakProminence", app.PeakEditField.Value);
            peakAngles = theta(plocs);
            
            % Calculate the x and y coordinates of peaks
            peakX = centerX + radius * cos(peakAngles);
            peakY = centerY + radius * sin(peakAngles);            
            
            % Assuming you have your peak locations in peakX and peakY
            points = [peakX,; peakY];
            points = points';
            
            % Calculate lines and intersections
            numPeaks = numel(peakX);
            if mod(numPeaks, 2) ~= 0
                middleIndex = ceil(numPeaks / 2);
                points(middleIndex, :) = [];
            end
            
            numPeaks = size(points, 1);    
            % fn_LineMaker(app.Film, P1, P2, app.UIAxes);

            [~, width] = size(app.Film);
            P1 = [app.Usr_center(1),1];
            P2 = [app.Usr_center(1),width ];

            imshow(app.Film, "Parent", app.UIAxes);
            hold(app.UIAxes, "on");

            % Handle cases with less than 4 peaks and odd number of peaks
            if numPeaks < 4
                msgbox('Not enough peaks for intersections (more than 4 needed)');
                return;
            else
                numLines = floor(numPeaks / 2); % Number of lines based on your logic
            
                % Flexible loop for line formation
                intersectionPoints = cell(numLines -1 , 2);
                for i = 1:numLines -1                    
                    idx3 = i +1; % Start index for second line
                    idx4 = numLines + idx3;             
                    %Extract points for the current line
                    P3 = points(idx3,:);
                    P4 = points(idx4,:);
            
                    %Find intersection (same as before)
                    [X, Y] = intersectFinder(app, P1, P3, P2, P4);
                    

                    % Store intersection point
                    intersectionPoints{i, 1} = X;
                    intersectionPoints{i, 2} = Y;
                end            
            end

            T_offset = cell(size(intersectionPoints, 1));
            for i = 1:numLines-1
                if ~isnan(intersectionPoints{i}) && ~isempty(intersectionPoints{i})
                   plot(app.UIAxes, intersectionPoints{i, 1}, intersectionPoints{i, 2}, 'o', 'MarkerEdgeColor','g', 'MarkerSize', 1, 'MarkerFaceColor','g');
                   Offset1 = sqrt(((app.Usr_center(1) - intersectionPoints{i, 1}) *  (25.4 / app.DPI)) ^ 2 + ((app.Usr_center(2) - intersectionPoints{i, 2}) * (25.4 / app.DPI)) ^ 2);
                   T_offset = Offset1;
                end
            end                  
            hold(app.UIAxes,"off");  
            app.NumofSpokesEditField.Value = numLines;
            app.CenterOffsetmmEditField.Value = mean(T_offset(:));
        end

        % Value changed function: ChannelDropDown
        function ChannelDropDownValueChanged(app, event)
        switch app.ChannelDropDown.Value
            case "None"
                imshow(app.Film, [], 'Parent', app.UIAxes);
            case "Red"
               imshow(app.Film(:, : , 1), [], 'Parent', app.UIAxes);
               colormap(app.UIAxes, 'jet');
            case "Blue"
                imshow(app.Film(:, : , 3), [], 'Parent', app.UIAxes);
                colormap(app.UIAxes, 'jet');
            case "Green"
                imshow(app.Film(:, : , 2), [], 'Parent', app.UIAxes);
                colormap(app.UIAxes, 'jet');
            case "Corrected"
                app.CorrectionButtonPushed;
        end 
        end
    end

    % Component initialization
    methods (Access = private)

        % Create UIFigure and components
        function createComponents(app)

            % Get the file path for locating images
            pathToMLAPP = fileparts(mfilename('fullpath'));

            % Create StartShotsUIFigure and hide until all components are created
            app.StartShotsUIFigure = uifigure('Visible', 'off');
            app.StartShotsUIFigure.Position = [100 100 1075 589];
            app.StartShotsUIFigure.Name = 'Start Shots';
            app.StartShotsUIFigure.Icon = fullfile(pathToMLAPP, 'dlgapps_resources', 'icons8-star-100.png');

            % Create UIAxes
            app.UIAxes = uiaxes(app.StartShotsUIFigure);
            app.UIAxes.XTick = [];
            app.UIAxes.YTick = [];
            app.UIAxes.Box = 'on';
            app.UIAxes.Position = [24 16 729 537];

            % Create UIAxes2
            app.UIAxes2 = uiaxes(app.StartShotsUIFigure);
            app.UIAxes2.Box = 'on';
            app.UIAxes2.Position = [765 38 295 177];

            % Create LoadFilmButton
            app.LoadFilmButton = uibutton(app.StartShotsUIFigure, 'push');
            app.LoadFilmButton.ButtonPushedFcn = createCallbackFcn(app, @LoadFilmButtonPushed, true);
            app.LoadFilmButton.Position = [33 552 83 23];
            app.LoadFilmButton.Text = 'Load Film';
            app.LoadFilmButton.Icon = fullfile(pathToMLAPP, 'dlgapps_resources', 'graphics_Upload.png');

            % Create ControlsPanel
            app.ControlsPanel = uipanel(app.StartShotsUIFigure);
            app.ControlsPanel.TitlePosition = 'centertop';
            app.ControlsPanel.Title = 'Controls';
            app.ControlsPanel.FontWeight = 'bold';
            app.ControlsPanel.Position = [764 374 278 162];

            % Create DrawCircleButton
            app.DrawCircleButton = uibutton(app.ControlsPanel, 'push');
            app.DrawCircleButton.ButtonPushedFcn = createCallbackFcn(app, @DrawCircleButtonPushed, true);
            app.DrawCircleButton.Position = [104 111 75 24];
            app.DrawCircleButton.Text = 'Draw Circle';

            % Create CropButton
            app.CropButton = uibutton(app.ControlsPanel, 'push');
            app.CropButton.ButtonPushedFcn = createCallbackFcn(app, @CropButtonPushed, true);
            app.CropButton.Position = [22 112 76 23];
            app.CropButton.Text = 'Crop';

            % Create CalculateButton
            app.CalculateButton = uibutton(app.ControlsPanel, 'push');
            app.CalculateButton.ButtonPushedFcn = createCallbackFcn(app, @CalculateButtonPushed, true);
            app.CalculateButton.Position = [183 11 75 23];
            app.CalculateButton.Text = 'Calculate';

            % Create CorrectionButton
            app.CorrectionButton = uibutton(app.ControlsPanel, 'push');
            app.CorrectionButton.ButtonPushedFcn = createCallbackFcn(app, @CorrectionButtonPushed, true);
            app.CorrectionButton.Position = [104 78 76 23];
            app.CorrectionButton.Text = 'Correction';

            % Create PickCenterButton
            app.PickCenterButton = uibutton(app.ControlsPanel, 'push');
            app.PickCenterButton.ButtonPushedFcn = createCallbackFcn(app, @PickCenterButtonPushed, true);
            app.PickCenterButton.Position = [22 78 75 23];
            app.PickCenterButton.Text = 'Pick Center';

            % Create ChannelDropDownLabel
            app.ChannelDropDownLabel = uilabel(app.ControlsPanel);
            app.ChannelDropDownLabel.HorizontalAlignment = 'right';
            app.ChannelDropDownLabel.Position = [19 11 50 22];
            app.ChannelDropDownLabel.Text = 'Channel';

            % Create ChannelDropDown
            app.ChannelDropDown = uidropdown(app.ControlsPanel);
            app.ChannelDropDown.Items = {'None', 'Red', 'Green', 'Blue', 'Corrected'};
            app.ChannelDropDown.ValueChangedFcn = createCallbackFcn(app, @ChannelDropDownValueChanged, true);
            app.ChannelDropDown.Position = [89 11 76 22];
            app.ChannelDropDown.Value = 'Red';

            % Create PeakEditFieldLabel
            app.PeakEditFieldLabel = uilabel(app.ControlsPanel);
            app.PeakEditFieldLabel.HorizontalAlignment = 'right';
            app.PeakEditFieldLabel.Position = [26 43 36 22];
            app.PeakEditFieldLabel.Text = 'Peak:';

            % Create PeakEditField
            app.PeakEditField = uieditfield(app.ControlsPanel, 'numeric');
            app.PeakEditField.Position = [77 43 45 22];
            app.PeakEditField.Value = 0.05;

            % Create ResultsPanel
            app.ResultsPanel = uipanel(app.StartShotsUIFigure);
            app.ResultsPanel.TitlePosition = 'centertop';
            app.ResultsPanel.Title = 'Results';
            app.ResultsPanel.FontWeight = 'bold';
            app.ResultsPanel.Position = [764 231 287 128];

            % Create CenterOffsetmmLabel
            app.CenterOffsetmmLabel = uilabel(app.ResultsPanel);
            app.CenterOffsetmmLabel.HorizontalAlignment = 'right';
            app.CenterOffsetmmLabel.Position = [11 42 111 22];
            app.CenterOffsetmmLabel.Text = 'Center Offset (mm):';

            % Create CenterOffsetmmEditField
            app.CenterOffsetmmEditField = uieditfield(app.ResultsPanel, 'numeric');
            app.CenterOffsetmmEditField.Position = [138 42 57 22];

            % Create NumofSpokesLabel
            app.NumofSpokesLabel = uilabel(app.ResultsPanel);
            app.NumofSpokesLabel.HorizontalAlignment = 'right';
            app.NumofSpokesLabel.Position = [13 74 90 22];
            app.NumofSpokesLabel.Text = 'Num of Spokes:';

            % Create NumofSpokesEditField
            app.NumofSpokesEditField = uieditfield(app.ResultsPanel, 'numeric');
            app.NumofSpokesEditField.Position = [138 74 56 22];

            % Show the figure after all components are created
            app.StartShotsUIFigure.Visible = 'on';
        end
    end

    % App creation and deletion
    methods (Access = public)

        % Construct app
        function app = StartShots_exported(varargin)

            % Create UIFigure and components
            createComponents(app)

            % Register the app with App Designer
            registerApp(app, app.StartShotsUIFigure)

            % Execute the startup function
            runStartupFcn(app, @(app)startupFcn(app, varargin{:}))

            if nargout == 0
                clear app
            end
        end

        % Code that executes before app deletion
        function delete(app)

            % Delete UIFigure when app is deleted
            delete(app.StartShotsUIFigure)
        end
    end
end