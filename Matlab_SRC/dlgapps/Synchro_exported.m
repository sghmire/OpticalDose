classdef Synchro_exported < matlab.apps.AppBase

    % Properties that correspond to app components
    properties (Access = public)
        SynchronicityUIFigure     matlab.ui.Figure
        Panel_2                   matlab.ui.container.Panel
        MaxErrorEditField_2       matlab.ui.control.NumericEditField
        MaxErrorEditField_2Label  matlab.ui.control.Label
        VerticaldistancebetweenadjacentsquaresmmLabel  matlab.ui.control.Label
        UITable_2                 matlab.ui.control.Table
        Panel                     matlab.ui.container.Panel
        MaxErrorEditField         matlab.ui.control.NumericEditField
        MaxErrorEditFieldLabel    matlab.ui.control.Label
        AngleformedbytworadiitohorizontallyadjacentsquaresdegreeLabel  matlab.ui.control.Label
        UITable                   matlab.ui.control.Table
        binaryfltrEditField       matlab.ui.control.NumericEditField
        binaryfltrEditFieldLabel  matlab.ui.control.Label
        PeaksEditField            matlab.ui.control.NumericEditField
        PeaksEditFieldLabel       matlab.ui.control.Label
        CutoffEditField           matlab.ui.control.NumericEditField
        CutoffEditFieldLabel      matlab.ui.control.Label
        RunButton                 matlab.ui.control.Button
        EditField                 matlab.ui.control.EditField
        ImportFilmButton          matlab.ui.control.Button
        UIAxes                    matlab.ui.control.UIAxes
    end

    
    properties (Access = private)
        org_file
        raw_img % Description
        mainapp
        path
    end
    

    % Callbacks that handle component events
    methods (Access = private)

        % Code that executes after component creation
        function startupFcn(app, mainApp, path)
            app.mainapp = mainApp;
            app.path = path;
        end

        % Button pushed function: ImportFilmButton
        function ImportFilmButtonPushed(app, event)
            [file, app.path] = uigetfile("*.tif", "Select the film", app.path);
            app.org_file = fullfile(app.path, file);
            app.EditField.Value = app.org_file;

            app.raw_img = imread(app.org_file);
            app.raw_img = double(rgb2gray(app.raw_img(:,:,1:3)));
            if size(app.raw_img, 1) < size(app.raw_img, 2)
                app.raw_img = app.raw_img';
            end

            imshow(app.raw_img,[], 'Parent',  app.UIAxes);

            app.UITable.Data = {};
            app.UITable_2.Data = {};
        end

        % Button pushed function: RunButton
        function RunButtonPushed(app, event)
            % Load and normalize image
            img = app.raw_img;
            cutoff = app.CutoffEditField.Value;
            angle_baseline = 14.4;
            cheese_diameter = 300;
            dist_baseline = angle_baseline / 360 * pi * cheese_diameter;
            info = imfinfo(app.org_file);
            pixel_size = 25.4 / info.XResolution;
            
            % Normalize and crop image
            img = img ./ max(img(:));
            img = imadjust(img, [0.1 1.0]); % Adjust with final desired range
            img = img(cutoff+1:end-cutoff, cutoff+1:end-cutoff); % Crop white background
            
            % Find peaks in histogram for thresholding
            [peaks, locs] = findpeaks(imhist(img, app.PeaksEditField.Value));
            [max1, loc1] = max(peaks);
            [~, loc2] = max(peaks(peaks < max1));
            binary_threshold = (locs(loc1) + locs(loc2)) / 2 / 100;
            
            % Create binary image
            b = img < binary_threshold;
            b = imfill(b, 'holes');
            b = bwareaopen(b, app.binaryfltrEditField.Value);
            
            % Extract centroid data
            stats = regionprops(b, 'Centroid', 'Area');
            pts = sortrows(reshape([stats.Centroid], [2, 15])', [1, 2]); % Sort by x, then y
            
            % Distance calculations
            d = pixel_size * pdist2(pts, pts);
            
            % Calculate horizontal and vertical distances
            horizontal_distance = [d(1, 2) d(2, 3) d(3, 4) d(4, 5);
                                   d(6, 7) d(7, 8) d(8, 9) d(9, 10);
                                   d(11, 12) d(12, 13) d(13, 14) d(14, 15)];

            degrees = 360 * horizontal_distance / (pi * cheese_diameter);
            
            vertical_distance = [d(1, 6) d(2, 7) d(3, 8) d(4, 9) d(5, 10);
                                 d(6, 11) d(7, 12) d(8, 13) d(9, 14) d(10, 15)];
            
            % Max error in degrees
            max_error_2 = max(abs(degrees(:) - angle_baseline));
            %pass_ratio_2 = mean(abs(degrees(:) - angle_baseline) <= 0.5);
            
            % Populate degrees table
            app.UITable.Data = num2cell(degrees);
            app.UITable.ColumnName = {};
            app.MaxErrorEditField.Value = max_error_2;
            
            % Max error in vertical distance
            max_error_3 = max(abs(vertical_distance(:) - 42));
            %pass_ratio_3 = mean(abs(vertical_distance(:) - 42) <= 0.5);
            
            % Populate vertical distance table
            app.UITable_2.Data = num2cell(vertical_distance);
            app.UITable_2.ColumnName = {};
            app.MaxErrorEditField_2.Value = max_error_3;


            imshow(img', [], 'Parent', app.UIAxes);
            hold(app.UIAxes, 'on');
            plot( app.UIAxes,... 
                pts(1:2, 2), pts(1:2, 1), ...
                pts(2:3, 2), pts(2:3, 1), ...
                pts(3:4, 2), pts(3:4, 1), ...
                pts(4:5, 2), pts(4:5, 1), ...
                pts(6:7, 2), pts(6:7, 1), ...
                pts(7:8, 2), pts(7:8, 1), ...
                pts(8:9, 2), pts(8:9, 1), ...
                pts(9:10, 2), pts(9:10, 1), ...
                pts(11:12, 2), pts(11:12, 1), ...
                pts(12:13, 2), pts(12:13, 1), ...
                pts(13:14, 2), pts(13:14, 1), ...
                pts(14:15, 2), pts(14:15, 1), ...
                pts([1 6], 2), pts([1 6], 1), ...
                pts([2 7], 2), pts([2 7], 1), ...
                pts([3 8], 2), pts([3 8], 1), ...
                pts([4 9], 2), pts([4 9], 1), ...
                pts([5 10], 2), pts([5 10], 1), ...
                pts([6 11], 2), pts([6 11], 1), ...
                pts([7 12], 2), pts([7 12], 1), ...
                pts([8 13], 2), pts([8 13], 1), ...
                pts([9 14], 2), pts([9 14], 1), ...
                pts([10 15], 2), pts([10 15], 1), 'color', 'red');
            hold(app.UIAxes, 'off');
        end
    end

    % Component initialization
    methods (Access = private)

        % Create UIFigure and components
        function createComponents(app)

            % Create SynchronicityUIFigure and hide until all components are created
            app.SynchronicityUIFigure = uifigure('Visible', 'off');
            app.SynchronicityUIFigure.Position = [100 100 454 663];
            app.SynchronicityUIFigure.Name = 'Synchronicity';

            % Create UIAxes
            app.UIAxes = uiaxes(app.SynchronicityUIFigure);
            app.UIAxes.XTick = [];
            app.UIAxes.YTick = [];
            app.UIAxes.Box = 'on';
            app.UIAxes.Position = [15 325 434 288];

            % Create ImportFilmButton
            app.ImportFilmButton = uibutton(app.SynchronicityUIFigure, 'push');
            app.ImportFilmButton.ButtonPushedFcn = createCallbackFcn(app, @ImportFilmButtonPushed, true);
            app.ImportFilmButton.Position = [14 622 77 23];
            app.ImportFilmButton.Text = 'Import Film';

            % Create EditField
            app.EditField = uieditfield(app.SynchronicityUIFigure, 'text');
            app.EditField.Position = [96 623 350 22];

            % Create RunButton
            app.RunButton = uibutton(app.SynchronicityUIFigure, 'push');
            app.RunButton.ButtonPushedFcn = createCallbackFcn(app, @RunButtonPushed, true);
            app.RunButton.BackgroundColor = [0.8118 0.9686 0.6];
            app.RunButton.Position = [378 295 68 23];
            app.RunButton.Text = 'Run';

            % Create CutoffEditFieldLabel
            app.CutoffEditFieldLabel = uilabel(app.SynchronicityUIFigure);
            app.CutoffEditFieldLabel.HorizontalAlignment = 'right';
            app.CutoffEditFieldLabel.Position = [17 295 40 22];
            app.CutoffEditFieldLabel.Text = 'Cutoff:';

            % Create CutoffEditField
            app.CutoffEditField = uieditfield(app.SynchronicityUIFigure, 'numeric');
            app.CutoffEditField.Position = [67 295 41 22];
            app.CutoffEditField.Value = 300;

            % Create PeaksEditFieldLabel
            app.PeaksEditFieldLabel = uilabel(app.SynchronicityUIFigure);
            app.PeaksEditFieldLabel.HorizontalAlignment = 'right';
            app.PeaksEditFieldLabel.Position = [125 295 42 22];
            app.PeaksEditFieldLabel.Text = 'Peaks:';

            % Create PeaksEditField
            app.PeaksEditField = uieditfield(app.SynchronicityUIFigure, 'numeric');
            app.PeaksEditField.Position = [174 295 41 22];
            app.PeaksEditField.Value = 100;

            % Create binaryfltrEditFieldLabel
            app.binaryfltrEditFieldLabel = uilabel(app.SynchronicityUIFigure);
            app.binaryfltrEditFieldLabel.HorizontalAlignment = 'right';
            app.binaryfltrEditFieldLabel.Position = [226 295 58 22];
            app.binaryfltrEditFieldLabel.Text = 'binary fltr:';

            % Create binaryfltrEditField
            app.binaryfltrEditField = uieditfield(app.SynchronicityUIFigure, 'numeric');
            app.binaryfltrEditField.ValueDisplayFormat = '%.0f';
            app.binaryfltrEditField.Position = [289 295 61 22];
            app.binaryfltrEditField.Value = 10000;

            % Create Panel
            app.Panel = uipanel(app.SynchronicityUIFigure);
            app.Panel.BackgroundColor = [0.8 0.8 0.8];
            app.Panel.Position = [17 145 429 142];

            % Create UITable
            app.UITable = uitable(app.Panel);
            app.UITable.ColumnName = {''; ''; ''; ''};
            app.UITable.RowName = {};
            app.UITable.Position = [10 36 409 74];

            % Create AngleformedbytworadiitohorizontallyadjacentsquaresdegreeLabel
            app.AngleformedbytworadiitohorizontallyadjacentsquaresdegreeLabel = uilabel(app.Panel);
            app.AngleformedbytworadiitohorizontallyadjacentsquaresdegreeLabel.FontSize = 10;
            app.AngleformedbytworadiitohorizontallyadjacentsquaresdegreeLabel.FontWeight = 'bold';
            app.AngleformedbytworadiitohorizontallyadjacentsquaresdegreeLabel.Position = [10 110 334 22];
            app.AngleformedbytworadiitohorizontallyadjacentsquaresdegreeLabel.Text = 'Angle formed by two radii to horizontally adjacent squares [degree]:';

            % Create MaxErrorEditFieldLabel
            app.MaxErrorEditFieldLabel = uilabel(app.Panel);
            app.MaxErrorEditFieldLabel.HorizontalAlignment = 'right';
            app.MaxErrorEditFieldLabel.Position = [10 5 61 22];
            app.MaxErrorEditFieldLabel.Text = 'Max Error:';

            % Create MaxErrorEditField
            app.MaxErrorEditField = uieditfield(app.Panel, 'numeric');
            app.MaxErrorEditField.Position = [93 5 55 22];

            % Create Panel_2
            app.Panel_2 = uipanel(app.SynchronicityUIFigure);
            app.Panel_2.BackgroundColor = [0.8 0.8 0.8];
            app.Panel_2.Position = [17 14 429 116];

            % Create UITable_2
            app.UITable_2 = uitable(app.Panel_2);
            app.UITable_2.ColumnName = {''; ''; ''; ''};
            app.UITable_2.RowName = {};
            app.UITable_2.Position = [10 33 409 52];

            % Create VerticaldistancebetweenadjacentsquaresmmLabel
            app.VerticaldistancebetweenadjacentsquaresmmLabel = uilabel(app.Panel_2);
            app.VerticaldistancebetweenadjacentsquaresmmLabel.FontSize = 10;
            app.VerticaldistancebetweenadjacentsquaresmmLabel.FontWeight = 'bold';
            app.VerticaldistancebetweenadjacentsquaresmmLabel.Position = [11 85 244 22];
            app.VerticaldistancebetweenadjacentsquaresmmLabel.Text = 'Vertical distance between adjacent squares [mm]:';

            % Create MaxErrorEditField_2Label
            app.MaxErrorEditField_2Label = uilabel(app.Panel_2);
            app.MaxErrorEditField_2Label.HorizontalAlignment = 'right';
            app.MaxErrorEditField_2Label.Position = [14 4 61 22];
            app.MaxErrorEditField_2Label.Text = 'Max Error:';

            % Create MaxErrorEditField_2
            app.MaxErrorEditField_2 = uieditfield(app.Panel_2, 'numeric');
            app.MaxErrorEditField_2.Position = [96 4 55 22];

            % Show the figure after all components are created
            app.SynchronicityUIFigure.Visible = 'on';
        end
    end

    % App creation and deletion
    methods (Access = public)

        % Construct app
        function app = Synchro_exported(varargin)

            % Create UIFigure and components
            createComponents(app)

            % Register the app with App Designer
            registerApp(app, app.SynchronicityUIFigure)

            % Execute the startup function
            runStartupFcn(app, @(app)startupFcn(app, varargin{:}))

            if nargout == 0
                clear app
            end
        end

        % Code that executes before app deletion
        function delete(app)

            % Delete UIFigure when app is deleted
            delete(app.SynchronicityUIFigure)
        end
    end
end