function [TPS_augmented] = fn_AugmentPlanDose(dicomplane, pixelspacing, interp_grid, x_zero, y_zero)

    % Size of TPS plane in pixels
    [t_row_px, t_col_px, ~] = size(dicomplane);
    pixelspacing = pixelspacing / interp_grid;
    
    % Convert the specified zero coordinates from pixels to millimeters
    x_zero_mm = (x_zero - 1) * pixelspacing(2);
    y_zero_mm = (y_zero - 1) * pixelspacing(1);
    
    % Create corrected coordinate vectors with specified zero point
    tps_x = linspace(-x_zero_mm, (t_col_px - x_zero) * pixelspacing(2), t_col_px);
    tps_y = linspace(-y_zero_mm, (t_row_px - y_zero) * pixelspacing(1), t_row_px);
    
    % Create augmented matrices (increased by 1 row and column to store coordinates)
    TPS_augmented = zeros(t_row_px + 1, t_col_px + 1);
    TPS_augmented(2:end, 2:end) = dicomplane;
    TPS_augmented(1, 2:end) = tps_x;
    TPS_augmented(2:end, 1) = tps_y';
    
end
