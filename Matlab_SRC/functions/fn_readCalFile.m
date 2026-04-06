function [channel, fitMode, degree, first_fit, second_fit, third_fit, delta_opt, rawData] = fn_readCalFile(filePath)
% fn_readCalFile  Load a CurrentCalibration.txt saved by fn_writeCalFile
%
%   Returns empty arrays for any sections not present in the file.

    channel    = '';
    fitMode    = '';
    degree     = 1;
    first_fit  = [];
    second_fit = [];
    third_fit  = [];
    delta_opt  = [];
    rawData    = [];

    fileID = fopen(filePath, 'r');
    if fileID == -1
        error('fn_readCalFile: cannot open file: %s', filePath);
    end

    currentSection = '';
    while ~feof(fileID)
        line = strtrim(fgetl(fileID));
        if isempty(line) || startsWith(line, '#')
            continue;
        end

        if startsWith(line, 'Channel:')
            channel = strtrim(extractAfter(line, 'Channel:'));
        elseif startsWith(line, 'FitMode:')
            fitMode = strtrim(extractAfter(line, 'FitMode:'));
        elseif startsWith(line, 'Degree:')
            degree = str2double(strtrim(extractAfter(line, 'Degree:')));
        elseif startsWith(line, '[RawData]')
            currentSection = 'rawdata';
        elseif startsWith(line, '[FirstFit]')
            currentSection = 'first';
        elseif startsWith(line, '[SecondFit]')
            currentSection = 'second';
        elseif startsWith(line, '[ThirdFit]')
            currentSection = 'third';
        elseif startsWith(line, '[DeltaOpt]')
            currentSection = 'delta';
        elseif ~isempty(currentSection)
            vals = str2num(line); %#ok<ST2NM>
            switch currentSection
                case 'rawdata'
                    if ~isempty(vals)
                        rawData = [rawData; vals];
                    end
                case 'first'
                    first_fit  = vals;
                    currentSection = '';
                case 'second'
                    second_fit = vals;
                    currentSection = '';
                case 'third'
                    third_fit  = vals;
                    currentSection = '';
                case 'delta'
                    delta_opt  = vals;
                    currentSection = '';
            end
        end
    end
    fclose(fileID);
end
