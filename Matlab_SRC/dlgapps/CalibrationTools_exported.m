classdef CalibrationTools_exported < matlab.apps.AppBase

    % Properties that correspond to app components
    properties (Access = public)
        CalibrationToolsUIFigure  matlab.ui.Figure
        FileMenu                  matlab.ui.container.Menu
        ImportRawFilmMenu         matlab.ui.container.Menu
        ImportCalibratioinMenu    matlab.ui.container.Menu
        CalibType1Menu            matlab.ui.container.Menu
        CalibType2Menu            matlab.ui.container.Menu
        ExportCalibrationMenu     matlab.ui.container.Menu
        TabGroup                  matlab.ui.container.TabGroup
        GenerateDataTab           matlab.ui.container.Tab
        STDEditField              matlab.ui.control.NumericEditField
        MeanEditField             matlab.ui.control.NumericEditField
        STDEditFieldLabel         matlab.ui.control.Label
        MeanEditFieldLabel        matlab.ui.control.Label
        Panel_4                   matlab.ui.container.Panel
        SetActiveConfigButton     matlab.ui.control.Button
        LoadConfigButton          matlab.ui.control.Button
        SaveConfigButton          matlab.ui.control.Button
        ToolsMenu                 matlab.ui.container.Menu
        SetasDefaultFitMenu       matlab.ui.container.Menu
        ContrastSlider            matlab.ui.control.RangeSlider
        ContrastSliderLabel       matlab.ui.control.Label
        R2EditField_2             matlab.ui.control.NumericEditField
        Label_2                   matlab.ui.control.Label
        R2EditField_2Label        matlab.ui.control.Label
        FitTable                  matlab.ui.control.Table
        FitButton                 matlab.ui.control.Button
        oSpinner                  matlab.ui.control.Spinner
        DegreeLabel               matlab.ui.control.Label
        ChannelDropDown           matlab.ui.control.DropDown
        ChannelLabel              matlab.ui.control.Label
        ChannelButtonGroup        matlab.ui.container.ButtonGroup
        TripleButton              matlab.ui.control.RadioButton
        DualButton                matlab.ui.control.RadioButton
        SingleButton              matlab.ui.control.RadioButton
        UITableMain               matlab.ui.control.Table
        ClearButton               matlab.ui.control.Button
        DeleteButton              matlab.ui.control.Button
        GenerateCalData           matlab.ui.control.Button
        ROI_YEditField            matlab.ui.control.NumericEditField
        FreeFormROICheckBox       matlab.ui.control.CheckBox
        ROI_YEditFieldLabel       matlab.ui.control.Label
        ROI_XEditField            matlab.ui.control.NumericEditField
        ROI_XEditFieldLabel       matlab.ui.control.Label
        Panel_2                   matlab.ui.container.Panel
        MeasureButton             matlab.ui.control.Button
        FilterButton              matlab.ui.control.Button
        FilterEditField           matlab.ui.control.NumericEditField
        Convert2DoseButton        matlab.ui.control.Button
        MedianFilterButton        matlab.ui.control.Button
        SizeEditField             matlab.ui.control.NumericEditField
        FlipVButton               matlab.ui.control.Button
        FilpHButton               matlab.ui.control.Button
        CCWButton                 matlab.ui.control.Button
        CWButton                  matlab.ui.control.Button
        Label                     matlab.ui.control.Label
        CalibLabel_2              matlab.ui.control.Label
        DropDown                  matlab.ui.control.DropDown
        UIAxes_4                  matlab.ui.control.UIAxes
        UIAxes_5                  matlab.ui.control.UIAxes
        CalibAxes                 matlab.ui.control.UIAxes
    end

    
    properties (Access = private)
        Data;
        Film;
        first_fit; second_fit;third_fit; fittype;
        Converted_Dose;
        mainapp;
        MainappTable;
        Path;
        ProjectRoot;
        raw_calib;
        datapoints = 0;
        fitValid = 0;
        channel;
        degreeoffit;
        delta_opt;
        FreeCheck = 0;
        calibrationName;
    end
    
    methods (Access = private)
        
        function [ X_pos_crs, Y_pos_crs] = ProfilePointExtact(app, X_size, Y_size, UIAxes)
                % Ensure the axes is fully rendered before drawing the crosshair
                drawnow;
                pause(0.05);

                % Draw the interactive crosshair — starts at image center
                h = drawcrosshair(UIAxes, 'LineWidth', 1.0, 'Color', 'Red');

                % BLOCK here until the user double-clicks to confirm position
                % (wait pauses execution until the ROI is deleted or committed)
                wait(h);

                % Now safely read the confirmed position
                if ~isvalid(h)
                    % User cancelled (e.g. pressed Escape) — return center of image
                    xl = UIAxes.XLim;
                    yl = UIAxes.YLim;
                    X_pos_crs = mean(xl);
                    Y_pos_crs = mean(yl);
                else
                    Position = h.Position;
                    X_pos_crs = Position(1);
                    Y_pos_crs = Position(2);

                    % Draw the fixed ROI rectangle at confirmed position
                    X1 = round(X_pos_crs - X_size / 2);
                    Y1 = round(Y_pos_crs - Y_size / 2);
                    hold(UIAxes, 'on');
                    rectangle(UIAxes, 'Position', [X1, Y1, X_size, Y_size], 'EdgeColor', 'cyan', 'LineWidth', 1.5);
                    hold(UIAxes, 'off');

                    delete(h); % remove crosshair, leave only the rectangle
                end
        end

        function adjustContrast(~, original_image, UIAxes, lowLimit, highLimit)
            % Normalize the original image data to the [0, 1] range
            norm_image = mat2gray(original_image); 
            
            % Ensure lowLimit and highLimit are valid and within the range [0, 1]
            if lowLimit >= 0 && highLimit <= 1 && lowLimit < highLimit
                % Adjust the contrast of the normalized image
                adjustedImage = imadjust(norm_image, [lowLimit, highLimit], []); 
                
                % Display the adjusted image
                imshow(adjustedImage, 'Parent', UIAxes);  % Display in the specified UI axes
            else
                % Handle invalid limits with a message or error handling
                disp('Invalid contrast limits: lowLimit must be less than highLimit and within [0, 1]');
            end
        end
    end
    

    % Callbacks that handle component events
    methods (Access = private)

        % Code that executes after component creation
        function startupFcn(app, mainapp, Table, path)
                app.mainapp = mainapp;
                app.Path = path;
                app.ProjectRoot = path; % Anchor the config path here
                
                % Add essential subfolders to the MATLAB path
                addpath(char(app.ProjectRoot)); 
                addpath(fullfile(char(app.Path), 'dlgapps'));
                addpath(fullfile(char(app.Path), 'functions'));
                
                % Initialize with empty state
                app.Label.Text = 'Ready to import calibration data';
        end




        % Menu selected function: CalibType1Menu
        function CalibType1MenuSelected(app, event)
            [filename, path] = uigetfile("*/*.txt", "please select the file", fullfile(app.ProjectRoot, 'configs'));
            filename = fullfile(path, filename);
            app.Label.Text = filename;
 
            ID = fopen(filename, 'r');
            all_lines = textscan(ID, '%s', 'Delimiter', '\n');
            all_lines = all_lines{1};
            
            begin_idx = find(strcmp(all_lines, '$BEGIN_DATA	'));
            end_idx  = find(strcmp(all_lines, '$END_DATA'));
            
            datalines = all_lines(begin_idx + 2 : end_idx -1);
            
            parsedData = cell(length(datalines), 4);
            for i = 1:length(parsedData)
                linedata = textscan(datalines{i}, '%s %f %f %f %f %f %f', 'Delimiter', '\t' );
                parsedData(i, :) = linedata(2:end-2);
            end

            fclose(ID);
            app.datapoints = 1;
            app.UITableMain.Data = parsedData;
        end

        % Menu selected function: CalibType2Menu
        function CalibType2MenuSelected(app, event)
            [filename, path] = uigetfile({'*.txt'}, 'Please select the image file', fullfile(app.ProjectRoot, 'configs'));
            file = fullfile(path, filename);
            % Read the table
            app.UITableMain.Data = readmatrix(file);        
            app.Label.Text = file;
            app.datapoints = 0;
        end

        % Menu selected function: ExportCalibrationMenu
        function ExportCalibrationMenuSelected(app, event)
             data_cell = app.UITableMain.Data;
            
            % Extracting individual columns
                Dose = data_cell(:, 1);                
                dose = cell2mat(Dose(:, 1));
                Red = data_cell(:, 2);                
                Red_ch = cell2mat(Red(:, 1));
                Green = data_cell(:, 3);
                Green_ch = cell2mat(Green(:, 1));
                Blue = data_cell(:, 4);
                Blue_ch = cell2mat(Blue(:, 1));
            
            % Prepare the full data to save, including headers
            data_to_save = [dose, Red_ch, Green_ch, Blue_ch];
            
            % Get file name and path for saving
            [filename, pathname] = uiputfile({'*.txt'}, 'Please select', fullfile(app.ProjectRoot, 'configs'));
            if filename ~= 0
                file_name = fullfile(pathname, filename);
                app.Path = pathname; % Update app's path property
            
                % Write the data as a tab-delimited text file
                writematrix(data_to_save, file_name, 'Delimiter', 'tab');     
                msgbox("Calibration saved successfully!");
            else
                disp('File saving was cancelled.');
            end
        end

        % Menu selected function: ImportRawFilmMenu
        function ImportRawFilmMenuSelected(app, event)
             if ~isempty(app.UITableMain.Data)
                question = questdlg('Data already exists in the table. Do you want to clear it?', 'TableData:');
                if strcmp(question, 'Yes')
                    app.UITableMain.Data = {};
                end
             end            
            
            [filename, filePaths] = uigetfile({'*/*.tif'}, 'Select the calibration films;', 'MultiSelect', 'on', app.Path);

            if isequal(filename,  0)
                return;
            else
                app.Path = filePaths;
            end

            if iscell(filename)
                % Read the images and store them in a cell array
                images = cell(1, numel(filename));

                for i = 1 :numel(filename)
                    img = (fullfile(filePaths, filename{i}));
                    images{i} = imrotate(imread(img), 270);
                end
                max_height = max(cellfun(@(x) size(x, 1), images)); 

                % Resize images to have the same height
                resizedImages = cellfun(@(x) imresize(x, [max_height, NaN]), images, 'UniformOutput', false);

                % Concatenate images horizontally
                concatenatedImage = cat(2, resizedImages{:});

                app.raw_calib = concatenatedImage;
                imshow(app.raw_calib, 'Parent', app.CalibAxes);
                axis(app.CalibAxes, 'image');
            else
                app.raw_calib = imread(fullfile(filePaths, filename));
                imshow(app.raw_calib, 'Parent', app.CalibAxes);
                axis(app.CalibAxes, 'image');
            end
            app.Converted_Dose = '';
        end

        % Button pushed function: CWButton
        function CWButtonPushed(app, event)
            if isempty(app.Converted_Dose)
                app.raw_calib= imrotate(app.raw_calib, 90);
                imshow(app.raw_calib, 'Parent', app.CalibAxes);
                colormap(app.CalibAxes, app.DropDown.Value);
            else
                app.Converted_Dose= imrotate(app.Converted_Dose, 90);
                imshow(app.Converted_Dose, 'Parent', app.CalibAxes);
                colormap(app.CalibAxes, app.DropDown.Value);
            end        
        end

        % Button pushed function: CCWButton
        function CCWButtonPushed(app, event)
            if isempty(app.Converted_Dose)
                app.raw_calib= imrotate(app.raw_calib, 270);
                imshow(app.raw_calib, 'Parent', app.CalibAxes);
                colormap(app.CalibAxes, app.DropDown.Value);
            else
                app.Converted_Dose= imrotate(app.Converted_Dose, 270);
                imshow(app.Converted_Dose, 'Parent', app.CalibAxes);
                colormap(app.CalibAxes, app.DropDown.Value);
            end
        end

        % Button pushed function: FilpHButton
        function FilpHButtonPushed(app, event)
            if isempty(app.Converted_Dose)
                app.raw_calib= fliplr(app.raw_calib);
                imshow(app.raw_calib, 'Parent', app.CalibAxes);
                colormap(app.CalibAxes, app.DropDown.Value);
            else
                app.Converted_Dose= fliplr(app.Converted_Dose);
                imshow(app.Converted_Dose, 'Parent', app.CalibAxes);
                colormap(app.CalibAxes, app.DropDown.Value);
            end
        end

        % Button pushed function: FlipVButton
        function FlipVButtonPushed(app, event)
            if isempty(app.Converted_Dose)
                app.raw_calib= flipud(app.raw_calib);
                imshow(app.raw_calib, 'Parent', app.CalibAxes);
                colormap(app.CalibAxes, app.DropDown.Value);
            else
                app.Converted_Dose= flipud(app.Converted_Dose);
                imshow(app.Converted_Dose, 'Parent', app.CalibAxes);
                colormap(app.CalibAxes, app.DropDown.Value);
            end
        end

        % Button pushed function: ClearButton
        function ClearButtonPushed(app, event)
            app.UITableMain.Data = {};
            app.FitButtonPushed;
        end

        % Button pushed function: DeleteButton
        function DeleteButtonPushed(app, event)
            current_Data = app.UITableMain.Data;
            if isa(current_Data, 'double')
                % If data is of type double, remove the last row
                new_Data = current_Data(1:end-1, :);
            elseif isa(current_Data, 'cell')
                % If data is a cell array, remove the last row
                new_Data = current_Data(1:end-1, :);
            elseif isa(current_Data, 'table')
                % If data is a table, remove the last row
                new_Data = current_Data(1:end-1, :);
            else
                % If the data type is unexpected, show an error or warning
                error('Unsupported data type: %s', class(current_Data));
            end
            app.UITableMain.Data = new_Data; 
            app.FitButtonPushed;
        end

        % Button pushed function: GenerateCalData
        function GenerateCalDataButtonPushed(app, event)
            % Guard: image must be loaded first
            if isempty(app.raw_calib) || ~isnumeric(app.raw_calib)
                msgbox('Please import a raw calibration film first.', 'No Image', 'warn');
                return;
            end

            if app.FreeCheck == 0
                % Re-display image and wait for axes to finish rendering
                imshow(app.raw_calib, 'Parent', app.CalibAxes);
                drawnow;
    
                % Draw blocking crosshair — execution pauses until user double-clicks
                [ crs_x , crs_y] = ProfilePointExtact(app, app.ROI_XEditField.Value, app.ROI_YEditField.Value, app.CalibAxes);
                    
                % Define ROI bounds from confirmed crosshair position
                roi_size_X = app.ROI_XEditField.Value;
                roi_size_Y = app.ROI_YEditField.Value;
    
                [nRows, nCols, ~] = size(app.raw_calib);
                X1 = max(1, round(crs_x - roi_size_X / 2));
                Y1 = max(1, round(crs_y - roi_size_Y / 2));
                X2 = min(nCols, round(crs_x + roi_size_X / 2));
                Y2 = min(nRows, round(crs_y + roi_size_Y / 2));
                    
                Dose_MU = str2double(inputdlg('Enter the corresponding Dose/MU'));

            elseif app.FreeCheck == 1
                % Re-display image and wait for axes to finish rendering
                imshow(app.raw_calib, 'Parent', app.CalibAxes);
                drawnow;
    
                ROI = drawrectangle(app.CalibAxes, 'LineWidth', 0.5, 'Color', 'Red');
                wait(ROI); % block until user double-clicks to confirm

                [nRows, nCols, ~] = size(app.raw_calib);
                X1 = max(1, round(ROI.Position(1)));
                Y1 = max(1, round(ROI.Position(2)));
                X2 = min(nCols, round(X1 + ROI.Position(3) - 1));
                Y2 = min(nRows, round(Y1 + ROI.Position(4) - 1));
                
                Dose_MU = str2double(inputdlg('Enter the corresponding Dose/MU'));
            end
    
             if  ~isempty(Dose_MU) && ~isnan(Dose_MU)
    
                    % Extracting color channels from rotated_image, not X
                    Red_channel = mean2(double(app.raw_calib(Y1:Y2, X1:X2, 1)));
                    Green_channel = mean2(double(app.raw_calib(Y1:Y2, X1:X2, 2)));
                    Blue_channel = mean2(double(app.raw_calib(Y1:Y2, X1:X2, 3)));
    
                    % Assigning values to the table
                    newRow = [Dose_MU, Red_channel, Green_channel, Blue_channel];
        
                    currentData = app.UITableMain.Data;
                    if iscell(currentData)
                        currentData = cell2mat(currentData);
                    end
                    
                    if isempty(currentData)
                        app.UITableMain.Data = newRow;
                    else
                        app.UITableMain.Data = [currentData; newRow];
                    end
             else
                    return;
             end
             app.datapoints = 1;

            app.FitButtonPushed;

        end

        % Value changed function: FreeFormROICheckBox
        function FreeFormROICheckBoxValueChanged(app, event)
           if app.FreeFormROICheckBox.Value == 1
             app.ROI_XEditField.Editable = 'off';
             app.ROI_YEditField.Editable = 'off'; 
             app.FreeCheck = 1;
           else
             app.ROI_XEditField.Editable = 'on';
             app.ROI_YEditField.Editable = 'on';
             app.FreeCheck = 0;
           end
        end

        % Button pushed function: FilterButton
        function FilterButtonPushed(app, event)
            filter_value = app.FilterEditField.Value;
            app.Converted_Dose(isnan(app.Converted_Dose) | isinf(app.Converted_Dose) | abs(app.Converted_Dose) > filter_value) = 1 ;
            imshow(app.Converted_Dose, 'Parent', app.CalibAxes);
            colormap(app.CalibAxes, app.DropDown.Value);
        end

        % Button pushed function: MeasureButton
        function MeasureButtonPushed(app, event)
           % Use Converted_Dose if available, otherwise fall back to raw_calib
           if isnumeric(app.Converted_Dose) && ~isempty(app.Converted_Dose)
               targetImage = app.Converted_Dose;
           elseif isnumeric(app.raw_calib) && ~isempty(app.raw_calib)
               % Raw film: use mean across colour channels as the signal to measure
               targetImage = double(mean(app.raw_calib, 3));
           else
               msgbox('Please import a film first.', 'No Image', 'warn');
               return;
           end

           cla(app.UIAxes_5);
           ROI = drawrectangle('Parent', app.CalibAxes, 'LineWidth', 0.5, 'Color', 'r');

           [nRows, nCols] = size(targetImage);

           X1 = max(1,     round(ROI.Position(1)));
           Y1 = max(1,     round(ROI.Position(2)));
           X2 = min(nCols, round(ROI.Position(1) + ROI.Position(3) - 1));
           Y2 = min(nRows, round(ROI.Position(2) + ROI.Position(4) - 1));

           if X1 > X2 || Y1 > Y2
               msgbox('ROI is fully outside the image. Please redraw within the image.', 'Invalid ROI', 'warn');
               return;
           end

           ROI_mat = targetImage(Y1:Y2, X1:X2);
           app.MeanEditField.Value  = mean(ROI_mat(:));
           app.STDEditField.Value   = std(ROI_mat(:));
           histogram(app.UIAxes_5, ROI_mat);
        end


        % Button pushed function: Convert2DoseButton
        function Convert2DoseButtonPushed(app, event)
            if app.fitValid == 0
                msgbox('No valid calibration for dose conversion!');
                return;
            end

             Red_channel = double(app.raw_calib(:,:,1));
             Red_norm= -log10(Red_channel./ 65535 + eps);
             Green_channel = double(app.raw_calib(:,:,2));
             Green_norm = -log10(Green_channel ./ 65535 + eps);
             Blue_channel = double(app.raw_calib(:,:,3));
             Blue_norm = -log10(Blue_channel ./ 65535 + eps);

            switch app.fittype
                case 'Red'
                   app.Converted_Dose = polyval(app.first_fit,  Red_norm);

                case 'Green'
                   app.Converted_Dose = polyval(app.first_fit,  Green_norm);

                case 'Blue'
                    app.Converted_Dose = polyval(app.first_fit,  Blue_norm);

                case 'Red/Blue'
                    Red_blue_corr = Red_norm ./ Blue_norm;
                    first_Dose = polyval(app.first_fit,  Red_blue_corr);
                    app.Converted_Dose = polyval(app.second_fit, first_Dose);

                case 'Green/Blue'
                    Green_blue_corr = Green_norm ./ Blue_norm;
                    first_Dose = polyval(app.first_fit,  Green_blue_corr);
                    app.Converted_Dose = polyval(app.second_fit, first_Dose);

                case 'Red | Green | Blue'
                    Red_dose = polyval(app.first_fit, Red_norm);
                    Green_dose = polyval(app.second_fit, Green_norm );
                    Blue_dose = polyval(app.third_fit, Blue_norm);
                    app.Converted_Dose = (Red_dose + Green_dose + Blue_dose ) .* app.delta_opt .* 1/3;
            end
            
            imshow(app.Converted_Dose,[], 'Parent', app.CalibAxes);
            colormap(app.CalibAxes, app.DropDown.Value);
        end

        % Button pushed function: FitButton
        function FitButtonPushed(app, event)
            if isempty(app.UITableMain.Data)
                return;
            end
            % Clear the axes (assuming it’s meant for plotting or refreshing)
            cla(app.UIAxes_4);
            
            % Renaming the table column names based on polynomial degree
            degree = app.oSpinner.Value;
            column_names = cell(1, degree + 1);  % Add +1 for the constant term
            
            for i = 0 : (degree - 1)
                column_names{i+1} = ['X^', num2str(degree - i)]; 
            end      
            column_names{end} = 'C';  % Constant term
            
            app.FitTable.ColumnName = column_names;
            
            % Read and process data from UITableMain
            data_cell = app.UITableMain.Data;
            
            % Robustly extract numeric matrix regardless of source (file or manual entry)
            if iscell(data_cell)
                data_mat = cell2mat(data_cell);
            else
                data_mat = data_cell;
            end
            
            Dose = data_mat(:, 1);
            Red = data_mat(:, 2);
            Green = data_mat(:, 3);
            Blue = data_mat(:, 4);
            
            % Normalize the color channels
            Red_norm = -log10(max(Red, 1) ./ 65535);
            Green_norm = -log10(max(Green, 1) ./ 65535);
            Blue_norm = -log10(max(Blue, 1) ./ 65535);
            
            Red_corr = Red_norm ./ Blue_norm;
            Green_corr = Green_norm ./ Blue_norm;
            
            % Initialize securely to avoid "Unrecognized Variable" crashes if switch bypasses
            Dose_fit = []; 
            
            % Polynomial fitting based on selected channel
            switch app.ChannelButtonGroup.SelectedObject.Text
                case 'Single'
                    if strcmp(app.ChannelDropDown.Value, 'Red')
                        app.first_fit = polyfit(Red_norm, Dose, degree); 
                        Channel_data = Red_norm;      

                        % Updating FitTable with polynomial coefficients
                        fit_table_data = num2cell(app.first_fit);  
                        app.FitTable.Data = fit_table_data;
                        
                        % Calculate the fitted Dose values
                        Dose_fit = polyval(app.first_fit, Channel_data);

                        app.fitValid = 1;  
                    elseif strcmp(app.ChannelDropDown.Value, 'Green')
                        app.first_fit = polyfit(Green_norm, Dose, degree);  
                        Channel_data = Green_norm; 
         
                        % Updating FitTable with polynomial coefficients
                        fit_table_data = num2cell(app.first_fit);  
                        app.FitTable.Data = fit_table_data;
                        
                        % Calculate the fitted Dose values
                        Dose_fit = polyval(app.first_fit, Channel_data);

                        app.fitValid = 1;  
          
                    elseif strcmp(app.ChannelDropDown.Value, 'Blue')
                        app.first_fit = polyfit(Blue_norm, Dose, degree);  
                        Channel_data = Blue_norm; 

                        % Updating FitTable with polynomial coefficients
                        fit_table_data = num2cell(app.first_fit);  
                        app.FitTable.Data = fit_table_data;
                        
                        % Calculate the fitted Dose values
                        Dose_fit = polyval(app.first_fit, Channel_data);

                        app.fitValid = 1;  

                    else
                        app.fitValid = 0;  
          
                    end

                case 'Dual'
                    if strcmp(app.ChannelDropDown.Value, 'Red/Blue')
                        app.first_fit = polyfit(Red_corr, Red_norm, degree);  
                        val_1 = polyval(app.first_fit, Red_corr);
                        app.second_fit = polyfit(val_1, Dose, degree); 

                        % Updating FitTable with polynomial coefficients
                        fit_table_data = num2cell(app.second_fit);  
                        app.FitTable.Data = fit_table_data;
                        
                        % Calculate the fitted Dose values
                        Dose_fit = polyval(app.second_fit, val_1);

                        app.fitValid = 1;  
          
                    elseif strcmp(app.ChannelDropDown.Value, 'Green/Blue')
                        app.first_fit = polyfit(Green_corr, Green_norm, degree);  
                        val_1 = polyval(app.first_fit, Green_corr);
                        app.second_fit = polyfit(val_1, Dose, degree); 

                        % Updating FitTable with polynomial coefficients
                        fit_table_data = num2cell(app.second_fit);  
                        app.FitTable.Data = fit_table_data;
                        
                        % Calculate the fitted Dose values
                        Dose_fit = polyval(app.second_fit, val_1);    

                        app.fitValid = 1;  
                    else
                        app.fitValid = 0;  
                    end

                case 'Triple'
                   % Fit polynomials for each channel
                    app.first_fit = polyfit(Red_norm, Dose, degree);
                    val_1 = polyval(app.first_fit, Red_norm);
                    
                    app.second_fit = polyfit(Green_norm, Dose, degree);
                    val_2 = polyval(app.second_fit, Green_norm);
                    
                    app.third_fit = polyfit(Blue_norm, Dose, degree);
                    val_3 = polyval(app.third_fit, Blue_norm);
                    
                    % Objective function including the blue channel
                    objective = @(delta) sum((polyval(app.first_fit, Red_norm * delta) - ...
                                  polyval(app.second_fit, Green_norm * delta)).^2) + ...
                             sum((polyval(app.first_fit, Red_norm * delta) - ...
                                  polyval(app.third_fit, Blue_norm * delta)).^2) + ...
                             sum((polyval(app.second_fit, Green_norm * delta) - ...
                                  polyval(app.third_fit, Blue_norm * delta)).^2);


                    % Optimize delta
                    delta_initial = 1;
                    lb = 0.8; 
                    ub = 1.2;
                    app.delta_opt = fmincon(objective, delta_initial, [], [], [], [], lb, ub);
                    

                    % Compute the average dose fit, scaled by the optimized delta
                    Dose_fit = (val_1 + val_2 + val_3) .* (1/3) .* app.delta_opt;

                    data = [app.first_fit; app.second_fit,; app.third_fit];
                    app.FitTable.Data = data;        

                    if ~isempty(data)
                        app.fitValid = 1;
                    else
                        app.fitValid = 0;
                    end
            end
            
            if isempty(Dose_fit)
                app.fitValid = 0;
                % Exit before plotting if the dropdown channel didn't precisely 
                % match any of the mathematical fit equations!
                return;
            end

            %$Plotting
            plot(app.UIAxes_4, Dose_fit, Dose, 'o-'); 

            % Update the app with fit data
            app.fittype = app.ChannelDropDown.Value;
            app.channel = app.ChannelButtonGroup.SelectedObject.Text;
            app.degreeoffit = app.oSpinner.Value;                               

            % Calculate R-squared with shape safety
            Dose = Dose(:); 
            Dose_fit = Dose_fit(:);
            
            SS_tot = sum((Dose - mean(Dose)).^2);  
            SS_res = sum((Dose - Dose_fit).^2);    
            
            if SS_tot > 0
                R_squared = 1 - (SS_res / SS_tot);
            else
                R_squared = 0; % Or 1, depending on definition, but 0 is safer for "No variance"
            end
            
            % Final scalar check for UI assignment
            if isempty(R_squared) || isnan(R_squared) || isinf(R_squared)
                R_squared = 0;
            end

            % Update the R2 value in the edit field
            app.R2EditField_2.Value = double(R_squared(1)); 

            if R_squared >= 0.9995
                app.R2EditField_2.BackgroundColor = 'Green';
            elseif R_squared <= 0.9995 && R_squared >= 0.9990
                app.R2EditField_2.BackgroundColor = 'Yellow';
            else
                app.R2EditField_2.BackgroundColor = 'Red';
            end            

            % Ensure any newly created function files are recognized by MATLAB
            rehash;

            % FIT BUTTON: Only update main app temporarily. Do NOT save to disk yet.
            app.calibrationName = 'Local Fit (Not Saved)';
            app.mainapp.updateFits(app.first_fit, app.second_fit, app.third_fit, app.delta_opt, ...
                                    app.channel, app.fittype, app.degreeoffit, app.calibrationName);
        end

        % Button pushed function: SetActiveConfigButton
        function SetActiveConfigButtonPushed(app, event)
            if isempty(app.first_fit)
                msgbox('Please press Fit first.', 'No Fit Found', 'warn');
                return;
            end
            
            % Push current fit to main app (session only — no file written)
            app.calibrationName = 'Active (Session)';
            app.mainapp.updateFits(app.first_fit, app.second_fit, app.third_fit, app.delta_opt, ...
                                    app.channel, app.fittype, app.degreeoffit, app.calibrationName);
            msgbox('Calibration set as active for this session.', 'Active');
        end

        % Button pushed function: SetDefaultConfigButton
        function SetDefaultConfigButtonPushed(app, event)
            if isempty(app.first_fit)
                msgbox('Please press Fit first before saving.', 'No Fit Found', 'warn');
                return;
            end
            
            numData = app.UITableMain.Data;
            if iscell(numData), numData = str2double(string(numData)); end
            
            % Save to CalibConfig_Default.txt
            rehash;
            fn_writeCalFile(char(app.ProjectRoot), 'CalibConfig_Default.txt', app.channel, app.fittype, app.degreeoffit, ...
                            app.first_fit, app.second_fit, app.third_fit, app.delta_opt, numData);
            
            % Update main app dropdown is handled by the main app logic
            msgbox('Fit saved as Default Calibration.', 'Success');
        end

        % Menu selected function: LoadConfigMenu
        function LoadConfigButtonPushed(app, event)
            [filename, pathname] = uigetfile('*.txt', 'Load Config File', fullfile(app.ProjectRoot, 'configs'));
            if isequal(filename, 0)
                return;
            end
            
            filepath = fullfile(pathname, filename);
            try
                [app.channel, app.fittype, app.degreeoffit, app.first_fit, app.second_fit, app.third_fit, app.delta_opt, rawData] = fn_readCalFile(filepath);
                
                if ~isempty(rawData)
                    app.UITableMain.Data = rawData;
                end
                
                if ~isempty(app.first_fit)
                    % Safely assign channel dropdown
                    loadedChannel = char(strtrim(app.channel));
                    if ismember(loadedChannel, app.ChannelDropDown.Items)
                        app.ChannelDropDown.Value = loadedChannel;
                    elseif ~isempty(loadedChannel)
                        % If not in the list, forcibly append it so it doesn't crash
                        app.ChannelDropDown.Items = [app.ChannelDropDown.Items, {loadedChannel}];
                        app.ChannelDropDown.Value = loadedChannel;
                    end
                    
                    % Safely assign spinner
                    if isnumeric(app.degreeoffit) && app.degreeoffit >= app.oSpinner.Limits(1) && app.degreeoffit <= app.oSpinner.Limits(2)
                        app.oSpinner.Value = double(app.degreeoffit);
                    end
                    
                    % Visually align radio buttons with the file's saved FitMode setting
                    app.fittype = strtrim(char(app.fittype));
                    if contains(lower(app.fittype), 'single')
                         app.ChannelButtonGroup.SelectedObject = app.SingleButton;
                    elseif contains(lower(app.fittype), 'triple')
                         app.ChannelButtonGroup.SelectedObject = app.TripleButton;
                    end
                    
                    % Explicitly render the loaded polynomial coefficients into the UI Table. 
                    % This avoids FitButtonPushed silently discarding them or crashing when 
                    % a Config file is loaded that only contains Poly Fits (no Raw Scatter Data).
                    degreeNum = double(app.degreeoffit(1));
                    colNames = cell(1, degreeNum + 1);
                    for i = 0 : (degreeNum - 1)
                        colNames{i+1} = ['X^', num2str(degreeNum - i)]; 
                    end      
                    colNames{end} = 'C';
                    app.FitTable.ColumnName = colNames;
                    
                    fitMatrix = app.first_fit;
                    if ~isempty(app.second_fit), fitMatrix(2, :) = app.second_fit; end
                    if ~isempty(app.third_fit),  fitMatrix(3, :) = app.third_fit;  end
                    app.FitTable.Data = num2cell(fitMatrix);
                    app.fitValid = 1;
                    
                    % We only trigger a full Scatter redraw if the file actually provided Raw Data
                    if ~isempty(rawData)
                        app.FitButtonPushed;
                    end
                else
                    app.Label.Text = 'Raw Template Loaded (No fit info found).';
                end
                msgbox('Config loaded successfully.', 'Success');
            catch ME
                errordlg(['Failed to load Config File: ', ME.message], 'Read Error');
            end
        end

        % Menu selected function: SaveConfigMenu
        function SaveConfigButtonPushed(app, event)
            try
                numData = app.UITableMain.Data;
                if iscell(numData), numData = str2double(string(numData)); end
                
                [filename, pathname] = uiputfile('*.txt', 'Save Config File As', fullfile(app.ProjectRoot, 'configs'));
                if isequal(filename, 0)
                    return;
                end
                
                % Write combined data and fit elements
                fn_writeCalFile(pathname, filename, app.channel, app.fittype, app.degreeoffit, ...
                            app.first_fit, app.second_fit, app.third_fit, app.delta_opt, numData);
                            
                app.Label.Text = 'Config Saved';
                msgbox('Config File saved successfully.', 'Success');
            catch ME
                errordlg(['Failed to save config: ', ME.message], 'Save Error');
            end
        end




        % Button pushed function: MedianFilterButton
        function MedianFilterButtonPushed(app, event)
           if numel(size(app.raw_calib)) > 2
                R = app.raw_calib(:, :, 1);
                G = app.raw_calib(:, :, 2);
                B = app.raw_calib(:, :, 3);

                % Apply median filter to each channel
                R_filtered = medfilt2(R, [app.SizeEditField.Value app.SizeEditField.Value]);
                G_filtered = medfilt2(G, [app.SizeEditField.Value app.SizeEditField.Value]);
                B_filtered = medfilt2(B, [app.SizeEditField.Value app.SizeEditField.Value]);

                % Combine the channels back into one image
                new_Image = cat(3, R_filtered, G_filtered, B_filtered);
            else
                new_Image = medfilt2(app.raw_calib, [app.SizeEditField.Value app.SizeEditField.Value]);
            end

            % Update the image display
            app.raw_calib = new_Image;
            imshow(app.raw_calib, 'Parent', app.CalibAxes);
        end

        % Value changing function: ContrastSlider
        function ContrastSliderValueChanging(app, event)
            fn_UpdateContrast(app.ContrastSlider, app.CalibAxes, app.Converted_Dose, event.Value, app.DropDown.Value);              
        end

        % Value changed function: DropDown
        function DropDownValueChanged(app, event)
            switch app.DropDown.Value
                case {'jet', 'bone', 'sky'}
                    imshow(app.Converted_Dose,[], 'Parent', app.CalibAxes);
                    colormap(app.CalibAxes, app.DropDown.Value);     
                case {'Red', 'Green', 'Blue'}
                    if strcmp(app.DropDown.Value, 'Red')
                        imshow(app.raw_calib(:, :, 1), [], 'Parent', app.CalibAxes);
                        colormap(app.CalibAxes, 'jet');  
                    elseif strcmp(app.DropDown.Value, 'Green')
                         imshow(app.raw_calib(:, :, 2), [], 'Parent', app.CalibAxes);
                         colormap(app.CalibAxes, 'parula');  
                    elseif strcmp(app.DropDown.Value, 'Blue')
                        imshow(app.raw_calib(:, :, 3), [], 'Parent', app.CalibAxes);  
                        colormap(app.CalibAxes, 'abyss');  
                    end
            end
        end

        % Selection changed function: ChannelButtonGroup
        function ChannelButtonGroupSelectionChanged(app, event)
            selectedButton = app.ChannelButtonGroup.SelectedObject.Text;
            if strcmp(selectedButton, 'Single')
                app.ChannelDropDown.Items = {'Red', 'Green', 'Blue'};
            elseif strcmp(selectedButton, 'Dual')
                app.ChannelDropDown.Items = {'Red/Blue', 'Green/Blue'};
            elseif strcmp(selectedButton, 'Triple')
                app.ChannelDropDown.Items = {'Red | Green | Blue'};
            end    
        end
    end

    % Component initialization
    methods (Access = private)

        % Create UIFigure and components
        function createComponents(app)

            % Get the file path for locating images
            pathToMLAPP = fileparts(mfilename('fullpath'));

            % Create CalibrationToolsUIFigure and hide until all components are created
            app.CalibrationToolsUIFigure = uifigure('Visible', 'off');
            app.CalibrationToolsUIFigure.Color = [0.902 0.902 0.902];
            app.CalibrationToolsUIFigure.Position = [100 100 1077 835];
            app.CalibrationToolsUIFigure.Name = 'Calibration Tools';
            app.CalibrationToolsUIFigure.Icon = fullfile(pathToMLAPP, 'dlgapps_resources', 'icons8-wrench-100.png');

            % Create FileMenu
            app.FileMenu = uimenu(app.CalibrationToolsUIFigure);
            app.FileMenu.Text = 'File';

            % Create ImportRawFilmMenu
            app.ImportRawFilmMenu = uimenu(app.FileMenu);
            app.ImportRawFilmMenu.MenuSelectedFcn = createCallbackFcn(app, @ImportRawFilmMenuSelected, true);
            app.ImportRawFilmMenu.Text = 'Import Raw Film';

            % Create ImportCalibratioinMenu
            app.ImportCalibratioinMenu = uimenu(app.FileMenu);
            app.ImportCalibratioinMenu.Text = 'Import Calibratioin';

            % Create CalibType1Menu
            app.CalibType1Menu = uimenu(app.ImportCalibratioinMenu);
            app.CalibType1Menu.MenuSelectedFcn = createCallbackFcn(app, @CalibType1MenuSelected, true);
            app.CalibType1Menu.Text = 'Calib(Type1)';

            % Create CalibType2Menu
            app.CalibType2Menu = uimenu(app.ImportCalibratioinMenu);
            app.CalibType2Menu.MenuSelectedFcn = createCallbackFcn(app, @CalibType2MenuSelected, true);
            app.CalibType2Menu.Text = 'Calib(Type2)';

            % Create ExportCalibrationMenu
            app.ExportCalibrationMenu = uimenu(app.FileMenu);
            app.ExportCalibrationMenu.MenuSelectedFcn = createCallbackFcn(app, @ExportCalibrationMenuSelected, true);
            app.ExportCalibrationMenu.Text = 'Export Calibration';

            % Create ToolsMenu
            app.ToolsMenu = uimenu(app.CalibrationToolsUIFigure);
            app.ToolsMenu.Text = 'Tools';

            % Create SetasDefaultFitMenu
            app.SetasDefaultFitMenu = uimenu(app.ToolsMenu);
            app.SetasDefaultFitMenu.MenuSelectedFcn = createCallbackFcn(app, @SetDefaultConfigButtonPushed, true);
            app.SetasDefaultFitMenu.Text = 'Set as Default Fit';

            % (Config Buttons moved to Panel_4 directly)

            % Create TabGroup
            app.TabGroup = uitabgroup(app.CalibrationToolsUIFigure);
            app.TabGroup.Position = [2 19 1069 816];

            % Create GenerateDataTab
            app.GenerateDataTab = uitab(app.TabGroup);
            app.GenerateDataTab.Title = 'Generate Data';

            % Create CalibAxes
            app.CalibAxes = uiaxes(app.GenerateDataTab);
            app.CalibAxes.XTick = [];
            app.CalibAxes.XTickLabel = '';
            app.CalibAxes.YTick = [];
            app.CalibAxes.Box = 'on';
            app.CalibAxes.Position = [12 178 711 584];

            % Create UIAxes_5
            app.UIAxes_5 = uiaxes(app.GenerateDataTab);
            app.UIAxes_5.Box = 'on';
            app.UIAxes_5.Position = [12 39 284 134];

            % Create UIAxes_4
            app.UIAxes_4 = uiaxes(app.GenerateDataTab);
            app.UIAxes_4.Box = 'on';
            app.UIAxes_4.Position = [308 10 415 167];

            % Create DropDown
            app.DropDown = uidropdown(app.GenerateDataTab);
            app.DropDown.Items = {'jet', 'bone', 'sky', 'Red', 'Green', 'Blue'};
            app.DropDown.ValueChangedFcn = createCallbackFcn(app, @DropDownValueChanged, true);
            app.DropDown.Position = [12 761 67 22];
            app.DropDown.Value = 'jet';

            % Create CalibLabel_2
            app.CalibLabel_2 = uilabel(app.GenerateDataTab);
            app.CalibLabel_2.Position = [434 761 36 22];
            app.CalibLabel_2.Text = 'Calib:';

            % Create Label
            app.Label = uilabel(app.GenerateDataTab);
            app.Label.Position = [476 761 247 22];
            app.Label.Text = '';

            % Create Panel_2
            app.Panel_2 = uipanel(app.GenerateDataTab);
            app.Panel_2.BackgroundColor = [0.8 0.8 0.8];
            app.Panel_2.Position = [731 710 323 73];

            % Create CWButton
            app.CWButton = uibutton(app.Panel_2, 'push');
            app.CWButton.ButtonPushedFcn = createCallbackFcn(app, @CWButtonPushed, true);
            app.CWButton.BackgroundColor = [0.9412 0.9412 0.9412];
            app.CWButton.Position = [5 42 44 24];
            app.CWButton.Text = 'CW';

            % Create CCWButton
            app.CCWButton = uibutton(app.Panel_2, 'push');
            app.CCWButton.ButtonPushedFcn = createCallbackFcn(app, @CCWButtonPushed, true);
            app.CCWButton.Position = [50 43 45 24];
            app.CCWButton.Text = 'CCW';

            % Create FilpHButton
            app.FilpHButton = uibutton(app.Panel_2, 'push');
            app.FilpHButton.ButtonPushedFcn = createCallbackFcn(app, @FilpHButtonPushed, true);
            app.FilpHButton.Position = [109 42 44 25];
            app.FilpHButton.Text = 'Filp H';

            % Create FlipVButton
            app.FlipVButton = uibutton(app.Panel_2, 'push');
            app.FlipVButton.ButtonPushedFcn = createCallbackFcn(app, @FlipVButtonPushed, true);
            app.FlipVButton.Position = [155 42 45 25];
            app.FlipVButton.Text = 'Flip V';

            % Create SizeEditField
            app.SizeEditField = uieditfield(app.Panel_2, 'numeric');
            app.SizeEditField.Position = [213 44 25 22];
            app.SizeEditField.Value = 3;

            % Create MedianFilterButton
            app.MedianFilterButton = uibutton(app.Panel_2, 'push');
            app.MedianFilterButton.ButtonPushedFcn = createCallbackFcn(app, @MedianFilterButtonPushed, true);
            app.MedianFilterButton.BackgroundColor = [1 1 1];
            app.MedianFilterButton.Position = [238 43 81 24];
            app.MedianFilterButton.Text = 'Median Filter';

            % Create Convert2DoseButton
            app.Convert2DoseButton = uibutton(app.Panel_2, 'push');
            app.Convert2DoseButton.ButtonPushedFcn = createCallbackFcn(app, @Convert2DoseButtonPushed, true);
            app.Convert2DoseButton.BackgroundColor = [0.702 0.9686 0.6784];
            app.Convert2DoseButton.Position = [4 7 91 23];
            app.Convert2DoseButton.Text = 'Convert2Dose';

            % Create FilterEditField
            app.FilterEditField = uieditfield(app.Panel_2, 'numeric');
            app.FilterEditField.Position = [103 7 45 22];
            app.FilterEditField.Value = 3000;

            % Create FilterButton
            app.FilterButton = uibutton(app.Panel_2, 'push');
            app.FilterButton.ButtonPushedFcn = createCallbackFcn(app, @FilterButtonPushed, true);
            app.FilterButton.Position = [144 7 78 23];
            app.FilterButton.Text = 'Filter';

            % Create MeasureButton
            app.MeasureButton = uibutton(app.Panel_2, 'push');
            app.MeasureButton.ButtonPushedFcn = createCallbackFcn(app, @MeasureButtonPushed, true);
            app.MeasureButton.Position = [238 7 81 23];
            app.MeasureButton.Text = 'Measure';

            % Create Panel_4
            app.Panel_4 = uipanel(app.GenerateDataTab);
            app.Panel_4.BackgroundColor = [0.8 0.8 0.8];
            app.Panel_4.Position = [731 10 323 688];

            % Create ROI_XEditFieldLabel
            app.ROI_XEditFieldLabel = uilabel(app.Panel_4);
            app.ROI_XEditFieldLabel.HorizontalAlignment = 'right';
            app.ROI_XEditFieldLabel.Position = [9 658 41 22];
            app.ROI_XEditFieldLabel.Text = 'ROI_X';

            % Create ROI_XEditField
            app.ROI_XEditField = uieditfield(app.Panel_4, 'numeric');
            app.ROI_XEditField.Position = [59 658 37 22];
            app.ROI_XEditField.Value = 40;

            % Create ROI_YEditFieldLabel
            app.ROI_YEditFieldLabel = uilabel(app.Panel_4);
            app.ROI_YEditFieldLabel.HorizontalAlignment = 'right';
            app.ROI_YEditFieldLabel.Position = [108 657 41 22];
            app.ROI_YEditFieldLabel.Text = 'ROI_Y';

            % Create FreeFormROICheckBox
            app.FreeFormROICheckBox = uicheckbox(app.Panel_4);
            app.FreeFormROICheckBox.ValueChangedFcn = createCallbackFcn(app, @FreeFormROICheckBoxValueChanged, true);
            app.FreeFormROICheckBox.Text = 'Free Form ROI';
            app.FreeFormROICheckBox.Position = [213 658 103 22];

            % Create ROI_YEditField
            app.ROI_YEditField = uieditfield(app.Panel_4, 'numeric');
            app.ROI_YEditField.Position = [163 657 37 22];
            app.ROI_YEditField.Value = 40;

            % Create GenerateCalData
            app.GenerateCalData = uibutton(app.Panel_4, 'push');
            app.GenerateCalData.ButtonPushedFcn = createCallbackFcn(app, @GenerateCalDataButtonPushed, true);
            app.GenerateCalData.BackgroundColor = [1 1 1];
            app.GenerateCalData.Tooltip = {''};
            app.GenerateCalData.Position = [99 612 124 35];
            app.GenerateCalData.Text = 'Generate Data Points';

            % Create DeleteButton
            app.DeleteButton = uibutton(app.Panel_4, 'push');
            app.DeleteButton.ButtonPushedFcn = createCallbackFcn(app, @DeleteButtonPushed, true);
            app.DeleteButton.BackgroundColor = [1 1 1];
            app.DeleteButton.Position = [10 586 43 22];
            app.DeleteButton.Text = 'Delete';

            % Create ClearButton
            app.ClearButton = uibutton(app.Panel_4, 'push');
            app.ClearButton.ButtonPushedFcn = createCallbackFcn(app, @ClearButtonPushed, true);
            app.ClearButton.BackgroundColor = [1 1 1];
            app.ClearButton.Position = [270 586 43 22];
            app.ClearButton.Text = 'Clear';

            % Create UITableMain
            app.UITableMain = uitable(app.Panel_4);
            app.UITableMain.ColumnName = {'Dose'; 'Red'; 'Green'; 'Blue'};
            app.UITableMain.RowName = {};
            app.UITableMain.ColumnSortable = true;
            app.UITableMain.ColumnEditable = true;
            app.UITableMain.Position = [9 423 304 160];

            % Create ChannelButtonGroup
            app.ChannelButtonGroup = uibuttongroup(app.Panel_4);
            app.ChannelButtonGroup.SelectionChangedFcn = createCallbackFcn(app, @ChannelButtonGroupSelectionChanged, true);
            app.ChannelButtonGroup.Title = 'Channel';
            app.ChannelButtonGroup.Position = [11 357 303 58];

            % Create SingleButton
            app.SingleButton = uiradiobutton(app.ChannelButtonGroup);
            app.SingleButton.Text = 'Single';
            app.SingleButton.Position = [11 6 55 22];
            app.SingleButton.Value = true;

            % Create DualButton
            app.DualButton = uiradiobutton(app.ChannelButtonGroup);
            app.DualButton.Text = 'Dual';
            app.DualButton.Position = [115 6 47 22];

            % Create TripleButton
            app.TripleButton = uiradiobutton(app.ChannelButtonGroup);
            app.TripleButton.Text = 'Triple';
            app.TripleButton.Position = [222 6 52 22];

            % Create ChannelLabel
            app.ChannelLabel = uilabel(app.Panel_4);
            app.ChannelLabel.HorizontalAlignment = 'right';
            app.ChannelLabel.FontWeight = 'bold';
            app.ChannelLabel.Position = [11 324 56 22];
            app.ChannelLabel.Text = 'Channel:';

            % Create ChannelDropDown
            app.ChannelDropDown = uidropdown(app.Panel_4);
            app.ChannelDropDown.Items = {'Red', 'Green', 'Blue'};
            app.ChannelDropDown.FontSize = 9;
            app.ChannelDropDown.FontWeight = 'bold';
            app.ChannelDropDown.Position = [74 324 90 22];
            app.ChannelDropDown.Value = 'Red';

            % Create DegreeLabel
            app.DegreeLabel = uilabel(app.Panel_4);
            app.DegreeLabel.FontWeight = 'bold';
            app.DegreeLabel.Position = [195 324 50 22];
            app.DegreeLabel.Text = 'Degree:';

            % Create oSpinner
            app.oSpinner = uispinner(app.Panel_4);
            app.oSpinner.Position = [256 324 57 22];
            app.oSpinner.Value = 3;

            % Create FitButton
            app.FitButton = uibutton(app.Panel_4, 'push');
            app.FitButton.ButtonPushedFcn = createCallbackFcn(app, @FitButtonPushed, true);
            app.FitButton.BackgroundColor = [0.702 0.9686 0.6784];
            app.FitButton.Position = [244 277 66 31];
            app.FitButton.Text = 'Fit';

            % Create FitTable
            app.FitTable = uitable(app.Panel_4);
            app.FitTable.ColumnName = '';
            app.FitTable.RowName = {};
            app.FitTable.Position = [9 104 304 141];

            % Create R2EditField_2Label
            app.R2EditField_2Label = uilabel(app.Panel_4);
            app.R2EditField_2Label.HorizontalAlignment = 'right';
            app.R2EditField_2Label.Interpreter = 'tex';
            app.R2EditField_2Label.Position = [12 121 33 27];
            app.R2EditField_2Label.Text = 'R^2: ';

            % Create Label_2
            app.Label_2 = uilabel(app.Panel_4);
            app.Label_2.Position = [25 123 25 22];
            app.Label_2.Text = '';

            % Create R2EditField_2
            app.R2EditField_2 = uieditfield(app.Panel_4, 'numeric');
            app.R2EditField_2.Position = [51 124 58 19];

            % Create ContrastSliderLabel
            app.ContrastSliderLabel = uilabel(app.Panel_4);
            app.ContrastSliderLabel.HorizontalAlignment = 'right';
            app.ContrastSliderLabel.Position = [6 45 50 22];
            app.ContrastSliderLabel.Text = 'Contrast';

            % Create ContrastSlider
            app.ContrastSlider = uislider(app.Panel_4, 'range');
            app.ContrastSlider.Limits = [0 1];
            app.ContrastSlider.ValueChangingFcn = createCallbackFcn(app, @ContrastSliderValueChanging, true);
            app.ContrastSlider.Position = [68 69 233 3];
            app.ContrastSlider.Value = [0 1];

            % Create SetActiveConfigButton
            app.SetActiveConfigButton = uibutton(app.Panel_4, 'push');
            app.SetActiveConfigButton.ButtonPushedFcn = createCallbackFcn(app, @SetActiveConfigButtonPushed, true);
            app.SetActiveConfigButton.BackgroundColor = [0.8078 0.902 0.6824];
            app.SetActiveConfigButton.Icon = fullfile(pathToMLAPP, 'dlgapps_resources', 'icons8-save-as-active.png');
            app.SetActiveConfigButton.Position = [9 277 150 31];
            app.SetActiveConfigButton.FontSize = 10;
            app.SetActiveConfigButton.FontWeight = 'bold';
            app.SetActiveConfigButton.Text = 'Set as Active';

            % Create LoadConfigButton
            app.LoadConfigButton = uibutton(app.Panel_4, 'push');
            app.LoadConfigButton.ButtonPushedFcn = createCallbackFcn(app, @LoadConfigButtonPushed, true);
            app.LoadConfigButton.Position = [9 249 145 22];
            app.LoadConfigButton.Text = 'Load Config File';

            % Create SaveConfigButton
            app.SaveConfigButton = uibutton(app.Panel_4, 'push');
            app.SaveConfigButton.ButtonPushedFcn = createCallbackFcn(app, @SaveConfigButtonPushed, true);
            app.SaveConfigButton.Position = [165 249 145 22];
            app.SaveConfigButton.Text = 'Save Config File';

            % Create MeanEditFieldLabel
            app.MeanEditFieldLabel = uilabel(app.GenerateDataTab);
            app.MeanEditFieldLabel.HorizontalAlignment = 'right';
            app.MeanEditFieldLabel.FontSize = 8;
            app.MeanEditFieldLabel.Position = [7 17 29 22];
            app.MeanEditFieldLabel.Text = ' Mean:';

            % Create STDEditFieldLabel
            app.STDEditFieldLabel = uilabel(app.GenerateDataTab);
            app.STDEditFieldLabel.HorizontalAlignment = 'right';
            app.STDEditFieldLabel.FontSize = 8;
            app.STDEditFieldLabel.Position = [262 17 25 22];
            app.STDEditFieldLabel.Text = 'STD:';

            % Create MeanEditField
            app.MeanEditField = uieditfield(app.GenerateDataTab, 'numeric');
            app.MeanEditField.FontSize = 10;
            app.MeanEditField.Position = [14 5 27 15];

            % Create STDEditField
            app.STDEditField = uieditfield(app.GenerateDataTab, 'numeric');
            app.STDEditField.FontSize = 10;
            app.STDEditField.Position = [269 5 27 16];

            % Show the figure after all components are created
            app.CalibrationToolsUIFigure.Visible = 'on';
        end
    end

    % App creation and deletion
    methods (Access = public)

        % Construct app
        function app = CalibrationTools_exported(varargin)

            % Create UIFigure and components
            createComponents(app)

            % Register the app with App Designer
            registerApp(app, app.CalibrationToolsUIFigure)

            % Execute the startup function
            runStartupFcn(app, @(app)startupFcn(app, varargin{:}))

            if nargout == 0
                clear app
            end
        end

        % Code that executes before app deletion
        function delete(app)

            % Delete UIFigure when app is deleted
            delete(app.CalibrationToolsUIFigure)
        end
    end
end