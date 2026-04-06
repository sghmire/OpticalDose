% Simple Gamma Analysis by SG
% refPlane: Dicom 2D dose plane
% measPlane: Film 2D dose plane

function [gammaImg, passingRate] = fn_SimpleGammaIndex(refPlane, measPlane, Type, GammaType, windowSize, DTA, DD, PixelSize, percentageValue)

    [r_ref, c_ref] = size(refPlane);
    [r_meas, c_meas] = size(measPlane);

    % Checking ref and meas plane size to process only the measured-sized plane
    if (r_meas < r_ref) && (c_meas < c_ref)
        target_size = [r_meas, c_meas];
        win1 = centerCropWindow2d([r_ref, c_ref], target_size);
        refPlane = imcrop(refPlane, win1);
        [rows, cols] = size(refPlane);
    else
        target_size = [r_ref, c_ref];
        win1 = centerCropWindow2d([r_meas, c_meas], target_size);
        measPlane = imcrop(measPlane, win1);
        [rows, cols] = size(measPlane);
    end

    % Initialize the gamma map
    gammaImg = zeros(rows, cols);

    passingCount = 0;
    maxRefPlane = max(refPlane(:));

    % Remove the irrelevant signal
    perSignal = maxRefPlane * percentageValue * 1/100;
    refPlane(abs(refPlane) <= perSignal) = 0;
    measPlane(abs(measPlane) <= perSignal) = 0;

    % Normalize the ref and meas planes if in 'Relative' mode
    if strcmp(Type, "Relative")
        refPlane = 1 + 99 * (refPlane - min(refPlane(:))) / (max(refPlane(:)) - min(refPlane(:)));
        measPlane = 1 + 99 * (measPlane - min(measPlane(:))) / (max(measPlane(:)) - min(measPlane(:)));
    end

    % Precompute the delta_distance matrix for vectorization
    [X, Y] = meshgrid(1:2*windowSize+1, 1:2*windowSize+1);
    delta_distance = sqrt((X - (windowSize + 1)).^2 + (Y - (windowSize + 1)).^2) * PixelSize;

    % Loop through each pixel in the refPlane and measPlane
    for i = 1 + windowSize : rows - windowSize
        for j = 1 + windowSize : cols - windowSize
            
            % Initialize gamma to a large value
            min_gamma = inf;

            % Extract the window of reference and measured planes around (i,j)
            refWindow = refPlane(i - windowSize : i + windowSize, j - windowSize : j + windowSize);
            measWindow = measPlane(i - windowSize : i + windowSize, j - windowSize : j + windowSize);

            % Calculate gamma for all pixels in the window using vectorized operations
            if strcmp(GammaType, 'Global')
                delta_dose = abs(measWindow - refWindow) * 100 ./ maxRefPlane; % Global normalization
            else
                delta_dose = abs(measWindow - refWindow) * 100 ./ refWindow; % Local normalization
                % Handle cases where refWindow is zero to avoid division by zero
                delta_dose(refWindow == 0) = inf;
            end

            % Calculate gamma index for all pixels in the window
            gamma = sqrt((delta_distance / DTA).^2 + (delta_dose / DD).^2);

            % Find the minimum gamma for the window
            min_gamma = min(gamma(:));

            % Assign the minimum gamma to the current pixel location
            gammaImg(i, j) = min_gamma;

            % Update passing count based on the minimum gamma value
            if min_gamma <= 1
                passingCount = passingCount + 1;
                gammaImg(i, j) = min_gamma - (0.15 * min_gamma);  % Optional visualization adjustment
            end
        end
    end

    % Calculate the passing rate
    totalEvaluatedPixels = (rows - 2 * windowSize) * (cols - 2 * windowSize);
    passingRate = passingCount * 100 / totalEvaluatedPixels;

end
