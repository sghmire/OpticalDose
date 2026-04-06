classdef DicomDummy_exported < matlab.apps.AppBase

    % Properties that correspond to app components
    properties (Access = public)
        DicomDummyUIFigure     matlab.ui.Figure
        Menu                   matlab.ui.container.Menu
        Menu_2                 matlab.ui.container.Menu
        Panel                  matlab.ui.container.Panel
        ZEditField             matlab.ui.control.NumericEditField
        ZEditFieldLabel        matlab.ui.control.Label
        YEditField             matlab.ui.control.NumericEditField
        YEditFieldLabel        matlab.ui.control.Label
        XEditField             matlab.ui.control.NumericEditField
        XEditFieldLabel        matlab.ui.control.Label
        PhantomCenterDropDown  matlab.ui.control.DropDown
        PhantomCenterLabel     matlab.ui.control.Label
        CloneButton            matlab.ui.control.Button
        EditField_2            matlab.ui.control.EditField
        EditField              matlab.ui.control.EditField
        EcliDoseButton         matlab.ui.control.Button
        ExtDoseButton          matlab.ui.control.Button
    end

    
    properties (Access = private)
        ext_dose;
        eclip_dose;% Description
        Mainapp; % Description
        path;
    end
    

    % Callbacks that handle component events
    methods (Access = private)

        % Code that executes after component creation
        function startupFcn(app, mainapp, path)
            app.Mainapp  = mainapp;
            app.path = path;
        end

        % Value changed function: PhantomCenterDropDown
        function PhantomCenterDropDownValueChanged(app, event)
            switch app.PhantomCenterDropDown.Value 
                case  'None'
                    app.XEditField.Value = 0;
                    app.YEditField.Value = 0;
                    app.ZEditField.Value = 0;

                    app.XEditField.Editable = "off";
                    app.YEditField.Editable = "off";
                    app.ZEditField.Editable = "off";
                case 'Center of Body'
                    app.XEditField.Value = 0.09;
                    app.YEditField.Value = -25;
                    app.ZEditField.Value = 3.20;

                    app.XEditField.Editable = "off";
                    app.YEditField.Editable = "off";
                    app.ZEditField.Editable = "off";
                case 'Center of Chamber'
                    app.XEditField.Value = -0.01;
                    app.YEditField.Value = -25.30;
                    app.ZEditField.Value = 0.12;

                    app.XEditField.Editable = "off";
                    app.YEditField.Editable = "off";
                    app.ZEditField.Editable = "off";
                case 'Volume Isocenter'
                    app.XEditField.Value = -31.03;
                    app.YEditField.Value = 0;
                    app.ZEditField.Value = 0;

                    app.XEditField.Editable = "off";
                    app.YEditField.Editable = "off";
                    app.ZEditField.Editable = "off";
                case 'Manual'
                    app.XEditField.Editable = "on";
                    app.YEditField.Editable = "on";
                    app.ZEditField.Editable = "on";
            end
            
        end

        % Button pushed function: ExtDoseButton
        function ExtDoseButtonPushed(app, event)
            [file, app.path] = uigetfile({'*.dcm'}, "Please select the external dose file", app.path);
            app.ext_dose = dicominfo(fullfile(app.path, file));   
            app.EditField.Value = file;
        end

        % Button pushed function: EcliDoseButton
        function EcliDoseButtonPushed(app, event)
            [file, app.path] = uigetfile({'*.dcm'}, "Please select the external dose file", app.path);
            app.eclip_dose = dicominfo(fullfile(app.path, file));  
            app.EditField_2.Value = file;
        end

        % Button pushed function: CloneButton
        function CloneButtonPushed(app, event)
            
            Realdose = app.ext_dose;
            fakedose = app.eclip_dose;


            RealimageData = dicomread(Realdose);
            FakeImageData = dicomread(fakedose);

            Real_IPP_x = Realdose.ImagePositionPatient(1) + app.XEditField.Value;
            Real_IPP_y = Realdose.ImagePositionPatient(2) + app.YEditField.Value;
            Real_IPP_z = fakedose.ImagePositionPatient(3) + app.ZEditField.Value;

            fakedose.ImagePositionPatient = [Real_IPP_x, Real_IPP_y, Real_IPP_z];
            fakedose.DoseGridScaling = Realdose.DoseGridScaling;
            fakedose.ImageOrientationPatient = Realdose.ImageOrientationPatient;
            fakedose.Rows = Realdose.Rows;
            fakedose.NumberOfFrames = Realdose.NumberOfFrames;
            fakedose.Columns = Realdose.Columns;
            fakedose.PixelSpacing = Realdose.PixelSpacing;
            fakedose.GridFrameOffsetVector = Realdose.GridFrameOffsetVector;

            % Write the new DICOM file
            [file, app.path] = uiputfile({'*.dcm'}, 'Select the folder', app.path);
            if file ~= 0
                filename = fullfile(app.path, file);
                dicomwrite(RealimageData, filename, fakedose, 'CreateMode', 'copy');
                msgbox("Dose file cloned successfully!");
            end

        end
    end

    % Component initialization
    methods (Access = private)

        % Create UIFigure and components
        function createComponents(app)

            % Create DicomDummyUIFigure and hide until all components are created
            app.DicomDummyUIFigure = uifigure('Visible', 'off');
            app.DicomDummyUIFigure.Position = [100 100 375 273];
            app.DicomDummyUIFigure.Name = 'DicomDummy';

            % Create Menu
            app.Menu = uimenu(app.DicomDummyUIFigure);
            app.Menu.Text = 'Menu';

            % Create Menu_2
            app.Menu_2 = uimenu(app.DicomDummyUIFigure);

            % Create ExtDoseButton
            app.ExtDoseButton = uibutton(app.DicomDummyUIFigure, 'push');
            app.ExtDoseButton.ButtonPushedFcn = createCallbackFcn(app, @ExtDoseButtonPushed, true);
            app.ExtDoseButton.Position = [16 238 76 23];
            app.ExtDoseButton.Text = '...Ext Dose';

            % Create EcliDoseButton
            app.EcliDoseButton = uibutton(app.DicomDummyUIFigure, 'push');
            app.EcliDoseButton.ButtonPushedFcn = createCallbackFcn(app, @EcliDoseButtonPushed, true);
            app.EcliDoseButton.Position = [16 204 76 23];
            app.EcliDoseButton.Text = '...Ecli Dose';

            % Create EditField
            app.EditField = uieditfield(app.DicomDummyUIFigure, 'text');
            app.EditField.Position = [95 240 264 19];

            % Create EditField_2
            app.EditField_2 = uieditfield(app.DicomDummyUIFigure, 'text');
            app.EditField_2.Position = [95 206 264 19];

            % Create CloneButton
            app.CloneButton = uibutton(app.DicomDummyUIFigure, 'push');
            app.CloneButton.ButtonPushedFcn = createCallbackFcn(app, @CloneButtonPushed, true);
            app.CloneButton.BackgroundColor = [0.6157 0.8588 0.4392];
            app.CloneButton.FontSize = 18;
            app.CloneButton.FontWeight = 'bold';
            app.CloneButton.Position = [144 23 100 40];
            app.CloneButton.Text = 'Clone ';

            % Create Panel
            app.Panel = uipanel(app.DicomDummyUIFigure);
            app.Panel.Position = [16 80 343 85];

            % Create PhantomCenterLabel
            app.PhantomCenterLabel = uilabel(app.Panel);
            app.PhantomCenterLabel.HorizontalAlignment = 'right';
            app.PhantomCenterLabel.Position = [1 53 96 22];
            app.PhantomCenterLabel.Text = 'Phantom Center:';

            % Create PhantomCenterDropDown
            app.PhantomCenterDropDown = uidropdown(app.Panel);
            app.PhantomCenterDropDown.Items = {'None', 'Center of Body', 'Center of Chamber', 'Volume Isocenter', 'Manual'};
            app.PhantomCenterDropDown.ValueChangedFcn = createCallbackFcn(app, @PhantomCenterDropDownValueChanged, true);
            app.PhantomCenterDropDown.Position = [111 53 182 22];
            app.PhantomCenterDropDown.Value = 'None';

            % Create XEditFieldLabel
            app.XEditFieldLabel = uilabel(app.Panel);
            app.XEditFieldLabel.HorizontalAlignment = 'right';
            app.XEditFieldLabel.Position = [52 10 25 22];
            app.XEditFieldLabel.Text = 'X:';

            % Create XEditField
            app.XEditField = uieditfield(app.Panel, 'numeric');
            app.XEditField.Position = [92 10 37 22];

            % Create YEditFieldLabel
            app.YEditFieldLabel = uilabel(app.Panel);
            app.YEditFieldLabel.HorizontalAlignment = 'right';
            app.YEditFieldLabel.Position = [128 10 25 22];
            app.YEditFieldLabel.Text = 'Y:';

            % Create YEditField
            app.YEditField = uieditfield(app.Panel, 'numeric');
            app.YEditField.Position = [168 10 37 22];

            % Create ZEditFieldLabel
            app.ZEditFieldLabel = uilabel(app.Panel);
            app.ZEditFieldLabel.HorizontalAlignment = 'right';
            app.ZEditFieldLabel.Position = [215 10 25 22];
            app.ZEditFieldLabel.Text = 'Z:';

            % Create ZEditField
            app.ZEditField = uieditfield(app.Panel, 'numeric');
            app.ZEditField.Position = [256 10 37 22];

            % Show the figure after all components are created
            app.DicomDummyUIFigure.Visible = 'on';
        end
    end

    % App creation and deletion
    methods (Access = public)

        % Construct app
        function app = DicomDummy_exported(varargin)

            % Create UIFigure and components
            createComponents(app)

            % Register the app with App Designer
            registerApp(app, app.DicomDummyUIFigure)

            % Execute the startup function
            runStartupFcn(app, @(app)startupFcn(app, varargin{:}))

            if nargout == 0
                clear app
            end
        end

        % Code that executes before app deletion
        function delete(app)

            % Delete UIFigure when app is deleted
            delete(app.DicomDummyUIFigure)
        end
    end
end