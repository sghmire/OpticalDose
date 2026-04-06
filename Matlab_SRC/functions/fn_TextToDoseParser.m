function [DoseMatrix, DPI, Interp, X_Size, Y_Size, file] = fn_TextToDoseParser(path)

    % Initialize outputs to empty arrays for safe cancellation handling
    DoseMatrix = [];
    DPI = [];
    Interp = [];
    X_Size = [];
    Y_Size = [];
    file = [];
    
    % Open file dialog
    [file, path] = uigetfile({'*.txt'}, 'Please select the film dose file', path);
    if isequal(file, 0) || isequal(path, 0)
        % User canceled the file selection
        disp('File selection canceled by user.');
        return;
    end
    
    % Construct the full path to the selected file
    text_file = fullfile(path, file);
    
    % Initialize data cell array
    data = cell(5, 1);
    
    % Open the file
    ID = fopen(text_file, 'r');
    if ID == -1
        error('File cannot be opened');
    end
    
    % Read specific lines for values
    for i = 1:5
        line = fgetl(ID);
        parts = strsplit(line, ':');
        value = strtrim(parts{2});
        data{i, 1} = value;
    end
    
    % Parse read data into variables
    DPI = str2double(data{2});
    Interp = str2double(data{3});
    X_Size = str2double(data{4});
    Y_Size = str2double(data{5});
    
    % Reset file pointer to the beginning of the file to read matrix
    frewind(ID);
    string1 = 'Array Start:';
    string2 = ':Array End';
    startReading = false;
    Matrix = [];
    
    % Create waitbar for progress
    f = waitbar(0, 'Opening the dose file!');
    
    % Read the file again to find the matrix
    while ~feof(ID)
        currentLine = fgetl(ID);
        if contains(currentLine, string1)
            startReading = true;
            continue; % Skip the line containing 'Array Start:'
        elseif contains(currentLine, string2)
            break; % Stop reading if 'Array End' is found
        end
        
        if startReading
            waitbar(0.5, f, 'Loading dose file!');
            numericData = str2num(currentLine); %#ok<ST2NM>
            Matrix = [Matrix; numericData]; %#ok<AGROW>
        end
    end
    
    % Close the waitbar
    waitbar(1, f, 'Done!');
    close(f);
    
    % Close the file
    fclose(ID);
    
    % Assign the matrix to the output
    DoseMatrix = double(Matrix);

end
