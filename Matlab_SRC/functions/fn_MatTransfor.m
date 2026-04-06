function tMatrix = fn_MatTransfor(iMatrix, SelectedPoint)
    [rows, cols, channels] = size(iMatrix);
    ImageCenter = [cols / 2, rows / 2];
    Translation = (ImageCenter - SelectedPoint);
    
    % Calculate padding needed to keep the translated image within view
    padX = abs(Translation(1));
    padY = abs(Translation(2));
    
    % Determine new canvas size with padding, ensuring integer values
    newCols = round(cols + 2 * padX);
    newRows = round(rows + 2 * padY);
    
    % Define the rigid transformation with the translation
    tform = rigidtform2d(eye(2), Translation);
    
    % Create an output view with the new padded dimensions
    followOutput = affineOutputView([newRows, newCols], tform, "BoundsStyle", "CenterOutput");
    
    % Initialize the transformed matrix with the padded dimensions
    tMatrix = zeros(followOutput.ImageSize, 'like', iMatrix);
    
    % Apply transformation to each channel with padding
    for ch = 1:channels
        paddedChannel = padarray(iMatrix(:, :, ch), [round(padY), round(padX)], 0, 'both');
        tMatrix(:, :, ch) = imwarp(paddedChannel, tform, "OutputView", followOutput);
    end
end
