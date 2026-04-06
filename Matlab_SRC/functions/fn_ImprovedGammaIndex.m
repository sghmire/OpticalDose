function [gammaImg, passingRate] = fn_ImprovedGammaIndex(refPlane, measPlane, varargin)
% FN_IMPROVEDGAMMAINDEX Calculates the gamma index for two dose distributions
%
% Usage:
%   [gammaImg, passingRate] = fn_ImprovedGammaIndex(refPlane, measPlane)
%   [gammaImg, passingRate] = fn_ImprovedGammaIndex(refPlane, measPlane, 'ParameterName', ParameterValue, ...)
%
% Inputs:
%   refPlane    - 2D matrix of the reference dose distribution
%   measPlane   - 2D matrix of the measured dose distribution
%
% Optional Name-Value Pair Arguments:
%   'Type'      - 'Absolute' or 'Relative' comparison (default: 'Relative')
%   'GammaType' - 'Global' or 'Local' normalization (default: 'Global')
%   'DTA'       - Distance to agreement in mm (default: 3)
%   'DD'        - Dose difference criterion in % (default: 3)
%   'PixelSize' - Pixel size in mm (default: 1)
%   'Threshold' - Low dose threshold in % of max dose (default: 10)
%   'Interp'    - Use interpolation for sub-pixel accuracy (default: false)
%
% Outputs:
%   gammaImg    - 2D matrix of gamma index values
%   passingRate - Percentage of points passing the gamma criterion

    % Parse inputs
    p = inputParser;
    addParameter(p, 'Type', 'Relative', @(x) any(validatestring(x,{'Absolute','Relative'})));
    addParameter(p, 'GammaType', 'Global', @(x) any(validatestring(x,{'Global','Local'})));
    addParameter(p, 'DTA', 3, @(x) isnumeric(x) && x > 0);
    addParameter(p, 'DD', 3, @(x) isnumeric(x) && x > 0);
    addParameter(p, 'PixelSize', 1, @(x) isnumeric(x) && x > 0);
    addParameter(p, 'Threshold', 10, @(x) isnumeric(x) && x >= 0 && x <= 100);
    addParameter(p, 'Interp', false, @islogical);
    parse(p, varargin{:});
    
    % Extract parameters
    Type = p.Results.Type;
    GammaType = p.Results.GammaType;
    DTA = p.Results.DTA;
    DD = p.Results.DD;
    PixelSize = p.Results.PixelSize;
    Threshold = p.Results.Threshold;
    useInterp = p.Results.Interp;

    % Handle different sized planes
    [refPlane, measPlane] = alignPlanes(refPlane, measPlane);

    % Apply threshold
    maxRefDose = max(refPlane(:));
    threshold = maxRefDose * Threshold / 100;
    refPlane(refPlane < threshold) = 0;
    measPlane(measPlane < threshold) = 0;

    % Normalize if in relative mode
    if strcmp(Type, 'Relative')
        refPlane = normalizeArray(refPlane);
        measPlane = normalizeArray(measPlane);
    end

    % Create distance kernel
    kernelSize = ceil(3 * DTA / PixelSize);
    [X, Y] = meshgrid(-kernelSize:kernelSize, -kernelSize:kernelSize);
    distKernel = sqrt(X.^2 + Y.^2) * PixelSize;

    % Initialize gamma image
    gammaImg = inf(size(refPlane));

    % Calculate gamma
    for i = 1:numel(distKernel)
        shiftedRef = circshift(refPlane, [X(i), Y(i)]);
        
        if strcmp(GammaType, 'Global')
            doseDiff = abs(measPlane - shiftedRef) / maxRefDose * 100;
        else
            doseDiff = abs(measPlane - shiftedRef) ./ shiftedRef * 100;
            doseDiff(shiftedRef == 0) = inf;
        end

        gamma = sqrt((doseDiff / DD).^2 + (distKernel(i) / DTA).^2);
        gammaImg = min(gammaImg, gamma);

        % Optional: Report progress
        if mod(i, 100) == 0
            fprintf('Progress: %.1f%%\n', i / numel(distKernel) * 100);
        end
    end

    % Sub-pixel interpolation (if enabled)
    if useInterp
        gammaImg = interpGamma(gammaImg, refPlane, measPlane, DTA, DD, PixelSize, GammaType);
    end

    % Calculate passing rate
    passingRate = sum(gammaImg(:) <= 1) / numel(gammaImg) * 100;
end

function [alignedRef, alignedMeas] = alignPlanes(refPlane, measPlane)
    [r_ref, c_ref] = size(refPlane);
    [r_meas, c_meas] = size(measPlane);
    
    % Determine the size of the output planes
    r_out = min(r_ref, r_meas);
    c_out = min(c_ref, c_meas);
    
    % Crop both planes to the smaller size
    win_ref = centerCropWindow2d([r_ref, c_ref], [r_out, c_out]);
    win_meas = centerCropWindow2d([r_meas, c_meas], [r_out, c_out]);
    
    alignedRef = imcrop(refPlane, win_ref);
    alignedMeas = imcrop(measPlane, win_meas);
end

function normArray = normalizeArray(arr)
    normArray = (arr - min(arr(:))) / (max(arr(:)) - min(arr(:))) * 99 + 1;
end

function interpGammaImg = interpGamma(gammaImg, refPlane, measPlane, DTA, DD, PixelSize, GammaType)
    [X, Y] = meshgrid(1:size(gammaImg, 2), 1:size(gammaImg, 1));
    [Xq, Yq] = meshgrid(1:0.1:size(gammaImg, 2), 1:0.1:size(gammaImg, 1));
    
    interpRefPlane = interp2(X, Y, refPlane, Xq, Yq, 'cubic');
    interpMeasPlane = interp2(X, Y, measPlane, Xq, Yq, 'cubic');
    
    if strcmp(GammaType, 'Global')
        doseDiff = abs(interpMeasPlane - interpRefPlane) / max(refPlane(:)) * 100;
    else
        doseDiff = abs(interpMeasPlane - interpRefPlane) ./ interpRefPlane * 100;
        doseDiff(interpRefPlane == 0) = inf;
    end
    
    distDiff = sqrt((Xq - X).^2 + (Yq - Y).^2) * PixelSize;
    
    interpGamma = sqrt((doseDiff / DD).^2 + (distDiff / DTA).^2);
    interpGammaImg = min(gammaImg, min(interpGamma, [], 3));
end