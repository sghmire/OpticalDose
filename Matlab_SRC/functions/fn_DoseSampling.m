function interpolatedMatrix = fn_DoseSampling(DoseMatrix, samplingDensity, method)
    % Ensure the input matrix is double for interpolation
    DoseMatrix = double(DoseMatrix);

    % If samplingDensity is 1, return the original (reset functionality)
    if samplingDensity == 1
        interpolatedMatrix = DoseMatrix;
        return;
    end

    % Validate interpolation method
    validMethods = {'nearest', 'linear', 'cubic', 'spline'};
    if ~ismember(lower(method), validMethods)
        warning('Interpolation method "%s" not recognized. Falling back to "linear".', method);
        method = 'linear';
    end

    % Prevent excessive upscaling
    samplingDensity = max(1, min(samplingDensity, 10));

    % Define the original grid
    [m, n] = size(DoseMatrix);
    [x, y] = meshgrid(1:n, 1:m);

    % Define the new grid with increased sampling density
    new_n = round(n * samplingDensity);
    new_m = round(m * samplingDensity);
    newGridX = linspace(1, n, new_n);
    newGridY = linspace(1, m, new_m);
    [newX, newY] = meshgrid(newGridX, newGridY);

    % Interpolate onto the new grid
    interpolatedMatrix = interp2(x, y, DoseMatrix, newX, newY, method);
end
