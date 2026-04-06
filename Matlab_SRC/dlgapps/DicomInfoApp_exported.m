classdef DicomInfoApp_exported < matlab.apps.AppBase

    % Properties that correspond to app components
    properties (Access = public)
        DicomInfoUIFigure  matlab.ui.Figure
        UITable            matlab.ui.control.Table
    end

    
    properties (Access = private)
        Mainapp;
    end
    
    methods (Access = private)
        
        
    end
    

    % Callbacks that handle component events
    methods (Access = private)

        % Code that executes after component creation
        function startupFcn(app, mainapp, DicomInfo)
            info = DicomInfo;
            allFields = fieldnames(info);
            tableData = cell(length(allFields), 2); % Initialize table data
            
            for i = 1:length(allFields)
                fieldName = allFields{i};
                fieldValue = info.(fieldName);
            
                if isstruct(fieldValue)
                    fieldValue = '[Nested Structure]'; 
                end
            
                tableData{i, 1} = fieldName;
                tableData{i, 2} = fieldValue; 
            end
            
            dicomTable = table(tableData(:, 1), tableData(:, 2), 'VariableNames', {'Field', 'Value'});
            app.UITable.Data = dicomTable;
            app.UITable.CellSelectionCallback = @app.myTableCellClickedCallback;      
        end
    end

    % Component initialization
    methods (Access = private)

        % Create UIFigure and components
        function createComponents(app)

            % Get the file path for locating images
            pathToMLAPP = fileparts(mfilename('fullpath'));

            % Create DicomInfoUIFigure and hide until all components are created
            app.DicomInfoUIFigure = uifigure('Visible', 'off');
            app.DicomInfoUIFigure.Position = [100 100 624 780];
            app.DicomInfoUIFigure.Name = 'DicomInfo';
            app.DicomInfoUIFigure.Icon = fullfile(pathToMLAPP, 'dlgapps_resources', 'icons8-medical-80.png');

            % Create UITable
            app.UITable = uitable(app.DicomInfoUIFigure);
            app.UITable.ColumnName = {'Meta'; 'Data'};
            app.UITable.RowName = {};
            app.UITable.Position = [25 19 581 747];

            % Show the figure after all components are created
            app.DicomInfoUIFigure.Visible = 'on';
        end
    end

    % App creation and deletion
    methods (Access = public)

        % Construct app
        function app = DicomInfoApp_exported(varargin)

            % Create UIFigure and components
            createComponents(app)

            % Register the app with App Designer
            registerApp(app, app.DicomInfoUIFigure)

            % Execute the startup function
            runStartupFcn(app, @(app)startupFcn(app, varargin{:}))

            if nargout == 0
                clear app
            end
        end

        % Code that executes before app deletion
        function delete(app)

            % Delete UIFigure when app is deleted
            delete(app.DicomInfoUIFigure)
        end
    end
end