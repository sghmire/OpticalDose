function [Film_augmented] = fn_AugmentFilmDose(DoseMatrix, DPI, interp_grid)

    DPI = DPI * interp_grid;

    % Size of Film plane in pixels
    [f_row_px, f_col_px, ~] = size(DoseMatrix);
    

    % Size of Film plane in mm
    f_row_mm = f_row_px * 25.4 / DPI; % convert pixels to mm using DPI
    f_col_mm = f_col_px * 25.4 / DPI;

    % Calculate center points
    film_center_x = f_col_mm / 2;
    film_center_y = f_row_mm / 2;
    
    % Create coordinate vectors
    film_x = linspace(-film_center_x, film_center_x, f_col_px);
    film_y = linspace(-film_center_y, film_center_y, f_row_px);


    % Same process for the Film plane
    Film_augmented = zeros(f_row_px + 1, f_col_px + 1);
    Film_augmented(2:end, 2:end) = DoseMatrix;   
    Film_augmented(1, 2:end) = film_x;          
    Film_augmented(2:end, 1) = film_y'; 

end
