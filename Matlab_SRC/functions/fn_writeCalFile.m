function fn_writeCalFile(projectRoot, fileName, channel, fitMode, degree, first_fit, second_fit, third_fit, delta_opt, rawData)
% fn_writeCalFile  Save current polynomial calibration fit to a specified txt file
%
%   projectRoot : full path to the FilmDosi_Converted folder
%   fileName    : e.g. 'CalibConfig_Active.txt' or 'CalibConfig_Default.txt'
%   channel     : channel mode string
%   fitMode     : same as channel (the fittype string from CalibrationTools)
%   degree      : polynomial degree (integer)
%   first_fit   : first polynomial coefficients vector
%   second_fit  : second polynomial coefficients vector (may be empty)
%   third_fit   : third polynomial coefficients vector (may be empty)
%   delta_opt   : optimised delta scalar (triple-channel only, may be empty)
%   rawData     : explicitly provided UI table data containing scatter points to save

    if nargin < 10
        rawData = [];
    end

    filePath = fullfile(projectRoot, fileName);
    fileID   = fopen(filePath, 'w');
    if fileID == -1
        warning('fn_writeCalFile: could not open %s for writing.', filePath);
        return;
    end

    fprintf(fileID, '# FilmDosimetry Calibration Configuration\r\n');
    fprintf(fileID, '# Saved: %s\r\n', datestr(now, 'yyyy-mm-dd HH:MM:SS'));
    fprintf(fileID, '\r\n');
    fprintf(fileID, 'Channel:  %s\r\n', channel);
    fprintf(fileID, 'FitMode:  %s\r\n', fitMode);
    fprintf(fileID, 'Degree:   %d\r\n', degree);
    fprintf(fileID, '\r\n');

    if ~isempty(rawData)
        fprintf(fileID, '[RawData]\r\n');
        for i = 1:size(rawData, 1)
            fprintf(fileID, '%g\t', rawData(i, :));
            fprintf(fileID, '\r\n');
        end
        fprintf(fileID, '\r\n');
    end

    fprintf(fileID, '[FirstFit]\r\n');
    fprintf(fileID, '%g ', first_fit);
    fprintf(fileID, '\r\n\r\n');

    if ~isempty(second_fit)
        fprintf(fileID, '[SecondFit]\r\n');
        fprintf(fileID, '%g ', second_fit);
        fprintf(fileID, '\r\n\r\n');
    end

    if ~isempty(third_fit)
        fprintf(fileID, '[ThirdFit]\r\n');
        fprintf(fileID, '%g ', third_fit);
        fprintf(fileID, '\r\n\r\n');
    end

    if ~isempty(delta_opt)
        fprintf(fileID, '[DeltaOpt]\r\n');
        fprintf(fileID, '%g\r\n', delta_opt);
    end

    fclose(fileID);
end
